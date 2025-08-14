namespace codecrafters_redis.Rdb.SortedSet;

public record SortedSetRecord : Record
{
    private readonly Dictionary<string, decimal> _memberScores;
    private readonly SortedSet<SortedSetItem> _sortedSet;

    private SortedSetRecord(SortedSet<SortedSetItem> sortedSet, Dictionary<string, decimal> memberScores, DateTime? expireAt = null) : base(sortedSet, ValueType.SortedSet, expireAt)
    {
        _sortedSet = sortedSet;
        _memberScores = memberScores;
    }

    public static SortedSetRecord Create(SortedSetItem[] entries, DateTime? expireAt = null)
    {
        var sortedSet = new SortedSet<SortedSetItem>(new SortedSetItemComparer());
        var memberScores = new Dictionary<string, decimal>();

        foreach (var entry in entries)
        {
            sortedSet.Add(entry);
            memberScores[entry.Member] = entry.Score;
        }

        return new SortedSetRecord(sortedSet, memberScores, expireAt);
    }

    public int Add(SortedSetItem item)
    {
        if (!_memberScores.TryGetValue(item.Member, out var oldScore))
        {
            _sortedSet.Add(item);
            _memberScores[item.Member] = item.Score;
            return 1;
        }

        if (oldScore == item.Score)
            return 0;

        var oldItem = item with { Score = oldScore };
        _sortedSet.Remove(oldItem);
        _sortedSet.Add(item);
        _memberScores[item.Member] = item.Score;
        return 0;
    }

    public int? Rank(string member)
    {
        if (!_memberScores.TryGetValue(member, out var score))
            return null;

        var item = new SortedSetItem(score, member);

        return _sortedSet.GetViewBetween(_sortedSet.Min, item).Count - 1;
    }
}