using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using static WinTools.IpHelper;

namespace WinTools;

// ip / ss / bridge (iproute2, iproute2mac): Linux's iproute2 over the IP Helper API. Interfaces keep
// their Windows names ("Wi-Fi", "Ethernet 2") or can be given by index; changes go through netsh
// (store=active, so like Linux they last until reboot) and need an elevated prompt.
static class IpRoute
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("ip", "Show and change addresses, links, routes and neighbours (iproute2)", RunIp);
        Commands.Add("ss", "Show TCP and UDP sockets (iproute2)", RunSs);
        Commands.Add("bridge", "Show Windows Network Bridge ports (iproute2)", RunBridge);
    }

    const string IpUsage =
        "Usage: ip [ OPTIONS ] OBJECT { COMMAND | help }\n" +
        "where  OBJECT := { address | link | neighbour | route }\n" +
        "       OPTIONS := { -4 | -6 | -0 | -s[tatistics] | -br[ief] | -o[neline] | -j[son] |\n" +
        "                    -p[retty] | -f[amily] { inet | inet6 | link } | -V[ersion] }\n\n" +
        "  ip address [ show [ dev IFNAME ] [ up ] ]     ip address { add | del } PREFIX dev IFNAME\n" +
        "  ip address flush dev IFNAME\n" +
        "  ip link [ show [ dev IFNAME ] ]               ip link set [ dev ] IFNAME { up | down | mtu N | name NEW }\n" +
        "  ip route [ show [ table { main | local | all } ] [ dev IFNAME ] ]\n" +
        "  ip route get ADDRESS                          ip route { add | del | replace } PREFIX [ via ADDR ] [ dev IFNAME ] [ metric N ]\n" +
        "  ip neigh [ show [ dev IFNAME ] ]              ip neigh { add | del } ADDR [ lladdr MAC ] dev IFNAME\n" +
        "  ip neigh flush [ dev IFNAME ]\n\n" +
        "Interfaces use their Windows names (quote names with spaces) or index numbers.\n" +
        "Changes are made with netsh, last until reboot, and need an elevated prompt.";

    sealed class Options
    {
        public ushort Family;
        public bool Brief, OneLine, Json, LinkOnly;
        public int Stats;
    }

    // Also the back end for ifconfig/route/arp changes (net-tools).
    internal static int RunIp(string[] args)
    {
        var o = new Options();
        var i = 0;
        for (; i < args.Length && args[i].StartsWith('-') && args[i].Length > 1; i++)
        {
            var opt = args[i].TrimStart('-');
            // Accepted and ignored: colour, details, pretty-printing, human-readable numbers.
            if (opt.StartsWith("color") || opt == "c" || opt.StartsWith("details") || opt == "d" || opt.StartsWith("pretty")
                || opt == "p" || opt.StartsWith("human")) continue;
            switch (opt)
            {
                case "help": Console.WriteLine(IpUsage); return 0;
                case "V" or "Version": Console.WriteLine($"ip utility, iproute2-wintools-{Commands.Version}"); return 0;
                case "4": o.Family = AF_INET; break;
                case "6": o.Family = AF_INET6; break;
                case "0": o.LinkOnly = true; break;
                case "br" or "brief": o.Brief = true; break;
                case "s" or "stats" or "statistics": o.Stats++; break;
                case "o" or "oneline": o.OneLine = true; break;
                case "j" or "json": o.Json = true; break;
                case "f" or "family":
                    if (++i == args.Length) throw new UsageException("-family needs an argument");
                    switch (args[i])
                    {
                        case "inet": o.Family = AF_INET; break;
                        case "inet6": o.Family = AF_INET6; break;
                        case "link": o.LinkOnly = true; break;
                        default: throw new UsageException($"invalid family \"{args[i]}\"");
                    }
                    break;
                case "n" or "netns":
                    Console.Error.WriteLine("ip: network namespaces don't exist on Windows");
                    return 1;
                default: throw new UsageException($"Option \"{args[i]}\" is unknown, try \"ip -help\".");
            }
        }
        if (i == args.Length) throw new UsageException("missing OBJECT (see ip -help)");
        var obj = args[i];
        var rest = args[(i + 1)..];
        if (Abbrev(obj, "address")) return o.LinkOnly ? Link(o, rest) : Address(o, rest);
        if (Abbrev(obj, "link")) return Link(o, rest);
        if (Abbrev(obj, "route", 1) || obj == "ro") return Route(o, rest);
        if (Abbrev(obj, "neighbour") || Abbrev(obj, "neighbor")) return Neigh(o, rest);
        if (Abbrev(obj, "help")) { Console.WriteLine(IpUsage); return 0; }
        if (Abbrev(obj, "rule", 2) || Abbrev(obj, "tunnel", 2) || Abbrev(obj, "maddress", 2) || Abbrev(obj, "netns", 4)
            || Abbrev(obj, "xfrm") || Abbrev(obj, "tuntap", 3) || Abbrev(obj, "mroute", 2) || Abbrev(obj, "monitor", 3))
        {
            Console.Error.WriteLine($"ip: \"{obj}\" has no Windows counterpart (Windows has no policy routing, netns, xfrm or tunnel devices); only address, link, route and neigh are available");
            return 1;
        }
        throw new UsageException($"Object \"{obj}\" is unknown, try \"ip help\".");
    }

    static bool Abbrev(string word, string full, int min = 1) =>
        word.Length >= min && full.StartsWith(word, StringComparison.Ordinal);

    static bool IsShow(string word) => Abbrev(word, "show") || Abbrev(word, "list") || word == "lst";

    // ---- ip address / ip link ----

    static int Address(Options o, string[] args)
    {
        if (args.Length == 0 || IsShow(args[0])) return ShowLinks(o, args.Length == 0 ? args : args[1..], withAddresses: true);
        if (Abbrev(args[0], "add") || Abbrev(args[0], "replace") || Abbrev(args[0], "change")) return AddressChange(args[1..], add: true);
        if (Abbrev(args[0], "delete")) return AddressChange(args[1..], add: false);
        if (Abbrev(args[0], "flush")) return AddressFlush(o, args[1..]);
        if (Abbrev(args[0], "help")) { Console.WriteLine(IpUsage); return 0; }
        throw new UsageException($"Command \"{args[0]}\" is unknown, try \"ip address help\".");
    }

    static int Link(Options o, string[] args)
    {
        if (args.Length == 0 || IsShow(args[0])) return ShowLinks(o, args.Length == 0 ? args : args[1..], withAddresses: false);
        if (Abbrev(args[0], "set")) return LinkSet(args[1..]);
        if (Abbrev(args[0], "help")) { Console.WriteLine(IpUsage); return 0; }
        if (args[0] is "add" or "delete" or "del")
        {
            Console.Error.WriteLine("ip: virtual links (veth, bridge, vlan, dummy) can't be created with ip on Windows; use Hyper-V switches or the adapter's VLAN setting");
            return 1;
        }
        throw new UsageException($"Command \"{args[0]}\" is unknown, try \"ip link help\".");
    }

    static int ShowLinks(Options o, string[] args, bool withAddresses)
    {
        string dev = null;
        var upOnly = false;
        string scope = null;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "dev": dev = Next(args, ref i); break;
                case "up": upOnly = true; break;
                case "scope": scope = Next(args, ref i); break;
                case "label": Next(args, ref i); break;
                default: dev = args[i]; break;
            }
        }
        var all = Interfaces();
        var shown = all.Where(x => dev == null || x == Require(all, dev)).Where(x => !upOnly || x.Up).ToList();
        if (dev != null && shown.Count == 0 && !upOnly) Require(all, dev);

        var json = new List<Raw>();
        foreach (var iface in shown)
        {
            var addrs = withAddresses
                ? iface.Addrs.Where(a => o.Family == 0 || (o.Family == AF_INET6) == a.V6).Where(a => scope == null || Scope(a.Address) == scope).ToList()
                : [];
            if (withAddresses && (o.Family != 0 || scope != null) && addrs.Count == 0) continue;
            if (o.Json) { json.Add(LinkJson(iface, addrs, withAddresses)); continue; }
            if (o.Brief)
            {
                var tail = withAddresses
                    ? string.Join(" ", addrs.Select(a => $"{a.Address}/{a.Prefix}"))
                    : $"{LinkAddress(iface),-17} <{string.Join(",", Flags(iface))}>";
                Console.WriteLine($"{iface.Name,-16} {State(iface),-14} {tail}".TrimEnd());
                continue;
            }
            if (!(o.OneLine && withAddresses))
            {
                var lines = new List<string>
                {
                    $"{iface.Index}: {iface.Name}: <{string.Join(",", Flags(iface))}> mtu {iface.Mtu} state {State(iface)} group default",
                    "    " + LinkLine(iface),
                };
                if (o.Stats > 0) lines.AddRange(StatLines(iface));
                Print(lines, o.OneLine);
            }
            foreach (var a in addrs)
            {
                var head = a.V6
                    ? $"    inet6 {a.Address}/{a.Prefix} scope {Scope(a.Address)}{AddrFlags(a)}"
                    : $"    inet {a.Address}/{a.Prefix}{(a.Prefix < 31 ? $" brd {Broadcast(a.Address, a.Prefix)}" : "")} scope {Scope(a.Address)}{AddrFlags(a)} {iface.Name}";
                var life = $"       valid_lft {Lifetime(a.Valid)} preferred_lft {Lifetime(a.Preferred)}";
                Print(o.OneLine ? [$"{iface.Index}: {iface.Name}{head}", life] : [head, life], o.OneLine);
            }
        }
        if (o.Json) Console.WriteLine(Js(json));
        return 0;
    }

    static void Print(List<string> lines, bool oneLine)
    {
        if (oneLine) Console.WriteLine(string.Join("\\", lines));
        else foreach (var line in lines) Console.WriteLine(line);
    }

    internal static List<string> Flags(Iface iface)
    {
        var flags = new List<string>();
        if (iface.Loopback) flags.Add("LOOPBACK");
        else if (iface.PointToPoint) flags.AddRange(["POINTOPOINT", "NOARP"]);
        else flags.Add("BROADCAST");
        if (iface.Multicast && !iface.Loopback) flags.Add("MULTICAST");
        flags.Add("UP");
        if (iface.Up) flags.Add("LOWER_UP");
        else flags.Insert(0, "NO-CARRIER");
        return flags;
    }

    internal static string State(Iface iface) => iface.Loopback ? "UNKNOWN" : iface.Status switch
    {
        System.Net.NetworkInformation.OperationalStatus.Up => "UP",
        System.Net.NetworkInformation.OperationalStatus.Down => "DOWN",
        System.Net.NetworkInformation.OperationalStatus.Dormant => "DORMANT",
        System.Net.NetworkInformation.OperationalStatus.LowerLayerDown => "LOWERLAYERDOWN",
        System.Net.NetworkInformation.OperationalStatus.NotPresent => "NOTPRESENT",
        System.Net.NetworkInformation.OperationalStatus.Testing => "TESTING",
        _ => "UNKNOWN",
    };

    static string LinkType(Iface iface) => iface.Loopback ? "loopback" : iface.Mac.Length == 6 ? "ether" : "none";

    static string LinkAddress(Iface iface) => iface.Loopback ? "00:00:00:00:00:00" : Mac(iface.Mac);

    static string LinkLine(Iface iface) => LinkType(iface) switch
    {
        "loopback" => "link/loopback 00:00:00:00:00:00 brd 00:00:00:00:00:00",
        "ether" => $"link/ether {Mac(iface.Mac)} brd ff:ff:ff:ff:ff:ff",
        _ => "link/none",
    };

    static IEnumerable<string> StatLines(Iface iface)
    {
        var s = iface.Nic.GetIPStatistics();
        yield return "    RX: bytes  packets  errors  dropped overrun mcast   ";
        yield return $"    {s.BytesReceived,-10} {s.UnicastPacketsReceived + s.NonUnicastPacketsReceived,-8} {s.IncomingPacketsWithErrors,-7} {s.IncomingPacketsDiscarded,-7} {0,-7} {s.NonUnicastPacketsReceived,-7}";
        yield return "    TX: bytes  packets  errors  dropped carrier collsns ";
        yield return $"    {s.BytesSent,-10} {s.UnicastPacketsSent + s.NonUnicastPacketsSent,-8} {s.OutgoingPacketsWithErrors,-7} {s.OutgoingPacketsDiscarded,-7} {0,-7} {0,-7}";
    }

    internal static string Scope(IPAddress a)
    {
        if (IPAddress.IsLoopback(a)) return "host";
        if (a.IsIPv6LinkLocal) return "link";
        var b = a.GetAddressBytes();
        return a.AddressFamily == AddressFamily.InterNetwork && b[0] == 169 && b[1] == 254 ? "link" : "global";
    }

    static string AddrFlags(IfAddr a) =>
        (a.Temporary ? " temporary" : "") + (a.Dynamic ? " dynamic" : "") + (a.Deprecated ? " deprecated" : "") + (a.Tentative ? " tentative" : "");

    static string Lifetime(long seconds) => seconds >= uint.MaxValue || seconds < 0 ? "forever" : $"{seconds}sec";

    static Raw LinkJson(Iface iface, List<IfAddr> addrs, bool withAddresses)
    {
        var fields = new List<(string, object)>
        {
            ("ifindex", iface.Index), ("ifname", iface.Name), ("flags", Flags(iface)), ("mtu", iface.Mtu),
            ("operstate", State(iface)), ("group", "default"), ("link_type", LinkType(iface)),
        };
        if (LinkType(iface) != "none")
        {
            fields.Add(("address", LinkAddress(iface)));
            fields.Add(("broadcast", iface.Loopback ? "00:00:00:00:00:00" : "ff:ff:ff:ff:ff:ff"));
        }
        if (withAddresses)
            fields.Add(("addr_info", addrs.Select(a => Obj(
                ("family", a.V6 ? "inet6" : "inet"), ("local", a.Address.ToString()), ("prefixlen", a.Prefix),
                ("broadcast", !a.V6 && a.Prefix < 31 ? Broadcast(a.Address, a.Prefix).ToString() : null),
                ("scope", Scope(a.Address)), ("dynamic", a.Dynamic ? true : null), ("temporary", a.Temporary ? true : null),
                ("label", a.V6 ? null : iface.Name),
                ("valid_life_time", Math.Clamp(a.Valid, 0, uint.MaxValue)), ("preferred_life_time", Math.Clamp(a.Preferred, 0, uint.MaxValue)))).ToList()));
        return Obj(fields.ToArray());
    }

    static string Next(string[] args, ref int i) =>
        ++i < args.Length ? args[i] : throw new UsageException($"argument expected after \"{args[i - 1]}\"");

    static int AddressChange(string[] args, bool add)
    {
        string prefixText = null, dev = null;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "dev": dev = Next(args, ref i); break;
                case "local": prefixText = Next(args, ref i); break;
                case "brd" or "broadcast" or "label" or "scope" or "valid_lft" or "preferred_lft" or "metric": Next(args, ref i); break;
                case "noprefixroute" or "nodad" or "home" or "dynamic": break;
                default: prefixText ??= args[i]; break;
            }
        }
        if (prefixText == null) throw new UsageException("missing PREFIX");
        if (dev == null) throw new UsageException("Not enough information: \"dev\" argument is required.");
        var iface = Require(Interfaces(), dev);
        var (address, prefix) = ParsePrefix(prefixText, false);
        var v6 = address.AddressFamily == AddressFamily.InterNetworkV6;
        if (v6)
            return Netsh(add
                ? $"interface ipv6 add address interface={iface.Index} address={address}/{prefix} store=active"
                : $"interface ipv6 delete address interface={iface.Index} address={address} store=active");
        return Netsh(add
            ? $"interface ipv4 add address name={iface.Index} address={address} mask={Mask(prefix)} store=active"
            : $"interface ipv4 delete address name={iface.Index} address={address} store=active");
    }

    static int AddressFlush(Options o, string[] args)
    {
        string dev = null;
        for (var i = 0; i < args.Length; i++) dev = args[i] == "dev" ? Next(args, ref i) : args[i];
        if (dev == null) throw new UsageException("Flush requires arguments.");
        var iface = Require(Interfaces(), dev);
        var status = 0;
        foreach (var a in iface.Addrs.Where(a => o.Family == 0 || (o.Family == AF_INET6) == a.V6).Where(a => !a.Address.IsIPv6LinkLocal))
            status |= a.V6
                ? Netsh($"interface ipv6 delete address interface={iface.Index} address={a.Address} store=active")
                : Netsh($"interface ipv4 delete address name={iface.Index} address={a.Address} store=active");
        return status;
    }

    static int LinkSet(string[] args)
    {
        string dev = null;
        var status = 0;
        var i = 0;
        if (i < args.Length && args[i] == "dev") i++;
        if (i >= args.Length) throw new UsageException("Not enough information: \"dev\" argument is required.");
        dev = args[i++];
        // A disabled adapter vanishes from the interface list, so fall back to the name as given.
        var iface = Find(Interfaces(), dev);
        var name = iface?.Name ?? dev;
        for (; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "up": status |= Netsh($"interface set interface name={Quote(name)} admin=enabled"); break;
                case "down": status |= Netsh($"interface set interface name={Quote(name)} admin=disabled"); break;
                case "mtu":
                    var mtu = Next(args, ref i);
                    if (!int.TryParse(mtu, out _)) throw new UsageException($"invalid mtu \"{mtu}\"");
                    if (iface == null) Require(Interfaces(), dev);
                    status |= Netsh($"interface ipv4 set subinterface {iface.Index} mtu={mtu} store=active");
                    if (iface.Nic.Supports(System.Net.NetworkInformation.NetworkInterfaceComponent.IPv6))
                        status |= Netsh($"interface ipv6 set subinterface {iface.Index} mtu={mtu} store=active");
                    break;
                case "name":
                    status |= Netsh($"interface set interface name={Quote(name)} newname={Quote(Next(args, ref i))}");
                    break;
                case "address":
                    Console.Error.WriteLine("ip: Windows sets MAC addresses through the adapter driver; use macchanger instead");
                    return 1;
                case "arp" or "multicast" or "promisc" or "allmulticast" or "txqueuelen" or "qlen" or "master" or "nomaster" or "netns":
                    Console.Error.WriteLine($"ip: \"{args[i]}\" can't be set on Windows network adapters");
                    return 1;
                default: throw new UsageException($"Garbage instead of arguments \"{args[i]}\". Try \"ip link help\".");
            }
        }
        return status;
    }

    // ---- ip route ----

    static int Route(Options o, string[] args)
    {
        if (args.Length == 0 || IsShow(args[0])) return ShowRoutes(o, args.Length == 0 ? args : args[1..]);
        if (Abbrev(args[0], "get")) return RouteGet(args[1..]);
        if (Abbrev(args[0], "add")) return RouteChange(o, args[1..], "add");
        if (Abbrev(args[0], "delete")) return RouteChange(o, args[1..], "delete");
        if (Abbrev(args[0], "replace") || Abbrev(args[0], "change")) return RouteChange(o, args[1..], "replace");
        if (Abbrev(args[0], "help")) { Console.WriteLine(IpUsage); return 0; }
        if (Abbrev(args[0], "flush")) { Console.Error.WriteLine("ip: Windows has no route cache to flush; delete routes one at a time"); return 1; }
        throw new UsageException($"Command \"{args[0]}\" is unknown, try \"ip route help\".");
    }

    // Linux keeps local, broadcast and multicast routes in the "local" table; Windows mixes them in.
    internal static string RouteType(Route r, Dictionary<int, Iface> byIndex)
    {
        var bytes = r.Dest.GetAddressBytes();
        if (r.V6 ? r.Dest.IsIPv6Multicast : bytes[0] >= 224 && bytes[0] < 240) return "multicast";
        byIndex.TryGetValue(r.IfIndex, out var iface);
        var max = r.V6 ? 128 : 32;
        if (!r.V6 && r.Prefix == 32 && r.Dest.Equals(IPAddress.Broadcast)) return "broadcast";
        if (r.Prefix == max && iface != null && iface.Addrs.Any(a => a.Address.Equals(r.Dest)) && !(r.V6 && iface.Loopback)) return "local";
        if (!r.V6 && iface != null && iface.Loopback && bytes[0] == 127) return r.Prefix == 32 && bytes[3] == 255 ? "broadcast" : "local";
        if (!r.V6 && r.Prefix == 32 && iface != null && iface.Addrs.Any(a => !a.V6 && a.Prefix < 31 && Broadcast(a.Address, a.Prefix).Equals(r.Dest)))
            return "broadcast";
        return "unicast";
    }

    // Windows records a DHCP-supplied default gateway as a manual route; spot it from the adapter's DHCP state.
    internal static string Proto(Route r, Iface iface) => r.Origin switch
    {
        2 => "dhcp",
        _ when !r.V6 && r.Gateway != null && iface is { Dhcp4: true } && iface.Gateways.Contains(r.Gateway) => "dhcp",
        3 => "ra",
        _ => r.Protocol switch
        {
            2 => "kernel", 3 or 10002 or 10006 or 10007 => r.Gateway == null ? "kernel" : "static",
            8 => "rip", 13 => "ospf", 14 => "bgp", 19 => "dhcp", _ => "boot",
        },
    };

    internal static uint EffectiveMetric(Route r, Dictionary<int, Iface> byIndex) =>
        r.Metric + (byIndex.TryGetValue(r.IfIndex, out var i) ? (r.V6 ? i.Metric6 : i.Metric4) : 0);

    static string Dest(Route r) =>
        r.Prefix == 0 ? "default" : r.Prefix == (r.V6 ? 128 : 32) ? r.Dest.ToString() : $"{r.Dest}/{r.Prefix}";

    static IPAddress PreferredSource(Route r, Iface iface) =>
        iface?.Addrs.Where(a => a.V6 == r.V6 && !a.Deprecated)
            .OrderByDescending(a => InPrefix(a.Address, r.Dest, Math.Min(r.Prefix, a.Prefix)))
            .ThenBy(a => a.Address.IsIPv6LinkLocal)
            .Select(a => a.Address).FirstOrDefault();

    static int ShowRoutes(Options o, string[] args)
    {
        var table = "main";
        string dev = null, proto = null, match = null;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "table": table = Next(args, ref i); break;
                case "dev" or "oif": dev = Next(args, ref i); break;
                case "proto": proto = Next(args, ref i); break;
                case "root" or "match" or "exact" or "to": match = Next(args, ref i); break;
                default: match = args[i]; break;
            }
        }
        if (table is "255") table = "local";
        if (table is "254") table = "main";
        if (table is "0" or "unspec") table = "all";
        if (table is not ("main" or "local" or "all")) throw new InvalidOperationException($"table \"{table}\" doesn't exist (Windows has a single routing table)");

        var all = Interfaces();
        var byIndex = all.ToDictionary(x => x.Index);
        Iface devIface = dev == null ? null : Require(all, dev);
        var v6 = o.Family == AF_INET6;
        (IPAddress address, int prefix)? exact = match == null ? null : ParsePrefix(match, v6);

        var json = new List<Raw>();
        foreach (var r in Routes(v6 ? AF_INET6 : AF_INET))
        {
            var type = RouteType(r, byIndex);
            var local = type != "unicast";
            if (table == "main" && local || table == "local" && !local) continue;
            if (devIface != null && r.IfIndex != devIface.Index) continue;
            byIndex.TryGetValue(r.IfIndex, out var iface);
            var rproto = Proto(r, iface);
            if (proto != null && proto != rproto) continue;
            if (exact is { } e && !(r.Prefix == e.prefix && Network(r.Dest, r.Prefix).Equals(Network(e.address, e.prefix)))) continue;
            var name = IfName(byIndex, r.IfIndex);
            var metric = EffectiveMetric(r, byIndex);
            var src = r.Gateway == null ? PreferredSource(r, iface) : null;
            var scope = local ? (type == "local" ? "host" : "link") : r.Gateway == null && !r.V6 ? "link" : null;
            var down = iface == null || !iface.Up;
            if (o.Json)
            {
                json.Add(Obj(("type", type == "unicast" ? null : type), ("dst", Dest(r)), ("gateway", r.Gateway?.ToString()),
                    ("dev", name), ("table", table == "all" && local ? "local" : null), ("protocol", rproto),
                    ("scope", scope), ("prefsrc", local && !r.V6 || scope == "link" ? src?.ToString() : null),
                    ("metric", r.V6 || !local ? metric : null), ("flags", down ? (string[])["linkdown"] : [])));
                continue;
            }
            var line = new List<string>();
            if (type != "unicast") line.Add(type);
            line.Add(Dest(r));
            if (r.Gateway != null) line.Add($"via {r.Gateway}");
            line.Add($"dev {name}");
            if (table == "all" && local) line.Add("table local");
            line.Add($"proto {rproto}");
            if (scope != null) line.Add($"scope {scope}");
            if (src != null && (scope == "link" || type == "local") && !r.V6) line.Add($"src {src}");
            if (r.V6 || !local) line.Add($"metric {metric}");
            if (down && !local) line.Add("linkdown");
            if (r.V6) line.Add("pref medium");
            Console.WriteLine(string.Join(" ", line));
        }
        if (o.Json) Console.WriteLine(Js(json));
        return 0;
    }

    static int RouteGet(string[] args)
    {
        var target = args.FirstOrDefault(a => a is not ("to" or "fibmatch"));
        if (target == null) throw new UsageException("need at least a destination address");
        var dest = ResolveOne(target);
        var (route, source) = BestRoute(dest);
        if (route == null) throw new InvalidOperationException("RTNETLINK answers: Network is unreachable");
        var all = Interfaces();
        var byIndex = all.ToDictionary(x => x.Index);
        var local = all.Any(i => i.Addrs.Any(a => a.Address.Equals(dest)));
        var line = (local ? "local " : "") + dest + (route.Gateway != null ? $" via {route.Gateway}" : "")
                   + $" dev {IfName(byIndex, route.IfIndex)}" + (source != null ? $" src {source}" : "");
        Console.WriteLine(line);
        Console.WriteLine("    cache");
        return 0;
    }

    internal static IPAddress ResolveOne(string host, AddressFamily family = AddressFamily.Unspecified)
    {
        if (IPAddress.TryParse(host, out var ip)) return ip;
        try
        {
            return Dns.GetHostAddresses(host).FirstOrDefault(a => family == AddressFamily.Unspecified ? a.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6 : a.AddressFamily == family)
                   ?? throw new InvalidOperationException($"{host}: Address family for hostname not supported");
        }
        catch (SocketException)
        {
            throw new InvalidOperationException($"{host}: Name or service not known");
        }
    }

    static int RouteChange(Options o, string[] args, string verb)
    {
        string prefixText = null, via = null, dev = null, metric = null;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "via" or "gw": via = Next(args, ref i); break;
                case "dev" or "oif": dev = Next(args, ref i); break;
                case "metric" or "priority" or "preference": metric = Next(args, ref i); break;
                case "proto" or "scope" or "table" or "src" or "mtu" or "type": Next(args, ref i); break;
                case "to": prefixText = Next(args, ref i); break;
                case "unicast" or "onlink": break;
                default:
                    if (args[i] is "blackhole" or "unreachable" or "prohibit" or "throw" or "nat")
                    {
                        Console.Error.WriteLine($"ip: Windows has no \"{args[i]}\" routes");
                        return 1;
                    }
                    prefixText ??= args[i];
                    break;
            }
        }
        if (prefixText == null) throw new UsageException("missing PREFIX");
        var viaAddress = via == null ? null : IPAddress.TryParse(via, out var g) ? g : throw new UsageException($"invalid gateway \"{via}\"");
        var v6 = o.Family == AF_INET6 || viaAddress?.AddressFamily == AddressFamily.InterNetworkV6 || prefixText.Contains(':');
        var (address, prefix) = ParsePrefix(prefixText, v6);
        var all = Interfaces();
        var byIndex = all.ToDictionary(x => x.Index);
        int index;
        if (dev != null) index = Require(all, dev).Index;
        else if (verb == "delete" && Routes(v6 ? AF_INET6 : AF_INET).FirstOrDefault(r => r.Prefix == prefix
                     && r.Dest.Equals(Network(address, prefix)) && (viaAddress == null || viaAddress.Equals(r.Gateway))) is { } existing)
            index = existing.IfIndex;
        else if (viaAddress != null)
            index = all.FirstOrDefault(i => i.Addrs.Any(a => InPrefix(viaAddress, a.Address, a.Prefix)))?.Index
                    ?? BestRoute(viaAddress).route?.IfIndex
                    ?? throw new InvalidOperationException($"Nexthop has invalid gateway {via}");
        else throw new UsageException("Not enough information: \"dev\" or \"via\" is required.");

        var family = v6 ? "ipv6" : "ipv4";
        var spec = $"prefix={Network(address, prefix)}/{prefix} interface={index}"
                   + (viaAddress != null ? $" nexthop={viaAddress}" : "");
        var extra = (metric != null ? $" metric={metric}" : "") + " store=active";
        if (verb == "add") return Netsh($"interface {family} add route {spec}{extra}");
        if (verb == "delete") return Netsh($"interface {family} delete route {spec} store=active");
        var exists = Routes(v6 ? AF_INET6 : AF_INET).Any(r => r.IfIndex == index && r.Prefix == prefix && r.Dest.Equals(Network(address, prefix)));
        return Netsh($"interface {family} {(exists ? "set" : "add")} route {spec}{extra}");
    }

    // ---- ip neigh ----

    static int Neigh(Options o, string[] args)
    {
        if (args.Length == 0 || IsShow(args[0])) return ShowNeighbors(o, args.Length == 0 ? args : args[1..]);
        if (Abbrev(args[0], "add") || Abbrev(args[0], "replace") || Abbrev(args[0], "change")) return NeighChange(args[1..], add: true);
        if (Abbrev(args[0], "delete")) return NeighChange(args[1..], add: false);
        if (Abbrev(args[0], "flush")) return NeighFlush(o, args[1..]);
        if (Abbrev(args[0], "help")) { Console.WriteLine(IpUsage); return 0; }
        throw new UsageException($"Command \"{args[0]}\" is unknown, try \"ip neigh help\".");
    }

    static int ShowNeighbors(Options o, string[] args)
    {
        string dev = null, nud = null;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "dev": dev = Next(args, ref i); break;
                case "nud": nud = Next(args, ref i).ToUpperInvariant(); break;
                default: break;
            }
        }
        var all = Interfaces();
        var byIndex = all.ToDictionary(x => x.Index);
        var devIface = dev == null ? null : Require(all, dev);
        var json = new List<Raw>();
        foreach (var n in Neighbors(o.Family))
        {
            if (IsNoiseNeighbor(n, byIndex)) continue;
            if (devIface != null && n.IfIndex != devIface.Index) continue;
            var state = NeighborState(n.State);
            if (nud != null && nud != "ALL" && nud != state) continue;
            var hasMac = n.Mac.Length > 0 && n.Mac.Any(b => b != 0);
            if (o.Json)
            {
                json.Add(Obj(("dst", n.Address.ToString()), ("dev", IfName(byIndex, n.IfIndex)),
                    ("lladdr", hasMac ? Mac(n.Mac) : null), ("router", n.IsRouter ? "" : null), ("state", (string[])[state])));
                continue;
            }
            Console.WriteLine($"{n.Address} dev {IfName(byIndex, n.IfIndex)}" + (hasMac ? $" lladdr {Mac(n.Mac)}" : "")
                              + (n.IsRouter ? " router" : "") + $" {state}");
        }
        if (o.Json) Console.WriteLine(Js(json));
        return 0;
    }

    static int NeighChange(string[] args, bool add)
    {
        string address = null, mac = null, dev = null;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "lladdr": mac = Next(args, ref i); break;
                case "dev": dev = Next(args, ref i); break;
                case "nud": Next(args, ref i); break;
                case "to": address = Next(args, ref i); break;
                case "router" or "proxy": break;
                default: address ??= args[i]; break;
            }
        }
        if (address == null || dev == null) throw new UsageException("Device and destination are required arguments.");
        if (!IPAddress.TryParse(address, out var ip)) throw new UsageException($"invalid address \"{address}\"");
        var iface = Require(Interfaces(), dev);
        var family = ip.AddressFamily == AddressFamily.InterNetworkV6 ? "ipv6" : "ipv4";
        if (!add) return Netsh($"interface {family} delete neighbors interface={iface.Index} address={ip} store=active");
        if (mac == null) throw new UsageException("lladdr is required to add a neighbour");
        var hw = string.Join("-", ParseMac(mac).Select(b => b.ToString("x2")));
        return Netsh($"interface {family} add neighbors interface={iface.Index} address={ip} neighbor={hw} store=active");
    }

    static int NeighFlush(Options o, string[] args)
    {
        string dev = null;
        for (var i = 0; i < args.Length; i++) if (args[i] == "dev") dev = Next(args, ref i);
        var families = o.Family switch { AF_INET => (string[])["ipv4"], AF_INET6 => ["ipv6"], _ => ["ipv4", "ipv6"] };
        var target = dev == null ? "" : $" interface={Require(Interfaces(), dev).Index}";
        var status = 0;
        foreach (var family in families) status |= Netsh($"interface {family} delete neighbors{target}");
        return status;
    }

    // ---- ss ----

    static readonly string[] SsStates = ["UNKNOWN", "CLOSED", "LISTEN", "SYN-SENT", "SYN-RECV", "ESTAB", "FIN-WAIT-1",
                                         "FIN-WAIT-2", "CLOSE-WAIT", "CLOSING", "LAST-ACK", "TIME-WAIT", "CLOSED"];

    static int RunSs(string[] args)
    {
        bool tcp = false, udp = false, listening = false, all = false, numeric = false, processes = false;
        bool v4 = false, v6 = false, summary = false, noHeader = false;
        string stateFilter = null;
        int? sport = null, dport = null;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.StartsWith("--"))
            {
                switch (arg)
                {
                    case "--help": Console.WriteLine(SsUsage); return 0;
                    case "--version": Console.WriteLine($"ss utility, iproute2-wintools-{Commands.Version}"); return 0;
                    case "--tcp": tcp = true; break;
                    case "--udp": udp = true; break;
                    case "--listening": listening = true; break;
                    case "--all": all = true; break;
                    case "--numeric": numeric = true; break;
                    case "--processes": processes = true; break;
                    case "--ipv4": v4 = true; break;
                    case "--ipv6": v6 = true; break;
                    case "--summary": summary = true; break;
                    case "--no-header": noHeader = true; break;
                    case "--resolve" or "--extended" or "--memory" or "--info" or "--options": break;
                    default: throw new UsageException($"unrecognized option '{arg}'");
                }
                continue;
            }
            if (arg.StartsWith('-') && arg.Length > 1)
            {
                foreach (var c in arg[1..])
                {
                    switch (c)
                    {
                        case 'h': Console.WriteLine(SsUsage); return 0;
                        case 'V': Console.WriteLine($"ss utility, iproute2-wintools-{Commands.Version}"); return 0;
                        case 't': tcp = true; break;
                        case 'u': udp = true; break;
                        case 'l': listening = true; break;
                        case 'a': all = true; break;
                        case 'n': numeric = true; break;
                        case 'p': processes = true; break;
                        case '4': v4 = true; break;
                        case '6': v6 = true; break;
                        case 's': summary = true; break;
                        case 'H': noHeader = true; break;
                        case 'r' or 'e' or 'm' or 'i' or 'o': break;
                        case 'x' or 'w':
                            Console.Error.WriteLine(c == 'x' ? "ss: Windows doesn't list AF_UNIX sockets; showing nothing" : "ss: Windows doesn't expose raw sockets");
                            return 1;
                        default: throw new UsageException($"invalid option -- '{c}'");
                    }
                }
                continue;
            }
            switch (arg)
            {
                case "state": stateFilter = Next(args, ref i).ToLowerInvariant(); break;
                case "sport" or "dport":
                    var op = Next(args, ref i);
                    var value = op is "=" or "==" or "eq" ? Next(args, ref i) : op;
                    if (!int.TryParse(value.TrimStart(':'), out var port)) throw new UsageException($"invalid port \"{value}\"");
                    if (arg == "sport") sport = port; else dport = port;
                    break;
                case "(" or ")" or "and": break;
                default: throw new UsageException($"unsupported filter \"{arg}\" (state, sport and dport are supported)");
            }
        }
        if (!tcp && !udp) { tcp = true; udp = all || listening; }
        if (!v4 && !v6) v4 = v6 = true;

        var socks = summary ? Sockets(true, true, v4, v6) : Sockets(tcp, udp, v4, v6);
        if (summary) return SsSummary(socks);

        var names = processes ? ProcessNames() : null;
        var rows = new List<string[]>();
        foreach (var s in socks)
        {
            var state = s.Tcp ? SsStates[Math.Clamp(s.State, 0, 12)] : "UNCONN";
            var isListen = s.Tcp ? s.State == TcpListen : true;
            if (stateFilter != null)
            {
                if (!StateMatches(stateFilter, state, s)) continue;
            }
            else if (!all && (listening ? !isListen : isListen)) continue;
            if (sport is { } sp && s.LocalPort != sp) continue;
            if (dport is { } dp && s.RemotePort != dp) continue;
            var proc = "";
            if (processes && names.TryGetValue(s.Pid, out var pname)) proc = $"users:((\"{pname}\",pid={s.Pid}))";
            else if (processes) proc = $"users:((pid={s.Pid}))";
            rows.Add([s.Tcp ? "tcp" : "udp", state, "0", "0",
                SsEndpoint(s.Local, s.LocalPort, s.V6, numeric, s.Tcp, false),
                SsEndpoint(s.Remote, s.RemotePort, s.V6, numeric, s.Tcp, isListen),
                proc]);
        }
        if (!noHeader) rows.Insert(0, ["Netid", "State", "Recv-Q", "Send-Q", "Local Address:Port", "Peer Address:Port", "Process"]);
        PrintTable(rows, rightAlign: [2, 3]);
        return 0;
    }

    static bool StateMatches(string filter, string state, Sock s) => filter switch
    {
        "all" => true,
        "connected" => s.Tcp && state is not ("LISTEN" or "CLOSED"),
        "synchronized" => s.Tcp && state is not ("LISTEN" or "CLOSED" or "SYN-SENT"),
        "bucket" => state is "TIME-WAIT" or "SYN-RECV",
        "big" => state is not ("TIME-WAIT" or "SYN-RECV"),
        "established" => state == "ESTAB",
        "listening" => state is "LISTEN" or "UNCONN",
        _ => state.Equals(filter.Replace('_', '-'), StringComparison.OrdinalIgnoreCase),
    };

    static string SsEndpoint(IPAddress address, int port, bool v6, bool numeric, bool tcp, bool wildcardPort)
    {
        var host = v6 ? $"[{Unscoped(address)}]" + (address.ScopeId != 0 && address.IsIPv6LinkLocal ? $"%{address.ScopeId}" : "") : address.ToString();
        var portText = wildcardPort ? "*" : numeric ? port.ToString() : Service(port, tcp);
        return $"{host}:{portText}";
    }

    internal static void PrintTable(List<string[]> rows, int[] rightAlign)
    {
        if (rows.Count == 0) return;
        var widths = new int[rows[0].Length];
        foreach (var row in rows)
            for (var c = 0; c < row.Length - 1; c++) widths[c] = Math.Max(widths[c], row[c].Length);
        foreach (var row in rows)
        {
            var cells = row.Select((cell, c) => c == row.Length - 1 ? cell
                : rightAlign.Contains(c) ? cell.PadLeft(widths[c]) : cell.PadRight(widths[c]));
            Console.WriteLine(string.Join("  ", cells).TrimEnd());
        }
    }

    static int SsSummary(List<Sock> socks)
    {
        var tcp = socks.Where(s => s.Tcp).ToList();
        var udp = socks.Where(s => !s.Tcp).ToList();
        Console.WriteLine($"Total: {socks.Count}");
        Console.WriteLine($"TCP:   {tcp.Count} (estab {tcp.Count(s => s.State == 5)}, closed {tcp.Count(s => s.State is 1 or 12)}, " +
                          $"orphaned 0, timewait {tcp.Count(s => s.State == 11)})");
        Console.WriteLine();
        Console.WriteLine("Transport Total     IP        IPv6");
        Console.WriteLine($"RAW       {0,-9} {0,-9} {0,-9}");
        Console.WriteLine($"UDP       {udp.Count,-9} {udp.Count(s => !s.V6),-9} {udp.Count(s => s.V6),-9}");
        Console.WriteLine($"TCP       {tcp.Count,-9} {tcp.Count(s => !s.V6),-9} {tcp.Count(s => s.V6),-9}");
        Console.WriteLine($"INET      {socks.Count,-9} {socks.Count(s => !s.V6),-9} {socks.Count(s => s.V6),-9}");
        Console.WriteLine($"FRAG      {0,-9} {0,-9} {0,-9}");
        return 0;
    }

    const string SsUsage =
        "Usage: ss [ OPTIONS ] [ FILTER ]\n" +
        "   -h, --help          this message\n" +
        "   -V, --version       output version information\n" +
        "   -n, --numeric       don't resolve service names\n" +
        "   -a, --all           display all sockets\n" +
        "   -l, --listening     display listening sockets\n" +
        "   -p, --processes     show process using socket\n" +
        "   -s, --summary       show socket usage summary\n" +
        "   -4, --ipv4          display only IP version 4 sockets\n" +
        "   -6, --ipv6          display only IP version 6 sockets\n" +
        "   -t, --tcp           display only TCP sockets\n" +
        "   -u, --udp           display only UDP sockets\n" +
        "   -H, --no-header     suppress header line\n" +
        "   FILTER := [ state STATE ] [ sport = :PORT ] [ dport = :PORT ]\n" +
        "   STATE  := { established | listening | connected | syn-sent | time-wait | close-wait | ... | all }";

    // ---- bridge ----

    static int RunBridge(string[] args)
    {
        var rest = args.SkipWhile(a => a.StartsWith('-') && a is not ("-h" or "-help" or "--help" or "-V" or "-Version")).ToArray();
        if (args.Any(a => a is "-h" or "-help" or "--help") || rest.FirstOrDefault() == "help")
        {
            Console.WriteLine("Usage: bridge [ OPTIONS ] OBJECT { COMMAND | help }\nwhere  OBJECT := { link }\n" +
                              "Lists the ports of the Windows Network Bridge (netsh bridge). fdb, vlan and mdb have no Windows counterpart.");
            return 0;
        }
        if (args.Any(a => a is "-V" or "-Version")) { Console.WriteLine($"bridge utility, iproute2-wintools-{Commands.Version}"); return 0; }
        var obj = rest.FirstOrDefault() ?? "link";
        if (Abbrev(obj, "fdb") || Abbrev(obj, "vlan") || Abbrev(obj, "mdb") || Abbrev(obj, "vni") || Abbrev(obj, "monitor"))
        {
            Console.Error.WriteLine($"bridge: Windows' Network Bridge doesn't expose \"{obj}\"; only \"bridge link\" is available");
            return 1;
        }
        if (!Abbrev(obj, "link")) throw new UsageException($"Object \"{obj}\" is unknown, try \"bridge help\".");
        var verb = rest.Skip(1).FirstOrDefault();
        if (verb != null && !IsShow(verb))
        {
            Console.Error.WriteLine("bridge: add or remove bridge ports in Network Connections (ncpa.cpl > Bridge Connections)");
            return 1;
        }

        var (code, output) = Capture("netsh", "bridge show adapter");
        if (code != 0) return 0;   // no bridge support on this edition: no ports
        var all = Interfaces();
        var master = all.FirstOrDefault(i => i.Description.Contains("Bridge", StringComparison.OrdinalIgnoreCase)
                                             && i.Description.Contains("Miniport", StringComparison.OrdinalIgnoreCase))
                     ?? all.FirstOrDefault(i => i.Description.Contains("Network Bridge", StringComparison.OrdinalIgnoreCase));
        foreach (var raw in output.Split('\n'))
        {
            // " 49  {GUID}  Adapter Name  IsBridged  Bridgeable  Compatibility"
            var parts = raw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 6 || !int.TryParse(parts[0], out var index) || !parts[1].StartsWith('{')) continue;
            var bridged = parts[^3];
            if (!bridged.Equals("Yes", StringComparison.OrdinalIgnoreCase)) continue;
            var iface = all.FirstOrDefault(i => i.Index == index);
            var name = iface?.Name ?? string.Join(" ", parts[2..^3]);
            var flags = iface == null ? "BROADCAST,MULTICAST" : string.Join(",", Flags(iface));
            Console.WriteLine($"{index}: {name}: <{flags}> mtu {iface?.Mtu ?? 1500}"
                              + (master != null ? $" master {master.Name}" : "")
                              + $" state {(iface?.Up == true ? "forwarding" : "disabled")} priority 32 cost 100");
        }
        return 0;
    }
}
