using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Rdb.Records;
using codecrafters_redis.Resp;
using codecrafters_redis.Server;

namespace codecrafters_redis.Commands;

public class TypeCommand(Database db) : ICommand
{
    public const string Name = "TYPE";

    private const string None = "none";

    public async Task Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(args.Length, 1);

        var key = args[0].GetString("key");

        var type = db.TryGetValue<Record>(key, out var record) ? record.Type.ToString().ToLowerInvariant() : None;

        await connection.SendResp(new SimpleString(type));
    }
}