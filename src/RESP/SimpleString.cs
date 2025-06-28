namespace codecrafters_redis.RESP;

public record SimpleString(string Data) : RespObject(DataType.SimpleString)
{
    public static implicit operator SimpleString(string data) => new(data);

    public static implicit operator string(SimpleString simpleString) => simpleString.ToString();

    public override string ToString()
    {
        return $"{FirstByte}{Data}{CRLF}";
    }
}