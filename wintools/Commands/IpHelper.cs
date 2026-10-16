using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// Shared IP Helper plumbing for the iproute2 / net-tools / iputils commands (ip, ss, bridge, ifconfig,
// netstat, route, arp, ping, tracepath, arping). Interfaces come from System.Net.NetworkInformation;
// routes, neighbours and sockets from iphlpapi tables read by offset; changes go through netsh.
static unsafe class IpHelper
{
    internal const ushort AF_UNSPEC = 0, AF_INET = 2, AF_INET6 = 23;

    internal sealed class Iface
    {
        public int Index;
        public string Name, Description;
        public NetworkInterfaceType Type;
        public OperationalStatus Status;
        public byte[] Mac = [];
        public long Mtu;
        public uint Metric4, Metric6;
        public bool Multicast, Dhcp4;
        public List<IPAddress> Gateways = [];
        public NetworkInterface Nic;
        public List<IfAddr> Addrs = [];
        public bool Up => Status == OperationalStatus.Up;
        public bool Loopback => Type == NetworkInterfaceType.Loopback;
        public bool PointToPoint => Type is NetworkInterfaceType.Ppp or NetworkInterfaceType.Tunnel
                                    || (Mac.Length == 0 && !Loopback);
    }

    internal sealed class IfAddr
    {
        public IPAddress Address;
        public int Prefix;
        public long Valid, Preferred;
        public bool Dynamic, Temporary, Deprecated, Tentative;
        public bool V6 => Address.AddressFamily == AddressFamily.InterNetworkV6;
    }

    internal sealed class Route
    {
        public IPAddress Dest, Gateway;   // Gateway is null for on-link routes
        public int Prefix, IfIndex, Protocol, Origin;
        public uint Metric;
        public bool Loopback;
        public bool V6 => Dest.AddressFamily == AddressFamily.InterNetworkV6;
    }

    internal sealed class Neighbor
    {
        public IPAddress Address;
        public int IfIndex, State;
        public byte[] Mac;
        public bool IsRouter;
    }

    internal sealed class Sock
    {
        public bool Tcp, V6;
        public IPAddress Local, Remote;
        public int LocalPort, RemotePort, State, Pid;
    }

    // ---- interfaces ----

