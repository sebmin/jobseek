namespace JobSeek.Normalizator.Services.Providers;

public class Bm25PhraseRankerProvider : IBm25PhraseRankerProvider
{
    public const double K1 = 1.2;
    public const double B = 0.75;

    public IReadOnlyList<string> Rank(
        IReadOnlyList<string> document,
        IReadOnlyList<(string Canonical, string[] Words)> queries)
    {
        if (document.Count == 0)
            return [];

        var termFrequency = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var word in document)
        {
            termFrequency.TryGetValue(word, out var count);
            termFrequency[word] = count + 1;
        }

        var best = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var (canonical, words) in queries)
        {
            if (words.Length == 0 || !ContainsPhrase(document, words))
                continue;

            var score = Score(document.Count, words, termFrequency);
            if (!best.TryGetValue(canonical, out var current) || score > current)
                best[canonical] = score;
        }

        return best
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => pair.Key)
            .ToList();
    }

    private static double Score(
        int documentLength,
        IReadOnlyList<string> query,
        Dictionary<string, int> termFrequency)
    {
        var score = 0d;
        foreach (var term in query.Distinct(StringComparer.Ordinal))
        {
            termFrequency.TryGetValue(term, out var frequency);
            if (frequency == 0)
                return 0;

            var idf = Math.Log(1 + (0.5d / 1.5d));
            var denominator = frequency + K1 * (1 - B + B * documentLength / (double)documentLength);
            score += idf * (frequency * (K1 + 1)) / denominator;
        }

        return score;
    }

    private static bool ContainsPhrase(IReadOnlyList<string> document, IReadOnlyList<string> phrase)
    {
        if (phrase.Count > document.Count)
            return false;

        for (var start = 0; start <= document.Count - phrase.Count; start++)
        {
            var matches = true;
            for (var offset = 0; offset < phrase.Count; offset++)
            {
                if (!string.Equals(document[start + offset], phrase[offset], StringComparison.Ordinal))
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
                return true;
        }

        return false;
    }
}

internal interface IBm25PhraseRankerProvider
{
    IReadOnlyList<string> Rank(
        IReadOnlyList<string> document,
        IReadOnlyList<(string Canonical, string[] Words)> queries);
}
