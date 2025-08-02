using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using codecrafters_redis.Server.Replications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace codecrafters_redis.Server;

public sealed class RedisServer : IDisposable
{
    private const string DefaultMasterReplicationId = "8371b4fb1155b71f4a04d3e1bc3e18c4a990aeeb";
    private const int DefaultMasterReplicationOffset = 0;
    public const string DefaultEmptyRdbFileInBase64 = "UkVESVMwMDEx+glyZWRpcy12ZXIFNy4yLjD6CnJlZGlzLWJpdHPAQPoFY3RpbWXCbQi8ZfoIdXNlZC1tZW3CsMQQAPoIYW9mLWJhc2XAAP/wbjv+wP9aog==";

    private readonly IServiceProvider _serviceProvider;
    private readonly ReplicationClient? _replicationClient;
    private readonly RedisConfiguration _config;
    private readonly ILogger<RedisServer> _logger;
    private readonly Channel<ConnectionTask> _connectionHandlerChannel;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public ServerRole Role { get; }
    public bool IsMaster => Role == ServerRole.Master;
    public bool IsSlave => Role == ServerRole.Slave;
    public string? MasterReplicationId { get; }
    public int? MasterReplicationOffset { get; }
    public int Offset { get; set; }

    public RedisServer(RedisConfiguration config, IServiceProvider serviceProvider, ILogger<RedisServer> logger)
    {
        _serviceProvider = serviceProvider;
        _config = config;
        _logger = logger;
        _connectionHandlerChannel = Channel.CreateBounded<ConnectionTask>(
            new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true
            });
        _cancellationTokenSource = new CancellationTokenSource();

        if (_config.IsReplica)
        {
            Role = ServerRole.Slave;
            _replicationClient = new ReplicationClient(_config.Port, _config.MasterHost!, _config.MasterPort!.Value);
        }
        else
        {
            Role = ServerRole.Master;
            MasterReplicationId = DefaultMasterReplicationId;
            MasterReplicationOffset = DefaultMasterReplicationOffset;
        }
    }

    public async Task Start()
    {
        var cancellationToken = _cancellationTokenSource.Token;

        var tasks = new List<Task>
        {
            StartListening(cancellationToken),
            HandleConnections(cancellationToken)
        };

        if (IsSlave && _replicationClient != null)
            tasks.Add(InitializeSlaveConnection());

        await Task.WhenAll(tasks);
    }

    private async Task InitializeSlaveConnection()
    {
        try
        {
            var masterConnection = await _replicationClient!.Handshake();
            var connectionHandler = _serviceProvider.GetRequiredService<ConnectionHandler>();
            var connectionTask = new ConnectionTask(masterConnection, connectionHandler.Handle(masterConnection));

            await _connectionHandlerChannel.Writer.WriteAsync(connectionTask);
            _logger.LogInformation("Slave connection to master established");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish slave connection to master");
            throw;
        }
    }

    private async Task StartListening(CancellationToken cancellationToken)
    {
        using var listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, _config.Port));
        listenSocket.Listen();

        _logger.LogInformation("Redis server listening on port {Port}", _config.Port);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var connection = await listenSocket.AcceptAsync(cancellationToken);
                var connectionHandler = _serviceProvider.GetRequiredService<ConnectionHandler>();
                var connectionTask = new ConnectionTask(connection, connectionHandler.Handle(connection));
                
                _logger.LogInformation("Accepted connection from {RemoteEndPoint}", connection.RemoteEndPoint);

                await _connectionHandlerChannel.Writer.WriteAsync(connectionTask, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Server listening stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in server listening loop");
            throw;
        }
    }

    private async Task HandleConnections(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var connectionTask in _connectionHandlerChannel.Reader.ReadAllAsync(cancellationToken))
            {
                if (connectionTask.Task.IsCompleted)
                {
                    var shouldConnectionContinue = await connectionTask.Task;
                    if (shouldConnectionContinue)
                    {
                        var connectionHandler = _serviceProvider.GetRequiredService<ConnectionHandler>();
                        var newTask = connectionTask with { Task = connectionHandler.Handle(connectionTask.Connection) };
                        await _connectionHandlerChannel.Writer.WriteAsync(newTask, cancellationToken);
                    }
                }
                else
                {
                    await _connectionHandlerChannel.Writer.WriteAsync(connectionTask, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Connection handling stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in connection handling loop");
            throw;
        }
    }

    private void Stop()
    {
        _cancellationTokenSource.Cancel();
        _connectionHandlerChannel.Writer.Complete();
    }

    public void Dispose()
    {
        Stop();
        _cancellationTokenSource.Dispose();
    }

    private record ConnectionTask(Socket Connection, Task<bool> Task);
}