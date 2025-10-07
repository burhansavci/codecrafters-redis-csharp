using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Rdb.List;
using codecrafters_redis.Resp;
using Array = codecrafters_redis.Resp.Array;

namespace codecrafters_redis.Commands;

public class BLPopCommand(Database db) : ICommand
{
    public const string Name = "BLPOP";
    private const int MinRequiredArgs = 2;

    public async Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.Length < MinRequiredArgs)
            throw new ArgumentException($"BLPOP requires at least {MinRequiredArgs} arguments");

        var timeoutSeconds = double.Parse(args[^1].GetString("timeoutSeconds"));
        var listKeys = args[..^1].Select(k => k.GetString("listKey")).ToArray();

        var timeout = timeoutSeconds == 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(timeoutSeconds);

        var result = await db.Pop(listKeys, timeout, ListPopDirection.Left);

        return result != null
            ? new Array(new BulkString(result.ListKey), new BulkString(result.Value))
            : new Array(null);
    }
}