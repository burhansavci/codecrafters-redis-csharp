using System.Collections.Concurrent;

namespace codecrafters_redis.Server;

public sealed class NotificationManager
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<TaskCompletionSource<bool>>> _subscriptions = new();

    public void Subscribe(string eventKey, TaskCompletionSource<bool> tcs)
    {
        var subscribers = _subscriptions.GetOrAdd(eventKey, _ => new ConcurrentQueue<TaskCompletionSource<bool>>());
        subscribers.Enqueue(tcs);
    }

    public void Notify(string eventKey)
    {
        if (_subscriptions.TryGetValue(eventKey, out var subscribers))
            if (subscribers.TryDequeue(out var tcs))
                tcs.TrySetResult(true);
    }

    public void NotifyAll(string eventKey)
    {
        if (_subscriptions.TryGetValue(eventKey, out var subscribers))
            while (subscribers.TryDequeue(out var tcs))
                tcs.TrySetResult(true);
    }
}