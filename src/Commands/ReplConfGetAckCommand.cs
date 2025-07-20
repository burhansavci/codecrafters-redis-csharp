using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Resp;
using codecrafters_redis.Server;
using Array = codecrafters_redis.Resp.Array;

namespace codecrafters_redis.Commands;

public class ReplConfGetAckCommand(RedisServer server) : ICommand
{
    public const string Name = "REPLCONF GETACK";

    public async Task Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(args.Length, 1);

        _ = args[0].GetString("pattern");

        var array = new Array(
            new BulkString("REPLCONF"),
            new BulkString("ACK"),
            new BulkString(server.Offset.ToString())
        );

        await connection.SendAsync(Encoding.UTF8.GetBytes(array));
    }
}