using FuzzySharp;
using JobSeek.Normalizator.Contracts.Services;
using JobSeek.Normalizator.Services.Providers;

namespace JobSeek.Normalizator.Services.Services;

public sealed class SkillCandidateService : ISkillCandidateService
{
    private readonly IEmbedderProvider _embedder;
    private readonly ISkillAliasProvider _aliases;
    private readonly ISkillTextProvider _text;
    private readonly ISkillCandidateFusionProvider _fusion;
    private readonly IBm25PhraseRankerProvider _bm25;
    private readonly Dictionary<string, string> _masterByKey;
    private readonly Dictionary<string, string> _normalizedSkills;
    private readonly Dictionary<string, float[]> _masterVectors;
    private readonly List<(string Canonical, string[] Words)> _queries;
    private bool _disposed;

    public SkillCandidateService(List<string> masterSkillList)
    {
        _embedder = new EmbedderProvider();
        _aliases = new SkillAliasProvider();
        _text = new SkillTextProvider();
        _fusion = new SkillCandidateFusionProvider();
        _bm25 = new Bm25PhraseRankerProvider();
        _masterByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _normalizedSkills = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _masterVectors = new Dictionary<string, float[]>(StringComparer.OrdinalIgnoreCase);

        foreach (var skill in masterSkillList)
        {
            if (string.IsNullOrWhiteSpace(skill) || _masterByKey.ContainsKey(skill))
                continue;

            var words = _text.SplitToWords(skill);
            if (words.Count == 0)
                continue;

            var normalized = string.Join(' ', words);
            _masterByKey[skill] = skill;
            _normalizedSkills[skill] = normalized;
            _masterVectors[skill] = _embedder.Embed(normalized);
        }

        _queries = BuildQueries();
    }

    public static ISkillCandidateService CreateService(List<string> masterSkillList) =>
        new SkillCandidateService(masterSkillList);

    public IReadOnlyList<string> Select(string description, IReadOnlyList<string> staticSkillNames)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var words = _text.SplitToWords(description);
        var bm25 = words.Count == 0
            ? []
            : _bm25.Rank(words, _queries);
        var windows = words.Count == 0
            ? []
            : RankWindows(words);

        var staticNames = new List<string>();
        foreach (var name in staticSkillNames)
        {
            if (string.IsNullOrWhiteSpace(name))
                continue;

            staticNames.Add(Canonical(name));
        }

        return _fusion.Fuse(staticNames, bm25, windows);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _embedder.Dispose();
        _disposed = true;
    }

    private List<(string Canonical, string[] Words)> BuildQueries()
    {
        var queries = new List<(string Canonical, string[] Words)>();
        foreach (var skill in _masterByKey.Values)
        {
            var words = _text.SplitToWords(skill);
            if (words.Count > 0)
                queries.Add((skill, words.ToArray()));
        }

        foreach (var (alias, canonical) in _aliases.Entries)
        {
            if (!_masterByKey.TryGetValue(canonical, out var master))
                continue;

            var words = _text.SplitToWords(alias);
            if (words.Count > 0)
                queries.Add((master, words.ToArray()));
        }

        return queries;
    }

    private string Canonical(string name) =>
        _masterByKey.TryGetValue(name, out var master) ? master : name.Trim();

    private IReadOnlyList<string> RankWindows(List<string> words)
    {
        var bestRatio = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var bestCosine = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        var seenWindows = new HashSet<string>(StringComparer.Ordinal);
        for (var width = 1; width <= 3 && width <= words.Count; width++)
        {
            for (var start = 0; start <= words.Count - width; start++)
            {
                var window = string.Join(' ', words.Skip(start).Take(width));
                if (!seenWindows.Add(window))
                    continue;

                var vector = _embedder.Embed(window);
                foreach (var (skill, master) in _masterVectors)
                {
                    var similarity = _embedder.Similarity(vector, master);
                    if (!bestCosine.TryGetValue(skill, out var current) || similarity > current)
                        bestCosine[skill] = similarity;

                    var ratio = Fuzz.Ratio(window, _normalizedSkills[skill]);
                    if (!bestRatio.TryGetValue(skill, out var currentRatio) || ratio > currentRatio)
                        bestRatio[skill] = ratio;
                }
            }
        }

        return _masterVectors.Keys
            .Select(skill =>
            {
                bestRatio.TryGetValue(skill, out var ratio);
                bestCosine.TryGetValue(skill, out var cosine);
                return (skill, ratio, cosine);
            })
            .Where(match => match.ratio >= EmbeddingModel.WindowWordRatioCutoff || match.cosine >= EmbeddingModel.WindowSimilarityCutoff)
            .OrderByDescending(match => Math.Max(match.ratio / 100d, match.cosine))
            .ThenBy(match => match.skill, StringComparer.OrdinalIgnoreCase)
            .Select(match => match.skill)
            .ToList();
    }
}
