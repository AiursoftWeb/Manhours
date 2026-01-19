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
    private const int IntervalHours = 18;
    private Timer? _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!EntryExtends.IsProgramEntry())
        {
            logger.LogInformation("Skip update repo stats in test environment.");
            return Task.CompletedTask;
        }

        logger.LogInformation("Timed Background Service is starting. Update all repos every {Interval} hours.", IntervalHours);
        _timer = new Timer(DoWork, null, TimeSpan.FromSeconds(5), TimeSpan.FromHours(IntervalHours));
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
                await UpdateRepo(repo, repoService, dbContext);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while updating repo stats");
        }
    }

    private async Task UpdateRepo(Repo repo, RepoService repoService, ManhoursDbContext dbContext)
    {
        var retryCount = 0;
        const int maxRetries = 3;

        while (retryCount < maxRetries)
        {
            try
            {
                logger.LogInformation("Updating stats for repo: {RepoUrl} (Attempt {Attempt}/{Max})", repo.Url, retryCount + 1, maxRetries);
                var repoName = repo.Url.Split('/').LastOrDefault()?.Replace(".git", "") ?? repo.Url;
                await repoService.GetRepoStatsAsync(repoName, repo.Url);
                logger.LogInformation("Successfully updated stats for repo: {RepoUrl}", repo.Url);
                return;
            }
            catch (Exception ex)
            {
                retryCount++;
                logger.LogWarning(ex, "Failed to update stats for repo: {RepoUrl}. Attempt {Attempt}/{Max}", repo.Url, retryCount, maxRetries);

                if (retryCount >= maxRetries)
                {
                    logger.LogError("Failed to update stats for repo: {RepoUrl} after {Max} attempts. Deleting repo from database.", repo.Url, maxRetries);
                    dbContext.Repos.Remove(repo);
                    await dbContext.SaveChangesAsync();
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(5 * retryCount)); // Exponential backoff-ish
                }
            }
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
