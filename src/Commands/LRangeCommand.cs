using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Rdb.Records;
using codecrafters_redis.Resp;
using Array = codecrafters_redis.Resp.Array;

namespace codecrafters_redis.Commands;

public class LRangeCommand(Database db) : ICommand
{
    public const string Name = "LRANGE";

    public Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfNotEqual(args.Length, 3);

        var listKey = args[0].GetString("listKey");

        if (!int.TryParse(args[1].GetString("startIndex"), out var startIndex))
            throw new ArgumentException("Invalid start index. Expected integer.");

        if (!int.TryParse(args[2].GetString("endIndex"), out var endIndex))
            throw new ArgumentException("Invalid end index. Expected integer.");

        if (!db.TryGetValue<ListRecord>(listKey, out var listRecord) || startIndex >= listRecord.Count)
            return Task.FromResult<RespObject>(new Array());
        
        if (endIndex >= listRecord.Count)
            endIndex = listRecord.Count - 1;
        
        if (startIndex > endIndex)
            return Task.FromResult<RespObject>(new Array());

        var entries = listRecord.GetEntriesInRange(startIndex, endIndex).Select(x => new BulkString(x)).ToArray<RespObject>();

        return Task.FromResult<RespObject>(new Array(entries));
    }
}