using Categorizator.Models.Contract;
using Categorizator.Models.Source;
using Prompter.Models;
using Prompter.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prompter;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        WriteIndented = true
    };

    public static async Task<int> Main(string[] args)
    {
        if (!TryParse(args, out var offersFile, out var outputFile, out var help, out var error))
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

        var json = await File.ReadAllTextAsync(offersFile);
        var sourceOffers = JsonSerializer.Deserialize<List<SourceOffer>>(json, JsonOptions) ?? [];
        var results = await ReadExistingAsync(outputFile);
        var done = results.Select(offer => offer.Id).ToHashSet();
        var service = AttributesOfferService.CreateService();

        for (var index = 0; index < 3; index++)
        {
            var source = sourceOffers[index];
            if (!Guid.TryParse(source.Guid, out var id))
                throw new InvalidOperationException($"Offer '{source.Slug}' has no guid.");

            if (!done.Add(id))
            {
                Console.WriteLine($"[{index + 1}/{sourceOffers.Count}] skip {source.Slug}");
                continue;
            }

            Console.WriteLine($"[{index + 1}/{sourceOffers.Count}] {source.Slug}");
            var required = MapSkills(source.RequiredSkills);
            var niceToHave = MapSkills(source.NiceToHaveSkills);

            try
            {
                var extracted = await service.ExtractAttributes(
                    id,
                    source.DescriptionRaw ?? "",
                    required,
                    niceToHave);

                results.Add(new OfferSkillsRecord(
                    id,
                    source.Slug ?? "",
                    extracted.RequiredSkills,
                    extracted.NiceToHaveSkills));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{source.Slug}: {ex.Message}");
                continue;
            }

            await SaveAsync(outputFile, results);
        }

        Console.WriteLine($"Wrote {results.Count} offers to {outputFile}");
        return 0;
    }

    private static List<SkillDto> MapSkills(List<SourceSkill>? skills) =>
        (skills ?? [])
            .Where(skill => !string.IsNullOrWhiteSpace(skill.Name))
            .Select(skill => new SkillDto(skill.Name!, skill.Level))
            .ToList();

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
        Prompter extracts skills from each offer description and combines them with the static tags.

        Usage:
          dotnet run --project Prompter -- [options]

        Options:
          --offers <file>   Parsed offers. Default: ./data/offers.json
          --output <file>   Combined skills. Default: ./data/offer-attributes.json
          --help            Show this help

        Requires OPENAI_API_KEY. LLM_MODEL is optional.
        Run it from the repository root.
        """;

    private static bool TryParse(
        string[] args,
        out string offersFile,
        out string outputFile,
        out bool help,
        out string error)
    {
        offersFile = Path.Combine(Directory.GetCurrentDirectory(), "data", "offers.json");
        outputFile = Path.Combine(Directory.GetCurrentDirectory(), "data", "offer-attributes.json");
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
        IReadOnlyList<OfferSkillDto> NiceToHaveSkills);
}
