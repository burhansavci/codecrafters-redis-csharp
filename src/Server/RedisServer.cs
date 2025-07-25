using System.Net;
using System.Net.Sockets;
using codecrafters_redis.Server.Replications;
using Microsoft.Extensions.DependencyInjection;

namespace codecrafters_redis.Server;

public class RedisServer
{
    private const string DefaultMasterReplicationId = "8371b4fb1155b71f4a04d3e1bc3e18c4a990aeeb";
    private const int DefaultMasterReplicationOffset = 0;
    public const string DefaultEmptyRdbFileInBase64 = "UkVESVMwMDEx+glyZWRpcy12ZXIFNy4yLjD6CnJlZGlzLWJpdHPAQPoFY3RpbWXCbQi8ZfoIdXNlZC1tZW3CsMQQAPoIYW9mLWJhc2XAAP/wbjv+wP9aog==";

    private readonly IServiceProvider _serviceProvider;
    private readonly ReplicationClient? _replicationClient;
    private readonly RedisConfiguration _config;
   
    public readonly ServerRole Role;
    public bool IsMaster => Role == ServerRole.Master;
    public bool IsSlave => Role == ServerRole.Slave;
    public readonly string? MasterReplicationId;
    public readonly int? MasterReplicationOffset;
    public int Offset { get; set; }

    public RedisServer(RedisConfiguration config, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _config = config;

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
        var listenTask = StartListening();

        if (IsSlave && _replicationClient != null)
        {
            var masterConnection = await _replicationClient.Handshake();
            var connectionHandler = _serviceProvider.GetRequiredService<ConnectionHandler>();
            _ = Task.Run(() => connectionHandler.Handle(masterConnection));
        }

        await listenTask;
    }

    private async Task StartListening()
    {
        using var listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, _config.Port));

        listenSocket.Listen();
        while (true)
        {
            var connection = await listenSocket.AcceptAsync();
            var connectionHandler = _serviceProvider.GetRequiredService<ConnectionHandler>();
            _ = Task.Run(() => connectionHandler.Handle(connection));
            await Task.Delay(50);
        }
    }
}