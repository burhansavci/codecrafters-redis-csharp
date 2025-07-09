using codecrafters_redis;
using codecrafters_redis.Commands;
using codecrafters_redis.Rdb;


var redisServer = new Server(Config());

redisServer.RegisterCommand(PingCommand.Name, new PingCommand());
redisServer.RegisterCommand(EchoCommand.Name, new EchoCommand());
redisServer.RegisterCommand(GetCommand.Name, new GetCommand(redisServer));
redisServer.RegisterCommand(SetCommand.Name, new SetCommand(redisServer));
redisServer.RegisterCommand(ConfigGetCommand.Name, new ConfigGetCommand(redisServer));
redisServer.RegisterCommand(KeysCommand.Name, new KeysCommand(redisServer));

await redisServer.StartAsync();
return;


Dictionary<string, string> Config()
{
    const string dirArg = "dir";
    const string dbFileNameArg = "dbfilename";

    return new Dictionary<string, string>
    {
        { dirArg, GetArgValue(dirArg) },
        { dbFileNameArg, GetArgValue(dbFileNameArg) }
    };
}

string GetArgValue(string arg)
{
    arg = $"--{arg}";
    var index = Array.IndexOf(args, arg);
    return index == -1 ? string.Empty : args[index + 1];
}