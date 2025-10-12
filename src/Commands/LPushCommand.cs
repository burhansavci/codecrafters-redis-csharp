using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Rdb.List;
using codecrafters_redis.Resp;

namespace codecrafters_redis.Commands;

public class LPushCommand(Database db) : ICommand
{
    public const string Name = "LPUSH";

    public Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfLessThan(args.Length, 2);

        var listKey = args[0].GetString("listKey");
        var values = args.Skip(1).Select(x => x.GetString("value")).ToArray();

        if (values.Length == 0)
            throw new ArgumentException("Invalid values. Expected at least one value.");
        
        var count = db.List.Push(listKey, values, ListPushDirection.Left);

        return Task.FromResult<RespObject>(new Integer(count));
    }
}