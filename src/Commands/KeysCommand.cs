using System.Net.Sockets;
using codecrafters_redis.Rdb;
using codecrafters_redis.Resp;
using Array = codecrafters_redis.Resp.Array;

namespace codecrafters_redis.Commands;

public class KeysCommand(Database db) : ICommand
{
    public const string Name = "KEYS";
    private const char Wildcard = '*';

    public Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(args.Length, 1);

        var pattern = args[0].GetString("pattern");

        var matchingKeys = db.Keys.Where(key => MatchesPattern(key, pattern)).Select(x => new BulkString(x)).ToArray<RespObject>();

        var array = new Array(matchingKeys);

        return Task.FromResult<RespObject>(array);
    }

    private static bool MatchesPattern(string key, string pattern)
    {
        if (pattern == "*")
            return true;

        return key.StartsWith(pattern[(pattern.IndexOf(Wildcard) + 1)..]);
    }
}