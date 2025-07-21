using System.Net.Sockets;
using codecrafters_redis.Resp;
using codecrafters_redis.Server;
using codecrafters_redis.Server.Transactions;

namespace codecrafters_redis.Commands;

public class MultiCommand(TransactionManager transactionManager) : ICommand
{
    public const string Name = "MULTI";
    
    public Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        transactionManager.StartTransaction(connection);

        return Task.FromResult<RespObject>(SimpleString.Ok);
    }
}