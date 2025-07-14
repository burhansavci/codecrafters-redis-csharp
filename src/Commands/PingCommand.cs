using System.Net.Sockets;
using codecrafters_redis.Resp;
using codecrafters_redis.Server;

namespace codecrafters_redis.Commands;

public class PingCommand : ICommand
{
    public const string Name = "PING";
    private static readonly SimpleString Pong = new("PONG");

    public async Task Handle(Socket connection, RespObject[] args) => await connection.SendResp(Pong);
}