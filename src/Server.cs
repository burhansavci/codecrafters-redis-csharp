using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Commands;
using codecrafters_redis.Commands.Echo;
using codecrafters_redis.Commands.Get;
using codecrafters_redis.Commands.Ping;
using codecrafters_redis.Commands.Set;
using codecrafters_redis.RESP;
using Array = codecrafters_redis.RESP.Array;

namespace codecrafters_redis;

public class Server(Mediator mediator)
{
    public static readonly Dictionary<string, Record> Db = new();

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
        var utcNow = DateTime.UtcNow;
        try
        {
            while (connection.Connected)
            {
                var read = await connection.ReceiveAsync(buffer);

                if (read <= 0) break;

                var request = Encoding.UTF8.GetString(buffer[..read]);

                var array = Array.Parse(request);

                BulkString command = (BulkString)array.Items.First();

                var commandName = command.Data!;
                
                //TODO: Refactor here to get rid of if statements
                if (commandName.Equals(PingCommand.Name, StringComparison.OrdinalIgnoreCase))
                {
                    var pingCommandResult = mediator.Send(new PingCommand());
                    await connection.SendAsync(Encoding.UTF8.GetBytes(pingCommandResult.ToString()));
                }

                if (commandName.Equals(EchoCommand.Name, StringComparison.OrdinalIgnoreCase))
                {
                    var echoCommandResult = mediator.Send(new EchoCommand((BulkString)array.Items.Last()));
                    await connection.SendAsync(Encoding.UTF8.GetBytes(echoCommandResult.ToString()));
                }

                if (commandName.Equals(SetCommand.Name, StringComparison.OrdinalIgnoreCase))
                {
                    TimeSpan? expireTime = null;
                    if (array.Items.Length > 4)
                    {
                        var expireParam = ((BulkString)array.Items[3]).Data;

                        if (string.Equals(expireParam, "PX", StringComparison.OrdinalIgnoreCase))
                        {
                            var expireTimeStr = ((BulkString)array.Items[4]).Data;
                            expireTime = TimeSpan.FromMilliseconds(long.Parse(expireTimeStr!));
                        }
                    }

                    var setCommandResult = mediator.Send(new SetCommand((BulkString)array.Items[1], (BulkString)array.Items[2], expireTime));
                    await connection.SendAsync(Encoding.UTF8.GetBytes(setCommandResult.ToString()));
                }

                if (commandName.Equals(GetCommand.Name, StringComparison.OrdinalIgnoreCase))
                {
                    var getCommandResult = mediator.Send(new GetCommand((BulkString)array.Items[1]));
                    await connection.SendAsync(Encoding.UTF8.GetBytes(getCommandResult.ToString()));
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

    private static Task SweepExpiredKeys()
    {
        while (true)
        {
            DateTime utcNow = DateTime.UtcNow;

            foreach (var (key, record) in Db)
            {
                if (record.ExpireAt is not null && record.ExpireAt < utcNow)
                    Db.Remove(key);
            }
        }
    }
}