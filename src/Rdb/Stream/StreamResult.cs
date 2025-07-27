using System.Collections.Immutable;

namespace codecrafters_redis.Rdb.Stream;

public sealed record StreamReadRequest(string StreamKey, string StartId);

public sealed record StreamResult(string StreamKey, IReadOnlyList<KeyValuePair<StreamEntryId, ImmutableDictionary<string, string>>> Entries);

public sealed record StreamReadResult(IReadOnlyList<StreamResult> Results);
