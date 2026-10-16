using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WinTools;

// launch: opens documents, folders, URLs and applications through ShellExecuteEx (Explorer's
// file associations), with launch's flags: -n to print, -f for file info, -p to print documents,
// -a to pick the application, -s to run elevated, -w to wait.
static class Launch
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("launch", "Open documents, folders, URLs and applications (like macOS open)", Run);
    }

    const string Usage = """
        usage: launch [-npswbmhLU] [-i AppUserModelID] [-u URL] [-a name|path] [-o argument] [item ...] [-]
           or: launch [-npflswbmhLU] [-o argument] item ...
          -n            print matching paths/URLs instead of opening them
          -p            ask application(s) to print document(s)
          -f            display information about item(s)
          -l            launch URLs (treat http:// URLs as documents to open)
          -s            launch target(s) as administrator (UAC prompt)
          -w            wait for application to finish opening before exiting
          -b            launch application in the background (don't activate it)
          -m            launch application again, even if already running
          -h            hide (minimize) application once it's finished opening
          -L            suppress normal startup processing (accepted, no effect)
          -U            interpret items as URLs, even if same-named files exist
          -i ID         match application by AppUserModelID (Microsoft.WindowsNotepad_8wekyb3d8bbwe!App)
          -u URL        open application at file:// URL
          -a name|path  match application by name or path (notepad, "C:\Tools\app.exe")
          -o argument   pass argument to application (may be specified more than once)
        'document' may be a file, folder, or drive - whatever the application can open.
        'item' may be an application, document or URL. "-" opens standard input as a document.
        """;

    static int Run(string[] args)
    {
        bool print = false, printDocs = false, info = false, admin = false, wait = false, background = false, hide = false, urls = false;
        string app = null, aumid = null;
        var appArgs = new List<string>();
        var items = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith('-') || arg == "-" || arg.Length == 1) { items.Add(arg); continue; }
            if (arg == "--help") { Console.WriteLine(Usage); return 0; }
            if (arg == "--version") { Console.WriteLine($"launch {Commands.Version} (wintools)"); return 0; }
            for (var j = 1; j < arg.Length; j++)
            {
                var c = arg[j];
                string Value()
                {
                    if (j + 1 < arg.Length) { var v = arg[(j + 1)..]; j = arg.Length; return v; }
                    if (++i >= args.Length) throw new UsageException($"option requires an argument -- {c}");
                    j = arg.Length;
                    return args[i];
                }
                switch (c)
                {
                    case 'n': print = true; break;
                    case 'p': printDocs = true; break;
                    case 'f': info = true; break;
                    case 'l': break;
                    case 's': admin = true; break;
                    case 'w': wait = true; break;
                    case 'b': background = true; break;
                    case 'm' or 'L' or 'C' or 'X': break;
                    case 'h': hide = true; break;
                    case 'U': urls = true; break;
                    case 'a': app = Value(); break;
                    case 'i': aumid = Value(); break;
                    case 'u':
                        var u = new Uri(Value());
                        if (!u.IsFile) throw new UsageException("-u needs a file:// URL");
                        app = u.LocalPath;
                        break;
                    case 'o': appArgs.Add(Value()); break;
                    case 'c':
                        Console.Error.WriteLine("launch: Windows applications have no creator codes; use -a or -i.");
                        return 1;
                    default: throw new UsageException($"illegal option -- {c}\n{Usage}");
                }
            }
        }
        if (items.Count == 0 && app == null && aumid == null) throw new UsageException("no items given\n" + Usage);
        // "-" reads standard input into a temporary text document.
        items = items.Select(item =>
        {
            if (item != "-") return item;
            var temp = Path.Combine(Path.GetTempPath(), $"launch-stdin-{Environment.ProcessId}.txt");
            using (var stdin = Console.OpenStandardInput()) using (var file = File.Create(temp)) stdin.CopyTo(file);
            return temp;
        }).ToList();
        var resolved = items.Select(item => IsUrl(item) && (urls || !File.Exists(item) && !Directory.Exists(item)) ? item : Path.GetFullPath(item)).ToList();
        foreach (var item in resolved.Where(r => !IsUrl(r) && !File.Exists(r) && !Directory.Exists(r)))
            throw new IOException($"can't find item '{item}'");
        var appPath = app != null ? FindApp(app) : null;
        if (app != null && appPath == null) throw new IOException($"can't find application '{app}'");

        if (info)
        {
            foreach (var item in resolved) Info(item);
            return 0;
        }
        if (print)
        {
            if (appPath != null) Console.WriteLine(appPath);
            else if (aumid != null) Console.WriteLine($"shell:AppsFolder\\{aumid}");
            foreach (var item in resolved) Console.WriteLine(item);
            return 0;
        }
        var status = 0;
        var show = hide ? SW_SHOWMINNOACTIVE : background ? SW_SHOWNOACTIVATE : SW_SHOWNORMAL;
        if (aumid != null)
        {
            // Packaged apps start through their AppsFolder entry; documents are passed as arguments.
            status |= Shell($"shell:AppsFolder\\{aumid}", null, Join(appArgs.Concat(resolved)), admin ? "runas" : null, show, wait);
        }
        else if (appPath != null)
        {
            status |= Shell(appPath, null, Join(appArgs.Concat(resolved)), admin ? "runas" : null, show, wait);
        }
        else
        {
            foreach (var item in resolved)
            {
                var verb = printDocs ? "print" : admin ? "runas" : null;
                var isApp = item.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
                status |= Shell(item, IsUrl(item) ? null : Path.GetDirectoryName(item), isApp && appArgs.Count > 0 ? Join(appArgs) : null, verb, show, wait);
            }
        }
        return status;
    }

    static bool IsUrl(string s) =>
        s.Length > 3 && (s.Contains("://") || s.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) || s.StartsWith("ms-", StringComparison.OrdinalIgnoreCase) && s.Contains(':'))
        && char.IsLetter(s[0]) && s.IndexOf(':') > 1;

    static string Join(IEnumerable<string> args) =>
        string.Join(' ', args.Select(a => a.Length > 0 && !a.Any(c => c is ' ' or '\t' or '"') ? a : "\"" + a.Replace("\"", "\\\"") + "\""));

    // An application by path, by name on PATH, or through the App Paths registry (like Win+R does).
    static string FindApp(string name)
    {
        if (File.Exists(name)) return Path.GetFullPath(name);
        var exe = name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name : name + ".exe";
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(dir.Trim('"'), exe);
            if (File.Exists(candidate)) return candidate;
        }
        var found = AssocString(ASSOCSTR_EXECUTABLE, exe, "open", ASSOCF_OPEN_BYEXENAME);
        return found != null && File.Exists(found) ? found : null;
    }

    static int Shell(string file, string directory, string parameters, string verb, int show, bool wait)
    {
        var info = new SHELLEXECUTEINFOW
        {
            cbSize = Marshal.SizeOf<SHELLEXECUTEINFOW>(),
            fMask = SEE_MASK_NOCLOSEPROCESS | SEE_MASK_FLAG_NO_UI | SEE_MASK_NOASYNC,
            lpVerb = verb,
            lpFile = file,
            lpParameters = parameters,
            lpDirectory = directory,
            nShow = show,
        };
        if (!ShellExecuteExW(ref info))
        {
            var error = Marshal.GetLastWin32Error();
            Console.Error.WriteLine($"launch: can't open '{file}': {new System.ComponentModel.Win32Exception(error).Message}");
            return 1;
        }
        if (info.hProcess != IntPtr.Zero)
        {
            if (wait) WaitForInputIdle(info.hProcess, 60000);
            CloseHandle(info.hProcess);
        }
        return 0;
    }

    // launch -f: what the file is and what opens it.
    static void Info(string item)
    {
        if (IsUrl(item))
        {
            var scheme = item[..item.IndexOf(':')];
            Console.WriteLine($"{item}: URL");
            Console.WriteLine($"\tscheme: {scheme}");
            var handler = AssocString(ASSOCSTR_FRIENDLYAPPNAME, scheme, "open", 0);
            if (handler != null) Console.WriteLine($"\tapplication: {handler} ({AssocString(ASSOCSTR_EXECUTABLE, scheme, "open", 0)})");
            return;
        }
        var isDir = Directory.Exists(item);
        FileSystemInfo fsi = isDir ? new DirectoryInfo(item) : new FileInfo(item);
        var ext = isDir ? null : Path.GetExtension(item);
        string kind;
        if (isDir) kind = Path.GetPathRoot(item) == item ? "drive" : "folder";
        else if (ext.Equals(".exe", StringComparison.OrdinalIgnoreCase)) kind = "application";
        else kind = "document";
        Console.WriteLine($"{item}: {kind}");
        if (!isDir && ext.Length > 0)
        {
            Console.WriteLine($"\ttype: {ext.ToLowerInvariant()}");
            if (AssocString(ASSOCSTR_FRIENDLYDOCNAME, ext, null, 0) is { } doc) Console.WriteLine($"\tkind: {doc}");
            if (AssocString(ASSOCSTR_CONTENTTYPE, ext, null, 0) is { } mime) Console.WriteLine($"\tcontent type: {mime}");
            var exe = AssocString(ASSOCSTR_EXECUTABLE, ext, "open", 0);
            if (exe != null && !kind.Equals("application"))
                Console.WriteLine($"\tapplication: {AssocString(ASSOCSTR_FRIENDLYAPPNAME, ext, "open", 0) ?? Path.GetFileNameWithoutExtension(exe)} ({exe})");
        }
        if (!isDir)
        {
            var version = FileVersionInfo.GetVersionInfo(item);
            if (version.FileDescription is { Length: > 0 } d) Console.WriteLine($"\tdescription: {d}");
            if (version.FileVersion is { Length: > 0 } v) Console.WriteLine($"\tversion: {v}");
            Console.WriteLine($"\tlength: {((FileInfo)fsi).Length:N0} bytes");
        }
        else if (Path.GetPathRoot(item) == item && new DriveInfo(item) is { IsReady: true } drive)
        {
            Console.WriteLine($"\tvolume: {drive.VolumeLabel} ({drive.DriveFormat}, {drive.DriveType})");
            Console.WriteLine($"\tfree: {drive.AvailableFreeSpace:N0} of {drive.TotalSize:N0} bytes");
        }
        else
        {
            try { Console.WriteLine($"\tcontents: {Directory.EnumerateFileSystemEntries(item).Count()} items"); }
            catch (UnauthorizedAccessException) { }
        }
        var attributes = fsi.Attributes & ~(FileAttributes.Directory | FileAttributes.Normal | FileAttributes.Archive);
        if (attributes != 0) Console.WriteLine($"\tattributes: {attributes}");
        Console.WriteLine($"\tcreated: {fsi.CreationTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"\tmodified: {fsi.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
    }

    static unsafe string AssocString(int what, string assoc, string extra, int flags)
    {
        var buffer = stackalloc char[1024];
        uint size = 1024;
        return AssocQueryStringW(flags, what, assoc, extra, buffer, ref size) == 0 && size > 1 ? new string(buffer, 0, (int)size - 1) : null;
    }

    const int SW_SHOWNORMAL = 1, SW_SHOWMINNOACTIVE = 7, SW_SHOWNOACTIVATE = 4;
    const uint SEE_MASK_NOCLOSEPROCESS = 0x40, SEE_MASK_FLAG_NO_UI = 0x400, SEE_MASK_NOASYNC = 0x100;
    const int ASSOCF_OPEN_BYEXENAME = 0x2;
    const int ASSOCSTR_EXECUTABLE = 2, ASSOCSTR_FRIENDLYDOCNAME = 3, ASSOCSTR_FRIENDLYAPPNAME = 4, ASSOCSTR_CONTENTTYPE = 14;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct SHELLEXECUTEINFOW
    {
        public int cbSize;
        public uint fMask;
        public IntPtr hwnd;
        public string lpVerb, lpFile, lpParameters, lpDirectory;
        public int nShow;
        public IntPtr hInstApp, lpIDList;
        public string lpClass;
        public IntPtr hkeyClass;
        public uint dwHotKey;
        public IntPtr hIcon, hProcess;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool ShellExecuteExW(ref SHELLEXECUTEINFOW info);

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    static extern unsafe int AssocQueryStringW(int flags, int str, string assoc, string extra, char* output, ref uint size);

    [DllImport("user32.dll")]
    static extern uint WaitForInputIdle(IntPtr process, uint milliseconds);

    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr handle);
}
