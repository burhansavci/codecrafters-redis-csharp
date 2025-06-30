using codecrafters_redis.RESP;

namespace codecrafters_redis.Commands.Echo;

public class EchoCommandHandler : ICommandHandler<EchoCommand, BulkString>
{
    public BulkString Handle(EchoCommand command) => command.Message;
}