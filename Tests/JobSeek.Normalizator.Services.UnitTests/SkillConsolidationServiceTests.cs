using JobSeek.Normalizator.Services;

namespace JobSeek.Normalizator.Services.UnitTests;

[TestClass]
public sealed class SkillConsolidationServiceTests
{
    private static SkillConsolidationService _service = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext _) => _service = SkillConsolidationService.CreateService();

    [ClassCleanup]
    public static void ClassCleanup() => _service.Dispose();

    [TestMethod]
    public void CreateMasterList_EmptyList_ReturnsEmpty()
    {
        var result = _service.CreateMasterList([]);

        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void CreateMasterList_Null_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _service.CreateMasterList(null!));
    }

    [TestMethod]
    public void CreateMasterList_OneSkill_ReturnsTrimmedLowercase()
    {
        var result = _service.CreateMasterList(["  Entity Framework  "]);

        CollectionAssert.AreEqual(new[] { "entity framework" }, result);
    }

    [TestMethod]
    public void CreateMasterList_CaseAndWhitespaceVariants_CollapseToOne()
    {
        var result = _service.CreateMasterList(["  C# ", "c#", "C#"]);

        CollectionAssert.AreEqual(new[] { "c#" }, result);
    }

    [TestMethod]
    public void CreateMasterList_BlankNames_CollapseToOneEmptyName()
    {
        var result = _service.CreateMasterList(["", "   ", "\t"]);

        CollectionAssert.AreEqual(new[] { "" }, result);
    }

    [TestMethod]
    public void CreateMasterList_MostFrequentSkill_ComesFirst()
    {
        var result = _service.CreateMasterList(["Docker", "React", "React", "React"]);

        CollectionAssert.AreEqual(new[] { "react", "docker" }, result);
    }

    [TestMethod]
    public void CreateMasterList_Counts_OrderByFrequency()
    {
        var result = _service.CreateMasterList(
            ["Docker", "SQL", "React", "React", "SQL", "React"]);

        CollectionAssert.AreEqual(new[] { "react", "sql", "docker" }, result);
    }

    [TestMethod]
    public void CreateMasterList_EqualCounts_KeepFirstSeenOrder()
    {
        var result = _service.CreateMasterList(["Kubernetes", "PostgreSQL", "Kubernetes", "PostgreSQL"]);

        CollectionAssert.AreEqual(new[] { "kubernetes", "postgresql" }, result);
    }

    [TestMethod]
    public void CreateMasterList_UnrelatedSkills_KeepsBoth()
    {
        var result = _service.CreateMasterList(["React", "Kubernetes"]);

        CollectionAssert.AreEqual(new[] { "react", "kubernetes" }, result);
    }

    [TestMethod]
    [DataRow("Javascript", "JS")]
    [DataRow("Unit tests", "Unit testing")]
    public void CreateMasterList_RelatedSkills_KeepsFirstOne(string skillA, string skillB)
    {
        var result = _service.CreateMasterList([skillA, skillB]);

        CollectionAssert.AreEqual(new[] { skillA.ToLower() }, result);
    }

    [TestMethod]
    [DataRow("Bazy relacyjne", "Relational databases")]
    public void CreateMasterList_RelatedMultiLanguagesSkills_KeepsFirstOne(string skillA, string skillB)
    {
        var result = _service.CreateMasterList([skillA, skillB]);

        CollectionAssert.AreEqual(new[] { skillA.ToLower() }, result);
    }

    [TestMethod]
    [DataRow("Java", "Javascript")]
    [DataRow("c#", "c++")]
    public void CreateMasterList_NotRelatedButSimilarSkills_KeepsBoth(string skillA, string skillB)
    {
        var result = _service.CreateMasterList([skillA, skillB]);

        CollectionAssert.AreEqual(new[] { skillA.ToLower(), skillB.ToLower() }, result);
    }

    [TestMethod]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var service = SkillConsolidationService.CreateService();

        service.Dispose();
        service.Dispose();
    }
}