    internal static List<Iface> Interfaces()
    {
        var result = new List<Iface>();
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            var props = nic.GetIPProperties();
            var iface = new Iface
            {
                Name = nic.Name, Description = nic.Description, Type = nic.NetworkInterfaceType,
                Status = nic.OperationalStatus, Nic = nic, Multicast = nic.SupportsMulticast,
            };
            if (nic.Supports(NetworkInterfaceComponent.IPv4))
                try { var v4 = props.GetIPv4Properties(); iface.Index = v4.Index; iface.Mtu = v4.Mtu; iface.Dhcp4 = v4.IsDhcpEnabled; }
                catch (NetworkInformationException) { }
            if (nic.Supports(NetworkInterfaceComponent.IPv6))
                try
                {
                    var v6 = props.GetIPv6Properties();
                    if (iface.Index == 0) iface.Index = v6.Index;
                    if (iface.Mtu <= 0) iface.Mtu = v6.Mtu;
                }
                catch (NetworkInformationException) { }
            if (iface.Index == 0) continue;
            // The loopback pseudo-interface reports an MTU of 0xFFFFFFFF; Linux's lo says 65536.
            if (iface.Loopback || iface.Mtu <= 0 || iface.Mtu > 65536) iface.Mtu = iface.Loopback ? 65536 : Math.Max(iface.Mtu, 0);
            iface.Mac = nic.GetPhysicalAddress().GetAddressBytes();
            iface.Metric4 = InterfaceMetric(AF_INET, iface.Index);
            iface.Metric6 = InterfaceMetric(AF_INET6, iface.Index);
            foreach (var ua in props.UnicastAddresses)
            {
                iface.Addrs.Add(new IfAddr
                {
                    Address = Unscoped(ua.Address),
                    Prefix = ua.PrefixLength,
                    Valid = ua.AddressValidLifetime,
                    Preferred = ua.AddressPreferredLifetime,
                    Dynamic = ua.PrefixOrigin is PrefixOrigin.Dhcp or PrefixOrigin.RouterAdvertisement,
                    Temporary = ua.SuffixOrigin == SuffixOrigin.Random,
                    Deprecated = ua.DuplicateAddressDetectionState == DuplicateAddressDetectionState.Deprecated,
                    Tentative = ua.DuplicateAddressDetectionState is DuplicateAddressDetectionState.Tentative
                                or DuplicateAddressDetectionState.Duplicate,
                });
            }
            iface.Addrs = iface.Addrs.OrderBy(a => a.V6).ToList();   // inet before inet6, as Linux lists them
            iface.Gateways = props.GatewayAddresses.Select(g => Unscoped(g.Address)).ToList();
            result.Add(iface);
        }
        result.Sort((a, b) => a.Index.CompareTo(b.Index));
        return result;
    }

    // An interface named exactly (case-insensitively) or by its index.
    internal static Iface Find(List<Iface> all, string arg) =>
        all.FirstOrDefault(i => i.Name.Equals(arg, StringComparison.OrdinalIgnoreCase))
        ?? (int.TryParse(arg, out var n) ? all.FirstOrDefault(i => i.Index == n) : null);

    internal static Iface Require(List<Iface> all, string arg) =>
        Find(all, arg) ?? throw new InvalidOperationException($"Device \"{arg}\" does not exist.");

    internal static string IfName(Dictionary<int, Iface> byIndex, int index) =>
        byIndex.TryGetValue(index, out var i) ? i.Name : $"if{index}";

    // Interface metric (what Windows adds to every route's own metric), via GetIpInterfaceEntry.
    static uint InterfaceMetric(ushort family, int index)
    {
        var row = stackalloc byte[256];
        new Span<byte>(row, 256).Clear();
        *(ushort*)row = family;
        *(int*)(row + 16) = index;
        return GetIpInterfaceEntry(row) == 0 ? *(uint*)(row + 148) : 0;
    }

    // ---- routes and neighbours ----

    internal static List<Route> Routes(ushort family)
    {
        var list = new List<Route>();
        if (GetIpForwardTable2(family, out var table) != 0) return list;
        try
        {
            var count = *(int*)table;
            for (var i = 0; i < count; i++) list.Add(ReadRoute((byte*)table + 8 + i * 104));
        }
        finally { FreeMibTable(table); }
        return list;
    }

    // MIB_IPFORWARD_ROW2: InterfaceIndex @8, DestinationPrefix @12 (prefix length @40), NextHop @44,
    // Metric @84, Protocol @88, Loopback @92, Origin @100; 104 bytes.
    static Route ReadRoute(byte* row)
    {
        var gateway = ReadSockaddr(row + 44);
        if (gateway != null && (gateway.Equals(IPAddress.Any) || gateway.Equals(IPAddress.IPv6Any))) gateway = null;
        return new Route
        {
            IfIndex = *(int*)(row + 8),
            Dest = ReadSockaddr(row + 12) ?? IPAddress.Any,
            Prefix = row[40],
            Gateway = gateway,
            Metric = *(uint*)(row + 84),
            Protocol = *(int*)(row + 88),
            Loopback = row[92] != 0,
            Origin = *(int*)(row + 100),
        };
    }

    // The route Windows would use to reach dest, and the source address it would pick.
    internal static (Route route, IPAddress source) BestRoute(IPAddress dest)
    {
        var dst = stackalloc byte[28];
        var row = stackalloc byte[104];
        var src = stackalloc byte[28];
        WriteSockaddr(dst, dest);
        if (GetBestRoute2(IntPtr.Zero, 0, null, dst, 0, row, src) != 0) return (null, null);
        return (ReadRoute(row), ReadSockaddr(src));
    }

    internal static List<Neighbor> Neighbors(ushort family)
    {
        var list = new List<Neighbor>();
        if (GetIpNetTable2(family, out var table) != 0) return list;
        try
        {
            var count = *(int*)table;
            for (var i = 0; i < count; i++)
            {
                // MIB_IPNET_ROW2: Address @0, InterfaceIndex @28, PhysicalAddress @40, its length @72,
                // State @76, Flags @80 (bit 0 IsRouter); 88 bytes.
                var row = (byte*)table + 8 + i * 88;
                var macLength = Math.Min(*(int*)(row + 72), 32);
                list.Add(new Neighbor
                {
                    Address = ReadSockaddr(row),
                    IfIndex = *(int*)(row + 28),
                    Mac = new ReadOnlySpan<byte>(row + 40, macLength).ToArray(),
                    State = *(int*)(row + 76),
                    IsRouter = (row[80] & 1) != 0,
                });
            }
        }
        finally { FreeMibTable(table); }
        return list.Where(n => n.Address != null).ToList();
    }

    // NL_NEIGHBOR_STATE as Linux names it.
    internal static string NeighborState(int state) => state switch
    {
        0 => "FAILED", 1 => "INCOMPLETE", 2 => "PROBE", 3 => "DELAY", 4 => "STALE", 5 => "REACHABLE",
        6 => "PERMANENT", _ => "NONE",
    };

    // Multicast and broadcast entries Windows keeps in its neighbour cache, which Linux doesn't list.
    internal static bool IsNoiseNeighbor(Neighbor n, Dictionary<int, Iface> byIndex)
    {
        var a = n.Address;
        if (a.AddressFamily == AddressFamily.InterNetworkV6) return a.IsIPv6Multicast;
        var b = a.GetAddressBytes();
        if (b[0] >= 224) return true;
        return byIndex.TryGetValue(n.IfIndex, out var iface)
               && (iface.Loopback || iface.Addrs.Any(x => !x.V6 && x.Prefix < 31 && Broadcast(x.Address, x.Prefix).Equals(a)));
    }

    // ---- sockets ----

    internal static List<Sock> Sockets(bool tcp, bool udp, bool v4, bool v6)
    {
        var list = new List<Sock>();
        if (tcp && v4) ReadSocketTable(true, AF_INET, list);
        if (tcp && v6) ReadSocketTable(true, AF_INET6, list);
        if (udp && v4) ReadSocketTable(false, AF_INET, list);
        if (udp && v6) ReadSocketTable(false, AF_INET6, list);
        return list;
    }

    static void ReadSocketTable(bool tcp, ushort af, List<Sock> into)
    {
        var size = 0;
        // TCP_TABLE_OWNER_PID_ALL = 5, UDP_TABLE_OWNER_PID = 1; ERROR_INSUFFICIENT_BUFFER = 122.
        _ = tcp ? GetExtendedTcpTable(IntPtr.Zero, ref size, true, af, 5, 0) : GetExtendedUdpTable(IntPtr.Zero, ref size, true, af, 1, 0);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var buffer = Marshal.AllocHGlobal(size + 1024);
            size += 1024;
            try
            {
                var rc = tcp ? GetExtendedTcpTable(buffer, ref size, true, af, 5, 0) : GetExtendedUdpTable(buffer, ref size, true, af, 1, 0);
                if (rc == 122) continue;
                if (rc != 0) return;
                var p = (byte*)buffer;
                var count = *(int*)p;
                for (var i = 0; i < count; i++)
                {
                    if (tcp && af == AF_INET)
                    {
                        // MIB_TCPROW_OWNER_PID: state, local addr, local port, remote addr, remote port, pid.
                        var row = p + 4 + i * 24;
                        into.Add(new Sock
                        {
                            Tcp = true, State = *(int*)row,
                            Local = new IPAddress(new ReadOnlySpan<byte>(row + 4, 4)), LocalPort = Port(row + 8),
                            Remote = new IPAddress(new ReadOnlySpan<byte>(row + 12, 4)), RemotePort = Port(row + 16),
                            Pid = *(int*)(row + 20),
                        });
                    }
                    else if (tcp)
                    {
                        // MIB_TCP6ROW_OWNER_PID: local addr[16], scope, port, remote addr[16], scope, port, state, pid.
                        var row = p + 4 + i * 56;
                        into.Add(new Sock
                        {
                            Tcp = true, V6 = true, State = *(int*)(row + 48),
                            Local = new IPAddress(new ReadOnlySpan<byte>(row, 16), *(uint*)(row + 16)), LocalPort = Port(row + 20),
                            Remote = new IPAddress(new ReadOnlySpan<byte>(row + 24, 16), *(uint*)(row + 40)), RemotePort = Port(row + 44),
                            Pid = *(int*)(row + 52),
                        });
                    }
                    else if (af == AF_INET)
                    {
                        var row = p + 4 + i * 12;
                        into.Add(new Sock
                        {
                            Local = new IPAddress(new ReadOnlySpan<byte>(row, 4)), LocalPort = Port(row + 4),
                            Remote = IPAddress.Any, Pid = *(int*)(row + 8),
                        });
                    }
                    else
                    {
                        var row = p + 4 + i * 28;
                        into.Add(new Sock
                        {
                            V6 = true,
                            Local = new IPAddress(new ReadOnlySpan<byte>(row, 16), *(uint*)(row + 16)), LocalPort = Port(row + 20),
                            Remote = IPAddress.IPv6Any, Pid = *(int*)(row + 24),
                        });
                    }
                }
                return;
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }
    }

    // Ports sit in network byte order in the low 16 bits of a DWORD.
    static int Port(byte* p) => (p[0] << 8) | p[1];

    internal const int TcpListen = 2;

    internal static Dictionary<int, string> ProcessNames()
    {
        var names = new Dictionary<int, string>();
        foreach (var process in Process.GetProcesses())
        {
            try { names[process.Id] = process.ProcessName; }
            catch (InvalidOperationException) { }
            finally { process.Dispose(); }
        }
        return names;
    }

    static Dictionary<(int, bool), string> services;

    // Service name for a port from %SystemRoot%\System32\drivers\etc\services, like getservbyport.
    internal static string Service(int port, bool tcp)
    {
        if (services == null)
        {
            services = [];
            try
            {
                foreach (var raw in File.ReadLines(Path.Combine(Environment.SystemDirectory, @"drivers\etc\services")))
                {
                    var line = raw.Split('#')[0];
                    var parts = line.Split((char[])[' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2) continue;
                    var slash = parts[1].Split('/');
                    if (slash.Length == 2 && int.TryParse(slash[0], out var p))
                        services.TryAdd((p, slash[1].Equals("tcp", StringComparison.OrdinalIgnoreCase)), parts[0]);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        return services.TryGetValue((port, tcp), out var name) ? name : port.ToString();
    }

    // ---- addresses ----

    internal static IPAddress Unscoped(IPAddress a) =>
        a.AddressFamily == AddressFamily.InterNetworkV6 && a.ScopeId != 0 ? new IPAddress(a.GetAddressBytes()) : a;

    internal static string Mac(byte[] mac) => string.Join(":", mac.Select(b => b.ToString("x2")));

    internal static IPAddress Mask(int prefix)
    {
        var bits = prefix == 0 ? 0u : uint.MaxValue << (32 - prefix);
        return new IPAddress([(byte)(bits >> 24), (byte)(bits >> 16), (byte)(bits >> 8), (byte)bits]);
    }

    internal static int MaskToPrefix(IPAddress mask)
    {
        var bits = mask.GetAddressBytes().Aggregate(0u, (acc, b) => (acc << 8) | b);
        var prefix = System.Numerics.BitOperations.PopCount(bits);
        return Mask(prefix).Equals(mask) ? prefix : throw new ArgumentException($"invalid netmask {mask}");
    }

    internal static IPAddress Network(IPAddress a, int prefix)
    {
        var bytes = a.GetAddressBytes();
        for (var i = 0; i < bytes.Length; i++)
        {
            var keep = Math.Clamp(prefix - i * 8, 0, 8);
            bytes[i] &= (byte)(0xFF << (8 - keep));
        }
        return new IPAddress(bytes);
    }

    internal static IPAddress Broadcast(IPAddress a, int prefix)
    {
        var bytes = a.GetAddressBytes();
        for (var i = 0; i < bytes.Length; i++)
        {
            var keep = Math.Clamp(prefix - i * 8, 0, 8);
            bytes[i] |= (byte)~(0xFF << (8 - keep));
        }
        return new IPAddress(bytes);
    }

    internal static bool InPrefix(IPAddress a, IPAddress net, int prefix) =>
        a.AddressFamily == net.AddressFamily && Network(a, prefix).Equals(Network(net, prefix));

    // "10.0.0.0/8", "10.1.2.3" (a host), or "default"/"all"/"any" (family taken from v6).
    internal static (IPAddress address, int prefix) ParsePrefix(string text, bool v6)
    {
        if (text is "default" or "all" or "any") return (v6 ? IPAddress.IPv6Any : IPAddress.Any, 0);
        var slash = text.IndexOf('/');
        if (!IPAddress.TryParse(slash < 0 ? text : text[..slash], out var address))
            throw new UsageException($"invalid prefix \"{text}\"");
        var max = address.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32;
        var prefix = max;
        if (slash >= 0 && (!int.TryParse(text[(slash + 1)..], out prefix) || prefix < 0 || prefix > max))
            throw new UsageException($"invalid prefix length in \"{text}\"");
        return (address, prefix);
    }

    internal static byte[] ParseMac(string text)
    {
        var parts = text.Split(':', '-');
        if (parts.Length != 6) throw new UsageException($"invalid MAC address \"{text}\"");
        return parts.Select(p => byte.TryParse(p, System.Globalization.NumberStyles.HexNumber, null, out var b)
            ? b : throw new UsageException($"invalid MAC address \"{text}\"")).ToArray();
    }

    // ---- netsh ----

    internal static string Quote(string s) => "\"" + s.Replace("\"", "") + "\"";

    // Runs netsh with an already-quoted argument string; on failure its message goes to stderr.
    internal static int Netsh(string arguments)
    {
        var (code, output) = Capture("netsh", arguments);
        if (code == 0) return 0;
        Console.Error.WriteLine("netsh " + arguments + ": " + output.Trim());
        return 1;
    }

    internal static (int code, string output) Capture(string file, string arguments)
    {
        var psi = new ProcessStartInfo(file, arguments)
        {
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true,
        };
        using var process = Process.Start(psi) ?? throw new InvalidOperationException($"could not start {file}");
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output + error.Result);
    }

    // ---- JSON (hand-written; no reflection) ----

    internal sealed record Raw(string Text);

    internal static Raw Obj(params (string key, object value)[] fields) =>
        new("{" + string.Join(",", fields.Where(f => f.value != null).Select(f => Js(f.key) + ":" + Js(f.value))) + "}");

    internal static string Js(object value) => value switch
    {
        null => "null",
        string s => "\"" + Escape(s) + "\"",
        bool b => b ? "true" : "false",
        Raw r => r.Text,
        IEnumerable<string> list => "[" + string.Join(",", list.Select(Js)) + "]",
        IEnumerable<Raw> list => "[" + string.Join(",", list.Select(r => r.Text)) + "]",
        IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
        _ => Js(value.ToString()),
    };

    static string Escape(string s)
    {
        var sb = new StringBuilder();
        foreach (var c in s)
        {
            if (c is '"' or '\\') sb.Append('\\').Append(c);
            else if (c < ' ') sb.Append($"\\u{(int)c:x4}");
            else sb.Append(c);
        }
        return sb.ToString();
    }

    // ---- sockaddr ----

    internal static IPAddress ReadSockaddr(byte* p)
    {
        var family = *(ushort*)p;
        if (family == AF_INET) return new IPAddress(new ReadOnlySpan<byte>(p + 4, 4));
        if (family == AF_INET6) return new IPAddress(new ReadOnlySpan<byte>(p + 8, 16));
        return null;
    }

    static void WriteSockaddr(byte* p, IPAddress a)
    {
        new Span<byte>(p, 28).Clear();
        var bytes = a.GetAddressBytes();
        if (a.AddressFamily == AddressFamily.InterNetwork)
        {
            *(ushort*)p = AF_INET;
            bytes.CopyTo(new Span<byte>(p + 4, 4));
        }
        else
        {
            *(ushort*)p = AF_INET6;
            bytes.CopyTo(new Span<byte>(p + 8, 16));
            *(uint*)(p + 24) = (uint)a.ScopeId;
        }
    }

    [DllImport("iphlpapi.dll")]
    static extern int GetIpForwardTable2(ushort family, out IntPtr table);

    [DllImport("iphlpapi.dll")]
    static extern int GetIpNetTable2(ushort family, out IntPtr table);

    [DllImport("iphlpapi.dll")]
    static extern void FreeMibTable(IntPtr table);

    [DllImport("iphlpapi.dll")]
    static extern int GetIpInterfaceEntry(byte* row);

    [DllImport("iphlpapi.dll")]
    static extern int GetBestRoute2(IntPtr interfaceLuid, int interfaceIndex, byte* source, byte* destination,
                                    uint sortOptions, byte* bestRoute, byte* bestSource);

    [DllImport("iphlpapi.dll")]
    static extern uint GetExtendedTcpTable(IntPtr table, ref int size, [MarshalAs(UnmanagedType.Bool)] bool order,
                                           int family, int tableClass, int reserved);

    [DllImport("iphlpapi.dll")]
    static extern uint GetExtendedUdpTable(IntPtr table, ref int size, [MarshalAs(UnmanagedType.Bool)] bool order,
                                           int family, int tableClass, int reserved);

    [DllImport("iphlpapi.dll")]
    internal static extern int SendARP(uint destination, uint source, byte[] mac, ref int length);
}
