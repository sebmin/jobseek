using JobSeek.Categorizator.Contracts.Models.Contract;
using JobSeek.Prompter.Contracts.Models;
using JobSeek.Prompter.Contracts.Services;
using JobSeek.Prompter.Models.StructureOutput;
using JobSeek.Prompter.Prompts;
using Microsoft.Extensions.AI;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using SchemaValueType = Json.Schema.SchemaValueType;

namespace JobSeek.Prompter.Services
{
    public sealed class AttributesOfferService : IAttributesOfferService
    {
        private readonly IChatClient _chatClient;

        public AttributesOfferService(IChatClient chatClient, string? model = null)
        {
            _chatClient = chatClient;
            Model = string.IsNullOrWhiteSpace(model) ? ResolveModel() : model.Trim();
        }

        public string Model { get; }

        public static string ResolveModel()
        {
            var model = Environment.GetEnvironmentVariable("LLM_MODEL");
            return string.IsNullOrWhiteSpace(model) ? "gpt-4o-mini" : model.Trim();
        }

        public async Task<OfferAttributesDto> ExtractAttributes(
            Guid id,
            string description,
            List<SkillDto> requiredStaticSkills,
            List<SkillDto> niceToHaveStaticSkills)
        {
            List<ChatMessage> messages =
            [
                new(ChatRole.System, OfferAttributesPrompt.System),
                new(ChatRole.User, OfferAttributesPrompt.User(description, requiredStaticSkills, niceToHaveStaticSkills))
            ];

            ChatResponse<OfferAttributesResponse> response = await _chatClient.GetResponseAsync<OfferAttributesResponse>(
                messages,
                new ChatOptions { Temperature = 0 });

            OfferAttributesResponse parsed = response.Result
                ?? throw new InvalidOperationException("The model returned no attributes.");

            return new OfferAttributesDto(
                id,
                MapSkills(parsed.RequiredSkills, requiredStaticSkills),
                MapSkills(parsed.NiceToHaveSkills, niceToHaveStaticSkills));
        }

        public async Task<OfferAttributesDto> ExtractAttributes(
            Guid id,
            string description,
            List<SkillDto> requiredStaticSkills,
            List<SkillDto> niceToHaveStaticSkills,
            List<string> databaseMasterList)
        {
            JsonNode[] allowedEnumValues = databaseMasterList
                .Select(skill => JsonValue.Create(skill) as JsonNode)
                .ToArray();

            var skillAttributeSchema = new JsonSchemaBuilder()
                .Type(SchemaValueType.Object)
                .Properties(
                    ("skill_name", new JsonSchemaBuilder()
                        .Type(SchemaValueType.String)
                        .Enum(allowedEnumValues)
                        .Description(OfferAttributesPrompt.MasterListSkillName)
                    ),
                    ("skill_level", new JsonSchemaBuilder()
                        .Type(SchemaValueType.Integer)
                        .Minimum(1)
                        .Maximum(5)
                        .Description(OfferAttributesPrompt.SkillLevel)
                    )
                )
                .Required("skill_name", "skill_level")
                .AdditionalProperties(false);

            var responseSchema = new JsonSchemaBuilder()
                .Defs(("skill", skillAttributeSchema))
                .Type(SchemaValueType.Object)
                .Properties(
                    ("required_skills", new JsonSchemaBuilder()
                        .Type(SchemaValueType.Array)
                        .Items(new JsonSchemaBuilder().Ref("#/$defs/skill"))
                        .Description(OfferAttributesPrompt.RequiredSkills)
                    ),
                    ("nice_to_have_skills", new JsonSchemaBuilder()
                        .Type(SchemaValueType.Array)
                        .Items(new JsonSchemaBuilder().Ref("#/$defs/skill"))
                        .Description(OfferAttributesPrompt.NiceToHaveSkills)
                    )
                )
                .Required("required_skills", "nice_to_have_skills")
                .AdditionalProperties(false)
                .Build();

            List<ChatMessage> messages =
            [
                new(ChatRole.System, OfferAttributesPrompt.MasterListSystem),
                new(ChatRole.User, OfferAttributesPrompt.User(description, requiredStaticSkills, niceToHaveStaticSkills))
            ];

            string schemaString = JsonSerializer.Serialize(responseSchema);
            using JsonDocument parsedSchemaDoc = JsonDocument.Parse(schemaString);

            var options = new ChatOptions
            {
                Temperature = 0,
                AdditionalProperties = new AdditionalPropertiesDictionary
                {
                    ["strict"] = true
                },
                ResponseFormat = ChatResponseFormat.ForJsonSchema(
                    parsedSchemaDoc.RootElement,
                    "OfferAttributesResponse")
            };

            ChatResponse response = await _chatClient.GetResponseAsync(messages, options);

            string rawJson = response.Text
                ?? throw new InvalidOperationException("The model returned no content.");

            var optionsSerializer = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            OfferAttributesResponse parsed = JsonSerializer.Deserialize<OfferAttributesResponse>(rawJson, optionsSerializer)
                ?? throw new InvalidOperationException("Failed to deserialize schema response.");

            return new OfferAttributesDto(
                id,
                MapSkills(parsed.RequiredSkills, requiredStaticSkills),
                MapSkills(parsed.NiceToHaveSkills, niceToHaveStaticSkills));
        }

        public static AttributesOfferService CreateService()
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_JOBSEEK_API_KEY")
                ?? throw new InvalidOperationException("OPENAI_JOBSEEK_API_KEY is not set.");
            var model = ResolveModel();

            IChatClient client = new OpenAI.Chat.ChatClient(model, apiKey).AsIChatClient();
            return new AttributesOfferService(client, model);
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
