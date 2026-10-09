using JobSeek.Prompter.Contracts.Models;

namespace JobSeek.Prompter.Services.ReleaseTests.Assertions;

internal static class ReleaseSkillAssertions
{
    public static void AssertSchema(OfferAttributesDto result, Guid id)
    {
        Assert.AreEqual(id, result.Id);

        var skills = result.RequiredSkills.Concat(result.NiceToHaveSkills).ToList();
        foreach (var skill in skills)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(skill.Name), "Skill name is empty.");
            Assert.IsTrue(skill.Level is >= 1 and <= 5, $"{skill.Name} level {skill.Level} is outside 1..5.");
        }

        var overlap = result.RequiredSkills
            .Select(skill => Normalize(skill.Name))
            .Intersect(result.NiceToHaveSkills.Select(skill => Normalize(skill.Name)))
            .ToList();
        Assert.IsEmpty(overlap, "Skill on both lists: " + string.Join(", ", overlap));
    }

    public static OfferSkillDto AssertPresent(IReadOnlyList<OfferSkillDto> skills, string name)
    {
        var match = skills.FirstOrDefault(skill => SameSkill(skill.Name, name));
        Assert.IsNotNull(match, $"Missing '{name}'. Actual: {Format(skills)}");
        return match!;
    }

    public static void AssertPresentWithLevel(IReadOnlyList<OfferSkillDto> skills, string name, int level)
    {
        var match = skills.FirstOrDefault(skill => SameSkill(skill.Name, name));
        Assert.IsNotNull(match, $"Missing '{name}'. Actual: {Format(skills)}");
        Assert.AreEqual(level, match.Level, $"Wrong level for '{name}'. Actual: {match.Level}, Expected: {level}");
    }

    public static void AssertAbsentFrom(IReadOnlyList<OfferSkillDto> skills, string name)
    {
        var match = skills.FirstOrDefault(skill => SameSkill(skill.Name, name));
        Assert.IsNull(match, $"'{name}' is present as '{match?.Name}'.");
    }

    public static void AssertTextAbsent(OfferAttributesDto result, params string[] forbidden)
    {
        var names = result.RequiredSkills.Concat(result.NiceToHaveSkills).Select(skill => skill.Name).ToList();
        foreach (var word in forbidden)
        {
            var hit = names.FirstOrDefault(name => Normalize(name).Contains(Normalize(word), StringComparison.Ordinal));
            Assert.IsNull(hit, $"'{word}' was extracted as '{hit}'.");
        }
    }

    public static void AssertNoPolishDiacritics(OfferAttributesDto result)
    {
        const string Polish = "ąćęłńóśźżĄĆĘŁŃÓŚŹŻ";
        foreach (var skill in result.RequiredSkills.Concat(result.NiceToHaveSkills))
        {
            var hit = skill.Name.FirstOrDefault(letter => Polish.Contains(letter));
            Assert.AreEqual('\0', hit, $"'{skill.Name}' is not an English technical name.");
        }
    }

    private static bool SameSkill(string actual, string expected)
    {
        var left = Normalize(actual);
        var right = Normalize(expected);
        return left == right || left.Split(' ').Contains(right, StringComparer.Ordinal);
    }

    private static string Normalize(string name) =>
        string.Join(' ', name.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

    private static string Format(IReadOnlyList<OfferSkillDto> skills) =>
        skills.Count == 0
            ? "(none)"
            : string.Join(", ", skills.Select(skill => $"{skill.Name} L{skill.Level}"));
}
