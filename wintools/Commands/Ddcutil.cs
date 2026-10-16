using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// ddcutil / ddcctl / m1ddc: read and set monitor controls (brightness, contrast, volume, input, power...)
// over DDC/CI through Windows' monitor configuration API (dxva2: GetVCPFeatureAndVCPFeatureReply /
// SetVCPFeature / CapabilitiesRequestAndCapabilitiesReply). Windows owns the I2C bus, so there are no
// /dev/i2c-N buses; displays are numbered like ddcutil numbers them, in screen order.
static unsafe partial class Ddcutil
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("ddcutil", "Control monitor settings using DDC/CI", Run);
        Commands.Add("ddcctl", "DDC monitor controls (brightness, contrast, volume, input) for external displays", RunDdcctl);
        Commands.Add("m1ddc", "Control external displays (luminance, contrast, volume, input) using DDC/CI", RunM1ddc);
    }

    // ---- shared DDC/CI access -------------------------------------------------------------------

    internal sealed class Monitor
    {
        public Displayplacer.Screen Screen;
        public IntPtr Handle;
        public string Description;   // dxva2's description, usually "Generic PnP Monitor"
        public string Name => Screen.Name;
    }

    // Every physical monitor of every active screen. Call Release when done.
    internal static List<Monitor> Monitors()
    {
        var list = new List<Monitor>();
        foreach (var screen in Displayplacer.Screens().Where(s => s.Active && s.HMonitor != IntPtr.Zero))
        {
            if (!GetNumberOfPhysicalMonitorsFromHMONITOR(screen.HMonitor, out var count) || count == 0) continue;
            var physical = new PHYSICAL_MONITOR[count];
            fixed (PHYSICAL_MONITOR* p = physical)
                if (!GetPhysicalMonitorsFromHMONITOR(screen.HMonitor, count, p)) continue;
            foreach (var m in physical)
                list.Add(new Monitor { Screen = screen, Handle = m.hPhysicalMonitor, Description = new string(m.szPhysicalMonitorDescription) });
        }
        return list;
    }

    internal static void Release(List<Monitor> monitors)
    {
        foreach (var m in monitors) DestroyPhysicalMonitor(m.Handle);
    }

    internal static bool TryGet(Monitor m, byte code, out uint current, out uint max)
    {
        // DDC/CI is a slow, lossy bus: retry once like ddcutil does.
        for (var attempt = 0; attempt < 2; attempt++)
            if (GetVCPFeatureAndVCPFeatureReply(m.Handle, code, out _, out current, out max)) return true;
        current = max = 0;
        return false;
    }

    internal static void Set(Monitor m, byte code, uint value)
    {
        if (!SetVCPFeature(m.Handle, code, value) && !SetVCPFeature(m.Handle, code, value))
            throw new InvalidOperationException($"{m.Name}: setting VCP 0x{code:x2} failed (the monitor did not accept the DDC/CI command)");
    }

    internal static string Capabilities(Monitor m)
    {
        if (!GetCapabilitiesStringLength(m.Handle, out var length) || length == 0) return null;
        var buffer = new byte[length];
        fixed (byte* b = buffer)
            if (!CapabilitiesRequestAndCapabilitiesReply(m.Handle, b, length)) return null;
        return Encoding.ASCII.GetString(buffer).TrimEnd('\0');
    }

    // "vcp(02 04 10 12 14(05 08 0B) 60(0F 11 12))" -> {0x02:[], 0x14:[05,08,0B], 0x60:[0F,11,12], ...}
    internal static SortedDictionary<int, List<int>> ParseVcpList(string caps)
    {
        var result = new SortedDictionary<int, List<int>>();
        var start = caps.IndexOf("vcp(", StringComparison.OrdinalIgnoreCase);
        if (start < 0) return result;
        var i = start + 4;
        var depth = 1;
        var current = -1;
        var token = new StringBuilder();
        void Flush()
        {
            if (token.Length == 0) return;
            if (int.TryParse(token.ToString(), System.Globalization.NumberStyles.HexNumber, null, out var value))
            {
                if (depth == 1) { current = value; result[value] = []; }
                else if (current >= 0) result[current].Add(value);
            }
            token.Clear();
        }
        for (; i < caps.Length && depth > 0; i++)
        {
            var c = caps[i];
            if (c == '(') { Flush(); depth++; }
            else if (c == ')') { Flush(); depth--; }
            else if (c == ' ') Flush();
            else token.Append(c);
        }
        return result;
    }

    internal static string CapsField(string caps, string name)
    {
        var start = caps.IndexOf(name + "(", StringComparison.OrdinalIgnoreCase);
        if (start < 0) return null;
        var end = caps.IndexOf(')', start);
        return end < 0 ? null : caps[(start + name.Length + 1)..end];
    }

    // MCCS feature names (the subset ddcutil users meet in practice).
    internal static readonly Dictionary<int, string> FeatureNames = new()
    {
        [0x02] = "New control value", [0x04] = "Restore factory defaults", [0x05] = "Restore factory brightness/contrast defaults",
        [0x06] = "Restore factory geometry defaults", [0x08] = "Restore color defaults", [0x0b] = "Color temperature increment",
        [0x0c] = "Color temperature request", [0x10] = "Brightness", [0x12] = "Contrast", [0x14] = "Select color preset",
        [0x16] = "Video gain: Red", [0x18] = "Video gain: Green", [0x1a] = "Video gain: Blue", [0x52] = "Active control",
        [0x60] = "Input Source", [0x62] = "Audio speaker volume", [0x6c] = "Video black level: Red", [0x6e] = "Video black level: Green",
        [0x70] = "Video black level: Blue", [0x87] = "Sharpness", [0x8d] = "Audio mute/Screen blank", [0xaa] = "Screen Orientation",
        [0xac] = "Horizontal frequency", [0xae] = "Vertical frequency", [0xb2] = "Flat panel sub-pixel layout",
        [0xb6] = "Display technology type", [0xc0] = "Display usage time", [0xc6] = "Application enable key",
        [0xc8] = "Display controller type", [0xc9] = "Display firmware level", [0xca] = "OSD/Button Control",
        [0xcc] = "OSD Language", [0xd6] = "Power mode", [0xdc] = "Display Mode", [0xdf] = "VCP Version",
    };

    // Non-continuous features print a named value instead of "current/max".
    static readonly HashSet<int> NonContinuous = [0x02, 0x04, 0x05, 0x06, 0x08, 0x14, 0x52, 0x60, 0x8d, 0xaa, 0xb2, 0xb6, 0xc6, 0xc8, 0xca, 0xcc, 0xd6, 0xdc];

    internal static readonly Dictionary<int, string> InputNames = new()
    {
        [0x01] = "VGA-1", [0x02] = "VGA-2", [0x03] = "DVI-1", [0x04] = "DVI-2", [0x05] = "Composite video 1", [0x06] = "Composite video 2",
        [0x07] = "S-Video-1", [0x08] = "S-Video-2", [0x09] = "Tuner-1", [0x0a] = "Tuner-2", [0x0b] = "Tuner-3", [0x0c] = "Component video (YPrPb/YCrCb) 1",
        [0x0d] = "Component video (YPrPb/YCrCb) 2", [0x0e] = "Component video (YPrPb/YCrCb) 3", [0x0f] = "DisplayPort-1", [0x10] = "DisplayPort-2",
        [0x11] = "HDMI-1", [0x12] = "HDMI-2", [0x1b] = "USB-C",
    };

    static string ValueName(int code, uint value) => code switch
    {
        0x60 => InputNames.GetValueOrDefault((int)(value & 0xff), "Unrecognized value"),
        0xd6 => (value & 0xff) switch { 1 => "DPM: On,  DPMS: Off", 2 => "DPM: Off, DPMS: Standby", 3 => "DPM: Off, DPMS: Suspend", 4 => "DPM: Off, DPMS: Off", 5 => "Write only value to turn off display", _ => "Unrecognized value" },
        0x8d => (value & 0xff) switch { 1 => "Mute the audio", 2 => "Unmute the audio", _ => "Unrecognized value" },
        0x14 => (value & 0xff) switch { 1 => "sRGB", 2 => "Display Native", 3 => "4000 K", 4 => "5000 K", 5 => "6500 K", 6 => "7500 K", 7 => "8200 K", 8 => "9300 K", 9 => "10000 K", 0x0b => "User 1", 0x0c => "User 2", 0x0d => "User 3", _ => "Unrecognized value" },
        _ => "Unrecognized value",
    };

    // ---- ddcutil -------------------------------------------------------------------------------

    static int Run(string[] args)
    {
        int? display = null;
        var terse = false;
        var verbose = false;
        var rest = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "-h" or "--help": Help(); return 0;
                case "-V" or "--version": Console.WriteLine($"ddcutil {Commands.Version} (Windows: DDC/CI through the monitor configuration API)"); return 0;
                case "-d" or "--display" or "--dis":
                    if (++i >= args.Length || !int.TryParse(args[i], out var d) || d < 1) throw new UsageException($"{arg} needs a display number (1, 2, ...)");
                    display = d;
                    break;
                case "-b" or "--bus":
                    throw new UsageException("Windows doesn't expose I2C buses; select a monitor with --display N (see ddcutil detect)");
                case "-t" or "--terse" or "--brief": terse = true; break;
                case "-v" or "--verbose": verbose = true; break;
                case "--noverify" or "--verify" or "--nodetect" or "--async" or "--noconfig" or "--enable-dynamic-sleep" or "--disable-dynamic-sleep": break;
                case "--sleep-multiplier" or "--maxtries" or "--trace" or "--syslog": i++; break;
                default:
                    if (arg.StartsWith("--") && !arg.StartsWith("--display=")) throw new UsageException($"unrecognized option '{arg}'");
                    if (arg.StartsWith("--display=")) { display = int.TryParse(arg[10..], out var dd) ? dd : throw new UsageException($"bad {arg}"); break; }
                    rest.Add(arg);
                    break;
            }
        }
        if (rest.Count == 0) { Help(); return 2; }
        var command = rest[0].ToLowerInvariant();
        var operands = rest.Skip(1).ToList();
        switch (command)
        {
            case "environment" or "env": return Environment(verbose);
            case "vcpinfo": return VcpInfo(operands);
            case "detect" or "det": return Detect(verbose);
            case "capabilities" or "cap": return WithDisplay(display, m => ShowCapabilities(m, verbose));
            case "getvcp" or "get":
                if (operands.Count == 0) throw new UsageException("getvcp needs a feature code (e.g. 10), 'known' or 'all'");
                var codes = ParseFeatureList(operands);
                return WithDisplay(display, m => GetVcp(m, codes, terse));
            case "setvcp" or "set":
                var settings = ParseSettings(operands);
                return WithDisplay(display, m => { foreach (var (code, sign, value) in settings) SetVcp(m, code, sign, value); return 0; });
            case "interrogate" or "probe":
                Detect(true);
                return WithDisplay(display, m => ShowCapabilities(m, true));
            default:
                throw new UsageException($"unrecognized command '{rest[0]}' (detect, capabilities, getvcp, setvcp, vcpinfo, environment)");
        }
    }

    static void Help() => Console.WriteLine("""
        Usage: ddcutil [options] command [arguments]
        Commands:
          detect                          Report monitors and whether they answer DDC/CI
          capabilities                    Query and show the monitor's capabilities string
          getvcp <code...|known|all>      Read features, e.g. getvcp 10 12
          setvcp <code> [+|-] <value> ... Write features, e.g. setvcp 10 70, setvcp 10 + 5
          vcpinfo [code...]               Show MCCS feature names
          environment                     Probe the DDC/CI environment
        Options:
          -d, --display <n>               Monitor number from detect (default 1)
          -t, --terse, --brief            Machine-readable output
          -v, --verbose                   More detail
          -V, --version                   Show version
        On Windows, monitors are reached through the monitor configuration API (dxva2); there are no
        /dev/i2c buses, so --bus is not available.
        """);

    // Valid displays are those answering DDC/CI, numbered from 1.
    static int WithDisplay(int? number, Func<Monitor, int> action)
    {
        var monitors = Monitors();
        try
        {
            var valid = monitors.Where(m => TryGet(m, 0x10, out _, out _) || TryGet(m, 0xdf, out _, out _)).ToList();
            if (valid.Count == 0) throw new InvalidOperationException("No displays found that support DDC/CI (built-in laptop panels don't; check the monitor's OSD for a DDC/CI setting)");
            var n = number ?? 1;
            if (n > valid.Count) throw new InvalidOperationException($"Display {n} not found ({valid.Count} DDC/CI display(s), see ddcutil detect)");
            return action(valid[n - 1]);
        }
        finally { Release(monitors); }
    }

    static int Detect(bool verbose)
    {
        var monitors = Monitors();
        try
        {
            if (monitors.Count == 0) { Console.WriteLine("No displays found."); return 0; }
            var number = 0;
            foreach (var m in monitors)
            {
                var ok = TryGet(m, 0x10, out _, out _) || TryGet(m, 0xdf, out _, out _);
                Console.WriteLine(ok ? $"Display {++number}" : "Invalid display");
                Console.WriteLine($"   Windows monitor:     {m.Screen.GdiName}{(m.Screen.Primary ? " (main)" : "")}");
                Console.WriteLine($"   Monitor:             {m.Name}{(m.Screen.BuiltIn ? " (built-in)" : "")}");
                Console.WriteLine($"   Persistent id:       {m.Screen.PersistentId}");
                if (ok && TryGet(m, 0xdf, out var version, out _) && version != 0)
                    Console.WriteLine($"   VCP version:         {(version >> 8) & 0xff}.{version & 0xff}");
                if (!ok) Console.WriteLine("   DDC communication failed" + (m.Screen.BuiltIn ? " (built-in panels use the brightness command instead)" : ""));
                if (verbose && ok)
                {
                    var caps = Capabilities(m);
                    if (caps != null) Console.WriteLine($"   Model:               {CapsField(caps, "model") ?? "(not reported)"}");
                }
                Console.WriteLine();
            }
            return 0;
        }
        finally { Release(monitors); }
    }

    static int ShowCapabilities(Monitor m, bool verbose)
    {
        var caps = Capabilities(m) ?? throw new InvalidOperationException($"{m.Name}: the monitor did not return a capabilities string");
        if (verbose) Console.WriteLine($"Unparsed capabilities string: {caps}");
        Console.WriteLine($"Model: {CapsField(caps, "model") ?? m.Name}");
        Console.WriteLine($"MCCS version: {CapsField(caps, "mccs_ver") ?? "unknown"}");
        var type = CapsField(caps, "type");
        if (type != null) Console.WriteLine($"Display type: {type}");
        Console.WriteLine("VCP Features:");
        foreach (var (code, values) in ParseVcpList(caps))
        {
            Console.WriteLine($"   Feature: {code:X2} ({FeatureNames.GetValueOrDefault(code, "Manufacturer specific feature")})");
            if (values.Count == 0) continue;
            Console.WriteLine("      Values:");
            foreach (var v in values)
                Console.WriteLine($"         {v:x2}: {(code == 0x60 || code == 0xd6 || code == 0x14 || code == 0x8d ? ValueName(code, (uint)v) : "")}".TrimEnd());
        }
        return 0;
    }

    static List<int> ParseFeatureList(List<string> operands)
    {
        if (operands.Count == 1 && operands[0].ToLowerInvariant() is "known" or "all" or "scan")
            return operands[0].ToLowerInvariant() == "known" ? FeatureNames.Keys.Where(k => k >= 0x10).Order().ToList() : Enumerable.Range(1, 0xfe).ToList();
        return operands.Select(ParseCode).ToList();
    }

    static int ParseCode(string text)
    {
        var t = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? text[2..] : text.TrimStart('x', 'X');
        if (!int.TryParse(t, System.Globalization.NumberStyles.HexNumber, null, out var code) || code is < 0 or > 0xff)
            throw new UsageException($"invalid feature code '{text}' (hex, e.g. 10 or 0x10)");
        return code;
    }

    static List<(int Code, char Sign, uint Value)> ParseSettings(List<string> operands)
    {
        var settings = new List<(int, char, uint)>();
        for (var i = 0; i < operands.Count;)
        {
            var code = ParseCode(operands[i++]);
            var sign = ' ';
            if (i < operands.Count && operands[i] is "+" or "-") sign = operands[i++][0];
            if (i >= operands.Count) throw new UsageException($"setvcp {operands[0]}: missing value");
            var text = operands[i++];
            uint value;
            var ok = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? uint.TryParse(text[2..], System.Globalization.NumberStyles.HexNumber, null, out value)
                : text.StartsWith('x') ? uint.TryParse(text[1..], System.Globalization.NumberStyles.HexNumber, null, out value)
                : uint.TryParse(text, out value);
            if (!ok) throw new UsageException($"invalid value '{text}'");
            settings.Add((code, sign, value));
        }
        if (settings.Count == 0) throw new UsageException("setvcp needs a feature code and a value, e.g. setvcp 10 70");
        return settings;
    }

    static int GetVcp(Monitor m, List<int> codes, bool terse)
    {
        var status = 0;
        var many = codes.Count > 20;
        foreach (var code in codes)
        {
            var name = FeatureNames.GetValueOrDefault(code, "Manufacturer specific feature");
            if (!TryGet(m, (byte)code, out var current, out var max))
            {
                if (many) continue;    // scans skip unsupported features quietly
                Console.WriteLine(terse ? $"VCP {code:X2} ERR" : $"VCP code 0x{code:x2} ({name,-30}): Unsupported feature code");
                status = 1;
                continue;
            }
            if (code == 0xdf)
                Console.WriteLine(terse ? $"VCP DF C {(current >> 8) & 0xff}.{current & 0xff}" : $"VCP code 0xdf ({name,-30}): {(current >> 8) & 0xff}.{current & 0xff}");
            else if (NonContinuous.Contains(code))
                Console.WriteLine(terse ? $"VCP {code:X2} SNC x{current & 0xff:x2}" : $"VCP code 0x{code:x2} ({name,-30}): {ValueName(code, current)} (sl=0x{current & 0xff:x2})");
            else
                Console.WriteLine(terse ? $"VCP {code:X2} C {current} {max}" : $"VCP code 0x{code:x2} ({name,-30}): current value = {current,5}, max value = {max,5}");
        }
        return status;
    }

    static void SetVcp(Monitor m, int code, char sign, uint value)
    {
        if (sign != ' ')
        {
            if (!TryGet(m, (byte)code, out var current, out var max)) throw new InvalidOperationException($"cannot read feature 0x{code:x2} to change it relatively");
            var target = sign == '+' ? Math.Min(max, current + value) : current > value ? current - value : 0;
            value = target;
        }
        Set(m, (byte)code, value);
    }

    static int VcpInfo(List<string> operands)
    {
        var codes = operands.Count == 0 ? FeatureNames.Keys.Order().ToList() : operands.Select(ParseCode).ToList();
        foreach (var code in codes)
        {
            var type = code == 0xdf ? "RO" : NonContinuous.Contains(code) ? "NC" : "C";
            Console.WriteLine($"VCP code {code:X2}: {FeatureNames.GetValueOrDefault(code, "Manufacturer specific feature")} ({type})");
        }
        return 0;
    }

    static int Environment(bool verbose)
    {
        Console.WriteLine("The following tests probe the runtime environment using multiple overlapping methods.");
        Console.WriteLine();
        Console.WriteLine($"Operating system:     {RuntimeInformation.OSDescription}");
        Console.WriteLine($"Architecture:         {RuntimeInformation.OSArchitecture}");
        var dxva2 = NativeLibrary.TryLoad("dxva2.dll", out var lib);
        if (dxva2) NativeLibrary.Free(lib);
        Console.WriteLine($"DDC/CI API:           dxva2.dll monitor configuration API {(dxva2 ? "present" : "MISSING")}");
        Console.WriteLine("I2C buses:            not exposed on Windows (the graphics driver owns them)");
        var screens = Displayplacer.Screens();
        Console.WriteLine($"Screens:              {screens.Count(s => s.Active)} active, {screens.Count} connected");
        var monitors = Monitors();
        try
        {
            foreach (var m in monitors)
            {
                var ok = TryGet(m, 0x10, out var b, out var max);
                Console.WriteLine($"   {m.Screen.GdiName} {m.Name}{(m.Screen.BuiltIn ? " (built-in)" : "")}: DDC/CI {(ok ? $"responds (brightness {b}/{max})" : "does not respond")}");
                if (verbose && ok) Console.WriteLine($"      capabilities: {Capabilities(m) ?? "(none)"}");
            }
            if (monitors.Count == 0) Console.WriteLine("   No physical monitors reported by Windows.");
        }
        finally { Release(monitors); }
        return 0;
    }

    // ---- ddcctl ---------------------------------------------------------------------------------

    static int RunDdcctl(string[] args)
    {
        if (args.Length == 0 || args.Contains("-h") || args.Contains("--help"))
        {
            Console.WriteLine("""
                Usage:
                ddcctl  -d <1-..>  [display#]
                        -w 100000  [delay usecs between settings]

                ----- Basic settings -----
                        -b <1-..>  [brightness]
                        -c <1-..>  [contrast]
                        -rbc       [reset brightness and contrast]

                ----- Settings that don't always work -----
                        -m <1|2>   [mute speaker OFF/ON]
                        -v <1-254> [speaker volume]
                        -i <1-18>  [select input source]
                        -p <1|2-5> [power on | standby/off]
                        -o         [read-only orientation]

                ----- Settings (testing) -----
                        -rg <1-..>  [red gain]
                        -gg <1-..>  [green gain]
                        -bg <1-..>  [blue gain]
                        -rrgb       [reset color]

                ----- Setting grammar -----
                        -X ?       (queries setting X)
                        -X NN      (setting X to NN)
                        -X <NN>-   (decreases setting X by NN)
                        -X <NN>+   (increases setting X by NN)
                """);
            return args.Length == 0 ? 2 : 0;
        }
        var display = 1;
        var delay = 0;
        var actions = new List<(byte Code, string Value, string Label)>();
        for (var i = 0; i < args.Length; i++)
        {
            string Next() => ++i < args.Length ? args[i] : throw new UsageException($"{args[i - 1]} needs a value");
            switch (args[i])
            {
                case "-d": display = int.TryParse(Next(), out var d) ? d : throw new UsageException("-d needs a display number"); break;
                case "-w": delay = int.TryParse(Next(), out var w) ? w : throw new UsageException("-w needs microseconds"); break;
                case "-b": actions.Add((0x10, Next(), "brightness")); break;
                case "-c": actions.Add((0x12, Next(), "contrast")); break;
                case "-m": actions.Add((0x8d, Next(), "mute")); break;
                case "-v": actions.Add((0x62, Next(), "volume")); break;
                case "-i": actions.Add((0x60, Next(), "input")); break;
                case "-p": actions.Add((0xd6, Next(), "power")); break;
                case "-o": actions.Add((0xaa, "?", "orientation")); break;
                case "-rg": actions.Add((0x16, Next(), "red gain")); break;
                case "-gg": actions.Add((0x18, Next(), "green gain")); break;
                case "-bg": actions.Add((0x1a, Next(), "blue gain")); break;
                case "-rbc": actions.Add((0x05, "1", "reset brightness and contrast")); break;
                case "-rrgb": actions.Add((0x08, "1", "reset color")); break;
                default: throw new UsageException($"unknown option {args[i]}");
            }
        }
        var monitors = Monitors();
        try
        {
            var external = monitors.Where(m => !m.Screen.BuiltIn).ToList();
            Console.WriteLine($"I: found {external.Count} external display{(external.Count == 1 ? "" : "s")}");
            if (display < 1 || display > external.Count)
            {
                Console.WriteLine($"E: Display index {display} out of range");
                return 1;
            }
            var m = external[display - 1];
            Console.WriteLine($"I: polling display {display}'s EDID: {m.Name} ({m.Screen.GdiName})");
            var status = 0;
            foreach (var (code, value, label) in actions)
            {
                if (delay > 0) Thread.Sleep(delay / 1000);
                if (value == "?")
                {
                    if (TryGet(m, code, out var cur, out var max)) Console.WriteLine($"I: VCP control #{code} (0x{code:X2}) = current: {cur}, max: {max}");
                    else { Console.WriteLine($"E: reading VCP control #{code} (0x{code:X2}) failed"); status = 1; }
                    continue;
                }
                uint target;
                if (value.EndsWith('+') || value.EndsWith('-'))
                {
                    if (!uint.TryParse(value[..^1], out var delta)) throw new UsageException($"bad value '{value}'");
                    if (!TryGet(m, code, out var cur, out var max)) { Console.WriteLine($"E: reading VCP control #{code} (0x{code:X2}) failed"); status = 1; continue; }
                    target = value.EndsWith('+') ? Math.Min(max, cur + delta) : cur > delta ? cur - delta : 0;
                }
                else if (!uint.TryParse(value, out target)) throw new UsageException($"bad value '{value}'");
                Console.WriteLine($"D: setting VCP control #{code} => {target} ({label})");
                try { Set(m, code, target); }
                catch (InvalidOperationException e) { Console.WriteLine($"E: {e.Message}"); status = 1; }
            }
            return status;
        }
        finally { Release(monitors); }
    }

    // ---- m1ddc ----------------------------------------------------------------------------------

    static readonly Dictionary<string, byte> M1Attributes = new()
    {
        ["luminance"] = 0x10, ["brightness"] = 0x10, ["contrast"] = 0x12, ["volume"] = 0x62, ["mute"] = 0x8d,
        ["input"] = 0x60, ["input-alt"] = 0xf4, ["red"] = 0x16, ["green"] = 0x18, ["blue"] = 0x1a, ["standby"] = 0xd6, ["power"] = 0xd6,
        ["pbp"] = 0xe9, ["pbp-input"] = 0xe8,
    };

    static int RunM1ddc(string[] args)
    {
        if (args.Length == 0 || args[0] is "help" or "-h" or "--help")
        {
            Console.WriteLine("""
                Controls volume, luminance (brightness), contrast, color gain, input selection of external displays using DDC/CI.

                Usage examples:

                  m1ddc set contrast 5           - Sets contrast to 5 on default display
                  m1ddc get luminance            - Returns current luminance on default display
                  m1ddc set red 90               - Sets red gain to 90
                  m1ddc chg volume -10           - Decreases volume by 10
                  m1ddc max luminance            - Returns the maximum luminance
                  m1ddc display list             - Lists displays
                  m1ddc display 1 set volume 50  - Sets volume to 50 on display 1
                  m1ddc display <id> get input   - Uses the display with the persistent id from display list

                Commands: set, get, max, chg (change by), display list [detailed]
                Attributes: luminance, contrast, volume, mute (on/off), input, input-alt, red, green, blue, standby (on/off)
                """);
            return 1;
        }
        var rest = args.ToList();
        string selector = null;
        if (rest[0] == "display")
        {
            if (rest.Count < 2) throw new UsageException("display needs 'list' or a display number/id");
            if (rest[1] == "list") return M1List(rest.Count > 2 && rest[2] == "detailed");
            selector = rest[1];
            rest = rest.Skip(2).ToList();
        }
        if (rest.Count < 2) throw new UsageException("expected <set|get|max|chg> <attribute> [value] (see m1ddc help)");
        var command = rest[0];
        if (command is not ("set" or "get" or "max" or "chg")) throw new UsageException($"unknown command '{command}'");
        if (!M1Attributes.TryGetValue(rest[1], out var code)) throw new UsageException($"unknown attribute '{rest[1]}'");
        if (command is "set" or "chg" && rest.Count < 3) throw new UsageException($"{command} {rest[1]} needs a value");

        var monitors = Monitors();
        try
        {
            var external = monitors.Where(m => !m.Screen.BuiltIn).ToList();
            Monitor target = null;
            if (selector == null) target = external.FirstOrDefault();
            else if (int.TryParse(selector, out var n)) target = n >= 1 && n <= external.Count ? external[n - 1] : null;
            else target = external.FirstOrDefault(m => m.Screen.PersistentId.Equals(selector, StringComparison.OrdinalIgnoreCase)
                                                       || m.Name.Contains(selector, StringComparison.OrdinalIgnoreCase));
            if (target == null)
            {
                Console.WriteLine("Could not find a suitable external display.");
                return 1;
            }
            switch (command)
            {
                case "get" or "max":
                    if (!TryGet(target, code, out var cur, out var max)) { Console.Error.WriteLine("DDC communication failure."); return 1; }
                    var shown = command == "max" ? max : cur;
                    Console.WriteLine(code == 0x8d ? (shown == 1 ? "on" : "off") : code == 0xd6 ? (shown == 1 ? "off" : "on") : shown.ToString());
                    return 0;
                case "set":
                    uint value = rest[2] switch
                    {
                        "on" => code == 0x8d ? 1u : code == 0xd6 ? 4u : 1u,
                        "off" => code == 0x8d ? 2u : code == 0xd6 ? 1u : 0u,
                        var v => uint.TryParse(v, out var parsed) ? parsed : throw new UsageException($"bad value '{v}'"),
                    };
                    Set(target, code, value);
                    Console.WriteLine(value);
                    return 0;
                default:
                    if (!int.TryParse(rest[2], out var delta)) throw new UsageException($"bad value '{rest[2]}'");
                    if (!TryGet(target, code, out var now, out var limit)) { Console.Error.WriteLine("DDC communication failure."); return 1; }
                    var next = (uint)Math.Clamp((long)now + delta, 0, limit);
                    Set(target, code, next);
                    Console.WriteLine(next);
                    return 0;
            }
        }
        finally { Release(monitors); }
    }

    static int M1List(bool detailed)
    {
        var monitors = Monitors();
        try
        {
            var external = monitors.Where(m => !m.Screen.BuiltIn).ToList();
            for (var i = 0; i < external.Count; i++)
            {
                var m = external[i];
                Console.WriteLine($"[{i + 1}] {m.Name} ({m.Screen.PersistentId})");
                if (!detailed) continue;
                Console.WriteLine($" - Windows device: {m.Screen.GdiName}");
                Console.WriteLine($" - Resolution: {m.Screen.Mode.dmPelsWidth}x{m.Screen.Mode.dmPelsHeight}@{m.Screen.Mode.dmDisplayFrequency}");
                Console.WriteLine($" - DDC/CI: {(TryGet(m, 0x10, out _, out _) ? "yes" : "no response")}");
            }
            if (external.Count == 0) Console.WriteLine("No external displays found.");
            return 0;
        }
        finally { Release(monitors); }
    }

    // ---- dxva2 ----------------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct PHYSICAL_MONITOR
    {
        public IntPtr hPhysicalMonitor;
        public fixed char szPhysicalMonitorDescription[128];
    }

    [DllImport("dxva2.dll", SetLastError = true)]
    static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hmon, out uint count);

    [DllImport("dxva2.dll", SetLastError = true)]
    static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hmon, uint count, PHYSICAL_MONITOR* monitors);

    [DllImport("dxva2.dll")]
    static extern bool DestroyPhysicalMonitor(IntPtr monitor);

    [DllImport("dxva2.dll", SetLastError = true)]
    static extern bool GetVCPFeatureAndVCPFeatureReply(IntPtr monitor, byte code, out uint type, out uint current, out uint max);

    [DllImport("dxva2.dll", SetLastError = true)]
    static extern bool SetVCPFeature(IntPtr monitor, byte code, uint value);

    [DllImport("dxva2.dll", SetLastError = true)]
    static extern bool GetCapabilitiesStringLength(IntPtr monitor, out uint length);

    [DllImport("dxva2.dll", SetLastError = true)]
    static extern bool CapabilitiesRequestAndCapabilitiesReply(IntPtr monitor, byte* buffer, uint length);
}
