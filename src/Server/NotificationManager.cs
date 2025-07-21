using System.Collections.Concurrent;
using System.Reactive.Subjects;

namespace codecrafters_redis.Server;

public sealed class NotificationManager : IDisposable
{
    private readonly ConcurrentDictionary<string, Subject<bool>> _subjects = new();
    private bool _disposed;

    public IObservable<bool> Subscribe(string eventKey)
    {
        var subject = _subjects.GetOrAdd(eventKey, _ => new Subject<bool>());
        return subject;
    }

    public void Notify(string eventKey)
    {
        if (_subjects.TryGetValue(eventKey, out var subject)) 
            subject.OnNext(true);
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        foreach (var subject in _subjects.Values)
        {
            subject.OnCompleted();
            subject.Dispose();
        }
        _subjects.Clear();
        _disposed = true;
    }
}