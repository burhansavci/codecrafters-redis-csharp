using codecrafters_redis.RESP;

namespace codecrafters_redis.Commands.Get;

public class GetCommandHandler : ICommandHandler<GetCommand, BulkString>
{
    public BulkString Handle(GetCommand command) =>
        Server.Db.TryGetValue(command.Key.Data!, out var record) ? new BulkString(record.Value) : new BulkString(null);
}