using System.Collections.Immutable;

namespace codecrafters_redis.Rdb.Records;

public sealed record StreamRecord : Record
{
    private readonly SortedDictionary<StreamEntryId, ImmutableDictionary<string, string>> _entries;
    private StreamEntryId? _lastEntryId;

    private StreamRecord(SortedDictionary<StreamEntryId, ImmutableDictionary<string, string>> entries, DateTime? expireAt = null) : base(entries, ValueType.Stream, expireAt)
    {
        _entries = entries;
        _lastEntryId = entries.Count > 0 ? entries.Keys.Last() : null;
    }

    public StreamEntryId? LastEntryId => _lastEntryId;

    public static StreamRecord Create(StreamEntryId entryId, IEnumerable<KeyValuePair<string, string>> fields, DateTime? expireAt = null)
    {
        var entries = new SortedDictionary<StreamEntryId, ImmutableDictionary<string, string>>();
        var entryData = fields.ToImmutableDictionary();
        entries.Add(entryId, entryData);

        return new StreamRecord(entries, expireAt);
    }

    public bool TryAppendEntry(StreamEntryId entryId, IEnumerable<KeyValuePair<string, string>> fields)
    {
        if (_lastEntryId.HasValue && entryId <= _lastEntryId.Value)
            return false;

        var entryData = fields.ToImmutableDictionary();
        _entries.Add(entryId, entryData);
        _lastEntryId = entryId;

        return true;
    }
}