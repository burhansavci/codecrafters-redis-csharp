namespace codecrafters_redis.RESP;

public record RespObject(char FirstByte)
{
    // ReSharper disable once InconsistentNaming
    public const string CRLF = "\r\n";
}