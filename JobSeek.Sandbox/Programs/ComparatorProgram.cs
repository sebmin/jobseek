using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using JobSeek.Categorizator.Contracts.Models.OfferSource;
using JobSeek.Comparator.Contracts.Models;
using JobSeek.Comparator.Contracts.Services;
using JobSeek.Comparator.Services;
using SourceCandidate = JobSeek.Categorizator.Contracts.Models.CandidateSource.Candidate;

namespace JobSeek.Sandbox.Programs;

internal static class ComparatorProgram
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    public static int Run(string[] args)
    {
        if (!ComparatorOptions.TryParse(args, out var options, out var error))
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine();
            Console.Error.WriteLine(ComparatorOptions.Usage);
            return 1;
        }

        if (options.Help)
        {
            Console.WriteLine(ComparatorOptions.Usage);
            return 0;
        }

        if (!File.Exists(options.OffersFile))
        {
            Console.Error.WriteLine($"Offers file does not exist: {options.OffersFile}");
            return 1;
        }

        if (!File.Exists(options.CandidateFile))
        {
            Console.Error.WriteLine($"Candidate file does not exist: {options.CandidateFile}");
            Console.Error.WriteLine("Expected JSON: { \"skills\": [ { \"name\": \"C#\", \"level\": 5 } ] }");
            return 1;
        }

        var sourceOffers = JsonSerializer.Deserialize<List<SourceOffer>>(File.ReadAllText(options.OffersFile), ReadOptions) ?? [];
        var sourceCandidate = JsonSerializer.Deserialize<SourceCandidate>(File.ReadAllText(options.CandidateFile), ReadOptions) ?? new SourceCandidate();
        var offers = sourceOffers.Select(ToOffer).ToList();
        var candidate = ToCandidate(sourceCandidate);

        IAttributeComparerService comparer = AttributeComparerService.CreateService();
        var results = offers
            .Select(offer => comparer.Score(candidate, offer))
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Slug, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var outputDirectory = Path.GetDirectoryName(options.OutputFile);
        if (!string.IsNullOrEmpty(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        File.WriteAllText(options.OutputFile, JsonSerializer.Serialize(results, WriteOptions));

        Console.WriteLine($"Candidate skills: {candidate.Skills.Count}");
        Console.WriteLine($"Ranked {results.Count} offers. Wrote {options.OutputFile}");
        Console.WriteLine();
        Console.WriteLine($"{"Rank",4}  {"Score",6}  {"Required",10}  {"Nice",8}  Slug");

        var shown = 0;
        foreach (var result in results.Take(options.Top))
        {
            shown++;
            Console.WriteLine(
                $"{shown,4}  {result.Score,6:0.000}  {result.RequiredMatches,3}/{result.RequiredSkillCount,-6}  {result.NiceToHaveMatches,2}/{result.NiceToHaveSkillCount,-5}  {result.Slug}");
        }

        return 0;
    }

    private static Offer ToOffer(SourceOffer source)
    {
        if (!Guid.TryParse(source.Guid, out var id))
            throw new InvalidOperationException($"Offer '{source.Slug}' has no guid.");

        return new Offer(
            id,
            source.Slug ?? "",
            ToSkills(source.RequiredSkills),
            ToSkills(source.NiceToHaveSkills));
    }

    private static IReadOnlyList<Skill> ToSkills(List<SourceSkill>? skills) =>
        (skills ?? []).Select(skill => new Skill(skill.Name ?? "", skill.Level)).ToArray();

    private static Candidate ToCandidate(SourceCandidate source) =>
        new(source.Skills?.Select(skill => new Skill(skill.Name ?? "Unknown", skill.Level)).ToArray() ?? []);
}
