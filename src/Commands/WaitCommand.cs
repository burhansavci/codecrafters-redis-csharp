using System.Net.Sockets;
using System.Reactive.Linq;
using codecrafters_redis.Resp;
using codecrafters_redis.Server;
using codecrafters_redis.Server.Replications;

namespace codecrafters_redis.Commands;

public class WaitCommand(NotificationManager notificationManager, ReplicationManager replicationManager) : ICommand
{
    public const string Name = "WAIT";
    private const string AcknowledgementEventKey = "acknowledgment";

    public async Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfNotEqual(args.Length, 2);

        if (!int.TryParse(args[0].GetString("number of replications"), out var targetReplicaCount))
            throw new ArgumentException("Invalid replica count format");

        if (!int.TryParse(args[1].GetString("timeout"), out var timeoutMs) || timeoutMs < 0)
            throw new ArgumentException("Invalid timeout format");

        var currentAcknowledgedCount = replicationManager.GetAcknowledgedReplicaCount();

        if (currentAcknowledgedCount >= targetReplicaCount)
            return new Integer(currentAcknowledgedCount);

        if (replicationManager.HasPendingWriteOperations())
            replicationManager.RequestReplicaAcknowledgments();

        return await WaitForAcknowledgments(targetReplicaCount, timeoutMs);
    }

    private async Task<RespObject> WaitForAcknowledgments(int targetReplicaCount, int timeoutMs)
    {
        var acknowledgmentStream = notificationManager.Subscribe(AcknowledgementEventKey).Where(_ => replicationManager.GetAcknowledgedReplicaCount() >= targetReplicaCount);

        var waitStream = timeoutMs == 0
            ? acknowledgmentStream
            : acknowledgmentStream.Merge(Observable.Timer(TimeSpan.FromMilliseconds(timeoutMs)).Select(_ => true));

        await waitStream.Take(1);

        return new Integer(replicationManager.GetAcknowledgedReplicaCount());
    }
}