using System.Text.Json;
using System.Text.RegularExpressions;
using Brew;

// Homebrew parity for keg lifecycles: outdated dependencies upgrade first, uninstall takes only the
// default keg, link --overwrite/--dry-run, the prefix lock's message, a finished install that can't
// show caveats, and an install interrupted before its receipt.
static class KegLifecycle
{
    public static async Task<int> Run(string fixtureRoot)
    {
        var keys = new[] { "BREW_PREFIX", "BREW_REPOSITORY", "BREW_CACHE" };
        var saved = keys.ToDictionary(k => k, Environment.GetEnvironmentVariable);
        var root = Path.Combine(fixtureRoot, "keg-lifecycle");
        var prefix = Path.Combine(root, "prefix"); var repo = Path.Combine(root, "repo"); var cache = Path.Combine(root, "cache");
        var count = 0;
        try
        {
            Environment.SetEnvironmentVariable("BREW_PREFIX", prefix);
            Environment.SetEnvironmentVariable("BREW_REPOSITORY", repo);
            Environment.SetEnvironmentVariable("BREW_CACHE", cache);
            Directory.CreateDirectory(cache);
            foreach (var file in Directory.GetFiles(Path.Combine(fixtureRoot, "cache"))) File.Copy(file, Path.Combine(cache, Path.GetFileName(file)));
            string FormulaFile(string name) => Path.Combine(repo, "tap", "homebrew-core", "Formula", name[..1], name + ".rb");
            foreach (var name in new[] { "probe", "leaf" })
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FormulaFile(name)));
                File.Copy(Path.Combine(fixtureRoot, "repository", "tap", "homebrew-core", "Formula", name[..1], name + ".rb"), FormulaFile(name));
            }
            void Bump(string name, string to) => File.WriteAllText(FormulaFile(name), Regex.Replace(File.ReadAllText(FormulaFile(name)), "version \"[^\"]+\"", $"version \"{to}\""));
            Bump("probe", "1.0"); Bump("leaf", "1.0");

            // A second brew gets Homebrew's OperationInProgressError, not a raw sharing violation.
            Directory.CreateDirectory(prefix);
            using (new FileStream(Path.Combine(prefix, ".brew.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var locked = await Brew("install", "leaf");
                Check(locked.Code == 1 && locked.Error == $"Error: A `brew install leaf` process has already locked {prefix}.\nPlease wait for it to finish or terminate it to continue.\n",
                    "lock message: " + locked.Error);
            }

            // Outdated dependencies are upgraded first (FormulaInstaller#install_dependency).
            Check((await Brew("install", "leaf")).Code == 0, "install dependency on request");
            Bump("leaf", "2.0");
            var install = await Brew("install", "probe");
            Check(install.Code == 0 && install.Output.Contains("==> Upgrading probe dependency: leaf (1.0 -> 2.0)\n"), "install upgrades an outdated dependency: " + install.Output + install.Error);
            Check((await Brew("list", "--versions", "leaf")).Output == "leaf 2.0\n", "dependency upgrade replaces its old keg");
            Check(new InstallationStore(new Paths()).Latest("leaf").Receipt.InstalledOnRequest, "dependency upgrade keeps installed-on-request");
            Check(File.Exists(Path.Combine(prefix, "bin", "leaf.cmd")), "dependency upgrade keeps it linked");
            Bump("leaf", "3.0"); Bump("probe", "3.0");
            var upgrade = await Brew("upgrade", "probe");
            Check(upgrade.Code == 0 && upgrade.Output.Contains("==> Upgrading probe dependency: leaf (2.0 -> 3.0)\n"), "upgrade upgrades an outdated dependency: " + upgrade.Output + upgrade.Error);
            Check((await Brew("list", "--versions", "leaf")).Output == "leaf 3.0\n", "upgraded dependency version");
            Bump("leaf", "4.0"); Bump("probe", "4.0");
            var both = await Brew("upgrade", "probe", "leaf");
            Check(both.Code == 0 && both.Error == "" && both.Output.IndexOf("==> Upgrading leaf\n") is >= 0 and var first && first < both.Output.IndexOf("==> Upgrading probe\n"),
                "upgrade sorts dependencies first: " + both.Output + both.Error);
            Check((await Brew("pin", "leaf")).Code == 0, "pin dependency");
            Bump("leaf", "5.0"); Bump("probe", "5.0");
            var pinned = await Brew("upgrade", "probe");
            Check(pinned.Code == 1 && pinned.Error.Contains("Error: You must `brew unpin leaf` as installing probe requires the latest version of pinned dependencies.\n"), "pinned outdated dependency: " + pinned.Error);
            Check((await Brew("list", "--versions", "probe")).Output == "probe 4.0\n", "refused upgrade keeps the old keg");
            Check((await Brew("unpin", "leaf")).Code == 0, "unpin dependency");
            Bump("leaf", "4.0"); Bump("probe", "4.0");

            // brew unlink/link --dry-run and link --overwrite (cmd/link.rb, Keg#link).
            var leafKeg = new InstallationStore(new Paths()).Latest("leaf").Path;
            var leafLink = Path.Combine(prefix, "bin", "leaf.cmd");
            Check((await Brew("unlink", "--dry-run", "leaf")).Output == $"Would remove:\n{leafLink}\n", "unlink dry run lists links");
            Check(new FileInfo(leafLink).LinkTarget != null, "unlink dry run keeps links");
            Check((await Brew("unlink", "leaf")).Code == 0, "unlink");
            File.WriteAllText(leafLink, "user file");
            var refused = await Brew("link", "leaf");
            Check(refused.Code == 1 && refused.Output == $"Linking {leafKeg}... \n" && refused.Error.Contains("To force the link and overwrite all conflicting files:\n  brew link --overwrite leaf\n"), "link conflict: " + refused.Output + refused.Error);
            Check((await Brew("link", "--dry-run", "leaf")).Output == $"Would link:\n{leafLink}\n", "link dry run lists links");
            Check((await Brew("link", "--overwrite", "--dry-run", "leaf")).Output == $"Would remove:\n{leafLink}\n", "overwrite dry run lists conflicts");
            Check(File.ReadAllText(leafLink) == "user file", "dry run deletes nothing");
            var overwritten = await Brew("link", "--overwrite", "leaf");
            Check(overwritten.Code == 0 && overwritten.Output == $"Linking {leafKeg}... 1 symlinks created.\n", "link --overwrite: " + overwritten.Output + overwritten.Error);
            Check(new FileInfo(leafLink).LinkTarget == Path.Combine(leafKeg, "bin", "leaf.cmd"), "overwrite replaced the conflicting file");

            // Without --force, uninstall removes only the default keg (the one opt/ points at).
            var paths = new Paths(); var current = new InstallationStore(paths).Latest("probe");
            var older = Path.Combine(paths.Rack("probe"), "3.5");
            Directory.CreateDirectory(Path.Combine(older, "bin"));
            File.Copy(Path.Combine(current.Path, "bin", "probe.cmd"), Path.Combine(older, "bin", "probe.cmd"));
            File.WriteAllText(Path.Combine(older, "INSTALL_RECEIPT.json"), JsonSerializer.Serialize(current.Receipt with { Version = "3.5", Links = [] }));
            var size = PackageManager.Abbreviation(current.Path);
            var removal = await Brew("uninstall", "probe");
            Check(removal.Code == 0 && removal.Output == $"Uninstalling {current.Path}... ({size})\nprobe 3.5 is still installed.\n"
                + "To remove all versions, run:\n  brew uninstall --force probe\n", "uninstall removes the default keg: " + removal.Output + removal.Error);
            Check(Directory.Exists(older) && !Directory.Exists(current.Path), "other versions stay installed");
            Directory.CreateDirectory(Path.Combine(paths.Rack("probe"), "3.6"));
            File.WriteAllText(Path.Combine(paths.Rack("probe"), "3.6", "INSTALL_RECEIPT.json"), JsonSerializer.Serialize(current.Receipt with { Version = "3.6", Links = [] }));
            var ambiguous = await Brew("uninstall", "probe");
            Check(ambiguous.Code == 1 && ambiguous.Error == "Error: probe has multiple installed versions\nRun `brew uninstall --force probe` to remove all versions.\n", "ambiguous default keg: " + ambiguous.Error);
            var rackSize = PackageManager.Abbreviation(paths.Rack("probe"));
            var forced = await Brew("uninstall", "--force", "probe");
            Check(forced.Code == 0 && forced.Output == $"Uninstalling probe... ({rackSize})\n" && !Directory.Exists(paths.Rack("probe")), "uninstall --force removes the rack: " + forced.Output + forced.Error);

            // Caveats that fail don't roll back a finished install.
            var leafText = File.ReadAllText(FormulaFile("leaf"));
            var url = Regex.Match(leafText, "url \"([^\"]+)\"").Groups[1].Value; var sha = Regex.Match(leafText, "sha256 \"([^\"]+)\"").Groups[1].Value;
            Directory.CreateDirectory(Path.GetDirectoryName(FormulaFile("cav")));
            File.WriteAllText(FormulaFile("cav"), $"class Cav < Formula\n  desc \"caveats fixture\"\n  homepage \"https://example.invalid/cav\"\n  version \"1.0\"\n  url \"{url}\"\n  sha256 \"{sha}\"\n  license \"MIT\"\n"
                + "  def install\n    prefix.install \"leaf.cmd\"\n  end\n  def caveats\n    raise \"caveats fixture failure\"\n  end\nend\n");
            var caveats = await Brew("install", "cav");
            Check(caveats.Code == 0 && caveats.Error.Contains("Warning: Could not show the caveats for cav") && new InstallationStore(new Paths()).Latest("cav") != null,
                "failing caveats keep the install: " + caveats.Output + caveats.Error);

            // A keg whose install died before its receipt: not installed, and the next brew removes it and its links.
            var ghost = Path.Combine(prefix, "Cellar", "ghost", "1.0");
            Directory.CreateDirectory(Path.Combine(ghost, "bin")); File.WriteAllText(Path.Combine(ghost, "bin", "ghost.cmd"), "@echo ghost");
            File.WriteAllText(Path.Combine(ghost, Bottles.PouredReceiptName), JsonSerializer.Serialize(new Receipt("ghost", "1.0", "", "", [])));
            File.WriteAllText(Path.Combine(prefix, "Cellar", "ghost", ".incomplete-1.0"), "");
            var ghostLink = Path.Combine(prefix, "bin", "ghost.cmd"); File.CreateSymbolicLink(ghostLink, Path.Combine(ghost, "bin", "ghost.cmd"));
            Check(!(await Brew("list")).Output.Contains("ghost"), "a keg without its receipt isn't installed");
            var recovered = await Brew("cleanup", "--dry-run");
            Check(recovered.Error.Contains($"Warning: Removed {ghost} left by an interrupted install.\n") && !Directory.Exists(Path.Combine(prefix, "Cellar", "ghost"))
                && new FileInfo(ghostLink).LinkTarget == null && !File.Exists(ghostLink), "interrupted install recovered: " + recovered.Error);

            // Cache names never carry characters Windows refuses (':' would name an alternate data stream).
            var cacheName = Downloader.FileName("https://example.invalid/dl/a%3Ab%3F%22c%7C.tar.gz");
            Check(cacheName.EndsWith(".tar.gz") && cacheName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && !cacheName.Contains(':'), "safe cache file name: " + cacheName);

            // A pinned git revision must really match HEAD.
            var head = "0123456789abcdef0123456789abcdef01234567";
            Check(!GitSources.RevisionMatches(head, "") && !GitSources.RevisionMatches(head, "0") && !GitSources.RevisionMatches(head, "012345")
                && GitSources.RevisionMatches(head, "0123456") && GitSources.RevisionMatches(head, head.ToUpperInvariant())
                && !GitSources.RevisionMatches(head, "0123456789abcdef0123456789abcdef01234568"), "git revision matching");
            try { await GitSources.StageAsync("https://example.invalid/x.git", "v1", "", null, Path.Combine(root, "git-empty")); Check(false, "empty revision accepted"); }
            catch (InvalidOperationException ex) when (ex.Message.StartsWith("Invalid git revision")) { count++; }
            return count;
        }
        finally { foreach (var pair in saved) Environment.SetEnvironmentVariable(pair.Key, pair.Value); }

        void Check(bool condition, string label) { count++; if (!condition) throw new Exception("FAIL " + label); }
    }

    static async Task<(int Code, string Output, string Error)> Brew(params string[] args)
    {
        var originalOut = Console.Out; var originalError = Console.Error;
        var output = new StringWriter(); var error = new StringWriter();
        try { Console.SetOut(output); Console.SetError(error); var code = await Cli.RunAsync(args); return (code, output.ToString().Replace("\r\n", "\n"), error.ToString().Replace("\r\n", "\n")); }
        finally { Console.SetOut(originalOut); Console.SetError(originalError); }
    }
}
