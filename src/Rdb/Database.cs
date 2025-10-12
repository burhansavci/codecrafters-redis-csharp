using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using codecrafters_redis.Rdb.Extensions;
using codecrafters_redis.Rdb.Extensions.Geo;
using codecrafters_redis.Rdb.List;
using codecrafters_redis.Rdb.SortedSet;
using codecrafters_redis.Rdb.Stream;
using codecrafters_redis.Server;

namespace codecrafters_redis.Rdb;

public sealed class Database : IDisposable
{
    private readonly RedisConfiguration _configuration;
    private readonly ConcurrentDictionary<string, Record> _records = new();
    private bool _disposed;

    public Database(RedisConfiguration configuration)
    {
        _configuration = configuration;
        List = new ListOperations(_records);
        Stream = new StreamOperations(_records);
        SortedSet = new SortedSetOperations(_records);
        LoadFromRdb();
    }

    public IEnumerable<string> Keys => _records.Keys;
    public ListOperations List { get; }
    public StreamOperations Stream { get; }
    public SortedSetOperations SortedSet { get; }

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

    public int GeoAdd(string sortedSetKey, double longitude, double latitude, string member)
    {
        var score = GeoHashConverter.Encode(longitude, latitude);

        return SortedSet.Add(sortedSetKey, score, member);
    }

    public (double Longitude, double Latitude)? GeoPos(string key, string member)
    {
        var score = SortedSet.Score(key, member);

        if (score == null)
            return null;

        return GeoHashConverter.Decode((long)score.Value);
    }

    public double? GeoDistance(string key, string firstMember, string secondMember)
    {
        var firstMemberScore = SortedSet.Score(key, firstMember);
        var secondMemberScore = SortedSet.Score(key, secondMember);

        if (firstMemberScore == null || secondMemberScore == null)
            return null;

        var (lon1, lat1) = GeoHashConverter.Decode((long)firstMemberScore.Value);
        var (lon2, lat2) = GeoHashConverter.Decode((long)secondMemberScore.Value);

        return Haversine.Calculate(lon1, lat1, lon2, lat2);
    }
    
    public List<string>? GeoSearch(string key, double longitude, double latitude, double radius, string unit)
    {
        if(unit != "m")
            throw new NotSupportedException("Only m (meter) unit is supported for now.");

        if (!TryGetRecord<SortedSetRecord>(key, out var sortedSet))
            return null;

        var results = new List<string>();

        // It can be optimized to O(N+log(M)) (https://redis.io/docs/latest/commands/geosearch/)
        // where N is the number of elements in the grid-aligned bounding box area around the shape provided as the filter and M is the number of items inside the shape
        // but for the simplicity, I'll use O(N)
        foreach (var entry in sortedSet.GetAllEntries())
        {
            var (memberLon, memberLat) = GeoHashConverter.Decode((long)entry.Score);
            var distance = Haversine.Calculate(longitude, latitude, memberLon, memberLat);

            if (distance <= radius)
                results.Add(entry.Member);
        }

        return results;
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

        List.Dispose();
        Stream.Dispose();
    }
}