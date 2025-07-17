namespace codecrafters_redis.Rdb;

public struct StreamEntryId : IComparable<StreamEntryId>, IEquatable<StreamEntryId>
{
    public long Timestamp { get; }
    public long Sequence { get; }

    public static StreamEntryId Zero => new(0, 0);

    public StreamEntryId(long timestamp, long sequence)
    {
        if (timestamp < 0 || sequence < 0)
            throw new ArgumentException("Timestamp and sequence must be non-negative.");

        Timestamp = timestamp;
        Sequence = sequence;
    }

    public static StreamEntryId Create(string id)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("Stream entry ID cannot be null or empty.", nameof(id));

        var parts = id.Split('-');
        if (parts.Length != 2)
            throw new FormatException("Invalid Stream entry ID format. Expected 'timestamp-sequence'.");

        if (!long.TryParse(parts[0], out var timestamp) || !long.TryParse(parts[1], out var sequence))
            throw new FormatException("Invalid number format in Stream entry ID components.");

        return new StreamEntryId(timestamp, sequence);
    }

    public int CompareTo(StreamEntryId other)
    {
        int timestampComparison = Timestamp.CompareTo(other.Timestamp);
        if (timestampComparison != 0)
        {
            return timestampComparison;
        }

        return Sequence.CompareTo(other.Sequence);
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