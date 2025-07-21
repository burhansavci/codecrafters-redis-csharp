using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Rdb.Records;
using codecrafters_redis.Resp;
using codecrafters_redis.Server;

namespace codecrafters_redis.Commands;

public sealed class XAddCommand(Database db, NotificationManager notificationManager) : ICommand
{
    public const string Name = "XADD";

    private const int MinRequiredArgs = 4;
    private const string ZeroIdError = "The ID specified in XADD must be greater than 0-0";
    private const string InvalidIdError = "The ID specified in XADD is equal or smaller than the target stream top item";

    public Task<RespObject> Handle(Socket connection, RespObject[] args)
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

            notificationManager.Notify($"stream:{streamKey}");

            return Task.FromResult<RespObject>(new BulkString(entryId.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult<RespObject>(new SimpleError($"ERR {ex.Message}"));
        }
    }

    private static (string StreamKey, string Id, List<KeyValuePair<string, string>> Fields) ValidateAndParseArguments(RespObject[] args)
    {
        if (args == null || args.Length < MinRequiredArgs)
            throw new ArgumentException($"XADD requires at least {MinRequiredArgs} arguments");

        if ((args.Length - 2) % 2 != 0)
            throw new ArgumentException("XADD requires an even number of field-value pairs");

        var streamKey = args[0].GetString("stream key");
        var id = args[1].GetString("entry ID");
        var fields = ParseFieldValuePairs(args.Skip(2).ToArray());

        return new ValueTuple<string, string, List<KeyValuePair<string, string>>>(streamKey, id, fields);
    }

    private static List<KeyValuePair<string, string>> ParseFieldValuePairs(RespObject[] fieldValueArgs)
    {
        var fields = new List<KeyValuePair<string, string>>();

        for (int i = 0; i < fieldValueArgs.Length; i += 2)
        {
            var fieldKey = fieldValueArgs[i].GetString($"field key at position {i + 2}");
            var fieldValue = fieldValueArgs[i + 1].GetString($"field value at position {i + 3}");

            fields.Add(new KeyValuePair<string, string>(fieldKey, fieldValue));
        }

        return fields;
    }

    private static void ValidateEntryId(StreamEntryId entryId, StreamRecord? existingStream)
    {
        if (entryId == StreamEntryId.Zero)
            throw new ArgumentException(ZeroIdError);

        if (existingStream?.LastEntryId != null && entryId <= existingStream.LastEntryId)
            throw new ArgumentException(InvalidIdError);
    }
}