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
        Role = "master";
    }

    public Dictionary<string, string> Config { get; }
    public readonly string DbFileName;
    public readonly string DbDirectory;
    public readonly string Role;
    private readonly int _port;

    public readonly Dictionary<string, Record> InMemoryDb = new();

    private readonly Dictionary<string, ICommand> _commands = [];

    public void RegisterCommand(string commandName, ICommand command)
    {
        _commands[commandName] = command;
    }

    public async Task StartAsync()
    {
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