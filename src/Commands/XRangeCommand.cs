using System.Collections.Immutable;
using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Rdb.Records;
using codecrafters_redis.Resp;
using codecrafters_redis.Server;
using Array = codecrafters_redis.Resp.Array;

namespace codecrafters_redis.Commands;

public class XRangeCommand(Database db) : ICommand
{
    public const string Name = "XRANGE";

    public async Task Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfNotEqual(args.Length, 3);

        var key = ExtractBulkStringData(args[0], "key");
        if (!db.TryGetValue<StreamRecord>(key, out var streamRecord))
            throw new ArgumentException("Invalid key format. Expected stream key.");

        var start = NormalizeStreamId(ExtractBulkStringData(args[1], "start"));
        var end = NormalizeStreamId(ExtractBulkStringData(args[2], "end"), long.MaxValue);

        var entries = streamRecord.GetEntriesInRange(start, end);

        var entryArrays = entries.Select(CreateEntryArray).ToArray<RespObject>();

        await connection.SendResp(new Array(entryArrays));
    }

    private static string ExtractBulkStringData(RespObject arg, string parameterName)
    {
        if (arg is not BulkString bulkString || bulkString.Data == null)
            throw new ArgumentException($"Invalid {parameterName} format. Expected bulk string.");

        return bulkString.Data;
    }

    private static StreamEntryId NormalizeStreamId(string id, long defaultSequence = 0)
    {
        if (id == "-")
            return StreamEntryId.Zero;

        id = id.Contains('-') ? id : $"{id}-{defaultSequence}";

        return StreamEntryId.Create(id);
    }

    private static Array CreateEntryArray(KeyValuePair<StreamEntryId, ImmutableDictionary<string, string>> entry)
    {
        var fieldValues = entry.Value
            .SelectMany(kvp => new[] { new BulkString(kvp.Key), new BulkString(kvp.Value) })
            .ToArray<RespObject>();

        return new Array(new BulkString(entry.Key.ToString()), new Array(fieldValues));
    }
}