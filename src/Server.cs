using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Commands;
using codecrafters_redis.RESP;
using Array = codecrafters_redis.RESP.Array;

namespace codecrafters_redis;

public class Server
{
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

                var array = Array.Parse(request);

                BulkString commandMessage = (BulkString)array.Items.First();

                var commandName = commandMessage.Data!;

                if (!_commands.TryGetValue(commandName, out var command))
                    throw new ArgumentException($"Command not found: {commandName}");

                var args = array.Items.Skip(1).Select(x => x).ToArray();

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