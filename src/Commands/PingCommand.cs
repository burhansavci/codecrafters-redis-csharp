using System.Net.Sockets;
using codecrafters_redis.Resp;

namespace codecrafters_redis.Commands;

public class PingCommand : ICommand
{
    public const string Name = "PING";
    private static readonly SimpleString Pong = new("PONG");

    public Task<RespObject> Handle(Socket connection, RespObject[] args) => Task.FromResult<RespObject>(Pong);
}