using JobSeek.Categorizator.Contracts.Models.Contract;
using JobSeek.Categorizator.Contracts.Models.OfferSource;
using JobSeek.Normalizator.Contracts.Services;
using JobSeek.Normalizator.Services.Services;
using JobSeek.Prompter.Contracts.Models;
using JobSeek.Prompter.Contracts.Services;
using JobSeek.Prompter.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JobSeek.Sandbox.Programs;

internal static class PrompterProgram
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        WriteIndented = true
    };

    public static async Task<int> Run(string[] args)
    {
        if (!TryParse(args, out var offersFile, out var outputFile, out var masterListFile, out var help, out var error))
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine();
            Console.Error.WriteLine(Usage);
            return 1;
        }

        if (help)
        {
            Console.WriteLine(Usage);
            return 0;
        }

        if (!File.Exists(offersFile))
        {
            Console.Error.WriteLine($"Offers file does not exist: {offersFile}");
            return 1;
        }

        if (!File.Exists(masterListFile))
        {
            Console.Error.WriteLine($"Master list file does not exist: {masterListFile}");
            return 1;
        }

        var json = await File.ReadAllTextAsync(offersFile);
        var sourceOffers = JsonSerializer.Deserialize<List<SourceOffer>>(json, JsonOptions) ?? [];
        var results = await ReadExistingAsync(outputFile);
        var done = CompletedIds(results);
        var masterSkills = await ReadMasterListAsync(masterListFile);
        var service = AttributesOfferService.CreateService();
        using var skillNormalizationService = SkillNormalizationService.CreateService(masterSkills);
        using var skillCandidateService = SkillCandidateService.CreateService(masterSkills);

        for (var index = 0; index < sourceOffers.Count; index++)
        {
            var source = sourceOffers[index];
            if (!Guid.TryParse(source.Guid, out var id))
                throw new InvalidOperationException($"Offer '{source.Slug}' has no guid.");

            var slug = source.Slug ?? "";
            if (done.Contains(id))
            {
                Console.WriteLine($"[{index + 1}/{sourceOffers.Count}] skip {slug}");
                continue;
            }

            var required = MapAndNormalizeSkills(source.RequiredSkills, skillNormalizationService);
            var niceToHave = MapAndNormalizeSkills(source.NiceToHaveSkills, skillNormalizationService);
            var candidateSkills = skillCandidateService.Select(
                source.DescriptionRaw ?? "",
                required.Select(skill => skill.Name).Concat(niceToHave.Select(skill => skill.Name)).ToList());

            Console.WriteLine($"[{index + 1}/{sourceOffers.Count}] {slug} ({candidateSkills.Count} candidate skills)");
            if (candidateSkills.Count == 0)
            {
                await SaveOfferAsync(outputFile, results, StaticRecord(id, slug, required, niceToHave, true));
                continue;
            }

            try
            {
                var extracted = await service.ExtractAttributes(
                    id,
                    source.DescriptionRaw ?? "",
                    required,
                    niceToHave,
                    candidateSkills.ToList());
                await SaveOfferAsync(outputFile, results, ExtractedRecord(id, slug, extracted, true));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{slug}: {ex.Message}");
                await SaveOfferAsync(outputFile, results, StaticRecord(id, slug, required, niceToHave, false));
            }
        }

        Console.WriteLine($"Wrote {results.Count} offers to {outputFile}");
        return 0;
    }

    private static HashSet<Guid> CompletedIds(IEnumerable<OfferSkillsRecord> results) =>
        results.Where(offer => offer.isResponseValid != false).Select(offer => offer.Id).ToHashSet();

    private static OfferSkillsRecord StaticRecord(
        Guid id,
        string slug,
        List<SkillDto> required,
        List<SkillDto> niceToHave,
        bool isResponseValid) =>
        new(id, slug, ToStaticRecords(required), ToStaticRecords(niceToHave), isResponseValid);

    private static OfferSkillsRecord ExtractedRecord(Guid id, string slug, OfferAttributesDto extracted, bool isResponseValid) =>
        new(id, slug, extracted.RequiredSkills, extracted.NiceToHaveSkills, isResponseValid);

    private static async Task SaveOfferAsync(string outputFile, List<OfferSkillsRecord> results, OfferSkillsRecord record)
    {
        results.RemoveAll(offer => offer.Id == record.Id);
        results.Add(record);
        await SaveAsync(outputFile, results);
    }

    private static List<OfferSkillDto> ToStaticRecords(List<SkillDto> skills)
    {
        var combined = new List<OfferSkillDto>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var skill in skills)
        {
            if (string.IsNullOrWhiteSpace(skill.Name) || !names.Add(skill.Name))
                continue;

            combined.Add(new OfferSkillDto(skill.Name, skill.Level, true));
        }

        return combined;
    }

    private static List<SkillDto> MapAndNormalizeSkills(List<SourceStaticSkill>? skills, ISkillNormalizationService skillNormalizationService) =>
        (skills ?? [])
            .Where(skill => !string.IsNullOrWhiteSpace(skill.Name))
            .Select(skill => new SkillDto(skillNormalizationService.CreateNormalizedSkill(skill.Name!), skill.Level))
            .ToList();

    private static async Task<List<string>> ReadMasterListAsync(string masterListFile)
    {
        var json = await File.ReadAllTextAsync(masterListFile);
        if (string.IsNullOrWhiteSpace(json))
            return [];

        return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
    }

    private static async Task<List<OfferSkillsRecord>> ReadExistingAsync(string outputFile)
    {
        if (!File.Exists(outputFile))
            return [];

        var json = await File.ReadAllTextAsync(outputFile);
        if (string.IsNullOrWhiteSpace(json))
            return [];

        return JsonSerializer.Deserialize<List<OfferSkillsRecord>>(json, JsonOptions) ?? [];
    }

    private static async Task SaveAsync(string outputFile, List<OfferSkillsRecord> results)
    {
        var directory = Path.GetDirectoryName(outputFile);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(outputFile, JsonSerializer.Serialize(results, JsonOptions));
    }

    private const string Usage = """
        JobSeek.Sandbox prompter extracts skills from each offer description and combines them with the static tags.

        Usage:
          dotnet run --project JobSeek.Sandbox -- prompter [options]

        Options:
          --offers <file>       Parsed offers. Default: ./data/offers.json
          --output <file>       Combined skills. Default: ./data/offer-master-strict-attributes.json
          --master-list <file>  Canonical skill names. Default: ./data/skill-master-list.json
          --help                Show this help

        Requires OPENAI_API_KEY. LLM_MODEL is optional.
        Run it from the repository root.
        """;

    private static bool TryParse(
        string[] args,
        out string offersFile,
        out string outputFile,
        out string masterListFile,
        out bool help,
        out string error)
    {
        offersFile = Path.Combine(Directory.GetCurrentDirectory(), "data", "offers.json");
        outputFile = Path.Combine(Directory.GetCurrentDirectory(), "data", "offer-master-strict-attributes.json");
        masterListFile = Path.Combine(Directory.GetCurrentDirectory(), "data", "skill-master-list.json");
        help = false;
        error = "";

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg is "--help" or "-h")
            {
                help = true;
                continue;
            }

            if (i + 1 >= args.Length)
            {
                error = $"Missing value for {arg}.";
                return false;
            }

            var value = args[++i];
            switch (arg)
            {
                case "--offers":
                    offersFile = Path.GetFullPath(value);
                    break;
                case "--output":
                    outputFile = Path.GetFullPath(value);
                    break;
                case "--master-list":
                    masterListFile = Path.GetFullPath(value);
                    break;
                default:
                    error = $"Unknown option '{arg}'.";
                    return false;
            }
        }

        return true;
    }

    private sealed record OfferSkillsRecord(
        Guid Id,
        string Slug,
        IReadOnlyList<OfferSkillDto> RequiredSkills,
        IReadOnlyList<OfferSkillDto> NiceToHaveSkills,
        bool? isResponseValid);
}
