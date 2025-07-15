using System.Net.Sockets;
using codecrafters_redis.Resp;
using codecrafters_redis.Server;

namespace codecrafters_redis.Commands;

public class ReplConfCommand(RedisServer server) : ICommand
{
    public const string Name = "REPLCONF";

    public async Task Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfNotEqual(args.Length, 2);

        server.AddReplica(connection);

        await connection.SendResp(SimpleString.Ok);
    }
}