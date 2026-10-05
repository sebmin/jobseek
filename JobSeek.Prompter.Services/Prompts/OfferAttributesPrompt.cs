using JobSeek.Categorizator.Contracts.Models.Contract;

namespace JobSeek.Prompter.Prompts
{
    public static class OfferAttributesPrompt
    {
        public const string RequiredSkills =
            "Technical skills (strictly technical skills like technologies, frameworks, tools, etc.) the offer requires.";

        public const string NiceToHaveSkills =
            "Technical skills (strictly technical skills like technologies, frameworks, tools, etc.) the offer treats as nice to have.";

        public const string SkillName =
            "Short skill name. Copy a static skill name exactly when it is the same skill.";

        public const string SkillLevel =
            "Required strength from 1 to 5. 1 basic theory only, 2 theory plus a little practice, 3 working experience, 4 senior expert, 5 master or lead. A mention with no stated experience is 1.";

        public static string System(
            IReadOnlyList<SkillDto> requiredSkills,
            IReadOnlyList<SkillDto> niceToHaveSkills)
        {
            return $"""
                Extract technical skills from the job description.
                The provided description can be in Polish or English.

                Static skills are the skills that are already tagged on this offer so they are the most reliable source of skills.
                Static required skills already tagged on this offer:
                <skills>{FormatSkills(requiredSkills)}</skills>

                Static nice-to-have skills already tagged on this offer:
                <nice-to-have-skills>{FormatSkills(niceToHaveSkills)}</nice-to-have-skills>

                Focus on sections that mention skills and technologies where it is mentioned as "umiejętności", "technologie", "kwalifikacje".
                Put a skill from the requirements section in required_skills.
                Put a skill from the nice-to-have section, such as "Mile widziane", in nice_to_have_skills.
                When the description mentions a static skill, keep it in the list it was tagged in and use that skill_name and skill_level.
                When it mentions a technical skill that is not tagged, add it and keep skill_name as written in the description.
                A skill belongs to only one list.

                skill_level is how much expertise the offer requires for that skill:
                1 means the lowest level: basic theoretical knowledge, without professional experience.
                2 means theoretical knowledge plus a little practice, not necessarily in commercial projects.
                3 means real experience and working knowledge.
                4 means expert, senior level.
                5 means the highest level: master or lead of this skill.
                A skill that is only named, with no experience stated, is 1.

                Skills are strictrly technical skills so skip (don't inlcude them in the response):
                - benefits, 
                -soft traits, 
                - company facts, 
                - spoken languages, 
                - project facts, 
                - projects experience.
                - type of projects like banking, finance, etc.
                - knowledge of specific industries like banking, finance, etc.
                
                The reponse rules are:
                <reponse_rules>
                    <rule>
                        Reponse must be in English technical language.
                    </rule>
                    <rule>
                        The response should be in JSON format.
                    </rule>
                </reponse_rules>
                
                """;
        }

        private static string FormatSkills(IReadOnlyList<SkillDto> skills) =>
            skills.Count == 0
                ? "(none)"
                : string.Join("\n", skills.Select(skill => $"- {skill.Name} (level {skill.Level})"));
    }
}
