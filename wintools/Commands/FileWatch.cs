using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace WinTools;

// fsevent_watch / notifywait + notifyloop (fsevents-tools) / kqwait: file watching through
// ReadDirectoryChangesW (FileSystemWatcher). Events carry FSEvents-style flags so the output formats
// match the macOS tools.
static class FileWatch
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("fsevent_watch", "Stream file system change events for paths (FSEvents-style output)", FseventWatch);
        Commands.Add("notifywait", "Wait until something under the given paths changes", NotifyWait);
        Commands.Add("notifyloop", "Run a command every time something under a path changes", NotifyLoop);
        Commands.Add("kqwait", "Wait for a change to one of the given files or directories", KqWait);
    }

    // FSEventStreamEventFlags values.
    const uint MustScanSubDirs = 0x1, RootChanged = 0x20, ItemCreated = 0x100, ItemRemoved = 0x200, ItemInodeMetaMod = 0x400,
        ItemRenamed = 0x800, ItemModified = 0x1000, ItemIsFile = 0x10000, ItemIsDir = 0x20000;

    static readonly (uint Flag, string Name)[] FlagNames =
    [
        (MustScanSubDirs, "MustScanSubDirs"), (RootChanged, "RootChanged"), (ItemCreated, "ItemCreated"), (ItemRemoved, "ItemRemoved"),
        (ItemInodeMetaMod, "ItemInodeMetaMod"), (ItemRenamed, "ItemRenamed"), (ItemModified, "ItemModified"),
        (ItemIsFile, "ItemIsFile"), (ItemIsDir, "ItemIsDir"),
    ];

    internal readonly record struct Change(ulong Id, string Path, uint Flags);

    static ulong nextId = (ulong)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond);

    // Watches files and directories (a file is watched through its parent, filtered to its name) and
    // queues their changes. Overflowed buffers come through as MustScanSubDirs on the root.
    sealed class Watch : IDisposable
    {
        public readonly BlockingCollection<Change> Queue = new();
        readonly List<FileSystemWatcher> watchers = [];

        public Watch(IEnumerable<string> paths, bool recursive)
        {
            foreach (var path in paths)
            {
                FileSystemWatcher w;
                if (Directory.Exists(path)) w = new FileSystemWatcher(path) { IncludeSubdirectories = recursive };
                else if (File.Exists(path)) w = new FileSystemWatcher(Path.GetDirectoryName(path)!, Path.GetFileName(path));
                else throw new IOException($"{path}: No such file or directory");
                w.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size
                    | NotifyFilters.Attributes | NotifyFilters.Security | NotifyFilters.CreationTime;
                w.InternalBufferSize = 64 * 1024;
                w.Created += (_, e) => Add(e.FullPath, ItemCreated);
                w.Deleted += (_, e) => Add(e.FullPath, ItemRemoved);
                w.Changed += (_, e) => Add(e.FullPath, ItemModified);
                w.Renamed += (_, e) => { Add(e.OldFullPath, ItemRenamed); Add(e.FullPath, ItemRenamed); };
                var root = path;
                w.Error += (_, _) => Add(root, MustScanSubDirs);
                w.EnableRaisingEvents = true;
                watchers.Add(w);
            }
        }

        void Add(string path, uint flags)
        {
            if (Directory.Exists(path)) flags |= ItemIsDir;
            else if (File.Exists(path) || (flags & (ItemRemoved | ItemRenamed)) != 0) flags |= ItemIsFile;
            Queue.Add(new Change(Interlocked.Increment(ref nextId), path, flags));
        }

        public void Dispose()
        {
            foreach (var w in watchers) w.Dispose();
        }
    }

    // ---- fsevent_watch ----

    static int FseventWatch(string[] args)
    {
        double latency = 0.3;
        bool noDefer = false, watchRoot = false, fileEvents = false;
        string format = "classic", since = null;
        var paths = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            var eq = arg.IndexOf('=');
            var name = arg.StartsWith("--") && eq > 0 ? arg[..eq] : arg;
            string Value() => arg.StartsWith("--") && eq > 0 ? arg[(eq + 1)..] : ++i < args.Length ? args[i] : throw new UsageException($"{name} needs a value");
            switch (name)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage: fsevent_watch [OPTIONS] [paths]
                          -h, --help                  you're looking at it
                          -V, --version               print version and exit
                          -s, --since-when=EventID    fire historical events since ID (not kept on Windows; ignored)
                          -l, --latency=seconds       latency period (default='0.3')
                          -n, --no-defer              deliver the first event of a burst at once
                          -r, --watch-root            report a RootChanged event when a watched root goes away
                          -F, --file-events           report individual files rather than directories
                          --ignore-self, --mark-self  accepted for compatibility
                          -f, --format=name           output format: classic (default), niw, tnetstring, otnetstring
                        Watches the current directory if no paths are given.
                        """);
                    return 0;
                case "-V" or "-v" or "--version": Console.WriteLine($"fsevent_watch {Commands.Version} (wintools)"); return 0;
                case "-s" or "--since-when": since = Value(); break;
                case "-l" or "--latency":
                    if (!double.TryParse(Value(), System.Globalization.CultureInfo.InvariantCulture, out latency) || latency < 0) throw new UsageException("invalid latency");
                    break;
                case "-n" or "--no-defer": noDefer = true; break;
                case "-r" or "--watch-root": watchRoot = true; break;
                case "-F" or "--file-events": fileEvents = true; break;
                case "--ignore-self" or "--mark-self": break;
                case "-f" or "--format":
                    format = Value();
                    if (format is not ("classic" or "niw" or "tnetstring" or "otnetstring")) throw new UsageException($"unknown format {format}");
                    break;
                default:
                    if (arg.StartsWith('-')) throw new UsageException($"unknown option {arg}");
                    paths.Add(Path.GetFullPath(arg));
                    break;
            }
        }
        if (since != null && since != "now" && since != "0") Console.Error.WriteLine("fsevent_watch: Windows keeps no event history; watching from now");
        if (paths.Count == 0) paths.Add(Directory.GetCurrentDirectory());
        using var watch = new Watch(paths, recursive: true);
        using var stdout = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = false };
        var roots = paths.ToDictionary(p => p, p => true);
        var batch = new List<Change>();
        while (true)
        {
            // Collect one batch: block for the first event, then gather for the latency period.
            var first = watch.Queue.Take();
            batch.Clear();
            batch.Add(first);
            if (!noDefer)
            {
                var deadline = Stopwatch.StartNew();
                while (deadline.Elapsed.TotalSeconds < latency && watch.Queue.TryTake(out var more, TimeSpan.FromSeconds(latency) - deadline.Elapsed)) batch.Add(more);
            }
            while (watch.Queue.TryTake(out var extra)) batch.Add(extra);
            if (watchRoot)
                foreach (var root in roots.Keys.ToList())
                {
                    var exists = Directory.Exists(root) || File.Exists(root);
                    if (exists != roots[root]) { roots[root] = exists; batch.Add(new Change(Interlocked.Increment(ref nextId), root, RootChanged)); }
                }
            var events = fileEvents ? batch : batch.Select(c => (c.Flags & RootChanged) != 0 ? c
                : c with { Path = ((c.Flags & ItemIsDir) != 0 && (c.Flags & (ItemCreated | ItemRemoved | ItemRenamed)) == 0 ? c.Path : Path.GetDirectoryName(c.Path)) + Path.DirectorySeparatorChar })
                .GroupBy(c => c.Path, StringComparer.OrdinalIgnoreCase)
                // Directory-level streams carry no item flags, only the stream-level ones.
                .Select(g => g.Last() with { Flags = g.Aggregate(0u, (f, c) => f | c.Flags) & (MustScanSubDirs | RootChanged) }).ToList();
            stdout.Write(FormatBatch(format, events.ToList()));
            stdout.Flush();
        }
    }

    internal static string FormatBatch(string format, List<Change> events)
    {
        var sb = new StringBuilder();
        switch (format)
        {
            case "classic":
                foreach (var e in events) sb.Append(e.Path).Append(':');
                sb.Append('\n');
                break;
            case "niw":
                foreach (var e in events) sb.Append($"{e.Flags}:{e.Id}:{e.Path}\n");
                sb.Append('\n');
                break;
            default:
                // [ {id, path, cflags, flags[]} ... ] as a tagged netstring.
                var list = new StringBuilder();
                foreach (var e in events)
                {
                    var flags = string.Concat(FlagNames.Where(f => (e.Flags & f.Flag) != 0).Select(f => Tnet(f.Name, ',')));
                    var dict = Tnet("id", ',') + Tnet(e.Id.ToString(), '#') + Tnet("path", ',') + Tnet(e.Path, ',')
                        + Tnet("cflags", ',') + Tnet(e.Flags.ToString(), '#') + Tnet("flags", ',') + Tnet(flags, ']');
                    list.Append(Tnet(dict, '}'));
                }
                sb.Append(Tnet(list.ToString(), ']'));
                break;
        }
        return sb.ToString();

        static string Tnet(string payload, char tag) => $"{Encoding.UTF8.GetByteCount(payload)}:{payload}{tag}";
    }

    // ---- notifywait / notifyloop ----

    static int NotifyWait(string[] args)
    {
        if (args.Length == 0) throw new UsageException("Usage: notifywait path [path ...]");
        if (args[0] is "-h" or "--help")
        {
            Console.WriteLine("Usage: notifywait path [path ...]\nBlocks until something under one of the paths changes, then prints the change and exits.");
            return 0;
        }
        if (args[0] is "-v" or "--version") { Console.WriteLine($"notifywait {Commands.Version} (wintools)"); return 0; }
        var paths = args.Select(Path.GetFullPath).ToList();
        using var watch = new Watch(paths, recursive: true);
        foreach (var p in paths) Console.WriteLine($"Watching {p}");
        Console.Out.Flush();
        var change = watch.Queue.Take();
        Console.WriteLine($"Change {change.Id} in {change.Path}, flags {change.Flags}");
        return 0;
    }

    static int NotifyLoop(string[] args)
    {
        if (args.Length > 0 && args[0] is "-h" or "--help")
        {
            Console.WriteLine("Usage: notifyloop path command [args ...]\nRuns command every time something under path changes (bursts of changes run it once).");
            return 0;
        }
        if (args.Length < 2) throw new UsageException("Usage: notifyloop path command [args ...]");
        using var watch = new Watch([Path.GetFullPath(args[0])], recursive: true);
        Console.Error.WriteLine($"Watching {Path.GetFullPath(args[0])}");
        while (true)
        {
            watch.Queue.Take();
            // Let a burst of writes settle, and drop whatever the command itself changes.
            Thread.Sleep(100);
            while (watch.Queue.TryTake(out _)) { }
            var psi = new ProcessStartInfo(args[1]) { UseShellExecute = false };
            foreach (var a in args[2..]) psi.ArgumentList.Add(a);
            using (var p = Process.Start(psi)) p.WaitForExit();
            Thread.Sleep(50);
            while (watch.Queue.TryTake(out _)) { }
        }
    }

    // ---- kqwait ----

    static int KqWait(string[] args)
    {
        var paths = new List<string>();
        foreach (var arg in args)
        {
            switch (arg)
            {
                case "-h" or "--help":
                    Console.WriteLine("Usage: kqwait [-v] <file|dir> [...]\nWaits until one of the files (or a directory's entries) changes, prints its path and exits.");
                    return 0;
                case "-v" or "--version": Console.WriteLine($"kqwait {Commands.Version} (wintools)"); return 0;
                default:
                    if (arg.StartsWith('-')) throw new UsageException($"unknown option {arg}");
                    paths.Add(Path.GetFullPath(arg));
                    break;
            }
        }
        if (paths.Count == 0) throw new UsageException("Usage: kqwait [-v] <file|dir> [...]");
        // kqueue watches a directory's own entries, not its whole tree.
        using var watch = new Watch(paths, recursive: false);
        var change = watch.Queue.Take();
        Console.WriteLine(change.Path);
        return 0;
    }
}
