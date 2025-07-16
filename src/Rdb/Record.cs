namespace codecrafters_redis.Rdb;

public record Record(string Value, DateTime? ExpireAt = null, ValueType Type = ValueType.String)
{
    public static implicit operator Record(string value) => new(value);

    public bool IsExpired => ExpireAt is not null && DateTime.UtcNow > ExpireAt;
}