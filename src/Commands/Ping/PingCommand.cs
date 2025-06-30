using codecrafters_redis.RESP;

namespace codecrafters_redis.Commands.Ping;

public record PingCommand : ICommand<SimpleString>
{
    public static string Name => "PING";
}