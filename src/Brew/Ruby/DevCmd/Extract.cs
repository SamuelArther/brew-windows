using System.Text.RegularExpressions;

namespace Brew.Ruby.DevCmd;

// Tap helpers: taps live at <repository>/tap/<user>-<repo> (homebrew/core = tap/homebrew-core).
static class DevTap
{
    public static string Root => System.IO.Path.Combine(DevCommands.Repository, "tap");

    public static string Normalize(string name)
    {
        var parts = name.ToLowerInvariant().Split('/');
        if (parts.Length != 2 || !Regex.IsMatch(name, @"\A[a-zA-Z0-9_.-]+/[a-zA-Z0-9_.-]+\z"))
            throw new BrewError($"Invalid tap name: '{name}'");
        return parts[0] + "/" + Regex.Replace(parts[1], "^homebrew-", "");
    }

    public static string Path(string name)
    {
        var n = Normalize(name).Split('/');
        return System.IO.Path.Combine(Root, n[0] + "-" + n[1]);
    }

    public static bool IsCore(string name) => Normalize(name) == "homebrew/core";

    // Tap.from_path: "user/repo" for a file inside a tap, else null.
    public static string FromPath(string file)
    {
        if (file == null) return null;
        var rel = System.IO.Path.GetRelativePath(Root, System.IO.Path.GetFullPath(file));
        if (rel.StartsWith("..") || System.IO.Path.IsPathRooted(rel)) return null;
        var dir = rel.Split(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar)[0];
        var meta = System.IO.Path.Combine(Root, dir, ".brew-tap.json");
        if (File.Exists(meta))
        {
            try
            {
                var n = System.Text.Json.JsonDocument.Parse(File.ReadAllText(meta)).RootElement.GetProperty("name").GetString();
                if (!string.IsNullOrEmpty(n)) return n;
            }
            catch (Exception e) when (e is System.Text.Json.JsonException or KeyNotFoundException) { }
        }
        int dash = dir.IndexOf('-');
        return dash < 0 ? dir : dir[..dash] + "/" + dir[(dash + 1)..];
    }

    // DevCommands.FormulaPath plus fully-qualified <user>/<repo>/<formula> names in any tap.
    public static string FormulaPath(string arg)
    {
        var parts = arg.Split('/');
        if (parts.Length == 3 && !arg.EndsWith(".rb", StringComparison.OrdinalIgnoreCase))
        {
            string dir;
            try { dir = Path(parts[0] + "/" + parts[1]); }
            catch (BrewError) { return null; }
            var name = parts[2].ToLowerInvariant();
            return Directory.Exists(dir) ? FormulaFiles(dir).FirstOrDefault(f => System.IO.Path.GetFileNameWithoutExtension(f) == name) : null;
        }
        return DevCommands.FormulaPath(arg);
    }

    public static FormulaSpec LoadFormula(string arg)
    {
        var path = FormulaPath(arg) ?? throw new BrewError($"No available formula with the name \"{DevCommands.NameOf(arg)}\".");
        return FormulaLoader.LoadSpec(path);
    }

    public static string FormulaDir(string tapDir) =>
        Directory.Exists(System.IO.Path.Combine(tapDir, "Formula")) ? System.IO.Path.Combine(tapDir, "Formula")
        : Directory.Exists(System.IO.Path.Combine(tapDir, "HomebrewFormula")) ? System.IO.Path.Combine(tapDir, "HomebrewFormula")
        : tapDir;

    public static IEnumerable<string> FormulaFiles(string tapDir)
    {
        var dir = FormulaDir(tapDir);
        if (!Directory.Exists(dir)) return Enumerable.Empty<string>();
        var opt = dir == tapDir ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;
        return Directory.EnumerateFiles(dir, "*.rb", opt).Order(StringComparer.Ordinal);
    }

    // Tap#new_formula_path: sharded by first letter for homebrew/core (or already-sharded taps).
    public static string NewFormulaPath(string tapName, string name)
    {
        var dir = System.IO.Path.Combine(Path(tapName), "Formula");
        bool sharded = IsCore(tapName) || (Directory.Exists(dir) && Directory.GetDirectories(dir).Any(d => System.IO.Path.GetFileName(d).Length == 1));
        name = name.ToLowerInvariant();
        return sharded ? System.IO.Path.Combine(dir, name[..1], name + ".rb") : System.IO.Path.Combine(dir, name + ".rb");
    }

    public static string TapUnavailableMessage(string name)
    {
        var msg = $"No available tap {name}.\n";
        return name is "homebrew/core" or "homebrew/cask"
            ? msg + $"Run {Formatter.Identifier($"brew tap --force {name}")} to tap {name}!"
            : msg + $"Run {Formatter.Identifier($"brew tap-new {name}")} to create a new {name} tap!";
    }

