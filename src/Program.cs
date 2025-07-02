using codecrafters_redis;
using codecrafters_redis.Commands;

var redisServer = new Server();

redisServer.RegisterCommand(PingCommand.Name, new PingCommand());
redisServer.RegisterCommand(EchoCommand.Name, new EchoCommand());
redisServer.RegisterCommand(GetCommand.Name, new GetCommand(redisServer));
redisServer.RegisterCommand(SetCommand.Name, new SetCommand(redisServer));

await redisServer.StartAsync();