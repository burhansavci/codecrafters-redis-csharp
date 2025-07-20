using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Rdb.Records;
using codecrafters_redis.Resp;
using codecrafters_redis.Server;

namespace codecrafters_redis.Commands;

public class GetCommand(Database db) : ICommand
{
    public const string Name = "GET";

    public async Task Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(args.Length, 1);

        var key = args[0].GetString("key");

        var response = db.TryGetValue<StringRecord>(key, out var record) ? new BulkString(record.Value) : new BulkString(null);

        await connection.SendResp(response);
    }
}