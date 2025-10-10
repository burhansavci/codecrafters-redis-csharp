using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Rdb.Extensions.Geo;
using codecrafters_redis.Resp;

namespace codecrafters_redis.Commands;

public class GeoAddCommand(Database db) : ICommand
{
    public const string Name = "GEOADD";

    public Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfNotEqual(args.Length, 4);

        var key = args[0].GetString("key");

        if (!double.TryParse(args[1].GetString("longitude"), out var longitude))
            throw new ArgumentException("Invalid longitude. Expected decimal.");

        if (!double.TryParse(args[2].GetString("latitude"), out var latitude))
            throw new ArgumentException("Invalid latitude. Expected decimal.");

        if (longitude is < GeoHashConverter.MinLongitude or > GeoHashConverter.MaxLongitude ||
            latitude is < GeoHashConverter.MinLatitude or > GeoHashConverter.MaxLatitude)
            return Task.FromResult<RespObject>(new SimpleError($"ERR invalid longitude,latitude pair {longitude},{latitude}"));

        var member = args[3].GetString("member");

        db.GeoAdd(key, longitude, latitude, member);

        return Task.FromResult<RespObject>(new Integer(1));
    }
}