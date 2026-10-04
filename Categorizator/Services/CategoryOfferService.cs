using Categorizator.Models;
using Categorizator.Models.Contract;
using Categorizator.Models.Source;

namespace Categorizator.Services
{
    public class CategoryOfferService
    {
        public IReadOnlyList<OfferDto> CreateCategoryOfferDtos(List<SourceOffer> sourceOffers)
        {
            var offers = sourceOffers.Select(MapDto).ToList();

            return offers;
        }

        private static OfferDto MapDto(SourceOffer source)
        {
            if (!Guid.TryParse(source.Guid, out var id))
                throw new InvalidOperationException($"Offer '{source.Slug}' has no guid.");

            return new OfferDto(
                id,
                source.Slug ?? "",
                MapSkillDtos(source.RequiredSkills),
                MapSkillDtos(source.NiceToHaveSkills));
        }

        private static IReadOnlyList<SkillDto> MapSkillDtos(List<SourceSkill>? skills) =>
            (skills ?? []).Select(item => new SkillDto(item.Name ?? "", item.Level)).ToArray();

        public static CategoryOfferService CreateService() => new();//TODO: IoC
    }
}
