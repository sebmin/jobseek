using JobSeek.Categorizator.Contracts.Models.Contract;
using JobSeek.Comparator.Contracts.Models;
using JobSeek.Comparator.Services;
using JobSeek.Normalizator.Contracts.Services;
using JobSeek.Normalizator.Services.Services;
using JobSeek.Prompter.Contracts.Models;
using JobSeek.Prompter.Contracts.Services;

namespace JobSeek.Pipeline.IntegrationTests;

[TestClass]
public sealed class OfferScoringPipelineTests
{
    private static readonly string[] MasterSkills =
    [
        "C#",
        ".NET",
        "Entity Framework",
        "Docker",
        "PostgreSQL",
        "JavaScript",
        "TypeScript",
        "Kubernetes",
        "React",
        "SQL",
        "Java",
        "Go",
        "Python",
        "Azure",
        "Redis"
    ];

    private static ISkillCandidateService _candidates = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext _) =>
        _candidates = SkillCandidateService.CreateService(MasterSkills.ToList());

    [ClassCleanup]
    public static void ClassCleanup() => _candidates.Dispose();

    [TestMethod]
    public async Task ScoreBatch_RanksTheMatchingOfferAbovePartialAndUnrelatedOffers()
    {
        var offers = SampleOffers();
        var candidate = new Candidate(
        [
            new Skill("C#", 4),
            new Skill(".NET", 4),
            new Skill("Entity Framework", 3)
        ]);

        var (ranked, extractor) = await ScoreAsync(offers, candidate);

        Assert.HasCount(offers.Count, extractor.Calls);
        Assert.AreEqual(offers.Count, extractor.Calls.Select(call => call.Id).Distinct().Count());
        Assert.IsTrue(extractor.Calls.All(call => call.Shortlist.Count < MasterSkills.Length));

        var matchingCall = extractor.Calls.Single(call => call.Id == offers[0].Id);
        CollectionAssert.IsSubsetOf(new[] { "C#", ".NET", "Entity Framework" }, matchingCall.Shortlist.ToList());

        Assert.AreEqual("backend-dotnet", ranked[0].Slug);
        Assert.IsGreaterThan(0, ranked.FindIndex(result => result.Slug == "docker-platform"));
        Assert.AreEqual("zzz-office", ranked[^1].Slug);
        AssertScoresDescend(ranked);
    }

    [TestMethod]
    public async Task ScoreBatch_KeepsAStaticSkillWhenTheDescriptionOmitsIt()
    {
        var offer = new SampleOffer(
            Guid.NewGuid(),
            "remote-team",
            "Remote work in a small team.",
            [new OfferSkillDto("Docker", 3, true)],
            ["Docker"]);

        var (_, extractor) = await ScoreAsync([offer], BackendCandidate());
        var call = extractor.Calls.Single();

        CollectionAssert.Contains(call.Shortlist.ToList(), "Docker");
        Assert.IsGreaterThan(call.Shortlist.Count, MasterSkills.Length);
    }

    [TestMethod]
    public async Task ScoreBatch_ShortlistKeepsAliasAndInflectedSkillNames()
    {
        var offer = new SampleOffer(
            Guid.NewGuid(),
            "alias-role",
            "We are looking for a dotnet developer. Are you good at javascripcie?",
            [new OfferSkillDto(".NET", 4, false), new OfferSkillDto("JavaScript", 3, false)],
            []);

        var (_, extractor) = await ScoreAsync([offer], BackendCandidate());
        var shortlist = extractor.Calls.Single().Shortlist.ToList();

        CollectionAssert.Contains(shortlist, ".NET");
        CollectionAssert.Contains(shortlist, "JavaScript");
        CollectionAssert.DoesNotContain(shortlist, "Java");
        Assert.IsGreaterThan(shortlist.Count, MasterSkills.Length);
    }

    [TestMethod]
    public async Task ScoreBatch_RanksAHighLevelMatchAboveADilutedLowLevelMatch()
    {
        var offers = new List<SampleOffer>
        {
            new(
                Guid.NewGuid(),
                "high-csharp",
                "The service is written in C# and runs on Docker.",
                [new OfferSkillDto("C#", 5, false), new OfferSkillDto("Docker", 1, false)],
                []),
            new(
                Guid.NewGuid(),
                "low-csharp",
                "The service is written in C# and runs on Docker.",
                [new OfferSkillDto("C#", 1, false), new OfferSkillDto("Docker", 5, false)],
                [])
        };

        var (ranked, _) = await ScoreAsync(offers, BackendCandidate());

        Assert.AreEqual("high-csharp", ranked[0].Slug);
        Assert.AreEqual("low-csharp", ranked[1].Slug);
        Assert.IsGreaterThan(ranked[1].Score, ranked[0].Score);
        AssertScoresDescend(ranked);
    }

    private static Candidate BackendCandidate() =>
        new(
        [
            new Skill("C#", 4),
            new Skill(".NET", 4),
            new Skill("Entity Framework", 3)
        ]);

    private static async Task<(List<OfferScore> Ranked, RecordingExtractor Extractor)> ScoreAsync(
        IReadOnlyList<SampleOffer> offers,
        Candidate candidate)
    {
        var extractor = new RecordingExtractor(offers);
        var comparer = AttributeComparerService.CreateService();
        var scores = new List<OfferScore>();

        foreach (var offer in offers)
        {
            var shortlist = _candidates.Select(offer.Description, offer.StaticSkillNames);
            var extracted = await extractor.ExtractAttributes(
                offer.Id,
                offer.Description,
                [],
                [],
                shortlist.ToList());

            scores.Add(comparer.Score(candidate, ToOffer(offer.Slug, extracted)));
        }

        var ranked = scores
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Slug, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return (ranked, extractor);
    }

    private static void AssertScoresDescend(IReadOnlyList<OfferScore> ranked)
    {
        for (var index = 1; index < ranked.Count; index++)
            Assert.IsGreaterThanOrEqualTo(ranked[index].Score, ranked[index - 1].Score);
    }

    private static List<SampleOffer> SampleOffers()
    {
        var offers = new List<SampleOffer>
        {
            new(
                Guid.NewGuid(),
                "backend-dotnet",
                "We use C# and .NET with entity framework.",
                [new OfferSkillDto("C#", 4, false), new OfferSkillDto(".NET", 4, false), new OfferSkillDto("Entity Framework", 3, false)],
                []),
            new(
                Guid.NewGuid(),
                "docker-platform",
                "The platform runs on Docker.",
                [new OfferSkillDto("Docker", 3, false)],
                []),
            new(
                Guid.NewGuid(),
                "zzz-office",
                "We answer customer calls and schedule meetings.",
                [new OfferSkillDto("Spreadsheet", 2, false)],
                [])
        };

        for (var index = 1; index <= 7; index++)
        {
            offers.Add(new SampleOffer(
                Guid.NewGuid(),
                $"csharp-team-{index}",
                "The service is written in C#.",
                [new OfferSkillDto("C#", 1, false)],
                []));
        }

        return offers;
    }

    private static Offer ToOffer(string slug, OfferAttributesDto extracted) =>
        new(
            extracted.Id,
            slug,
            extracted.RequiredSkills.Select(skill => new Skill(skill.Name, skill.Level)).ToArray(),
            extracted.NiceToHaveSkills.Select(skill => new Skill(skill.Name, skill.Level)).ToArray());

    private sealed record SampleOffer(
        Guid Id,
        string Slug,
        string Description,
        IReadOnlyList<OfferSkillDto> RequiredSkills,
        IReadOnlyList<string> StaticSkillNames);

    private sealed class RecordingExtractor : IAttributesOfferService
    {
        private readonly Dictionary<Guid, SampleOffer> _offers;

        public RecordingExtractor(IEnumerable<SampleOffer> offers)
        {
            _offers = offers.ToDictionary(offer => offer.Id);
        }

        public List<(Guid Id, IReadOnlyList<string> Shortlist)> Calls { get; } = [];

        public Task<OfferAttributesDto> ExtractAttributes(
            Guid id,
            string description,
            List<SkillDto> requiredStaticSkills,
            List<SkillDto> niceToHaveStaticSkills,
            List<string> databaseMasterList)
        {
            Calls.Add((id, databaseMasterList));
            var offer = _offers[id];
            return Task.FromResult(new OfferAttributesDto(id, offer.RequiredSkills, []));
        }

        public Task<OfferAttributesDto> ExtractAttributes(
            Guid id,
            string description,
            List<SkillDto> requiredStaticSkills,
            List<SkillDto> niceToHaveStaticSkills) =>
            throw new NotSupportedException();

        public Task<OfferAttributesDto> ExtractRawAttributes(
            Guid id,
            string description,
            List<SkillDto> requiredStaticSkills,
            List<SkillDto> niceToHaveStaticSkills,
            List<string> databaseMasterList) =>
            throw new NotSupportedException();
    }
}
