using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Resp;

namespace codecrafters_redis.Commands;

public class ZRemCommand(Database db) : ICommand
{
    public const string Name = "ZREM";

    public Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfNotEqual(args.Length, 2);

        var sortedSetKey = args[0].GetString("sortedSetKey");
        var member = args[1].GetString("member");

        var count = db.ZRem(sortedSetKey, member);

        return Task.FromResult<RespObject>(new Integer(count));
    }
}