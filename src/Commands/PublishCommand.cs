using System.Net.Sockets;
using codecrafters_redis.Resp;
using codecrafters_redis.Server.Channels;

namespace codecrafters_redis.Commands;

public class PublishCommand(ChannelManager channelManager) : ICommand
{
    public const string Name = "PUBLISH";

    public async Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfNotEqual(args.Length, 2);

        var channelName = args[0].GetString("channelName");
        var message = args[1].GetString("message");

        var count = await channelManager.Publish(channelName, message);

        return new Integer(count);
    }
}