using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using codecrafters_redis.Rdb.Extensions;
using codecrafters_redis.Rdb.List;
using codecrafters_redis.Rdb.SortedSet;
using codecrafters_redis.Rdb.Stream;
using codecrafters_redis.Server;

namespace codecrafters_redis.Rdb;

public sealed class Database : IDisposable
{
    private readonly RedisConfiguration _configuration;
    private readonly ConcurrentDictionary<string, Record> _records = new();
    private readonly ListOperations _listOperations;
    private readonly StreamOperations _streamOperations;
    private readonly SortedSetOperations _sortedSetOperations;
    private bool _disposed;

    public Database(RedisConfiguration configuration)
    {
        _configuration = configuration;
        _listOperations = new ListOperations(_records);
        _streamOperations = new StreamOperations(_records);
        _sortedSetOperations = new SortedSetOperations(_records);
        LoadFromRdb();
    }

    public IEnumerable<string> Keys => _records.Keys;

    public bool TryGetRecord<T>(string key, [MaybeNullWhen(false)] out T record) where T : Record
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _records.TryGetRecord(key, out record);
    }

    public void AddOrUpdate(string key, Record record)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(record);

        _records.AddOrUpdate(key, record, (_, _) => record);
    }

    public int Push(string listKey, IReadOnlyList<string> values, ListPushDirection direction)
        => _listOperations.Push(listKey, values, direction);

    public string[]? Pop(string listKey, int count = 1, ListPopDirection direction = ListPopDirection.Left)
        => _listOperations.Pop(listKey, count, direction);

    public async Task<ListPopResult?> Pop(IReadOnlyList<string> listKeys, TimeSpan timeout, ListPopDirection direction = ListPopDirection.Left)
        => await _listOperations.Pop(listKeys, timeout, direction);

    public StreamEntryId AddStreamEntry(string streamKey, string entryIdString, IReadOnlyList<KeyValuePair<string, string>> fields)
        => _streamOperations.AddStreamEntry(streamKey, entryIdString, fields);

    public async Task<StreamReadResult?> GetStreams(IReadOnlyList<StreamReadRequest> requests, TimeSpan timeout)
        => await _streamOperations.Get(requests, timeout);

    public int ZAdd(string sortedSetKey, decimal score, string member)
        => _sortedSetOperations.Add(sortedSetKey, score, member);

    public int? ZRank(string sortedSetKey, string member)
        => _sortedSetOperations.Rank(sortedSetKey, member);

    public decimal? ZScore(string sortedSetKey, string member)
        => _sortedSetOperations.Score(sortedSetKey, member);

    public int ZRem(string sortedSetKey, string member)
        => _sortedSetOperations.Remove(sortedSetKey, member);

    public int GeoAdd(string sortedSetKey, double longitude, double latitude, string member)
    {
        var score = GeoHashConverter.Encode(longitude, latitude);

        return _sortedSetOperations.Add(sortedSetKey, score, member);
    }

    public (double Longitude, double Latitude)? GeoPos(string key, string member)
    {
        var score = _sortedSetOperations.Score(key, member);

        if (score == null)
            return null;

        return GeoHashConverter.Decode((long)score.Value);
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

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _listOperations.Dispose();
        _streamOperations.Dispose();
    }
}