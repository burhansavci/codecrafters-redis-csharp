using codecrafters_redis.RESP;

namespace codecrafters_redis.Commands.Set;

public class SetCommandHandler : ICommandHandler<SetCommand, SimpleString>
{
    public SimpleString Handle(SetCommand command)
    {
        var utcNow = DateTime.UtcNow;

        Server.Db[command.Key.Data!] = new Record(command.Value.Data!, utcNow + command.ExpiryTimeInMillSeconds);

        return SimpleString.Ok;
    }
}