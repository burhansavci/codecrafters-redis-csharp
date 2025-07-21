namespace codecrafters_redis.Resp;

/// <summary>
/// Represents a response handled by the command itself,
/// signaling to the ConnectionHandler that no further response-sending operation is needed.
/// </summary>
public record SelfHandled() : RespObject('!') // Using a non-standard RESP type for internal signaling
{
    public static readonly SelfHandled Instance = new();
}