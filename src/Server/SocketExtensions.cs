using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Resp;
using codecrafters_redis.Server.Replications;

namespace codecrafters_redis.Server;

public static class SocketExtensions
{
    public static async Task<int> SendResp<T>(this Socket connection, T resp) where T : RespObject
    {
        if (connection.IsMaster())
            return -1;

        return await connection.SendAsync(Encoding.UTF8.GetBytes(resp.ToString()));
    }

    public static bool IsMaster(this Socket connection)
    {
        if (ReplicationClient.MasterIpAddress == null || ReplicationClient.MasterPort == null)
            return false;

        var ipEndPoint = (IPEndPoint)connection.RemoteEndPoint!;
        var ipAddress = ipEndPoint.Address;
        var port = ipEndPoint.Port;

        var normalizedConnectionIp = ipAddress.IsIPv4MappedToIPv6 ? ipAddress.MapToIPv4() : ipAddress;
        var normalizedMasterIp = ReplicationClient.MasterIpAddress.IsIPv4MappedToIPv6 ? ReplicationClient.MasterIpAddress.MapToIPv4() : ReplicationClient.MasterIpAddress;

        return Equals(normalizedConnectionIp, normalizedMasterIp) && port == ReplicationClient.MasterPort;
    }
}