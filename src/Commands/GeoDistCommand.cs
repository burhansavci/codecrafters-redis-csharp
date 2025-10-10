using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Resp;

namespace codecrafters_redis.Commands;

public class GeoDistCommand(Database db) : ICommand
{
    public const string Name = "GEODIST";
    
    public Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfNotEqual(args.Length, 3);
        
        var key = args[0].GetString("key");
        
        var firstMember = args[1].GetString("firstMember");
        var secondMember = args[2].GetString("secondMember");
        
        var distance =  db.GeoDistance(key, firstMember, secondMember);
        
        return Task.FromResult<RespObject>(new BulkString(distance?.ToString()));
    }
}