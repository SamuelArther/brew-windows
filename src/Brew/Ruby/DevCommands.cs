using System.Reflection;

namespace Brew.Ruby;

[AttributeUsage(AttributeTargets.Class)]
public sealed class DevCommandAttribute(string name, params string[] aliases) : Attribute
{
    public string Name { get; } = name;
    public string[] Aliases { get; } = aliases;
}

// One `brew <dev-command>`. Subclasses carry [DevCommand("name")] and live in DevCmd/*.cs.
public abstract class DevCommand
{
    public string Name { get; internal set; }
    public abstract CommandParser Args();
    public abstract void Run(ParsedArgs args);
}

// Entry point for the CLI (see docs/FORMULA_API.md): `brew <command>` for formula-related dev commands.
public static class DevCommands
{
    static Dictionary<string, (Type type, string name)> table;

    static Dictionary<string, (Type type, string name)> Table
    {
        get
        {
            if (table != null) return table;
            var t = new Dictionary<string, (Type, string)>();
            foreach (var type in typeof(DevCommands).Assembly.GetTypes())
            {
                if (type.IsAbstract || !typeof(DevCommand).IsAssignableFrom(type)) continue;
                var attr = type.GetCustomAttribute<DevCommandAttribute>();
                if (attr == null) continue;
                t[attr.Name] = (type, attr.Name);
                foreach (var a in attr.Aliases) t[a] = (type, attr.Name);
            }
            return table = t;
        }
    }

    // Primary command names (no aliases), sorted.
    public static IReadOnlyList<string> Names => Table.Values.Select(v => v.name).Distinct().Order(StringComparer.Ordinal).ToList();

    public static bool Handles(string command) => command != null && Table.ContainsKey(command);

    static DevCommand Create(string command)
    {
        var (type, name) = Table[command];
        var cmd = (DevCommand)Activator.CreateInstance(type)!;
        cmd.Name = name;
        return cmd;
    }

    public static string Help(string command) => Create(command).Args().HelpText();

    // Help for any command (dev or user) using Homebrew's exact renderer. Prints it and returns
    // true, or returns false if the command's arguments aren't defined here.
    public static bool TryHelp(string command, string[] args)
    {
        var parser = Handles(command) ? Create(command).Args() : UserCommandHelp.Parser(command);
        if (parser == null) return false;
        Console.Out.WriteLine(parser.HelpText());
        return true;
    }

    // Parses a user command's arguments with Homebrew's rules. Returns null after printing
    // help/usage errors (exitCode says what to exit with), or the parsed args.
    public static ParsedArgs ParseUserArgs(string command, string[] args, out int exitCode)
    {
        exitCode = 0;
        var parser = UserCommandHelp.Parser(command);
        if (parser == null) throw new InvalidOperationException($"No argument definition for {command}");
        try
        {
            var parsed = parser.Parse(args);
            if (!parsed.Help) return parsed;
            Console.Out.WriteLine(parser.HelpText());
            return null;
        }
        catch (UsageError e)
        {
            Console.Error.WriteLine(parser.HelpText());
            Console.Error.WriteLine();
            Out.Onoe(e.Message);
            exitCode = 1;
            return null;
        }
        catch (OptionError e)
        {
            Out.Onoe(e.Message);
            exitCode = 1;
            return null;
        }
    }

    // Returns the process exit code. Never throws for user-facing errors.
    public static int Run(string command, string[] args)
    {
        var cmd = Create(command);
        var parser = cmd.Args();
        try
        {
            var parsed = parser.Parse(args);
            if (parsed.Help)
            {
                Console.Out.WriteLine(parser.HelpText());
                return 0;
            }
            Failed = false;
            cmd.Run(parsed);
            return Failed ? 1 : 0;
        }
        catch (UsageError e)
        {
            Console.Error.WriteLine(parser.HelpText());
            Console.Error.WriteLine();
            Out.Onoe(e.Message);
            return 1;
        }
        catch (Exception e) when (e is OptionError or BrewError or InvalidOperationException or IOException or UnauthorizedAccessException or RubySyntaxError)
        {
            Out.Onoe(e.Message);
            return 1;
        }
    }

    // Homebrew.failed? / ofail
    public static bool Failed;

    public static void Ofail(string message)
    {
        Out.Onoe(message);
        Failed = true;
    }

    // ---- shared helpers for commands ----

    public static readonly Paths Paths = new();

    public static string Repository => Paths.Repository;
    public static string CoreTapFormulaDir => Path.Combine(Paths.Repository, "tap", "homebrew-core", "Formula");

    public static IEnumerable<string> AllFormulaFiles() =>
        Directory.Exists(CoreTapFormulaDir)
            ? Directory.EnumerateFiles(CoreTapFormulaDir, "*.rb", SearchOption.AllDirectories).Order(StringComparer.Ordinal)
            : Enumerable.Empty<string>();

