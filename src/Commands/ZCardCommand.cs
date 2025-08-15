using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Rdb.SortedSet;
using codecrafters_redis.Resp;

namespace codecrafters_redis.Commands;

public class ZCardCommand(Database db) : ICommand
{
    public const string Name = "ZCARD";
    
    public Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfNotEqual(args.Length, 1);
        
        var sortedSetKey = args[0].GetString("sortedSetKey");

        if (!db.TryGetRecord<SortedSetRecord>(sortedSetKey, out var sortedSetRecord))
            return Task.FromResult<RespObject>(new Integer(0));
        
        return Task.FromResult<RespObject>(new Integer(sortedSetRecord.Count));
    }
}