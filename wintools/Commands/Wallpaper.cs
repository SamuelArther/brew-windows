using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace WinTools;

// wallpaper: get and set the desktop picture per screen, or a solid colour, through the shell's
// IDesktopWallpaper (what Settings > Personalization > Background uses). Falls back to
// SystemParametersInfo(SPI_GETDESKWALLPAPER / SPI_SETDESKWALLPAPER) where the shell object is missing.
static unsafe partial class Wallpaper
{
    [ModuleInitializer]
    internal static void Register() => Commands.Add("wallpaper", "Manage the desktop wallpaper", Run);

    static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            Console.WriteLine("""
                Usage: wallpaper <command> [options]

                Commands:
                  get [--screen <all|main|index>]              Print the wallpaper path
                  set <path> [--screen <all|main|index>]
                             [--scale <auto|fill|fit|stretch|center|tile|span>]
                  set-solid-color <hex color>                  e.g. 1e1e1e or #ff8800
                  screens                                      List screens (index: name)

                Windows applies --scale to all screens and a solid colour to the whole desktop.
                """);
            return args.Length == 0 ? 2 : 0;
        }
        if (args[0] is "--version" or "-v") { Console.WriteLine(Commands.Version); return 0; }
        string screen = "all", scale = null;
        var operands = new List<string>();
        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--screen": screen = ++i < args.Length ? args[i] : throw new UsageException("--screen needs all, main or an index"); break;
                case "--scale": scale = ++i < args.Length ? args[i] : throw new UsageException("--scale needs a value"); break;
                default:
                    if (args[i].StartsWith("--")) throw new UsageException($"unknown option {args[i]}");
                    operands.Add(args[i]);
                    break;
            }
        }
        try
        {
            switch (args[0])
            {
                case "get": return Get(screen);
                case "screens":
                    foreach (var (index, _, name, _) in Monitors()) Console.WriteLine($"{index}: {name}");
                    return 0;
                case "set":
                    if (operands.Count != 1) throw new UsageException("set needs one image path");
                    return SetPicture(operands[0], screen, scale);
                case "set-solid-color":
                    if (operands.Count != 1) throw new UsageException("set-solid-color needs a hex colour");
                    return SetColor(operands[0]);
                default: throw new UsageException($"unknown command '{args[0]}'");
            }
        }
        catch (COMException e) { throw new InvalidOperationException($"the desktop wallpaper service failed (0x{e.HResult:x8})"); }
    }

    // Shell monitor ids with an index and a name from the display enumeration (matched by position).
    static List<(int Index, string Id, string Name, bool Main)> Monitors()
    {
        var list = new List<(int, string, string, bool)>();
        var wallpaper = Create();
        if (wallpaper == null) return list;
        var screens = Displayplacer.Screens().Where(s => s.Active).ToList();
        var count = wallpaper.GetMonitorDevicePathCount();
        for (uint i = 0; i < count; i++)
        {
            var id = wallpaper.GetMonitorDevicePathAt(i);
            Displayplacer.RECT rect;
            try { rect = wallpaper.GetMonitorRECT(id); }
            catch (COMException) { continue; }   // a disconnected monitor the shell still remembers
            var match = screens.FirstOrDefault(s => s.DevicePath.Length > 0 && s.DevicePath.Equals(id, StringComparison.OrdinalIgnoreCase))
                        ?? screens.FirstOrDefault(s => s.Mode.dmPositionX == rect.Left && s.Mode.dmPositionY == rect.Top);
            list.Add((list.Count, id, match?.Name ?? $"Screen {list.Count}", match?.Primary ?? false));
        }
        return list;
    }

    static IEnumerable<(int Index, string Id, string Name, bool Main)> Select(string screen)
    {
        var monitors = Monitors();
        return screen switch
        {
            "all" => monitors,
            "main" => monitors.Where(m => m.Main).DefaultIfEmpty(monitors.FirstOrDefault()).Where(m => m.Id != null),
            _ when int.TryParse(screen, out var n) => n >= 0 && n < monitors.Count ? [monitors[n]] : throw new InvalidOperationException($"no screen {n} (see wallpaper screens)"),
            _ => throw new UsageException($"--screen must be all, main or an index, not '{screen}'"),
        };
    }

    static int Get(string screen)
    {
        var wallpaper = Create();
        var printed = false;
        List<(int Index, string Id, string Name, bool Main)> selected;
        try { selected = wallpaper == null ? [] : Select(screen).ToList(); }
        catch (COMException) { selected = []; }   // shell object without monitors (headless sessions)
        if (wallpaper != null)
        {
            foreach (var m in selected)
            {
                string path;
                try { path = wallpaper.GetWallpaper(m.Id); }
                catch (COMException) { continue; }
                if (string.IsNullOrEmpty(path)) continue;
                Console.WriteLine(path);
                printed = true;
            }
        }
        if (!printed)
        {
            // No shell object (e.g. a service session) or a slideshow/solid colour: ask the system setting.
            var buffer = new char[1024];
            fixed (char* b = buffer)
                if (SystemParametersInfoW(SPI_GETDESKWALLPAPER, (uint)buffer.Length, (IntPtr)b, 0))
                {
                    var path = new string(b);
                    if (path.Length > 0) Console.WriteLine(path);
                }
        }
        return 0;
    }

    static int SetPicture(string path, string screen, string scale)
    {
        var full = Path.GetFullPath(path);
        if (!File.Exists(full)) throw new InvalidOperationException($"{path}: No such file");
        int? position = scale switch
        {
            null => null,
            "auto" or "fill" => 4,
            "fit" => 3,
            "stretch" => 2,
            "center" => 0,
            "tile" => 1,
            "span" => 5,
            _ => throw new UsageException($"--scale must be auto, fill, fit, stretch, center, tile or span, not '{scale}'"),
        };
        var wallpaper = Create();
        if (wallpaper == null)
        {
            if (screen != "all") throw new InvalidOperationException("per-screen wallpapers need the shell's desktop wallpaper service");
            fixed (char* p = full)
                if (!SystemParametersInfoW(SPI_SETDESKWALLPAPER, 0, (IntPtr)p, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE))
                    throw new InvalidOperationException("could not set the wallpaper");
            return 0;
        }
        wallpaper.Enable(true);
        if (position is int pos) wallpaper.SetPosition(pos);
        foreach (var m in Select(screen)) wallpaper.SetWallpaper(m.Id, full);
        return 0;
    }

    static int SetColor(string text)
    {
        var hex = text.TrimStart('#');
        if (hex.Length != 6 || !uint.TryParse(hex, NumberStyles.HexNumber, null, out var rgb))
            throw new UsageException($"invalid colour '{text}' (expected hex like ff8800)");
        var colorref = ((rgb & 0xff) << 16) | (rgb & 0xff00) | ((rgb >> 16) & 0xff);   // RRGGBB -> 0x00BBGGRR
        var wallpaper = Create() ?? throw new InvalidOperationException("solid colours need the shell's desktop wallpaper service");
        wallpaper.SetBackgroundColor(colorref);
        wallpaper.Enable(false);   // no picture: the background colour shows
        return 0;
    }

    // ---- COM --------------------------------------------------------------------------------------

    static readonly StrategyBasedComWrappers Wrappers = new();

    static IDesktopWallpaper Create()
    {
        CoInitializeEx(IntPtr.Zero, COINIT_MULTITHREADED);
        var clsid = new Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD");
        var iid = new Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B");
        if (CoCreateInstance(in clsid, IntPtr.Zero, CLSCTX_LOCAL_SERVER | CLSCTX_INPROC_SERVER, in iid, out var ptr) != 0) return null;
        try { return (IDesktopWallpaper)Wrappers.GetOrCreateObjectForComInstance(ptr, CreateObjectFlags.UniqueInstance); }
        finally { Marshal.Release(ptr); }
    }

    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16), Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
    internal partial interface IDesktopWallpaper
    {
        void SetWallpaper(string monitorId, string wallpaper);
        string GetWallpaper(string monitorId);
        string GetMonitorDevicePathAt(uint index);
        uint GetMonitorDevicePathCount();
        Displayplacer.RECT GetMonitorRECT(string monitorId);
        void SetBackgroundColor(uint color);
        uint GetBackgroundColor();
        void SetPosition(int position);
        int GetPosition();
        void SetSlideshow(IntPtr items);
        IntPtr GetSlideshow();
        void SetSlideshowOptions(int options, uint tick);
        void GetSlideshowOptions(out int options, out uint tick);
        void AdvanceSlideshow(string monitorId, int direction);
        int GetStatus();
        void Enable([MarshalAs(UnmanagedType.Bool)] bool enable);
    }

    const uint COINIT_MULTITHREADED = 0, CLSCTX_INPROC_SERVER = 0x1, CLSCTX_LOCAL_SERVER = 0x4;
    const uint SPI_GETDESKWALLPAPER = 0x73, SPI_SETDESKWALLPAPER = 0x14, SPIF_UPDATEINIFILE = 0x1, SPIF_SENDCHANGE = 0x2;

    [DllImport("ole32.dll")]
    static extern int CoInitializeEx(IntPtr reserved, uint coinit);

    [DllImport("ole32.dll")]
    static extern int CoCreateInstance(in Guid clsid, IntPtr outer, uint context, in Guid iid, out IntPtr obj);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern bool SystemParametersInfoW(uint action, uint param, IntPtr pvParam, uint winIni);
}
