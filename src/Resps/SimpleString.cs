namespace codecrafters_redis.Resps;

public record SimpleString(string Data) : RespObject(DataType.SimpleString)
{
    public static SimpleString Ok => new("OK");
    
    public static implicit operator SimpleString(string data) => new(data);
    public static implicit operator string(SimpleString simpleString) => simpleString.ToString();

    public static SimpleString Parse(string rawSimpleString)
    {
        var endIndex = rawSimpleString.Length - CRLF.Length; // Remove \r\n
        var data = rawSimpleString.Substring(1, endIndex - 1); // Skip the first byte 

        return data;
    }

    public override string ToString()
    {
        return $"{FirstByte}{Data}{CRLF}";
    }
}