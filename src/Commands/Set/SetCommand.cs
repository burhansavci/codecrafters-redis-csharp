using codecrafters_redis.RESP;

namespace codecrafters_redis.Commands.Set;

public record SetCommand(BulkString Key, BulkString Value, TimeSpan? ExpiryTimeInMillSeconds) : ICommand<SimpleString>
{
    public static string Name => "SET";
}