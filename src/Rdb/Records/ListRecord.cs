using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace codecrafters_redis.Rdb.Records;

public sealed record ListRecord : Record
{
    private readonly ConcurrentDeque<string> _entries;

    private ListRecord(string listKey, ConcurrentDeque<string> entries, DateTime? expireAt = null) : base(entries, ValueType.List, expireAt)
    {
        ListKey = listKey;
        _entries = entries;
    }

    public string ListKey { get; }
    public int Count => _entries.Count;

    public static ListRecord Create(string listKey, string[] values, DateTime? expireAt = null, bool isReversed = false)
    {
        var deque = new ConcurrentDeque<string>();

        if (values.Length <= 0)
            return new ListRecord(listKey, deque, expireAt);

        if (isReversed)
        {
            for (var i = values.Length - 1; i >= 0; i--)
                deque.PushLeft(values[i]);
        }
        else
        {
            foreach (var value in values)
                deque.PushRight(value);
        }

        return new ListRecord(listKey, deque, expireAt);
    }

    public void Append(string entry) => _entries.PushRight(entry);

    public void Prepend(string entry) => _entries.PushLeft(entry);

    public string? PopLeft() => _entries.TryPopLeft(out var value) ? value : null;

    public string? PopRight() => _entries.TryPopLeft(out var value) ? value : null;

    public bool TryPopLeft(int count, [MaybeNullWhen(false)] out string[] result)
    {
        if (count <= 0 || _entries.Count == 0)
        {
            result = null;
            return false;
        }

        count = Math.Min(count, _entries.Count);
        var list = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            if (!_entries.TryPopLeft(out var value))
                break;

            list.Add(value);
        }

        if (list.Count == 0)
        {
            result = null;
            return false;
        }

        result = list.ToArray();
        return true;
    }

    public bool TryPopRight(int count, [MaybeNullWhen(false)] out string[] result)
    {
        if (count <= 0 || _entries.Count == 0)
        {
            result = null;
            return false;
        }

        count = Math.Min(count, _entries.Count);
        var list = new List<string>(count);
        
        for (int i = 0; i < count; i++)
        {
            if (!_entries.TryPopRight(out var value))
                break;

            list.Add(value);
        }

        if (list.Count == 0)
        {
            result = null;
            return false;
        }

        result = list.ToArray();
        return true;
    }

    public string[] GetEntriesInRange(int startIndex, int endIndex)
    {
        if (startIndex < 0 || endIndex < 0 || startIndex > endIndex || _entries.Count == 0)
            return [];

        var snapshot = _entries.ToArray();

        startIndex = Math.Max(0, Math.Min(startIndex, snapshot.Length - 1));
        endIndex = Math.Max(startIndex, Math.Min(endIndex, snapshot.Length - 1));

        var length = endIndex - startIndex + 1;
        return snapshot.AsSpan(startIndex, length).ToArray();
    }
}

public sealed class ConcurrentDeque<T>
{
    private readonly ConcurrentQueue<T> _leftQueue = new();
    private readonly ConcurrentQueue<T> _rightQueue = new();
    private volatile int _count;

    public int Count => _count;

    public void PushLeft(T item)
    {
        _leftQueue.Enqueue(item);
        Interlocked.Increment(ref _count);
    }

    public void PushRight(T item)
    {
        _rightQueue.Enqueue(item);
        Interlocked.Increment(ref _count);
    }

    public bool TryPopLeft([MaybeNullWhen(false)] out T item)
    {
        if (_leftQueue.TryDequeue(out item) || _rightQueue.TryDequeue(out item))
        {
            Interlocked.Decrement(ref _count);
            return true;
        }

        item = default;
        return false;
    }

    public bool TryPopRight([MaybeNullWhen(false)] out T item)
    {
        if (_rightQueue.TryDequeue(out item) || _leftQueue.TryDequeue(out item))
        {
            Interlocked.Decrement(ref _count);
            return true;
        }

        item = default;
        return false;
    }

    public T[] ToArray()
    {
        var leftItems = _leftQueue.ToArray();
        var rightItems = _rightQueue.ToArray();

        var result = new T[leftItems.Length + rightItems.Length];

        // Left queue items are in reverse order for left-side operations
        for (int i = 0; i < leftItems.Length; i++)
            result[leftItems.Length - 1 - i] = leftItems[i];

        Array.Copy(rightItems, 0, result, leftItems.Length, rightItems.Length);

        return result;
    }
}