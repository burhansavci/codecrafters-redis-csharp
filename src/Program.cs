using codecrafters_redis;
using codecrafters_redis.Commands;

var redisServer = new Server(Config());

redisServer.RegisterCommand(PingCommand.Name, new PingCommand());
redisServer.RegisterCommand(EchoCommand.Name, new EchoCommand());
redisServer.RegisterCommand(GetCommand.Name, new GetCommand(redisServer));
redisServer.RegisterCommand(SetCommand.Name, new SetCommand(redisServer));
redisServer.RegisterCommand(ConfigGetCommand.Name, new ConfigGetCommand(redisServer));
redisServer.RegisterCommand(KeysCommand.Name, new KeysCommand(redisServer));
redisServer.RegisterCommand(InfoCommand.Name, new InfoCommand(redisServer));

await redisServer.StartAsync();
return;


Dictionary<string, string> Config()
{
    var supportedArgs = new[] { "dir", "dbfilename", "port" };
    var config = new Dictionary<string, string>();

    foreach (var arg in supportedArgs)
    {
        var value = GetArgValue(arg);
        if (!string.IsNullOrWhiteSpace(value))
            config.Add(arg, value);
    }

    return config;
}

string GetArgValue(string arg)
{
    arg = $"--{arg}";
    var index = Array.IndexOf(args, arg);
    return index == -1 ? string.Empty : args[index + 1];
}