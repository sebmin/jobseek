using JobSeek.Comparator.Contracts.Models;

namespace JobSeek.Comparator.Contracts.Services;

public interface IAttributeComparerService
{
    OfferScore Score(Candidate candidate, Offer offer);
}
