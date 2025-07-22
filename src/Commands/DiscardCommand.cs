using System.Net.Sockets;
using codecrafters_redis.Resp;
using codecrafters_redis.Server.Transactions;

namespace codecrafters_redis.Commands;

public class DiscardCommand(TransactionManager transactionManager) : ICommand
{
    public const string Name = "DISCARD";
    private const string NoTransactionError = "ERR DISCARD without MULTI";

    public Task<RespObject> Handle(Socket connection, RespObject[] args)
        => transactionManager.DiscardTransaction(connection) ? Task.FromResult<RespObject>(SimpleString.Ok) : Task.FromResult<RespObject>(new SimpleError(NoTransactionError));
}