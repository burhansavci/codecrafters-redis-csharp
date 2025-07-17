namespace codecrafters_redis.Rdb.Records;

public record StreamRecord(SortedDictionary<StreamEntryId, Dictionary<string, string>> Value, DateTime? ExpireAt = null) : Record(Value, ValueType.Stream, ExpireAt)
{
    public new SortedDictionary<StreamEntryId, Dictionary<string, string>> Value => (SortedDictionary<StreamEntryId, Dictionary<string, string>>)base.Value;

    public static StreamRecord Create(StreamEntryId streamEntryId, string streamEntryKey, string streamEntryValue, DateTime? expireAt = null)
    {
        var stream = new SortedDictionary<StreamEntryId, Dictionary<string, string>>();
        var streamEntry = new Dictionary<string, string> { { streamEntryKey, streamEntryValue } };

        stream.Add(streamEntryId, streamEntry);

        return new StreamRecord(stream, expireAt);
    }

    public void AddOrUpdateStream(StreamEntryId streamEntryId, string streamEntryKey, string streamEntryValue)
    {
        if (Value.TryGetValue(streamEntryId, out var streamEntries))
        {
            AddOrUpdateStreamEntry(streamEntries, streamEntryKey, streamEntryValue);
        }
        else
        {
            var streamEntry = new Dictionary<string, string> { { streamEntryKey, streamEntryValue } };

            Value.Add(streamEntryId, streamEntry);
        }
    }

    private static void AddOrUpdateStreamEntry(Dictionary<string, string> streamEntries, string streamEntryKey, string streamEntryValue)
    {
        if (!streamEntries.TryAdd(streamEntryKey, streamEntryValue))
            streamEntries[streamEntryKey] = streamEntryValue;
    }
}