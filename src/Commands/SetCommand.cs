using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Rdb.Records;
using codecrafters_redis.Resp;

namespace codecrafters_redis.Commands;

public class SetCommand(Database db) : ICommand
{
    public const string Name = "SET";

    public Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(args.Length, 4);

        var utcNow = DateTime.UtcNow;
        TimeSpan? expireTime = null;

        var key = args[0].GetString("key");
        var value = args[1].GetString("value");

        if (args.Length > 2)
        {
            var expireOption = args[2].GetString("expire option");

            if (!string.Equals(expireOption, "PX", StringComparison.OrdinalIgnoreCase))
                throw new FormatException("Invalid expire option. Expected 'PX'.");

            var expireTimeMs = args[3].GetString("expire time");

            if (!long.TryParse(expireTimeMs, out var expireTimeMsLong))
                throw new FormatException("Invalid expire time format. Expected numeric value.");

            expireTime = TimeSpan.FromMilliseconds(expireTimeMsLong);
        }

        db.AddOrUpdate(key, new StringRecord(value!, utcNow + expireTime));

        return Task.FromResult<RespObject>(SimpleString.Ok);
    }
}