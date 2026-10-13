using JobSeek.Normalizator.Contracts.Services;
using JobSeek.Normalizator.Services.Providers;

namespace JobSeek.Normalizator.Services;

public class SkillConsolidationService : ISkillConsolidationService
{
    private readonly IEmbedderProvider _embeddedProvider;
    private bool _disposed;

    public SkillConsolidationService()
    {
        _embeddedProvider = new EmbedderProvider();
    }
    public List<string> CreateMasterList(List<string> rawOffersSkills)
    {
        var skillCounts = rawOffersSkills.GroupBy(s => s.Trim().ToLower())
            .ToDictionary(g => g.Key, g => g.Count());

        var masterSkillEmbeddingDict = new Dictionary<string, float[]>();
        foreach (var skill in skillCounts.Keys.OrderByDescending(k => skillCounts[k]))
        {
            var currentVector = _embeddedProvider.Embed(skill);
            var existingMatch = masterSkillEmbeddingDict.FirstOrDefault(existing => _embeddedProvider.Similarity(currentVector, existing.Value) > EmbeddingModel.SimilarityCutoff);

            if (existingMatch.Equals(default(KeyValuePair<string, float[]>)))
            {
                masterSkillEmbeddingDict.Add(skill, currentVector);
            }
        }
        var masterList = masterSkillEmbeddingDict.Keys.ToList();

        return masterList;
    }

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
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public static SkillConsolidationService CreateService() => new SkillConsolidationService(); // TODO: IoC
}
