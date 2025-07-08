namespace codecrafters_redis.Rdb;

public enum FileSection
{
    Metadata = 0XFA,
    DatabaseSubSectionStart = 0XFE,
    HashTableSize = 0XFB,
    ExpireTimeInMilliseconds = 0XFC,
    ExpireTimeInSeconds = 0XFD,
    EndOfFile = 0XFF
}