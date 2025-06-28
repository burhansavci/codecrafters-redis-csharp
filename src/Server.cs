using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis;
using codecrafters_redis.RESP;
using Array = codecrafters_redis.RESP.Array;

var db = new Dictionary<string, Record>();

_ = Task.Run(async () => await SweepExpiredKeys());

using var listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, 6379));

listenSocket.Listen();
while (true)
{
    // Wait for a new connection to arrive
    var connection = await listenSocket.AcceptAsync();

    _ = Task.Run(async () => await HandleConnectionAsync(connection));
}

async Task HandleConnectionAsync(Socket connection)
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

            if (command.Data == "SET")
            {
                var key = ((BulkString)array.Items[1]).Data!;
                var value = ((BulkString)array.Items[2]).Data!;
                TimeSpan? expireTime = null;

                if (array.Items.Length > 4)
                {
                    var expire = ((BulkString)array.Items[3]).Data;

                    if (string.Equals(expire, "PX", StringComparison.OrdinalIgnoreCase))
                    {
                        var expireTimeStr = ((BulkString)array.Items[4]).Data;
                        expireTime = TimeSpan.FromMilliseconds(long.Parse(expireTimeStr!));
                    }
                }
                
                db[key] = new Record(value, utcNow + expireTime);
            }

            string response = command.Data switch
            {
                "PING" => new SimpleString("PONG"),
                "ECHO" => (BulkString)array.Items.Last(),
                "SET" => new SimpleString("OK"),
                "GET" => db.TryGetValue(((BulkString)array.Items.Last()).Data!, out var record) ? new BulkString(record.Value) : new BulkString(null),
                _ => ""
            };

            await connection.SendAsync(Encoding.UTF8.GetBytes(response));
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

Task SweepExpiredKeys()
{
    while (true)
    {
        DateTime utcNow = DateTime.UtcNow;

        foreach (var (key, record) in db)
        {
            if (record.ExpireAt is not null && record.ExpireAt < utcNow)
                db.Remove(key);
        }
    }
}