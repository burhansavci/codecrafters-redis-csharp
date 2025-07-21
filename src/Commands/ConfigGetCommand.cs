using System.Net.Sockets;
using codecrafters_redis.Resp;
using codecrafters_redis.Server;
using Array = codecrafters_redis.Resp.Array;

namespace codecrafters_redis.Commands;

public class ConfigGetCommand(RedisConfiguration configuration) : ICommand
{
    public const string Name = "CONFIG GET";

    public Task<RespObject> Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(args.Length, 1);

        var configName = args[0].GetString("configName");

        var response = configName.ToLowerInvariant() switch
        {
            "dir" => new Array(new BulkString("dir"), new BulkString(configuration.Directory)),
            "dbfilename" => new Array(new BulkString("dbfilename"), new BulkString(configuration.DbFileName)),
            _ => throw new ArgumentException($"Unknown configuration parameter: {configName}")
        };

        return Task.FromResult<RespObject>(response);
    }
}