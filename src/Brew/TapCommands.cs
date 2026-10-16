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
        var directory = DirectoryFor(args.Names[0]);
        // Homebrew Tap.fetch: `user/homebrew-repo` and `user/repo` are the same tap, user/repo.
        var components = args.Names[0].ToLowerInvariant().Split('/');
        components[1] = System.Text.RegularExpressions.Regex.Replace(components[1], @"\Ahomebrew-", "");
        var name = components[0] + "/" + components[1];
        // Homebrew's `brew tap` is silent about a tap that's already there.
        if (Directory.Exists(directory)) return 0;
        var remote = args.Names.Count == 2 ? args.Names[1] : $"https://github.com/{components[0]}/homebrew-{components[1]}.git";
        Directory.CreateDirectory(paths.Taps);
        CommandOutput.Header($"Tapping {name}", error: true);
        var status = await ProcessRunner.Run("git", ["clone", "--", remote, directory]);
        if (status != 0) return status;
        File.WriteAllText(Path.Combine(directory, ".brew-tap.json"), JsonSerializer.Serialize(new { name, remote }));
        Console.Error.WriteLine($"Tapped{Contents(directory)} ({PackageManager.Abbreviation(directory)}).");
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
            var contents = Contents(directory); var size = PackageManager.Abbreviation(directory);
            Console.Error.WriteLine($"Untapping {name}...");
            // DirectoryFor validates the owner/name and cannot escape this repository's tap root.
            // (A git clone's .git/objects files are read-only, which a plain Directory.Delete refuses.)
            PackageManager.DeleteTree(directory);
            Console.Error.WriteLine($"Untapped{contents} ({size}).");
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

    public async Task<int> Update()
    {
        CommandOutput.Header("Updating Homebrew...");
        bool changed = false, failed = false;
        foreach (var directory in new[] { paths.Repository }.Concat(Directory.Exists(paths.Taps) ? Directory.GetDirectories(paths.Taps) : []))
        {
            if (!Directory.Exists(Path.Combine(directory, ".git")) && !File.Exists(Path.Combine(directory, ".git"))) continue;
            var remote = await ProcessRunner.Capture("git", ["-C", directory, "remote", "get-url", "origin"]);
            if (remote.Code != 0) { CommandOutput.Warning($"No remote 'origin' in {directory}, skipping update!"); continue; }
            // One broken tap mustn't stop the others (or brew.exe) from updating.
            try
            {
                var before = await ProcessRunner.Git(directory, "rev-parse", "HEAD");
                // Untracked files don't block a fast-forward: the repository holds its taps' clones
                // (tap/<user>-<repo>) and each tap holds its .brew-tap.json, none of them tracked.
                if (!string.IsNullOrEmpty(await ProcessRunner.Git(directory, "status", "--porcelain", "--untracked-files=no")))
                    throw new InvalidOperationException($"Cannot update {directory}: your working tree has uncommitted changes.");
                await ProcessRunner.Git(directory, "fetch", "origin");
                var upstream = await ProcessRunner.Git(directory, "rev-parse", "--abbrev-ref", "@{upstream}");
                await ProcessRunner.Git(directory, "merge", "--ff-only", upstream);
                changed |= before != await ProcessRunner.Git(directory, "rev-parse", "HEAD");
            }
            catch (InvalidOperationException e)
            {
                CommandOutput.Error(e.Message.StartsWith("Cannot update ") ? e.Message : $"Failed to update {directory}: {e.Message}");
                failed = true;
            }
        }
        changed |= await UpdateBrewExe();
        if (!changed && !failed) Console.WriteLine("Already up-to-date.");
        return failed ? 1 : 0;
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
            var api = $"https://api.github.com/repos/{match.Groups[1].Value}/{match.Groups[2].Value}";
            // The same repository publishes a release per bottle, so the newest releases are rarely
            // brew-windows ones: find them by tag instead, newest version first.
            var tags = JsonDocument.Parse(await http.GetStringAsync($"{api}/git/matching-refs/tags/brew-windows-")).RootElement
                .EnumerateArray().Select(r => r.GetProperty("ref").GetString()?.Replace("refs/tags/", "") ?? "")
                .Select(tag => (Tag: tag, Version: Version.TryParse(tag["brew-windows-".Length..], out var v) ? v : null))
                .Where(t => t.Version != null).OrderByDescending(t => t.Version).ToArray();
            foreach (var (tag, latest) in tags)
            {
                if (current == null || latest.CompareTo(new Version(current.Major, current.Minor, Math.Max(current.Build, 0))) <= 0) return false;
                using var response = await http.GetAsync($"{api}/releases/tags/{tag}");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound) continue;   // a tag without a release
                response.EnsureSuccessStatusCode();
                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var release = document.RootElement;
                if (release.GetProperty("prerelease").GetBoolean() || release.TryGetProperty("draft", out var draft) && draft.GetBoolean()) continue;
                var assets = release.GetProperty("assets").EnumerateArray().ToArray();
                var asset = assets.FirstOrDefault(a => a.GetProperty("name").GetString() == tag + ".zip");
                if (asset.ValueKind != JsonValueKind.Object) asset = assets.FirstOrDefault(a => a.GetProperty("name").GetString()?.EndsWith(".zip") == true);
                if (asset.ValueKind != JsonValueKind.Object) return false;
                // GitHub publishes each asset's SHA-256 as its digest; never install a download it can't check.
                if (!asset.TryGetProperty("digest", out var digest) || digest.ValueKind != JsonValueKind.String
                    || digest.GetString() is not { } expected || !expected.StartsWith("sha256:", StringComparison.Ordinal))
                {
                    CommandOutput.Warning($"Not updating brew-windows: {tag} has no published SHA-256 digest.");
                    return false;
                }
                var zip = Path.Combine(Path.GetTempPath(), $"brew-windows-{Guid.NewGuid():N}.zip");
                var staging = Path.Combine(Path.GetTempPath(), $"brew-windows-{Guid.NewGuid():N}");
                try
                {
                    await File.WriteAllBytesAsync(zip, await http.GetByteArrayAsync(asset.GetProperty("browser_download_url").GetString()));
                    var actual = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(zip)));
                    if (!actual.Equals(expected["sha256:".Length..], StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException($"{tag}: checksum mismatch");
                    System.IO.Compression.ZipFile.ExtractToDirectory(zip, staging);
                    ReplaceExe(exe, staging, tag);
                }
                finally
                {
                    try
                    {
                        if (File.Exists(zip)) File.Delete(zip);
                        if (Directory.Exists(staging)) PackageManager.DeleteTree(staging);
                    }
                    catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
                }
                CommandOutput.Header($"Updated brew-windows from {current.ToString(3)} to {latest}.");
                return true;
            }
        }
        catch (Exception e) when (e is HttpRequestException or IOException or UnauthorizedAccessException or System.IO.InvalidDataException
            or JsonException or TaskCanceledException or KeyNotFoundException)
        {
            CommandOutput.Warning($"Could not update brew-windows: {e.Message}");
        }
        return false;
    }

    // Installs an unpacked release over exe. The new exe is staged beside the old one (same volume)
    // and swapped in by renaming, since a running exe can be renamed but not overwritten; the old one
    // stays as <exe>.old (Program.cs removes it on the next run). Any failure leaves the old exe working.
    public static void ReplaceExe(string exe, string staging, string release)
    {
        var fresh = Path.Combine(staging, Path.GetFileName(exe));
        if (!File.Exists(fresh)) throw new InvalidOperationException($"{release}: the release has no {Path.GetFileName(exe)}");
        var incoming = exe + ".new"; var old = exe + ".old";
        try
        {
            File.Copy(fresh, incoming, true);
            if (File.Exists(old)) File.Delete(old);
            File.Move(exe, old);
            try { File.Move(incoming, exe); }
            catch { File.Move(old, exe); throw; }
        }
        finally { if (File.Exists(incoming)) File.Delete(incoming); }
        foreach (var file in Directory.GetFiles(staging).Where(f => f != fresh))
            File.Copy(file, Path.Combine(Path.GetDirectoryName(exe)!, Path.GetFileName(file)), true);
    }

    static int FormulaCount(string directory) => Directory.GetFiles(directory, "*.rb", SearchOption.AllDirectories).Count(f => f.Split(Path.DirectorySeparatorChar).Contains("Formula"));
    // Homebrew Tap#contents, as " 1 formula" / " 2 formulae" (empty for none).
    static string Contents(string directory) => FormulaCount(directory) is var count and > 0 ? $" {count} {(count == 1 ? "formula" : "formulae")}" : "";
}
