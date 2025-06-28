namespace codecrafters_redis.RESP;

public record BulkString(string Data) : RespObject(DataType.BulkString)
{
    public static implicit operator BulkString(string data) => new(data);
    
    public static implicit operator string(BulkString bulkString) => bulkString.ToString();
    
    public override string ToString()
    {
        return $"{FirstByte}{Data.Length}{CRLF}{Data}{CRLF}";
    }
}