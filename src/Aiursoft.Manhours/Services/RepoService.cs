using Aiursoft.Manhours.Entities;
using Aiursoft.ManHours.Models;
using Aiursoft.Scanner.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using Aiursoft.GitRunner;
using Aiursoft.GitRunner.Models;
using Aiursoft.CSTools.Tools;
using Aiursoft.ManHours.Services;

namespace Aiursoft.Manhours.Services;

public class RepoService(
    TemplateDbContext dbContext,
    ILogger<RepoService> logger,
    WorkspaceManager workspaceManager,
    IConfiguration configuration) : IScopedDependency
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Lockers = new();
    private readonly string _workspaceFolder = Path.Combine(configuration["Storage:Path"]!, "Repos");

    public async Task<RepoStats> GetRepoStatsAsync(string repoName, string repoUrl)
    {
        var cachedRepo = await GetCachedRepoAsync(repoUrl);
        if (cachedRepo != null)
        {
            return ConvertToRepoStats(cachedRepo);
        }

        var locker = Lockers.GetOrAdd(repoName, _ => new SemaphoreSlim(1, 1));
        logger.LogInformation("Waiting for locker for repo: {Repo}", repoName);
        await locker.WaitAsync();
        try
        {
            cachedRepo = await GetCachedRepoAsync(repoUrl);
            if (cachedRepo != null)
            {
                return ConvertToRepoStats(cachedRepo);
            }

            var repoLocalPath = repoName.Replace('/', Path.DirectorySeparatorChar);
            var workPath = Path.GetFullPath(Path.Combine(_workspaceFolder, repoLocalPath));
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
            logger.LogInformation("Release locker for repo: {Repo}", repoName);
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
            await workspaceManager.ResetRepo(
                workPath,
                null,
                repoUrl,
                CloneMode.BareWithOnlyCommits);

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
