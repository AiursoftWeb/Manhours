using Aiursoft.CSTools.Tools;
using Aiursoft.Manhours.Entities;
using Aiursoft.Manhours.Services;
using Aiursoft.Scanner.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.Manhours.BackgroundJobs;

public class UpdateRepoStatsJob(
    ILogger<UpdateRepoStatsJob> logger,
    IServiceScopeFactory scopeFactory)
    : IHostedService, IDisposable, ISingletonDependency
{
    private const int IntervalHours = 24; // Was 18h, extended to reduce frequency
    private Timer? _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!EntryExtends.IsProgramEntry())
        {
            logger.LogInformation("Skip update repo stats in test environment.");
            return Task.CompletedTask;
        }

        logger.LogInformation("Timed Background Service is starting. Update all repos every {Interval} hours.", IntervalHours);
        // Delay initial run to 30 minutes after startup (was 5 seconds).
        // This prevents slamming the disk immediately on boot when other services
        // (bcache writeback, other containers) are also starting up.
        _timer = new Timer(DoWork, null, TimeSpan.FromMinutes(30), TimeSpan.FromHours(IntervalHours));
        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        try
        {
            logger.LogInformation("Update repo stats job started");
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ManhoursDbContext>();
            var repoService = scope.ServiceProvider.GetRequiredService<RepoService>();

            var repos = await dbContext.Repos.ToListAsync();
            foreach (var repo in repos)
            {
                await UpdateRepo(repo, repoService);
                // Add a 30-second delay between repos to prevent IO storms.
                // On a slow mechanical disk, back-to-back git fetches are devastating.
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while updating repo stats");
        }
    }

    private async Task UpdateRepo(Repo repo, RepoService repoService)
    {
        try
        {
            logger.LogInformation("Updating stats for repo: {RepoUrl}", repo.Url);
            var repoName = repo.Url.Split('/').LastOrDefault()?.Replace(".git", "") ?? repo.Url;
            await repoService.GetRepoStatsAsync(repoName, repo.Url);
            logger.LogInformation("Successfully updated stats for repo: {RepoUrl}", repo.Url);
        }
        catch (Exception ex)
        {
            // Log and move on — do NOT delete the repo from DB on failure.
            // The old logic deleted repos after 3 failures, which meant the next request
            // would trigger a full git clone (much heavier than a fetch).
            logger.LogWarning(ex, "Failed to update stats for repo: {RepoUrl}. Will retry next cycle.", repo.Url);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Timed Background Service is stopping");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
