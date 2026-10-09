using JobSeek.Sandbox.Programs;

namespace JobSeek.Sandbox;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "--help" or "-h" or "help")
        {
            Console.WriteLine(Usage);
            return args.Length == 0 ? 1 : 0;
        }

        var command = args[0];
        var rest = args[1..];
        return command.ToLowerInvariant() switch
        {
            "categorizator" => CategorizatorProgram.Run(rest),
            "comparator" => ComparatorProgram.Run(rest),
            "prompter" => await PrompterProgram.Run(rest),
            _ => Unknown(command)
        };
    }

    private static int Unknown(string command)
    {
        Console.Error.WriteLine($"Unknown command '{command}'.");
        Console.Error.WriteLine();
        Console.Error.WriteLine(Usage);
        return 1;
    }

    private const string Usage = """
        JobSeek.Sandbox runs the categorizator, comparator, and prompter programs.

        Usage:
          dotnet run --project JobSeek.Sandbox -- <command> [options]

        Commands:
          categorizator   Map parsed offers to comparison fields
          comparator      Rank offers against a candidate
          prompter        Extract skills from offer descriptions

        Run a command with --help for its options.
        Run it from the repository root.
        """;
}
