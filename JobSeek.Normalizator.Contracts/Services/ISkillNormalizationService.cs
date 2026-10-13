
namespace JobSeek.Normalizator.Contracts.Services;

public interface ISkillNormalizationService : IDisposable
{
    string CreateNormalizedSkill(string rawSkill);
}
