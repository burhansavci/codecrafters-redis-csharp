using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Rdb.Records;
using codecrafters_redis.Resp;

namespace codecrafters_redis.Commands;

public class LPopCommand(Database db) : ICommand
{
    public const string Name = "LPOP";

    public Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfNotEqual(args.Length, 1);

        db.TryGetValue<ListRecord>(args[0].GetString("listKey"), out var listRecord);

        return Task.FromResult<RespObject>(listRecord is null ? new BulkString(null) : new BulkString(listRecord.Pop()));
    }
}