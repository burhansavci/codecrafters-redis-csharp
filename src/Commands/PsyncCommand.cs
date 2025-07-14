using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Resp;
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
        ArgumentOutOfRangeException.ThrowIfLessThan(args.Length, 1);

        if (args[0] is not BulkString replicationId)
            throw new FormatException("Invalid replication id format. Expected bulk string.");

        if (replicationId.Data != "?")
            throw new FormatException("Invalid replication id. Expected '?'");

        if (args[1] is not BulkString replicationOffset)
            throw new FormatException("Invalid replication offset format. Expected bulk string.");

        if (replicationOffset.Data != "-1")
            throw new FormatException("Invalid replication offset. Expected '-1'");

        var response = new SimpleString($"FULLRESYNC {redisServer.MasterReplicationId} {redisServer.MasterReplicationOffset}");

        await connection.SendAsync(Encoding.UTF8.GetBytes(response));

        var rdbBinaryData = Convert.FromBase64String(RedisServer.DefaultEmptyRdbFileInBase64);
        var emptyRdbFileResponse = $"${rdbBinaryData.Length}\r\n";
        
        // Combine the RDB file header and binary data into a single byte array
        var headerBytes = Encoding.UTF8.GetBytes(emptyRdbFileResponse);
        var combinedResponse = new byte[headerBytes.Length + rdbBinaryData.Length];
        System.Array.Copy(headerBytes, 0, combinedResponse, 0, headerBytes.Length);
        System.Array.Copy(rdbBinaryData, 0, combinedResponse, headerBytes.Length, rdbBinaryData.Length);

        await connection.SendAsync(combinedResponse);
    }
}