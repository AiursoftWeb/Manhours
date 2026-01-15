namespace Aiursoft.Manhours.Services.Background;

public interface IBackgroundTaskQueue
{
    ValueTask QueueBackgroundWorkItemAsync(RepoUpdateTask workItem);
    ValueTask<RepoUpdateTask> DequeueAsync(CancellationToken cancellationToken);
}
