namespace codecrafters_redis.Resp;

public record SimpleError(string Message) : RespObject(DataType.SimpleError)
{
    public static implicit operator SimpleError(string message) => new(message);
    public static implicit operator string(SimpleError simpleError) => simpleError.ToString();
    
    public override string ToString() => $"{FirstByte}{Message}{CRLF}";
}