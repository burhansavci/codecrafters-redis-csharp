using System.Collections.Concurrent;
using System.Net.Sockets;
using codecrafters_redis.Resp;
using Array = codecrafters_redis.Resp.Array;

namespace codecrafters_redis.Server.Channels;

public class ChannelManager
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<Socket>> _channels = new();
    private readonly ConcurrentDictionary<Socket, int> _connectionSubscriptionCounts = new();
    private static readonly BulkString Message = new("message");

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

    public async Task<int> Publish(string channelName, string message)
    {
        ArgumentException.ThrowIfNullOrEmpty(channelName);
        ArgumentException.ThrowIfNullOrEmpty(message);
        
        if (!_channels.TryGetValue(channelName, out var connections))
            throw new ArgumentException("Channel not found.");
        
        var response = new Array(
            Message,
            new BulkString(channelName),
            new BulkString(message)
        );

        var publishTasks = connections.Select(connection => connection.SendResp(response));

        await Task.WhenAll(publishTasks);

        return connections.Count;
    }
}