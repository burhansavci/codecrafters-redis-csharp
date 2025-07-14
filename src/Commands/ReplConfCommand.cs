using System.Net.Sockets;
using codecrafters_redis.Resps;
using codecrafters_redis.Server;

namespace codecrafters_redis.Commands;

public class ReplConfCommand(RedisServer server) : ICommand
{
    public const string Name = "REPLCONF";

    public async Task Handle(Socket connection, RespObject[] args)
    {
        server.ConnectedReplications.Add(connection);
        
       await connection.SendResp(SimpleString.Ok);
    }
}