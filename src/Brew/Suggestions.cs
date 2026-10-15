namespace Brew;

public static class Suggestions
{
    public static string Sentence(IEnumerable<string> values, string conjunction = "and")
    {
        var words = values.ToArray();
        return words.Length < 2 ? string.Join("", words)
            : string.Join(", ", words[..^1]) + " " + conjunction + " " + words[^1];
    }

    public static string[] Correct(string input, IEnumerable<string> dictionary)
    {
        var normalized = input.ToLowerInvariant().Replace("@", "");
        return dictionary.Distinct().Where(w => w != input)
            .Select(w => (Word: w, Text: w.ToLowerInvariant().Replace("@", "")))
            .Select(w => (w.Word, Score: Similarity(normalized, w.Text), Distance: Distance(normalized, w.Text)))
            .Where(w => w.Score >= (normalized.Length > 3 ? .834 : .77)
                && w.Distance <= Math.Ceiling(normalized.Length * .25))
            .OrderByDescending(w => w.Score).Select(w => w.Word).ToArray();
    }

    static int Distance(string a, string b)
    {
        var row = Enumerable.Range(0, b.Length + 1).ToArray();
        for (var i = 1; i <= a.Length; i++)
        {
            var diagonal = row[0]; row[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var previous = row[j];
                row[j] = Math.Min(Math.Min(row[j] + 1, row[j - 1] + 1), diagonal + (a[i - 1] == b[j - 1] ? 0 : 1));
                diagonal = previous;
            }
        }
        return row[^1];
    }

    static double Similarity(string a, string b)
    {
        if (a == b) return 1;
        if (a.Length == 0 || b.Length == 0) return 0;
        var window = Math.Max(0, Math.Max(a.Length, b.Length) / 2 - 1);
        var usedA = new bool[a.Length]; var usedB = new bool[b.Length]; var matches = 0;
        for (var i = 0; i < a.Length; i++)
            for (var j = Math.Max(0, i - window); j < Math.Min(b.Length, i + window + 1); j++)
                if (!usedB[j] && a[i] == b[j]) { usedA[i] = usedB[j] = true; matches++; break; }
        if (matches == 0) return 0;
        var first = a.Where((_, i) => usedA[i]).ToArray();
        var second = b.Where((_, i) => usedB[i]).ToArray();
        var transpositions = first.Where((c, i) => c != second[i]).Count() / 2.0;
        var score = (matches / (double)a.Length + matches / (double)b.Length + (matches - transpositions) / matches) / 3;
        var prefix = 0;
        while (prefix < Math.Min(4, Math.Min(a.Length, b.Length)) && a[prefix] == b[prefix]) prefix++;
        return score > .7 ? score + prefix * .1 * (1 - score) : score;
    }
}
