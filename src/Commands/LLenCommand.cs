using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Rdb.List;
using codecrafters_redis.Rdb.Records;
using codecrafters_redis.Resp;

namespace codecrafters_redis.Commands;

public class LLenCommand(Database db) : ICommand
{
    public const string Name = "LLEN";

    public Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfNotEqual(args.Length, 1);

        db.TryGetValue<ListRecord>(args[0].GetString("listKey"), out var listRecord);

        return Task.FromResult<RespObject>(listRecord is null ? new Integer(0) : new Integer(listRecord.Count));
    }
}