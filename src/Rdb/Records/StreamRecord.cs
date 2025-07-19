using System.Collections.Immutable;

namespace codecrafters_redis.Rdb.Records;

public sealed record StreamRecord : Record
{
    private readonly SortedDictionary<StreamEntryId, ImmutableDictionary<string, string>> _entries;
    private StreamEntryId? _lastEntryId;

    private StreamRecord(string streamKey, SortedDictionary<StreamEntryId, ImmutableDictionary<string, string>> entries, DateTime? expireAt = null) : base(entries, ValueType.Stream, expireAt)
    {
        StreamKey = streamKey;
        _entries = entries;
        _lastEntryId = entries.Count > 0 ? entries.Keys.Last() : null;
    }

    public StreamEntryId? LastEntryId => _lastEntryId;
    public string StreamKey { get; }

    public static StreamRecord Create(string streamKey, StreamEntryId entryId, IEnumerable<KeyValuePair<string, string>> fields, DateTime? expireAt = null)
    {
        var entries = new SortedDictionary<StreamEntryId, ImmutableDictionary<string, string>>();
        var entryData = fields.ToImmutableDictionary();
        entries.Add(entryId, entryData);

        return new StreamRecord(streamKey, entries, expireAt);
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

    public IEnumerable<KeyValuePair<StreamEntryId, ImmutableDictionary<string, string>>> GetEntriesInRange(StreamEntryId startId, StreamEntryId endId) =>
        _entries
            .SkipWhile(kvp => kvp.Key < startId)
            .TakeWhile(kvp => kvp.Key <= endId);

    public IEnumerable<KeyValuePair<StreamEntryId, ImmutableDictionary<string, string>>> GetEntriesAfter(StreamEntryId startId) =>
        _entries.SkipWhile(kvp => kvp.Key <= startId);
}