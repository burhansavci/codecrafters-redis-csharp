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

    public bool AppendStreamEntry(StreamEntryId streamEntryId, string streamEntryKey, string streamEntryValue)
    {
        if (streamEntryId <= Value.Last().Key)
            return false;

        var streamEntry = new Dictionary<string, string> { { streamEntryKey, streamEntryValue } };

        Value.Add(streamEntryId, streamEntry);

        return true;
    }
}