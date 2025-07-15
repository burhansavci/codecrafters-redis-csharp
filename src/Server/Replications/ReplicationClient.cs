using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Resp;
using Array = codecrafters_redis.Resp.Array;

namespace codecrafters_redis.Server.Replications;

public class ReplicationClient(int port, string masterHost, int masterPort)
{
    private const int ReplicationBufferSize = 1024;
    private Socket _masterConnection = null!;

    public static IPAddress? MasterIpAddress { get; private set; }
    public static int? MasterPort { get; private set; }

    public async Task<Socket> Handshake()
    {
        _masterConnection = new Socket(SocketType.Stream, ProtocolType.Tcp);

        MasterIpAddress = await ResolveMasterHost();
        MasterPort = masterPort;

        await _masterConnection.ConnectAsync(MasterIpAddress, MasterPort.Value);

        await SendPing(_masterConnection);
        await SendReplConfListeningPort(_masterConnection);
        await SendReplConfCapability(_masterConnection);
        await SendPsync(_masterConnection);

        return _masterConnection;
    }

    private async Task<IPAddress> ResolveMasterHost()
    {
        var hostEntry = await Dns.GetHostEntryAsync(masterHost);
        return hostEntry.AddressList.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetwork) ?? hostEntry.AddressList[0];
    }

    private static async Task SendPing(Socket client)
    {
        var pingCommand = new Array(new BulkString("PING"));
        await client.SendAsync(Encoding.UTF8.GetBytes(pingCommand));

        SimpleString pong = SimpleString.Parse(await ReceiveResponse(client));

        if (pong.Data != "PONG")
            throw new Exception("Master server is not responding to PING");
    }

    private async Task SendReplConfListeningPort(Socket client)
    {
        var replConfCommand = new Array(
            new BulkString("REPLCONF"),
            new BulkString("listening-port"),
            new BulkString(port.ToString())
        );
        await client.SendAsync(Encoding.UTF8.GetBytes(replConfCommand));

        SimpleString replConfResponse = SimpleString.Parse(await ReceiveResponse(client));

        if (replConfResponse != SimpleString.Ok)
            throw new Exception("Master server is not responding to REPLCONF listening-port");
    }

    private static async Task SendReplConfCapability(Socket client)
    {
        var replConfCommand = new Array(
            new BulkString("REPLCONF"),
            new BulkString("capa"),
            new BulkString("psync2")
        );
        await client.SendAsync(Encoding.UTF8.GetBytes(replConfCommand));

        SimpleString replConfResponse = SimpleString.Parse(await ReceiveResponse(client));

        if (replConfResponse != SimpleString.Ok)
            throw new Exception("Master server is not responding to REPLCONF capa");
    }

    private static async Task SendPsync(Socket client)
    {
        var psyncCommand = new Array(
            new BulkString("PSYNC"),
            new BulkString("?"),
            new BulkString("-1")
        );
        await client.SendAsync(Encoding.UTF8.GetBytes(psyncCommand));
    }

    private static async Task<string> ReceiveResponse(Socket client)
    {
        var buffer = new byte[ReplicationBufferSize];
        var bytesReceived = await client.ReceiveAsync(buffer, SocketFlags.None);
        return Encoding.UTF8.GetString(buffer[..bytesReceived]);
    }
}