using System.Text.Json;

namespace Brew.Ruby.DevCmd;

[DevCommand("test")]
sealed class TestCmd : DevCommand
{
    public override CommandParser Args() => new CommandParser("test")
        .Description("""
            Run the test method provided by an installed formula.
            There is no standard output or return code, but generally it should notify the
            user if something is wrong with the installed formula.

            *Example:* `brew install jruby && brew test jruby`
            """)
        .Switch(new[] { "-f", "--force" }, "Test formulae even if they are unlinked.")
        .Switch(new[] { "--HEAD" }, "Test the HEAD version of a formula.")
        .Switch(new[] { "--keep-tmp" }, "Retain the temporary files created for the test.")
        .Switch(new[] { "--retry" }, "Retry if a testing fails.")
        .NamedArgs("installed_formula", min: 1);

    static string Temp => Path.GetFullPath(Environment.GetEnvironmentVariable("HOMEBREW_TEMP") is { Length: > 0 } t ? t : Path.GetTempPath());

    static bool AnyInstalled(string name) => DevCommands.InstalledKegs(name).Count > 0;

    // The installed keg for the formula's current version (or newest HEAD keg with --HEAD).
    static string LatestKeg(FormulaSpec f, bool head)
    {
        var kegs = DevCommands.InstalledKegs(f.Name);
        if (head) return kegs.LastOrDefault(k => Path.GetFileName(k).StartsWith("HEAD"));
        return kegs.FirstOrDefault(k => Path.GetFileName(k) == f.PkgVersion);
    }

    // Kegs are linked when their receipt lists links (or there is nothing to link).
    static bool Linked(string keg)
    {
        var receipt = Path.Combine(keg, "INSTALL_RECEIPT.json");
        if (!File.Exists(receipt)) return true;
        try
        {
            var root = JsonDocument.Parse(File.ReadAllText(receipt)).RootElement;
            bool links = root.TryGetProperty("Links", out var l) && l.ValueKind == JsonValueKind.Array && l.GetArrayLength() > 0;
            bool commands = root.TryGetProperty("Commands", out var c) && c.ValueKind == JsonValueKind.Array && c.GetArrayLength() > 0;
            return links || !commands;
        }
        catch (JsonException) { return true; }
    }

