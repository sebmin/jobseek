using JobSeek.Categorizator.Contracts.Models.Contract;
using JobSeek.Prompter.Services.ReleaseTests.Assertions;
using JobSeek.Prompter.Services.ReleaseTests.Factories;

namespace JobSeek.Prompter.Services.ReleaseTests.AttributesOfferService;

[TestClass]
[TestCategory("Llm")]
[TestCategory("Release-MustHave")]
public sealed class MustHaveReleaseTests
{
    [ReleaseCriterion("MH-01", "Offer id is returned in the schema")]
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
            []);

        ReleaseSkillAssertions.AssertSchema(result, id);
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
            []);

        ReleaseSkillAssertions.AssertSchema(result, id);
        var skill = ReleaseSkillAssertions.AssertPresent(result.RequiredSkills, "Angular");
        Assert.IsTrue(skill.isStatic);
        Assert.AreEqual(4, skill.Level);
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
            [new SkillDto("Redis", 2)]);

        ReleaseSkillAssertions.AssertSchema(result, id);
        var skill = ReleaseSkillAssertions.AssertPresent(result.NiceToHaveSkills, "Redis");
        Assert.IsTrue(skill.isStatic);
        Assert.AreEqual(2, skill.Level);
        ReleaseSkillAssertions.AssertAbsentFrom(result.RequiredSkills, "Redis");
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
            []);

        ReleaseSkillAssertions.AssertSchema(result, id);
        var skill = ReleaseSkillAssertions.AssertPresent(result.RequiredSkills, "PostgreSQL");
        Assert.IsFalse(skill.isStatic);
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
            []);

        ReleaseSkillAssertions.AssertSchema(result, id);
        var skill = ReleaseSkillAssertions.AssertPresent(result.NiceToHaveSkills, "Docker");
        Assert.IsFalse(skill.isStatic);
        ReleaseSkillAssertions.AssertAbsentFrom(result.RequiredSkills, "Docker");
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
            [new SkillDto("c#", 5)],
            []);

        ReleaseSkillAssertions.AssertSchema(result, id);
        ReleaseSkillAssertions.AssertPresentWithLevel(result.RequiredSkills, "c#", 5);
    }
}
