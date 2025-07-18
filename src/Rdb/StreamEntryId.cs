using System.Globalization;

namespace codecrafters_redis.Rdb;

public struct StreamEntryId : IComparable<StreamEntryId>, IEquatable<StreamEntryId>
{
    public long Timestamp { get; }
    public long Sequence { get; }

    public static StreamEntryId Zero => new(0, 0);
    
    public static StreamEntryId MaxValue => new (long.MaxValue, long.MaxValue);

    private StreamEntryId(long timestamp, long sequence)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(timestamp);
        ArgumentOutOfRangeException.ThrowIfNegative(sequence);

        Timestamp = timestamp;
        Sequence = sequence;
    }

    public static StreamEntryId Create(string id, StreamEntryId? lastIdInStream = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);

        return id switch
        {
            "*" => FullyAutoGenerate(lastIdInStream),
            _ when id.EndsWith("-*") => PartiallyAutoGenerate(id, lastIdInStream),
            _ => GenerateExplicit(id)
        };
    }

    private static StreamEntryId FullyAutoGenerate(StreamEntryId? lastIdInStream)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var sequence = timestamp == lastIdInStream?.Timestamp ? lastIdInStream.Value.Sequence + 1 : 0;

        return new StreamEntryId(timestamp, sequence);
    }

    private static StreamEntryId PartiallyAutoGenerate(string id, StreamEntryId? lastIdInStream)
    {
        var timestampStr = id.AsSpan(0, id.Length - 2); // Remove "-*"

        if (!long.TryParse(timestampStr, NumberStyles.None, CultureInfo.InvariantCulture, out var timestamp))
            throw new FormatException($"Invalid timestamp format in ID: {id}");

        long sequence;
        if (!lastIdInStream.HasValue)
            sequence = timestamp == 0 ? 1 : 0;
        else
        {
            var lastId = lastIdInStream.Value;

            sequence = timestamp switch
            {
                _ when timestamp > lastId.Timestamp => 0,
                _ when timestamp == lastId.Timestamp => lastId.Sequence + 1,
                _ => 0 // timestamp < lastId.Timestamp
            };
        }

        return new StreamEntryId(timestamp, sequence);
    }

    private static StreamEntryId GenerateExplicit(string id)
    {
        var separatorIndex = id.IndexOf('-');
        if (separatorIndex == -1)
            throw new FormatException($"Invalid stream entry ID format: {id}. Expected 'timestamp-sequence'.");

        var timestampSpan = id.AsSpan(0, separatorIndex);
        var sequenceSpan = id.AsSpan(separatorIndex + 1);

        if (!long.TryParse(timestampSpan, NumberStyles.None, CultureInfo.InvariantCulture, out var timestamp) ||
            !long.TryParse(sequenceSpan, NumberStyles.None, CultureInfo.InvariantCulture, out var sequence))
            throw new FormatException($"Invalid number format in stream entry ID: {id}");

        return new StreamEntryId(timestamp, sequence);
    }

    public int CompareTo(StreamEntryId other)
    {
        var timestampComparison = Timestamp.CompareTo(other.Timestamp);
        return timestampComparison != 0 ? timestampComparison : Sequence.CompareTo(other.Sequence);
    }

    public override string ToString() => $"{Timestamp}-{Sequence}";

    public override bool Equals(object? obj) => obj is StreamEntryId other && Equals(other);

    public bool Equals(StreamEntryId other) => Timestamp == other.Timestamp && Sequence == other.Sequence;

    public override int GetHashCode() => HashCode.Combine(Timestamp, Sequence);

    public static bool operator ==(StreamEntryId left, StreamEntryId right) => left.Equals(right);
    public static bool operator !=(StreamEntryId left, StreamEntryId right) => !left.Equals(right);
    public static bool operator <(StreamEntryId left, StreamEntryId right) => left.CompareTo(right) < 0;
    public static bool operator <=(StreamEntryId left, StreamEntryId right) => left.CompareTo(right) <= 0;
    public static bool operator >(StreamEntryId left, StreamEntryId right) => left.CompareTo(right) > 0;
    public static bool operator >=(StreamEntryId left, StreamEntryId right) => left.CompareTo(right) >= 0;
}