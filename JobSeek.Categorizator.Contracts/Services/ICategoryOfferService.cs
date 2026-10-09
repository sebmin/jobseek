using JobSeek.Categorizator.Contracts.Models.Contract;
using JobSeek.Categorizator.Contracts.Models.OfferSource;

namespace JobSeek.Categorizator.Contracts.Services;

public interface ICategoryOfferService
{
    IReadOnlyList<OfferDto> CreateCategoryOfferDtos(List<SourceOffer> sourceOffers);
}
