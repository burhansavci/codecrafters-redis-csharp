using System.Net;
using System.Net.Sockets;
using System.Text;

using var listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
listenSocket.Bind(new IPEndPoint(IPAddress.Any, 6379));

listenSocket.Listen();
while (true)
{
    // Wait for a new connection to arrive
    var connection = await listenSocket.AcceptAsync();

    var response = "+PONG\r\n";

    await connection.SendAsync(Encoding.UTF8.GetBytes(response));
}