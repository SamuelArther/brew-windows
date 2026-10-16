using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Text.Json;

namespace WinTools;

// pipewire and wireplumber on the Windows audio graph. PipeWire's objects map onto Core Audio:
// the core is the Windows Audio service, devices are audio adapters, nodes are the endpoints
// (render endpoints are Audio/Sink, capture endpoints Audio/Source), and the "default" metadata is
// Windows' default endpoints. pw-cli/pw-dump list them, pw-cat/pw-play/pw-record stream WAV files
// through WASAPI, pw-metadata reads and sets the defaults, and wpctl is WirePlumber's control tool.
// Object ids are numbered in a stable order (core 0, adapters from 30, then endpoints).
static unsafe partial class PipeWire
{
    const string Version = "1.6.9";
    const string WirePlumberVersion = "0.5.18";

    [ModuleInitializer]
    internal static void Register()
    {
        Add("pipewire", "Report or start the Windows audio server", RunDaemon);
        Add("pw-cli", "Inspect the audio graph (PipeWire objects on Core Audio)", RunCli);
        Add("pw-dump", "Dump the audio graph as PipeWire JSON", RunDump);
        Add("pw-cat", "Play or record WAV files through WASAPI", args => RunCat(args, null));
        Add("pw-play", "Play a WAV file through WASAPI", args => RunCat(args, true));
        Add("pw-record", "Record a WAV file through WASAPI", args => RunCat(args, false));
        Add("pw-link", "List the audio graph's ports and links", RunLink);
        Add("pw-metadata", "Show or set the default audio endpoints", RunMetadata);
        Add("wireplumber", "Report the Windows audio session manager", RunWirePlumber);
        Add("wpctl", "WirePlumber control: devices, volumes, mute and defaults", RunWpctl);
    }

    // Core Audio failures (a device unplugged mid-command, access denied) end as "name: message", exit 1.
    static void Add(string name, string description, Func<string[], int> run) =>
        Commands.Add(name, description, args =>
        {
            try { return run(args); }
            catch (COMException e)
            {
                Console.Error.WriteLine($"{name}: {e.Message.Trim()}");
                return 1;
            }
        });

    // ---- the graph ----

    sealed class Node
    {
        public int Id, DeviceId;
        public string EndpointId, Description, Nick, AdapterName;
        public bool Sink, IsDefault;
        public uint Rate, Channels;
    }

    sealed class Graph
    {
        public List<(int Id, string Name)> Devices = new();
        public List<Node> Nodes = new();
        public IMMDeviceEnumerator Enumerator;
    }

    // With the audio service stopped or absent (servers, CI machines) the graph is just the core.
    static Graph Load()
    {
        try { return LoadEndpoints(); }
        catch (COMException e)
        {
            Console.Error.WriteLine($"warning: no audio endpoints available ({e.Message.Trim()})");
            return new Graph();
        }
    }

    static Graph LoadEndpoints()
    {
        var g = new Graph { Enumerator = Enumerator() };
        var found = new List<Node>();
        foreach (var (flow, sink) in new[] { (eRender, true), (eCapture, false) })
        {
            string defaultId = null;
            if (g.Enumerator.GetDefaultAudioEndpoint(flow, eConsole, out var def) == 0 && def != null) defaultId = def.GetId();
            var collection = g.Enumerator.EnumAudioEndpoints(flow, DEVICE_STATE_ACTIVE);
            for (uint i = 0, n = collection.GetCount(); i < n; i++)
            {
                var device = collection.Item(i);
                var node = new Node { EndpointId = device.GetId(), Sink = sink };
                var store = device.OpenPropertyStore(STGM_READ);
                node.Description = StringProperty(store, PkeyFriendlyName, 14) ?? node.EndpointId;
                node.Nick = StringProperty(store, PkeyFriendlyName, 2) ?? node.Description;
                node.AdapterName = StringProperty(store, PkeyInterfaceName, 2) ?? "Audio adapter";
                node.IsDefault = node.EndpointId == defaultId;
                if (Activate<IAudioClient>(device, IidAudioClient) is { } client && client.GetMixFormat(out var fmt) == 0 && fmt != null)
                {
                    node.Rate = fmt->nSamplesPerSec;
                    node.Channels = fmt->nChannels;
                    Marshal.FreeCoTaskMem((IntPtr)fmt);
                }
                found.Add(node);
            }
        }
        found.Sort((a, b) => a.Sink != b.Sink ? (a.Sink ? -1 : 1) : string.CompareOrdinal(a.EndpointId, b.EndpointId));
        var id = 30;
        foreach (var adapter in found.Select(n => n.AdapterName).Distinct().Order(StringComparer.Ordinal))
            g.Devices.Add((id++, adapter));
        foreach (var node in found)
        {
            node.Id = id++;
            node.DeviceId = g.Devices.First(d => d.Name == node.AdapterName).Id;
            g.Nodes.Add(node);
        }
        return g;
    }

    static string MediaClass(Node n) => n.Sink ? "Audio/Sink" : "Audio/Source";

    static List<(string Key, string Value)> NodeProps(Node n) =>
    [
        ("object.serial", n.Id.ToString()),
        ("object.path", $"wasapi:{n.EndpointId}"),
        ("node.name", n.EndpointId),
        ("node.description", n.Description),
        ("node.nick", n.Nick),
        ("media.class", MediaClass(n)),
        ("device.id", n.DeviceId.ToString()),
        ("device.api", "wasapi"),
        ("audio.rate", n.Rate.ToString()),
        ("audio.channels", n.Channels.ToString()),
    ];

    // Picks a node by id, node.name (endpoint id) or description, or the @DEFAULT_...@ aliases.
    static Node Find(Graph g, string what)
    {
        switch (what)
        {
            case "@DEFAULT_AUDIO_SINK@" or "@DEFAULT_SINK@": return g.Nodes.FirstOrDefault(n => n.Sink && n.IsDefault);
            case "@DEFAULT_AUDIO_SOURCE@" or "@DEFAULT_SOURCE@": return g.Nodes.FirstOrDefault(n => !n.Sink && n.IsDefault);
        }
        if (int.TryParse(what, out var id)) return g.Nodes.FirstOrDefault(n => n.Id == id);
        return g.Nodes.FirstOrDefault(n => n.EndpointId == what)
            ?? g.Nodes.FirstOrDefault(n => string.Equals(n.Description, what, StringComparison.OrdinalIgnoreCase));
    }

