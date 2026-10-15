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
        if (!changed) Console.WriteLine("Already up-to-date.");
    }

    static int FormulaCount(string directory) => Directory.GetFiles(directory, "*.rb", SearchOption.AllDirectories).Count(f => f.Split(Path.DirectorySeparatorChar).Contains("Formula"));
}
