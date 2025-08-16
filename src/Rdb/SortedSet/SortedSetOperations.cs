using System.Collections.Concurrent;
using codecrafters_redis.Rdb.Extensions;

namespace codecrafters_redis.Rdb.SortedSet;

public sealed class SortedSetOperations(ConcurrentDictionary<string, Record> records) : IDisposable
{
    private bool _disposed;

    public int Add(string sortedSetKey, decimal score, string member)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrEmpty(sortedSetKey);
        ArgumentNullException.ThrowIfNull(member);

        if (!records.TryGetRecord<SortedSetRecord>(sortedSetKey, out var sortedSet))
        {
            sortedSet = SortedSetRecord.Create([new SortedSetItem(score, member)]);
            records.AddOrUpdate(sortedSetKey, sortedSet, (_, _) => sortedSet);
            return 1;
        }

        var newItem = new SortedSetItem(score, member);
        records.AddOrUpdate(sortedSetKey, sortedSet, (_, _) => sortedSet);
        return sortedSet.Add(newItem);
    }

    public int? Rank(string sortedSetKey, string member)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrEmpty(sortedSetKey);
        ArgumentNullException.ThrowIfNull(member);
        
        return !records.TryGetRecord<SortedSetRecord>(sortedSetKey, out var sortedSet) ? null : sortedSet.Rank(member);
    }
    
    public decimal? Score(string sortedSetKey, string member)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrEmpty(sortedSetKey);
        ArgumentNullException.ThrowIfNull(member);
        
        return !records.TryGetRecord<SortedSetRecord>(sortedSetKey, out var sortedSet) ? null : sortedSet.Score(member);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
    }
}