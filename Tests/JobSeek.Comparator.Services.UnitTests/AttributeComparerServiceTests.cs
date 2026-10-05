using JobSeek.Comparator.Contracts.Models;
using JobSeek.Comparator.Services;

namespace JobSeek.Comparator.Services.UnitTests;

[TestClass]
public sealed class AttributeComparerServiceTests
{
    private static readonly Guid OfferId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly AttributeComparerService _comparer = AttributeComparerService.CreateService();

    [TestMethod]
    public void Score_BothListsEmpty_ReturnsZero()
    {
        var result = Score(candidate: [], required: [], nice: []);

        Assert.AreEqual(0, result.Score, Delta);
        Assert.AreEqual(0, result.RequiredSkillCount);
        Assert.AreEqual(0, result.NiceToHaveSkillCount);
    }

    [TestMethod]
    public void Score_RequiredSkillsAreInvalidAndNiceToHaveIsEmpty_ReturnsZero()
    {
        var result = Score(
            candidate: [new Skill("C#", 5)],
            required: [new Skill("", 5), new Skill("   ", 4), new Skill("Java", 0), new Skill("SQL", -1)],
            nice: []);

        Assert.AreEqual(0, result.Score, Delta);
        Assert.AreEqual(0, result.RequiredSkillCount);
        Assert.AreEqual(0, result.RequiredMatches);
    }

    [TestMethod]
    public void Score_OneRequiredSkillMatches_ReturnsHalf()
    {
        var result = Score(
            candidate: [new Skill("C#", 5)],
            required: [new Skill("C#", 5)]);

        Assert.AreEqual(0.5, result.Score, Delta);
        Assert.AreEqual(0.5, result.RequiredScore, Delta);
        Assert.AreEqual(1, result.RequiredMatches);
        Assert.AreEqual(1, result.RequiredSkillCount);
        Assert.AreEqual(0, result.NiceToHaveScore, Delta);
    }

    [TestMethod]
    public void Score_OneRequiredSkillMisses_ReturnsZeroWithCount()
    {
        var result = Score(
            candidate: [new Skill("Java", 5)],
            required: [new Skill("C#", 5)]);

        Assert.AreEqual(0, result.Score, Delta);
        Assert.AreEqual(0, result.RequiredMatches);
        Assert.AreEqual(1, result.RequiredSkillCount);
    }

    [TestMethod]
    public void Score_TwoRequiredSkillsBothMatch_ReturnsTwoThirds()
    {
        var result = Score(
            candidate: [new Skill("C#", 1), new Skill("SQL", 1)],
            required: [new Skill("C#", 5), new Skill("SQL", 1)]);

        Assert.AreEqual(2.0 / 3.0, result.Score, Delta);
        Assert.AreEqual(2, result.RequiredMatches);
        Assert.AreEqual(2, result.RequiredSkillCount);
    }

    [TestMethod]
    public void Score_OnlyHeavierRequiredSkillMatches_WeightsByLevel()
    {
        var result = Score(
            candidate: [new Skill("C#", 1)],
            required: [new Skill("C#", 5), new Skill("SQL", 1)]);

        Assert.AreEqual(5.0 / 12.0, result.Score, Delta);
        Assert.AreEqual(1, result.RequiredMatches);
        Assert.AreEqual(2, result.RequiredSkillCount);
    }

    [TestMethod]
    public void Score_OnlyLighterRequiredSkillMatches_WeightsByLevel()
    {
        var result = Score(
            candidate: [new Skill("SQL", 5)],
            required: [new Skill("C#", 5), new Skill("SQL", 1)]);

        Assert.AreEqual(1.0 / 12.0, result.Score, Delta);
        Assert.AreEqual(1, result.RequiredMatches);
    }