    static string ChannelName(uint index, uint count) => count switch
    {
        1 => "MONO",
        2 => index == 0 ? "FL" : "FR",
        _ => index < 8 ? new[] { "FL", "FR", "FC", "LFE", "RL", "RR", "SL", "SR" }[index] : $"AUX{index}",
    };

    // ---- pipewire / wireplumber: the servers are Windows services ----

    static int RunDaemon(string[] args) => ServerStatus(args, "pipewire", Version, "Audiosrv", "Windows Audio");

    static int RunWirePlumber(string[] args) =>
        ServerStatus(args, "wireplumber", WirePlumberVersion, "AudioEndpointBuilder", "Windows Audio Endpoint Builder");

    static int ServerStatus(string[] args, string name, string version, string service, string display)
    {
        foreach (var a in args)
        {
            switch (a)
            {
                case "-h" or "--help":
                    Console.WriteLine($"{name} [options]\n  -h, --help     Show this help\n      --version  Show version\n\n" +
                                      $"On Windows the {(name == "pipewire" ? "audio server" : "session manager")} is the {display} service ({service});\n" +
                                      $"{name} reports it and starts it if it is stopped.");
                    return 0;
                case "--version":
                    Console.WriteLine(name == "pipewire"
                        ? $"pipewire\nCompiled with libpipewire {version}\nLinked with libpipewire {version}"
                        : $"wireplumber\nCompiled with libwireplumber {version}\nLinked with libwireplumber {version}");
                    return 0;
                case "-v" or "--verbose": break;
                default: throw new UsageException($"unknown option {a}");
            }
        }
        var state = ServiceState(service);
        if (state == null) { Console.Error.WriteLine($"{name}: the {display} service ({service}) is not installed"); return 1; }
        if (state != SERVICE_RUNNING)
        {
            Console.WriteLine($"{name}: starting the {display} service ({service})");
            if (!StartWindowsService(service)) { Console.Error.WriteLine($"{name}: could not start {service} (needs an administrator)"); return 1; }
            return 0;
        }
        Console.WriteLine($"{name}: the {display} service ({service}) is running");
        return 0;
    }

    // ---- pw-cli ----

