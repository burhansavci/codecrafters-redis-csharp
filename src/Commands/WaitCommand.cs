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

        if (args[0] is not BulkString numReplicationsArg)
            throw new FormatException("Invalid number of replications format. Expected bulk string.");

        if (args[1] is not BulkString timeoutArg)
            throw new FormatException("Invalid timeout format. Expected bulk string.");

        _targetReplicaCount = int.Parse(numReplicationsArg.Data!);

        var currentAcknowledgedCount = server.GetAcknowledgedReplicaCount();
        if (currentAcknowledgedCount >= _targetReplicaCount)
        {
            await connection.SendResp(new Integer(currentAcknowledgedCount));
            return;
        }

        if (server.HasPendingWriteOperations())
            server.RequestReplicaAcknowledgments();

        // Set up timeout
        var cancellationTokenSource = new CancellationTokenSource(int.Parse(timeoutArg.Data!));
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