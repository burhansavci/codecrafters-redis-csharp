namespace codecrafters_redis.Rdb.Records;

public record Record(object Value, ValueType Type, DateTime? ExpireAt = null)
{
    public bool IsExpired => ExpireAt is not null && DateTime.UtcNow > ExpireAt;
}