using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Rdb.Records;
using codecrafters_redis.Resp;
using codecrafters_redis.Server;

namespace codecrafters_redis.Commands;

public sealed class XAddCommand(Database db, RedisServer server) : ICommand
{
    public const string Name = "XADD";

    private const int MinRequiredArgs = 4;
    private const string ZeroIdError = "The ID specified in XADD must be greater than 0-0";
    private const string InvalidIdError = "The ID specified in XADD is equal or smaller than the target stream top item";

    public async Task Handle(Socket connection, RespObject[] args)
    {
        try
        {
            var (streamKey, id, fields) = ValidateAndParseArguments(args);

            db.TryGetValue<StreamRecord>(streamKey, out var existingStream);

            var entryId = StreamEntryId.Create(id, existingStream?.LastEntryId);

            ValidateEntryId(entryId, existingStream);

            if (existingStream != null)
            {
                if (!existingStream.TryAppendEntry(entryId, fields))
                    throw new ArgumentException(InvalidIdError);
            }
            else
            {
                var newStream = StreamRecord.Create(streamKey, entryId, fields);
                db.Add(streamKey, newStream);
            }

            server.NotifyClientsForStream(streamKey);

            await connection.SendResp(new BulkString(entryId.ToString()));
        }
        catch (Exception ex)
        {
            await connection.SendResp(new SimpleError($"ERR {ex.Message}"));
        }
    }

    private static (string StreamKey, string Id, List<KeyValuePair<string, string>> Fields) ValidateAndParseArguments(RespObject[] args)
    {
        if (args == null || args.Length < MinRequiredArgs)
            throw new ArgumentException($"XADD requires at least {MinRequiredArgs} arguments");

        if ((args.Length - 2) % 2 != 0)
            throw new ArgumentException("XADD requires an even number of field-value pairs");

        var streamKey = ExtractBulkStringData(args[0], "stream key");
        var id = ExtractBulkStringData(args[1], "entry ID");
        var fields = ParseFieldValuePairs(args.Skip(2).ToArray());

        return new ValueTuple<string, string, List<KeyValuePair<string, string>>>(streamKey, id, fields);
    }

    private static List<KeyValuePair<string, string>> ParseFieldValuePairs(RespObject[] fieldValueArgs)
    {
        var fields = new List<KeyValuePair<string, string>>();

        for (int i = 0; i < fieldValueArgs.Length; i += 2)
        {
            var fieldKey = ExtractBulkStringData(fieldValueArgs[i], $"field key at position {i + 2}");
            var fieldValue = ExtractBulkStringData(fieldValueArgs[i + 1], $"field value at position {i + 3}");

            fields.Add(new KeyValuePair<string, string>(fieldKey, fieldValue));
        }

        return fields;
    }

    private static string ExtractBulkStringData(RespObject arg, string parameterName)
    {
        if (arg is not BulkString bulkString || bulkString.Data == null)
            throw new ArgumentException($"Invalid {parameterName} format. Expected bulk string.");

        return bulkString.Data;
    }

    private static void ValidateEntryId(StreamEntryId entryId, StreamRecord? existingStream)
    {
        if (entryId == StreamEntryId.Zero)
            throw new ArgumentException(ZeroIdError);

        if (existingStream?.LastEntryId != null && entryId <= existingStream.LastEntryId)
            throw new ArgumentException(InvalidIdError);
    }
}