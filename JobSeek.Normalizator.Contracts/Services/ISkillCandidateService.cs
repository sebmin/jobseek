namespace JobSeek.Normalizator.Contracts.Services;

public interface ISkillCandidateService : IDisposable
{
    IReadOnlyList<string> Select(string description, IReadOnlyList<string> staticSkillNames);
}
