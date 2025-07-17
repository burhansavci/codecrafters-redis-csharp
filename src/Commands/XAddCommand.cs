using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Rdb.Records;
using codecrafters_redis.Resp;
using codecrafters_redis.Server;

namespace codecrafters_redis.Commands;

public class XAddCommand(Database db) : ICommand
{
    public const string Name = "XADD";

    public async Task Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfLessThan(args.Length, 4);

        if (args[0] is not BulkString streamKeyArg)
            throw new FormatException("Invalid stream key format. Expected bulk string.");

        if (args[1] is not BulkString idArg)
            throw new FormatException("Invalid id format. Expected bulk string.");

        if (args[2] is not BulkString keyArg)
            throw new FormatException("Invalid key format. Expected bulk string.");

        if (args[3] is not BulkString valueArg)
            throw new FormatException("Invalid value format. Expected bulk string.");

        var streamKey = streamKeyArg.Data!;
        var id = idArg.Data!;
        
        db.TryGetValue<StreamRecord>(streamKey, out var streamRecord);
        var streamEntryId = StreamEntryId.Create(id, streamRecord?.LastStreamEntryId);

        if (streamEntryId == StreamEntryId.Zero)
        {
            SimpleError error = $"ERR The ID specified in XADD must be greater than {StreamEntryId.Zero}";
            await connection.SendResp(error);
            return;
        }

        if (streamRecord != null)
        {
            if (!streamRecord.AppendStreamEntry(streamEntryId, keyArg.Data!, valueArg.Data!))
            {
                SimpleError error = "ERR The ID specified in XADD is equal or smaller than the target stream top item";
                await connection.SendResp(error);
                return;
            }
        }
        else
        {
            db.Add(streamKey, StreamRecord.Create(streamEntryId, keyArg.Data!, valueArg.Data!));
        }

        await connection.SendResp(new BulkString(streamEntryId.ToString()));
    }
}