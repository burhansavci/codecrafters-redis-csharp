using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Commands;
using codecrafters_redis.Rdb;
using codecrafters_redis.RESP;
using Array = codecrafters_redis.RESP.Array;

namespace codecrafters_redis;

public class Server
{
    public Server(Dictionary<string, string> config)
    {
        Config = config;
        DbFileName = Config.GetValueOrDefault("dbfilename", string.Empty);
        DbDirectory = Config.GetValueOrDefault("dir", string.Empty);
        _port = int.Parse(Config.GetValueOrDefault("port", "6379"));

        if (Config.TryGetValue("replicaof", out var replicaOf))
        {
            Role = "slave";
            var parts = replicaOf.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            MasterHost = parts[0];
            MasterPort = parts[1];
        }
        else
        {
            Role = "master";
            MasterReplicationId = "8371b4fb1155b71f4a04d3e1bc3e18c4a990aeeb";
            MasterReplicationOffset = 0;
        }
    }

    public Dictionary<string, string> Config { get; }
    public readonly string DbFileName;
    public readonly string DbDirectory;
    public readonly string Role;
    public readonly string? MasterReplicationId;
    public readonly int? MasterReplicationOffset;
    public readonly string? MasterHost;
    public readonly string? MasterPort;

    private readonly int _port;

    public readonly Dictionary<string, Record> InMemoryDb = new();

    private readonly Dictionary<string, ICommand> _commands = [];

    public void RegisterCommand(string commandName, ICommand command)
    {
        _commands[commandName] = command;
    }

    public async Task StartAsync()
    {
        if (Role == "slave")
        {
            using var client = new Socket(SocketType.Stream, ProtocolType.Tcp);
            
            var hostEntry = await Dns.GetHostEntryAsync(MasterHost!);
            var ipAddress = hostEntry.AddressList.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetwork) ?? hostEntry.AddressList[0];
            
            await client.ConnectAsync(ipAddress, int.Parse(MasterPort!));
            
            var pingCommand = new Array(new BulkString("PING"));
            await client.SendAsync(Encoding.UTF8.GetBytes(pingCommand));
        }

        using var listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, _port));

        listenSocket.Listen();
        while (true)
        {
            // Wait for a new connection to arrive
            var connection = await listenSocket.AcceptAsync();

            _ = Task.Run(async () => await HandleConnectionAsync(connection));
        }
    }

    private async Task HandleConnectionAsync(Socket connection)
    {
        var buffer = new byte[4 * 1024];
        try
        {
            while (connection.Connected)
            {
                var read = await connection.ReceiveAsync(buffer);

                if (read <= 0) break;

                var request = Encoding.UTF8.GetString(buffer[..read]);

                var (commandName, args) = ParseCommandAndArgs(request);

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
}