    [TestMethod]
    public void Score_OnlyNiceToHaveSkillMatches_UsesThatListScore()
    {
        var result = Score(
            candidate: [new Skill("Docker", 2)],
            required: [],
            nice: [new Skill("Docker", 4)]);

        Assert.AreEqual(0.5, result.Score, Delta);
        Assert.AreEqual(0.5, result.NiceToHaveScore, Delta);
        Assert.AreEqual(1, result.NiceToHaveMatches);
        Assert.AreEqual(1, result.NiceToHaveSkillCount);
        Assert.AreEqual(0, result.RequiredScore, Delta);
    }

    [TestMethod]
    public void Score_OnlyNiceToHaveSkillMisses_ReturnsZero()
    {
        var result = Score(
            candidate: [new Skill("C#", 5)],
            required: [],
            nice: [new Skill("Docker", 4)]);

        Assert.AreEqual(0, result.Score, Delta);
        Assert.AreEqual(0, result.NiceToHaveMatches);
        Assert.AreEqual(1, result.NiceToHaveSkillCount);
    }

    [TestMethod]
    public void Score_BothListsFullyMatch_CombinesEqualListScores()
    {
        var result = Score(
            candidate: [new Skill("C#", 5), new Skill("Docker", 1)],
            required: [new Skill("C#", 5)],
            nice: [new Skill("Docker", 3)]);

        Assert.AreEqual(0.5, result.RequiredScore, Delta);
        Assert.AreEqual(0.5, result.NiceToHaveScore, Delta);
        Assert.AreEqual(0.5, result.Score, Delta);
    }

    [TestMethod]
    public void Score_RequiredMissesAndNiceToHaveMatches_UsesNiceToHaveWeight()
    {
        var result = Score(
            candidate: [new Skill("Docker", 5)],
            required: [new Skill("C#", 5)],
            nice: [new Skill("Docker", 3)]);

        Assert.AreEqual(0, result.RequiredScore, Delta);
        Assert.AreEqual(0.5, result.NiceToHaveScore, Delta);
        Assert.AreEqual(0.1, result.Score, Delta);
    }

    [TestMethod]
    public void Score_RequiredMatchesAndNiceToHaveMisses_UsesRequiredWeight()
    {
        var result = Score(
            candidate: [new Skill("C#", 5)],
            required: [new Skill("C#", 5)],
            nice: [new Skill("Docker", 3)]);

        Assert.AreEqual(0.5, result.RequiredScore, Delta);
        Assert.AreEqual(0, result.NiceToHaveScore, Delta);
        Assert.AreEqual(0.4, result.Score, Delta);
    }

    [TestMethod]
    public void Score_SkillNamesMatchIgnoringCase()
    {
        var result = Score(
            candidate: [new Skill("c#", 1)],
            required: [new Skill("C#", 5)]);

        Assert.AreEqual(0.5, result.Score, Delta);
        Assert.AreEqual(1, result.RequiredMatches);
    }

    [TestMethod]
    public void Score_WhitespaceOfferSkillName_IsSkipped()
    {
        var result = Score(
            candidate: [new Skill("C#", 5)],
            required: [new Skill("   ", 5), new Skill("C#", 2)]);

        Assert.AreEqual(0.5, result.Score, Delta);
        Assert.AreEqual(1, result.RequiredSkillCount);
        Assert.AreEqual(1, result.RequiredMatches);
    }

    [TestMethod]
    public void Score_NonPositiveOfferLevels_AreSkipped()
    {
        var result = Score(
            candidate: [new Skill("C#", 5)],
            required: [new Skill("Java", 0), new Skill("SQL", -3), new Skill("C#", 3)]);

        Assert.AreEqual(0.5, result.Score, Delta);
        Assert.AreEqual(1, result.RequiredSkillCount);
    }

    [TestMethod]
    public void Score_BlankCandidateSkillName_DoesNotMatch()
    {
        var result = Score(
            candidate: [new Skill("", 5), new Skill("   ", 3)],
            required: [new Skill("C#", 5)]);

        Assert.AreEqual(0, result.Score, Delta);
        Assert.AreEqual(0, result.RequiredMatches);
        Assert.AreEqual(1, result.RequiredSkillCount);
    }

