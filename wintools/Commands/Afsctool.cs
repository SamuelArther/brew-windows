using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace WinTools;

// afsctool: transparent file compression, the Windows way. APFS/HFS+ decmpfs compression becomes
// the Windows Overlay Filter (WOF: XPRESS4K/8K/16K or LZX, FSCTL_SET_EXTERNAL_BACKING, what
// `compact /exe` uses) or classic NTFS LZNT1 compression (FSCTL_SET_COMPRESSION). Like afsctool,
// files stay readable by every program; -c compresses, -d decompresses, no flag reports.
static class Afsctool
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("afsctool", "Compress files transparently with NTFS/WOF compression (afsctool)", Run);
    }

    const string Help = """
        Usage:
          Report if file is compressed:                afsctool [-v] file[s]
          Report if folder contains compressed files:  afsctool [-fvi] [-t <extension>] folder[s]
          List compressed files in folder:             afsctool -l[fv] folder
          Decompress file or folder:                   afsctool -d[i] [-t <extension>] file[s]/folder[s]
          Compress file or folder:                     afsctool -c[nlfvib] [-jN|-JN] [-<level>] [-m <size>]
                                                       [-s <percentage>] [-t <extension>] [-T compressor] file[s]/folder[s]

        Options:
          -v  Increase verbosity (print file info after compressing)
          -f  Detect hard links (process each file once)
          -l  List files that fail to compress (with -c), or compressed files (report mode)
          -n  Do not verify files after compression (not recommended)
          -m <size>        Largest file size to compress, in bytes
          -s <percentage>  Keep compression only if it saves at least this percentage
          -t <extension>   Only files with this extension (repeatable); -i inverts the selection
          -b  Make a backup (<file>.bak) of files before compressing them
          -jN / -JN        Compress with N threads
          -<level>         1-9; picks the WOF algorithm when -T is ZLIB (1-3 XPRESS4K, 4-6 XPRESS8K,
                           7-8 XPRESS16K, 9 LZX); default 5
          -T <compressor>  XPRESS4K, XPRESS8K, XPRESS16K, LZX (Windows Overlay Filter) or LZNT1 (NTFS);
                           afsctool's ZLIB/LZVN/LZFSE map to level-based/XPRESS4K/XPRESS16K
          -L, -S, -RM, -k, -p  Accepted for compatibility (no effect on Windows)

        Compressed files stay readable by every program; writing to a WOF-compressed file
        decompresses it. -a/-x (decmpfs archives) have no Windows equivalent.
        """;

    internal enum Algo { None = -2, Lznt1 = -1, Xpress4K = 0, Lzx = 1, Xpress8K = 2, Xpress16K = 3 }

    sealed class Options
    {
        public bool Compress, Decompress, List, Verbose, HardLinks, NoVerify, Invert, Backup;
        public long MaxSize = long.MaxValue;
        public double MinSavings = double.NaN;
        public int Level = 5, Threads = 1;
        public string Compressor = "ZLIB";
        public readonly List<string> Extensions = [];
        public readonly List<string> Paths = [];
    }

    static int Run(string[] args)
    {
        var o = Parse(args);
        if (o == null) { Console.WriteLine(Help); return 0; }
        if (o.Paths.Count == 0) throw new UsageException("no files given (see afsctool -h)");
        if (o.Compress && o.Decompress) throw new UsageException("-c and -d are mutually exclusive");

        var status = 0;
        foreach (var path in o.Paths)
        {
            var full = Path.GetFullPath(path);
            if (!File.Exists(full) && !Directory.Exists(full))
            {
                Console.Error.WriteLine($"{path}: No such file or directory");
                status = 1;
                continue;
            }
            if (o.Compress || o.Decompress) status |= Transform(full, o);
            else if (Directory.Exists(full)) ReportFolder(full, o);
            else ReportFile(full);
        }
        return status;
    }

    internal static Algo AlgorithmFor(string compressor, int level) => compressor.ToUpperInvariant() switch
    {
        "LZNT1" or "NTFS" => Algo.Lznt1,
        "XPRESS4K" or "LZVN" => Algo.Xpress4K,
        "XPRESS8K" => Algo.Xpress8K,
        "XPRESS16K" or "LZFSE" => Algo.Xpress16K,
        "LZX" => Algo.Lzx,
        "ZLIB" => level <= 3 ? Algo.Xpress4K : level <= 6 ? Algo.Xpress8K : level <= 8 ? Algo.Xpress16K : Algo.Lzx,
        _ => throw new UsageException($"unsupported compressor '{compressor}' (XPRESS4K, XPRESS8K, XPRESS16K, LZX, LZNT1)"),
    };

    static Options Parse(string[] args)
    {
        var o = new Options();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string Value(string flag, string rest)
            {
                if (rest.Length > 0) return rest;
                if (++i >= args.Length) throw new UsageException($"option {flag} requires an argument");
                return args[i];
            }
            if (arg is "--help" or "-h") return null;
            if (arg == "--") { o.Paths.AddRange(args[(i + 1)..]); break; }
            if (!arg.StartsWith('-') || arg == "-") { o.Paths.Add(arg); continue; }
            if (arg == "-RM") continue;
            // Bundled single-letter flags (-cvl, -9, -j4).
            for (var k = 1; k < arg.Length; k++)
            {
                var c = arg[k];
                var rest = arg[(k + 1)..];
                switch (c)
                {
                    case 'c': o.Compress = true; break;
                    case 'd': o.Decompress = true; break;
                    case 'v': o.Verbose = true; break;
                    case 'l': o.List = true; break;
                    case 'f': o.HardLinks = true; break;
                    case 'n': o.NoVerify = true; break;
                    case 'i': o.Invert = true; break;
                    case 'b': o.Backup = true; break;
                    case 'L' or 'S' or 'k' or 'p': break;
                    case 'a' or 'x':
                        throw new InvalidOperationException("decmpfs archives (-a/-x) are an HFS+/APFS format with no Windows counterpart");
                    case 'h': return null;
                    case >= '1' and <= '9':
                        o.Level = c - '0';
                        break;
                    case 'j' or 'J':
                        if (!int.TryParse(rest, out o.Threads) || o.Threads < 1) throw new UsageException($"-{c} needs a thread count");
                        k = arg.Length;
                        break;
                    case 'm':
                        if (!long.TryParse(Value("-m", rest), out o.MaxSize)) throw new UsageException("-m needs a size in bytes");
                        k = arg.Length;
                        break;
                    case 's':
                        if (!double.TryParse(Value("-s", rest), System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out o.MinSavings) || o.MinSavings < 0 || o.MinSavings > 100)
                            throw new UsageException("-s needs a percentage between 0 and 100");
                        k = arg.Length;
                        break;
                    case 't':
                        o.Extensions.Add(Value("-t", rest).TrimStart('.').ToLowerInvariant());
                        k = arg.Length;
                        break;
                    case 'T':
                        o.Compressor = Value("-T", rest);
                        AlgorithmFor(o.Compressor, o.Level);   // validate now
                        k = arg.Length;
                        break;
                    default: throw new UsageException($"unknown option -{c}");
                }
            }
        }
        return o;
    }

    // ---- compress / decompress ---------------------------------------------------------------

    static int Transform(string root, Options o)
    {
        var files = Directory.Exists(root)
            ? Directory.EnumerateFiles(root, "*", new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint })
            : [root];
        var seen = new HashSet<(uint, ulong)>();
        var selected = files.Where(f => Selected(f, o) && (!o.HardLinks || seen.Add(FileId(f)))).ToList();
        var algo = AlgorithmFor(o.Compressor, o.Level);
        var failed = 0;
        Parallel.ForEach(selected, new ParallelOptions { MaxDegreeOfParallelism = o.Threads }, file =>
        {
            string error;
            try { error = o.Decompress ? Decompress(file) : Compress(file, algo, o); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { error = e.Message; }
            if (error != null)
            {
                Interlocked.Increment(ref failed);
                if (o.List || o.Decompress) Console.Error.WriteLine($"{file}: {error}");
            }
            else if (o.Verbose && !Directory.Exists(root)) lock (selected) ReportFile(file);
        });
        if (o.Verbose && Directory.Exists(root)) ReportFolder(root, o);
        // afsctool exits 0 when files merely turn out incompressible; real errors are reported above.
        return o.Decompress && failed > 0 ? 1 : 0;
    }

    static bool Selected(string file, Options o)
    {
        if (o.Extensions.Count == 0) return true;
        var match = o.Extensions.Contains(Path.GetExtension(file).TrimStart('.').ToLowerInvariant());
        return match != o.Invert;
    }

    // Returns null on success, else why the file was left uncompressed.
    static string Compress(string file, Algo algo, Options o)
    {
        var info = new FileInfo(file);
        if (info.Length == 0) return "empty file";
        if (info.Length > o.MaxSize) return "larger than the -m limit";
        if (CurrentAlgorithm(file) == algo) return null;
        var hash = o.NoVerify ? null : Hash(file);
        if (o.Backup) File.Copy(file, file + ".bak", overwrite: true);

        var written = info.LastWriteTimeUtc;
        var readOnly = info.IsReadOnly;
        if (readOnly) info.IsReadOnly = false;
        try
        {
            if (CurrentAlgorithm(file) != Algo.None) Decompress(file);
            var error = algo == Algo.Lznt1 ? SetNtfsCompression(file, true) : SetWof(file, algo);
            if (error != null) return error;
            if (hash != null && !hash.AsSpan().SequenceEqual(Hash(file)))
            {
                Decompress(file);
                return "verification failed; file left uncompressed";
            }
            if (!double.IsNaN(o.MinSavings))
            {
                var (size, onDisk) = Sizes(file);
                if (size > 0 && 100.0 * (size - onDisk) / size < o.MinSavings)
                {
                    Decompress(file);
                    return $"saves less than {o.MinSavings}%";
                }
            }
            return null;
        }
        finally
        {
            File.SetLastWriteTimeUtc(file, written);
            if (readOnly) new FileInfo(file).IsReadOnly = true;
        }
    }

    static string Decompress(string file)
    {
        var algo = CurrentAlgorithm(file);
        if (algo == Algo.None) return null;
        var written = File.GetLastWriteTimeUtc(file);
        using (var h = Open(file, write: true))
        {
            if (algo == Algo.Lznt1) { if (SetNtfsCompression(h, false) is { } e) return e; }
            // WOF expands the file and then reports ERROR_OBJECT_NOT_EXTERNALLY_BACKED from the layer
            // below it, so that error means success.
            else if (!DeviceIoControl(h, FSCTL_DELETE_EXTERNAL_BACKING, IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero)
                     && Marshal.GetLastWin32Error() is var err && err != ERROR_OBJECT_NOT_EXTERNALLY_BACKED)
                return new System.ComponentModel.Win32Exception(err).Message;
        }
        File.SetLastWriteTimeUtc(file, written);
        return null;
    }

    static unsafe string SetWof(string file, Algo algo)
    {
        using var h = Open(file, write: true);
        // WOF_EXTERNAL_INFO { Version=1, Provider=WOF_PROVIDER_FILE } + FILE_PROVIDER_EXTERNAL_INFO_V1.
        var input = stackalloc uint[] { 1, 2, 1, (uint)algo, 0 };
        if (DeviceIoControl(h, FSCTL_SET_EXTERNAL_BACKING, (IntPtr)input, 20, IntPtr.Zero, 0, out _, IntPtr.Zero)) return null;
        var err = Marshal.GetLastWin32Error();
        return err switch
        {
            ERROR_COMPRESSION_NOT_BENEFICIAL => "not compressible",
            ERROR_INVALID_FUNCTION or ERROR_NOT_SUPPORTED => "this volume does not support Windows Overlay Filter compression (try -T LZNT1)",
            _ => new System.ComponentModel.Win32Exception(err).Message,
        };
    }

    static string SetNtfsCompression(string file, bool on)
    {
        using var h = Open(file, write: true);
        return SetNtfsCompression(h, on);
    }

    static unsafe string SetNtfsCompression(SafeFileHandle h, bool on)
    {
        ushort format = on ? (ushort)1 : (ushort)0;   // COMPRESSION_FORMAT_DEFAULT (LZNT1) / NONE
        if (DeviceIoControl(h, FSCTL_SET_COMPRESSION, (IntPtr)(&format), 2, IntPtr.Zero, 0, out _, IntPtr.Zero)) return null;
        var err = Marshal.GetLastWin32Error();
        return err is ERROR_INVALID_FUNCTION or ERROR_NOT_SUPPORTED
            ? "this volume does not support NTFS compression"
            : new System.ComponentModel.Win32Exception(err).Message;
    }

    // ---- reporting ---------------------------------------------------------------------------

    static unsafe Algo CurrentAlgorithm(string file)
    {
        using var h = Open(file, write: false);
        var output = stackalloc uint[5];
        if (DeviceIoControl(h, FSCTL_GET_EXTERNAL_BACKING, IntPtr.Zero, 0, (IntPtr)output, 20, out _, IntPtr.Zero)
            && output[1] == 2)   // WOF_PROVIDER_FILE
            return (Algo)output[3];
        return (File.GetAttributes(file) & FileAttributes.Compressed) != 0 ? Algo.Lznt1 : Algo.None;
    }

    // (logical size, bytes actually allocated on disk). For WOF files the data lives in the
    // WofCompressedData stream and the main stream is sparse, so its allocation is added.
    static (long Size, long OnDisk) Sizes(string file)
    {
        var size = new FileInfo(file).Length;
        var low = GetCompressedFileSizeW(file, out var high);
        var onDisk = low == uint.MaxValue && Marshal.GetLastWin32Error() != 0 ? size : ((long)high << 32) | low;
        return (size, onDisk);
    }

    internal static string Algorithm(Algo a) => a switch
    {
        Algo.Lznt1 => "LZNT1 (NTFS compression)",
        Algo.Xpress4K => "XPRESS4K (Windows Overlay Filter)",
        Algo.Xpress8K => "XPRESS8K (Windows Overlay Filter)",
        Algo.Xpress16K => "XPRESS16K (Windows Overlay Filter)",
        Algo.Lzx => "LZX (Windows Overlay Filter)",
        _ => "none",
    };

    internal static string FormatSize(long bytes)
    {
        static string Round(double v) => v.ToString(v < 10 ? "0.#" : "0", System.Globalization.CultureInfo.InvariantCulture);
        return $"{bytes} bytes / {Round(bytes / 1000.0)} KB (kilobytes) / {Round(bytes / 1024.0)} KiB (kibibytes)";
    }

    internal static string Savings(long size, long onDisk) =>
        size == 0 ? "0.0%" : (100.0 * (size - onDisk) / size).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "%";

    static void ReportFile(string file)
    {
        var algo = CurrentAlgorithm(file);
        var (size, onDisk) = Sizes(file);
        Console.WriteLine($"{file}:");
        Console.WriteLine(algo == Algo.None ? "File is not compressed." : "File is compressed.");
        if (algo != Algo.None) Console.WriteLine($"Compression type: {Algorithm(algo)}");
        Console.WriteLine($"File size (uncompressed): {FormatSize(size)}");
        Console.WriteLine($"File size (on disk): {FormatSize(onDisk)}");
        if (algo != Algo.None) Console.WriteLine($"Compression savings: {Savings(size, onDisk)}");
    }

    static void ReportFolder(string root, Options o)
    {
        long files = 0, folders = 0, compressed = 0, size = 0, onDisk = 0;
        var seen = new HashSet<(uint, ulong)>();
        var enumeration = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint };
        foreach (var dir in Directory.EnumerateDirectories(root, "*", enumeration)) folders++;
        foreach (var file in Directory.EnumerateFiles(root, "*", enumeration))
        {
            if (!Selected(file, o) || (o.HardLinks && !seen.Add(FileId(file)))) continue;
            files++;
            Algo algo;
            (long, long) s;
            try { algo = CurrentAlgorithm(file); s = Sizes(file); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { continue; }
            size += s.Item1;
            onDisk += s.Item2;
            if (algo == Algo.None) continue;
            compressed++;
            if (o.List) Console.WriteLine(o.Verbose ? $"{file}: {Algorithm(algo)}, {Savings(s.Item1, s.Item2)} saved" : file);
        }
        if (o.List && !o.Verbose) return;
        Console.WriteLine($"{root}:");
        Console.WriteLine($"Number of compressed files: {compressed}");
        Console.WriteLine($"Total number of files: {files}");
        Console.WriteLine($"Total number of folders: {folders}");
        Console.WriteLine($"Total number of items (number of files + number of folders): {files + folders}");
        Console.WriteLine($"Folder size (uncompressed): {FormatSize(size)}");
        Console.WriteLine($"Folder size (on disk): {FormatSize(onDisk)}");
        Console.WriteLine($"Compression savings: {Savings(size, onDisk)}");
    }

    static byte[] Hash(string file)
    {
        using var stream = File.OpenRead(file);
        return SHA256.HashData(stream);
    }

    static (uint, ulong) FileId(string file)
    {
        using var h = Open(file, write: false);
        return GetFileInformationByHandle(h, out var info)
            ? (info.VolumeSerialNumber, ((ulong)info.FileIndexHigh << 32) | info.FileIndexLow)
            : (0, (ulong)file.GetHashCode());
    }

    static SafeFileHandle Open(string file, bool write) =>
        File.OpenHandle(file, FileMode.Open, write ? FileAccess.ReadWrite : FileAccess.Read, FileShare.ReadWrite);

    const uint FSCTL_SET_COMPRESSION = 0x9C040, FSCTL_SET_EXTERNAL_BACKING = 0x9030C,
        FSCTL_GET_EXTERNAL_BACKING = 0x90310, FSCTL_DELETE_EXTERNAL_BACKING = 0x90314;
    const int ERROR_INVALID_FUNCTION = 1, ERROR_NOT_SUPPORTED = 50, ERROR_COMPRESSION_NOT_BENEFICIAL = 344,
        ERROR_OBJECT_NOT_EXTERNALLY_BACKED = 342;

    [StructLayout(LayoutKind.Sequential)]
    struct BY_HANDLE_FILE_INFORMATION
    {
        public uint FileAttributes;
        public long CreationTime, LastAccessTime, LastWriteTime;
        public uint VolumeSerialNumber, FileSizeHigh, FileSizeLow, NumberOfLinks, FileIndexHigh, FileIndexLow;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool DeviceIoControl(SafeFileHandle h, uint code, IntPtr input, int inputSize, IntPtr output, int outputSize, out int returned, IntPtr overlapped);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern uint GetCompressedFileSizeW(string file, out uint high);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GetFileInformationByHandle(SafeFileHandle h, out BY_HANDLE_FILE_INFORMATION info);
}
