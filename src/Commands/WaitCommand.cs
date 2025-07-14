using System.Net.Sockets;
using codecrafters_redis.Resp;
using codecrafters_redis.Server;

namespace codecrafters_redis.Commands;

public class WaitCommand(RedisServer server) : ICommand
{
    public const string Name = "WAIT";

    public async Task Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(args.Length, 2);

        await connection.SendResp(new Integer(server.ConnectedReplications.Count));
    }
}