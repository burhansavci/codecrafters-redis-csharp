using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Rdb.Records;
using codecrafters_redis.Resp;
using codecrafters_redis.Server;

namespace codecrafters_redis.Commands;

public class SetCommand(Database db) : ICommand
{
    public const string Name = "SET";

    public async Task Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(args.Length, 4);

        var utcNow = DateTime.UtcNow;
        TimeSpan? expireTime = null;

        if (args[0] is not BulkString key)
            throw new FormatException("Invalid key format. Expected bulk string.");

        if (args[1] is not BulkString value)
            throw new FormatException("Invalid value format. Expected bulk string.");

        if (args.Length > 2)
        {
            if (args[2] is not BulkString expireOption)
                throw new FormatException("Invalid expire option format. Expected bulk string.");

            if (!string.Equals(expireOption.Data, "PX", StringComparison.OrdinalIgnoreCase))
                throw new FormatException("Invalid expire option. Expected 'PX'.");

            if (args[3] is not BulkString expireTimeMs)
                throw new FormatException("Invalid expire time format. Expected bulk string.");

            if (!long.TryParse(expireTimeMs.Data, out var expireTimeMsLong))
                throw new FormatException("Invalid expire time format. Expected numeric value.");

            expireTime = TimeSpan.FromMilliseconds(expireTimeMsLong);
        }

        db.Add(key.Data!, new StringRecord(value.Data!, utcNow + expireTime));

        await connection.SendResp(SimpleString.Ok);
    }
}