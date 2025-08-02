using System.Collections.Concurrent;
using System.Net.Sockets;
using codecrafters_redis.Commands;
using codecrafters_redis.Resp;
using Array = codecrafters_redis.Resp.Array;

namespace codecrafters_redis.Server.Channels;

public class ChannelManager
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Socket, byte>> _channels = new();
    private readonly ConcurrentDictionary<Socket, int> _connectionSubscriptionCounts = new();
    private static readonly BulkString Message = new("message");

    public static List<string> AllowedCommandsInSubscribedMode => [SubscribeCommand.Name, UnsubscribeCommand.Name, "PSUBSCRIBE", "PUNSUBSCRIBE", PingCommand.Name, "QUIT"];

    public bool IsInSubscribedMode(Socket connection) => _connectionSubscriptionCounts.ContainsKey(connection);

    public int Subscribe(string channelName, Socket connection)
    {
        ArgumentException.ThrowIfNullOrEmpty(channelName);
        ArgumentNullException.ThrowIfNull(connection);

        var channel = _channels.GetOrAdd(channelName, _ => new ConcurrentDictionary<Socket, byte>());
        channel.TryAdd(connection, 0);

        return _connectionSubscriptionCounts.AddOrUpdate(connection, 1, (_, count) => count + 1);
    }

    public int Unsubscribe(string channelName, Socket connection)
    {
        ArgumentException.ThrowIfNullOrEmpty(channelName);
        ArgumentNullException.ThrowIfNull(connection);

        if (_channels.TryGetValue(channelName, out var connections))
        {
            connections.TryRemove(connection, out _);

            if (connections.IsEmpty)
                _channels.TryRemove(channelName, out _);
        }

        if (!_connectionSubscriptionCounts.TryGetValue(connection, out var subscriptionCount))
            return 0;

        var newCount = Math.Max(0, subscriptionCount - 1);

        if (newCount == 0)
        {
            _connectionSubscriptionCounts.TryRemove(connection, out _);
            return 0;
        }

        _connectionSubscriptionCounts[connection] = newCount;
        return newCount;
    }

    public async Task<int> Publish(string channelName, string message)
    {
        ArgumentException.ThrowIfNullOrEmpty(channelName);
        ArgumentException.ThrowIfNullOrEmpty(message);

        if (!_channels.TryGetValue(channelName, out var connections) || connections.IsEmpty)
            return 0;

        var response = new Array(
            Message,
            new BulkString(channelName),
            new BulkString(message)
        );

        var publishTasks = connections.Keys.Select(connection => connection.SendResp(response));

        await Task.WhenAll(publishTasks);

        return connections.Count;
    }
}