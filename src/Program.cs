using codecrafters_redis.Commands;
using codecrafters_redis.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton(GetConfig());

        services.AddSingleton<RedisServer>();

        services.AddKeyedScoped<ICommand, PingCommand>(PingCommand.Name);
        services.AddKeyedScoped<ICommand, EchoCommand>(EchoCommand.Name);
        services.AddKeyedScoped<ICommand, GetCommand>(GetCommand.Name);
        services.AddKeyedScoped<ICommand, SetCommand>(SetCommand.Name);
        services.AddKeyedScoped<ICommand, ConfigGetCommand>(ConfigGetCommand.Name);
        services.AddKeyedScoped<ICommand, KeysCommand>(KeysCommand.Name);
        services.AddKeyedScoped<ICommand, InfoCommand>(InfoCommand.Name);
        services.AddKeyedScoped<ICommand, ReplConfCommand>(ReplConfCommand.Name);
        services.AddKeyedScoped<ICommand, PsyncCommand>(PsyncCommand.Name);
        services.AddKeyedScoped<ICommand, ReplConfGetAckCommand>(ReplConfGetAckCommand.Name);
        services.AddKeyedScoped<ICommand, ReplConfAckCommand>(ReplConfAckCommand.Name);
        services.AddKeyedScoped<ICommand, WaitCommand>(WaitCommand.Name);
    })
    .Build();

var redisServer = host.Services.GetRequiredService<RedisServer>();
await redisServer.Start();

return;

Dictionary<string, string> GetConfig()
{
    var supportedArgs = new[] { "dir", "dbfilename", "port", "replicaof" };
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