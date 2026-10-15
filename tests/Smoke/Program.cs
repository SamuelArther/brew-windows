using System.Diagnostics;
using Brew;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../.test-work/smoke"));
if (Directory.Exists(root)) throw new Exception($"Remove previous test workspace before running: {root}");
Environment.SetEnvironmentVariable("BREW_PREFIX", root);
Environment.SetEnvironmentVariable("BREW_REPOSITORY", Path.GetFullPath(Path.Combine(root, "../..")));
var paths = new Paths();
var manager = new PackageManager(paths);
var download = await new Downloader(paths.Cache).FetchAsync(
    "https://github.com/jqlang/jq/releases/download/jq-1.8.2/jq-windows-amd64.exe",
    "a6fc67fedaf9128a3309a1e2ebb8b986aeccf70122ee46d2cb4849e423f0c627");
await manager.InstallAsync("jq");
var jq = Path.Combine(paths.Bin, "jq.exe");
Check(new FileInfo(jq).LinkTarget != null, "jq is a real symlink");
await RunJq(jq);
Check(await Cli.RunAsync(["list"]) == 0, "list");
Check(await Cli.RunAsync(["list", "jq"]) == 0, "list jq");
await manager.InstallAsync("jq");
manager.Link("jq", true);
Check(!File.Exists(jq), "unlink");
manager.Link("jq", false);
await RunJq(jq);
manager.Uninstall("jq", false);
Check(!File.Exists(jq) && !Directory.Exists(paths.Rack("jq")), "uninstall removes bin and rack");
Check(await Cli.RunAsync(["uninstall", "jq"]) == 1, "missing keg exit status");

// A collision must preserve the user's file and roll back the new keg.
File.WriteAllText(jq, "keep me");
await MustFail(() => manager.InstallAsync("jq"), "link conflict");
Check(File.ReadAllText(jq) == "keep me" && !Directory.Exists(paths.Rack("jq")), "conflict rollback");
File.Delete(jq);

// Corruption of a cached download must fail before creating a keg.
File.WriteAllText(download, "corrupted");
await MustFail(() => manager.InstallAsync("jq"), "SHA256 mismatch");
Check(!Directory.Exists(paths.Rack("jq")), "checksum failure creates no keg");
File.Delete(download);

// A real imported DLL beside the executable selects the native shim.
var shimKeg = Path.Combine(paths.Cellar, "fixture", "1");
Directory.CreateDirectory(Path.Combine(shimKeg, "bin"));
File.Copy(Path.Combine(AppContext.BaseDirectory, "fixture.exe"), Path.Combine(shimKeg, "bin", "fixture.exe"));
File.Copy(Path.Combine(AppContext.BaseDirectory, "fixture.dll"), Path.Combine(shimKeg, "bin", "fixture.dll"));
var linker = new Linker(paths);
var links = linker.Link(shimKeg);
Check(links.Count == 1 && links[0].Shim && new FileInfo(links[0].Path).LinkTarget == null, "DLL shim selection");
var result = await Run(links[0].Path, ["hello world", "embedded\"quote", @"C:\path with spaces\"]);
Check(result.Code == 37, "shim forwards exit status");
Check(result.Output.Contains("hello world") && result.Output.Contains("embedded\"quote")
    && result.Output.Contains(@"C:\path with spaces\") && result.Output.Contains("stdin works"), "shim forwards arguments/stdin/stdout");
linker.Unlink(links);
Check(!File.Exists(links[0].Path) && !File.Exists(links[0].Path + ".brew-shim"), "shim unlink");
Console.WriteLine("PASS: package lifecycle, SHA256, collision rollback, symlinks, native shim.");

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception("FAIL: " + message);
}
static async Task MustFail(Func<Task> action, string message)
{
    try { await action(); }
    catch (Exception ex) when (ex.Message.Contains(message, StringComparison.OrdinalIgnoreCase)
        || message == "link conflict" && ex.Message.Contains("already exists")) { return; }
    throw new Exception("Expected failure: " + message);
}
static async Task RunJq(string path)
{
    var result = await Run(path, ["-n", "--arg", "value", "hello world", "{value: $value}"]);
    Check(result.Code == 0 && result.Output.Contains("hello world"), "jq executes and receives quoted arguments");
}
static async Task<(int Code, string Output)> Run(string path, string[] args)
{
    var info = new ProcessStartInfo(path) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardInput = true, CreateNoWindow = true };
    foreach (var arg in args) info.ArgumentList.Add(arg);
    using var process = Process.Start(info);
    await process.StandardInput.WriteLineAsync("stdin works");
    process.StandardInput.Close();
    var output = await process.StandardOutput.ReadToEndAsync();
    await process.WaitForExitAsync();
    return (process.ExitCode, output);
}
