using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace codecrafters_redis.Rdb.List;

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