using codecrafters_redis.RESP;

namespace codecrafters_redis.Commands.Echo;

public record EchoCommand(BulkString Message) : ICommand<BulkString>
{
    public static string Name => "ECHO";
}