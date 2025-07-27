using System.Collections.Immutable;

namespace codecrafters_redis.Rdb.Stream;

public sealed record StreamRecord : Record, IDisposable
{
    private readonly SortedDictionary<StreamEntryId, ImmutableDictionary<string, string>> _entries;
    private readonly ReaderWriterLockSlim _lock = new();
    private StreamEntryId? _lastEntryId;

    private StreamRecord(SortedDictionary<StreamEntryId, ImmutableDictionary<string, string>> entries, DateTime? expireAt = null) : base(entries, ValueType.Stream, expireAt)
    {
        _entries = entries;
        _lastEntryId = entries.Count > 0 ? entries.Keys.Last() : null;
    }

    public StreamEntryId? LastEntryId
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _lastEntryId;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }
    
    public static StreamRecord Create(StreamEntryId entryId, IEnumerable<KeyValuePair<string, string>> fields, DateTime? expireAt = null)
    {
        var entries = new SortedDictionary<StreamEntryId, ImmutableDictionary<string, string>>();
        var entryData = fields.ToImmutableDictionary();
        entries.Add(entryId, entryData);

        return new StreamRecord(entries, expireAt);
    }

    public bool TryAppendEntry(StreamEntryId entryId, IEnumerable<KeyValuePair<string, string>> fields)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_lastEntryId.HasValue && entryId <= _lastEntryId.Value)
                return false;

            var entryData = fields.ToImmutableDictionary();
            _entries.Add(entryId, entryData);
            _lastEntryId = entryId;

            return true;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public List<KeyValuePair<StreamEntryId, ImmutableDictionary<string, string>>> GetEntriesInRange(StreamEntryId startId, StreamEntryId endId)
    {
        _lock.EnterReadLock();
        try
        {
            return _entries
                .SkipWhile(kvp => kvp.Key < startId)
                .TakeWhile(kvp => kvp.Key <= endId)
                .ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public List<KeyValuePair<StreamEntryId, ImmutableDictionary<string, string>>> GetEntriesAfter(StreamEntryId startId)
    {
        _lock.EnterReadLock();
        try
        {
            return _entries
                .SkipWhile(kvp => kvp.Key <= startId)
                .ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Dispose()
    {
        _lock.Dispose();
    }
}