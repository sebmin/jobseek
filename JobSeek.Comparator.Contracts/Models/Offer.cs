namespace JobSeek.Comparator.Contracts.Models;

public sealed record Offer(
    Guid Id,
    string Slug,
    IReadOnlyList<Skill> RequiredSkills,
    IReadOnlyList<Skill> NiceToHaveSkills);
