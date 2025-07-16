using System.Net.Sockets;
using codecrafters_redis.Resp;
using codecrafters_redis.Server;

namespace codecrafters_redis.Commands;

public class TypeCommand(RedisServer redisServer) : ICommand
{
    public const string Name = "TYPE";

    private const string None = "none";

    public async Task Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(args.Length, 1);

        if (args[0] is not BulkString keyArg)
            throw new FormatException("Invalid key format. Expected bulk string.");

        var key = keyArg.Data!;

        var type = redisServer.InMemoryDb.TryGetValue(key, out var record) ? record.Type.ToString().ToLowerInvariant() : None;

        await connection.SendResp(new SimpleString(type));
    }
}