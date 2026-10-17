using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Brew;

if (args.FirstOrDefault() == "--download-worker") { await ConcurrentDownloads.Worker(args); return; }
if (args.FirstOrDefault() == "--child") { ProcessApi.Child(args[1..]); return; }
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
if (args.FirstOrDefault() == "--process-api") { Console.WriteLine($"PASS: {ProcessApi.Run(Path.Combine(root, "process-api"))} process API tests"); return; }
try
{
    assertions += await DownloadTests.Run(root);
    assertions += await ConcurrentDownloads.Run(root);
    assertions += LinkConflicts.Run(root);
    assertions += TreeLinks.Run(root);
    assertions += PosixLines.Run(root);
    assertions += RubyLibrary.Run(root);
    assertions += ProcessApi.Run(Path.Combine(root, "process-api"));
    assertions += RubyStdLib.Run(root);
    assertions += FormulaDsl.Run(root);
    assertions += GoRustFormulae.Run(root);
    assertions += Patches.Run(root);
    assertions += await BottleRelocation.Run(root);
    assertions += await TapUpdate.Run(root);
    await Formula("resources", "1.0", resourceNames: ["mingw-w64-ucrt-x86_64-SDL2", "PyYAML", "MarkupSafe"]);
    Equal((await Run("install", "resources")).Code, 0, "uppercase resource names install end to end");
    Equal((await Run("uninstall", "resources")).Code, 0, "uppercase resource fixture uninstalls");
    // Resource#download_name escapes '/' (swift-nav/cmake); Perl module names (Crypt::Twofish) and other characters
    // Windows file names can't hold are escaped too, so each still stages in one folder of its own.
    await Formula("resources", "1.0", resourceNames: ["Crypt::Twofish", "swift-nav/cmake", "a\\b", "C:escape"]);
    Equal((await Run("install", "resources")).Code, 0, "Perl- and path-style resource names install");
    Equal(Directory.Exists(Path.Combine(prefix, "Cellar", "resources")), true, "escaped resource names make the keg");
    Equal((await Run("uninstall", "resources")).Code, 0, "escaped resource fixture uninstalls");
    foreach (var invalid in new[] { "../escape", "..", ".", "", "NUL", "trailing." })
    {
        await Formula("resources", "1.0", resourceNames: [invalid]);
        var rejected = await Run("install", "resources");
        Equal(rejected.Code, 1, "unsafe resource fails: " + invalid);
        Equal(rejected.Error.Contains("Invalid resource name:"), true, "resource diagnostic: " + invalid);
        Equal(Directory.Exists(Path.Combine(prefix, "Cellar", "resources")), false, "unsafe resource creates no keg");
    }
    await Formula("leaf", "1.0"); await Formula("probe", "1.0", dependency: "leaf");
    await Formula("terraform", "1.0", tap: "hashicorp-tap");
    await Formula("libfixture", "1.0");
    await Formula("libfixture", "2.0", tap: "secondary-tap");
    var aliasDir = Path.Combine(repo, "tap", "homebrew-core", "Aliases");
    Directory.CreateDirectory(aliasDir);
    File.CreateSymbolicLink(Path.Combine(aliasDir, "probe@1"), "../Formula/p/probe.rb");
    File.WriteAllText(Path.Combine(aliasDir, "libfixture@1"), "../Formula/lib/libfixture.rb\n");
    var thirdPartyAliases = Path.Combine(repo, "tap", "hashicorp-tap", "Aliases");
    Directory.CreateDirectory(thirdPartyAliases);
    File.WriteAllText(Path.Combine(thirdPartyAliases, "terraform@1"), "../Formula/t/terraform.rb");
    Equal((await Run("info", "probe@1")).Code, 0, "symlink alias info");
    Equal((await Run("info", "hashicorp/tap/terraform@1")).Code, 0, "qualified third-party alias");
    var libraryPath = Path.Combine(repo, "tap", "homebrew-core", "Formula", "lib", "libfixture.rb");
    Equal(new Paths().Formula("libfixture"), libraryPath, "lib formula canonical shard");
    Equal(new FormulaRepository(new Paths()).Find("libfixture"), libraryPath, "core lib shard takes precedence over another tap");
    Equal((await Run("install", "libfixture@1")).Code, 0, "install text alias to lib-sharded formula");
    Equal((await Run("uninstal", "libfixture")).Code, 0, "uninstal command alias");
    Equal((await Run("--version")).Output, "Homebrew 7.0.9\n", "version");
    Equal((await Run("--help")).Output, Normalize(CommandHelp.General) + "\n", "generic Homebrew help");
    Equal((await Run()).Code, 1, "no-arguments exit status");
    var unknown = await Run("frobnicate");
    Equal(unknown.Code, 1, "unknown command exit status");
    Equal(unknown.Error, Normalize(CommandHelp.General) + "\n\nError: Invalid usage: Unknown command: brew frobnicate\n", "Homebrew unknown command help and error");
    Equal((await Run("doctro")).Error, Normalize(CommandHelp.General) + "\n\nError: Invalid usage: Unknown command: brew doctro\nDid you mean doctor?\n", "unknown command suggestion");
    var commandNames = (await Run("commands", "--quiet", "--include-aliases")).Output.Split('\n');
    foreach (var alias in new[] { "ls", "homepage", "-S", "up", "ln", "instal", "uninstal", "post_install", "rm", "remove", "abv", "dr", "--repo", "environment", "--config", "-v", "lc", "tc", "x" })
        Equal(commandNames.Contains(alias), true, "Homebrew alias table includes " + alias);
    var unknownFormula = await Run("install", "proeb");
    Equal(unknownFormula.Code, 1, "unknown install exits unsuccessfully");
    Equal(unknownFormula.Error, "Warning: No available formula or cask with the name \"proeb\". Did you mean probe?\n", "unknown install warning and suggestion");
    Equal(unknownFormula.Output, "==> Searching for similarly named formulae and casks...\n==> Formulae\nprobe\n\nTo install probe, run:\n  brew install probe\n", "unknown install search results");
    var noMatch = await Run("install", "zzzzzzzzzz");
    Equal(noMatch.Code, 1, "unknown install with no matches exits unsuccessfully");
    Equal(noMatch.Error, "Warning: No available formula or cask with the name \"zzzzzzzzzz\".\nError: No formulae or casks found for zzzzzzzzzz.\n", "unknown install with no matches diagnostics");
    Equal((await Run("search", "probe")).Output, "probe\n", "search");
    Equal((await Run("search", "PRO-be")).Output, "probe\n", "search matches Homebrew's simplified names");
    Equal((await Run("search", "--desc", "probe fix")).Output, "==> Formulae\nprobe: probe fixture\n", "search --desc prints descriptions");
    Equal((await Run("-v", "commands")).Output.Contains("install\n"), true, "leading -v is --verbose for the command that follows");
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
    var dependencyInstall = await Run("instal", "probe@1");
    Equal(dependencyInstall.Code, 0, "instal through symlink alias");
    Equal(dependencyInstall.Output.Contains("==> Installing probe dependency: leaf (1.0)\n"), true, "Homebrew dependency installation header");
    Equal((await Run("missing")).Output, "", "satisfied dependencies produce no missing output");
    var hidden = await Run("missing", "--hide=leaf");
    Equal(hidden.Code, 1, "missing hidden dependency exit status");
    Equal(hidden.Output, "probe: leaf\n", "missing prefixes names when checking multiple kegs");
    Equal((await Run("missing", "probe", "--hide=leaf")).Output, "leaf\n", "missing omits name when checking one keg");
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
    await Formula("branch", "1.0", dependency: "leaf");
    await Formula("diamond", "1.0", dependencies: ["branch", "probe"]);
    Equal((await Run("uses", "branch", "probe")).Output, "diamond\n", "uses intersects requested dependencies");
    Equal((await Run("uses", "--recursive", "leaf", "probe")).Output, "diamond\n", "recursive uses intersects transitive dependencies");
    Equal((await Run("deps", "--tree", "diamond")).Output,
        "diamond\n├── branch\n│   └── leaf\n└── probe\n    └── leaf\n\n", "Homebrew dependency tree branches and repeated subtrees");
    Equal((await Run("deps", "--tree", "--prune", "diamond")).Output,
        "diamond\n├── branch\n│   └── leaf\n└── probe\n    └── leaf (PRUNED)\n\n", "Homebrew pruned dependency tree");
    await Formula("cyclea", "1.0", dependency: "cycleb");
    await Formula("cycleb", "1.0", dependency: "cyclea");
    var cycle = await Run("deps", "--tree", "cyclea");
    Equal(cycle.Code, 1, "circular dependency tree exit status");
    Equal(cycle.Output, "cyclea\n└── cycleb\n    └── cyclea (CIRCULAR DEPENDENCY)\n\n", "Homebrew circular dependency marker");
    var multipleDependencies = await Run("install", "diamond");
    Equal(multipleDependencies.Code, 0, "install multiple dependency fixture");
    Equal(multipleDependencies.Output.StartsWith("==> Installing dependencies for diamond: leaf, branch and probe\n"), true, "Homebrew multiple dependency summary (recursive, deepest first)");
    Equal((await Run("uninstall", "--ignore-dependencies", "leaf")).Code, 0, "remove dependency for missing check");
    Equal((await Run("missing", "diamond")).Output, "leaf\n", "missing detects transitive absent dependencies");
    assertions += await LifecycleRegression.Run(root);
    assertions += await KegLifecycle.Run(root);
    Console.WriteLine($"PASS: {assertions} command assertions; no GUI or GitHub writes. Fixtures: {root}");
}
finally { foreach (var pair in saved) Environment.SetEnvironmentVariable(pair.Key, pair.Value); }

async Task Formula(string name, string version, string dependency = null, string tap = "homebrew-core", bool fail = false, string conflict = null, bool kegOnly = false, string[] dependencies = null, string[] resourceNames = null)
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
        string.Concat((dependencies ?? []).Select(d => $"  depends_on \"{d}\"\n")) +
        (conflict == null ? "" : $"  conflicts_with \"{conflict}\", because: \"fixture conflict\"\n") +
        (kegOnly ? "  keg_only :versioned_formula\n" : "") +
        string.Concat((resourceNames ?? []).Select(r => $"  resource {JsonSerializer.Serialize(r)} do\n    url \"https://example.invalid/{Path.GetFileName(zip)}\"\n    sha256 \"{sha}\"\n  end\n")) +
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
