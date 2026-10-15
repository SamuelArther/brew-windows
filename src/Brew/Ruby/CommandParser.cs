using System.Text;
using System.Text.RegularExpressions;

namespace Brew.Ruby;

// Raised for bad command-line usage; printed as "Error: Invalid usage: ..." after the help text.
public sealed class UsageError : Exception
{
    public string Reason { get; }
    public UsageError(string reason = null) : base(reason == null ? "Invalid usage" : $"Invalid usage: {reason}") { Reason = reason; }
}

// OptionParser errors (invalid/ambiguous option, missing argument): printed as "Error: <message>".
public sealed class OptionError : Exception
{
    public OptionError(string message) : base(message) { }
}

public sealed class ParsedArgs
{
    internal readonly Dictionary<string, object> Values = new();
    public List<string> Named { get; internal set; } = new();
    public List<string> Remaining { get; internal set; } = new();

    // Switch value by option name, e.g. Switch("dry-run") for --dry-run / -n.
    public bool Switch(string name) => Values.TryGetValue(Key(name), out var v) && v is true;
    public bool? SwitchOrNull(string name) => Values.TryGetValue(Key(name), out var v) ? v as bool? : null;
    public string Flag(string name) => Values.TryGetValue(Key(name), out var v) ? v as string : null;
    public IReadOnlyList<string> Array(string name) => Values.TryGetValue(Key(name), out var v) && v is List<string> l ? l : null;
    public bool Has(string name) => Values.ContainsKey(Key(name));
    public bool Debug => Switch("debug");
    public bool Quiet => Switch("quiet");
    public bool Verbose => Switch("verbose");
    public bool Help => Switch("help");

    internal static string Key(string name) => Regex.Replace(name, @"^--?(\[no-\])?", "").Replace('-', '_').Replace("=", "");
}

// Port of Homebrew::CLI::Parser (help text, usage banner, validation) on top of a small
// re-implementation of Ruby's OptionParser.
public sealed class CommandParser
{
    sealed class Opt
    {
        public string[] Short = System.Array.Empty<string>(), Long = System.Array.Empty<string>();
        public string[] Desc;
        public string Type;      // switch, required_flag, optional_flag, comma_array
        public bool Hidden, Global, Negatable;
        public string DependsOn;
    }

    const string HiddenPlaceholder = "@@HIDDEN@@";
    static readonly Dictionary<string, string> UsageMapping = new()
    {
        ["service"] = "<service>",
        ["text_or_regex"] = "<text>|`/`<regex>`/`",
        ["url"] = "<URL>",
    };

    readonly string command;
    readonly string[] aliases;
    readonly List<Opt> opts = new();
    readonly List<Opt> globals = new();
    readonly List<string[]> conflicts = new();
    string description, usageBanner;
    string[] namedTypes;   // null = not declared, empty = none
    int? minNamed, maxNamed;

    public CommandParser(string command, params string[] aliases)
    {
        this.command = command;
        this.aliases = aliases;
        foreach (var (s, l, d) in new[]
                 {
                     ("-d", "--debug", "Display any debugging information."),
                     ("-q", "--quiet", "Make some output more quiet."),
                     ("-v", "--verbose", "Make some output more verbose."),
                     ("-h", "--help", "Show this message."),
                 })
            globals.Add(new Opt { Short = new[] { s }, Long = new[] { l }, Desc = WrapDesc(d), Type = "switch", Global = true });
    }

    public CommandParser Description(string text)
    {
        description = text.TrimEnd('\n');
        return this;
    }

    public CommandParser UsageBanner(string text)
    {
        var t = text.TrimEnd('\n');
        var parts = t.Split("\n\n", 2);
        usageBanner = parts[0];
        if (parts.Length > 1) description = parts[1];
        return this;
    }

    public CommandParser Switch(params string[] names) => Switch(names, null);

    public CommandParser Switch(string[] names, string description, bool hidden = false, string dependsOn = null)
    {
        var o = Make(names, "switch", description, hidden);
        o.Negatable = names.Any(n => n.StartsWith("--[no-]"));
        o.DependsOn = dependsOn;
        Add(o);
        return this;
    }

