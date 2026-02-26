using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Aiursoft.Manhours.Services.Background;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<RepoUpdateTask> _queue;

    // Track which repos are already pending to avoid duplicate work.
    // Key: repoUrl, prevents the same repo from being queued multiple times.
    private readonly ConcurrentDictionary<string, byte> _pendingRepos = new();

    public BackgroundTaskQueue()
    {
        // Reduced from 1000 to 20 — we don't want hundreds of git fetches piled up.
        // On a slow disk, even 20 is aggressive.
        var options = new BoundedChannelOptions(20)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        };
        _queue = Channel.CreateBounded<RepoUpdateTask>(options);
    }

    public async ValueTask QueueBackgroundWorkItemAsync(RepoUpdateTask workItem)
    {
        // Deduplicate: if this repo is already queued, skip it.
        // This prevents the WeeklyReportStatus long-polling loop from
        // queuing the same repo 20 times in 10 seconds.
        if (!_pendingRepos.TryAdd(workItem.RepoUrl, 0))
        {
            return; // Already queued, skip
        }

        await _queue.Writer.WriteAsync(workItem);
    }

    public async ValueTask<RepoUpdateTask> DequeueAsync(CancellationToken cancellationToken)
    {
        var task = await _queue.Reader.ReadAsync(cancellationToken);
        // Remove from pending set so it can be re-queued later if needed
        _pendingRepos.TryRemove(task.RepoUrl, out _);
        return task;
    }
}
