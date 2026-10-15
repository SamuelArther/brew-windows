using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Brew;

var workspace = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../"));
var root = Path.Combine(workspace, ".test-work", "commands-" + Guid.NewGuid().ToString("N"));
var repo = Path.Combine(root, "repository"); var prefix = Path.Combine(root, "prefix"); var cache = Path.Combine(root, "cache");
var keys = new[] { "BREW_PREFIX", "BREW_REPOSITORY", "BREW_CACHE", "HOMEBREW_NO_EMOJI", "HOMEBREW_BROWSER", "PATH" };
var saved = keys.ToDictionary(k => k, Environment.GetEnvironmentVariable);
Environment.SetEnvironmentVariable("BREW_PREFIX", prefix); Environment.SetEnvironmentVariable("BREW_REPOSITORY", repo);
Environment.SetEnvironmentVariable("BREW_CACHE", cache); Environment.SetEnvironmentVariable("HOMEBREW_NO_EMOJI", "1");
Environment.SetEnvironmentVariable("HOMEBREW_BROWSER", "false");
Environment.SetEnvironmentVariable("PATH", Path.Combine(prefix, "bin") + ";" + saved["PATH"]);
Directory.CreateDirectory(cache);
int assertions = 0;
try
{
    await Formula("leaf", "1.0"); await Formula("probe", "1.0", dependency: "leaf");
    await Formula("terraform", "1.0", tap: "hashicorp-tap");
    await Formula("libfixture", "1.0");
    await Formula("libfixture", "2.0", tap: "secondary-tap");
    var libraryPath = Path.Combine(repo, "tap", "homebrew-core", "Formula", "lib", "libfixture.rb");
    Equal(new Paths().Formula("libfixture"), libraryPath, "lib formula canonical shard");
    Equal(new FormulaRepository(new Paths()).Find("libfixture"), libraryPath, "core lib shard takes precedence over another tap");
    Equal((await Run("install", "libfixture")).Code, 0, "install lib-sharded formula");
    Equal((await Run("uninstall", "libfixture")).Code, 0, "uninstall lib-sharded formula");
    Equal((await Run("--version")).Output, "Homebrew 7.0.9\n", "version");
    Equal((await Run("--help")).Output, Normalize(CommandHelp.General) + "\n", "generic Homebrew help");
    Equal((await Run()).Code, 1, "no-arguments exit status");
    Equal((await Run("search", "probe")).Output, "probe\n", "search");
    Equal((await Run("desc", "probe")).Output, "probe: probe fixture\n", "description");
    Equal((await Run("deps", "probe")).Output, "leaf\n", "dependencies");
    Equal((await Run("uses", "leaf")).Output, "probe\n", "reverse dependencies");
    using (var info = JsonDocument.Parse((await Run("info", "--json=v2", "probe")).Output))
        Equal(info.RootElement.GetProperty("formulae")[0].GetProperty("name").GetString(), "probe", "info JSON");
    Equal((await Run("tap")).Output, "hashicorp/tap\nhomebrew/core\nsecondary/tap\n", "tap listing");
    Equal((await Run("info", "hashicorp/tap/terraform")).Code, 0, "fully qualified third-party formula");
    await Formula("isolated", "1.0", kegOnly: true);
    Equal((await Run("install", "isolated")).Code, 0, "keg-only windows_exe installation");
    Equal(File.Exists(Path.Combine(prefix, "bin", "isolated.exe")), false, "keg-only executable stays out of global bin");
    var kegBin = Path.Combine(prefix, "opt", "isolated", "bin", "isolated.exe");
    Equal(File.Exists(kegBin), true, "keg-only executable exists through opt prefix");
    Equal((await ProcessRunner.Capture(kegBin, ["cmd.exe"])).Code, 0, "keg-only executable runs");
    var isolatedRemoval = await Run("uninstall", "isolated");
    Equal(isolatedRemoval.Code, 0, "keg-only uninstall: " + isolatedRemoval.Error);
    Equal((await Run("install", "probe")).Code, 0, "install");
    await Formula("rival", "1.0", conflict: "probe");
    var conflict = await Run("install", "rival");
    Equal(conflict.Code, 1, "declared conflict prevents installation");
    Equal(conflict.Error.StartsWith("Error: Cannot install rival because conflicting formulae are installed.\n  probe: because fixture conflict\n"), true, "Homebrew conflict wording and reason");
    Equal(Directory.Exists(Path.Combine(prefix, "Cellar", "rival")), false, "conflict creates no keg");
    Equal(conflict.Output, "", "conflict checked before download");
    Equal((await Run("install", "--force", "rival")).Code, 0, "force bypasses declared conflict");
    Equal((await Run("uninstall", "rival")).Code, 0, "remove forced conflicting fixture");
    Equal((await Run("unlink", "probe")).Code, 0, "unlink conflicting formula");
    Equal((await Run("install", "rival")).Code, 0, "unlinked formula permits conflicting installation");
    Equal((await Run("uninstall", "rival")).Code, 0, "remove conflicting fixture");
    Equal((await Run("link", "probe")).Code, 0, "restore original formula links");
    Equal((await Run("list", "--installed-as-dependency")).Output, "leaf\n", "dependency receipt");
    Equal((await Run("list", "--installed-on-request")).Output, "probe\n", "requested receipt");
    Equal((await Run("leaves")).Output, "probe\n", "leaves");
    Equal((await Run("uninstall", "leaf")).Code, 1, "installed dependent prevents removal");
    Equal((await Run("pin", "probe")).Code, 0, "pin");
    Equal((await Run("list", "--pinned")).Output, "probe\n", "pinned listing");
    Equal((await Run("pin", "probe")).Error, "Warning: probe already pinned\n", "Homebrew already-pinned warning");
    Equal((await Run("reinstall", "probe")).Code, 1, "pinned reinstall refusal");
    Equal((await Run("unpin", "probe")).Code, 0, "unpin");
    Equal((await Run("unpin", "probe")).Error, "Warning: probe not pinned\n", "Homebrew not-pinned warning");
    Equal((await Run("unlink", "probe")).Code, 0, "unlink");
    Equal((await Run("link", "probe")).Code, 0, "relink");

    var original = new InstallationStore(new Paths()).Latest("probe");
    var receipt = File.ReadAllText(Path.Combine(original.Path, "INSTALL_RECEIPT.json"));
    await Formula("probe", "1.0", dependency: "leaf", fail: true);
    Equal((await Run("reinstall", "probe")).Code, 1, "failed reinstall");
    Equal(File.ReadAllText(Path.Combine(original.Path, "INSTALL_RECEIPT.json")), receipt, "failed reinstall restores receipt");
    Equal(File.Exists(Path.Combine(prefix, "bin", "probe.cmd")), true, "failed reinstall restores links");
    await Formula("probe", "2.0", dependency: "leaf");
    Equal((await Run("outdated")).Output, "probe\n", "outdated");
    Equal((await Run("outdated", "probe")).Code, 1, "named outdated exit status");
    Equal((await Run("upgrade", "--dry-run")).Code, 0, "upgrade dry run");
    Equal(new InstallationStore(new Paths()).Latest("probe").Receipt.Version, "1.0", "upgrade dry run preserves keg");
    Equal((await Run("upgrade", "probe")).Code, 0, "upgrade");
    Equal(new InstallationStore(new Paths()).Latest("probe").Receipt.Version, "2.0", "upgraded version");
    Equal((await Run("outdated")).Output, "", "up-to-date output");
    Equal((await Run("reinstall", "probe")).Code, 0, "successful reinstall");
    Equal((await Run("fetch", "probe")).Code, 0, "fetch verified cache");
    Equal((await Run("--prefix", "probe")).Output.Trim(), Path.Combine(prefix, "opt", "probe"), "formula opt prefix");
    Equal(File.Exists(Path.Combine(prefix, "opt", "probe", "bin", "probe.cmd")), true, "opt symlink resolves installed files");
    Equal((await Run("home", "probe")).Output, "https://example.invalid/probe\n", "homepage without GUI");
    Equal((await Run("doctor")).Output, "Your system is ready to brew.\n", "doctor");
    Equal((await Run("shellenv", "pwsh")).Output.Contains("$env:HOMEBREW_PREFIX='" + prefix + "'"), true, "PowerShell environment");
    Equal((await Run("tap-new", "--no-git", "scratch/test")).Code, 0, "tap-new");
    Equal((await Run("untap", "scratch/test")).Code, 0, "untap managed fixture");
    Equal((await Run("pr-publish", "--repo=example/tap", "--dry-run", "123")).Output.Contains("pull_request=123"), true, "GitHub publish workflow dry run");
    Equal((await Run("dispatch-build-bottle", "--repo=example/tap", "--dry-run", "probe")).Output.Contains("runner=windows-latest"), true, "Windows bottle workflow dry run");
    Equal((await Run("commands")).Output.Contains("generate-formula-api\n"), true, "command discovery");
    Equal((await Run("uninstall", "probe")).Code, 0, "uninstall");
    Equal((await Run("autoremove", "--dry-run")).Code, 0, "autoremove dry run");
    Equal(Directory.Exists(Path.Combine(prefix, "Cellar", "leaf")), true, "autoremove dry run preserves dependency");
    Equal((await Run("autoremove")).Code, 0, "autoremove");
    Equal((await Run("list")).Output, "", "all fixtures removed");
    Equal((await Run("cleanup", "--prune=all", "--dry-run")).Code, 0, "cache cleanup dry run");
    Console.WriteLine($"PASS: {assertions} command assertions; no GUI or GitHub writes. Fixtures: {root}");
}
finally { foreach (var pair in saved) Environment.SetEnvironmentVariable(pair.Key, pair.Value); }

