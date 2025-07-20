using System.Net.Sockets;
using codecrafters_redis.Resp;
using codecrafters_redis.Server;

namespace codecrafters_redis.Commands;

public class WaitCommand(RedisServer server) : ICommand
{
    public const string Name = "WAIT";

    private readonly TaskCompletionSource<int> _completionSource = new();
    private int _targetReplicaCount;

    public bool IsCompleted => _completionSource?.Task.IsCompleted ?? true;

    public async Task Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfNotEqual(args.Length, 2);

        var numReplications = args[0].GetString(" number of replications");
        var timeout = args[1].GetString("timeout");

        _targetReplicaCount = int.Parse(numReplications);

        var currentAcknowledgedCount = server.GetAcknowledgedReplicaCount();
        if (currentAcknowledgedCount >= _targetReplicaCount)
        {
            await connection.SendResp(new Integer(currentAcknowledgedCount));
            return;
        }

        if (server.HasPendingWriteOperations())
            server.RequestReplicaAcknowledgments();

        // Set up timeout
        var cancellationTokenSource = new CancellationTokenSource(int.Parse(timeout));
        cancellationTokenSource.Token.Register(() =>
        {
            if (!_completionSource.Task.IsCompleted)
            {
                // On timeout, complete with current acknowledged count
                var currentCount = server.GetAcknowledgedReplicaCount();
                _completionSource.TrySetResult(currentCount);
            }
        });

        // Register this command for notifications and wait for acknowledgments
        server.RegisterWaitCommand(this);

        try
        {
            var acknowledgedCount = await _completionSource.Task;
            await connection.SendResp(new Integer(acknowledgedCount));
        }
        finally
        {
            server.UnregisterWaitCommand(this);
            cancellationTokenSource.Dispose();
        }
    }

    public void NotifyAcknowledgmentUpdate(int currentAcknowledgedCount)
    {
        if (currentAcknowledgedCount >= _targetReplicaCount && !IsCompleted)
            _completionSource.TrySetResult(currentAcknowledgedCount);
    }
}