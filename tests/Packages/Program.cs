using System.Diagnostics;
using System.Text.Json;
using Brew;
using Brew.Ruby;

var repository = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../"));
Environment.SetEnvironmentVariable("BREW_REPOSITORY", repository);
var catalog = JsonDocument.Parse(File.ReadAllText(Path.Combine(repository, "tools", "windows-packages.json")));
foreach (var spec in catalog.RootElement.EnumerateArray())
{
    var name = spec.GetProperty("name").GetString();
    var formula = FormulaLoader.Load(Path.Combine(repository, "tap", "homebrew-core", "Formula", name[..1], name + ".rb"));
    Check(formula.Name == name && formula.Version == spec.GetProperty("version").GetString(), name + " metadata");
    Check(formula.Sha256 == spec.GetProperty("sha256").GetString(), name + " checksum");
    Check(formula.Resources.Count == spec.GetProperty("resources").GetArrayLength(), name + " resource metadata");
}
Console.WriteLine("PASS: all 22 formulae load through the real Ruby interpreter.");
if (args.Length == 0) return;

var prefix = Path.Combine(repository, ".test-work", "packages-" + Guid.NewGuid().ToString("N"));
Environment.SetEnvironmentVariable("BREW_PREFIX", prefix);
Environment.SetEnvironmentVariable("BREW_CACHE", Path.Combine(repository, ".test-work", "package-cache"));
var paths = new Paths();
var manager = new PackageManager(paths);
foreach (var name in args)
{
    await manager.InstallAsync(name);
    if (name == "firefox")
    {
        Check(File.Exists(Path.Combine(paths.Bin, "firefox.exe")), "Firefox payload linked; GUI deliberately not launched");
        Console.WriteLine("PASS: firefox payload and launcher; GUI not launched.");
    }
    else if (name is "readline" or "libyaml")
    {
        var keg = Directory.GetDirectories(paths.Rack(name)).Single();
        Check(Directory.EnumerateFiles(Path.Combine(keg, "include"), "*.h", SearchOption.AllDirectories).Any(), name + " development headers");
        var source = Path.Combine(prefix, name + "-check.c");
        File.WriteAllText(source, name == "libyaml"
            ? "#include <yaml.h>\nint main(void) { int a,b,c; yaml_get_version(&a,&b,&c); return a == 0 ? 0 : 1; }\n"
            : "#include <stdio.h>\n#include <readline/readline.h>\nint main(void) { return rl_readline_version > 0 ? 0 : 1; }\n");
        var outputExe = Path.Combine(prefix, name + "-check.exe");
        var compile = new ProcessStartInfo("clang") { UseShellExecute = false, CreateNoWindow = true };
        foreach (var arg in new[] { "-I" + Path.Combine(keg, "include"), source, "-L" + Path.Combine(keg, "lib"),
            name == "libyaml" ? "-lyaml" : "-lreadline", "-o", outputExe }) compile.ArgumentList.Add(arg);
        using (var compiler = Process.Start(compile)) { await compiler.WaitForExitAsync(); Check(compiler.ExitCode == 0, name + " compiles and links"); }
        var run = new ProcessStartInfo(outputExe) { UseShellExecute = false, CreateNoWindow = true };
        run.Environment["PATH"] = Path.Combine(keg, "bin") + ";" + Environment.GetEnvironmentVariable("PATH");
        using (var check = Process.Start(run)) { await check.WaitForExitAsync(); Check(check.ExitCode == 0, name + " DLL loads and runs"); }
        Console.WriteLine($"PASS: {name} headers, native linking, DLL load and execution.");
    }
    else
    {
        var (command, versionArgs) = name switch
        {
            "coreutils" => ("ls", new[] { "--version" }),
            "redis" => ("redis-server", new[] { "--version" }),
            "python3" => ("python3", new[] { "--version" }),
            "ffmpeg" => ("ffmpeg", new[] { "-version" }),
            "maven" => ("mvn", new[] { "--version" }),
            "awscli" => ("aws", new[] { "--version" }),
            _ => (name, new[] { "--version" })
        };
        Console.WriteLine($"PASS: {name}: {await VersionOutput(paths.Bin, command, versionArgs)}");
        foreach (var extra in name == "node" ? new[] { "npm", "npx" } : name == "python3" ? new[] { "pip3" } : Array.Empty<string>())
            Console.WriteLine($"PASS: {extra}: {await VersionOutput(paths.Bin, extra, ["--version"])}");
    }
    if (name == "yarn")
    {
        try { manager.Uninstall("node", false); throw new Exception("Node dependency was removed while Yarn is installed."); }
        catch (InvalidOperationException ex) when (ex.Message.Contains("required by yarn")) { }
    }
    manager.Uninstall(name, false);
    Check(!Directory.Exists(paths.Rack(name)), name + " removed from Cellar");
}
Console.WriteLine($"PASS: package install/version/uninstall. Prefix: {prefix}");

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception("FAIL: " + message);
}

static async Task<string> VersionOutput(string bin, string command, string[] versionArgs)
{
    var exe = Path.Combine(bin, command + ".exe");
    var batch = !File.Exists(exe);
    var start = new ProcessStartInfo(batch ? Environment.GetEnvironmentVariable("COMSPEC") : exe)
    { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
    if (batch)
    {
        start.ArgumentList.Add("/d"); start.ArgumentList.Add("/c");
        start.ArgumentList.Add(Path.Combine(bin, command + ".cmd"));
    }
    foreach (var arg in versionArgs) start.ArgumentList.Add(arg);
    start.Environment["HEROKU_DISABLE_AUTOUPDATE"] = "1";
    start.Environment["HEROKU_DISABLE_TELEMETRY"] = "1";
    start.Environment["NPM_CONFIG_UPDATE_NOTIFIER"] = "false";
    using var process = Process.Start(start);
    var output = process.StandardOutput.ReadToEndAsync();
    var error = process.StandardError.ReadToEndAsync();
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    try { await process.WaitForExitAsync(timeout.Token); }
    catch (OperationCanceledException) { process.Kill(entireProcessTree: true); throw; }
    var stdout = await output; var stderr = await error;
    Check(process.ExitCode == 0, $"{command} version command exited {process.ExitCode}: {stdout} {stderr}");
    return stdout.Split('\n')[0].Trim() + stderr.Split('\n')[0].Trim();
}
