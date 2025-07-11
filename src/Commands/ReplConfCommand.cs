using System.Net.Sockets;
using System.Text;
using codecrafters_redis.RESP;

namespace codecrafters_redis.Commands;

public class ReplConfCommand : ICommand
{
    public const string Name = "REPLCONF";

    public async Task Handle(Socket connection, RespObject[] args)
    {
        await connection.SendAsync(Encoding.UTF8.GetBytes(SimpleString.Ok));
    }
}