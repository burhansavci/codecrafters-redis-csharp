namespace codecrafters_redis.Resp;

public static class DataType
{
    public const char SimpleString = '+';

    public const char BulkString = '$';

    public const char Array = '*';

    public const char Integer = ':';
    
    public const char SimpleError = '-';
}