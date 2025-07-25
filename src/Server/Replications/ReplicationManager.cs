using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Resp;
using Array = codecrafters_redis.Resp.Array;

namespace codecrafters_redis.Server.Replications;

public class ReplicationManager(RedisServer redisServer, NotificationManager notificationManager)
{
    private readonly ConcurrentDictionary<Socket, ReplicaState> _replicaStates = new();

    public void AddReplica(Socket replicaSocket)
    {
        var replicaState = new ReplicaState
        {
            Socket = replicaSocket,
            AcknowledgedOffset = 0,
            ExpectedOffset = redisServer.Offset,
            IsAcknowledged = redisServer.Offset == 0 // If no writes yet, consider acknowledged
        };

        _replicaStates.TryAdd(replicaSocket, replicaState);
    }

    public void RemoveReplica(Socket replicaSocket)
    {
        _replicaStates.TryRemove(replicaSocket, out _);
    }

    public void BroadcastToReplicas(string request)
    {
        foreach (var (socket, replicaState) in _replicaStates)
        {
            try
            {
                _ = socket.SendAsync(Encoding.UTF8.GetBytes(request));
                replicaState.ExpectedOffset = redisServer.Offset;
                replicaState.IsAcknowledged = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send to replica: {ex.Message}");
                RemoveReplica(socket);
            }
        }
    }

    public void RequestReplicaAcknowledgments()
    {
        var getAckCommand = new Array(
            new BulkString("REPLCONF"),
            new BulkString("GETACK"),
            new BulkString("*")
        );

        foreach (var (socket, _) in _replicaStates)
        {
            try
            {
                _ = socket.SendAsync(Encoding.UTF8.GetBytes(getAckCommand));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send GETACK to replica: {ex.Message}");
                RemoveReplica(socket);
            }
        }
    }

    public void HandleReplicaAcknowledgment(Socket replicaSocket, int acknowledgedOffset)
    {
        if (_replicaStates.TryGetValue(replicaSocket, out var replicaState))
        {
            replicaState.AcknowledgedOffset = acknowledgedOffset;
            replicaState.IsAcknowledged = acknowledgedOffset >= replicaState.ExpectedOffset;
        }

        notificationManager.NotifyAll("acknowledgment");
    }

    public int GetAcknowledgedReplicaCount() => _replicaStates.Values.Count(state => state.IsAcknowledged || state.AcknowledgedOffset >= state.ExpectedOffset);

    public bool HasPendingWriteOperations() => _replicaStates.Values.Any(state => !state.IsAcknowledged && state.ExpectedOffset > state.AcknowledgedOffset);
}