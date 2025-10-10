using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Resp;
using Array = codecrafters_redis.Resp.Array;

namespace codecrafters_redis.Commands;

public class GeoSearchCommand(Database db) : ICommand
{
    public const string Name = "GEOSEARCH";

    public Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfNotEqual(args.Length, 7);

        var key = args[0].GetString("key");
        
        args[1].GetString("from option");
        
        if (!double.TryParse(args[2].GetString("longitude"), out var longitude))
            throw new ArgumentException("Invalid longitude. Expected double.");

        if (!double.TryParse(args[3].GetString("latitude"), out var latitude))
            throw new ArgumentException("Invalid latitude. Expected double.");
        
        args[4].GetString("by option");
        
        if (!double.TryParse(args[5].GetString("radius"), out var radius))
            throw new ArgumentException("Invalid radius. Expected double.");

        var unit = args[6].GetString("unit");

        var members = db.GeoSearch(key, longitude, latitude, radius, unit);

        return members is null
            ? Task.FromResult<RespObject>(new Array(null))
            : Task.FromResult<RespObject>(new Array(members.Select(x => new BulkString(x)).ToArray<RespObject>()));
    }
}