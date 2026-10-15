using System.Text.RegularExpressions;

namespace Brew.Ruby.DevCmd;

[DevCommand("audit")]
sealed class AuditCmd : DevCommand
{
    public override CommandParser Args() => new CommandParser("audit")
        .Description("""
            Check <formula> or <cask> for Homebrew coding style violations. This should be run
            before submitting a new formula or cask. If no <formula> or <cask> are provided, check
            all locally available formulae and casks and skip style checks. Will exit with a
            non-zero status if any errors are found.
            """)
        .Flag(new[] { "--os=" }, "Audit the given operating system. (Pass `all` to audit all operating systems.)")
        .Flag(new[] { "--arch=" }, "Audit the given CPU architecture. (Pass `all` to audit all architectures.)")
        .Switch(new[] { "--strict" }, "Run additional, stricter style checks.")
        .Switch(new[] { "--git" }, "Run additional, slower style checks that navigate the Git repository.")
        .Switch(new[] { "--online" }, "Run additional, slower style checks that require a network connection.")
        .Switch(new[] { "--installed" }, "Only check formulae and casks that are currently installed.")
        .Switch(new[] { "--eval-all" }, null, hidden: true)
        .Switch(new[] { "--new" }, "Run various additional style checks to determine if a new formula or cask is eligible " +
                                   "for Homebrew. This should be used when creating new formulae or casks and implies " +
                                   "`--strict` and `--online`.")
        .Switch(new[] { "--[no-]signing" }, null, hidden: true)
        .Switch(new[] { "--changed" }, "Check files that were changed from the `main` branch.")
        .Flag(new[] { "--tap=" }, "Check formulae and casks within the given tap, specified as <user>`/`<repo>.")
        .Switch(new[] { "--fix" }, "Fix style violations automatically using RuboCop's auto-correct feature. " +
                                   "When passed with `--online` for casks, also correct the `depends_on macos:` " +
                                   "stanza and the case of artifact stanzas.")
        .Switch(new[] { "--display-cop-names" }, null, hidden: true)
        .Switch(new[] { "--display-filename" }, "Prefix every line of output with the file or formula name being audited, to " +
                                                "make output easy to grep.")
        .Switch(new[] { "--skip-style" }, "Skip running non-RuboCop style checks. Useful if you plan on running " +
                                          "`brew style` separately. Enabled by default unless a formula is specified by name.")
        .Switch(new[] { "-D", "--audit-debug" }, "Enable debugging and profiling of audit methods.")
        .CommaArray("--only", "Specify a comma-separated <method> list to only run the methods named `audit_`<method>.")
        .CommaArray("--except", "Specify a comma-separated <method> list to skip running the methods named `audit_`<method>.")
        .CommaArray("--only-cops", "Specify a comma-separated <cops> list to check for violations of only the listed RuboCop cops.")
        .CommaArray("--except-cops", "Specify a comma-separated <cops> list to skip checking for violations of the listed RuboCop cops.")
        .Switch(new[] { "--formula", "--formulae" }, "Treat all named arguments as formulae.")
        .Switch(new[] { "--cask", "--casks" }, "Treat all named arguments as casks.")
        .Conflicts("--installed", "--eval-all", "--changed", "--tap")
        .Conflicts("--only", "--except")
        .Conflicts("--only-cops", "--except-cops", "--strict")
        .Conflicts("--only-cops", "--except-cops", "--only")
        .Conflicts("--formula", "--cask")
        .NamedArgs(new[] { "formula", "cask" });

