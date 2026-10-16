using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WinTools;

// cliclick / wtype: synthesize mouse and keyboard input with SendInput. cliclick's command language
// (c:x,y  m:x,y  kp:return  t:text ...) and wtype's options (-M ctrl -k c -m ctrl) are parsed up front,
// so a typo never sends half a sequence. Also hosts the input helpers peekaboo and keyd use.
static unsafe class Cliclick
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("cliclick", "Emulate mouse and keyboard events", Run);
        Commands.Add("wtype", "Type text and keys via synthesized keyboard input (xdotool type style)", RunWtype);
    }

    // ---- shared input layer ---------------------------------------------------------------------

    // dwExtraInfo on everything we inject, so keyd's hook can tell our events from the user's.
    internal const nuint Marker = 0x77746f6f;   // "wtoo"

    internal enum Button { Left, Right, Middle }

    internal static (int X, int Y) CursorPos()
    {
        Displayplacer.MakeDpiAware();
        GetCursorPos(out var p);
        return (p.X, p.Y);
    }

    internal static void MoveTo(int x, int y)
    {
        Displayplacer.MakeDpiAware();
        SetCursorPos(x, y);
    }

    // Glide in steps (cliclick -e easing, peekaboo --smooth) instead of jumping.
    internal static void MoveSmooth(int x, int y, int durationMs)
    {
        var (sx, sy) = CursorPos();
        var steps = Math.Max(1, durationMs / 10);
        for (var i = 1; i <= steps; i++)
        {
            var t = (double)i / steps;
            t = t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2;   // ease in-out
            MoveTo(sx + (int)Math.Round((x - sx) * t), sy + (int)Math.Round((y - sy) * t));
            Thread.Sleep(10);
        }
    }

    internal static void Mouse(Button button, bool down)
    {
        var flags = (button, down) switch
        {
            (Button.Left, true) => MOUSEEVENTF_LEFTDOWN, (Button.Left, false) => MOUSEEVENTF_LEFTUP,
            (Button.Right, true) => MOUSEEVENTF_RIGHTDOWN, (Button.Right, false) => MOUSEEVENTF_RIGHTUP,
            (Button.Middle, true) => MOUSEEVENTF_MIDDLEDOWN, _ => MOUSEEVENTF_MIDDLEUP,
        };
        var input = new INPUT { type = INPUT_MOUSE };
        input.u.mi = new MOUSEINPUT { dwFlags = flags, dwExtraInfo = Marker };
        Send(input);
    }

    internal static void Click(Button button, int count)
    {
        for (var i = 0; i < count; i++) { Mouse(button, true); Mouse(button, false); }
    }

    // Wheel clicks: positive dy scrolls up, positive dx scrolls right.
    internal static void Scroll(int dx, int dy)
    {
        if (dy != 0)
        {
            var input = new INPUT { type = INPUT_MOUSE };
            input.u.mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_WHEEL, mouseData = (uint)(dy * 120), dwExtraInfo = Marker };
            Send(input);
        }
        if (dx != 0)
        {
            var input = new INPUT { type = INPUT_MOUSE };
            input.u.mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_HWHEEL, mouseData = (uint)(dx * 120), dwExtraInfo = Marker };
            Send(input);
        }
    }

    internal static void Key(ushort vk, bool down)
    {
        var input = new INPUT { type = INPUT_KEYBOARD };
        var flags = down ? 0u : KEYEVENTF_KEYUP;
        if (IsExtended(vk)) flags |= KEYEVENTF_EXTENDEDKEY;
        input.u.ki = new KEYBDINPUT { wVk = vk, wScan = (ushort)MapVirtualKeyW(vk, 0), dwFlags = flags, dwExtraInfo = Marker };
        Send(input);
    }

    internal static void Tap(ushort vk) { Key(vk, true); Key(vk, false); }

    // Text goes in as Unicode characters, independent of the keyboard layout; newlines press Return.
    internal static void TypeText(string text, int delayMs)
    {
        foreach (var c in text.Replace("\r\n", "\n"))
        {
            if (c == '\n') Tap(VK_RETURN);
            else if (c == '\t') Tap(VK_TAB);
            else
            {
                var down = new INPUT { type = INPUT_KEYBOARD };
                down.u.ki = new KEYBDINPUT { wScan = c, dwFlags = KEYEVENTF_UNICODE, dwExtraInfo = Marker };
                var up = down;
                up.u.ki.dwFlags |= KEYEVENTF_KEYUP;
                Send(down);
                Send(up);
            }
            if (delayMs > 0) Thread.Sleep(delayMs);
        }
    }

    // A printable character's virtual key on the current layout (for shortcuts like ctrl+c), or 0.
    internal static ushort VkForChar(char c)
    {
        var scan = VkKeyScanW(c);
        return scan == -1 ? (ushort)0 : (ushort)(scan & 0xff);
    }

    static bool IsExtended(ushort vk) => vk is VK_RMENU or VK_RCONTROL or VK_INSERT or VK_DELETE or VK_HOME or VK_END or VK_PRIOR
        or VK_NEXT or VK_LEFT or VK_RIGHT or VK_UP or VK_DOWN or VK_NUMLOCK or VK_SNAPSHOT or VK_DIVIDE or VK_LWIN or VK_RWIN or VK_APPS;

    static void Send(INPUT input)
    {
        if (SendInput(1, &input, sizeof(INPUT)) != 1)
            throw new InvalidOperationException("input was blocked (another program is running elevated, or the desktop is locked)");
    }

    internal static (byte R, byte G, byte B) PixelColor(int x, int y)
    {
        Displayplacer.MakeDpiAware();
        var dc = GetDC(IntPtr.Zero);
        try
        {
            var c = GetPixel(dc, x, y);
            return ((byte)(c & 0xff), (byte)((c >> 8) & 0xff), (byte)((c >> 16) & 0xff));
        }
        finally { ReleaseDC(IntPtr.Zero, dc); }
    }

    // Virtual keys by common names, shared by peekaboo and keyd.
    internal const ushort VK_BACK = 0x08, VK_TAB = 0x09, VK_CLEAR = 0x0c, VK_RETURN = 0x0d, VK_SHIFT = 0x10, VK_CONTROL = 0x11, VK_MENU = 0x12,
        VK_PAUSE = 0x13, VK_CAPITAL = 0x14, VK_ESCAPE = 0x1b, VK_SPACE = 0x20, VK_PRIOR = 0x21, VK_NEXT = 0x22, VK_END = 0x23, VK_HOME = 0x24,
        VK_LEFT = 0x25, VK_UP = 0x26, VK_RIGHT = 0x27, VK_DOWN = 0x28, VK_SNAPSHOT = 0x2c, VK_INSERT = 0x2d, VK_DELETE = 0x2e,
        VK_LWIN = 0x5b, VK_RWIN = 0x5c, VK_APPS = 0x5d, VK_NUMPAD0 = 0x60, VK_MULTIPLY = 0x6a, VK_ADD = 0x6b, VK_SUBTRACT = 0x6d,
        VK_DECIMAL = 0x6e, VK_DIVIDE = 0x6f, VK_F1 = 0x70, VK_NUMLOCK = 0x90, VK_SCROLL = 0x91, VK_LSHIFT = 0xa0, VK_RSHIFT = 0xa1,
        VK_LCONTROL = 0xa2, VK_RCONTROL = 0xa3, VK_LMENU = 0xa4, VK_RMENU = 0xa5, VK_VOLUME_MUTE = 0xad, VK_VOLUME_DOWN = 0xae,
        VK_VOLUME_UP = 0xaf, VK_MEDIA_NEXT_TRACK = 0xb0, VK_MEDIA_PREV_TRACK = 0xb1, VK_MEDIA_STOP = 0xb2, VK_MEDIA_PLAY_PAUSE = 0xb3;

    // ---- cliclick -------------------------------------------------------------------------------

    sealed record Step(string Command, string Arg);

    static int Run(string[] args)
    {
        var mode = "";
        var restore = false;
        var waitMs = 20;
        var easing = 0;
        string dest = null;
        var steps = new List<Step>();
        for (var i = 0; i < args.Length; i++)
        {
            string Value() => ++i < args.Length ? args[i] : throw new UsageException($"{args[i - 1]} needs a value");
            switch (args[i])
            {
                case "-h" or "--help": CliclickHelp(); return 0;
                case "-V" or "--version": Console.WriteLine($"cliclick {Commands.Version}"); return 0;
                case "-r": restore = true; break;
                case "-m": mode = Value(); if (mode is not ("verbose" or "test")) throw new UsageException("-m must be verbose or test"); break;
                case "-w": waitMs = int.TryParse(Value(), out var w) && w >= 0 ? w : throw new UsageException("-w needs milliseconds"); break;
                case "-e": easing = int.TryParse(Value(), out var e) && e >= 0 ? e : throw new UsageException("-e needs a number"); break;
                case "-d": dest = Value(); break;
                case "-f":
                    var path = Value();
                    var lines = path == "-" ? Console.In.ReadToEnd().Split('\n') : File.ReadAllLines(path);
                    foreach (var line in lines.Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith('#')))
                        steps.Add(ParseStep(line));
                    break;
                default:
                    steps.Add(ParseStep(args[i]));
                    break;
            }
        }
        if (steps.Count == 0) { CliclickHelp(); return 2; }
        foreach (var step in steps) Validate(step);
        var test = mode == "test";
        var verbose = mode == "verbose";
        var start = CursorPos();
        var output = new List<string>();
        foreach (var step in steps)
        {
            var description = Execute(step, test, easing, output);
            if (test || verbose) Console.WriteLine(description);
            if (!test && waitMs > 0) Thread.Sleep(waitMs);
        }
        if (restore && !test) MoveTo(start.X, start.Y);
        if (output.Count > 0)
        {
            if (dest == null || dest == "stdout") foreach (var line in output) Console.WriteLine(line);
            else if (dest == "clipboard") CopyToClipboard(string.Join(Environment.NewLine, output));
            else File.AppendAllLines(dest, output);
        }
        return 0;
    }

    static void CliclickHelp() => Console.WriteLine("""
        cliclick - Emulate mouse and keyboard events

        Usage: cliclick [-r] [-m <mode>] [-d <target>] [-e <easing>] [-f <file>] [-w <ms>] command1 [command2 ...]

          -r  Restore the initial mouse location when finished
          -m  verbose (print each command) or test (only print, don't send events)
          -d  Where p and cp print to: stdout (default), clipboard or a file path
          -e  Easing factor: move the mouse gradually over roughly that many milliseconds
          -f  Read commands from a file, one per line ("-" for standard input)
          -w  Wait this many milliseconds after each command (default 20)
          -V  Show version

        Commands:
          c:x,y   Click             dc:x,y  Double-click      tc:x,y  Triple-click
          rc:x,y  Right-click       m:x,y   Move              cp:x,y  Print the colour at x,y
          dd:x,y  Drag down         dm:x,y  Drag move         du:x,y  Drag up
          kd:keys Hold keys down    ku:keys Release keys      (alt, cmd, ctrl, shift, win; comma-separated)
          kp:key  Press a key       t:text  Type text         w:ms    Wait
          p[:str] Print the mouse position, prefixed with str if given

        Coordinates: absolute (100), relative (+50, -50), current (.), or =-20 for a negative absolute value.
        On Windows, cmd is the Ctrl key (the shortcut modifier) and win is the Windows key.
        kp keys: arrow-down arrow-left arrow-right arrow-up delete end enter esc f1-f16 fwd-delete home
                 mute num-0..num-9 num-clear num-divide num-enter num-equals num-minus num-multiply num-plus
                 page-down page-up play-next play-pause play-previous return space tab volume-down volume-up
        """);

    static Step ParseStep(string text)
    {
        var colon = text.IndexOf(':');
        var command = colon < 0 ? text : text[..colon];
        var arg = colon < 0 ? null : text[(colon + 1)..];
        return new Step(command, arg);
    }

    static readonly string[] PointCommands = ["c", "dc", "tc", "rc", "m", "dd", "dm", "du", "cp"];

    static void Validate(Step s)
    {
        if (PointCommands.Contains(s.Command)) { ParsePoint(s.Arg ?? throw new UsageException($"{s.Command}: needs coordinates x,y"), (0, 0)); return; }
        switch (s.Command)
        {
            case "kd" or "ku":
                foreach (var k in (s.Arg ?? throw new UsageException($"{s.Command}: needs keys")).Split(',')) ModifierVk(k);
                break;
            case "kp": KeyVk(s.Arg ?? throw new UsageException("kp: needs a key")); break;
            case "t": if (s.Arg == null) throw new UsageException("t: needs text"); break;
            case "w": if (!int.TryParse(s.Arg, out var ms) || ms < 0) throw new UsageException("w: needs milliseconds"); break;
            case "p": break;
            default: throw new UsageException($"unknown command '{s.Command}'");
        }
    }

    // "100,200", "+10,-5", ".,300", "=-20,40" relative to the current position.
    internal static (int X, int Y) ParsePoint(string text, (int X, int Y) current)
    {
        var parts = text.Split(',');
        if (parts.Length != 2) throw new UsageException($"invalid coordinates '{text}' (expected x,y)");
        return (Coordinate(parts[0], current.X, text), Coordinate(parts[1], current.Y, text));
    }

    static int Coordinate(string part, int current, string whole)
    {
        if (part == ".") return current;
        if (part.StartsWith('=') && int.TryParse(part[1..], out var abs)) return abs;
        if ((part.StartsWith('+') || part.StartsWith('-')) && int.TryParse(part, out var rel)) return current + rel;
        if (int.TryParse(part, out var value)) return value;
        throw new UsageException($"invalid coordinates '{whole}'");
    }

    static ushort ModifierVk(string name) => name switch
    {
        "alt" => VK_MENU,
        "cmd" or "ctrl" => VK_CONTROL,
        "shift" => VK_SHIFT,
        "win" => VK_LWIN,
        "fn" => throw new UsageException("the fn key is handled by the keyboard's firmware and can't be sent on Windows"),
        _ => throw new UsageException($"unknown modifier '{name}' (alt, cmd, ctrl, shift, win)"),
    };

    static ushort KeyVk(string name)
    {
        if (name.StartsWith("num-") && name.Length == 5 && char.IsDigit(name[4])) return (ushort)(VK_NUMPAD0 + (name[4] - '0'));
        if (name.Length >= 2 && name[0] == 'f' && int.TryParse(name[1..], out var f) && f is >= 1 and <= 16) return (ushort)(VK_F1 + f - 1);
        return name switch
        {
            "arrow-down" => VK_DOWN, "arrow-left" => VK_LEFT, "arrow-right" => VK_RIGHT, "arrow-up" => VK_UP,
            "delete" => VK_BACK, "fwd-delete" => VK_DELETE, "end" => VK_END, "home" => VK_HOME, "enter" or "return" or "num-enter" => VK_RETURN,
            "esc" => VK_ESCAPE, "space" => VK_SPACE, "tab" => VK_TAB, "page-down" => VK_NEXT, "page-up" => VK_PRIOR,
            "mute" => VK_VOLUME_MUTE, "volume-down" => VK_VOLUME_DOWN, "volume-up" => VK_VOLUME_UP,
            "play-next" => VK_MEDIA_NEXT_TRACK, "play-previous" => VK_MEDIA_PREV_TRACK, "play-pause" => VK_MEDIA_PLAY_PAUSE,
            "num-clear" => VK_CLEAR, "num-divide" => VK_DIVIDE, "num-minus" => VK_SUBTRACT, "num-multiply" => VK_MULTIPLY, "num-plus" => VK_ADD,
            "num-equals" => VK_RETURN,
            "brightness-up" or "brightness-down" or "keys-light-up" or "keys-light-down" or "keys-light-toggle" =>
                throw new UsageException($"'{name}' has no Windows key code (use the brightness command for brightness)"),
            _ => throw new UsageException($"unknown key '{name}' (see cliclick -h)"),
        };
    }

    static string Execute(Step s, bool test, int easing, List<string> output)
    {
        var current = CursorPos();
        if (PointCommands.Contains(s.Command))
        {
            var (x, y) = ParsePoint(s.Arg, current);
            if (s.Command == "cp")
            {
                if (!test) { var (r, g, b) = PixelColor(x, y); output.Add($"{r} {g} {b}"); }
                return $"Print color at point {x},{y}";
            }
            if (!test)
            {
                if (easing > 0) MoveSmooth(x, y, easing); else MoveTo(x, y);
                switch (s.Command)
                {
                    case "c": Click(Button.Left, 1); break;
                    case "dc": Click(Button.Left, 2); break;
                    case "tc": Click(Button.Left, 3); break;
                    case "rc": Click(Button.Right, 1); break;
                    case "dd": Mouse(Button.Left, true); break;
                    case "du": Mouse(Button.Left, false); break;
                }
            }
            return s.Command switch
            {
                "c" => $"Click at {x},{y}", "dc" => $"Double-click at {x},{y}", "tc" => $"Triple-click at {x},{y}",
                "rc" => $"Right-click at {x},{y}", "m" => $"Move to {x},{y}", "dd" => $"Drag press at {x},{y}",
                "dm" => $"Drag move to {x},{y}", _ => $"Drag release at {x},{y}",
            };
        }
        switch (s.Command)
        {
            case "kd" or "ku":
                foreach (var k in s.Arg.Split(','))
                    if (!test) Key(ModifierVk(k), s.Command == "kd");
                return $"{(s.Command == "kd" ? "Hold" : "Release")} {s.Arg} key{(s.Arg.Contains(',') ? "s" : "")}";
            case "kp":
                if (!test) Tap(KeyVk(s.Arg));
                return $"Press {s.Arg} key";
            case "t":
                if (!test) TypeText(s.Arg, 0);
                return $"Type: “{s.Arg}”";
            case "w":
                if (!test) Thread.Sleep(int.Parse(s.Arg, CultureInfo.InvariantCulture));
                return $"Wait {s.Arg} milliseconds";
            default:
                var text = s.Arg is { Length: > 0 } prefix ? $"{prefix}: {current.X},{current.Y}" : $"{current.X},{current.Y}";
                output.Add(text);
                return "Print current mouse position";
        }
    }

    static void CopyToClipboard(string text)
    {
        if (!OpenClipboard(IntPtr.Zero)) throw new InvalidOperationException("the clipboard is in use");
        try
        {
            EmptyClipboard();
            var bytes = (text.Length + 1) * 2;
            var mem = GlobalAlloc(0x2, (nuint)bytes);
            var p = (char*)GlobalLock(mem);
            fixed (char* src = text) Buffer.MemoryCopy(src, p, bytes, text.Length * 2);
            p[text.Length] = '\0';
            GlobalUnlock(mem);
            SetClipboardData(13, mem);   // CF_UNICODETEXT
        }
        finally { CloseClipboard(); }
    }

    // ---- wtype ----------------------------------------------------------------------------------

    static int RunWtype(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            Console.WriteLine("""
                Usage: wtype <text...> [-M <modifier>] [-m <modifier>] [-P <key>] [-p <key>] [-k <key>] [-s <ms>] [-d <ms>] [-]
                  -M mod   Press modifier (shift, capslock, ctrl, logo, win, alt, altgr)
                  -m mod   Release modifier
                  -P key   Press key (XKB keysym name, e.g. Return, Left, F5, a)
                  -p key   Release key
                  -k key   Type (press and release) key
                  -s ms    Sleep
                  -d ms    Delay between typed characters
                  -        Type text read from standard input
                Options apply in order, e.g. wtype -M ctrl c -m ctrl  (Ctrl+C).
                """);
            return args.Length == 0 ? 2 : 0;
        }
        // Parse everything first: a bad key name must not leave half a shortcut pressed.
        var actions = new List<Action>();
        var held = new HashSet<ushort>();
        var delay = 0;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string Value() => ++i < args.Length ? args[i] : throw new UsageException($"{arg} needs a value");
            switch (arg)
            {
                case "-M": { var vk = WtypeModifier(Value()); actions.Add(() => { Key(vk, true); held.Add(vk); }); break; }
                case "-m": { var vk = WtypeModifier(Value()); actions.Add(() => { Key(vk, false); held.Remove(vk); }); break; }
                case "-P": { var k = WtypeKey(Value()); actions.Add(() => Press(k, true)); break; }
                case "-p": { var k = WtypeKey(Value()); actions.Add(() => Press(k, false)); break; }
                case "-k": { var k = WtypeKey(Value()); actions.Add(() => { Press(k, true); Press(k, false); }); break; }
                case "-s": { var ms = int.TryParse(Value(), out var s) && s >= 0 ? s : throw new UsageException("-s needs milliseconds"); actions.Add(() => Thread.Sleep(ms)); break; }
                case "-d": { var ms = int.TryParse(Value(), out var d) && d >= 0 ? d : throw new UsageException("-d needs milliseconds"); actions.Add(() => delay = ms); break; }
                case "-": actions.Add(() => TypeText(Console.In.ReadToEnd(), delay)); break;
                default:
                    if (arg.Length > 1 && arg[0] == '-' && arg != "--") throw new UsageException($"unknown option {arg}");
                    if (arg == "--") break;
                    actions.Add(() => TypeWithHeld(arg, delay, held));
                    break;
            }
        }
        try { foreach (var a in actions) a(); }
        finally { foreach (var vk in held) Key(vk, false); }   // never leave a modifier stuck
        return 0;
    }

    // With modifiers held (-M ctrl), characters must go as key codes so shortcuts work.
    static void TypeWithHeld(string text, int delay, HashSet<ushort> held)
    {
        if (held.Count == 0 || held.SetEquals([VK_SHIFT])) { TypeText(text, delay); return; }
        foreach (var c in text)
        {
            var vk = VkForChar(char.ToLowerInvariant(c));
            if (vk == 0) TypeText(c.ToString(), 0); else Tap(vk);
            if (delay > 0) Thread.Sleep(delay);
        }
    }

    // A key is a virtual key, or a character to send as Unicode when the layout has no key for it.
    static (ushort Vk, char Char) WtypeKey(string name)
    {
        if (WtypeKeys.TryGetValue(name, out var vk)) return (vk, '\0');
        if (name.Length >= 2 && name[0] == 'F' && int.TryParse(name[1..], out var f) && f is >= 1 and <= 24) return ((ushort)(VK_F1 + f - 1), '\0');
        if (name.StartsWith("KP_") && name.Length == 4 && char.IsDigit(name[3])) return ((ushort)(VK_NUMPAD0 + name[3] - '0'), '\0');
        if (name.Length == 1)
        {
            var v = VkForChar(char.ToLowerInvariant(name[0]));
            return (v, v == 0 ? name[0] : '\0');
        }
        throw new UsageException($"Unknown key '{name}'");
    }

    static void Press((ushort Vk, char Char) key, bool down)
    {
        if (key.Vk != 0) { Key(key.Vk, down); return; }
        if (down) TypeText(key.Char.ToString(), 0);
    }

    static ushort WtypeModifier(string name) => name.ToLowerInvariant() switch
    {
        "shift" => VK_SHIFT,
        "capslock" => VK_CAPITAL,
        "ctrl" or "control" => VK_CONTROL,
        "logo" or "win" or "super" => VK_LWIN,
        "alt" => VK_MENU,
        "altgr" => VK_RMENU,
        _ => throw new UsageException($"Invalid modifier name '{name}'"),
    };

    static readonly Dictionary<string, ushort> WtypeKeys = new(StringComparer.Ordinal)
    {
        ["Return"] = VK_RETURN, ["KP_Enter"] = VK_RETURN, ["Tab"] = VK_TAB, ["Escape"] = VK_ESCAPE, ["BackSpace"] = VK_BACK,
        ["Delete"] = VK_DELETE, ["Insert"] = VK_INSERT, ["Home"] = VK_HOME, ["End"] = VK_END, ["Page_Up"] = VK_PRIOR, ["Prior"] = VK_PRIOR,
        ["Page_Down"] = VK_NEXT, ["Next"] = VK_NEXT, ["Left"] = VK_LEFT, ["Right"] = VK_RIGHT, ["Up"] = VK_UP, ["Down"] = VK_DOWN,
        ["space"] = VK_SPACE, ["Print"] = VK_SNAPSHOT, ["Pause"] = VK_PAUSE, ["Menu"] = VK_APPS, ["Caps_Lock"] = VK_CAPITAL,
        ["Num_Lock"] = VK_NUMLOCK, ["Scroll_Lock"] = VK_SCROLL, ["Shift_L"] = VK_LSHIFT, ["Shift_R"] = VK_RSHIFT,
        ["Control_L"] = VK_LCONTROL, ["Control_R"] = VK_RCONTROL, ["Alt_L"] = VK_LMENU, ["Alt_R"] = VK_RMENU, ["ISO_Level3_Shift"] = VK_RMENU,
        ["Super_L"] = VK_LWIN, ["Super_R"] = VK_RWIN, ["KP_Add"] = VK_ADD, ["KP_Subtract"] = VK_SUBTRACT, ["KP_Multiply"] = VK_MULTIPLY,
        ["KP_Divide"] = VK_DIVIDE, ["KP_Decimal"] = VK_DECIMAL, ["XF86AudioPlay"] = VK_MEDIA_PLAY_PAUSE, ["XF86AudioPause"] = VK_MEDIA_PLAY_PAUSE,
        ["XF86AudioNext"] = VK_MEDIA_NEXT_TRACK, ["XF86AudioPrev"] = VK_MEDIA_PREV_TRACK, ["XF86AudioStop"] = VK_MEDIA_STOP,
        ["XF86AudioMute"] = VK_VOLUME_MUTE, ["XF86AudioRaiseVolume"] = VK_VOLUME_UP, ["XF86AudioLowerVolume"] = VK_VOLUME_DOWN,
    };

    // ---- Win32 ----------------------------------------------------------------------------------

    const uint INPUT_MOUSE = 0, INPUT_KEYBOARD = 1;
    const uint KEYEVENTF_EXTENDEDKEY = 0x1, KEYEVENTF_KEYUP = 0x2, KEYEVENTF_UNICODE = 0x4;
    const uint MOUSEEVENTF_LEFTDOWN = 0x2, MOUSEEVENTF_LEFTUP = 0x4, MOUSEEVENTF_RIGHTDOWN = 0x8, MOUSEEVENTF_RIGHTUP = 0x10,
               MOUSEEVENTF_MIDDLEDOWN = 0x20, MOUSEEVENTF_MIDDLEUP = 0x40, MOUSEEVENTF_WHEEL = 0x800, MOUSEEVENTF_HWHEEL = 0x1000;

    [StructLayout(LayoutKind.Sequential)]
    struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public nuint dwExtraInfo; }

    [StructLayout(LayoutKind.Sequential)]
    struct KEYBDINPUT { public ushort wVk, wScan; public uint dwFlags, time; public nuint dwExtraInfo; }

    [StructLayout(LayoutKind.Explicit)]
    struct InputUnion { [FieldOffset(0)] public MOUSEINPUT mi; [FieldOffset(0)] public KEYBDINPUT ki; }

    [StructLayout(LayoutKind.Sequential)]
    struct INPUT { public uint type; public InputUnion u; }

    [DllImport("user32.dll")] static extern uint SendInput(uint count, INPUT* inputs, int size);
    [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT point);
    [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] static extern uint MapVirtualKeyW(uint code, uint type);
    [DllImport("user32.dll")] static extern short VkKeyScanW(char c);
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);
    [DllImport("gdi32.dll")] static extern uint GetPixel(IntPtr dc, int x, int y);
    [DllImport("user32.dll")] static extern bool OpenClipboard(IntPtr owner);
    [DllImport("user32.dll")] static extern bool EmptyClipboard();
    [DllImport("user32.dll")] static extern bool CloseClipboard();
    [DllImport("user32.dll")] static extern IntPtr SetClipboardData(uint format, IntPtr mem);
    [DllImport("kernel32.dll")] static extern IntPtr GlobalAlloc(uint flags, nuint bytes);
    [DllImport("kernel32.dll")] static extern IntPtr GlobalLock(IntPtr mem);
    [DllImport("kernel32.dll")] static extern bool GlobalUnlock(IntPtr mem);
}
