using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WinTools;

// displayplacer / screenresolution: list, get and set display resolution, refresh rate, colour depth,
// rotation and arrangement with EnumDisplaySettingsEx / ChangeDisplaySettingsEx. Also owns the screen
// enumeration (GDI name, monitor name from QueryDisplayConfig, HMONITOR, DPI) the other display tools use.
static unsafe partial class Displayplacer
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("displayplacer", "Configure multi-display resolutions and arrangements", Run);
        Commands.Add("screenresolution", "Get, set, and list display resolution", RunScreenresolution);
    }

    // ---- shared screen enumeration -------------------------------------------------------------

    internal sealed class Screen
    {
        public int Index;              // 0-based, in adapter output order
        public string GdiName;         // \\.\DISPLAY1
        public string Adapter;         // "Intel(R) Iris(R) Xe Graphics"
        public string Name;            // monitor name from EDID, e.g. "DELL U2415"
        public string DevicePath;      // \\?\DISPLAY#DEL40B8#...#{e6f07b5f-...}
        public string PersistentId;    // DEL40B8-UID4352: stable across reboots and re-plugs on the same port
        public bool Active, Primary, BuiltIn;
        public IntPtr HMonitor;
        public DEVMODE Mode;           // current mode (registry mode when inactive)
        public int Dpi = 96;
    }

    // Every connected monitor, active or not. QueryDisplayConfig is the source of truth (it knows the
    // monitors even when EnumDisplayDevices reports no monitor child, as on some hybrid-GPU laptops);
    // EnumDisplayDevices adds adapter names and is the fallback when display config isn't available.
    internal static List<Screen> Screens()
    {
        MakeDpiAware();
        var monitors = new Dictionary<string, IntPtr>(StringComparer.OrdinalIgnoreCase);
        var handle = GCHandle.Alloc(monitors);
        try { EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, &CollectMonitor, GCHandle.ToIntPtr(handle)); }
        finally { handle.Free(); }
        var adapters = new Dictionary<string, (string Adapter, uint Flags)>(StringComparer.OrdinalIgnoreCase);
        var adapter = new DISPLAY_DEVICE { cb = sizeof(DISPLAY_DEVICE) };
        for (uint i = 0; EnumDisplayDevicesW(null, i, ref adapter, 0); i++, adapter = new DISPLAY_DEVICE { cb = sizeof(DISPLAY_DEVICE) })
            if ((adapter.StateFlags & DISPLAY_DEVICE_MIRRORING_DRIVER) == 0)
                adapters[Str(adapter.DeviceName)] = (Str(adapter.DeviceString), adapter.StateFlags);

        var list = new List<Screen>();
        foreach (var t in Targets())
        {
            // One path per (source, target) pair is listed; keep the first (active) one per monitor.
            if (list.Any(s => s.PersistentId.Equals(t.Id, StringComparison.OrdinalIgnoreCase))) continue;
            adapters.TryGetValue(t.GdiName, out var a);
            list.Add(new Screen
            {
                GdiName = t.GdiName,
                Adapter = a.Adapter ?? "",
                DevicePath = t.DevicePath,
                PersistentId = t.Id,
                Name = t.Name.Length > 0 ? t.Name : t.BuiltIn ? "Built-in display" : "Generic PnP Monitor",
                Active = t.Active,
                Primary = t.Active && (a.Flags & DISPLAY_DEVICE_PRIMARY_DEVICE) != 0,
                BuiltIn = t.BuiltIn,
            });
        }
        if (list.Count == 0)
            foreach (var (gdi, a) in adapters.Where(x => (x.Value.Flags & DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) != 0))
            {
                var monitor = new DISPLAY_DEVICE { cb = sizeof(DISPLAY_DEVICE) };
                var hasMonitor = EnumDisplayDevicesW(gdi, 0, ref monitor, EDD_GET_DEVICE_INTERFACE_NAME);
                list.Add(new Screen
                {
                    GdiName = gdi, Adapter = a.Adapter, Active = true,
                    DevicePath = hasMonitor ? Str(monitor.DeviceID) : "",
                    Name = hasMonitor ? Str(monitor.DeviceString) : "Display",
                    Primary = (a.Flags & DISPLAY_DEVICE_PRIMARY_DEVICE) != 0,
                });
            }
        foreach (var screen in list)
        {
            screen.PersistentId ??= PersistentIdOf(screen.DevicePath, screen.GdiName);
            screen.Mode = NewDevMode();
            if (!EnumDisplaySettingsExW(screen.GdiName, screen.Active ? ENUM_CURRENT_SETTINGS : ENUM_REGISTRY_SETTINGS, ref screen.Mode, 0))
                screen.Mode = NewDevMode();
            if (screen.Active && monitors.TryGetValue(screen.GdiName, out var hmon))
            {
                screen.HMonitor = hmon;
                if (GetDpiForMonitor(hmon, 0, out var dpiX, out _) == 0) screen.Dpi = (int)dpiX;
            }
        }
        // Active screens first, the main one first, then in DISPLAYn order.
        list = list.OrderByDescending(s => s.Active).ThenByDescending(s => s.Primary)
                   .ThenBy(s => s.GdiName.Length).ThenBy(s => s.GdiName, StringComparer.Ordinal).ToList();
        for (var i = 0; i < list.Count; i++) list[i].Index = i;
        return list;
    }

    // "\\?\DISPLAY#DEL40B8#5&2a8f4c7&0&UID4352#{...}" -> "DEL40B8-UID4352".
    internal static string PersistentIdOf(string devicePath, string fallback)
    {
        var parts = devicePath.Split('#');
        if (parts.Length < 3) return fallback.Replace(@"\\.\", "");
        var instance = parts[2].Split('&');
        return $"{parts[1]}-{instance[^1]}";
    }

    // EDID manufacturer ids are three 5-bit letters, stored big-endian.
    static string EdidVendor(ushort raw)
    {
        var v = (ushort)((raw >> 8) | (raw << 8));
        return new string([(char)('A' - 1 + ((v >> 10) & 31)), (char)('A' - 1 + ((v >> 5) & 31)), (char)('A' - 1 + (v & 31))]);
    }

    // Each connected target with its source's GDI name; active paths first.
    static List<(string GdiName, string DevicePath, string Id, string Name, bool BuiltIn, bool Active)> Targets()
    {
        var result = new List<(string, string, string, string, bool, bool)>();
        if (GetDisplayConfigBufferSizes(QDC_ALL_PATHS, out var pathCount, out var modeCount) != 0) return result;
        var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
        var modes = new byte[modeCount * 64];
        fixed (DISPLAYCONFIG_PATH_INFO* p = paths)
        fixed (byte* m = modes)
            if (QueryDisplayConfig(QDC_ALL_PATHS, ref pathCount, p, ref modeCount, m, IntPtr.Zero) != 0) return result;
        foreach (var path in paths.Take(pathCount).OrderByDescending(x => x.flags & DISPLAYCONFIG_PATH_ACTIVE))
        {
            var active = (path.flags & DISPLAYCONFIG_PATH_ACTIVE) != 0;
            if (!active && path.targetAvailable == 0) continue;
            var target = new DISPLAYCONFIG_TARGET_DEVICE_NAME();
            target.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;
            target.header.size = (uint)sizeof(DISPLAYCONFIG_TARGET_DEVICE_NAME);
            target.header.adapterId = path.targetAdapterId;
            target.header.id = path.targetId;
            if (DisplayConfigGetDeviceInfo(ref target.header) != 0) continue;
            var source = new DISPLAYCONFIG_SOURCE_DEVICE_NAME();
            source.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
            source.header.size = (uint)sizeof(DISPLAYCONFIG_SOURCE_DEVICE_NAME);
            source.header.adapterId = path.sourceAdapterId;
            source.header.id = path.sourceId;
            if (DisplayConfigGetDeviceInfo(ref source.header) != 0) continue;
            var devicePath = new string(target.monitorDevicePath);
            // Without a monitor device node (seen on some laptop panels) build the id from the EDID instead.
            var id = devicePath.Length > 0 ? PersistentIdOf(devicePath, "")
                : (target.flags & 4) != 0 ?$"{EdidVendor(target.edidManufactureId)}{target.edidProductCodeId:X4}-T{path.targetId}"
                : $"T{path.targetAdapterId.Low:X}-{path.targetId}";
            var builtIn = target.outputTechnology is OUTPUT_INTERNAL or OUTPUT_LVDS or OUTPUT_DISPLAYPORT_EMBEDDED or OUTPUT_UDI_EMBEDDED;
            result.Add((new string(source.viewGdiDeviceName), devicePath, id, new string(target.monitorFriendlyDeviceName), builtIn, active));
        }
        return result;
    }

    [UnmanagedCallersOnly]
    static int CollectMonitor(IntPtr hmon, IntPtr hdc, RECT* rect, IntPtr data)
    {
        var info = new MONITORINFOEX { cbSize = sizeof(MONITORINFOEX) };
        if (GetMonitorInfoW(hmon, ref info))
            ((Dictionary<string, IntPtr>)GCHandle.FromIntPtr(data).Target!)[new string(info.szDevice)] = hmon;
        return 1;
    }

    // Physical pixel coordinates everywhere; the default (unaware) gives scaled ones on high-DPI screens.
    internal static void MakeDpiAware()
    {
        try { SetProcessDpiAwarenessContext(-4); } catch (EntryPointNotFoundException) { }
    }

    internal static List<DEVMODE> Modes(string gdiName)
    {
        var modes = new List<DEVMODE>();
        var mode = NewDevMode();
        for (var i = 0; EnumDisplaySettingsExW(gdiName, i, ref mode, 0); i++, mode = NewDevMode()) modes.Add(mode);
        return modes;
    }

    internal static DEVMODE NewDevMode() => new() { dmSize = (ushort)sizeof(DEVMODE) };

    static string Str(char* chars) => new(chars);

    // ---- screenresolution ---------------------------------------------------------------------

    static int RunScreenresolution(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            Console.WriteLine("""
                Usage: screenresolution get
                       screenresolution list
                       screenresolution set <WxH[xDepth][@Hz]> [<WxH[xDepth][@Hz]> ...]
                       screenresolution -version
                Modes apply to active displays in order (display 0 first), e.g. 1920x1080x32@60.
                """);
            return args.Length == 0 ? 2 : 0;
        }
        var active = Screens().Where(s => s.Active).ToList();
        switch (args[0])
        {
            case "-version" or "--version" or "-v":
                Console.WriteLine($"screenresolution version {Commands.Version}");
                return 0;
            case "get":
                if (active.Count == 0) throw new InvalidOperationException("no active displays");
                for (var i = 0; i < active.Count; i++) Console.WriteLine($"Display {i}: {ModeString(active[i].Mode)}");
                return 0;
            case "list":
                for (var i = 0; i < active.Count; i++)
                {
                    Console.WriteLine($"Available Modes on Display {i}");
                    var seen = new HashSet<string>();
                    var line = new List<string>();
                    foreach (var mode in Modes(active[i].GdiName))
                        if (seen.Add(ModeString(mode))) line.Add(ModeString(mode));
                    for (var j = 0; j < line.Count; j += 4) Console.WriteLine("  " + string.Join("  ", line.Skip(j).Take(4).Select(s => s.PadRight(18))).TrimEnd());
                }
                return 0;
            case "set":
                if (args.Length < 2) throw new UsageException("set needs at least one mode, e.g. 1920x1080x32@60");
                var requests = args[1..].Select(ParseModeString).ToList();
                if (requests.Count > active.Count) throw new UsageException($"{requests.Count} modes given but only {active.Count} active display(s)");
                for (var i = 0; i < requests.Count; i++)
                {
                    var (w, h, depth, hz) = requests[i];
                    var mode = PickMode(active[i], w, h, hz, depth)
                               ?? throw new InvalidOperationException($"display {i} has no mode matching {args[i + 1]} (see screenresolution list)");
                    Console.WriteLine($"Display {i}: setting {ModeString(mode)}");
                    Check(ChangeDisplaySettingsExW(active[i].GdiName, ref mode, IntPtr.Zero, CDS_UPDATEREGISTRY | CDS_NORESET, IntPtr.Zero));
                }
                Check(ChangeDisplaySettingsExW(null, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero));
                return 0;
            default:
                throw new UsageException($"unknown command '{args[0]}' (get, list or set)");
        }
    }

    internal static string ModeString(DEVMODE m) => $"{m.dmPelsWidth}x{m.dmPelsHeight}x{m.dmBitsPerPel}@{m.dmDisplayFrequency}";

    // "1920x1080", "1920x1080x32", "1920x1080x32@60", "1920x1080@60" -> parts (0 = unspecified).
    internal static (uint W, uint H, uint Depth, uint Hz) ParseModeString(string text)
    {
        var at = text.Split('@');
        if (at.Length > 2) throw new UsageException($"bad mode '{text}'");
        var dims = at[0].Split('x');
        uint hz = 0, depth = 0;
        if (dims.Length is < 2 or > 3 || !uint.TryParse(dims[0], out var w) || !uint.TryParse(dims[1], out var h)
            || (dims.Length == 3 && !uint.TryParse(dims[2], out depth)) || (at.Length == 2 && !uint.TryParse(at[1], out hz)))
            throw new UsageException($"bad mode '{text}' (expected WxH[xDepth][@Hz])");
        return (w, h, depth, hz);
    }

    // The best mode of a screen for the given size; unspecified depth/rate prefer the current, then the highest.
    internal static DEVMODE? PickMode(Screen screen, uint w, uint h, uint hz, uint depth)
    {
        var candidates = Modes(screen.GdiName).Where(m => m.dmPelsWidth == w && m.dmPelsHeight == h
            && (hz == 0 || m.dmDisplayFrequency == hz) && (depth == 0 || m.dmBitsPerPel == depth)).ToList();
        if (candidates.Count == 0) return null;
        return candidates.OrderByDescending(m => hz == 0 && m.dmDisplayFrequency == screen.Mode.dmDisplayFrequency)
                         .ThenByDescending(m => depth == 0 && m.dmBitsPerPel == screen.Mode.dmBitsPerPel)
                         .ThenByDescending(m => m.dmDisplayFrequency).ThenByDescending(m => m.dmBitsPerPel)
                         .ThenBy(m => m.dmDisplayFixedOutput).First();
    }

    static void Check(int result)
    {
        if (result == DISP_CHANGE_SUCCESSFUL) return;
        if (result == DISP_CHANGE_RESTART) { Console.Error.WriteLine("The change takes effect after a restart."); return; }
        throw new InvalidOperationException(result switch
        {
            -2 => "the graphics mode is not supported",
            -3 => "unable to write settings to the registry",
            -5 => "an invalid parameter was passed",
            -6 => "the system is not DualView capable",
            _ => $"the display driver failed the change (error {result})",
        });
    }

    // ---- displayplacer --------------------------------------------------------------------------

    sealed class Placement
    {
        public string Id;
        public uint? Width, Height, Hz, ColorDepth;
        public int? Mode, X, Y, Degree;
        public bool? Enabled;
        public string Text;
    }

    static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            Console.WriteLine("""
                Usage:
                    Show current screen info and possible resolutions: displayplacer list
                    Apply screen config (hz and color_depth are optional):
                        displayplacer "id:<screenId> res:<width>x<height> hz:<num> color_depth:<num> scaling:<on/off> origin:(<x>,<y>) degree:<0/90/180/270>"
                        displayplacer "id:<screenId> mode:<modeNum> origin:(<x>,<y>) degree:<0/90/180/270>"
                        displayplacer "id:<screenId> enabled:false"
                    Add --dry-run to print the changes without applying them.
                    Mirror all screens: displayplacer "id:<mainScreenId>+<id2>+... res:..."

                screenId is the persistent id from `displayplacer list` (monitor model + port, stable across
                reboots), the contextual id (1, 2, ... in Windows' DISPLAYn order) or the GDI name (\\.\DISPLAY1).
                origin (0,0) is the main display. scaling is accepted for compatibility; Windows' per-monitor
                scale is set in Settings > Display. color_depth is bits per channel (8 = 32-bit colour).
                """);
            return args.Length == 0 ? 2 : 0;
        }
        if (args[0] is "-v" or "--version" or "version") { Console.WriteLine($"displayplacer v{Commands.Version}"); return 0; }
        if (args[0] == "list") return List();
        var dryRun = args.Contains("--dry-run");
        var placements = args.Where(a => a != "--dry-run").Select(ParsePlacement).ToList();
        if (placements.Count == 0) throw new UsageException("no screen config given");
        return Apply(placements, dryRun);
    }

    static int List()
    {
        var screens = Screens();
        foreach (var s in screens)
        {
            var m = s.Mode;
            Console.WriteLine($"Persistent screen id: {s.PersistentId}");
            Console.WriteLine($"Contextual screen id: {ContextualId(s)}");
            Console.WriteLine($"GDI device name: {s.GdiName}");
            Console.WriteLine($"Type: {s.Name} ({(s.BuiltIn ? "built in screen" : "external screen")}, {s.Adapter})");
            Console.WriteLine($"Resolution: {m.dmPelsWidth}x{m.dmPelsHeight}");
            Console.WriteLine($"Hertz: {m.dmDisplayFrequency}");
            Console.WriteLine($"Color Depth: {ColorDepthOf(m.dmBitsPerPel)}");
            Console.WriteLine($"Scaling: {(s.Dpi > 96 ? "on" : "off")} ({s.Dpi * 100 / 96}%)");
            Console.WriteLine($"Origin: ({m.dmPositionX},{m.dmPositionY}){(s.Primary ? " - main display" : "")}");
            Console.WriteLine($"Rotation: {DegreeOf(m.dmDisplayOrientation)}");
            Console.WriteLine($"Enabled: {(s.Active ? "true" : "false")}");
            Console.WriteLine($"Resolutions for rotation {DegreeOf(m.dmDisplayOrientation)}:");
            var modes = Modes(s.GdiName);
            for (var i = 0; i < modes.Count; i++)
            {
                var mode = modes[i];
                var current = s.Active && mode.dmPelsWidth == m.dmPelsWidth && mode.dmPelsHeight == m.dmPelsHeight
                              && mode.dmDisplayFrequency == m.dmDisplayFrequency && mode.dmBitsPerPel == m.dmBitsPerPel
                              && mode.dmDisplayFixedOutput == m.dmDisplayFixedOutput;
                Console.WriteLine($"  mode {i}: res:{mode.dmPelsWidth}x{mode.dmPelsHeight} hz:{mode.dmDisplayFrequency} color_depth:{ColorDepthOf(mode.dmBitsPerPel)}{(current ? " <-- current mode" : "")}");
            }
            Console.WriteLine();
        }
        Console.WriteLine("Execute the command below to set your screens to the current arrangement. If screen ids are switching, please run `displayplacer --help` for info on using contextual ids instead of persistent ids.");
        Console.WriteLine();
        var parts = screens.Select(s => s.Active
            ? $"\"id:{s.PersistentId} res:{s.Mode.dmPelsWidth}x{s.Mode.dmPelsHeight} hz:{s.Mode.dmDisplayFrequency} color_depth:{ColorDepthOf(s.Mode.dmBitsPerPel)} enabled:true scaling:{(s.Dpi > 96 ? "on" : "off")} origin:({s.Mode.dmPositionX},{s.Mode.dmPositionY}) degree:{DegreeOf(s.Mode.dmDisplayOrientation)}\""
            : $"\"id:{s.PersistentId} enabled:false\"");
        Console.WriteLine("displayplacer " + string.Join(" ", parts));
        return 0;
    }

    static string ContextualId(Screen s) => s.GdiName.Replace(@"\\.\DISPLAY", "");

    internal static int ColorDepthOf(uint bpp) => bpp switch { 32 or 24 => 8, 16 => 5, 8 => 2, _ => (int)bpp / 4 };
    static uint BppOf(uint colorDepth) => colorDepth switch { 8 => 32, 5 or 6 => 16, 4 => 16, 2 => 8, 10 => 32, _ => colorDepth * 4 };
    internal static int DegreeOf(uint orientation) => (int)(orientation & 3) * 90;

    // "id:X res:1920x1080 hz:60 color_depth:8 enabled:true scaling:off origin:(0,0) degree:0"
    static Placement ParsePlacement(string text)
    {
        var p = new Placement { Text = text };
        foreach (var token in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var colon = token.IndexOf(':');
            if (colon <= 0) throw new UsageException($"bad setting '{token}' in \"{text}\"");
            var key = token[..colon];
            var value = token[(colon + 1)..];
            switch (key)
            {
                case "id": p.Id = value; break;
                case "res":
                    var dims = value.Split('x');
                    if (dims.Length != 2 || !uint.TryParse(dims[0], out var w) || !uint.TryParse(dims[1], out var h))
                        throw new UsageException($"bad res '{value}'");
                    (p.Width, p.Height) = (w, h);
                    break;
                case "hz": p.Hz = uint.TryParse(value, out var hz) ? hz : throw new UsageException($"bad hz '{value}'"); break;
                case "color_depth": p.ColorDepth = uint.TryParse(value, out var cd) ? cd : throw new UsageException($"bad color_depth '{value}'"); break;
                case "mode": p.Mode = int.TryParse(value, out var mode) ? mode : throw new UsageException($"bad mode '{value}'"); break;
                case "scaling":
                    if (value is not ("on" or "off")) throw new UsageException($"bad scaling '{value}'");
                    break;
                case "enabled":
                    p.Enabled = value switch { "true" => true, "false" => false, _ => throw new UsageException($"bad enabled '{value}'") };
                    break;
                case "degree":
                    p.Degree = int.TryParse(value, out var deg) && deg is 0 or 90 or 180 or 270 ? deg : throw new UsageException($"bad degree '{value}' (0, 90, 180 or 270)");
                    break;
                case "origin":
                    var xy = value.Trim('(', ')').Split(',');
                    if (xy.Length != 2 || !int.TryParse(xy[0], out var x) || !int.TryParse(xy[1], out var y))
                        throw new UsageException($"bad origin '{value}' (expected (x,y))");
                    (p.X, p.Y) = (x, y);
                    break;
                default: throw new UsageException($"unknown setting '{key}' in \"{text}\"");
            }
        }
        if (p.Id == null) throw new UsageException($"missing id: in \"{text}\"");
        return p;
    }

    static Screen FindScreen(List<Screen> screens, string id) =>
        screens.FirstOrDefault(s => s.PersistentId.Equals(id, StringComparison.OrdinalIgnoreCase))
        ?? screens.FirstOrDefault(s => ContextualId(s) == id || s.GdiName.Equals(id, StringComparison.OrdinalIgnoreCase)
                                       || s.GdiName.Equals(@"\\.\" + id, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"unable to find screen {id} - run `displayplacer list` for the ids");

    static int Apply(List<Placement> placements, bool dryRun)
    {
        var screens = Screens();
        var changes = new List<(Screen Screen, DEVMODE Mode, uint Flags, string Description)>();
        foreach (var p in placements)
        {
            if (p.Id.Contains('+'))
            {
                // Windows mirrors the whole desktop (clone topology), not arbitrary groups.
                var group = p.Id.Split('+').Select(id => FindScreen(screens, id)).ToList();
                if (screens.Count(s => s.Active) != group.Count || group.Any(s => !s.Active))
                    throw new InvalidOperationException("Windows can only mirror all active screens together; list every active screen id in the mirror group");
                if (dryRun) { Console.WriteLine($"mirror {string.Join(", ", group.Select(s => s.GdiName))} (clone topology)"); continue; }
                var r = SetDisplayConfig(0, IntPtr.Zero, 0, IntPtr.Zero, SDC_APPLY | SDC_TOPOLOGY_CLONE);
                if (r != 0) throw new InvalidOperationException($"could not mirror the screens (error {r})");
                screens = Screens();
                p.Id = p.Id.Split('+')[0];
            }
            var screen = FindScreen(screens, p.Id);
            var mode = screen.Mode;
            mode.dmSize = (ushort)sizeof(DEVMODE);
            mode.dmDriverExtra = 0;
            uint fields = 0;
            if (p.Enabled == false)
            {
                mode.dmPelsWidth = mode.dmPelsHeight = 0;
                mode.dmPositionX = mode.dmPositionY = 0;
                mode.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT | DM_POSITION;
                changes.Add((screen, mode, CDS_UPDATEREGISTRY | CDS_NORESET, $"{screen.GdiName}: disable"));
                continue;
            }
            var degree = p.Degree ?? DegreeOf(screen.Mode.dmDisplayOrientation);
            // Mode lists come in one orientation; a portrait layout of a landscape mode swaps width and height.
            var curDegree = DegreeOf(screen.Mode.dmDisplayOrientation);
            var listRotated = curDegree is 90 or 270 && screen.Mode.dmPelsWidth < screen.Mode.dmPelsHeight
                              && Modes(screen.GdiName).Any(m => m.dmPelsWidth == screen.Mode.dmPelsWidth && m.dmPelsHeight == screen.Mode.dmPelsHeight);
            var swap = (degree is 90 or 270) != listRotated;
            DEVMODE? chosen = null;
            if (p.Mode is int index)
            {
                var modes = Modes(screen.GdiName);
                if (index < 0 || index >= modes.Count) throw new InvalidOperationException($"{screen.GdiName} has no mode {index} (modes 0-{modes.Count - 1})");
                chosen = modes[index];
            }
            else if (p.Width is uint w && p.Height is uint h)
            {
                var bpp = p.ColorDepth is uint c ? BppOf(c) : 0;
                chosen = PickMode(screen, w, h, p.Hz ?? 0, bpp) ?? PickMode(screen, h, w, p.Hz ?? 0, bpp)
                         ?? throw new InvalidOperationException($"{screen.GdiName} has no mode res:{w}x{h}{(p.Hz is uint hz ? $" hz:{hz}" : "")} (see displayplacer list)");
            }
            else if (p.Hz != null || p.ColorDepth != null)
            {
                var (cw, ch) = (curDegree is 90 or 270) != listRotated
                    ? (screen.Mode.dmPelsHeight, screen.Mode.dmPelsWidth) : (screen.Mode.dmPelsWidth, screen.Mode.dmPelsHeight);
                chosen = PickMode(screen, cw, ch, p.Hz ?? 0, p.ColorDepth is uint c ? BppOf(c) : 0)
                         ?? throw new InvalidOperationException($"{screen.GdiName} has no mode with those settings (see displayplacer list)");
            }
            if (chosen is DEVMODE pick)
            {
                (mode.dmPelsWidth, mode.dmPelsHeight) = swap ? (pick.dmPelsHeight, pick.dmPelsWidth) : (pick.dmPelsWidth, pick.dmPelsHeight);
                (mode.dmDisplayFrequency, mode.dmBitsPerPel) = (pick.dmDisplayFrequency, pick.dmBitsPerPel);
                fields |= DM_PELSWIDTH | DM_PELSHEIGHT | DM_DISPLAYFREQUENCY | DM_BITSPERPEL;
            }
            if (p.Degree is int)
            {
                var old = curDegree;
                if ((old is 0 or 180) != (degree is 0 or 180) && chosen == null)
                    (mode.dmPelsWidth, mode.dmPelsHeight) = (mode.dmPelsHeight, mode.dmPelsWidth);
                mode.dmDisplayOrientation = (uint)(degree / 90);
                fields |= DM_DISPLAYORIENTATION | DM_PELSWIDTH | DM_PELSHEIGHT;
            }
            if (p.X is int x && p.Y is int y)
            {
                (mode.dmPositionX, mode.dmPositionY) = (x, y);
                fields |= DM_POSITION;
            }
            if (!screen.Active || p.Enabled == true) fields |= DM_PELSWIDTH | DM_PELSHEIGHT | DM_POSITION;
            mode.dmFields = fields;
            var flags = CDS_UPDATEREGISTRY | CDS_NORESET;
            if (p.X == 0 && p.Y == 0) flags |= CDS_SET_PRIMARY;
            changes.Add((screen, mode, flags,
                $"{screen.GdiName}: {mode.dmPelsWidth}x{mode.dmPelsHeight} hz:{mode.dmDisplayFrequency} color_depth:{ColorDepthOf(mode.dmBitsPerPel)} origin:({mode.dmPositionX},{mode.dmPositionY}) degree:{degree}{((flags & CDS_SET_PRIMARY) != 0 ? " main" : "")}"));
        }
        foreach (var change in changes)
        {
            if (dryRun) { Console.WriteLine(change.Description); continue; }
            var m = change.Mode;
            Check(ChangeDisplaySettingsExW(change.Screen.GdiName, ref m, IntPtr.Zero, change.Flags, IntPtr.Zero));
        }
        if (!dryRun && changes.Count > 0) Check(ChangeDisplaySettingsExW(null, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero));
        return 0;
    }

    // ---- Win32 ----------------------------------------------------------------------------------

    const uint DISPLAY_DEVICE_ATTACHED_TO_DESKTOP = 0x1, DISPLAY_DEVICE_PRIMARY_DEVICE = 0x4, DISPLAY_DEVICE_MIRRORING_DRIVER = 0x8;
    const uint EDD_GET_DEVICE_INTERFACE_NAME = 1;
    const int ENUM_CURRENT_SETTINGS = -1, ENUM_REGISTRY_SETTINGS = -2;
    const uint DM_POSITION = 0x20, DM_DISPLAYORIENTATION = 0x80, DM_BITSPERPEL = 0x40000, DM_PELSWIDTH = 0x80000,
               DM_PELSHEIGHT = 0x100000, DM_DISPLAYFREQUENCY = 0x400000;
    const uint CDS_UPDATEREGISTRY = 0x1, CDS_SET_PRIMARY = 0x10, CDS_NORESET = 0x10000000;
    const int DISP_CHANGE_SUCCESSFUL = 0, DISP_CHANGE_RESTART = 1;
    const uint QDC_ALL_PATHS = 1;
    const uint DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1, DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME = 2, DISPLAYCONFIG_PATH_ACTIVE = 1;
    const uint OUTPUT_LVDS = 6, OUTPUT_DISPLAYPORT_EMBEDDED = 11, OUTPUT_UDI_EMBEDDED = 13, OUTPUT_INTERNAL = 0x80000000;
    const uint SDC_TOPOLOGY_CLONE = 0x2, SDC_APPLY = 0x80;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct DISPLAY_DEVICE
    {
        public int cb;
        public fixed char DeviceName[32];
        public fixed char DeviceString[128];
        public uint StateFlags;
        public fixed char DeviceID[128];
        public fixed char DeviceKey[128];
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct DEVMODE
    {
        public fixed char dmDeviceName[32];
        public ushort dmSpecVersion, dmDriverVersion, dmSize, dmDriverExtra;
        public uint dmFields;
        public int dmPositionX, dmPositionY;
        public uint dmDisplayOrientation, dmDisplayFixedOutput;
        public short dmColor, dmDuplex, dmYResolution, dmTTOption, dmCollate;
        public fixed char dmFormName[32];
        public ushort dmLogPixels;
        public uint dmBitsPerPel, dmPelsWidth, dmPelsHeight, dmDisplayFlags, dmDisplayFrequency;
        public uint dmICMMethod, dmICMIntent, dmMediaType, dmDitherType, dmReserved1, dmReserved2, dmPanningWidth, dmPanningHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor, rcWork;
        public uint dwFlags;
        public fixed char szDevice[32];
    }

    // LUID is two 4-byte fields: a long here would add padding that native display-config structs lack.
    [StructLayout(LayoutKind.Sequential)]
    struct LUID { public uint Low; public int High; }

    [StructLayout(LayoutKind.Sequential)]
    struct DISPLAYCONFIG_PATH_INFO
    {
        public LUID sourceAdapterId;
        public uint sourceId, sourceModeInfoIdx, sourceStatusFlags;
        public LUID targetAdapterId;
        public uint targetId, targetModeInfoIdx, outputTechnology, rotation, scaling, refreshNum, refreshDen, scanLineOrdering;
        public int targetAvailable;
        public uint targetStatusFlags, flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct DISPLAYCONFIG_DEVICE_INFO_HEADER
    {
        public uint type, size;
        public LUID adapterId;
        public uint id;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct DISPLAYCONFIG_TARGET_DEVICE_NAME
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        public uint flags, outputTechnology;
        public ushort edidManufactureId, edidProductCodeId;
        public uint connectorInstance;
        public fixed char monitorFriendlyDeviceName[64];
        public fixed char monitorDevicePath[128];
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct DISPLAYCONFIG_SOURCE_DEVICE_NAME
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        public fixed char viewGdiDeviceName[32];
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern bool EnumDisplayDevicesW(string device, uint index, ref DISPLAY_DEVICE info, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern bool EnumDisplaySettingsExW(string device, int mode, ref DEVMODE devMode, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int ChangeDisplaySettingsExW(string device, ref DEVMODE devMode, IntPtr hwnd, uint flags, IntPtr param);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int ChangeDisplaySettingsExW(string device, IntPtr devMode, IntPtr hwnd, uint flags, IntPtr param);

    [DllImport("user32.dll")]
    static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, delegate* unmanaged<IntPtr, IntPtr, RECT*, IntPtr, int> callback, IntPtr data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern bool GetMonitorInfoW(IntPtr hmon, ref MONITORINFOEX info);

    [DllImport("user32.dll")]
    static extern bool SetProcessDpiAwarenessContext(nint context);

    [DllImport("shcore.dll")]
    static extern int GetDpiForMonitor(IntPtr hmon, int type, out uint dpiX, out uint dpiY);

    [DllImport("user32.dll")]
    static extern int GetDisplayConfigBufferSizes(uint flags, out int paths, out int modes);

    [DllImport("user32.dll")]
    static extern int QueryDisplayConfig(uint flags, ref int pathCount, DISPLAYCONFIG_PATH_INFO* paths, ref int modeCount, byte* modes, IntPtr topology);

    [DllImport("user32.dll")]
    static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_DEVICE_INFO_HEADER packet);

    [DllImport("user32.dll")]
    static extern int SetDisplayConfig(uint pathCount, IntPtr paths, uint modeCount, IntPtr modes, uint flags);
}
