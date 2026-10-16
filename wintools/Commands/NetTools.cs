using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using static WinTools.IpHelper;

namespace WinTools;

// ifconfig / netstat / route / arp (net-tools): the classic Linux tools with their Linux output, read
// from the IP Helper API. Changes are handed to `ip` (and from there to netsh), so they need an
// elevated prompt and, like Linux, last until reboot. Names are never reverse-resolved through DNS.
static class NetTools
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("ifconfig", "Show or configure network interfaces (net-tools)", RunIfconfig);
        Commands.Add("netstat", "Show connections, routes, interface counters and statistics (net-tools)", RunNetstat);
        Commands.Add("route", "Show or change the IP routing table (net-tools)", RunRoute);
        Commands.Add("arp", "Show or change the ARP cache (net-tools)", RunArp);
    }

    static string Version => $"net-tools 2.10 (wintools {Commands.Version})";

    // ---- ifconfig ----

    static int RunIfconfig(string[] args)
    {
        var all = false;
        var shortList = false;
        var rest = new List<string>();
        foreach (var arg in args)
        {
            switch (arg)
            {
                case "-h" or "--help":
                    Console.WriteLine("Usage:\n  ifconfig [-a] [-s] [interface]\n  ifconfig interface [up|down] [mtu N] [ADDRESS [netmask MASK]]\n" +
                                      "                    [add|del ADDRESS/PREFIXLEN] [inet6 add|del ADDRESS/PREFIXLEN]\n" +
                                      "  -a  show all interfaces, including those without a link\n  -s  short list (like netstat -i)\n" +
                                      "Interfaces use their Windows names (quote names with spaces) or index numbers.\n" +
                                      "Changes go through netsh, last until reboot, and need an elevated prompt.");
                    return 0;
                case "-V" or "--version": Console.WriteLine(Version); return 0;
                case "-a": all = true; break;
                case "-s": shortList = true; break;
                case "-v": break;
                default:
                    if (arg.StartsWith('-') && rest.Count == 0) throw new UsageException($"unknown option {arg}");
                    rest.Add(arg);
                    break;
            }
        }
        var ifaces = Interfaces();
        if (rest.Count == 0)
        {
            var shown = ifaces.Where(i => all || i.Up).ToList();
            if (shortList) return InterfaceTable(shown);
            foreach (var iface in shown) IfconfigBlock(iface);
            return 0;
        }
        if (rest.Count == 1)
        {
            var iface = Find(ifaces, rest[0]);
            if (iface == null) { Console.Error.WriteLine($"{rest[0]}: error fetching interface information: Device not found"); return 1; }
            if (shortList) return InterfaceTable([iface]);
            IfconfigBlock(iface);
            return 0;
        }
        return IfconfigSet(ifaces, rest);
    }

    static void IfconfigBlock(Iface iface)
    {
        var flags = 1 + (iface.Loopback ? 8 : iface.PointToPoint ? 0x10 + 0x80 : 2) + (iface.Up ? 0x40 : 0) + (iface.Multicast && !iface.Loopback ? 0x1000 : 0);
        var names = new List<string> { "UP" };
        if (!iface.Loopback && !iface.PointToPoint) names.Add("BROADCAST");
        if (iface.Loopback) names.Add("LOOPBACK");
        if (iface.PointToPoint) names.Add("POINTOPOINT");
        if (iface.Up) names.Add("RUNNING");
        if (iface.PointToPoint) names.Add("NOARP");
        if (iface.Multicast && !iface.Loopback) names.Add("MULTICAST");
        Console.WriteLine($"{iface.Name}: flags={flags}<{string.Join(",", names)}>  mtu {iface.Mtu}");
        foreach (var a in iface.Addrs.Where(a => !a.V6))
            Console.WriteLine($"        inet {a.Address}  netmask {Mask(a.Prefix)}" + (a.Prefix < 31 && !iface.Loopback ? $"  broadcast {Broadcast(a.Address, a.Prefix)}" : ""));
        foreach (var a in iface.Addrs.Where(a => a.V6))
        {
            var scope = IpRoute.Scope(a.Address) switch { "host" => "0x10<host>", "link" => "0x20<link>", _ => "0x0<global>" };
            Console.WriteLine($"        inet6 {a.Address}  prefixlen {a.Prefix}  scopeid {scope}");
        }
        if (iface.Loopback) Console.WriteLine("        loop  txqueuelen 1000  (Local Loopback)");
        else if (iface.Mac.Length == 6) Console.WriteLine($"        ether {Mac(iface.Mac)}  txqueuelen 1000  (Ethernet)");
        else Console.WriteLine("        unspec 00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00  txqueuelen 1000  (UNSPEC)");
        var s = iface.Nic.GetIPStatistics();
        Console.WriteLine($"        RX packets {s.UnicastPacketsReceived + s.NonUnicastPacketsReceived}  bytes {s.BytesReceived} ({Human(s.BytesReceived)})");
        Console.WriteLine($"        RX errors {s.IncomingPacketsWithErrors}  dropped {s.IncomingPacketsDiscarded}  overruns 0  frame 0");
        Console.WriteLine($"        TX packets {s.UnicastPacketsSent + s.NonUnicastPacketsSent}  bytes {s.BytesSent} ({Human(s.BytesSent)})");
        Console.WriteLine($"        TX errors {s.OutgoingPacketsWithErrors}  dropped {s.OutgoingPacketsDiscarded} overruns 0  carrier 0  collisions 0");
        Console.WriteLine();
    }

    // net-tools 2.10's "(1.2 MiB)" byte counts: binary units, one decimal, truncated.
    static string Human(long bytes)
    {
        string[] units = ["B", "KiB", "MiB", "GiB", "TiB", "PiB"];
        var unit = 0;
        var scaled = (decimal)bytes;
        while (unit < units.Length - 1 && scaled > 1024) { scaled /= 1024; unit++; }
        var tenths = (long)(scaled * 10);
        return $"{tenths / 10}.{tenths % 10} {units[unit]}";
    }

    static int IfconfigSet(List<Iface> ifaces, List<string> args)
    {
        var dev = args[0];
        var iface = Find(ifaces, dev);
        var status = 0;
        for (var i = 1; i < args.Count; i++)
        {
            var arg = args[i];
            string Next() => ++i < args.Count ? args[i] : throw new UsageException($"{arg} needs an argument");
            switch (arg)
            {
                case "up" or "down": status |= IpRoute.RunIp(["link", "set", "dev", dev, arg]); break;
                case "mtu": status |= IpRoute.RunIp(["link", "set", "dev", dev, "mtu", Next()]); break;
                case "add": status |= IpRoute.RunIp(["addr", "add", Next(), "dev", dev]); break;
                case "del": status |= IpRoute.RunIp(["addr", "del", Next(), "dev", dev]); break;
                case "inet6":
                    var verb = Next();
                    if (verb is not ("add" or "del")) throw new UsageException("inet6 needs add or del");
                    status |= IpRoute.RunIp(["-6", "addr", verb, Next(), "dev", dev]);
                    break;
                case "inet": break;
                case "netmask":
                    var current = iface?.Addrs.FirstOrDefault(a => !a.V6)
                                  ?? throw new InvalidOperationException($"{dev} has no IPv4 address to apply a netmask to");
                    status |= SetAddress(Require(ifaces, dev), current.Address, Next());
                    break;
                case "broadcast" or "pointopoint" or "dstaddr" or "metric": Next(); break;
                case "hw":
                    Console.Error.WriteLine("ifconfig: Windows sets MAC addresses through the adapter driver; use macchanger instead");
                    return 1;
                case "arp" or "-arp" or "promisc" or "-promisc" or "allmulti" or "-allmulti" or "multicast" or "txqueuelen":
                    Console.Error.WriteLine($"ifconfig: \"{arg}\" can't be set on Windows network adapters");
                    return 1;
                default:
                    if (!IPAddress.TryParse(arg.Split('/')[0], out var address)) throw new UsageException($"{arg}: Unknown host");
                    var mask = arg.Contains('/') ? Mask(int.Parse(arg.Split('/')[1])).ToString() : null;
                    if (i + 1 < args.Count && args[i + 1] == "netmask") { i++; mask = Next(); }
                    if (address.AddressFamily == AddressFamily.InterNetworkV6)
                        status |= IpRoute.RunIp(["-6", "addr", "add", arg, "dev", dev]);
                    else
                        status |= SetAddress(Require(ifaces, dev), address, mask ?? ClassfulMask(address));
                    break;
            }
        }
        return status;
    }

    // ifconfig replaces an interface's address rather than adding one: netsh "set address ... static".
    static int SetAddress(Iface iface, IPAddress address, string mask)
    {
        if (!IPAddress.TryParse(mask, out var m)) throw new UsageException($"{mask}: invalid netmask");
        MaskToPrefix(m);
        return Netsh($"interface ipv4 set address name={iface.Index} source=static address={address} mask={m} store=active");
    }

    static string ClassfulMask(IPAddress a) => a.GetAddressBytes()[0] switch
    {
        < 128 => "255.0.0.0", < 192 => "255.255.0.0", _ => "255.255.255.0",
    };

    // ---- netstat ----

    static readonly string[] NetstatStates = ["UNKNOWN", "CLOSE", "LISTEN", "SYN_SENT", "SYN_RECV", "ESTABLISHED", "FIN_WAIT1",
                                              "FIN_WAIT2", "CLOSE_WAIT", "CLOSING", "LAST_ACK", "TIME_WAIT", "CLOSE"];

    static int RunNetstat(string[] args)
    {
        bool tcp = false, udp = false, listening = false, all = false, numeric = false, programs = false;
        bool routes = false, interfaces = false, statistics = false, v4 = false, v6 = false;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.StartsWith("--"))
            {
                switch (arg.Split('=')[0])
                {
                    case "--help": Console.WriteLine(NetstatUsage); return 0;
                    case "--version": Console.WriteLine(Version); return 0;
                    case "--tcp": tcp = true; break;
                    case "--udp": udp = true; break;
                    case "--listening": listening = true; break;
                    case "--all": all = true; break;
                    case "--numeric" or "--numeric-hosts" or "--numeric-ports": numeric = true; break;
                    case "--programs": programs = true; break;
                    case "--route": routes = true; break;
                    case "--interfaces": interfaces = true; break;
                    case "--statistics": statistics = true; break;
                    case "--inet" or "--ip" or "--protocol": v4 = true; break;
                    case "--inet6": v6 = true; break;
                    case "--extend" or "--wide" or "--verbose" or "--timers": break;
                    case "--groups" or "--masquerade" or "--unix" or "--raw" or "--continuous":
                        Console.Error.WriteLine($"netstat: {arg} isn't available on Windows");
                        return 1;
                    default: throw new UsageException($"unrecognized option '{arg}'");
                }
                continue;
            }
            if (!arg.StartsWith('-') || arg.Length < 2) throw new UsageException($"unexpected argument '{arg}'");
            foreach (var c in arg[1..])
            {
                switch (c)
                {
                    case 'h': Console.WriteLine(NetstatUsage); return 0;
                    case 'V': Console.WriteLine(Version); return 0;
                    case 't': tcp = true; break;
                    case 'u': udp = true; break;
                    case 'l': listening = true; break;
                    case 'a': all = true; break;
                    case 'n': numeric = true; break;
                    case 'p': programs = true; break;
                    case 'r': routes = true; break;
                    case 'i': interfaces = true; break;
                    case 's': statistics = true; break;
                    case '4': v4 = true; break;
                    case '6': v6 = true; break;
                    case 'e' or 'W' or 'v' or 'o' or 'N' or 'w': break;
                    case 'A':
                        var family = ++i < args.Length ? args[i] : throw new UsageException("-A needs an address family");
                        if (family.Contains("inet6")) v6 = true;
                        if (family.Split(',').Contains("inet")) v4 = true;
                        break;
                    case 'g' or 'M' or 'x' or 'c':
                        Console.Error.WriteLine($"netstat: -{c} isn't available on Windows");
                        return 1;
                    default: throw new UsageException($"invalid option -- '{c}'");
                }
            }
        }
        if (routes) return v6 && !v4 ? RouteTable6() : RouteTable(numeric, netstatColumns: true);
        if (interfaces) return InterfaceTable(Interfaces().Where(i => all || i.Up).ToList());
        if (statistics) return Statistics(v6 && !v4, tcp, udp);
        if (!tcp && !udp) tcp = udp = true;
        if (!v4 && !v6) v4 = v6 = true;

        var names = programs ? ProcessNames() : null;
        Console.WriteLine($"Active Internet connections ({(all ? "servers and established" : listening ? "only servers" : "w/o servers")})");
        Console.WriteLine("Proto Recv-Q Send-Q Local Address           Foreign Address         State      " + (programs ? " PID/Program name" : ""));
        foreach (var s in Sockets(tcp, udp, v4, v6))
        {
            var isListen = !s.Tcp || s.State == TcpListen;
            if (!all && (listening ? !isListen : isListen)) continue;
            var proto = (s.Tcp ? "tcp" : "udp") + (s.V6 ? "6" : "");
            var state = s.Tcp ? NetstatStates[Math.Clamp(s.State, 0, 12)] : "";
            var local = Endpoint(s.Local, s.LocalPort, s.Tcp, numeric, false);
            var remote = Endpoint(s.Remote, s.RemotePort, s.Tcp, numeric, isListen);
            var line = $"{proto,-5} {0,6} {0,6} {local,-23} {remote,-23} {state,-11}";
            if (programs) line += " " + (names.TryGetValue(s.Pid, out var name) ? $"{s.Pid}/{name}" : s.Pid == 0 ? "-" : $"{s.Pid}/-");
            Console.WriteLine(line.TrimEnd());
        }
        return 0;
    }

    static string Endpoint(IPAddress address, int port, bool tcp, bool numeric, bool wildcardPort)
    {
        var v6 = address.AddressFamily == AddressFamily.InterNetworkV6;
        var host = Unscoped(address).ToString();
        if (!numeric && IPAddress.IsLoopback(address)) host = "localhost";
        else if (!numeric && v6) host = $"[{host}]";
        var portText = wildcardPort ? "*" : numeric ? port.ToString() : Service(port, tcp);
        return $"{host}:{portText}";
    }

    // Shared by `netstat -i` and `ifconfig -s`.
    static int InterfaceTable(List<Iface> ifaces)
    {
        var width = Math.Max(5, ifaces.Count == 0 ? 0 : ifaces.Max(i => i.Name.Length));
        Console.WriteLine("Kernel Interface table");
        Console.WriteLine($"{"Iface".PadRight(width)}   MTU    RX-OK RX-ERR RX-DRP RX-OVR    TX-OK TX-ERR TX-DRP TX-OVR Flg");
        foreach (var iface in ifaces)
        {
            var s = iface.Nic.GetIPStatistics();
            var flg = (iface.Loopback || iface.PointToPoint ? "" : "B") + (iface.Loopback ? "L" : "") + (iface.Multicast && !iface.Loopback ? "M" : "")
                      + (iface.PointToPoint ? "OP" : "") + (iface.Up ? "R" : "") + "U";
            Console.WriteLine($"{iface.Name.PadRight(width)} {iface.Mtu,5} {s.UnicastPacketsReceived + s.NonUnicastPacketsReceived,8} " +
                              $"{s.IncomingPacketsWithErrors,6} {s.IncomingPacketsDiscarded,6} {0,6} {s.UnicastPacketsSent + s.NonUnicastPacketsSent,8} " +
                              $"{s.OutgoingPacketsWithErrors,6} {s.OutgoingPacketsDiscarded,6} {0,6} {flg}");
        }
        return 0;
    }

    static int Statistics(bool v6, bool tcpOnly, bool udpOnly)
    {
        var g = IPGlobalProperties.GetIPGlobalProperties();
        var both = !tcpOnly && !udpOnly;
        if (both)
        {
            var ip = v6 ? g.GetIPv6GlobalStatistics() : g.GetIPv4GlobalStatistics();
            Console.WriteLine(v6 ? "Ip6:" : "Ip:");
            Console.WriteLine($"    Forwarding: {(ip.ForwardingEnabled ? 1 : 2)}");
            Console.WriteLine($"    {ip.ReceivedPackets} total packets received");
            Console.WriteLine($"    {ip.ReceivedPacketsWithAddressErrors} with invalid addresses");
            Console.WriteLine($"    {ip.ReceivedPacketsForwarded} forwarded");
            Console.WriteLine($"    {ip.ReceivedPacketsDiscarded} incoming packets discarded");
            Console.WriteLine($"    {ip.ReceivedPacketsDelivered} incoming packets delivered");
            Console.WriteLine($"    {ip.OutputPacketRequests} requests sent out");
            Console.WriteLine($"    {ip.OutputPacketsDiscarded} outgoing packets dropped");
            Console.WriteLine($"    {ip.OutputPacketsWithNoRoute} dropped because of missing route");
            Console.WriteLine($"    {ip.PacketsReassembled} reassemblies ok, {ip.PacketReassemblyFailures} failed");
            Console.WriteLine($"    {ip.PacketsFragmented} fragments created, {ip.PacketFragmentFailures} failed");

            var icmpIn = new List<(string, long)>();
            var icmpOut = new List<(string, long)>();
            long received, receiveErrors, sent, sendErrors;
            if (v6)
            {
                var icmp = g.GetIcmpV6Statistics();
                (received, receiveErrors, sent, sendErrors) = (icmp.MessagesReceived, icmp.ErrorsReceived, icmp.MessagesSent, icmp.ErrorsSent);
                icmpIn.AddRange([("destination unreachable", icmp.DestinationUnreachableMessagesReceived), ("time exceeded", icmp.TimeExceededMessagesReceived),
                                 ("echo requests", icmp.EchoRequestsReceived), ("echo replies", icmp.EchoRepliesReceived),
                                 ("neighbour solicits", icmp.NeighborSolicitsReceived), ("neighbour advertisements", icmp.NeighborAdvertisementsReceived)]);
                icmpOut.AddRange([("destination unreachable", icmp.DestinationUnreachableMessagesSent), ("time exceeded", icmp.TimeExceededMessagesSent),
                                  ("echo requests", icmp.EchoRequestsSent), ("echo replies", icmp.EchoRepliesSent),
                                  ("neighbour solicits", icmp.NeighborSolicitsSent), ("neighbour advertisements", icmp.NeighborAdvertisementsSent)]);
            }
            else
            {
                var icmp = g.GetIcmpV4Statistics();
                (received, receiveErrors, sent, sendErrors) = (icmp.MessagesReceived, icmp.ErrorsReceived, icmp.MessagesSent, icmp.ErrorsSent);
                icmpIn.AddRange([("destination unreachable", icmp.DestinationUnreachableMessagesReceived), ("timeout in transit", icmp.TimeExceededMessagesReceived),
                                 ("redirects", icmp.RedirectsReceived), ("echo requests", icmp.EchoRequestsReceived), ("echo replies", icmp.EchoRepliesReceived)]);
                icmpOut.AddRange([("destination unreachable", icmp.DestinationUnreachableMessagesSent), ("time exceeded", icmp.TimeExceededMessagesSent),
                                  ("redirect", icmp.RedirectsSent), ("echo requests", icmp.EchoRequestsSent), ("echo replies", icmp.EchoRepliesSent)]);
            }
            Console.WriteLine(v6 ? "Icmp6:" : "Icmp:");
            Console.WriteLine($"    {received} ICMP messages received");
            Console.WriteLine($"    {receiveErrors} input ICMP message failed");
            Console.WriteLine("    ICMP input histogram:");
            foreach (var (name, count) in icmpIn.Where(x => x.Item2 > 0)) Console.WriteLine($"        {name}: {count}");
            Console.WriteLine($"    {sent} ICMP messages sent");
            Console.WriteLine($"    {sendErrors} ICMP messages failed");
            Console.WriteLine("    ICMP output histogram:");
            foreach (var (name, count) in icmpOut.Where(x => x.Item2 > 0)) Console.WriteLine($"        {name}: {count}");
        }
        if (both || tcpOnly)
        {
            var t = v6 ? g.GetTcpIPv6Statistics() : g.GetTcpIPv4Statistics();
            Console.WriteLine("Tcp:");
            Console.WriteLine($"    {t.ConnectionsInitiated} active connection openings");
            Console.WriteLine($"    {t.ConnectionsAccepted} passive connection openings");
            Console.WriteLine($"    {t.FailedConnectionAttempts} failed connection attempts");
            Console.WriteLine($"    {t.ResetConnections} connection resets received");
            Console.WriteLine($"    {t.CurrentConnections} connections established");
            Console.WriteLine($"    {t.SegmentsReceived} segments received");
            Console.WriteLine($"    {t.SegmentsSent} segments sent out");
            Console.WriteLine($"    {t.SegmentsResent} segments retransmitted");
            Console.WriteLine($"    {t.ErrorsReceived} bad segments received");
            Console.WriteLine($"    {t.ResetsSent} resets sent");
        }
        if (both || udpOnly)
        {
            var u = v6 ? g.GetUdpIPv6Statistics() : g.GetUdpIPv4Statistics();
            Console.WriteLine(v6 ? "Udp6:" : "Udp:");
            Console.WriteLine($"    {u.DatagramsReceived} packets received");
            Console.WriteLine($"    {u.IncomingDatagramsDiscarded} packets to unknown port received");
            Console.WriteLine($"    {u.IncomingDatagramsWithErrors} packet receive errors");
            Console.WriteLine($"    {u.DatagramsSent} packets sent");
        }
        return 0;
    }

    const string NetstatUsage =
        "usage: netstat [-vWeenNac] {-i|-r|-s}\n" +
        "       netstat [-vWnNcaeol] [<Socket> ...]\n\n" +
        "        -r, --route              display routing table\n" +
        "        -i, --interfaces         display interface table\n" +
        "        -s, --statistics         display networking statistics (like SNMP)\n" +
        "        -n, --numeric            don't resolve port names\n" +
        "        -p, --programs           display PID/Program name for sockets\n" +
        "        -l, --listening          display listening server sockets\n" +
        "        -a, --all                display all sockets (default: connected)\n" +
        "        -t, --tcp  -u, --udp     select protocols\n" +
        "        -4, --inet  -6, --inet6  select address families (or -A inet,inet6)";

    // ---- route ----

    static int RunRoute(string[] args)
    {
        var numeric = false;
        var v6 = false;
        var rest = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help":
                    Console.WriteLine("Usage: route [-nNvee] [-FC] [-A family|-4|-6]          List the routing table\n" +
                                      "       route [-v] [-A family|-4|-6] add|del -net|-host TARGET [netmask MASK] [gw GW] [metric M] [[dev] If]\n" +
                                      "       route add|del default gw GW [dev If]\n" +
                                      "Interfaces use their Windows names or index numbers; changes go through netsh,\n" +
                                      "last until reboot, and need an elevated prompt.");
                    return 0;
                case "-V" or "--version": Console.WriteLine(Version); return 0;
                case "-n" or "--numeric": numeric = true; break;
                case "-6": v6 = true; break;
                case "-4": v6 = false; break;
                case "-A":
                    v6 = (++i < args.Length ? args[i] : throw new UsageException("-A needs an address family")) == "inet6";
                    break;
                case "-v" or "-e" or "-ee" or "-F" or "-N" or "-ne" or "-en": break;
                case "-C":
                    Console.Error.WriteLine("route: Windows has no routing cache");
                    return 1;
                default:
                    if (args[i].StartsWith('-') && rest.Count == 0 && args[i] is not ("-net" or "-host"))
                    {
                        // Combined short options such as -nee.
                        if (args[i][1..].All(c => c is 'n' or 'e' or 'v' or 'F' or 'N')) { numeric |= args[i].Contains('n'); break; }
                        throw new UsageException($"invalid option {args[i]}");
                    }
                    rest.Add(args[i]);
                    break;
            }
        }
        if (rest.Count == 0) return v6 ? RouteTable6() : RouteTable(numeric, netstatColumns: false);
        if (rest[0] is not ("add" or "del")) throw new UsageException($"unknown command {rest[0]} (see route --help)");
        return RouteChange(rest[0], rest.Skip(1).ToList(), v6);
    }

    static int RouteChange(string verb, List<string> args, bool v6)
    {
        string target = null, mask = null, gw = null, dev = null, metric = null;
        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            string Next() => ++i < args.Count ? args[i] : throw new UsageException($"{arg} needs an argument");
            switch (arg)
            {
                case "-net" or "-host": target = Next(); break;
                case "netmask" or "genmask": mask = Next(); break;
                case "gw": gw = Next(); break;
                case "dev": dev = Next(); break;
                case "metric": metric = Next(); break;
                case "mss" or "window" or "irtt": Next(); break;
                case "mod" or "dyn" or "reinstate": break;
                case "reject":
                    Console.Error.WriteLine("route: Windows has no reject routes");
                    return 1;
                case "default": target = "default"; break;
                default:
                    if (target == null) target = arg;
                    else dev = arg;   // a trailing bare word is the interface, as in Linux
                    break;
            }
        }
        if (target == null) throw new UsageException("missing target (see route --help)");
        if (mask != null && !target.Contains('/'))
            target += "/" + (IPAddress.TryParse(mask, out var m) ? MaskToPrefix(m) : throw new UsageException($"{mask}: invalid netmask"));
        var ipArgs = new List<string>();
        if (v6) ipArgs.Add("-6");
        ipArgs.AddRange(["route", verb, target]);
        if (gw != null) ipArgs.AddRange(["via", gw]);
        if (dev != null) ipArgs.AddRange(["dev", dev]);
        if (metric != null) ipArgs.AddRange(["metric", metric]);
        return IpRoute.RunIp([.. ipArgs]);
    }

    // `route -n` and `netstat -rn`: the IPv4 main table.
    static int RouteTable(bool numeric, bool netstatColumns)
    {
        var byIndex = Interfaces().ToDictionary(i => i.Index);
        Console.WriteLine("Kernel IP routing table");
        Console.WriteLine(netstatColumns
            ? "Destination     Gateway         Genmask         Flags   MSS Window  irtt Iface"
            : "Destination     Gateway         Genmask         Flags Metric Ref    Use Iface");
        var routes = Routes(AF_INET).Where(r => IpRoute.RouteType(r, byIndex) == "unicast")
            .OrderByDescending(r => r.Prefix == 0).ThenBy(r => IpRoute.EffectiveMetric(r, byIndex)).ToList();
        foreach (var r in routes)
        {
            var dest = !numeric && r.Prefix == 0 ? "default" : r.Dest.ToString();
            var gateway = r.Gateway?.ToString() ?? "0.0.0.0";
            var flags = "U" + (r.Gateway != null ? "G" : "") + (r.Prefix == 32 ? "H" : "");
            var name = IfName(byIndex, r.IfIndex);
            Console.WriteLine(netstatColumns
                ? $"{dest,-15} {gateway,-15} {Mask(r.Prefix),-15} {flags,-5} {0,5} {0,-6} {0,5} {name}"
                : $"{dest,-15} {gateway,-15} {Mask(r.Prefix),-15} {flags,-5} {IpRoute.EffectiveMetric(r, byIndex),-6} {0,-6} {0,3} {name}");
        }
        return 0;
    }

    static int RouteTable6()
    {
        var byIndex = Interfaces().ToDictionary(i => i.Index);
        Console.WriteLine("Kernel IPv6 routing table");
        Console.WriteLine("Destination                    Next Hop                   Flag Met Ref Use If");
        foreach (var r in Routes(AF_INET6).Where(r => !r.Dest.IsIPv6Multicast))
        {
            var local = IpRoute.RouteType(r, byIndex) == "local";
            var flags = "U" + (r.Gateway != null ? "G" : "") + (r.Prefix == 128 ? "H" : "") + (r.Origin == 3 ? "A" : "");
            if (local) flags = "Un";
            Console.WriteLine($"{$"{r.Dest}/{r.Prefix}",-30} {r.Gateway?.ToString() ?? "::",-26} {flags,-4} {IpRoute.EffectiveMetric(r, byIndex),-3} {1,-1} {0,5} {IfName(byIndex, r.IfIndex)}");
        }
        return 0;
    }

    // ---- arp ----

    static int RunArp(string[] args)
    {
        var bsd = false;
        string dev = null, host = null, set = null, setMac = null, delete = null, file = null;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string Next() => ++i < args.Length ? args[i] : throw new UsageException($"option requires an argument -- '{arg.TrimStart('-')}'");
            switch (arg)
            {
                case "-h" or "--help":
                    Console.WriteLine("Usage:\n  arp [-vn]  [<HW>] [-i <if>] [-a] [<hostname>]             <-Display ARP cache\n" +
                                      "  arp [-v]   [-i <if>] -d  <host> [pub]                    <-Delete ARP entry\n" +
                                      "  arp [-vnD] [<HW>] [-i <if>] -f  [<filename>]             <-Add entry from file\n" +
                                      "  arp [-v]   [<HW>] [-i <if>] -s  <host> <hwaddr> [temp]   <-Add entry\n\n" +
                                      "        -a                       display (all) hosts in alternative (BSD) style\n" +
                                      "        -e                       display (all) hosts in default (Linux) style\n" +
                                      "        -n, --numeric            don't resolve names\n" +
                                      "        -i, --device             specify network interface (e.g. Wi-Fi)\n" +
                                      "Changes go through netsh and need an elevated prompt.");
                    return 0;
                case "-V" or "--version": Console.WriteLine(Version); return 0;
                case "-a" or "--all": bsd = true; break;
                case "-e": bsd = false; break;
                case "-n" or "--numeric" or "-v" or "--verbose" or "-D" or "--use-device": break;
                case "-H" or "--hw-type" or "-t": if (Next() != "ether") throw new UsageException("only the ether hardware type exists here"); break;
                case "-i" or "--device": dev = Next(); break;
                case "-s" or "--set": set = Next(); setMac = Next(); break;
                case "-d" or "--delete": delete = Next(); break;
                case "-f" or "--file": file = i + 1 < args.Length ? args[++i] : "/etc/ethers"; break;
                case "temp" or "pub" or "netmask": break;
                default:
                    if (arg.StartsWith('-')) throw new UsageException($"invalid option -- '{arg.TrimStart('-')}'");
                    host = arg;
                    break;
            }
        }
        var ifaces = Interfaces();
        if (set != null) return ArpSet(ifaces, set, setMac, dev);
        if (delete != null) return ArpDelete(ifaces, delete, dev);
        if (file != null)
        {
            if (!File.Exists(file)) throw new IOException($"cannot open etherfile {file} !");
            var status = 0;
            foreach (var line in File.ReadLines(file).Select(l => l.Split('#')[0].Trim()).Where(l => l.Length > 0))
            {
                var parts = line.Split((char[])[' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2) status |= ArpSet(ifaces, parts[1], parts[0], dev);
            }
            return status;
        }

        var byIndex = ifaces.ToDictionary(i => i.Index);
        var devIface = dev == null ? null : Require(ifaces, dev);
        IPAddress hostIp = host == null ? null : IpRoute.ResolveOne(host, AddressFamily.InterNetwork);
        var entries = Neighbors(AF_INET).Where(n => !IsNoiseNeighbor(n, byIndex))
            .Where(n => devIface == null || n.IfIndex == devIface.Index)
            .Where(n => hostIp == null || n.Address.Equals(hostIp)).ToList();
        if (host != null && entries.Count == 0)
        {
            Console.WriteLine($"{host} ({hostIp}) -- no entry");
            return 1;
        }
        if (!bsd) Console.WriteLine("Address                  HWtype  HWaddress           Flags Mask            Iface");
        foreach (var n in entries)
        {
            var complete = n.State is >= 2 and <= 6 && n.Mac.Any(b => b != 0);
            var permanent = n.State == 6;
            var name = IfName(byIndex, n.IfIndex);
            if (bsd)
                Console.WriteLine(complete
                    ? $"? ({n.Address}) at {Mac(n.Mac)} [ether]{(permanent ? " PERM" : "")} on {name}"
                    : $"? ({n.Address}) at <incomplete> on {name}");
            else
                Console.WriteLine(complete
                    ? $"{n.Address,-23}  {"ether",-8}{Mac(n.Mac),-20}{(permanent ? "CM" : "C"),-6}{"",-16}{name}"
                    : $"{n.Address,-23}  {"",-8}{"(incomplete)",-20}{"",-6}{"",-16}{name}");
        }
        return 0;
    }

    static int ArpSet(List<Iface> ifaces, string host, string mac, string dev)
    {
        var ip = IpRoute.ResolveOne(host, AddressFamily.InterNetwork);
        dev ??= ifaces.FirstOrDefault(i => i.Addrs.Any(a => !a.V6 && InPrefix(ip, a.Address, a.Prefix)))?.Name
                ?? throw new InvalidOperationException($"{host}: no interface on that network (give one with -i)");
        return IpRoute.RunIp(["neigh", "add", ip.ToString(), "lladdr", mac, "dev", dev]);
    }

    static int ArpDelete(List<Iface> ifaces, string host, string dev)
    {
        var ip = IpRoute.ResolveOne(host, AddressFamily.InterNetwork);
        var byIndex = ifaces.ToDictionary(i => i.Index);
        if (dev == null)
        {
            var entry = Neighbors(AF_INET).FirstOrDefault(n => n.Address.Equals(ip));
            if (entry == null) { Console.Error.WriteLine($"No ARP entry for {host}"); return 1; }
            dev = IfName(byIndex, entry.IfIndex);
        }
        return IpRoute.RunIp(["neigh", "del", ip.ToString(), "dev", dev]);
    }
}
