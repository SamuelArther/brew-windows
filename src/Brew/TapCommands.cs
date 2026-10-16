using System.Text.Json;

namespace Brew;

public sealed class TapCommands(Paths paths)
{
    public string[] Names => Directory.Exists(paths.Taps) ? Directory.GetDirectories(paths.Taps).Select(Name).Order(StringComparer.Ordinal).ToArray() : [];
    public string DirectoryFor(string name) => Path.Combine(paths.Taps, FormulaRepository.TapName(name));
    string Name(string directory)
    {
        var metadata = Path.Combine(directory, ".brew-tap.json");
        if (File.Exists(metadata)) return JsonDocument.Parse(File.ReadAllText(metadata)).RootElement.GetProperty("name").GetString();
        var name = Path.GetFileName(directory); var separator = name.IndexOf('-');
        return separator < 0 ? name : name[..separator] + "/" + name[(separator + 1)..];
    }

    public async Task<int> Tap(CommandArguments args)
    {
        if (args.Names.Count == 0) { foreach (var installedName in Names) Console.WriteLine(installedName); return 0; }
        if (args.Names.Count > 2) throw new InvalidOperationException("Invalid usage: brew tap accepts at most two arguments.");
        var name = args.Names[0].ToLowerInvariant(); var directory = DirectoryFor(name);
        if (Directory.Exists(directory)) { CommandOutput.Warning($"Tap {name} already tapped."); return 0; }
        var components = name.Split('/');
        var remote = args.Names.Count == 2 ? args.Names[1] : $"https://github.com/{components[0]}/homebrew-{components[1]}.git";
        Directory.CreateDirectory(paths.Taps);
        CommandOutput.Header($"Tapping {name}");
        var status = await ProcessRunner.Run("git", ["clone", "--", remote, directory]);
        if (status != 0) return status;
        File.WriteAllText(Path.Combine(directory, ".brew-tap.json"), JsonSerializer.Serialize(new { name, remote }));
        Console.WriteLine($"Tapped {FormulaCount(directory)} formulae ({directory}).");
        return 0;
    }

