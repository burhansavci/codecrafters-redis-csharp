namespace codecrafters_redis.Resp;

public record BulkString(string? Data) : RespObject(DataType.BulkString)
{
    public static implicit operator BulkString(string data) => new(data);

    public static implicit operator string(BulkString bulkString) => bulkString.ToString();

    public override string ToString()
    {
        return Data is null ? $"{FirstByte}-1{CRLF}" : $"{FirstByte}{Data.Length}{CRLF}{Data}{CRLF}";
    }
}