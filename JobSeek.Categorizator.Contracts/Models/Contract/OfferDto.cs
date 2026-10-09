namespace JobSeek.Categorizator.Contracts.Models.Contract
{
    public sealed record OfferDto(
        Guid Id,
        string Slug,
        IReadOnlyList<SkillDto> RequiredSkills,
        IReadOnlyList<SkillDto> NiceToHaveSkills);
}
