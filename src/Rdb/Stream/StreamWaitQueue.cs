using System.Collections.Concurrent;

namespace codecrafters_redis.Rdb.Stream;

internal sealed class StreamWaitQueue : IDisposable
{
    private readonly ConcurrentQueue<StreamWaiter> _waiters = new();
    private bool _disposed;

    public async Task<StreamResult?> WaitForUpdate(string streamKey, StreamEntryId afterId, CancellationToken cancellationToken)
    {
        if (_disposed) return null;

        using var waiter = new StreamWaiter(streamKey, afterId);
        _waiters.Enqueue(waiter);
        
        return await waiter.Wait(cancellationToken);
    }

    public void NotifyUpdate(string streamKey, StreamRecord streamRecord)
    {
        if (_disposed) return;

        var unmatchedWaiters = new List<StreamWaiter>();
        var processed = 0;
        var initialCount = _waiters.Count;

        while (processed < initialCount && _waiters.TryDequeue(out var waiter))
        {
            processed++;

            if (!waiter.TryComplete(streamKey, streamRecord)) 
                unmatchedWaiters.Add(waiter);
        }

        foreach (var waiter in unmatchedWaiters) 
            _waiters.Enqueue(waiter);
    }

    public void Dispose()
    {
        _disposed = true;
        while (_waiters.TryDequeue(out var waiter))
            waiter.Dispose();
    }
}

internal sealed class StreamWaiter(string key, StreamEntryId afterId) : IDisposable
{
    private readonly TaskCompletionSource<StreamResult?> _tcs = new();
    private volatile bool _disposed;

    public Task<StreamResult?> Wait(CancellationToken cancellationToken)
    {
        if (_disposed)
            return Task.FromResult<StreamResult?>(null);

        cancellationToken.Register(() => _tcs.TrySetCanceled(cancellationToken));
        return _tcs.Task;
    }

    public bool TryComplete(string streamKey, StreamRecord streamRecord)
    {
        if (_disposed || streamKey != key)
            return false;
        
        if (streamRecord.LastEntryId > afterId)
        {
            var entries = streamRecord.GetEntriesAfter(afterId);
            var result = new StreamResult(streamKey, entries);
            return _tcs.TrySetResult(result);
        }

        return false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _tcs.TrySetResult(null);
    }
}