using System.Collections.Concurrent;

namespace codecrafters_redis.Rdb.List;

internal sealed class ListWaitQueue : IDisposable
{
    private readonly ConcurrentQueue<ListWaiter> _waiters = new();
    private bool _disposed;

    public async Task<ListPopResult?> WaitForUpdate(string listKey, ListPopDirection direction, CancellationToken cancellationToken)
    {
        if (_disposed) return null;

        using var waiter = new ListWaiter(listKey, direction);
        _waiters.Enqueue(waiter);

        return await waiter.Wait(cancellationToken);
    }

    public void NotifyUpdate(ListRecord listRecord)
    {
        if (_disposed) return;

        if (listRecord.Count > 0 && _waiters.TryDequeue(out var waiter))
        {
            if (!waiter.TryComplete(listRecord))
                _waiters.Enqueue(waiter);
        }
    }

    public void Dispose()
    {
        _disposed = true;

        while (_waiters.TryDequeue(out var waiter))
            waiter.Dispose();
    }
}

internal sealed class ListWaiter(string listKey, ListPopDirection direction) : IDisposable
{
    private readonly TaskCompletionSource<ListPopResult?> _tcs = new();
    private bool _disposed;

    public Task<ListPopResult?> Wait(CancellationToken cancellationToken)
    {
        if (_disposed)
            return Task.FromResult<ListPopResult?>(null);

        cancellationToken.Register(() => _tcs.TrySetCanceled(cancellationToken));
        return _tcs.Task;
    }

    public bool TryComplete(ListRecord listRecord)
    {
        if (_disposed)
            return false;

        var value = direction == ListPopDirection.Left ? listRecord.PopLeft() : listRecord.PopRight();

        if (value == null)
            return false;

        var result = new ListPopResult(listKey, value);
        return _tcs.TrySetResult(result);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _tcs.TrySetResult(null);
    }
}