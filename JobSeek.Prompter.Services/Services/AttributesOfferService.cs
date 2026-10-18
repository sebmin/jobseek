using JobSeek.Categorizator.Contracts.Models.Contract;
using JobSeek.Prompter.Contracts.Models;
using JobSeek.Prompter.Contracts.Services;
using JobSeek.Prompter.Models.StructureOutput;
using JobSeek.Prompter.Prompts;
using Microsoft.Extensions.AI;
using System.ClientModel;
using System.ClientModel.Primitives;
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
                MapSkills(parsed.RequiredSkills.Select(skill => ((string?)skill.SkillName, skill.SkillLevel)), requiredStaticSkills),
                MapSkills(parsed.NiceToHaveSkills.Select(skill => ((string?)skill.SkillName, skill.SkillLevel)), niceToHaveStaticSkills));
        }

        public async Task<OfferAttributesDto> ExtractAttributes(
            Guid id,
            string description,
            List<SkillDto> requiredStaticSkills,
            List<SkillDto> niceToHaveStaticSkills,
            List<string> databaseMasterList)
        {
            JsonNode[] allowedEnumValues = AllowedValues(databaseMasterList);
            if (allowedEnumValues.Length == 0)
                throw new InvalidOperationException("The strict skill list is empty.");

            var skillSchema = new JsonSchemaBuilder()
                .Type(SchemaValueType.Object)
                .Properties(
                    ("skill_name", new JsonSchemaBuilder()
                        .Type(SchemaValueType.String)
                        .Enum(allowedEnumValues)
                        .Description(OfferAttributesPrompt.MasterListSkillName)),
                    ("skill_level", SkillLevelSchema()))
                .Required("skill_name", "skill_level")
                .AdditionalProperties(false);

            List<ChatMessage> messages =
            [
                new(ChatRole.System, OfferAttributesPrompt.MasterListSystem),
                new(ChatRole.User, OfferAttributesPrompt.User(description, requiredStaticSkills, niceToHaveStaticSkills))
            ];

            using var timeout = new CancellationTokenSource(StrictTimeout);
            string rawJson;
            try
            {
                rawJson = await RequestJsonAsync(messages, ResponseSchema(skillSchema), strict: true, timeout.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Strict skill extraction exceeded {StrictTimeout.TotalSeconds:0} seconds.");
            }

            OfferAttributesResponse parsed = Deserialize<OfferAttributesResponse>(rawJson);
            return new OfferAttributesDto(
                id,
                MapSkills(parsed.RequiredSkills.Select(skill => ((string?)skill.SkillName, skill.SkillLevel)), requiredStaticSkills),
                MapSkills(parsed.NiceToHaveSkills.Select(skill => ((string?)skill.SkillName, skill.SkillLevel)), niceToHaveStaticSkills));
        }

        public async Task<OfferAttributesDto> ExtractRawAttributes(
            Guid id,
            string description,
            List<SkillDto> requiredStaticSkills,
            List<SkillDto> niceToHaveStaticSkills,
            List<string> databaseMasterList)
        {
            var allowed = databaseMasterList
                .Where(skill => !string.IsNullOrWhiteSpace(skill))
                .ToList();

            var skillSchema = new JsonSchemaBuilder()
                .Type(SchemaValueType.Object)
                .Properties(
                    ("skill_name", new JsonSchemaBuilder()
                        .Type(SchemaValueType.String)
                        .Description(OfferAttributesPrompt.RawSkillName)),
                    ("skill_raw", new JsonSchemaBuilder()
                        .Type(SchemaValueType.String)
                        .Description(OfferAttributesPrompt.SkillRaw)),
                    ("skill_level", SkillLevelSchema()))
                .Required("skill_name", "skill_raw", "skill_level")
                .AdditionalProperties(false);

            List<ChatMessage> messages =
            [
                new(ChatRole.System, OfferAttributesPrompt.RawSystem(allowed)),
                new(ChatRole.User, OfferAttributesPrompt.User(description, requiredStaticSkills, niceToHaveStaticSkills))
            ];

            string rawJson = await RequestJsonAsync(messages, ResponseSchema(skillSchema), strict: false, CancellationToken.None);
            MasterListOfferAttributesResponse parsed = Deserialize<MasterListOfferAttributesResponse>(rawJson);
            return new OfferAttributesDto(
                id,
                MapSkills(parsed.RequiredSkills.Select(skill => ResolveRawSkill(skill, allowed)), requiredStaticSkills),
                MapSkills(parsed.NiceToHaveSkills.Select(skill => ResolveRawSkill(skill, allowed)), niceToHaveStaticSkills));
        }

        public static AttributesOfferService CreateService()
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_JOBSEEK_API_KEY")
                ?? throw new InvalidOperationException("OPENAI_JOBSEEK_API_KEY is not set.");
            var model = ResolveModel();
            var clientOptions = new OpenAI.OpenAIClientOptions
            {
                RetryPolicy = new ClientRetryPolicy(0)
            };

            IChatClient client = new OpenAI.Chat.ChatClient(model, new ApiKeyCredential(apiKey), clientOptions).AsIChatClient();
            return new AttributesOfferService(client, model);
        }

        private static readonly TimeSpan StrictTimeout = TimeSpan.FromSeconds(45);

        private static JsonNode[] AllowedValues(IEnumerable<string> skills) =>
            skills
                .Where(skill => !string.IsNullOrWhiteSpace(skill))
                .Select(skill => JsonValue.Create(skill) as JsonNode)
                .ToArray();

        private static JsonSchemaBuilder SkillLevelSchema() => new JsonSchemaBuilder()
            .Type(SchemaValueType.Integer)
            .Minimum(1)
            .Maximum(5)
            .Description(OfferAttributesPrompt.SkillLevel);

        private static JsonSchema ResponseSchema(JsonSchemaBuilder skillSchema) => new JsonSchemaBuilder()
            .Defs(("skill", skillSchema))
            .Type(SchemaValueType.Object)
            .Properties(
                ("required_skills", new JsonSchemaBuilder()
                    .Type(SchemaValueType.Array)
                    .Items(new JsonSchemaBuilder().Ref("#/$defs/skill"))
                    .Description(OfferAttributesPrompt.RequiredSkills)),
                ("nice_to_have_skills", new JsonSchemaBuilder()
                    .Type(SchemaValueType.Array)
                    .Items(new JsonSchemaBuilder().Ref("#/$defs/skill"))
                    .Description(OfferAttributesPrompt.NiceToHaveSkills)))
            .Required("required_skills", "nice_to_have_skills")
            .AdditionalProperties(false)
            .Build();

        private async Task<string> RequestJsonAsync(
            List<ChatMessage> messages,
            JsonSchema schema,
            bool strict,
            CancellationToken cancellationToken)
        {
            using JsonDocument parsedSchemaDoc = JsonDocument.Parse(JsonSerializer.Serialize(schema));
            var options = new ChatOptions
            {
                Temperature = 0,
                ResponseFormat = ChatResponseFormat.ForJsonSchema(
                    parsedSchemaDoc.RootElement,
                    "OfferAttributesResponse")
            };
            if (strict)
            {
                options.AdditionalProperties = new AdditionalPropertiesDictionary
                {
                    ["strict"] = true
                };
            }

            ChatResponse response = await _chatClient.GetResponseAsync(messages, options, cancellationToken);
            return response.Text ?? throw new InvalidOperationException("The model returned no content.");
        }

        private static T Deserialize<T>(string rawJson)
        {
            var optionsSerializer = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<T>(rawJson, optionsSerializer)
                ?? throw new InvalidOperationException("Failed to deserialize schema response.");
        }

        private static (string? Name, int Level) ResolveRawSkill(MasterListSkillAttribute skill, IReadOnlyList<string> allowed)
        {
            var name = Canonical(skill.SkillName, allowed)
                ?? Canonical(skill.SkillRaw, allowed)
                ?? (!string.IsNullOrWhiteSpace(skill.SkillName) ? skill.SkillName : skill.SkillRaw);
            return (name, skill.SkillLevel);
        }

        private static string? Canonical(string? value, IReadOnlyList<string> allowed)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return allowed.FirstOrDefault(name => string.Equals(name, value, StringComparison.OrdinalIgnoreCase));
        }

        private static List<OfferSkillDto> MapSkills(
            IEnumerable<(string? Name, int Level)> skills,
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
                if (string.IsNullOrWhiteSpace(skill.Name) || !names.Add(skill.Name))
                    continue;

                combined.Add(new OfferSkillDto(skill.Name, skill.Level, false));
            }

            return combined;
        }
    }
}
