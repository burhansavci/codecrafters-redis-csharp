using System.Collections.Concurrent;
using codecrafters_redis.Rdb.Extensions;

namespace codecrafters_redis.Rdb.List;

public sealed class ListOperations(ConcurrentDictionary<string, Record> records) : IDisposable
{
    private readonly ConcurrentDictionary<string, ListWaitQueue> _listWaiters = new();
    private bool _disposed;

    public int Push(string listKey, IReadOnlyList<string> values, ListPushDirection direction)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrEmpty(listKey);
        ArgumentNullException.ThrowIfNull(values);

        if (values.Count == 0)
            throw new ArgumentException("At least one value is required");

        if (!records.TryGetRecord<ListRecord>(listKey, out var list))
            list = ListRecord.Create([]);

        foreach (var value in values)
        {
            if (direction == ListPushDirection.Left)
                list.Prepend(value);
            else
                list.Append(value);
        }

        records.AddOrUpdate(listKey, list, (_, _) => list);

        var count = list.Count;

        if (_listWaiters.TryGetValue(listKey, out var waitQueue))
            waitQueue.NotifyUpdate(list);

        return count;
    }

    public string[]? Pop(string listKey, int count = 1, ListPopDirection direction = ListPopDirection.Left)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrEmpty(listKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        if (!records.TryGetRecord<ListRecord>(listKey, out var list))
            return null;

        if (direction == ListPopDirection.Left)
        {
            if (list.TryPopLeft(count, out var result))
                return result;
        }
        else
        {
            if (list.TryPopRight(count, out var result))
                return result;
        }

        return null;
    }

    public async Task<ListPopResult?> Pop(IReadOnlyList<string> listKeys, TimeSpan timeout, ListPopDirection direction = ListPopDirection.Left)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(listKeys);

        if (listKeys.Count == 0)
            throw new ArgumentException("At least one list key is required");

        using var cts = new CancellationTokenSource(timeout);
        var waitTasks = new List<Task<ListPopResult?>>();

        foreach (var listKey in listKeys)
        {
            var waitQueue = _listWaiters.GetOrAdd(listKey, _ => new ListWaitQueue());
            waitTasks.Add(waitQueue.WaitForUpdate(listKey, direction, cts.Token));
        }

        foreach (var listKey in listKeys)
        {
            var values = Pop(listKey, 1, direction);
            if (values != null)
            {
                await cts.CancelAsync();
                return new ListPopResult(listKey, values[0]);
            }
        }

        try
        {
            var completedTask = await Task.WhenAny(waitTasks);
            return await completedTask;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        finally
        {
            await cts.CancelAsync();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        foreach (var waitQueue in _listWaiters.Values)
            waitQueue.Dispose();

        _listWaiters.Clear();
    }
}