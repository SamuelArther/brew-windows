using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WinTools;

// dmsetup (device-mapper): on Linux, every logical volume is a device-mapper device whose table maps
// its sectors onto extents of other block devices. Windows' volume manager does the same job: each
// volume (\\?\Volume{GUID}) is a list of extents on physical disks (basic partitions, spanned or
// striped dynamic volumes, Storage Spaces virtual disks show up as their own disk). dmsetup's query
// commands read that mapping: ls, info, table (as linear targets over PhysicalDriveN), status, deps,
// targets and version. Creating, reloading, suspending or removing mappings needs a kernel driver;
// on Windows that is Disk Management, diskpart or Storage Spaces, and those commands say so.
static class Dmsetup
{
    const string LibraryVersion = "1.02.208 (wintools)";

    [ModuleInitializer]
    internal static void Register() => Commands.Add("dmsetup", "Show the volume-to-disk mappings of Windows volumes (device-mapper)", Run);

    sealed record Extent(uint Disk, long Offset, long Length);

    sealed class Volume
    {
        public string Guid;           // \\?\Volume{...}\
        public string Name;           // Volume{...}
        public List<string> Paths = [];
        public List<Extent> Extents = [];
        public long Sectors => Extents.Sum(e => e.Length) / 512;
    }

    static int Run(string[] args)
    {
        var opts = new HashSet<string>();
        string target = null, columns = null;
        var rest = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            switch (a)
            {
                case "-h" or "--help" or "help": Usage(Console.Out); return 0;
                case "--version": rest.Insert(0, "version"); break;
                case "-c" or "-C" or "--columns": opts.Add("columns"); break;
                case "--noheadings": opts.Add("noheadings"); break;
                case "--tree": opts.Add("tree"); break;
                case "--target": target = ++i < args.Length ? args[i] : throw new UsageException("--target needs a type"); break;
                case "-o" or "--options": columns = ++i < args.Length ? args[i] : throw new UsageException("-o needs a value"); break;
                case "-v" or "--verbose" or "--showkeys" or "--noudevsync" or "--noudevrules" or "-y" or "--yes"
                    or "--readonly" or "-r" or "--noflush" or "--nolockfs" or "--inactive" or "--exec": break;
                case "--table" or "-u" or "--uuid" or "-j" or "-m" or "--major" or "--minor" or "--sort" or "--separator" or "--count": i++; break;
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unrecognized option '{a}'");
                    rest.Add(a);
                    break;
            }
        }
        if (rest.Count == 0) { Usage(Console.Error); return 2; }
        var cmd = rest[0];
        var device = rest.Count > 1 ? rest[1] : null;
        switch (cmd)
        {
            case "version":
                Console.WriteLine($"Library version:   {LibraryVersion}");
                Console.WriteLine($"Driver version:    Windows volume manager {Environment.OSVersion.Version}");
                return 0;
            case "targets":
                Console.WriteLine("linear           v1.4.0");
                Console.WriteLine("striped          v1.6.0");
                Console.WriteLine("mirror           v1.14.0");
                return 0;
            case "ls": return Ls(target, opts.Contains("tree"));
            case "info":
            {
                var cols = opts.Contains("columns");
                var headings = !opts.Contains("noheadings");
                return Each(device, (v, first) =>
                {
                    if (!first && !cols) Console.WriteLine();
                    Info(v, cols, headings && first, columns);
                });
            }
            case "table": return Each(device, (v, _) => Table(v, device == null));
            case "status": return Each(device, (v, _) => Status(v, device == null));
            case "deps": return Each(device, (v, _) => Deps(v, device == null));
            case "splitname":
                if (device == null) throw new UsageException("splitname needs a device name");
                var parts = device.Split('-', 3);
                Console.WriteLine($"VG:{parts[0]} LV:{(parts.Length > 1 ? parts[1] : "")} LVLayer:{(parts.Length > 2 ? parts[2] : "")}");
                return 0;
            case "mknodes" or "udevcomplete_all" or "udevcookies" or "udevflags" or "mangle": return 0;   // no /dev or udev on Windows
            case "create" or "remove" or "remove_all" or "suspend" or "resume" or "load" or "reload" or "clear"
                or "rename" or "message" or "wipe_table" or "setgeometry" or "wait":
                Console.Error.WriteLine($"dmsetup: {cmd}: Windows volumes are composed by its volume manager, not by loaded tables; " +
                                        "use Disk Management, diskpart or Storage Spaces (New-VirtualDisk)");
                return 1;
            default:
                throw new UsageException($"unknown command: {cmd}");
        }
    }

    static void Usage(TextWriter w) => w.Write("""
        Usage:

        dmsetup
                [--version] [-h|--help [-c|-C|--columns]]
                [-v|--verbose [-v|--verbose ...]] [--noheadings] [-o <fields>]

                ls [--target <target_type>] [--tree]
                info [-c|-C|--columns] [--noheadings] [-o <fields>] [<device>]
                table [<device>]
                status [<device>]
                deps [<device>]
                targets
                version
                splitname <device> [<subsystem>]

        <device> is a volume name (Volume{GUID}), its \\?\ path or a drive letter (C:).
        create, reload, suspend, resume, remove and message need a device-mapper kernel driver;
        Windows composes volumes with Disk Management, diskpart or Storage Spaces instead.

        """);

    // Runs show for the named device, or for every volume (show's second argument: is it the first).
    static int Each(string device, Action<Volume, bool> show)
    {
        var volumes = Volumes();
        if (device != null)
        {
            var v = Find(volumes, device);
            if (v == null) { Console.Error.WriteLine($"Device {device} not found"); Console.Error.WriteLine("Command failed."); return 1; }
            show(v, true);
            return 0;
        }
        if (volumes.Count == 0) { Console.WriteLine("No devices found"); return 0; }
        for (var i = 0; i < volumes.Count; i++) show(volumes[i], i == 0);
        return 0;
    }

    static Volume Find(List<Volume> volumes, string device)
    {
        var d = device.TrimEnd('\\');
        return volumes.FirstOrDefault(v =>
            v.Name.Equals(d, StringComparison.OrdinalIgnoreCase) ||
            v.Guid.TrimEnd('\\').Equals(d, StringComparison.OrdinalIgnoreCase) ||
            v.Paths.Any(p => p.TrimEnd('\\').Equals(d, StringComparison.OrdinalIgnoreCase) ||
                             p.TrimEnd('\\').Equals(d + ":", StringComparison.OrdinalIgnoreCase)));
    }

    static int Ls(string target, bool tree)
    {
        var volumes = Volumes();
        if (target != null && target != "linear") { Console.WriteLine("No devices found"); return 0; }
        if (volumes.Count == 0) { Console.WriteLine("No devices found"); return 0; }
        var i = 0;
        foreach (var v in volumes)
        {
            var label = v.Paths.Count > 0 ? $"{v.Name} [{string.Join(", ", v.Paths)}]" : v.Name;
            if (!tree) { Console.WriteLine($"{label}\t(253:{i++})"); continue; }
            Console.WriteLine($"{label} (253:{i++})");
            var disks = v.Extents.Select(e => e.Disk).Distinct().ToList();
            for (var k = 0; k < disks.Count; k++)
                Console.WriteLine($" {(k == disks.Count - 1 ? "`-" : "|-")} (PhysicalDrive{disks[k]})");
        }
        return 0;
    }

    static void Info(Volume v, bool columns, bool headings, string fields)
    {
        var index = Volumes().FindIndex(x => x.Guid == v.Guid);
        if (columns)
        {
            var cols = (fields ?? "name,major,minor,attr,open,segments,events,uuid").Split(',');
            if (headings) Console.WriteLine(string.Join(" ", cols.Select(Heading)));
            Console.WriteLine(string.Join(" ", cols.Select(c => c switch
            {
                "name" => v.Name,
                "major" or "maj" => "253",
                "minor" or "min" => index.ToString(),
                "attr" => "L--w",
                "open" => "0",
                "segments" or "targ" => v.Extents.Count.ToString(),
                "events" or "event" => "0",
                "uuid" => v.Guid,
                "blkdevname" => v.Paths.FirstOrDefault() ?? v.Name,
                _ => throw new UsageException($"unrecognised field: {c}"),
            })));
            return;
        }
        Console.WriteLine($"Name:              {v.Name}");
        Console.WriteLine("State:             ACTIVE");
        Console.WriteLine("Read Ahead:        256");
        Console.WriteLine("Tables present:    LIVE");
        Console.WriteLine("Open count:        0");
        Console.WriteLine("Event number:      0");
        Console.WriteLine($"Major, minor:      253, {index}");
        Console.WriteLine($"Number of targets: {v.Extents.Count}");
        Console.WriteLine($"UUID: {v.Guid}");
        if (v.Paths.Count > 0) Console.WriteLine($"Mount points:      {string.Join(", ", v.Paths)}");
    }

    static string Heading(string c) => c switch
    {
        "name" => "Name", "major" or "maj" => "Maj", "minor" or "min" => "Min", "attr" => "Stat", "open" => "Open",
        "segments" or "targ" => "Targ", "events" or "event" => "Event", "uuid" => "UUID", "blkdevname" => "BlkDevName",
        _ => c,
    };

    // A volume's table: one linear target per extent, in sectors, mapped onto the physical disk.
    static void Table(Volume v, bool prefix)
    {
        long start = 0;
        if (v.Extents.Count == 0) { Console.WriteLine(prefix ? $"{v.Name}: " : ""); return; }
        foreach (var e in v.Extents)
        {
            var line = $"{start} {e.Length / 512} linear PhysicalDrive{e.Disk} {e.Offset / 512}";
            Console.WriteLine(prefix ? $"{v.Name}: {line}" : line);
            start += e.Length / 512;
        }
    }

    static void Status(Volume v, bool prefix)
    {
        long start = 0;
        foreach (var e in v.Extents)
        {
            var line = $"{start} {e.Length / 512} linear ";
            Console.WriteLine(prefix ? $"{v.Name}: {line}" : line);
            start += e.Length / 512;
        }
    }

    static void Deps(Volume v, bool prefix)
    {
        var disks = v.Extents.Select(e => e.Disk).Distinct().ToList();
        var line = $"{disks.Count} dependencies\t: {string.Join(" ", disks.Select(d => $"(PhysicalDrive{d})"))}";
        Console.WriteLine(prefix ? $"{v.Name}: {line}" : line);
    }

    // ---- Windows volumes ----

    static List<Volume> Volumes()
    {
        var list = new List<Volume>();
        var name = new char[260];
        var find = FindFirstVolumeW(name, name.Length);
        if (find == new IntPtr(-1)) return list;
        try
        {
            do
            {
                var guid = new string(name, 0, Array.IndexOf(name, '\0') is var z and >= 0 ? z : name.Length);
                var v = new Volume { Guid = guid, Name = guid.TrimEnd('\\').Replace(@"\\?\", "") };
                var paths = new char[1024];
                if (GetVolumePathNamesForVolumeNameW(guid, paths, paths.Length, out var len) && len > 1)
                    v.Paths = new string(paths, 0, (int)len).Split('\0', StringSplitOptions.RemoveEmptyEntries).ToList();
                v.Extents = Extents(guid.TrimEnd('\\'));
                list.Add(v);
                Array.Clear(name);
            }
            while (FindNextVolumeW(find, name, name.Length));
        }
        finally { FindVolumeClose(find); }
        // Lettered volumes first, in letter order, like a stable ls.
        return list.OrderBy(v => v.Paths.Count == 0).ThenBy(v => v.Paths.FirstOrDefault() ?? v.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    static List<Extent> Extents(string volumePath)
    {
        var result = new List<Extent>();
        var h = CreateFileW(volumePath, 0, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);   // no access: IOCTL_VOLUME_* is FILE_ANY_ACCESS
        if (h == new IntPtr(-1)) return result;
        try
        {
            var buffer = new byte[8 + 24 * 64];
            if (!DeviceIoControl(h, IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS, IntPtr.Zero, 0, buffer, buffer.Length, out _, IntPtr.Zero)) return result;
            var count = BitConverter.ToUInt32(buffer, 0);
            for (var i = 0; i < count && i < 64; i++)
            {
                var p = 8 + i * 24;   // DISK_EXTENT: DWORD DiskNumber, (pad), LARGE_INTEGER StartingOffset, ExtentLength
                result.Add(new Extent(BitConverter.ToUInt32(buffer, p), BitConverter.ToInt64(buffer, p + 8), BitConverter.ToInt64(buffer, p + 16)));
            }
        }
        finally { CloseHandle(h); }
        return result;
    }

    const uint IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS = 0x00560000;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr FindFirstVolumeW([Out] char[] name, int length);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool FindNextVolumeW(IntPtr find, [Out] char[] name, int length);

    [DllImport("kernel32.dll")]
    static extern bool FindVolumeClose(IntPtr find);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool GetVolumePathNamesForVolumeNameW(string volume, [Out] char[] names, int length, out uint returned);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr CreateFileW(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool DeviceIoControl(IntPtr h, uint code, IntPtr input, int inputSize, byte[] output, int outputSize, out int returned, IntPtr overlapped);

    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr h);
}
