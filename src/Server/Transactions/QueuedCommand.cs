using codecrafters_redis.Commands;
using codecrafters_redis.Resp;

namespace codecrafters_redis.Server.Transactions;

public record QueuedCommand(string CommandName, string Request, ICommand Command, RespObject[] Args);