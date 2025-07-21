using System.Net.Sockets;
using codecrafters_redis.Resp;
using codecrafters_redis.Server;
using codecrafters_redis.Server.Transactions;
using Array = codecrafters_redis.Resp.Array;

namespace codecrafters_redis.Commands;

public class ExecCommand(TransactionManager transactionManager, ConnectionHandler connectionHandler) : ICommand
{
    public const string Name = "EXEC";

    private const string NoTransactionError = "ERR EXEC without MULTI";

    public async Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        var responses = await transactionManager.ExecuteTransaction(connection, connectionHandler);

        if (responses is null)
            return new SimpleError(NoTransactionError);

        return responses.Count == 0 ? new Array() : new Array(responses.ToArray());
    }
}