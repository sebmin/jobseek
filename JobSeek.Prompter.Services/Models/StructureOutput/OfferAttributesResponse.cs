using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using JobSeek.Prompter.Prompts;

namespace JobSeek.Prompter.Models.StructureOutput
{
    internal sealed class OfferAttributesResponse
    {
        [JsonPropertyName("required_skills")]
        [Description(OfferAttributesPrompt.RequiredSkills)]
        public List<SkillAttribute> RequiredSkills { get; set; } = [];

        [JsonPropertyName("nice_to_have_skills")]
        [Description(OfferAttributesPrompt.NiceToHaveSkills)]
        public List<SkillAttribute> NiceToHaveSkills { get; set; } = [];
    }

    internal sealed class SkillAttribute
    {
        [JsonPropertyName("skill_name")]
        [Description(OfferAttributesPrompt.SkillName)]
        public string SkillName { get; set; } = "";

        [JsonPropertyName("skill_level")]
        [Range(1, 5)]
        [Description(OfferAttributesPrompt.SkillLevel)]
        public int SkillLevel { get; set; }
    }
}
