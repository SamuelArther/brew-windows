using System.Runtime.CompilerServices;

namespace WinTools;

// fuse-overlayfs: an overlay file system on the Projected File System. The lower folders are merged
// (the first one listed wins) and projected into the upper folder, which ProjFS turns into the
// overlay's writable layer: writing to a lower file copies it up, new files land there, and deletes
// leave tombstones that hide the lower copy (overlayfs's whiteouts). The mount point is a junction
// to upperdir, so the changes end up exactly where overlayfs would put them. workdir is accepted
// but unused (ProjFS needs no scratch space). Without upperdir the mount is read-only and the mount
// point itself is the root. `fuse-overlayfs -u <mountpoint>` unmounts.
static class FuseOverlayfs
{
    const string Version = "1.18";

    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("fuse-overlayfs", "Overlay file system: merge folders, writes go to an upper folder (ProjFS)", Run);

    const string Help = """
        usage: fuse-overlayfs [-f] [-d] -o lowerdir=DIR[:DIR...][,upperdir=DIR,workdir=DIR] mountpoint
               fuse-overlayfs -u mountpoint

        Merges the lower folders (the first one is on top) at mountpoint. With upperdir, every change
        (copied-up files, new files, deletions) is kept in upperdir; without it the mount is read-only.
        Runs in the background until unmounted with -u (-f stays in the foreground; Ctrl+C unmounts).

        options:
            -h, --help          print help
            -V, --version       print version
            -f                  foreground operation
            -d                  debug (implies -f)
            -o lowerdir=        colon-separated lower folders (C:\ drive letters are understood)
            -o upperdir=        the writable layer
            -o workdir=         accepted for compatibility (not needed on Windows)
            -u, --unmount       unmount mountpoint
        Needs the Windows Projected File System (the Client-ProjFS optional feature).
        """;

    static int Run(string[] args)
    {
        var opts = new List<string>();
        var positional = new List<string>();
        var foreground = false;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            switch (a)
            {
                case "-h" or "--help": Console.WriteLine(Help); return 0;
                case "-V" or "--version":
                    Console.WriteLine($"fuse-overlayfs: version {Version}\nFUSE library version: ProjFS (Windows Projected File System)");
                    return 0;
                case "-u" or "--unmount":
                    if (++i >= args.Length) throw new UsageException("-u needs a mount point");
                    return Projfs.Unmount("fuse-overlayfs", args[i]);
                case "-f" or "-d" or "--debug": foreground = true; break;
                case "-o":
                    if (++i >= args.Length) throw new UsageException("-o needs options");
                    opts.Add(args[i]);
                    break;
                default:
                    if (a.StartsWith("-o") && a.Length > 2) opts.Add(a[2..]);
                    else if (a.StartsWith('-')) throw new UsageException($"unknown option {a}");
                    else positional.Add(a);
                    break;
            }
        }
        var options = new Dictionary<string, string>();
        foreach (var o in opts.SelectMany(SplitOptions))
        {
            var eq = o.IndexOf('=');
            options[eq < 0 ? o : o[..eq]] = eq < 0 ? "" : o[(eq + 1)..];
        }
        if (positional.Count != 1) throw new UsageException("no mountpoint specified (see fuse-overlayfs --help)");
        if (!options.TryGetValue("lowerdir", out var lowerdir) || lowerdir.Length == 0)
        {
            Console.Error.WriteLine("fuse-overlayfs: no lowerdir specified");
            return 1;
        }
        var lowers = SplitPaths(lowerdir).Select(Path.GetFullPath).ToList();
        foreach (var l in lowers)
            if (!Directory.Exists(l)) { Console.Error.WriteLine($"fuse-overlayfs: cannot open lowerdir {l}: No such file or directory"); return 1; }
        var upper = options.TryGetValue("upperdir", out var u) && u.Length > 0 ? Path.GetFullPath(u) : null;
        if (upper != null && !options.ContainsKey("workdir")) { Console.Error.WriteLine("fuse-overlayfs: workdir not specified"); return 1; }
        if (upper != null && !Directory.Exists(upper)) { Console.Error.WriteLine($"fuse-overlayfs: cannot open upperdir {upper}: No such file or directory"); return 1; }
        var mountpoint = Projfs.MountPath(positional[0]);
        if (!Directory.Exists(mountpoint)) { Console.Error.WriteLine($"fuse-overlayfs: cannot mount: {positional[0]}: No such file or directory"); return 1; }
        if (upper == null && Directory.EnumerateFileSystemEntries(mountpoint).Any())
        { Console.Error.WriteLine($"fuse-overlayfs: mountpoint is not empty: {mountpoint}"); return 1; }

