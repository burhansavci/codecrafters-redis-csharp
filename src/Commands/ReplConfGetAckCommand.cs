using System.Net.Sockets;
using System.Text;
using codecrafters_redis.RESP;
using Array = codecrafters_redis.RESP.Array;

namespace codecrafters_redis.Commands;

public class ReplConfGetAckCommand : ICommand
{
    public const string Name = "REPLCONF GETACK";
    private const char Wildcard = '*';

    public async Task Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(args.Length, 1);

        if (args[0] is not BulkString pattern)
            throw new FormatException("Invalid pattern format. Expected bulk string.");

        var array = new Array(
            new BulkString("REPLCONF"),
            new BulkString("ACK"),
            new BulkString("0")
        );

        await connection.SendAsync(Encoding.UTF8.GetBytes(array));
    }
}