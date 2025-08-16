using codecrafters_redis.Commands;
using codecrafters_redis.Rdb;
using codecrafters_redis.Resp.Parsing;
using codecrafters_redis.Server;
using codecrafters_redis.Server.Channels;
using codecrafters_redis.Server.Replications;
using codecrafters_redis.Server.Transactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        services.AddSingleton(new RedisConfiguration(args));

        services.AddSingleton<Database>();

        services.AddSingleton<RedisServer>();

        services.AddScoped<ConnectionHandler>();

        services.AddSingleton<ReplicationManager>();
        services.AddSingleton<TransactionManager>();
        services.AddSingleton<ChannelManager>();

        services.AddSingleton<RespCommandParser>();

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
        services.AddKeyedScoped<ICommand, TypeCommand>(TypeCommand.Name);
        services.AddKeyedScoped<ICommand, XAddCommand>(XAddCommand.Name);
        services.AddKeyedScoped<ICommand, XRangeCommand>(XRangeCommand.Name);
        services.AddKeyedScoped<ICommand, XReadCommand>(XReadCommand.Name);
        services.AddKeyedScoped<ICommand, IncrCommand>(IncrCommand.Name);
        services.AddKeyedScoped<ICommand, MultiCommand>(MultiCommand.Name);
        services.AddKeyedScoped<ICommand, ExecCommand>(ExecCommand.Name);
        services.AddKeyedScoped<ICommand, DiscardCommand>(DiscardCommand.Name);
        services.AddKeyedScoped<ICommand, RPushCommand>(RPushCommand.Name);
        services.AddKeyedScoped<ICommand, LRangeCommand>(LRangeCommand.Name);
        services.AddKeyedScoped<ICommand, LPushCommand>(LPushCommand.Name);
        services.AddKeyedScoped<ICommand, LLenCommand>(LLenCommand.Name);
        services.AddKeyedScoped<ICommand, LPopCommand>(LPopCommand.Name);
        services.AddKeyedScoped<ICommand, BLPopCommand>(BLPopCommand.Name);
        services.AddKeyedScoped<ICommand, SubscribeCommand>(SubscribeCommand.Name);
        services.AddKeyedScoped<ICommand, PublishCommand>(PublishCommand.Name);
        services.AddKeyedScoped<ICommand, UnsubscribeCommand>(UnsubscribeCommand.Name);
        services.AddKeyedScoped<ICommand, ZAddCommand>(ZAddCommand.Name);
        services.AddKeyedScoped<ICommand, ZRankCommand>(ZRankCommand.Name);
        services.AddKeyedScoped<ICommand, ZRangeCommand>(ZRangeCommand.Name);
        services.AddKeyedScoped<ICommand, ZCardCommand>(ZCardCommand.Name);
        services.AddKeyedScoped<ICommand, ZScoreCommand>(ZScoreCommand.Name);
    })
    .Build();

var redisServer = host.Services.GetRequiredService<RedisServer>();
await redisServer.Start();