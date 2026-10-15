namespace Brew;

public sealed class CommandArguments
{
    public List<string> Names { get; } = [];
    readonly Dictionary<string, string> flags = new(StringComparer.Ordinal);
    readonly object shared;
    public bool Has(params string[] names) => names.Any(name => shared == null ? flags.ContainsKey(name)
        : (bool)shared.GetType().GetMethod("Switch").Invoke(shared, [name]) || shared.GetType().GetMethod("Flag").Invoke(shared, [name]) != null);
    public string Value(string name, string fallback = null) => shared != null ? shared.GetType().GetMethod("Flag").Invoke(shared, [name])?.ToString() ?? fallback
        : flags.TryGetValue(name, out var value) && value != null ? value : fallback;
    public IEnumerable<string> Flags => flags.Keys;

    public CommandArguments(object parsed)
    {
        shared = parsed;
        Names.AddRange((IEnumerable<string>)parsed.GetType().GetProperty("Named").GetValue(parsed));
    }

    public CommandArguments(string[] args, IEnumerable<string> allowed, IEnumerable<string> values = null)
    {
        var accepted = allowed.Concat(new[] { "--formula", "--formulae", "--verbose", "-v", "--quiet", "-q", "--debug", "-d", "--force", "-f" }).ToHashSet();
        var withValues = (values ?? []).ToHashSet();
        bool positional = false;
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg == "--") { positional = true; continue; }
            if (positional || !arg.StartsWith('-') || arg == "-") { Names.Add(arg); continue; }
            var parts = arg.Split('=', 2); var flag = parts[0];
            if (!accepted.Contains(flag)) throw new InvalidOperationException($"invalid option: {arg}");
            string value = parts.Length == 2 ? parts[1] : null;
            if (withValues.Contains(flag) && value == null)
            {
                if (++i >= args.Length) throw new InvalidOperationException($"missing argument: {flag}");
                value = args[i];
            }
            flags[flag] = value;
        }
    }

    public void RequireNames(string command)
    {
        if (Names.Count == 0) throw new InvalidOperationException($"Invalid usage: brew {command} requires at least one formula argument.");
    }
}
