using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using codecrafters_redis.Rdb.Records;
using codecrafters_redis.Server;

namespace codecrafters_redis.Rdb;

public class Database
{
    private readonly RedisConfiguration _configuration;
    private readonly ConcurrentDictionary<string, Record> _records = new();

    public Database(RedisConfiguration configuration)
    {
        _configuration = configuration;

        LoadFromRdb();
    }

    public void Add(string key, Record record) => _records.TryAdd(key, record);

    public bool TryGetValue<T>(string key, [MaybeNullWhen(false)] out T record) where T : Record
    {
        if (_records.TryGetValue(key, out var innerRecord) && innerRecord is T { IsExpired: false } typedRecord)
        {
            record = typedRecord;
            return true;
        }

        record = null;
        return false;
    }

    private void LoadFromRdb()
    {
        if (string.IsNullOrWhiteSpace(_configuration.Directory) || string.IsNullOrWhiteSpace(_configuration.DbFileName))
            return;

        using var reader = new RdbReader(_configuration.Directory, _configuration.DbFileName);
        var db = reader.Read();

        foreach (var kvp in db)
            _records.TryAdd(kvp.Key, kvp.Value);
    }
}