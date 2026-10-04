namespace Categorizator;

internal sealed record CategorizatorOptions(string OffersFile, string OutputFile, string PromptsDirectory, bool Help)
{
    public const string Usage = """
        Categorizator reads offers.json and keeps the fields used to compare an offer with a candidate.
        It also writes one LLM prompt per offer. The extra properties in that prompt are not defined yet.

        Usage:
          dotnet run --project Categorizator -- [options]

        Options:
          --offers <file>   Parsed offers. Default: ./data/offers.json
          --output <file>   Categorized offers. Default: ./data/categorized-offers.json
          --prompts <dir>   Prompt files, one per offer. Default: ./data/prompts
          --help            Show this help

        Run it from the repository root, after Parser has written data/offers.json.
        """;

    public static bool TryParse(string[] args, out CategorizatorOptions options, out string error)
    {
        var offersFile = Path.Combine(Directory.GetCurrentDirectory(), "data", "offers.json");
        var outputFile = Path.Combine(Directory.GetCurrentDirectory(), "data", "categorized-offers.json");
        var promptsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "data", "prompts");
        var help = false;

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
                options = null!;
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
                case "--prompts":
                    promptsDirectory = Path.GetFullPath(value);
                    break;
                default:
                    options = null!;
                    error = $"Unknown option '{arg}'.";
                    return false;
            }
        }

        options = new CategorizatorOptions(offersFile, outputFile, promptsDirectory, help);
        error = "";
        return true;
    }
}
