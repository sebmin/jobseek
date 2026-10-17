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

        public const string MasterListSkillName =
            "Exact allowed skill name. Copy it from the allowed values. Leave the skill out when no allowed name is that skill.";

        public const string RawSkillName =
            "Exact allowed skill name, with the allowed spelling and case. Use an empty string when the skill is not an allowed value.";

        public const string SkillRaw =
            "English technical skill name. Use this only when skill_name is empty. Use an empty string when skill_name is set.";

        public const string SkillLevel =
            "Required strength from 1 to 5. 1 basic theory only, 2 theory plus a little practice, 3 working experience, 4 senior expert, 5 master or lead. A mention with no stated experience is 1.";

        public const string System = $$"""
                Extract technical skills from the job description.
                The provided description can be in Polish or English.

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

                Skills are strictly technical skills so skip (don't inlcude them in the response):
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

                Example:
                Description: "Wymagania: zaawansowana znajomość C# / .NET. Mile widziane: Docker. Nie używamy PHP. Angielski wymagany. "
                required_skills: [{"skill_name":"C#","skill_level":4},{"skill_name":".NET","skill_level":4}]
                nice_to_have_skills: [{"skill_name":"Docker","skill_level":1}]
                """;

        public const string MasterListSystem = $$"""
                Extract technical skills from the job description.
                The provided description can be in Polish or English.

                The section heading decides the list.
                Requirements, Wymagania, Skills, Technologies, Umiejętności, Technologie, Kwalifikacje, and Knowledge and qualifications go to required_skills.
                Nice to have, Mile widziane, and plus go to nice_to_have_skills.
                A skill in a requirements section goes to required_skills even when the static required list is empty and the static nice-to-have list is not.
                Do not move a required skill onto nice_to_have_skills because another skill is already tagged there.
                skill_name must be copied exactly from the allowed values.
                When a technical skill in the description is not an allowed skill_name, leave it out. Do not substitute a similar value.
                A static tag keeps that one skill on its tagged list and level. It does not move any other skill.
                A skill belongs to only one list.

                skill_level is how much expertise the offer requires for that skill:
                1 means the lowest level: basic theoretical knowledge, without professional experience.
                2 means theoretical knowledge plus a little practice, not necessarily in commercial projects.
                3 means real experience and working knowledge.
                4 means expert, senior level.
                5 means the highest level: master or lead of this skill.
                A skill that is only named, with no experience stated, is 1.

                Skills are strictly technical skills so skip (don't inlcude them in the response):
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

                Example:
                Description: "Wymagania: zaawansowana znajomość C# / .NET. Git. Mile widziane: Docker. Nie używamy PHP. Angielski wymagany."
                In this example the allowed skill_name values include C#, .NET, and Docker. Git is not an allowed value.
                required_skills: [{"skill_name":"C#","skill_level":4},{"skill_name":".NET","skill_level":4}]
                nice_to_have_skills: [{"skill_name":"Docker","skill_level":1}]
                Git is left out because it is not an allowed skill_name. PHP is left out because the offer rejects it. English is left out because it is a spoken language.

                Example:
                Static required: (none). Static nice-to-have: Redis (level 2).
                Description: "Requirements: Java."
                required_skills: [{"skill_name":"Java","skill_level":1}]
                nice_to_have_skills: [{"skill_name":"Redis","skill_level":2}]
                Java stays required. Redis stays nice-to-have because it was tagged there.
                """;

        public static string RawSystem(IReadOnlyList<string> allowedSkillNames)
        {
            var allowed = allowedSkillNames.Count == 0
                ? "(none)"
                : string.Join("\n", allowedSkillNames.Select(name => $"- {name}"));

            return $$"""
                Extract technical skills from the job description.
                The provided description can be in Polish or English.

                The section heading decides the list.
                Requirements, Wymagania, Skills, Technologies, Umiejętności, Technologie, Kwalifikacje, and Knowledge and qualifications go to required_skills.
                Nice to have, Mile widziane, and plus go to nice_to_have_skills.
                A skill in a requirements section goes to required_skills even when the static required list is empty and the static nice-to-have list is not.
                Do not move a required skill onto nice_to_have_skills because another skill is already tagged there.
                A match ignores case. Copy the allowed spelling into skill_name and set skill_raw to an empty string.
                When a technical skill matches no allowed value, set skill_name to an empty string and skill_raw to its English name.
                Do not substitute a similar allowed value. Do not put the same skill in both fields.
                A static tag keeps that one skill on its tagged list and level. It does not move any other skill.
                A skill belongs to only one list.

                skill_level is how much expertise the offer requires for that skill:
                1 means the lowest level: basic theoretical knowledge, without professional experience.
                2 means theoretical knowledge plus a little practice, not necessarily in commercial projects.
                3 means real experience and working knowledge.
                4 means expert, senior level.
                5 means the highest level: master or lead of this skill.
                A skill that is only named, with no experience stated, is 1.

                Skills are strictly technical skills so skip (don't inlcude them in the response):
                - benefits, 
                -soft traits, 
                - company facts, 
                - spoken languages, 
                - project facts, 
                - projects experience.
                - type of projects like banking, finance, etc.
                - knowledge of specific industries like banking, finance, etc.

                Allowed skill_name values:
                {{allowed}}

                Example:
                Description: "Wymagania: zaawansowana znajomość C# / .NET. Azure. Git. Mile widziane: Docker. Nie używamy PHP. Angielski wymagany."
                In this example the allowed skill_name values include C#, .NET, azure, and Docker.
                required_skills: [{"skill_name":"C#","skill_raw":"","skill_level":4},{"skill_name":".NET","skill_raw":"","skill_level":4},{"skill_name":"azure","skill_raw":"","skill_level":1},{"skill_name":"","skill_raw":"Git","skill_level":1}]
                nice_to_have_skills: [{"skill_name":"Docker","skill_raw":"","skill_level":1}]
                Azure uses skill_name azure because the match ignores case. Git uses skill_raw because it is not an allowed value. PHP is left out because the offer rejects it. English is left out because it is a spoken language.
                """;
        }

        public static string User(
            string description,
            IReadOnlyList<SkillDto> requiredSkills,
            IReadOnlyList<SkillDto> niceToHaveSkills)
        {
            return $"""
                Static skills are already decided. Keep each static skill on its tagged list and level. A static tag does not move any other skill.
                Skills found only in the description follow the section they appear in.
                Static required skills already tagged on this offer:
                <skills>{FormatSkills(requiredSkills)}</skills>
                
                Static nice-to-have skills already tagged on this offer:
                <nice-to-have-skills>{FormatSkills(niceToHaveSkills)}</nice-to-have-skills>

                Job description:
                <job-description>{description}</job-description>
                """;
        }

        private static string FormatSkills(IReadOnlyList<SkillDto> skills) =>
            skills.Count == 0
                ? "(none)"
                : string.Join("\n", skills.Select(skill => $"- {skill.Name} (level {skill.Level})"));
    }
}
