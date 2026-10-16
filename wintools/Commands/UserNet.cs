using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace WinTools;

// passt and slirp4netns: user-mode networking for virtual machines. A guest's Ethernet frames
// arrive over a QEMU "stream" socket (each frame preceded by its 32-bit big-endian length, what
// `-netdev stream,addr.type=unix` speaks) and are terminated here: ARP and DHCP are answered,
// DNS goes to the host's resolvers, and TCP, UDP and ping are carried on ordinary host sockets,
// so the guest reaches the network as the host user does, with no driver or administrator.
// IPv4 only. passt serves the socket (QEMU connects); slirp4netns connects to a socket QEMU
// serves (--target-type=stream), since Windows has no network namespaces or TAP devices; pasta,
// which joins a namespace, can't exist on Windows.
static class UserNet
{
    const string PasstVersion = "2026_10_02.cba3570";
    const string SlirpVersion = "1.3.6";

    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("passt", "User-mode networking for virtual machines (QEMU stream socket)", a => Guard("passt", Passt, a));
        Commands.Add("pasta", "User-mode networking for network namespaces", Pasta);
        Commands.Add("slirp4netns", "User-mode networking (slirp) for a VM's frame socket", a => Guard("slirp4netns", Slirp4netns, a));
    }

    // Socket errors (socket path in use, nothing listening, port taken) end as "name: message", exit 1.
    static int Guard(string name, Func<string[], int> run, string[] args)
    {
        try { return run(args); }
        catch (SocketException e)
        {
            Console.Error.WriteLine($"{name}: {e.Message}");
            return 1;
        }
    }

    // ---- passt ----

    static int Passt(string[] args)
    {
        var cfg = new Config();
        string socketPath = null, pidFile = null;
        var oneOff = false;
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Value()
            {
                var eq = a.IndexOf('=');
                if (a.StartsWith("--") && eq > 0) return a[(eq + 1)..];
                return ++i < args.Length ? args[i] : throw new UsageException($"option '{a}' requires an argument");
            }
            var name = a.StartsWith("--") && a.Contains('=') ? a[..a.IndexOf('=')] : a;
            switch (name)
            {
                case "-h" or "--help": Console.WriteLine(PasstHelp); return 0;
                case "--version": Console.WriteLine($"passt {PasstVersion}\nCopyright Red Hat\nGNU General Public License, version 2 or later\n  <https://www.gnu.org/licenses/old-licenses/gpl-2.0.html>\nThis is free software: you are free to change and redistribute it.\nThere is NO WARRANTY, to the extent permitted by law."); return 0;
                case "-s" or "--socket" or "--socket-path": socketPath = Value(); break;
                case "-1" or "--one-off": oneOff = true; break;
                case "-t" or "--tcp-ports": cfg.TcpForwards.AddRange(PortSpec(Value(), "-t")); break;
                case "-u" or "--udp-ports": cfg.UdpForwards.AddRange(PortSpec(Value(), "-u")); break;
                case "-a" or "--address": cfg.GuestIp = V4(Value()); break;
                case "-n" or "--netmask": cfg.PrefixLength = Prefix(Value()); break;
                case "-g" or "--gateway": cfg.Gateway = V4(Value()); break;
                case "-D" or "--dns": cfg.Dns = V4(Value()); break;
                case "--dns-forward": cfg.Dns = V4(Value()); break;
                case "-M" or "--mac-addr": cfg.OurMac = Mac(Value()); break;
                case "-m" or "--mtu": cfg.Mtu = int.TryParse(Value(), out var m) && m is >= 576 and <= 65520 ? m : throw new UsageException("invalid MTU"); break;
                case "--no-dhcp": cfg.Dhcp = false; break;
                case "--no-map-gw": cfg.MapGateway = false; break;
                case "--no-tcp": cfg.Tcp = false; break;
                case "--no-udp": cfg.Udp = false; break;
                case "--no-icmp": cfg.Icmp = false; break;
                case "-P" or "--pid": pidFile = Value(); break;
                case "-4" or "--ipv4-only" or "-f" or "--foreground" or "-q" or "--quiet" or "-d" or "--debug" or "--trace" or "-e" or "--stderr"
                    or "--no-ndp" or "--no-dhcpv6" or "--no-ra":
                    if (name is "-d" or "--debug" or "--trace") cfg.Verbose = true;
                    break;
                case "-l" or "--log-file" or "-i" or "--interface" or "-o" or "--outbound" or "-S" or "--search": Value(); break;
                case "-6" or "--ipv6-only":
                    Console.Error.WriteLine("passt: IPv6 isn't available in this build; the guest gets IPv4");
                    return 1;
                case "-F" or "--fd" or "--vhost-user":
                    Console.Error.WriteLine($"passt: {name} passes Unix file descriptors, which Windows processes can't share; use --socket");
                    return 1;
                default: throw new UsageException($"invalid option -- '{a}'");
            }
        }
        cfg.FillFromHost();
        socketPath ??= FreeSocketPath();
        if (File.Exists(socketPath)) File.Delete(socketPath);
        using var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        listener.Bind(new UnixDomainSocketEndPoint(socketPath));
        listener.Listen(1);
        if (pidFile != null) File.WriteAllText(pidFile, Environment.ProcessId + "\n");
        Console.Error.WriteLine($"UNIX domain socket bound at {socketPath}\n");
        Console.Error.WriteLine("You can now start qemu (>= 7.2, with commit 13c6be96618c):");
        Console.Error.WriteLine($"    qemu-system-x86_64 ... -device virtio-net-pci,netdev=s -netdev stream,id=s,server=off,addr.type=unix,addr.path={socketPath}");
        Console.Error.WriteLine($"guest: {cfg.GuestIp}/{cfg.PrefixLength} gateway {cfg.Gateway} dns {cfg.Dns}");
        try
        {
            do
            {
                using var client = listener.Accept();
                Console.Error.WriteLine("accepted connection from PID 0");
                var stack = new Stack(cfg);
                stack.Run(new NetworkStream(client, ownsSocket: false)).GetAwaiter().GetResult();
                Console.Error.WriteLine("Client connection closed");
            } while (!oneOff);
        }
        finally { try { File.Delete(socketPath); } catch (IOException) { } }
        return 0;
    }

    const string PasstHelp = """
        Usage: passt [OPTION]...

          -d, --debug		Be verbose
          -q, --quiet		Don't print informational messages
          -f, --foreground	Don't run in background (always the case on Windows)
          -h, --help		Display this help message and exit
          --version		Show version and exit
          -s, --socket PATH	UNIX domain socket path
            default: probe free path starting from %TEMP%\passt_1.socket
          -1, --one-off	Quit after handling one single client
          -t, --tcp-ports SPEC	TCP port forwarding to guest
            SPEC: none, or [host_port[-host_port]][:guest_port[-guest_port]][,...]
          -u, --udp-ports SPEC	UDP port forwarding to guest, same SPEC
          -a, --address ADDR	Assign IPv4 address ADDR to the guest
          -n, --netmask MASK	Assign IPv4 MASK, dot-decimal or bits
          -M, --mac-addr ADDR	Use source MAC address ADDR
          -g, --gateway ADDR	Pass IPv4 gateway address to guest
          -D, --dns ADDR	Address the guest uses for DNS (forwarded to the host's resolvers)
          -m, --mtu MTU	Assign MTU via DHCP
          --no-tcp, --no-udp, --no-icmp, --no-dhcp, --no-map-gw
          -P, --pid FILE	Write own PID to the given file

        The guest's frames are carried over host sockets; connections to the gateway address
        reach the host's own loopback services (unless --no-map-gw). IPv4 only.
        """;

    static string FreeSocketPath()
    {
        for (int n = 1; n < 100; n++)
        {
            var p = Path.Combine(Path.GetTempPath(), $"passt_{n}.socket");
            if (!File.Exists(p)) return p;
        }
        throw new InvalidOperationException("no free socket path in %TEMP%");
    }

    static int Pasta(string[] args)
    {
        if (args.Any(a => a is "-h" or "--help"))
        {
            Console.WriteLine("Usage: pasta [OPTION]... [COMMAND [ARG]...]\n       pasta [OPTION]... PID\n       pasta [OPTION]... --netns [PATH|NAME]\n\n" +
                              "pasta connects a Linux network namespace to the host's network. Windows has no network\n" +
                              "namespaces; for virtual machines use passt, which serves the same stack over a socket.");
            return 0;
        }
        if (args.Contains("--version")) { Console.WriteLine($"pasta {PasstVersion}"); return 0; }
        Console.Error.WriteLine("pasta: Windows has no network namespaces to attach to; use passt with a virtual machine");
        return 1;
    }

    // ---- slirp4netns ----

    static int Slirp4netns(string[] args)
    {
        var cfg = new Config { GuestIp = IPAddress.Parse("10.0.2.100"), Gateway = IPAddress.Parse("10.0.2.2"), Dns = IPAddress.Parse("10.0.2.3"), PrefixLength = 24 };
        string targetType = "netns", apiSocket = null;
        var positional = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            var eq = a.IndexOf('=');
            var name = a.StartsWith("--") && eq > 0 ? a[..eq] : a;
            string Value() => a.StartsWith("--") && eq > 0 ? a[(eq + 1)..] : ++i < args.Length ? args[i] : throw new UsageException($"option '{a}' requires an argument");
            switch (name)
            {
                case "-h" or "--help": Console.WriteLine(SlirpHelp); return 0;
                case "-v" or "--version":
                    Console.WriteLine($"slirp4netns version {SlirpVersion}\ncommit: windows\nlibslirp: (built in, IPv4)\nSLIRP_CONFIG_VERSION_MAX: 5\nlibseccomp: (none)");
                    return 0;
                case "-c" or "--configure": break;   // the guest configures itself through DHCP
                case "-m" or "--mtu": cfg.Mtu = int.TryParse(Value(), out var m) && m is >= 68 and <= 65521 ? m : throw new UsageException("MTU must be a number between 68 and 65521"); break;
                case "--cidr":
                {
                    var cidr = Value().Split('/');
                    var net = V4(cidr[0]);
                    cfg.PrefixLength = cidr.Length > 1 ? Prefix(cidr[1]) : 24;
                    var b = net.GetAddressBytes();
                    cfg.Gateway = new IPAddress([b[0], b[1], b[2], (byte)(b[3] | 2)]);
                    cfg.Dns = new IPAddress([b[0], b[1], b[2], (byte)(b[3] | 3)]);
                    cfg.GuestIp = new IPAddress([b[0], b[1], b[2], (byte)(b[3] | 100)]);
                    break;
                }
                case "--disable-host-loopback": cfg.MapGateway = false; break;
                case "--disable-dns": cfg.Dns = null; break;
                case "--macaddress": cfg.GuestMacHint = Mac(Value()); break;
                case "-a" or "--api-socket": apiSocket = Value(); break;
                case "--target-type": targetType = Value(); break;
                case "--outbound-addr": cfg.Outbound = V4(Value()); break;
                case "--netns-type" or "--userns-path" or "--outbound-addr6" or "-e" or "--exit-fd" or "-r" or "--ready-fd": Value(); break;
                case "--enable-sandbox" or "--enable-seccomp": break;
                case "-6" or "--enable-ipv6":
                    Console.Error.WriteLine("slirp4netns: IPv6 isn't available in this build");
                    return 1;
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unrecognized option '{a}'");
                    positional.Add(a);
                    break;
            }
        }
        if (positional.Count is < 1 or > 2) throw new UsageException("usage: slirp4netns [OPTION]... PID|PATH [TAPNAME]");
        if (targetType != "stream")
        {
            Console.Error.WriteLine("slirp4netns: Windows has no network namespaces or TAP devices; give --target-type=stream and the path of a QEMU -netdev stream server socket");
            return 1;
        }
        var stack = new Stack(cfg);
        using var api = apiSocket != null ? StartApi(apiSocket, stack) : null;
        using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        socket.Connect(new UnixDomainSocketEndPoint(positional[0]));
        Console.Error.WriteLine($"connected to {positional[0]}; guest {cfg.GuestIp}/{cfg.PrefixLength}, gateway {cfg.Gateway}, dns {cfg.Dns?.ToString() ?? "(disabled)"}");
        stack.Run(new NetworkStream(socket, ownsSocket: false)).GetAwaiter().GetResult();
        return 0;
    }

    const string SlirpHelp = """
        Usage: slirp4netns [OPTION]... PID|PATH [TAPNAME]
        User-mode networking for virtual machines' frame sockets.

        -c, --configure          bring up the interface (the guest uses DHCP)
        -m, --mtu=MTU            specify MTU (default=1500, max=65521)
        -a, --api-socket=PATH    specify API socket path (add_hostfwd, list_hostfwd, remove_hostfwd)
        --cidr=CIDR              specify network address CIDR (default=10.0.2.0/24)
        --disable-host-loopback  prohibit connecting to 127.0.0.1:* on the host
        --outbound-addr=IPv4     sets outbound ipv4 address to bound to
        --disable-dns            disables 10.0.2.3 (or configured internal ip) to host dns redirect
        --macaddress=MAC         accepted (the guest's own MAC is learned)
        --target-type=TYPE       stream: PATH is a QEMU -netdev stream server socket (required on
                                 Windows, which has no network namespaces or TAP devices)
        -h, --help               show this help and exit
        -v, --version            show version and exit
        """;

    // slirp4netns's JSON API: one request per connection on a Unix socket.
    static IDisposable StartApi(string path, Stack stack)
    {
        if (File.Exists(path)) File.Delete(path);
        var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        listener.Bind(new UnixDomainSocketEndPoint(path));
        listener.Listen(8);
        _ = Task.Run(async () =>
        {
            while (true)
            {
                Socket client;
                try { client = await listener.AcceptAsync(); }
                catch (Exception e) when (e is SocketException or ObjectDisposedException) { return; }
                _ = Task.Run(async () =>
                {
                    using var c = client;
                    var buffer = new byte[4096];
                    var n = await c.ReceiveAsync(buffer);
                    var reply = ApiRequest(stack, Encoding.UTF8.GetString(buffer, 0, n));
                    await c.SendAsync(Encoding.UTF8.GetBytes(reply));
                });
            }
        });
        return listener;
    }

    static string ApiRequest(Stack stack, string request)
    {
        try
        {
            using var doc = JsonDocument.Parse(request);
            var root = doc.RootElement;
            var execute = root.GetProperty("execute").GetString();
            var args = root.TryGetProperty("arguments", out var a) ? a : default;
            switch (execute)
            {
                case "add_hostfwd":
                {
                    var proto = args.GetProperty("proto").GetString();
                    var hostAddr = args.TryGetProperty("host_addr", out var ha) ? ha.GetString() : "0.0.0.0";
                    var hostPort = args.GetProperty("host_port").GetInt32();
                    var guestPort = args.GetProperty("guest_port").GetInt32();
                    if (proto is not ("tcp" or "udp")) return Error("bad request: add_hostfwd: bad arguments.proto");
                    var id = stack.AddForward(proto == "tcp", IPAddress.Parse(hostAddr), hostPort, guestPort);
                    return $"{{\"return\": {{\"id\": {id}}}}}";
                }
                case "list_hostfwd":
                {
                    var entries = stack.Forwards.Select(f =>
                        $"{{\"id\": {f.Id}, \"proto\": \"{(f.Tcp ? "tcp" : "udp")}\", \"host_addr\": \"{f.HostAddr}\", \"host_port\": {f.HostPort}, \"guest_addr\": \"{stack.Cfg.GuestIp}\", \"guest_port\": {f.GuestPort}}}");
                    return $"{{\"return\": {{\"entries\": [{string.Join(", ", entries)}]}}}}";
                }
                case "remove_hostfwd":
                    return stack.RemoveForward(args.GetProperty("id").GetInt32()) ? "{\"return\": {}}" : Error("bad request: remove_hostfwd: bad arguments.id");
                default:
                    return Error($"bad request: unknown execute: {execute}");
            }
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException or FormatException or SocketException)
        {
            return Error($"bad request: {e.Message}");
        }

        static string Error(string desc) => $"{{\"error\": {{\"desc\": \"{JsonEncodedText.Encode(desc)}\"}}}}";
    }

    // ---- configuration ----

    sealed class Config
    {
        public IPAddress GuestIp, Gateway, Dns, Outbound;
        public int PrefixLength = 24, Mtu = 1500;
        public byte[] OurMac = [0x9a, 0x55, 0x9a, 0x55, 0x9a, 0x55], GuestMacHint;
        public bool Dhcp = true, MapGateway = true, Tcp = true, Udp = true, Icmp = true, Verbose;
        public List<(int Host, int Guest)> TcpForwards = new(), UdpForwards = new();
        public List<IPAddress> HostDns = new();

        // passt's default: the guest takes the host's own address and gateway.
        public void FillFromHost()
        {
            var nic = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .FirstOrDefault(n => n.GetIPProperties().GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork));
            var props = nic?.GetIPProperties();
            var addr = props?.UnicastAddresses.FirstOrDefault(u => u.Address.AddressFamily == AddressFamily.InterNetwork);
            if (GuestIp == null && addr != null) { GuestIp = addr.Address; if (PrefixLength == 24) PrefixLength = addr.PrefixLength; }
            Gateway ??= props?.GatewayAddresses.Select(g => g.Address).FirstOrDefault(g => g.AddressFamily == AddressFamily.InterNetwork);
            GuestIp ??= IPAddress.Parse("10.0.2.15");
            Gateway ??= IPAddress.Parse("10.0.2.2");
            Dns ??= Gateway;
        }
    }

    static IPAddress V4(string s) => IPAddress.TryParse(s, out var ip) && ip.AddressFamily == AddressFamily.InterNetwork ? ip : throw new UsageException($"invalid IPv4 address {s}");

    static int Prefix(string s)
    {
        if (int.TryParse(s, out var bits) && bits is >= 0 and <= 32) return bits;
        var mask = BinaryPrimitives.ReadUInt32BigEndian(V4(s).GetAddressBytes());
        return System.Numerics.BitOperations.PopCount(mask);
    }

    static byte[] Mac(string s)
    {
        var parts = s.Split(':', '-');
        if (parts.Length != 6) throw new UsageException($"invalid MAC address {s}");
        return parts.Select(p => Convert.ToByte(p, 16)).ToArray();
    }

    // "80", "8080:80", "1000-1010", "1000-1010:2000-2010", comma-separated; "none" for no forwards.
    static IEnumerable<(int, int)> PortSpec(string spec, string option)
    {
        if (spec == "none") yield break;
        if (spec is "all" or "auto") throw new UsageException($"{option} {spec}: give explicit ports on Windows");
        foreach (var item in spec.Split(','))
        {
            var sides = item.Split(':');
            var (h0, h1) = Range(sides[0], option);
            var (g0, g1) = sides.Length > 1 ? Range(sides[1], option) : (h0, h1);
            if (h1 - h0 != g1 - g0) throw new UsageException($"{option} {item}: port ranges differ in size");
            for (int p = 0; p <= h1 - h0; p++) yield return (h0 + p, g0 + p);
        }
    }

    static (int, int) Range(string s, string option)
    {
        var dash = s.Split('-');
        if (!int.TryParse(dash[0], out var a) || a is < 1 or > 65535) throw new UsageException($"{option}: invalid port {s}");
        var b = a;
        if (dash.Length > 1 && (!int.TryParse(dash[1], out b) || b < a || b > 65535)) throw new UsageException($"{option}: invalid port range {s}");
        return (a, b);
    }

    // ---- the stack ----

    sealed class Forward
    {
        public int Id, HostPort, GuestPort;
        public bool Tcp;
        public IPAddress HostAddr;
        public IDisposable Listener;
    }

    sealed class TcpConn
    {
        public uint RemoteIp;            // the address the guest talks to (host byte order)
        public ushort RemotePort, GuestPort;
        public uint OurNext, OurUnacked, GuestNext;
        public int GuestWindow = 65535, Mss = 1460;
        public bool Established, OurFinSent, GuestFinSeen, Closed, GuestInitiated;
        public Socket Host;
        public readonly List<byte> Unacked = new();   // bytes from OurUnacked on
        public long LastSend;
        public readonly Channel<byte[]> ToHost = Channel.CreateUnbounded<byte[]>();
        public readonly SemaphoreSlim WindowOpen = new(0);
    }

    sealed class UdpFlow
    {
        public Socket Socket;
        public uint RemoteIp;
        public ushort RemotePort, GuestPort;
        public long LastUsed;
        public IPEndPoint LastPeer;    // inbound forwards: where replies go
    }

    sealed class Stack(Config cfg)
    {
        public readonly Config Cfg = cfg;
        readonly object sync = new();
        readonly Channel<byte[]> toGuest = Channel.CreateUnbounded<byte[]>();
        readonly Dictionary<(ushort GuestPort, uint RemoteIp, ushort RemotePort), TcpConn> tcp = new();
        readonly Dictionary<(ushort GuestPort, uint RemoteIp, ushort RemotePort), UdpFlow> udp = new();
        public readonly List<Forward> Forwards = new();
        byte[] guestMac = cfg.GuestMacHint ?? [0xff, 0xff, 0xff, 0xff, 0xff, 0xff];
        ushort ipId;
        int nextForwardId = 1, nextEphemeral = 49152;
        readonly CancellationTokenSource stop = new();

        uint GuestIp => U32(Cfg.GuestIp);
        uint GatewayIp => U32(Cfg.Gateway);
        uint DnsIp => Cfg.Dns != null ? U32(Cfg.Dns) : 0;

        public async Task Run(Stream link)
        {
            foreach (var (h, g) in Cfg.TcpForwards) AddForward(true, IPAddress.Any, h, g);
            foreach (var (h, g) in Cfg.UdpForwards) AddForward(false, IPAddress.Any, h, g);
            if (Cfg.HostDns.Count == 0)
                Cfg.HostDns.AddRange(NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up)
                    .SelectMany(n => n.GetIPProperties().DnsAddresses)
                    .Where(d => d.AddressFamily == AddressFamily.InterNetwork).Distinct());
            var writer = Task.Run(async () =>
            {
                var header = new byte[4];
                await foreach (var frame in toGuest.Reader.ReadAllAsync(stop.Token))
                {
                    BinaryPrimitives.WriteUInt32BigEndian(header, (uint)frame.Length);
                    await link.WriteAsync(header, stop.Token);
                    await link.WriteAsync(frame, stop.Token);
                }
            });
            var timers = Task.Run(Timers);
            try
            {
                var len = new byte[4];
                while (true)
                {
                    if (!await ReadExactly(link, len)) break;
                    var n = (int)BinaryPrimitives.ReadUInt32BigEndian(len);
                    if (n is < 14 or > 65550) break;
                    var frame = new byte[n];
                    if (!await ReadExactly(link, frame)) break;
                    lock (sync) OnFrame(frame);
                }
            }
            catch (IOException) { }
            finally
            {
                stop.Cancel();
                lock (sync)
                {
                    foreach (var c in tcp.Values) c.Host?.Dispose();
                    foreach (var f in udp.Values) f.Socket.Dispose();
                    foreach (var f in Forwards) f.Listener?.Dispose();
                    tcp.Clear();
                    udp.Clear();
                }
            }
            try { await Task.WhenAll(writer, timers); } catch (OperationCanceledException) { }
        }

        static async Task<bool> ReadExactly(Stream s, byte[] buffer)
        {
            var got = 0;
            while (got < buffer.Length)
            {
                var n = await s.ReadAsync(buffer.AsMemory(got));
                if (n == 0) return false;
                got += n;
            }
            return true;
        }

        void Log(string message)
        {
            if (Cfg.Verbose) Console.Error.WriteLine(message);
        }

        // ---- frames from the guest ----

        void OnFrame(byte[] f)
        {
            var type = BinaryPrimitives.ReadUInt16BigEndian(f.AsSpan(12));
            if (type == 0x0806) OnArp(f);
            else if (type == 0x0800 && f.Length >= 34)
            {
                var ip = f.AsSpan(14);
                var ihl = (ip[0] & 0xf) * 4;
                var total = BinaryPrimitives.ReadUInt16BigEndian(ip[2..]);
                if ((ip[0] >> 4) != 4 || ihl < 20 || total > ip.Length || total < ihl) return;
                if ((BinaryPrimitives.ReadUInt16BigEndian(ip[6..]) & 0x3fff) != 0) return;   // fragments aren't reassembled
                guestMac = f[6..12];
                var proto = ip[9];
                var src = BinaryPrimitives.ReadUInt32BigEndian(ip[12..]);
                var dst = BinaryPrimitives.ReadUInt32BigEndian(ip[16..]);
                var payload = f.AsSpan(14 + ihl, total - ihl);
                switch (proto)
                {
                    case 6 when Cfg.Tcp: OnTcp(src, dst, payload); break;
                    case 17 when Cfg.Udp: OnUdp(src, dst, payload); break;
                    case 1 when Cfg.Icmp: OnIcmp(dst, payload); break;
                }
            }
        }

        // Proxy ARP: every address but the guest's own is at our MAC.
        void OnArp(byte[] f)
        {
            if (f.Length < 42) return;
            var arp = f.AsSpan(14);
            if (BinaryPrimitives.ReadUInt16BigEndian(arp[6..]) != 1) return;   // request
            var target = BinaryPrimitives.ReadUInt32BigEndian(arp[24..]);
            guestMac = arp[8..14].ToArray();
            if (target == BinaryPrimitives.ReadUInt32BigEndian(arp[14..])) return;   // gratuitous
            var reply = new byte[42];
            guestMac.CopyTo(reply, 0);
            Cfg.OurMac.CopyTo(reply, 6);
            reply[12] = 0x08; reply[13] = 0x06;
            var r = reply.AsSpan(14);
            arp[..6].CopyTo(r);
            BinaryPrimitives.WriteUInt16BigEndian(r[6..], 2);
            Cfg.OurMac.CopyTo(r[8..]);
            BinaryPrimitives.WriteUInt32BigEndian(r[14..], target);
            arp[8..18].CopyTo(r[18..]);   // sender MAC and IP become the target
            Send(reply);
        }

        // ---- UDP, DHCP and DNS ----

        void OnUdp(uint src, uint dst, ReadOnlySpan<byte> u)
        {
            if (u.Length < 8) return;
            var sport = BinaryPrimitives.ReadUInt16BigEndian(u);
            var dport = BinaryPrimitives.ReadUInt16BigEndian(u[2..]);
            var data = u[8..Math.Clamp(BinaryPrimitives.ReadUInt16BigEndian(u[4..]), 8, u.Length)].ToArray();
            if (dport == 67 && Cfg.Dhcp) { OnDhcp(data); return; }
            if (dst == 0xffffffff || (dst >> 28) == 0xe) return;   // broadcast and multicast stay on the link
            var key = (sport, dst, dport);
            if (!udp.TryGetValue(key, out var flow))
            {
                var target = MapOutbound(dst, dport, udp: true);
                if (target == null) return;
                var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                if (Cfg.Outbound != null) s.Bind(new IPEndPoint(Cfg.Outbound, 0));
                else s.Bind(new IPEndPoint(IPAddress.Any, 0));
                s.Connect(target);
                flow = new UdpFlow { Socket = s, RemoteIp = dst, RemotePort = dport, GuestPort = sport };
                udp[key] = flow;
                _ = UdpReceive(flow, key);
            }
            flow.LastUsed = Environment.TickCount64;
            try
            {
                if (flow.LastPeer != null) flow.Socket.SendTo(data, flow.LastPeer);
                else flow.Socket.Send(data);
            }
            catch (SocketException e) { Log($"udp send: {e.Message}"); }
        }

        async Task UdpReceive(UdpFlow flow, (ushort, uint, ushort) key)
        {
            var buffer = new byte[65535];
            try
            {
                while (!stop.IsCancellationRequested)
                {
                    var r = await flow.Socket.ReceiveFromAsync(buffer, new IPEndPoint(IPAddress.Any, 0), stop.Token);
                    lock (sync)
                    {
                        flow.LastUsed = Environment.TickCount64;
                        if (flow.LastPeer != null) flow.LastPeer = (IPEndPoint)r.RemoteEndPoint;
                        SendUdp(flow.RemoteIp, flow.RemotePort, GuestIp, flow.GuestPort, buffer.AsSpan(0, r.ReceivedBytes));
                    }
                }
            }
            catch (Exception e) when (e is SocketException or OperationCanceledException or ObjectDisposedException) { }
            lock (sync) { udp.Remove(key); }
            flow.Socket.Dispose();
        }

        // Where a guest's packet to dst:port goes on the host: the gateway is the host itself, the
        // DNS address is the host's resolver, anything else is reached directly.
        IPEndPoint MapOutbound(uint dst, int port, bool udp)
        {
            if (dst == DnsIp && port == 53)
                return Cfg.HostDns.Count > 0 ? new IPEndPoint(Cfg.HostDns[0], 53) : null;
            if (dst == GatewayIp && Cfg.MapGateway) return new IPEndPoint(IPAddress.Loopback, port);
            if (dst == GuestIp) return null;
            return new IPEndPoint(new IPAddress(BinaryPrimitives.ReverseEndianness(dst)), port);
        }

        void OnDhcp(byte[] d)
        {
            if (d.Length < 240 || d[0] != 1 || BinaryPrimitives.ReadUInt32BigEndian(d.AsSpan(236)) != 0x63825363) return;
            int type = 0;
            for (int i = 240; i < d.Length && d[i] != 255;)
            {
                if (d[i] == 0) { i++; continue; }
                if (i + 1 >= d.Length) break;
                if (d[i] == 53 && d[i + 1] >= 1 && i + 2 < d.Length) type = d[i + 2];
                i += 2 + d[i + 1];
            }
            var reply = type switch { 1 => 2, 3 => 5, _ => 0 };   // DISCOVER -> OFFER, REQUEST -> ACK
            if (reply == 0) return;
            var o = new byte[300];
            o[0] = 2; o[1] = 1; o[2] = 6;
            d.AsSpan(4, 4).CopyTo(o.AsSpan(4));          // xid
            d.AsSpan(10, 2).CopyTo(o.AsSpan(10));        // flags
            BinaryPrimitives.WriteUInt32BigEndian(o.AsSpan(16), GuestIp);
            BinaryPrimitives.WriteUInt32BigEndian(o.AsSpan(20), GatewayIp);
            d.AsSpan(28, 16).CopyTo(o.AsSpan(28));       // chaddr
            BinaryPrimitives.WriteUInt32BigEndian(o.AsSpan(236), 0x63825363);
            var p = 240;
            void Opt(byte code, ReadOnlySpan<byte> value) { o[p++] = code; o[p++] = (byte)value.Length; value.CopyTo(o.AsSpan(p)); p += value.Length; }
            Span<byte> v = stackalloc byte[4];
            Opt(53, [(byte)reply]);
            BinaryPrimitives.WriteUInt32BigEndian(v, GatewayIp); Opt(54, v);
            BinaryPrimitives.WriteUInt32BigEndian(v, 86400); Opt(51, v);
            BinaryPrimitives.WriteUInt32BigEndian(v, Cfg.PrefixLength == 0 ? 0 : uint.MaxValue << (32 - Cfg.PrefixLength)); Opt(1, v);
            BinaryPrimitives.WriteUInt32BigEndian(v, GatewayIp); Opt(3, v);
            if (DnsIp != 0) { BinaryPrimitives.WriteUInt32BigEndian(v, DnsIp); Opt(6, v); }
            Span<byte> mtu = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(mtu, (ushort)Cfg.Mtu); Opt(26, mtu);
            o[p++] = 255;
            SendUdp(GatewayIp, 67, 0xffffffff, 68, o.AsSpan(0, Math.Max(p, 300)), broadcast: true);
            Log($"DHCP: {(reply == 2 ? "offer" : "ack")} {Cfg.GuestIp}");
        }

        void SendUdp(uint src, ushort sport, uint dst, ushort dport, ReadOnlySpan<byte> data, bool broadcast = false)
        {
            var udpLen = 8 + data.Length;
            var frame = NewIp(17, src, dst, udpLen, broadcast);
            var u = frame.AsSpan(34);
            BinaryPrimitives.WriteUInt16BigEndian(u, sport);
            BinaryPrimitives.WriteUInt16BigEndian(u[2..], dport);
            BinaryPrimitives.WriteUInt16BigEndian(u[4..], (ushort)udpLen);
            data.CopyTo(u[8..]);
            var sum = TransportChecksum(src, dst, 17, u[..udpLen]);
            BinaryPrimitives.WriteUInt16BigEndian(u[6..], sum == 0 ? (ushort)0xffff : sum);
            Send(frame);
        }

        // ---- ICMP echo through the host's ping ----

        void OnIcmp(uint dst, ReadOnlySpan<byte> icmp)
        {
            if (icmp.Length < 8 || icmp[0] != 8) return;
            var id = BinaryPrimitives.ReadUInt16BigEndian(icmp[4..]);
            var seq = BinaryPrimitives.ReadUInt16BigEndian(icmp[6..]);
            var data = icmp[8..].ToArray();
            var target = dst == GatewayIp && Cfg.MapGateway ? IPAddress.Loopback : new IPAddress(BinaryPrimitives.ReverseEndianness(dst));
            _ = Task.Run(async () =>
            {
                using var ping = new Ping();
                try
                {
                    var r = await ping.SendPingAsync(target, 4000, data.Length > 0 ? data : [0]);
                    if (r.Status != IPStatus.Success) return;
                    lock (sync)
                    {
                        var len = 8 + data.Length;
                        var frame = NewIp(1, dst, GuestIp, len);
                        var e = frame.AsSpan(34);
                        BinaryPrimitives.WriteUInt16BigEndian(e[4..], id);
                        BinaryPrimitives.WriteUInt16BigEndian(e[6..], seq);
                        data.CopyTo(e[8..]);
                        BinaryPrimitives.WriteUInt16BigEndian(e[2..], Checksum(e[..len], 0));
                        Send(frame);
                    }
                }
                catch (PingException) { }
            });
        }

        // ---- TCP ----

        void OnTcp(uint src, uint dst, ReadOnlySpan<byte> t)
        {
            if (t.Length < 20) return;
            var sport = BinaryPrimitives.ReadUInt16BigEndian(t);
            var dport = BinaryPrimitives.ReadUInt16BigEndian(t[2..]);
            var seq = BinaryPrimitives.ReadUInt32BigEndian(t[4..]);
            var ack = BinaryPrimitives.ReadUInt32BigEndian(t[8..]);
            var off = (t[12] >> 4) * 4;
            var flags = t[13];
            var window = BinaryPrimitives.ReadUInt16BigEndian(t[14..]);
            if (off < 20 || off > t.Length) return;
            var data = t[off..];
            bool syn = (flags & 0x02) != 0, fin = (flags & 0x01) != 0, rst = (flags & 0x04) != 0, hasAck = (flags & 0x10) != 0;
            var key = (sport, dst, dport);
            tcp.TryGetValue(key, out var c);

            if (rst)
            {
                if (c != null) Close(key, c, sendRst: false);
                return;
            }
            if (c == null)
            {
                if (!syn || hasAck)
                {
                    if (!syn) SendTcp(dst, dport, src, sport, hasAck ? ack : 0, seq + (uint)data.Length + (fin ? 1u : 0), 0x14, []);
                    return;
                }
                var target = MapOutbound(dst, dport, udp: false);
                if (target == null) { SendTcp(dst, dport, src, sport, 0, seq + 1, 0x14, []); return; }
                c = new TcpConn
                {
                    RemoteIp = dst, RemotePort = dport, GuestPort = sport, GuestNext = seq + 1, GuestWindow = window,
                    OurNext = (uint)Random.Shared.Next(), Mss = Math.Min(MssOption(t[20..off]) ?? 536, Cfg.Mtu - 40), GuestInitiated = true,
                };
                c.OurUnacked = c.OurNext;
                tcp[key] = c;
                _ = Connect(key, c, target);
                return;
            }
            if (syn)
            {
                // A retransmitted SYN, or the guest's SYN-ACK to a forwarded connection.
                if (c.GuestInitiated) { if (c.Host != null && !c.Established) SendSynAck(c); return; }
                if (hasAck && ack == c.OurNext && !c.Established)
                {
                    c.GuestNext = seq + 1;
                    c.GuestWindow = window;
                    c.Mss = Math.Min(MssOption(t[20..off]) ?? 536, Cfg.Mtu - 40);
                    c.Established = true;
                    c.OurUnacked = c.OurNext;
                    SendAck(c);
                    _ = Pump(key, c);
                    _ = HostWriter(key, c);
                }
                return;
            }
            if (hasAck)
            {
                if (!c.Established && c.GuestInitiated && ack == c.OurNext)
                {
                    c.Established = true;
                    c.OurUnacked = c.OurNext;
                    _ = Pump(key, c);
                    _ = HostWriter(key, c);
                }
                var acked = (int)(ack - c.OurUnacked);
                if (acked > 0 && acked <= c.Unacked.Count + (c.OurFinSent ? 1 : 0))
                {
                    c.Unacked.RemoveRange(0, Math.Min(acked, c.Unacked.Count));
                    c.OurUnacked = ack;
                }
                c.GuestWindow = window;
                c.WindowOpen.Release();
            }
            if (data.Length > 0 && c.Established)
            {
                if (seq == c.GuestNext)
                {
                    c.ToHost.Writer.TryWrite(data.ToArray());
                    c.GuestNext += (uint)data.Length;
                }
                SendAck(c);   // in order, retransmitted or ahead: the ACK says what we have
            }
            if (fin && c.Established && seq + (uint)data.Length == c.GuestNext)
            {
                c.GuestNext++;
                c.GuestFinSeen = true;
                c.ToHost.Writer.TryComplete();
                SendAck(c);
            }
            if (c.GuestFinSeen && c.OurFinSent && c.OurUnacked == c.OurNext) Close(key, c, sendRst: false);
        }

        static int? MssOption(ReadOnlySpan<byte> options)
        {
            for (int i = 0; i < options.Length;)
            {
                if (options[i] == 0) break;
                if (options[i] == 1) { i++; continue; }
                if (i + 1 >= options.Length || options[i + 1] < 2) break;
                if (options[i] == 2 && options[i + 1] == 4 && i + 3 < options.Length) return BinaryPrimitives.ReadUInt16BigEndian(options[(i + 2)..]);
                i += options[i + 1];
            }
            return null;
        }

        async Task Connect((ushort, uint, ushort) key, TcpConn c, IPEndPoint target)
        {
            var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                if (Cfg.Outbound != null) s.Bind(new IPEndPoint(Cfg.Outbound, 0));
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                using var both = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, stop.Token);
                await s.ConnectAsync(target, both.Token);
            }
            catch (Exception e) when (e is SocketException or OperationCanceledException)
            {
                s.Dispose();
                lock (sync)
                {
                    Log($"tcp connect {target}: {e.Message}");
                    if (tcp.Remove(key)) SendTcp(c.RemoteIp, c.RemotePort, GuestIp, c.GuestPort, 0, c.GuestNext, 0x14, []);
                }
                return;
            }
            lock (sync)
            {
                if (!tcp.ContainsKey(key)) { s.Dispose(); return; }
                c.Host = s;
                SendSynAck(c);
            }
        }

        void SendSynAck(TcpConn c)
        {
            Span<byte> mss = [2, 4, 0, 0];
            BinaryPrimitives.WriteUInt16BigEndian(mss[2..], (ushort)(Cfg.Mtu - 40));
            SendTcp(c.RemoteIp, c.RemotePort, GuestIp, c.GuestPort, c.OurUnacked, c.GuestNext, 0x12, [], mss);
            c.OurNext = c.OurUnacked + 1;
            c.LastSend = Environment.TickCount64;
        }

        void SendAck(TcpConn c) => SendTcp(c.RemoteIp, c.RemotePort, GuestIp, c.GuestPort, c.OurNext, c.GuestNext, 0x10, []);

        // Guest -> host: the guest's bytes in order, then a half-close when it sent FIN.
        async Task HostWriter((ushort, uint, ushort) key, TcpConn c)
        {
            try
            {
                await foreach (var chunk in c.ToHost.Reader.ReadAllAsync(stop.Token)) await c.Host.SendAsync(chunk, stop.Token);
                c.Host.Shutdown(SocketShutdown.Send);
            }
            catch (Exception e) when (e is SocketException or OperationCanceledException or ObjectDisposedException)
            {
                lock (sync) { if (tcp.ContainsKey(key)) Close(key, c, sendRst: true); }
            }
        }

        // Host -> guest: read what fits the guest's window, send it in MSS segments, FIN at the end.
        async Task Pump((ushort, uint, ushort) key, TcpConn c)
        {
            var buffer = new byte[65536];
            try
            {
                while (!stop.IsCancellationRequested)
                {
                    int room;
                    lock (sync) room = c.GuestWindow - c.Unacked.Count;
                    if (room <= 0) { await c.WindowOpen.WaitAsync(200, stop.Token); continue; }
                    var n = await c.Host.ReceiveAsync(buffer.AsMemory(0, Math.Min(room, buffer.Length)), stop.Token);
                    lock (sync)
                    {
                        if (!tcp.ContainsKey(key)) return;
                        if (n == 0)
                        {
                            SendTcp(c.RemoteIp, c.RemotePort, GuestIp, c.GuestPort, c.OurNext, c.GuestNext, 0x11, []);
                            c.OurNext++;
                            c.OurFinSent = true;
                            c.LastSend = Environment.TickCount64;
                            return;
                        }
                        for (int at = 0; at < n; at += c.Mss)
                        {
                            var len = Math.Min(c.Mss, n - at);
                            SendTcp(c.RemoteIp, c.RemotePort, GuestIp, c.GuestPort, c.OurNext, c.GuestNext, 0x18, buffer.AsSpan(at, len));
                            c.OurNext += (uint)len;
                        }
                        c.Unacked.AddRange(buffer.AsSpan(0, n).ToArray());
                        c.LastSend = Environment.TickCount64;
                    }
                }
            }
            catch (Exception e) when (e is SocketException or OperationCanceledException or ObjectDisposedException)
            {
                lock (sync) { if (tcp.ContainsKey(key)) Close(key, c, sendRst: true); }
            }
        }

        void Close((ushort, uint, ushort) key, TcpConn c, bool sendRst)
        {
            if (sendRst) SendTcp(c.RemoteIp, c.RemotePort, GuestIp, c.GuestPort, c.OurNext, c.GuestNext, 0x14, []);
            tcp.Remove(key);
            c.Closed = true;
            c.ToHost.Writer.TryComplete();
            try { c.Host?.Dispose(); } catch (ObjectDisposedException) { }
        }

        // Retransmission (unacknowledged data or FIN after 1 s) and idle UDP flows (60 s).
        async Task Timers()
        {
            while (!stop.IsCancellationRequested)
            {
                try { await Task.Delay(250, stop.Token); } catch (OperationCanceledException) { return; }
                lock (sync)
                {
                    var now = Environment.TickCount64;
                    foreach (var c in tcp.Values.ToList())
                    {
                        if (now - c.LastSend < 1000) continue;
                        if (!c.Established)
                        {
                            // Handshakes: our SYN-ACK (guest's connection) or SYN (forwarded one) again.
                            if (c.GuestInitiated && c.Host != null) SendSynAck(c);
                            else if (!c.GuestInitiated)
                            {
                                Span<byte> mss = [2, 4, 0, 0];
                                BinaryPrimitives.WriteUInt16BigEndian(mss[2..], (ushort)(Cfg.Mtu - 40));
                                SendTcp(c.RemoteIp, c.RemotePort, GuestIp, c.GuestPort, c.OurUnacked, 0, 0x02, [], mss);
                            }
                            c.LastSend = now;
                            continue;
                        }
                        if (c.Unacked.Count > 0)
                        {
                            var len = Math.Min(c.Mss, c.Unacked.Count);
                            SendTcp(c.RemoteIp, c.RemotePort, GuestIp, c.GuestPort, c.OurUnacked, c.GuestNext, 0x18, c.Unacked.GetRange(0, len).ToArray());
                            c.LastSend = now;
                        }
                        else if (c.OurFinSent && c.OurUnacked != c.OurNext)
                        {
                            SendTcp(c.RemoteIp, c.RemotePort, GuestIp, c.GuestPort, c.OurNext - 1, c.GuestNext, 0x11, []);
                            c.LastSend = now;
                        }
                    }
                    foreach (var (key, f) in udp.ToList())
                        if (f.LastPeer == null && now - f.LastUsed > 60_000) { udp.Remove(key); f.Socket.Dispose(); }
                }
            }
        }

        // ---- port forwarding into the guest ----

        public int AddForward(bool isTcp, IPAddress hostAddr, int hostPort, int guestPort)
        {
            lock (sync)
            {
                var fwd = new Forward { Id = nextForwardId++, Tcp = isTcp, HostAddr = hostAddr, HostPort = hostPort, GuestPort = guestPort };
                if (isTcp)
                {
                    var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    listener.Bind(new IPEndPoint(hostAddr, hostPort));
                    listener.Listen(16);
                    fwd.Listener = listener;
                    _ = AcceptLoop(listener, guestPort);
                }
                else
                {
                    var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    s.Bind(new IPEndPoint(hostAddr, hostPort));
                    fwd.Listener = s;
                    var port = (ushort)nextEphemeral++;
                    var flow = new UdpFlow { Socket = s, RemoteIp = GatewayIp, RemotePort = port, GuestPort = (ushort)guestPort, LastPeer = new IPEndPoint(IPAddress.Any, 0) };
                    var key = ((ushort)guestPort, GatewayIp, port);
                    udp[key] = flow;
                    _ = UdpReceive(flow, key);
                }
                Forwards.Add(fwd);
                return fwd.Id;
            }
        }

        public bool RemoveForward(int id)
        {
            lock (sync)
            {
                var f = Forwards.FirstOrDefault(x => x.Id == id);
                if (f == null) return false;
                f.Listener?.Dispose();
                Forwards.Remove(f);
                return true;
            }
        }

        async Task AcceptLoop(Socket listener, int guestPort)
        {
            while (!stop.IsCancellationRequested)
            {
                Socket client;
                try { client = await listener.AcceptAsync(stop.Token); }
                catch (Exception e) when (e is SocketException or OperationCanceledException or ObjectDisposedException) { return; }
                lock (sync)
                {
                    // The guest sees the connection come from the gateway address.
                    var port = (ushort)(nextEphemeral++ % 16384 + 49152);
                    var c = new TcpConn { RemoteIp = GatewayIp, RemotePort = port, GuestPort = (ushort)guestPort, Host = client, OurNext = (uint)Random.Shared.Next() };
                    c.OurUnacked = c.OurNext;
                    var key = ((ushort)guestPort, GatewayIp, port);
                    tcp[key] = c;
                    Span<byte> mss = [2, 4, 0, 0];
                    BinaryPrimitives.WriteUInt16BigEndian(mss[2..], (ushort)(Cfg.Mtu - 40));
                    SendTcp(GatewayIp, port, GuestIp, (ushort)guestPort, c.OurNext, 0, 0x02, [], mss);
                    c.OurNext++;
                    c.LastSend = Environment.TickCount64;
                }
            }
        }

        // ---- frame building ----

        byte[] NewIp(byte proto, uint src, uint dst, int payloadLength, bool broadcast = false)
        {
            var frame = new byte[34 + payloadLength];
            (broadcast ? [0xff, 0xff, 0xff, 0xff, 0xff, 0xff] : guestMac).CopyTo(frame, 0);
            Cfg.OurMac.CopyTo(frame, 6);
            frame[12] = 0x08;
            var ip = frame.AsSpan(14);
            ip[0] = 0x45;
            BinaryPrimitives.WriteUInt16BigEndian(ip[2..], (ushort)(20 + payloadLength));
            BinaryPrimitives.WriteUInt16BigEndian(ip[4..], ipId++);
            ip[6] = proto == 6 ? (byte)0x40 : (byte)0;   // DF for TCP
            ip[8] = 64;
            ip[9] = proto;
            BinaryPrimitives.WriteUInt32BigEndian(ip[12..], src);
            BinaryPrimitives.WriteUInt32BigEndian(ip[16..], dst);
            BinaryPrimitives.WriteUInt16BigEndian(ip[10..], Checksum(ip[..20], 0));
            return frame;
        }

        void SendTcp(uint src, ushort sport, uint dst, ushort dport, uint seq, uint ack, byte flags, ReadOnlySpan<byte> data, ReadOnlySpan<byte> options = default)
        {
            var hdr = 20 + options.Length;
            var frame = NewIp(6, src, dst, hdr + data.Length);
            var t = frame.AsSpan(34);
            BinaryPrimitives.WriteUInt16BigEndian(t, sport);
            BinaryPrimitives.WriteUInt16BigEndian(t[2..], dport);
            BinaryPrimitives.WriteUInt32BigEndian(t[4..], seq);
            BinaryPrimitives.WriteUInt32BigEndian(t[8..], ack);
            t[12] = (byte)((hdr / 4) << 4);
            t[13] = flags;
            BinaryPrimitives.WriteUInt16BigEndian(t[14..], 65535);
            options.CopyTo(t[20..]);
            data.CopyTo(t[hdr..]);
            BinaryPrimitives.WriteUInt16BigEndian(t[16..], TransportChecksum(src, dst, 6, t[..(hdr + data.Length)]));
            Send(frame);
        }

        void Send(byte[] frame) => toGuest.Writer.TryWrite(frame);
    }

    static uint U32(IPAddress ip) => BinaryPrimitives.ReadUInt32BigEndian(ip.GetAddressBytes());

    // The Internet checksum (RFC 1071), continuing from a partial sum.
    static ushort Checksum(ReadOnlySpan<byte> data, uint sum)
    {
        int i = 0;
        for (; i + 1 < data.Length; i += 2) sum += BinaryPrimitives.ReadUInt16BigEndian(data[i..]);
        if (i < data.Length) sum += (uint)data[i] << 8;
        while (sum >> 16 != 0) sum = (sum & 0xffff) + (sum >> 16);
        return (ushort)~sum;
    }

    // TCP/UDP checksum over the IPv4 pseudo-header and the segment (whose checksum field is zero).
    static ushort TransportChecksum(uint src, uint dst, byte proto, ReadOnlySpan<byte> segment)
    {
        uint sum = (src >> 16) + (src & 0xffff) + (dst >> 16) + (dst & 0xffff) + proto + (uint)segment.Length;
        return Checksum(segment, sum);
    }
}

