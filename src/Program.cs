using codecrafters_redis;
using codecrafters_redis.Commands;

var mediator = new Mediator();

var redisServer = new Server(mediator);
await redisServer.StartAsync();

