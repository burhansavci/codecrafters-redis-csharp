using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Rdb.Records;
using codecrafters_redis.Resp;

namespace codecrafters_redis.Commands;

public class IncrCommand(Database db) : ICommand
{
    public const string Name = "INCR";

    private const string ValueIsNotIntegerError = "ERR value is not an integer or out of range";

    public Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(args.Length, 1);

        var key = args[0].GetString("key");

        int newValue;
        if (db.TryGetValue<StringRecord>(key, out var stringRecord))
        {
            if (!int.TryParse(stringRecord.Value, out var value))
                return Task.FromResult<RespObject>(new SimpleError(ValueIsNotIntegerError));

            newValue = value + 1;
        }
        else
            newValue = 1;

        db.AddOrUpdate(key, new StringRecord(newValue.ToString()));

        return Task.FromResult<RespObject>(new Integer(newValue));
    }
}