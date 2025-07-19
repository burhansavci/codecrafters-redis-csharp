using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
    private const string StreamsArg = "STREAMS";
    private const string BlockArg = "BLOCK";

    public async Task Handle(Socket connection, RespObject[] args)
    {
        var (blockTimeout, streamKeyIdPairArgs) = ValidateAndParseArguments(args);

        if (blockTimeout.HasValue)
            await Task.Delay(blockTimeout.Value);

        var streamArrays = ParseStreamKeyIdPairs(streamKeyIdPairArgs).Select(CreateStreamArray).Where(x => x != Array.Empty).ToArray<RespObject>();

        if (streamArrays.Length > 0)
            await connection.SendResp(new Array(streamArrays));
        else
            await connection.SendResp(new BulkString(null));
    }

    private static (TimeSpan? BlockTimeout, RespObject[] StreamKeyIdPairArgs) ValidateAndParseArguments(RespObject[] args)
    {
        if (args == null || args.Length < MinRequiredArgs)
            throw new ArgumentException($"XREAD requires at least {MinRequiredArgs} arguments");

        long? blockTimeoutMs = null;
        var argIndex = 0;

        while (argIndex < args.Length)
        {
            var arg = ExtractBulkStringData(args[argIndex], $"argument at position {argIndex + 1}");

            switch (arg.ToUpperInvariant())
            {
                case BlockArg:
                    if (argIndex + 1 >= args.Length || !TryExtractString(args[argIndex + 1], out var timeoutStr))
                        throw new ArgumentException($"{BlockArg} requires a timeout value");

                    if (!long.TryParse(timeoutStr, out var timeoutMs))
                        throw new FormatException($"Invalid timeout format: {timeoutStr}");

                    blockTimeoutMs = timeoutMs;
                    argIndex += 2;
                    break;

                case StreamsArg:
                    var streamKeyIdPairArgs = args.Skip(argIndex + 1).ToArray();
                    if (streamKeyIdPairArgs.Length == 0 || streamKeyIdPairArgs.Length % 2 != 0)
                        throw new ArgumentException("STREAMS requires pairs of stream keys and IDs");

                    return (blockTimeoutMs.HasValue ? TimeSpan.FromMilliseconds(blockTimeoutMs.Value) : null, streamKeyIdPairArgs);

                default:
                    throw new ArgumentException($"Unknown argument: {arg}");
            }
        }

        throw new ArgumentException($"XREAD command must contain the '{StreamsArg}' keyword");
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

    private static bool TryExtractString(RespObject arg, [NotNullWhen(true)] out string? value)
    {
        if (arg is BulkString { Data: not null } bulkString)
        {
            value = bulkString.Data;
            return true;
        }

        value = null;
        return false;
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

        return entryArrays.Length == 0 ? Array.Empty : new Array(new BulkString(pair.Key.StreamKey), new Array(entryArrays));
    }

    private static Array CreateEntryArray(KeyValuePair<StreamEntryId, ImmutableDictionary<string, string>> entry)
    {
        var fieldValues = entry.Value
            .SelectMany(kvp => new[] { new BulkString(kvp.Key), new BulkString(kvp.Value) })
            .ToArray<RespObject>();

        return new Array(new BulkString(entry.Key.ToString()), new Array(fieldValues));
    }
}