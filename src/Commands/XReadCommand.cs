using System.Collections.Immutable;
using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Rdb.Stream;
using codecrafters_redis.Resp;
using Array = codecrafters_redis.Resp.Array;

namespace codecrafters_redis.Commands;

public sealed class XReadCommand(Database db) : ICommand
{
    public const string Name = "XREAD";
    private const int MinRequiredArgs = 3;
    private const string StreamsKeyword = "STREAMS";
    private const string BlockKeyword = "BLOCK";

    public async Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        try
        {
            var (blockTimeout, streamRequests) = ParseArguments(args);
            
            var result = await db.GetStreams(streamRequests, blockTimeout);
            
            return result != null ? CreateResponseArray(result) : new BulkString(null);
        }
        catch (Exception ex)
        {
            return new SimpleError($"ERR {ex.Message}");
        }
    }
    
    private static (TimeSpan BlockTimeout, List<StreamReadRequest> StreamRequests) ParseArguments(RespObject[] args)
    {
        if (args.Length < MinRequiredArgs)
            throw new ArgumentException($"XREAD requires at least {MinRequiredArgs} arguments");

        var blockTimeout = TimeSpan.Zero;
        var argIndex = 0;

        // Parse optional BLOCK timeout
        if (argIndex < args.Length && string.Equals(args[argIndex].GetString(), BlockKeyword, StringComparison.OrdinalIgnoreCase))
        {
            if (++argIndex >= args.Length)
                throw new ArgumentException("BLOCK requires a timeout value");

            var timeoutStr = args[argIndex++].GetString("timeout");
            if (!long.TryParse(timeoutStr, out var timeoutMs))
                throw new FormatException($"Invalid timeout format: {timeoutStr}");

            blockTimeout = timeoutMs == 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(timeoutMs);
        }

        // Parse STREAMS keyword and stream/ID pairs
        if (argIndex >= args.Length || !string.Equals(args[argIndex++].GetString(), StreamsKeyword, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"XREAD command must contain '{StreamsKeyword}' keyword");
        
        var remainingArgs = args.Length - argIndex;
        if (remainingArgs == 0 || remainingArgs % 2 != 0)
            throw new ArgumentException("STREAMS requires pairs of stream keys and IDs");

        var streamCount = remainingArgs / 2;
        var streamRequests = new List<StreamReadRequest>(streamCount);

        // Parse stream keys and IDs
        for (int i = 0; i < streamCount; i++)
        {
            var streamKey = args[argIndex + i].GetString($"stream key at position {argIndex + i + 1}");
            var startId = args[argIndex + streamCount + i].GetString($"stream ID at position {argIndex + streamCount + i + 1}");
            
            streamRequests.Add(new StreamReadRequest(streamKey, startId));
        }

        return (blockTimeout, streamRequests);
    }

    private static Array CreateResponseArray(StreamReadResult result)
    {
        var streamArrays = result.Results
            .Select(CreateStreamArray)
            .ToArray<RespObject>();

        return new Array(streamArrays);
    }

    private static Array CreateStreamArray(StreamResult streamResult)
    {
        var entryArrays = streamResult.Entries
            .Select(CreateEntryArray)
            .ToArray<RespObject>();

        return new Array(
            new BulkString(streamResult.StreamKey),
            new Array(entryArrays)
        );
    }

    private static Array CreateEntryArray(KeyValuePair<StreamEntryId, ImmutableDictionary<string, string>> entry)
    {
        var fieldValues = entry.Value
            .SelectMany(field => new RespObject[] { new BulkString(field.Key), new BulkString(field.Value) })
            .ToArray();

        return new Array(
            new BulkString(entry.Key.ToString()),
            new Array(fieldValues)
        );
    }
}