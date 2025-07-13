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

                if (Role == SlaveRole)
                    request = SkipFullResyncResponse(request);

                var commands = ParseCommandAndArgs(request);

                foreach (var (commandName, args) in commands)
                {
                    if (Role == MasterRole && IsWriteCommand(commandName))
                        foreach (var client in ConnectedReplications)
                            _ = client.SendAsync(requestInBytes);

                    if (!_commands.TryGetValue(commandName.ToUpperInvariant(), out var command))
                        throw new ArgumentException($"Command not found: {commandName}");

                    await command.Handle(connection, args);
                }
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

    private List<(string CommandName, RespObject[] Args)> ParseCommandAndArgs(string request)
    {
        var commands = new List<(string CommandName, RespObject[] Args)>();
        var requests = request.Split(RespObject.CRLF, StringSplitOptions.RemoveEmptyEntries);

        for (var index = 0; index < requests.Length; index++)
        {
            var requestPart = requests[index];

            if (!requestPart.StartsWith(DataType.Array))
                continue;

            var (array, requestPartsLength) = ParseArrayRequest(requests, index);

            var (commandName, args) = ExtractCommandAndArgs(array);

            commands.Add((commandName, args));

            index += requestPartsLength;
        }

        return commands;
    }

    private static (Array Array, int RequestPartsLength) ParseArrayRequest(string[] requests, int startIndex)
    {
        var requestPart = requests[startIndex];
        var arrayItemsLength = int.Parse(requestPart[1].ToString()) * 2; //$<length>\r\n<data>\r\n

        var request = new StringBuilder();
        request.Append(requestPart);
        request.Append(RespObject.CRLF);

        var endIndex = startIndex + 1 + arrayItemsLength;
        if (endIndex > requests.Length)
            throw new IndexOutOfRangeException($"{endIndex} is out of range. Request length: {requests.Length}");

        var arrayItems = requests[(startIndex + 1)..endIndex];
        request.Append(string.Join(RespObject.CRLF, arrayItems));
        request.Append(RespObject.CRLF);

        var array = Array.Parse(request.ToString());
        return (array, arrayItemsLength);
    }

    private (string CommandName, RespObject[] Args) ExtractCommandAndArgs(Array array)
    {
        if (array.Items.Length == 0)
            throw new ArgumentException("Empty command array");

        var commandMessage = (BulkString)array.Items[0];
        var commandName = commandMessage.Data ?? string.Empty;
        var skipCount = 1;

        // Handle commands that might have sub-commands
        if (array.Items.Length > 1)
        {
            var subCommand = ((BulkString)array.Items[1]).Data;
            var compositeCommandName = $"{commandName} {subCommand}";

            if (_commands.ContainsKey(compositeCommandName))
            {
                commandName = compositeCommandName;
                skipCount = 2;
            }
        }

        var args = array.Items.Skip(skipCount).ToArray();
        return (commandName, args);
    }

    private static string SkipFullResyncResponse(string request)
    {
        if (!request.StartsWith("+FULLRESYNC", StringComparison.OrdinalIgnoreCase))
            return request;

        var commandStartIndex = request.IndexOf(DataType.Array, StringComparison.Ordinal);

        // Skip FullResync response and empty rdb file
        return request[commandStartIndex..];
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