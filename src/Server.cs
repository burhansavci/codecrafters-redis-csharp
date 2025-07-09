using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Commands;
using codecrafters_redis.RESP;
using Array = codecrafters_redis.RESP.Array;

namespace codecrafters_redis;

public class Server(Dictionary<string, string> config)
{
    public Dictionary<string, string> Config { get; } = config;
    public string DbFileName => Config.GetValueOrDefault("dbfilename", string.Empty);
    public string DbDirectory => Config.GetValueOrDefault("dir", string.Empty);

    public readonly Dictionary<string, Record> Db = new();
    private readonly Dictionary<string, ICommand> _commands = [];

    public void RegisterCommand(string commandName, ICommand command)
    {
        _commands[commandName] = command;
    }

    public async Task StartAsync(int port = 6379)
    {
        _ = Task.Run(async () => await SweepExpiredKeys());

        using var listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, port));

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

    private Task SweepExpiredKeys()
    {
        while (true)
        {
            DateTime utcNow = DateTime.UtcNow;

            foreach (var (key, record) in Db)
                if (record.ExpireAt is not null && record.ExpireAt < utcNow)
                    Db.Remove(key);
        }
    }
}