using System.Collections.Concurrent;
using System.Net.Sockets;

namespace codecrafters_redis.Server.Channels;

public class ChannelManager
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<Socket>> _channels = new();
    private readonly ConcurrentDictionary<Socket, int> _connectionSubscriptionCounts = new();

    public static List<string> AllowedCommandsInSubscribedMode => ["SUBSCRIBE", "UNSUBSCRIBE", "PSUBSCRIBE", "PUNSUBSCRIBE", "PING", "QUIT"];

    public bool IsInSubscribedMode(Socket connection) => _connectionSubscriptionCounts.ContainsKey(connection);

    public int Subscribe(string channelName, Socket connection)
    {
        ArgumentException.ThrowIfNullOrEmpty(channelName);
        ArgumentNullException.ThrowIfNull(connection);

        _channels.AddOrUpdate(
            channelName,
            [connection],
            (_, existingSockets) =>
            {
                existingSockets.Add(connection);
                return existingSockets;
            });

        return _connectionSubscriptionCounts.AddOrUpdate(
            connection,
            1,
            (_, currentCount) => currentCount + 1
        );
    }

    public int Publish(string channelName, string message)
    {
        if (!_channels.TryGetValue(channelName, out var channel))
            throw new ArgumentException("Channel not found.");
        
        //publish logic
        
        return channel.Count;
    }
}