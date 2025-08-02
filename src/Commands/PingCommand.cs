using System.Net.Sockets;
using codecrafters_redis.Resp;
using codecrafters_redis.Server.Channels;
using Array = codecrafters_redis.Resp.Array;

namespace codecrafters_redis.Commands;

public class PingCommand(ChannelManager channelManager) : ICommand
{
    public const string Name = "PING";
    private static readonly SimpleString Pong = new("PONG");
    private static readonly Array PongArray = new(new BulkString("pong"), new BulkString(string.Empty));

    public Task<RespObject> Handle(Socket connection, RespObject[] args) =>
        channelManager.IsInSubscribedMode(connection)
            ? Task.FromResult<RespObject>(PongArray)
            : Task.FromResult<RespObject>(Pong);
}