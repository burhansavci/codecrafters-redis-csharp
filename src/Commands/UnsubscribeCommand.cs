using System.Net.Sockets;
using codecrafters_redis.Resp;
using codecrafters_redis.Server.Channels;
using Array = codecrafters_redis.Resp.Array;

namespace codecrafters_redis.Commands;

public class UnsubscribeCommand(ChannelManager channelManager) : ICommand
{
    public const string Name = "UNSUBSCRIBE";

    public Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfNotEqual(args.Length, 1);

        var channelName = args[0].GetString("channelName");

        var count = channelManager.Unsubscribe(channelName, connection);
      
        return Task.FromResult<RespObject>(new Array(
            new BulkString("unsubscribe"),
            new BulkString(channelName),
            new Integer(count)
        ));
    }
}