using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Rdb.Records;
using codecrafters_redis.Resp;
using codecrafters_redis.Server;

namespace codecrafters_redis.Commands;

public class RPushCommand(Database db, NotificationManager notificationManager) : ICommand
{
    public const string Name = "RPUSH";

    public Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfLessThan(args.Length, 2);

        var listKey = args[0].GetString("listKey");
        var values = args.Skip(1).Select(x => x.GetString("value")).ToArray();

        if (values.Length == 0)
            throw new ArgumentException("Invalid values. Expected at least one value.");

        if (db.TryGetValue<ListRecord>(listKey, out var listRecord))
            foreach (var value in values)
                listRecord.Append(value);
        else
            listRecord = ListRecord.Create(listKey, values: values);

        db.AddOrUpdate(listKey, listRecord);

        notificationManager.Notify($"list:{listKey}");

        return Task.FromResult<RespObject>(new Integer(listRecord.Count));
    }
}