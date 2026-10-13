namespace JobSeek.Normalizator.Contracts.Services;

public interface ISkillConsolidationService : IDisposable
{
    List<string> CreateMasterList(List<string> rawSkills);
}
