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
    Background.IBackgroundTaskQueue backgroundQueue,
    IMemoryCache cache) : IScopedDependency
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Lockers = new();
    private static readonly Regex RepoNameRegex = new("^[a-zA-Z0-9._/-]+$", RegexOptions.Compiled);
    private readonly string _workspaceFolder = Path.Combine(configuration["Storage:Path"]!, "Repos");

    // Global semaphore: only 1 git fetch at a time across the entire application.
    // On slow disks, concurrent git operations are catastrophic.
    private static readonly SemaphoreSlim GlobalGitSemaphore = new(1, 1);

    // DB cache freshness period (was 24 hours — extended to 7 days for statistical data)
    private const int DbCacheFreshnessHours = 168;

    // Memory cache TTL for full repo stats (was 6 hours — extended to 24 hours)
    private static readonly TimeSpan MemoryCacheTtl = TimeSpan.FromHours(24);
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

    public async Task<RepoStats> GetRepoStatsAsync(string repoName, string repoUrl, bool force = false)
    {
        ValidateRepoInput(repoName, repoUrl);

        // Layer 1: Memory cache (fastest, no DB hit, no disk hit)
        var memKey = MemCachePrefix + repoUrl;
        if (!force && cache.TryGetValue<RepoStats>(memKey, out var memStats))
        {
            return memStats!;
        }

        // Layer 2: Fresh DB cache (DB hit only, no git/disk)
        var cachedRepo = await GetCachedRepoAsync(repoUrl);
        if (!force && cachedRepo != null)
        {
            var result = ConvertToRepoStats(cachedRepo);
            SetMemoryCache(memKey, result, MemoryCacheTtl);
            return result;
        }

        // Layer 3: Stale DB cache — return stale data immediately (stale-while-revalidate)
        var staleRepo = await GetCachedRepoStaleOkAsync(repoUrl);
        if (!force && staleRepo != null)
        {
            logger.LogInformation("Returning stale data for repo: {RepoUrl} (last updated: {LastUpdate})",
                repoUrl, staleRepo.LastUpdateTime);

            // Trigger background update if stale
            await backgroundQueue.QueueBackgroundWorkItemAsync(new Background.RepoUpdateTask
            {
                RepoName = repoName,
                RepoUrl = repoUrl
            });

            var result = ConvertToRepoStats(staleRepo);
            // Cache stale data in memory for a shorter period (1 hour) to avoid spamming the background queue
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
            if (!force && cache.TryGetValue(memKey, out memStats))
                return memStats!;

            if (!force)
            {
                cachedRepo = await GetCachedRepoStaleOkAsync(repoUrl);
                if (cachedRepo != null)
                {
                    var result = ConvertToRepoStats(cachedRepo);
                    SetMemoryCache(memKey, result, TimeSpan.FromHours(1));
                    return result;
                }
            }

            // Acquire global git semaphore — only 1 git fetch at a time
            var workPath = GetWorkPath(repoUrl);
            if (!workPath.StartsWith(Path.GetFullPath(_workspaceFolder), StringComparison.Ordinal))
            {
                throw new ArgumentException(@"Invalid repository path.", nameof(repoUrl));
            }

            await EnsureRepoFreshOnDisk(repoName, repoUrl, workPath, force);

            var stats = await GetRepoStatsFromDiskAsync(repoName, workPath);
            await UpdateRepoStatsAsync(repoUrl, stats);
            SetMemoryCache(memKey, stats, MemoryCacheTtl);
            return stats;
        }
        finally
        {
            logger.LogInformation("Release per-repo locker for repo: {RepoUrl}", repoUrl);
            locker.Release();
        }
    }

    private async Task<RepoStats> GetRepoStatsFromDiskAsync(
        string repoName,
        string workPath)
    {
        logger.LogInformation("Getting commits for repo: {Repo} on {Path}", repoName, workPath);
        var commits = await workspaceManager.GetCommits(workPath);

        logger.LogInformation("Calculating work time for repo: {Repo} on {Path}", repoName, workPath);
        return WorkTimeService.CalculateWorkTime(commits);
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

    private async Task EnsureRepoFreshOnDisk(string repoName, string repoUrl, string workPath, bool force = false)
    {
        // Check if we already have this repo in DB and it was updated recently
        var cachedRepo = await GetCachedRepoAsync(repoUrl);
        if (!force && cachedRepo != null && Directory.Exists(workPath))
        {
            // Even if it's fresh in DB, the disk might have been wiped.
            // But if it's fresh in DB AND folder exists, we can assume it's good to go.
            // This avoids a costly git fetch.
            logger.LogInformation("Repo {Repo} is fresh in DB (last update: {LastUpdate}) and exists on disk. Skipping git fetch.",
                repoName, cachedRepo.LastUpdateTime);
            return;
        }

        // Acquire global git semaphore — only 1 git fetch at a time
        logger.LogInformation("Waiting for global git semaphore to fetch repo: {RepoUrl}", repoUrl);
        await GlobalGitSemaphore.WaitAsync();
        try
        {
            if (!Directory.Exists(workPath))
            {
                logger.LogInformation("Create folder for repo: {Repo} on {Path}", repoName, workPath);
                Directory.CreateDirectory(workPath);
            }

            logger.LogInformation("Resetting repo: {Repo} on {Path}", repoName, workPath);
            await retryEngine.RunWithRetry(async _ =>
            {
                await workspaceManager.ResetRepo(
                    workPath,
                    null,
                    repoUrl,
                    CloneMode.BareWithOnlyCommits);
            }, attempts: EntryExtends.IsInUnitTests() ? 1 : 2);
        }
        finally
        {
            GlobalGitSemaphore.Release();
        }
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

        // Layer 2: No fresh cache. Return stale data if available?
        // Range stats are NOT stored in DB, so we don't have "stale" DB stats for ranges.
        // But we can trigger a background update and return null/loading.

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

            var workPath = GetWorkPath(repoUrl);
            if (!workPath.StartsWith(Path.GetFullPath(_workspaceFolder), StringComparison.Ordinal))
            {
                throw new ArgumentException(@"Invalid repository path.", nameof(repoUrl));
            }

            // Ensure repo is on disk and fresh
            await EnsureRepoFreshOnDisk(repoName, repoUrl, workPath);

            logger.LogInformation("Getting commits for repo: {Repo} on {Path}", repoName, workPath);
            var commits = await workspaceManager.GetCommits(workPath);

            logger.LogInformation("Calculating work time for repo: {Repo} on {Path} in range {Start} to {End}",
                repoName, workPath, startDate, endDate);
            var stats = WorkTimeService.CalculateWorkTimeInRange(commits, startDate, endDate);

            // If this was a full fetch, we should also update the total stats in DB!
            // But only if we did a fetch.
            // Actually, calculating total stats is cheap once we have commits.
            var totalStats = WorkTimeService.CalculateWorkTime(commits);
            await UpdateRepoStatsAsync(repoUrl, totalStats);

            // Cache the result in memory (24 hours)
            SetMemoryCache(cacheKey, stats, MemoryCacheTtl);
            logger.LogInformation("Cached stats for repo: {Repo} from {Start} to {End}", repoName, startDate, endDate);

            return stats;
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
