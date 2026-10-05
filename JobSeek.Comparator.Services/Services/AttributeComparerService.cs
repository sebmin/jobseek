using JobSeek.Comparator.Contracts.Models;
using JobSeek.Comparator.Contracts.Services;

namespace JobSeek.Comparator.Services;

public class AttributeComparerService : IAttributeComparerService
{
    public const double RequiredWeight = 0.8;
    public const double NiceToHaveWeight = 0.2;

    public OfferScore Score(Candidate candidate, Offer offer)
    {
        var candidateSkills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var skill in candidate.Skills)
        {
            if (!string.IsNullOrWhiteSpace(skill.Name))
                candidateSkills.Add(skill.Name);
        }

        var required = ScoreList(offer.RequiredSkills, candidateSkills);
        var niceToHave = ScoreList(offer.NiceToHaveSkills, candidateSkills);
        var score = Combine(required.Score, required.SkillCount, niceToHave.Score, niceToHave.SkillCount);

        return new OfferScore(
            offer.Id,
            offer.Slug,
            score,
            required.Score,
            niceToHave.Score,
            required.Matches,
            required.SkillCount,
            niceToHave.Matches,
            niceToHave.SkillCount);
    }

    public static AttributeComparerService CreateService() => new(); // TODO: IoC

    private double Combine(double requiredScore, int requiredCount, double niceToHaveScore, int niceToHaveCount)
    {
        if (requiredCount == 0 && niceToHaveCount == 0)
            return 0;

        if (requiredCount == 0)
            return niceToHaveScore;

        if (niceToHaveCount == 0)
            return requiredScore;

        return (RequiredWeight * requiredScore) + (NiceToHaveWeight * niceToHaveScore);
    }

    private static ListScore ScoreList(IReadOnlyList<Skill> skills, HashSet<string> candidateSkills)
    {
        var weight = 0;
        var matchedWeight = 0;
        var matches = 0;
        var counted = 0;

        foreach (var skill in skills)
        {
            if (string.IsNullOrWhiteSpace(skill.Name) || skill.Level <= 0)
                continue;

            counted++;
            weight += skill.Level;
            if (!candidateSkills.Contains(skill.Name))
                continue;

            matches++;
            matchedWeight += skill.Level;
        }

        var coverage = weight == 0 ? 0 : (double)matchedWeight / weight;
        var breadth = matches / (matches + 1.0);
        return new ListScore(coverage * breadth, matches, counted);
    }

    private readonly record struct ListScore(double Score, int Matches, int SkillCount);
}
