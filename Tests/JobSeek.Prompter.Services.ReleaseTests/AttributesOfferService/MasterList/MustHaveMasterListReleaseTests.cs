using JobSeek.Categorizator.Contracts.Models.Contract;
using JobSeek.Prompter.Services.ReleaseTests.Assertions;
using JobSeek.Prompter.Services.ReleaseTests.Factories;

namespace JobSeek.Prompter.Services.ReleaseTests.AttributesOfferService.MasterList;

[TestClass]
[TestCategory("Llm")]
[TestCategory("Release-MustHave")]
public sealed class MustHaveMasterListReleaseTests
{
    [ReleaseCriterion("MH-01", "Offer id is returned and an off-list skill is dropped")]
    public async Task ExtractAttributes_ReturnsAValidSchemaForTheOfferId()
    {
        var id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");

        var result = await AttributesOfferServiceFactory.Service.ExtractAttributes(
            id,
            """
            Requirements:
            - Git
            """,
            [],
            [],
            MasterSkillList.All);

        ReleaseSkillAssertions.AssertSchema(result, id);
        ReleaseSkillAssertions.AssertNonStaticNamesAreListed(result, MasterSkillList.All);
        ReleaseSkillAssertions.AssertAbsentFrom(result.RequiredSkills, "Git");
        ReleaseSkillAssertions.AssertAbsentFrom(result.NiceToHaveSkills, "Git");
    }

    [ReleaseCriterion("MH-02", "Static required skill keeps its name and level")]
    public async Task ExtractAttributes_PreservesStaticRequiredSkillNameAndLevel()
    {
        var id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");

        var result = await AttributesOfferServiceFactory.Service.ExtractAttributes(
            id,
            """
            Requirements:
            - Git
            """,
            [new SkillDto("Angular", 4)],
            [],
            MasterSkillList.All);

        ReleaseSkillAssertions.AssertSchema(result, id);
        ReleaseSkillAssertions.AssertNonStaticNamesAreListed(result, MasterSkillList.All);
        var skill = ReleaseSkillAssertions.AssertPresent(result.RequiredSkills, "Angular");
        Assert.IsTrue(skill.isStatic);
        Assert.AreEqual(4, skill.Level);
        ReleaseSkillAssertions.AssertAbsentFrom(result.RequiredSkills, "Git");
        Assert.IsEmpty(result.NiceToHaveSkills);
    }

    [ReleaseCriterion("MH-03", "Static nice-to-have skill stays on its list")]
    public async Task ExtractAttributes_PreservesStaticNiceToHaveSkillOnItsList()
    {
        var id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3");

        var result = await AttributesOfferServiceFactory.Service.ExtractAttributes(
            id,
            """
            Requirements:
            - Java
            """,
            [],
            [new SkillDto("Redis", 2)],
            MasterSkillList.All);

        ReleaseSkillAssertions.AssertSchema(result, id);
        ReleaseSkillAssertions.AssertNonStaticNamesAreListed(result, MasterSkillList.All);
        var skill = ReleaseSkillAssertions.AssertPresent(result.NiceToHaveSkills, "Redis");
        Assert.IsTrue(skill.isStatic);
        Assert.AreEqual(2, skill.Level);
        ReleaseSkillAssertions.AssertAbsentFrom(result.RequiredSkills, "Redis");
        var java = ReleaseSkillAssertions.AssertPresent(result.RequiredSkills, "Java");
        Assert.IsFalse(java.isStatic);
    }

    [ReleaseCriterion("MH-04", "An untagged technology goes to required")]
    public async Task ExtractAttributes_PutsTechnologyOnRequiredList()
    {
        var id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4");

        var result = await AttributesOfferServiceFactory.Service.ExtractAttributes(
            id,
            """
            Wymagania:
            - PostgreSQL
            """,
            [],
            [],
            MasterSkillList.All);

        ReleaseSkillAssertions.AssertSchema(result, id);
        ReleaseSkillAssertions.AssertNonStaticNamesAreListed(result, MasterSkillList.All);
        var skill = ReleaseSkillAssertions.AssertPresent(result.RequiredSkills, "PostgreSQL");
        Assert.IsFalse(skill.isStatic);
        Assert.AreEqual("PostgreSQL", skill.Name);
        ReleaseSkillAssertions.AssertAbsentFrom(result.NiceToHaveSkills, "PostgreSQL");
    }

    [ReleaseCriterion("MH-05", "An untagged technology goes to nice-to-have")]
    public async Task ExtractAttributes_PutsTechnologyOnNiceToHaveList()
    {
        var id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5");

        var result = await AttributesOfferServiceFactory.Service.ExtractAttributes(
            id,
            """
            Wymagania:
            - Java

            Mile widziane:
            - Docker
            """,
            [],
            [],
            MasterSkillList.All);

        ReleaseSkillAssertions.AssertSchema(result, id);
        ReleaseSkillAssertions.AssertNonStaticNamesAreListed(result, MasterSkillList.All);
        var skill = ReleaseSkillAssertions.AssertPresent(result.NiceToHaveSkills, "Docker");
        Assert.IsFalse(skill.isStatic);
        Assert.AreEqual("Docker", skill.Name);
        ReleaseSkillAssertions.AssertAbsentFrom(result.RequiredSkills, "Docker");
        ReleaseSkillAssertions.AssertPresent(result.RequiredSkills, "Java");
    }

    [ReleaseCriterion("MH-06", "Static level wins over the description")]
    public async Task ExtractAttributes_StaticSkillLevelWinEvenOppositeDescriptionLevel()
    {
        var id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1");

        var result = await AttributesOfferServiceFactory.Service.ExtractAttributes(
            id,
            """
            Your role on the team:

            develop and support existing REST API modules,
            develop and maintain the frontend application in Angular,
            implement new features and modify existing solutions,
            work with the team on applications used in the financial markets area,
            collaborate on analyzing and solving technical problems,
            care for the quality and stability of the solutions being developed.

            Knowledge and qualifications:
            primary knowledge of C# / .NET

            If you heard anything about OpenShift it's a plus.
            """,
            [new SkillDto("C#", 5)],
            [],
            MasterSkillList.All);

        ReleaseSkillAssertions.AssertSchema(result, id);
        ReleaseSkillAssertions.AssertNonStaticNamesAreListed(result, MasterSkillList.All);
        var skill = ReleaseSkillAssertions.AssertPresent(result.RequiredSkills, "C#");
        Assert.IsTrue(skill.isStatic);
        Assert.AreEqual(5, skill.Level);
        Assert.AreEqual("C#", skill.Name);
    }
}