    [TestMethod]
    public void Score_CandidateLevel_IsIgnored()
    {
        var result = Score(
            candidate: [new Skill("C#", 1)],
            required: [new Skill("C#", 5)]);

        Assert.AreEqual(0.5, result.Score, Delta);
        Assert.AreEqual(1, result.RequiredMatches);
    }

    [TestMethod]
    public void Score_DuplicateCandidateSkillNames_CountOnce()
    {
        var result = Score(
            candidate: [new Skill("C#", 1), new Skill("C#", 5)],
            required: [new Skill("C#", 4)]);

        Assert.AreEqual(0.5, result.Score, Delta);
        Assert.AreEqual(1, result.RequiredMatches);
        Assert.AreEqual(1, result.RequiredSkillCount);
    }

    [TestMethod]
    public void Score_DuplicateOfferSkillNames_BothCount()
    {
        var result = Score(
            candidate: [new Skill("C#", 1)],
            required: [new Skill("C#", 2), new Skill("C#", 4)]);

        Assert.AreEqual(2.0 / 3.0, result.Score, Delta);
        Assert.AreEqual(2, result.RequiredMatches);
        Assert.AreEqual(2, result.RequiredSkillCount);
    }

    [TestMethod]
    public void Score_CopiesIdentityAndCountsValidSkillsSeparately()
    {
        var result = Score(
            candidate: [new Skill("C#", 1), new Skill("SQL", 2)],
            required: [new Skill("", 9), new Skill("Java", 0), new Skill("C#", 5), new Skill("Go", 3)],
            nice: [new Skill(" ", 2), new Skill("SQL", 4)],
            slug: "backend-developer");

        Assert.AreEqual(OfferId, result.Id);
        Assert.AreEqual("backend-developer", result.Slug);
        Assert.AreEqual(5.0 / 16.0, result.RequiredScore, Delta);
        Assert.AreEqual(0.5, result.NiceToHaveScore, Delta);
        Assert.AreEqual(0.35, result.Score, Delta);
        Assert.AreEqual(1, result.RequiredMatches);
        Assert.AreEqual(2, result.RequiredSkillCount);
        Assert.AreEqual(1, result.NiceToHaveMatches);
        Assert.AreEqual(1, result.NiceToHaveSkillCount);
    }

    [TestMethod]
    public void Score_ThreeRequiredSkillsAllMatch_ReturnsThreeQuarters()
    {
        var result = Score(
            candidate: [new Skill("C#", 1), new Skill("SQL", 1), new Skill("Docker", 1)],
            required: [new Skill("C#", 1), new Skill("SQL", 2), new Skill("Docker", 3)]);

        Assert.AreEqual(0.75, result.Score, Delta);
        Assert.AreEqual(3, result.RequiredMatches);
        Assert.AreEqual(3, result.RequiredSkillCount);
    }

    [TestMethod]
    public void Score_TwoOfThreeRequiredSkillsMatch_WeightsMatchedLevels()
    {
        var result = Score(
            candidate: [new Skill("C#", 1), new Skill("SQL", 1)],
            required: [new Skill("C#", 5), new Skill("SQL", 3), new Skill("Go", 1)]);

        Assert.AreEqual(16.0 / 27.0, result.Score, Delta);
        Assert.AreEqual(2, result.RequiredMatches);
        Assert.AreEqual(3, result.RequiredSkillCount);
    }

    [TestMethod]
    public void Score_EqualRequiredLevelsWithOneMatch_ReturnsQuarter()
    {
        var result = Score(
            candidate: [new Skill("C#", 1)],
            required: [new Skill("C#", 4), new Skill("Java", 4)]);

        Assert.AreEqual(0.25, result.Score, Delta);
        Assert.AreEqual(1, result.RequiredMatches);
        Assert.AreEqual(2, result.RequiredSkillCount);
    }

    [TestMethod]
    public void Score_ThreeOfFourEqualRequiredSkillsMatch_ReturnsNineSixteenths()
    {
        var result = Score(
            candidate: [new Skill("C#", 1), new Skill("SQL", 1), new Skill("Docker", 1)],
            required: [new Skill("C#", 2), new Skill("SQL", 2), new Skill("Docker", 2), new Skill("Go", 2)]);

        Assert.AreEqual(9.0 / 16.0, result.Score, Delta);
        Assert.AreEqual(3, result.RequiredMatches);
        Assert.AreEqual(4, result.RequiredSkillCount);
    }

