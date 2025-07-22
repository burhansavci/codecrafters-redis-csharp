using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Rdb.Records;
using codecrafters_redis.Resp;

namespace codecrafters_redis.Commands;

public class RPushCommand(Database db) : ICommand
{
    public const string Name = "RPUSH";

    public Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfNotEqual(args.Length, 2);

        var listKey = args[0].GetString("listKey");
        var value = args[1].GetString("value");

        if (db.TryGetValue<ListRecord>(listKey, out var listRecord))
            listRecord.Append(value);
        else
            listRecord = ListRecord.Create(listKey, values: value);

        db.AddOrUpdate(listKey, listRecord);

        return Task.FromResult<RespObject>(new Integer(listRecord.Count));
    }
}