using System.Net;
using System.Net.Sockets;
using System.Text;

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

            var response = "+PONG\r\n";

            await connection.SendAsync(Encoding.UTF8.GetBytes(response));
        }
    }
    finally
    {
        connection.Dispose();
    }
}