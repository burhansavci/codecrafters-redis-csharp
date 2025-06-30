using codecrafters_redis.RESP;

namespace codecrafters_redis.Commands.Get;

public record GetCommand(BulkString Key) : ICommand<BulkString>
{
    public static string Name => "GET";
}