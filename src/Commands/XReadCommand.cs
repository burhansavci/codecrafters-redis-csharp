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

    private const int MinRequiredArgs = 3;

    public async Task Handle(Socket connection, RespObject[] args)
    {
        var pairs = ValidateAndParseArguments(args);

        var streamArrays = pairs.Select(CreateStreamArray).ToArray<RespObject>();

        await connection.SendResp(new Array(streamArrays));
    }

    private List<KeyValuePair<StreamRecord, StreamEntryId>> ValidateAndParseArguments(RespObject[] args)
    {
        if (args == null || args.Length < MinRequiredArgs)
            throw new ArgumentException($"XRANGE requires at least {MinRequiredArgs} arguments");

        if ((args.Length - 1) % 2 != 0)
            throw new ArgumentException("XRANGE requires an even number of stream key-id pairs");

        ExtractBulkStringData(args[0], "streams");

        return ParseStreamKeyIdPairs(args.Skip(1).ToArray());
    }

    private List<KeyValuePair<StreamRecord, StreamEntryId>> ParseStreamKeyIdPairs(RespObject[] streamKeyIdPairArgs)
    {
        var pairs = new List<KeyValuePair<StreamRecord, StreamEntryId>>();
        var streamKeysLength = streamKeyIdPairArgs.Length / 2;

        for (int i = 0; i < streamKeysLength; i++)
        {
            var streamKey = ExtractBulkStringData(streamKeyIdPairArgs[i], $"stream key at position {i + 1}");
            if (!db.TryGetValue<StreamRecord>(streamKey, out var streamRecord))
                throw new ArgumentException($"Invalid stream key format. Expected stream key.");

            var streamEntryIdString = ExtractBulkStringData(streamKeyIdPairArgs[i + streamKeysLength], $"stream entry ID at position {i + streamKeysLength + 2}");
            var streamEntryId = StreamEntryId.Create(streamEntryIdString);

            pairs.Add(new KeyValuePair<StreamRecord, StreamEntryId>(streamRecord, streamEntryId));
        }

        return pairs;
    }

    private static string ExtractBulkStringData(RespObject arg, string parameterName)
    {
        if (arg is not BulkString bulkString || bulkString.Data == null)
            throw new ArgumentException($"Invalid {parameterName} format. Expected bulk string.");

        return bulkString.Data;
    }

    private static Array CreateStreamArray(KeyValuePair<StreamRecord, StreamEntryId> pair)
    {
        var entries = pair.Key.GetEntriesAfter(pair.Value);
        var entryArrays = entries.Select(CreateEntryArray).ToArray<RespObject>();
        return new Array(new BulkString(pair.Key.StreamKey), new Array(entryArrays));
    }

    private static Array CreateEntryArray(KeyValuePair<StreamEntryId, ImmutableDictionary<string, string>> entry)
    {
        var fieldValues = entry.Value
            .SelectMany(kvp => new[] { new BulkString(kvp.Key), new BulkString(kvp.Value) })
            .ToArray<RespObject>();

        return new Array(new BulkString(entry.Key.ToString()), new Array(fieldValues));
    }
}