using System.Collections.Concurrent;

namespace codecrafters_redis.Rdb.Records;

public record ListRecord : Record
{
    private readonly ConcurrentQueue<string> _entries;

    private ListRecord(string listKey, ConcurrentQueue<string> entries, DateTime? expireAt = null) : base(entries, ValueType.List, expireAt)
    {
        _entries = entries;
        ListKey = listKey;
    }

    public string ListKey { get; }

    public int Count => _entries.Count;

    public static ListRecord Create(string listKey, DateTime? expireAt = null, params string[] values)
    {
        var list = new ConcurrentQueue<string>(values);

        return new ListRecord(listKey, list, expireAt);
    }

    public void Append(string entry) => _entries.Enqueue(entry);
}