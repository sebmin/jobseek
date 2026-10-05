namespace Prompter.Models
{
    public sealed record OfferAttributesDto(
        Guid Id,
        IReadOnlyList<OfferSkillDto> RequiredSkills,
        IReadOnlyList<OfferSkillDto> NiceToHaveSkills);
}
