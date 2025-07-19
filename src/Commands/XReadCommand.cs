using System.Collections.Immutable;
using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Rdb.Records;
using codecrafters_redis.Resp;
using codecrafters_redis.Server;
using Array = codecrafters_redis.Resp.Array;

namespace codecrafters_redis.Commands;

public class XReadCommand(Database db) : ICommand
{
    public const string Name = "XREAD";

    public async Task Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfNotEqual(args.Length, 3);

        ExtractBulkStringData(args[0], "streams");

        var key = ExtractBulkStringData(args[1], "key");
        if (!db.TryGetValue<StreamRecord>(key, out var streamRecord))
            throw new ArgumentException("Invalid key format. Expected stream key.");

        var id = ExtractBulkStringData(args[2], "id");
        var streamEntryId = StreamEntryId.Create(id);
        
        var entries = streamRecord.GetEntriesAfter(streamEntryId);

        var entryArrays = entries.Select(CreateEntryArray).ToArray<RespObject>();

        var array = new Array(new Array(new BulkString(key), new Array(entryArrays)));

        await connection.SendResp(array);
    }

    private static string ExtractBulkStringData(RespObject arg, string parameterName)
    {
        if (arg is not BulkString bulkString || bulkString.Data == null)
            throw new ArgumentException($"Invalid {parameterName} format. Expected bulk string.");

        return bulkString.Data;
    }
    
    private static Array CreateEntryArray(KeyValuePair<StreamEntryId, ImmutableDictionary<string, string>> entry)
    {
        var fieldValues = entry.Value
            .SelectMany(kvp => new[] { new BulkString(kvp.Key), new BulkString(kvp.Value) })
            .ToArray<RespObject>();

        return new Array(new BulkString(entry.Key.ToString()), new Array(fieldValues));
    }
}