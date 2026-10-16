using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace WinTools;

// vfkit: boot a virtual machine from the command line. On macOS vfkit drives Apple's
// Virtualization.framework; on Windows the hypervisor is reached through QEMU's WHPX accelerator
// (the Windows Hypervisor Platform), so vfkit translates its --bootloader/--device flags into a
// qemu-system command line and runs it in the foreground. --restful-uri serves vfkit's REST API
// (/vm/state, /vm/inspect) over QEMU's QMP monitor. virtio-vsock, virtio-fs and Rosetta have no
// QEMU host backend on Windows and are refused with an explanation.
static class Vfkit
{
    const string Version = "0.6.4";

    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("vfkit", "Run a virtual machine from the command line (QEMU on the Windows Hypervisor Platform)", Run);

    sealed class VfkitDevice(string type, List<(string Key, string Value)> options)
    {
        public readonly string Type = type;
        public readonly List<(string Key, string Value)> Options = options;
        public bool Has(string key) => Options.Any(o => o.Key == key);
        public string Get(string key) => Options.FirstOrDefault(o => o.Key == key).Value;
    }

    sealed class VfkitConfig
    {
        public int Cpus = 1;
        public ulong MemoryMiB = 512;
        public string Kernel, Initrd, Cmdline, EfiStore, RestUri, Ignition;
        public bool EfiCreate, Efi, Gui, PrintOnly;
        public readonly List<VfkitDevice> Devices = [];
        public readonly List<string> CloudInit = [];
    }

    static int Run(string[] args)
    {
        var config = Parse(args, out var exit);
        if (config == null) return exit;
        Validate(config);

        var qemu = FindQemu();
        if (qemu == null && !config.PrintOnly)
        {
            Console.Error.WriteLine("vfkit: QEMU was not found. Install it (winget install SoftwareFreedomConservancy.QEMU)");
            Console.Error.WriteLine("or put qemu-system-x86_64.exe on PATH (or set VFKIT_QEMU to its path).");
            return 1;
        }
        int qmpPort = config.RestUri is { } uri && uri != "none" ? FreePort() : 0;
        var qemuArgs = BuildQemuArgs(config, qemu, qmpPort);
        var exe = qemu ?? QemuExeName();
        if (config.PrintOnly)
        {
            Console.WriteLine(string.Join(" ", new[] { exe }.Concat(qemuArgs).Select(Quote)));
            return 0;
        }

        var psi = new ProcessStartInfo(exe) { UseShellExecute = false };
        foreach (var a in qemuArgs) psi.ArgumentList.Add(a);
        Log("INFO", $"starting VM with {config.Cpus} vCPU(s) and {config.MemoryMiB} MiB (QEMU, WHPX accelerator)");
        using var process = Process.Start(psi);
        Thread rest = null;
        if (qmpPort != 0)
        {
            rest = new Thread(() => ServeRest(config, process, qmpPort)) { IsBackground = true };
            rest.Start();
        }
        process.WaitForExit();
        Log("INFO", $"VM stopped (QEMU exit status {process.ExitCode})");
        return process.ExitCode;
    }

    // ---- command line ---------------------------------------------------------------------------

