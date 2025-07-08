using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Rdb;
using codecrafters_redis.RESP;
using Array = codecrafters_redis.RESP.Array;

namespace codecrafters_redis.Commands;

public class KeysCommand(RdbReader reader) : ICommand
{
    public const string Name = "KEYS";
    private const char Wildcard = '*';

    public async Task Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(args.Length, 1);

        if (args[0] is not BulkString pattern)
            throw new FormatException("Invalid pattern format. Expected bulk string.");

        var db = reader.Read();
        reader.Dispose();

        var allKeys = db.Keys.ToArray();

        var matchingKeys = allKeys.Where(key => MatchesPattern(key, pattern.Data!)).Select(x => new BulkString(x)).ToArray<RespObject>();

        var array = new Array(matchingKeys);

        await connection.SendAsync(Encoding.UTF8.GetBytes(array));
    }

    private static bool MatchesPattern(string key, string pattern)
    {
        if (pattern == "*")
            return true;

        return key.StartsWith(pattern[(pattern.IndexOf(Wildcard) + 1)..]);
    }
}