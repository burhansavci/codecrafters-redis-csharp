namespace codecrafters_redis.Resp.Parsing;

public record RespCommand(string CommandName, RespObject[] Args, Array RequestArray);