    // Installs record `bin.install`ed programs as launchers in HOMEBREW_PREFIX/bin rather than the
    // keg's own bin/, but tests call `bin/"program"`. Put launchers in the keg's bin/ while the
    // test runs; returns what was created so it can be removed afterwards.
    static List<string> MaterializeBin(string keg)
    {
        var created = new List<string>();
        var receipt = Path.Combine(keg, "INSTALL_RECEIPT.json");
        if (!File.Exists(receipt)) return created;
        JsonElement root;
        try { root = JsonDocument.Parse(File.ReadAllText(receipt)).RootElement; }
        catch (JsonException) { return created; }
        if (!root.TryGetProperty("Commands", out var commands) || commands.ValueKind != JsonValueKind.Array) return created;
        var links = root.TryGetProperty("Links", out var l) && l.ValueKind == JsonValueKind.Array ? l.EnumerateArray().ToList() : new List<JsonElement>();
        var bin = Path.Combine(keg, "bin");
        foreach (var c in commands.EnumerateArray())
        {
            var name = c.TryGetProperty("Name", out var n) ? n.GetString() : null;
            var target = c.TryGetProperty("Target", out var t) ? t.GetString() : null;
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(target) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) continue;
            var dest = Path.Combine(bin, name);
            var stem = Path.Combine(bin, Path.GetFileNameWithoutExtension(name));
            if (File.Exists(dest) || File.Exists(stem + ".exe") || File.Exists(stem + ".cmd")) continue;
            if (!Directory.Exists(bin)) { Directory.CreateDirectory(bin); created.Add(bin); }
            var link = links.FirstOrDefault(x => x.TryGetProperty("Path", out var p) && Path.GetFileName(p.GetString()) == name);
            var linkPath = link.ValueKind == JsonValueKind.Object ? link.GetProperty("Path").GetString() : null;
            if (linkPath != null && File.Exists(linkPath) && File.Exists(linkPath + ".brew-shim"))
            {
                File.Copy(linkPath, dest);
                File.Copy(linkPath + ".brew-shim", dest + ".brew-shim");
                created.Add(dest);
                created.Add(dest + ".brew-shim");
                continue;
            }
            var args = c.TryGetProperty("Arguments", out var a) && a.ValueKind == JsonValueKind.Array
                ? string.Concat(a.EnumerateArray().Select(x => $" \"{x.GetString()}\"")) : "";
            var launcher = stem + ".cmd";
            File.WriteAllText(launcher, $"@\"{target}\"{args} %*\r\n");
            created.Add(launcher);
        }
        return created;
    }

    static void Unmaterialize(List<string> created)
    {
        foreach (var p in Enumerable.Reverse(created))
        {
            try
            {
                if (File.Exists(p)) File.Delete(p);
                else if (Directory.Exists(p) && !Directory.EnumerateFileSystemEntries(p).Any()) Directory.Delete(p);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        }
    }

    // Utils.rewrite_child_error: build failures keep their message, anything else is wrapped.
    static string ChildError(Exception e)
    {
        var ruby = e as RubyError ?? e.InnerException as RubyError;
        string cls = ruby?.ClassName, message = ruby?.Message ?? e.Message;
        if (ruby == null)
        {
            cls = e switch
            {
                FileNotFoundException or DirectoryNotFoundException => "Errno::ENOENT",
                UnauthorizedAccessException => "Errno::EACCES",
                _ => "RuntimeError",
            };
        }
        if (cls is "BuildError" or "ErrorDuringExecution") return message;
        return $"An exception occurred within a child process:\n  {cls}: {message}";
    }

    static bool Required(Dependency d) => !d.Build && !d.Test && !d.Optional && !d.Recommended;

    // f.recursive_dependencies with test.rb's pruning block
    static List<string> MissingTestDeps(FormulaSpec f)
    {
        var missing = new List<string>();
        var seen = new HashSet<string>();
        void Walk(FormulaSpec dependent)
        {
            foreach (var d in dependent.Dependencies)
            {
                if (AnyInstalled(d.Name)) continue;
                bool keep = d.Test && dependent == f;
                if (!keep && !Required(d)) continue;
                if (!seen.Add(d.Name)) continue;
                missing.Add(d.Name);
                if (DevCommands.FormulaPath(d.Name) is { } p)
                {
                    try { Walk(FormulaLoader.LoadSpec(p)); }
                    catch (InvalidOperationException) { }
                }
            }
        }
        Walk(f);
        return missing;
    }

    public override void Run(ParsedArgs args)
    {
        var formulae = args.Named.Select(DevCommands.LoadFormula).ToList();
        var failedOnce = new HashSet<string>();
        foreach (var f in formulae)
        {
            var fullName = LivecheckEngine.FullName(f);
            var keg = LatestKeg(f, args.Switch("HEAD"));
            if (keg == null)
            {
                DevCommands.Ofail($"Testing requires the latest version of {fullName}");
                continue;
            }
            if (!f.HasTest)
            {
                DevCommands.Ofail($"{fullName} defines no test");
                continue;
            }
            if (!args.Switch("force") && !f.KegOnly && !Linked(keg))
            {
                DevCommands.Ofail($"{fullName} is not linked");
                continue;
            }
            var missing = MissingTestDeps(f);
            if (missing.Count > 0)
            {
                DevCommands.Ofail($"{fullName} is missing test dependencies: {string.Join(" ", missing)}");
                continue;
            }

            Out.Oh1($"Testing {fullName}");
            while (true)
            {
                var testPath = Path.Combine(Temp, $"{f.Name}-test-{DateTime.Now:yyyyMMdd}-{Environment.ProcessId}-{Guid.NewGuid().ToString("N")[..6]}");
                Directory.CreateDirectory(testPath);
                bool retain = args.Switch("keep-tmp");
                var oldTestPath = Environment.GetEnvironmentVariable("HOMEBREW_TEST_PATH");
                Exception error = null;
                var materialized = new List<string>();
                try
                {
                    Environment.SetEnvironmentVariable("HOMEBREW_TEST_PATH", testPath);
                    materialized = MaterializeBin(keg);
                    f.RunTest(keg, DevCommands.Prefix, testPath);
                }
                catch (Exception e) when (e is InvalidOperationException or RubyError or RubySyntaxError or IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
                {
                    error = e;
                    if (args.Debug) retain = true;
                }
                finally
                {
                    Unmaterialize(materialized);
                    Environment.SetEnvironmentVariable("HOMEBREW_TEST_PATH", oldTestPath);
                    if (retain) Out.Ohai("Temporary files retained at:", testPath);
                    else
                    {
                        try { Directory.Delete(testPath, true); }
                        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
                    }
                }
                if (error == null) break;

                if (args.Switch("retry") && failedOnce.Add(f.Name))
                {
                    Out.Oh1($"Testing {fullName} (again)");
                    Environment.SetEnvironmentVariable("RUST_BACKTRACE", "full");
                    continue;
                }
                DevCommands.Ofail($"{fullName}: failed");
                Console.Error.WriteLine(ChildError(error));
                if (args.Debug && error.StackTrace != null) Console.Error.WriteLine(error.StackTrace);
                break;
            }
        }
    }
}
