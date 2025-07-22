using System.Collections.Concurrent;

namespace codecrafters_redis.Server;

public sealed class NotificationManager
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<TaskCompletionSource<bool>, byte>> _subscriptions = new();

    public void Subscribe(string eventKey, TaskCompletionSource<bool> tcs)
    {
        var subscribers = _subscriptions.GetOrAdd(eventKey, _ => new ConcurrentDictionary<TaskCompletionSource<bool>, byte>());
        subscribers.TryAdd(tcs, 0); // The value (0) is a dummy value
    }

    public void Unsubscribe(string eventKey, TaskCompletionSource<bool> tcs)
    {
        if (_subscriptions.TryGetValue(eventKey, out var subscribers))
            subscribers.TryRemove(tcs, out _);
    }
    
    public void UnsubscribeAll(string eventKey)
    {
        if (_subscriptions.TryGetValue(eventKey, out var subscribers))
            subscribers.Clear();
    }

    public void Notify(string eventKey)
    {
        if (_subscriptions.TryGetValue(eventKey, out var subscribers))
            foreach (var tcs in subscribers.Keys)
                tcs.TrySetResult(true);
    }
}