    public CommandParser Flag(string[] names, string description, bool hidden = false, string dependsOn = null)
    {
        var type = names.Any(n => n.EndsWith('=')) ? "required_flag" : "optional_flag";
        var o = Make(names.Select(n => n.TrimEnd('=')).ToArray(), type, description, hidden);
        o.DependsOn = dependsOn;
        Add(o);
        return this;
    }

    public CommandParser CommaArray(string name, string description, bool hidden = false)
    {
        Add(Make(new[] { name.TrimEnd('=') }, "comma_array", description, hidden));
        return this;
    }

    public CommandParser Conflicts(params string[] options)
    {
        conflicts.Add(options);
        return this;
    }

    // types: e.g. "formula", "cask", "tap", "command", "none" (or several).
    public CommandParser NamedArgs(string[] types = null, int? number = null, int? min = null, int? max = null)
    {
        namedTypes = types ?? System.Array.Empty<string>();
        if (types is ["none"]) { maxNamed = 0; return this; }
        if (number != null) minNamed = maxNamed = number;
        else { minNamed = min; maxNamed = max; }
        return this;
    }

    public CommandParser NamedArgs(string type, int? number = null, int? min = null, int? max = null) =>
        NamedArgs(new[] { type }, number, min, max);

    Opt Make(string[] names, string type, string desc, bool hidden)
    {
        var o = new Opt { Type = type, Hidden = hidden };
        o.Short = names.Where(n => n.StartsWith('-') && !n.StartsWith("--")).ToArray();
        o.Long = names.Where(n => n.StartsWith("--")).ToArray();
        desc = hidden ? HiddenPlaceholder : !string.IsNullOrEmpty(desc) ? desc : names.Select(n => Regex.Replace(n, "^--?", "").Replace('-', ' ')).Max(StringComparer.Ordinal);
        o.Desc = WrapDesc(desc);
        return o;
    }

    void Add(Opt o)
    {
        if (o.Long.Length > 0) opts.RemoveAll(x => x.Long.FirstOrDefault() == o.Long[0]);
        opts.Add(o);
    }

    // wrap_option_desc; like Ruby's split, drop trailing empty lines
    static string[] WrapDesc(string desc)
    {
        var lines = Formatter.FormatHelpText(desc, Formatter.OptionDescWidth).Split('\n').ToList();
        while (lines.Count > 0 && lines[^1].Length == 0) lines.RemoveAt(lines.Count - 1);
        return lines.ToArray();
    }

    // ---- help ----

    string GenerateUsageBanner()
    {
        var names = new List<string> { $"`{command}`" };
        names.AddRange(aliases.Select(a => $"`{a}`").OrderBy(a => a, StringComparer.Ordinal));
        string options;
        var shown = opts.Where(o => !o.Hidden).ToList();
        if (shown.Count == 0) options = "";
        else if (shown.Count > 2) options = " [<options>]";
        else options = string.Concat(shown.Select(o => o.Type is "required_flag" or "comma_array" ? $" [`{o.Long.FirstOrDefault() ?? o.Short[0]}=`]" : $" [`{o.Long.FirstOrDefault() ?? o.Short[0]}`]"));
        string named = "";
        if (namedTypes != null && !(namedTypes.Length == 1 && namedTypes[0] == "none") && namedTypes.Length > 0)
        {
            string argType = string.Join("|", namedTypes.Select(t => UsageMapping.TryGetValue(t, out var m) ? m : $"<{t}>"));
            named = minNamed == null && maxNamed == 1 ? $" [{argType}]"
                : minNamed == null ? $" [{argType} ...]"
                : minNamed == 1 && maxNamed == 1 ? $" {argType}"
                : minNamed == 1 ? $" {argType} [...]"
                : $" {argType} ...";
        }
        return string.Join(", ", names) + options + named;
    }

