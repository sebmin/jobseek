namespace JobSeek.Categorizator.Contracts.Models.Contract
{
    public sealed record CandidateDto(
        IReadOnlyList<SkillDto> Skills);
}
