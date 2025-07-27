using System.Net.Sockets;
using codecrafters_redis.Rdb;
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
        
        var listKey = args[0].GetString("listKey");
        var count = args.Length > 1 ? int.Parse(args[1].GetString("count")) : 1;
        
        var values = db.Pop(listKey, count);

        if (values is null)
            return Task.FromResult<RespObject>(new BulkString(null));

        return values.Length == 1
            ? Task.FromResult<RespObject>(new BulkString(values[0]))
            : Task.FromResult<RespObject>(new Array(values.Select(x => new BulkString(x)).ToArray<RespObject>()));
    }
}