    public override void Run(ParsedArgs args)
    {
        if (args.Switch("eval-all"))
            throw new BrewError("Calling `brew audit --eval-all` is disabled! Use the default trusted-tap behaviour instead.");
        if (args.SwitchOrNull("signing") != null)
            throw new BrewError("Calling `brew audit --signing` is disabled! There is no replacement.");

        bool strict = args.Switch("new") || args.Switch("strict");
        bool online = args.Switch("new") || args.Switch("online");
        var tap = args.Flag("tap");
        bool tapAudit = !string.IsNullOrEmpty(tap);
        bool skipStyle = args.Switch("skip-style") || args.Named.Count == 0 || tapAudit;
        bool noNamed = false;

        var paths = new List<string>();
        if (args.Switch("changed"))
        {
            var tapDir = StyleCheck.TapDir;
            var cwd = Path.GetFullPath(Environment.CurrentDirectory);
            if (!(cwd + Path.DirectorySeparatorChar).StartsWith(tapDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new BrewError("`brew audit --changed` must be run inside a tap!");
            noNamed = true;
            var git = DevCommands.Which("git");
            if (git != null)
            {
                var baseRef = DevCommands.Capture(git, new[] { "-C", tapDir, "merge-base", "origin/HEAD", "HEAD" }).stdout.Trim();
                if (baseRef.Length == 0) baseRef = "main";
                var top = DevCommands.Capture(git, new[] { "-C", tapDir, "rev-parse", "--show-toplevel" }).stdout.Trim();
                foreach (var f in DevCommands.Capture(git, new[] { "-C", tapDir, "diff", "--name-only", "--no-relative", baseRef }).stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!f.EndsWith(".rb")) continue;
                    var abs = Path.GetFullPath(Path.Combine(top, f));
                    if (File.Exists(abs) && abs.StartsWith(DevCommands.CoreTapFormulaDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                        paths.Add(abs);
                }
            }
        }
        else if (tapAudit)
        {
            if (!Regex.IsMatch(tap, @"^[\w-]+/[\w-]+$")) throw new BrewError($"Invalid tap name: '{tap}'");
            if (tap.Equals("homebrew/core", StringComparison.OrdinalIgnoreCase)) paths.AddRange(DevCommands.AllFormulaFiles());
        }
        else if (args.Switch("installed"))
        {
            noNamed = true;
            paths.AddRange(DevCommands.AllFormulaFiles().Where(p => DevCommands.InstalledKegs(Path.GetFileNameWithoutExtension(p)).Count > 0));
        }
        else if (args.Named.Count == 0)
        {
            noNamed = true;
            paths.AddRange(DevCommands.AllFormulaFiles());
        }
        else
        {
            if (args.Named.Any(n => n.EndsWith(".rb")))
                throw new BrewError("Calling `brew audit [path ...]` is disabled! Use `brew audit [name ...]` instead.");
            foreach (var n in args.Named)
            {
                if (args.Switch("cask")) throw new BrewError($"Cask '{n}' is unavailable: No Cask with this name exists.");
                var p = DevCommands.FormulaPath(n) ?? throw new BrewError($"No available formula or cask with the name \"{DevCommands.NameOf(n)}\".");
                paths.Add(p);
            }
        }

        var formulae = new List<FormulaSpec>();
        foreach (var p in paths.Distinct())
        {
            try { formulae.Add(FormulaLoader.LoadSpec(p)); }
            catch (Exception e) when (e is not OutOfMemoryException)
            {
                var msg = e is InvalidOperationException ? e.Message : $"{e.Message} ({e.GetType().Name})";
                if (!noNamed && !tapAudit) throw new BrewError(msg);
                Out.Onoe($"Failed to import: {Path.GetFileNameWithoutExtension(p)}");
                Console.Error.WriteLine(msg);
            }
        }

        if (formulae.Count == 0 && !tapAudit)
        {
            DevCommands.Ofail("No matching formulae or casks to audit!");
            return;
        }

        var onlyCops = args.Array("only_cops")?.ToList();
        var exceptCops = args.Array("except_cops")?.ToList();
        List<string> styleOnly = null, styleExcept = null;
        if (onlyCops != null) styleOnly = onlyCops;
        else if (args.Switch("new")) { }
        else if (exceptCops != null) styleExcept = exceptCops;
        else if (!strict) styleExcept = new List<string> { "FormulaAuditStrict" };

        Dictionary<string, List<CopOffense>> styleOffenses = null;
        if (!skipStyle) styleOffenses = StyleCheck.CheckStyleJson(paths, args.Switch("fix"), styleOnly, styleExcept);

        var only = onlyCops != null ? new List<string> { "style" } : args.Array("only")?.ToList();
        var problems = new List<(string name, string path, List<AuditProblem> list)>();
        foreach (var f in formulae.OrderBy(f => f.Name, StringComparer.Ordinal))
        {
            var fa = new FormulaAuditor(f)
            {
                NewFormula = args.Switch("new") && !f.Name.Contains('@'),
                NewFormulaInclusive = args.Switch("new"),
                Strict = strict, Online = online, Git = args.Switch("git"),
                Only = only, Except = args.Array("except")?.ToList(),
                StyleOffenses = styleOffenses == null ? null : styleOffenses.GetValueOrDefault(f.Path) ?? new List<CopOffense>(),
            };
            fa.Audit();
            var all = fa.Problems.Concat(fa.NewFormulaProblems).Distinct().ToList();
            if (all.Count > 0) problems.Add((f.Name, f.Path, all));
        }

        foreach (var (name, path, list) in problems)
        {
            var lines = list.Select(FormatProblem).ToList();
            if (args.Switch("display-filename"))
                foreach (var l in lines) Console.WriteLine($"{path}: {l}");
            else
            {
                Console.WriteLine(name);
                foreach (var l in lines) Console.WriteLine("  " + l);
            }
        }

        int formulaCount = problems.Count;
        int total = problems.Sum(p => p.list.Count);
        int corrected = problems.Sum(p => p.list.Count(x => x.Corrected));
        if (total > 0)
        {
            var summary = Out.Pluralize("problem", total, true);
            var sources = new List<string>();
            if (formulaCount > 0) sources.Add(Out.Pluralize("formula", formulaCount, true));
            if (sources.Count > 0) summary += " in " + Out.ToSentence(sources);
            summary += " detected";
            if (corrected > 0) summary += $", {Out.Pluralize("problem", corrected, true)} corrected";
            if (total == corrected) Out.Opoo(summary + ".");
            else DevCommands.Ofail(summary + ".");
        }

        if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != null)
            foreach (var (_, path, list) in problems)
                foreach (var p in list.Where(p => !p.Corrected))
                {
                    var loc = p.Line != null ? $" file={path},line={p.Line}" + (p.Column != null ? $",col={p.Column}" : "") : $" file={path}";
                    Console.WriteLine($"::error{loc}::{p.Message.Replace("%", "%25").Replace("\r", "%0D").Replace("\n", "%0A")}");
                }
    }

    static string FormatProblem(AuditProblem p)
    {
        var status = p.Corrected ? " " + Formatter.Success("[corrected]") : "";
        var location = p.Line != null ? $"line {p.Line}" + (p.Column != null ? $", col {p.Column}" : "") + ": " : "";
        var message = p.Message.TrimEnd('\n').Replace("\n", "\n    ");
        return $"* {location}{message}{status}";
    }
}

sealed record AuditProblem(string Message, int? Line = null, int? Column = null, bool Corrected = false);

// Port of Homebrew's FormulaAuditor + ResourceAuditor for this tap (homebrew/core rules apply).
// Online audits (`--online`) need network APIs Homebrew talks to (GitHub, endoflife.date); they're accepted and skipped.
sealed class FormulaAuditor
{
    readonly FormulaSpec f;
    readonly string text;
    readonly RbSource src;
    public bool NewFormula, NewFormulaInclusive, Strict, Online, Git;
    public List<string> Only, Except;
    public List<CopOffense> StyleOffenses;
    public readonly List<AuditProblem> Problems = new(), NewFormulaProblems = new();
    readonly bool coreTap;
    bool Versioned => f.Name.Contains('@');
    List<RbStmt> Body => src.FormulaClass?.Body ?? new List<RbStmt>();

    public FormulaAuditor(FormulaSpec formula)
    {
        f = formula;
        text = File.ReadAllText(formula.Path);
        src = new RbSource(formula.Path, text);
        coreTap = formula.Path.StartsWith(DevCommands.CoreTapFormulaDir, StringComparison.OrdinalIgnoreCase);
    }

    void Problem(string m) => Problems.Add(new AuditProblem(m));
    void NewProblem(string m) => NewFormulaProblems.Add(new AuditProblem(m));

    public void Audit()
    {
        var audits = new (string name, Action run)[]
        {
            ("style", AuditStyle), ("file", AuditFile), ("synced_versions_formulae", () => { }), ("name", AuditName),
            ("license", AuditLicense), ("deps", AuditDeps), ("node_modules", AuditNodeModules), ("conflicts", AuditConflicts),
            ("gcc_dependency", () => { }), ("glibc", () => { }), ("relicensed_formulae", () => { }),
            ("versioned_keg_only", AuditVersionedKegOnly), ("homepage", () => { }), ("homepage_domain_age", () => { }),
            ("duplicate_formula", () => { }), ("bottle_spec", AuditBottleSpec), ("eol", () => { }), ("wayback_url", AuditWaybackUrl),
            ("github_repository_archived", () => { }), ("gitlab_repository_archived", () => { }), ("forgejo_repository_archived", () => { }),
            ("github_repository", () => { }), ("gitlab_repository", () => { }), ("bitbucket_repository", () => { }),
            ("forgejo_repository", () => { }), ("specs", AuditSpecs), ("stable_version", AuditStableVersion),
            ("revision", AuditRevision), ("compatibility_version", () => { }), ("version_scheme", AuditVersionScheme),
            ("unconfirmed_checksum_change", AuditUnconfirmedChecksumChange), ("text", AuditText),
            ("reverse_migration", () => { }), ("prefix_has_contents", AuditPrefixHasContents), ("deprecate_disable", AuditDeprecateDisable),
        };
        foreach (var (name, run) in audits)
        {
            if (Only != null && !Only.Contains(name)) continue;
            if (Except != null && Except.Contains(name)) continue;
            run();
        }
    }

    void AuditStyle()
    {
        if (StyleOffenses == null) return;
        foreach (var o in StyleOffenses)
            Problems.Add(new AuditProblem(o.Message, o.Line, o.Column, o.Corrected));
    }

    static string Shard(string name) => name.StartsWith("lib") ? "lib" : name[..1];

    static string NewFormulaPath(string name)
    {
        var dir = DevCommands.CoreTapFormulaDir;
        var lower = name.ToLowerInvariant();
        var sub = Path.Combine(dir, Shard(lower));
        return Directory.Exists(sub) ? Path.Combine(sub, lower + ".rb") : Path.Combine(dir, lower + ".rb");
    }

    void AuditFile()
    {
        if (coreTap && Versioned)
        {
            var unversioned = Regex.Replace(f.Name, "@.*$", "");
            // Formulary.factory also resolves aliases (e.g. openssl -> openssl@3)
            var aliasDir = Path.Combine(Path.GetDirectoryName(DevCommands.CoreTapFormulaDir)!, "Aliases");
            if (DevCommands.FormulaPath(unversioned) == null
                && !(Directory.Exists(aliasDir) && Directory.EnumerateFileSystemEntries(aliasDir).Any(a => Path.GetFileName(a) == unversioned)))
                Problem($"{f.Name} is versioned but no {unversioned} formula exists");
        }
        else if (!Versioned)
        {
            var others = DevCommands.AllFormulaFiles().Select(Path.GetFileNameWithoutExtension)
                .Where(n => n.StartsWith(f.Name + "@", StringComparison.Ordinal)).ToList();
            var versions = new List<(string name, string version)>();
            foreach (var o in others)
                if (Load(o) is { } spec) versions.Add((o, spec.Version));
            versions.Sort((a, b) => Versions.Compare(b.version, a.version));
            if (versions.Count > 0)
            {
                var lastAliasVersion = versions[^1].name.Split('@')[^1];
                var parts = f.Version.Split('.');
                var major = $"{f.Name}@{parts[0]}";
                var majorMinor = parts.Length > 1 ? $"{f.Name}@{parts[0]}.{parts[1]}" : major;
                var aliasName = lastAliasVersion.Split('.').Length == 1 ? major : majorMinor;
                var versionedAliases = DevCommands.Aliases(f.Name).Where(x => System.Text.RegularExpressions.Regex.IsMatch(x, @".@\d")).ToList();
                var validAliases = versionedAliases.Intersect(new[] { major, majorMinor }).ToList();
                if (validAliases.Count == 0 && aliasName != versions[0].name)
                {
                    var aliasDir = Path.Combine(StyleCheck.TapDir, "Aliases");
                    var rel = f.Path.Replace(StyleCheck.TapDir, "..");
                    Problem($"Formula has other versions so create a versioned alias:\n  cd {aliasDir}\n  ln -s {rel} {aliasName}\n");
                }
            }
        }
        if (!coreTap) return;
        var expected = NewFormulaPath(f.Name);
        if (!string.Equals(Path.GetFullPath(f.Path), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase))
            Problem($"Formula is in wrong path:\n  Expected: {expected}\n    Actual: {f.Path}\n");
    }

    void AuditName()
    {
        var name = f.Name;
        var errors = new List<string>();
        if (Regex.IsMatch(name, "[A-Z]")) errors.Add("uppercase letters");
        if (Regex.IsMatch(name, @"\s")) errors.Add("whitespace");
        if (name.Any(ch => ch > 127)) errors.Add("non-ASCII characters");
        if (name.Contains("--")) errors.Add("double hyphens");
        if (name.StartsWith('@')) errors.Add("a leading @");
        if (name.EndsWith('@')) errors.Add("a trailing @");
        if (name.StartsWith('-')) errors.Add("a leading hyphen");
        if (name.EndsWith('-')) errors.Add("a trailing hyphen");
        if (name.Count(ch => ch == '@') > 1) errors.Add("multiple @ symbols");
        if (name.Contains("-@")) errors.Add("a hyphen followed by an @");
        if (name.Contains("@-")) errors.Add("an @ followed by a hyphen");
        if (errors.Count > 0) Problem($"Formula name '{name}' must not contain {Out.ToSentence(errors, "or")}.");
    }

    // ---- licenses ----

    static readonly string[] IncompatibleLicenses = { "Aladdin", "CPOL-1.02", "gSOAP-1.3b", "JSON", "MS-LPL", "OPL-1.0" };
    static readonly string[] IncompatiblePrefixes = { "BUSL", "CC-BY-NC", "Elastic", "SSPL" };
    static readonly string[] AllowedLicenseSymbols = { "public_domain", "cannot_represent", "truncated" };

    static Dictionary<string, bool> licenseIndex;
    static HashSet<string> exceptionIndex;

    static Dictionary<string, bool> LicenseIndex => licenseIndex ??= SpdxLicenseIds.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .ToDictionary(x => x.TrimStart('!').ToLowerInvariant(), x => x.StartsWith('!'));
    static HashSet<string> ExceptionIndex => exceptionIndex ??= SpdxExceptionIds.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Select(x => x.ToLowerInvariant()).ToHashSet();

    // Licenses (value, is a symbol) and exceptions from the `license` call's source.
    (List<(string v, bool sym)> licenses, List<string> exceptions)? LicenseExpression()
    {
        var lic = Body.FirstOrDefault(s => s.Kind == "send" && s.Name == "license");
        if (lic == null) return null;
        var licenses = new List<(string, bool)>();
        var exceptions = new List<string>();
        int start = src.ArgStart(lic, out _);
        var items = new List<(int pos, string v, bool sym, bool exc)>();
        foreach (var s in src.StringsIn(lic.Start, lic.End))
        {
            var before = new string(src.Mask, start, Math.Max(0, s.Start - start));
            items.Add((s.Start, s.Content, false, Regex.IsMatch(before, @"\bwith:\s*$")));
        }
        var masked = new string(src.Mask, start, Math.Max(0, lic.End - start));
        foreach (Match m in Regex.Matches(masked, @"(?<![\w:]):([a-z_]\w*)\b(?!:)"))
            items.Add((start + m.Index, m.Groups[1].Value, true, false));
        foreach (var it in items.OrderBy(x => x.pos))
            if (it.exc) exceptions.Add(it.v);
            else licenses.Add((it.v, it.sym));
        return (licenses, exceptions);
    }

    static string Inspect(IEnumerable<(string v, bool sym)> l) => "[" + string.Join(", ", l.Select(x => x.sym ? ":" + x.v : $"\"{x.v}\"")) + "]";
    static string Inspect(IEnumerable<string> l) => "[" + string.Join(", ", l.Select(x => $"\"{x}\"")) + "]";

    static bool ValidLicense((string v, bool sym) l) => l.sym ? AllowedLicenseSymbols.Contains(l.v) : LicenseIndex.ContainsKey(l.v.TrimEnd('+').ToLowerInvariant());
    static bool DeprecatedLicense((string v, bool sym) l) => !l.sym && LicenseIndex.GetValueOrDefault(l.v.TrimEnd('+').ToLowerInvariant());

    void AuditLicense()
    {
        var expr = LicenseExpression();
        if (expr != null || !string.IsNullOrEmpty(f.License))
        {
            var (licenses, exceptions) = expr ?? (new List<(string, bool)> { (f.License, false) }, new List<string>());
            var incompatible = licenses.Where(l => !l.sym && (IncompatiblePrefixes.Any(p => l.v.StartsWith(p)) || IncompatibleLicenses.Contains(l.v))).ToList();
            if (incompatible.Count > 0 && coreTap)
                Problem($"Formula {f.Name} contains incompatible licenses: {Inspect(incompatible)}.\n" +
                        "Formulae in homebrew/core must either use a Debian Free Software Guidelines license\n" +
                        $"or be released into the public domain: {Formatter.Url("https://docs.brew.sh/Licence-Guidelines")}\n");
            var nonStandard = licenses.Where(l => !ValidLicense(l)).ToList();
            if (nonStandard.Count > 0)
                Problem($"Formula {f.Name} contains non-standard SPDX licenses: {Inspect(nonStandard)}.\n" +
                        $"For a list of valid licenses check: {Formatter.Url("https://spdx.org/licenses/")}\n");
            if (Strict || coreTap)
            {
                var deprecated = licenses.Where(DeprecatedLicense).ToList();
                if (deprecated.Count > 0)
                    Problem($"Formula {f.Name} contains deprecated SPDX licenses: {Inspect(deprecated)}.\n" +
                            "You may need to add `-only` or `-or-later` for GNU licenses (e.g. `GPL`, `LGPL`, `AGPL`, `GFDL`).\n" +
                            $"For a list of valid licenses check: {Formatter.Url("https://spdx.org/licenses/")}\n");
            }
            var invalidExceptions = exceptions.Where(e => !ExceptionIndex.Contains(e.ToLowerInvariant())).ToList();
            if (invalidExceptions.Count > 0)
                Problem($"Formula {f.Name} contains invalid or deprecated SPDX license exceptions: {Inspect(invalidExceptions)}.\n" +
                        "For a list of valid license exceptions check:\n" +
                        $"  {Formatter.Url("https://spdx.org/licenses/exceptions-index.html")}\n");
        }
        else if (coreTap && !f.Disabled)
            Problem("Formulae in homebrew/core must specify a license.");
    }

    // ---- dependencies / conflicts ----

    static readonly string[] ReservedTags = { "build", "optional", "recommended", "run", "test", "linked", "implicit", "no_linkage" };
    static readonly Dictionary<string, FormulaSpec> cache = new();

    internal static FormulaSpec Load(string name)
    {
        if (cache.TryGetValue(name, out var s)) return s;
        var p = DevCommands.FormulaPath(name);
        FormulaSpec spec = null;
        if (p != null)
            try { spec = FormulaLoader.LoadSpec(p); } catch (Exception) { }
        return cache[name] = spec;
    }

    internal static List<string> RecursiveRuntimeDeps(FormulaSpec root)
    {
        var seen = new List<string>();
        var stack = new Stack<FormulaSpec>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var cur = stack.Pop();
            foreach (var d in cur.Dependencies.Where(d => d.Runtime))
            {
                var n = DevCommands.NameOf(d.Name);
                if (seen.Contains(n)) continue;
                seen.Add(n);
                if (Load(n) is { } df) stack.Push(df);
            }
        }
        return seen;
    }

    void AuditDeps()
    {
        foreach (var dep in f.Dependencies)
        {
            if (dep.Name.Count(c => c == '/') >= 2 && !dep.Name.StartsWith("homebrew/core/")) continue;
            var df = Load(DevCommands.NameOf(dep.Name));
            if (df == null)
            {
                Problem($"Can't find dependency '{dep.Name}'.");
                continue;
            }
            if (NewFormula && dep.Name == "git") Problem("Don't use 'git' as a dependency (it's always available)");
            foreach (var tag in dep.Tags)
            {
                if (tag is "run" or "linked") Problem($"Dependency '{dep.Name}' is marked as :{tag}. Remove :{tag}; it is a no-op.");
                else if (!ReservedTags.Contains(tag)) Problem($"Dependency '{dep.Name}' is marked as :{tag} which is not a valid tag.");
            }
            if (!coreTap) continue;
            if (df.Deprecated && !f.Deprecated && !f.Disabled)
                Problem($"Dependency '{dep.Name}' is deprecated but has un-deprecated dependents. Either\n" +
                        $"un-deprecate '{dep.Name}' or deprecate it and all of its dependents.\n");
            if (df.Disabled && !f.Disabled)
                Problem($"Dependency '{dep.Name}' is disabled but has un-disabled dependents. Either\n" +
                        $"un-disable '{dep.Name}' or disable it and all of its dependents.\n");
            if (dep.Recommended || dep.Optional)
                Problem("Formulae in homebrew/core should not have optional or recommended dependencies");
        }
        if (!coreTap) return;
        var versionHash = new Dictionary<string, List<string>>();
        var conflicts = new List<string>();
        foreach (var name in RecursiveRuntimeDeps(f))
        {
            var unversioned = name.Split('@')[0];
            if (!versionHash.TryGetValue(unversioned, out var set)) versionHash[unversioned] = set = new List<string>();
            if (!set.Contains(name)) set.Add(name);
            if (set.Count < 2) continue;
            foreach (var n in set) if (!conflicts.Contains(n)) conflicts.Add(n);
        }
        if (conflicts.Count == 0 || f.Disabled) return;
        if (f.Deprecated && !(f.DeprecationReason is RSymbol { Name: "versioned_formula" })) return;
        Problem($"{f.Name} contains conflicting version recursive dependencies:\n  {string.Join(", ", conflicts)}\nView these with `brew deps --tree {f.Name}`.\n");
    }

    void AuditNodeModules()
    {
        if (!coreTap) return;
        var nm = Path.Combine(DevCommands.Cellar, f.Name, f.PkgVersion, "libexec", "lib", "node_modules");
        if (!Directory.Exists(nm)) return;
        foreach (var package in new[] { "@anthropic-ai/claude-agent-sdk", "@github/copilot" })
        {
            var leaf = package.Replace('/', Path.DirectorySeparatorChar);
            bool found = Directory.Exists(Path.Combine(nm, leaf)) ||
                         Directory.EnumerateDirectories(nm, "node_modules", SearchOption.AllDirectories).Any(d => Directory.Exists(Path.Combine(d, leaf)));
            if (!found) continue;
            Problem($"Formula {f.Name} uses {package} which has an incompatible license.\n" +
                    $"All installed npm dependencies must satisfy {Formatter.Url("https://docs.brew.sh/Licence-Guidelines")}\n");
        }
    }

    void AuditConflicts()
    {
        foreach (var c in f.Conflicts)
        {
            if (c.Name.Contains('/') && !c.Name.StartsWith("homebrew/core/")) continue;
            var cf = Load(DevCommands.NameOf(c.Name));
            if (cf == null)
            {
                Problem($"Can't find conflicting formula \"{c.Name}\".");
                continue;
            }
            if (cf.Name == f.Name) Problem("Formula should not conflict with itself");
            bool reverse = cf.Conflicts.Any(rc => DevCommands.NameOf(rc.Name) == f.Name);
            if (!reverse) Problem($"Formula {cf.Name} should also have a conflict declared with {f.Name}");
        }
    }

    // Tap#audit_exception: tap/<tap>/audit_exceptions/<list>.json is a JSON array of formula names, or an object
    // whose values are compared with `value` (an array value includes it).
    static bool AuditException(string list, string name, string value = null)
    {
        var file = Path.Combine(StyleCheck.TapDir, "audit_exceptions", list + ".json");
        if (!File.Exists(file)) return false;
        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(file));
        var root = doc.RootElement;
        if (root.ValueKind == System.Text.Json.JsonValueKind.Array) return root.EnumerateArray().Any(e => e.GetString() == name);
        if (root.ValueKind != System.Text.Json.JsonValueKind.Object || !root.TryGetProperty(name, out var entry)) return false;
        if (string.IsNullOrEmpty(value)) return entry.ValueKind is not (System.Text.Json.JsonValueKind.False or System.Text.Json.JsonValueKind.Null);
        return entry.ValueKind == System.Text.Json.JsonValueKind.Array
            ? entry.EnumerateArray().Any(e => e.ValueKind == System.Text.Json.JsonValueKind.String && e.GetString() == value)
            : entry.ValueKind == System.Text.Json.JsonValueKind.String && entry.GetString() == value;
    }

    void AuditVersionedKegOnly()
    {
        if (!Versioned || !coreTap) return;
        if (f.KegOnlyReason is RSymbol { Name: "versioned_formula" }) return;
        if (f.KegOnly && (f.Name.StartsWith("openssl") || f.Name.StartsWith("libressl")) && f.KegOnlyReason is RSymbol { Name: "provided_by_macos" or "shadowed_by_macos" }) return;
        if (AuditException("versioned_keg_only_allowlist", f.Name)) return;
        Problem("Versioned formulae in homebrew/core should use `keg_only :versioned_formula`");
    }

    void AuditBottleSpec()
    {
        if (!NewFormulaInclusive || !coreTap) return;
        if (Body.Any(s => s.Name == "bottle" && s.Kind == "block"))
            NewProblem("New formulae in homebrew/core should not have a `bottle do` block");
    }

    void AuditWaybackUrl()
    {
        if (!coreTap || f.Deprecated || f.Disabled) return;
        const string re = @"^https?://web\.archive\.org";
        const string prefix = "Formula with an Internet Archive Wayback Machine";
        if (f.Url != null && Regex.IsMatch(f.Url, re)) Problem($"{prefix} `url` should be deprecated with `:repo_removed`");
        if (f.Homepage != null && Regex.IsMatch(f.Homepage, re)) Problem($"{prefix} `homepage` should find an alternative `homepage` or be deprecated.");
        if (f.HeadUrl != null && Regex.IsMatch(f.HeadUrl, re)) Problem("Remove Internet Archive Wayback Machine `head` URL");
    }

    // ---- specs (ResourceAuditor) ----

    sealed class ResInfo
    {
        public string Name, Url, Version, Using, Tag, Revision, Branch, Module;
        public bool HasChecksum, ExplicitVersion;
        public List<string> Mirrors = new();
    }

    // DownloadStrategyDetector, reduced to the strategy family.
    static string Strategy(string url, string usingSym)
    {
        if (usingSym != null)
            return usingSym switch
            {
                "git" => "git", "svn" => "svn", "hg" => "hg", "bzr" => "bzr", "fossil" => "fossil", "cvs" => "cvs",
                "curl" => "curl", "homebrew_curl" => "homebrew_curl", "post" => "curl_post", "nounzip" => "nounzip", _ => usingSym,
            };
        url ??= "";
        if (Regex.IsMatch(url, @"^https?://.+\.git$") || url.StartsWith("git://") || Regex.IsMatch(url, @"^https?://git\.sr\.ht/[^/]+/[^/]+$")) return "git";
        if (url.StartsWith("svn://") || url.StartsWith("svn+http://") || Regex.IsMatch(url, @"^https?://svn\.")) return "svn";
        if (url.StartsWith("hg://") || url.StartsWith("hg+")) return "hg";
        if (url.StartsWith("bzr://")) return "bzr";
        if (url.StartsWith("fossil://")) return "fossil";
        if (url.StartsWith("cvs://")) return "cvs";
        return "curl";
    }

    static bool IsCurl(string strategy) => strategy is not ("git" or "svn" or "hg" or "bzr" or "fossil" or "cvs");

    static string Opt(string args, string key)
    {
        var m = Regex.Match(args, $@"\b{key}:\s*(?::(\w+)|""([^""]*)"")");
        return m.Success ? m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value : null;
    }

    ResInfo UrlInfo(RbStmt url)
    {
        var args = src.ArgsSource(url);
        return new ResInfo
        {
            Url = src.FirstStringArg(url)?.Content, Using = Opt(args, "using"), Tag = Opt(args, "tag"),
            Revision = Opt(args, "revision"), Branch = Opt(args, "branch"), Module = Opt(args, "module"),
        };
    }

    ResInfo FromCalls(List<RbStmt> body)
    {
        var url = body.FirstOrDefault(s => s.Kind == "send" && s.Name == "url");
        if (url == null) return null;
        var r = UrlInfo(url);
        var v = body.FirstOrDefault(s => s.Kind == "send" && s.Name == "version");
        if (v != null) { r.Version = src.FirstStringArg(v)?.Content; r.ExplicitVersion = r.Version != null; }
        r.HasChecksum = body.Any(s => s.Kind == "send" && s.Name == "sha256" && src.FirstStringArg(s) is { Content.Length: > 0 });
        r.Mirrors = body.Where(s => s.Kind == "send" && s.Name == "mirror").Select(s => src.FirstStringArg(s)?.Content).Where(x => x != null).ToList();
        return r;
    }

    // The stable spec as loaded (it may come from on_windows/on_intel/stable blocks) plus the url's options from the source.
    ResInfo StableInfo()
    {
        if (f.Url == null) return null;
        var urlStmt = src.FormulaClass?.Descendants().FirstOrDefault(s => s.Kind == "send" && s.Name == "url" && src.FirstStringArg(s)?.Content == f.Url
                                                                         && s.Parent?.Name is not ("resource" or "livecheck" or "head"));
        var r = urlStmt != null ? UrlInfo(urlStmt) : new ResInfo();
        r.Url = f.Url;
        r.Version = f.Version;
        r.ExplicitVersion = !f.VersionFromUrl;
        r.HasChecksum = !string.IsNullOrEmpty(f.Sha256);
        r.Mirrors = f.Mirrors.ToList();
        r.Name = f.Name;
        return r;
    }

    ResInfo HeadInfo()
    {
        if (f.HeadUrl == null) return null;
        var h = Body.FirstOrDefault(s => s.Name == "head" && s.Kind is "send" or "block");
        var r = h?.Kind == "block" ? FromCalls(h.Body) ?? new ResInfo() : h != null ? UrlInfo(h) : new ResInfo();
        r.Url = f.HeadUrl;
        r.Version = "HEAD";
        r.ExplicitVersion = false;
        r.Name = f.Name;
        return r;
    }

    List<string> AuditResource(ResInfo r, string specName, bool formulaOwner)
    {
        var problems = new List<string>();
        bool On(string audit) => (Only == null || Only.Contains(audit)) && (Except == null || !Except.Contains(audit));
        if (On("version"))
        {
            if (r.Version == null) problems.Add("Missing version");
            else if (formulaOwner && coreTap && !Regex.IsMatch(r.Version, @"^[a-zA-Z0-9_][a-zA-Z0-9._-]{0,127}$"))
                problems.Add($"`version {r.Version}` does not match ^[a-zA-Z0-9_][a-zA-Z0-9._-]{{0,127}}$");
            else if (r.ExplicitVersion && r.Url != null && Versions.Parse(r.Url) == r.Version)
                problems.Add($"`version {r.Version}` is redundant with version scanned from URL");
        }
        if (On("download_strategy") && r.Url != null)
        {
            var urlStrategy = Strategy(r.Url, null);
            if ((r.Using == "git" || urlStrategy == "git") && r.Tag != null && r.Revision == null)
                problems.Add("Git should specify `revision:` when a `tag:` is specified.");
            if (r.Using != null)
            {
                if (r.Using == "cvs")
                {
                    if (r.Module == r.Name) problems.Add("Redundant `module:` value in URL");
                    if (Regex.IsMatch(r.Url, ":[^/]+$"))
                    {
                        var mod = r.Url.Split(':')[^1];
                        problems.Add(mod == r.Name ? "Redundant CVS module appended to URL" : $"Specify CVS module as `module: \"{mod}\"` instead of appending it to the URL");
                    }
                }
                if (urlStrategy == Strategy("", r.Using)) problems.Add("Redundant `using:` value in URL");
            }
        }
        if (On("checksum") && specName != "head" && IsCurl(Strategy(r.Url, r.Using)) && !r.HasChecksum)
            problems.Add("Checksum is missing");
        if (On("resource_name_matches_pypi_package_name_in_url") && r.Url != null && !formulaOwner &&
            Regex.IsMatch(r.Url, @"^https?://files\.pythonhosted\.org/packages/"))
        {
            string pypi;
            if (r.Url.EndsWith(".whl")) pypi = Path.GetFileName(new Uri(r.Url).AbsolutePath).Split('-', 2)[0];
            else
            {
                var m = Regex.Match(r.Url, "/(?<package_name>[^/]+)-");
                pypi = m.Success ? m.Groups["package_name"].Value : "";
            }
            pypi = Regex.Replace(pypi, "[_.]", "-");
            if (!string.Equals(r.Name, pypi, StringComparison.OrdinalIgnoreCase))
                problems.Add($"`resource` name should be '{pypi}' to match the PyPI package name");
        }
        return problems;
    }

    void AuditSpecs()
    {
        var stable = StableInfo();
        var head = HeadInfo();
        if (stable == null && head != null && coreTap) Problem("HEAD-only (no stable download)");
        foreach (var (label, spec) in new[] { ("Stable", stable), ("HEAD", head) })
        {
            if (spec == null) continue;
            foreach (var m in AuditResource(spec, label == "HEAD" ? "head" : "stable", true)) Problem($"{label}: {m}");
            var resources = src.FormulaClass == null ? new List<RbStmt>()
                : src.FormulaClass.Descendants().Where(s => s.Kind == "block" && s.Name == "resource" && s.Parent?.Name != "test" &&
                                                           (label == "HEAD") == (s.Parent?.Name == "head")).ToList();
            foreach (var res in resources)
            {
                var rname = src.FirstStringArg(res)?.Content;
                if (rname == null) continue;
                if (rname == f.Name) Problem("Resource name should be different from the formula name");
                var ri = FromCalls(res.Body) ?? new ResInfo();
                ri.Name = rname;
                if (ri.Version == null && ri.Url != null) ri.Version = Versions.Parse(ri.Url);
                foreach (var m in AuditResource(ri, label == "HEAD" ? "head" : "stable", false)) Problem($"{label} resource \"{rname}\": {m}");
            }
            bool hasPatches = src.FormulaClass?.Descendants().Any(s => s.Name == "patch" && s.Kind is "send" or "block") == true || src.CodeEnd < text.Length;
            if (hasPatches && NewFormula && coreTap)
                NewProblem("Formulae should not require patches to build. Patches should be submitted and accepted upstream first.");
        }
        if (!coreTap) return;
        if (head != null && Versioned) Problem("Versioned formulae should not have a `head` spec");
        if (stable?.Url == null) return;
        var version = f.Version;
        if (!Regex.IsMatch(version, @"\d")) Problem($"Stable: version ({version}) is set to a string without a digit");
        if (version.StartsWith("HEAD")) Problem($"Stable: non-HEAD version ({version}) should not begin with `HEAD`");
        var url = stable.Url;
        var urlVersion = Versions.Parse(url);
        int minor = 0;
        if (urlVersion != null) int.TryParse(urlVersion.Split('.', '-', '_').ElementAtOrDefault(1), out minor);
        Match mm;
        if ((mm = Regex.Match(url, @"[\d._-](alpha|beta|rc\d)")).Success)
        {
            var versionPrefix = Regex.Replace(version, @"\d+$", "");
            if (!AuditException("unstable_allowlist", f.Name, versionPrefix) && !AuditException("unstable_devel_allowlist", f.Name, versionPrefix))
                Problem($"Stable: version URLs should not contain `{mm.Groups[1].Value}`");
        }
        else if (Regex.IsMatch(url, @"download\.gnome\.org/sources") || Regex.IsMatch(url, @"ftp\.gnome\.org/pub/GNOME/sources", RegexOptions.IgnoreCase))
        {
            if (urlVersion == null || Versions.Compare(urlVersion, "1.0") < 0 || Versions.Compare(urlVersion, "40.0") >= 0 || minor % 2 == 0) return;
            Problem($"Stable: version ({version}) is a development release");
        }
        else if (Regex.IsMatch(url, @"isc\.org/isc/bind\d*/", RegexOptions.IgnoreCase))
        {
            if (minor % 2 == 0) return;
            Problem($"Stable: version ({version}) is a development release");
        }
    }

    // ---- git history (--git) ----

    (Dictionary<string, object> previous, Dictionary<string, object> baseRef)? committed;

    (Dictionary<string, object> previous, Dictionary<string, object> baseRef) CommittedVersionInfo()
    {
        if (committed is { } c) return c;
        var previous = new Dictionary<string, object>();
        var baseInfo = new Dictionary<string, object>();
        committed = (previous, baseInfo);
        var git = DevCommands.Which("git");
        if (!Git || git == null) return committed.Value;
        var tapDir = StyleCheck.TapDir;
        if (DevCommands.Capture(git, new[] { "-C", tapDir, "rev-parse", "--git-dir" }).code != 0) return committed.Value;
        var top = DevCommands.Capture(git, new[] { "-C", tapDir, "rev-parse", "--show-toplevel" }).stdout.Trim();
        var rel = Path.GetRelativePath(top, f.Path).Replace('\\', '/');
        var baseRef = DevCommands.Capture(git, new[] { "-C", tapDir, "merge-base", "origin/HEAD", "HEAD" }).stdout.Trim();
        if (baseRef.Length == 0) baseRef = "origin/HEAD";
        var revs = DevCommands.Capture(git, new[] { "-C", top, "rev-list", "--abbrev-commit", "--remove-empty", baseRef, "--", rel }).stdout
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var tmpDir = Path.Combine(Path.GetTempPath(), "brew-audit-" + Guid.NewGuid().ToString("N"));
        try
        {
            foreach (var rev in revs)
            {
                var show = DevCommands.Capture(git, new[] { "-C", top, "show", $"{rev}:{rel}" });
                if (show.code != 0) continue;
                Directory.CreateDirectory(tmpDir);
                var tmp = Path.Combine(tmpDir, Path.GetFileName(f.Path));
                File.WriteAllText(tmp, show.stdout);
                FormulaSpec old;
                try { old = FormulaLoader.LoadSpec(tmp); } catch (Exception) { break; }
                previous["version"] = old.Version;
                previous["checksum"] = old.Sha256;
                previous["revision"] = old.Revision;
                previous["version_scheme"] = old.VersionScheme;
                baseInfo.TryAdd("url", old.Url);
                baseInfo.TryAdd("version", old.Version);
                baseInfo.TryAdd("checksum", old.Sha256);
                baseInfo.TryAdd("revision", old.Revision);
                baseInfo.TryAdd("version_scheme", old.VersionScheme);
                if (f.Version != old.Version || f.Revision != old.Revision) break;
            }
        }
        finally
        {
            try { if (Directory.Exists(tmpDir)) Directory.Delete(tmpDir, true); } catch (IOException) { }
        }
        return committed.Value;
    }

    void AuditStableVersion()
    {
        if (!Git) return;
        var (prev, baseInfo) = CommittedVersionInfo();
        if (baseInfo.GetValueOrDefault("version") is not string baseVersion) return;
        var cur = f.Version;
        if (Versions.Compare(cur, baseVersion) == 0 && cur != baseVersion)
            Problem($"Stable: version should not change from {baseVersion} to {cur}");
        else if (Versions.Compare(cur, baseVersion) < 0 && Equals(f.VersionScheme, prev.GetValueOrDefault("version_scheme")))
            Problem($"Stable: version should not decrease (from {baseVersion} to {cur})");
    }

    void AuditRevision()
    {
        if (NewFormula && f.Revision != 0) NewProblem("New formulae should not define a revision.");
        if (!Git) return;
        var (prev, baseInfo) = CommittedVersionInfo();
        var prevVersion = prev.GetValueOrDefault("version") as string;
        var prevRevision = prev.GetValueOrDefault("revision") as int?;
        var baseVersion = baseInfo.GetValueOrDefault("version") as string;
        var baseRevision = baseInfo.GetValueOrDefault("revision") as int?;
        int cur = f.Revision;
        if ((prevVersion != baseVersion || f.Version != baseVersion) && cur != 0 && cur == baseRevision && cur == prevRevision)
            Problem($"`revision {cur}` should be removed");
        else if (f.Version == prevVersion && prevRevision != null && cur < prevRevision)
            Problem($"`revision` should not decrease (from {prevRevision} to {cur})");
        else if (baseRevision != null && cur > baseRevision + 1)
            Problem("`revision` should only increment by 1");
    }

    void AuditVersionScheme()
    {
        if (!Git) return;
        var (_, baseInfo) = CommittedVersionInfo();
        if (baseInfo.GetValueOrDefault("version_scheme") is not int previous) return;
        if (f.VersionScheme < previous) Problem($"`version_scheme` should not decrease (from {previous} to {f.VersionScheme})");
        else if (f.VersionScheme > previous + 1) Problem("`version_scheme` should only increment by 1");
    }

    void AuditUnconfirmedChecksumChange()
    {
        if (!Git) return;
        var (_, baseInfo) = CommittedVersionInfo();
        var baseChecksum = baseInfo.GetValueOrDefault("checksum") as string;
        if (f.Version == baseInfo.GetValueOrDefault("version") as string && f.Url == baseInfo.GetValueOrDefault("url") as string &&
            !string.IsNullOrEmpty(f.Sha256) && !string.IsNullOrEmpty(baseChecksum) && f.Sha256 != baseChecksum)
            Problem("stable sha256 changed without the url/version also changing; " +
                    "please create an issue upstream to rule out malicious " +
                    "circumstances and to find out why the file changed.");
    }

    void AuditText()
    {
        var binNames = new List<string> { f.Name };
        var prefix = Path.Combine(DevCommands.Cellar, f.Name, f.PkgVersion);
        foreach (var dir in new[] { "bin", "sbin" })
        {
            var d = Path.Combine(prefix, dir);
            if (Directory.Exists(d))
                foreach (var child in Directory.EnumerateFileSystemEntries(d))
                    if (!binNames.Contains(Path.GetFileName(child))) binNames.Add(Path.GetFileName(child));
        }
        foreach (var name in binNames)
            foreach (var cmd in new[] { "system", "shell_output", "pipe_output" })
                if (Regex.IsMatch(text, $@"test do.*{cmd}[(\s]+['""]{Regex.Escape(name)}[\s'""]", RegexOptions.Singleline))
                    Problem($"Fully scope test `{cmd}` calls, e.g.: {cmd} \"#{{bin}}/{name}\"");
    }

    void AuditPrefixHasContents()
    {
        var prefix = Path.Combine(DevCommands.Cellar, f.Name, f.PkgVersion);
        if (!Directory.Exists(prefix)) return;
        foreach (var entry in Directory.EnumerateFileSystemEntries(prefix))
        {
            var bn = Path.GetFileName(entry);
            if (bn.StartsWith('.')) continue;
            if (Directory.Exists(entry))
            {
                if (Directory.EnumerateFileSystemEntries(entry).Any(c => Path.GetFileName(c) != ".DS_Store")) return;
                continue;
            }
            if (bn is "INSTALL_RECEIPT.json" or ".DS_Store") continue;
            if (Regex.IsMatch(bn, @"^(about|authors|changelog|changes|copying|copyright|history|licen[cs]e|news|notes|notice|readme|todo)(\.(md|html|rtf|txt|rst))?$", RegexOptions.IgnoreCase)) continue;
            return;
        }
        Problem("The installation seems to be empty. Please ensure the prefix\nis set correctly and expected files are installed.\nThe prefix configure/make argument may be case-sensitive.\n");
    }

    static readonly string[] DeprecateDisableReasons =
        { "does_not_build", "no_license", "repo_archived", "repo_removed", "unmaintained", "unreachable", "unsupported", "deprecated_upstream", "versioned_formula", "checksum_mismatch" };

    void AuditDeprecateDisable()
    {
        if (!f.Deprecated && !f.Disabled) return;
        var reason = (f.Deprecated ? f.DeprecationReason : f.DisableReason) as RSymbol;
        if (reason == null) return;
        if (!DeprecateDisableReasons.Contains(reason.Name)) Problem($"{reason.Name} is not a valid deprecate! or disable! reason");
    }

    // SPDX License List 3.29.0 (Library/Homebrew/data/spdx); "!" marks deprecated IDs.
    const string SpdxLicenseIds =
        "0BSD 3D-Slicer-1.0 AAL Abstyles AdaCore-doc Adobe-2006 Adobe-Display-PostScript Adobe-Glyph Adobe-Utopia ADSL " +
        "Advanced-Cryptics-Dictionary AFL-1.1 AFL-1.2 AFL-2.0 AFL-2.1 AFL-3.0 Afmparse !AGPL-1.0 AGPL-1.0-only " +
        "AGPL-1.0-or-later !AGPL-3.0 AGPL-3.0-only AGPL-3.0-or-later Aladdin ALGLIB-Documentation AMD-newlib AMDPLPA " +
        "AML AML-glslang AMPAS ANTLR-PD ANTLR-PD-fallback any-OSI any-OSI-perl-modules Apache-1.0 Apache-1.1 Apache-2.0 " +
        "APAFML APL-1.0 App-s2p APSL-1.0 APSL-1.1 APSL-1.2 APSL-2.0 Arphic-1999 Artistic-1.0 Artistic-1.0-cl8 " +
        "Artistic-1.0-Perl Artistic-2.0 Artistic-dist Aspell-RU ASWF-Digital-Assets-1.0 ASWF-Digital-Assets-1.1 " +
        "atc-game Baekmuk Bahyph Barr bcrypt-Solar-Designer Beerware Bitstream-Charter Bitstream-Vera BitTorrent-1.0 " +
        "BitTorrent-1.1 blessing BlueOak-1.0.0 Boehm-GC Boehm-GC-without-fee BOLA-1.1 Borceux Brian-Gladman-2-Clause " +
        "Brian-Gladman-3-Clause Brian-Gladman-3-Clause-no-conversion BSD-1-Clause BSD-2-Clause BSD-2-Clause-Darwin " +
        "BSD-2-Clause-first-lines !BSD-2-Clause-FreeBSD !BSD-2-Clause-NetBSD BSD-2-Clause-Patent " +
        "BSD-2-Clause-pkgconf-disclaimer BSD-2-Clause-pos-unchanged BSD-2-Clause-Views BSD-3-Clause BSD-3-Clause-acpica " +
        "BSD-3-Clause-Attribution BSD-3-Clause-Clear BSD-3-Clause-flex BSD-3-Clause-HP BSD-3-Clause-LBNL " +
        "BSD-3-Clause-Modification BSD-3-Clause-No-Military-License BSD-3-Clause-No-Nuclear-License " +
        "BSD-3-Clause-No-Nuclear-License-2014 BSD-3-Clause-No-Nuclear-Warranty BSD-3-Clause-Open-MPI " +
        "BSD-3-Clause-OpenWebUI BSD-3-Clause-Sun BSD-3-Clause-Tso BSD-4-Clause BSD-4-Clause-Shortened BSD-4-Clause-UC " +
        "BSD-4.3RENO BSD-4.3TAHOE BSD-Advertising-Acknowledgement BSD-ask-to-endorse BSD-Attribution-HPND-disclaimer " +
        "BSD-Inferno-Nettverk BSD-Mark-Modifications BSD-Protection BSD-Source-alt-GPL BSD-Source-beginning-file " +
        "BSD-Source-Code BSD-Source-Code-no-disclaimer BSD-Systemics BSD-Systemics-W3Works BSL-1.0 Buddy Bugroff " +
        "BUSL-1.1 !bzip2-1.0.5 bzip2-1.0.6 C-UDA-1.0 CAL-1.0 CAL-1.0-Combined-Work-Exception Caldera " +
        "Caldera-no-preamble CAPEC-tou Catharon CATOSL-1.1 CC-BY-1.0 CC-BY-2.0 CC-BY-2.5 CC-BY-2.5-AU CC-BY-3.0 " +
        "CC-BY-3.0-AT CC-BY-3.0-AU CC-BY-3.0-DE CC-BY-3.0-IGO CC-BY-3.0-NL CC-BY-3.0-US CC-BY-4.0 CC-BY-NC-1.0 " +
        "CC-BY-NC-2.0 CC-BY-NC-2.5 CC-BY-NC-3.0 CC-BY-NC-3.0-DE CC-BY-NC-3.0-IGO CC-BY-NC-4.0 CC-BY-NC-ND-1.0 " +
        "CC-BY-NC-ND-2.0 CC-BY-NC-ND-2.5 CC-BY-NC-ND-3.0 CC-BY-NC-ND-3.0-DE CC-BY-NC-ND-3.0-IGO CC-BY-NC-ND-4.0 " +
        "CC-BY-NC-SA-1.0 CC-BY-NC-SA-2.0 CC-BY-NC-SA-2.0-DE CC-BY-NC-SA-2.0-FR CC-BY-NC-SA-2.0-UK CC-BY-NC-SA-2.5 " +
        "CC-BY-NC-SA-3.0 CC-BY-NC-SA-3.0-DE CC-BY-NC-SA-3.0-IGO CC-BY-NC-SA-4.0 CC-BY-ND-1.0 CC-BY-ND-2.0 CC-BY-ND-2.5 " +
        "CC-BY-ND-3.0 CC-BY-ND-3.0-DE CC-BY-ND-4.0 CC-BY-SA-1.0 CC-BY-SA-2.0 CC-BY-SA-2.0-UK CC-BY-SA-2.1-JP " +
        "CC-BY-SA-2.5 CC-BY-SA-3.0 CC-BY-SA-3.0-AT CC-BY-SA-3.0-DE CC-BY-SA-3.0-IGO CC-BY-SA-4.0 CC-PDDC CC-PDM-1.0 " +
        "CC-SA-1.0 CC0-1.0 CDDL-1.0 CDDL-1.1 CDL-1.0 CDLA-Permissive-1.0 CDLA-Permissive-2.0 CDLA-Sharing-1.0 " +
        "CECILL-1.0 CECILL-1.1 CECILL-2.0 CECILL-2.1 CECILL-B CECILL-C CERN-OHL-1.1 CERN-OHL-1.2 CERN-OHL-P-2.0 " +
        "CERN-OHL-S-2.0 CERN-OHL-W-2.0 CFITSIO check-cvs checkmk ClArtistic Clips CMU-Mach CMU-Mach-nodoc CNRI-Jython " +
        "CNRI-Python CNRI-Python-GPL-Compatible COIL-1.0 Community-Spec-1.0 Condor-1.1 copyleft-next-0.3.0 " +
        "copyleft-next-0.3.1 Cornell-Lossless-JPEG CPAL-1.0 CPL-1.0 CPOL-1.02 Cronyx Crossword CryptoSwift " +
        "CrystalStacker CUA-OPL-1.0 Cube curl cve-tou D-FSL-1.0 DEC-3-Clause diffmark DL-DE-BY-2.0 DL-DE-ZERO-2.0 DOC " +
        "DocBook-DTD DocBook-Schema DocBook-Stylesheet DocBook-XML Dotseqn DRL-1.0 DRL-1.1 DSDP dtoa dvipdfm ECL-1.0 " +
        "ECL-2.0 !eCos-2.0 EFL-1.0 EFL-2.0 eGenix Elastic-2.0 Entessa EPICS EPL-1.0 EPL-2.0 ErlPL-1.1 " +
        "ESA-PL-permissive-2.4 ESA-PL-strong-copyleft-2.4 ESA-PL-weak-copyleft-2.4 etalab-2.0 EUDatagrid EUPL-1.0 " +
        "EUPL-1.1 EUPL-1.2 Eurosym Fair FBM FDK-AAC FDK-MPEG-H Ferguson-Twofish Frameworx-1.0 FreeBSD-DOC FreeImage " +
        "FSFAP FSFAP-no-warranty-disclaimer FSFUL FSFULLR FSFULLRSD FSFULLRWD FSL-1.1-ALv2 FSL-1.1-MIT FTL Furuseth " +
        "fwlw Game-Programming-Gems GCR-docs GD generic-xts !GFDL-1.1 GFDL-1.1-invariants-only " +
        "GFDL-1.1-invariants-or-later GFDL-1.1-no-invariants-only GFDL-1.1-no-invariants-or-later GFDL-1.1-only " +
        "GFDL-1.1-or-later !GFDL-1.2 GFDL-1.2-invariants-only GFDL-1.2-invariants-or-later GFDL-1.2-no-invariants-only " +
        "GFDL-1.2-no-invariants-or-later GFDL-1.2-only GFDL-1.2-or-later !GFDL-1.3 GFDL-1.3-invariants-only " +
        "GFDL-1.3-invariants-or-later GFDL-1.3-no-invariants-only GFDL-1.3-no-invariants-or-later GFDL-1.3-only " +
        "GFDL-1.3-or-later Giftware GL2PS Glide Glulxe GLWTPL gnuplot !GPL-1.0 !GPL-1.0+ GPL-1.0-only GPL-1.0-or-later " +
        "!GPL-2.0 !GPL-2.0+ GPL-2.0-only GPL-2.0-or-later !GPL-2.0-with-autoconf-exception " +
        "!GPL-2.0-with-bison-exception !GPL-2.0-with-classpath-exception !GPL-2.0-with-font-exception " +
        "!GPL-2.0-with-GCC-exception !GPL-3.0 !GPL-3.0+ GPL-3.0-only GPL-3.0-or-later !GPL-3.0-with-autoconf-exception " +
        "!GPL-3.0-with-GCC-exception Graphics-Gems gSOAP-1.3b gtkbook Gutmann HaskellReport HDF5 hdparm HIDAPI " +
        "Hippocratic-2.1 Hippocratic-3.0-core HP-1986 HP-1989 HPND HPND-DEC HPND-doc HPND-doc-sell HPND-export-US " +
        "HPND-export-US-acknowledgement HPND-export-US-modify HPND-export2-US HPND-Fenneberg-Livingston HPND-INRIA-IMAG " +
        "HPND-Intel HPND-Kevlin-Henney HPND-Markus-Kuhn HPND-merchantability-variant HPND-MIT-disclaimer HPND-Netrek " +
        "HPND-Pbmplus HPND-sell-MIT-disclaimer-xserver HPND-sell-regexpr HPND-sell-variant " +
        "HPND-sell-variant-critical-systems HPND-sell-variant-MIT-disclaimer HPND-sell-variant-MIT-disclaimer-rev " +
        "HPND-SMC HPND-UC HPND-UC-export-US HTMLTIDY hyphen-bulgarian IBM-pibs ICU IEC-Code-Components-EULA IJG " +
        "IJG-short ImageMagick iMatix Imlib2 Info-ZIP Informatica Inner-Net-2.0 InnoSetup Intel Intel-ACPI " +
        "Interbase-1.0 IPA IPL-1.0 ISC ISC-Veillard ISO-permission Jam JasPer-2.0 jove JPL-image JPNIC JSON Kastrup " +
        "Kazlib Knuth-CTAN LAL-1.2 LAL-1.3 Latex2e Latex2e-translated-notice Leptonica !LGPL-2.0 !LGPL-2.0+ " +
        "LGPL-2.0-only LGPL-2.0-or-later !LGPL-2.1 !LGPL-2.1+ LGPL-2.1-only LGPL-2.1-or-later !LGPL-3.0 !LGPL-3.0+ " +
        "LGPL-3.0-only LGPL-3.0-or-later LGPLLR Libpng libpng-1.6.35 libpng-2.0 libselinux-1.0 libtiff " +
        "libutil-David-Nugent LiLiQ-P-1.1 LiLiQ-R-1.1 LiLiQ-Rplus-1.1 Linux-man-pages-1-para Linux-man-pages-copyleft " +
        "Linux-man-pages-copyleft-2-para Linux-man-pages-copyleft-var Linux-OpenIB LOOP LPD-document LPL-1.0 LPL-1.02 " +
        "LPPL-1.0 LPPL-1.1 LPPL-1.2 LPPL-1.3a LPPL-1.3c lsof Lucida-Bitmap-Fonts LZMA-SDK-9.11-to-9.20 LZMA-SDK-9.22 " +
        "Mackerras-3-Clause Mackerras-3-Clause-acknowledgment magaz mailprio MakeIndex man2html Martin-Birgmeier " +
        "McPhee-slideshow metamail Minpack MIPS MirOS MIT MIT-0 MIT-advertising MIT-Click MIT-CMU MIT-enna MIT-feh " +
        "MIT-Festival MIT-Khronos-old MIT-Modern-Variant MIT-open-group MIT-STK MIT-testregex MIT-Wu MITNFA MMIXware " +
        "MMPL-1.0.1 Motosoto MPEG-SSG mpi-permissive mpich2 MPL-1.0 MPL-1.1 MPL-2.0 MPL-2.0-no-copyleft-exception mplus " +
        "MS-LPL MS-PL MS-RL MTLL MulanPSL-1.0 MulanPSL-2.0 Multics Mup MVT-1.1 NAIST-2003 NASA-1.3 Naumen NBPL-1.0 " +
        "NCBI-PD NCGL-UK-2.0 NCL NCSA !Net-SNMP NetCDF Newsletr NGPL ngrep NICTA-1.0 NIST-PD NIST-PD-fallback " +
        "NIST-PD-TNT NIST-Software NLOD-1.0 NLOD-2.0 NLPL Nokia NOSL Noweb NPL-1.0 NPL-1.1 NPOSL-3.0 NRL NTIA-PD NTP " +
        "NTP-0 !Nunit O-UDA-1.0 OAR OCCT-PL OCLC-2.0 ODbL-1.0 ODC-By-1.0 OFFIS OFL-1.0 OFL-1.0-no-RFN OFL-1.0-RFN " +
        "OFL-1.1 OFL-1.1-no-RFN OFL-1.1-RFN OGC-1.0 OGDL-Taiwan-1.0 OGL-Canada-2.0 OGL-UK-1.0 OGL-UK-2.0 OGL-UK-3.0 " +
        "OGTSL OLDAP-1.1 OLDAP-1.2 OLDAP-1.3 OLDAP-1.4 OLDAP-2.0 OLDAP-2.0.1 OLDAP-2.1 OLDAP-2.2 OLDAP-2.2.1 " +
        "OLDAP-2.2.2 OLDAP-2.3 OLDAP-2.4 OLDAP-2.5 OLDAP-2.6 OLDAP-2.7 OLDAP-2.8 OLFL-1.3 OML OpenMDW-1.0 OpenPBS-2.3 " +
        "OpenSSL OpenSSL-standalone OpenVision OPL-1.0 OPL-UK-3.0 OPUBL-1.0 OSC-1.0 OSET-PL-2.1 OSL-1.0 OSL-1.1 OSL-2.0 " +
        "OSL-2.1 OSL-3.0 OSSP PADL ParaType-Free-Font-1.3 Parity-6.0.0 Parity-7.0.0 PDDL-1.0 PHP-3.0 PHP-3.01 Pixar " +
        "pkgconf Plexus pnmstitch PolyForm-Noncommercial-1.0.0 PolyForm-Small-Business-1.0.0 PostgreSQL PPL PSF-2.0 " +
        "psfrag psutils Python-2.0 Python-2.0.1 python-ldap Qhull QPL-1.0 QPL-1.0-INRIA-2004 radvd Rdisc RHeCos-1.1 " +
        "RPL-1.1 RPL-1.5 RPSL-1.0 RSA-MD RSCPL Ruby Ruby-pty SAX-PD SAX-PD-2.0 Saxpath SCEA SchemeReport Sendmail " +
        "Sendmail-8.23 Sendmail-Open-Source-1.1 SGI-B-1.0 SGI-B-1.1 SGI-B-2.0 SGI-OpenGL SGMLUG-PM SGP4 SHL-0.5 " +
        "SHL-0.51 SimPL-2.0 SISSL SISSL-1.2 SL Sleepycat SMAIL-GPL SMLNJ SMPPL SNIA snprintf SOFA softSurfer Soundex " +
        "Spencer-86 Spencer-94 Spencer-99 SPL-1.0 ssh-keyscan SSH-OpenSSH SSH-short SSLeay-standalone SSPL-1.0 " +
        "!StandardML-NJ SugarCRM-1.1.3 SUL-1.0 Sun-PPP Sun-PPP-2000 SunPro SWL swrule Symlinks TAPR-OHL-1.0 TCL " +
        "TCP-wrappers TekHVC TermReadKey TGPPL-1.0 ThirdEye threeparttable TMate TORQUE-1.1 TOSL TPDL TPL-1.0 " +
        "TrustedQSL TTWL TTYP0 TU-Berlin-1.0 TU-Berlin-2.0 Ubuntu-font-1.0 UCAR UCL-1.0 ulem UMich-Merit Unicode-3.0 " +
        "Unicode-DFS-2015 Unicode-DFS-2016 Unicode-TOU UnixCrypt Unlicense Unlicense-libtelnet Unlicense-libwhirlpool " +
        "UnRAR UPL-1.0 URT-RLE Vim Vixie-Cron VOSTROM VSL-1.0 W3C W3C-19980720 W3C-20150513 w3m Watcom-1.0 " +
        "Widget-Workshop WordNet Wsuipa WTFNMFPL WTFPL wwl !wxWindows X11 X11-distribute-modifications-variant " +
        "X11-no-permit-persons X11-swapped Xdebug-1.03 Xerox Xfig XFree86-1.1 xinetd xkeyboard-config-Zinoviev xlock " +
        "Xnet xpp XSkat xzoom YPL-1.0 YPL-1.1 Zed Zeeff Zend-2.0 Zimbra-1.3 Zimbra-1.4 Zlib zlib-acknowledgement " +
        "ZPL-1.1 ZPL-2.0 ZPL-2.1 ";
    const string SpdxExceptionIds =
        "389-exception Asterisk-exception Asterisk-linking-protocols-exception Autoconf-exception-2.0 " +
        "Autoconf-exception-3.0 Autoconf-exception-generic Autoconf-exception-generic-3.0 Autoconf-exception-macro " +
        "Bison-exception-1.24 Bison-exception-2.2 Bootloader-exception CGAL-linking-exception Classpath-exception-2.0 " +
        "Classpath-exception-2.0-short CLISP-exception-2.0 cryptsetup-OpenSSL-exception Digia-Qt-LGPL-exception-1.1 " +
        "DigiRule-FOSS-exception eCos-exception-2.0 erlang-otp-linking-exception Fawkes-Runtime-exception " +
        "FLTK-exception fmt-exception Font-exception-2.0 freertos-exception-2.0 GCC-exception-2.0 " +
        "GCC-exception-2.0-note GCC-exception-3.1 Gmsh-exception GNAT-exception GNOME-examples-exception " +
        "GNU-compiler-exception gnu-javamail-exception Google-Patent-WebM GPL-3.0-389-ds-base-exception " +
        "GPL-3.0-interface-exception GPL-3.0-linking-exception GPL-3.0-linking-source-exception GPL-CC-1.0 " +
        "GStreamer-exception-2005 GStreamer-exception-2008 harbour-exception i2p-gpl-java-exception " +
        "Independent-modules-exception KiCad-libraries-exception kvirc-openssl-exception LGPL-3.0-linking-exception " +
        "libpri-OpenH323-exception Libtool-exception Linux-syscall-note LLGPL LLVM-exception LZMA-exception " +
        "mif-exception mxml-exception OCaml-LGPL-linking-exception OCCT-exception-1.0 OpenJDK-assembly-exception-1.0 " +
        "openvpn-openssl-exception PCRE2-exception polyparse-exception PS-or-PDF-font-exception-20170817 " +
        "QPL-1.0-INRIA-2004-exception Qt-GPL-exception-1.0 Qt-LGPL-exception-1.1 Qwt-exception-1.0 romic-exception " +
        "RRDtool-FLOSS-exception-2.0 rsync-linking-exception SANE-exception SHL-2.0 SHL-2.1 " +
        "Simple-Library-Usage-exception Spelling-Provider-LGPL-exception sqlitestudio-OpenSSL-exception " +
        "stunnel-exception SWI-exception Swift-exception Texinfo-exception u-boot-exception-2.0 UBDL-exception " +
        "Universal-FOSS-exception-1.0 vsftpd-openssl-exception WxWindows-exception-3.1 x11vnc-openssl-exception ";
}
