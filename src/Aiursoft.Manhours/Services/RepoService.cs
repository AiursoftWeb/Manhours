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
        var cachedRepo = await GetCachedRepoAsync(repoUrl);
        if (cachedRepo != null)
        {
            return ConvertToRepoStats(cachedRepo);
        }

        var locker = Lockers.GetOrAdd(repoUrl, _ => new SemaphoreSlim(1, 1));
        logger.LogInformation("Waiting for locker for repo: {RepoUrl}", repoUrl);
        await locker.WaitAsync();
        try
        {
            cachedRepo = await GetCachedRepoAsync(repoUrl);
            if (cachedRepo != null)
            {
                return ConvertToRepoStats(cachedRepo);
            }

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
            return stats;
        }
        finally
        {
            logger.LogInformation("Release locker for repo: {RepoUrl}", repoUrl);
            locker.Release();
        }
    }

    private async Task<RepoStats> GetWorkHoursFromGitPath(
        string repoName,
        string repoUrl,
        string workPath,
        bool autoCleanIfError = true)
    {
        try
        {
            logger.LogInformation("Resetting repo: {Repo} on {Path}", repoName, workPath);
            await retryEngine.RunWithRetry(async _ =>
            {
                await workspaceManager.ResetRepo(
                    workPath,
                    null,
                    repoUrl,
                    CloneMode.BareWithOnlyCommits);
            }, attempts: EntryExtends.IsInUnitTests() ? 1 : 5);

            logger.LogInformation("Getting commits for repo: {Repo} on {Path}", repoName, workPath);
            var commits = await workspaceManager.GetCommits(workPath);

            logger.LogInformation("Calculating work time for repo: {Repo} on {Path}", repoName, workPath);
            return WorkTimeService.CalculateWorkTime(commits);
        }
        catch (Exception e)
        {
            if (!autoCleanIfError) throw;
            logger.LogError(e, "Error on repo: {Repo} on {Path}", repoName, workPath);
            logger.LogInformation("Cleaning repo: {Repo} on {Path}", repoName, workPath);
            FolderDeleter.DeleteByForce(workPath, keepFolder: true);
            return await GetWorkHoursFromGitPath(repoName, repoUrl, workPath, false);
        }
    }

    private async Task<RepoStats> GetWorkHoursFromGitPathInRange(
        string repoName,
        string repoUrl,
        string workPath,
        DateTime startDate,
        DateTime endDate,
        bool autoCleanIfError = true)
    {
        try
        {
            logger.LogInformation("Resetting repo: {Repo} on {Path}", repoName, workPath);
            await retryEngine.RunWithRetry(async _ =>
            {
                await workspaceManager.ResetRepo(
                    workPath,
                    null,
                    repoUrl,
                    CloneMode.BareWithOnlyCommits);
            }, attempts: EntryExtends.IsInUnitTests() ? 1 : 5);

            logger.LogInformation("Getting commits for repo: {Repo} on {Path}", repoName, workPath);
            var commits = await workspaceManager.GetCommits(workPath);

            logger.LogInformation("Calculating work time for repo: {Repo} on {Path} in range {Start} to {End}",
                repoName, workPath, startDate, endDate);
            return WorkTimeService.CalculateWorkTimeInRange(commits, startDate, endDate);
        }
        catch (Exception e)
        {
            if (!autoCleanIfError) throw;
            logger.LogError(e, "Error on repo: {Repo} on {Path}", repoName, workPath);
            logger.LogInformation("Cleaning repo: {Repo} on {Path}", repoName, workPath);
            FolderDeleter.DeleteByForce(workPath, keepFolder: true);
            return await GetWorkHoursFromGitPathInRange(repoName, repoUrl, workPath, startDate, endDate, false);
        }
    }

    public bool TryGetCachedStats(string repoUrl, DateTime startDate, DateTime endDate, out RepoStats? stats)
    {
        var cacheKey = $"RepoStats_{repoUrl}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";
        return cache.TryGetValue(cacheKey, out stats);
    }

    public async Task<RepoStats> GetRepoStatsInRangeAsync(string repoName, string repoUrl, DateTime startDate, DateTime endDate)
    {
        ValidateRepoInput(repoName, repoUrl);
        // Create a cache key based on repo URL and date range
        var cacheKey = $"RepoStats_{repoUrl}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

        // Try to get from cache first
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

            // Cache the result for 1 hour
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(1))
                .SetSize(1); // Assuming each entry has a size of 1

            cache.Set(cacheKey, stats, cacheOptions);
            logger.LogInformation("Cached stats for repo: {Repo} from {Start} to {End}", repoName, startDate, endDate);

            return stats;
        }
        finally
        {
            logger.LogInformation("Release locker for repo: {RepoUrl}", repoUrl);
            locker.Release();
        }
    }

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

        if (repo.LastUpdateTime < DateTime.UtcNow.AddHours(-6))
        {
            return null; // Treat as stale/not found to trigger update
        }

        return repo;
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