    public void Untap(CommandArguments args)
    {
        args.RequireNames("untap"); var store = new InstallationStore(paths);
        foreach (var name in args.Names)
        {
            var directory = DirectoryFor(name);
            if (!Directory.Exists(directory)) throw new InvalidOperationException($"No available tap {name}.");
            var dependents = store.All.Where(k => k.Receipt.SourcePath != null && Path.GetFullPath(k.Receipt.SourcePath)
                .StartsWith(Path.GetFullPath(directory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)).Select(k => k.Receipt.Name).Distinct().ToArray();
            if (dependents.Length > 0 && !args.Has("--force", "-f"))
                throw new InvalidOperationException($"Refusing to untap {name} because it contains the following installed formulae:\n{string.Join("\n", dependents)}");
            var count = FormulaCount(directory);
            CommandOutput.Header($"Untapping {name}");
            // DirectoryFor validates the owner/name and cannot escape this repository's tap root.
            Directory.Delete(directory, recursive: true);
            Console.WriteLine($"Untapped {count} formulae ({directory}).");
        }
    }

    public async Task Info(CommandArguments args)
    {
        var names = args.Names.Count == 0 ? Names : args.Names.ToArray();
        var json = new List<object>();
        foreach (var name in names)
        {
            var directory = DirectoryFor(name); bool installed = Directory.Exists(directory);
            if (!installed) throw new InvalidOperationException($"No available tap {name}.");
            var remote = await ProcessRunner.Capture("git", ["-C", directory, "remote", "get-url", "origin"]);
            var formulae = Directory.GetFiles(directory, "*.rb", SearchOption.AllDirectories).Where(f => f.Split(Path.DirectorySeparatorChar).Contains("Formula")).ToArray();
            json.Add(new { name, installed, path = directory, remote = remote.Code == 0 ? remote.Output.Trim() : null,
                official = name.StartsWith("homebrew/"), formula_names = formulae.Select(Path.GetFileNameWithoutExtension).Order(StringComparer.Ordinal).ToArray() });
            if (!args.Has("--json"))
            {
                if (json.Count > 1) Console.WriteLine();
                Console.WriteLine($"{name}: {formulae.Length} formulae"); Console.WriteLine(directory);
                if (remote.Code == 0) Console.WriteLine("From: " + remote.Output.Trim());
            }
        }
        if (args.Has("--json")) Console.WriteLine(JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true }));
    }

    public async Task Update()
    {
        CommandOutput.Header("Updating Homebrew...");
        bool changed = false;
        foreach (var directory in new[] { paths.Repository }.Concat(Directory.Exists(paths.Taps) ? Directory.GetDirectories(paths.Taps) : []))
        {
            if (!Directory.Exists(Path.Combine(directory, ".git")) && !File.Exists(Path.Combine(directory, ".git"))) continue;
            var remote = await ProcessRunner.Capture("git", ["-C", directory, "remote", "get-url", "origin"]);
            if (remote.Code != 0) { CommandOutput.Warning($"No remote 'origin' in {directory}, skipping update!"); continue; }
            var before = await ProcessRunner.Git(directory, "rev-parse", "HEAD");
            if (!string.IsNullOrEmpty(await ProcessRunner.Git(directory, "status", "--porcelain")))
                throw new InvalidOperationException($"Cannot update {directory}: your working tree has uncommitted changes.");
            await ProcessRunner.Git(directory, "fetch", "origin");
            var upstream = await ProcessRunner.Git(directory, "rev-parse", "--abbrev-ref", "@{upstream}");
            await ProcessRunner.Git(directory, "merge", "--ff-only", upstream);
            changed |= before != await ProcessRunner.Git(directory, "rev-parse", "HEAD");
        }
        changed |= await UpdateBrewExe();
        if (!changed) Console.WriteLine("Already up-to-date.");
    }

    // brew.exe isn't in git: an installed brew (<repository>\bin\brew.exe, from install.ps1) replaces
    // itself with the newest brew-windows-<version> release of the repository it was cloned from.
    async Task<bool> UpdateBrewExe()
    {
        var exe = Environment.ProcessPath;
        if (exe == null || Environment.GetEnvironmentVariable("BREW_NO_SELF_UPDATE") is { Length: > 0 }
            || !string.Equals(Path.GetDirectoryName(exe), Path.Combine(paths.Repository, "bin"), StringComparison.OrdinalIgnoreCase))
            return false;
        var remote = await ProcessRunner.Capture("git", ["-C", paths.Repository, "remote", "get-url", "origin"]);
        var match = System.Text.RegularExpressions.Regex.Match(remote.Output ?? "", @"github\.com[/:]([^/\s]+)/([^/\s]+?)(?:\.git)?\s*$");
        if (remote.Code != 0 || !match.Success) return false;
        var current = typeof(TapCommands).Assembly.GetName().Version;
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("brew-windows");
            var releases = JsonDocument.Parse(await http.GetStringAsync(
                $"https://api.github.com/repos/{match.Groups[1].Value}/{match.Groups[2].Value}/releases?per_page=30")).RootElement;
            foreach (var release in releases.EnumerateArray())
            {
                var tag = release.GetProperty("tag_name").GetString() ?? "";
                if (!tag.StartsWith("brew-windows-") || release.GetProperty("prerelease").GetBoolean()) continue;
                if (!Version.TryParse(tag["brew-windows-".Length..], out var latest) || current == null
                    || latest.CompareTo(new Version(current.Major, current.Minor, Math.Max(current.Build, 0))) <= 0)
                    return false;
                var asset = release.GetProperty("assets").EnumerateArray().FirstOrDefault(a => a.GetProperty("name").GetString()?.EndsWith(".zip") == true);
                if (asset.ValueKind != JsonValueKind.Object) return false;
                var zip = Path.Combine(Path.GetTempPath(), $"brew-windows-{Guid.NewGuid():N}.zip");
                await File.WriteAllBytesAsync(zip, await http.GetByteArrayAsync(asset.GetProperty("browser_download_url").GetString()));
                // GitHub publishes each asset's SHA-256 as its digest; refuse a download that doesn't match.
                if (asset.TryGetProperty("digest", out var digest) && digest.GetString() is { } expected && expected.StartsWith("sha256:"))
                {
                    var actual = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(zip))).ToLowerInvariant();
                    if (actual != expected["sha256:".Length..]) { File.Delete(zip); throw new InvalidOperationException($"{tag}: checksum mismatch"); }
                }
                var staging = Path.Combine(Path.GetTempPath(), $"brew-windows-{Guid.NewGuid():N}");
                System.IO.Compression.ZipFile.ExtractToDirectory(zip, staging);
                File.Delete(zip);
                // A running exe can be renamed but not overwritten; the old one is removed next time.
                var old = exe + ".old";
                if (File.Exists(old)) File.Delete(old);
                File.Move(exe, old);
                foreach (var file in Directory.GetFiles(staging)) File.Copy(file, Path.Combine(Path.GetDirectoryName(exe)!, Path.GetFileName(file)), true);
                PackageManager.DeleteTree(staging);
                CommandOutput.Header($"Updated brew-windows from {current.ToString(3)} to {latest}.");
                return true;
            }
        }
        catch (Exception e) when (e is HttpRequestException or IOException or JsonException or TaskCanceledException or KeyNotFoundException)
        {
            CommandOutput.Warning($"Could not check for a newer brew-windows: {e.Message}");
        }
        return false;
    }

    static int FormulaCount(string directory) => Directory.GetFiles(directory, "*.rb", SearchOption.AllDirectories).Count(f => f.Split(Path.DirectorySeparatorChar).Contains("Formula"));
}
