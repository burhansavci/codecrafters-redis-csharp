using System.Net.Sockets;
using System.Text;
using codecrafters_redis.RESP;

namespace codecrafters_redis.Commands;

public class PingCommand : ICommand
{
    public const string Name = "PING";
    private static readonly string Pong = new SimpleString("PONG");

    public async Task Handle(Socket connection, RespObject[] args) => await connection.SendAsync(Encoding.UTF8.GetBytes(Pong));
}