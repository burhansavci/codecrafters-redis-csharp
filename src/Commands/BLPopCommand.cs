using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Rdb.Records;
using codecrafters_redis.Resp;
using codecrafters_redis.Server;
using Array = codecrafters_redis.Resp.Array;

namespace codecrafters_redis.Commands;

public class BLPopCommand(Database db, NotificationManager notificationManager) : ICommand
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

        if (TryGetListValues(listKeys, out var listValues))
            return listValues;

        var tcs = new TaskCompletionSource<bool>();

        foreach (var listKey in listKeys)
            notificationManager.Subscribe($"list:{listKey}", tcs);

        if (timeoutSeconds == 0)
        {
            await tcs.Task;
        }
        else
        {
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds)));
            if (completedTask != tcs.Task)
                return new BulkString(null);
        }

        TryGetListValues(listKeys, out var finalListValues);
        return finalListValues;
    }

    private bool TryGetListValues(string[] listKeys, out Array listValues)
    {
        foreach (var listKey in listKeys)
        {
            if (db.TryGetValue<ListRecord>(listKey, out var listRecord))
            {
                var value = listRecord.Pop(1);
                if (value is { Length: > 0 })
                {
                    listValues = new Array(new BulkString(listKey), new BulkString(value[0]));
                    return true;
                }
            }
        }

        listValues = new Array();
        return false;
    }
}