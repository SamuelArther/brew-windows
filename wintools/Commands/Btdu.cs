using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// btdu: a sampling disk usage profiler. Like btdu does for btrfs, it picks random clusters of the
// whole volume and asks the file system who owns each one, so after N samples every file and
// folder's share of the disk is known to about 1/sqrt(N), however many files there are and however
// they share or fragment their data. NTFS answers the "who owns cluster X" question directly with
// FSCTL_LOOKUP_STREAM_FROM_CLUSTER (free clusters come from the volume bitmap), so this is the same
// method on Windows. Needs administrator rights (it reads the volume). Instead of btdu's ncurses
// browser it samples until --max-samples/--max-time (or Ctrl+C) and prints the biggest entries as a
// tree; --export writes every sampled path as JSON.
static class Btdu
{
    const string Version = "0.6.1";

    [ModuleInitializer]
    internal static void Register() => Commands.Add("btdu", "Sampling disk usage profiler for NTFS volumes", Run);

    sealed class Node(string name)
    {
        public readonly string Name = name;
        public long Samples;
        public readonly Dictionary<string, Node> Children = new(StringComparer.OrdinalIgnoreCase);

        public Node Child(string name)
        {
            if (!Children.TryGetValue(name, out var c)) Children[name] = c = new Node(name);
            return c;
        }
    }

