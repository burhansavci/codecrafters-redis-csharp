using System.Net.Sockets;
using codecrafters_redis.Resp;
using codecrafters_redis.Server.Replications;

namespace codecrafters_redis.Commands;

public class ReplConfCommand(ReplicationManager replicationManager) : ICommand
{
    public const string Name = "REPLCONF";

    public Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfNotEqual(args.Length, 2);

        replicationManager.AddReplica(connection);

        return Task.FromResult<RespObject>(SimpleString.Ok);
    }
}