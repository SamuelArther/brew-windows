using System.Collections;
using System.Text.Json;
using System.Text.RegularExpressions;
using Brew.Formulae;

namespace Brew;

public sealed class QueryCommands(Paths paths)
{
    readonly FormulaRepository repository = new(paths);
    readonly InstallationStore store = new(paths);

    public void Search(CommandArguments args)
    {
        args.RequireNames("search");
        var query = string.Join(" ", args.Names);
        var pattern = Pattern(query);
        var names = repository.Names.Where(name => pattern.IsMatch(name)
            || args.Has("--desc", "--description") && pattern.IsMatch(repository.Load(name).Description ?? "")).ToArray();
        if (names.Length == 0) throw new InvalidOperationException($"No formulae or casks found for {JsonSerializer.Serialize(query)}.");
        if (!Console.IsOutputRedirected) CommandOutput.Header("Formulae");
        foreach (var name in names) Console.WriteLine(name);
    }

    public void Desc(CommandArguments args)
    {
        var names = args.Has("--search", "-s", "--name", "-n", "--description", "-d")
            ? repository.Names.Where(n => Pattern(string.Join(" ", args.Names)).IsMatch(args.Has("--description", "-d") ? repository.Load(n).Description ?? "" : n))
            : args.Names.Count == 0 ? repository.Names : args.Names;
        foreach (var name in names) { var formula = repository.Load(name); Console.WriteLine($"{formula.Name}: {formula.Description}"); }
    }

