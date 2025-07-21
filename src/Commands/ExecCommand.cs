using System.Net.Sockets;
using codecrafters_redis.Resp;
using codecrafters_redis.Server;
using Array = codecrafters_redis.Resp.Array;

namespace codecrafters_redis.Commands;

public class ExecCommand(RedisServer server) : ICommand
{
    public const string Name = "EXEC";

    private const string NoTransactionError = "ERR EXEC without MULTI";

    public async Task Handle(Socket connection, RespObject[] args)
    {
        var executedCommandCount = await server.ExecuteWaitingCommands(connection);
        
        switch (executedCommandCount)
        {
            case -1:
                await connection.SendResp(new SimpleError(NoTransactionError));
                return;
            case 0:
                await connection.SendResp(new Array());
                break;
        }
    }
}