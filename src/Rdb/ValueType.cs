namespace codecrafters_redis.Rdb;

// Reference: https://github.com/redis/redis/blob/unstable/src/rdb.h
public enum ValueType
{
    String = 0,
    Stream = 15 
}