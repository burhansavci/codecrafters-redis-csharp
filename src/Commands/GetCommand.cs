using System.Net.Sockets;
using System.Text;
using codecrafters_redis.RESP;

namespace codecrafters_redis.Commands;

public class GetCommand(Server server) : ICommand
{
    public const string Name = "GET";

    public async Task Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(args.Length, 1);

        if (args[0] is not BulkString key)
            throw new FormatException("Invalid key format. Expected bulk string.");

        var value = server.Db.TryGetValue(key.Data!, out var record) ? new BulkString(record.Value) : new BulkString(null);

        await connection.SendAsync(Encoding.UTF8.GetBytes(value));
    }
}