using JobSeek.Normalizator.Contracts.Services;
using JobSeek.Normalizator.Services.Services;

namespace JobSeek.Normalizator.Services.UnitTests;

[TestClass]
public sealed class SkillNormalizationServiceTests
{
    private static ISkillNormalizationService _service = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext _) =>
        _service = SkillNormalizationService.CreateService(
        [
            "JavaScript",
            "Java",
            "React",
            "Docker",
            "Kubernetes",
            "PostgreSQL",
            "unit tests",
            "bazy relacyjne",
            "C#",
            "C++"
        ]);

    [ClassCleanup]
    public static void ClassCleanup() => _service.Dispose();

    [TestMethod]
    public void CreateNormalizedSkill_Null_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, _service.CreateNormalizedSkill(null!));
    }

    [TestMethod]
    public void CreateNormalizedSkill_Empty_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, _service.CreateNormalizedSkill(""));
    }

    [TestMethod]
    public void CreateNormalizedSkill_Whitespace_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, _service.CreateNormalizedSkill(" \t "));
    }

    [TestMethod]
    public void CreateNormalizedSkill_ExactMatch_ReturnsMasterCasing()
    {
        Assert.AreEqual("JavaScript", _service.CreateNormalizedSkill("javascript"));
    }

    [TestMethod]
    public void CreateNormalizedSkill_PaddedExactMatch_ReturnsMasterName()
    {
        Assert.AreEqual("React", _service.CreateNormalizedSkill("  React  "));
    }

    [TestMethod]
    public void CreateNormalizedSkill_ExactMatch_WinsOverSimilarMaster()
    {
        Assert.AreEqual("Java", _service.CreateNormalizedSkill("java"));
    }

    [TestMethod]
    public void CreateNormalizedSkill_CSharp_DoesNotBecomeCPlusPlus()
    {
        Assert.AreEqual("C#", _service.CreateNormalizedSkill("c#"));
    }

    [TestMethod]
    public void CreateNormalizedSkill_CPlusPlus_KeepsItsMasterName()
    {
        Assert.AreEqual("C++", _service.CreateNormalizedSkill("c++"));
    }

    [TestMethod]
    public void CreateNormalizedSkill_Typo_ReturnsMasterName()
    {
        Assert.AreEqual("Kubernetes", _service.CreateNormalizedSkill("kuberntes"));
    }

    [TestMethod]
    public void CreateNormalizedSkill_PrefixTypo_ReturnsPostgreSql()
    {
        Assert.AreEqual("PostgreSQL", _service.CreateNormalizedSkill("postgre"));
    }

    [TestMethod]
    [DataRow("Javascripta", "JavaScript")]
    [DataRow("Javascrip", "JavaScript")]
    [DataRow("c+", "C++")]
    [DataRow("c+++", "C++")]
    public void CreateNormalizedSkill_PrefixTypo_ReturnsProperName(string rawTypoSkill, string normalizedSKill)
    {
        Assert.AreEqual(normalizedSKill, _service.CreateNormalizedSkill(rawTypoSkill));
    }

    [TestMethod]
    [DataRow("c sharp", "C#")]
    [DataRow("c plus plus", "C++")]
    public void CreateNormalizedSkill_Alias_ReturnsProperName(string rawTypoSkill, string normalizedSKill)
    {
        Assert.AreEqual(normalizedSKill, _service.CreateNormalizedSkill(rawTypoSkill));
    }

    [TestMethod]
    public void CreateNormalizedSkill_Alias_ReturnsJavaScript()
    {
        Assert.AreEqual("JavaScript", _service.CreateNormalizedSkill("JS"));
    }

    [TestMethod]
    public void CreateNormalizedSkill_NearPhrase_ReturnsUnitTests()
    {
        Assert.AreEqual("unit tests", _service.CreateNormalizedSkill("unit testing"));
    }

    [TestMethod]
    public void CreateNormalizedSkill_Translation_ReturnsPolishMasterName()
    {
        Assert.AreEqual("bazy relacyjne", _service.CreateNormalizedSkill("relational databases"));
    }

    [TestMethod]
    public void CreateNormalizedSkill_UnrelatedSkill_ReturnsTrimmedRawName()
    {
        Assert.AreEqual("gardening", _service.CreateNormalizedSkill("  gardening  "));
    }

    [TestMethod]
    public void CreateNormalizedSkill_BelowCutoff_DoesNotReturnNearestMaster()
    {
        Assert.AreEqual("gardening", _service.CreateNormalizedSkill("gardening"));
    }

    [TestMethod]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var service = SkillNormalizationService.CreateService(["React"]);

        service.Dispose();
        service.Dispose();
    }
}
