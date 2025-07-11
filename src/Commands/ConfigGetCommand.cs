using System.Net.Sockets;
using System.Text;
using codecrafters_redis.RESP;
using Array = codecrafters_redis.RESP.Array;

namespace codecrafters_redis.Commands;

public class ConfigGetCommand(Server.Server server) : ICommand
{
    public const string Name = "CONFIG GET";

    public async Task Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(args.Length, 1);

        if (args[0] is not BulkString commandName)
            throw new FormatException("Invalid name format. Expected bulk string.");

        if (!server.Config.TryGetValue(commandName.Data!, out var value))
            throw new InvalidOperationException($"Invalid command name: {commandName.Data}");

        var array = new Array(commandName, new BulkString(value));

        await connection.SendAsync(Encoding.UTF8.GetBytes(array));
    }
}