    // OptionParser::Switch#summarize with width 32, max 31, indent "  "
    // shorts/longs: the names not already shown by a later definition (OptionParser's sdone/ldone)
    static IEnumerable<string> Summarize(Opt o, string[] shorts, string[] longs)
    {
        const int width = 32, max = 31;
        if (shorts.Length == 0 && longs.Length == 0) yield break;
        var left = new List<string> { string.Join(", ", shorts) };
        var right = new List<string>(o.Desc);
        var lopts = new Queue<string>(longs);
        while (lopts.Count > 0)
        {
            var s = lopts.Dequeue();
            int l = left[^1].Length + s.Length;
            if (!(l < max || shorts.Length == 0)) left.Add("");
            left[^1] += (left[^1].Length == 0 ? "    " : ", ") + s;
        }
        int mlen = left.Max(x => x.Length);
        while (mlen > width && left.Count > 0)
        {
            var l = left[0];
            left.RemoveAt(0);
            if (l.Length == mlen) mlen = left.Count == 0 ? 0 : left.Max(x => x.Length);
            if (l.Length < width && right.Count > 0 && right[0].Length > 0)
            {
                l = l.PadRight(width) + " " + right[0];
                right.RemoveAt(0);
            }
            yield return "  " + l;
        }
        while (left.Count > 0 || right.Count > 0)
        {
            var l = left.Count > 0 ? left[0] : null;
            if (left.Count > 0) left.RemoveAt(0);
            var r = right.Count > 0 ? right[0] : null;
            if (right.Count > 0) right.RemoveAt(0);
            if (l == null && r == null) break;
            l ??= "";
            if (!string.IsNullOrEmpty(r)) l = l.PadRight(width) + " " + r;
            yield return "  " + l;
        }
    }

    public string HelpText()
    {
        var banner = $"{usageBanner ?? GenerateUsageBanner()}\n\n{description}\n\n";
        var sb = new StringBuilder(banner);
        // OptionParser visits command options, then the on_tail globals; within each list the
        // last definition claims a name first.
        var sdone = new HashSet<string>();
        var ldone = new HashSet<string>();
        foreach (var list in new[] { opts, globals })
        {
            var part = new List<List<string>>();
            for (int k = list.Count - 1; k >= 0; k--)
            {
                var o = list[k];
                var shorts = o.Short.Where(sdone.Add).ToArray();
                var longs = o.Long.Where(ldone.Add).ToArray();
                part.Insert(0, Summarize(o, shorts, longs).ToList());
            }
            foreach (var line in part.SelectMany(x => x))
                sb.Append(line).Append('\n');
        }
        var text = Formatter.FormatHelpText(sb.ToString(), Formatter.CommandDescWidth);
        text = Regex.Replace(text, $@"\n.*?{HiddenPlaceholder}.*?(?=\n)", "");
        text = $"{Tty.Bold}Usage: brew{Tty.Reset} " + text;
        text = Regex.Replace(text, "`(.*?)`", $"{Tty.Bold}$1{Tty.Reset}", RegexOptions.Singleline);
        text = Regex.Replace(text, @"<[^\s]+?://[^\s]+?>", m => Formatter.Url(m.Value));
        text = Regex.Replace(text, @"\*.*?\*|<.*?>", m =>
            Regex.Replace(m.Value[1..^1], @"^(\s*)(.*?)$", $"$1{Tty.Underline}$2{Tty.Reset}", RegexOptions.Multiline), RegexOptions.Singleline);
        return text.TrimEnd('\n');
    }

    // ---- parsing ----

    IEnumerable<Opt> All => opts.Concat(globals);

    (Opt opt, string name, bool negated) FindLong(string name)
    {
        foreach (var o in All)
            foreach (var l in o.Long)
            {
                var plain = l.Replace("[no-]", "");
                if (plain == name) return (o, l, false);
                if (o.Negatable && "--no-" + plain[2..] == name) return (o, l, true);
            }
        var candidates = All.SelectMany(o => o.Long.Select(l => (o, l))).Where(p => p.l.Replace("[no-]", "").StartsWith(name, StringComparison.Ordinal)).ToList();
        if (candidates.Select(c => c.o).Distinct().Count() == 1) return (candidates[0].o, candidates[0].l, false);
        if (candidates.Count > 1) throw new OptionError($"ambiguous option: {name}");
        return (null, null, false);
    }

    void Set(ParsedArgs args, Opt o, object value)
    {
        foreach (var n in o.Short.Concat(o.Long)) args.Values[ParsedArgs.Key(n)] = value;
    }

