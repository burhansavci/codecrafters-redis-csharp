using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis.RESP;
using Array = codecrafters_redis.RESP.Array;

var db = new Dictionary<string, string>();

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
    try
    {
        while (connection.Connected)
        {
            var read = await connection.ReceiveAsync(buffer);

            if (read <= 0) break;

            var request = Encoding.UTF8.GetString(buffer[..read]);

            var array = Array.Parse(request);

            BulkString command = (BulkString)array.Items.First();
            
            if(command.Data == "SET")
                db[(BulkString)array.Items[1]] = ((BulkString)array.Items[2]).Data!;
            
            string response = command.Data switch
            {
                "PING" => new SimpleString("PONG"),
                "ECHO" => (BulkString)array.Items.Last(),
                "SET" => new SimpleString("OK"),
                "GET" => db.TryGetValue((BulkString)array.Items.Last(), out var value) ? new BulkString(value) : new BulkString(null),
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