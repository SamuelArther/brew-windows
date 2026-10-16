using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WinTools;

// trash / rmtrash / osx-trash / macos-trash: move files to the Recycle Bin (SHFileOperation with
// FOF_ALLOWUNDO), like macOS's Trash. `trash -l` lists and `trash -e` empties it.
static class Trash
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("trash", "Move files and folders to the Recycle Bin", Run);
        Commands.Add("rmtrash", "Move files to the Recycle Bin (rm-compatible flags)", RunRm);
    }

    static int Run(string[] args)
    {
        var paths = new List<string>();
        foreach (var arg in args)
        {
            switch (arg)
            {
                case "-h" or "--help":
                    Console.WriteLine("Usage: trash [-v] [-e|--empty] [-l|--list] <path>...\nMove files and folders to the Recycle Bin.");
                    return 0;
                case "-e" or "--empty": return Empty();
                case "-l" or "--list": return List();
                case "-v" or "--verbose" or "-F" or "-y": break;   // accepted for compatibility
                default:
                    if (arg.StartsWith('-') && arg != "-") throw new UsageException($"unknown option {arg}");
                    paths.Add(arg);
                    break;
            }
        }
        if (paths.Count == 0) throw new UsageException("no paths given (see trash --help)");
        return Recycle(paths, ignoreMissing: false);
    }

    // rmtrash accepts rm's flags (-r, -f, -i ...); the Recycle Bin makes them moot except -f.
    static int RunRm(string[] args)
    {
        var force = false;
        var paths = new List<string>();
        var options = true;
        foreach (var arg in args)
        {
            if (options && arg == "--") { options = false; continue; }
            if (options && arg is "-h" or "--help")
            {
                Console.WriteLine("Usage: rmtrash [-f] [-r] <path>...\nLike rm, but moves to the Recycle Bin.");
                return 0;
            }
            if (options && arg.StartsWith('-') && arg.Length > 1) { force |= arg.Contains('f'); continue; }
            paths.Add(arg);
        }
        if (paths.Count == 0) throw new UsageException("missing operand");
        return Recycle(paths, ignoreMissing: force);
    }

    static int Recycle(List<string> paths, bool ignoreMissing)
    {
        var status = 0;
        foreach (var path in paths)
        {
            var full = Path.GetFullPath(path);
            if (!File.Exists(full) && !Directory.Exists(full))
            {
                if (!ignoreMissing) { Console.Error.WriteLine($"trash: {path}: No such file or directory"); status = 1; }
                continue;
            }
            var op = new SHFILEOPSTRUCT
            {
                wFunc = FO_DELETE,
                pFrom = full + "\0\0",
                fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_NOERRORUI | FOF_SILENT,
            };
            var result = SHFileOperation(ref op);
            if (result != 0 || op.fAnyOperationsAborted)
            {
                Console.Error.WriteLine($"trash: {path}: could not move to the Recycle Bin (error {result})");
                status = 1;
            }
        }
        return status;
    }

    static int Empty()
    {
        var hr = SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
        // E_UNEXPECTED is what an already-empty bin returns.
        return hr == 0 || hr == unchecked((int)0x8000FFFF) ? 0 : 1;
    }

    static int List()
    {
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
        {
            var bin = Path.Combine(drive.RootDirectory.FullName, "$Recycle.Bin");
            if (!Directory.Exists(bin)) continue;
            IEnumerable<string> infos;
            try { infos = Directory.EnumerateFiles(bin, "$I*", SearchOption.AllDirectories).ToList(); }
            catch (UnauthorizedAccessException) { continue; }
            foreach (var info in infos)
            {
                // $I files: 8-byte version, 8-byte size, 8-byte FILETIME, then the original path.
                try
                {
                    var data = File.ReadAllBytes(info);
                    if (data.Length < 28) continue;
                    var version = BitConverter.ToInt64(data, 0);
                    var original = version >= 2
                        ? System.Text.Encoding.Unicode.GetString(data, 28, BitConverter.ToInt32(data, 24) * 2).TrimEnd('\0')
                        : System.Text.Encoding.Unicode.GetString(data, 24, Math.Min(520, data.Length - 24)).TrimEnd('\0');
                    Console.WriteLine(original);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
        return 0;
    }

    const uint FO_DELETE = 3;
    const ushort FOF_SILENT = 0x4, FOF_NOCONFIRMATION = 0x10, FOF_ALLOWUNDO = 0x40, FOF_NOERRORUI = 0x400;
    const uint SHERB_NOCONFIRMATION = 1, SHERB_NOPROGRESSUI = 2, SHERB_NOSOUND = 4;

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

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHFileOperation(ref SHFILEOPSTRUCT op);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHEmptyRecycleBin(IntPtr hwnd, string root, uint flags);
}