    static VfkitConfig Parse(string[] args, out int exit)
    {
        exit = 0;
        var c = new VfkitConfig();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string inline = null;
            if (arg.StartsWith("--") && arg.IndexOf('=') is var eq and > 2) { inline = arg[(eq + 1)..]; arg = arg[..eq]; }
            string Next() => inline ?? (i + 1 < args.Length ? args[++i] : throw new UsageException($"flag needs an argument: {arg}"));
            switch (arg)
            {
                case "-h" or "--help": Help(); return null;
                case "-v" or "--version": Console.WriteLine($"vfkit version: v{Version}"); return null;
                case "--cpus": c.Cpus = int.TryParse(Next(), out var n) && n > 0 ? n : throw new UsageException("invalid argument for --cpus"); break;
                case "--memory": c.MemoryMiB = ulong.TryParse(Next(), out var m) && m > 0 ? m : throw new UsageException("invalid argument for --memory"); break;
                case "--kernel": c.Kernel = Next(); break;
                case "--initrd": c.Initrd = Next(); break;
                case "--kernel-cmdline": c.Cmdline = Next(); break;
                case "--bootloader": ParseBootloader(c, Next()); break;
                case "-d" or "--device": c.Devices.Add(ParseDevice(Next())); break;
                case "--restful-uri": c.RestUri = Next(); break;
                case "--gui": c.Gui = inline is null or "true"; break;
                case "--ignition": c.Ignition = Next(); break;
                case "--cloud-init": c.CloudInit.AddRange(Next().Split(',', StringSplitOptions.RemoveEmptyEntries)); break;
                case "--log-level": Next(); break;
                case "--timesync":
                    Next();
                    Log("WARN", "--timesync uses virtio-vsock, which QEMU has no Windows host backend for; ignored");
                    break;
                case "--print-qemu-command": c.PrintOnly = true; break;
                default: throw new UsageException($"unknown flag: {arg}");
            }
        }
        if (c.Kernel == null && !c.Efi) throw new UsageException("missing --bootloader (or --kernel/--initrd/--kernel-cmdline)");
        return c;
    }

    static void ParseBootloader(VfkitConfig c, string spec)
    {
        var parts = SplitOptions(spec);
        var kind = parts.Count > 0 ? parts[0].Key : "";
        var opts = parts.Skip(1).ToList();
        string Opt(string k) => opts.FirstOrDefault(o => o.Key == k).Value;
        switch (kind)
        {
            case "efi":
                c.Efi = true;
                c.EfiStore = Opt("variable-store") ?? throw new UsageException("efi bootloader needs variable-store=<path>");
                c.EfiCreate = opts.Any(o => o.Key == "create");
                break;
            case "linux":
                c.Kernel = Opt("kernel") ?? throw new UsageException("linux bootloader needs kernel=<path>");
                c.Initrd = Opt("initrd");
                c.Cmdline = Opt("cmdline");
                break;
            default: throw new UsageException($"unknown bootloader type: {kind} (efi or linux)");
        }
    }

    static VfkitDevice ParseDevice(string spec)
    {
        var parts = SplitOptions(spec);
        if (parts.Count == 0) throw new UsageException("empty --device");
        return new VfkitDevice(parts[0].Key, parts.Skip(1).ToList());
    }

    // "type,key=value,flag,key=\"a,b\"" -> [(type, null), (key, value), (flag, null), ...]
    static List<(string Key, string Value)> SplitOptions(string spec)
    {
        var result = new List<(string, string)>();
        var current = new StringBuilder();
        var quoted = false;
        void Flush()
        {
            var item = current.ToString();
            current.Clear();
            if (item.Length == 0) return;
            var eq = item.IndexOf('=');
            result.Add(eq < 0 ? (item, null) : (item[..eq], item[(eq + 1)..].Trim('"')));
        }
        foreach (var ch in spec)
        {
            if (ch == '"') quoted = !quoted;
            if (ch == ',' && !quoted) Flush();
            else current.Append(ch);
        }
        Flush();
        return result;
    }

    static void Validate(VfkitConfig c)
    {
        static void Exists(string path)
        {
            if (path != null && !File.Exists(path)) throw new IOException($"open {path}: no such file or directory");
        }
        Exists(c.Kernel);
        Exists(c.Initrd);
        Exists(c.Ignition);
        foreach (var f in c.CloudInit) Exists(f);
        if (c.Efi && !c.EfiCreate) Exists(c.EfiStore);
        foreach (var d in c.Devices)
        {
            switch (d.Type)
            {
                case "virtio-blk" or "nvme" or "usb-mass-storage":
                    Exists(d.Get("path") ?? throw new UsageException($"{d.Type} needs path=<disk image>"));
                    break;
                case "virtio-net" when d.Has("fd"):
                    throw new UsageException("virtio-net,fd= passes a file descriptor, which Windows has no equivalent for; use unixSocketPath= or nat");
                case "virtio-serial" when d.Has("pty"):
                    throw new InvalidOperationException("virtio-serial,pty: Windows has no pseudo-terminals; use stdio or logFilePath=");
                case "virtio-vsock":
                    throw new InvalidOperationException("virtio-vsock: QEMU has no vsock host backend on Windows");
                case "virtio-fs":
                    throw new InvalidOperationException("virtio-fs: QEMU has no virtiofs host backend on Windows; share the folder over SMB instead");
                case "rosetta":
                    throw new InvalidOperationException("rosetta: Rosetta is Apple's x86-64 translator and only exists on macOS");
                case "virtio-net" or "virtio-serial" or "virtio-rng" or "virtio-gpu" or "virtio-input" or "virtio-balloon":
                    break;
                default:
                    throw new UsageException($"unknown device type: {d.Type}");
            }
        }
    }

    // ---- QEMU -----------------------------------------------------------------------------------

    static bool Arm64 => RuntimeInformation.OSArchitecture == Architecture.Arm64;
    static string QemuExeName() => Arm64 ? "qemu-system-aarch64.exe" : "qemu-system-x86_64.exe";

    // VFKIT_QEMU, PATH, brew's qemu, then the official installer's folder.
    static string FindQemu()
    {
        if (Environment.GetEnvironmentVariable("VFKIT_QEMU") is { Length: > 0 } env && File.Exists(env)) return env;
        var name = QemuExeName();
        var dirs = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator).ToList();
        if (Environment.GetEnvironmentVariable("HOMEBREW_PREFIX") is { Length: > 0 } prefix) dirs.Add(Path.Combine(prefix, "opt", "qemu", "bin"));
        dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "qemu"));
        foreach (var dir in dirs.Where(d => !string.IsNullOrWhiteSpace(d)))
        {
            try { var full = Path.Combine(dir.Trim('"'), name); if (File.Exists(full)) return full; }
            catch (ArgumentException) { }
        }
        return null;
    }

    // The EDK2 firmware QEMU ships: share\ next to the official build's exe, ..\share\qemu for MSYS2's.
    static string Firmware(string qemu, string file)
    {
        if (qemu != null)
        {
            var dir = Path.GetDirectoryName(qemu);
            foreach (var candidate in new[] { Path.Combine(dir, "share", file), Path.Combine(dir, "..", "share", "qemu", file) })
                if (File.Exists(candidate)) return Path.GetFullPath(candidate);
            if (!File.Exists(file)) throw new IOException($"QEMU's EFI firmware {file} was not found next to {qemu}");
        }
        return file;
    }

    // QEMU option values double their commas.
    static string Q(string value) => value.Replace(",", ",,");

    static List<string> BuildQemuArgs(VfkitConfig c, string qemu, int qmpPort)
    {
        var a = new List<string> { "-nodefaults", "-no-user-config" };
        if (Arm64) a.AddRange(["-machine", "virt", "-cpu", "max"]);
        else a.AddRange(["-machine", "q35"]);
        // WHPX first; TCG (emulation) when the Windows Hypervisor Platform feature is off.
        a.AddRange(["-accel", Arm64 ? "whpx" : "whpx,kernel-irqchip=off", "-accel", "tcg"]);
        a.AddRange(["-smp", c.Cpus.ToString(), "-m", $"{c.MemoryMiB}M"]);

        if (c.Efi)
        {
            var code = Firmware(qemu, Arm64 ? "edk2-aarch64-code.fd" : "edk2-x86_64-code.fd");
            if (c.EfiCreate && !File.Exists(c.EfiStore) && !c.PrintOnly)
                File.Copy(Firmware(qemu, Arm64 ? "edk2-arm-vars.fd" : "edk2-i386-vars.fd"), c.EfiStore);
            a.AddRange(["-drive", $"if=pflash,format=raw,readonly=on,file={Q(code)}"]);
            a.AddRange(["-drive", $"if=pflash,format=raw,file={Q(Path.GetFullPath(c.EfiStore))}"]);
        }
        else
        {
            a.AddRange(["-kernel", Path.GetFullPath(c.Kernel)]);
            if (c.Initrd != null) a.AddRange(["-initrd", Path.GetFullPath(c.Initrd)]);
            if (c.Cmdline != null) a.AddRange(["-append", c.Cmdline]);
        }

        int n = 0;
        var usb = false;
        foreach (var d in c.Devices)
        {
            var id = $"{d.Type.Replace("-", "")}{n++}";
            switch (d.Type)
            {
                case "virtio-blk":
                    a.AddRange(["-drive", $"if=none,id={id},format={DiskFormat(d.Get("path"))},file={Q(Path.GetFullPath(d.Get("path")))}"]);
                    a.AddRange(["-device", d.Get("deviceId") is { } serial ? $"virtio-blk-pci,drive={id},serial={Q(serial)}" : $"virtio-blk-pci,drive={id}"]);
                    break;
                case "nvme":
                    a.AddRange(["-drive", $"if=none,id={id},format={DiskFormat(d.Get("path"))},file={Q(Path.GetFullPath(d.Get("path")))}"]);
                    a.AddRange(["-device", $"nvme,drive={id},serial={id}"]);
                    break;
                case "usb-mass-storage":
                    if (!usb) { a.AddRange(["-device", "qemu-xhci,id=xhci"]); usb = true; }
                    var ro = d.Has("readonly") ? ",readonly=on" : "";
                    a.AddRange(["-drive", $"if=none,id={id},format={DiskFormat(d.Get("path"))},file={Q(Path.GetFullPath(d.Get("path")))}{ro}"]);
                    a.AddRange(["-device", $"usb-storage,bus=xhci.0,drive={id}"]);
                    break;
                case "virtio-net":
                    if (d.Get("unixSocketPath") is { } sock)
                        a.AddRange(["-netdev", $"stream,id={id},server=off,addr.type=unix,addr.path={Q(Path.GetFullPath(sock))}"]);
                    else
                        a.AddRange(["-netdev", $"user,id={id}"]);   // nat: QEMU's user-mode NAT
                    a.AddRange(["-device", d.Get("mac") is { } mac ? $"virtio-net-pci,netdev={id},mac={mac}" : $"virtio-net-pci,netdev={id}"]);
                    break;
                case "virtio-serial":
                    a.AddRange(["-device", $"virtio-serial-pci,id={id}"]);
                    a.AddRange(["-chardev", d.Has("stdio") ? $"stdio,id={id}c,signal=off" : $"file,id={id}c,path={Q(Path.GetFullPath(d.Get("logFilePath") ?? throw new UsageException("virtio-serial needs logFilePath=<file>, stdio or pty")))}"]);
                    a.AddRange(["-device", $"virtconsole,bus={id}.0,chardev={id}c"]);
                    break;
                case "virtio-rng": a.AddRange(["-device", "virtio-rng-pci"]); break;
                case "virtio-balloon": a.AddRange(["-device", "virtio-balloon-pci"]); break;
                case "virtio-gpu":
                    var gpu = "virtio-gpu-pci";
                    if (d.Get("width") is { } w) gpu += $",xres={w}";
                    if (d.Get("height") is { } h) gpu += $",yres={h}";
                    a.AddRange(["-device", gpu]);
                    break;
                case "virtio-input":
                    a.AddRange(["-device", d.Has("keyboard") ? "virtio-keyboard-pci" : "virtio-tablet-pci"]);
                    break;
            }
        }

        // Ignition: QEMU's fw_cfg key that Fedora CoreOS reads (vfkit serves it over vsock instead).
        if (c.Ignition != null) a.AddRange(["-fw_cfg", $"name=opt/com.coreos/config,file={Q(Path.GetFullPath(c.Ignition))}"]);
        // cloud-init: a read-only FAT disk labelled CIDATA holding user-data/meta-data (NoCloud).
        if (c.CloudInit.Count > 0)
        {
            var dir = Path.Combine(Path.GetTempPath(), $"vfkit-cidata-{Environment.ProcessId}");
            if (!c.PrintOnly)
            {
                Directory.CreateDirectory(dir);
                foreach (var f in c.CloudInit) File.Copy(f, Path.Combine(dir, Path.GetFileName(f)), overwrite: true);
            }
            a.AddRange(["-drive", $"if=virtio,readonly=on,format=raw,file.driver=vvfat,file.dir={Q(dir)},file.label=CIDATA"]);
        }

        if (!c.Gui) a.AddRange(["-display", "none"]);
        if (qmpPort != 0) a.AddRange(["-qmp", $"tcp:127.0.0.1:{qmpPort},server=on,wait=off"]);
        return a;
    }

    static string DiskFormat(string path)
    {
        try
        {
            using var f = File.OpenRead(path);
            Span<byte> magic = stackalloc byte[4];
            return f.Read(magic) == 4 && magic.SequenceEqual((ReadOnlySpan<byte>)[0x51, 0x46, 0x49, 0xFB]) ? "qcow2" : "raw";   // "QFI\xfb"
        }
        catch (IOException) { return "raw"; }
    }

    static string Quote(string s) => s.Length > 0 && !s.Any(ch => ch is ' ' or '"' or '\t') ? s : "\"" + s.Replace("\"", "\\\"") + "\"";

    static void Log(string level, string message) =>
        Console.Error.WriteLine($"{level}[{(int)(DateTime.Now - Process.GetCurrentProcess().StartTime).TotalSeconds:0000}] {message}");

    static int FreePort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    // ---- REST API over QMP ----------------------------------------------------------------------

    // vfkit's REST API: GET /vm/state, POST /vm/state {"state": "Stop|HardStop|Pause|Resume"}, GET /vm/inspect.
    // tcp://host:port listens on TCP, unix:///path on an AF_UNIX socket (Windows 10 1803+).
    static void ServeRest(VfkitConfig config, Process qemu, int qmpPort)
    {
        Socket listener;
        var uri = config.RestUri;
        try
        {
            if (uri.StartsWith("unix://"))
            {
                var path = uri["unix://".Length..];
                if (File.Exists(path)) File.Delete(path);
                listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                listener.Bind(new UnixDomainSocketEndPoint(path));
            }
            else if (uri.StartsWith("tcp://"))
            {
                var hostPort = uri["tcp://".Length..].TrimEnd('/');
                var colon = hostPort.LastIndexOf(':');
                var host = hostPort[..colon];
                var address = host is "localhost" or "" ? IPAddress.Loopback : IPAddress.Parse(host);
                listener = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(address, int.Parse(hostPort[(colon + 1)..])));
            }
            else { Log("ERRO", $"unsupported --restful-uri {uri} (tcp://host:port or unix:///path)"); return; }
            listener.Listen(8);
        }
        catch (Exception e) when (e is SocketException or FormatException or IOException or ArgumentException)
        {
            Log("ERRO", $"cannot listen on {uri}: {e.Message}");
            return;
        }
        Thread.Sleep(500);   // QEMU opens its QMP socket at startup
        using var qmp = new VfkitQmp(qmpPort);
        while (!qemu.HasExited)
        {
            using var client = listener.Accept();
            try { HandleRest(client, config, qmp); }
            catch (Exception e) when (e is IOException or SocketException or JsonException) { }
        }
    }

    static void HandleRest(Socket client, VfkitConfig config, VfkitQmp qmp)
    {
        using var stream = new NetworkStream(client);
        var reader = new StreamReader(stream, Encoding.UTF8);
        var request = reader.ReadLine()?.Split(' ');
        if (request is not { Length: >= 2 }) return;
        var length = 0;
        for (string line; !string.IsNullOrEmpty(line = reader.ReadLine());)
            if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)) length = int.Parse(line[15..].Trim());
        var body = new char[length];
        if (length > 0) reader.ReadBlock(body, 0, length);

        int status = 200;
        string response;
        switch ((request[0], request[1]))
        {
            case ("GET", "/vm/state"):
                response = StateJson(qmp.Status());
                break;
            case ("POST", "/vm/state"):
                using (var doc = JsonDocument.Parse(new string(body)))
                {
                    var wanted = doc.RootElement.GetProperty("state").GetString();
                    var command = wanted switch
                    {
                        "Stop" => "system_powerdown", "HardStop" => "quit", "Pause" => "stop", "Resume" or "Running" => "cont", _ => null,
                    };
                    if (command == null) { status = 400; response = "{\"error\":\"unknown state\"}"; break; }
                    qmp.Execute(command);
                    response = StateJson(command == "quit" ? "shutdown" : qmp.Status());
                }
                break;
            case ("GET", "/vm/inspect"):
                response = InspectJson(config);
                break;
            default:
                status = 404;
                response = "{\"error\":\"not found\"}";
                break;
        }
        var bytes = Encoding.UTF8.GetBytes(response);
        var head = $"HTTP/1.1 {status} {(status == 200 ? "OK" : "Error")}\r\nContent-Type: application/json\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n";
        stream.Write(Encoding.ASCII.GetBytes(head));
        stream.Write(bytes);
    }

    static string StateJson(string qemuStatus)
    {
        var (state, running, paused) = qemuStatus switch
        {
            "running" => ("VirtualMachineStateRunning", true, false),
            "paused" => ("VirtualMachineStatePaused", false, true),
            "shutdown" or "" => ("VirtualMachineStateStopped", false, false),
            _ => ("VirtualMachineStateStopping", false, false),
        };
        return $"{{\"state\":\"{state}\",\"canStart\":false,\"canPause\":{(running ? "true" : "false")},\"canResume\":{(paused ? "true" : "false")},\"canStop\":{(running ? "true" : "false")},\"canHardStop\":true}}";
    }

    static string InspectJson(VfkitConfig c)
    {
        using var ms = new MemoryStream();
        using (var w = new Utf8JsonWriter(ms))
        {
            w.WriteStartObject();
            w.WriteStartObject("bootloader");
            if (c.Efi) { w.WriteString("kind", "efiBootloader"); w.WriteString("efiVariableStorePath", c.EfiStore); w.WriteBoolean("createVariableStore", c.EfiCreate); }
            else { w.WriteString("kind", "linuxBootloader"); w.WriteString("vmlinuzPath", c.Kernel); w.WriteString("initrdPath", c.Initrd); w.WriteString("kernelCmdLine", c.Cmdline); }
            w.WriteEndObject();
            w.WriteNumber("vcpus", c.Cpus);
            w.WriteNumber("memoryBytes", c.MemoryMiB * 1024 * 1024);
            w.WriteStartArray("devices");
            foreach (var d in c.Devices)
            {
                w.WriteStartObject();
                w.WriteString("kind", d.Type);
                foreach (var (k, v) in d.Options) { if (v == null) w.WriteBoolean(k, true); else w.WriteString(k, v); }
                w.WriteEndObject();
            }
            w.WriteEndArray();
            w.WriteEndObject();
        }
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    // Minimal QMP client: greeting, qmp_capabilities, then one command per request.
    sealed class VfkitQmp : IDisposable
    {
        readonly TcpClient tcp;
        readonly StreamReader reader;
        readonly StreamWriter writer;

        public VfkitQmp(int port)
        {
            tcp = new TcpClient();
            for (var attempt = 0; ; attempt++)
            {
                try { tcp.Connect(IPAddress.Loopback, port); break; }
                catch (SocketException) when (attempt < 20) { Thread.Sleep(250); }
            }
            var stream = tcp.GetStream();
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream) { AutoFlush = true, NewLine = "\n" };
            reader.ReadLine();   // greeting
            Execute("qmp_capabilities");
        }

        public JsonElement Execute(string command)
        {
            lock (this)
            {
                writer.WriteLine($"{{\"execute\":\"{command}\"}}");
                for (string line; (line = reader.ReadLine()) != null;)
                {
                    using var doc = JsonDocument.Parse(line);
                    if (doc.RootElement.TryGetProperty("return", out var ret)) return ret.Clone();
                    if (doc.RootElement.TryGetProperty("error", out var err)) throw new IOException(err.GetProperty("desc").GetString());
                    // asynchronous events are skipped
                }
                return default;
            }
        }

        public string Status()
        {
            try { return Execute("query-status").GetProperty("status").GetString(); }
            catch (Exception e) when (e is IOException or InvalidOperationException or KeyNotFoundException) { return "shutdown"; }
        }

        public void Dispose() => tcp.Dispose();
    }

    static void Help() => Console.WriteLine("""
        A hypervisor for the command line (QEMU on the Windows Hypervisor Platform)

        Usage:
          vfkit [flags]

        Flags:
              --bootloader string         bootloader configuration: efi,variable-store=<path>[,create]
                                          or linux,kernel=<path>,initrd=<path>,cmdline="<args>"
              --kernel string             path to the virtual machine linux kernel (deprecated)
              --initrd string             path to the virtual machine initrd (deprecated)
              --kernel-cmdline string     kernel command line (deprecated)
              --cpus uint                 number of virtual CPUs (default 1)
              --memory uint               virtual machine RAM size in MiB (default 512)
          -d, --device stringArray        devices: virtio-blk,path=<img> | nvme,path=<img> |
                                          usb-mass-storage,path=<img>[,readonly] |
                                          virtio-net,nat[,mac=<mac>] | virtio-net,unixSocketPath=<sock> |
                                          virtio-serial,logFilePath=<file> | virtio-serial,stdio |
                                          virtio-rng | virtio-balloon | virtio-gpu,width=<w>,height=<h> |
                                          virtio-input,keyboard | virtio-input,pointing
              --restful-uri string        URI of the REST API: tcp://host:port or unix:///path (default "none")
              --gui                       show the VM's display in a window
              --ignition string           Ignition config (passed through QEMU's fw_cfg)
              --cloud-init string         comma-separated user-data/meta-data files (NoCloud CIDATA disk)
              --timesync string           accepted; needs vsock, which Windows' QEMU lacks
              --log-level string          log level
              --print-qemu-command        (Windows) print the QEMU command line instead of running it
          -h, --help                      help for vfkit
          -v, --version                   version for vfkit

        The VM runs in QEMU (qemu-system-x86_64, or -aarch64 on Arm) with the WHPX accelerator; turn on
        the "Windows Hypervisor Platform" feature for hardware speed. QEMU is found on PATH, through
        VFKIT_QEMU, in brew's qemu or in the official installer's folder. virtio-vsock, virtio-fs and
        rosetta devices have no Windows host backend.
        """);
}
