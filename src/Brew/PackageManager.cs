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

    public async Task InstallAsync(string name, bool requested = true, bool force = false, bool ignoreDependencies = false, bool? linkKeg = null, bool fromSource = false, bool overwrite = false)
    {
        if (!installing.Add(name)) throw new InvalidOperationException($"Circular dependency: {name}");
        try { await InstallCoreAsync(name, requested, force, ignoreDependencies, linkKeg, fromSource, overwrite); }
        finally { installing.Remove(name); }
    }

    async Task InstallCoreAsync(string name, bool requested, bool force, bool ignoreDependencies, bool? linkKeg, bool fromSource, bool overwrite)
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
        // Another version installed: `brew install` upgrades it (install/check.rb), or explains the pin.
        var older = store.Kegs(name);
        if (older.Length > 0)
        {
            var message = $"{name} {older[^1].Receipt.Version} is already installed";
            if (!requested) return;   // a dependency that's present at any version is satisfied
            if (store.IsPinned(name) || Environment.GetEnvironmentVariable("HOMEBREW_NO_INSTALL_UPGRADE") is { Length: > 0 })
            {
                var unpin = store.IsPinned(name) ? $"brew unpin {name} && " : "";
                throw new InvalidOperationException($"{message}\nTo upgrade to {Metadata.Version(formula)}, run:\n  {unpin}brew upgrade {name}");
            }
            Console.WriteLine($"{message} but outdated (so it will be upgraded).");
            await ReplaceAsync(name, older, () => InstallCoreAsync(name, true, force, ignoreDependencies,
                older.Any(k => k.Receipt.Links.Count > 0), fromSource, overwrite));
            return;
        }
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
        var pending = ignoreDependencies ? [] : UnsatisfiedDependencies(formula);
        // FormulaInstaller#check_install_sanity: a pinned dependency can't be upgraded for this.
        var pinned = pending.Where(d => d.Installed != null && store.IsPinned(Paths.FormulaName(d.Name))).Select(d => d.Name).ToArray();
        if (pinned.Length > 0)
            throw new InvalidOperationException($"You must `brew unpin {string.Join(" ", pinned)}` as installing {name} requires the latest version of pinned dependencies.");
        if (pending.Count > 1)
            CommandOutput.Header($"Installing dependencies for {name}:" + (CommandOutput.Color ? "\u001b[0m" : "")
                + " " + Suggestions.Sentence(pending.Select(d => Upgradable(CommandOutput.Identifier(d.Name), d.Installed != null))), green: true);
        foreach (var dependency in pending)
        {
            if (dependency.Installed == null)
            {
                CommandOutput.Header($"Installing {name} dependency: {CommandOutput.Identifier(dependency.Name)} ({Metadata.Version(dependency.Formula)})", green: true);
                await InstallAsync(dependency.Name, requested: false);
                continue;
            }
            // FormulaInstaller#install_dependency: an outdated dependency is upgraded first, keeping
            // its linked and installed-on-request state.
            var dependencyName = Paths.FormulaName(dependency.Name);
            var from = new DirectoryInfo(opt.PathFor(dependencyName)).LinkTarget is string optTarget ? Path.GetFileName(Path.TrimEndingDirectorySeparator(optTarget)) + " -> " : "";
            CommandOutput.Header($"Upgrading {name} dependency: {CommandOutput.Identifier(dependency.Name)} ({from}{Metadata.Version(dependency.Formula)})", green: true);
            await ReplaceAsync(dependencyName, store.Kegs(dependencyName));
        }
        var bottle = fromSource || Environment.GetEnvironmentVariable("HOMEBREW_BUILD_FROM_SOURCE") == "1" ? null : Bottles.For(formula);
        var buildDependencies = QueryCommands.Dependencies(formula).Where(d => d.Tags.Contains("build")).Select(d => d.Name)
            .Where(d => !BuildEnvironment.ProvidesBuildTool(d)).ToList();
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
        // Cellar/<name>/.incomplete-<version> marks a keg this install is still filling: if brew dies
        // before the receipt (written last) exists, RecoverBackups removes what it left.
        var incomplete = IncompleteMarker(rack, Metadata.Version(formula));
        InstallContext context;
        try
        {
            if (bottle != null)
            {
                var archive = await new Downloader(paths.Cache).FetchAsync(bottle.Url, bottle.Sha256);
                CommandOutput.Header($"Pouring {bottle.FileName}");
                MarkIncomplete(incomplete);
                createdKeg = true;
                await Bottles.PourAsync(paths, archive, keg, bottle.Cellar);
                EtcVar.Restore(paths.Prefix, keg);
                // The bottle's own receipt is kept aside by PourAsync, so the keg doesn't look installed yet.
                var bottled = Path.Combine(keg, Bottles.PouredReceiptName);
                var poured = File.Exists(bottled) ? JsonSerializer.Deserialize<Receipt>(File.ReadAllText(bottled)) : null;
                context = new InstallContext(keg, build, null, paths.Prefix) { PouredFromBottle = true };
                context.Commands.AddRange(poured?.Commands ?? []);
            }
            else
            {
                string stagedBuild;
                if (formula.GitBranch != null)
                {
                    CommandOutput.Header($"Cloning {formula.Url}");
                    stagedBuild = await GitSources.StageAsync(formula.Url, formula.GitTag, formula.GitRevision, formula.GitBranch, Path.Combine(build, "src"));
                }
                else
                {
                    var download = await new Downloader(paths.Cache).FetchAsync(formula.Url, formula.Sha256, Metadata.Strings(formula, "Mirrors"));
                    stagedBuild = await Archive.StageAsync(download, Downloader.FileName(formula.Url), build);
                }
                var resources = new Dictionary<string, string>();
                foreach (var resource in formula.Resources)
                {
                    Paths.ValidateResourceName(resource.Name);
                    var resourcePath = Path.Combine(build, ".resources", resource.Name);
                    if (resource.Git)
                    {
                        resources.Add(resource.Name, await GitSources.StageAsync(resource.Url, resource.Tag, resource.Revision, resource.Branch ?? "", resourcePath));
                        continue;
                    }
                    var resourceFile = await new Downloader(paths.Cache).FetchAsync(resource.Url, resource.Sha256);
                    var stagedResource = await Archive.StageAsync(resourceFile, Downloader.FileName(resource.Url), resourcePath);
                    resources.Add(resource.Name, stagedResource);
                }
                await ApplyPatchesAsync(formula, stagedBuild, build, file);
                CommandOutput.Header($"Installing {name}");
                MarkIncomplete(incomplete);
                Directory.CreateDirectory(keg);
                createdKeg = true;
                context = new InstallContext(keg, stagedBuild, resources, paths.Prefix);
                BuildEnvironment.Apply(paths, context, runtimeDependencies.Concat(buildDependencies));
                var etcVar = EtcVar.Snapshot(paths.Prefix);
                formula.Install(context);
                EtcVar.Preserve(paths.Prefix, keg, etcVar);
                Linker.WriteScriptLaunchers(keg);
                // Direct dependencies first, then every other installed keg (their own dependencies).
                var direct = runtimeDependencies.Distinct(StringComparer.OrdinalIgnoreCase).Select(d => Path.Combine(paths.Prefix, "opt", Paths.FormulaName(d))).ToList();
                var installed = Directory.Exists(Path.Combine(paths.Prefix, "opt")) ? Directory.GetDirectories(Path.Combine(paths.Prefix, "opt")) : [];
                BuildEnvironment.BundleToolchainDlls(keg, direct.Concat(installed.Except(direct, StringComparer.OrdinalIgnoreCase))
                    .Where(k => !string.Equals(Path.GetFileName(k), name, StringComparison.OrdinalIgnoreCase)));
                if (Environment.GetEnvironmentVariable("BREW_DEDUPLICATE_MSYS2") == "1") SharedRuntime.Deduplicate(paths, keg);
                linker.MaterializeCommands(keg, context.Commands);
            }
            links = (linkKeg ?? !Metadata.Bool(formula, "KegOnly", "IsKegOnly")) ? linker.Link(keg, context.Commands, overwrite) : [];
            opt.Set(name, keg);
            Metadata.Call(formula, "RunPostInstall", keg, paths.Prefix);
            ReceiptFile.Write(Path.Combine(keg, ReceiptName),
                new Receipt(name, Metadata.Version(formula), formula.Url, formula.Sha256, links, context.Commands, runtimeDependencies,
                    requested, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), file, bottle != null));
        }
        catch
        {
            linker.Unlink(links);
            opt.Remove(name, keg);
            if (createdKeg && Directory.Exists(keg)) DeleteTree(keg);
            if (File.Exists(incomplete)) File.Delete(incomplete);
            RemoveEmptyRack(rack);
            throw;
        }
        finally { try { DeleteTree(build); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { CommandOutput.Warning($"Could not remove {build}: {e.Message}"); } }
        // Installed: nothing from here on may roll the keg back.
        try { File.Delete(Path.Combine(keg, Bottles.PouredReceiptName)); File.Delete(incomplete); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { CommandOutput.Warning($"Could not finish {keg}: {e.Message}"); }
        try
        {
            var caveats = FormulaCaveats(formula, context);
            if (!string.IsNullOrWhiteSpace(caveats)) { CommandOutput.Header("Caveats"); Console.WriteLine(caveats.TrimEnd()); CommandOutput.Header("Summary"); }
        }
        catch (Exception e) { CommandOutput.Warning($"Could not show the caveats for {name}: {e.Message}"); }
        try { if (!CommandOutput.Quiet) Console.WriteLine($"{CommandOutput.Badge}{keg}: {Abbreviation(keg)}"); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { CommandOutput.Warning($"Could not measure {keg}: {e.Message}"); }
    }

    static string IncompleteMarker(string rack, string version) => Path.Combine(rack, ".incomplete-" + version);

    static void MarkIncomplete(string marker)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(marker)!);
        File.WriteAllText(marker, "");
    }

    // Upgrade::formula_installers sorts the formulae topologically: dependencies upgrade before
    // their dependents (cycles are ignored).
    public string[] DependencyOrder(IEnumerable<string> names)
    {
        var wanted = names.ToList();
        var keys = wanted.Select(Paths.FormulaName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var order = new List<string>();
        void Visit(string name)
        {
            var key = Paths.FormulaName(name);
            if (!visited.Add(key)) return;
            try { foreach (var dependency in QueryCommands.Dependencies(repository.Load(name)).Where(d => d.Runtime)) Visit(dependency.Name); }
            catch (FormulaUnavailableException) { }
            if (keys.Contains(key)) order.Add(key);
        }
        foreach (var name in wanted) Visit(name);
        return wanted.OrderBy(n => order.IndexOf(Paths.FormulaName(n))).ToArray();
    }

    sealed record PendingDependency(string Name, FormulaDefinition Formula, InstalledKeg Installed);

    // FormulaInstaller#expand_dependencies (Dependency.expand): every recursive runtime dependency,
    // deepest first. One whose latest version is installed is skipped, but its own dependencies are
    // still checked; a missing one is installed and an outdated one upgraded.
    List<PendingDependency> UnsatisfiedDependencies(FormulaDefinition formula)
    {
        var pending = new List<PendingDependency>();
        var expanded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stack = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { formula.Name };
        void Expand(FormulaDefinition dependent)
        {
            foreach (var dependency in QueryCommands.Dependencies(dependent).Where(d => d.Runtime).Select(d => d.Name).Distinct())
            {
                var key = Paths.FormulaName(dependency);
                if (stack.Contains(key) || !expanded.Add(key)) continue;
                FormulaDefinition definition;
                // An installed dependency whose formula is gone can't be checked further; it stays as it is.
                try { definition = repository.Load(dependency); }
                catch (FormulaUnavailableException) when (store.Latest(key) != null) { continue; }
                stack.Add(key);
                try { Expand(definition); }
                finally { stack.Remove(key); }
                var installed = store.Latest(key);
                if (installed == null || VersionOrder.Instance.Compare(installed.Receipt.Version, Metadata.Version(definition)) < 0)
                    pending.Add(new(dependency, definition, installed));
            }
        }
        Expand(formula);
        return pending;
    }

    // Utils::Output#pretty_upgradable, for an installed but outdated dependency on a terminal.
    static string Upgradable(string name, bool outdated) => !outdated || Console.IsOutputRedirected ? name
        : Environment.GetEnvironmentVariable("HOMEBREW_NO_EMOJI") != null ? name + " (upgradable)"
        : name + " " + (CommandOutput.Color ? "\u001b[32m↑\u001b[0m" : "↑");

    // Homebrew's Formula#patch: inline/DATA diffs, downloaded diffs, and `apply` files inside patch archives.
    async Task ApplyPatchesAsync(FormulaDefinition formula, string source, string build, string formulaFile)
    {
        // `patch do file "Patches/x/y.patch"` names a file in the tap (homebrew-core keeps them in
        // Patches/ beside Formula/); a formula outside a tap resolves it next to itself.
        static string PatchFile(string formulaFile, string file)
        {
            for (var dir = Path.GetDirectoryName(formulaFile); dir != null; dir = Path.GetDirectoryName(dir))
                if (string.Equals(Path.GetFileName(dir), "Formula", StringComparison.OrdinalIgnoreCase))
                    return Path.Combine(Path.GetDirectoryName(dir)!, file);
            return Path.Combine(Path.GetDirectoryName(formulaFile)!, file);
        }

        if (formula.Patches.Count == 0) return;
        CommandOutput.Header("Patching");
        var archive = 0;
        foreach (var patch in formula.Patches)
        {
            var directory = string.IsNullOrEmpty(patch.Directory) ? source : Path.Combine(source, patch.Directory);
            if (patch.Data != null) { Patcher.Apply(patch.Data, patch.Strip, directory); continue; }
            if (patch.File != null)
            {
                Patcher.Apply(File.ReadAllBytes(PatchFile(formulaFile, patch.File)), patch.Strip, directory);
                continue;
            }
            var download = await new Downloader(paths.Cache).FetchAsync(patch.Url, patch.Sha256);
            if (patch.Apply.Count == 0) { Patcher.Apply(File.ReadAllBytes(download), patch.Strip, directory); continue; }
            var staged = await Archive.StageAsync(download, Downloader.FileName(patch.Url), Path.Combine(build, ".patches", (archive++).ToString()));
            foreach (var diff in patch.Apply) Patcher.Apply(File.ReadAllBytes(Path.Combine(staged, diff)), patch.Strip, directory);
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
                foreach (var file in Paths.TreeFiles(keg).Order(StringComparer.Ordinal))
                    if (Path.GetFileName(file) != ReceiptName) Console.WriteLine(file);
        }
    }

    public void Uninstall(string name, bool force, bool ignoreDependencies = false)
    {
        name = store.ResolveName(name);
        var kegs = InstalledKegs(name);
        if (kegs.Length == 0 && !force) throw new InvalidOperationException($"No such keg: {paths.Rack(name)}");
        if (!force && store.IsPinned(name)) throw new InvalidOperationException($"{name} is pinned. You must unpin it to uninstall.");
        // Without --force only the default keg goes (cmd/uninstall.rb's :default_kegs); --force takes them all.
        if (!force && kegs.Length > 1) kegs = [DefaultKeg(name, kegs)];
        if (kegs.Length > 0 && !ignoreDependencies)
        {
            // Only Cellar/<rack>/<version>/INSTALL_RECEIPT.json: a recursive search would also read files
            // deep inside kegs (and walk their directory links).
            var dependents = store.All.Select(k => k.Receipt)
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
        var rack = paths.Rack(name);
        if (force && Directory.Exists(rack)) Console.WriteLine($"Uninstalling {name}... ({Abbreviation(rack)})");
        foreach (var removal in removals)
        {
            var keg = removal.Path; var receipt = removal.Receipt;
            if (!force) Console.WriteLine($"Uninstalling {keg}... ({Abbreviation(keg)})");
            linker.Unlink(receipt.Links);
            opt.Remove(name, keg);
            DeleteTree(keg);
        }
        RemoveEmptyRack(rack);
        var pin = Path.Combine(store.PinDirectory, name);
        if (File.Exists(pin)) File.Delete(pin);
        if (!force && Directory.Exists(rack))
        {
            var versions = Directory.GetDirectories(rack).Select(Path.GetFileName).Where(v => !v.StartsWith('.')).Order(VersionOrder.Instance).ToArray();
            if (versions.Length > 0)
                Console.WriteLine($"{name} {Suggestions.Sentence(versions)} {(versions.Length == 1 ? "is" : "are")} still installed.\n"
                    + $"To remove all versions, run:\n  brew uninstall --force {name}");
        }
    }

    // NamedArgs#resolve_default_keg: the keg opt/<name> points at, the only keg, or the formula's version.
    string DefaultKeg(string name, string[] kegs)
    {
        var optLink = opt.PathFor(name);
        if (new DirectoryInfo(optLink).LinkTarget is string target
            && kegs.FirstOrDefault(k => string.Equals(k, Path.GetFullPath(target, Path.GetDirectoryName(optLink)!), StringComparison.OrdinalIgnoreCase)) is { } linked)
            return linked;
        if (kegs.Length == 1) return kegs[0];
        try
        {
            var current = Path.Combine(paths.Rack(name), Metadata.Version(repository.Load(name)));
            return kegs.FirstOrDefault(k => string.Equals(k, current, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"{name} has multiple installed versions\nRun `brew uninstall --force {name}` to remove all versions.");
        }
        catch (FormulaUnavailableException)
        {
            throw new InvalidOperationException($"Multiple kegs installed to {paths.Rack(name)}\nHowever we don't know which one you refer to.\nPlease delete (with `rm -rf`!) all but one and then try again.");
        }
    }

    public void Link(string name, bool unlink, bool force = false, bool overwrite = false, bool dryRun = false)
    {
        name = store.ResolveName(name);
        // `brew link` links the latest keg only (to_latest_kegs); linking every version would collide.
        var kegs = RequireKegs(name);
        foreach (var keg in unlink ? kegs : [kegs[^1]])
        {
            var receiptPath = Path.Combine(keg, ReceiptName);
            var receipt = JsonSerializer.Deserialize<Receipt>(File.ReadAllText(receiptPath));
            if (unlink && dryRun)
            {
                Console.WriteLine("Would remove:");
                foreach (var path in linker.Present(receipt.Links)) Console.WriteLine(path);
            }
            else if (unlink)
            {
                // Refuse a locked/read-only receipt before removing any links.
                using (var writable = new FileStream(receiptPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read)) { }
                linker.Unlink(receipt.Links);
                ReceiptFile.Write(receiptPath, receipt with { Links = [] });
                Console.WriteLine($"Unlinking {keg}... {receipt.Links.Count} symlinks removed.");
            }
            else
            {
                var file = receipt.SourcePath;
                FormulaDefinition Formula() => file != null && File.Exists(file) ? repository.LoadFile(file) : repository.Load(name);
                bool StrictKegOnly(FormulaDefinition f) => Metadata.Bool(f, "KegOnly")
                    && Metadata.Text(Metadata.Get(f, "KegOnlyReason"), "Name") != "versioned_formula";
                if (receipt.Links.Count > 0)
                {
                    Console.Error.WriteLine($"Warning: Already linked: {keg}");
                    bool strict;
                    try { strict = StrictKegOnly(Formula()); } catch (FormulaUnavailableException) { strict = false; }
                    Console.WriteLine($"To relink, run:\n  brew unlink {name} && brew link {(strict ? "--force " : "")}{name}");
                    continue;
                }
                void PathMessage()
                {
                    var bins = new[] { "bin", "sbin" }.Where(dir => Directory.Exists(Path.Combine(keg, dir))).ToArray();
                    if (bins.Length == 0) return;
                    Console.WriteLine("\nIf you need to have this software first in your PATH instead consider running:");
                    foreach (var dir in bins)
                        Console.WriteLine($"  $env:PATH = '{Path.Combine(opt.PathFor(name), dir).Replace("'", "''")};' + $env:PATH");
                }
                bool kegOnly;
                try { kegOnly = StrictKegOnly(Formula()); } catch (FormulaUnavailableException) { kegOnly = false; }
                // cmd/link.rb: a dry run lists before the keg-only refusal.
                if (dryRun)
                {
                    Console.WriteLine(overwrite ? "Would remove:" : "Would link:");
                    linker.Link(keg, receipt.Commands, overwrite, dryRun: true);
                    if (kegOnly) PathMessage();
                    continue;
                }
                if (!force && kegOnly)
                {
                    CommandOutput.Warning($"{name} is keg-only and must be linked with `--force`.");
                    PathMessage();
                    continue;
                }
                Console.Write($"Linking {keg}... ");
                List<BinLink> links;
                try { links = linker.Link(keg, receipt.Commands, overwrite); }
                catch { Console.WriteLine(); throw; }
                try { ReceiptFile.Write(receiptPath, receipt with { Links = links }); }
                catch { linker.Unlink(links); throw; }
                Console.WriteLine($"{links.Count} symlinks created.");
                if (kegOnly) PathMessage();
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
        if (formula.GitBranch != null)
        {
            var clone = Path.Combine(paths.Cache, Paths.FormulaName(formula.Name) + "--git");
            DeleteTree(clone);
            await GitSources.StageAsync(formula.Url, formula.GitTag, formula.GitRevision, formula.GitBranch, clone);
        }
        else await new Downloader(paths.Cache).FetchAsync(formula.Url, formula.Sha256, Metadata.Strings(formula, "Mirrors"));
        foreach (var resource in formula.Resources.Where(r => !r.Git)) await new Downloader(paths.Cache).FetchAsync(resource.Url, resource.Sha256);
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

    async Task ReplaceAsync(string name, InstalledKeg[] previous, Func<Task> install = null)
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
            await (install?.Invoke() ?? InstallAsync(name, previous.Length == 0 || previous.Any(k => k.Receipt.InstalledOnRequest),
                linkKeg: previous.Length == 0 ? null : previous.Any(k => k.Receipt.Links.Count > 0)));
        }
        catch
        {
            // Put every keg back before relinking any, and keep going past individual failures.
            foreach (var (keg, backup) in moved)
                try { Directory.Move(backup, keg.Path); }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                { CommandOutput.Warning($"Could not restore {keg.Path} from {backup}: {e.Message}"); }
            foreach (var (keg, _) in moved.Where(m => Directory.Exists(m.Keg.Path)))
                try
                {
                    var links = keg.Receipt.Links.Count == 0 ? [] : linker.Link(keg.Path, keg.Receipt.Commands);
                    InstallationStore.Save(keg, keg.Receipt with { Links = links });
                }
                catch (IOException e) { CommandOutput.Warning($"Could not relink {keg.Path}: {e.Message}"); }
            if (previous.Length > 0 && Directory.Exists(previous[^1].Path)) opt.Set(name, previous[^1].Path);
            throw;
        }
        // The replacement is installed: a backup that can't be removed now is stale, not a failure.
        // Its receipt goes first, so RecoverBackups never restores a half-deleted copy over it.
        foreach (var (_, backup) in moved)
            try { File.Delete(Path.Combine(backup, ReceiptName)); DeleteTree(backup); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            { CommandOutput.Warning($"Could not remove {backup}: {e.Message}"); }
    }

    // Directory.Delete refuses read-only files (chmod 0444, `install -m 444`); clear the attribute first.
    // Directory links are removed, never entered: their targets' attributes aren't ours to change.
    internal static void DeleteTree(string path)
    {
        if (!Directory.Exists(path)) return;
        if (new DirectoryInfo(path).LinkTarget == null)
            foreach (var entry in Paths.TreeFiles(path, includeDirectories: true))
            {
                var attributes = File.GetAttributes(entry);
                if (attributes.HasFlag(FileAttributes.ReadOnly) && !attributes.HasFlag(FileAttributes.ReparsePoint))
                    File.SetAttributes(entry, attributes & ~FileAttributes.ReadOnly);
            }
        Directory.Delete(path, true);
    }

    // Leftovers of an interrupted upgrade (Cellar/<name>/.backup-*): put the keg back if its version
    // is missing, otherwise drop the stale copy.
    public void RecoverBackups()
    {
        if (!Directory.Exists(paths.Cellar)) return;
        foreach (var rack in Directory.GetDirectories(paths.Cellar))
        {
            // An install that died before writing its receipt: drop the partial keg and its links.
            var markers = Directory.GetFiles(rack, ".incomplete-*");
            foreach (var marker in markers)
                try
                {
                    var keg = Path.Combine(rack, Path.GetFileName(marker)[".incomplete-".Length..]);
                    if (File.Exists(Path.Combine(keg, ReceiptName))) File.Delete(Path.Combine(keg, Bottles.PouredReceiptName));
                    else if (Directory.Exists(keg))
                    {
                        linker.UnlinkInto(keg);
                        opt.Remove(Path.GetFileName(rack), keg);
                        DeleteTree(keg);
                        CommandOutput.Warning($"Removed {keg} left by an interrupted install.");
                    }
                    File.Delete(marker);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException)
                { CommandOutput.Warning($"Could not remove the interrupted install {marker}: {e.Message}"); }
            RecoverRack(rack);
            if (markers.Length > 0)
                try { RemoveEmptyRack(rack); }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException) { CommandOutput.Warning($"Could not remove {rack}: {e.Message}"); }
        }
    }

    void RecoverRack(string rack)
    {
        foreach (var backup in Directory.GetDirectories(rack, ".backup-*"))
        {
            try
            {
                var receipt = Path.Combine(backup, ReceiptName);
                var saved = File.Exists(receipt) ? JsonSerializer.Deserialize<Receipt>(File.ReadAllText(receipt)) : null;
                var keg = saved?.Version == null ? null : Path.Combine(rack, saved.Version);
                if (keg != null && !Directory.Exists(keg))
                {
                    Directory.Move(backup, keg);
                    // ReplaceAsync unlinked the keg before moving it, so the receipt's links are gone:
                    // record that, or `brew link` would answer "Already linked".
                    if (saved.Links.Count > 0) ReceiptFile.Write(Path.Combine(keg, ReceiptName), saved with { Links = [] });
                    try
                    {
                        // opt/<name> may still name the replacement that never finished.
                        var optTarget = new DirectoryInfo(opt.PathFor(Path.GetFileName(rack))).LinkTarget;
                        if (optTarget == null || !File.Exists(Path.Combine(Path.GetFullPath(optTarget, Path.Combine(paths.Prefix, "opt")), ReceiptName)))
                            opt.Set(Path.GetFileName(rack), keg);
                    }
                    catch (Exception e) when (e is IOException or InvalidOperationException)
                    { CommandOutput.Warning($"Could not point opt at {keg}: {e.Message}"); }
                    CommandOutput.Warning($"Restored {keg} from an interrupted upgrade; run `brew link {Path.GetFileName(rack)}` if needed.");
                }
                else DeleteTree(backup);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
            { CommandOutput.Warning($"Could not recover {backup}: {e.Message}"); }
        }
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
        var files = Paths.TreeFiles(keg).ToArray();
        double bytes = files.Sum(f => new FileInfo(f).Length);
        var units = new[] { "B", "KB", "MB", "GB" };
        var unit = 0;
        while (Math.Round(bytes, 1) >= 1000 && unit < units.Length - 1) { bytes /= 1000; unit++; }
        return (files.Length > 1 ? $"{files.Length:N0} files, " : "") + $"{bytes:0.#}{units[unit]}";
    }
}
