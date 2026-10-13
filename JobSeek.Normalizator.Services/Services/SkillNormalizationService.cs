using FuzzySharp;
using JobSeek.Normalizator.Contracts.Services;
using JobSeek.Normalizator.Services.Providers;

namespace JobSeek.Normalizator.Services.Services;

public class SkillNormalizationService : ISkillNormalizationService
{
    private readonly List<string> _masterSkillList;
    private readonly IEmbedderProvider _embeddedProvider;
    private readonly ISkillAliasProvider _aliasProvider;
    private bool _disposed;

    public SkillNormalizationService(List<string> masterSkillList)
    {
        _masterSkillList = masterSkillList;
        _embeddedProvider = new EmbedderProvider();
        _aliasProvider = new SkillAliasProvider();
    }

    public string CreateNormalizedSkill(string rawSkill)
    {
        if (string.IsNullOrWhiteSpace(rawSkill)) return string.Empty;

        string cleanedRaw = rawSkill.Trim();

        var exactMatch = _masterSkillList.FirstOrDefault(m => string.Equals(m, cleanedRaw, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null)
        {
            return exactMatch;
        }

        var alias = _aliasProvider.Resolve(cleanedRaw);
        if (alias != null)
        {
            var master = _masterSkillList.FirstOrDefault(skill =>
                string.Equals(skill, alias, StringComparison.OrdinalIgnoreCase));
            return master ?? alias;
        }

        // Handle typos
        var query = cleanedRaw.ToLowerInvariant();
        var fuzzyMatch = _masterSkillList
            .Select(master => (Master: master, Score: Fuzz.Ratio(query, master.ToLowerInvariant())))
            .OrderByDescending(match => match.Score)
            .FirstOrDefault();
        if (fuzzyMatch.Score > 90)
            return fuzzyMatch.Master;

        //Embedding
        var rawEmbedding = _embeddedProvider.Embed(cleanedRaw);
        string? bestVectorMatch = null;
        float highestSimilarity = 0f;

        foreach (var masterSkill in _masterSkillList)
        {
            var masterEmbedding = _embeddedProvider.Embed(masterSkill);
            float similarity = _embeddedProvider.Similarity(rawEmbedding, masterEmbedding);

            if (similarity > highestSimilarity)
            {
                highestSimilarity = similarity;
                bestVectorMatch = masterSkill;
            }
        }

        if (highestSimilarity > EmbeddingModel.SimilarityCutoff && bestVectorMatch != null)
        {
            return bestVectorMatch;
        }

        //Fallback
        return cleanedRaw;
    }

    public static ISkillNormalizationService CreateService(List<string> masterSkillList) => new SkillNormalizationService(masterSkillList); // TODO: IoC

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _embeddedProvider.Dispose();
        }
        _disposed = true;
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }
}
