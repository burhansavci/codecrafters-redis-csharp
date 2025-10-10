using System.Globalization;
using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Resp;
using Array = codecrafters_redis.Resp.Array;

namespace codecrafters_redis.Commands;

public class GeoPosCommand(Database db) : ICommand
{
    public const string Name = "GEOPOS";

    public Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfLessThan(args.Length, 2);

        var key = args[0].GetString("key");

        var members = args.Skip(1).Select(x => x.GetString("member")).ToArray();

        var positions = new List<RespObject>();
        foreach (var member in members)
        {
            var position = db.GeoPos(key, member);

            var positionArray = position is null
                ? new Array(null)
                : new Array(
                    new BulkString(position.Value.Longitude.ToString(CultureInfo.InvariantCulture)),
                    new BulkString(position.Value.Latitude.ToString(CultureInfo.InvariantCulture))
                );

            positions.Add(positionArray);
        }

        return Task.FromResult<RespObject>(new Array(positions.ToArray()));
    }
}