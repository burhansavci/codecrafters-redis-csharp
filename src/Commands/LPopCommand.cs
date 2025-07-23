using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Rdb.Records;
using codecrafters_redis.Resp;
using Array = codecrafters_redis.Resp.Array;

namespace codecrafters_redis.Commands;

public class LPopCommand(Database db) : ICommand
{
    public const string Name = "LPOP";

    public Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(args.Length, 2);

        db.TryGetValue<ListRecord>(args[0].GetString("listKey"), out var listRecord);

        var count = args.Length > 1 ? int.Parse(args[1].GetString("count")) : 1;

        if (listRecord is null)
            return Task.FromResult<RespObject>(new BulkString(null));

        var value = listRecord.Pop(count);

        if (value is null)
            return Task.FromResult<RespObject>(new BulkString(null));

        return value.Length == 1
            ? Task.FromResult<RespObject>(new BulkString(value[0]))
            : Task.FromResult<RespObject>(new Array(value.Select(x => new BulkString(x)).ToArray<RespObject>()));
    }
}