using JobSeek.Categorizator.Contracts.Models.Contract;
using JobSeek.Prompter.Services.ReleaseTests.Assertions;
using JobSeek.Prompter.Services.ReleaseTests.Factories;

namespace JobSeek.Prompter.Services.ReleaseTests.AttributesOfferService.MasterList;

[TestClass]
[TestCategory("Llm")]
[TestCategory("Release-ShouldHave")]
public sealed class ShouldHaveMasterListReleaseTests
{
    [ReleaseCriterion("SH-01", "Benefits, soft skills, and spoken languages are dropped")]
    public async Task ExtractAttributes_DropsBenefitsSoftSkillsAndSpokenLanguages()
    {
        var id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");

        var result = await AttributesOfferServiceFactory.Service.ExtractAttributes(
            id,
            """
            Requirements:
            - Kotlin

            We offer private medical care and a multisport card.
            We are looking for a communicative team player.
            English is required.
            """,
            [],
            [],
            MasterSkillList.All);

        ReleaseSkillAssertions.AssertSchema(result, id);
        ReleaseSkillAssertions.AssertNonStaticNamesAreListed(result, MasterSkillList.All);
        var skill = ReleaseSkillAssertions.AssertPresent(result.RequiredSkills, "Kotlin");
        Assert.IsFalse(skill.isStatic);
        Assert.AreEqual("Kotlin", skill.Name);
        ReleaseSkillAssertions.AssertTextAbsent(
            result,
            "medical",
            "multisport",
            "communicative",
            "team player",
            "english");
    }

    [ReleaseCriterion("SH-02", "An untagged technical skill is still extracted")]
    public async Task ExtractAttributes_AddsTechnicalSkillThatIsNotInTheStaticTags()
    {
        var id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");

        var result = await AttributesOfferServiceFactory.Service.ExtractAttributes(
            id,
            """
            Requirements:
            - C#
            - RabbitMQ
            """,
            [new SkillDto("C#", 3)],
            [],
            MasterSkillList.All);

        ReleaseSkillAssertions.AssertSchema(result, id);
        ReleaseSkillAssertions.AssertNonStaticNamesAreListed(result, MasterSkillList.All);
        var skill = ReleaseSkillAssertions.AssertPresent(result.RequiredSkills, "RabbitMQ");
        Assert.IsFalse(skill.isStatic);
        Assert.AreEqual("RabbitMQ", skill.Name);
        ReleaseSkillAssertions.AssertAbsentFrom(result.NiceToHaveSkills, "RabbitMQ");
        var staticSkill = ReleaseSkillAssertions.AssertPresent(result.RequiredSkills, "C#");
        Assert.IsTrue(staticSkill.isStatic);
        Assert.AreEqual(3, staticSkill.Level);
    }

    [ReleaseCriterion("SH-03", "A named-only skill is not treated as senior")]
    public async Task ExtractAttributes_NamedOnlySkill_IsNotTreatedAsSenior()
    {
        var id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3");

        var result = await AttributesOfferServiceFactory.Service.ExtractAttributes(
            id,
            """
            Requirements:
            - Terraform
            """,
            [],
            [],
            MasterSkillList.All);

        ReleaseSkillAssertions.AssertSchema(result, id);
        ReleaseSkillAssertions.AssertNonStaticNamesAreListed(result, MasterSkillList.All);
        var skill = ReleaseSkillAssertions.AssertPresent(result.RequiredSkills, "Terraform");
        Assert.AreEqual("Terraform", skill.Name);
        Assert.IsTrue(skill.Level is >= 1 and <= 3, $"Named-only Terraform was level {skill.Level}.");
    }

    [ReleaseCriterion("SH-04", "An English requirements section extracts the technology")]
    public async Task ExtractAttributes_EnglishRequirementsSection_ExtractsTheTechnology()
    {
        var id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb4");

        var result = await AttributesOfferServiceFactory.Service.ExtractAttributes(
            id,
            """
            Requirements:
            - Elasticsearch
            """,
            [],
            [],
            MasterSkillList.All);

        ReleaseSkillAssertions.AssertSchema(result, id);
        ReleaseSkillAssertions.AssertNonStaticNamesAreListed(result, MasterSkillList.All);
        var skill = ReleaseSkillAssertions.AssertPresent(result.RequiredSkills, "Elasticsearch");
        Assert.IsFalse(skill.isStatic);
        Assert.AreEqual("Elasticsearch", skill.Name);
        ReleaseSkillAssertions.AssertAbsentFrom(result.NiceToHaveSkills, "Elasticsearch");
    }
}
