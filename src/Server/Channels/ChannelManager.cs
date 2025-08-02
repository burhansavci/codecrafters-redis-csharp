using System.Collections.Concurrent;
using System.Net.Sockets;

namespace codecrafters_redis.Server.Channels;

public class ChannelManager
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<Socket>> _channels = new();
    private readonly ConcurrentDictionary<Socket, int> _socketSubscriptionCounts = new();

    public int Subscribe(string channelName, Socket socket)
    {
        ArgumentException.ThrowIfNullOrEmpty(channelName);
        ArgumentNullException.ThrowIfNull(socket);

        _channels.AddOrUpdate(
            channelName,
            [socket],
            (_, existingSockets) =>
            {
                existingSockets.Add(socket);
                return existingSockets;
            });

        return _socketSubscriptionCounts.AddOrUpdate(
            socket,
            1,
            (_, currentCount) => currentCount + 1
        );
    }
}