using System.Text.Json;

namespace JobSeek.Normalizator.Services.Providers;

public class SkillAliasProvider : ISkillAliasProvider
{
    private readonly Dictionary<string, string> _aliases;

    public SkillAliasProvider()
        : this(Resolve())
    {
    }

    public SkillAliasProvider(string path)
    {
        var json = File.ReadAllText(path);
        var aliases = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
        _aliases = new Dictionary<string, string>(aliases, StringComparer.OrdinalIgnoreCase);
    }

    public string? Resolve(string rawSkill)
    {
        return _aliases.TryGetValue(rawSkill.Trim(), out var canonical) ? canonical : null;
    }

    public IEnumerable<KeyValuePair<string, string>> Entries => _aliases;

    private static string Resolve()
    {
        var fileName = Path.Combine("Data", "skill-aliases.json");
        var outputPath = Path.Combine(AppContext.BaseDirectory, fileName);
        if (File.Exists(outputPath))
            return outputPath;

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var projectPath = Path.Combine(directory.FullName, "JobSeek.Normalizator.Services", fileName);
            if (File.Exists(projectPath))
                return projectPath;

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Skill alias file was not found.", fileName);
    }
}

internal interface ISkillAliasProvider
{
    string? Resolve(string rawSkill);

    IEnumerable<KeyValuePair<string, string>> Entries { get; }
}
