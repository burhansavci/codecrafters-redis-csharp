using System.Net.Sockets;
using codecrafters_redis.Resps;
using codecrafters_redis.Server;
using Array = codecrafters_redis.Resps.Array;

namespace codecrafters_redis.Commands;

public class ConfigGetCommand(RedisServer redisServer) : ICommand
{
    public const string Name = "CONFIG GET";

    public async Task Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(args.Length, 1);

        if (args[0] is not BulkString commandName)
            throw new FormatException("Invalid name format. Expected bulk string.");

        if (!redisServer.Config.TryGetValue(commandName.Data!, out var value))
            throw new InvalidOperationException($"Invalid command name: {commandName.Data}");

        var array = new Array(commandName, new BulkString(value));

        await connection.SendResp(array);
    }
}