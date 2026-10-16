using Brew;

// `brew tap` / `brew update` against local git remotes (no network), and brew.exe's self-replacement.
static class TapUpdate
{
    public static async Task<int> Run(string fixtureRoot)
    {
        var keys = new[] { "BREW_PREFIX", "BREW_REPOSITORY", "BREW_NO_SELF_UPDATE" };
        var saved = keys.ToDictionary(k => k, Environment.GetEnvironmentVariable);
        var root = Path.Combine(fixtureRoot, "tap-update");
        var count = 0;
        void Check(bool condition, string label) { count++; if (!condition) throw new Exception("FAIL " + label); }
        try
        {
            var origin = Path.Combine(root, "origin"); var tapOrigin = Path.Combine(root, "tap-origin"); var repo = Path.Combine(root, "repo");
            await Commit(origin, Path.Combine("tap", "homebrew-core", "Formula", "a", "alpha.rb"), Formula("Alpha"), init: true);
            await Commit(tapOrigin, Path.Combine("Formula", "beta.rb"), Formula("Beta"), init: true);
            await ProcessRunner.Git(root, "clone", "-q", origin, repo);
            Environment.SetEnvironmentVariable("BREW_PREFIX", Path.Combine(root, "prefix"));
            Environment.SetEnvironmentVariable("BREW_REPOSITORY", repo);
            Environment.SetEnvironmentVariable("BREW_NO_SELF_UPDATE", "1");

            var tapped = await Brew("tap", "foo/homebrew-bar", tapOrigin);
            Check(tapped.Code == 0, "tap from a local remote: " + tapped.Error);
            Check(tapped.Error.Contains("Tapped 1 formula ("), "Homebrew tap summary: " + tapped.Error);
            Check((await Brew("tap")).Output.Contains("foo/bar\n"), "user/homebrew-repo taps as user/repo");
            var again = await Brew("tap", "foo/bar");
            Check(again.Code == 0 && again.Output == "" && again.Error == "", "tapping an installed tap is silent");

            // The repository now holds an untracked tap clone, and the tap an untracked .brew-tap.json.
            await Commit(origin, Path.Combine("tap", "homebrew-core", "Formula", "g", "gamma.rb"), Formula("Gamma"));
            await Commit(tapOrigin, Path.Combine("Formula", "delta.rb"), Formula("Delta"));
            var updated = await Brew("update");
            Check(updated.Code == 0, "update with tapped taps: " + updated.Error);
            Check(File.Exists(Path.Combine(repo, "tap", "homebrew-core", "Formula", "g", "gamma.rb")), "update fast-forwards the repository");
            Check(File.Exists(Path.Combine(repo, "tap", "foo-bar", "Formula", "delta.rb")), "update fast-forwards a tap");

            File.WriteAllText(Path.Combine(repo, "tap", "homebrew-core", "Formula", "a", "alpha.rb"), "# local edit\n");
            await Commit(tapOrigin, Path.Combine("Formula", "epsilon.rb"), Formula("Epsilon"));
            var blocked = await Brew("update");
            Check(blocked.Code == 1 && blocked.Error.Contains("uncommitted changes"), "local edits block that repository's update");
            Check(File.Exists(Path.Combine(repo, "tap", "foo-bar", "Formula", "epsilon.rb")), "other taps still update");

            var untapped = await Brew("untap", "foo/bar");
            Check(untapped.Code == 0 && untapped.Error.StartsWith("Untapping foo/bar...\nUntapped 3 formulae ("), "Homebrew untap messages: " + untapped.Error);

            var bin = Path.Combine(root, "bin"); var staging = Path.Combine(root, "staging");
            Directory.CreateDirectory(bin); Directory.CreateDirectory(staging);
            var exe = Path.Combine(bin, "brew.exe");
            File.WriteAllText(exe, "old"); File.WriteAllText(Path.Combine(staging, "LICENSE"), "license");
            try { TapCommands.ReplaceExe(exe, staging, "brew-windows-9.9.9"); Check(false, "release without brew.exe accepted"); }
            catch (InvalidOperationException) { }
            Check(File.ReadAllText(exe) == "old" && !File.Exists(exe + ".old"), "release without brew.exe leaves brew.exe alone");
            File.WriteAllText(Path.Combine(staging, "brew.exe"), "new");
            TapCommands.ReplaceExe(exe, staging, "brew-windows-9.9.9");
            Check(File.ReadAllText(exe) == "new" && File.ReadAllText(exe + ".old") == "old", "self-update swaps brew.exe and keeps the old one");
            Check(File.ReadAllText(Path.Combine(bin, "LICENSE")) == "license" && !File.Exists(exe + ".new"), "self-update copies the rest and cleans up");
            return count;
        }
        finally { foreach (var pair in saved) Environment.SetEnvironmentVariable(pair.Key, pair.Value); }
    }

    static string Formula(string name) => $"class {name} < Formula\n  url \"https://example.invalid/{name}.zip\"\n  version \"1\"\n  sha256 \"{new string('0', 64)}\"\nend\n";

    static async Task Commit(string repository, string relative, string text, bool init = false)
    {
        if (init) { Directory.CreateDirectory(repository); await ProcessRunner.Git(repository, "init", "-q", "-b", "main"); }
        var file = Path.Combine(repository, relative); Directory.CreateDirectory(Path.GetDirectoryName(file));
        File.WriteAllText(file, text);
        await ProcessRunner.Git(repository, "add", "-A");
        await ProcessRunner.Git(repository, "-c", "user.name=test", "-c", "user.email=test@example.invalid", "-c", "commit.gpgsign=false", "commit", "-q", "-m", relative);
    }

    static async Task<(int Code, string Output, string Error)> Brew(params string[] arguments)
    {
        var output = new StringWriter(); var error = new StringWriter(); var oldOut = Console.Out; var oldError = Console.Error;
        try { Console.SetOut(output); Console.SetError(error); var code = await Cli.RunAsync(arguments); return (code, output.ToString().Replace("\r\n", "\n"), error.ToString().Replace("\r\n", "\n")); }
        finally { Console.SetOut(oldOut); Console.SetError(oldError); }
    }
}
