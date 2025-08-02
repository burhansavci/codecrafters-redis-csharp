using System.Net.Sockets;
using codecrafters_redis.Resp;
using codecrafters_redis.Server.Channels;
using Array = codecrafters_redis.Resp.Array;

namespace codecrafters_redis.Commands;

public class SubscribeCommand(ChannelManager channelManager) : ICommand
{
    public const string Name = "SUBSCRIBE";

    public Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfNotEqual(args.Length, 1);

        var channelName = args[0].GetString("channelName");

        var count = channelManager.Subscribe(channelName, connection);

        return Task.FromResult<RespObject>(new Array(
            new BulkString("subscribe"),
            new BulkString(channelName),
            new Integer(count)
        ));
    }
}