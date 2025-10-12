using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Resp;

namespace codecrafters_redis.Commands;

public class ZAddCommand(Database db) : ICommand
{
    public const string Name = "ZADD";

    public Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfNotEqual(args.Length, 3);

        var sortedSetKey = args[0].GetString("sortedSetKey");
        
        if (!decimal.TryParse(args[1].GetString("score"), out var score))
            throw new ArgumentException("Invalid score. Expected decimal.");
        
        var member = args[2].GetString("member");

        var count = db.SortedSet.Add(sortedSetKey, score, member);

        return Task.FromResult<RespObject>(new Integer(count));
    }
}