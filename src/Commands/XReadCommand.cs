using System.Collections.Immutable;
using System.Net.Sockets;
using System.Reactive.Linq;
using codecrafters_redis.Rdb;
using codecrafters_redis.Rdb.Records;
using codecrafters_redis.Resp;
using codecrafters_redis.Server;
using Array = codecrafters_redis.Resp.Array;

namespace codecrafters_redis.Commands;

public class XReadCommand(Database db, NotificationManager notificationManager) : ICommand
{
    public const string Name = "XREAD";

    private const int MinRequiredArgs = 3;
    private const string StreamsArg = "STREAMS";
    private const string BlockArg = "BLOCK";
    private const string SpecialId = "$";

    public async Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        var (blockTimeout, streamKeyIdPairArgs) = ValidateAndParseArguments(args);
        var streamKeyIdPairs = ParseStreamKeyIdPairs(streamKeyIdPairArgs);

        var immediateResults = GetStreamResults(streamKeyIdPairs);
        if (immediateResults.Length > 0)
            return new Array(immediateResults);

        if (!blockTimeout.HasValue)
            return new BulkString(null);

        return await HandleBlockingRead(streamKeyIdPairs, blockTimeout.Value);
    }

    private async Task<RespObject> HandleBlockingRead(List<KeyValuePair<StreamRecord, StreamEntryId>> streamKeyIdPairs, TimeSpan blockTimeout)
    {
        var streamKeysToWatch = streamKeyIdPairs.ConvertAll(p => p.Key.StreamKey);
        var streamObservables = streamKeysToWatch.Select(streamKey => notificationManager.Subscribe($"stream:{streamKey}")).ToArray();
        var mergedNotifications = streamObservables.Length == 1 ? streamObservables[0] : Observable.Merge(streamObservables);

        RespObject[] initialStreamArrays = [];
        
        // Initial check stream to see if data arrived between initial check and subscription
        var initialCheckStream = Observable.Defer(() =>
        {
            initialStreamArrays = GetStreamResults(streamKeyIdPairs);
            return initialStreamArrays.Length > 0 ? Observable.Return(true) : Observable.Empty<bool>();
        });

        var timeoutStream = blockTimeout == TimeSpan.Zero ? Observable.Never<bool>() : Observable.Timer(blockTimeout).Select(_ => true);

        await initialCheckStream
            .Merge(mergedNotifications)
            .Merge(timeoutStream)
            .Take(1);

        var finalStreamArrays = initialStreamArrays.Length > 0 ? initialStreamArrays : GetStreamResults(streamKeyIdPairs);
        return finalStreamArrays.Length > 0 ? new Array(finalStreamArrays) : new BulkString(null);
    }

    private static RespObject[] GetStreamResults(List<KeyValuePair<StreamRecord, StreamEntryId>> streamKeyIdPairs)
    {
        var results = new List<RespObject>(streamKeyIdPairs.Count);
        results.AddRange(streamKeyIdPairs.Select(CreateStreamArray).Where(streamArray => streamArray != Array.Empty));

        return results.ToArray();
    }

    private static (TimeSpan? BlockTimeout, RespObject[] StreamKeyIdPairArgs) ValidateAndParseArguments(RespObject[] args)
    {
        if (args == null || args.Length < MinRequiredArgs)
            throw new ArgumentException($"XREAD requires at least {MinRequiredArgs} arguments");

        long? blockTimeoutMs = null;
        var argIndex = 0;

        while (argIndex < args.Length)
        {
            var arg = args[argIndex].GetString($"argument at position {argIndex + 1}");

            switch (arg.ToUpperInvariant())
            {
                case BlockArg:
                    if (argIndex + 1 >= args.Length || !args[argIndex + 1].TryGetString(out var timeoutStr))
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
        var streamKeysLength = streamKeyIdPairArgs.Length / 2;
        var pairs = new List<KeyValuePair<StreamRecord, StreamEntryId>>(streamKeysLength);

        for (int i = 0; i < streamKeysLength; i++)
        {
            var streamKey = streamKeyIdPairArgs[i].GetString($"stream key at position {i + 1}");
            var streamRecord = GetOrCreateStreamRecord(streamKey);

            var streamEntryIdString = streamKeyIdPairArgs[i + streamKeysLength].GetString($"stream entry ID at position {i + streamKeysLength + 2}");
            var streamEntryId = streamEntryIdString == SpecialId ? streamRecord.LastEntryId ?? StreamEntryId.Zero : StreamEntryId.Create(streamEntryIdString);
            pairs.Add(new KeyValuePair<StreamRecord, StreamEntryId>(streamRecord, streamEntryId));
        }

        return pairs;
    }

    private StreamRecord GetOrCreateStreamRecord(string streamKey)
    {
        if (db.TryGetValue<StreamRecord>(streamKey, out var streamRecord))
            return streamRecord;

        // If stream doesn't exist, create a dummy one to hold the ID.
        // This is needed so XREAD can block on a non-existent stream.
        return StreamRecord.Create(streamKey, StreamEntryId.Zero, []);
    }

    private static Array CreateStreamArray(KeyValuePair<StreamRecord, StreamEntryId> pair)
    {
        var entries = pair.Key.GetEntriesAfter(pair.Value);

        if (entries.Count == 0)
            return Array.Empty;

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