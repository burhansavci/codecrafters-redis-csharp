namespace codecrafters_redis.Rdb.Records;

public record StringRecord(string Value, DateTime? ExpireAt = null) : Record(Value, ValueType.String, ExpireAt)
{
    public new string Value => (string)base.Value;
}