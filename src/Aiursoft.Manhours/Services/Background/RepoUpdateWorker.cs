

using Aiursoft.Scanner.Abstractions;

namespace Aiursoft.Manhours.Services.Background;

public class RepoUpdateWorker(
    IBackgroundTaskQueue taskQueue,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<RepoUpdateWorker> logger)
    : BackgroundService, ISingletonDependency
{
    // Limit to 4 concurrent git operations
    private readonly SemaphoreSlim _semaphore = new(4);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Repo Update Worker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await taskQueue.DequeueAsync(stoppingToken);

                // Wait for a slot
                await _semaphore.WaitAsync(stoppingToken);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = serviceScopeFactory.CreateScope();
                        var repoService = scope.ServiceProvider.GetRequiredService<RepoService>();

                        logger.LogInformation("Processing background update for {Repo}", workItem.RepoName);

                        // This will check cache, lock, fetch git, calculate, and Populate Cache.
                        await repoService.GetRepoStatsInRangeAsync(
                            workItem.RepoName,
                            workItem.RepoUrl,
                            workItem.StartDate,
                            workItem.EndDate);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error occurred executing background work item for {Repo}", workItem.RepoName);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Prevent throwing if stoppingToken was signaled
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred processing task queue");
            }
        }

        logger.LogInformation("Repo Update Worker is stopping.");
    }
}
