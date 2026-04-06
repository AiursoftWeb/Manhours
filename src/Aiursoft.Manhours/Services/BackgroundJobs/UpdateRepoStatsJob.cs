using Aiursoft.Canon.BackgroundJobs;
using Aiursoft.Manhours.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.Manhours.Services.BackgroundJobs;

public class UpdateRepoStatsJob(
    ManhoursDbContext db,
    RepoService repoService,
    ILogger<UpdateRepoStatsJob> logger) : IBackgroundJob
{
    public string Name => "Update Repo Stats";
    public string Description => "Fetches latest commit statistics for all repositories and updates the database. Runs daily.";

    public async Task ExecuteAsync()
    {
        logger.LogInformation("Update repo stats job started");
        var repos = await db.Repos.ToListAsync();
        foreach (var repo in repos)
        {
            await UpdateRepo(repo);
            // Add a 30-second delay between repos to prevent IO storms.
            // On a slow mechanical disk, back-to-back git fetches are devastating.
            await Task.Delay(TimeSpan.FromSeconds(30));
        }

        logger.LogInformation("Update repo stats job completed for {Count} repos", repos.Count);
    }

    private async Task UpdateRepo(Repo repo)
    {
        try
        {
            logger.LogInformation("Updating stats for repo: {RepoUrl}", repo.Url);
            var repoName = repo.Url.Split('/').LastOrDefault()?.Replace(".git", "") ?? repo.Url;
            // Force update to bypass stale cache and perform git fetch
            await repoService.GetRepoStatsAsync(repoName, repo.Url, force: true);
            logger.LogInformation("Successfully updated stats for repo: {RepoUrl}", repo.Url);
        }
        catch (Exception ex)
        {
            // Log and move on — do NOT delete the repo from DB on failure.
            // The old logic deleted repos after failures, which caused expensive full clones on next request.
            logger.LogWarning(ex, "Failed to update stats for repo: {RepoUrl}. Will retry next cycle.", repo.Url);
        }
    }
}
