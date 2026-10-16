using System.Text.Json;
using Brew.Formulae;

namespace Brew;

public sealed record Receipt(string Name, string Version, string Url, string Sha256, List<BinLink> Links,
    List<CommandSpec> Commands = null, List<string> Dependencies = null,
    bool InstalledOnRequest = true, long InstalledAt = 0, string SourcePath = null, bool PouredFromBottle = false);

public sealed class PackageManager(Paths paths)
{
    const string ReceiptName = "INSTALL_RECEIPT.json";
    readonly Linker linker = new(paths);
    readonly HashSet<string> installing = new(StringComparer.OrdinalIgnoreCase);
    readonly FormulaRepository repository = new(paths);
    readonly InstallationStore store = new(paths);
    readonly OptLinks opt = new(paths);

    public async Task InstallAsync(string name, bool requested = true, bool force = false, bool ignoreDependencies = false, bool? linkKeg = null, bool fromSource = false)
    {
        if (!installing.Add(name)) throw new InvalidOperationException($"Circular dependency: {name}");
        try { await InstallCoreAsync(name, requested, force, ignoreDependencies, linkKeg, fromSource); }
        finally { installing.Remove(name); }
    }

    async Task InstallCoreAsync(string name, bool requested, bool force, bool ignoreDependencies, bool? linkKeg, bool fromSource)
    {
        var file = repository.Find(name);
        var formula = repository.LoadFile(file);
        name = Paths.ValidateName(formula.Name);
        if (formula.Install == null)
            throw new InvalidOperationException("Invalid formula definition; see docs/FORMULA_API.md.");
        var deprecation = Metadata.Call(formula, "DeprecationMessage")?.ToString();
        if (Metadata.Bool(formula, "Disabled") && !force) throw new InvalidOperationException($"{name} has been {deprecation}");
        if (Metadata.Bool(formula, "Deprecated") || Metadata.Bool(formula, "Disabled")) CommandOutput.Warning($"{name} has been {deprecation}");
        if (string.IsNullOrEmpty(formula.Version) || formula.Version.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || formula.Version is "." or "..") throw new InvalidOperationException("Invalid formula version.");
        var rack = paths.Rack(name);
        var keg = Path.Combine(rack, Metadata.Version(formula));
        if (File.Exists(Path.Combine(keg, ReceiptName)))
        {
            var installed = JsonSerializer.Deserialize<Receipt>(File.ReadAllText(Path.Combine(keg, ReceiptName)));
            if (requested && !installed.InstalledOnRequest)
                ReceiptFile.Write(Path.Combine(keg, ReceiptName), installed with { InstalledOnRequest = true });
            var hasCommands = installed.Commands?.Count > 0 || Directory.Exists(Path.Combine(keg, "bin"))
                && Directory.EnumerateFiles(Path.Combine(keg, "bin")).Any(f => !Path.GetExtension(f).Equals(".dll", StringComparison.OrdinalIgnoreCase));
            if (installed.Links.Count == 0 && hasCommands && !Metadata.Bool(formula, "KegOnly"))
                CommandOutput.Warning($"{name} {Metadata.Version(formula)} is already installed, it's just not linked.\nTo link this version, run:\n  brew link {name}");
            else
                CommandOutput.Warning($"{name} {Metadata.Version(formula)} is already installed and up-to-date.\nTo reinstall {Metadata.Version(formula)}, run:\n  brew reinstall {name}");
            return;
        }
        if (Directory.Exists(keg)) throw new IOException($"Incomplete keg already exists: {keg}");
        if (!force && Metadata.Get(formula, "Conflicts") is System.Collections.IEnumerable declared)
        {
            var conflicts = declared.Cast<object>().Where(c => store.Kegs(Metadata.Text(c, "Name")).Any(k => k.Receipt.Links.Count > 0)).ToArray();
            if (conflicts.Length > 0)
            {
                var names = conflicts.Select(c => Metadata.Text(c, "Name")).ToArray();
                var lines = conflicts.Select(c => "  " + Metadata.Text(c, "Name") + (Metadata.Text(c, "Because", "Reason") is string reason ? ": because " + reason : ""));
                throw new InvalidOperationException($"Cannot install {name} because conflicting formulae are installed.\n{string.Join("\n", lines)}\n\nPlease `brew unlink {string.Join(" ", names)}` before continuing.\n\nUnlinking removes a formula's symlinks from {paths.Prefix}. You can\nlink the formula again after the install finishes. You can `--force` this\ninstall, but the build may fail or cause obscure side effects in the\nresulting software.\n");
            }
        }
        var runtimeDependencies = QueryCommands.Dependencies(formula).Where(d => d.Runtime).Select(d => d.Name).ToList();
        var pending = (ignoreDependencies ? [] : runtimeDependencies).Where(d => store.Latest(d) == null).ToArray();
        if (pending.Length > 1)
            CommandOutput.Header($"Installing dependencies for {name}:" + (CommandOutput.Color ? "\u001b[0m" : "")
                + " " + Suggestions.Sentence(pending.Select(CommandOutput.Identifier)), green: true);
        foreach (var dependency in pending)
        {
            var definition = repository.Load(dependency);
            CommandOutput.Header($"Installing {name} dependency: {CommandOutput.Identifier(dependency)} ({Metadata.Version(definition)})", green: true);
            await InstallAsync(dependency, requested: false);
        }
        var bottle = fromSource || Environment.GetEnvironmentVariable("HOMEBREW_BUILD_FROM_SOURCE") == "1" ? null : Bottles.For(formula);
        var buildDependencies = QueryCommands.Dependencies(formula).Where(d => d.Tags.Contains("build")).Select(d => d.Name).ToList();
        if (bottle == null && !ignoreDependencies)
            foreach (var dependency in buildDependencies.Where(d => store.Latest(d) == null))
            {
                var definition = repository.Load(dependency);
                CommandOutput.Header($"Installing {name} dependency: {CommandOutput.Identifier(dependency)} ({Metadata.Version(definition)})", green: true);
                await InstallAsync(dependency, requested: false);
            }
        var build = Path.Combine(paths.Prefix, "var", "homebrew", "build", name + "-" + Guid.NewGuid().ToString("N")[..8]); // short: deep source trees hit MAX_PATH
        Directory.CreateDirectory(build);
        var createdKeg = false;
        List<BinLink> links = [];
        try
        {
            InstallContext context;
            if (bottle != null)
            {
                var archive = await new Downloader(paths.Cache).FetchAsync(bottle.Url, bottle.Sha256);
                CommandOutput.Header($"Pouring {bottle.FileName}");
                createdKeg = true;
                await Bottles.PourAsync(paths, archive, keg, bottle.Cellar);
                var poured = JsonSerializer.Deserialize<Receipt>(File.ReadAllText(Path.Combine(keg, ReceiptName)));
                context = new InstallContext(keg, build, null, paths.Prefix) { PouredFromBottle = true };
                context.Commands.AddRange(poured.Commands ?? []);
            }
            else
            {
                var download = await new Downloader(paths.Cache).FetchAsync(formula.Url, formula.Sha256, Metadata.Strings(formula, "Mirrors"));
                var filename = Downloader.FileName(formula.Url);
                var stagedBuild = await Archive.StageAsync(download, filename, build);
                var resources = new Dictionary<string, string>();
                foreach (var resource in formula.Resources)
                {
                    Paths.ValidateResourceName(resource.Name);
                    var resourceFile = await new Downloader(paths.Cache).FetchAsync(resource.Url, resource.Sha256);
                    var resourcePath = Path.Combine(build, ".resources", resource.Name);
                    var stagedResource = await Archive.StageAsync(resourceFile, Downloader.FileName(resource.Url), resourcePath);
                    resources.Add(resource.Name, stagedResource);
                }
                await ApplyPatchesAsync(formula, stagedBuild, build, file);
                CommandOutput.Header($"Installing {name}");
                Directory.CreateDirectory(keg);
                createdKeg = true;
                context = new InstallContext(keg, stagedBuild, resources, paths.Prefix);
                BuildEnvironment.Apply(paths, context, runtimeDependencies.Concat(buildDependencies));
                formula.Install(context);
                if (Environment.GetEnvironmentVariable("BREW_DEDUPLICATE_MSYS2") == "1") SharedRuntime.Deduplicate(paths, keg);
                linker.MaterializeCommands(keg, context.Commands);
            }
            links = (linkKeg ?? !Metadata.Bool(formula, "KegOnly", "IsKegOnly")) ? linker.Link(keg, context.Commands) : [];
            opt.Set(name, keg);
            Metadata.Call(formula, "RunPostInstall", keg, paths.Prefix);
            ReceiptFile.Write(Path.Combine(keg, ReceiptName),
                new Receipt(name, Metadata.Version(formula), formula.Url, formula.Sha256, links, context.Commands, runtimeDependencies,
                    requested, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), file, bottle != null));
            var caveats = FormulaCaveats(formula, context);
            if (!string.IsNullOrWhiteSpace(caveats)) { CommandOutput.Header("Caveats"); Console.WriteLine(caveats.TrimEnd()); CommandOutput.Header("Summary"); }
            if (!CommandOutput.Quiet) Console.WriteLine($"{CommandOutput.Badge}{keg}: {Abbreviation(keg)}");
        }
        catch
        {
            linker.Unlink(links);
            opt.Remove(name, keg);
            if (createdKeg && Directory.Exists(keg)) Directory.Delete(keg, true);
            RemoveEmptyRack(rack);
            throw;
        }
        finally { if (Directory.Exists(build)) Directory.Delete(build, true); }
    }

    // Homebrew's Formula#patch: inline/DATA diffs, downloaded diffs, and `apply` files inside patch archives.
    async Task ApplyPatchesAsync(FormulaDefinition formula, string source, string build, string formulaFile)
    {
        if (formula.Patches.Count == 0) return;
        CommandOutput.Header("Patching");
        var archive = 0;
        foreach (var patch in formula.Patches)
        {
            var directory = string.IsNullOrEmpty(patch.Directory) ? source : Path.Combine(source, patch.Directory);
            if (patch.Data != null) { Patcher.Apply(patch.Data, patch.Strip, directory); continue; }
            if (patch.File != null)
            {
                Patcher.Apply(File.ReadAllText(Path.Combine(Path.GetDirectoryName(formulaFile)!, patch.File)), patch.Strip, directory);
                continue;
            }
            var download = await new Downloader(paths.Cache).FetchAsync(patch.Url, patch.Sha256);
            if (patch.Apply.Count == 0) { Patcher.Apply(File.ReadAllText(download), patch.Strip, directory); continue; }
            var staged = await Archive.StageAsync(download, Downloader.FileName(patch.Url), Path.Combine(build, ".patches", (archive++).ToString()));
            foreach (var diff in patch.Apply) Patcher.Apply(File.ReadAllText(Path.Combine(staged, diff)), patch.Strip, directory);
        }
    }

    public void List(string[] names, bool versions)
    {
        if (names.Length == 0)
        {
            if (!Directory.Exists(paths.Cellar)) return;
            names = Directory.GetDirectories(paths.Cellar).Select(Path.GetFileName)
                .Where(n => InstalledKegs(n).Length > 0).Order(StringComparer.Ordinal).ToArray();
            foreach (var name in names)
                Console.WriteLine(versions ? name + " " + string.Join(" ", InstalledKegs(name).Select(Path.GetFileName)) : name);
            return;
        }
        foreach (var name in names)
        {
            var kegs = RequireKegs(name);
            if (versions) Console.WriteLine(name + " " + string.Join(" ", kegs.Select(Path.GetFileName)));
            else foreach (var keg in kegs)
                foreach (var file in Directory.EnumerateFiles(keg, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
                    if (Path.GetFileName(file) != ReceiptName) Console.WriteLine(file);
        }
    }

    public void Uninstall(string name, bool force, bool ignoreDependencies = false)
    {
        name = store.ResolveName(name);
        var kegs = InstalledKegs(name);
        if (kegs.Length == 0 && !force) throw new InvalidOperationException($"No such keg: {paths.Rack(name)}");
        if (!force && store.IsPinned(name)) throw new InvalidOperationException($"{name} is pinned. You must unpin it to uninstall.");
        if (kegs.Length > 0 && !ignoreDependencies)
        {
            var dependents = Directory.EnumerateFiles(paths.Cellar, ReceiptName, SearchOption.AllDirectories)
                .Select(f => JsonSerializer.Deserialize<Receipt>(File.ReadAllText(f)))
                .Where(r => r.Name != name && r.Dependencies?.Any(d => Paths.FormulaName(d) == name) == true)
                .Select(r => r.Name).Distinct().Order(StringComparer.Ordinal).ToArray();
            if (dependents.Length > 0)
                throw new InvalidOperationException($"Refusing to uninstall {paths.Rack(name)}\nbecause it is required by {string.Join(", ", dependents)}, which {(dependents.Length == 1 ? "is" : "are")} currently installed.\nYou can override this and force removal with:\n  brew uninstall --ignore-dependencies {name}");
        }
        var removals = kegs.Select(keg => new InstalledKeg(keg,
            JsonSerializer.Deserialize<Receipt>(File.ReadAllText(Path.Combine(keg, ReceiptName)))
                ?? throw new IOException($"Invalid install receipt: {keg}"))).ToArray();
        // Check every keg before touching links or payloads: Windows can refuse deletion of open files.
        foreach (var removal in removals)
        {
            RemovalPreflight.Tree(removal.Path);
            linker.PreflightUnlink(removal.Receipt.Links);
            var optLink = opt.PathFor(name);
            if (new DirectoryInfo(optLink).LinkTarget is string target
                && string.Equals(Path.GetFullPath(target, Path.GetDirectoryName(optLink)), removal.Path, StringComparison.OrdinalIgnoreCase))
                RemovalPreflight.Check(optLink);
        }
        foreach (var removal in removals)
        {
            var keg = removal.Path; var receipt = removal.Receipt;
            Console.WriteLine($"Uninstalling {keg}... ({Abbreviation(keg)})");
            linker.Unlink(receipt.Links);
            opt.Remove(name, keg);
            Directory.Delete(keg, true);
        }
        RemoveEmptyRack(paths.Rack(name));
        var pin = Path.Combine(store.PinDirectory, name);
        if (File.Exists(pin)) File.Delete(pin);
    }

    public void Link(string name, bool unlink, bool force = false)
    {
        name = store.ResolveName(name);
        foreach (var keg in RequireKegs(name))
        {
            var receiptPath = Path.Combine(keg, ReceiptName);
            var receipt = JsonSerializer.Deserialize<Receipt>(File.ReadAllText(receiptPath));
            if (unlink)
            {
                // Refuse a locked/read-only receipt before removing any links.
                using (var writable = new FileStream(receiptPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read)) { }
                linker.Unlink(receipt.Links);
                ReceiptFile.Write(receiptPath, receipt with { Links = [] });
                Console.WriteLine($"Unlinking {keg}... {receipt.Links.Count} symlinks removed.");
            }
            else
            {
                if (receipt.Links.Count > 0) { Console.Error.WriteLine($"Warning: Already linked: {keg}"); continue; }
                var file = receipt.SourcePath;
                var formula = file != null && File.Exists(file) ? repository.LoadFile(file) : repository.Load(name);
                if (!force && Metadata.Bool(formula, "KegOnly")
                    && Metadata.Text(Metadata.Get(formula, "KegOnlyReason"), "Name") != "versioned_formula")
                {
                    CommandOutput.Warning($"{name} is keg-only and must be linked with `--force`.");
                    var bins = new[] { "bin", "sbin" }.Where(dir => Directory.Exists(Path.Combine(keg, dir))).ToArray();
                    if (bins.Length > 0)
                    {
                        Console.WriteLine("\nIf you need to have this software first in your PATH instead consider running:");
                        foreach (var dir in bins)
                            Console.WriteLine($"  $env:PATH = '{Path.Combine(opt.PathFor(name), dir).Replace("'", "''")};' + $env:PATH");
                    }
                    continue;
                }
                var links = linker.Link(keg, receipt.Commands);
                try { ReceiptFile.Write(receiptPath, receipt with { Links = links }); }
                catch { linker.Unlink(links); throw; }
                Console.WriteLine($"Linking {keg}... {links.Count} symlinks created.");
            }
        }
    }

    string[] RequireKegs(string name)
    {
        var kegs = InstalledKegs(name);
        if (kegs.Length == 0) throw new InvalidOperationException($"No such keg: {paths.Rack(name)}");
        return kegs;
    }

    public string[] InstalledKegs(string name) => store.Kegs(store.ResolveName(name)).Select(k => k.Path).ToArray();

    public async Task FetchAsync(string name)
    {
        var formula = repository.Load(name);
        await new Downloader(paths.Cache).FetchAsync(formula.Url, formula.Sha256, Metadata.Strings(formula, "Mirrors"));
        foreach (var resource in formula.Resources) await new Downloader(paths.Cache).FetchAsync(resource.Url, resource.Sha256);
    }

    public async Task ReinstallAsync(string name)
    {
        var formula = repository.Load(name); name = formula.Name;
        if (store.IsPinned(name)) throw new InvalidOperationException($"{name} is pinned. You must unpin it to reinstall.");
        var previous = store.Kegs(name);
        CommandOutput.Header($"Reinstalling {name}");
        await ReplaceAsync(name, previous);
    }

    public async Task UpgradeAsync(string name)
    {
        var formula = repository.Load(name); name = formula.Name;
        var previous = store.Kegs(name);
        if (previous.Length == 0) throw new InvalidOperationException($"{name} not installed");
        if (store.IsPinned(name)) { CommandOutput.Warning($"{name} is pinned. Skipping upgrade."); return; }
        if (VersionOrder.Instance.Compare(previous[^1].Receipt.Version, Metadata.Version(formula)) >= 0)
        { CommandOutput.Warning($"{name} {previous[^1].Receipt.Version} already installed"); return; }
        CommandOutput.Header($"Upgrading {name}\n  {previous[^1].Receipt.Version} -> {Metadata.Version(formula)}");
        await ReplaceAsync(name, previous);
    }

    async Task ReplaceAsync(string name, InstalledKeg[] previous)
    {
        var moved = new List<(InstalledKeg Keg, string Backup)>();
        try
        {
            foreach (var keg in previous)
            {
                linker.Unlink(keg.Receipt.Links);
                var backup = Path.Combine(paths.Rack(name), ".backup-" + Guid.NewGuid().ToString("N"));
                try { Directory.Move(keg.Path, backup); }
                catch { if (keg.Receipt.Links.Count > 0) InstallationStore.Save(keg, keg.Receipt with { Links = linker.Link(keg.Path, keg.Receipt.Commands) }); throw; }
                moved.Add((keg, backup));
            }
            await InstallAsync(name, previous.Length == 0 || previous.Any(k => k.Receipt.InstalledOnRequest),
                linkKeg: previous.Length == 0 ? null : previous.Any(k => k.Receipt.Links.Count > 0));
        }
        catch
        {
            foreach (var (keg, backup) in moved)
            {
                Directory.Move(backup, keg.Path);
                var links = keg.Receipt.Links.Count == 0 ? [] : linker.Link(keg.Path, keg.Receipt.Commands);
                InstallationStore.Save(keg, keg.Receipt with { Links = links });
            }
            if (previous.Length > 0) opt.Set(name, previous[^1].Path);
            throw;
        }
        foreach (var (_, backup) in moved) Directory.Delete(backup, true);
    }

    public static string FormulaCaveats(FormulaDefinition formula, InstallContext context)
    {
        if (Metadata.Spec(formula) != null) return Metadata.Call(formula, "Caveats", context.Prefix, context.BrewPrefix)?.ToString() ?? "";
        var value = Metadata.Get(formula, "Caveats", "CaveatsRenderer");
        return value switch { string text => text, Func<InstallContext, string> render => render(context), _ => "" };
    }

    static void RemoveEmptyRack(string rack)
    {
        if (Directory.Exists(rack) && !Directory.EnumerateFileSystemEntries(rack).Any()) Directory.Delete(rack);
    }

    public static string Abbreviation(string keg)
    {
        var files = Directory.GetFiles(keg, "*", SearchOption.AllDirectories);
        double bytes = files.Sum(f => new FileInfo(f).Length);
        var units = new[] { "B", "KB", "MB", "GB" };
        var unit = 0;
        while (Math.Round(bytes, 1) >= 1000 && unit < units.Length - 1) { bytes /= 1000; unit++; }
        return (files.Length > 1 ? $"{files.Length:N0} files, " : "") + $"{bytes:0.#}{units[unit]}";
    }
}
