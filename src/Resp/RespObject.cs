using System.Diagnostics.CodeAnalysis;

namespace codecrafters_redis.Resp;

public record RespObject(char FirstByte)
{
    // ReSharper disable once InconsistentNaming
    public const string CRLF = "\r\n";

    public bool TryGetString([NotNullWhen(true)] out string? value)
    {
        if (this is BulkString { Data: not null } bulkString)
        {
            value = bulkString.Data;
            return true;
        }

        value = null;
        return false;
    }

    public string GetString(string parameterName = "")
    {
        if (this is not BulkString bulkString || bulkString.Data == null)
            throw new ArgumentException($"Invalid {parameterName} format. Expected bulk string.");

        return bulkString.Data;
    }
}