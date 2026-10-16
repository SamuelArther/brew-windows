using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using Microsoft.Win32;

namespace WinTools;

// dockutil: the Windows taskbar is the Dock. Pinned items are the shortcuts in
// %APPDATA%\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar, ordered as in the Taskband
// "Favorites" registry blob; targets are resolved with IShellLink. --list, --find and --remove (the
// shell's own "Unpin from taskbar" verb) work. Windows has no API for programs to pin or reorder, so
// --add and --move explain that instead of guessing.
static unsafe partial class Dockutil
{
    [ModuleInitializer]
    internal static void Register() => Commands.Add("dockutil", "Manage taskbar (Dock) items", Run);

    static string PinnedFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");

    sealed record Pin(string Label, string Shortcut, string Target);

    static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            Console.WriteLine("""
                usage:     dockutil -h
                usage:     dockutil --list [--section apps|others]
                usage:     dockutil --find <label>
                usage:     dockutil --remove <label|path|all> [--no-restart]
                usage:     dockutil --add <path> [--label <label>] [--position <n|beginning|end|middle>]
                usage:     dockutil --move <label> --position <n|beginning|end|middle>
                usage:     dockutil --version

                The Dock is the Windows taskbar. Listing, finding and removing pins work; Windows doesn't
                let programs add or reorder pins (right-click an app and choose "Pin to taskbar", or drag it).
                """);
            return args.Length == 0 ? 2 : 0;
        }
        string action = null, item = null, section = null;
        for (var i = 0; i < args.Length; i++)
        {
            string Value() => ++i < args.Length ? args[i] : throw new UsageException($"{args[i - 1]} needs a value");
            switch (args[i])
            {
                case "--version" or "-V": Console.WriteLine(Commands.Version); return 0;
                case "--list" or "-L": action = "list"; break;
                case "--find" or "-F": action = "find"; item = Value(); break;
                case "--remove" or "-R": action = "remove"; item = Value(); break;
                case "--add" or "-a": action = "add"; item = Value(); break;
                case "--move" or "-m": action = "move"; item = Value(); break;
                case "--section": section = Value(); if (section is not ("apps" or "others")) throw new UsageException("--section must be apps or others"); break;
                case "--label" or "--position" or "--after" or "--before" or "--replacing" or "--type" or "--view" or "--display" or "--sort" or "--homeloc" or "--allhomes": Value(); break;
                case "--no-restart" or "--restart" or "--allhomes-flag" or "-v" or "--verbose": break;
                default:
                    if (args[i].StartsWith('-')) throw new UsageException($"unknown option {args[i]}");
                    break;   // dockutil accepts a plist path last; there's only one taskbar
            }
        }
        try
        {
            switch (action)
            {
                case "list":
                    if (section == "others") return 0;   // no folders/stacks on the taskbar
                    foreach (var p in Pins()) Console.WriteLine($"{p.Label}\t{Uri(p.Target)}\tpersistentApps\t{PinnedFolder}");
                    return 0;
                case "find":
                    var pins = Pins();
                    var index = pins.FindIndex(p => Matches(p, item));
                    if (index < 0) { Console.WriteLine($"{item} was not found in {PinnedFolder}"); return 1; }
                    Console.WriteLine($"{item} was found in persistentApps at slot {index + 1} in {PinnedFolder}");
                    return 0;
                case "remove":
                    var targets = item == "all" ? Pins() : Pins().Where(p => Matches(p, item)).ToList();
                    if (targets.Count == 0) { Console.Error.WriteLine($"No matching item found for {item}"); return 1; }
                    var status = 0;
                    foreach (var p in targets)
                    {
                        if (Unpin(p)) Console.WriteLine($"Removed {p.Label}");
                        else { Console.Error.WriteLine($"dockutil: Windows refused to unpin {p.Label}; right-click it on the taskbar and choose \"Unpin from taskbar\""); status = 1; }
                    }
                    return status;
                case "add":
                    Console.Error.WriteLine($"dockutil: Windows doesn't let programs pin to the taskbar; right-click {Path.GetFileName(item)} and choose \"Pin to taskbar\"");
                    return 1;
                case "move":
                    Console.Error.WriteLine("dockutil: Windows doesn't let programs reorder taskbar pins; drag the icon instead");
                    return 1;
                default:
                    throw new UsageException("no action given (--list, --find, --remove, --add or --move)");
            }
        }
        catch (COMException e) { throw new InvalidOperationException($"the shell failed (0x{e.HResult:x8})"); }
    }

    static bool Matches(Pin p, string item) =>
        p.Label.Equals(item, StringComparison.OrdinalIgnoreCase)
        || (p.Target != null && (p.Target.Equals(item, StringComparison.OrdinalIgnoreCase)
                                 || Path.GetFileNameWithoutExtension(p.Target).Equals(item, StringComparison.OrdinalIgnoreCase)
                                 || p.Target.Equals(Path.GetFullPath(item), StringComparison.OrdinalIgnoreCase)));

    static string Uri(string target) => target == null ? "" : target.Contains("://") ? target : new Uri(target).AbsoluteUri;

    // Shortcuts in the pinned folder, in taskbar order (where each name first appears in the Favorites blob).
    static List<Pin> Pins()
    {
        if (!Directory.Exists(PinnedFolder)) return [];
        var blob = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Taskband")?.GetValue("Favorites") as byte[] ?? [];
        var text = Encoding.Unicode.GetString(blob, 0, blob.Length & ~1);
        var odd = blob.Length > 1 ? Encoding.Unicode.GetString(blob, 1, (blob.Length - 1) & ~1) : "";
        int Position(string file)
        {
            var at = text.IndexOf(file, StringComparison.OrdinalIgnoreCase);
            if (at < 0) at = odd.IndexOf(file, StringComparison.OrdinalIgnoreCase);
            return at < 0 ? int.MaxValue : at;
        }
        return Directory.GetFiles(PinnedFolder, "*.lnk")
            .Select(f => new Pin(Path.GetFileNameWithoutExtension(f), f, ResolveShortcut(f)))
            .OrderBy(p => Position(Path.GetFileName(p.Shortcut))).ThenBy(p => p.Label, StringComparer.OrdinalIgnoreCase).ToList();
    }

    static bool Unpin(Pin p)
    {
        var info = new SHELLEXECUTEINFOW
        {
            cbSize = (uint)sizeof(SHELLEXECUTEINFOW),
            fMask = SEE_MASK_INVOKEIDLIST | SEE_MASK_FLAG_NO_UI,
        };
        fixed (char* verb = "taskbarunpin")
        fixed (char* file = p.Shortcut)
        {
            info.lpVerb = verb;
            info.lpFile = file;
            return ShellExecuteExW(&info);
        }
    }

    // ---- IShellLink --------------------------------------------------------------------------------

    static readonly StrategyBasedComWrappers Wrappers = new();

    static string ResolveShortcut(string path)
    {
        CoInitializeEx(IntPtr.Zero, 0);
        var clsid = new Guid("00021401-0000-0000-C000-000000000046");
        var iid = new Guid("000214F9-0000-0000-C000-000000000046");
        if (CoCreateInstance(in clsid, IntPtr.Zero, 1, in iid, out var ptr) != 0) return null;
        try
        {
            var obj = Wrappers.GetOrCreateObjectForComInstance(ptr, CreateObjectFlags.UniqueInstance);
            ((IPersistFile)obj).Load(path, 0);
            var buffer = stackalloc char[1024];
            ((IShellLinkW)obj).GetPath(buffer, 1024, IntPtr.Zero, 0);
            var target = new string(buffer);
            return target.Length > 0 ? target : null;   // packaged apps and special folders have no file path
        }
        catch (COMException) { return null; }
        finally { Marshal.Release(ptr); }
    }

    [GeneratedComInterface, Guid("000214F9-0000-0000-C000-000000000046")]
    internal partial interface IShellLinkW
    {
        [PreserveSig] int GetPath(char* file, int length, IntPtr findData, uint flags);
    }

    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16), Guid("0000010b-0000-0000-C000-000000000046")]
    internal partial interface IPersistFile
    {
        [PreserveSig] int GetClassID(Guid* clsid);
        [PreserveSig] int IsDirty();
        void Load(string fileName, uint mode);
    }

    // ---- Win32 -----------------------------------------------------------------------------------

    const uint SEE_MASK_INVOKEIDLIST = 0xC, SEE_MASK_FLAG_NO_UI = 0x400;

    [StructLayout(LayoutKind.Sequential)]
    struct SHELLEXECUTEINFOW
    {
        public uint cbSize, fMask;
        public IntPtr hwnd;
        public char* lpVerb, lpFile, lpParameters, lpDirectory;
        public int nShow;
        public IntPtr hInstApp, lpIDList;
        public char* lpClass;
        public IntPtr hkeyClass;
        public uint dwHotKey;
        public IntPtr hIconOrMonitor, hProcess;
    }

    [DllImport("shell32.dll")] static extern bool ShellExecuteExW(SHELLEXECUTEINFOW* info);
    [DllImport("ole32.dll")] static extern int CoInitializeEx(IntPtr reserved, uint coinit);
    [DllImport("ole32.dll")] static extern int CoCreateInstance(in Guid clsid, IntPtr outer, uint context, in Guid iid, out IntPtr obj);
}
