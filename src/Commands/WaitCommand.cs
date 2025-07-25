using System.Net.Sockets;
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

        var tcs = new TaskCompletionSource<bool>();
        notificationManager.Subscribe(AcknowledgementEventKey, tcs);

        var timeout = timeoutMs == 0 ? Timeout.Infinite : timeoutMs;

        using var timeoutCts = new CancellationTokenSource(timeout);
        var notificationTask = tcs.Task;

        while (!timeoutCts.IsCancellationRequested)
        {
            if (replicationManager.GetAcknowledgedReplicaCount() >= targetReplicaCount)
                break;

            try
            {
                await notificationTask.WaitAsync(timeoutCts.Token);
                
                await Task.Delay(100, timeoutCts.Token);

                // If notified, create a new task for the next notification.
                tcs = new TaskCompletionSource<bool>();
                notificationManager.Subscribe(AcknowledgementEventKey, tcs);
                notificationTask = tcs.Task;
            }
            catch (TaskCanceledException)
            {
                // Expected on timeout. Loop will terminate.
            }
        }

        return new Integer(replicationManager.GetAcknowledgedReplicaCount());
    }
}