namespace Comparator;

internal sealed record ComparatorOptions(string OffersFile, string CandidateFile, string OutputFile, int Top, bool Help)
{
    public const string Usage = """
        Comparator ranks offers by how well their skills match a candidate.

        A skill counts when the names match, ignoring case. The offer's skill level
        is the weight: level 5 counts as 1, level 1 as 0.2. Required skills are 80%
        of the score and nice-to-have skills are 20% when both lists exist.

        Usage:
          dotnet run --project Comparator -- [options]

        Options:
          --offers <file>      Parsed offers. Default: ./data/offers.json
          --candidate <file>   Candidate skills. Default: ./data/candidate.json
          --output <file>      Ranked results. Default: ./data/comparison.json
          --top <n>            How many rows to print. Default: 20
          --help               Show this help

        The candidate file is JSON: { "skills": [ { "name": "C#", "level": 5 } ] }
        Run it from the repository root.
        """;

    public static bool TryParse(string[] args, out ComparatorOptions options, out string error)
    {
        var offersFile = Path.Combine(Directory.GetCurrentDirectory(), "data", "offers.json");
        var candidateFile = Path.Combine(Directory.GetCurrentDirectory(), "data", "candidate.json");
        var outputFile = Path.Combine(Directory.GetCurrentDirectory(), "data", "comparison.json");
        var top = 20;
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
                case "--candidate":
                    candidateFile = Path.GetFullPath(value);
                    break;
                case "--output":
                    outputFile = Path.GetFullPath(value);
                    break;
                case "--top":
                    if (!int.TryParse(value, out top) || top < 1)
                    {
                        options = null!;
                        error = "--top must be an integer of at least 1.";
                        return false;
                    }

                    break;
                default:
                    options = null!;
                    error = $"Unknown option '{arg}'.";
                    return false;
            }
        }

        options = new ComparatorOptions(offersFile, candidateFile, outputFile, top, help);
        error = "";
        return true;
    }
}
