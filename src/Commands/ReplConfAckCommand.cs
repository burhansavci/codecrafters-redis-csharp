using System.Net.Sockets;
using codecrafters_redis.Resp;
using codecrafters_redis.Server;

namespace codecrafters_redis.Commands;

public class ReplConfAckCommand(RedisServer server) : ICommand
{
    public const string Name = "REPLCONF ACK";

    public Task Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(args.Length, 1);

        if (args[0] is not BulkString replicationOffsetBulkString)
            throw new FormatException("Invalid replication offset format. Expected bulk string.");

        var acknowledgedOffset = int.Parse(replicationOffsetBulkString.Data!);

        server.HandleReplicaAcknowledgment(connection, acknowledgedOffset);

        return Task.CompletedTask;
    }
}