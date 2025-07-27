using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace codecrafters_redis.Rdb.Extensions;

public static class DatabaseExtensions
{
    public static bool TryGetRecord<T>(this ConcurrentDictionary<string, Record> records, string key, [MaybeNullWhen(false)] out T record) where T : Record
    {
        if (records.TryGetValue(key, out var innerRecord) && innerRecord is T { IsExpired: false } typedRecord)
        {
            record = typedRecord;
            return true;
        }

        record = null;
        return false;
    }
}