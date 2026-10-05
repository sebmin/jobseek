using JobSeek.Categorizator.Contracts.Models.Contract;
using JobSeek.Prompter.Contracts.Models;

namespace JobSeek.Prompter.Contracts.Services;

public interface IAttributesOfferService
{
    Task<OfferAttributesDto> ExtractAttributes(
        Guid id,
        string description,
        List<SkillDto> requiredSkills,
        List<SkillDto> niceToHaveSkills);
}
