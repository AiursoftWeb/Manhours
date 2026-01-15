using System.Threading.Channels;

namespace Aiursoft.Manhours.Services.Background;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<RepoUpdateTask> _queue;

    public BackgroundTaskQueue()
    {
        // Limit the queue capacity to prevent memory overflow if too many requests come in.
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        };
        _queue = Channel.CreateBounded<RepoUpdateTask>(options);
    }

    public async ValueTask QueueBackgroundWorkItemAsync(RepoUpdateTask workItem)
    {
        await _queue.Writer.WriteAsync(workItem);
    }

    public async ValueTask<RepoUpdateTask> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