    public void Info(CommandArguments args)
    {
        var names = args.Names.Count == 0 && args.Has("--installed") ? store.All.Select(k => k.Receipt.Name).Distinct().ToArray() : args.Names.ToArray();
        if (args.Has("--json"))
        {
            if (names.Length == 0) names = args.Has("--eval-all") ? repository.Names.ToArray() : store.All.Select(k => k.Receipt.Name).Distinct().ToArray();
            var formulae = names.Select(n => JsonFormula(repository.Load(n))).ToArray();
            var version = args.Value("--json", "v1");
            object json = version is "v2" or "2" ? new { formulae, casks = Array.Empty<object>() } : formulae;
            Console.WriteLine(JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }
        if (names.Length == 0)
        {
            var all = store.All;
            Console.WriteLine($"{all.Length} {(all.Length == 1 ? "keg" : "kegs")}, {CommandOutput.Size(all.Sum(k => Directory.EnumerateFiles(k.Path, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length)))}");
            return;
        }
        for (int index = 0; index < names.Length; index++)
        {
            if (index > 0) Console.WriteLine();
            var formula = repository.Load(names[index]); var kegs = store.Kegs(formula.Name);
            var version = $"stable {Metadata.StableVersion(formula)}";
            if (kegs.Length > 0 && VersionOrder.Instance.Compare(kegs[^1].Receipt.Version, Metadata.Version(formula)) < 0)
                version = $"{kegs[^1].Receipt.Version} → {version}";
            CommandOutput.Header($"{formula.Name}: {version}" + (Metadata.Bool(formula, "KegOnly") ? " [keg-only]" : ""));
            if (formula.Description != null) Console.WriteLine(formula.Description);
            if (formula.Homepage != null) Console.WriteLine(formula.Homepage);
            var deprecated = Metadata.Call(formula, "DeprecationMessage")?.ToString();
            if (!string.IsNullOrEmpty(deprecated)) Console.WriteLine(char.ToUpperInvariant(deprecated[0]) + deprecated[1..]);
            var aliases = Metadata.Strings(formula, "Aliases"); if (aliases.Length > 0) Console.WriteLine("Aliases: " + string.Join(", ", aliases));
            if (kegs.Length == 0) Console.WriteLine("Not installed");
            else Console.WriteLine(store.IsPinned(formula.Name) ? "Pinned" : "Installed");
            Console.WriteLine("From: " + Source(formula.Name));
            if (!string.IsNullOrEmpty(formula.License)) Console.WriteLine("License: " + formula.License);
            if (kegs.Length > 0)
            {
                CommandOutput.Header("Installed Versions");
                foreach (var keg in kegs) Console.WriteLine($"{keg.Path} ({PackageManager.Abbreviation(keg.Path)})");
            }
            var dependencies = Dependencies(formula).ToArray();
            if (dependencies.Length > 0)
            {
                CommandOutput.Header("Dependencies");
                foreach (var group in dependencies.GroupBy(d => d.Tags.Contains("build") ? "Build" : d.Tags.Contains("test") ? "Test" : "Required"))
                    Console.WriteLine(group.Key + ": " + string.Join(", ", group.Select(d => d.Name)));
            }
            var context = new InstallContext(kegs.LastOrDefault()?.Path ?? Path.Combine(paths.Rack(formula.Name), Metadata.Version(formula)), Environment.CurrentDirectory, brewPrefix: paths.Prefix);
            var caveats = PackageManager.FormulaCaveats(formula, context);
            if (!string.IsNullOrWhiteSpace(caveats)) { CommandOutput.Header("Caveats"); Console.WriteLine(caveats.TrimEnd()); }
        }
    }

    public object JsonFormula(FormulaDefinition formula)
    {
        var deps = Dependencies(formula).ToArray();
        return new
        {
            name = formula.Name, full_name = formula.Name, tap = TapFor(formula.Name),
            oldnames = Array.Empty<string>(), aliases = Metadata.Strings(formula, "Aliases"),
            desc = formula.Description, license = formula.License, homepage = formula.Homepage,
            versions = new { stable = Metadata.StableVersion(formula), head = Metadata.Text(formula, "HeadUrl"), bottle = false },
            urls = new { stable = new Dictionary<string, object> { ["url"] = formula.Url, ["tag"] = null, ["revision"] = null, ["using"] = null, ["checksum"] = formula.Sha256 } },
            revision = Metadata.Get(formula, "Revision") ?? 0, version_scheme = Metadata.Get(formula, "VersionScheme") ?? 0,
            dependencies = deps.Where(d => d.Runtime).Select(d => d.Name).ToArray(),
            build_dependencies = deps.Where(d => d.Tags.Contains("build")).Select(d => d.Name).ToArray(),
            test_dependencies = deps.Where(d => d.Tags.Contains("test")).Select(d => d.Name).ToArray(),
            optional_dependencies = deps.Where(d => d.Tags.Contains("optional")).Select(d => d.Name).ToArray(),
            recommended_dependencies = deps.Where(d => d.Tags.Contains("recommended")).Select(d => d.Name).ToArray(),
            keg_only = Metadata.Bool(formula, "KegOnly"), deprecated = Metadata.Bool(formula, "Deprecated"), disabled = Metadata.Bool(formula, "Disabled"),
            pinned = store.IsPinned(formula.Name), outdated = store.Latest(formula.Name) is InstalledKeg latest && VersionOrder.Instance.Compare(latest.Receipt.Version, Metadata.Version(formula)) < 0,
            installed = store.Kegs(formula.Name).Select(k => new { version = k.Receipt.Version, installed_on_request = k.Receipt.InstalledOnRequest,
                installed_as_dependency = !k.Receipt.InstalledOnRequest, poured_from_bottle = false,
                time = k.Receipt.InstalledAt, runtime_dependencies = (k.Receipt.Dependencies ?? []).Select(n => new { full_name = n }).ToArray() }).ToArray()
        };
    }

    public string[] OutdatedNames(IEnumerable<string> names = null) => (names ?? store.All.Select(k => k.Receipt.Name).Distinct())
        .Where(n => store.Latest(n) is InstalledKeg keg && VersionOrder.Instance.Compare(keg.Receipt.Version, Metadata.Version(repository.Load(n))) < 0)
        .Order(StringComparer.Ordinal).ToArray();

    public int Outdated(CommandArguments args)
    {
        var names = OutdatedNames(args.Names.Count == 0 ? null : args.Names);
        if (args.Has("--json"))
            Console.WriteLine(JsonSerializer.Serialize(new { formulae = names.Select(n => new { name = n, installed_versions = store.Kegs(n).Select(k => k.Receipt.Version),
                current_version = Metadata.Version(repository.Load(n)), pinned = store.IsPinned(n) }), casks = Array.Empty<object>() }, new JsonSerializerOptions { WriteIndented = true }));
        else foreach (var name in names)
        {
            if (args.Has("--verbose", "-v") || !Console.IsOutputRedirected)
                Console.WriteLine($"{name} ({string.Join(", ", store.Kegs(name).Select(k => k.Receipt.Version))}) < {Metadata.Version(repository.Load(name))}" + (store.IsPinned(name) ? $" [pinned at {store.Latest(name).Receipt.Version}]" : ""));
            else Console.WriteLine(name);
        }
        return args.Names.Count > 0 && names.Length > 0 ? 1 : 0;
    }

    public int Deps(CommandArguments args)
    {
        var circular = false;
        var names = args.Names.Count > 0 ? args.Names : args.Has("--installed") ? store.All.Select(k => k.Receipt.Name).Distinct().ToList() : repository.Names.ToList();
        foreach (var name in names)
        {
            var visited = new HashSet<string>();
            var active = new HashSet<string>();
            void Visit(string current, string prefix)
            {
                active.Add(current);
                var dependencies = Dependencies(repository.Load(current)).Where(d => Include(d, args)).ToArray();
                for (var i = 0; i < dependencies.Length; i++)
                {
                    var dependency = dependencies[i];
                    var last = i == dependencies.Length - 1;
                    var isCircular = active.Contains(dependency.Name);
                    var repeated = !visited.Add(dependency.Name);
                    var pruned = args.Has("--prune") && repeated;
                    if (args.Has("--tree"))
                    {
                        Console.WriteLine(prefix + (last ? "└── " : "├── ") + dependency.Name
                            + (isCircular ? " (CIRCULAR DEPENDENCY)" : pruned ? " (PRUNED)" : ""));
                        circular |= isCircular;
                    }
                    if (isCircular || pruned || !args.Has("--tree") && repeated) continue;
                    if (!args.Has("--direct")) Visit(dependency.Name, prefix + (last ? "    " : "│   "));
                }
                active.Remove(current);
            }
            if (args.Has("--tree")) Console.WriteLine(name);
            Visit(name, "");
            if (args.Has("--tree")) Console.WriteLine();
            if (!args.Has("--tree"))
            {
                var result = visited.Order(StringComparer.Ordinal).ToArray();
                if (args.Names.Count == 1) foreach (var dependency in result) Console.WriteLine(dependency);
                else Console.WriteLine(name + ": " + string.Join(" ", result));
            }
        }
        return circular ? 1 : 0;
    }

    public void Uses(CommandArguments args)
    {
        args.RequireNames("uses");
        var targets = args.Names.Select(n => repository.Load(n).Name).ToHashSet();
        var candidates = args.Has("--installed") ? store.All.Select(k => k.Receipt.Name).Distinct() : repository.Names;
        foreach (var name in candidates)
        {
            var seen = new HashSet<string>();
            var reached = new HashSet<string>();
            void Visit(string current)
            {
                if (!seen.Add(current)) return;
                foreach (var dependency in Dependencies(repository.Load(current)).Where(d => Include(d, args)))
                {
                    reached.Add(dependency.Name);
                    if (args.Has("--recursive")) Visit(dependency.Name);
                }
            }
            Visit(name);
            if (targets.All(reached.Contains)) Console.WriteLine(name);
        }
    }

    public int Missing(CommandArguments args)
    {
        if (!Directory.Exists(paths.Cellar)) return 0;
        var installed = store.All.GroupBy(k => k.Receipt.Name).ToDictionary(g => g.Key, g => g.Last());
        var names = args.Names.Count == 0 ? installed.Keys.Order(StringComparer.Ordinal).ToArray()
            : args.Names.Select(n => repository.Load(n).Name).Distinct().Order(StringComparer.Ordinal).ToArray();
        var hidden = args.Array("--hide").ToHashSet(StringComparer.Ordinal);
        var failed = false;
        foreach (var name in names)
        {
            var seen = new HashSet<string>(); var missing = new List<string>();
            void Visit(string current)
            {
                if (!seen.Add(current) || !installed.TryGetValue(current, out var keg)) return;
                foreach (var dependency in keg.Receipt.Dependencies ?? [])
                {
                    var baseName = dependency.Split('/')[^1];
                    if (hidden.Contains(baseName) || !Directory.Exists(paths.Rack(baseName))
                        && !Directory.Exists(Path.Combine(paths.Prefix, "opt", baseName))) missing.Add(dependency);
                    else Visit(baseName);
                }
            }
            Visit(name);
            if (missing.Count == 0) continue;
            Console.WriteLine((names.Length > 1 ? name + ": " : "") + string.Join(" ", missing.Distinct()));
            failed = true;
        }
        return failed ? 1 : 0;
    }

    public int SuggestInstall(string name, bool formulaOnly)
    {
        if (name == "updog") { CommandOutput.Error("What's updog?"); return 1; }
        var fuzzy = Suggestions.Correct(name, repository.Names);
        CommandOutput.Warning($"No available formula or cask with the name \"{name}\"."
            + (fuzzy.Length == 0 ? "" : " Did you mean " + Suggestions.Sentence(fuzzy, "or") + "?"));
        if (name.Contains('/')) return 1;
        var types = formulaOnly ? "formulae" : "formulae and casks";
        CommandOutput.Header($"Searching for similarly named {types}...");
        static string Simplify(string value) => Regex.Replace(value.ToLowerInvariant(), "[^a-z0-9@+]", "");
        var simplified = Simplify(name);
        var matches = repository.Names.Where(n => Simplify(n).Contains(simplified)).Concat(fuzzy).Distinct().ToArray();
        if (matches.Length == 0) { CommandOutput.Error($"No {(formulaOnly ? "formulae" : "formulae or casks")} found for {name}."); return 1; }
        CommandOutput.Header("Formulae");
        foreach (var match in matches) Console.WriteLine(match);
        Console.WriteLine($"\nTo install {matches[0]}, run:\n  brew install {matches[0]}");
        return 1;
    }

    public string[] Leaves(CommandArguments args)
    {
        var all = store.All.GroupBy(k => k.Receipt.Name).Select(g => g.Last()).ToArray();
        var dependencies = all.SelectMany(k => k.Receipt.Dependencies ?? []).Select(Paths.FormulaName).ToHashSet();
        return all.Where(k => !dependencies.Contains(k.Receipt.Name)
            && (!args.Has("--installed-on-request") || k.Receipt.InstalledOnRequest)
            && (!args.Has("--installed-as-dependency") || !k.Receipt.InstalledOnRequest)).Select(k => k.Receipt.Name).Order(StringComparer.Ordinal).ToArray();
    }

    string TapFor(string name)
    {
        var directory = Path.GetRelativePath(paths.Taps, repository.Find(name)).Split(Path.DirectorySeparatorChar)[0];
        var separator = directory.IndexOf('-');
        return separator < 0 ? directory : directory[..separator] + "/" + directory[(separator + 1)..];
    }
    string Source(string name) => Path.GetFullPath(repository.Find(name));
    static Regex Pattern(string query) => new(query.Length > 1 && query.StartsWith('/') && query.EndsWith('/') ? query[1..^1] : Regex.Escape(query), RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
    public sealed record DependencyView(string Name, string[] Tags) { public bool Runtime => !Tags.Intersect(new[] { "build", "test", "optional" }).Any(); }
    public static IEnumerable<DependencyView> Dependencies(FormulaDefinition formula)
    {
        var entries = Metadata.Get(formula, "DependencyDetails", "TaggedDependencies");
        var spec = Metadata.Get(formula, "Spec");
        if (entries == null && spec != null) entries = Metadata.Get(spec, "Dependencies");
        if (entries is IEnumerable list)
            foreach (var dependency in list.Cast<object>())
                yield return new(Metadata.Text(dependency, "Name") ?? dependency.ToString(), Metadata.Strings(dependency, "Tags"));
        else foreach (var name in formula.Dependencies) yield return new(name, []);
    }
    static bool Include(DependencyView dependency, CommandArguments args) => (dependency.Runtime || args.Has("--include-build") && dependency.Tags.Contains("build")
        || args.Has("--include-test") && dependency.Tags.Contains("test") || args.Has("--include-optional") && dependency.Tags.Contains("optional"))
        && (!args.Has("--skip-recommended") || !dependency.Tags.Contains("recommended"));
}
