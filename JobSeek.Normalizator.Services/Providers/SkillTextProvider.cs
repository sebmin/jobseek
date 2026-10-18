namespace JobSeek.Normalizator.Services.Providers;

public class SkillTextProvider : ISkillTextProvider
{
    public List<string> SplitToWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var words = new List<string>();
        foreach (var raw in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var piece in raw.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var word = TrimEdges(piece.ToLowerInvariant());
                if (word.Length > 0)
                    words.Add(word);
            }
        }

        return words;
    }

    private static string TrimEdges(string word)
    {
        var start = 0;
        var end = word.Length - 1;
        while (start <= end && IsEdgePunctuation(word[start], atStart: true))
            start++;
        while (end >= start && IsEdgePunctuation(word[end], atStart: false))
            end--;

        return start > end ? "" : word[start..(end + 1)];
    }

    private static bool IsEdgePunctuation(char character, bool atStart) =>
        character switch
        {
            '#' => false,
            '.' => !atStart,
            _ => char.IsPunctuation(character)
        };
}

internal interface ISkillTextProvider
{
    List<string> SplitToWords(string? text);
}
