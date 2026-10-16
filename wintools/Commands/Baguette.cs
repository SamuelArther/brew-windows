using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WinTools;

// baguette: headless iOS Simulator control. Its command line (subcommands, help, usage errors with
// exit 64) is reproduced, and `list` reads CoreSimulator device sets (each device's device.plist),
// which works for a device set copied from a Mac via --device-set. Booting simulators, injecting
// input, streaming and everything else that drives a live simulator needs CoreSimulator, which is
// part of Xcode on macOS; those subcommands say so and exit 1.
static class Baguette
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("baguette", "Headless iOS Simulator manager and input injection (simulator part of Xcode)", Run);
    }

    // Root subcommands with their abstracts (and nested subcommands), in baguette's order.
    static readonly (string Name, string Abstract, (string Name, string Abstract)[] Sub)[] Subcommands =
    [
        ("list", "List simulators in the default or a custom device set", []),
        ("boot", "Boot a simulator headlessly", []),
        ("shutdown", "Shut down a booted simulator", []),
        ("lifetime", "Show or change whether Simulator.app leaves devices booted", []),
        ("input", "Read newline-delimited JSON gestures from stdin, ack each on stdout", []),
        ("stream", "Stream framebuffer to stdout (mjpeg / avcc). Reads runtime config commands from stdin.", []),
        ("tap", "Single tap at a point", []),
        ("double-tap", "Two taps at one point — fires UITapGestureRecognizer / TapGesture(count: 2)", []),
        ("swipe", "One-finger drag from start to end", []),
        ("pinch", "Two-finger pinch / spread around a centre point", []),
        ("pan", "Two-finger parallel drag", []),
        ("press", "Press-and-release a hardware button (home, lock, volume-up, volume-down, action, siri)", []),
        ("key", "Press a single keyboard key with optional modifiers", []),
        ("type", "Type a string of US-ASCII text into the focused field", []),
        ("paste", "Paste text into the focused field via the simulator's pasteboard", []),
        ("clipboard", "Read the simulator's pasteboard, or sync it to/from the host",
            [("get", "Print the simulator's pasteboard text to stdout"), ("sync", "Copy the host's clipboard onto the simulator"),
             ("copy", "Copy the simulator's pasteboard onto the host clipboard")]),
        ("chrome", "Read DeviceKit chrome (bezel) data for a simulator",
            [("layout", "Print the chrome layout JSON for the named device"), ("composite", "Write the rasterized composite bezel PNG to stdout")]),
        ("screenshot", "Capture one frame from a simulator's screen", []),
        ("record", "Record a simulator's screen to an H.264 video file", []),
        ("render-3d", "Render a simulator screen on an installed 3D device model", []),
        ("describe-ui", "Dump the simulator's accessibility tree as JSON", []),
        ("logs", "Stream the booted simulator's unified log to stdout", []),
        ("serve", "Start the standalone simulator UI server", []),
        ("orientation", "Set the booted simulator's orientation", []),
        ("hinge", "Read or move iPhone Duo's hinge", []),
        ("shake", "Shake the booted simulator (fires UIKit motionShake)", []),
        ("status-bar", "Override or clear the booted simulator's status bar",
            [("override", "Set one or more status-bar overrides"), ("clear", "Clear every status-bar override")]),
        ("interface", "Read or set appearance, contrast and text size",
            [("appearance", "Read or set the light / dark appearance style"), ("contrast", "Read or set Increase Contrast"),
             ("text-size", "Read or set the content size category (Dynamic Type)")]),
        ("location", "Set, route, walk, or clear the booted simulator's simulated GPS location",
            [("set", "Pin the simulator to a lat,lon position"), ("start", "Run a moving route between two or more lat,lon waypoints"),
             ("walk", "Walk from a lat,lon position along a compass bearing"), ("clear", "Clear the simulated location, restoring live values")]),
        ("motion", "Drive CoreMotion — activity, pedometer, and device motion",
            [("start", "Arm motion and report an activity"), ("set", "Change the reported activity without re-arming"),
             ("stop", "Park the device as stationary and stop injecting motion")]),
        ("network", "Condition the network — latency, bandwidth, loss, offline",
            [("set", "Condition the network"), ("clear", "Stop conditioning"), ("status", "Report the condition currently applied")]),
        ("install", "Install an app (.ipa/.app) onto the booted simulator", []),
        ("add-media", "Add a photo or video to the booted simulator's Photos", []),
        ("openurl", "Open a deep link on the booted simulator", []),
        ("schemes", "List URL schemes the booted simulator's apps answer to", []),
        ("plugin", "Inspect and run installed plugins",
            [("list", "List installed plugins"), ("show", "Print what a plugin contributes, without running it"),
             ("validate", "Check a plugin directory's manifest and say what's wrong"), ("run", "Run a plugin command by its namespaced id"),
             ("install", "Install a plugin"), ("remove", "Uninstall a plugin"), ("update", "Re-install plugins from their bakery at its latest commit")]),
        ("bakery", "Add and manage trusted sources of plugins",
            [("add", "Trust a bakery and record its menu"), ("list", "List trusted bakeries"), ("remove", "Forget a bakery"),
             ("outdated", "Check trusted bakeries for newer commits"), ("update", "Re-pull bakeries and re-pin their commits")]),
        ("diag-digitizer-trackpad", "Probe whether the simulator's HID bridge accepts digitizer-finger events", []),
        ("heal", "Reclaim the input surface after Xcode's Device Hub attaches (restarts SpringBoard)", []),
        ("defaults", "Read or edit an app's UserDefaults, typed, including App Group suites",
            [("read", "Print the app's persisted defaults"), ("write", "Store a typed value under a key"),
             ("delete", "Remove a key"), ("export", "Print the suite's preferences as typed JSON"), ("import", "Write every value from a `defaults export` file")]),
    ];

    static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help") { RootHelp(); return 0; }
        if (args[0] == "--version") { Console.WriteLine(Commands.Version); return 0; }
        if (args[0] == "help")
        {
            if (args.Length == 1) { RootHelp(); return 0; }
            return SubHelp(args[1], args.Length > 2 ? args[2] : null) ? 0 : UsageError(args[1]);
        }
        var sub = Subcommands.FirstOrDefault(s => s.Name == args[0]);
        if (sub.Name == null) return UsageError(args[0]);
        var rest = args[1..];
        if (rest.Contains("-h") || rest.Contains("--help"))
        {
            SubHelp(sub.Name, rest.Length > 0 && !rest[0].StartsWith('-') ? rest[0] : null);
            return 0;
        }
        if (sub.Name == "list") return List(rest);
        Console.Error.WriteLine($"Error: baguette {sub.Name} needs a live iOS Simulator: {IosSim.NoSimulator}");
        return 1;
    }

    static void RootHelp()
    {
        var sb = new StringBuilder();
        sb.Append("OVERVIEW: Headless iOS simulator control\n\nUSAGE: baguette <subcommand>\n\nOPTIONS:\n");
        sb.Append("  --version               Show the version.\n  -h, --help              Show help information.\n\nSUBCOMMANDS:\n");
        foreach (var (name, abs, _) in Subcommands) sb.Append($"  {name,-24}{abs}\n");
        sb.Append("\n  See 'baguette help <subcommand>' for detailed help.\n\n");
        sb.Append($"  On Windows only `list` (for device sets copied from a Mac) works: {IosSim.NoSimulator}.");
        Console.WriteLine(sb.ToString());
    }

    static bool SubHelp(string name, string nested)
    {
        var sub = Subcommands.FirstOrDefault(s => s.Name == name);
        if (sub.Name == null) return false;
        var leaf = nested == null ? default : sub.Sub.FirstOrDefault(s => s.Name == nested);
        if (leaf.Name != null)
        {
            Console.WriteLine($"OVERVIEW: {leaf.Abstract}\n\nUSAGE: baguette {sub.Name} {leaf.Name} [<options>]\n\nOPTIONS:\n  -h, --help              Show help information.");
            return true;
        }
        var sb = new StringBuilder($"OVERVIEW: {sub.Abstract}\n\n");
        if (sub.Sub.Length > 0)
        {
            sb.Append($"USAGE: baguette {sub.Name} <subcommand>\n\nOPTIONS:\n  -h, --help              Show help information.\n\nSUBCOMMANDS:\n");
            foreach (var (n, a) in sub.Sub) sb.Append($"  {n,-24}{a}\n");
        }
        else if (sub.Name == "list")
            sb.Append("USAGE: baguette list [--device-set <device-set>] [--json]\n\nOPTIONS:\n" +
                      "  --device-set <device-set>\n                          Custom device set path\n" +
                      "  --json                  Emit one running/available JSON envelope (matches /simulators.json)\n" +
                      "  -h, --help              Show help information.\n");
        else
            sb.Append($"USAGE: baguette {sub.Name} [<options>]\n\nOPTIONS:\n  -h, --help              Show help information.\n");
        Console.Write(sb.ToString());
        return true;
    }

    // ArgumentParser's usage error: exit code 64 (EX_USAGE).
    static int UsageError(string arg)
    {
        Console.Error.WriteLine($"Error: Unexpected argument '{arg}'\nUsage: baguette <subcommand>\n  See 'baguette --help' for more information.");
        return 64;
    }

    record Sim(string Udid, string Name, string State, string Runtime, string DeviceType, string RuntimeId);

    static int List(string[] args)
    {
        string deviceSet = null;
        var json = false;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--device-set":
                    if (++i >= args.Length) { Console.Error.WriteLine("Error: Missing value for '--device-set <device-set>'"); return 64; }
                    deviceSet = args[i];
                    break;
                case "--json": json = true; break;
                default:
                    if (args[i].StartsWith("--device-set=")) { deviceSet = args[i]["--device-set=".Length..]; break; }
                    return UsageError(args[i]);
            }
        }
        // CoreSimulator's default set; on Windows it exists only if someone copied one there.
        deviceSet ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Developer", "CoreSimulator", "Devices");
        var sims = ReadDeviceSet(deviceSet);
        if (!json)
        {
            foreach (var s in sims)
                Console.WriteLine($"{{\"udid\":\"{s.Udid}\",\"name\":\"{Escape(s.Name)}\",\"state\":\"{s.State}\",\"runtime\":\"{Escape(s.Runtime)}\"}}");
            return 0;
        }
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream))
        {
            w.WriteStartObject();
            foreach (var (section, booted) in new[] { ("available", false), ("running", true) })
            {
                w.WriteStartArray(section);
                foreach (var s in sims.Where(x => (x.State == "Booted") == booted))
                {
                    // Keys sorted, like baguette's JSONSerialization(.sortedKeys).
                    w.WriteStartObject();
                    WriteNullable(w, "deviceTypeIdentifier", s.DeviceType);
                    w.WriteString("name", s.Name);
                    WriteNullable(w, "productFamily", ProductFamily(s.DeviceType));
                    w.WriteString("runtime", s.Runtime);
                    WriteNullable(w, "runtimeIdentifier", s.RuntimeId);
                    WriteNullable(w, "runtimeVersion", Regex.Match(s.Runtime, @"[\d.]+$").Value is { Length: > 0 } v ? v : null);
                    w.WriteString("state", s.State);
                    w.WriteString("udid", s.Udid);
                    w.WriteEndObject();
                }
                w.WriteEndArray();
            }
            w.WriteEndObject();
        }
        Console.WriteLine(Encoding.UTF8.GetString(stream.ToArray()));
        return 0;
    }

    static void WriteNullable(Utf8JsonWriter w, string key, string value)
    {
        if (value == null) w.WriteNull(key);
        else w.WriteString(key, value);
    }

    static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    static string ProductFamily(string deviceType) => deviceType switch
    {
        null => null,
        _ when deviceType.Contains("iPad") => "iPad",
        _ when deviceType.Contains("iPhone") => "iPhone",
        _ when deviceType.Contains("Watch") => "Apple Watch",
        _ when deviceType.Contains("Apple-TV") => "Apple TV",
        _ when deviceType.Contains("Vision") => "Apple Vision",
        _ => null,
    };

    // <set>/<UDID>/device.plist holds UDID, name, deviceType, runtime and state (CoreSimulator's
    // SimDeviceState: 0 creating, 1 shutdown, 2 booting, 3 booted, 4 shutting down).
    static List<Sim> ReadDeviceSet(string set)
    {
        var sims = new List<Sim>();
        if (!Directory.Exists(set)) return sims;
        foreach (var dir in Directory.GetDirectories(set))
        {
            var plistPath = Path.Combine(dir, "device.plist");
            if (!File.Exists(plistPath)) continue;
            Dictionary<string, object> plist;
            try { plist = IosSim.ReadPlist(File.ReadAllBytes(plistPath)) as Dictionary<string, object>; }
            catch (Exception e) when (e is InvalidOperationException or FormatException or System.Xml.XmlException or IndexOutOfRangeException) { continue; }
            if (plist == null) continue;
            var runtimeId = plist.GetValueOrDefault("runtime") as string;
            var state = plist.GetValueOrDefault("state") is long n ? n : 1;
            sims.Add(new Sim(
                plist.GetValueOrDefault("UDID") as string ?? Path.GetFileName(dir),
                plist.GetValueOrDefault("name") as string ?? "",
                state switch { 0 => "Creating", 2 => "Booting", 3 => "Booted", 4 => "ShuttingDown", _ => "Shutdown" },
                RuntimeName(runtimeId),
                plist.GetValueOrDefault("deviceType") as string,
                runtimeId));
        }
        return sims.OrderBy(s => s.Name, StringComparer.Ordinal).ToList();
    }

    // com.apple.CoreSimulator.SimRuntime.iOS-26-0 -> "iOS 26.0"
    static string RuntimeName(string id)
    {
        if (id == null) return "";
        var m = Regex.Match(id, @"SimRuntime\.([A-Za-z]+)-([\d-]+)$");
        return m.Success ? $"{m.Groups[1].Value} {m.Groups[2].Value.Replace('-', '.')}" : id;
    }
}
