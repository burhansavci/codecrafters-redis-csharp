using System.Net.Sockets;
using codecrafters_redis.Resp;
using codecrafters_redis.Server;

namespace codecrafters_redis.Commands;

public class MultiCommand : ICommand
{
    public const string Name = "MULTI";
    
    public async Task Handle(Socket connection, RespObject[] args)
    {
        await connection.SendResp(SimpleString.Ok);
    }
}