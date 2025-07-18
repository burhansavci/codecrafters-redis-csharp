using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Commands;
using codecrafters_redis.Resp;
using codecrafters_redis.Resp.Parsing;
using codecrafters_redis.Server.Replications;
using Microsoft.Extensions.DependencyInjection;
using Array = codecrafters_redis.Resp.Array;

namespace codecrafters_redis.Server;

public class RedisServer
{
    private const string DefaultMasterReplicationId = "8371b4fb1155b71f4a04d3e1bc3e18c4a990aeeb";
    private const int DefaultMasterReplicationOffset = 0;
    public const string DefaultEmptyRdbFileInBase64 = "UkVESVMwMDEx+glyZWRpcy12ZXIFNy4yLjD6CnJlZGlzLWJpdHPAQPoFY3RpbWXCbQi8ZfoIdXNlZC1tZW3CsMQQAPoIYW9mLWJhc2XAAP/wbjv+wP9aog==";
    private const string MasterRole = "master";
    private const string SlaveRole = "slave";
    private const int BufferSize = 4 * 1024;

    private readonly IServiceProvider _serviceProvider;
    private readonly ReplicationClient? _replicationClient;
    private readonly RespCommandParser _commandParser;

    private readonly ConcurrentDictionary<Socket, ReplicaState> _replicaStates = [];
    private readonly ConcurrentDictionary<int, WaitCommand> _activeWaitCommands = new();
    private readonly Lock _waitCommandsLock = new();

    public RedisConfiguration Config { get; }
    public readonly string Role;
    public readonly string? MasterReplicationId;
    public readonly int? MasterReplicationOffset;
    public int Offset { get; private set; }

    public RedisServer(RedisConfiguration config, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _commandParser = new RespCommandParser(serviceProvider);
        Config = config;

        if (Config.IsReplica)
        {
            Role = SlaveRole;
            _replicationClient = new ReplicationClient(Config.Port, Config.MasterHost!, Config.MasterPort!.Value);
        }
        else
        {
            Role = MasterRole;
            MasterReplicationId = DefaultMasterReplicationId;
            MasterReplicationOffset = DefaultMasterReplicationOffset;
        }
    }

    public void RequestReplicaAcknowledgments()
    {
        if (Role != MasterRole) return;

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

    public bool HasPendingWriteOperations() => _replicaStates.Values.Any(state => !state.IsAcknowledged && state.ExpectedOffset > state.AcknowledgedOffset);

    public int GetAcknowledgedReplicaCount() => _replicaStates.Values.Count(state => state.IsAcknowledged || state.AcknowledgedOffset >= state.ExpectedOffset);

    public void RegisterWaitCommand(WaitCommand waitCommand) => _activeWaitCommands.TryAdd(waitCommand.GetHashCode(), waitCommand);

    public void UnregisterWaitCommand(WaitCommand waitCommand) => _activeWaitCommands.TryRemove(waitCommand.GetHashCode(), out _);

    /// <summary>
    /// Handles replica acknowledgment and notifies waiting WAIT commands
    /// </summary>
    public void HandleReplicaAcknowledgment(Socket replicaSocket, int acknowledgedOffset)
    {
        if (_replicaStates.TryGetValue(replicaSocket, out var replicaState))
        {
            replicaState.AcknowledgedOffset = acknowledgedOffset;
            replicaState.IsAcknowledged = acknowledgedOffset >= replicaState.ExpectedOffset;
        }

        // Notifies all active WAIT commands about acknowledgment updates
        var currentAcknowledgedCount = GetAcknowledgedReplicaCount();

        lock (_waitCommandsLock)
        {
            foreach (var waitCommand in _activeWaitCommands.Values.Where(w => !w.IsCompleted))
                waitCommand.NotifyAcknowledgmentUpdate(currentAcknowledgedCount);
        }
    }

    public void AddReplica(Socket replicaSocket)
    {
        var replicaState = new ReplicaState
        {
            Socket = replicaSocket,
            AcknowledgedOffset = 0,
            ExpectedOffset = Offset,
            IsAcknowledged = Offset == 0 // If no writes yet, consider acknowledged
        };

        _replicaStates.TryAdd(replicaSocket, replicaState);
    }

    public void RemoveReplica(Socket replicaSocket)
    {
        _replicaStates.TryRemove(replicaSocket, out _);
    }

    public async Task Start()
    {
        var listenTask = StartListening();

        if (Role == SlaveRole && _replicationClient != null)
        {
            var masterConnection = await _replicationClient.Handshake();
            _ = Task.Run(async () => await HandleConnection(masterConnection));
        }

        await listenTask;
    }

    private async Task StartListening()
    {
        using var listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, Config.Port));

        listenSocket.Listen();
        while (true)
        {
            // Wait for a new connection to arrive
            var connection = await listenSocket.AcceptAsync();
            _ = Task.Run(async () => await HandleConnection(connection));
        }
    }

    private async Task HandleConnection(Socket connection)
    {
        var buffer = new byte[BufferSize];
        try
        {
            while (connection.Connected)
            {
                var read = await connection.ReceiveAsync(buffer);
                if (read <= 0) break;

                var request = Encoding.UTF8.GetString(buffer[..read]);

                if (Role == SlaveRole && connection.IsMaster())
                {
                    request = SkipFullResyncResponse(request);
                    if (string.IsNullOrWhiteSpace(request))
                        continue;
                }

                var commands = _commandParser.GetRespCommands(request);

                using var scope = _serviceProvider.CreateScope();
                foreach (var (commandName, args, requestArray) in commands)
                {
                    var singleRequest = requestArray.ToString();

                    if (Role == MasterRole && IsWriteCommand(commandName))
                        BroadcastToReplications(singleRequest);

                    var command = scope.ServiceProvider.GetRequiredKeyedService<ICommand>(commandName.ToUpperInvariant());

                    await command.Handle(connection, args);

                    if (Role == SlaveRole && connection.IsMaster())
                        Offset += singleRequest.Length;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally
        {
            if (Role == MasterRole)
                RemoveReplica(connection);

            connection.Dispose();
        }
    }

    private void BroadcastToReplications(string request)
    {
        if (Role != MasterRole) return;

        // Update offset before broadcasting
        Offset += request.Length;

        foreach (var (socket, replicaState) in _replicaStates)
        {
            try
            {
                _ = socket.SendAsync(Encoding.UTF8.GetBytes(request));

                replicaState.ExpectedOffset = Offset;
                replicaState.IsAcknowledged = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send to replica: {ex.Message}");
                RemoveReplica(socket);
            }
        }
    }

    private static string SkipFullResyncResponse(string response)
    {
        if (!response.StartsWith("+FULLRESYNC", StringComparison.OrdinalIgnoreCase))
            return response;

        var commandStartIndex = response.IndexOf(DataType.Array, StringComparison.Ordinal);

        // Skip FullResync response and empty rdb file
        return commandStartIndex == -1 ? string.Empty : response[commandStartIndex..];
    }

    private static bool IsWriteCommand(string commandName) => commandName == SetCommand.Name;
}