    // Tap#install: clone https://github.com/<user>/homebrew-<repo>.
    public static void Install(string name)
    {
        var n = Normalize(name).Split('/');
        var dir = Path(name);
        var git = DevCommands.Which("git") ?? throw new BrewError("Git must be installed to tap repositories.");
        Out.Ohai($"Tapping {n[0]}/{n[1]}");
        Directory.CreateDirectory(Root);
        var code = DevCommands.Exec(git, new[] { "clone", $"https://github.com/{n[0]}/homebrew-{n[1]}", dir });
        if (code != 0) throw new BrewError($"Failure while executing; `git clone https://github.com/{n[0]}/homebrew-{n[1]} {dir}` exited with {code}.");
    }

    public static string Git => DevCommands.Which("git") ?? throw new BrewError("Git must be installed.");

    public static string GitRead(string repo, params string[] args)
    {
        var (code, stdout, _) = DevCommands.Capture(Git, new[] { "-C", repo }.Concat(args));
        return code == 0 ? stdout : "";
    }

    public static bool IsGitRepo(string dir) => Directory.Exists(dir) && GitRead(dir, "rev-parse", "--git-dir").Trim().Length > 0;
}

[DevCommand("extract")]
sealed class Extract : DevCommand
{
    static readonly Regex BottleBlockRegex = new(@"  bottle (?:do.+?end|:[a-z]+)\n\n", RegexOptions.Singleline);

    public override CommandParser Args() => new CommandParser("extract")
        .UsageBanner("`extract` [`--version=`] [`--git-revision=`] [`--force`] <formula> <tap>")
        .Description("""
            Look through repository history to find the most recent version of <formula> and
            create a copy in <tap>. Specifically, the command will create the new
            formula file at <tap>`/Formula/`<formula>`@`<version>`.rb`. If the tap is not
            installed yet, attempt to install/clone the tap before continuing. To extract
            a formula from a tap that is not `homebrew/core` use its fully-qualified form of
            <user>`/`<repo>`/`<formula>.
            """)
        .Flag(new[] { "--git-revision=" }, "Search for the specified <version> of <formula> starting at <revision> instead of HEAD.")
        .Flag(new[] { "--version=" }, "Extract the specified <version> of <formula> instead of the most recent.")
        .Switch(new[] { "-f", "--force" }, "Overwrite the destination formula if it already exists.")
        .NamedArgs(new[] { "formula", "tap" }, number: 2);

    static void Odebug(ParsedArgs args, string s)
    {
        if (args.Debug) Console.Error.WriteLine(Formatter.Headline(s, Tty.Magenta));
    }

    public override void Run(ParsedArgs args)
    {
        var first = args.Named[0];
        string sourceTap, name;
        var parts = first.Split('/');
        if (parts.Length == 3) { sourceTap = DevTap.Normalize(parts[0] + "/" + parts[1]); name = parts[2].ToLowerInvariant(); }
        else { name = first.ToLowerInvariant(); sourceTap = "homebrew/core"; }
        var repo = DevTap.Path(sourceTap);
        if (!Directory.Exists(repo)) throw new BrewError($"No available formula with the name \"{sourceTap}/{name}\".\nThis command requires the tap {sourceTap}.\nIf you trust this tap, tap it explicitly and then try again:\n  brew tap {sourceTap}");

        var destTap = DevTap.Normalize(args.Named[1]);
        if (Environment.GetEnvironmentVariable("HOMEBREW_DEVELOPER") is not { Length: > 0 })
        {
            if (destTap == "homebrew/core") throw new BrewError("Cannot extract formula to homebrew/core!");
            if (destTap == "homebrew/cask") throw new BrewError("Cannot extract formula to homebrew/cask!");
            if (destTap == sourceTap) throw new BrewError("Cannot extract formula to the same tap!");
        }
        var destDir = DevTap.Path(destTap);
        if (!Directory.Exists(destDir)) DevTap.Install(destTap);

        var startRev = args.Flag("git-revision") ?? "HEAD";
        var pattern = sourceTap == "homebrew/core"
            ? new[] { Rel(repo, DevTap.NewFormulaPath(sourceTap, name)), $"Formula/{name}.rb" }.Distinct().ToArray()
            : new[] { $"{name}.rb", $"**/{name}.rb" };

        string rev = null, result = "";
        FormulaSpec formula = null;
        var version = args.Flag("version");
        Out.Ohai("Searching repository history");
        if (version != null)
        {
            var segments = GemSegments(version);
            while (true)
            {
                rev = rev == null ? startRev : $"{rev}~1";
                var (r, paths) = LastRevisionCommitOfFiles(repo, pattern, rev);
                rev = r;
                if (rev == null && File.Exists(Path.Combine(GitDir(repo), "shallow")))
                    throw new BrewError($"Could not find {name} but {sourceTap} is a shallow clone!\nTry again after running:\n  git -C \"{repo}\" fetch --unshallow");
                if (rev == null || paths.Count == 0) throw new BrewError($"Could not find {name}! The formula or version may not have existed.");
                var file = paths[0];
                result = LastRevisionOfFile(repo, file, rev);
                if (result.Length == 0)
                {
                    Odebug(args, $"Skipping revision {rev} - file is empty at this revision");
                    continue;
                }
                formula = FormulaAtRevision(repo, name, file, rev);
                if (formula == null || formula.Version == version) break;
                if (segments != null && GemSegments(formula.Version) is { } test && segments.Length < test.Length)
                {
                    Odebug(args, $"Apply semantic versioning with [{string.Join(", ", test)}]");
                    if (segments.SequenceEqual(test.Take(segments.Length))) break;
                }
                Odebug(args, $"Trying {formula.Version} from revision {rev} against desired {version}");
            }
        }
        else
        {
            var (r, paths) = LastRevisionCommitOfFiles(repo, pattern, startRev);
            rev = r;
            if (rev == null || paths.Count == 0) throw new BrewError($"Could not find {name}! The formula or version may not have existed.");
            formula = FormulaAtRevision(repo, name, paths[0], rev);
            result = FileAtCommit(repo, paths[0], rev);
        }
        if (formula == null) throw new BrewError($"Could not find {name}! The formula or version may not have existed.");
        version ??= formula.Version;

        var className = FormulaLoader.ClassName(name);
        var versionString = Regex.Replace(Regex.Replace(version, @"^\D*(.+?)\D*$", "$1"), @"\D+", ".");
        name = Regex.Replace(name, @"\b@.*\z", "", RegexOptions.IgnoreCase);
        var versionedName = FormulaLoader.ClassName($"{name}@{versionString}");
        result = ReplaceFirst(result, $"class {className} < Formula", $"class {versionedName} < Formula");
        result = BottleBlockRegex.Replace(result, "", 1);

        var path = Path.Combine(destDir, "Formula", $"{name}@{versionString}.rb");
        if (File.Exists(path))
        {
            if (!args.Switch("force"))
                throw new BrewError($"Destination formula already exists: {path}\nTo overwrite it and continue anyways, run:\n  brew extract --force --version={version} {name} {destTap}");
            Odebug(args, $"Overwriting existing formula at {path}");
            File.Delete(path);
        }
        Out.Ohai($"Writing formula for {name} at {version} from revision {rev} to:", path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, result);
    }

