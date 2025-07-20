using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Rdb.Records;
using codecrafters_redis.Resp;
using codecrafters_redis.Server;

namespace codecrafters_redis.Commands;

public class IncrCommand(Database db) : ICommand
{
    public const string Name = "INCR";

    public async Task Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(args.Length, 1);

        var key = args[0].GetString("key");

        var newValue = db.TryGetValue<StringRecord>(key, out var stringRecord) ? int.Parse(stringRecord.Value) + 1 : 1;

        db.AddOrUpdate(key, new StringRecord(newValue.ToString()));

        await connection.SendResp(new Integer(newValue));
    }
}