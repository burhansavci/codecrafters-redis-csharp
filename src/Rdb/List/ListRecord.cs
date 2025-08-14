using System.Diagnostics.CodeAnalysis;

namespace codecrafters_redis.Rdb.List;

public sealed record ListRecord : Record
{
    private readonly ConcurrentDeque<string> _entries;

    private ListRecord(ConcurrentDeque<string> entries, DateTime? expireAt = null) : base(entries, ValueType.List, expireAt)
    {
        _entries = entries;
    }

    public int Count => _entries.Count;

    public static ListRecord Create(string[] values, DateTime? expireAt = null, bool isReversed = false)
    {
        var deque = new ConcurrentDeque<string>();

        if (values.Length <= 0)
            return new ListRecord(deque, expireAt);

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

        return new ListRecord(deque, expireAt);
    }

    public void Append(string entry) => _entries.PushRight(entry);

    public void Prepend(string entry) => _entries.PushLeft(entry);

    public string? PopLeft() => _entries.TryPopLeft(out var value) ? value : null;

    public string? PopRight() => _entries.TryPopRight(out var value) ? value : null;

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