using Aiursoft.Manhours.Entities;
using Aiursoft.ManHours.Models;
using Aiursoft.Scanner.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.Manhours.Services;

public class RepoService(
    TemplateDbContext dbContext,
    ILogger<RepoService> logger) : IScopedDependency
{
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
                Name = c.Contributor?.Name ?? "Unknown",
                Email = c.Contributor?.Email ?? "Unknown",
                WorkTime = TimeSpan.FromHours(c.TotalWorkHours),
                CommitCount = c.CommitCount,
                ContributionDays = c.ActiveDays
            }).ToList()
        };
    }
}