async Task Formula(string name, string version, string dependency = null, string tap = "homebrew-core", bool fail = false, string conflict = null, bool kegOnly = false)
{
    var path = Path.Combine(repo, "tap", tap, "Formula", name.StartsWith("lib", StringComparison.Ordinal) ? "lib" : name[..1], name + ".rb"); Directory.CreateDirectory(Path.GetDirectoryName(path));
    var zip = Path.Combine(root, name + "-" + version + ".zip");
    if (!File.Exists(zip))
    {
        using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
        {
        if (kegOnly) archive.CreateEntryFromFile(Path.Combine(Environment.SystemDirectory, "where.exe"), "package/payload.exe");
        using (var writer = new StreamWriter(archive.CreateEntry("package/" + name + ".cmd").Open())) writer.Write("@echo " + version + "\r\n");
        }
    }
    var sha = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(zip)));
    File.Copy(zip, Path.Combine(cache, sha + "--" + Path.GetFileName(zip)), overwrite: true);
    File.WriteAllText(path, $"class {char.ToUpperInvariant(name[0]) + name[1..]} < Formula\n  desc \"{name} fixture\"\n  homepage \"https://example.invalid/{name}\"\n  version \"{version}\"\n  url \"https://example.invalid/{Path.GetFileName(zip)}\"\n  sha256 \"{sha}\"\n  license \"MIT\"\n" +
        (dependency == null ? "" : $"  depends_on \"{dependency}\"\n") +
        (conflict == null ? "" : $"  conflicts_with \"{conflict}\", because: \"fixture conflict\"\n") +
        (kegOnly ? "  keg_only :versioned_formula\n" : "") +
        "  def install\n" + (kegOnly ? $"    install_tree\n    windows_exe \"libexec/payload.exe\", \"{name}\"\n" : $"    bin.install \"{name}.cmd\"\n") +
        (fail ? "    raise \"fixture failure\"\n" : "") + "  end\nend\n");
}
async Task<(int Code, string Output, string Error)> Run(params string[] arguments)
{
    var output = new StringWriter(); var error = new StringWriter(); var oldOut = Console.Out; var oldError = Console.Error;
    try { Console.SetOut(output); Console.SetError(error); var code = await Cli.RunAsync(arguments); return (code, Normalize(output.ToString()), Normalize(error.ToString())); }
    finally { Console.SetOut(oldOut); Console.SetError(oldError); }
}
void Equal<T>(T actual, T expected, string label)
{
    assertions++;
    if (!EqualityComparer<T>.Default.Equals(actual, expected)) throw new Exception($"FAIL {label}: expected {expected}; got {actual}");
}
static string Normalize(string text) => text.Replace("\r\n", "\n");
