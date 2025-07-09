using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Rdb;
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

        var keyData = key.Data!;

        var value = server.InMemoryDb.TryGetValue(keyData, out var record) && !record.IsExpired ? new BulkString(record.Value) : GetValueFromRdb(keyData);

        await connection.SendAsync(Encoding.UTF8.GetBytes(value));
    }
    
    private BulkString GetValueFromRdb(string key)
    {
        using var reader = new RdbReader(server.DbDirectory, server.DbFileName);
        var db = reader.Read();

        if (!db.TryGetValue(key, out var rdbRecord) || rdbRecord.IsExpired)
            return new BulkString(null);

        return new BulkString(rdbRecord.Value);
    }
}