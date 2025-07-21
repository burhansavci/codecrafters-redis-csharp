using System.Net.Sockets;
using codecrafters_redis.Resp;
using codecrafters_redis.Server;

namespace codecrafters_redis.Commands;

public class ExecCommand : ICommand
{
    public const string Name = "EXEC";

    private const string NoTransactionError = "ERR EXEC without MULTI";

    public async Task Handle(Socket connection, RespObject[] args)
    {
        await connection.SendResp(new SimpleError(NoTransactionError));
    }
}