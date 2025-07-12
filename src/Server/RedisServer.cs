using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Commands;
using codecrafters_redis.Rdb;
using codecrafters_redis.RESP;
using Array = codecrafters_redis.RESP.Array;

namespace codecrafters_redis.Server;

public class RedisServer
{
    private const string DefaultPort = "6379";
    private const string DefaultMasterReplicationId = "8371b4fb1155b71f4a04d3e1bc3e18c4a990aeeb";
    private const int DefaultMasterReplicationOffset = 0;
    public const string DefaultEmptyRdbFileInBase64 = "UkVESVMwMDEx+glyZWRpcy12ZXIFNy4yLjD6CnJlZGlzLWJpdHPAQPoFY3RpbWXCbQi8ZfoIdXNlZC1tZW3CsMQQAPoIYW9mLWJhc2XAAP/wbjv+wP9aog==";
    private const string MasterRole = "master";
    private const string SlaveRole = "slave";
    private const int BufferSize = 4 * 1024;

    private readonly int _port;
    private readonly Dictionary<string, ICommand> _commands = [];
    private readonly ReplicationClient? _replicationClient;

    public Dictionary<string, string> Config { get; }
    public readonly string DbFileName;
    public readonly string DbDirectory;
    public readonly string Role;
    public readonly string? MasterReplicationId;
    public readonly int? MasterReplicationOffset;
    public readonly HashSet<Socket> ConnectedReplications = [];

    public readonly Dictionary<string, Record> InMemoryDb = new();

    public RedisServer(Dictionary<string, string> config)
    {
        Config = config;
        DbFileName = Config.GetValueOrDefault("dbfilename", string.Empty);
        DbDirectory = Config.GetValueOrDefault("dir", string.Empty);
        _port = int.Parse(Config.GetValueOrDefault("port", DefaultPort));

        if (Config.TryGetValue("replicaof", out var replicaOf))
        {
            var (masterHost, masterPort) = ParseReplicaOfConfig(replicaOf);
            Role = SlaveRole;
            _replicationClient = new ReplicationClient(_port, masterHost, masterPort);
        }
        else
        {
            Role = MasterRole;
            MasterReplicationId = DefaultMasterReplicationId;
            MasterReplicationOffset = DefaultMasterReplicationOffset;
        }
    }

    public void RegisterCommand(string commandName, ICommand command)
    {
        _commands[commandName] = command;
    }

    public async Task Start()
    {
        if (Role == SlaveRole && _replicationClient != null)
            await _replicationClient.Handshake();

        await StartListening();
    }

    private async Task StartListening()
    {
        using var listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, _port));

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

                var requestInBytes = buffer[..read];

                var request = Encoding.UTF8.GetString(requestInBytes);

                var (commandName, args) = ParseCommandAndArgs(request);

                if (IsWriteCommand(commandName))
                    foreach (var client in ConnectedReplications)
                        _ = client.SendAsync(requestInBytes);

                if (!_commands.TryGetValue(commandName.ToUpperInvariant(), out var command))
                    throw new ArgumentException($"Command not found: {commandName}");

                await command.Handle(connection, args);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally
        {
            connection.Dispose();
        }
    }

    private static (string CommandName, RespObject[] Args) ParseCommandAndArgs(string request)
    {
        var array = Array.Parse(request);
        var commandMessage = (BulkString)array.Items.First();
        var commandName = commandMessage.Data!;
        var skip = 1;

        const string config = "CONFIG";
        if (commandName == config)
        {
            commandName += $" {((BulkString)array.Items[1]).Data}";
            skip = 2;
        }

        var args = array.Items.Skip(skip).ToArray();
        return (commandName, args);
    }

    private static (string Host, string Port) ParseReplicaOfConfig(string replicaOf)
    {
        var parts = replicaOf.Split(" ", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            throw new ArgumentException("Invalid replicaof configuration format");

        return (parts[0], parts[1]);
    }

    private static bool IsWriteCommand(string commandName) => commandName == SetCommand.Name;
}