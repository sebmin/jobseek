using JobSeek.Normalizator.Services.Providers;

namespace JobSeek.Normalizator.Services.UnitTests;

[TestClass]
public sealed class SkillCandidateFusionProviderTests
{
    private readonly SkillCandidateFusionProvider _fusion = new();

    [TestMethod]
    public void Fuse_KeepsBm25HitWhenWindowNeighborsWouldFillTheCap()
    {
        var bm25 = new[] { "terraform" };
        var windows = Enumerable.Range(1, 40).Select(index => $"skill-{index:00}").Prepend("cloud").ToList();

        var result = _fusion.Fuse([], bm25, windows, cap: 30);

        CollectionAssert.Contains(result.ToList(), "terraform");
        Assert.HasCount(30, result);
        Assert.AreEqual("terraform", result[0]);
    }

    [TestMethod]
    public void Fuse_DropsWindowOnlySkillsPastTheCap()
    {
        var windows = Enumerable.Range(1, 40).Select(index => $"skill-{index:00}").ToList();

        var result = _fusion.Fuse([], [], windows, cap: 30);

        Assert.HasCount(30, result);
        CollectionAssert.Contains(result.ToList(), "skill-01");
        CollectionAssert.DoesNotContain(result.ToList(), "skill-40");
    }

    [TestMethod]
    public void Fuse_KeepsEveryBm25HitWhenTheyExceedTheCap()
    {
        var bm25 = Enumerable.Range(1, 35).Select(index => $"skill-{index:00}").ToList();

        var result = _fusion.Fuse([], bm25, ["extra"], cap: 30);

        Assert.HasCount(35, result);
        CollectionAssert.DoesNotContain(result.ToList(), "extra");
    }

    [TestMethod]
    public void Fuse_EmptyPools_ReturnsStaticNamesOnly()
    {
        var result = _fusion.Fuse(["Docker"], [], []);

        CollectionAssert.AreEqual(new[] { "Docker" }, result.ToList());
    }
}
