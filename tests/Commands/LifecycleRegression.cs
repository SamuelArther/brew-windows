using System.Text.Json;
using Brew;

static class LifecycleRegression
{
    public static async Task<int> Run(string fixtureRoot)
    {
        var keys = new[] { "BREW_PREFIX", "BREW_REPOSITORY", "BREW_CACHE" };
        var saved = keys.ToDictionary(k => k, Environment.GetEnvironmentVariable);
        var root = Path.Combine(fixtureRoot, "lifecycle-regression");
        var prefix = Path.Combine(root, "prefix"); var repo = Path.Combine(root, "repo"); var cache = Path.Combine(root, "cache");
        var count = 0;
        try
        {
            Environment.SetEnvironmentVariable("BREW_PREFIX", prefix);
            Environment.SetEnvironmentVariable("BREW_REPOSITORY", repo);
            Environment.SetEnvironmentVariable("BREW_CACHE", cache);
            Directory.CreateDirectory(cache);
            foreach (var file in Directory.GetFiles(Path.Combine(fixtureRoot, "cache"))) File.Copy(file, Path.Combine(cache, Path.GetFileName(file)));
            foreach (var name in new[] { "probe", "leaf", "isolated" })
            {
                var relative = Path.Combine("tap", "homebrew-core", "Formula", name[..1], name + ".rb");
                var target = Path.Combine(repo, relative); Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(Path.Combine(fixtureRoot, "repository", relative), target);
            }
            var aliases = Path.Combine(repo, "tap", "homebrew-core", "Aliases"); Directory.CreateDirectory(aliases);
            File.WriteAllText(Path.Combine(aliases, "probe@1"), "../Formula/p/probe.rb");
            Check((await Brew("install", "probe@1")).Code == 0, "install alias fixture");
            Check((await Brew("list", "--versions", "probe@1")).Output == "probe 2.0\n", "list resolves alias");
            Check((await Brew("pin", "probe@1")).Code == 0, "pin resolves alias");
            Check(File.Exists(Path.Combine(prefix, "var", "homebrew", "pinned", "probe")), "pin uses canonical name");
            Check((await Brew("unpin", "probe@1")).Code == 0, "unpin resolves alias");
            var keg = new InstallationStore(new Paths()).Latest("probe");
            var receipt = Path.Combine(keg.Path, "INSTALL_RECEIPT.json"); var originalReceipt = File.ReadAllText(receipt);
            var payload = Path.Combine(keg.Path, "bin", "probe.cmd");
            foreach (var lockedPath in new[] { receipt, payload })
            {
                using (var locked = new FileStream(lockedPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    Check((await Brew("uninstall", "probe@1")).Code == 1, "locked file prevents uninstall");
                Check(File.ReadAllText(receipt) == originalReceipt, "failed uninstall preserves receipt");
                Check(File.Exists(payload), "failed uninstall preserves payload");
                Check(File.Exists(Path.Combine(prefix, "bin", "probe.cmd")), "failed uninstall preserves global link");
                Check(File.Exists(Path.Combine(prefix, "opt", "probe", "bin", "probe.cmd")), "failed uninstall preserves opt link");
            }
            File.SetAttributes(payload, File.GetAttributes(payload) | FileAttributes.ReadOnly);
            try { Check((await Brew("uninstall", "probe")).Code == 1, "read-only payload prevents uninstall"); }
            finally { File.SetAttributes(payload, File.GetAttributes(payload) & ~FileAttributes.ReadOnly); }
            Check(File.Exists(Path.Combine(prefix, "bin", "probe.cmd")), "read-only failure preserves links");
            Check((await Brew("unlink", "probe@1")).Code == 0, "unlink resolves alias");
            Check((await Brew("reinstall", "probe@1")).Code == 0, "reinstall unlinked alias");
            Check(!File.Exists(Path.Combine(prefix, "bin", "probe.cmd")), "reinstall preserves unlinked state");
            Check(File.Exists(Path.Combine(prefix, "opt", "probe", "bin", "probe.cmd")), "unlinked reinstall preserves opt bin");
            var probeFile = Path.Combine(repo, "tap", "homebrew-core", "Formula", "p", "probe.rb");
            File.WriteAllText(probeFile, File.ReadAllText(probeFile).Replace("version \"2.0\"", "version \"3.0\""));
            Check((await Brew("upgrade", "probe@1")).Code == 0, "upgrade unlinked alias");
            Check(!File.Exists(Path.Combine(prefix, "bin", "probe.cmd")), "upgrade preserves unlinked state");
            Check((await Brew("link", "probe@1")).Code == 0, "link resolves alias");
            Check(File.Exists(Path.Combine(prefix, "bin", "probe.cmd")), "alias relinks canonical keg");

            Check((await Brew("install", "isolated")).Code == 0, "install keg-only fixture");
            Check((await Brew("link", "isolated")).Code == 0, "versioned keg-only link allowed without force");
            Check(File.Exists(Path.Combine(prefix, "bin", "isolated.exe")), "versioned exception creates link");
            await Brew("unlink", "isolated");
            var isolatedFile = Path.Combine(repo, "tap", "homebrew-core", "Formula", "i", "isolated.rb");
            File.WriteAllText(isolatedFile, File.ReadAllText(isolatedFile).Replace("keg_only :versioned_formula", "keg_only \"conflicts with another package\""));
            var refused = await Brew("link", "isolated");
            Check(refused.Error == "Warning: isolated is keg-only and must be linked with `--force`.\n", "keg-only force warning matches Homebrew");
            Check(!File.Exists(Path.Combine(prefix, "bin", "isolated.exe")), "keg-only refusal leaves global bin untouched");
            Check((await Brew("link", "--force", "isolated")).Code == 0, "explicit force permits keg-only linking");
            Check(File.Exists(Path.Combine(prefix, "bin", "isolated.exe")), "forced link creates executable");
            Check((await Brew("reinstall", "isolated")).Code == 0, "reinstall forcibly linked keg-only formula");
            Check(File.Exists(Path.Combine(prefix, "bin", "isolated.exe")), "reinstall preserves forced keg-only link");

            var paths = new Paths(); var installed = new InstallationStore(paths).Latest("probe").Receipt;
            var selectedCache = Path.Combine(cache, installed.Sha256 + "--" + Path.GetFileName(new Uri(installed.Url).LocalPath));
            var leaf = new InstallationStore(paths).Latest("leaf").Receipt;
            var unrelatedCache = Path.Combine(cache, leaf.Sha256 + "--" + Path.GetFileName(new Uri(leaf.Url).LocalPath));
            File.SetLastWriteTimeUtc(selectedCache, DateTime.UtcNow.AddDays(-130));
            File.SetLastWriteTimeUtc(unrelatedCache, DateTime.UtcNow.AddDays(-130));
            var dry = await Brew("cleanup", "--dry-run", "probe@1");
            Check(dry.Output.Contains(selectedCache) && !dry.Output.Contains(unrelatedCache), "named cleanup dry run selects only matching cache");
            Check(File.Exists(selectedCache), "cleanup dry run keeps selected download");
            Check((await Brew("cleanup", "probe@1")).Code == 0, "named cleanup resolves alias");
            Check(!File.Exists(selectedCache), "named cleanup removes old matching download");
            Check(File.Exists(unrelatedCache), "named cleanup preserves unrelated old download");
            File.Copy(Path.Combine(fixtureRoot, "cache", Path.GetFileName(selectedCache)), selectedCache);
            Check((await Brew("uninstall", "probe@1")).Code == 0, "uninstall resolves alias");
            Check(!Directory.Exists(paths.Rack("probe")), "alias uninstall removes canonical rack");
            await Brew("uninstall", "isolated"); await Brew("uninstall", "leaf");

            var tapDir = Path.Combine(repo, "tap", "other-tools", "Formula", "l"); Directory.CreateDirectory(tapDir);
            File.Copy(Path.Combine(repo, "tap", "homebrew-core", "Formula", "l", "leaf.rb"), Path.Combine(tapDir, "leaf.rb"));
            File.WriteAllText(probeFile, File.ReadAllText(probeFile).Replace("depends_on \"leaf\"", "depends_on \"other/tools/leaf\""));
            Check((await Brew("install", "probe")).Code == 0, "install fully qualified dependency");
            Check((await Brew("uninstall", "leaf")).Code == 1, "uninstall protects fully qualified dependency");
            Check((await Brew("autoremove", "--dry-run")).Output == "", "autoremove dry run retains qualified dependency");
            Check((await Brew("autoremove")).Code == 0, "autoremove qualified dependency graph");
            Check(Directory.Exists(paths.Rack("leaf")), "autoremove retains required dependency rack");
            Check((await Brew("missing", "probe")).Code == 0, "qualified dependency remains satisfied");
            Check((await Brew("leaves")).Output == "probe\n", "leaves normalizes qualified dependencies");
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
