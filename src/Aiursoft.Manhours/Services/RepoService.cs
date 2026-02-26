using Aiursoft.Manhours.Entities;
using Aiursoft.Scanner.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Aiursoft.Canon;
using Aiursoft.GitRunner;
using Aiursoft.GitRunner.Models;
using Aiursoft.CSTools.Tools;
using Aiursoft.Manhours.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Aiursoft.Manhours.Services;

public class RepoService(
    RetryEngine retryEngine,
    ManhoursDbContext dbContext,
    ILogger<RepoService> logger,
    WorkspaceManager workspaceManager,
    IConfiguration configuration,
    IMemoryCache cache) : IScopedDependency
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Lockers = new();
    private static readonly Regex RepoNameRegex = new("^[a-zA-Z0-9._/-]+$", RegexOptions.Compiled);
    private readonly string _workspaceFolder = Path.Combine(configuration["Storage:Path"]!, "Repos");

    // Global semaphore: only 1 git fetch at a time across the entire application.
    // On slow disks, concurrent git operations are catastrophic.
    private static readonly SemaphoreSlim GlobalGitSemaphore = new(1, 1);

    // DB cache freshness period (was 6 hours — far too short for statistical data)
    private const int DbCacheFreshnessHours = 24;

    // Memory cache TTL for full repo stats
    private static readonly TimeSpan MemoryCacheTtl = TimeSpan.FromHours(6);
    private const string MemCachePrefix = "RepoStatsFullMemory_";

    private void ValidateRepoInput(string repoName, string repoUrl)
    {
        if (string.IsNullOrWhiteSpace(repoName) || !RepoNameRegex.IsMatch(repoName))
        {
            throw new ArgumentException(@"Invalid repository name.", nameof(repoName));
        }

        if (repoName.Contains("..") || repoName.Contains(':'))
        {
            throw new ArgumentException(@"Invalid repository name format.", nameof(repoName));
        }

        if (string.IsNullOrWhiteSpace(repoUrl) ||
            (!repoUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
             !repoUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException(@"Invalid repository URL. Only HTTPS/HTTP are allowed.", nameof(repoUrl));
        }

        // Additional check for shell metacharacters in URL
        var illegalChars = new[] { ';', '&', '|', '`', '$', '(', ')', '<', '>', '*', '?', '[', ']', '{', '}', '\\', '"', '\'' };
        if (repoUrl.Any(c => illegalChars.Contains(c)))
        {
            throw new ArgumentException(@"Invalid characters in repository URL.", nameof(repoUrl));
        }
    }

    private string GetWorkPath(string repoUrl)
    {
        var repoName = repoUrl;
        if (repoName.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            repoName = repoName["https://".Length..];
        }

        if (repoName.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            repoName = repoName["http://".Length..];
        }

        if (repoName.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            repoName = repoName[..^4];
        }

        var repoLocalPath = repoName
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace(':', Path.DirectorySeparatorChar)
            .Replace('@', Path.DirectorySeparatorChar)
            .Replace('?', '_')
            .Replace('*', '_');

        return Path.GetFullPath(Path.Combine(_workspaceFolder, repoLocalPath));
    }

    public async Task<RepoStats> GetRepoStatsAsync(string repoName, string repoUrl)
    {
        ValidateRepoInput(repoName, repoUrl);

        // Layer 1: Memory cache (fastest, no DB hit, no disk hit)
        var memKey = MemCachePrefix + repoUrl;
        if (cache.TryGetValue<RepoStats>(memKey, out var memStats))
        {
            return memStats!;
        }

        // Layer 2: Fresh DB cache (DB hit only, no git/disk)
        var cachedRepo = await GetCachedRepoAsync(repoUrl);
        if (cachedRepo != null)
        {
            var result = ConvertToRepoStats(cachedRepo);
            SetMemoryCache(memKey, result, MemoryCacheTtl);
            return result;
        }

        // Layer 3: Stale DB cache — return stale data immediately (stale-while-revalidate)
        // The periodic UpdateRepoStatsJob will refresh it in the background.
        var staleRepo = await GetCachedRepoStaleOkAsync(repoUrl);
        if (staleRepo != null)
        {
            logger.LogInformation("Returning stale data for repo: {RepoUrl} (last updated: {LastUpdate})",
                repoUrl, staleRepo.LastUpdateTime);
            var result = ConvertToRepoStats(staleRepo);
            // Cache stale data in memory for a shorter period
            SetMemoryCache(memKey, result, TimeSpan.FromHours(1));
            return result;
        }

        // Layer 4: No data at all — must do git fetch (only path that hits disk)
        // Use both per-repo lock AND global semaphore to prevent IO storms
        var locker = Lockers.GetOrAdd(repoUrl, _ => new SemaphoreSlim(1, 1));
        logger.LogInformation("Waiting for per-repo locker for repo: {RepoUrl}", repoUrl);
        await locker.WaitAsync();
        try
        {
            // Double-check all cache layers after acquiring lock
            if (cache.TryGetValue<RepoStats>(memKey, out memStats))
                return memStats!;

            cachedRepo = await GetCachedRepoStaleOkAsync(repoUrl);
            if (cachedRepo != null)
            {
                var result = ConvertToRepoStats(cachedRepo);
                SetMemoryCache(memKey, result, TimeSpan.FromHours(1));
                return result;
            }

            // Acquire global git semaphore — only 1 git fetch at a time
            logger.LogInformation("Waiting for global git semaphore for repo: {RepoUrl}", repoUrl);
            await GlobalGitSemaphore.WaitAsync();
            try
            {
                var workPath = GetWorkPath(repoUrl);
                if (!workPath.StartsWith(Path.GetFullPath(_workspaceFolder), StringComparison.Ordinal))
                {
                    throw new ArgumentException(@"Invalid repository path.", nameof(repoUrl));
                }
                if (!Directory.Exists(workPath))
                {
                    logger.LogInformation("Create folder for repo: {Repo} on {Path}", repoName, workPath);
                    Directory.CreateDirectory(workPath);
                }

                var stats = await GetWorkHoursFromGitPath(repoName, repoUrl, workPath);
                await UpdateRepoStatsAsync(repoUrl, stats);
                SetMemoryCache(memKey, stats, MemoryCacheTtl);
                return stats;
            }
            finally
            {
                GlobalGitSemaphore.Release();
            }
        }
        finally
        {
            logger.LogInformation("Release per-repo locker for repo: {RepoUrl}", repoUrl);
            locker.Release();
        }
    }

    private async Task<RepoStats> GetWorkHoursFromGitPath(
        string repoName,
        string repoUrl,
        string workPath)
    {
        // Reduced from 5 to 2 retries — on slow disks, retrying just compounds the IO pressure.
        // Removed the destructive autoCleanIfError pattern that deleted the entire repo folder
        // and re-cloned from scratch, which was the root cause of the IO death spiral:
        //   slow disk → git timeout → delete folder → full re-clone → even more IO → repeat
        logger.LogInformation("Resetting repo: {Repo} on {Path}", repoName, workPath);
        await retryEngine.RunWithRetry(async _ =>
        {
            await workspaceManager.ResetRepo(
                workPath,
                null,
                repoUrl,
                CloneMode.BareWithOnlyCommits);
        }, attempts: EntryExtends.IsInUnitTests() ? 1 : 2);

        logger.LogInformation("Getting commits for repo: {Repo} on {Path}", repoName, workPath);
        var commits = await workspaceManager.GetCommits(workPath);

        logger.LogInformation("Calculating work time for repo: {Repo} on {Path}", repoName, workPath);
        return WorkTimeService.CalculateWorkTime(commits);
    }

    private async Task<RepoStats> GetWorkHoursFromGitPathInRange(
        string repoName,
        string repoUrl,
        string workPath,
        DateTime startDate,
        DateTime endDate)
    {
        logger.LogInformation("Resetting repo: {Repo} on {Path}", repoName, workPath);
        await retryEngine.RunWithRetry(async _ =>
        {
            await workspaceManager.ResetRepo(
                workPath,
                null,
                repoUrl,
                CloneMode.BareWithOnlyCommits);
        }, attempts: EntryExtends.IsInUnitTests() ? 1 : 2);

        logger.LogInformation("Getting commits for repo: {Repo} on {Path}", repoName, workPath);
        var commits = await workspaceManager.GetCommits(workPath);

        logger.LogInformation("Calculating work time for repo: {Repo} on {Path} in range {Start} to {End}",
            repoName, workPath, startDate, endDate);
        return WorkTimeService.CalculateWorkTimeInRange(commits, startDate, endDate);
    }

    /// <summary>
    /// Normalize date range to day boundaries so cache keys are stable.
    /// Without this, DateTime.UtcNow changes every request → cache key never matches.
    /// </summary>
    private static (DateTime start, DateTime end) NormalizeDateRange(DateTime startDate, DateTime endDate)
    {
        return (startDate.Date, endDate.Date);
    }

    private static string GetRangeCacheKey(string repoUrl, DateTime startDate, DateTime endDate)
    {
        var (s, e) = NormalizeDateRange(startDate, endDate);
        return $"RepoStats_{repoUrl}_{s:yyyyMMdd}_{e:yyyyMMdd}";
    }

    public bool TryGetCachedStats(string repoUrl, DateTime startDate, DateTime endDate, out RepoStats? stats)
    {
        var cacheKey = GetRangeCacheKey(repoUrl, startDate, endDate);
        return cache.TryGetValue(cacheKey, out stats);
    }

    public async Task<RepoStats> GetRepoStatsInRangeAsync(string repoName, string repoUrl, DateTime startDate, DateTime endDate)
    {
        ValidateRepoInput(repoName, repoUrl);
        var cacheKey = GetRangeCacheKey(repoUrl, startDate, endDate);

        // Layer 1: Memory cache
        if (cache.TryGetValue<RepoStats>(cacheKey, out var cachedStats))
        {
            logger.LogInformation("Using cached stats for repo: {Repo} from {Start} to {End}", repoName, startDate, endDate);
            return cachedStats!;
        }

        var locker = Lockers.GetOrAdd(repoUrl, _ => new SemaphoreSlim(1, 1));
        logger.LogInformation("Waiting for locker for repo: {RepoUrl}", repoUrl);
        await locker.WaitAsync();
        try
        {
            // Double-check cache after acquiring lock
            if (cache.TryGetValue(cacheKey, out cachedStats))
            {
                return cachedStats!;
            }

            // Acquire global git semaphore — only 1 git fetch at a time
            logger.LogInformation("Waiting for global git semaphore for repo: {RepoUrl}", repoUrl);
            await GlobalGitSemaphore.WaitAsync();
            try
            {
                var workPath = GetWorkPath(repoUrl);
                if (!workPath.StartsWith(Path.GetFullPath(_workspaceFolder), StringComparison.Ordinal))
                {
                    throw new ArgumentException(@"Invalid repository path.", nameof(repoUrl));
                }
                if (!Directory.Exists(workPath))
                {
                    logger.LogInformation("Create folder for repo: {Repo} on {Path}", repoName, workPath);
                    Directory.CreateDirectory(workPath);
                }

                var stats = await GetWorkHoursFromGitPathInRange(repoName, repoUrl, workPath, startDate, endDate);

                // Cache the result for 6 hours (was 1 hour)
                SetMemoryCache(cacheKey, stats, MemoryCacheTtl);
                logger.LogInformation("Cached stats for repo: {Repo} from {Start} to {End}", repoName, startDate, endDate);

                return stats;
            }
            finally
            {
                GlobalGitSemaphore.Release();
            }
        }
        finally
        {
            logger.LogInformation("Release locker for repo: {RepoUrl}", repoUrl);
            locker.Release();
        }
    }

    /// <summary>
    /// Get cached repo from DB, returns null if stale (older than DbCacheFreshnessHours).
    /// </summary>
    public async Task<Repo?> GetCachedRepoAsync(string repoUrl)
    {
        var repo = await dbContext.Repos
            .AsNoTracking()
            .Include(r => r.Contributions)
            .ThenInclude(c => c.Contributor)
            .FirstOrDefaultAsync(r => r.Url == repoUrl);

        if (repo == null)
        {
            return null;
        }

        if (repo.LastUpdateTime < DateTime.UtcNow.AddHours(-DbCacheFreshnessHours))
        {
            return null; // Treat as stale to trigger update
        }

        return repo;
    }

    /// <summary>
    /// Get cached repo from DB regardless of staleness — for stale-while-revalidate pattern.
    /// Returns whatever we have, even if it's months old.
    /// </summary>
    private async Task<Repo?> GetCachedRepoStaleOkAsync(string repoUrl)
    {
        return await dbContext.Repos
            .AsNoTracking()
            .Include(r => r.Contributions)
            .ThenInclude(c => c.Contributor)
            .FirstOrDefaultAsync(r => r.Url == repoUrl);
    }

    private void SetMemoryCache(string key, RepoStats stats, TimeSpan ttl)
    {
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(ttl)
            .SetSize(1);
        cache.Set(key, stats, cacheOptions);
    }

    public async Task UpdateRepoStatsAsync(string repoUrl, RepoStats stats)
    {
        var repo = await dbContext.Repos
            .Include(r => r.Contributions)
            .ThenInclude(c => c.Contributor)
            .FirstOrDefaultAsync(r => r.Url == repoUrl);

        if (repo == null)
        {
            repo = new Repo
            {
                Id = Guid.NewGuid(),
                Url = repoUrl,
                Name = repoUrl.Split('/').LastOrDefault()?.Replace(".git", "") ?? repoUrl
            };
            dbContext.Repos.Add(repo);
        }

        repo.LastUpdateTime = DateTime.UtcNow;

        var emails = stats.Contributors.Select(c => c.Email).Distinct().ToList();
        var existingContributors = await dbContext.Contributors
            .Where(c => emails.Contains(c.Email))
            .ToListAsync();

        foreach (var contributorStat in stats.Contributors)
        {
            var contributor = existingContributors.FirstOrDefault(c => c.Email == contributorStat.Email);

            if (contributor == null)
            {
                contributor = new Contributor
                {
                    Id = Guid.NewGuid(),
                    Email = contributorStat.Email,
                    Name = contributorStat.Name
                };
                dbContext.Contributors.Add(contributor);
                existingContributors.Add(contributor);
            }
            else
            {
                if (!string.IsNullOrEmpty(contributorStat.Name))
                {
                    contributor.Name = contributorStat.Name;
                }
            }

            contributorStat.Id = contributor.Id;

            var contribution = repo.Contributions.FirstOrDefault(c => c.ContributorId == contributor.Id);
            if (contribution == null)
            {
                contribution = new RepoContribution
                {
                    Id = Guid.NewGuid(),
                    RepoId = repo.Id,
                    ContributorId = contributor.Id
                };
                dbContext.RepoContributions.Add(contribution);
            }

            contribution.CommitCount = contributorStat.CommitCount;
            contribution.ActiveDays = contributorStat.ContributionDays;
            contribution.TotalWorkHours = contributorStat.WorkTime.TotalHours;
        }

        await dbContext.SaveChangesAsync();
    }

    public RepoStats ConvertToRepoStats(Repo repo)
    {
        return new RepoStats
        {
            TotalWorkTime = TimeSpan.FromHours(repo.Contributions.Sum(c => c.TotalWorkHours)),
            Contributors = repo.Contributions.Select(c => new ContributorStat
            {
                Id = c.Contributor?.Id,
                Name = c.Contributor?.Name ?? "Unknown",
                Email = c.Contributor?.Email ?? "Unknown",
                WorkTime = TimeSpan.FromHours(c.TotalWorkHours),
                CommitCount = c.CommitCount,
                ContributionDays = c.ActiveDays
            }).ToList()
        };
    }
}
