namespace Brew;

public sealed class MaintenanceCommands(Paths paths)
{
    readonly InstallationStore store = new(paths);

    public void Cleanup(CommandArguments args)
    {
        var selected = args.Names.Count == 0 ? null : args.Names.Select(store.ResolveName).ToHashSet();
        var selectedCache = selected == null ? null : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (selectedCache != null)
        {
            void IncludeDownload(string url, string sha)
            {
                if (sha != null && Uri.TryCreate(url, UriKind.Absolute, out var uri))
                    selectedCache.Add(sha.ToLowerInvariant() + "--" + Downloader.FileName(url));
            }
            var repository = new FormulaRepository(paths);
            foreach (var input in args.Names)
            {
                var kegs = store.Kegs(store.ResolveName(input));
                foreach (var keg in kegs) IncludeDownload(keg.Receipt.Url, keg.Receipt.Sha256);
                try
                {
                    var file = kegs.LastOrDefault()?.Receipt.SourcePath;
                    var formula = file != null && File.Exists(file) ? repository.LoadFile(file) : repository.Load(input);
                    IncludeDownload(formula.Url, formula.Sha256);
                    foreach (var resource in formula.Resources) IncludeDownload(resource.Url, resource.Sha256);
                }
                catch (FormulaUnavailableException) when (kegs.Length > 0) { }
            }
        }
        long bytes = 0;
        foreach (var group in store.All.GroupBy(k => k.Receipt.Name))
        {
            if (selected != null && !selected.Contains(group.Key) || store.IsPinned(group.Key)) continue;
            foreach (var keg in group.SkipLast(1))
            {
                if (keg.Receipt.Links.Any(l => new FileInfo(l.Path).LinkTarget == l.Target || l.Shim && File.Exists(l.Path + ".brew-shim") && File.ReadAllText(l.Path + ".brew-shim").StartsWith(l.Target))) continue;
                bytes += Paths.TreeFiles(keg.Path).Sum(f => new FileInfo(f).Length);
                Console.WriteLine($"{(args.Has("--dry-run", "-n") ? "Would remove" : "Removing")}: {keg.Path}... ({PackageManager.Abbreviation(keg.Path)})");
                if (!args.Has("--dry-run", "-n")) PackageManager.DeleteTree(keg.Path);
            }
        }
        if (Directory.Exists(paths.Cache))
        {
            var age = int.TryParse(args.Value("--prune"), out var days) ? days : args.Value("--prune") == "all" ? 0 : 120;
            foreach (var file in Directory.EnumerateFiles(paths.Cache))
            {
                var filename = Path.GetFileName(file);
                var key = filename.EndsWith(".incomplete", StringComparison.OrdinalIgnoreCase) ? filename[..^11] : filename;
                if (selectedCache != null && !selectedCache.Contains(key)) continue;
                using var cacheLock = DownloadLock.TryAcquire(paths.Cache, key);
                if (cacheLock == null || !File.Exists(file)) continue;
                if (File.GetLastWriteTimeUtc(file) > DateTime.UtcNow.AddDays(-age)) continue;
                bytes += new FileInfo(file).Length;
                Console.WriteLine($"{(args.Has("--dry-run", "-n") ? "Would remove" : "Removing")}: {file}... ({CommandOutput.Size(new FileInfo(file).Length)})");
                if (!args.Has("--dry-run", "-n")) File.Delete(file);
            }
        }
        if (bytes > 0) CommandOutput.Header($"This operation {(args.Has("--dry-run", "-n") ? "would free" : "has freed")} approximately {CommandOutput.Size(bytes)} of disk space.");
    }

    public void Autoremove(CommandArguments args)
    {
        var all = store.All.GroupBy(k => k.Receipt.Name).ToDictionary(g => g.Key, g => g.Last());
        var keep = new HashSet<string>();
        void Retain(string name)
        {
            name = Paths.FormulaName(name);
            if (!keep.Add(name) || !all.TryGetValue(name, out var keg)) return;
            foreach (var dependency in keg.Receipt.Dependencies ?? []) Retain(dependency);
        }
        foreach (var keg in all.Values.Where(k => k.Receipt.InstalledOnRequest || store.IsPinned(k.Receipt.Name))) Retain(keg.Receipt.Name);
        var unused = all.Keys.Except(keep).Order(StringComparer.Ordinal).ToArray();
        if (unused.Length == 0) return;
        CommandOutput.Header($"{(args.Has("--dry-run", "-n") ? "Would autoremove" : "Autoremoving")} {unused.Length} unneeded {(unused.Length == 1 ? "formula" : "formulae")}:\n{string.Join("\n", unused)}");
        if (!args.Has("--dry-run", "-n")) foreach (var name in unused) new PackageManager(paths).Uninstall(name, false, ignoreDependencies: true);
    }
}
