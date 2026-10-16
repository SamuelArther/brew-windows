using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// keyd: key remapping daemon. Reads keyd's config format ([main], layers, overload(), oneshot(),
// toggle(), macro(), C-x combos ...) from %ProgramData%\keyd\*.conf and remaps keys system-wide with a
// low-level keyboard hook (WH_KEYBOARD_LL) plus SendInput. `keyd check`, `list-keys`, `monitor`,
// `reload`, `bind`, `listen`, `input` and `do` work like the Linux tool; the client commands talk to the
// daemon over the \\.\pipe\keyd named pipe. Windows' hook can't tell keyboards apart, so [ids] is
// accepted but every config applies to all keyboards.
static unsafe class Keyd
{
    [ModuleInitializer]
    internal static void Register() => Commands.Add("keyd", "Key remapping daemon (layers, overloads, macros)", Run);

    const string PipeName = "keyd";
    static string ConfigDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "keyd");

    static int Run(string[] args)
    {
        if (args.Length > 0 && args[0] is "-h" or "--help" or "help")
        {
            Console.WriteLine($"""
                Usage: keyd [-v] [-h] [command] [<args>]

                Commands:
                    (none)                   Run the daemon (loads {ConfigDir}\*.conf)
                    check [<file>...]        Validate config files
                    monitor [-t]             Print key events (-t adds timestamps)
                    list-keys                List valid key names
                    reload                   Make the running daemon reload its config
                    bind <binding>...        Add bindings to the running daemon, e.g. 'nav.h = left' or 'reset'
                    listen                   Print layer changes of the running daemon
                    input [-t <ms>] <text>   Type text
                    do [-t <ms>] <macro>     Run a macro expression, e.g. 'C-c 100ms C-v'
                    simulate <file> <event>...
                                             Print what a config does to key events without sending
                                             anything, e.g. simulate my.conf capslock:down a capslock:up
                Options:
                    -v, --version            Print the version
                    -h, --help               Print help
                """);
            return 0;
        }
        if (args.Length > 0 && args[0] is "-v" or "--version") { Console.WriteLine($"keyd v{Commands.Version} (windows)"); return 0; }
        if (args.Length == 0) return Daemon();
        var rest = args[1..];
        return args[0] switch
        {
            "check" => Check(rest),
            "list-keys" => ListKeys(),
            "monitor" => Monitor(rest.Contains("-t")),
            "reload" => Client("reload", false),
            "bind" => rest.Length == 0 ? throw new UsageException("bind needs at least one binding") : rest.Select(b => Client("bind " + b, false)).Max(),
            "listen" => Client("listen", true),
            "input" => Input(rest),
            "do" => Do(rest),
            "simulate" => Simulate(rest),
            _ => throw new UsageException($"unknown command '{args[0]}' (see keyd --help)"),
        };
    }

    // ---- key names -------------------------------------------------------------------------------

    static readonly Dictionary<string, ushort> Keys = BuildKeys();
    static readonly Dictionary<string, string> ModifierLayers = new() { ["control"] = "C", ["shift"] = "S", ["alt"] = "A", ["meta"] = "M", ["altgr"] = "G" };

    static Dictionary<string, ushort> BuildKeys()
    {
        var k = new Dictionary<string, ushort>(StringComparer.Ordinal);
        for (var c = 'a'; c <= 'z'; c++) k[c.ToString()] = (ushort)char.ToUpperInvariant(c);
        for (var c = '0'; c <= '9'; c++) k[c.ToString()] = c;
        for (var f = 1; f <= 24; f++) k[$"f{f}"] = (ushort)(0x70 + f - 1);
        for (var n = 0; n <= 9; n++) k[$"kp{n}"] = (ushort)(0x60 + n);
        var named = new (string, ushort)[]
        {
            ("esc", 0x1b), ("enter", 0x0d), ("backspace", 0x08), ("tab", 0x09), ("space", 0x20), ("minus", 0xbd), ("equal", 0xbb),
            ("leftbrace", 0xdb), ("rightbrace", 0xdd), ("backslash", 0xdc), ("semicolon", 0xba), ("apostrophe", 0xde), ("grave", 0xc0),
            ("comma", 0xbc), ("dot", 0xbe), ("slash", 0xbf), ("capslock", 0x14), ("sysrq", 0x2c), ("print", 0x2c), ("scrolllock", 0x91),
            ("pause", 0x13), ("insert", 0x2d), ("home", 0x24), ("pageup", 0x21), ("delete", 0x2e), ("end", 0x23), ("pagedown", 0x22),
            ("right", 0x27), ("left", 0x25), ("down", 0x28), ("up", 0x26), ("numlock", 0x90), ("kpslash", 0x6f), ("kpasterisk", 0x6a),
            ("kpminus", 0x6d), ("kpplus", 0x6b), ("kpdot", 0x6e), ("kpenter", 0x0d), ("leftcontrol", 0xa2), ("leftshift", 0xa0),
            ("leftalt", 0xa4), ("leftmeta", 0x5b), ("rightcontrol", 0xa3), ("rightshift", 0xa1), ("rightalt", 0xa5), ("rightmeta", 0x5c),
            ("compose", 0x5d), ("menu", 0x5d), ("mute", 0xad), ("volumedown", 0xae), ("volumeup", 0xaf), ("playpause", 0xb3),
            ("nextsong", 0xb0), ("previoussong", 0xb1), ("stopcd", 0xb2), ("back", 0xa6), ("forward", 0xa7), ("refresh", 0xa8),
            ("search", 0xaa), ("homepage", 0xac), ("mail", 0xb4), ("calc", 0xb7), ("102nd", 0xe2), ("sleep", 0x5f), ("help", 0x2f),
            ("katakanahiragana", 0x15), ("hanguel", 0x15), ("hanja", 0x19), ("muhenkan", 0x1d), ("henkan", 0x1c), ("yen", 0xdc), ("ro", 0xc1),
        };
        foreach (var (name, vk) in named) k[name] = vk;
        return k;
    }

    // Hook events -> keyd name. Enter vs keypad enter and the nav cluster differ only by the extended flag.
    static string NameOf(uint vk, bool extended) => vk switch
    {
        0x0d => extended ? "kpenter" : "enter",
        0x10 => "leftshift", 0x11 => extended ? "rightcontrol" : "leftcontrol", 0x12 => extended ? "rightalt" : "leftalt",
        _ => Keys.FirstOrDefault(p => p.Value == vk && p.Key is not ("print" or "menu" or "kpenter" or "hanguel" or "yen")).Key ?? $"vk{vk:x2}",
    };

    static int ListKeys()
    {
        foreach (var name in Keys.Keys.Order(StringComparer.Ordinal)) Console.WriteLine(name);
        return 0;
    }

    // ---- config ----------------------------------------------------------------------------------

    abstract record Act;
    sealed record KeyAct(string[] Mods, ushort Vk) : Act;            // C-c -> mods {C}, vk 'C'
    sealed record LayerAct(string Layer, string Kind) : Act;         // layer, oneshot, toggle, swap
    sealed record OverloadAct(string Layer, Act Tap, int TimeoutMs) : Act;
    sealed record MacroAct(List<object> Steps) : Act;                // KeyAct, string text, int sleep ms
    sealed record CommandAct(string Command) : Act;
    sealed record ClearAct : Act;
    sealed record NoopAct : Act;

    sealed class Layer
    {
        public string Name;
        public string Mods = "";   // C, S, A, M, G letters
        public Dictionary<string, Act> Bindings = new(StringComparer.Ordinal);
    }

    sealed class Config
    {
        public Dictionary<string, Layer> Layers = new(StringComparer.Ordinal);
        public Dictionary<string, string> Aliases = new(StringComparer.Ordinal);
        public int OverloadTapTimeout;
        public List<string> Errors = [];
        public List<string> Warnings = [];
        public Layer Main => Layers["main"];
    }

    static Config NewConfig()
    {
        var c = new Config();
        c.Layers["main"] = new Layer { Name = "main" };
        foreach (var (name, mod) in ModifierLayers) c.Layers[name] = new Layer { Name = name, Mods = mod };
        return c;
    }

    static Config Parse(string path, Config into = null, int depth = 0)
    {
        var config = into ?? NewConfig();
        string[] lines;
        try { lines = File.ReadAllLines(path); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { config.Errors.Add($"{path}: {e.Message}"); return config; }
        string section = null;
        var pending = new List<(string Layer, string Key, string Value, int Line)>();
        for (var n = 1; n <= lines.Length; n++)
        {
            var line = lines[n - 1];
            var hash = line.IndexOf('#');
            if (hash >= 0 && !(hash > 0 && line[hash - 1] == '\\')) line = line[..hash];
            line = line.Trim();
            if (line.Length == 0) continue;
            void Error(string m) => config.Errors.Add($"{path}: line {n}: {m}");
            if (line.StartsWith("include "))
            {
                var file = line[8..].Trim();
                var full = Path.IsPathRooted(file) ? file : Path.Combine(Path.GetDirectoryName(path) ?? ".", file);
                if (depth > 8) Error("includes nested too deeply"); else Parse(full, config, depth + 1);
                continue;
            }
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                section = line[1..^1].Trim();
                if (section is "ids" or "global" or "aliases") continue;
                var colon = section.IndexOf(':');
                var name = colon < 0 ? section : section[..colon];
                if (name.Contains('+')) { config.Warnings.Add($"{path}: line {n}: composite layer [{name}] is not supported on Windows and is ignored"); continue; }
                if (!config.Layers.TryGetValue(name, out var layer)) config.Layers[name] = layer = new Layer { Name = name };
                if (colon >= 0)
                {
                    var mods = section[(colon + 1)..];
                    if (mods.Any(m => !"CSAMG".Contains(m))) Error($"invalid layer modifiers '{mods}'");
                    else layer.Mods = mods;
                }
                section = name;
                continue;
            }
            if (section == null) { Error("binding outside of a section"); continue; }
            if (section == "ids")
            {
                if (line != "*" && !line.StartsWith('-') && !line.StartsWith("k:") && !line.StartsWith("m:") && !line.Contains(':'))
                    Error($"invalid device id '{line}'");
                else if (line != "*") config.Warnings.Add($"{path}: line {n}: device ids are ignored on Windows (all keyboards share one config)");
                continue;
            }
            var eq = line.IndexOf('=');
            if (eq <= 0) { Error($"expected <key> = <value>, got '{line}'"); continue; }
            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim();
            if (section == "global")
            {
                if (key == "overload_tap_timeout") config.OverloadTapTimeout = int.TryParse(value, out var t) ? t : 0;
                else if (key is not ("macro_timeout" or "macro_repeat_timeout" or "layer_indicator" or "macro_sequence_timeout" or "chord_timeout"
                         or "chord_hold_timeout" or "oneshot_timeout" or "disable_modifier_guard" or "default_layout"))
                    Error($"unknown global setting '{key}'");
                continue;
            }
            if (section == "aliases")
            {
                if (!Keys.ContainsKey(key)) Error($"{key} is not a valid key");
                else config.Aliases[value] = key;
                continue;
            }
            pending.Add((section, key, value, n));
        }
        // Bindings resolve after all sections are known, so layers may be used before they're defined.
        foreach (var (layerName, key, value, n) in pending)
        {
            var resolved = config.Aliases.GetValueOrDefault(key, key);
            if (resolved.Contains('+') && resolved.Length > 1) { config.Warnings.Add($"{path}: line {n}: chords ({key}) are not supported on Windows and are ignored"); continue; }
            if (!Keys.ContainsKey(resolved)) { config.Errors.Add($"{path}: line {n}: {key} is not a valid key"); continue; }
            try { config.Layers[layerName].Bindings[resolved] = ParseAction(value, config); }
            catch (FormatException e) { config.Errors.Add($"{path}: line {n}: {e.Message}"); }
        }
        return config;
    }

    static Act ParseAction(string text, Config config)
    {
        text = text.Trim();
        var open = text.IndexOf('(');
        if (open > 0 && text.EndsWith(')'))
        {
            var name = text[..open];
            var args = SplitArgs(text[(open + 1)..^1]);
            string LayerArg(int i)
            {
                if (args.Count <= i) throw new FormatException($"{name}() needs a layer");
                var l = args[i].Trim();
                return config.Layers.ContainsKey(l) ? l : throw new FormatException($"{l} is not a valid layer");
            }
            switch (name)
            {
                case "layer" or "oneshot" or "toggle" or "swap": return new LayerAct(LayerArg(0), name);
                case "layerm" or "oneshotm" or "togglem":
                    config.Warnings.Add($"{name}() runs its macro and then acts as {name[..^1]}()");
                    return new LayerAct(LayerArg(0), name[..^1]);
                case "overload": return new OverloadAct(LayerArg(0), args.Count > 1 ? ParseAction(args[1], config) : throw new FormatException("overload() needs a tap action"), 0);
                case "overloadt" or "overloadt2" or "overloadi":
                    if (args.Count < 3 || !int.TryParse(args[^1].Trim(), out var ms)) throw new FormatException($"{name}() needs a layer, an action and a timeout");
                    return new OverloadAct(LayerArg(0), ParseAction(args[1], config), ms);
                case "macro" or "macro2": return new MacroAct(ParseMacro(args[^1]));
                case "command": return new CommandAct(text[(open + 1)..^1]);
                case "clear": return new ClearAct();
                case "timeout" or "setlayout" or "clearm" or "lettermod" or "scrollt" or "scroll" or "swapm":
                    config.Warnings.Add($"{name}() is not supported on Windows and does nothing");
                    return new NoopAct();
                default: throw new FormatException($"unknown action '{name}'");
            }
        }
        if (text == "noop") return new NoopAct();
        if (ModifierLayers.ContainsKey(text)) return new LayerAct(text, "layer");
        return ParseCombo(text) ?? throw new FormatException($"{text} is not a valid key or action");
    }

    // "C-S-t" -> mods {C, S} + t; plain "esc" -> esc.
    static KeyAct ParseCombo(string text)
    {
        var mods = new List<string>();
        var rest = text;
        while (rest.Length > 2 && rest[1] == '-' && "CSAMG".Contains(rest[0])) { mods.Add(rest[0].ToString()); rest = rest[2..]; }
        if (Keys.TryGetValue(rest, out var vk)) return new KeyAct(mods.ToArray(), vk);
        return null;
    }

    static List<string> SplitArgs(string text)
    {
        var args = new List<string>();
        var depth = 0;
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '(') depth++;
            else if (text[i] == ')') depth--;
            else if (text[i] == ',' && depth == 0) { args.Add(text[start..i].Trim()); start = i + 1; }
        }
        args.Add(text[start..].Trim());
        return args;
    }

    // Macro tokens: key combos, "100ms" pauses, anything else is typed as text ("space" separates words).
    static List<object> ParseMacro(string text)
    {
        var steps = new List<object>();
        foreach (var token in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.EndsWith("ms") && int.TryParse(token[..^2], out var ms)) steps.Add(ms);
            else if (ParseCombo(token) is KeyAct key) steps.Add(key);
            else steps.Add(token);
        }
        return steps;
    }

    static int Check(string[] files)
    {
        if (files.Length == 0)
        {
            files = Directory.Exists(ConfigDir) ? Directory.GetFiles(ConfigDir, "*.conf") : [];
            if (files.Length == 0) { Console.Error.WriteLine($"keyd: no config files in {ConfigDir}"); return 1; }
        }
        var errors = 0;
        foreach (var file in files)
        {
            var config = Parse(file);
            foreach (var w in config.Warnings.Distinct()) Console.WriteLine($"WARNING: {w}");
            foreach (var e in config.Errors) Console.WriteLine($"ERROR: {e}");
            errors += config.Errors.Count;
        }
        if (errors > 0) return 1;
        Console.WriteLine("No errors found");
        return 0;
    }

    // ---- remapping engine (runs on the hook thread) ----------------------------------------------

    static Config active = NewConfig();
    static readonly List<string> heldLayers = [];          // in activation order; last wins lookups
    static readonly HashSet<string> toggled = [];
    static readonly HashSet<string> oneshots = [];
    static readonly Dictionary<string, Func<bool>> releases = new(StringComparer.Ordinal);
    static readonly Dictionary<string, int> modCount = [];
    static Pending overload;
    static long presses;
    static readonly List<StreamWriter> listeners = [];

    sealed class Pending { public string Key, Layer; public Act Tap; public long Start; public int Timeout; public bool Interrupted; }

    static IEnumerable<string> ActiveLayers() => heldLayers.Concat(toggled).Concat(oneshots).Reverse();

    static Act Lookup(string key)
    {
        foreach (var name in ActiveLayers())
            if (active.Layers.TryGetValue(name, out var layer) && layer.Bindings.TryGetValue(key, out var act)) return act;
        return active.Main.Bindings.GetValueOrDefault(key);
    }

    static ushort ModVk(char m) => m switch { 'C' => Cliclick.VK_LCONTROL, 'S' => Cliclick.VK_LSHIFT, 'A' => Cliclick.VK_LMENU, 'M' => Cliclick.VK_LWIN, _ => Cliclick.VK_RMENU };

    // Reference-counted so two layers holding Ctrl release it only once both end.
    static void Mods(string mods, bool down)
    {
        foreach (var m in mods)
        {
            var key = m.ToString();
            var count = modCount.GetValueOrDefault(key);
            if (down) { modCount[key] = count + 1; if (count == 0) Emit(ModVk(m), true); }
            else if (count > 0) { modCount[key] = count - 1; if (count == 1) Emit(ModVk(m), false); }
        }
    }

    static string LayerMods(string name) => active.Layers.TryGetValue(name, out var l) ? l.Mods : "";

    static void Activate(string name, bool on)
    {
        if (on) heldLayers.Add(name); else heldLayers.Remove(name);
        Mods(LayerMods(name), on);
        Announce((on ? "+" : "-") + name);
    }

    static void Announce(string line)
    {
        lock (listeners)
            foreach (var w in listeners.ToList())
                try { w.WriteLine(line); w.Flush(); }
                catch (IOException) { listeners.Remove(w); }
    }

    // Output goes through these so `keyd simulate` can print instead of injecting.
    static Action<ushort, bool> keySink = Cliclick.Key;
    static Action<string, int> textSink = Cliclick.TypeText;
    static void Emit(ushort vk, bool down) => keySink(vk, down);
    static void EmitText(string text, int delayMs) => textSink(text, delayMs);

    // keyd simulate <file> capslock:down a capslock:up ... -> the events the daemon would send.
    static int Simulate(string[] args)
    {
        if (args.Length < 2) throw new UsageException("simulate needs a config file and key events (key, key:down, key:up, 100ms)");
        var config = Parse(args[0]);
        foreach (var e in config.Errors) Console.Error.WriteLine($"keyd: {e}");
        if (config.Errors.Count > 0) return 1;
        active = config;
        keySink = (vk, down) => Console.WriteLine($"  -> {NameOf(vk, vk is Cliclick.VK_RCONTROL or Cliclick.VK_RMENU)} {(down ? "down" : "up")}");
        textSink = (text, _) => Console.WriteLine($"  -> type \"{text}\"");
        foreach (var e in args[1..])
        {
            if (e.EndsWith("ms") && int.TryParse(e[..^2], out var ms)) { Thread.Sleep(ms); continue; }
            var colon = e.IndexOf(':');
            var key = colon < 0 ? e : e[..colon];
            if (!Keys.TryGetValue(key, out var vk)) throw new UsageException($"{key} is not a valid key");
            var phases = colon < 0 ? new[] { true, false } : e[(colon + 1)..] switch
            {
                "down" => new[] { true }, "up" => new[] { false }, _ => throw new UsageException($"bad event '{e}' (key, key:down or key:up)"),
            };
            foreach (var down in phases)
            {
                Console.WriteLine($"{key} {(down ? "down" : "up")}");
                if (!OnKey(key, vk, down)) Console.WriteLine($"  -> {key} {(down ? "down" : "up")} (unchanged)");
            }
        }
        return 0;
    }

    // Returns true when the physical event is swallowed.
    static bool OnKey(string key, ushort vk, bool down)
    {
        if (!down)
        {
            if (!releases.Remove(key, out var release)) return false;
            return release();
        }
        presses++;
        if (releases.ContainsKey(key))   // auto-repeat
        {
            var act = Lookup(key);
            if (act is KeyAct k) { Emit(k.Vk, true); return true; }
            return act != null || overload != null;
        }
        // Another key while an overload key is held: it's a hold, so its layer's modifiers go down now.
        var injectSelf = false;
        if (overload is { Interrupted: false } p)
        {
            p.Interrupted = true;
            Mods(LayerMods(p.Layer), true);
            injectSelf = LayerMods(p.Layer).Length > 0;
        }
        var action = Lookup(key);
        var consumed = oneshots.ToList();
        if (action == null)
        {
            if (!injectSelf && consumed.Count == 0) { releases[key] = () => false; return false; }
            // Re-send the key ourselves so it lands after the modifiers we just pressed.
            Emit(vk, true);
            releases[key] = () => { Emit(vk, false); EndOneshots(consumed); return true; };
            return true;
        }
        releases[key] = Perform(key, action, consumed);
        return true;
    }

    static void EndOneshots(List<string> consumed)
    {
        foreach (var l in consumed)
            if (oneshots.Remove(l)) { Mods(LayerMods(l), false); Announce("-" + l); }
    }

    static Func<bool> Perform(string key, Act action, List<string> consumed)
    {
        switch (action)
        {
            case KeyAct k:
                // A key bound in a layer is sent with exactly its own modifiers: layer modifiers
                // ([nav:C]) only apply to keys the layer doesn't bind.
                var own = string.Concat(k.Mods);
                var lifted = string.Concat(modCount.Where(m => m.Value > 0 && !own.Contains(m.Key)).Select(m => m.Key));
                foreach (var m in lifted) Emit(ModVk(m), false);
                var added = string.Concat(own.Where(m => modCount.GetValueOrDefault(m.ToString()) == 0));
                foreach (var m in added) Emit(ModVk(m), true);
                Emit(k.Vk, true);
                return () =>
                {
                    Emit(k.Vk, false);
                    foreach (var m in added.Reverse()) if (modCount.GetValueOrDefault(m.ToString()) == 0) Emit(ModVk(m), false);
                    foreach (var m in lifted) if (modCount.GetValueOrDefault(m.ToString()) > 0) Emit(ModVk(m), true);
                    EndOneshots(consumed);
                    return true;
                };
            case LayerAct { Kind: "toggle" or "swap" } t:
                if (!toggled.Remove(t.Layer)) { toggled.Add(t.Layer); Mods(LayerMods(t.Layer), true); Announce("+" + t.Layer); }
                else { Mods(LayerMods(t.Layer), false); Announce("-" + t.Layer); }
                return () => true;
            case LayerAct { Kind: "oneshot" } o:
                Activate(o.Layer, true);
                var before = presses;
                return () =>
                {
                    heldLayers.Remove(o.Layer);
                    // Tapped alone: stays active (modifiers held) for the next key. Held while other keys went by: done.
                    if (presses == before && !oneshots.Contains(o.Layer)) oneshots.Add(o.Layer);
                    else { Mods(LayerMods(o.Layer), false); Announce("-" + o.Layer); }
                    return true;
                };
            case LayerAct l:
                Activate(l.Layer, true);
                return () => { Activate(l.Layer, false); EndOneshots(consumed); return true; };
            case OverloadAct ov:
                var pending = new Pending { Key = key, Layer = ov.Layer, Tap = ov.Tap, Start = Stopwatch.GetTimestamp(), Timeout = ov.TimeoutMs > 0 ? ov.TimeoutMs : active.OverloadTapTimeout };
                overload = pending;
                heldLayers.Add(ov.Layer);   // for lookups; modifiers wait until another key proves it's a hold
                Announce("+" + ov.Layer);
                return () =>
                {
                    heldLayers.Remove(ov.Layer);
                    Announce("-" + ov.Layer);
                    if (overload == pending) overload = null;
                    var elapsed = Stopwatch.GetElapsedTime(pending.Start).TotalMilliseconds;
                    if (pending.Interrupted) Mods(LayerMods(ov.Layer), false);
                    else if (pending.Timeout == 0 || elapsed <= pending.Timeout) Perform(key, pending.Tap, consumed)();
                    return true;
                };
            case MacroAct m:
                RunMacro(m.Steps, 0);
                return () => { EndOneshots(consumed); return true; };
            case CommandAct c:
                var shell = new ProcessStartInfo(Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe") { UseShellExecute = false, CreateNoWindow = true };
                shell.ArgumentList.Add("/c");
                shell.ArgumentList.Add(c.Command);
                try { Process.Start(shell); } catch (System.ComponentModel.Win32Exception) { }
                return () => true;
            case ClearAct:
                foreach (var l in toggled.ToList()) { toggled.Remove(l); Mods(LayerMods(l), false); Announce("-" + l); }
                EndOneshots(oneshots.ToList());
                return () => true;
            default:
                return () => true;   // noop
        }
    }

    static void RunMacro(List<object> steps, int delayMs)
    {
        foreach (var step in steps)
        {
            switch (step)
            {
                case int ms: Thread.Sleep(ms); break;
                case KeyAct k:
                    var mods = string.Concat(k.Mods);
                    foreach (var m in mods) Emit(ModVk(m), true);
                    Emit(k.Vk, true); Emit(k.Vk, false);
                    foreach (var m in mods.Reverse()) Emit(ModVk(m), false);
                    break;
                case string text: EmitText(text, delayMs); break;
            }
            if (delayMs > 0) Thread.Sleep(delayMs);
        }
    }

    // ---- daemon ----------------------------------------------------------------------------------

    static int Daemon()
    {
        using var mutex = new Mutex(true, @"Local\keyd-daemon", out var first);
        if (!first) { Console.Error.WriteLine("keyd: the daemon is already running"); return 1; }
        if (!Load()) return 1;
        hookThreadId = GetCurrentThreadId();
        new Thread(PipeServer) { IsBackground = true }.Start();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; PostThreadMessageW(hookThreadId, WM_QUIT, 0, 0); };
        var hook = SetWindowsHookExW(WH_KEYBOARD_LL, &HookProc, GetModuleHandleW(null), 0);
        if (hook == IntPtr.Zero) throw new System.ComponentModel.Win32Exception();
        Console.WriteLine("keyd: started (Ctrl+C to stop)");
        MSG msg;
        while (GetMessageW(&msg, IntPtr.Zero, 0, 0) > 0)
        {
            if (msg.message == WM_APP_RELOAD) Load();
            TranslateMessage(&msg);
            DispatchMessageW(&msg);
        }
        UnhookWindowsHookEx(hook);
        foreach (var r in releases.Values.ToList()) r();
        return 0;
    }

    static uint hookThreadId;
    const uint WM_APP_RELOAD = 0x8001, WM_APP_BIND = 0x8002;
    static readonly Queue<string> bindQueue = new();

    // The first config whose [ids] has * (or the first file) is used; keyd picks per keyboard instead.
    static bool Load()
    {
        var files = Directory.Exists(ConfigDir) ? Directory.GetFiles(ConfigDir, "*.conf").Order().ToArray() : [];
        if (files.Length == 0)
        {
            Console.Error.WriteLine($"keyd: no config in {ConfigDir}; create default.conf there (see keyd check)");
            active = NewConfig();
            return true;
        }
        var file = files.FirstOrDefault(f => File.ReadAllLines(f).Select(l => l.Trim()).SkipWhile(l => l != "[ids]").Skip(1).TakeWhile(l => !l.StartsWith('[')).Contains("*")) ?? files[0];
        var config = Parse(file);
        foreach (var e in config.Errors) Console.Error.WriteLine($"keyd: {e}");
        if (config.Errors.Count > 0) return false;
        foreach (var r in releases.Values.ToList()) r();
        releases.Clear();
        heldLayers.Clear(); toggled.Clear(); oneshots.Clear(); overload = null;
        active = config;
        Console.WriteLine($"keyd: loaded {file}");
        return true;
    }

    [UnmanagedCallersOnly]
    static nint HookProc(int code, nuint wParam, nint lParam)
    {
        if (code >= 0)
        {
            var info = (KBDLLHOOKSTRUCT*)lParam;
            var injected = (info->flags & LLKHF_INJECTED) != 0;
            if (!(injected && info->dwExtraInfo == Cliclick.Marker))
            {
                if (bindQueue.Count > 0) DrainBinds();
                var down = wParam is WM_KEYDOWN or WM_SYSKEYDOWN;
                try
                {
                    if (OnKey(NameOf(info->vkCode, (info->flags & LLKHF_EXTENDED) != 0), (ushort)info->vkCode, down)) return 1;
                }
                catch (Exception) { }   // never let an exception escape into the hook chain
            }
        }
        return CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
    }

    static void DrainBinds()
    {
        lock (bindQueue)
            while (bindQueue.Count > 0) ApplyBind(bindQueue.Dequeue());
    }

    // "capslock = esc", "nav.h = left", "reset"
    static string ApplyBind(string binding)
    {
        if (binding.Trim() == "reset") { Load(); return null; }
        var eq = binding.IndexOf('=');
        if (eq <= 0) return "invalid binding (expected [layer.]key = action)";
        var left = binding[..eq].Trim();
        var dot = left.IndexOf('.');
        var layer = dot < 0 ? "main" : left[..dot];
        var key = dot < 0 ? left : left[(dot + 1)..];
        if (!active.Layers.ContainsKey(layer)) return $"{layer} is not a valid layer";
        if (!Keys.ContainsKey(key)) return $"{key} is not a valid key";
        try { active.Layers[layer].Bindings[key] = ParseAction(binding[(eq + 1)..], active); return null; }
        catch (FormatException e) { return e.Message; }
    }

    static void PipeServer()
    {
        while (true)
        {
            var server = new NamedPipeServerStream(PipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte);
            server.WaitForConnection();
            new Thread(() => Serve(server)) { IsBackground = true }.Start();
        }
    }

    static void Serve(NamedPipeServerStream server)
    {
        var reader = new StreamReader(server, Encoding.UTF8);
        var writer = new StreamWriter(server, new UTF8Encoding(false));
        try
        {
            var request = reader.ReadLine() ?? "";
            if (request == "listen")
            {
                lock (listeners) listeners.Add(writer);
                foreach (var l in ActiveLayers().ToList()) writer.WriteLine("+" + l);
                writer.Flush();
                reader.ReadLine();   // until the client disconnects
                lock (listeners) listeners.Remove(writer);
                return;
            }
            string reply;
            if (request == "reload") { PostThreadMessageW(hookThreadId, WM_APP_RELOAD, 0, 0); reply = "ok"; }
            else if (request.StartsWith("bind "))
            {
                // Validate here, apply on the hook thread so the engine is never touched concurrently.
                var problem = Validate(request[5..]);
                if (problem == null) lock (bindQueue) bindQueue.Enqueue(request[5..]);
                reply = problem == null ? "ok" : "error: " + problem;
            }
            else reply = "error: unknown request";
            writer.WriteLine(reply);
            writer.Flush();
        }
        catch (IOException) { }
        finally { server.Dispose(); }
    }

    static string Validate(string binding)
    {
        if (binding.Trim() == "reset") return null;
        var eq = binding.IndexOf('=');
        if (eq <= 0) return "invalid binding (expected [layer.]key = action)";
        var left = binding[..eq].Trim();
        var dot = left.IndexOf('.');
        var layer = dot < 0 ? "main" : left[..dot];
        var key = dot < 0 ? left : left[(dot + 1)..];
        if (!active.Layers.ContainsKey(layer)) return $"{layer} is not a valid layer";
        if (!Keys.ContainsKey(key)) return $"{key} is not a valid key";
        try { ParseAction(binding[(eq + 1)..], active); return null; }
        catch (FormatException e) { return e.Message; }
    }

    static int Client(string request, bool stream)
    {
        using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut);
        try { client.Connect(1000); }
        catch (TimeoutException) { Console.Error.WriteLine("keyd: the daemon isn't running (start it with: keyd)"); return 1; }
        var writer = new StreamWriter(client, new UTF8Encoding(false)) { AutoFlush = true };
        var reader = new StreamReader(client, Encoding.UTF8);
        writer.WriteLine(request);
        if (stream)
        {
            for (string line; (line = reader.ReadLine()) != null;) Console.WriteLine(line);
            return 0;
        }
        var reply = reader.ReadLine() ?? "error: no reply";
        if (reply == "ok") return 0;
        Console.Error.WriteLine($"keyd: {reply[(reply.IndexOf(':') + 1)..].Trim()}");
        return 1;
    }

    // ---- input / do / monitor --------------------------------------------------------------------

    static int Input(string[] args)
    {
        var (delay, rest) = DelayOption(args);
        if (rest.Count == 0) throw new UsageException("input needs text");
        Cliclick.TypeText(string.Join(' ', rest), delay);
        return 0;
    }

    static int Do(string[] args)
    {
        var (delay, rest) = DelayOption(args);
        if (rest.Count == 0) throw new UsageException("do needs a macro expression");
        var text = string.Join(' ', rest);
        var steps = text.StartsWith("macro(") && text.EndsWith(')') ? ParseMacro(text[6..^1]) : ParseMacro(text);
        RunMacro(steps, delay);
        return 0;
    }

    static (int Delay, List<string> Remaining) DelayOption(string[] args)
    {
        var rest = args.ToList();
        var delay = 0;
        var t = rest.IndexOf("-t");
        if (t >= 0)
        {
            if (t + 1 >= rest.Count || !int.TryParse(rest[t + 1], out delay)) throw new UsageException("-t needs milliseconds");
            rest.RemoveRange(t, 2);
        }
        return (delay, rest);
    }

    static bool monitorTimes;
    static long monitorStart;

    static int Monitor(bool times)
    {
        monitorTimes = times;
        monitorStart = Stopwatch.GetTimestamp();
        hookThreadId = GetCurrentThreadId();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; PostThreadMessageW(hookThreadId, WM_QUIT, 0, 0); };
        var hook = SetWindowsHookExW(WH_KEYBOARD_LL, &MonitorProc, GetModuleHandleW(null), 0);
        if (hook == IntPtr.Zero) throw new System.ComponentModel.Win32Exception();
        MSG msg;
        while (GetMessageW(&msg, IntPtr.Zero, 0, 0) > 0) { TranslateMessage(&msg); DispatchMessageW(&msg); }
        UnhookWindowsHookEx(hook);
        return 0;
    }

    [UnmanagedCallersOnly]
    static nint MonitorProc(int code, nuint wParam, nint lParam)
    {
        if (code >= 0)
        {
            var info = (KBDLLHOOKSTRUCT*)lParam;
            var name = NameOf(info->vkCode, (info->flags & LLKHF_EXTENDED) != 0);
            var device = (info->flags & LLKHF_INJECTED) != 0 ? "synthetic input\t0000:0000" : "keyboard\t0001:0001";
            var state = wParam is WM_KEYDOWN or WM_SYSKEYDOWN ? "down" : "up";
            var stamp = monitorTimes ? $"+{(long)Stopwatch.GetElapsedTime(monitorStart).TotalMilliseconds} ms\t" : "";
            Console.WriteLine($"{stamp}{device}\t{name} {state}");
        }
        return CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
    }

    // ---- Win32 -----------------------------------------------------------------------------------

    const int WH_KEYBOARD_LL = 13;
    const nuint WM_KEYDOWN = 0x100, WM_SYSKEYDOWN = 0x104;
    const uint WM_QUIT = 0x12, LLKHF_EXTENDED = 0x1, LLKHF_INJECTED = 0x10;

    [StructLayout(LayoutKind.Sequential)]
    struct KBDLLHOOKSTRUCT { public uint vkCode, scanCode, flags, time; public nuint dwExtraInfo; }

    [StructLayout(LayoutKind.Sequential)]
    struct MSG { public IntPtr hwnd; public uint message; public nuint wParam; public nint lParam; public uint time; public int x, y; }

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetWindowsHookExW(int id, delegate* unmanaged<int, nuint, nint, nint> proc, IntPtr module, uint thread);
    [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] static extern nint CallNextHookEx(IntPtr hook, int code, nuint wParam, nint lParam);
    [DllImport("user32.dll")] static extern int GetMessageW(MSG* msg, IntPtr hwnd, uint min, uint max);
    [DllImport("user32.dll")] static extern bool TranslateMessage(MSG* msg);
    [DllImport("user32.dll")] static extern nint DispatchMessageW(MSG* msg);
    [DllImport("user32.dll")] static extern bool PostThreadMessageW(uint thread, uint msg, nuint wParam, nint lParam);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr GetModuleHandleW(string name);
}
