using System.Text.Json;
using System.Text.Json.Serialization;
using JobSeek.Normalizator.Contracts.Services;
using JobSeek.Normalizator.Services;

namespace JobSeek.Sandbox.Programs;

internal static class NormalizatorProgram
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
        var offers = JsonSerializer.Deserialize<List<OfferAttributesRecord>>(json, JsonOptions) ?? [];
        var rawSkills = offers
            .SelectMany(offer => (offer.RequiredSkills ?? []).Concat(offer.NiceToHaveSkills ?? []))
            .Select(skill => skill.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();

        using var service = SkillConsolidationService.CreateService();
        var masterList = service.CreateMasterList(rawSkills);

        await SaveAsync(outputFile, masterList);
        Console.WriteLine($"Wrote {masterList.Count} skills to {outputFile}");
        return 0;
    }

    private static async Task SaveAsync(string outputFile, List<string> skills)
    {
        var directory = Path.GetDirectoryName(outputFile);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(outputFile, JsonSerializer.Serialize(skills, JsonOptions));
    }

    private const string Usage = """
        JobSeek.Sandbox normalizator builds one master skill list from offer attributes.

        Usage:
          dotnet run --project JobSeek.Sandbox -- normalizator [options]

        Options:
          --offers <file>   Offer attributes. Default: ./data/offer-attributes.json
          --output <file>   Master skill list. Default: ./data/skill-master-list.json
          --help            Show this help

        Run it from the repository root.
        """;

    private static bool TryParse(
        string[] args,
        out string offersFile,
        out string outputFile,
        out bool help,
        out string error)
    {
        offersFile = Path.Combine(Directory.GetCurrentDirectory(), "data", "offer-attributes.json");
        outputFile = Path.Combine(Directory.GetCurrentDirectory(), "data", "skill-master-list.json");
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

    private sealed record OfferAttributesRecord(
        Guid Id,
        string Slug,
        IReadOnlyList<SkillRecord>? RequiredSkills,
        IReadOnlyList<SkillRecord>? NiceToHaveSkills);

    private sealed record SkillRecord(string Name, int Level, bool IsStatic);
}
