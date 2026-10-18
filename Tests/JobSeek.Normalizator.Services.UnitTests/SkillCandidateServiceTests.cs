using JobSeek.Normalizator.Contracts.Services;
using JobSeek.Normalizator.Services.Services;

namespace JobSeek.Normalizator.Services.UnitTests;

[TestClass]
public sealed class SkillCandidateServiceTests
{
    private static ISkillCandidateService _service = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext _) =>
        _service = SkillCandidateService.CreateService(
        [
            "JavaScript",
            "Java",
            "Entity Framework",
            "Docker",
            "PostgreSQL",
            ".NET"
        ]);

    [ClassCleanup]
    public static void ClassCleanup() => _service.Dispose();

    [TestMethod]
    public void Select_InflectedJavascript_ReturnsJavaScriptWithoutUnrelatedSkills()
    {
        var result = _service.Select("Are you good at javascripcie? You are the ideal candidate.", []).ToList();

        CollectionAssert.Contains(result, "JavaScript");
        CollectionAssert.DoesNotContain(result, "Java");
        CollectionAssert.DoesNotContain(result, "PostgreSQL");
        CollectionAssert.DoesNotContain(result, "Docker");
    }

    [TestMethod]
    public void Select_EntityFrameworkPhrase_IsKept()
    {
        var result = _service.Select("Required knowledge of entity framework and SQL.", []);

        CollectionAssert.Contains(result.ToList(), "Entity Framework");
    }

    [TestMethod]
    public void Select_DotNetAlias_ReturnsCanonicalName()
    {
        var result = _service.Select("We are looking for a dotnet developer.", []);

        CollectionAssert.Contains(result.ToList(), ".NET");
    }

    [TestMethod]
    public void Select_StaticSkill_IsKeptWhenDescriptionOmitsIt()
    {
        var result = _service.Select("Remote work in a small team.", ["Docker"]);

        CollectionAssert.Contains(result.ToList(), "Docker");
    }

    [TestMethod]
    public void Select_EmptyDescription_ReturnsStaticNamesOnly()
    {
        var result = _service.Select("   ", ["Docker", "docker"]);

        CollectionAssert.AreEqual(new[] { "Docker" }, result.ToList());
    }
}
