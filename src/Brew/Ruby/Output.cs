namespace Brew.Ruby;

// Homebrew's Tty + Formatter + Utils::Output (ohai/oh1/opoo/onoe/odie).
public static class Tty
{
    public static bool Color =>
        Environment.GetEnvironmentVariable("HOMEBREW_COLOR") != null ||
        (!Console.IsOutputRedirected && Environment.GetEnvironmentVariable("HOMEBREW_NO_COLOR") == null && Environment.GetEnvironmentVariable("NO_COLOR") == null);

    static string Code(int n) => Color ? $"\u001b[{n}m" : "";
    public static string Reset => Code(0);
    public static string Bold => Code(1);
    public static string Underline => Code(4);
    public static string NoUnderline => Code(24);
    public static string Red => Code(31);
    public static string Green => Code(32);
    public static string Yellow => Code(33);
    public static string Blue => Code(34);
    public static string Magenta => Code(35);
    public static string Cyan => Code(36);
    public static string Default => Code(39);

    public static int Width
    {
        get
        {
            try { return Console.IsOutputRedirected ? 80 : Math.Max(Console.WindowWidth, 10); }
            catch (IOException) { return 80; }
        }
    }

    public static string Truncate(string s) => Console.IsOutputRedirected || s.Length <= Width - 4 ? s : s[..(Width - 4)];
}

public static class Formatter
{
    public const int CommandDescWidth = 80;
    public const int OptionDescWidth = 45;

    public static string Headline(string s, string color) => $"{color}==>{Tty.Reset} {Title(s)}";
    public static string Title(string s) => $"{Tty.Bold}{s}{Tty.Reset}";
    public static string Success(string s) => $"{Tty.Green}{s}{Tty.Reset}";
    public static string Warning(string s, string label = null) => Label(label, s, Tty.Yellow);
    public static string Error(string s, string label = null) => Label(label, s, Tty.Red);
    public static string Url(string s) => $"{Tty.Underline}{s}{Tty.NoUnderline}";
    public static string Option(string s) => $"{Tty.Bold}{s}{Tty.Reset}";
    public static string Identifier(string s) => $"{Tty.Green}{s}{Tty.Reset}";

    static string Label(string label, string s, string color) =>
        label == null ? $"{color}{s}{Tty.Reset}" : $"{color}{label}:{Tty.Reset} {s}";

    // Formatter.format_help_text
    public static string FormatHelpText(string s, int width = 172)
    {
        int desc = OptionDescWidth;
        int indent = width - desc;
        var pad = new string(' ', indent);
        s = System.Text.RegularExpressions.Regex.Replace(s, @"(?<=\S) *\n(?=\S)", " ");
        s = System.Text.RegularExpressions.Regex.Replace(s, @"([`>)\]]:) ", "$1\n    ");
        s = System.Text.RegularExpressions.Regex.Replace(s, $@"^( +-.+  +(?=\S.{{{desc}}}))(.{{1,{desc}}})(?: +|$)(?!-)\n?", $"$1$2\n{pad}", System.Text.RegularExpressions.RegexOptions.Multiline);
        s = System.Text.RegularExpressions.Regex.Replace(s, $@"^( {{{indent}}}(?=\S.{{{desc}}}))(.{{1,{desc}}})(?: +|$)(?!-)\n?", $"$1$2\n{pad}", System.Text.RegularExpressions.RegexOptions.Multiline);
        s = System.Text.RegularExpressions.Regex.Replace(s, $@"(.{{1,{width}}})(?: +|$)(?!-)\n?", "$1\n", System.Text.RegularExpressions.RegexOptions.Multiline);
        return s;
    }
}

public static class Out
{
    public static TextWriter Std => Console.Out;
    public static TextWriter ErrW => Console.Error;

    public static void Ohai(string title, params string[] lines)
    {
        Std.WriteLine(Formatter.Headline(Tty.Truncate(title), Tty.Blue));
        foreach (var l in lines) Std.WriteLine(l);
    }

    public static void Oh1(string title) => Std.WriteLine(Formatter.Headline(Tty.Truncate(title), Tty.Green));
    public static void Opoo(string message) => ErrW.WriteLine(Formatter.Warning(message, "Warning"));
    public static void Onoe(string message) => ErrW.WriteLine(Formatter.Error(message, "Error"));
    public static void Puts(string s = "") => Std.WriteLine(s);

    // Utils.pluralize
    public static string Pluralize(string stem, long count, bool includeCount = false)
    {
        string plural = "s", singular = "";
        if (stem == "formula") plural = "e";
        else if (stem is "dependency" or "try") { stem = stem[..^1]; plural = "ies"; singular = "y"; }
        return (includeCount ? $"{count} " : "") + stem + (count == 1 ? singular : plural);
    }

    // Utils::Text.to_sentence
    public static string ToSentence(IReadOnlyList<string> values, string conjunction = "and") => values.Count switch
    {
        0 => "",
        1 => values[0],
        2 => $"{values[0]} {conjunction} {values[1]}",
        _ => $"{string.Join(", ", values.Take(values.Count - 1))} {conjunction} {values[^1]}",
    };
}

// Homebrew's odie / Kernel exit with a message.
public sealed class BrewError : Exception
{
    public BrewError(string message) : base(message) { }
}