    static int RunCli(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            Console.WriteLine("pw-cli [options] [command]\n  -h, --help     Show this help\n      --version  Show version\n\n" +
                              "Commands:\n  list-objects (ls) [type]   List objects, optionally only those of a type (Node, Device, Core)\n" +
                              "  info (i) <id>|all          Show an object's properties\n  help (h)                   Show this help");
            return 0;
        }
        if (args[0] == "--version") { Console.WriteLine($"pw-cli\nCompiled with libpipewire {Version}\nLinked with libpipewire {Version}"); return 0; }
        var g = Load();
        switch (args[0])
        {
            case "list-objects" or "ls":
            {
                var filter = args.Length > 1 ? args[1] : null;
                foreach (var (id, type, props) in Objects(g))
                {
                    if (filter != null && !type.Contains(filter, StringComparison.OrdinalIgnoreCase) && filter != id.ToString()) continue;
                    Console.WriteLine($"\tid {id}, type {type}");
                    foreach (var (k, v) in props) Console.WriteLine($" \t\t{k} = \"{v}\"");
                }
                return 0;
            }
            case "info" or "i":
            {
                if (args.Length < 2) throw new UsageException("info needs an object id or 'all'");
                var any = false;
                foreach (var (id, type, props) in Objects(g))
                {
                    if (args[1] != "all" && args[1] != id.ToString()) continue;
                    any = true;
                    Console.WriteLine($"\tid: {id}\n\tpermissions: r-x-\n\ttype: {type}\n*\tproperties:");
                    foreach (var (k, v) in props) Console.WriteLine($"*\t\t{k} = \"{v}\"");
                }
                if (!any) { Console.Error.WriteLine($"Error: \"info\": unknown global {args[1]}"); return 1; }
                return 0;
            }
            default:
                throw new UsageException($"unknown command '{args[0]}' (see pw-cli help)");
        }
    }

    static IEnumerable<(int Id, string Type, List<(string, string)> Props)> Objects(Graph g)
    {
        yield return (0, "PipeWire:Interface:Core/4", [("object.serial", "0"), ("core.name", "pipewire-0"), ("core.version", Version), ("server", "Windows Audio (Audiosrv)")]);
        foreach (var (id, name) in g.Devices)
            yield return (id, "PipeWire:Interface:Device/3", [("object.serial", id.ToString()), ("device.description", name), ("device.name", name), ("device.api", "wasapi"), ("media.class", "Audio/Device")]);
        foreach (var n in g.Nodes) yield return (n.Id, "PipeWire:Interface:Node/3", NodeProps(n));
    }

    // ---- pw-dump ----

    static int RunDump(string[] args)
    {
        string only = null;
        foreach (var a in args)
        {
            if (a is "-h" or "--help")
            {
                Console.WriteLine("pw-dump [options] [ID]\n  -h, --help     Show this help\n      --version  Show version\n  -N, --no-colors  (accepted)\n\nDump the audio graph as JSON.");
                return 0;
            }
            if (a == "--version") { Console.WriteLine($"pw-dump\nCompiled with libpipewire {Version}\nLinked with libpipewire {Version}"); return 0; }
            if (a is "-N" or "--no-colors" or "-m" or "--monitor") continue;
            if (a.StartsWith('-')) throw new UsageException($"unknown option {a}");
            only = a;
        }
        var g = Load();
        using var stdout = Console.OpenStandardOutput();
        using var w = new Utf8JsonWriter(stdout, new JsonWriterOptions { Indented = true });
        w.WriteStartArray();
        foreach (var (id, type, props) in Objects(g))
        {
            if (only != null && only != id.ToString()) continue;
            var slash = type.LastIndexOf('/');
            w.WriteStartObject();
            w.WriteNumber("id", id);
            w.WriteString("type", type[..slash]);
            w.WriteNumber("version", int.Parse(type[(slash + 1)..]));
            w.WriteStartArray("permissions");
            foreach (var p in new[] { "r", "x" }) w.WriteStringValue(p);
            w.WriteEndArray();
            w.WriteStartObject("info");
            if (g.Nodes.FirstOrDefault(n => n.Id == id) is { } node)
            {
                w.WriteNumber("max-input-ports", node.Sink ? (int)node.Channels : 0);
                w.WriteNumber("max-output-ports", node.Sink ? 0 : (int)node.Channels);
                w.WriteString("state", "running");
            }
            w.WriteStartObject("props");
            foreach (var (k, v) in props)
            {
                if (long.TryParse(v, out var num) && k != "node.name") w.WriteNumber(k, num);
                else w.WriteString(k, v);
            }
            w.WriteEndObject();
            w.WriteEndObject();
            w.WriteEndObject();
        }
        if (only == null)
        {
            // The "default" metadata object, as PipeWire publishes it.
            w.WriteStartObject();
            w.WriteNumber("id", 1);
            w.WriteString("type", "PipeWire:Interface:Metadata");
            w.WriteNumber("version", 3);
            w.WriteStartObject("props");
            w.WriteString("metadata.name", "default");
            w.WriteEndObject();
            w.WriteStartArray("metadata");
            foreach (var (key, node) in Defaults(g))
            {
                w.WriteStartObject();
                w.WriteNumber("subject", 0);
                w.WriteString("key", key);
                w.WriteString("type", "Spa:String:JSON");
                w.WriteStartObject("value");
                w.WriteString("name", node.EndpointId);
                w.WriteEndObject();
                w.WriteEndObject();
            }
            w.WriteEndArray();
            w.WriteEndObject();
        }
        w.WriteEndArray();
        w.Flush();
        stdout.WriteByte((byte)'\n');
        return 0;
    }

    static IEnumerable<(string Key, Node Node)> Defaults(Graph g)
    {
        foreach (var n in g.Nodes.Where(n => n.IsDefault))
        {
            var kind = n.Sink ? "sink" : "source";
            yield return ($"default.audio.{kind}", n);
            yield return ($"default.configured.audio.{kind}", n);
        }
    }

    // ---- pw-metadata ----

    static int RunMetadata(string[] args)
    {
        var positional = new List<string>();
        foreach (var a in args)
        {
            if (a is "-h" or "--help")
            {
                Console.WriteLine("pw-metadata [options] [ id [ key [ value [ type ] ] ] ]\n  -h, --help     Show this help\n      --version  Show version\n" +
                                  "  -n, --name     Metadata name (only \"default\" exists)\n\n" +
                                  "Keys: default.audio.sink, default.audio.source and their default.configured.* forms.\n" +
                                  "Setting one makes that endpoint Windows' default device.");
                return 0;
            }
            if (a == "--version") { Console.WriteLine($"pw-metadata\nCompiled with libpipewire {Version}\nLinked with libpipewire {Version}"); return 0; }
            if (a is "-n" or "--name" or "-m" or "--monitor" || a.StartsWith("--name=") || a == "default") continue;
            if (a.StartsWith('-') && a.Length > 1 && !int.TryParse(a, out _)) throw new UsageException($"unknown option {a}");
            positional.Add(a);
        }
        var g = Load();
        Console.WriteLine("Found \"default\" metadata 1");
        if (positional.Count <= 2)
        {
            foreach (var (key, node) in Defaults(g))
            {
                if (positional.Count == 2 && positional[1] != key) continue;
                Console.WriteLine($"update: id:0 key:'{key}' value:'{{\"name\":\"{node.EndpointId}\"}}' type:'Spa:String:JSON'");
            }
            return 0;
        }
        var target = positional[1];
        if (target is not ("default.audio.sink" or "default.audio.source" or "default.configured.audio.sink" or "default.configured.audio.source"))
        {
            Console.Error.WriteLine($"pw-metadata: '{target}' isn't a key Windows keeps (only the default audio sink and source)");
            return 1;
        }
        // The value is JSON {"name":"<node.name>"} or a bare node name/id.
        var value = positional[2];
        if (value.StartsWith('{'))
        {
            try
            {
                using var doc = JsonDocument.Parse(value);
                value = doc.RootElement.TryGetProperty("name", out var nm) ? nm.GetString() : value;
            }
            catch (JsonException) { throw new UsageException("value is not valid JSON"); }
        }
        var n = Find(g, value);
        if (n == null || n.Sink != target.EndsWith("sink")) { Console.Error.WriteLine($"pw-metadata: no {(target.EndsWith("sink") ? "sink" : "source")} named {value}"); return 1; }
        SetDefault(n);
        Console.WriteLine($"set property: id:0 key:{target} value:{{\"name\":\"{n.EndpointId}\"}} type:Spa:String:JSON");
        return 0;
    }

    static void SetDefault(Node n)
    {
        var policy = Create<IPolicyConfig>(new Guid("870af99c-171d-4f9e-af0d-e63df40c2bc9"), new Guid("f8679f50-850a-41cf-9c72-430f290290c8"));
        foreach (var role in new[] { eConsole, eMultimedia, eCommunications }) policy.SetDefaultEndpoint(n.EndpointId, role);
    }

    // ---- pw-link ----

    static int RunLink(string[] args)
    {
        bool outputs = false, inputs = false, links = false, ids = false;
        var positional = new List<string>();
        foreach (var a in args)
        {
            switch (a)
            {
                case "-h" or "--help":
                    Console.WriteLine("pw-link [options] [output] [input]\n  -h, --help     Show this help\n      --version  Show version\n" +
                                      "  -o, --output   List output ports\n  -i, --input    List input ports\n  -l, --links    List links\n  -I, --id       List IDs\n\n" +
                                      "Ports are the endpoints' channels. Windows routes every stream to an endpoint itself, so\nlinks can be listed but not created or removed.");
                    return 0;
                case "--version": Console.WriteLine($"pw-link\nCompiled with libpipewire {Version}\nLinked with libpipewire {Version}"); return 0;
                case "-o" or "--output": outputs = true; break;
                case "-i" or "--input": inputs = true; break;
                case "-l" or "--links": links = true; break;
                case "-I" or "--id": ids = true; break;
                case "-d" or "--disconnect" or "-L" or "--linger" or "-P" or "--passive": positional.Add(a); break;
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unknown option {a}");
                    positional.Add(a);
                    break;
            }
        }
        if (positional.Count > 0)
        {
            Console.Error.WriteLine("pw-link: Windows routes streams to endpoints itself; ports can't be linked by hand");
            return 1;
        }
        if (!outputs && !inputs && !links) throw new UsageException("give -o, -i or -l (see --help)");
        var g = Load();
        var portId = 1000;
        foreach (var n in g.Nodes)
        {
            for (uint c = 0; c < Math.Max(1u, n.Channels); c++)
            {
                var id = portId++;
                // A sink takes input (playback_*) and offers its monitor as output; a source only outputs.
                var ch = ChannelName(c, n.Channels);
                if (inputs && n.Sink) Console.WriteLine($"{(ids ? $"{id,4} " : "")}{n.Description}:playback_{ch}");
                if (outputs) Console.WriteLine($"{(ids ? $"{id,4} " : "")}{n.Description}:{(n.Sink ? "monitor" : "capture")}_{ch}");
            }
        }
        return 0;
    }

    // ---- wpctl ----

    static int RunWpctl(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            Console.WriteLine("Usage:\n  wpctl [COMMAND] [COMMAND_OPTION?]\n\nCommands:\n  status\n  get-volume ID\n  inspect ID\n" +
                              "  set-default ID\n  set-volume ID VOL[%][-/+] [-l LIMIT]\n  set-mute ID 1|0|toggle\n  settings\n  clear-default [ID]\n\n" +
                              "ID is an object id from 'wpctl status', or @DEFAULT_AUDIO_SINK@ / @DEFAULT_AUDIO_SOURCE@.");
            return 0;
        }
        if (args[0] == "--version") { Console.WriteLine($"wpctl\nCompiled with libwireplumber {WirePlumberVersion}\nLinked with libwireplumber {WirePlumberVersion}"); return 0; }
        var g = Load();
        Node Target(int index)
        {
            if (args.Length <= index) throw new UsageException($"{args[0]} needs an ID");
            return Find(g, args[index]) ?? throw new InvalidOperationException($"Object '{args[index]}' not found");
        }
        switch (args[0])
        {
            case "status": return WpStatus(g);
            case "get-volume":
            {
                var n = Target(1);
                var vol = Volume(g, n);
                Console.WriteLine($"Volume: {vol.GetMasterVolumeLevelScalar():0.00}{(vol.GetMute() != 0 ? " [MUTED]" : "")}");
                return 0;
            }
            case "set-volume":
            {
                var n = Target(1);
                if (args.Length < 3) throw new UsageException("set-volume needs a volume");
                var limit = 1.5;
                for (int i = 3; i < args.Length; i++)
                {
                    if (args[i] is "-l" or "--limit" && i + 1 < args.Length && double.TryParse(args[++i], System.Globalization.CultureInfo.InvariantCulture, out var l)) limit = l;
                    else throw new UsageException($"unexpected argument {args[i]}");
                }
                var vol = Volume(g, n);
                var target = ParseVolume(args[2], vol.GetMasterVolumeLevelScalar());
                // Windows endpoint volume tops out at 1.0 (no software boost).
                target = Math.Clamp(target, 0, Math.Min(limit, 1.0));
                var context = Guid.Empty;
                vol.SetMasterVolumeLevelScalar((float)target, &context);
                return 0;
            }
            case "set-mute":
            {
                var n = Target(1);
                if (args.Length < 3) throw new UsageException("set-mute needs 1, 0 or toggle");
                var vol = Volume(g, n);
                var mute = args[2] switch
                {
                    "1" or "true" => true,
                    "0" or "false" => false,
                    "toggle" => vol.GetMute() == 0,
                    _ => throw new UsageException("set-mute needs 1, 0 or toggle"),
                };
                var context = Guid.Empty;
                vol.SetMute(mute ? 1 : 0, &context);
                return 0;
            }
            case "set-default":
                SetDefault(Target(1));
                return 0;
            case "clear-default":
                Console.Error.WriteLine("wpctl: Windows always has a default endpoint while one exists; choose another with set-default");
                return 1;
            case "inspect":
            {
                var n = Target(1);
                Console.WriteLine($"id {n.Id}, type PipeWire:Interface:Node");
                foreach (var (k, v) in NodeProps(n)) Console.WriteLine($"  {(k is "node.name" or "node.description" or "media.class" ? "*" : " ")} {k} = \"{v}\"");
                return 0;
            }
            case "settings":
                Console.WriteLine("Settings:");
                foreach (var (key, node) in Defaults(g)) Console.WriteLine($"  {key}: {node.EndpointId}");
                return 0;
            default:
                throw new UsageException($"unknown command '{args[0]}' (see wpctl --help)");
        }
    }

    static double ParseVolume(string text, double current)
    {
        var sign = text.EndsWith('+') ? 1 : text.EndsWith('-') ? -1 : 0;
        var body = sign != 0 ? text[..^1] : text;
        var percent = body.EndsWith('%');
        if (percent) body = body[..^1];
        if (!double.TryParse(body, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) || v < 0)
            throw new UsageException($"invalid volume '{text}'");
        if (percent) v /= 100;
        return sign == 0 ? v : current + sign * v;
    }

    static int WpStatus(Graph g)
    {
        var o = new StringBuilder();
        o.Append($"PipeWire 'pipewire-0' [{Version}, {Environment.UserName}@{Environment.MachineName}, Windows Audio]\n");
        o.Append(" └─ Clients:\n\n");
        o.Append("Audio\n ├─ Devices:\n");
        foreach (var (id, name) in g.Devices) o.Append($" │  {id,8}. {name,-40} [wasapi]\n");
        o.Append(" │  \n ├─ Sinks:\n");
        foreach (var n in g.Nodes.Where(n => n.Sink)) o.Append(StatusLine(g, n));
        o.Append(" │  \n ├─ Sources:\n");
        foreach (var n in g.Nodes.Where(n => !n.Sink)) o.Append(StatusLine(g, n));
        o.Append(" │  \n ├─ Filters:\n │  \n └─ Streams:\n\n");
        o.Append("Video\n ├─ Devices:\n │  \n ├─ Sinks:\n │  \n ├─ Sources:\n │  \n ├─ Filters:\n │  \n └─ Streams:\n\n");
        o.Append("Settings\n └─ Default Configured Devices:\n");
        var i = 0;
        foreach (var n in g.Nodes.Where(n => n.IsDefault)) o.Append($"     {i++,4}. {MediaClass(n),-13} {n.EndpointId}\n");
        // The tree lines are Unicode: UTF-8 into pipes and files, the console's own encoding otherwise.
        if (Console.IsOutputRedirected)
        {
            using var stdout = Console.OpenStandardOutput();
            stdout.Write(new UTF8Encoding(false).GetBytes(o.ToString()));
        }
        else Console.Write(o.ToString());
        return 0;
    }

    static string StatusLine(Graph g, Node n)
    {
        var vol = Volume(g, n);
        var level = $"[vol: {vol.GetMasterVolumeLevelScalar():0.00}{(vol.GetMute() != 0 ? " MUTED" : "")}]";
        return $" │  {(n.IsDefault ? "*" : " ")} {n.Id,5}. {n.Description,-40} {level}\n";
    }

    static IAudioEndpointVolume Volume(Graph g, Node n) =>
        Activate<IAudioEndpointVolume>(g.Enumerator.GetDevice(n.EndpointId), IidEndpointVolume)
        ?? throw new InvalidOperationException($"{n.Description} has no volume control");

    // ---- pw-cat / pw-play / pw-record ----

    sealed class CatOptions
    {
        public bool? Playback;
        public string Target, File;
        public uint Rate = 48000, Channels = 2;
        public string Format = "s16";
        public double Volume = 1.0;
        public bool Verbose;
    }

    static int RunCat(string[] args, bool? mode)
    {
        var name = mode == null ? "pw-cat" : mode == true ? "pw-play" : "pw-record";
        var o = new CatOptions { Playback = mode };
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Value() => ++i < args.Length ? args[i] : throw new UsageException($"{a} needs a value");
            string Inline(string prefix) => a[(prefix.Length + 1)..];
            switch (a)
            {
                case "-h" or "--help":
                    Console.WriteLine($"{name} [options] [<file>|-]\n  -h, --help                Show this help\n      --version             Show version\n  -v, --verbose             Enable verbose operations\n" +
                                      (mode == null ? "  -p, --playback            Playback mode\n  -r, --record              Recording mode\n" : "") +
                                      "      --target              Node target (id, node.name or description; default the default endpoint)\n" +
                                      "      --rate                Sample rate (record, default 48000)\n      --channels            Number of channels (record, default 2)\n" +
                                      "      --format              Sample format s16, s24, s32, f32 (record, default s16)\n      --volume              Stream volume 0-1.0 (default 1.000)\n\n" +
                                      "Files are WAV (PCM or float). Recording stops with Ctrl+C.");
                    return 0;
                case "--version": Console.WriteLine($"{name}\nCompiled with libpipewire {Version}\nLinked with libpipewire {Version}"); return 0;
                case "-p" or "--playback" when mode == null: o.Playback = true; break;
                case "-r" or "--record" when mode == null: o.Playback = false; break;
                case "-v" or "--verbose": o.Verbose = true; break;
                case "--target": o.Target = Value(); break;
                case "--rate": o.Rate = uint.TryParse(Value(), out var r) && r is >= 8000 and <= 384000 ? r : throw new UsageException("invalid rate"); break;
                case "--channels": o.Channels = uint.TryParse(Value(), out var c) && c is >= 1 and <= 8 ? c : throw new UsageException("invalid channels"); break;
                case "--format": o.Format = Value() is "s16" or "s24" or "s32" or "f32" ? args[i] : throw new UsageException("format must be s16, s24, s32 or f32"); break;
                case "--volume": o.Volume = double.TryParse(Value(), System.Globalization.CultureInfo.InvariantCulture, out var v) ? Math.Clamp(v, 0, 1) : throw new UsageException("invalid volume"); break;
                default:
                    if (a.StartsWith("--target=")) o.Target = Inline("--target");
                    else if (a.StartsWith("--rate=") && uint.TryParse(Inline("--rate"), out var r2)) o.Rate = r2;
                    else if (a.StartsWith("--channels=") && uint.TryParse(Inline("--channels"), out var c2)) o.Channels = c2;
                    else if (a.StartsWith("--format=")) o.Format = Inline("--format");
                    else if (a.StartsWith('-') && a != "-") throw new UsageException($"unknown option {a}");
                    else if (o.File == null) o.File = a;
                    else throw new UsageException("only one file may be given");
                    break;
            }
        }
        if (o.Playback == null) throw new UsageException("give -p (playback) or -r (record)");
        if (o.File == null) throw new UsageException("no file given");
        var g = Load();
        var node = o.Target != null ? Find(g, o.Target) : g.Nodes.FirstOrDefault(n => n.Sink == o.Playback && n.IsDefault);
        if (node == null || node.Sink != o.Playback)
        {
            Console.Error.WriteLine($"{name}: no {(o.Playback == true ? "playback" : "capture")} target{(o.Target != null ? $" '{o.Target}'" : " (no audio endpoint)")}");
            return 1;
        }
        var device = g.Enumerator.GetDevice(node.EndpointId);
        return o.Playback == true ? Play(device, node, o, name) : Record(device, node, o, name);
    }

    static int Play(IMMDevice device, Node node, CatOptions o, string name)
    {
        using var input = o.File == "-" ? Console.OpenStandardInput() : File.OpenRead(o.File);
        var (format, dataLength) = ReadWavHeader(input) ?? throw new InvalidOperationException($"{o.File}: not a WAV file");
        if (o.Verbose) Console.Error.WriteLine($"{name}: playing {o.File} ({format.nSamplesPerSec} Hz, {format.nChannels} ch, {format.wBitsPerSample} bit) on {node.Description}");
        var client = Activate<IAudioClient>(device, IidAudioClient) ?? throw new InvalidOperationException("could not open the endpoint");
        var fmt = format;
        Check(client.Initialize(0, AUDCLNT_STREAMFLAGS_EVENTCALLBACK | AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM | AUDCLNT_STREAMFLAGS_SRC_DEFAULT_QUALITY,
                                2_000_000, 0, &fmt, null), "initialize the stream");
        using var ready = new AutoResetEvent(false);
        Check(client.SetEventHandle(ready.SafeWaitHandle.DangerousGetHandle()), "set the event");
        Check(client.GetBufferSize(out var bufferFrames), "get the buffer size");
        var render = Service<IAudioRenderClient>(client, IidRenderClient);
        SetStreamVolume(client, o.Volume);
        var frameBytes = format.nBlockAlign;
        var chunk = new byte[bufferFrames * frameBytes];
        long remaining = dataLength;
        using var cancel = CancelOnCtrlC();
        Check(client.Start(), "start the stream");
        try
        {
            var done = false;
            while (!done && !cancel.IsCancellationRequested)
            {
                ready.WaitOne(500);
                Check(client.GetCurrentPadding(out var padding), "query the buffer");
                var frames = bufferFrames - padding;
                if (frames == 0) continue;
                var want = (int)Math.Min(frames * frameBytes, remaining);
                var got = ReadFully(input, chunk, want);
                remaining -= got;
                var whole = (uint)(got / frameBytes);
                if (whole == 0) { done = true; break; }
                Check(render.GetBuffer(whole, out var data), "get the buffer");
                Marshal.Copy(chunk, 0, (IntPtr)data, (int)(whole * frameBytes));
                Check(render.ReleaseBuffer(whole, 0), "release the buffer");
                if (got < want || remaining <= 0) done = true;
            }
            // Let the queued audio play out.
            while (!cancel.IsCancellationRequested && client.GetCurrentPadding(out var left) == 0 && left > 0) Thread.Sleep(10);
        }
        finally { client.Stop(); }
        return 0;
    }

    static int Record(IMMDevice device, Node node, CatOptions o, string name)
    {
        var bits = o.Format switch { "s24" => 24, "s32" or "f32" => 32, _ => 16 };
        var fmt = new WAVEFORMATEX
        {
            wFormatTag = (ushort)(o.Format == "f32" ? WAVE_FORMAT_IEEE_FLOAT : WAVE_FORMAT_PCM),
            nChannels = (ushort)o.Channels,
            nSamplesPerSec = o.Rate,
            wBitsPerSample = (ushort)bits,
            nBlockAlign = (ushort)(o.Channels * bits / 8),
        };
        fmt.nAvgBytesPerSec = fmt.nSamplesPerSec * fmt.nBlockAlign;
        var client = Activate<IAudioClient>(device, IidAudioClient) ?? throw new InvalidOperationException("could not open the endpoint");
        Check(client.Initialize(0, AUDCLNT_STREAMFLAGS_EVENTCALLBACK | AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM | AUDCLNT_STREAMFLAGS_SRC_DEFAULT_QUALITY,
                                2_000_000, 0, &fmt, null), "initialize the stream");
        using var ready = new AutoResetEvent(false);
        Check(client.SetEventHandle(ready.SafeWaitHandle.DangerousGetHandle()), "set the event");
        var capture = Service<IAudioCaptureClient>(client, IidCaptureClient);
        using var output = o.File == "-" ? Console.OpenStandardOutput() : File.Create(o.File);
        var seekable = output.CanSeek;
        WriteWavHeader(output, fmt, seekable ? 0 : uint.MaxValue - 36);
        if (o.Verbose) Console.Error.WriteLine($"{name}: recording {node.Description} to {o.File} ({o.Rate} Hz, {o.Channels} ch, {o.Format}); Ctrl+C stops");
        long written = 0;
        var buffer = Array.Empty<byte>();
        using var cancel = CancelOnCtrlC();
        Check(client.Start(), "start the stream");
        try
        {
            while (!cancel.IsCancellationRequested)
            {
                ready.WaitOne(200);
                while (capture.GetNextPacketSize(out var packet) == 0 && packet > 0)
                {
                    Check(capture.GetBuffer(out var data, out var frames, out var flags, out _, out _), "read the buffer");
                    var bytes = (int)(frames * fmt.nBlockAlign);
                    if (buffer.Length < bytes) buffer = new byte[bytes];
                    if ((flags & AUDCLNT_BUFFERFLAGS_SILENT) != 0) Array.Clear(buffer, 0, bytes);
                    else Marshal.Copy((IntPtr)data, buffer, 0, bytes);
                    capture.ReleaseBuffer(frames);
                    output.Write(buffer, 0, bytes);
                    written += bytes;
                }
            }
        }
        finally { client.Stop(); }
        if (seekable)
        {
            output.Position = 0;
            WriteWavHeader(output, fmt, (uint)Math.Min(written, uint.MaxValue - 36));
        }
        return 0;
    }

    static CancellationTokenSource CancelOnCtrlC()
    {
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
        return cts;
    }

    static int ReadFully(Stream s, byte[] buffer, int count)
    {
        var total = 0;
        while (total < count)
        {
            var n = s.Read(buffer, total, count - total);
            if (n == 0) break;
            total += n;
        }
        return total;
    }

    // RIFF/WAVE: the fmt chunk (PCM, IEEE float or WAVE_FORMAT_EXTENSIBLE) and the data length.
    static (WAVEFORMATEX, long)? ReadWavHeader(Stream s)
    {
        var head = new byte[12];
        if (ReadFully(s, head, 12) < 12 || Encoding.ASCII.GetString(head, 0, 4) != "RIFF" || Encoding.ASCII.GetString(head, 8, 4) != "WAVE") return null;
        WAVEFORMATEX? fmt = null;
        var chunk = new byte[8];
        while (ReadFully(s, chunk, 8) == 8)
        {
            var id = Encoding.ASCII.GetString(chunk, 0, 4);
            var size = BitConverter.ToUInt32(chunk, 4);
            if (id == "fmt ")
            {
                var body = new byte[size + (size & 1)];
                if (ReadFully(s, body, body.Length) < size || size < 16) return null;
                var tag = BitConverter.ToUInt16(body, 0);
                if (tag == 0xFFFE && size >= 40) tag = BitConverter.ToUInt16(body, 24);   // extensible: the subformat's tag
                if (tag is not (WAVE_FORMAT_PCM or WAVE_FORMAT_IEEE_FLOAT)) throw new InvalidOperationException("only PCM and float WAV files are supported");
                fmt = new WAVEFORMATEX
                {
                    wFormatTag = tag,
                    nChannels = BitConverter.ToUInt16(body, 2),
                    nSamplesPerSec = BitConverter.ToUInt32(body, 4),
                    nAvgBytesPerSec = BitConverter.ToUInt32(body, 8),
                    nBlockAlign = BitConverter.ToUInt16(body, 12),
                    wBitsPerSample = BitConverter.ToUInt16(body, 14),
                };
            }
            else if (id == "data")
            {
                return fmt is { } f ? (f, size == 0 || size == uint.MaxValue ? long.MaxValue : size) : null;
            }
            else
            {
                var skip = new byte[Math.Min(size + (size & 1), 1 << 20)];
                long left = size + (size & 1);
                while (left > 0) { var n = ReadFully(s, skip, (int)Math.Min(left, skip.Length)); if (n == 0) return null; left -= n; }
            }
        }
        return null;
    }

    static void WriteWavHeader(Stream s, WAVEFORMATEX f, uint dataBytes)
    {
        var w = new BinaryWriter(s, Encoding.ASCII, leaveOpen: true);
        w.Write("RIFF"u8);
        w.Write(36 + dataBytes);
        w.Write("WAVEfmt "u8);
        w.Write(16);
        w.Write(f.wFormatTag);
        w.Write(f.nChannels);
        w.Write(f.nSamplesPerSec);
        w.Write(f.nAvgBytesPerSec);
        w.Write(f.nBlockAlign);
        w.Write(f.wBitsPerSample);
        w.Write("data"u8);
        w.Write(dataBytes);
        w.Flush();
    }

    static void SetStreamVolume(IAudioClient client, double volume)
    {
        if (volume >= 1.0) return;
        var simple = Service<ISimpleAudioVolume>(client, IidSimpleVolume);
        var context = Guid.Empty;
        simple.SetMasterVolume((float)volume, &context);
    }

    static void Check(int hr, string what)
    {
        if (hr < 0) throw new InvalidOperationException($"could not {what} (0x{hr:x8})");
    }

    // ---- services ----

    static int? ServiceState(string name)
    {
        var scm = OpenSCManagerW(null, null, SC_MANAGER_CONNECT);
        if (scm == IntPtr.Zero) return null;
        try
        {
            var svc = OpenServiceW(scm, name, SERVICE_QUERY_STATUS);
            if (svc == IntPtr.Zero) return null;
            try { return QueryServiceStatus(svc, out var status) ? (int)status.dwCurrentState : null; }
            finally { CloseServiceHandle(svc); }
        }
        finally { CloseServiceHandle(scm); }
    }

    static bool StartWindowsService(string name)
    {
        var scm = OpenSCManagerW(null, null, SC_MANAGER_CONNECT);
        if (scm == IntPtr.Zero) return false;
        try
        {
            var svc = OpenServiceW(scm, name, SERVICE_START);
            if (svc == IntPtr.Zero) return false;
            try { return StartServiceW(svc, 0, IntPtr.Zero); }
            finally { CloseServiceHandle(svc); }
        }
        finally { CloseServiceHandle(scm); }
    }

    // ---- COM ----

    static readonly StrategyBasedComWrappers Wrappers = new();
    static readonly Guid PkeyFriendlyName = new("a45c254e-df1c-4efd-8020-67d146a850e0");   // pid 14 name, 2 description
    static readonly Guid PkeyInterfaceName = new("026e516e-b814-414b-83cd-856d6fef4822");  // pid 2: the adapter
    static readonly Guid IidAudioClient = new("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");
    static readonly Guid IidRenderClient = new("F294ACFC-3146-4483-A7BF-ADDCA7C260E2");
    static readonly Guid IidCaptureClient = new("C8ADBD64-E71E-48a0-A4DE-185C395CD317");
    static readonly Guid IidEndpointVolume = new("5CDF2C82-841E-4546-9722-0CF74078229A");
    static readonly Guid IidSimpleVolume = new("87CE5498-68D6-44E5-9215-6DA47EF883D8");

    static IMMDeviceEnumerator Enumerator() =>
        Create<IMMDeviceEnumerator>(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"), new Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"));

    static T Create<T>(Guid clsid, Guid iid)
    {
        CoInitializeEx(IntPtr.Zero, COINIT_MULTITHREADED);
        Marshal.ThrowExceptionForHR(CoCreateInstance(in clsid, IntPtr.Zero, CLSCTX_ALL, in iid, out var ptr));
        try { return (T)Wrappers.GetOrCreateObjectForComInstance(ptr, CreateObjectFlags.UniqueInstance); }
        finally { Marshal.Release(ptr); }
    }

    static T Activate<T>(IMMDevice device, Guid iid) where T : class
    {
        if (device.Activate(&iid, CLSCTX_ALL, IntPtr.Zero, out var ptr) < 0 || ptr == IntPtr.Zero) return null;
        try { return (T)Wrappers.GetOrCreateObjectForComInstance(ptr, CreateObjectFlags.UniqueInstance); }
        finally { Marshal.Release(ptr); }
    }

    static T Service<T>(IAudioClient client, Guid iid)
    {
        Check(client.GetService(&iid, out var ptr), "open the stream service");
        try { return (T)Wrappers.GetOrCreateObjectForComInstance(ptr, CreateObjectFlags.UniqueInstance); }
        finally { Marshal.Release(ptr); }
    }

    static string StringProperty(IPropertyStore store, Guid fmtid, uint pid)
    {
        var key = new PROPERTYKEY { fmtid = fmtid, pid = pid };
        if (store.GetValue(&key, out var value) != 0) return null;
        try { return value.vt == VT_LPWSTR ? Marshal.PtrToStringUni(value.p) : null; }
        finally { PropVariantClear(ref value); }
    }

    const int eRender = 0, eCapture = 1, eConsole = 0, eMultimedia = 1, eCommunications = 2;
    const uint DEVICE_STATE_ACTIVE = 1, STGM_READ = 0, CLSCTX_ALL = 0x17, COINIT_MULTITHREADED = 0;
    const ushort VT_LPWSTR = 31, WAVE_FORMAT_PCM = 1, WAVE_FORMAT_IEEE_FLOAT = 3;
    const uint AUDCLNT_STREAMFLAGS_EVENTCALLBACK = 0x00040000, AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM = 0x80000000,
               AUDCLNT_STREAMFLAGS_SRC_DEFAULT_QUALITY = 0x08000000, AUDCLNT_BUFFERFLAGS_SILENT = 0x2;
    const uint SC_MANAGER_CONNECT = 0x1, SERVICE_QUERY_STATUS = 0x4, SERVICE_START = 0x10;
    const int SERVICE_RUNNING = 4;

    [StructLayout(LayoutKind.Sequential)]
    internal struct PROPERTYKEY { public Guid fmtid; public uint pid; }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PROPVARIANT { public ushort vt, r1, r2, r3; public IntPtr p, p2; }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal struct WAVEFORMATEX
    {
        public ushort wFormatTag, nChannels;
        public uint nSamplesPerSec, nAvgBytesPerSec;
        public ushort nBlockAlign, wBitsPerSample, cbSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SERVICE_STATUS
    {
        public uint dwServiceType, dwCurrentState, dwControlsAccepted, dwWin32ExitCode, dwServiceSpecificExitCode, dwCheckPoint, dwWaitHint;
    }

    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16), Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    internal partial interface IMMDeviceEnumerator
    {
        IMMDeviceCollection EnumAudioEndpoints(int dataFlow, uint stateMask);
        [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice device);
        IMMDevice GetDevice(string id);
        void RegisterEndpointNotificationCallback(IntPtr client);
        void UnregisterEndpointNotificationCallback(IntPtr client);
    }

    [GeneratedComInterface, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
    internal partial interface IMMDeviceCollection
    {
        uint GetCount();
        IMMDevice Item(uint index);
    }

    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16), Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    internal partial interface IMMDevice
    {
        [PreserveSig] int Activate(Guid* iid, uint clsCtx, IntPtr activationParams, out IntPtr iface);
        IPropertyStore OpenPropertyStore(uint access);
        string GetId();
        uint GetState();
    }

    [GeneratedComInterface, Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99")]
    internal partial interface IPropertyStore
    {
        uint GetCount();
        void GetAt(uint index, PROPERTYKEY* key);
        [PreserveSig] int GetValue(PROPERTYKEY* key, out PROPVARIANT value);
    }

    [GeneratedComInterface, Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2")]
    internal partial interface IAudioClient
    {
        [PreserveSig] int Initialize(int shareMode, uint streamFlags, long bufferDuration, long periodicity, WAVEFORMATEX* format, Guid* session);
        [PreserveSig] int GetBufferSize(out uint frames);
        [PreserveSig] int GetStreamLatency(out long latency);
        [PreserveSig] int GetCurrentPadding(out uint frames);
        [PreserveSig] int IsFormatSupported(int shareMode, WAVEFORMATEX* format, WAVEFORMATEX** closest);
        [PreserveSig] int GetMixFormat(out WAVEFORMATEX* format);
        [PreserveSig] int GetDevicePeriod(out long defaultPeriod, out long minimumPeriod);
        [PreserveSig] int Start();
        [PreserveSig] int Stop();
        [PreserveSig] int Reset();
        [PreserveSig] int SetEventHandle(IntPtr handle);
        [PreserveSig] int GetService(Guid* iid, out IntPtr service);
    }

    [GeneratedComInterface, Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2")]
    internal partial interface IAudioRenderClient
    {
        [PreserveSig] int GetBuffer(uint frames, out byte* data);
        [PreserveSig] int ReleaseBuffer(uint frames, uint flags);
    }

    [GeneratedComInterface, Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317")]
    internal partial interface IAudioCaptureClient
    {
        [PreserveSig] int GetBuffer(out byte* data, out uint frames, out uint flags, out ulong devicePosition, out ulong qpcPosition);
        [PreserveSig] int ReleaseBuffer(uint frames);
        [PreserveSig] int GetNextPacketSize(out uint frames);
    }

    [GeneratedComInterface, Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8")]
    internal partial interface ISimpleAudioVolume
    {
        void SetMasterVolume(float level, Guid* context);
        float GetMasterVolume();
        void SetMute(int mute, Guid* context);
        int GetMute();
    }

    [GeneratedComInterface, Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    internal partial interface IAudioEndpointVolume
    {
        void RegisterControlChangeNotify(IntPtr notify);
        void UnregisterControlChangeNotify(IntPtr notify);
        uint GetChannelCount();
        void SetMasterVolumeLevel(float level, Guid* context);
        void SetMasterVolumeLevelScalar(float level, Guid* context);
        float GetMasterVolumeLevel();
        float GetMasterVolumeLevelScalar();
        void SetChannelVolumeLevel(uint channel, float level, Guid* context);
        void SetChannelVolumeLevelScalar(uint channel, float level, Guid* context);
        float GetChannelVolumeLevel(uint channel);
        float GetChannelVolumeLevelScalar(uint channel);
        void SetMute(int mute, Guid* context);
        int GetMute();
    }

    // Undocumented (the Sound control panel's); SetDefaultEndpoint is the 11th method.
    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16), Guid("f8679f50-850a-41cf-9c72-430f290290c8")]
    internal partial interface IPolicyConfig
    {
        [PreserveSig] int GetMixFormat();
        [PreserveSig] int GetDeviceFormat();
        [PreserveSig] int ResetDeviceFormat();
        [PreserveSig] int SetDeviceFormat();
        [PreserveSig] int GetProcessingPeriod();
        [PreserveSig] int SetProcessingPeriod();
        [PreserveSig] int GetShareMode();
        [PreserveSig] int SetShareMode();
        [PreserveSig] int GetPropertyValue();
        [PreserveSig] int SetPropertyValue();
        void SetDefaultEndpoint(string deviceId, int role);
    }

    [DllImport("ole32.dll")]
    static extern int CoInitializeEx(IntPtr reserved, uint coinit);

    [DllImport("ole32.dll")]
    static extern int CoCreateInstance(in Guid clsid, IntPtr outer, uint context, in Guid iid, out IntPtr obj);

    [DllImport("ole32.dll")]
    static extern int PropVariantClear(ref PROPVARIANT value);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr OpenSCManagerW(string machine, string database, uint access);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr OpenServiceW(IntPtr scm, string name, uint access);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool QueryServiceStatus(IntPtr service, out SERVICE_STATUS status);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool StartServiceW(IntPtr service, uint argc, IntPtr argv);

    [DllImport("advapi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CloseServiceHandle(IntPtr handle);
}
