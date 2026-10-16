using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Text.Json;

namespace WinTools;

// peekaboo: screen capture, UI inspection and automation for scripts and agents. Screenshots use GDI
// (BitBlt for screens, PrintWindow for windows) and GDI+ to encode PNG/JPEG; `see` walks a window with UI
// Automation (IUIAutomation, the accessibility API) and numbers its controls (B1, T2 ...) so `click --on B1`
// can find them again; input goes through the cliclick/wtype SendInput layer. Windows needs no screen
// recording or accessibility permission. The AI commands (agent, analyze) need a model provider and are
// not part of this port.
static unsafe partial class Peekaboo
{
    [ModuleInitializer]
    internal static void Register() => Commands.Add("peekaboo", "Screen capture, UI inspection and automation", Run);

    static string ConfigDir => Environment.GetEnvironmentVariable("PEEKABOO_CONFIG_DIR") is { Length: > 0 } dir
        ? dir : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".peekaboo");

    // ---- argument parsing ------------------------------------------------------------------------

    sealed class Args
    {
        public readonly List<string> Positional = [];
        readonly Dictionary<string, string> values = new(StringComparer.Ordinal);
        readonly HashSet<string> flags = new(StringComparer.Ordinal);
        public bool Json => Flag("--json-output") || Flag("--json") || Flag("-j");

        static readonly HashSet<string> Booleans =
        [
            "--json-output", "--json", "-j", "--double", "--right", "--press-return", "--clear", "--smooth", "--force", "--center",
            "--wait-until-ready", "--no-focus", "--all", "--retina", "--annotate", "--verbose", "-v", "--quiet",
        ];

        public Args(IEnumerable<string> args)
        {
            var list = args.ToList();
            for (var i = 0; i < list.Count; i++)
            {
                var a = list[i];
                if (a.StartsWith("--") && a.Contains('=')) { values[a[..a.IndexOf('=')]] = a[(a.IndexOf('=') + 1)..]; continue; }
                if (Booleans.Contains(a)) { flags.Add(a); continue; }
                if (a.StartsWith("--") || (a.StartsWith('-') && a.Length == 2 && !char.IsDigit(a[1])))
                {
                    if (i + 1 >= list.Count) throw new UsageException($"{a} needs a value");
                    values[a] = list[++i];
                    continue;
                }
                Positional.Add(a);
            }
        }

        public string Value(params string[] names) => names.Select(n => values.GetValueOrDefault(n)).FirstOrDefault(v => v != null);
        public bool Flag(string name) => flags.Contains(name);
        public int? Int(params string[] names) => Value(names) is string s ? int.TryParse(s, out var v) ? v : throw new UsageException($"{names[0]} needs a number") : null;
        public IEnumerable<string> Unknown(params string[] known) => values.Keys.Where(k => !known.Contains(k));
    }

    static int Run(string[] argv)
    {
        if (argv.Length == 0 || argv[0] is "-h" or "--help" or "help") { Help(); return argv.Length == 0 ? 2 : 0; }
        if (argv[0] is "--version" or "version") { Console.WriteLine($"Peekaboo {Commands.Version}"); return 0; }
        Displayplacer.MakeDpiAware();
        var a = new Args(argv[1..]);
        try
        {
            return argv[0] switch
            {
                "image" => Image(a),
                "see" => See(a),
                "list" => List(a),
                "permissions" => Permissions(a),
                "config" => Config(a),
                "click" => Click(a),
                "type" => TypeCommand(a),
                "press" => Press(a),
                "hotkey" => Hotkey(a),
                "scroll" => Scroll(a),
                "move" => Move(a),
                "drag" => Drag(a),
                "sleep" => SleepCommand(a),                "window" => Window(a),
                "app" => App(a),
                "agent" or "analyze" or "learn" => Unsupported($"'{argv[0]}' needs an AI model provider, which the Windows port doesn't include; use image/see output with your own model"),
                "menu" or "menubar" or "dock" or "space" or "dialog" => Unsupported($"'{argv[0]}' drives macOS-only UI (menu bar, Dock, Spaces, sheets); use see/click on the window instead"),
                _ => throw new UsageException($"unknown command '{argv[0]}' (see peekaboo --help)"),
            };
        }
        catch (COMException e) { throw new InvalidOperationException($"UI Automation failed (0x{e.HResult:x8})"); }
    }

    static int Unsupported(string message) { Console.Error.WriteLine($"peekaboo: {message}"); return 1; }

    static void Help() => Console.WriteLine("""
        Peekaboo - screen capture, UI inspection and automation

        Usage: peekaboo <command> [options]

        Capture and inspect:
          image [--app <name>|--pid <n>] [--window-title <t>] [--mode screen|window|frontmost|multi]
                [--screen-index <n>] [--path <file>] [--format png|jpg]      Screenshot
          see [--app <name>] [--window-title <t>] [--mode window|frontmost|screen] [--path <file>]
                                                    Screenshot + UI element map (B1 buttons, T1 text fields ...)
          list apps | windows --app <name> | screens | permissions
          permissions                               Show capture/automation permission status
        Interact:
          click [<text>] [--on <id>] [--coords x,y] [--snapshot <id>] [--double] [--right]
          type <text> [--press-return] [--clear] [--delay <ms>]
          press <key>... [--count <n>]              e.g. press return, press tab --count 3
          hotkey --keys "ctrl,shift,t"              (cmd is Ctrl on Windows)
          scroll --direction up|down|left|right [--amount <n>] [--on <id>|--coords x,y]
          move --coords x,y | --on <id> | --center [--smooth]
          drag --from x,y --to x,y [--duration <ms>]
          sleep <ms>
        Windows and apps:
          window close|minimize|maximize|restore|focus|move|resize|set-bounds --app <name> [--window-title <t>]
                 [--x <n> --y <n>] [--width <n> --height <n>]
          app launch|quit|relaunch|switch|hide|unhide|list --app <name> [--force] [--wait-until-ready]
        Configuration:
          config init [--force] | show | validate | path   ($PEEKABOO_CONFIG_DIR, default ~/.peekaboo)

        Add --json-output (or -j) for machine-readable output. Apps match by process name, "PID:<n>" or window title.
        """);

    // ---- JSON output -----------------------------------------------------------------------------

    static string J(string s) => MediaControl.JsonString(s);

    static int Ok(Args a, string json, string text)
    {
        if (a.Json) Console.WriteLine($"{{\"success\":true,\"data\":{json}}}");
        else if (text != null) Console.WriteLine(text);
        return 0;
    }

    // ---- windows and apps ------------------------------------------------------------------------

    sealed class Win
    {
        public IntPtr Handle;
        public string Title, App, Path;
        public int Pid;
        public Displayplacer.RECT Bounds;
        public bool Minimized, Focused;
        public int Width => Bounds.Right - Bounds.Left;
        public int Height => Bounds.Bottom - Bounds.Top;
    }

    // Visible, un-owned, uncloaked top-level windows in z-order (topmost first).
    static List<Win> Windows()
    {
        var handles = new List<IntPtr>();
        var gc = GCHandle.Alloc(handles);
        try { EnumWindows(&CollectWindow, GCHandle.ToIntPtr(gc)); }
        finally { gc.Free(); }
        var foreground = GetForegroundWindow();
        var names = new Dictionary<int, (string, string)>();
        var list = new List<Win>();
        foreach (var h in handles)
        {
            if (!IsWindowVisible(h) || GetWindow(h, GW_OWNER) != IntPtr.Zero) continue;
            if ((GetWindowLongPtrW(h, GWL_EXSTYLE) & WS_EX_TOOLWINDOW) != 0) continue;
            int cloaked = 0;
            if (DwmGetWindowAttribute(h, DWMWA_CLOAKED, &cloaked, 4) == 0 && cloaked != 0) continue;
            var length = GetWindowTextLengthW(h);
            if (length == 0) continue;
            var title = new char[length + 1];
            fixed (char* t = title) GetWindowTextW(h, t, length + 1);
            GetWindowThreadProcessId(h, out var pid);
            if (!names.TryGetValue((int)pid, out var name)) names[(int)pid] = name = ProcessInfo((int)pid);
            Displayplacer.RECT rect;
            if (DwmGetWindowAttribute(h, DWMWA_EXTENDED_FRAME_BOUNDS, &rect, sizeof(Displayplacer.RECT)) != 0) GetWindowRect(h, out rect);
            list.Add(new Win
            {
                Handle = h, Title = new string(title, 0, length), Pid = (int)pid, App = name.Item1, Path = name.Item2,
                Bounds = rect, Minimized = IsIconic(h), Focused = h == foreground,
            });
        }
        return list;
    }

    [UnmanagedCallersOnly]
    static int CollectWindow(IntPtr hwnd, IntPtr data)
    {
        ((List<IntPtr>)GCHandle.FromIntPtr(data).Target!).Add(hwnd);
        return 1;
    }

    static (string Name, string Path) ProcessInfo(int pid)
    {
        var process = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)pid);
        if (process == IntPtr.Zero) return ($"PID {pid}", null);
        try
        {
            var buffer = stackalloc char[1024];
            var size = 1024u;
            if (!QueryFullProcessImageNameW(process, 0, buffer, ref size)) return ($"PID {pid}", null);
            var path = new string(buffer, 0, (int)size);
            return (System.IO.Path.GetFileNameWithoutExtension(path), path);
        }
        finally { CloseHandle(process); }
    }

    // --app matches "PID:123", a process name, or part of a process name or window title.
    static List<Win> AppWindows(string app, string title = null)
    {
        var windows = Windows();
        IEnumerable<Win> match;
        if (app == null) match = windows;
        else if (app.StartsWith("PID:", StringComparison.OrdinalIgnoreCase) && int.TryParse(app[4..], out var pid)) match = windows.Where(w => w.Pid == pid);
        else
        {
            var exact = windows.Where(w => w.App.Equals(app, StringComparison.OrdinalIgnoreCase) || w.App.Equals(System.IO.Path.GetFileNameWithoutExtension(app), StringComparison.OrdinalIgnoreCase)).ToList();
            match = exact.Count > 0 ? exact : windows.Where(w => w.App.Contains(app, StringComparison.OrdinalIgnoreCase) || w.Title.Contains(app, StringComparison.OrdinalIgnoreCase));
        }
        if (title != null) match = match.Where(w => w.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
        return match.ToList();
    }

    static Win TargetWindow(Args a, bool frontmostDefault)
    {
        var app = a.Value("--app");
        var title = a.Value("--window-title");
        var pid = a.Int("--pid");
        if (pid is int p) app = $"PID:{p}";
        if (app == null && title == null)
        {
            if (!frontmostDefault) return null;
            var fg = GetForegroundWindow();
            return Windows().FirstOrDefault(w => w.Handle == fg) ?? Windows().FirstOrDefault() ?? throw new InvalidOperationException("no windows are open");
        }
        var windows = AppWindows(app, title);
        if (windows.Count == 0) throw new InvalidOperationException($"no window found for {(app != null ? $"app '{app}'" : "")}{(title != null ? $" titled '{title}'" : "")}");
        var index = a.Int("--window-index") ?? 0;
        return index >= 0 && index < windows.Count ? windows[index] : throw new InvalidOperationException($"window index {index} out of range ({windows.Count} windows)");
    }

    static void Focus(Win w)
    {
        if (w.Minimized) ShowWindow(w.Handle, SW_RESTORE);
        var fg = GetForegroundWindow();
        var fgThread = GetWindowThreadProcessId(fg, out _);
        var me = GetCurrentThreadId();
        // Windows only lets the foreground thread hand out focus; borrow its input queue for the call.
        var attached = fgThread != me && AttachThreadInput(me, fgThread, true);
        SetForegroundWindow(w.Handle);
        BringWindowToTop(w.Handle);
        if (attached) AttachThreadInput(me, fgThread, false);
    }

    static int List(Args a)
    {
        var what = a.Positional.FirstOrDefault() ?? throw new UsageException("list needs apps, windows, screens or permissions");
        switch (what)
        {
            case "apps":
                var apps = Windows().GroupBy(w => w.Pid).Select(g => (g.First(), g.Count(), g.Any(w => w.Focused))).OrderBy(x => x.Item1.App, StringComparer.OrdinalIgnoreCase).ToList();
                var json = "{\"applications\":[" + string.Join(",", apps.Select(x => $"{{\"app_name\":{J(x.Item1.App)},\"pid\":{x.Item1.Pid},\"path\":{J(x.Item1.Path)},\"window_count\":{x.Item2},\"is_active\":{(x.Item3 ? "true" : "false")}}}")) + "]}";
                var text = new StringBuilder($"Running Applications ({apps.Count}):\n");
                for (var i = 0; i < apps.Count; i++)
                    text.Append($"\n{i + 1}. {apps[i].Item1.App}{(apps[i].Item3 ? " [ACTIVE]" : "")}\n   Path: {apps[i].Item1.Path}\n   PID: {apps[i].Item1.Pid}\n   Windows: {apps[i].Item2}\n");
                return Ok(a, json, text.ToString().TrimEnd());
            case "windows":
                var app = a.Value("--app") ?? (a.Int("--pid") is int pid ? $"PID:{pid}" : throw new UsageException("list windows needs --app <name>"));
                var windows = AppWindows(app);
                if (windows.Count == 0) throw new InvalidOperationException($"no windows found for app '{app}'");
                var wj = "{\"target_application_info\":{\"app_name\":" + J(windows[0].App) + ",\"pid\":" + windows[0].Pid + "},\"windows\":["
                         + string.Join(",", windows.Select((w, i) => $"{{\"window_title\":{J(w.Title)},\"window_id\":{(long)w.Handle},\"window_index\":{i},\"bounds\":{{\"x\":{w.Bounds.Left},\"y\":{w.Bounds.Top},\"width\":{w.Width},\"height\":{w.Height}}},\"is_minimized\":{(w.Minimized ? "true" : "false")},\"is_on_screen\":{(!w.Minimized ? "true" : "false")}}}")) + "]}";
                var wt = new StringBuilder($"Windows for {windows[0].App} (PID {windows[0].Pid}):\n");
                for (var i = 0; i < windows.Count; i++)
                {
                    var w = windows[i];
                    wt.Append($"\n{i + 1}. \"{w.Title}\"{(w.Minimized ? " [minimized]" : "")}{(w.Focused ? " [focused]" : "")}\n   Window ID: 0x{(long)w.Handle:x}\n   Bounds: ({w.Bounds.Left}, {w.Bounds.Top}) {w.Width}x{w.Height}\n");
                }
                return Ok(a, wj, wt.ToString().TrimEnd());
            case "screens":
                var screens = Displayplacer.Screens().Where(s => s.Active).ToList();
                var sj = "{\"screens\":[" + string.Join(",", screens.Select(s => $"{{\"index\":{s.Index},\"name\":{J(s.Name)},\"width\":{s.Mode.dmPelsWidth},\"height\":{s.Mode.dmPelsHeight},\"x\":{s.Mode.dmPositionX},\"y\":{s.Mode.dmPositionY},\"scale_factor\":{(s.Dpi / 96.0).ToString(CultureInfo.InvariantCulture)},\"is_main\":{(s.Primary ? "true" : "false")}}}")) + "]}";
                var st = new StringBuilder($"Screens ({screens.Count}):\n");
                foreach (var s in screens)
                    st.Append($"\n{s.Index}. {s.Name}{(s.Primary ? " (Main)" : "")}\n   Resolution: {s.Mode.dmPelsWidth}x{s.Mode.dmPelsHeight} @ {s.Mode.dmDisplayFrequency}Hz\n   Position: ({s.Mode.dmPositionX}, {s.Mode.dmPositionY})\n   Scale: {s.Dpi * 100 / 96}%\n");
                return Ok(a, sj, st.ToString().TrimEnd());
            case "permissions": return Permissions(a);
            default: throw new UsageException($"list {what}: expected apps, windows, screens or permissions");
        }
    }

    static int Permissions(Args a) => Ok(a,
        "{\"permissions\":[{\"name\":\"Screen Recording\",\"isGranted\":true,\"isRequired\":false},{\"name\":\"Accessibility\",\"isGranted\":true,\"isRequired\":false}]}",
        "Screen Recording: Granted (Windows doesn't require a permission)\nAccessibility: Granted (UI Automation needs no permission; elevated windows need an elevated peekaboo)");

    static int Window(Args a)
    {
        var action = a.Positional.FirstOrDefault() ?? throw new UsageException("window needs an action (close, minimize, maximize, restore, focus, move, resize, set-bounds, list)");
        if (action == "list") return List(new Args(["windows", .. Rebuild(a)]));
        var w = TargetWindow(a, false) ?? throw new UsageException("window needs --app or --window-title");
        int? x = a.Int("--x"), y = a.Int("--y"), width = a.Int("--width"), height = a.Int("--height");
        switch (action)
        {
            case "close": PostMessageW(w.Handle, WM_CLOSE, 0, 0); break;
            case "minimize": ShowWindow(w.Handle, SW_MINIMIZE); break;
            case "maximize": ShowWindow(w.Handle, SW_MAXIMIZE); break;
            case "restore" or "unminimize": ShowWindow(w.Handle, SW_RESTORE); break;
            case "focus": Focus(w); break;
            case "move" or "resize" or "set-bounds":
                if (action == "move" && (x == null || y == null)) throw new UsageException("move needs --x and --y");
                if (action == "resize" && (width == null || height == null)) throw new UsageException("resize needs --width and --height");
                if (w.Minimized || IsZoomed(w.Handle)) ShowWindow(w.Handle, SW_RESTORE);
                GetWindowRect(w.Handle, out var r);
                SetWindowPos(w.Handle, IntPtr.Zero, x ?? r.Left, y ?? r.Top, width ?? r.Right - r.Left, height ?? r.Bottom - r.Top, SWP_NOZORDER | SWP_NOACTIVATE);
                break;
            default: throw new UsageException($"unknown window action '{action}'");
        }
        return Ok(a, $"{{\"action\":{J(action)},\"window_title\":{J(w.Title)},\"app_name\":{J(w.App)}}}", $"{action}: \"{w.Title}\" ({w.App})");
    }

    static IEnumerable<string> Rebuild(Args a)
    {
        foreach (var n in new[] { "--app", "--pid" }) if (a.Value(n) is string v) { yield return n; yield return v; }
        if (a.Json) yield return "--json-output";
    }

    static int App(Args a)
    {
        var action = a.Positional.FirstOrDefault() ?? throw new UsageException("app needs an action (launch, quit, relaunch, switch, hide, unhide, list)");
        if (action == "list") return List(new Args(["apps", .. a.Json ? new[] { "--json-output" } : []]));
        var name = a.Value("--app", "--name") ?? a.Positional.ElementAtOrDefault(1) ?? throw new UsageException($"app {action} needs an app name");
        switch (action)
        {
            case "launch":
                var p = Process.Start(new ProcessStartInfo(name) { UseShellExecute = true });
                if (a.Flag("--wait-until-ready")) try { p?.WaitForInputIdle(10_000); } catch (InvalidOperationException) { }
                return Ok(a, $"{{\"action\":\"launch\",\"app_name\":{J(name)},\"pid\":{p?.Id ?? 0}}}", $"Launched {name}{(p != null ? $" (PID {p.Id})" : "")}");
            case "quit" or "relaunch":
                var windows = AppWindows(name);
                var pids = windows.Select(w => w.Pid).Distinct().ToList();
                if (pids.Count == 0) pids = Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(name)).Select(x => x.Id).ToList();
                if (pids.Count == 0) throw new InvalidOperationException($"app '{name}' isn't running");
                var path = windows.FirstOrDefault()?.Path;
                foreach (var pid in pids)
                {
                    using var proc = Process.GetProcessById(pid);
                    path ??= ProcessInfo(pid).Path;
                    if (a.Flag("--force")) proc.Kill();
                    else foreach (var w in windows.Where(w => w.Pid == pid)) PostMessageW(w.Handle, WM_CLOSE, 0, 0);
                }
                if (action == "relaunch" && path != null)
                {
                    Thread.Sleep(1000);
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
                return Ok(a, $"{{\"action\":{J(action)},\"app_name\":{J(name)}}}", $"{(action == "quit" ? "Quit" : "Relaunched")} {name}");
            case "switch" or "focus" or "unhide":
                var target = AppWindows(name).FirstOrDefault() ?? throw new InvalidOperationException($"no window found for app '{name}'");
                Focus(target);
                return Ok(a, $"{{\"action\":{J(action)},\"app_name\":{J(target.App)}}}", $"Switched to {target.App}");
            case "hide":
                foreach (var w in AppWindows(name)) ShowWindow(w.Handle, SW_MINIMIZE);
                return Ok(a, $"{{\"action\":\"hide\",\"app_name\":{J(name)}}}", $"Hid {name}");
            default: throw new UsageException($"unknown app action '{action}'");
        }
    }

    // ---- screenshots -----------------------------------------------------------------------------

    static int Image(Args a)
    {
        if (a.Value("--analyze") != null) return Unsupported("--analyze needs an AI model provider, which the Windows port doesn't include");
        var format = (a.Value("--format") ?? (a.Value("--path") is string pp && System.IO.Path.GetExtension(pp).ToLowerInvariant() is ".jpg" or ".jpeg" ? "jpg" : "png")).ToLowerInvariant();
        if (format is not ("png" or "jpg" or "jpeg")) throw new UsageException("--format must be png or jpg");
        var mode = a.Value("--mode") ?? (a.Value("--app") != null || a.Value("--window-title") != null || a.Value("--pid") != null ? "window" : "screen");
        var captures = new List<(string Path, string Label, Displayplacer.RECT Bounds)>();
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        string PathFor(string label, int n, int count)
        {
            var given = a.Value("--path");
            var ext = format == "png" ? ".png" : ".jpg";
            if (given == null) return System.IO.Path.GetFullPath($"peekaboo_{label}_{stamp}{(count > 1 ? $"_{n}" : "")}{ext}");
            if (Directory.Exists(given)) return System.IO.Path.Combine(System.IO.Path.GetFullPath(given), $"peekaboo_{label}_{stamp}{(count > 1 ? $"_{n}" : "")}{ext}");
            if (count == 1) return System.IO.Path.GetFullPath(given);
            return System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(given) ?? ".", $"{System.IO.Path.GetFileNameWithoutExtension(given)}_{n}{System.IO.Path.GetExtension(given)}"));
        }
        switch (mode)
        {
            case "screen":
                var screens = Displayplacer.Screens().Where(s => s.Active).ToList();
                if (a.Int("--screen-index") is int si)
                    screens = si >= 0 && si < screens.Count ? [screens[si]] : throw new InvalidOperationException($"no screen {si} (see peekaboo list screens)");
                for (var i = 0; i < screens.Count; i++)
                {
                    var s = screens[i];
                    var rect = new Displayplacer.RECT { Left = s.Mode.dmPositionX, Top = s.Mode.dmPositionY, Right = s.Mode.dmPositionX + (int)s.Mode.dmPelsWidth, Bottom = s.Mode.dmPositionY + (int)s.Mode.dmPelsHeight };
                    var path = PathFor($"screen{s.Index}", i, screens.Count);
                    CaptureScreen(rect, path, format);
                    captures.Add((path, s.Name, rect));
                }
                break;
            case "window" or "frontmost" or "multi":
                var windows = mode == "multi" ? AppWindows(a.Value("--app") ?? throw new UsageException("--mode multi needs --app"), a.Value("--window-title"))
                    : [TargetWindow(a, true)];
                if (windows.Count == 0) throw new InvalidOperationException("no matching windows");
                for (var i = 0; i < windows.Count; i++)
                {
                    var w = windows[i];
                    var path = PathFor(Safe(w.App), i, windows.Count);
                    CaptureWindow(w, path, format);
                    captures.Add((path, $"{w.App} - {w.Title}", w.Bounds));
                }
                break;
            case "area":
                var area = (a.Value("--area") ?? throw new UsageException("--mode area needs --area x,y,width,height")).Split(',').Select(v => int.TryParse(v, out var n) ? n : throw new UsageException("--area needs x,y,width,height")).ToArray();
                if (area.Length != 4) throw new UsageException("--area needs x,y,width,height");
                var ar = new Displayplacer.RECT { Left = area[0], Top = area[1], Right = area[0] + area[2], Bottom = area[1] + area[3] };
                var ap = PathFor("area", 0, 1);
                CaptureScreen(ar, ap, format);
                captures.Add((ap, "area", ar));
                break;
            default: throw new UsageException($"unknown mode '{mode}' (screen, window, frontmost, multi, area)");
        }
        var json = "{\"saved_files\":[" + string.Join(",", captures.Select(c => $"{{\"path\":{J(c.Path)},\"item_label\":{J(c.Label)},\"mime_type\":\"image/{(format == "png" ? "png" : "jpeg")}\"}}")) + "]}";
        return Ok(a, json, string.Join("\n", captures.Select(c => $"Saved {c.Label} to {c.Path}")));
    }

    static string Safe(string s) => new(s.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());

    static void CaptureScreen(Displayplacer.RECT r, string path, string format)
    {
        var screen = GetDC(IntPtr.Zero);
        var mem = CreateCompatibleDC(screen);
        var bitmap = CreateCompatibleBitmap(screen, r.Right - r.Left, r.Bottom - r.Top);
        var old = SelectObject(mem, bitmap);
        try
        {
            if (!BitBlt(mem, 0, 0, r.Right - r.Left, r.Bottom - r.Top, screen, r.Left, r.Top, SRCCOPY | CAPTUREBLT))
                throw new InvalidOperationException("screen capture failed (is the desktop locked?)");
            SelectObject(mem, old);
            Save(bitmap, path, format);
        }
        finally
        {
            DeleteObject(bitmap);
            DeleteDC(mem);
            ReleaseDC(IntPtr.Zero, screen);
        }
    }

    // PrintWindow renders the window even when it's covered; the frame is cropped to the visible bounds.
    static void CaptureWindow(Win w, string path, string format)
    {
        if (w.Minimized) throw new InvalidOperationException($"\"{w.Title}\" is minimized (peekaboo window restore --app \"{w.App}\")");
        GetWindowRect(w.Handle, out var full);
        int fw = full.Right - full.Left, fh = full.Bottom - full.Top;
        var screen = GetDC(IntPtr.Zero);
        var mem = CreateCompatibleDC(screen);
        var bitmap = CreateCompatibleBitmap(screen, fw, fh);
        var old = SelectObject(mem, bitmap);
        var crop = CreateCompatibleDC(screen);
        var cropped = CreateCompatibleBitmap(screen, w.Width, w.Height);
        var oldCrop = SelectObject(crop, cropped);
        try
        {
            if (!PrintWindow(w.Handle, mem, PW_RENDERFULLCONTENT)) throw new InvalidOperationException($"could not capture \"{w.Title}\"");
            BitBlt(crop, 0, 0, w.Width, w.Height, mem, w.Bounds.Left - full.Left, w.Bounds.Top - full.Top, SRCCOPY);
            SelectObject(crop, oldCrop);
            SelectObject(mem, old);
            Save(cropped, path, format);
        }
        finally
        {
            DeleteObject(cropped); DeleteDC(crop);
            DeleteObject(bitmap); DeleteDC(mem);
            ReleaseDC(IntPtr.Zero, screen);
        }
    }

    static void Save(IntPtr hbitmap, string path, string format)
    {
        var input = new GdiplusStartupInput { GdiplusVersion = 1 };
        if (GdiplusStartup(out var token, &input, IntPtr.Zero) != 0) throw new InvalidOperationException("GDI+ is unavailable");
        try
        {
            if (GdipCreateBitmapFromHBITMAP(hbitmap, IntPtr.Zero, out var image) != 0) throw new InvalidOperationException("could not encode the image");
            try
            {
                var encoder = format == "png" ? new Guid("557CF406-1A04-11D3-9A73-0000F81EF32E") : new Guid("557CF401-1A04-11D3-9A73-0000F81EF32E");
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
                var status = GdipSaveImageToFile(image, path, &encoder, IntPtr.Zero);
                if (status != 0) throw new IOException($"could not write {path} (GDI+ status {status})");
            }
            finally { GdipDisposeImage(image); }
        }
        finally { GdiplusShutdown(token); }
    }

    // ---- see: UI element map ---------------------------------------------------------------------

    sealed record Element(string Id, string Role, string Name, string AutomationId, Displayplacer.RECT Bounds, bool Enabled);

    static readonly Dictionary<int, (string Prefix, string Role)> Roles = new()
    {
        [50000] = ("B", "button"), [50031] = ("B", "button"), [50002] = ("C", "checkbox"), [50003] = ("D", "combobox"),
        [50004] = ("T", "textfield"), [50030] = ("T", "textarea"), [50005] = ("L", "link"), [50007] = ("I", "listitem"),
        [50011] = ("M", "menuitem"), [50013] = ("R", "radiobutton"), [50015] = ("S", "slider"), [50019] = ("I", "tabitem"),
        [50024] = ("I", "treeitem"), [50029] = ("I", "dataitem"), [50006] = ("G", "image"), [50020] = ("X", "text"),
    };

    static int See(Args a)
    {
        var mode = a.Value("--mode") ?? "window";
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
        var snapshotDir = System.IO.Path.Combine(ConfigDir, "snapshots", stamp);
        Directory.CreateDirectory(snapshotDir);
        var shot = a.Value("--path") is string p ? System.IO.Path.GetFullPath(p) : System.IO.Path.Combine(snapshotDir, "raw.png");
        IntPtr root;
        string label;
        Displayplacer.RECT origin;
        if (mode == "screen")
        {
            var s = Displayplacer.Screens().First(x => x.Primary || x.Active);
            origin = new Displayplacer.RECT { Left = s.Mode.dmPositionX, Top = s.Mode.dmPositionY, Right = s.Mode.dmPositionX + (int)s.Mode.dmPelsWidth, Bottom = s.Mode.dmPositionY + (int)s.Mode.dmPelsHeight };
            CaptureScreen(origin, shot, "png");
            root = GetDesktopWindow();
            label = s.Name;
        }
        else
        {
            var w = TargetWindow(a, true);
            CaptureWindow(w, shot, "png");
            root = w.Handle;
            origin = w.Bounds;
            label = $"{w.App} - {w.Title}";
        }
        var elements = Walk(root, mode == "screen" ? 6 : 30);
        var map = new StringBuilder();
        map.Append($"{{\"snapshot_id\":{J(stamp)},\"screenshot\":{J(shot)},\"target\":{J(label)},\"elements\":[");
        map.Append(string.Join(",", elements.Select(e => $"{{\"id\":{J(e.Id)},\"role\":{J(e.Role)},\"label\":{J(e.Name)},\"identifier\":{J(e.AutomationId)},\"is_enabled\":{(e.Enabled ? "true" : "false")},\"bounds\":{{\"x\":{e.Bounds.Left},\"y\":{e.Bounds.Top},\"width\":{e.Bounds.Right - e.Bounds.Left},\"height\":{e.Bounds.Bottom - e.Bounds.Top}}}}}")));
        map.Append("]}");
        File.WriteAllText(System.IO.Path.Combine(snapshotDir, "map.json"), map.ToString());
        var text = new StringBuilder($"Snapshot {stamp}: {label}\nScreenshot: {shot}\nElements ({elements.Count}):\n");
        foreach (var e in elements)
            text.Append($"  {e.Id,-5} {e.Role,-12} {(e.Name.Length > 0 ? $"\"{e.Name}\"" : "(no label)")}{(e.Enabled ? "" : " [disabled]")} at ({e.Bounds.Left},{e.Bounds.Top}) {e.Bounds.Right - e.Bounds.Left}x{e.Bounds.Bottom - e.Bounds.Top}\n");
        return Ok(a, map.ToString(), text.ToString().TrimEnd());
    }

    static List<Element> Walk(IntPtr hwnd, int maxDepth)
    {
        var automation = CreateAutomation();
        var walker = automation.get_ControlViewWalker();
        var root = automation.ElementFromHandle(hwnd);
        var counters = new Dictionary<string, int>();
        var result = new List<Element>();
        void Visit(IUIAutomationElement element, int depth)
        {
            if (result.Count >= 1500 || depth > maxDepth) return;
            IUIAutomationElement child;
            try { child = walker.GetFirstChildElement(element); }
            catch (COMException) { return; }
            while (child != null && result.Count < 1500)
            {
                try
                {
                    var type = child.get_CurrentControlType();
                    if (Roles.TryGetValue(type, out var role) && child.get_CurrentIsOffscreen() == 0)
                    {
                        var r = child.get_CurrentBoundingRectangle();
                        if (r.Right > r.Left && r.Bottom > r.Top)
                        {
                            var n = counters.GetValueOrDefault(role.Prefix) + 1;
                            counters[role.Prefix] = n;
                            result.Add(new Element($"{role.Prefix}{n}", role.Role, (child.get_CurrentName() ?? "").Trim(), child.get_CurrentAutomationId() ?? "", r, child.get_CurrentIsEnabled() != 0));
                        }
                    }
                    Visit(child, depth + 1);
                    child = walker.GetNextSiblingElement(child);
                }
                catch (COMException) { break; }   // the window changed under us
            }
        }
        Visit(root, 0);
        return result;
    }

    static readonly StrategyBasedComWrappers Wrappers = new();

    static IUIAutomation CreateAutomation()
    {
        CoInitializeEx(IntPtr.Zero, 0);
        var clsid = new Guid("ff48dba4-60ef-4201-aa87-54103eef594e");
        var iid = new Guid("30cbe57d-d9d0-452a-ab13-7ac5ac4825ee");
        Marshal.ThrowExceptionForHR(CoCreateInstance(in clsid, IntPtr.Zero, 1, in iid, out var ptr));
        try { return (IUIAutomation)Wrappers.GetOrCreateObjectForComInstance(ptr, CreateObjectFlags.UniqueInstance); }
        finally { Marshal.Release(ptr); }
    }

    // Elements of a saved snapshot (the latest, or --snapshot <id>).
    static Element FindElement(string id, string snapshot)
    {
        var dir = System.IO.Path.Combine(ConfigDir, "snapshots");
        var chosen = snapshot != null ? System.IO.Path.Combine(dir, snapshot)
            : Directory.Exists(dir) ? Directory.GetDirectories(dir).OrderByDescending(d => d, StringComparer.Ordinal).FirstOrDefault() : null;
        var file = chosen == null ? null : System.IO.Path.Combine(chosen, "map.json");
        if (file == null || !File.Exists(file)) throw new InvalidOperationException("no snapshot found; run peekaboo see first");
        using var doc = JsonDocument.Parse(File.ReadAllText(file));
        foreach (var e in doc.RootElement.GetProperty("elements").EnumerateArray())
        {
            if (!e.GetProperty("id").GetString()!.Equals(id, StringComparison.OrdinalIgnoreCase)) continue;
            var b = e.GetProperty("bounds");
            int x = b.GetProperty("x").GetInt32(), y = b.GetProperty("y").GetInt32();
            return new Element(id, e.GetProperty("role").GetString(), e.GetProperty("label").GetString() ?? "", "",
                new Displayplacer.RECT { Left = x, Top = y, Right = x + b.GetProperty("width").GetInt32(), Bottom = y + b.GetProperty("height").GetInt32() }, true);
        }
        throw new InvalidOperationException($"element {id} isn't in snapshot {System.IO.Path.GetFileName(chosen)}");
    }

    // ---- interaction -----------------------------------------------------------------------------

    static (int X, int Y) Point(Args a, bool required = true)
    {
        if (a.Value("--coords") is string coords) return Cliclick.ParsePoint(coords, Cliclick.CursorPos());
        if (a.Value("--on", "--id") is string id)
        {
            var e = FindElement(id, a.Value("--snapshot"));
            return ((e.Bounds.Left + e.Bounds.Right) / 2, (e.Bounds.Top + e.Bounds.Bottom) / 2);
        }
        if (!required) return Cliclick.CursorPos();
        throw new UsageException("give --on <element id> or --coords x,y");
    }

    static int Click(Args a)
    {
        (int X, int Y) target;
        string what;
        if (a.Positional.Count > 0 && a.Value("--on", "--id", "--coords") == null)
        {
            // Click by text: search the target (or frontmost) window's controls for a matching label.
            var query = string.Join(' ', a.Positional);
            var w = TargetWindow(a, true);
            var match = Walk(w.Handle, 30).Where(e => e.Role != "text").FirstOrDefault(e => e.Name.Equals(query, StringComparison.OrdinalIgnoreCase))
                        ?? Walk(w.Handle, 30).FirstOrDefault(e => e.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                        ?? throw new InvalidOperationException($"no element labelled '{query}' in \"{w.Title}\"");
            target = ((match.Bounds.Left + match.Bounds.Right) / 2, (match.Bounds.Top + match.Bounds.Bottom) / 2);
            what = $"{match.Role} \"{match.Name}\"";
        }
        else
        {
            target = Point(a);
            what = a.Value("--on", "--id") ?? $"{target.X},{target.Y}";
        }
        Cliclick.MoveTo(target.X, target.Y);
        var button = a.Flag("--right") ? Cliclick.Button.Right : Cliclick.Button.Left;
        Cliclick.Click(button, a.Flag("--double") ? 2 : 1);
        return Ok(a, $"{{\"clicked\":{J(what)},\"x\":{target.X},\"y\":{target.Y}}}", $"Clicked {what} at ({target.X}, {target.Y})");
    }

    static int TypeCommand(Args a)
    {
        var text = a.Value("--text") ?? (a.Positional.Count > 0 ? string.Join(' ', a.Positional) : null);
        if (text == null && !a.Flag("--press-return") && !a.Flag("--clear")) throw new UsageException("type needs text");
        if (a.Value("--app") != null) Focus(TargetWindow(a, false));
        if (a.Flag("--clear"))
        {
            Cliclick.Key(Cliclick.VK_CONTROL, true); Cliclick.Tap(Cliclick.VkForChar('a')); Cliclick.Key(Cliclick.VK_CONTROL, false);
            Cliclick.Tap(Cliclick.VK_BACK);
        }
        if (text != null) Cliclick.TypeText(text, a.Int("--delay") ?? 0);
        if (a.Flag("--press-return")) Cliclick.Tap(Cliclick.VK_RETURN);
        return Ok(a, $"{{\"typed\":{J(text ?? "")}}}", $"Typed {(text ?? "").Length} characters");
    }

    static ushort KeyCode(string name)
    {
        var n = name.ToLowerInvariant();
        if (n.Length == 1) return Cliclick.VkForChar(n[0]) is var vk and not 0 ? vk : throw new UsageException($"no key for '{name}' on this keyboard layout");
        if (n[0] == 'f' && int.TryParse(n[1..], out var f) && f is >= 1 and <= 24) return (ushort)(Cliclick.VK_F1 + f - 1);
        return n switch
        {
            "return" or "enter" => Cliclick.VK_RETURN, "tab" => Cliclick.VK_TAB, "space" => Cliclick.VK_SPACE, "escape" or "esc" => Cliclick.VK_ESCAPE,
            "delete" or "backspace" => Cliclick.VK_BACK, "forward_delete" or "forwarddelete" or "del" => Cliclick.VK_DELETE,
            "up" or "arrow_up" => Cliclick.VK_UP, "down" or "arrow_down" => Cliclick.VK_DOWN, "left" or "arrow_left" => Cliclick.VK_LEFT,
            "right" or "arrow_right" => Cliclick.VK_RIGHT, "home" => Cliclick.VK_HOME, "end" => Cliclick.VK_END,
            "pageup" or "page_up" => Cliclick.VK_PRIOR, "pagedown" or "page_down" => Cliclick.VK_NEXT, "insert" => Cliclick.VK_INSERT,
            "cmd" or "command" or "ctrl" or "control" => Cliclick.VK_CONTROL, "alt" or "option" or "opt" => Cliclick.VK_MENU,
            "shift" => Cliclick.VK_SHIFT, "win" or "windows" or "meta" or "super" => Cliclick.VK_LWIN, "capslock" or "caps_lock" => Cliclick.VK_CAPITAL,
            "printscreen" or "print" => Cliclick.VK_SNAPSHOT, "menu" or "apps" => Cliclick.VK_APPS,
            "volumeup" or "volume_up" => Cliclick.VK_VOLUME_UP, "volumedown" or "volume_down" => Cliclick.VK_VOLUME_DOWN, "mute" => Cliclick.VK_VOLUME_MUTE,
            "playpause" or "play_pause" => Cliclick.VK_MEDIA_PLAY_PAUSE,
            _ => throw new UsageException($"unknown key '{name}'"),
        };
    }

    static int Press(Args a)
    {
        if (a.Positional.Count == 0) throw new UsageException("press needs at least one key, e.g. press return");
        var keys = a.Positional.Select(KeyCode).ToList();   // validate everything before sending anything
        var count = a.Int("--count") ?? 1;
        var delay = a.Int("--delay") ?? 50;
        for (var i = 0; i < count; i++)
            foreach (var k in keys) { Cliclick.Tap(k); if (delay > 0) Thread.Sleep(delay); }
        return Ok(a, $"{{\"keys\":[{string.Join(",", a.Positional.Select(J))}],\"count\":{count}}}", $"Pressed {string.Join(" ", a.Positional)}{(count > 1 ? $" x{count}" : "")}");
    }

    static int Hotkey(Args a)
    {
        var spec = a.Value("--keys") ?? (a.Positional.Count > 0 ? string.Join(",", a.Positional) : throw new UsageException("hotkey needs --keys, e.g. --keys \"ctrl,shift,t\""));
        var keys = spec.Split(',', '+', ' ').Where(k => k.Length > 0).Select(KeyCode).ToList();
        foreach (var k in keys) Cliclick.Key(k, true);
        foreach (var k in Enumerable.Reverse(keys)) Cliclick.Key(k, false);
        return Ok(a, $"{{\"keys\":{J(spec)}}}", $"Pressed {spec}");
    }

    static int Scroll(Args a)
    {
        var direction = a.Value("--direction") ?? a.Positional.FirstOrDefault() ?? throw new UsageException("scroll needs --direction up|down|left|right");
        var amount = a.Int("--amount") ?? 3;
        if (a.Value("--on", "--id", "--coords") != null) { var (x, y) = Point(a); Cliclick.MoveTo(x, y); }
        switch (direction)
        {
            case "up": Cliclick.Scroll(0, amount); break;
            case "down": Cliclick.Scroll(0, -amount); break;
            case "left": Cliclick.Scroll(-amount, 0); break;
            case "right": Cliclick.Scroll(amount, 0); break;
            default: throw new UsageException("--direction must be up, down, left or right");
        }
        return Ok(a, $"{{\"direction\":{J(direction)},\"amount\":{amount}}}", $"Scrolled {direction} {amount}");
    }

    static int Move(Args a)
    {
        (int X, int Y) target;
        if (a.Flag("--center"))
        {
            var s = Displayplacer.Screens().First(x => x.Primary || x.Active);
            target = (s.Mode.dmPositionX + (int)s.Mode.dmPelsWidth / 2, s.Mode.dmPositionY + (int)s.Mode.dmPelsHeight / 2);
        }
        else target = a.Positional.Count > 0 && a.Value("--coords", "--on", "--id") == null ? Cliclick.ParsePoint(a.Positional[0], Cliclick.CursorPos()) : Point(a);
        if (a.Flag("--smooth")) Cliclick.MoveSmooth(target.X, target.Y, a.Int("--duration") ?? 300);
        else Cliclick.MoveTo(target.X, target.Y);
        return Ok(a, $"{{\"x\":{target.X},\"y\":{target.Y}}}", $"Moved to ({target.X}, {target.Y})");
    }

    static int Drag(Args a)
    {
        (int X, int Y) Resolve(string coords, string id)
        {
            if (coords != null) return Cliclick.ParsePoint(coords, Cliclick.CursorPos());
            if (id != null) { var e = FindElement(id, a.Value("--snapshot")); return ((e.Bounds.Left + e.Bounds.Right) / 2, (e.Bounds.Top + e.Bounds.Bottom) / 2); }
            throw new UsageException("drag needs --from x,y (or --from-id) and --to x,y (or --to-id)");
        }
        var from = Resolve(a.Value("--from"), a.Value("--from-id"));
        var to = Resolve(a.Value("--to"), a.Value("--to-id"));
        Cliclick.MoveTo(from.X, from.Y);
        Cliclick.Mouse(Cliclick.Button.Left, true);
        try { Cliclick.MoveSmooth(to.X, to.Y, a.Int("--duration") ?? 500); }
        finally { Cliclick.Mouse(Cliclick.Button.Left, false); }
        return Ok(a, $"{{\"from\":{{\"x\":{from.X},\"y\":{from.Y}}},\"to\":{{\"x\":{to.X},\"y\":{to.Y}}}}}", $"Dragged from ({from.X}, {from.Y}) to ({to.X}, {to.Y})");
    }

    static int SleepCommand(Args a)
    {
        var ms = a.Positional.Count == 1 && int.TryParse(a.Positional[0], out var v) && v >= 0 ? v : a.Int("--duration") ?? throw new UsageException("sleep needs milliseconds");
        Thread.Sleep(ms);
        return Ok(a, $"{{\"slept_ms\":{ms}}}", null);
    }

    // ---- config ----------------------------------------------------------------------------------

    const string DefaultConfig = """
        {
          "screenshots": {
            "format": "png",
            "path": ""
          },
          "see": {
            "maxDepth": 30
          },
          "input": {
            "typeDelayMs": 0
          },
          "aiProviders": {
            "providers": ""
          }
        }
        """;

    static int Config(Args a)
    {
        var action = a.Positional.FirstOrDefault() ?? "show";
        var file = System.IO.Path.Combine(ConfigDir, "config.json");
        switch (action)
        {
            case "init":
                if (File.Exists(file) && !a.Flag("--force")) { Console.WriteLine($"Configuration file already exists at {file} (use --force to overwrite)"); return 0; }
                Directory.CreateDirectory(ConfigDir);
                File.WriteAllText(file, DefaultConfig + "\n");
                return Ok(a, $"{{\"path\":{J(file)}}}", $"Configuration file created at {file}");
            case "path": Console.WriteLine(file); return 0;
            case "show":
                if (!File.Exists(file)) throw new InvalidOperationException($"no configuration at {file} (run peekaboo config init)");
                Console.WriteLine(File.ReadAllText(file).TrimEnd());
                return 0;
            case "validate":
                if (!File.Exists(file)) throw new InvalidOperationException($"no configuration at {file} (run peekaboo config init)");
                try
                {
                    // Peekaboo's config is JSONC: comments and trailing commas are fine.
                    using var doc = JsonDocument.Parse(File.ReadAllText(file), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
                    if (doc.RootElement.ValueKind != JsonValueKind.Object) throw new JsonException("the top level must be an object");
                }
                catch (JsonException e) { Console.Error.WriteLine($"Configuration is invalid: {e.Message}"); return 1; }
                return Ok(a, $"{{\"valid\":true,\"path\":{J(file)}}}", "Configuration is valid");
            default: throw new UsageException($"unknown config action '{action}' (init, show, validate, path)");
        }
    }

    // ---- UI Automation COM ------------------------------------------------------------------------

    // Only the members used are typed; the rest hold their vtable slots.
    [GeneratedComInterface, Guid("30cbe57d-d9d0-452a-ab13-7ac5ac4825ee")]
    internal partial interface IUIAutomation
    {
        [PreserveSig] int CompareElements();
        [PreserveSig] int CompareRuntimeIds();
        [PreserveSig] int GetRootElement();
        IUIAutomationElement ElementFromHandle(IntPtr hwnd);
        [PreserveSig] int ElementFromPoint();
        [PreserveSig] int GetFocusedElement();
        [PreserveSig] int GetRootElementBuildCache();
        [PreserveSig] int ElementFromHandleBuildCache();
        [PreserveSig] int ElementFromPointBuildCache();
        [PreserveSig] int GetFocusedElementBuildCache();
        [PreserveSig] int CreateTreeWalker();
        IUIAutomationTreeWalker get_ControlViewWalker();
    }

    [GeneratedComInterface, Guid("4042c624-389c-4afc-a630-9df854a541fc")]
    internal partial interface IUIAutomationTreeWalker
    {
        [PreserveSig] int GetParentElement();
        IUIAutomationElement GetFirstChildElement(IUIAutomationElement element);
        [PreserveSig] int GetLastChildElement();
        IUIAutomationElement GetNextSiblingElement(IUIAutomationElement element);
    }

    [GeneratedComInterface, Guid("d22108aa-8ac5-49a5-837b-37bbb3d7591e")]
    internal partial interface IUIAutomationElement
    {
        [PreserveSig] int SetFocus();
        [PreserveSig] int GetRuntimeId();
        [PreserveSig] int FindFirst();
        [PreserveSig] int FindAll();
        [PreserveSig] int FindFirstBuildCache();
        [PreserveSig] int FindAllBuildCache();
        [PreserveSig] int BuildUpdatedCache();
        [PreserveSig] int GetCurrentPropertyValue();
        [PreserveSig] int GetCurrentPropertyValueEx();
        [PreserveSig] int GetCachedPropertyValue();
        [PreserveSig] int GetCachedPropertyValueEx();
        [PreserveSig] int GetCurrentPatternAs();
        [PreserveSig] int GetCachedPatternAs();
        [PreserveSig] int GetCurrentPattern();
        [PreserveSig] int GetCachedPattern();
        [PreserveSig] int GetCachedParent();
        [PreserveSig] int GetCachedChildren();
        int get_CurrentProcessId();
        int get_CurrentControlType();
        [return: MarshalAs(UnmanagedType.BStr)] string get_CurrentLocalizedControlType();
        [return: MarshalAs(UnmanagedType.BStr)] string get_CurrentName();
        [return: MarshalAs(UnmanagedType.BStr)] string get_CurrentAcceleratorKey();
        [return: MarshalAs(UnmanagedType.BStr)] string get_CurrentAccessKey();
        int get_CurrentHasKeyboardFocus();
        int get_CurrentIsKeyboardFocusable();
        int get_CurrentIsEnabled();
        [return: MarshalAs(UnmanagedType.BStr)] string get_CurrentAutomationId();
        [return: MarshalAs(UnmanagedType.BStr)] string get_CurrentClassName();
        [return: MarshalAs(UnmanagedType.BStr)] string get_CurrentHelpText();
        int get_CurrentCulture();
        int get_CurrentIsControlElement();
        int get_CurrentIsContentElement();
        int get_CurrentIsPassword();
        IntPtr get_CurrentNativeWindowHandle();
        [return: MarshalAs(UnmanagedType.BStr)] string get_CurrentItemType();
        int get_CurrentIsOffscreen();
        int get_CurrentOrientation();
        [return: MarshalAs(UnmanagedType.BStr)] string get_CurrentFrameworkId();
        int get_CurrentIsRequiredForForm();
        [return: MarshalAs(UnmanagedType.BStr)] string get_CurrentItemStatus();
        Displayplacer.RECT get_CurrentBoundingRectangle();
    }

    // ---- Win32 -----------------------------------------------------------------------------------

    const int GW_OWNER = 4, GWL_EXSTYLE = -20, SW_MAXIMIZE = 3, SW_MINIMIZE = 6, SW_RESTORE = 9;
    const long WS_EX_TOOLWINDOW = 0x80;
    const uint DWMWA_EXTENDED_FRAME_BOUNDS = 9, DWMWA_CLOAKED = 14, PROCESS_QUERY_LIMITED_INFORMATION = 0x1000, WM_CLOSE = 0x10;
    const uint SWP_NOZORDER = 0x4, SWP_NOACTIVATE = 0x10, SRCCOPY = 0x00CC0020, CAPTUREBLT = 0x40000000, PW_RENDERFULLCONTENT = 2;

    [StructLayout(LayoutKind.Sequential)]
    struct GdiplusStartupInput { public uint GdiplusVersion; public IntPtr DebugEventCallback; public int SuppressBackgroundThread, SuppressExternalCodecs; }

    [DllImport("user32.dll")] static extern bool EnumWindows(delegate* unmanaged<IntPtr, IntPtr, int> callback, IntPtr data);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")] static extern bool IsZoomed(IntPtr hwnd);
    [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr hwnd, int cmd);
    [DllImport("user32.dll")] static extern long GetWindowLongPtrW(IntPtr hwnd, int index);
    [DllImport("user32.dll")] static extern int GetWindowTextLengthW(IntPtr hwnd);
    [DllImport("user32.dll")] static extern int GetWindowTextW(IntPtr hwnd, char* text, int max);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hwnd, out Displayplacer.RECT rect);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern IntPtr GetDesktopWindow();
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] static extern bool BringWindowToTop(IntPtr hwnd);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hwnd, int cmd);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] static extern bool PostMessageW(IntPtr hwnd, uint msg, nuint wParam, nint lParam);
    [DllImport("user32.dll")] static extern bool AttachThreadInput(uint attach, uint to, bool on);
    [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    [DllImport("kernel32.dll")] static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);
    [DllImport("kernel32.dll")] static extern bool QueryFullProcessImageNameW(IntPtr process, uint flags, char* name, ref uint size);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
    [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr hwnd, uint attribute, void* value, int size);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool BitBlt(IntPtr dest, int x, int y, int w, int h, IntPtr src, int sx, int sy, uint rop);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr hdc);
    [DllImport("gdiplus.dll")] static extern int GdiplusStartup(out IntPtr token, GdiplusStartupInput* input, IntPtr output);
    [DllImport("gdiplus.dll")] static extern void GdiplusShutdown(IntPtr token);
    [DllImport("gdiplus.dll")] static extern int GdipCreateBitmapFromHBITMAP(IntPtr hbitmap, IntPtr palette, out IntPtr bitmap);
    [DllImport("gdiplus.dll", CharSet = CharSet.Unicode)] static extern int GdipSaveImageToFile(IntPtr image, string file, Guid* encoder, IntPtr parameters);
    [DllImport("gdiplus.dll")] static extern int GdipDisposeImage(IntPtr image);
    [DllImport("ole32.dll")] static extern int CoInitializeEx(IntPtr reserved, uint coinit);
    [DllImport("ole32.dll")] static extern int CoCreateInstance(in Guid clsid, IntPtr outer, uint context, in Guid iid, out IntPtr obj);
}
