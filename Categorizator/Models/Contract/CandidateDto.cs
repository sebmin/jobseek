namespace Categorizator.Models.Contract
{
    public sealed record CandidateDto(
        IReadOnlyList<SkillDto> Skills);
}
