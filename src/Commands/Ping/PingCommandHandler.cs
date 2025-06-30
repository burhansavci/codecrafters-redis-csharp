using codecrafters_redis.RESP;

namespace codecrafters_redis.Commands.Ping;

public class PingCommandHandler : ICommandHandler<PingCommand, SimpleString>
{
    public SimpleString Handle(PingCommand command) => new("PONG");
}