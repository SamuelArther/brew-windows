using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// mac-cleanup-go (mac-cleanup) and mole (mo): disk cleaners. On Windows the cache, log and temp
// locations are the Windows ones (%TEMP%, %LOCALAPPDATA% caches, browser caches, package-manager
// caches, the Recycle Bin, Windows Update leftovers), scanned read-only. Nothing is deleted unless
// you ask: --clean / `mo clean` without --dry-run moves the selected items to the Recycle Bin.
static class Cleaners
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("mac-cleanup", "Scan caches, logs and temp files and clean what you choose", MacCleanup);
        Commands.Add("mole", "Deep clean, analyze disk usage and show a system dashboard", Mole);
        Commands.Add("mo", "Deep clean, analyze disk usage and show a system dashboard", Mole);
    }

    // A cleanup target: a label, an impact level and the paths it covers.
    sealed record Target(string Name, string Level, string[] Paths, bool Default = true);

    static string Env(string var) => Environment.GetEnvironmentVariable(var) ?? "";
    static string Local => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    static string Roaming => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    static string Home => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    // The catalogue of what these cleaners look at. "safe" regenerates automatically; "moderate"
    // may need a re-download or re-login; "risky" can hold user data and starts unselected.
    static List<Target> Catalogue() =>
    [
        new("Temporary Files", "safe", [Env("TEMP"), Path.Combine(Local, "Temp"), @"C:\Windows\Temp"]),
        new("Windows Thumbnail Cache", "safe", [Path.Combine(Local, "Microsoft", "Windows", "Explorer")]),
        new("Windows Update Cache", "moderate", [@"C:\Windows\SoftwareDistribution\Download"]),
        new("Windows Error Reports", "safe", [Path.Combine(Local, "Microsoft", "Windows", "WER"), Path.Combine(Env("ProgramData"), "Microsoft", "Windows", "WER")]),
        new("Delivery Optimization", "safe", [@"C:\Windows\SoftwareDistribution\DeliveryOptimization"]),
        new("Crash Dumps", "safe", [Path.Combine(Local, "CrashDumps")]),
        new("Chrome Cache", "moderate", [Path.Combine(Local, "Google", "Chrome", "User Data", "Default", "Cache")]),
        new("Edge Cache", "moderate", [Path.Combine(Local, "Microsoft", "Edge", "User Data", "Default", "Cache")]),
        new("Firefox Cache", "moderate", [Path.Combine(Local, "Mozilla", "Firefox", "Profiles")]),
        new("npm Cache", "safe", [Path.Combine(Local, "npm-cache"), Path.Combine(Roaming, "npm-cache")]),
        new("pip Cache", "safe", [Path.Combine(Local, "pip", "Cache")]),
        new("NuGet Cache", "safe", [Path.Combine(Home, ".nuget", "packages")]),
        new("Gradle Cache", "safe", [Path.Combine(Home, ".gradle", "caches")]),
        new("Go Build Cache", "safe", [Path.Combine(Local, "go-build")]),
        new("Cargo Registry Cache", "safe", [Path.Combine(Home, ".cargo", "registry", "cache")]),
        new("Yarn Cache", "safe", [Path.Combine(Local, "Yarn", "Cache")]),
        new("VS Code Cache", "safe", [Path.Combine(Roaming, "Code", "Cache"), Path.Combine(Roaming, "Code", "CachedData")]),
        new("Docker (dangling data)", "moderate", [Path.Combine(Local, "Docker", "wsl", "data")], Default: false),
        new("Downloads (old)", "risky", [Path.Combine(Home, "Downloads")], Default: false),
        new("Recycle Bin", "safe", [], Default: false),
    ];

    // ---- shared sizing ----

    sealed class Scan { public long Bytes; public int Files; public bool Partial; }

    static Scan Measure(string path)
    {
        var s = new Scan();
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return s;
        var stack = new Stack<string>();
        stack.Push(path);
        while (stack.Count > 0)
        {
            var dir = stack.Pop();
            try
            {
                foreach (var entry in new DirectoryInfo(dir).EnumerateFileSystemInfos())
                {
                    if (entry.Attributes.HasFlag(FileAttributes.ReparsePoint)) continue;   // don't follow junctions
                    if (entry is DirectoryInfo d) stack.Push(d.FullName);
                    else { s.Bytes += ((FileInfo)entry).Length; s.Files++; }
                }
            }
            catch (UnauthorizedAccessException) { s.Partial = true; }
            catch (IOException) { s.Partial = true; }
        }
        return s;
    }

    static string Human(long bytes)
    {
        string[] u = ["B", "KB", "MB", "GB", "TB"];
        double v = bytes;
        var i = 0;
        while (v >= 1024 && i < u.Length - 1) { v /= 1024; i++; }
        return i == 0 ? $"{v:0} B" : $"{v:0.0} {u[i]}";
    }

    // Move everything inside the target paths to the Recycle Bin (never the folders themselves).
    static (long Bytes, int Items) Clean(Target t)
    {
        long bytes = 0;
        var items = 0;
        if (t.Name == "Recycle Bin") { EmptyRecycleBin(); return (0, 0); }
        foreach (var path in t.Paths)
        {
            if (!Directory.Exists(path)) continue;
            foreach (var entry in SafeEntries(path))
            {
                var scan = Directory.Exists(entry) ? Measure(entry) : new Scan { Bytes = FileLen(entry), Files = 1 };
                if (Recycle(entry)) { bytes += scan.Bytes; items += Math.Max(1, scan.Files); }
            }
        }
        return (bytes, items);
    }

    static IEnumerable<string> SafeEntries(string dir)
    {
        try { return new DirectoryInfo(dir).EnumerateFileSystemInfos().Select(e => e.FullName).ToList(); }
        catch (Exception e) when (e is UnauthorizedAccessException or IOException) { return []; }
    }

    static long FileLen(string f) { try { return new FileInfo(f).Length; } catch (IOException) { return 0; } }

    // ---- mac-cleanup-go ----

    static int MacCleanup(string[] args)
    {
        bool clean = false, dryRun = false, select = false, debug = false, json = false;
        foreach (var a in args)
        {
            switch (a)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage: mac-cleanup [flags]

                        Scan Windows cache, log and temp locations and clean what you choose. By default items
                        go to the Recycle Bin; only the Recycle Bin category empties it permanently.

                        Flags:
                          --clean            Perform cleanup (combine with --dry-run to preview)
                          --dry-run          Preview the cleanup report without deleting anything
                          --select           List configurable cleanup targets and their impact levels
                          --debug            Print each scanned path
                          --json             Machine-readable report
                          --update           Shown for compatibility (use `brew upgrade mac-cleanup-go`)
                          -V, --version      Print version
                        """);
                    return 0;
                case "-V" or "--version": Console.WriteLine("1.6.0"); return 0;
                case "--clean": clean = true; break;
                case "--dry-run" or "-n": dryRun = true; break;
                case "--select" or "-s": select = true; break;
                case "--debug": debug = true; break;
                case "--json": json = true; break;
                case "--update": Console.WriteLine("mac-cleanup-go was installed by brew; run: brew upgrade mac-cleanup-go"); return 0;
                default: throw new UsageException($"unknown flag {a} (see mac-cleanup --help)");
            }
        }
        var targets = Catalogue();
        if (select)
        {
            Console.WriteLine("Cleanup targets                         * safe  o moderate  ! risky");
            Console.WriteLine(new string('-', 57));
            foreach (var t in targets)
            {
                var mark = t.Level == "safe" ? '*' : t.Level == "moderate" ? 'o' : '!';
                Console.WriteLine($"  [{(t.Default ? 'x' : ' ')}] {mark} {t.Name}");
            }
            return 0;
        }

        // Default action and --clean both scan first; without --clean we only report.
        var results = new List<(Target T, Scan S)>();
        foreach (var t in targets.Where(t => t.Default || clean))
        {
            var scan = new Scan();
            foreach (var p in t.Paths)
            {
                if (debug) Console.Error.WriteLine($"scanning {p}");
                var m = Measure(p);
                scan.Bytes += m.Bytes; scan.Files += m.Files; scan.Partial |= m.Partial;
            }
            if (t.Name == "Recycle Bin") scan = RecycleBinSize();
            results.Add((t, scan));
        }
        results.Sort((a, b) => b.S.Bytes.CompareTo(a.S.Bytes));
        var total = results.Sum(r => r.S.Bytes);

        if (json)
        {
            var sb = new StringBuilder("{\"mode\":");
            sb.Append(clean && !dryRun ? "\"clean\"" : dryRun ? "\"dry-run\"" : "\"report\"");
            sb.Append($",\"freed_bytes\":{total},\"categories\":[");
            sb.Append(string.Join(",", results.Select(r => $"{{\"name\":{PerfSampler.Json(r.T.Name)},\"level\":{PerfSampler.Json(r.T.Level)},\"bytes\":{r.S.Bytes},\"items\":{r.S.Files}{(r.S.Partial ? ",\"scan_status\":\"partial\"" : "")}}}")));
            Console.WriteLine(sb.Append("]}").ToString());
        }
        else
        {
            Console.WriteLine(clean && !dryRun ? "Cleanup Report" : "Dry Run Report");
            Console.WriteLine(new string('-', 50));
            Console.WriteLine($"Mode: {(clean && !dryRun ? "Clean" : "Dry Run")}\n");
            Console.WriteLine($"{"STATUS",-8}{"CATEGORY",-28}{"ITEMS",8}{"SIZE",12}");
            foreach (var (t, s) in results.Where(r => r.S.Bytes > 0 || r.S.Files > 0))
                Console.WriteLine($"{(s.Partial ? "PARTIAL" : "OK"),-8}{t.Name,-28}{s.Files,8}{Human(s.Bytes),12}");
            Console.WriteLine(new string('-', 50));
            Console.WriteLine($"{(clean && !dryRun ? "Freed" : "Freed (dry-run)"),-36}{Human(total),12}");
        }

        if (clean && !dryRun)
        {
            long freed = 0;
            foreach (var (t, _) in results)
            {
                var (bytes, _) = Clean(t);
                freed += bytes;
            }
            if (!json) Console.WriteLine($"\nRemoved to Recycle Bin: {Human(freed)}");
        }
        return 0;
    }

    // ---- mole ----

    static int Mole(string[] args)
    {
        if (args.Length == 0)
        {
            MoleHelp();
            return 0;
        }
        var cmd = args[0];
        var rest = args[1..];
        bool dryRun = rest.Contains("--dry-run"), json = rest.Contains("--json"), yes = rest.Contains("--yes");
        var paths = rest.Where(a => !a.StartsWith('-')).ToList();
        switch (cmd)
        {
            case "-h" or "--help" or "help": MoleHelp(); return 0;
            case "-V" or "--version" or "version": Console.WriteLine("1.60.0"); return 0;
            case "status": return MoleStatus(json);
            case "analyze" or "analyse": return MoleAnalyze(paths.FirstOrDefault() ?? Home, json);
            case "clean": return MoleClean(dryRun, json);
            case "purge": return MolePurge(paths.FirstOrDefault() ?? Home, dryRun || !yes, json);
            case "optimize" or "installer" or "uninstall":
                Console.Error.WriteLine($"mole: `{cmd}` applies to macOS services and app bundles and has no Windows equivalent; use `mole clean`, `mole purge`, Storage Sense or cleanmgr");
                return 1;
            case "completion" or "touchid" or "history" or "update" or "remove":
                Console.Error.WriteLine($"mole: `{cmd}` is managed by brew on Windows");
                return 1;
            default: throw new UsageException($"unknown command '{cmd}' (see mole --help)");
        }
    }

    static int MoleStatus(bool json)
    {
        using var sampler = new PerfSampler(false, PerfSampler.DiskCounters);
        var a = sampler.Take();
        PerfSampler.Sleep(0.4);
        var b = sampler.Take();
        var cpu = PerfSampler.CpuBusy(a.Total, b.Total);
        var mem = b.Mem;
        var usedMem = mem.Total - mem.Available;
        var drive = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory)!);
        var diskUsed = drive.TotalSize - drive.AvailableFreeSpace;
        var batt = PerfSampler.Battery();
        if (json)
        {
            var sb = new StringBuilder("{");
            sb.Append($"\"cpu\":{{\"usage\":{PerfSampler.Json(cpu / 100)},\"cores\":{Environment.ProcessorCount}}},");
            sb.Append($"\"memory\":{{\"used\":{usedMem},\"total\":{mem.Total},\"usage\":{PerfSampler.Json((double)usedMem / Math.Max(1, mem.Total))}}},");
            sb.Append($"\"disk\":{{\"used\":{diskUsed},\"total\":{drive.TotalSize},\"free\":{drive.AvailableFreeSpace}}},");
            sb.Append($"\"power\":{{\"battery\":{batt.Percent},\"charging\":{(batt.Charging ? "true" : "false")},\"has_battery\":{(batt.HasBattery ? "true" : "false")}}},");
            sb.Append($"\"scan_status\":\"complete\"");
            Console.WriteLine(sb.Append('}').ToString());
            return 0;
        }
        Console.OutputEncoding = Encoding.UTF8;
        var health = (int)Math.Clamp(100 - cpu * 0.3 - 100.0 * usedMem / mem.Total * 0.3 - 100.0 * diskUsed / drive.TotalSize * 0.4, 0, 100);
        Console.WriteLine($"Mole Status  Health {health}  {PerfSampler.CpuName}  {PerfSampler.OsName}\n");
        Console.WriteLine($"CPU     {PerfSampler.Bar(cpu, 20)}  {cpu,5:F1}%   ({Environment.ProcessorCount} cores)");
        Console.WriteLine($"Memory  {PerfSampler.Bar(100.0 * usedMem / mem.Total, 20)}  {Human(usedMem)} / {Human(mem.Total)}");
        Console.WriteLine($"Disk    {PerfSampler.Bar(100.0 * diskUsed / drive.TotalSize, 20)}  {Human(diskUsed)} / {Human(drive.TotalSize)} ({Human(drive.AvailableFreeSpace)} free)");
        if (batt.HasBattery) Console.WriteLine($"Power   {PerfSampler.Bar(batt.Percent, 20)}  {batt.Percent}% {(batt.Charging ? "charging" : batt.OnAc ? "on AC" : "on battery")}");
        return 0;
    }

    static int MoleAnalyze(string root, bool json)
    {
        root = Path.GetFullPath(root);
        if (!Directory.Exists(root)) throw new IOException($"{root}: No such directory");
        var entries = new List<(string Name, Scan S)>();
        var rootPartial = false;
        foreach (var dir in SafeEntries(root))
        {
            if (Directory.Exists(dir)) entries.Add((Path.GetFileName(dir) + "/", Measure(dir)));
            else entries.Add((Path.GetFileName(dir), new Scan { Bytes = FileLen(dir), Files = 1 }));
        }
        try { _ = new DirectoryInfo(root).EnumerateFileSystemInfos().Any(); }
        catch (Exception e) when (e is UnauthorizedAccessException or IOException) { rootPartial = true; }
        entries.Sort((a, b) => b.S.Bytes.CompareTo(a.S.Bytes));
        var total = entries.Sum(e => e.S.Bytes);
        var partial = rootPartial || entries.Any(e => e.S.Partial);
        if (json)
        {
            var sb = new StringBuilder($"{{\"path\":{PerfSampler.Json(root)},\"size\":{total},\"scan_status\":\"{(partial ? "partial" : "complete")}\",\"entries\":[");
            sb.Append(string.Join(",", entries.Select(e => $"{{\"name\":{PerfSampler.Json(e.Name)},\"size\":{e.S.Bytes},\"scan_status\":\"{(e.S.Partial ? "partial" : "complete")}\"}}")));
            Console.WriteLine(sb.Append("]}").ToString());
            return 0;
        }
        Console.WriteLine($"Disk usage of {root}{(partial ? " (partial)" : "")}\n");
        foreach (var (name, s) in entries.Take(30))
            Console.WriteLine($"{Human(s.Bytes),12}{(s.Partial ? "+" : " ")}  {name}");
        Console.WriteLine(new string('-', 40));
        Console.WriteLine($"{Human(total),12}{(partial ? "+" : " ")}  total");
        return 0;
    }

    static int MoleClean(bool dryRun, bool json)
    {
        // mole clean maps to the safe/moderate cache targets; it previews unless you drop --dry-run.
        var targets = Catalogue().Where(t => t.Level != "risky").ToList();
        var results = targets.Select(t =>
        {
            if (t.Name == "Recycle Bin") return (t, RecycleBinSize());
            var scan = new Scan();
            foreach (var p in t.Paths) { var m = Measure(p); scan.Bytes += m.Bytes; scan.Files += m.Files; scan.Partial |= m.Partial; }
            return (t, scan);
        }).Where(r => r.Item2.Bytes > 0).OrderByDescending(r => r.Item2.Bytes).ToList();
        var total = results.Sum(r => r.Item2.Bytes);
        if (json)
        {
            Console.WriteLine($"{{\"mode\":\"{(dryRun ? "dry-run" : "clean")}\",\"reclaimable\":{total},\"items\":[{string.Join(",", results.Select(r => $"{{\"name\":{PerfSampler.Json(r.Item1.Name)},\"size\":{r.Item2.Bytes}}}"))}]}}");
            return 0;
        }
        Console.WriteLine($"mole clean {(dryRun ? "(dry run)" : "")}\n");
        foreach (var (t, s) in results) Console.WriteLine($"{Human(s.Bytes),12}  {t.Name}");
        Console.WriteLine(new string('-', 36));
        Console.WriteLine($"{Human(total),12}  reclaimable");
        if (dryRun) { Console.WriteLine("\nDry run complete - no changes made"); return 0; }
        long freed = results.Sum(r => Clean(r.Item1).Bytes);
        Console.WriteLine($"\nMoved to Recycle Bin: {Human(freed)}");
        return 0;
    }

    static int MolePurge(string root, bool dryRun, bool json)
    {
        string[] artifactNames = ["node_modules", "target", ".build", "build", "dist", ".next", "out", "obj", "bin", ".gradle", "Pods", ".venv"];
        root = Path.GetFullPath(root);
        var found = new List<(string Path, Scan S)>();
        var stack = new Stack<(string Dir, int Depth)>();
        stack.Push((root, 0));
        while (stack.Count > 0)
        {
            var (dir, depth) = stack.Pop();
            if (depth > 6) continue;
            foreach (var sub in SafeEntries(dir).Where(Directory.Exists))
            {
                var name = Path.GetFileName(sub);
                if (artifactNames.Contains(name))
                {
                    // Skip anything touched in the last week, matching mole's default.
                    if (!RecentlyModified(sub)) found.Add((sub, Measure(sub)));
                }
                else if (name != ".git") stack.Push((sub, depth + 1));
            }
        }
        found.Sort((a, b) => b.S.Bytes.CompareTo(a.S.Bytes));
        var total = found.Sum(f => f.S.Bytes);
        if (json)
        {
            Console.WriteLine($"{{\"root\":{PerfSampler.Json(root)},\"reclaimable\":{total},\"artifacts\":[{string.Join(",", found.Select(f => $"{{\"path\":{PerfSampler.Json(f.Path)},\"size\":{f.S.Bytes}}}"))}]}}");
            return 0;
        }
        Console.WriteLine($"mole purge {root} {(dryRun ? "(dry run)" : "")}\n");
        foreach (var (p, s) in found.Take(40)) Console.WriteLine($"{Human(s.Bytes),12}  {Path.GetRelativePath(root, p)}");
        Console.WriteLine(new string('-', 40));
        Console.WriteLine($"{Human(total),12}  in {found.Count} build artifact folders");
        if (dryRun) { Console.WriteLine("\nDry run complete - no changes made (use --yes to delete)"); return 0; }
        long freed = 0;
        foreach (var (p, s) in found) if (Recycle(p)) freed += s.Bytes;
        Console.WriteLine($"\nMoved to Recycle Bin: {Human(freed)}");
        return 0;
    }

    static bool RecentlyModified(string dir)
    {
        try { return (DateTime.Now - Directory.GetLastWriteTime(dir)).TotalDays < 7; }
        catch (IOException) { return true; }
    }

    static void MoleHelp() => Console.WriteLine("""
        Usage: mole <command> [flags]   (also available as `mo`)

        Deep clean and inspect disk usage on Windows.

        Commands:
          status                 Read-only dashboard: CPU, memory, disk, power
          analyze [path]         Disk usage explorer for a directory (default: home)
          clean                  Clean safe/moderate caches (preview with --dry-run)
          purge [path]           Find rebuildable build artifacts (node_modules, target, dist, ...)
          --version, --help

        Flags:
          --dry-run              Preview without deleting (clean, purge)
          --yes                  Confirm deletion for purge
          --json                 Machine-readable output (status, analyze, clean, purge)

        Items are moved to the Recycle Bin, never deleted outright. macOS-only commands
        (optimize, uninstall, installer, touchid) report that they have no Windows equivalent.
        """);

    // ---- Recycle Bin (SHFileOperation / SHEmptyRecycleBin / SHQueryRecycleBin) ----

    static bool Recycle(string path)
    {
        var op = new SHFILEOPSTRUCT
        {
            wFunc = 3,   // FO_DELETE
            pFrom = path + "\0\0",
            fFlags = 0x0040 | 0x0010 | 0x0400 | 0x0004,   // ALLOWUNDO | NOCONFIRMATION | NOERRORUI | SILENT
        };
        return SHFileOperation(ref op) == 0 && !op.fAnyOperationsAborted;
    }

    static void EmptyRecycleBin() => SHEmptyRecycleBin(IntPtr.Zero, null, 1 | 2 | 4);

    static Scan RecycleBinSize()
    {
        var info = new SHQUERYRBINFO { cbSize = (uint)Marshal.SizeOf<SHQUERYRBINFO>() };
        return SHQueryRecycleBin(null, ref info) == 0
            ? new Scan { Bytes = info.i64Size, Files = (int)info.i64NumItems }
            : new Scan();
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        public uint wFunc;
        public string pFrom;
        public string pTo;
        public ushort fFlags;
        [MarshalAs(UnmanagedType.Bool)] public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        public string lpszProgressTitle;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SHQUERYRBINFO { public uint cbSize; public long i64Size; public long i64NumItems; }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHFileOperation(ref SHFILEOPSTRUCT op);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHEmptyRecycleBin(IntPtr hwnd, string root, uint flags);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHQueryRecycleBin(string root, ref SHQUERYRBINFO info);
}