    static int Run(string[] args)
    {
        string path = null, export = null;
        long maxSamples = 0;
        double maxTime = 0;
        int? seed = null;
        int depth = 2, top = 10;
        var headless = false;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Value()
            {
                var eq = a.IndexOf('=');
                if (eq > 0) return a[(eq + 1)..];
                return ++i < args.Length ? args[i] : throw new UsageException($"{a} needs a value");
            }
            var key = a.Contains('=') ? a[..a.IndexOf('=')] : a;
            switch (key)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage: btdu [OPTION]... PATH
                        Sampling disk usage profiler for NTFS. PATH is any folder on the volume to profile.

                          --max-samples=N    stop after N samples
                          --max-time=SECONDS stop after this long (default 10 when neither is given)
                          --seed=N           random seed, for repeatable sampling
                          --depth=N          tree levels to print (default 2)
                          --top=N            entries to print per level (default 10)
                          --export=FILE      write all sampled paths with their estimated sizes as JSON
                          --headless         no progress line
                          --version          show the version

                        Ctrl+C stops sampling early and prints what was found. Run as administrator.
                        """);
                    return 0;
                case "--version": Console.WriteLine($"btdu v{Version}"); return 0;
                case "--max-samples": maxSamples = long.TryParse(Value(), out var n) && n > 0 ? n : throw new UsageException("--max-samples needs a positive number"); break;
                case "--max-time": maxTime = ParseTime(Value()); break;
                case "--seed": seed = int.TryParse(Value(), out var s) ? s : throw new UsageException("--seed needs a number"); break;
                case "--depth": depth = int.TryParse(Value(), out var d) && d >= 0 ? d : throw new UsageException("--depth needs a number"); break;
                case "--top": top = int.TryParse(Value(), out var t) && t > 0 ? t : throw new UsageException("--top needs a positive number"); break;
                case "--export": export = Value(); break;
                case "--headless": headless = true; break;
                case "-j" or "--procs" or "--min-resolution": Value(); break;
                case "-x" or "--one-filesystem" or "--expert" or "--physical" or "--du" or "--prefer-ignore": break;
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unrecognized option {a}");
                    if (path != null) throw new UsageException("only one path may be given");
                    path = a;
                    break;
            }
        }
        if (path == null)
        {
            Console.Error.WriteLine("Fatal error: No path specified");
            return 1;
        }
        if (!Directory.Exists(path)) { Console.Error.WriteLine($"Fatal error: {path}: no such directory"); return 1; }
        if (maxSamples == 0 && maxTime == 0) maxTime = 10;
        return Profile(path, maxSamples, maxTime, seed, depth, top, export, headless);
    }

    static double ParseTime(string s)
    {
        // Seconds, or a number with s/m/h.
        var unit = s.Length > 0 && char.IsLetter(s[^1]) ? s[^1] : 's';
        var num = char.IsLetter(s[^1]) ? s[..^1] : s;
        if (!double.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) || v <= 0)
            throw new UsageException($"bad duration: {s}");
        return unit switch { 's' => v, 'm' => v * 60, 'h' => v * 3600, _ => throw new UsageException($"bad duration: {s}") };
    }

    static int Profile(string path, long maxSamples, double maxTime, int? seed, int depth, int top, string export, bool headless)
    {
        var full = Path.GetFullPath(path);
        var volumeRoot = Path.GetPathRoot(full) ?? full;
        var fs = new DriveInfo(volumeRoot).DriveFormat;
        if (!fs.Equals("NTFS", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"Fatal error: {volumeRoot} is a {fs} volume; btdu needs NTFS (it asks NTFS which file owns each cluster)");
            return 1;
        }
        var device = @"\\.\" + volumeRoot.TrimEnd('\\');
        var h = CreateFileW(device, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
        if (h == new IntPtr(-1))
        {
            var err = Marshal.GetLastWin32Error();
            Console.Error.WriteLine(err == 5
                ? $"Fatal error: opening {device}: access denied (btdu reads the volume; run it as administrator)"
                : $"Fatal error: opening {device}: {new System.ComponentModel.Win32Exception(err).Message}");
            return 1;
        }
        try
        {
            var vol = new byte[128];
            if (!DeviceIoControl(h, FSCTL_GET_NTFS_VOLUME_DATA, null, 0, vol, vol.Length, out _, IntPtr.Zero))
            {
                Console.Error.WriteLine($"Fatal error: reading NTFS volume data: {new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message}");
                return 1;
            }
            var totalClusters = BitConverter.ToInt64(vol, 16);
            var bytesPerCluster = BitConverter.ToUInt32(vol, 44);
            var totalBytes = totalClusters * bytesPerCluster;
            var rng = seed is { } s ? new Random(s) : new Random();
            var tree = new Node(volumeRoot);
            var watch = Stopwatch.StartNew();
            var stop = false;
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop = true; };
            var bitmap = new byte[24];
            long samples = 0;
            // NTFS answers a cluster lookup by scanning the MFT, and one scan answers a whole batch of
            // clusters, so samples are drawn in batches that grow while the time allows.
            var batch = 4096;
            while (!stop && (maxSamples == 0 || samples < maxSamples) && (maxTime == 0 || watch.Elapsed.TotalSeconds < maxTime))
            {
                var n = maxSamples == 0 ? batch : (int)Math.Min(batch, maxSamples - samples);
                var lcns = new long[n];
                for (var i = 0; i < n; i++) lcns[i] = rng.NextInt64(totalClusters);
                var started = watch.Elapsed.TotalSeconds;
                foreach (var parts in Owners(h, lcns, bitmap))
                {
                    tree.Samples++;
                    samples++;
                    var node = tree;
                    foreach (var part in parts)
                    {
                        node = node.Child(part);
                        node.Samples++;
                    }
                }
                var took = watch.Elapsed.TotalSeconds - started;
                if (batch < 1 << 16) batch *= 2;
                if (maxTime > 0 && took > 0)
                {
                    // Don't start a batch that would run far past --max-time.
                    var left = maxTime - watch.Elapsed.TotalSeconds;
                    if (left < took * 0.5) break;
                }
                if (!headless && !Console.IsErrorRedirected)
                    Console.Error.Write($"\rbtdu: {samples} samples, resolution {Size(totalBytes / Math.Sqrt(samples))}   ");
            }
            if (!headless && !Console.IsErrorRedirected) Console.Error.Write("\r" + new string(' ', 60) + "\r");
            Console.WriteLine($"{volumeRoot} ({fs}, {Size(totalBytes)}, {bytesPerCluster}-byte clusters): {samples} samples, resolution +/-{Size(totalBytes / Math.Sqrt(Math.Max(1, samples)))}");
            Print(tree, totalBytes, samples, depth, top, 0);
            if (export != null) File.WriteAllText(export, Export(tree, totalBytes, samples, volumeRoot));
            return 0;
        }
        finally { CloseHandle(h); }
    }

    // For each cluster, the path components of its owner: <UNUSED> for free clusters, <SYSTEM> for
    // NTFS metadata, the file's folders and name otherwise.
    static List<List<string>> Owners(IntPtr h, long[] lcns, byte[] bitmap)
    {
        var result = new List<string>[lcns.Length];
        var used = new List<int>();
        for (var i = 0; i < lcns.Length; i++)
        {
            if (Free(h, lcns[i], bitmap)) result[i] = ["<UNUSED>"];
            else used.Add(i);
        }
        if (used.Count > 0)
        {
            // LOOKUP_STREAM_FROM_CLUSTER_INPUT: Flags, NumberOfClusters, Cluster[].
            var input = new byte[8 + 8 * used.Count];
            BitConverter.TryWriteBytes(input.AsSpan(4, 4), used.Count);
            for (var k = 0; k < used.Count; k++) BitConverter.TryWriteBytes(input.AsSpan(8 + 8 * k, 8), lcns[used[k]]);
            var output = new byte[1 << 20];
            var ok = DeviceIoControl(h, FSCTL_LOOKUP_STREAM_FROM_CLUSTER, input, input.Length, output, output.Length, out _, IntPtr.Zero);
            if (!ok && Marshal.GetLastWin32Error() == ERROR_MORE_DATA)
            {
                output = new byte[BitConverter.ToUInt32(output, 8) + 64];   // BufferSizeRequired
                ok = DeviceIoControl(h, FSCTL_LOOKUP_STREAM_FROM_CLUSTER, input, input.Length, output, output.Length, out _, IntPtr.Zero);
            }
            var owners = new Dictionary<long, List<string>>();
            if (ok)
            {
                // Entries: OffsetToNext, Flags, Reserved, Cluster, FileName (NUL-terminated).
                var matches = BitConverter.ToUInt32(output, 4);
                var entry = (int)BitConverter.ToUInt32(output, 0);
                for (var m = 0; m < matches && entry > 0 && entry + 24 < output.Length; m++)
                {
                    var flags = BitConverter.ToUInt32(output, entry + 4);
                    var cluster = BitConverter.ToInt64(output, entry + 16);
                    var end = entry + 24;
                    while (end + 1 < output.Length && (output[end] | output[end + 1]) != 0) end += 2;
                    owners.TryAdd(cluster, Parts(Encoding.Unicode.GetString(output, entry + 24, end - entry - 24), flags));
                    var next = (int)BitConverter.ToUInt32(output, entry);
                    if (next == 0) break;
                    entry += next;
                }
            }
            foreach (var i in used)
                result[i] = !ok ? ["<ERROR>"] : owners.TryGetValue(lcns[i], out var p) ? p : ["<UNREACHABLE>"];
        }
        return result.ToList();
    }

    // NTFS's own metadata files in the volume root.
    static readonly HashSet<string> MetaFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "$Mft", "$MftMirr", "$LogFile", "$Volume", "$AttrDef", "$Bitmap", "$Boot", "$BadClus", "$Secure", "$UpCase", "$Extend",
    };

    // "\dir\file:stream:$DATA" -> dir, file (a named stream is a child of its file).
    static List<string> Parts(string name, uint flags)
    {
        var colon = name.IndexOf(':', 1);
        string stream = null;
        if (colon > 0)
        {
            var streamName = name[(colon + 1)..].Split(':')[0];
            name = name[..colon];
            if (streamName.Length > 0) stream = ":" + streamName;
        }
        var parts = name.Split('\\', StringSplitOptions.RemoveEmptyEntries).ToList();
        if ((flags & 0x4) != 0 || parts.Count == 1 && MetaFiles.Contains(parts[0]) || parts.Count > 0 && parts[0] == "$Extend") parts.Insert(0, "<SYSTEM>");
        if (stream != null) parts.Add(stream);
        if (parts.Count == 0) parts.Add("<UNREACHABLE>");
        return parts;
    }

    // The volume bitmap bit for lcn (STARTING_LCN_INPUT_BUFFER -> VOLUME_BITMAP_BUFFER).
    static bool Free(IntPtr h, long lcn, byte[] bitmap)
    {
        var start = new byte[8];
        BitConverter.TryWriteBytes(start, lcn & ~7L);
        if (!DeviceIoControl(h, FSCTL_GET_VOLUME_BITMAP, start, 8, bitmap, bitmap.Length, out _, IntPtr.Zero)
            && Marshal.GetLastWin32Error() != ERROR_MORE_DATA) return false;
        var bit = lcn - BitConverter.ToInt64(bitmap, 0);
        return bit is >= 0 and < 64 && (bitmap[16 + bit / 8] & (1 << (int)(bit % 8))) == 0;
    }

    static void Print(Node node, long totalBytes, long samples, int depth, int top, int level)
    {
        if (level >= depth || samples == 0) return;
        var children = node.Children.Values.OrderByDescending(c => c.Samples).ToList();
        foreach (var c in children.Take(top))
        {
            var bytes = (double)c.Samples / samples * totalBytes;
            Console.WriteLine($"{new string(' ', level * 2)}{Size(bytes),10} {100.0 * c.Samples / samples,5:0.0}%  {c.Name}");
            Print(c, totalBytes, samples, depth, top, level + 1);
        }
        if (children.Count > top)
        {
            var rest = children.Skip(top).Sum(c => c.Samples);
            Console.WriteLine($"{new string(' ', level * 2)}{Size((double)rest / samples * totalBytes),10} {100.0 * rest / samples,5:0.0}%  ({children.Count - top} more)");
        }
    }

    static string Size(double bytes)
    {
        string[] units = ["B", "KiB", "MiB", "GiB", "TiB", "PiB"];
        var u = 0;
        while (bytes >= 1024 && u < units.Length - 1) { bytes /= 1024; u++; }
        return u == 0 ? $"{bytes:0} B" : string.Create(CultureInfo.InvariantCulture, $"{bytes:0.0} {units[u]}");
    }

    static string Export(Node tree, long totalBytes, long samples, string root)
    {
        var sb = new StringBuilder();
        sb.Append("{\"fsPath\":").Append(Json(root)).Append(",\"totalSize\":").Append(totalBytes).Append(",\"samples\":").Append(samples).Append(",\"root\":");
        Write(tree);
        sb.Append("}\n");
        return sb.ToString();

        void Write(Node n)
        {
            var size = samples == 0 ? 0 : (long)((double)n.Samples / samples * totalBytes);
            sb.Append("{\"name\":").Append(Json(n.Name)).Append(",\"samples\":").Append(n.Samples).Append(",\"size\":").Append(size);
            if (n.Children.Count > 0)
            {
                sb.Append(",\"children\":[");
                var first = true;
                foreach (var c in n.Children.Values.OrderByDescending(c => c.Samples))
                {
                    if (!first) sb.Append(',');
                    Write(c);
                    first = false;
                }
                sb.Append(']');
            }
            sb.Append('}');
        }
    }

    static string Json(string s)
    {
        var sb = new StringBuilder("\"");
        foreach (var c in s)
            sb.Append(c switch
            {
                '"' => "\\\"",
                '\\' => "\\\\",
                < ' ' => $"\\u{(int)c:x4}",
                _ => c.ToString(),
            });
        return sb.Append('"').ToString();
    }

    const uint GENERIC_READ = 0x80000000, FILE_SHARE_READ = 1, FILE_SHARE_WRITE = 2, OPEN_EXISTING = 3;
    const uint FSCTL_GET_NTFS_VOLUME_DATA = 0x00090064, FSCTL_GET_VOLUME_BITMAP = 0x0009006F, FSCTL_LOOKUP_STREAM_FROM_CLUSTER = 0x000901FC;
    const int ERROR_MORE_DATA = 234;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr CreateFileW(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool DeviceIoControl(IntPtr h, uint code, byte[] input, int inputSize, byte[] output, int outputSize, out int returned, IntPtr overlapped);

    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr h);
}
