using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Resp;

namespace codecrafters_redis.Commands;

public class ZRankCommand(Database db) : ICommand
{
    public const string Name = "ZRANK";

    public Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfNotEqual(args.Length, 2);

        var sortedSetKey = args[0].GetString("sortedSetKey");
        var member = args[1].GetString("member");

        var index = db.SortedSet.Rank(sortedSetKey, member);

        return index is null
            ? Task.FromResult<RespObject>(new BulkString(null))
            : Task.FromResult<RespObject>(new Integer(index.Value));
    }
}