    [TestMethod]
    public void Score_NoCandidateSkills_ScoresThreeRequiredSkillsAsZero()
    {
        var result = Score(
            candidate: [],
            required: [new Skill("C#", 5), new Skill("SQL", 3), new Skill("Go", 1)]);

        Assert.AreEqual(0, result.Score, Delta);
        Assert.AreEqual(0, result.RequiredMatches);
        Assert.AreEqual(3, result.RequiredSkillCount);
    }

    [TestMethod]
    public void Score_OnlyHeavierNiceToHaveSkillMatches_ReturnsOneThird()
    {
        var result = Score(
            candidate: [new Skill("Docker", 1)],
            required: [],
            nice: [new Skill("Docker", 4), new Skill("Kubernetes", 2)]);

        Assert.AreEqual(1.0 / 3.0, result.Score, Delta);
        Assert.AreEqual(1.0 / 3.0, result.NiceToHaveScore, Delta);
        Assert.AreEqual(0, result.RequiredSkillCount);
        Assert.AreEqual(1, result.NiceToHaveMatches);
        Assert.AreEqual(2, result.NiceToHaveSkillCount);
    }

    [TestMethod]
    public void Score_InvalidRequiredSkills_DoNotBlendWithMatchingNiceToHave()
    {
        var result = Score(
            candidate: [new Skill("SQL", 5)],
            required: [new Skill("", 5), new Skill("Java", 0)],
            nice: [new Skill("SQL", 2)]);

        Assert.AreEqual(0.5, result.Score, Delta);
        Assert.AreEqual(0, result.RequiredSkillCount);
        Assert.AreEqual(1, result.NiceToHaveMatches);
    }

    [TestMethod]
    public void Score_InvalidNiceToHaveSkills_DoNotBlendWithPartialRequired()
    {
        var result = Score(
            candidate: [new Skill("C#", 1)],
            required: [new Skill("C#", 5), new Skill("Java", 5)],
            nice: [new Skill("   ", 4), new Skill("Docker", -1)]);

        Assert.AreEqual(0.25, result.Score, Delta);
        Assert.AreEqual(0.25, result.RequiredScore, Delta);
        Assert.AreEqual(0, result.NiceToHaveSkillCount);
    }

    [TestMethod]
    public void Score_PartialRequiredAndPartialNiceToHave_CombinesWeights()
    {
        var result = Score(
            candidate: [new Skill("C#", 1), new Skill("Docker", 1)],
            required: [new Skill("C#", 4), new Skill("Java", 2)],
            nice: [new Skill("Docker", 3), new Skill("AWS", 3)]);

        Assert.AreEqual(1.0 / 3.0, result.RequiredScore, Delta);
        Assert.AreEqual(0.25, result.NiceToHaveScore, Delta);
        Assert.AreEqual(19.0 / 60.0, result.Score, Delta);
    }

    [TestMethod]
    public void Score_FullRequiredPairAndFullNiceToHave_CombinesWeights()
    {
        var result = Score(
            candidate: [new Skill("C#", 1), new Skill("SQL", 1), new Skill("Docker", 1)],
            required: [new Skill("C#", 5), new Skill("SQL", 1)],
            nice: [new Skill("Docker", 4)]);

        Assert.AreEqual(2.0 / 3.0, result.RequiredScore, Delta);
        Assert.AreEqual(0.5, result.NiceToHaveScore, Delta);
        Assert.AreEqual(19.0 / 30.0, result.Score, Delta);
    }

    private const double Delta = 1e-9;

    private OfferScore Score(Skill[] candidate, Skill[] required, Skill[]? nice = null, string slug = "offer") =>
        _comparer.Score(
            new Candidate(candidate),
            new Offer(OfferId, slug, required, nice ?? []));
}
