namespace codecrafters_redis.Rdb.SortedSet;

public sealed record SortedSetItem(decimal Score, string Member);

public class SortedSetItemComparer : IComparer<SortedSetItem>
{
    public int Compare(SortedSetItem? x, SortedSetItem? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (y is null) return 1;
        if (x is null) return -1;

        // 1. Primary sort by Score.
        var scoreComparison = x.Score.CompareTo(y.Score);
        if (scoreComparison != 0) return scoreComparison;

        // 2. Secondary sort by Member (alphabetically) if scores are equal.
        return string.Compare(x.Member, y.Member, StringComparison.Ordinal);
    }
}