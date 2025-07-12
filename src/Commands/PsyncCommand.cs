using System.Net.Sockets;
using System.Text;
using codecrafters_redis.RESP;
using codecrafters_redis.Server;

namespace codecrafters_redis.Commands;

public class PsyncCommand(RedisServer redisServer) : ICommand
{
    public const string Name = "PSYNC";

    public async Task Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(args.Length, 2);
        
        if (args[0] is not BulkString replicationId)
            throw new FormatException("Invalid replication id format. Expected bulk string.");
        
        if (args.Length > 1 && args[1] is not BulkString replicationOffset)
            throw new FormatException("Invalid replication offset format. Expected bulk string.");
        
        var response = new SimpleString($"FULLRESYNC {redisServer.MasterReplicationId} {redisServer.MasterReplicationOffset}");

        await connection.SendAsync(Encoding.UTF8.GetBytes(response));
    }
}