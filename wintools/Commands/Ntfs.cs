using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// ntfs-3g and ntfsprogs. Windows' own NTFS driver is the read-write driver ntfs-3g provides on
// other systems, so "mounting" is giving a volume a mount point (a drive letter or an empty
// folder, SetVolumeMountPoint), and the ntfsprogs tools work on volumes through it: ntfslabel,
// ntfsinfo (FSCTL_GET_NTFS_VOLUME_DATA and file records by id), ntfsls (directory listing with
// MFT record numbers), ntfscat (files, streams and records by number), ntfsfix (chkdsk) and
// mkntfs (Format-Volume). Devices are drive letters (E:), volume GUID paths or \\.\E:.
static unsafe class Ntfs
{
    const string Version = "2026.9.28";

    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("ntfs-3g", "Give an NTFS volume a mount point (drive letter or folder)", Mount);
        Commands.Add("lowntfs-3g", "Give an NTFS volume a mount point (drive letter or folder)", Mount);
        Commands.Add("ntfs-3g.probe", "Check whether an NTFS volume can be mounted", Probe);
        Commands.Add("ntfslabel", "Show or set an NTFS volume's label", Label);
        Commands.Add("ntfsinfo", "Dump NTFS volume or file record information", Info);
        Commands.Add("ntfsls", "List a directory on an NTFS volume", Ls);
        Commands.Add("ntfscat", "Print a file, stream or MFT record's data from an NTFS volume", Cat);
        Commands.Add("ntfsfix", "Check an NTFS volume and fix common errors (chkdsk)", Fix);
        Commands.Add("mkntfs", "Create an NTFS file system on a volume", Mkfs);
    }

    // NTFS_VOLUME_* codes ntfs-3g.probe and ntfs-3g exit with.
    const int VolumeOk = 0, NotNtfs = 11, Corrupt = 12, Unclean = 14, Locked = 15, Unknown = 17, NoPrivilege = 18;

    sealed record Volume(string Root, string DevicePath, string Display);

    // "E:", "E:\", "\\.\E:", "\\?\Volume{...}\" -> the root path and the raw device path.
    static Volume Resolve(string spec)
    {
        var s = spec.Replace('/', '\\');
        if (s.StartsWith(@"\\.\") && s.Length >= 6 && s[5] == ':') s = s[4..];
        if (s.Length >= 2 && char.IsAsciiLetter(s[0]) && s[1] == ':' && (s.Length == 2 || s == $"{s[..2]}\\"))
        {
            var letter = char.ToUpperInvariant(s[0]);
            return new Volume($"{letter}:\\", $@"\\.\{letter}:", $"{letter}:");
        }
        if (s.StartsWith(@"\\?\Volume{", StringComparison.OrdinalIgnoreCase))
        {
            var root = s.EndsWith('\\') ? s : s + "\\";
            return new Volume(root, root.TrimEnd('\\'), root);
        }
        if (File.Exists(spec))
            throw new InvalidOperationException($"{spec}: image files are mounted by Windows first (Mount-DiskImage for .vhd/.vhdx/.iso); then use its drive letter");
        throw new InvalidOperationException($"{spec}: not a volume (use a drive letter like E: or a \\\\?\\Volume{{...}}\\ path)");
    }

    static (string Label, string FileSystem, uint Serial, uint Flags)? VolumeInfo(Volume v)
    {
        var label = new StringBuilder(261);
        var fs = new StringBuilder(261);
        if (!GetVolumeInformationW(v.Root, label, 261, out var serial, out _, out var flags, fs, 261)) return null;
        return (label.ToString(), fs.ToString(), serial, flags);
    }

    static IntPtr OpenVolume(Volume v, bool write = false)
    {
        // Full read access needs an administrator; FSCTLs that only query work with none.
        foreach (var access in write ? new[] { GENERIC_READ | GENERIC_WRITE } : new[] { GENERIC_READ, 0u })
        {
            var h = CreateFileW(v.DevicePath, access, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
            if (h != INVALID_HANDLE_VALUE) return h;
        }
        return INVALID_HANDLE_VALUE;
    }

    static bool IsDirty(Volume v, out bool dirty)
    {
        dirty = false;
        var h = OpenVolume(v);
        if (h == INVALID_HANDLE_VALUE) return false;
        try
        {
            uint flags = 0;
            if (!DeviceIoControl(h, FSCTL_IS_VOLUME_DIRTY, null, 0, &flags, 4, out _, IntPtr.Zero)) return false;
            dirty = (flags & 1) != 0;
            return true;
        }
        finally { CloseHandle(h); }
    }

    // ---- ntfs-3g ----

    static int Mount(string[] args)
    {
        var positional = new List<string>();
        var options = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help":
                    Console.WriteLine($"ntfs-3g {Version} integrated FUSE 28 - Third Generation NTFS Driver\n\n" +
                                      "Usage:    ntfs-3g [-o option[,...]] <device|image_file> <mount_point>\n\n" +
                                      "On Windows the NTFS driver is built in: this gives the volume a mount point, a drive\n" +
                                      "letter (X:) or an empty folder on an NTFS volume (needs an administrator).\n" +
                                      "Remove it again with: mountvol <mount_point> /D\n\n" +
                                      "Options:  ro (unsupported per mount), rw, and ntfs-3g's ownership/caching options,\n" +
                                      "          which Windows' driver handles itself and are accepted as no-ops.");
                    return 0;
                case "-V" or "--version":
                    Console.WriteLine($"ntfs-3g {Version} integrated FUSE 28");
                    return 0;
                case "-o":
                    if (++i >= args.Length) throw new UsageException("-o needs options");
                    options.AddRange(args[i].Split(',', StringSplitOptions.RemoveEmptyEntries));
                    break;
                case "-n" or "-v" or "-f" or "--no-mtab" or "--verbose": break;
                default:
                    if (args[i].StartsWith("-o") && args[i].Length > 2) options.AddRange(args[i][2..].Split(','));
                    else if (args[i].StartsWith('-')) throw new UsageException($"unknown option {args[i]}");
                    else positional.Add(args[i]);
                    break;
            }
        }
        if (positional.Count != 2) throw new UsageException("expected a device and a mount point (see --help)");
        if (options.Any(o => o == "ro" || o.StartsWith("ro=")))
        {
            Console.Error.WriteLine("ntfs-3g: Windows has no per-mount read-only option; make the volume read-only with diskpart (attributes volume set readonly)");
            return Unknown;
        }
        var v = Resolve(positional[0]);
        if (VolumeInfo(v) is not { } info) { Console.Error.WriteLine($"ntfs-3g: {positional[0]}: volume not found"); return Unknown; }
        if (info.FileSystem != "NTFS") { Console.Error.WriteLine($"ntfs-3g: {positional[0]}: not an NTFS volume ({info.FileSystem})"); return NotNtfs; }

        var volumeName = new StringBuilder(64);
        if (!GetVolumeNameForVolumeMountPointW(v.Root, volumeName, 64))
            throw new InvalidOperationException($"{positional[0]}: no volume GUID ({Marshal.GetLastPInvokeErrorMessage()})");
        var mount = positional[1].Replace('/', '\\');
        if (mount.Length == 2 && mount[1] == ':') mount += "\\";
        else
        {
            mount = Path.GetFullPath(mount);
            if (!Directory.Exists(mount)) throw new InvalidOperationException($"{positional[1]}: mount point must be an existing empty folder or a free drive letter");
            if (Directory.EnumerateFileSystemEntries(mount).Any()) throw new InvalidOperationException($"{positional[1]}: mount point is not empty");
            if (!mount.EndsWith('\\')) mount += "\\";
        }
        if (!SetVolumeMountPointW(mount, volumeName.ToString()))
        {
            var err = Marshal.GetLastPInvokeError();
            Console.Error.WriteLine($"ntfs-3g: could not mount {positional[0]} on {positional[1]}: {Marshal.GetPInvokeErrorMessage(err)}");
            return err == 5 ? NoPrivilege : Unknown;
        }
        return VolumeOk;
    }

    // ---- ntfs-3g.probe ----

    static int Probe(string[] args)
    {
        var readWrite = false;
        string device = null;
        foreach (var a in args)
        {
            switch (a)
            {
                case "-h" or "--help":
                    Console.WriteLine("ntfs-3g.probe <--readonly|--readwrite> <device|image_file>\n\n" +
                                      "Exit status: 0 mountable, 11 not NTFS, 12 corrupt, 14 unclean (dirty), 15 locked/read-only,\n" +
                                      "17 unknown error, 18 no privilege.");
                    return 0;
                case "-r" or "--readonly": readWrite = false; break;
                case "-w" or "--readwrite": readWrite = true; break;
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unknown option {a}");
                    device = a;
                    break;
            }
        }
        if (device == null) throw new UsageException("no device given");
        var v = Resolve(device);
        if (VolumeInfo(v) is not { } info) { Console.Error.WriteLine($"{device}: volume not found"); return Unknown; }
        if (info.FileSystem != "NTFS") { Console.Error.WriteLine($"{device}: not an NTFS volume ({info.FileSystem})"); return NotNtfs; }
        if (readWrite && (info.Flags & FILE_READ_ONLY_VOLUME) != 0) { Console.Error.WriteLine($"{device}: the volume is read-only"); return Locked; }
        if (readWrite && IsDirty(v, out var dirty) && dirty)
        {
            Console.Error.WriteLine($"{device}: the volume is marked dirty; run chkdsk (ntfsfix) first");
            return Unclean;
        }
        return VolumeOk;
    }

    // ---- ntfslabel ----

    static int Label(string[] args)
    {
        bool noAction = false;
        var positional = new List<string>();
        foreach (var a in args)
        {
            switch (a)
            {
                case "-h" or "--help":
                    Console.WriteLine("Usage: ntfslabel [options] device [label]\n    -n, --no-action    Do not write to disk\n    -f, --force        Accepted\n" +
                                      "    -q, --quiet        Less output\n    -v, --verbose      More output\n    -V, --version      Display version information");
                    return 0;
                case "-V" or "--version": Console.WriteLine($"ntfslabel v{Version} (libntfs-3g)"); return 0;
                case "-n" or "--no-action": noAction = true; break;
                case "-f" or "--force" or "-q" or "--quiet" or "-v" or "--verbose": break;
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unknown option {a}");
                    positional.Add(a);
                    break;
            }
        }
        if (positional.Count is < 1 or > 2) throw new UsageException("usage: ntfslabel [options] device [label]");
        var v = Resolve(positional[0]);
        if (VolumeInfo(v) is not { } info) { Console.Error.WriteLine($"{positional[0]}: volume not found"); return 1; }
        if (info.FileSystem != "NTFS") { Console.Error.WriteLine($"{positional[0]}: not an NTFS volume ({info.FileSystem})"); return 1; }
        if (positional.Count == 1) { Console.WriteLine(info.Label); return 0; }
        var label = positional[1];
        if (label.Length > 32)
        {
            Console.Error.WriteLine("New label is too long (32 characters maximum); truncating.");
            label = label[..32];
        }
        if (noAction) { Console.WriteLine($"New label would be: {label}"); return 0; }
        if (!SetVolumeLabelW(v.Root, label))
            throw new InvalidOperationException($"could not set the label: {Marshal.GetLastPInvokeErrorMessage()}");
        return 0;
    }

    // ---- ntfsinfo ----

    static int Info(string[] args)
    {
        bool volume = false;
        long? inode = null;
        string file = null, device = null;
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Value() => ++i < args.Length ? args[i] : throw new UsageException($"{a} needs a value");
            switch (a)
            {
                case "-h" or "--help":
                    Console.WriteLine("Usage: ntfsinfo [options] device\n    -i, --inode NUM  Display information about this inode (MFT record)\n" +
                                      "    -F, --file FILE  Display information about this file (absolute path)\n    -m, --mft        Dump information about the volume\n" +
                                      "    -t, --notime     Don't print timestamps\n    -V, --version    Version information");
                    return 0;
                case "-V" or "--version": Console.WriteLine($"ntfsinfo v{Version} (libntfs-3g)"); return 0;
                case "-m" or "--mft": volume = true; break;
                case "-i" or "--inode": inode = long.TryParse(Value(), out var n) && n >= 0 ? n : throw new UsageException("invalid inode number"); break;
                case "-F" or "--file": file = Value(); break;
                case "-d" or "--device": device = Value(); break;
                case "-t" or "--notime" or "-v" or "--verbose" or "-f" or "--force" or "-q" or "--quiet": break;
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unknown option {a}");
                    device = a;
                    break;
            }
        }
        if (device == null) throw new UsageException("no device given (see --help)");
        if (!volume && inode == null && file == null) throw new UsageException("give -m, -i or -F");
        var v = Resolve(device);
        if (VolumeInfo(v) is not { } info || info.FileSystem != "NTFS") { Console.Error.WriteLine($"{device}: not an NTFS volume"); return 1; }
        if (volume) return DumpVolume(v, info);
        var path = file != null ? Path.Combine(v.Root, file.TrimStart('\\', '/').Replace('/', '\\')) : null;
        return DumpFile(v, path, inode);
    }

    static int DumpVolume(Volume v, (string Label, string FileSystem, uint Serial, uint Flags) info)
    {
        var h = OpenVolume(v);
        if (h == INVALID_HANDLE_VALUE) throw new InvalidOperationException($"{v.Display}: could not open the volume: {Marshal.GetLastPInvokeErrorMessage()}");
        var buffer = stackalloc byte[256];
        try
        {
            if (!DeviceIoControl(h, FSCTL_GET_NTFS_VOLUME_DATA, null, 0, buffer, 256, out _, IntPtr.Zero))
                throw new InvalidOperationException($"{v.Display}: could not read the volume data (run as administrator): {Marshal.GetLastPInvokeErrorMessage()}");
        }
        finally { CloseHandle(h); }
        var d = (NTFS_VOLUME_DATA_BUFFER*)buffer;
        var x = (NTFS_EXTENDED_VOLUME_DATA*)(buffer + sizeof(NTFS_VOLUME_DATA_BUFFER));
        IsDirty(v, out var dirty);
        var used = d->TotalClusters - d->FreeClusters;
        var o = new StringBuilder();
        o.Append("Volume Information \n");
        o.Append($"\tName of device: {v.Display}\n");
        o.Append($"\tVolume Name: {info.Label}\n");
        o.Append($"\tVolume Serial Number: {(ulong)d->VolumeSerialNumber:X16}\n");
        o.Append($"\tVolume Flags: 0x{(dirty ? 1 : 0):x4}{(dirty ? " DIRTY" : "")}\n");
        o.Append($"\tVolume Version: {x->MajorVersion}.{x->MinorVersion}\n");
        o.Append($"\tSector Size: {d->BytesPerSector}\n");
        o.Append($"\tCluster Size: {d->BytesPerCluster}\n");
        o.Append($"\tVolume Size in Clusters: {d->TotalClusters}\n");
        o.Append("MFT Information \n");
        o.Append($"\tMFT Record Size: {d->BytesPerFileRecordSegment}\n");
        o.Append($"\tMFT Valid Data Length: {d->MftValidDataLength}\n");
        o.Append($"\tMFT Records: {d->MftValidDataLength / Math.Max(1u, d->BytesPerFileRecordSegment)}\n");
        o.Append($"\tMFT Zone Start: {d->MftZoneStart}\n");
        o.Append($"\tMFT Zone End: {d->MftZoneEnd}\n");
        o.Append($"\tLCN of Data Attribute for FILE_MFT: {d->MftStartLcn}\n");
        o.Append($"\tLCN of Data Attribute for File_MFTMirr: {d->Mft2StartLcn}\n");
        o.Append($"\tAllocated clusters {used} ({100.0 * used / Math.Max(1, d->TotalClusters):0.0}%)\n");
        o.Append($"\tFree Clusters: {d->FreeClusters} ({100.0 * d->FreeClusters / Math.Max(1, d->TotalClusters):0.0}%)\n");
        Console.Write(o.ToString());
        return 0;
    }

    static int DumpFile(Volume v, string path, long? inode)
    {
        var h = path != null
            ? CreateFileW(path, FILE_READ_ATTRIBUTES, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero)
            : OpenById(v, inode.Value, FILE_READ_ATTRIBUTES);
        if (h == INVALID_HANDLE_VALUE)
        {
            Console.Error.WriteLine($"Error opening {(path ?? $"inode {inode}")}: {Marshal.GetLastPInvokeErrorMessage()}");
            return 1;
        }
        try
        {
            if (!GetFileInformationByHandle(h, out var fi)) throw new InvalidOperationException(Marshal.GetLastPInvokeErrorMessage());
            var id = ((ulong)fi.nFileIndexHigh << 32) | fi.nFileIndexLow;
            var name = NameOf(h) ?? path ?? "?";
            var o = new StringBuilder();
            o.Append($"Dumping Inode {id & 0xFFFFFFFFFFFF} ({name})\n");
            o.Append($"MFT Record Seq. Numb.:\t {id >> 48} (0x{id >> 48:x})\n");
            o.Append($"Number of Hard Links:\t {fi.nNumberOfLinks} (0x{fi.nNumberOfLinks:x})\n");
            o.Append($"File Attributes:\t {AttributeNames(fi.dwFileAttributes)}\n");
            o.Append($"File Creation Time:\t {Time(fi.ftCreationTime)}\n");
            o.Append($"File Altered Time:\t {Time(fi.ftLastWriteTime)}\n");
            o.Append($"Last Accessed Time:\t {Time(fi.ftLastAccessTime)}\n");
            if ((fi.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) == 0)
                o.Append($"Data Size:\t\t {((ulong)fi.nFileSizeHigh << 32) | fi.nFileSizeLow}\n");
            foreach (var (stream, size) in Streams(name))
                o.Append($"Dumping attribute $DATA (0x80)\n\tAttribute name:\t '{stream}'\n\tData size:\t {size}\n");
            Console.Write(o.ToString());
        }
        finally { CloseHandle(h); }
        return 0;
    }

    static string AttributeNames(uint a)
    {
        var names = new List<string>();
        foreach (var (bit, n) in new[] { (0x1u, "READONLY"), (0x2u, "HIDDEN"), (0x4u, "SYSTEM"), (0x10u, "DIRECTORY"), (0x20u, "ARCHIVE"),
                     (0x80u, "NORMAL"), (0x100u, "TEMPORARY"), (0x200u, "SPARSE_FILE"), (0x400u, "REPARSE_POINT"), (0x800u, "COMPRESSED"),
                     (0x1000u, "OFFLINE"), (0x2000u, "NOT_CONTENT_INDEXED"), (0x4000u, "ENCRYPTED") })
            if ((a & bit) != 0) names.Add(n);
        return $"0x{a:x8} ({string.Join(" | ", names)})";
    }

    static string Time(System.Runtime.InteropServices.ComTypes.FILETIME ft)
    {
        var ticks = ((long)(uint)ft.dwHighDateTime << 32) | (uint)ft.dwLowDateTime;
        return DateTime.FromFileTimeUtc(ticks).ToLocalTime().ToString("ddd MMM dd HH:mm:ss.fffffff yyyy", System.Globalization.CultureInfo.InvariantCulture);
    }

    static string NameOf(IntPtr h)
    {
        var buf = new StringBuilder(32768);
        var n = GetFinalPathNameByHandleW(h, buf, 32768, 0);
        if (n == 0 || n >= 32768) return null;
        var s = buf.ToString();
        return s.StartsWith(@"\\?\") ? s[4..] : s;
    }

    // Named data streams ("file:name:$DATA"), not the unnamed one.
    static List<(string Name, long Size)> Streams(string path)
    {
        var list = new List<(string, long)>();
        var h = FindFirstStreamW(path, 0, out var data, 0);
        if (h == INVALID_HANDLE_VALUE) return list;
        try
        {
            do
            {
                var name = data.cStreamName;
                if (name != "::$DATA" && name.EndsWith(":$DATA")) list.Add((name[1..^6], data.StreamSize));
            } while (FindNextStreamW(h, out data));
        }
        finally { FindClose(h); }
        return list;
    }

    static IntPtr OpenById(Volume v, long record, uint access)
    {
        var hint = CreateFileW(v.Root, 0, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
        if (hint == INVALID_HANDLE_VALUE) return hint;
        try
        {
            // A file id is the record number plus the record's sequence number in the top 16 bits;
            // a bare record number gets its sequence number from the MFT record itself.
            if (record >> 48 == 0 && SequenceNumber(v, record) is { } sequence) record |= (long)sequence << 48;
            var id = new FILE_ID_DESCRIPTOR { dwSize = (uint)sizeof(FILE_ID_DESCRIPTOR), Type = 0, FileId = record };
            var h = OpenFileById(hint, ref id, access, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, IntPtr.Zero, FILE_FLAG_BACKUP_SEMANTICS);
            var error = Marshal.GetLastPInvokeError();
            CloseHandle(hint);
            Marshal.SetLastPInvokeError(error);
            return h;
        }
        catch { CloseHandle(hint); throw; }
    }

    // The sequence number in an in-use MFT record's header (FSCTL_GET_NTFS_FILE_RECORD, administrator).
    static ushort? SequenceNumber(Volume v, long record)
    {
        var h = OpenVolume(v);
        if (h == INVALID_HANDLE_VALUE) return null;
        try
        {
            var output = new byte[16 + 4096];
            fixed (byte* p = output)
            {
                if (!DeviceIoControl(h, FSCTL_GET_NTFS_FILE_RECORD, &record, 8, p, (uint)output.Length, out _, IntPtr.Zero)) return null;
                // The FSCTL answers with the nearest in-use record at or below the one asked for.
                if (*(long*)p != record || *(uint*)(p + 12) != 0x454C4946) return null;   // "FILE"
                return *(ushort*)(p + 12 + 16);
            }
        }
        finally { CloseHandle(h); }
    }

    // ---- ntfsls ----

    static int Ls(string[] args)
    {
        bool all = false, classify = false, inodes = false, longList = false, system = false, dos = false;
        string path = "\\", device = null;
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            switch (a)
            {
                case "-h" or "--help":
                    Console.WriteLine("Usage: ntfsls [options] device\n    -a, --all            Display all files\n    -F, --classify       Mark directories with /\n" +
                                      "    -i, --inode          Display inode (MFT record) numbers\n    -l, --long           Display long info\n" +
                                      "    -p, --path PATH      Directory whose contents to list\n    -s, --system         Display system files\n" +
                                      "    -x, --dos            Use short (DOS 8.3) names\n    -V, --version        Display version information");
                    return 0;
                case "-V" or "--version": Console.WriteLine($"ntfsls v{Version} (libntfs-3g)"); return 0;
                case "-a" or "--all": all = true; break;
                case "-F" or "--classify": classify = true; break;
                case "-i" or "--inode": inodes = true; break;
                case "-l" or "--long": longList = true; break;
                case "-s" or "--system": system = true; break;
                case "-x" or "--dos": dos = true; break;
                case "-p" or "--path": path = ++i < args.Length ? args[i] : throw new UsageException("-p needs a path"); break;
                case "-f" or "--force" or "-q" or "--quiet" or "-v" or "--verbose": break;
                default:
                    if (a.StartsWith("--path=")) path = a[7..];
                    else if (a.StartsWith('-')) throw new UsageException($"unknown option {a}");
                    else device = a;
                    break;
            }
        }
        if (device == null) throw new UsageException("no device given (see --help)");
        var v = Resolve(device);
        if (VolumeInfo(v) is not { } info || info.FileSystem != "NTFS") { Console.Error.WriteLine($"{device}: not an NTFS volume"); return 1; }
        var dir = Path.Combine(v.Root, path.Replace('/', '\\').TrimStart('\\'));
        var h = CreateFileW(dir, FILE_LIST_DIRECTORY, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
        if (h == INVALID_HANDLE_VALUE) { Console.Error.WriteLine($"Failed to open {path}: {Marshal.GetLastPInvokeErrorMessage()}"); return 1; }
        var entries = new List<(string Name, string Short, ulong Id, long Size, long Written, uint Attributes)>();
        try
        {
            var buffer = new byte[64 * 1024];
            fixed (byte* p = buffer)
            {
                var cls = FileIdBothDirectoryRestartInfo;
                while (GetFileInformationByHandleEx(h, cls, p, (uint)buffer.Length))
                {
                    cls = FileIdBothDirectoryInfo;
                    for (var offset = 0; ;)
                    {
                        var e = p + offset;
                        var next = *(uint*)e;
                        var nameLength = *(uint*)(e + 60);
                        var name = new string((char*)(e + 104), 0, (int)nameLength / 2);
                        var shortName = new string((char*)(e + 70), 0, *(e + 68) / 2);
                        entries.Add((name, shortName, *(ulong*)(e + 96), *(long*)(e + 40), *(long*)(e + 24), *(uint*)(e + 56)));
                        if (next == 0) break;
                        offset += (int)next;
                    }
                }
            }
        }
        finally { CloseHandle(h); }

        foreach (var e in entries.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
        {
            var dot = e.Name is "." or "..";
            var hidden = (e.Attributes & (FILE_ATTRIBUTE_HIDDEN | FILE_ATTRIBUTE_SYSTEM)) != 0;
            if (dot && !all) continue;
            if (hidden && !all && !system) continue;
            var isDir = (e.Attributes & FILE_ATTRIBUTE_DIRECTORY) != 0;
            var name = dos && e.Short.Length > 0 ? e.Short : e.Name;
            var line = new StringBuilder();
            if (inodes) line.Append($"{e.Id & 0xFFFFFFFFFFFF,7} ");
            if (longList)
            {
                var when = DateTime.FromFileTimeUtc(e.Written).ToLocalTime();
                line.Append($"{(isDir ? 0 : e.Size),12}  {when:yyyy-MM-dd HH:mm}  ");
            }
            line.Append(name);
            if (classify && isDir && !dot) line.Append('/');
            Console.WriteLine(line.ToString());
        }
        return 0;
    }

    // ---- ntfscat ----

    static int Cat(string[] args)
    {
        string device = null, file = null, stream = null, attribute = null;
        long? inode = null;
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Value() => ++i < args.Length ? args[i] : throw new UsageException($"{a} needs a value");
            switch (a)
            {
                case "-h" or "--help":
                    Console.WriteLine("Usage: ntfscat [options] device [file]\n\n    -a, --attribute TYPE       Display this attribute type ($DATA, 0x80)\n" +
                                      "    -n, --attribute-name NAME  Display this attribute name (a named data stream)\n" +
                                      "    -i, --inode NUM            Display this inode (MFT record)\n    -V, --version              Display version information");
                    return 0;
                case "-V" or "--version": Console.WriteLine($"ntfscat v{Version} (libntfs-3g)"); return 0;
                case "-a" or "--attribute": attribute = Value(); break;
                case "-n" or "--attribute-name": stream = Value(); break;
                case "-i" or "--inode": inode = long.TryParse(Value(), out var n) && n >= 0 ? n : throw new UsageException("invalid inode number"); break;
                case "-f" or "--force" or "-q" or "--quiet" or "-v" or "--verbose": break;
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unknown option {a}");
                    if (device == null) device = a; else if (file == null) file = a; else throw new UsageException("too many arguments");
                    break;
            }
        }
        if (device == null || (file == null && inode == null)) throw new UsageException("usage: ntfscat [options] device [file]");
        if (attribute != null && attribute.ToUpperInvariant() is not ("$DATA" or "0X80" or "128" or "DATA"))
        {
            Console.Error.WriteLine("ntfscat: only $DATA attributes (file contents and named streams) are readable through Windows' NTFS driver");
            return 1;
        }
        var v = Resolve(device);
        IntPtr h;
        if (inode != null)
        {
            if (stream != null) throw new UsageException("-n can't be combined with -i");
            h = OpenById(v, inode.Value, GENERIC_READ);
        }
        else
        {
            var path = Path.Combine(v.Root, file.Replace('/', '\\').TrimStart('\\'));
            if (stream != null) path += ":" + stream;
            h = CreateFileW(path, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
        }
        if (h == INVALID_HANDLE_VALUE)
        {
            Console.Error.WriteLine($"ntfscat: cannot open {(file ?? $"inode {inode}")}{(stream != null ? ":" + stream : "")}: {Marshal.GetLastPInvokeErrorMessage()}");
            return 1;
        }
        using var input = new FileStream(new Microsoft.Win32.SafeHandles.SafeFileHandle(h, ownsHandle: true), FileAccess.Read);
        using var stdout = Console.OpenStandardOutput();
        input.CopyTo(stdout);
        return 0;
    }

    // ---- ntfsfix ----

    static int Fix(string[] args)
    {
        bool noAction = false, clearDirty = false;
        string device = null;
        foreach (var a in args)
        {
            switch (a)
            {
                case "-h" or "--help":
                    Console.WriteLine("Usage: ntfsfix [options] device\n    Attempt to fix an NTFS partition.\n\n" +
                                      "    -b, --clear-bad-sectors Scan for and mark bad sectors (chkdsk /r)\n    -d, --clear-dirty       Fix errors and clear the dirty flag (chkdsk /f)\n" +
                                      "    -n, --no-action         Only report the volume's state\n    -V, --version           Display version information\n\n" +
                                      "Without -d the volume is checked online (chkdsk /scan). chkdsk needs an administrator.");
                    return 0;
                case "-V" or "--version": Console.WriteLine($"ntfsfix v{Version} (libntfs-3g)"); return 0;
                case "-n" or "--no-action": noAction = true; break;
                case "-d" or "--clear-dirty": clearDirty = true; break;
                case "-b" or "--clear-bad-sectors": clearDirty = true; break;
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unknown option {a}");
                    device = a;
                    break;
            }
        }
        if (device == null) throw new UsageException("no device given (see --help)");
        var v = Resolve(device);
        if (VolumeInfo(v) is not { } info || info.FileSystem != "NTFS") { Console.Error.WriteLine($"{device}: not an NTFS volume"); return 1; }
        Console.WriteLine("Mounting volume... OK");
        var known = IsDirty(v, out var dirty);
        Console.WriteLine(known ? $"Checking the volume's dirty flag... {(dirty ? "SET" : "clear")}" : "Checking the volume's dirty flag... unknown (needs an administrator)");
        if (noAction)
        {
            Console.WriteLine("No change made");
            return dirty ? 1 : 0;
        }
        var chkdsk = Path.Combine(Environment.SystemDirectory, "chkdsk.exe");
        var target = v.Display.TrimEnd('\\');
        var arguments = args.Contains("-b") || args.Contains("--clear-bad-sectors") ? $"{target} /r /x" : clearDirty ? $"{target} /f /x" : $"{target} /scan";
        Console.WriteLine($"Running chkdsk {arguments}");
        using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(chkdsk, arguments) { UseShellExecute = false });
        p.WaitForExit();
        return p.ExitCode;
    }

    // ---- mkntfs ----

    static int Mkfs(string[] args)
    {
        bool quick = false, noAction = false, force = false;
        string label = null, device = null;
        int? cluster = null;
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Value() => ++i < args.Length ? args[i] : throw new UsageException($"{a} needs a value");
            switch (a)
            {
                case "-h" or "--help":
                    Console.WriteLine("Usage: mkntfs [options] device\n\n    -f, -Q, --fast, --quick  Perform a quick format\n    -L, --label STRING       Set the volume label\n" +
                                      "    -c, --cluster-size BYTES Specify the cluster size for the volume\n    -n, --no-action          Do not write to disk\n" +
                                      "    -F, --force              Force execution despite errors\n    -V, --version            Display version information\n\n" +
                                      "The volume is formatted by Windows (Format-Volume); the system volume is refused.");
                    return 0;
                case "-V" or "--version": Console.WriteLine($"mkntfs v{Version} (libntfs-3g)"); return 0;
                case "-f" or "-Q" or "--fast" or "--quick": quick = true; break;
                case "-n" or "--no-action": noAction = true; break;
                case "-F" or "--force": force = true; break;
                case "-L" or "--label": label = Value(); break;
                case "-c" or "--cluster-size": cluster = int.TryParse(Value(), out var c) && c >= 512 && (c & (c - 1)) == 0 ? c : throw new UsageException("cluster size must be a power of two of at least 512"); break;
                case "-I" or "--no-indexing" or "-C" or "--enable-compression" or "-q" or "--quiet" or "-v" or "--verbose": break;
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unknown option {a}");
                    device = a;
                    break;
            }
        }
        if (device == null) throw new UsageException("no device given (see --help)");
        var v = Resolve(device);
        if (v.Root.Length != 3) throw new InvalidOperationException("mkntfs formats a volume with a drive letter (assign one with ntfs-3g or Disk Management)");
        var system = Path.GetPathRoot(Environment.SystemDirectory);
        if (string.Equals(v.Root, system, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException($"{v.Display} is the Windows system volume");
        var letter = v.Root[0];
        var command = $"Format-Volume -DriveLetter {letter} -FileSystem NTFS -Full:${(!quick).ToString().ToLowerInvariant()} -Force -Confirm:$false" +
                      (label != null ? $" -NewFileSystemLabel '{label.Replace("'", "''")}'" : "") + (cluster != null ? $" -AllocationUnitSize {cluster}" : "");
        if (noAction)
        {
            Console.WriteLine($"Would run: {command}");
            return 0;
        }
        if (!force && VolumeInfo(v) is { } info) Console.WriteLine($"Formatting {v.Display} ({info.FileSystem}, label '{info.Label}') as NTFS");
        var psi = new System.Diagnostics.ProcessStartInfo("powershell.exe") { UseShellExecute = false };
        foreach (var arg in new[] { "-NoProfile", "-NonInteractive", "-Command", command + " | Out-Null" }) psi.ArgumentList.Add(arg);
        using var p = System.Diagnostics.Process.Start(psi);
        p.WaitForExit();
        if (p.ExitCode == 0) Console.WriteLine("mkntfs completed successfully. Have a nice day.");
        return p.ExitCode == 0 ? 0 : 1;
    }

    // ---- Win32 ----

    const uint GENERIC_READ = 0x80000000, GENERIC_WRITE = 0x40000000, FILE_READ_ATTRIBUTES = 0x80, FILE_LIST_DIRECTORY = 0x1;
    const uint FILE_SHARE_READ = 1, FILE_SHARE_WRITE = 2, FILE_SHARE_DELETE = 4, OPEN_EXISTING = 3, FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
    const uint FILE_ATTRIBUTE_HIDDEN = 0x2, FILE_ATTRIBUTE_SYSTEM = 0x4, FILE_ATTRIBUTE_DIRECTORY = 0x10, FILE_READ_ONLY_VOLUME = 0x80000;
    const uint FSCTL_GET_NTFS_VOLUME_DATA = 0x90064, FSCTL_GET_NTFS_FILE_RECORD = 0x90068, FSCTL_IS_VOLUME_DIRTY = 0x90078;
    const int FileIdBothDirectoryInfo = 10, FileIdBothDirectoryRestartInfo = 11;
    static readonly IntPtr INVALID_HANDLE_VALUE = new(-1);

    [StructLayout(LayoutKind.Sequential)]
    struct NTFS_VOLUME_DATA_BUFFER
    {
        public long VolumeSerialNumber, NumberSectors, TotalClusters, FreeClusters, TotalReserved;
        public uint BytesPerSector, BytesPerCluster, BytesPerFileRecordSegment, ClustersPerFileRecordSegment;
        public long MftValidDataLength, MftStartLcn, Mft2StartLcn, MftZoneStart, MftZoneEnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct NTFS_EXTENDED_VOLUME_DATA
    {
        public uint ByteCount;
        public ushort MajorVersion, MinorVersion;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct BY_HANDLE_FILE_INFORMATION
    {
        public uint dwFileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime, ftLastAccessTime, ftLastWriteTime;
        public uint dwVolumeSerialNumber, nFileSizeHigh, nFileSizeLow, nNumberOfLinks, nFileIndexHigh, nFileIndexLow;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct FILE_ID_DESCRIPTOR
    {
        public uint dwSize;
        public int Type;
        public long FileId;
        public long Pad;   // the union is as large as a FILE_ID_128
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct WIN32_FIND_STREAM_DATA
    {
        public long StreamSize;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 296)] public string cStreamName;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetVolumeInformationW(string root, StringBuilder label, int labelSize, out uint serial, out uint maxComponent, out uint flags, StringBuilder fs, int fsSize);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SetVolumeLabelW(string root, string label);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetVolumeNameForVolumeMountPointW(string mountPoint, StringBuilder volumeName, int size);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SetVolumeMountPointW(string mountPoint, string volumeName);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr CreateFileW(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenFileById(IntPtr volumeHint, ref FILE_ID_DESCRIPTOR id, uint access, uint share, IntPtr security, uint flags);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool DeviceIoControl(IntPtr device, uint code, void* input, uint inputSize, void* output, uint outputSize, out uint returned, IntPtr overlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetFileInformationByHandle(IntPtr file, out BY_HANDLE_FILE_INFORMATION info);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetFileInformationByHandleEx(IntPtr file, int infoClass, void* buffer, uint size);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern uint GetFinalPathNameByHandleW(IntPtr file, StringBuilder path, uint size, uint flags);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr FindFirstStreamW(string file, int level, out WIN32_FIND_STREAM_DATA data, uint flags);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool FindNextStreamW(IntPtr find, out WIN32_FIND_STREAM_DATA data);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool FindClose(IntPtr find);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CloseHandle(IntPtr handle);
}
