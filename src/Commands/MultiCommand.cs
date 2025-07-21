using System.Net.Sockets;
using codecrafters_redis.Resp;
using codecrafters_redis.Server;

namespace codecrafters_redis.Commands;

public class MultiCommand(RedisServer server) : ICommand
{
    public const string Name = "MULTI";

    public async Task Handle(Socket connection, RespObject[] args)
    {
        server.StartExecWaitingCommands(connection);

        await connection.SendResp(SimpleString.Ok);
    }
}