    static string ReplaceFirst(string s, string old, string replacement)
    {
        int i = s.IndexOf(old, StringComparison.Ordinal);
        return i < 0 ? s : s[..i] + replacement + s[(i + old.Length)..];
    }

    static string Rel(string repo, string path) => Path.GetRelativePath(repo, path).Replace('\\', '/');

    static string GitDir(string repo)
    {
        var d = DevTap.GitRead(repo, "rev-parse", "--absolute-git-dir").Trim();
        return d.Length > 0 ? d : Path.Combine(repo, ".git");
    }

    // Gem::Version#segments when Gem::Version.correct?
    static string[] GemSegments(string v) =>
        v != null && Regex.IsMatch(v, @"\A\s*([0-9]+(?>\.[0-9a-zA-Z]+)*(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?)?\s*\z")
            ? Regex.Matches(v, @"[0-9]+|[a-z]+", RegexOptions.IgnoreCase).Select(m => m.Value).ToArray()
            : null;

    // Utils::Git.last_revision_commit_of_files (paths relative to the tap, like Homebrew's tap-rooted repos)
    static (string rev, List<string> paths) LastRevisionCommitOfFiles(string repo, string[] files, string beforeCommit)
    {
        var a = new List<string> { "log", "--pretty=format:%h", "--abbrev=7", "--max-count=1", "--diff-filter=d", "--name-only", "--relative", beforeCommit.Split("..")[0], "--" };
        a.AddRange(files.Select(f => f.Contains('*') ? $":(glob){f}" : f));
        var lines = DevTap.GitRead(repo, a.ToArray()).Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Length > 0).ToList();
        return lines.Count == 0 ? (null, new List<string>()) : (lines[0], lines.Skip(1).ToList());
    }

    static string LastRevisionOfFile(string repo, string file, string beforeCommit)
    {
        var commit = DevTap.GitRead(repo, "log", "--format=%h", "--abbrev=7", "--max-count=1", beforeCommit.Split("..")[0], "--", file).Trim();
        return FileAtCommit(repo, file, commit);
    }

    static string FileAtCommit(string repo, string file, string commit) =>
        commit.Length == 0 ? "" : DevTap.GitRead(repo, "show", $"{commit}:./{file}");

    static FormulaSpec FormulaAtRevision(string repo, string name, string file, string rev)
    {
        if (string.IsNullOrEmpty(rev)) return null;
        var contents = LastRevisionOfFile(repo, file, rev);
        contents = contents.Replace("@url=", "url ").Replace("require 'brewkit'", "require 'formula'");
        contents = BottleBlockRegex.Replace(contents, "", 1);
        var dir = Path.Combine(Path.GetTempPath(), "brew-extract-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            var tmp = Path.Combine(dir, name + ".rb");
            File.WriteAllText(tmp, contents);
            try { return FormulaLoader.LoadSpec(tmp); }
            catch (Exception e) when (e is InvalidOperationException or RubySyntaxError or RubyError) { return null; }
        }
        finally { Directory.Delete(dir, true); }
    }
}
