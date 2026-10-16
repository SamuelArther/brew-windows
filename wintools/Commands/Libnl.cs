using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WinTools;

// libnl's command-line tools (nl-link-list, nl-addr-list, nl-route-list, nl-neigh-list, nl-monitor)
// on the IP Helper API, which is what Windows has in place of rtnetlink: links, addresses, routes and
// neighbours are read through the shared IpHelper tables and printed in libnl's one-line dump format;
// nl-monitor subscribes to IP Helper's change notifications (NotifyIpInterfaceChange,
// NotifyUnicastIpAddressChange, NotifyRouteChange2), Windows' counterpart of rtnetlink multicast groups.
static unsafe class Libnl
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("nl-link-list", "List network links (libnl, on the IP Helper API)", LinkList);
        Commands.Add("nl-addr-list", "List network addresses (libnl, on the IP Helper API)", AddrList);
        Commands.Add("nl-route-list", "List routes (libnl, on the IP Helper API)", RouteList);
        Commands.Add("nl-neigh-list", "List neighbour (ARP/NDP) cache entries (libnl, on the IP Helper API)", NeighList);
        Commands.Add("nl-monitor", "Print link, address and route change events (libnl, on IP Helper notifications)", Monitor);
    }

    const string LibnlVersion = "3.12.0";

    // --name=value / --name value / -x value options; positional arguments are rejected.
    sealed class Options
    {
        public readonly Dictionary<string, string> Values = new();
        public bool Details, Stats;
    }

    static Options Parse(string[] args, string tool, string usage, params string[] known)
    {
        var o = new Options();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a is "-h" or "--help") { Console.WriteLine($"Usage: {tool} [OPTION]...\n\n{usage}\n  -f, --format=TYPE     Output format { "{brief|details|stats}" }\n  -h, --help            Show this help\n  -v, --version         Show versioning information"); throw new HelpShown(); }
            if (a is "-v" or "--version") { Console.WriteLine($"libnl tools version {LibnlVersion} (wintools {Commands.Version})"); throw new HelpShown(); }
            if (a is "-d" or "--details") { o.Details = true; continue; }
            if (a is "-s" or "--stats") { o.Stats = o.Details = true; continue; }
            string name, value;
            if (a.StartsWith("--"))
            {
                var eq = a.IndexOf('=');
                name = eq > 0 ? a[2..eq] : a[2..];
                value = eq > 0 ? a[(eq + 1)..] : ++i < args.Length ? args[i] : throw new UsageException($"option --{name} needs a value");
            }
            else if (a.StartsWith('-') && a.Length >= 2)
            {
                name = a[1..2] switch { "f" => "format", "n" => "name", "a" => "addr", "d" => "dev", _ => a[1..2] };
                value = a.Length > 2 ? a[2..] : ++i < args.Length ? args[i] : throw new UsageException($"option {a} needs a value");
            }
            else throw new UsageException($"unexpected argument '{a}'");
            if (name == "format")
            {
                if (value is "details" or "detailed") o.Details = true;
                else if (value == "stats") o.Stats = o.Details = true;
                else if (value != "brief") throw new UsageException($"Unknown format \"{value}\"");
                continue;
            }
            if (!known.Contains(name)) throw new UsageException($"unrecognized option '--{name}'");
            o.Values[name] = value;
        }
        return o;
    }

    sealed class HelpShown : Exception;

    static int Guard(Func<int> body)
    {
        try { return body(); }
        catch (HelpShown) { return 0; }
    }

    static bool FamilyWanted(Options o, bool v6) => o.Values.GetValueOrDefault("family") switch
    {
        null => true,
        "inet" => !v6,
        "inet6" => v6,
        var f => throw new UsageException($"Unknown family \"{f}\""),
    };

    static string Addr(IPAddress a, int prefix, bool v6) => prefix == (v6 ? 128 : 32) ? a.ToString() : $"{a}/{prefix}";

    // ---- links ----

    static int LinkList(string[] args) => Guard(() =>
    {
        var o = Parse(args, "nl-link-list", "  -n, --name=NAME       name of link\n  -i, --index=NUM       interface index\n      --mtu=NUM          MTU value\n      --family=FAMILY    address family", "name", "index", "mtu", "family", "txqlen", "weight", "state", "mode", "operstate", "ifalias");
        foreach (var i in IpHelper.Interfaces())
        {
            if (o.Values.TryGetValue("name", out var name) && !i.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) continue;
            if (o.Values.TryGetValue("index", out var idx) && idx != i.Index.ToString()) continue;
            var type = i.Loopback ? "loopback" : i.Type switch
            {
                NetworkInterfaceType.Ethernet or NetworkInterfaceType.GigabitEthernet or NetworkInterfaceType.FastEthernetT or NetworkInterfaceType.Wireless80211 => "ether",
                NetworkInterfaceType.Tunnel => "tunnel",
                NetworkInterfaceType.Ppp => "ppp",
                _ => i.Mac.Length == 6 ? "ether" : "none",
            };
            var flags = new List<string>();
            if (i.Loopback) flags.Add("loopback");
            else if (!i.PointToPoint) flags.Add("broadcast");
            if (i.PointToPoint) flags.Add("pointopoint");
            if (i.Multicast) flags.Add("multicast");
            if (i.Up) flags.AddRange(["up", "running", "lowerup"]);
            var mac = i.Mac.Length > 0 ? IpHelper.Mac(i.Mac) : "00:00:00:00:00:00";
            Console.WriteLine($"{i.Name} {type} {mac} <{string.Join(",", flags)}>");
            if (o.Details)
            {
                Console.WriteLine($"    mtu {i.Mtu} txqlen 0 weight 0 qdisc none index {i.Index}");
                Console.WriteLine($"    state {(i.Up ? "up" : "down")} mode default carrier {(i.Up ? "on" : "off")}");
                Console.WriteLine($"    alias {i.Description}");
            }
            if (o.Stats)
            {
                try
                {
                    var s = i.Nic.GetIPStatistics();
                    Console.WriteLine($"    RX  {s.BytesReceived} bytes  {s.UnicastPacketsReceived + s.NonUnicastPacketsReceived} packets  {s.IncomingPacketsDiscarded} dropped  {s.IncomingPacketsWithErrors} errors");
                    Console.WriteLine($"    TX  {s.BytesSent} bytes  {s.UnicastPacketsSent + s.NonUnicastPacketsSent} packets  {s.OutgoingPacketsDiscarded} dropped  {s.OutgoingPacketsWithErrors} errors");
                }
                catch (NetworkInformationException) { }
            }
        }
        return 0;
    });

    // ---- addresses ----

    static int AddrList(string[] args) => Guard(() =>
    {
        var o = Parse(args, "nl-addr-list", "      --family=FAMILY    address family (inet, inet6)\n      --local=ADDR       local address\n  -d, --dev=DEV         device the address is on\n      --scope=SCOPE      scope (host, link, global)\n      --label=STRING     address label", "family", "local", "dev", "scope", "label", "peer", "broadcast", "valid", "preferred");
        foreach (var i in IpHelper.Interfaces())
        {
            if (o.Values.TryGetValue("dev", out var dev) && !i.Name.Equals(dev, StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var a in i.Addrs)
            {
                if (!FamilyWanted(o, a.V6)) continue;
                if (o.Values.TryGetValue("local", out var local) && !Addr(a.Address, a.Prefix, a.V6).StartsWith(local.Split('/')[0])) continue;
                var scope = IPAddress.IsLoopback(a.Address) ? "host" : a.Address.IsIPv6LinkLocal || a.Address.ToString().StartsWith("169.254.") ? "link" : "global";
                if (o.Values.TryGetValue("scope", out var sc) && sc != scope) continue;
                var flags = new List<string>();
                if (!a.Dynamic) flags.Add("permanent");
                if (a.Temporary) flags.Add("temporary");
                if (a.Deprecated) flags.Add("deprecated");
                if (a.Tentative) flags.Add("tentative");
                var text = $"{a.Address}/{a.Prefix} {(a.V6 ? "inet6" : "inet")} dev {i.Name} scope {scope}";
                if (flags.Count > 0) text += $" <{string.Join(",", flags)}>";
                Console.WriteLine(text);
                if (o.Details)
                    Console.WriteLine($"    valid-lifetime {(a.Valid >= uint.MaxValue ? "forever" : $"{a.Valid}s")} preferred-lifetime {(a.Preferred >= uint.MaxValue ? "forever" : $"{a.Preferred}s")}");
            }
        }
        return 0;
    });

    // ---- routes ----

    static int RouteList(string[] args) => Guard(() =>
    {
        var o = Parse(args, "nl-route-list", "      --family=FAMILY    address family (inet, inet6)\n      --dst=ADDR         destination prefix\n      --src=ADDR         source prefix\n  -d, --dev=DEV         outgoing device\n      --table=TABLE      routing table (main, local)\n      --type=TYPE        route type (unicast, local, broadcast, multicast)\n      --proto=PROTO      routing protocol", "family", "dst", "src", "dev", "table", "type", "proto", "scope", "iif", "pref-src", "metrics");
        var interfaces = IpHelper.Interfaces();
        var byIndex = interfaces.ToDictionary(i => i.Index);
        var local = interfaces.SelectMany(i => i.Addrs).Select(a => a.Address).ToHashSet();
        var broadcast = interfaces.SelectMany(i => i.Addrs.Where(a => !a.V6 && a.Prefix < 31).Select(a => IpHelper.Broadcast(a.Address, a.Prefix))).ToHashSet();
        foreach (var family in new[] { IpHelper.AF_INET, IpHelper.AF_INET6 })
        {
            var v6 = family == IpHelper.AF_INET6;
            if (!FamilyWanted(o, v6)) continue;
            foreach (var r in IpHelper.Routes(family).OrderBy(r => r.IfIndex).ThenByDescending(r => r.Prefix))
            {
                var full = r.Prefix == (v6 ? 128 : 32);
                var type = full && local.Contains(r.Dest) ? "local"
                    : full && broadcast.Contains(r.Dest) || !v6 && r.Dest.Equals(IPAddress.Broadcast) ? "broadcast"
                    : v6 ? r.Dest.IsIPv6Multicast ? "multicast" : "unicast"
                    : r.Dest.GetAddressBytes()[0] is >= 224 and < 240 ? "multicast" : "unicast";
                var table = type is "local" or "broadcast" ? "local" : "main";
                var dev = IpHelper.IfName(byIndex, r.IfIndex);
                var proto = r.Protocol switch { 2 => "kernel", 3 => "static", 4 => "dhcp", 8 => "ra", _ => "boot" };
                if (o.Values.TryGetValue("table", out var t) && t != table) continue;
                if (o.Values.TryGetValue("type", out var ty) && ty != type) continue;
                if (o.Values.TryGetValue("dev", out var d) && !dev.Equals(d, StringComparison.OrdinalIgnoreCase)) continue;
                if (o.Values.TryGetValue("dst", out var dst) && Addr(r.Dest, r.Prefix, v6) != dst && r.Dest.ToString() != dst) continue;
                if (o.Values.TryGetValue("proto", out var p) && p != proto) continue;
                var text = $"{(v6 ? "inet6" : "inet")} {Addr(r.Dest, r.Prefix, v6)} table {table} type {type}";
                if (r.Gateway != null) text += $" via {r.Gateway}";
                text += $" dev {dev}";
                if (o.Details) text += $" proto {proto} metric {r.Metric}";
                Console.WriteLine(text);
            }
        }
        return 0;
    });

    // ---- neighbours ----

    static int NeighList(string[] args) => Guard(() =>
    {
        var o = Parse(args, "nl-neigh-list", "      --family=FAMILY    address family (inet, inet6)\n      --addr=ADDR        destination address\n      --lladdr=ADDR      link layer address\n  -d, --dev=DEV         device the entry is on\n      --state=STATE      neighbour state", "family", "addr", "lladdr", "dev", "state", "dst");
        var byIndex = IpHelper.Interfaces().ToDictionary(i => i.Index);
        foreach (var family in new[] { IpHelper.AF_INET, IpHelper.AF_INET6 })
        {
            var v6 = family == IpHelper.AF_INET6;
            if (!FamilyWanted(o, v6)) continue;
            foreach (var n in IpHelper.Neighbors(family))
            {
                if (IpHelper.IsNoiseNeighbor(n, byIndex)) continue;
                var dev = IpHelper.IfName(byIndex, n.IfIndex);
                var state = IpHelper.NeighborState(n.State).ToLowerInvariant();
                var mac = n.Mac.Length > 0 ? IpHelper.Mac(n.Mac) : null;
                if (o.Values.TryGetValue("dev", out var d) && !dev.Equals(d, StringComparison.OrdinalIgnoreCase)) continue;
                if ((o.Values.GetValueOrDefault("addr") ?? o.Values.GetValueOrDefault("dst")) is { } addr && n.Address.ToString() != addr) continue;
                if (o.Values.TryGetValue("lladdr", out var ll) && !string.Equals(mac, ll, StringComparison.OrdinalIgnoreCase)) continue;
                if (o.Values.TryGetValue("state", out var st) && st != state) continue;
                var text = $"{n.Address} dev {dev}";
                if (mac != null && n.Mac.Any(b => b != 0)) text += $" lladdr {mac}";
                if (n.IsRouter) text += " router";
                Console.WriteLine($"{text} <{state}>");
            }
        }
        return 0;
    });

    // ---- nl-monitor ----

    static int Monitor(string[] args)
    {
        var groups = new HashSet<string>();
        foreach (var a in args)
        {
            if (a is "-h" or "--help")
            {
                Console.WriteLine("Usage: nl-monitor [<groups>] ...\n\nGroups: link, ipv4-ifaddr, ipv6-ifaddr, ipv4-route, ipv6-route (default: all)\n" +
                                  "Events come from IP Helper change notifications; Ctrl+C stops.");
                return 0;
            }
            if (a is not ("link" or "ipv4-ifaddr" or "ipv6-ifaddr" or "ipv4-route" or "ipv6-route" or "neigh" or "ipv4-mroute" or "ipv6-mroute"))
                throw new UsageException($"Unknown multicast group \"{a}\"");
            groups.Add(a);
        }
        bool Want(string g) => groups.Count == 0 || groups.Contains(g);
        var handles = new List<IntPtr>();
        IntPtr h;
        if (Want("link") && NotifyIpInterfaceChange(IpHelper.AF_UNSPEC, &OnInterface, IntPtr.Zero, false, out h) == 0) handles.Add(h);
        var addrFamily = Want("ipv4-ifaddr") && Want("ipv6-ifaddr") ? IpHelper.AF_UNSPEC : Want("ipv4-ifaddr") ? IpHelper.AF_INET : Want("ipv6-ifaddr") ? IpHelper.AF_INET6 : (ushort)0xFFFF;
        if (addrFamily != 0xFFFF && NotifyUnicastIpAddressChange(addrFamily, &OnAddress, IntPtr.Zero, false, out h) == 0) handles.Add(h);
        var routeFamily = Want("ipv4-route") && Want("ipv6-route") ? IpHelper.AF_UNSPEC : Want("ipv4-route") ? IpHelper.AF_INET : Want("ipv6-route") ? IpHelper.AF_INET6 : (ushort)0xFFFF;
        if (routeFamily != 0xFFFF && NotifyRouteChange2(routeFamily, &OnRoute, IntPtr.Zero, false, out h) == 0) handles.Add(h);
        if (handles.Count == 0) throw new InvalidOperationException("could not register for IP Helper notifications");
        using var stop = new ManualResetEventSlim();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Set(); };
        stop.Wait();
        foreach (var handle in handles) CancelMibChangeNotify2(handle);
        return 0;
    }

    static string Kind(int type) => type switch { 0 => "CHANGE", 1 => "ADD", 2 => "DEL", _ => "INIT" };

    // MIB_IPINTERFACE_ROW: Family @0, InterfaceIndex @16, Connected @... (only the index is used here).
    [UnmanagedCallersOnly]
    static void OnInterface(IntPtr context, byte* row, int type)
    {
        if (row == null) return;
        var index = *(int*)(row + 16);
        var family = *(ushort*)row == IpHelper.AF_INET6 ? "inet6" : "inet";
        var iface = IpHelper.Interfaces().FirstOrDefault(i => i.Index == index);
        Console.WriteLine($"[{Kind(type)}] {iface?.Name ?? $"if{index}"} {family} <{(iface?.Up == true ? "up,running,lowerup" : "down")}>");
    }

    // MIB_UNICASTIPADDRESS_ROW: Address @0 (SOCKADDR_INET), InterfaceIndex @40, OnLinkPrefixLength @60.
    [UnmanagedCallersOnly]
    static void OnAddress(IntPtr context, byte* row, int type)
    {
        if (row == null) return;
        var address = IpHelper.ReadSockaddr(row);
        var index = *(int*)(row + 40);
        var prefix = row[60];
        var v6 = address?.AddressFamily == AddressFamily.InterNetworkV6;
        var name = IpHelper.Interfaces().FirstOrDefault(i => i.Index == index)?.Name ?? $"if{index}";
        Console.WriteLine($"[{Kind(type)}] {address}/{prefix} {(v6 ? "inet6" : "inet")} dev {name}");
    }

    // MIB_IPFORWARD_ROW2: InterfaceIndex @8, DestinationPrefix @12 (length @40), NextHop @44.
    [UnmanagedCallersOnly]
    static void OnRoute(IntPtr context, byte* row, int type)
    {
        if (row == null) return;
        var index = *(int*)(row + 8);
        var dest = IpHelper.ReadSockaddr(row + 12);
        var prefix = row[40];
        var gateway = IpHelper.ReadSockaddr(row + 44);
        var v6 = dest?.AddressFamily == AddressFamily.InterNetworkV6;
        var name = IpHelper.Interfaces().FirstOrDefault(i => i.Index == index)?.Name ?? $"if{index}";
        var via = gateway != null && !gateway.Equals(IPAddress.Any) && !gateway.Equals(IPAddress.IPv6Any) ? $" via {gateway}" : "";
        Console.WriteLine($"[{Kind(type)}] {(v6 ? "inet6" : "inet")} {dest}/{prefix} table main{via} dev {name}");
    }

    // ---- Win32 ----

    [DllImport("iphlpapi.dll")]
    static extern int NotifyIpInterfaceChange(ushort family, delegate* unmanaged<IntPtr, byte*, int, void> callback, IntPtr context, bool initial, out IntPtr handle);

    [DllImport("iphlpapi.dll")]
    static extern int NotifyUnicastIpAddressChange(ushort family, delegate* unmanaged<IntPtr, byte*, int, void> callback, IntPtr context, bool initial, out IntPtr handle);

    [DllImport("iphlpapi.dll")]
    static extern int NotifyRouteChange2(ushort family, delegate* unmanaged<IntPtr, byte*, int, void> callback, IntPtr context, bool initial, out IntPtr handle);

    [DllImport("iphlpapi.dll")]
    static extern int CancelMibChangeNotify2(IntPtr handle);
}
