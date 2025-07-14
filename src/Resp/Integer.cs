namespace codecrafters_redis.Resp;

public record Integer(int Value) : RespObject(DataType.Integer)
{
    public static implicit operator Integer(int value) => new(value);

    public static implicit operator string(Integer integer) => integer.ToString();

    public override string ToString() => $"{FirstByte}{Value}{CRLF}";
}