using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace WinTools;

// Input-source switching on Windows keyboard layouts and IMEs.
// betterglobekey: a background keyboard hook that turns a lone key tap (Right Ctrl by default) into
// the Mac Globe key: one tap moves to the next favourite input language, a double tap to the next
// of the others. fcitx-remote (fcitx-remote-for-osx): the vim/emacs helper that reads and flips
// the foreground window's input method - IME open status (WM_IME_CONTROL) for Chinese, Japanese
// and Korean IMEs, otherwise a switch between the base (English) layout and the last other one.
static class InputSources
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("betterglobekey", "Globe-key style input language switching on a single key tap", BetterGlobeKey);
        Commands.Add("fcitx-remote", "Query and switch the input method of the focused window", FcitxRemote);
    }

    // ---- installed layouts -------------------------------------------------------------------

    internal sealed record Layout(IntPtr Hkl, string Klid, string Locale, string Name)
    {
        public override string ToString() => $"{Klid}\t{Locale}\t{Name}";
        public ushort Lang => (ushort)((long)Hkl & 0xFFFF);
        public bool Matches(string id) =>
            id.Equals(Klid, StringComparison.OrdinalIgnoreCase) || id.Equals(Locale, StringComparison.OrdinalIgnoreCase)
            || id.Equals(Name, StringComparison.OrdinalIgnoreCase) || id.Equals($"{(long)Hkl:X8}", StringComparison.OrdinalIgnoreCase);
    }

    internal static List<Layout> Layouts()
    {
        var count = GetKeyboardLayoutList(0, null);
        var hkls = new IntPtr[Math.Max(count, 0)];
        if (count > 0) count = GetKeyboardLayoutList(count, hkls);
        var list = hkls.Take(Math.Max(count, 0)).Select(Describe).ToList();
        if (list.Count == 0)
        {
            // No desktop (services, some CI sessions): fall back to the user's preloaded layouts.
            using var preload = Registry.CurrentUser.OpenSubKey(@"Keyboard Layout\Preload");
            foreach (var name in (preload?.GetValueNames() ?? []).OrderBy(n => n, StringComparer.Ordinal))
                if (preload.GetValue(name) is string klid && uint.TryParse(klid, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var id))
                    list.Add(Describe((IntPtr)(long)(((id & 0xFFFF) << 16) | (id & 0xFFFF))) with { Klid = klid.ToUpperInvariant() });
        }
        return list;
    }

    internal static Layout Describe(IntPtr hkl)
    {
        var value = (ulong)(long)hkl;
        var lang = (ushort)(value & 0xFFFF);
        var device = (ushort)((value >> 16) & 0xFFFF);
        var klid = $"{device:X8}";
        using var layouts = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Keyboard Layouts");
        if ((device & 0xF000) == 0xF000 && layouts != null)
        {
            // Layout variants (Dvorak, ...) carry a "Layout Id" instead of their KLID.
            var wanted = $"{device & 0x0FFF:X4}";
            foreach (var sub in layouts.GetSubKeyNames())
            {
                using var key = layouts.OpenSubKey(sub);
                if (key?.GetValue("Layout Id") is string id && id.Equals(wanted, StringComparison.OrdinalIgnoreCase)) { klid = sub.ToUpperInvariant(); break; }
            }
        }
        string name = null;
        using (var key = layouts?.OpenSubKey(klid))
        {
            if (key?.GetValue("Layout Display Name") is string display && display.StartsWith('@'))
            {
                var sb = new StringBuilder(256);
                if (SHLoadIndirectString(display, sb, sb.Capacity, IntPtr.Zero) == 0) name = sb.ToString();
            }
            name ??= key?.GetValue("Layout Text") as string;
        }
        var locale = new StringBuilder(85);
        var localeName = LCIDToLocaleName(lang, locale, locale.Capacity, 0) > 0 ? locale.ToString() : $"{lang:X4}";
        return new Layout(hkl, klid, localeName, name ?? klid);
    }

    static Layout Find(List<Layout> layouts, string id) =>
        layouts.FirstOrDefault(l => l.Matches(id)) ?? throw new InvalidOperationException($"no installed input source matches '{id}'");

    static IntPtr ForegroundLayout(out IntPtr window)
    {
        window = GetForegroundWindow();
        var thread = window == IntPtr.Zero ? 0 : GetWindowThreadProcessId(window, out _);
        return GetKeyboardLayout(thread);
    }

    static void Activate(IntPtr window, IntPtr hkl)
    {
        if (window == IntPtr.Zero) throw new InvalidOperationException("no focused window to switch the input source of");
        PostMessageW(window, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, hkl);
    }

    // ---- betterglobekey ----------------------------------------------------------------------

    const string GlobeHelp = """
        Usage:
          betterglobekey [--key <key>] [--config <file>]   run (keeps running; start it at login)
          betterglobekey list                              list installed input sources
          betterglobekey version

        Tap the Globe key once to switch to the next favourite input source, twice quickly to switch
        to the next of the other installed ones. A tap only counts when no other key is pressed with it,
        so the key keeps working in shortcuts.

        Config (%APPDATA%\betterglobekey\config.yaml):
          key: rctrl        # rctrl, lctrl, ralt, rshift, rwin, capslock, apps, scrolllock, f13-f24 or 0x<vk>
          favorite_input_sources:   # IDs from `betterglobekey list` (KLID, locale or name); empty = all
            - 00000409
            - 00000419
        """;

    static int BetterGlobeKey(string[] args)
    {
        string key = null, config = null;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help" or "help": Console.WriteLine(GlobeHelp); return 0;
                case "version" or "--version" or "-v": Console.WriteLine($"betterglobekey {Commands.Version}"); return 0;
                case "list":
                    foreach (var l in Layouts()) Console.WriteLine(l);
                    return 0;
                case "--key": key = ++i < args.Length ? args[i] : throw new UsageException("--key needs a key name"); break;
                case "--config": config = ++i < args.Length ? args[i] : throw new UsageException("--config needs a file"); break;
                default: throw new UsageException($"unknown argument '{args[i]}' (see betterglobekey --help)");
            }
        }
        config ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "betterglobekey", "config.yaml");
        var (configKey, favourites) = ReadConfig(config);
        Globe.Key = KeyCode(key ?? configKey ?? "rctrl");
        Globe.Favourites = favourites;
        return Globe.Run();
    }

    internal static (string Key, List<string> Favourites) ReadConfig(string path)
    {
        string key = null;
        var favourites = new List<string>();
        if (!File.Exists(path)) return (key, favourites);
        var inList = false;
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Split('#')[0].TrimEnd();
            if (line.Trim().Length == 0) continue;
            var trimmed = line.Trim();
            if (inList && trimmed.StartsWith('-')) { favourites.Add(trimmed[1..].Trim().Trim('"', '\'')); continue; }
            inList = false;
            var colon = trimmed.IndexOf(':');
            if (colon < 0) throw new InvalidOperationException($"{path}: cannot parse '{trimmed}'");
            var name = trimmed[..colon].Trim();
            var value = trimmed[(colon + 1)..].Trim().Trim('"', '\'');
            if (name == "key") key = value;
            else if (name is "favorite_input_sources" or "favourite_input_sources")
            {
                inList = value.Length == 0;
                if (value.StartsWith('[')) favourites.AddRange(value.Trim('[', ']').Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
        }
        return (key, favourites);
    }

    internal static uint KeyCode(string name) => name.ToLowerInvariant() switch
    {
        "rctrl" or "rightctrl" or "rcontrol" => 0xA3,
        "lctrl" or "leftctrl" or "lcontrol" => 0xA2,
        "ralt" or "rightalt" or "altgr" => 0xA5,
        "rshift" or "rightshift" => 0xA1,
        "rwin" or "rightwin" => 0x5C,
        "capslock" or "caps" => 0x14,
        "apps" or "menu" => 0x5D,
        "scrolllock" => 0x91,
        var f when f.StartsWith('f') && int.TryParse(f[1..], out var n) && n is >= 13 and <= 24 => (uint)(0x7C + n - 13),
        var hex when hex.StartsWith("0x") && uint.TryParse(hex[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var vk) && vk is > 0 and < 0xFF => vk,
        _ => throw new UsageException($"unknown key '{name}' (rctrl, lctrl, ralt, rshift, rwin, capslock, apps, scrolllock, f13-f24, 0x<vk>)"),
    };

    // Picks the source after the current one within a group (or the group's first if the current
    // one is outside it).
    internal static Layout Next(List<Layout> group, IntPtr current)
    {
        if (group.Count == 0) return null;
        var index = group.FindIndex(l => l.Hkl == current);
        return group[(index + 1) % group.Count];
    }

    static unsafe class Globe
    {
        public static uint Key;
        public static List<string> Favourites;
        static bool down, chorded, pending;
        static long downAt;
        static nuint timer;

        public static int Run()
        {
            var hook = SetWindowsHookExW(13 /* WH_KEYBOARD_LL */, &Hook, GetModuleHandleW(null), 0);
            if (hook == IntPtr.Zero) throw new System.ComponentModel.Win32Exception();
            Console.WriteLine($"betterglobekey {Commands.Version}: listening for the Globe key (VK 0x{Key:X2}); Ctrl+C to quit");
            MSG msg;
            while (GetMessageW(&msg, IntPtr.Zero, 0, 0) > 0)
            {
                if (msg.message == WM_TIMER && msg.wParam == timer)
                {
                    KillTimer(IntPtr.Zero, timer);
                    timer = 0;
                    if (pending) { pending = false; Switch(doubleTap: false); }
                    continue;
                }
                TranslateMessage(&msg);
                DispatchMessageW(&msg);
            }
            UnhookWindowsHookEx(hook);
            return 0;
        }

        [UnmanagedCallersOnly]
        static IntPtr Hook(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0)
            {
                var info = (KBDLLHOOKSTRUCT*)lParam;
                var isDown = wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN;
                if (info->vkCode == Key)
                {
                    if (isDown && !down) { down = true; chorded = false; downAt = Environment.TickCount64; }
                    else if (!isDown && down)
                    {
                        down = false;
                        if (!chorded && Environment.TickCount64 - downAt < 600) Tap();
                    }
                    if (Key == 0x14) return 1;   // a Globe CapsLock no longer toggles caps
                }
                else if (isDown && down) chorded = true;
            }
            return CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
        }

        static void Tap()
        {
            if (pending)
            {
                pending = false;
                KillTimer(IntPtr.Zero, timer);
                timer = 0;
                Switch(doubleTap: true);
                return;
            }
            pending = true;
            timer = SetTimer(IntPtr.Zero, 0, 300, IntPtr.Zero);
        }

        static void Switch(bool doubleTap)
        {
            try
            {
                var all = Layouts();
                var favourites = Favourites.Count == 0 ? all : all.Where(l => Favourites.Any(l.Matches)).ToList();
                var others = all.Except(favourites).ToList();
                var group = doubleTap && others.Count > 0 ? others : favourites;
                var current = ForegroundLayout(out var window);
                var next = Next(group, current);
                if (next == null || next.Hkl == current) return;
                Activate(window, next.Hkl);
                Console.WriteLine($"betterglobekey: {next}");
            }
            catch (InvalidOperationException e) { Console.Error.WriteLine($"betterglobekey: {e.Message}"); }
        }
    }

    // ---- fcitx-remote ------------------------------------------------------------------------

    const string FcitxHelp = """
        Usage: fcitx-remote [OPTION]
          -c          inactivate input method (IME off, or switch to the base layout)
          -o          activate input method (IME on, or switch back to the last other layout)
          -t, -T      switch Active/Inactive
          -n          print the current input source (KLID, see betterglobekey list)
          -s <name>   switch to the input source (KLID, locale like ja-JP, or layout name)
          [no option] display the status of the input method: 1 for inactive, 2 for active
          -h          display this help and exit

        The base layout is the first English layout installed (else the first one); set
        FCITX_REMOTE_BASE to a KLID or locale to choose another.
        """;

    static int FcitxRemote(string[] args)
    {
        if (args.Length == 0) { Console.WriteLine(Status(out _, out _, out _) ? 2 : 1); return 0; }
        switch (args[0])
        {
            case "-h" or "--help": Console.WriteLine(FcitxHelp); return 0;
            case "-n":
                Console.WriteLine(Describe(ForegroundLayout(out _)).Klid);
                return 0;
            case "-s":
                if (args.Length < 2) throw new UsageException("-s needs an input source name");
                var target = Find(Layouts(), args[1]);
                ForegroundLayout(out var window);
                Activate(window, target.Hkl);
                return 0;
            case "-c": SetActive(false); return 0;
            case "-o": SetActive(true); return 0;
            case "-t" or "-T":
                SetActive(!Status(out _, out _, out _));
                return 0;
            default: throw new UsageException($"unknown option {args[0]} (see fcitx-remote -h)");
        }
    }

    static bool IsImeLanguage(ushort lang) => (lang & 0x3FF) is 0x04 or 0x11 or 0x12;   // zh, ja, ko

    // True when the input method is active. For IME languages that is the IME's open status;
    // otherwise any layout other than the base one counts as active.
    static bool Status(out IntPtr window, out IntPtr hkl, out IntPtr imeWindow)
    {
        hkl = ForegroundLayout(out window);
        imeWindow = window == IntPtr.Zero ? IntPtr.Zero : ImmGetDefaultIMEWnd(window);
        var lang = (ushort)((long)hkl & 0xFFFF);
        if (IsImeLanguage(lang) && imeWindow != IntPtr.Zero)
            return SendMessageTimeoutW(imeWindow, WM_IME_CONTROL, IMC_GETOPENSTATUS, IntPtr.Zero, 2, 500, out var open) != IntPtr.Zero && open != IntPtr.Zero;
        var b = Base(Layouts());
        return b != null && b.Hkl != hkl;
    }

    static Layout Base(List<Layout> layouts)
    {
        var configured = Environment.GetEnvironmentVariable("FCITX_REMOTE_BASE");
        if (!string.IsNullOrEmpty(configured)) return Find(layouts, configured);
        return layouts.FirstOrDefault(l => (l.Lang & 0x3FF) == 0x09) ?? layouts.FirstOrDefault();
    }

    static void SetActive(bool active)
    {
        var isActive = Status(out var window, out var hkl, out var imeWindow);
        if (isActive == active) return;
        var lang = (ushort)((long)hkl & 0xFFFF);
        if (IsImeLanguage(lang) && imeWindow != IntPtr.Zero)
        {
            SendMessageTimeoutW(imeWindow, WM_IME_CONTROL, IMC_SETOPENSTATUS, active ? 1 : 0, 2, 500, out _);
            return;
        }
        var layouts = Layouts();
        var state = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "fcitx-remote", "last-input-source");
        if (!active)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(state)!);
            File.WriteAllText(state, Describe(hkl).Klid);
            Activate(window, Base(layouts).Hkl);
            return;
        }
        var last = File.Exists(state) ? layouts.FirstOrDefault(l => l.Matches(File.ReadAllText(state).Trim())) : null;
        last ??= layouts.FirstOrDefault(l => l.Hkl != Base(layouts).Hkl);
        if (last == null) throw new InvalidOperationException("only one input source is installed");
        Activate(window, last.Hkl);
    }

    // ---- Win32 -------------------------------------------------------------------------------

    const uint WM_INPUTLANGCHANGEREQUEST = 0x50, WM_IME_CONTROL = 0x283, WM_TIMER = 0x113;
    const int IMC_GETOPENSTATUS = 5, IMC_SETOPENSTATUS = 6;
    static readonly IntPtr WM_KEYDOWN = 0x100, WM_SYSKEYDOWN = 0x104;

    [StructLayout(LayoutKind.Sequential)]
    struct KBDLLHOOKSTRUCT { public uint vkCode, scanCode, flags, time; public nuint dwExtraInfo; }

    [StructLayout(LayoutKind.Sequential)]
    struct MSG { public IntPtr hwnd; public uint message; public nuint wParam; public nint lParam; public uint time; public int x, y; }

    [DllImport("user32.dll")] static extern int GetKeyboardLayoutList(int count, [Out] IntPtr[] list);
    [DllImport("user32.dll")] static extern IntPtr GetKeyboardLayout(uint thread);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("user32.dll")] static extern bool PostMessageW(IntPtr window, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] static extern IntPtr SendMessageTimeoutW(IntPtr window, uint msg, nint wParam, nint lParam, uint flags, uint timeout, out IntPtr result);
    [DllImport("imm32.dll")] static extern IntPtr ImmGetDefaultIMEWnd(IntPtr window);
    [DllImport("user32.dll", SetLastError = true)] static extern unsafe IntPtr SetWindowsHookExW(int id, delegate* unmanaged<int, IntPtr, IntPtr, IntPtr> proc, IntPtr module, uint thread);
    [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] static extern unsafe int GetMessageW(MSG* msg, IntPtr window, uint min, uint max);
    [DllImport("user32.dll")] static extern unsafe bool TranslateMessage(MSG* msg);
    [DllImport("user32.dll")] static extern unsafe IntPtr DispatchMessageW(MSG* msg);
    [DllImport("user32.dll")] static extern nuint SetTimer(IntPtr window, nuint id, uint ms, IntPtr proc);
    [DllImport("user32.dll")] static extern bool KillTimer(IntPtr window, nuint id);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr GetModuleHandleW(string name);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern int LCIDToLocaleName(uint lcid, StringBuilder name, int size, uint flags);
    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)] static extern int SHLoadIndirectString(string source, StringBuilder output, int size, IntPtr reserved);
}