        var childArgs = args.Where(a => a is not ("-f" or "-d" or "--debug")).ToArray();
        return Projfs.Mount("fuse-overlayfs", mountpoint, upper ?? mountpoint, junction: upper != null, foreground, childArgs,
                            () => new OverlaySource(lowers, readOnly: upper == null));
    }

    // -o values: commas separate options (a "\," keeps a comma in a path).
    static IEnumerable<string> SplitOptions(string text) =>
        System.Text.RegularExpressions.Regex.Split(text, @"(?<!\\),").Select(s => s.Replace("\\,", ",")).Where(s => s.Length > 0);

    // "a:b:C:\x\y:/d" -> a, b, C:\x\y, /d: a single letter before ':' plus a slash is a drive.
    internal static List<string> SplitPaths(string text)
    {
        var list = new List<string>();
        var current = new System.Text.StringBuilder();
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == ':' && current.Length == 1 && char.IsLetter(current[0]) && i + 1 < text.Length && text[i + 1] is '\\' or '/')
            { current.Append(c); continue; }
            if (c == ':') { if (current.Length > 0) list.Add(current.ToString()); current.Clear(); continue; }
            current.Append(c);
        }
        if (current.Length > 0) list.Add(current.ToString());
        return list;
    }

    // The merged view of the lower folders.
    sealed class OverlaySource(List<string> layers, bool readOnly) : ProjfsSource
    {
        public override bool ReadOnly => readOnly;

        public override List<ProjfsEntry> List(string dir)
        {
            var seen = new Dictionary<string, ProjfsEntry>(StringComparer.OrdinalIgnoreCase);
            var anyDir = false;
            foreach (var layer in layers)
            {
                var full = Path.Combine(layer, dir);
                if (Directory.Exists(full))
                {
                    anyDir = true;
                    foreach (var info in new DirectoryInfo(full).EnumerateFileSystemInfos())
                        if (!seen.ContainsKey(info.Name)) seen[info.Name] = Entry(info);
                }
                else if (File.Exists(full)) break;   // a file on top hides the folders below
            }
            return anyDir ? seen.Values.ToList() : null;
        }

        public override ProjfsEntry Stat(string path)
        {
            foreach (var layer in layers)
            {
                var full = Path.Combine(layer, path);
                if (Directory.Exists(full)) return Entry(new DirectoryInfo(full));
                if (File.Exists(full)) return Entry(new FileInfo(full));
            }
            return null;
        }

        public override int Read(string path, long offset, Span<byte> buffer)
        {
            foreach (var layer in layers)
            {
                var full = Path.Combine(layer, path);
                if (!File.Exists(full)) continue;
                using var file = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                file.Position = offset;
                var total = 0;
                while (total < buffer.Length)
                {
                    var n = file.Read(buffer[total..]);
                    if (n == 0) break;
                    total += n;
                }
                return total;
            }
            return 0;
        }

        static ProjfsEntry Entry(FileSystemInfo info) =>
            new(info.Name, info is DirectoryInfo, info is FileInfo f ? f.Length : 0, info.LastWriteTimeUtc);
    }
}
