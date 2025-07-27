namespace codecrafters_redis.Rdb.List;

public enum ListPushDirection
{
    Left,
    Right
}

public enum ListPopDirection
{
    Left,
    Right
}

public sealed record ListPopResult(string ListKey, string Value);