    public static string NameOf(string arg) =>
        arg.StartsWith("homebrew/core/", StringComparison.OrdinalIgnoreCase) ? arg["homebrew/core/".Length..] : arg;

    // Formulary.loader_for (FromPathLoader, FromTapLoader, FromNameLoader): a path to a .rb file, a
    // fully-qualified <user>/<repo>/<name> in any installed tap, or a bare name, which homebrew/core
    // wins and otherwise must be in exactly one other installed tap. Names may be aliases or old
    // (renamed) names. Null when nothing matches; BrewError when a bare name is ambiguous.
    public static string FormulaPath(string arg)
    {
        if (arg.EndsWith(".rb", StringComparison.OrdinalIgnoreCase) && File.Exists(arg)) return Path.GetFullPath(arg);
        var parts = arg.ToLowerInvariant().Split('/');
        if (parts.Length == 3)
        {
            string tapDir;
            try { tapDir = DevCmd.DevTap.Path(parts[0] + "/" + parts[1]); }
            catch (BrewError) { return null; }
            return FormulaInTap(tapDir, parts[2]);
        }
        if (parts.Length != 1) return null;
        var name = parts[0];
        var coreDir = Path.GetDirectoryName(CoreTapFormulaDir)!;
        if (FormulaInTap(coreDir, name) is { } core) return core;
        var found = (Directory.Exists(DevCmd.DevTap.Root) ? Directory.GetDirectories(DevCmd.DevTap.Root) : [])
            .Where(d => !string.Equals(Path.GetFullPath(d), Path.GetFullPath(coreDir), StringComparison.OrdinalIgnoreCase))
            .Select(d => FormulaInTap(d, name)).Where(p => p != null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(p => (Path: p, Tap: DevCmd.DevTap.FromPath(p))).OrderBy(t => t.Tap, StringComparer.Ordinal).ToList();
        if (found.Count > 1)
        {
            // TapFormulaAmbiguityError
            var formulae = found.Select(t => $"{t.Tap}/{name}").ToList();
            throw new BrewError($"Formulae found in multiple taps:{string.Concat(formulae.Select(f => "\n       * " + f))}\n\n"
                + $"Please use the fully-qualified name (e.g. {formulae[0]}) to refer to a specific formula.");
        }
        return found.Count == 1 ? found[0].Path : null;
    }

    // Formulary.tap_formula_name_type + find_formula_in_tap: the tap's Aliases, then its
    // formula_renames.json, then the formula file itself (Tap#formula_files_by_name).
    static string FormulaInTap(string tapDir, string name)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"\A[\w+\-.@]+\z") || !Directory.Exists(tapDir)) return null;
        var formulaDir = DevCmd.DevTap.FormulaDir(tapDir);
        var alias = Path.Combine(tapDir, "Aliases", name);
        if (File.Exists(alias))
        {
            string target;
            // Git with core.symlinks=false checks a symlink out as its relative target text.
            try { target = new FileInfo(alias).LinkTarget ?? File.ReadAllText(alias).Trim(); }
            catch (IOException) { target = null; }
            if (target != null)
            {
                var file = Path.GetFullPath(target, Path.GetDirectoryName(alias)!);
                if (file.StartsWith(Path.GetFullPath(formulaDir) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                    && file.EndsWith(".rb", StringComparison.OrdinalIgnoreCase) && File.Exists(file))
                    return file;
            }
        }
        var renames = Path.Combine(tapDir, "formula_renames.json");
        if (File.Exists(renames))
            try
            {
                using var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(renames));
                if (json.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object
                    && json.RootElement.TryGetProperty(name, out var renamed) && renamed.GetString() is { Length: > 0 } newName
                    && System.Text.RegularExpressions.Regex.IsMatch(newName, @"\A[\w+\-.@]+\z"))
                    name = newName;
            }
            catch (Exception e) when (e is System.Text.Json.JsonException or InvalidOperationException or IOException) { }
        foreach (var p in new[] { Path.Combine(formulaDir, Shard(name), name + ".rb"), Path.Combine(formulaDir, name + ".rb") })
            if (File.Exists(p)) return p;
        return DevCmd.DevTap.FormulaFiles(tapDir).FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == name);
    }

    // FormulaOrCaskUnavailableError (FormulaUnavailableError without orCask); for <user>/<repo>/<name>,
    // TapFormulaOrCaskUnavailableError, which also says how to tap a tap that isn't installed.
    public static string UnavailableMessage(string arg, bool orCask = true)
    {
        var what = orCask ? "formula or cask" : "formula";
        var parts = arg.ToLowerInvariant().Split('/');
        string tap = null;
        if (parts.Length == 3)
            try { tap = DevCmd.DevTap.Normalize(parts[0] + "/" + parts[1]); }
            catch (BrewError) { }
        if (tap == null) return $"No available {what} with the name \"{arg}\".";
        var message = $"No available {what} with the name \"{tap}/{parts[2]}\".";
        if (!Directory.Exists(DevCmd.DevTap.Path(tap)))
            message += $"\nThis command requires the tap {tap}.\nIf you trust this tap, tap it explicitly and then try again:\n  brew tap {tap}";
        return message;
    }

    // The path a formula would have, whether or not it exists.
    public static string ExpectedFormulaPath(string name)
    {
        var parts = name.ToLowerInvariant().Split('/');
        if (parts.Length == 3 && !(parts[0] == "homebrew" && parts[1] == "core"))
            try { return Path.Combine(DevCmd.DevTap.FormulaDir(DevCmd.DevTap.Path(parts[0] + "/" + parts[1])), parts[2] + ".rb"); }
            catch (BrewError) { }
        name = NameOf(name).ToLowerInvariant();
        return Path.Combine(CoreTapFormulaDir, Shard(name), name + ".rb");
    }

    // Formula#aliases: entries in the tap's Aliases dir (symlinks, or git's plain-text symlink files) pointing at the formula.
    public static List<string> Aliases(string formulaName)
    {
        var dir = Path.Combine(Paths.Repository, "tap", "homebrew-core", "Aliases");
        var result = new List<string>();
        if (!Directory.Exists(dir)) return result;
        foreach (var entry in Directory.EnumerateFileSystemEntries(dir).Order(StringComparer.Ordinal))
        {
            string target;
            try { target = new FileInfo(entry).LinkTarget ?? File.ReadAllText(entry).Trim(); }
            catch (IOException) { continue; }
            if (Path.GetFileNameWithoutExtension(target.Replace('\\', '/').Split('/')[^1]) == formulaName)
                result.Add(Path.GetFileName(entry));
        }
        return result;
    }

    // CoreTap#new_formula_subdirectory
    public static string Shard(string name) => name.StartsWith("lib") ? "lib" : name[..1];

    // Formulary.factory: loads or raises FormulaUnavailableError's message.
    public static FormulaSpec LoadFormula(string arg)
    {
        var path = FormulaPath(arg) ?? throw new BrewError(UnavailableMessage(arg, orCask: false));
        return FormulaLoader.LoadSpec(path);
    }

    public static string Cellar => Paths.Cellar;
    public static string Prefix => Paths.Prefix;

    // Installed keg directories for a formula (Cellar/name/*), newest last.
    public static List<string> InstalledKegs(string name)
    {
        var rack = Path.Combine(Paths.Cellar, name);
        return Directory.Exists(rack)
            ? Directory.GetDirectories(rack).OrderBy(d => Path.GetFileName(d), Comparer<string>.Create(Versions.Compare)).ToList()
            : new List<string>();
    }

    // Runs a program inheriting the console; returns its exit code.
    public static int Exec(string file, IEnumerable<string> args, string cwd = null, IDictionary<string, string> env = null)
    {
        var psi = new System.Diagnostics.ProcessStartInfo(file) { UseShellExecute = false, WorkingDirectory = cwd ?? Environment.CurrentDirectory };
        foreach (var a in args) psi.ArgumentList.Add(a);
        if (env != null) foreach (var kv in env) psi.Environment[kv.Key] = kv.Value;
        using var p = System.Diagnostics.Process.Start(psi)!;
        p.WaitForExit();
        return p.ExitCode;
    }

    // Runs a program and captures stdout (CRLF normalized); throws BrewError on failure when check is set.
    public static (int code, string stdout, string stderr) Capture(string file, IEnumerable<string> args, string cwd = null, bool check = false)
    {
        var psi = new System.Diagnostics.ProcessStartInfo(file)
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
            WorkingDirectory = cwd ?? Environment.CurrentDirectory,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = System.Diagnostics.Process.Start(psi)!;
        var errTask = p.StandardError.ReadToEndAsync();
        var stdout = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        var stderr = errTask.Result;
        if (check && p.ExitCode != 0)
            throw new BrewError($"Failure while executing; `{file} {string.Join(" ", args)}` exited with {p.ExitCode}.");
        return (p.ExitCode, stdout.Replace("\r\n", "\n"), stderr.Replace("\r\n", "\n"));
    }

    // Finds an executable on PATH (Windows PATHEXT aware).
    public static string Which(string cmd)
    {
        var exts = (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT").Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var p = Path.Combine(dir.Trim('"'), cmd);
            if (Path.HasExtension(cmd) && File.Exists(p)) return p;
            foreach (var e in exts)
                if (File.Exists(p + e)) return p + e;
        }
        return null;
    }
}
