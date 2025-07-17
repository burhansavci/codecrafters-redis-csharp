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

        if (args[0] is not BulkString key)
            throw new FormatException("Invalid key format. Expected bulk string.");

        var keyData = key.Data!;

        var response = db.TryGetValue<StringRecord>(keyData, out var record) ? new BulkString(record.Value) : new BulkString(null);

        await connection.SendResp(response);
    }
}