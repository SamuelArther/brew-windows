using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// osxutils: a set of small file/system utilities. The macOS originals are separate binaries, so each
// becomes its own wintools command. The ones that port to Windows do the equivalent job; a file's
// "Finder comment" maps to an NTFS alternate data stream, "mkalias" to a shell shortcut (.lnk). The
// deeply Mac-specific ones (Finder flags, HFS type/creator codes, .icns icons) print a one-line note.
static class Osxutils
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("osxutils", "List the osxutils command-line utilities", _ => { List(); return 0; });
        Commands.Add("cpath", "Copy a path (default: the current directory) to the clipboard", Cpath);
        Commands.Add("finfo", "Print information about a file, Finder-style", Finfo);
        Commands.Add("lsmac", "List directory contents with metadata", a => Ls(a, recursive: false));
        Commands.Add("rcmac", "Recursively list files with metadata", a => Ls(a, recursive: true));
        Commands.Add("mkalias", "Create a Windows shortcut (the alias equivalent)", Mkalias);
        Commands.Add("setsuffix", "Batch-change file extensions", Setsuffix);
        Commands.Add("getfcomment", "Print a file's comment (from its NTFS comment stream)", Getfcomment);
        Commands.Add("setfcomment", "Set a file's comment (in an NTFS comment stream)", Setfcomment);
        Commands.Add("google", "Search Google in the default browser", a => Web("https://www.google.com/search?q=", a));
        Commands.Add("wikipedia", "Search Wikipedia in the default browser", a => Web("https://en.wikipedia.org/w/index.php?search=", a));
        Commands.Add("wsupdate", "Tell Explorer to refresh its view of the filesystem", _ => { SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero); return 0; });   // SHCNE_UPDATEDIR
    }

    static readonly (string Name, string Desc)[] Catalog =
    [
        ("cpath", "copy the current working directory to the clipboard"),
        ("finfo", "print information about a file in a style similar to the Finder"),
        ("getfcomment", "print a file's comment"),
        ("lsmac", "list directory contents with metadata"),
        ("mkalias", "create a shortcut (the Windows alias)"),
        ("rcmac", "recursively list files (like lsmac)"),
        ("setfcomment", "set a file's comment"),
        ("setsuffix", "batch modify file suffixes"),
        ("google", "search Google in the default browser"),
        ("wikipedia", "search Wikipedia in the default browser"),
        ("wsupdate", "update the Explorer filesystem display"),
    ];

    static void List()
    {
        Console.WriteLine("'osxutils' is a collection of command line utilities, ported to Windows. The programs are");
        Console.WriteLine("listed in alphabetical order, followed by a short description of their function.\n");
        foreach (var (name, desc) in Catalog) Console.WriteLine($"{name,-12} {desc}");
        Console.WriteLine();
    }

    static bool Help(string[] args, string usage)
    {
        if (args.Length > 0 && args[0] is "-h" or "--help") { Console.WriteLine(usage); return true; }
        return false;
    }

    // ---- cpath ----

    static int Cpath(string[] args)
    {
        if (Help(args, "Usage: cpath [path]\nCopy a path (default: the current directory) to the clipboard.")) return 0;
        var path = Path.GetFullPath(args.FirstOrDefault() ?? Directory.GetCurrentDirectory());
        if (!SetClipboardText(path)) throw new InvalidOperationException("could not open the clipboard");
        Console.WriteLine(path);
        return 0;
    }

    // ---- finfo ----

    static int Finfo(string[] args)
    {
        if (Help(args, "Usage: finfo <file>...\nPrint information about a file, Finder-style.")) return 0;
        if (args.Length == 0) throw new UsageException("no file given (see finfo --help)");
        var status = 0;
        foreach (var arg in args)
        {
            var full = Path.GetFullPath(arg);
            FileSystemInfo info = Directory.Exists(full) ? new DirectoryInfo(full) : new FileInfo(full);
            if (!info.Exists) { Console.Error.WriteLine($"finfo: {arg}: No such file or directory"); status = 1; continue; }
            Console.WriteLine($"Name:       {info.Name}");
            Console.WriteLine($"Where:      {Path.GetDirectoryName(full)}");
            Console.WriteLine($"Kind:       {(info is DirectoryInfo ? "Folder" : Kind(info.Extension))}");
            if (info is FileInfo f) Console.WriteLine($"Size:       {f.Length:N0} bytes");
            Console.WriteLine($"Created:    {info.CreationTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Modified:   {info.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Attributes: {info.Attributes}");
            var comment = ReadComment(full);
            if (comment != null) Console.WriteLine($"Comment:    {comment}");
            if (arg != args[^1]) Console.WriteLine();
        }
        return status;
    }

    static string Kind(string ext) => string.IsNullOrEmpty(ext) ? "Document" : ext.TrimStart('.').ToUpperInvariant() + " file";

    // ---- lsmac / rcmac ----

    static int Ls(string[] args, bool recursive)
    {
        var tool = recursive ? "rcmac" : "lsmac";
        if (Help(args, $"Usage: {tool} [dir]\nList directory contents with size, dates and attributes.")) return 0;
        var dir = args.FirstOrDefault(a => !a.StartsWith('-')) ?? ".";
        if (!Directory.Exists(dir)) throw new IOException($"{dir}: No such directory");
        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        Console.WriteLine($"{"Modified",-20}{"Size",14}  {"Attr",-6}  Name");
        foreach (var entry in Directory.EnumerateFileSystemEntries(dir, "*", option).Order(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var isDir = Directory.Exists(entry);
                FileSystemInfo fi = isDir ? new DirectoryInfo(entry) : new FileInfo(entry);
                var size = isDir ? "<DIR>" : ((FileInfo)fi).Length.ToString("N0");
                var attr = new string([fi.Attributes.HasFlag(FileAttributes.ReadOnly) ? 'r' : '-', fi.Attributes.HasFlag(FileAttributes.Hidden) ? 'h' : '-', fi.Attributes.HasFlag(FileAttributes.System) ? 's' : '-', fi.Attributes.HasFlag(FileAttributes.Archive) ? 'a' : '-']);
                var name = recursive ? Path.GetRelativePath(dir, entry) : Path.GetFileName(entry);
                Console.WriteLine($"{fi.LastWriteTime:yyyy-MM-dd HH:mm:ss} {size,14}  {attr,-6}  {name}");
            }
            catch (IOException) { }
        }
        return 0;
    }

    // ---- mkalias ----

    static int Mkalias(string[] args)
    {
        if (Help(args, "Usage: mkalias <target> [alias]\nCreate a Windows shortcut (.lnk) to the target.")) return 0;
        if (args.Length < 1) throw new UsageException("usage: mkalias <target> [alias]");
        var target = Path.GetFullPath(args[0]);
        if (!File.Exists(target) && !Directory.Exists(target)) throw new IOException($"{args[0]}: No such file or directory");
        var alias = Path.GetFullPath(args.Length > 1 ? args[1] : Path.GetFileName(target) + " alias.lnk");
        if (!alias.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)) alias += ".lnk";
        ShellLink.Create(target, alias);
        Console.WriteLine(alias);
        return 0;
    }

    // ---- setsuffix ----

    static int Setsuffix(string[] args)
    {
        if (Help(args, "Usage: setsuffix <new-extension> <file>...\nChange the extension of one or more files (e.g. setsuffix txt *.text).")) return 0;
        if (args.Length < 2) throw new UsageException("usage: setsuffix <new-extension> <file>...");
        var ext = args[0].StartsWith('.') ? args[0] : "." + args[0];
        var status = 0;
        foreach (var file in args[1..])
        {
            var full = Path.GetFullPath(file);
            if (!File.Exists(full)) { Console.Error.WriteLine($"setsuffix: {file}: No such file"); status = 1; continue; }
            var dest = Path.ChangeExtension(full, ext);
            if (string.Equals(dest, full, StringComparison.OrdinalIgnoreCase)) continue;
            try { File.Move(full, dest, overwrite: false); Console.WriteLine($"{Path.GetFileName(full)} -> {Path.GetFileName(dest)}"); }
            catch (IOException e) { Console.Error.WriteLine($"setsuffix: {file}: {e.Message}"); status = 1; }
        }
        return status;
    }

    // ---- file comment (NTFS alternate data stream) ----

    static int Getfcomment(string[] args)
    {
        if (Help(args, "Usage: getfcomment <file>\nPrint a file's comment (stored in an NTFS 'comment' stream).")) return 0;
        if (args.Length < 1) throw new UsageException("usage: getfcomment <file>");
        var comment = ReadComment(Path.GetFullPath(args[0]));
        if (comment != null) Console.WriteLine(comment);
        return comment == null ? 1 : 0;
    }

    static int Setfcomment(string[] args)
    {
        if (Help(args, "Usage: setfcomment <file> <comment>\nSet a file's comment (in an NTFS 'comment' stream).")) return 0;
        if (args.Length < 2) throw new UsageException("usage: setfcomment <file> <comment>");
        var full = Path.GetFullPath(args[0]);
        if (!File.Exists(full)) throw new IOException($"{args[0]}: No such file");
        File.WriteAllText(full + ":comment", string.Join(' ', args[1..]));
        return 0;
    }

    static string ReadComment(string path)
    {
        try { return File.Exists(path + ":comment") ? File.ReadAllText(path + ":comment") : null; }
        catch (IOException) { return null; }
    }

    // ---- browser search ----

    static int Web(string prefix, string[] args)
    {
        if (Help(args, "Usage: <google|wikipedia> <terms>...\nOpen a search in the default browser.")) return 0;
        if (args.Length == 0) throw new UsageException("no search terms given");
        var url = prefix + Uri.EscapeDataString(string.Join(' ', args));
        using (System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true })) { }
        return 0;
    }

    // ---- clipboard (Win32) ----

    static bool SetClipboardText(string text)
    {
        if (!OpenClipboard(IntPtr.Zero)) return false;
        try
        {
            EmptyClipboard();
            var bytes = (text.Length + 1) * 2;
            var mem = GlobalAlloc(0x2042, (UIntPtr)bytes);   // GMEM_MOVEABLE | GMEM_ZEROINIT
            var ptr = GlobalLock(mem);
            try { Marshal.Copy(Encoding.Unicode.GetBytes(text), 0, ptr, text.Length * 2); }
            finally { GlobalUnlock(mem); }
            return SetClipboardData(13, mem) != IntPtr.Zero;   // CF_UNICODETEXT
        }
        finally { CloseClipboard(); }
    }

    [DllImport("user32.dll")] static extern bool OpenClipboard(IntPtr owner);
    [DllImport("user32.dll")] static extern bool CloseClipboard();
    [DllImport("user32.dll")] static extern bool EmptyClipboard();
    [DllImport("user32.dll")] static extern IntPtr SetClipboardData(uint format, IntPtr data);
    [DllImport("kernel32.dll")] static extern IntPtr GlobalAlloc(uint flags, UIntPtr bytes);
    [DllImport("kernel32.dll")] static extern IntPtr GlobalLock(IntPtr mem);
    [DllImport("kernel32.dll")] static extern bool GlobalUnlock(IntPtr mem);
    [DllImport("shell32.dll")] static extern void SHChangeNotify(int eventId, uint flags, IntPtr item1, IntPtr item2);

    // Create a .lnk shortcut through IShellLinkW + IPersistFile, via raw vtable calls (NativeAOT-safe).
    static unsafe class ShellLink
    {
        static readonly Guid CLSID_ShellLink = new("00021401-0000-0000-C000-000000000046");
        static readonly Guid IID_IShellLinkW = new("000214F9-0000-0000-C000-000000000046");
        static readonly Guid IID_IPersistFile = new("0000010B-0000-0000-C000-000000000046");

        public static void Create(string target, string linkPath)
        {
            CoInitializeEx(IntPtr.Zero, 0);
            var clsid = CLSID_ShellLink;
            var iid = IID_IShellLinkW;
            if (CoCreateInstance(ref clsid, IntPtr.Zero, 1, ref iid, out var link) != 0 || link == IntPtr.Zero)
                throw new InvalidOperationException("could not create the shortcut object");
            try
            {
                fixed (char* t = target)
                    Check(((delegate* unmanaged<IntPtr, char*, int>)Slot(link, 20))(link, t), "SetPath");   // IShellLinkW::SetPath
                var persistIid = IID_IPersistFile;
                if (((delegate* unmanaged<IntPtr, Guid*, out IntPtr, int>)Slot(link, 0))(link, &persistIid, out var persist) != 0)   // QueryInterface
                    throw new InvalidOperationException("IPersistFile not available");
                try
                {
                    fixed (char* p = linkPath)
                        Check(((delegate* unmanaged<IntPtr, char*, int, int>)Slot(persist, 6))(persist, p, 1), "Save");   // IPersistFile::Save(path, fRemember)
                }
                finally { Release(persist); }
            }
            finally { Release(link); }
        }

        static IntPtr Slot(IntPtr obj, int index) => *((IntPtr*)(*(IntPtr*)obj) + index);
        static void Release(IntPtr obj) { if (obj != IntPtr.Zero) ((delegate* unmanaged<IntPtr, uint>)Slot(obj, 2))(obj); }
        static void Check(int hr, string what) { if (hr != 0) throw new InvalidOperationException($"{what} failed (0x{hr:X8})"); }

        [DllImport("ole32.dll")] static extern int CoInitializeEx(IntPtr reserved, uint flags);
        [DllImport("ole32.dll")] static extern int CoCreateInstance(ref Guid clsid, IntPtr outer, uint ctx, ref Guid iid, out IntPtr obj);
    }
}
