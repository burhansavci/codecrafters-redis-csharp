using System.Collections.Concurrent;

namespace codecrafters_redis.Rdb.Records;

public record ListRecord : Record
{
    private ConcurrentQueue<string> Entries { get; set; }

    private ListRecord(string listKey, ConcurrentQueue<string> entries, DateTime? expireAt = null) : base(entries, ValueType.List, expireAt)
    {
        Entries = entries;
        ListKey = listKey;
    }

    public string ListKey { get; }

    public int Count => Entries.Count;

    public static ListRecord Create(string listKey, DateTime? expireAt = null, params string[] values)
    {
        var list = new ConcurrentQueue<string>(values);

        return new ListRecord(listKey, list, expireAt);
    }

    public void Append(string entry) => Entries.Enqueue(entry);

    public void Prepend(string entry)
    {
        var list = Entries.ToList();
        list.Insert(0, entry);
        Entries = new ConcurrentQueue<string>(list);
    }
    
    public string? Pop() => Entries.TryDequeue(out var entry) ? entry : null;

    public string[] GetEntriesInRange(int startIndex, int endIndex)
    {
        if (startIndex < 0 || endIndex < 0 || startIndex > endIndex || startIndex >= Entries.Count)
            return [];

        var count = endIndex - startIndex + 1;
        if (endIndex >= Entries.Count)
            count = Entries.Count - startIndex;

        return Entries.ToArray().AsSpan(startIndex, count).ToArray();
    }
}