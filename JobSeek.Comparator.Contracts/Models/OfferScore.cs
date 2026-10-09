namespace JobSeek.Comparator.Contracts.Models;

public sealed record OfferScore(
    Guid Id,
    string Slug,
    double Score,
    double RequiredScore,
    double NiceToHaveScore,
    int RequiredMatches,
    int RequiredSkillCount,
    int NiceToHaveMatches,
    int NiceToHaveSkillCount);
