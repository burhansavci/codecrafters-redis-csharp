using System.Net.Sockets;
using System.Text;
using codecrafters_redis.RESP;
using codecrafters_redis.Server;

namespace codecrafters_redis.Commands;

public class InfoCommand(RedisServer redisServer) : ICommand
{
    public const string Name = "INFO";
    private const string AllSection = "all";
    private const string ReplicationSection = "replication";


    public async Task Handle(Socket connection, RespObject[] args)
    {
        var section = GetRequestedSection(args);
        var infoResponse = GenerateInfoResponse(section);

        await connection.SendAsync(Encoding.UTF8.GetBytes(infoResponse));
    }

    private static string GetRequestedSection(RespObject[] args)
    {
        if (args is null or [])
            return AllSection;

        ArgumentOutOfRangeException.ThrowIfGreaterThan(args.Length, 1);

        if (args[0] is not BulkString section)
            throw new FormatException("Invalid section format. Expected bulk string.");

        return section.Data ?? AllSection;
    }

    private BulkString GenerateInfoResponse(string section)
    {
        var infoBuilder = new StringBuilder();

        switch (section.ToLowerInvariant())
        {
            case ReplicationSection:
                AppendReplicationInfo(infoBuilder);
                break;
            case AllSection:
                AppendReplicationInfo(infoBuilder);
                break;
            default:
                // For unknown sections, return replication info (Redis behavior)
                AppendReplicationInfo(infoBuilder);
                break;
        }

        return new BulkString(infoBuilder.ToString().TrimEnd());
    }

    private void AppendReplicationInfo(StringBuilder builder)
    {
        builder.AppendLine("# Replication");
        builder.AppendLine($"role:{redisServer.Role}");
        builder.AppendLine($"master_replid:{redisServer.MasterReplicationId}");
        builder.AppendLine($"master_repl_offset:{redisServer.MasterReplicationOffset}");
    }
}