    public ParsedArgs Parse(IEnumerable<string> argv)
    {
        var args = new ParsedArgs();
        var list = argv.ToList();
        var named = new List<string>();
        var nonOptions = new List<string>();
        int dd = list.IndexOf("--");
        if (dd >= 0) { nonOptions = list.Skip(dd + 1).ToList(); list = list.Take(dd).ToList(); }
        bool InvalidThrow(string arg)
        {
            Console.Error.WriteLine(HelpText());
            throw new OptionError($"invalid option: {arg}");
        }

        for (int i = 0; i < list.Count; i++)
        {
            var a = list[i];
            if (a.StartsWith("--") && a.Length > 2)
            {
                string name = a, value = null;
                int eq = a.IndexOf('=');
                if (eq > 0) { name = a[..eq]; value = a[(eq + 1)..]; }
                var (o, _, negated) = FindLong(name);
                if (o == null) { InvalidThrow(a); continue; }
                if (o.Type == "switch")
                {
                    if (value != null) throw new OptionError($"needless argument: {a}");
                    Set(args, o, !negated);
                }
                else
                {
                    if (value == null)
                    {
                        if (o.Type == "optional_flag" && (i + 1 >= list.Count || list[i + 1].StartsWith('-'))) value = "";
                        else if (i + 1 < list.Count) value = list[++i];
                        else throw new OptionError($"missing argument: {name}");
                    }
                    Set(args, o, o.Type == "comma_array" ? value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() : value);
                }
            }
            else if (a.StartsWith('-') && a.Length > 1)
            {
                for (int k = 1; k < a.Length; k++)
                {
                    var flag = "-" + a[k];
                    var o = All.FirstOrDefault(x => x.Short.Contains(flag));
                    if (o == null) { InvalidThrow(flag); continue; }
                    if (o.Type == "switch") { Set(args, o, true); continue; }
                    string value = k + 1 < a.Length ? a[(k + 1)..] : i + 1 < list.Count ? list[++i] : throw new OptionError($"missing argument: {flag}");
                    Set(args, o, o.Type == "comma_array" ? value.Split(',').ToList() : value);
                    break;
                }
            }
            else named.Add(a);
        }

        foreach (var o in opts)
            if (o.Type == "switch")
                foreach (var n in o.Short.Concat(o.Long))
                    args.Values.TryAdd(ParsedArgs.Key(n), o.Negatable ? null : false);

        args.Named = named.Concat(nonOptions).ToList();
        args.Remaining = nonOptions.Count == 0 ? named : named.Append("--").Concat(nonOptions).ToList();
        if (args.Help) return args;

        CheckConstraints(args);
        CheckNamed(args.Named);
        return args;
    }

    void CheckConstraints(ParsedArgs args)
    {
        bool Passed(string name) => args.Values.TryGetValue(ParsedArgs.Key(name), out var v) && v is not (false or null);
        foreach (var o in opts.Where(o => o.DependsOn != null))
            foreach (var n in o.Long.Take(1))
                if (Passed(n) && !Passed(o.DependsOn))
                    throw new UsageError($"`{n}` cannot be passed without `{o.DependsOn}`.");
        foreach (var group in conflicts)
        {
            var passed = group.Where(Passed).ToList();
            if (passed.Count > 1)
                throw new UsageError($"Options {string.Join(" and ", passed.Select(Formatter.Option))} are mutually exclusive.");
        }
    }

    void CheckNamed(List<string> named)
    {
        var types = namedTypes == null ? new List<string>() : namedTypes.Where(t => t != "none").Select(t => t.Replace('_', ' ')).Distinct().ToList();
        string Kinds(int n) => Out.ToSentence(types.Count == 0 ? new List<string> { "named" } : types, "or") + " " + Out.Pluralize("argument", n);
        if (minNamed != null && maxNamed != null && minNamed == maxNamed && named.Count != maxNamed)
            throw new UsageError($"This command requires exactly {minNamed} {Kinds(minNamed.Value)}.");
        if (minNamed != null && named.Count < minNamed)
            throw new UsageError($"This command requires at least {minNamed} {Kinds(minNamed.Value)}.");
        if (maxNamed != null && named.Count > maxNamed)
            throw new UsageError(maxNamed == 0 ? "This command does not take named arguments."
                : $"This command does not take more than {maxNamed} {Kinds(maxNamed.Value)}.");
    }
}
