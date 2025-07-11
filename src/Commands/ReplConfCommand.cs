using System.Net.Sockets;
using System.Text;
using codecrafters_redis.RESP;
using codecrafters_redis.Server;

namespace codecrafters_redis.Commands;

public class ReplConfCommand(RedisServer server) : ICommand
{
    public const string Name = "REPLCONF";

    public async Task Handle(Socket connection, RespObject[] args)
    {
        server.ConnectedReplications.Add(connection);
        
        await connection.SendAsync(Encoding.UTF8.GetBytes(SimpleString.Ok));
    }
}