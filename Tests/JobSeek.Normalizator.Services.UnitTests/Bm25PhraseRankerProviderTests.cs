using JobSeek.Normalizator.Services.Providers;

namespace JobSeek.Normalizator.Services.UnitTests;

[TestClass]
public sealed class Bm25PhraseRankerProviderTests
{
    private readonly SkillTextProvider _text = new();
    private readonly Bm25PhraseRankerProvider _ranker = new();

    [TestMethod]
    public void Rank_RequiresAContiguousPhrase()
    {
        var document = _text.SplitToWords("entity or framework");

        var ranked = _ranker.Rank(document, [("Entity Framework", ["entity", "framework"])]);

        Assert.IsEmpty(ranked);
    }

    [TestMethod]
    public void Rank_ContiguousPhrase()
    {
        var document = _text.SplitToWords("Entity Framework");

        var ranked = _ranker.Rank(document, [("Entity Framework", ["entity", "framework"])]);

        CollectionAssert.AreEqual(new[] { "Entity Framework" }, ranked.ToList());
    }

    [TestMethod]
    public void Rank_MapsAnAliasPhraseToTheCanonicalName()
    {
        var document = _text.SplitToWords("I am looking for developer for dotnet.");

        var ranked = _ranker.Rank(document, [(".NET", ["dotnet"])]);

        CollectionAssert.AreEqual(new[] { ".NET" }, ranked.ToList());
    }

    [TestMethod]
    public void Rank_LongerDescription_ReturnsMentionedSkillsInScoreOrder()
    {
        var description = """
            We are hiring a senior backend developer for a product team.
            Requirements: C# and .NET with entity framework.
            The stack also includes javascript/typescript, docker, and kubernetes.
            Docker is used to run integration tests.
            Candidates should know SQL.
            Experience with react is welcome, and native mobile work is a separate team.
            """;
        var document = _text.SplitToWords(description);
        (string Canonical, string[] Words)[] queries =
        [
            ("C#", _text.SplitToWords("C#").ToArray()),
            (".NET", _text.SplitToWords(".NET").ToArray()),
            ("Entity Framework", _text.SplitToWords("Entity Framework").ToArray()),
            ("JavaScript", _text.SplitToWords("JavaScript").ToArray()),
            ("TypeScript", _text.SplitToWords("TypeScript").ToArray()),
            ("Docker", _text.SplitToWords("Docker").ToArray()),
            ("Kubernetes", _text.SplitToWords("Kubernetes").ToArray()),
            ("SQL", _text.SplitToWords("SQL").ToArray()),
            ("React Native", _text.SplitToWords("React Native").ToArray()),
            ("PostgreSQL", _text.SplitToWords("PostgreSQL").ToArray())
        ];

        var ranked = _ranker.Rank(document, queries).ToList();

        CollectionAssert.AreEqual(
            new[]
            {
                "Entity Framework",
                "Docker",
                ".NET",
                "C#",
                "JavaScript",
                "Kubernetes",
                "SQL",
                "TypeScript"
            },
            ranked);
    }
}
