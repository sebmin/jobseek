using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using JobSeek.Prompter.Services.ReleaseTests.Factories;

namespace JobSeek.Prompter.Services.ReleaseTests.Leaderboard;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ReleaseCriterionAttribute : TestMethodAttribute
{
    public ReleaseCriterionAttribute(
        string id,
        string criterion,
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = -1)
        : base(callerFilePath, callerLineNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(criterion);
        Id = id;
        Criterion = criterion;
        DisplayName = $"{id} {criterion}";
    }

    public string Id { get; }

    public string Criterion { get; }

    public override async Task<TestResult[]> ExecuteAsync(ITestMethod testMethod)
    {
        TestResult[] results;
        try
        {
            results = await base.ExecuteAsync(testMethod);
        }
        catch (Exception)
        {
            ReleaseLeaderboard.Record(Id, Criterion, passed: 0, total: 1, AttributesOfferServiceFactory.ActiveModel);
            throw;
        }

        var total = results.Length == 0 ? 1 : results.Length;
        var passed = results.Count(result => result.Outcome == UnitTestOutcome.Passed);
        ReleaseLeaderboard.Record(Id, Criterion, passed, total, AttributesOfferServiceFactory.ActiveModel);
        return results;
    }
}

public static class ReleaseLeaderboard
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    private static readonly Lock Gate = new();
    private static readonly Dictionary<string, List<CriterionTally>> ResultsByModel = new(StringComparer.Ordinal);

    public static void Record(string id, string criterion, int passed, int total, string model)
    {
        lock (Gate)
        {
            if (!ResultsByModel.TryGetValue(model, out var results))
            {
                results = [];
                ResultsByModel.Add(model, results);
            }

            var existing = results.FirstOrDefault(item => item.Id == id);
            if (existing is null)
            {
                results.Add(new CriterionTally
                {
                    Id = id,
                    Name = criterion,
                    Passed = passed,
                    Total = total
                });
                return;
            }

            existing.Passed += passed;
            existing.Total += total;
        }
    }

    public static void Write()
    {
        List<LeaderboardRun> runs;
        lock (Gate)
        {
            if (ResultsByModel.Count == 0)
                return;

            var ranAt = DateTimeOffset.UtcNow;
            runs = ResultsByModel
                .Select(entry => new LeaderboardRun
                {
                    Model = entry.Key,
                    RanAt = ranAt,
                    Criteria = entry.Value
                        .OrderBy(item => item.Id, StringComparer.Ordinal)
                        .Select(item => new LeaderboardCriterion
                        {
                            Id = item.Id,
                            Name = item.Name,
                            Passed = item.Passed,
                            Total = item.Total
                        })
                        .ToList()
                })
                .ToList();
        }

        var directory = LeaderboardDirectory();
        foreach (var run in runs)
        {
            var fileName = SafeFileName(run.Model);
            var cardPath = Path.Combine(directory, fileName + ".md");
            var card = FormatCard(run);
            File.WriteAllText(Path.Combine(directory, fileName + ".json"), JsonSerializer.Serialize(run, JsonOptions));
            File.WriteAllText(cardPath, card);
            Console.WriteLine(card);
            Console.WriteLine(cardPath);
        }

        File.WriteAllText(Path.Combine(directory, "summary.md"), FormatSummary(directory));
    }

    internal static string FormatCard(LeaderboardRun run)
    {
        var lines = new List<string>
        {
            $"# 🏆 Leaderboard Card: JobSeek-Attributes-{run.Model}",
            "",
            $"Run: {run.RanAt:yyyy-MM-dd HH:mm} UTC",
            ""
        };

        foreach (var (title, criteria) in Groups(run.Criteria))
        {
            var passed = criteria.Sum(item => item.Passed);
            var total = criteria.Sum(item => item.Total);
            lines.Add($"## {title}");
            lines.Add("");
            lines.Add("| ID | Success criterion | Result (Passed/Total) | Status |");
            lines.Add("|---|---|---|---|");
            foreach (var criterion in criteria)
                lines.Add($"| {Cell(criterion.Id)} | {Cell(criterion.Name)} | {criterion.Passed}/{criterion.Total} | {Status(criterion.Passed, criterion.Total)} |");

            lines.Add($"| | **{title}** | **{passed}/{total}** | {Status(passed, total)} |");
            lines.Add("");
        }

        var allPassed = run.Criteria.Sum(item => item.Passed);
        var allTotal = run.Criteria.Sum(item => item.Total);
        lines.Add($"**Total** {allPassed}/{allTotal} {Status(allPassed, allTotal)}");
        lines.Add("");
        return string.Join(Environment.NewLine, lines);
    }

    private static IEnumerable<(string Title, List<LeaderboardCriterion> Criteria)> Groups(IReadOnlyList<LeaderboardCriterion> criteria)
    {
        (string Prefix, string Title)[] order =
        [
            ("MH", "Must have"),
            ("SH", "Should have"),
            ("NH", "Nice to have"),
        ];

        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (prefix, title) in order)
        {
            var items = criteria
                .Where(item => item.Id.StartsWith(prefix + "-", StringComparison.Ordinal) || item.Id.Equals(prefix, StringComparison.Ordinal))
                .OrderBy(item => item.Id, StringComparer.Ordinal)
                .ToList();
            if (items.Count == 0)
                continue;

            foreach (var item in items)
                used.Add(item.Id);

            yield return (title, items);
        }

        var rest = criteria
            .Where(item => !used.Contains(item.Id))
            .OrderBy(item => item.Id, StringComparer.Ordinal)
            .ToList();
        if (rest.Count > 0)
            yield return ("Other", rest);
    }

    private static string FormatSummary(string directory)
    {
        var runs = Directory.GetFiles(directory, "*.json")
            .Select(path => JsonSerializer.Deserialize<LeaderboardRun>(File.ReadAllText(path), JsonOptions))
            .Where(run => run is not null)
            .Select(run => run!)
            .OrderBy(run => run.Model, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var lines = new List<string>
        {
            "# Leaderboard",
            "",
            "| Model | Passed/Total | Status | Ran (UTC) | Card |",
            "|---|---|---|---|---|",
        };

        foreach (var run in runs)
        {
            var passed = run.Criteria.Sum(item => item.Passed);
            var total = run.Criteria.Sum(item => item.Total);
            var card = SafeFileName(run.Model) + ".md";
            lines.Add($"| {Cell(run.Model)} | {passed}/{total} | {Status(passed, total)} | {run.RanAt:yyyy-MM-dd HH:mm} | [{card}]({card}) |");
        }

        lines.Add("");
        return string.Join(Environment.NewLine, lines);
    }

    private static string LeaderboardDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        for (var depth = 0; depth < 8 && directory is not null; depth++, directory = directory.Parent)
        {
            var project = Path.Combine(directory.FullName, "JobSeek.Prompter.Services.ReleaseTests.csproj");
            if (!File.Exists(project))
                continue;

            var folder = Path.Combine(directory.FullName, "leaderboard");
            Directory.CreateDirectory(folder);
            return folder;
        }

        var fallback = Path.Combine(AppContext.BaseDirectory, "leaderboard");
        Directory.CreateDirectory(fallback);
        return fallback;
    }

    private static string SafeFileName(string model)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var name = new string(model.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(name) ? "unknown-model" : name;
    }

    private static string Status(int passed, int total) =>
        total > 0 && passed == total ? "✅ PASS" : "❌ FAIL";

    private static string Cell(string value) =>
        value.Replace("|", "\\|", StringComparison.Ordinal).Replace("\r", "", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);

    private sealed class CriterionTally
    {
        public required string Id { get; init; }

        public required string Name { get; init; }

        public int Passed { get; set; }

        public int Total { get; set; }
    }
}

[TestClass]
public sealed class ReleaseLeaderboardHook
{
    [AssemblyCleanup]
    public static void Write() => ReleaseLeaderboard.Write();
}

public sealed class LeaderboardRun
{
    public required string Model { get; init; }

    public DateTimeOffset RanAt { get; init; }

    public required List<LeaderboardCriterion> Criteria { get; init; }
}

public sealed class LeaderboardCriterion
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public int Passed { get; init; }

    public int Total { get; init; }
}
