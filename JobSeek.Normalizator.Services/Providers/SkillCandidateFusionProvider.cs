namespace JobSeek.Normalizator.Services.Providers;

public class SkillCandidateFusionProvider : ISkillCandidateFusionProvider
{
    public const int DefaultCap = 30;
    public const int RrfK = 60;

    public IReadOnlyList<string> Fuse(
        IReadOnlyList<string> staticNames,
        IReadOnlyList<string> bm25Ranked,
        IReadOnlyList<string> windowRanked,
        int cap = DefaultCap)
    {
        var protectedNames = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddRange(protectedNames, seen, staticNames);
        AddRange(protectedNames, seen, bm25Ranked);

        if (protectedNames.Count >= cap)
            return protectedNames;

        var bm25Rank = RankMap(bm25Ranked);
        var windowRank = RankMap(windowRanked);
        var windowOnly = windowRanked
            .Where(name => !seen.Contains(name))
            .OrderByDescending(name => ReciprocalRank(name, bm25Rank, windowRank))
            .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var result = new List<string>(protectedNames);
        foreach (var name in windowOnly)
        {
            if (result.Count >= cap)
                break;

            result.Add(name);
        }

        return result;
    }

    private static void AddRange(List<string> target, HashSet<string> seen, IReadOnlyList<string> names)
    {
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name) || !seen.Add(name))
                continue;

            target.Add(name);
        }
    }

    private static Dictionary<string, int> RankMap(IReadOnlyList<string> ranked)
    {
        var ranks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < ranked.Count; index++)
        {
            if (!ranks.ContainsKey(ranked[index]))
                ranks[ranked[index]] = index + 1;
        }

        return ranks;
    }

    private static double ReciprocalRank(
        string name,
        Dictionary<string, int> bm25Rank,
        Dictionary<string, int> windowRank)
    {
        var score = 0d;
        if (bm25Rank.TryGetValue(name, out var bm25))
            score += 1d / (RrfK + bm25);
        if (windowRank.TryGetValue(name, out var window))
            score += 1d / (RrfK + window);

        return score;
    }
}

internal interface ISkillCandidateFusionProvider
{
    IReadOnlyList<string> Fuse(
        IReadOnlyList<string> staticNames,
        IReadOnlyList<string> bm25Ranked,
        IReadOnlyList<string> windowRanked,
        int cap = SkillCandidateFusionProvider.DefaultCap);
}
