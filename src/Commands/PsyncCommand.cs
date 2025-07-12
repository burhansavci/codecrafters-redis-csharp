using System.Net.Sockets;
using System.Text;
using codecrafters_redis.RESP;

namespace codecrafters_redis.Commands;

public class PsyncCommand : ICommand
{
    public const string Name = "PSYNC";

    public async Task Handle(Socket connection, RespObject[] args)
    {
        var response = new SimpleString("FULLRESYNC");

        await connection.SendAsync(Encoding.UTF8.GetBytes(response));
    }
}