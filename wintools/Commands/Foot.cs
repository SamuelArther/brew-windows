using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// foot / footclient: open a terminal window. foot is a Wayland terminal; Windows' own terminal is
// Windows Terminal (wt.exe, built into Windows 11), with the console host as the fallback on systems
// without it. foot's options become Windows Terminal's (-T title, -D directory, -W size, -m/-F window
// mode, -H hold) and foot.ini is read and checked like foot does (`foot --check-config`, exit 230 on
// errors): [main] title, shell, login-shell, initial-window-size-chars, initial-window-mode and
// [environment] are applied; fonts, colors and key bindings belong to Windows Terminal's settings.
// foot's server mode (foot --server + footclient) has no counterpart: Windows Terminal is its own
// single-instance host, so footclient opens a window directly.
static class Foot
{
    const string Version = "1.28.0";
    const int ExitFailure = 230;   // foot's exit status for its own failures (-26)

    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("foot", "Open a terminal window (Windows Terminal) with foot's options and foot.ini", args => Run(args, client: false));
        Commands.Add("footclient", "Open a terminal window like foot (no server needed on Windows)", args => Run(args, client: true));
    }

    sealed class Options
    {
        public string ConfigPath, Title, Directory, Term;
        public bool CheckConfig, Maximized, Fullscreen, Login, Hold, NoWait;
        public (int Cols, int Rows)? Chars;
        public (int Width, int Height)? Pixels;
        public List<string> Overrides = [];
        public List<string> Command = [];
    }

    static int Run(string[] args, bool client)
    {
        var name = client ? "footclient" : "foot";
        var o = new Options();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith('-') || arg == "-") { o.Command.AddRange(args[i..]); break; }
            if (arg == "--") { o.Command.AddRange(args[(i + 1)..]); break; }
            var (flag, inline) = SplitOption(arg);
            string Value() => inline ?? (++i < args.Length ? args[i] : throw new UsageException($"option '{flag}' requires an argument"));
            switch (flag)
            {
                case "-h" or "--help": Usage(name, client); return 0;
                case "-v" or "--version": Console.WriteLine($"{name} version: {Version} (Windows Terminal launcher)"); return 0;
                case "-c" or "--config" when !client: o.ConfigPath = Value(); break;
                case "-C" or "--check-config" when !client: o.CheckConfig = true; break;
                case "-o" or "--override": o.Overrides.Add(Value()); break;
                case "-f" or "--font" when !client: Value(); break;   // fonts are Windows Terminal profile settings
                case "-t" or "--term": o.Term = Value(); break;
                case "-T" or "--title": o.Title = Value(); break;
                case "-a" or "--app-id" or "--toplevel-tag": Value(); break;
                case "-m" or "--maximized": o.Maximized = true; break;
                case "-F" or "--fullscreen": o.Fullscreen = true; break;
                case "-L" or "--login-shell": o.Login = true; break;
                case "-D" or "--working-directory": o.Directory = Value(); break;
                case "-w" or "--window-size-pixels": o.Pixels = ParseDimensions(Value()) ?? throw new UsageException("invalid window size (must be in the form WIDTHxHEIGHT)"); break;
                case "-W" or "--window-size-chars": o.Chars = ParseDimensions(Value()) ?? throw new UsageException("invalid window size (must be in the form WIDTHxHEIGHT)"); break;
                case "-H" or "--hold": o.Hold = true; break;
                case "-N" or "--no-wait" when client: o.NoWait = true; break;
                case "-E" or "--client-environment" when client: break;   // the window always gets this environment
                case "-s" or "--server-socket" when client: Value(); break;   // no server to talk to
                case "-d" or "--log-level": Value(); break;
                case "-l" or "--log-colorize" or "-S" or "--log-no-syslog" or "-e": break;
                case "-p" or "--print-pid" when !client: Value(); break;
                case "-s" or "--server" when !client:
                    Console.Error.WriteLine("foot: server mode has no Windows counterpart: Windows Terminal hosts its own windows, so run footclient (or foot) directly");
                    return 1;
                case "--pty" when !client:
                    Console.Error.WriteLine("foot: --pty: Windows consoles can't adopt an existing PTY");
                    return 1;
                default: throw new UsageException($"unrecognized option '{arg}'");
            }
        }

        var path = o.ConfigPath ?? FindConfig();
        var config = new FootConfig();
        if (path != null)
        {
            if (!File.Exists(path))
            {
                if (o.ConfigPath != null) { Console.Error.WriteLine($"err: {path}: failed to open: No such file or directory"); return ExitFailure; }
            }
            else if (!config.Load(path, File.ReadAllLines(path))) return ExitFailure;
        }
        foreach (var o2 in o.Overrides)
            if (!config.Override(o2)) return ExitFailure;
        if (o.CheckConfig) return 0;

        return Launch(name, o, config);
    }

    static (string Flag, string Inline) SplitOption(string arg)
    {
        if (arg.StartsWith("--"))
        {
            var eq = arg.IndexOf('=');
            return eq > 0 ? (arg[..eq], arg[(eq + 1)..]) : (arg, null);
        }
        // -T title / -Ttitle
        return arg.Length > 2 && "ctoTaDwWdlpsf".Contains(arg[1]) ? (arg[..2], arg[2..]) : (arg, null);
    }

    internal static (int, int)? ParseDimensions(string text)
    {
        var parts = text.Split('x');
        return parts.Length == 2 && int.TryParse(parts[0], out var w) && int.TryParse(parts[1], out var h) && w > 0 && h > 0
            ? (w, h) : null;
    }

    // $XDG_CONFIG_HOME/foot/foot.ini, ~/.config/foot/foot.ini, then %APPDATA%\foot\foot.ini.
    static string FindConfig()
    {
        var candidates = new List<string>();
        if (Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") is { Length: > 0 } xdg) candidates.Add(Path.Combine(xdg, "foot", "foot.ini"));
        foreach (var home in new[] { Environment.GetEnvironmentVariable("HOME"), Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) })
            if (!string.IsNullOrEmpty(home)) candidates.Add(Path.Combine(home, ".config", "foot", "foot.ini"));
        if (Environment.GetEnvironmentVariable("XDG_CONFIG_DIRS") is { Length: > 0 } dirs)
            candidates.AddRange(dirs.Split(';').Select(d => Path.Combine(d, "foot", "foot.ini")));
        candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "foot", "foot.ini"));
        return candidates.FirstOrDefault(File.Exists);
    }

    static void Usage(string name, bool client)
    {
        Console.WriteLine($"Usage: {name} [OPTIONS...]\nUsage: {name} [OPTIONS...] command [ARGS...]\n\nOptions:");
        if (!client)
            Console.WriteLine("""
                  -c,--config=PATH                         load configuration from PATH ($XDG_CONFIG_HOME/foot/foot.ini)
                  -C,--check-config                        verify configuration, exit with 0 if ok, otherwise exit with 230
                """.TrimEnd());
        Console.WriteLine("""
              -o,--override=[section.]key=value        override configuration option
              -t,--term=TERM                           value to set the environment variable TERM to
              -T,--title=TITLE                         initial window title (foot)
              -m,--maximized                           start in maximized mode
              -F,--fullscreen                          start in fullscreen mode
              -L,--login-shell                         start shell as a login shell
              -D,--working-directory=DIR               directory to start in (CWD)
              -w,--window-size-pixels=WIDTHxHEIGHT     initial width and height, in pixels
              -W,--window-size-chars=WIDTHxHEIGHT      initial width and height, in characters
              -H,--hold                                remain open after child process exits
              -v,--version                             show the version number and quit
            """.TrimEnd());
        Console.WriteLine(client
            ? "  -N,--no-wait                             detach from the terminal instead of waiting for it"
            : "  -s,--server                              (not available: Windows Terminal hosts its own windows)");
        Console.WriteLine("\nWindows opens the window in Windows Terminal (or the console host without it); fonts,\ncolors and key bindings come from Windows Terminal's settings.");
    }

    static int Launch(string name, Options o, FootConfig config)
    {
        var title = o.Title ?? config.Get("main", "title") ?? "foot";
        var maximized = o.Maximized || config.Get("main", "initial-window-mode") == "maximized";
        var fullscreen = o.Fullscreen || config.Get("main", "initial-window-mode") == "fullscreen";
        // Pixel sizes become character cells of a typical 9x18 console font.
        var chars = o.Chars ?? (o.Pixels is { } px ? (Math.Max(20, px.Width / 9), Math.Max(5, px.Height / 18)) : ((int, int)?)null)
                    ?? ParseDimensions(config.Get("main", "initial-window-size-chars") ?? "");
        var directory = o.Directory != null ? Path.GetFullPath(o.Directory) : Environment.CurrentDirectory;
        var login = o.Login || FootConfig.IsTrue(config.Get("main", "login-shell"));

        // No command: the configured shell, else Windows Terminal's default profile (or cmd).
        var command = new List<string>(o.Command);
        if (command.Count == 0 && config.Get("main", "shell") is { } shell)
            command.AddRange(FootConfig.SplitCommandLine(shell));
        if (login && command.Count > 0) command = [.. Reattach.LoginShell([.. command])];
        if (o.Hold && command.Count > 0)
            command = ["cmd.exe", "/d", "/c", string.Join(" ", command.Select(Reattach.Quote)) + " & pause"];

        foreach (var (key, value) in config.Section("environment")) Environment.SetEnvironmentVariable(key, value);
        if (o.Term != null) Environment.SetEnvironmentVariable("TERM", o.Term);

        if (FindWindowsTerminal() is { } wt)
        {
            var args = new List<string> { "-w", "new" };
            if (fullscreen) args.Add("--fullscreen");
            else if (maximized) args.Add("--maximized");
            if (chars is { } size) { args.Add("--size"); args.Add($"{size.Item1},{size.Item2}"); }
            args.AddRange(["new-tab", "--title", title, "--suppressApplicationTitle", "-d", directory]);
            if (command.Count > 0)
            {
                args.Add("--");
                // wt splits its command line on ';'.
                args.AddRange(command.Select(a => a.Replace(";", "\\;")));
            }
            var psi = new ProcessStartInfo(wt) { UseShellExecute = false };
            foreach (var a in args) psi.ArgumentList.Add(a);
            using var p = Process.Start(psi)!;
            p.WaitForExit();
            return p.ExitCode;
        }
        return ConsoleHost(command.Count > 0 ? command : [Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe"],
                           title, directory, chars, maximized || fullscreen, wait: !o.NoWait);
    }

    static string FindWindowsTerminal()
    {
        var alias = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps", "wt.exe");
        if (File.Exists(alias)) return alias;
        return (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(d => Path.Combine(d, "wt.exe")).FirstOrDefault(File.Exists);
    }

    // Without Windows Terminal: a new console window (conhost) titled and sized as asked.
    static unsafe int ConsoleHost(List<string> command, string title, string directory, (int, int)? chars, bool maximized, bool wait)
    {
        var (app, arguments) = Reattach.ResolveCommand([.. command]);
        var line = (Reattach.Quote(app) + " " + arguments).TrimEnd() + "\0";
        fixed (char* t = title)
        fixed (char* cmd = line.ToCharArray())
        {
            var si = new STARTUPINFO { cb = sizeof(STARTUPINFO), lpTitle = t, dwFlags = STARTF_USESHOWWINDOW, wShowWindow = (short)(maximized ? SW_MAXIMIZE : SW_SHOWNORMAL) };
            if (chars is { } size)
            {
                si.dwFlags |= STARTF_USECOUNTCHARS;
                si.dwXCountChars = size.Item1;
                si.dwYCountChars = size.Item2;
            }
            if (!CreateProcessW(app, cmd, IntPtr.Zero, IntPtr.Zero, false, CREATE_NEW_CONSOLE, IntPtr.Zero, directory, ref si, out var pi))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            CloseHandle(pi.hThread);
            uint code = 0;
            if (wait)
            {
                WaitForSingleObject(pi.hProcess, uint.MaxValue);
                GetExitCodeProcess(pi.hProcess, out code);
            }
            CloseHandle(pi.hProcess);
            return (int)code;
        }
    }

    const uint CREATE_NEW_CONSOLE = 0x10;
    const int STARTF_USESHOWWINDOW = 0x1, STARTF_USECOUNTCHARS = 0x8, SW_SHOWNORMAL = 1, SW_MAXIMIZE = 3;

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct STARTUPINFO
    {
        public int cb;
        public char* lpReserved, lpDesktop, lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PROCESS_INFORMATION
    {
        public IntPtr hProcess, hThread;
        public uint dwProcessId, dwThreadId;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern unsafe bool CreateProcessW(string app, char* commandLine, IntPtr processAttributes, IntPtr threadAttributes,
        bool inheritHandles, uint flags, IntPtr environment, string cwd, ref STARTUPINFO si, out PROCESS_INFORMATION pi);

    [DllImport("kernel32.dll")]
    static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

    [DllImport("kernel32.dll")]
    static extern bool GetExitCodeProcess(IntPtr process, out uint code);

    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr handle);
}

// foot.ini, parsed and checked the way foot's config.c does: sections, key=value, '#' comments, and
// per-key value types with foot's error messages ("<path>:<line>: [section].key: value: message").
sealed class FootConfig
{
    readonly Dictionary<string, Dictionary<string, string>> values = new(StringComparer.Ordinal);

    // Value types per section (foot 1.28's config.c). Sections mapped to null take any key.
    static readonly Dictionary<string, Dictionary<string, string>> Schema = new()
    {
        ["main"] = Keys("include=path term=str shell=str login-shell=bool title=str locked-title=bool app-id=str toplevel-tag=str "
            + "initial-window-size-pixels=dim initial-window-size-chars=dim pad=str resize-delay-ms=uint resize-by-cells=bool "
            + "resize-keep-grid=bool bold-text-in-bright=boolish initial-window-mode=enum:windowed|maximized|fullscreen font=str "
            + "font-bold=str font-italic=str font-bold-italic=str font-size-adjustment=ptpx line-height=ptpx letter-spacing=ptpx "
            + "horizontal-letter-offset=ptpx vertical-letter-offset=ptpx underline-offset=ptpx underline-thickness=ptpx "
            + "strikeout-thickness=ptpx dpi-aware=bool workers=uint word-delimiters=str selection-target=enum:none|primary|clipboard|both "
            + "box-drawings-uses-font-glyphs=bool utmp-helper=str gamma-correct-blending=bool initial-color-theme=enum:dark|light|1|2 "
            + "uppercase-regex-insert=bool"),
        ["security"] = Keys("osc52=enum:disabled|copy-enabled|paste-enabled|enabled"),
        ["bell"] = Keys("urgent=bool notify=bool system=bool visual=bool command=str command-focused=bool"),
        ["desktop-notifications"] = Keys("command=str command-action-argument=str close=str inhibit-when-focused=bool"),
        ["scrollback"] = Keys("lines=uint indicator-position=enum:none|fixed|relative indicator-format=str multiplier=float"),
        ["url"] = Keys("launch=str label-letters=str style=enum:none|single|double|curly|dotted|dashed osc8-underline=enum:url-mode|always regex=str"),
        ["regex"] = Keys("regex=str launch=str"),
        ["colors-dark"] = ColorKeys(),
        ["colors-light"] = ColorKeys(),
        ["cursor"] = Keys("style=enum:block|underline|beam|hollow unfocused-style=enum:unchanged|hollow|none blink=bool blink-rate=uint "
            + "beam-thickness=ptpx underline-thickness=ptpx color=color2"),
        ["mouse"] = Keys("hide-when-typing=bool alternate-scroll-mode=bool"),
        ["csd"] = Keys("preferred=enum:none|server|client font=str color=color size=uint button-width=uint button-color=color "
            + "button-minimize-color=color button-maximize-color=color button-close-color=color border-color=color border-width=uint "
            + "hide-when-maximized=bool double-click-to-maximize=bool"),
        ["key-bindings"] = null, ["search-bindings"] = null, ["url-bindings"] = null,
        ["mouse-bindings"] = null, ["text-bindings"] = null, ["environment"] = null,
        ["tweak"] = Keys("scaling-filter=enum:none|nearest|bilinear|cubic|lanczos3 overflowing-glyphs=bool damage-whole-window=bool "
            + "grapheme-shaping=bool grapheme-width-method=enum:wcswidth|double-width|max render-timer=enum:none|osd|log|both "
            + "delayed-render-lower=uint delayed-render-upper=uint max-shm-pool-size-mb=uint box-drawing-base-thickness=float "
            + "box-drawing-solid-shades=bool font-monospace-warn=bool sixel=bool dim-amount=float bold-text-in-bright-amount=float "
            + "surface-bit-depth=enum:auto|8-bit|10-bit|16-bit min-stride-alignment=uint pre-apply-damage=bool"),
        ["touch"] = Keys("long-press-delay=uint"),
    };

    static Dictionary<string, string> Keys(string spec) =>
        spec.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Split('=', 2)).ToDictionary(p => p[0], p => p[1]);

    static Dictionary<string, string> ColorKeys()
    {
        var keys = Keys("foreground=color background=color selection-foreground=color selection-background=color jump-labels=color2 "
            + "scrollback-indicator=color2 search-box-no-match=color2 search-box-match=color2 cursor=color2 urls=color flash=color "
            + "alpha=unit flash-alpha=unit alpha-mode=enum:default|matching|all dim-blend-towards=enum:black|white blur=bool");
        foreach (var prefix in new[] { "regular", "bright", "dim" })
            for (var i = 0; i < 8; i++) keys[prefix + i] = "color";
        for (var i = 0; i < 16; i++) keys["sixel" + i] = "color";
        for (var i = 16; i < 256; i++) keys[i.ToString(CultureInfo.InvariantCulture)] = "color";
        return keys;
    }

    public string Get(string section, string key) =>
        values.TryGetValue(section, out var s) && s.TryGetValue(key, out var v) ? v : null;

    public IEnumerable<(string Key, string Value)> Section(string section) =>
        values.TryGetValue(section, out var s) ? s.Select(kv => (kv.Key, kv.Value)) : [];

    public bool Load(string path, string[] lines)
    {
        var section = "main";
        var valid = true;
        for (var n = 0; n < lines.Length; n++)
        {
            var line = StripComment(lines[n]).Trim();
            if (line.Length == 0) continue;
            if (line[0] == '[')
            {
                var end = line.IndexOf(']');
                if (end < 0) return Error(path, n + 1, line[1..], null, null, "syntax error: no closing ']'");
                if (end == 1) return Error(path, n + 1, "", null, null, "empty section name");
                if (end != line.Length - 1) return Error(path, n + 1, line[1..end], null, null, "section declaration contains trailing characters");
                var name = line[1..end];
                // [url-bindings:name] style suffixes are allowed for the regex section.
                var colon = name.IndexOf(':');
                var baseName = colon > 0 && name[..colon] == "regex" ? "regex" : name;
                if (baseName == "colors") baseName = "colors-dark";
                if (!Schema.ContainsKey(baseName)) return Error(path, n + 1, name, null, null, $"invalid section name: {name}");
                section = baseName;
                continue;
            }
            var eq = line.IndexOf('=');
            var key = eq < 0 ? line : line[..eq].Trim();
            var value = eq < 0 ? null : Unquote(line[(eq + 1)..].Trim());
            if (key.Length == 0 || string.IsNullOrEmpty(value))
                return Error(path, n + 1, section, null, null, $"syntax error: key/value pair has no {(key.Length == 0 ? "key" : "value")}");
            valid &= Set(path, n + 1, section, key, value);
            if (!valid) return false;
        }
        return valid;
    }

    // -o [section.]key=value
    public bool Override(string spec)
    {
        var eq = spec.IndexOf('=');
        if (eq < 0) return Error("override", 1, "main", null, null, "syntax error: key/value pair has no value");
        var lhs = spec[..eq].Trim();
        var dot = lhs.IndexOf('.');
        var section = dot > 0 ? lhs[..dot] : "main";
        var key = dot > 0 ? lhs[(dot + 1)..] : lhs;
        if (!Schema.ContainsKey(section)) return Error("override", 1, section, null, null, $"invalid section name: {section}");
        return Set("override", 1, section, key, Unquote(spec[(eq + 1)..].Trim()));
    }

    bool Set(string path, int line, string section, string key, string value)
    {
        var schema = Schema[section];
        if (schema != null)
        {
            if (!schema.TryGetValue(key, out var type)) return Error(path, line, section, key, value, $"not a valid option: {key}");
            var problem = Check(type, value);
            if (problem != null) return Error(path, line, section, key, value, problem);
        }
        if (!values.TryGetValue(section, out var s)) values[section] = s = new Dictionary<string, string>(StringComparer.Ordinal);
        s[key] = value;
        return true;
    }

    // null = valid, else foot's message.
    internal static string Check(string type, string value)
    {
        switch (type)
        {
            case "bool": return ParseBool(value) == null ? "invalid boolean value" : null;
            case "boolish": return ParseBool(value) == null && value != "palette-based" ? "invalid boolean value" : null;
            case "uint": return uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out _) ? null : $"invalid integer value, or outside range 0-{uint.MaxValue}";
            case "float": return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _) ? null : "invalid decimal value";
            case "unit":
                return !double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? "invalid decimal value"
                    : f is < 0 or > 1 ? "not in range 0.0-1.0" : null;
            case "dim": return Foot.ParseDimensions(value) == null ? "invalid dimensions (must be in the form AxB)" : null;
            case "ptpx":
                var number = value.EndsWith("px") ? value[..^2] : value;
                return double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out _) ? null : "invalid px value (must be in the form 12px)";
            case "color": return IsColor(value) ? null : "color must be in either RGB or ARGB format";
            case "color2":
                var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return parts.Length == 2 && parts.All(IsColor) ? null : "invalid double color value";
            case "path":
                var expanded = value.StartsWith('~') ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + value[1..] : value;
                return Path.IsPathRooted(expanded) ? null : "not an absolute path";
            default:
                if (type.StartsWith("enum:"))
                {
                    var options = type[5..].Split('|');
                    return options.Any(v => v.Equals(value, StringComparison.OrdinalIgnoreCase)) ? null
                        : "not one of " + string.Join(", ", options.Select(v => $"'{v}'"));
                }
                return null;
        }
    }

    internal static bool? ParseBool(string value) =>
        value.ToLowerInvariant() switch { "on" or "true" or "yes" or "1" => true, "off" or "false" or "no" or "0" => false, _ => null };

    public static bool IsTrue(string value) => value != null && ParseBool(value) == true;

    static bool IsColor(string value)
    {
        var hex = value.StartsWith("0x") ? value[2..] : value;
        return hex.Length is 6 or 8 && hex.All(Uri.IsHexDigit);
    }

    // A '#' starts a comment at the start of a line or after whitespace.
    static string StripComment(string line)
    {
        for (var i = 0; i < line.Length; i++)
            if (line[i] == '#' && (i == 0 || char.IsWhiteSpace(line[i - 1]))) return line[..i];
        return line;
    }

    static string Unquote(string value) =>
        value.Length >= 2 && value[0] == '"' && value[^1] == '"' ? value[1..^1] : value;

    static bool Error(string path, int line, string section, string key, string value, string message)
    {
        var context = key == null ? $"[{section}]" : $"[{section}].{key}" + (value == null ? "" : $": {value}");
        Console.Error.WriteLine($"err: {path}:{line}: {context}: {message}");
        return false;
    }

    // shell=... is a command line: whitespace-separated, with "..." and '...' quoting.
    internal static List<string> SplitCommandLine(string text)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        char quote = '\0';
        var any = false;
        foreach (var c in text)
        {
            if (quote != '\0') { if (c == quote) quote = '\0'; else current.Append(c); continue; }
            if (c is '"' or '\'') { quote = c; any = true; continue; }
            if (char.IsWhiteSpace(c)) { if (any || current.Length > 0) result.Add(current.ToString()); current.Clear(); any = false; continue; }
            current.Append(c);
        }
        if (any || current.Length > 0) result.Add(current.ToString());
        return result;
    }
}
