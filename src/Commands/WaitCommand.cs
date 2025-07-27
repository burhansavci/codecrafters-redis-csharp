using System.Net.Sockets;
using codecrafters_redis.Resp;
using codecrafters_redis.Server.Replications;

namespace codecrafters_redis.Commands;

public class WaitCommand(ReplicationManager replicationManager) : ICommand
{
    public const string Name = "WAIT";

    public async Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfNotEqual(args.Length, 2);

        if (!int.TryParse(args[0].GetString("number of replications"), out var targetReplicaCount))
            throw new ArgumentException("Invalid replica count format");

        if (!int.TryParse(args[1].GetString("timeout"), out var timeoutMs) || timeoutMs < 0)
            throw new ArgumentException("Invalid timeout format");

        var timeout = timeoutMs == 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(timeoutMs);

        var acknowledgedCount = await replicationManager.WaitForAcknowledgments(targetReplicaCount, timeout);

        return new Integer(acknowledgedCount);
    }
}