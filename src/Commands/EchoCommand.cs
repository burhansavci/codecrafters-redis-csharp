using System.Net.Sockets;
using codecrafters_redis.Resp;

namespace codecrafters_redis.Commands;

public class EchoCommand : ICommand
{
    public const string Name = "ECHO";

    public Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(args.Length, 1);

        if (args[0] is not BulkString message)
            throw new FormatException("Invalid message format. Expected bulk string.");

        return Task.FromResult<RespObject>(message);
    }
}