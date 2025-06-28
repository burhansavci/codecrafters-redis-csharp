namespace codecrafters_redis;

public record Record(string Value, DateTime? ExpireAt = null)
{
    public static implicit operator Record(string value) => new(value);
}