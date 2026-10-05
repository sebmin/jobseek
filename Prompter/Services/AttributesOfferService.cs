using Categorizator.Models.Contract;
using Microsoft.Extensions.AI;
using Prompter.Models;
using Prompter.Models.StructureOutput;
using Prompter.Prompts;

namespace Prompter.Services
{
    public sealed class AttributesOfferService
    {
        private readonly IChatClient _chatClient;

        public AttributesOfferService(IChatClient chatClient) => _chatClient = chatClient;

        public async Task<OfferAttributesDto> ExtractAttributes(
            Guid id,
            string description,
            List<SkillDto> requiredSkills,
            List<SkillDto> niceToHaveSkills)
        {
            List<ChatMessage> messages =
            [
                new(ChatRole.System, OfferAttributesPrompt.System(requiredSkills, niceToHaveSkills)),
                new(ChatRole.User, description)
            ];

            ChatResponse<OfferAttributesResponse> response = await _chatClient.GetResponseAsync<OfferAttributesResponse>(
                messages,
                new ChatOptions { Temperature = 0 });

            OfferAttributesResponse parsed = response.Result
                ?? throw new InvalidOperationException("The model returned no attributes.");

            return new OfferAttributesDto(
                id,
                MapSkills(parsed.RequiredSkills, requiredSkills),
                MapSkills(parsed.NiceToHaveSkills, niceToHaveSkills));
        }

        public static AttributesOfferService CreateService()
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_JOBSEEK_API_KEY")
                ?? throw new InvalidOperationException("OPENAI_JOBSEEK_API_KEY is not set.");
            var model = Environment.GetEnvironmentVariable("LLM_MODEL") ?? "gpt-4o-mini";

            IChatClient client = new OpenAI.Chat.ChatClient(model, apiKey).AsIChatClient();
            return new AttributesOfferService(client);
        }

        private static List<OfferSkillDto> MapSkills(
            IEnumerable<SkillAttribute> skills,
            List<SkillDto> staticSkills)
        {
            var combined = new List<OfferSkillDto>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var skill in staticSkills)
            {
                if (string.IsNullOrWhiteSpace(skill.Name) || !names.Add(skill.Name))
                    continue;

                combined.Add(new OfferSkillDto(skill.Name, skill.Level, true));
            }

            foreach (var skill in skills)
            {
                if (string.IsNullOrWhiteSpace(skill.SkillName) || !names.Add(skill.SkillName))
                    continue;

                combined.Add(new OfferSkillDto(skill.SkillName, skill.SkillLevel, false));
            }

            return combined;
        }
    }
}
