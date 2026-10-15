using System.Diagnostics;
using System.IO.Compression;
using Brew;
using Brew.Formulae;

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
var receiptPath = Path.Combine(paths.Rack("jq"), "1.8.2", "INSTALL_RECEIPT.json");
var beforeUnlink = File.ReadAllText(receiptPath);
using (var locked = new FileStream(receiptPath, FileMode.Open, FileAccess.Read, FileShare.Read))
    MustFailIo(() => manager.Link("jq", true));
Check(File.ReadAllText(receiptPath) == beforeUnlink && File.Exists(jq), "locked receipt leaves linked package intact");
manager.Link("jq", true);
Check(!File.Exists(jq), "unlink");
var errors = new StringWriter();
var previousError = Console.Error;
try
{
    Console.SetError(errors);
    await manager.InstallAsync("jq");
}
finally { Console.SetError(previousError); }
Check(errors.ToString() == "Warning: jq 1.8.2 is already installed, it's just not linked.\nTo link this version, run:\n  brew link jq" + Environment.NewLine,
    "Homebrew warning for an unlinked installation");
Check(!File.Exists(jq), "install does not silently relink");

// A locked receipt makes atomic replacement fail after link creation.
// Relinking must undo its links, preserve the old receipt, and remove its temp file.
var unlinkedReceipt = File.ReadAllText(receiptPath);
using (var locked = new FileStream(receiptPath, FileMode.Open, FileAccess.Read, FileShare.Read))
    MustFailIo(() => manager.Link("jq", false));
Check(!File.Exists(jq), "relink failure rolls back new links");
Check(File.ReadAllText(receiptPath) == unlinkedReceipt, "failed replacement preserves receipt bytes");
Check(!Directory.EnumerateFiles(Path.GetDirectoryName(receiptPath), "*.tmp").Any(), "receipt temporary cleanup");
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

// A formula error after copying files must roll back its keg and build directory.
Brew.Ruby.FormulaLoader.InstallOverride = context =>
{
    context.InstallBin("jq-windows-amd64.exe", "jq.exe");
    throw new InvalidOperationException("fixture install failure");
};
try { await MustFail(() => manager.InstallAsync("jq"), "fixture install failure"); }
finally { Brew.Ruby.FormulaLoader.InstallOverride = null; }
Check(!Directory.Exists(paths.Rack("jq")) && !File.Exists(jq), "formula error rollback");
Check(!Directory.EnumerateFileSystemEntries(Path.Combine(root, "var", "homebrew", "build")).Any(), "build directory cleanup");

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

// Unlink must preserve files or links which a user has replaced after installation.
links = linker.Link(shimKeg);
File.WriteAllText(links[0].Path, "user replacement");
linker.Unlink(links);
Check(File.ReadAllText(links[0].Path) == "user replacement", "preserve replaced shim executable");
File.Delete(links[0].Path);
File.Delete(links[0].Path + ".brew-shim");
File.Delete(Path.Combine(shimKeg, "bin", "fixture.dll"));
links = linker.Link(shimKeg);
File.Delete(links[0].Path);
File.WriteAllText(links[0].Path, "user replacement");
linker.Unlink(links);
Check(File.ReadAllText(links[0].Path) == "user replacement", "preserve replaced symlink");
File.Delete(links[0].Path);

// Fixed launcher arguments must preserve quotes/backslashes before user arguments.
File.Copy(Path.Combine(AppContext.BaseDirectory, "fixture.dll"), Path.Combine(shimKeg, "bin", "fixture.dll"));
links = linker.Link(shimKeg, [new CommandSpec("prefixed.exe", Path.Combine(shimKeg, "bin", "fixture.exe"), ["fixed with spaces", "fixed\"quote", @"trailing\"])]);
result = await Run(Path.Combine(paths.Bin, "prefixed.exe"), ["user argument"]);
Check(result.Code == 37 && result.Output.Contains("fixed with spaces") && result.Output.Contains("fixed\"quote")
    && result.Output.Contains(@"trailing\") && result.Output.Contains("user argument"), "fixed launcher argument quoting");
linker.Unlink(links);

// Reject archive traversal, and expose an archive's single enclosing directory as buildpath.
var zip = Path.Combine(root, "normal.zip");
using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
using (var writer = new StreamWriter(archive.CreateEntry("package/payload.txt").Open())) writer.Write("payload");
var staged = await Archive.StageAsync(zip, "normal.zip", Path.Combine(root, "archive-stage"));
Check(File.ReadAllText(Path.Combine(staged, "payload.txt")) == "payload", "archive enclosing directory");
var malicious = Path.Combine(root, "malicious.zip");
using (var archive = ZipFile.Open(malicious, ZipArchiveMode.Create))
using (var writer = new StreamWriter(archive.CreateEntry("../escaped.txt").Open())) writer.Write("bad");
try { await Archive.StageAsync(malicious, "malicious.zip", Path.Combine(root, "unsafe-stage")); throw new Exception("Traversal accepted."); }
catch (IOException) { }
Check(!File.Exists(Path.Combine(root, "escaped.txt")), "archive traversal rejected");
Console.WriteLine("PASS: lifecycle, SHA256, rollback, atomic receipts, launchers, archives, symlinks and native shim.");

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
static void MustFailIo(Action action)
{
    try { action(); }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return; }
    throw new Exception("Expected filesystem failure.");
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
