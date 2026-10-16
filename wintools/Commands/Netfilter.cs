using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace WinTools;

// The netfilter/netlink userspace libraries' tools on their Windows counterparts:
//   libnetfilter_conntrack  conntrack       the TCP connection table (GetExtendedTcpTable), events by polling,
//                                           -D resets connections (SetTcpEntry)
//   libnetfilter-queue      nf-queue        packets handed to user space live, through Packet Monitor (pktmon)
//   libnftnl                nft-*-get       Windows Firewall (its rules in the registry) as an nftables ruleset
//   libmnl                  rtnl-*          rtnetlink's link/address/route/neighbour dumps and link events (IP Helper)
//   libnfnetlink            nfnl-iftest     the interface index table nfnetlink keeps (libnfnetlink's iftest)
// None of them has a C library on Windows: programs use iphlpapi and the Windows Filtering Platform.
static unsafe class Netfilter
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("conntrack", "List, watch and reset tracked TCP connections", Conntrack);
        Commands.Add("nf-queue", "Show packets passing the network stack live (Packet Monitor)", NfQueue);
        Commands.Add("nft-ruleset-get", "Print the Windows Firewall as an nftables ruleset", a => NftGet(a, "ruleset"));
        Commands.Add("nft-table-get", "List the Windows Firewall's nftables tables", a => NftGet(a, "table"));
        Commands.Add("nft-chain-get", "List the Windows Firewall's chains and policies", a => NftGet(a, "chain"));
        Commands.Add("nft-rule-get", "List the Windows Firewall's rules in nftables syntax", a => NftGet(a, "rule"));
        Commands.Add("rtnl-link-dump", "Dump network links (rtnetlink style)", RtnlLinkDump);
        Commands.Add("rtnl-addr-dump", "Dump interface addresses (rtnetlink style)", RtnlAddrDump);
        Commands.Add("rtnl-route-dump", "Dump the routing table (rtnetlink style)", RtnlRouteDump);
        Commands.Add("rtnl-neigh-dump", "Dump the neighbour (ARP/NDP) cache (rtnetlink style)", RtnlNeighDump);
        Commands.Add("rtnl-link-event", "Print link events as they happen (rtnetlink style)", RtnlLinkEvent);
        Commands.Add("nfnl-iftest", "Print the interface index table and its changes", NfnlIftest);
    }

    // ---- conntrack ----

    sealed class Flow
    {
        public AddressFamily Family;
        public IPAddress Src, Dst;
        public int SPort, DPort, Pid;
        public string State;
        public string Key => $"{Src}|{SPort}|{Dst}|{DPort}";
    }

    static readonly string[] TcpStates =
        ["NONE", "CLOSE", "LISTEN", "SYN_SENT", "SYN_RECV", "ESTABLISHED", "FIN_WAIT", "FIN_WAIT", "CLOSE_WAIT", "CLOSING", "LAST_ACK", "TIME_WAIT", "CLOSE"];

    static int Conntrack(string[] args)
    {
        string command = null, proto = null, state = null;
        IPAddress src = null, dst = null;
        int? sport = null, dport = null;
        AddressFamily? family = null;
        var extended = false;
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Value() => ++i < args.Length ? args[i] : throw new UsageException($"option `{a}' requires an argument");
            switch (a)
            {
                case "-h" or "--help":
                    Console.WriteLine("Command line interface for the connection tracking system. Version 1.4.8 (Windows TCP table)\n" +
                                      "Usage: conntrack [commands] [options]\n\nCommands:\n" +
                                      "  -L [table] [options]\t\tList conntrack table\n  -E [table] [options]\t\tShow events (polled every second)\n" +
                                      "  -C [table]\t\t\t\tShow counter\n  -S\t\t\t\t\tShow statistics\n" +
                                      "  -D [table] parameters\t\tDelete conntrack: resets the matching TCP connections (IPv4, administrator)\n" +
                                      "  -F [table]\t\t\t\tFlush table (not available: it would reset every connection)\n\n" +
                                      "Parameters and options:\n  -p, --proto proto\t\tLayer 4 protocol (tcp; Windows tracks no UDP flows)\n" +
                                      "  -s, --src, --orig-src ip\t\tSource address from original direction\n  -d, --dst, --orig-dst ip\t\tDestination address from original direction\n" +
                                      "  --sport, --orig-port-src port\tSource port in original direction\n  --dport, --orig-port-dst port\tDestination port in original direction\n" +
                                      "  --state state\t\t\tTCP state, e.g. ESTABLISHED\n  -f, --family proto\t\tLayer 3 protocol: ipv4 or ipv6\n  -o, --output type\t\tOutput format: extended");
                    return 0;
                case "-V" or "--version": Console.WriteLine("conntrack v1.4.8 (conntrack-tools)"); return 0;
                case "-L" or "--dump" or "-E" or "--event" or "-C" or "--count" or "-S" or "--stats" or "-D" or "--delete" or "-F" or "--flush":
                    if (command != null) throw new UsageException("Invalid parameters: only one command at a time");
                    command = a switch
                    {
                        "-L" or "--dump" => "L", "-E" or "--event" => "E", "-C" or "--count" => "C",
                        "-S" or "--stats" => "S", "-D" or "--delete" => "D", _ => "F",
                    };
                    if (i + 1 < args.Length && args[i + 1] is "conntrack" or "expect" or "dying" or "unconfirmed") i++;
                    break;
                case "-p" or "--proto" or "--protonum": proto = Value().ToLowerInvariant(); break;
                case "-s" or "--src" or "--orig-src": src = Ip(Value()); break;
                case "-d" or "--dst" or "--orig-dst": dst = Ip(Value()); break;
                case "--sport" or "--orig-port-src": sport = Port(Value()); break;
                case "--dport" or "--orig-port-dst": dport = Port(Value()); break;
                case "--state": state = Value().ToUpperInvariant(); break;
                case "-f" or "--family":
                    family = Value() switch { "ipv4" => AddressFamily.InterNetwork, "ipv6" => AddressFamily.InterNetworkV6, var f => throw new UsageException($"unknown family {f}") };
                    break;
                case "-o" or "--output": extended = Value().Split(',').Contains("extended"); break;
                case "-n" or "--src-nat" or "-g" or "--dst-nat" or "--any-nat":
                    throw new UsageException($"{a}: Windows tracks no NAT state for connections");
                case "-z" or "--zero": break;
                default: throw new UsageException($"unknown option `{a}'");
            }
        }
        command ??= "L";
        // Only TCP has a connection table on Windows; other protocols match nothing.
        var tcp = proto is null or "tcp" or "6";
        bool Match(Flow f) =>
            tcp && (family == null || f.Family == family) && (src == null || f.Src.Equals(src)) && (dst == null || f.Dst.Equals(dst))
            && (sport == null || f.SPort == sport) && (dport == null || f.DPort == dport) && (state == null || f.State == state);

        switch (command)
        {
            case "L":
            {
                var flows = Flows().Where(Match).ToList();
                foreach (var f in flows) Console.WriteLine(FlowLine(f, extended));
                Console.Error.WriteLine($"conntrack v1.4.8 (conntrack-tools): {flows.Count} flow entries have been shown.");
                return 0;
            }
            case "C":
                Console.WriteLine(Flows().Count(Match));
                return 0;
            case "S":
            {
                var stats = new MIB_TCPSTATS();
                var v4 = GetTcpStatisticsEx(ref stats, 2) == 0 ? stats : default;
                var v6 = GetTcpStatisticsEx(ref stats, 23) == 0 ? stats : default;
                Console.WriteLine($"cpu=0   \tfound={v4.dwCurrEstab + v6.dwCurrEstab} invalid={v4.dwInErrs + v6.dwInErrs} insert={v4.dwActiveOpens + v4.dwPassiveOpens + v6.dwActiveOpens + v6.dwPassiveOpens} " +
                                  $"insert_failed={v4.dwAttemptFails + v6.dwAttemptFails} drop=0 early_drop=0 error={v4.dwOutRsts + v6.dwOutRsts} search_restart=0 clash_resolve=0 chaintoolong=0");
                return 0;
            }
            case "E":
            {
                using var stop = new CancellationTokenSource();
                Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
                var known = Flows().Where(Match).ToDictionary(f => f.Key);
                while (!stop.IsCancellationRequested)
                {
                    stop.Token.WaitHandle.WaitOne(1000);
                    var now = Flows().Where(Match).ToDictionary(f => f.Key);
                    foreach (var (key, f) in now)
                    {
                        if (!known.TryGetValue(key, out var old)) Console.WriteLine($"    [NEW] {FlowLine(f, extended)}");
                        else if (old.State != f.State) Console.WriteLine($" [UPDATE] {FlowLine(f, extended)}");
                    }
                    foreach (var (key, f) in known)
                        if (!now.ContainsKey(key)) Console.WriteLine($"[DESTROY] {FlowLine(f, extended)}");
                    known = now;
                }
                return 0;
            }
            case "D":
            {
                if (src == null && dst == null && sport == null && dport == null)
                    throw new UsageException("-D needs at least one of -s, -d, --sport, --dport");
                var deleted = 0;
                foreach (var f in Flows().Where(Match))
                {
                    if (f.Family != AddressFamily.InterNetwork || f.State == "LISTEN") continue;
                    var row = new MIB_TCPROW
                    {
                        dwState = 12,   // MIB_TCP_STATE_DELETE_TCB
                        dwLocalAddr = (uint)BitConverter.ToInt32(f.Src.GetAddressBytes(), 0),
                        dwLocalPort = (uint)IPAddress.HostToNetworkOrder((short)f.SPort) & 0xffff,
                        dwRemoteAddr = (uint)BitConverter.ToInt32(f.Dst.GetAddressBytes(), 0),
                        dwRemotePort = (uint)IPAddress.HostToNetworkOrder((short)f.DPort) & 0xffff,
                    };
                    var err = SetTcpEntry(ref row);
                    if (err != 0) { Console.Error.WriteLine($"conntrack: could not reset {FlowLine(f, false)}: {Marshal.GetPInvokeErrorMessage((int)err)}"); continue; }
                    Console.WriteLine(FlowLine(f, extended));
                    deleted++;
                }
                Console.Error.WriteLine($"conntrack v1.4.8 (conntrack-tools): {deleted} flow entries have been deleted.");
                return deleted > 0 ? 0 : 1;
            }
            default:
                Console.Error.WriteLine("conntrack: Windows keeps no tracking table apart from the connections themselves; flushing would reset every TCP connection");
                return 1;
        }
    }

    static IPAddress Ip(string s) => IPAddress.TryParse(s, out var ip) ? ip : throw new UsageException($"Bad parameter `{s}'");
    static int Port(string s) => int.TryParse(s, out var p) && p is >= 0 and <= 65535 ? p : throw new UsageException($"Bad port `{s}'");

    static string FlowLine(Flow f, bool extended)
    {
        var assured = f.State == "ESTABLISHED" ? " [ASSURED]" : "";
        var reply = f.State is "SYN_SENT" ? " [UNREPLIED]" : "";
        var line = $"tcp      6 0 {f.State} src={f.Src} dst={f.Dst} sport={f.SPort} dport={f.DPort}{reply} src={f.Dst} dst={f.Src} sport={f.DPort} dport={f.SPort}{assured} mark=0 use=1";
        return extended ? $"{(f.Family == AddressFamily.InterNetwork ? "ipv4     2" : "ipv6     10")} {line}" : line;
    }

    // Every TCP connection except listeners (which are sockets, not flows).
    static List<Flow> Flows()
    {
        var flows = new List<Flow>();
        foreach (var af in new[] { AF_INET, AF_INET6 })
        {
            uint size = 0;
            GetExtendedTcpTable(null, ref size, false, af, TCP_TABLE_OWNER_PID_ALL, 0);
            var buffer = new byte[size + 4096];
            fixed (byte* p = buffer)
            {
                size = (uint)buffer.Length;
                if (GetExtendedTcpTable(p, ref size, false, af, TCP_TABLE_OWNER_PID_ALL, 0) != 0) continue;
                var count = *(uint*)p;
                for (uint i = 0; i < count; i++)
                {
                    Flow f;
                    if (af == AF_INET)
                    {
                        var r = (MIB_TCPROW_OWNER_PID*)(p + 4) + i;
                        f = new Flow
                        {
                            Family = AddressFamily.InterNetwork, State = State(r->dwState),
                            Src = new IPAddress(r->dwLocalAddr), SPort = NetPort(r->dwLocalPort),
                            Dst = new IPAddress(r->dwRemoteAddr), DPort = NetPort(r->dwRemotePort), Pid = (int)r->dwOwningPid,
                        };
                    }
                    else
                    {
                        var r = (MIB_TCP6ROW_OWNER_PID*)(p + 4) + i;
                        f = new Flow
                        {
                            Family = AddressFamily.InterNetworkV6, State = State(r->dwState),
                            Src = new IPAddress(new ReadOnlySpan<byte>(r->ucLocalAddr, 16), r->dwLocalScopeId), SPort = NetPort(r->dwLocalPort),
                            Dst = new IPAddress(new ReadOnlySpan<byte>(r->ucRemoteAddr, 16), r->dwRemoteScopeId), DPort = NetPort(r->dwRemotePort),
                            Pid = (int)r->dwOwningPid,
                        };
                    }
                    if (f.State != "LISTEN") flows.Add(f);
                }
            }
        }
        return flows;
    }

    static string State(uint s) => s < TcpStates.Length ? TcpStates[s] : "NONE";
    static int NetPort(uint p) => (int)(((p & 0xff) << 8) | ((p >> 8) & 0xff));

    // ---- nf-queue ----

    static int NfQueue(string[] args)
    {
        var queue = 0;
        foreach (var a in args)
        {
            if (a is "-h" or "--help")
            {
                Console.WriteLine("Usage: nf-queue [queue_num]\n\nPrints the packets passing the network stack as they arrive (Packet Monitor, pktmon;\n" +
                                  "needs an administrator), one line per packet, until Ctrl+C. Windows hands packets to user\n" +
                                  "space for observation only, so every packet's verdict is NF_ACCEPT.");
                return 0;
            }
            if (!int.TryParse(a, out queue) || queue < 0) throw new UsageException($"invalid queue number {a}");
        }
        var pktmon = Path.Combine(Environment.SystemDirectory, "pktmon.exe");
        if (!File.Exists(pktmon)) { Console.Error.WriteLine("nf-queue: Packet Monitor (pktmon) needs Windows 10 version 2004 or later"); return 1; }
        var psi = new ProcessStartInfo(pktmon) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var a in new[] { "start", "--capture", "--comp", "nics", "--pkt-size", "0", "-m", "real-time" }) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi);
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            using var stop = Process.Start(new ProcessStartInfo(pktmon, "stop") { UseShellExecute = false, RedirectStandardOutput = true });
            stop?.WaitForExit();
        };
        Console.WriteLine($"binding this socket to queue '{queue}'");
        var id = 0;
        string line;
        while ((line = p.StandardOutput.ReadLine()) != null)
        {
            // pktmon prints a packet per line after its banner; those carry an ethertype.
            if (!line.Contains("ethertype", StringComparison.OrdinalIgnoreCase)) continue;
            var len = System.Text.RegularExpressions.Regex.Match(line, @"length (\d+)");
            var hw = line.Contains("IPv6", StringComparison.Ordinal) ? "0x86dd" : line.Contains("ARP", StringComparison.Ordinal) ? "0x0806" : "0x0800";
            Console.WriteLine($"packet received (id={++id} hw={hw} hook=1, payload len {(len.Success ? len.Groups[1].Value : "?")}) {line.Trim()}");
        }
        p.WaitForExit();
        if (p.ExitCode != 0)
        {
            Console.Error.WriteLine($"nf-queue: pktmon failed: {p.StandardError.ReadToEnd().Trim()}");
            return 1;
        }
        return 0;
    }

    // ---- nft-*-get: Windows Firewall as nftables ----

    const string PolicyKey = @"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy";
    const string GroupPolicyKey = @"SOFTWARE\Policies\Microsoft\WindowsFirewall";

    sealed class FwRule
    {
        public string Name, Action, Dir, Protocol, App, Service;
        public List<string> LocalPorts = new(), RemotePorts = new(), LocalAddresses = new(), RemoteAddresses = new(), Profiles = new();
    }

    static int NftGet(string[] args, string what)
    {
        var json = false;
        foreach (var a in args)
        {
            switch (a)
            {
                case "-h" or "--help":
                    Console.WriteLine($"Usage: nft-{what}-get [<family>] [json]\n\nPrints Windows Firewall as nftables: table inet windows_firewall, chains input and\n" +
                                      "output (hook, policy from the active profiles' default actions) and the enabled rules.");
                    return 0;
                case "json": json = true; break;
                case "ip" or "ip6" or "inet" or "unspec" or "xml" or "default": break;
                default: throw new UsageException($"unknown argument {a} (see --help)");
            }
        }
        var rules = ReadFirewall();
        var (inPolicy, outPolicy) = DefaultPolicies();
        var o = new StringBuilder();
        if (json)
        {
            using var ms = new MemoryStream();
            using (var w = new System.Text.Json.Utf8JsonWriter(ms, new System.Text.Json.JsonWriterOptions { Indented = true }))
            {
                w.WriteStartObject();
                w.WriteStartArray("nftables");
                if (what is "ruleset" or "table") { w.WriteStartObject(); w.WriteStartObject("table"); w.WriteString("family", "inet"); w.WriteString("name", "windows_firewall"); w.WriteEndObject(); w.WriteEndObject(); }
                if (what is "ruleset" or "chain")
                    foreach (var (chain, hook, policy) in new[] { ("input", "input", inPolicy), ("output", "output", outPolicy) })
                    {
                        w.WriteStartObject(); w.WriteStartObject("chain");
                        w.WriteString("family", "inet"); w.WriteString("table", "windows_firewall"); w.WriteString("name", chain);
                        w.WriteString("type", "filter"); w.WriteString("hook", hook); w.WriteNumber("prio", 0); w.WriteString("policy", policy);
                        w.WriteEndObject(); w.WriteEndObject();
                    }
                if (what is "ruleset" or "rule")
                    foreach (var r in rules)
                    {
                        w.WriteStartObject(); w.WriteStartObject("rule");
                        w.WriteString("family", "inet"); w.WriteString("table", "windows_firewall"); w.WriteString("chain", r.Dir == "In" ? "input" : "output");
                        w.WriteString("expr", RuleText(r)); w.WriteString("comment", r.Name);
                        w.WriteEndObject(); w.WriteEndObject();
                    }
                w.WriteEndArray();
                w.WriteEndObject();
            }
            Console.WriteLine(Encoding.UTF8.GetString(ms.ToArray()));
            return 0;
        }
        switch (what)
        {
            case "table":
                o.Append("table inet windows_firewall\n");
                break;
            case "chain":
                o.Append($"table inet windows_firewall chain input type filter hook input priority 0 policy {inPolicy}\n");
                o.Append($"table inet windows_firewall chain output type filter hook output priority 0 policy {outPolicy}\n");
                break;
            case "rule":
                foreach (var r in rules) o.Append($"table inet windows_firewall chain {(r.Dir == "In" ? "input" : "output")} {RuleText(r)} comment \"{r.Name}\"\n");
                break;
            default:
                o.Append("table inet windows_firewall {\n");
                foreach (var (chain, policy) in new[] { ("input", inPolicy), ("output", outPolicy) })
                {
                    o.Append($"\tchain {chain} {{\n\t\ttype filter hook {chain} priority filter; policy {policy};\n");
                    foreach (var r in rules.Where(r => (r.Dir == "In") == (chain == "input")))
                        o.Append($"\t\t{RuleText(r)} comment \"{r.Name.Replace("\"", "'")}\"\n");
                    o.Append("\t}\n");
                }
                o.Append("}\n");
                break;
        }
        if (Console.IsOutputRedirected)
        {
            using var stdout = Console.OpenStandardOutput();
            stdout.Write(new UTF8Encoding(false).GetBytes(o.ToString()));
        }
        else Console.Write(o.ToString());
        return 0;
    }

    static string RuleText(FwRule r)
    {
        var parts = new List<string>();
        static string Set(List<string> items) => items.Count == 1 ? items[0] : "{ " + string.Join(", ", items) + " }";
        var addrs = r.Dir == "In" ? (Local: "daddr", Remote: "saddr") : (Local: "saddr", Remote: "daddr");
        var ports = r.Dir == "In" ? (Local: "dport", Remote: "sport") : (Local: "sport", Remote: "dport");
        if (r.LocalAddresses.Count > 0) parts.Add($"ip {addrs.Local} {Set(r.LocalAddresses)}");
        if (r.RemoteAddresses.Count > 0) parts.Add($"ip {addrs.Remote} {Set(r.RemoteAddresses)}");
        var l4 = r.Protocol switch { "6" => "tcp", "17" => "udp", "1" => "icmp", "58" => "icmpv6", _ => null };
        if (l4 is "tcp" or "udp")
        {
            if (r.LocalPorts.Count > 0) parts.Add($"{l4} {ports.Local} {Set(r.LocalPorts)}");
            if (r.RemotePorts.Count > 0) parts.Add($"{l4} {ports.Remote} {Set(r.RemotePorts)}");
            if (r.LocalPorts.Count == 0 && r.RemotePorts.Count == 0) parts.Add($"meta l4proto {l4}");
        }
        else if (l4 != null) parts.Add($"meta l4proto {l4}");
        else if (r.Protocol != null) parts.Add($"meta l4proto {r.Protocol}");
        // Windows Firewall matches programs and services, which nftables can't express: kept as meta comments.
        if (r.App != null) parts.Add($"meta program \"{r.App.Replace("\"", "'")}\"");
        if (r.Service != null) parts.Add($"meta service \"{r.Service}\"");
        parts.Add(r.Action == "Block" ? "drop" : "accept");
        return string.Join(" ", parts);
    }

    static List<FwRule> ReadFirewall()
    {
        var rules = new List<FwRule>();
        foreach (var path in new[] { GroupPolicyKey + @"\FirewallRules", PolicyKey + @"\FirewallRules" })
        {
            using var key = Registry.LocalMachine.OpenSubKey(path);
            if (key == null) continue;
            foreach (var name in key.GetValueNames())
            {
                if (key.GetValue(name) is not string data) continue;
                var f = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var part in data.Split('|'))
                {
                    var eq = part.IndexOf('=');
                    if (eq <= 0) continue;
                    if (!f.TryGetValue(part[..eq], out var list)) f[part[..eq]] = list = new List<string>();
                    list.Add(part[(eq + 1)..]);
                }
                string One(string k) => f.TryGetValue(k, out var l) ? l[0] : null;
                List<string> All(params string[] keys) => keys.SelectMany(k => f.GetValueOrDefault(k) ?? []).ToList();
                if (!"TRUE".Equals(One("Active"), StringComparison.OrdinalIgnoreCase) || One("Dir") is not ("In" or "Out")) continue;
                rules.Add(new FwRule
                {
                    Name = IndirectString(One("Name") ?? name),
                    Action = One("Action"),
                    Dir = One("Dir"),
                    Protocol = One("Protocol") is "256" ? null : One("Protocol"),
                    App = One("App"),
                    Service = One("Svc"),
                    LocalPorts = All("LPort", "LPort2_10", "LPort2_20"),
                    RemotePorts = All("RPort", "RPort2_10").ToList(),
                    LocalAddresses = All("LA4", "LA6").ToList(),
                    RemoteAddresses = All("RA4", "RA6").ToList(),
                });
            }
        }
        return rules.OrderBy(r => r.Dir).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    // Inbound defaults to block and outbound to allow unless a profile says otherwise; the strictest
    // active profile wins (the public profile, normally).
    static (string In, string Out) DefaultPolicies()
    {
        string inbound = "drop", outbound = "accept";
        foreach (var profile in new[] { "PublicProfile", "StandardProfile", "DomainProfile" })
        {
            using var key = Registry.LocalMachine.OpenSubKey($@"{PolicyKey}\{profile}");
            if (key == null) continue;
            if (key.GetValue("DefaultInboundAction") is int ia) inbound = ia == 1 ? "drop" : "accept";
            if (key.GetValue("DefaultOutboundAction") is int oa) outbound = oa == 1 ? "drop" : "accept";
            break;
        }
        return (inbound, outbound);
    }

    static string IndirectString(string s)
    {
        if (!s.StartsWith('@')) return s;
        var buffer = new char[1024];
        fixed (char* p = buffer)
            if (SHLoadIndirectString(s, p, 1024, IntPtr.Zero) == 0) return new string(p);
        // "@{Package.Name_version_...?ms-resource://...}" of a package this user doesn't have: its name.
        return s.StartsWith("@{") && s.IndexOf('_') is > 2 and var end ? s[2..end] : s;
    }

    // ---- rtnl-*: rtnetlink dumps on IP Helper ----

    static int RtnlLinkDump(string[] args)
    {
        if (Help(args, "rtnl-link-dump", "Dump the network links: index, type, flags, MTU, name, address, state.")) return 0;
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces().OrderBy(IfIndex)) Console.WriteLine(LinkLine(nic));
        return 0;
    }

    static string LinkLine(NetworkInterface nic)
    {
        var up = nic.OperationalStatus == OperationalStatus.Up;
        var loop = nic.NetworkInterfaceType == NetworkInterfaceType.Loopback;
        var flags = (up ? 0x1 | 0x40 : 0) | (loop ? 0x8 : 0x2 | 0x1000);   // IFF_UP|IFF_RUNNING, IFF_LOOPBACK or IFF_BROADCAST|IFF_MULTICAST
        var type = loop ? 772 : nic.NetworkInterfaceType is NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211 or NetworkInterfaceType.GigabitEthernet ? 1 : 65534;
        var mtu = 0;
        try { mtu = nic.GetIPProperties().GetIPv4Properties()?.Mtu ?? 0; } catch (NetworkInformationException) { }
        var mac = string.Join(":", nic.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("x2")));
        return $"index={IfIndex(nic)} type={type} flags={flags} family=0 mtu={mtu} name={nic.Name} address={(mac.Length > 0 ? mac : "00:00:00:00:00:00")} {(up ? "[RUNNING]" : "[NOT RUNNING]")}";
    }

    static int IfIndex(NetworkInterface nic)
    {
        try
        {
            var p = nic.GetIPProperties();
            return p.GetIPv4Properties()?.Index ?? p.GetIPv6Properties()?.Index ?? 0;
        }
        catch (NetworkInformationException) { return 0; }
    }

    static int RtnlAddrDump(string[] args)
    {
        string family = null;
        foreach (var a in args)
        {
            if (Help(args, "rtnl-addr-dump", "Usage: rtnl-addr-dump <inet|inet6>\nDump the interface addresses of a family (both without one).")) return 0;
            family = a is "inet" or "inet6" ? a : throw new UsageException("family must be inet or inet6");
        }
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces().OrderBy(IfIndex))
            foreach (var u in nic.GetIPProperties().UnicastAddresses)
            {
                var v6 = u.Address.AddressFamily == AddressFamily.InterNetworkV6;
                if (family != null && (family == "inet6") != v6) continue;
                var scope = IPAddress.IsLoopback(u.Address) ? 254 : u.Address.IsIPv6LinkLocal ? 253 : 0;
                Console.WriteLine($"index={IfIndex(nic)} family={(v6 ? 10 : 2)} addr={u.Address}/{u.PrefixLength} scope={scope} label={nic.Name}");
            }
        return 0;
    }

    static int RtnlRouteDump(string[] args)
    {
        string family = null;
        foreach (var a in args)
        {
            if (Help(args, "rtnl-route-dump", "Usage: rtnl-route-dump <inet|inet6>\nDump the routing table of a family (both without one).")) return 0;
            family = a is "inet" or "inet6" ? a : throw new UsageException("family must be inet or inet6");
        }
        void* table;
        var err = GetIpForwardTable2(family == "inet" ? (ushort)AF_INET : family == "inet6" ? (ushort)AF_INET6 : (ushort)0, &table);
        if (err != 0) throw new InvalidOperationException($"could not read the routing table: {Marshal.GetPInvokeErrorMessage((int)err)}");
        try
        {
            var count = *(uint*)table;
            var rows = (byte*)table + 8;
            for (uint i = 0; i < count; i++)
            {
                var row = rows + i * 104;
                var dst = SockAddr(row + 12);
                var prefix = row[40];
                var gw = SockAddr(row + 44);
                var metric = *(uint*)(row + 84);
                var proto = *(int*)(row + 88);
                var index = *(uint*)(row + 8);
                var v6 = dst.AddressFamily == AddressFamily.InterNetworkV6;
                var line = $"family={(v6 ? 10 : 2)} type=1 proto={proto} table=254 dst={dst}/{prefix}";
                if (!gw.Equals(IPAddress.Any) && !gw.Equals(IPAddress.IPv6Any)) line += $" gw={gw}";
                Console.WriteLine($"{line} oif={index} prio={metric}");
            }
        }
        finally { FreeMibTable(table); }
        return 0;
    }

    static int RtnlNeighDump(string[] args)
    {
        string family = null;
        foreach (var a in args)
        {
            if (Help(args, "rtnl-neigh-dump", "Usage: rtnl-neigh-dump <inet|inet6>\nDump the neighbour (ARP/NDP) cache of a family (both without one).")) return 0;
            family = a is "inet" or "inet6" ? a : throw new UsageException("family must be inet or inet6");
        }
        void* table;
        var err = GetIpNetTable2(family == "inet" ? (ushort)AF_INET : family == "inet6" ? (ushort)AF_INET6 : (ushort)0, &table);
        if (err != 0) throw new InvalidOperationException($"could not read the neighbour table: {Marshal.GetPInvokeErrorMessage((int)err)}");
        string[] states = ["UNREACHABLE", "INCOMPLETE", "PROBE", "DELAY", "STALE", "REACHABLE", "PERMANENT"];
        try
        {
            var count = *(uint*)table;
            var rows = (byte*)table + 8;
            for (uint i = 0; i < count; i++)
            {
                var row = rows + i * 88;
                var addr = SockAddr(row);
                var index = *(uint*)(row + 28);
                var len = *(uint*)(row + 72);
                var mac = string.Join(":", Enumerable.Range(0, (int)Math.Min(len, 32)).Select(b => row[40 + b].ToString("x2")));
                var state = *(int*)(row + 76);
                Console.WriteLine($"family={(addr.AddressFamily == AddressFamily.InterNetworkV6 ? 10 : 2)} ifindex={index} dst={addr} lladdr={(mac.Length > 0 ? mac : "00:00:00:00:00:00")} state={(state >= 0 && state < states.Length ? states[state] : "NONE")}");
            }
        }
        finally { FreeMibTable(table); }
        return 0;
    }

    static IPAddress SockAddr(byte* p)
    {
        var family = *(ushort*)p;
        if (family == AF_INET) return new IPAddress(new ReadOnlySpan<byte>(p + 4, 4));
        if (family == AF_INET6) return new IPAddress(new ReadOnlySpan<byte>(p + 8, 16), *(uint*)(p + 24));
        return IPAddress.Any;
    }

    static int RtnlLinkEvent(string[] args)
    {
        if (Help(args, "rtnl-link-event", "Print link changes (up/down, added/removed) as they happen, until Ctrl+C.")) return 0;
        return WatchLinks(nic => Console.WriteLine(LinkLine(nic)), removed => Console.WriteLine($"index={removed} [REMOVED]"));
    }

    // ---- nfnl-iftest ----

    static int NfnlIftest(string[] args)
    {
        var monitor = false;
        foreach (var a in args)
        {
            if (Help(args, "nfnl-iftest", "Usage: nfnl-iftest [-m]\nPrint the interface table (index, name, up/down); -m keeps printing changes until Ctrl+C.")) return 0;
            monitor = a is "-m" or "--monitor" ? true : throw new UsageException($"unknown option {a}");
        }
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces().OrderBy(IfIndex)) Console.WriteLine(IftestLine(nic));
        return monitor ? WatchLinks(nic => Console.WriteLine(IftestLine(nic)), removed => Console.WriteLine($"index ({removed}) removed")) : 0;
    }

    static string IftestLine(NetworkInterface nic) =>
        $"index ({IfIndex(nic)}) is {nic.Name} ({(nic.OperationalStatus == OperationalStatus.Up ? "up" : "down")})";

    // Interface changes come from NotifyIpInterfaceChange; each one re-reads the table and reports differences.
    static int WatchLinks(Action<NetworkInterface> changed, Action<int> removed)
    {
        using var stop = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
        var known = NetworkInterface.GetAllNetworkInterfaces().ToDictionary(IfIndex, n => n.OperationalStatus);
        using var signal = new AutoResetEvent(false);
        NetworkChange.NetworkAddressChanged += (_, _) => signal.Set();
        NetworkChange.NetworkAvailabilityChanged += (_, _) => signal.Set();
        while (!stop.IsCancellationRequested)
        {
            WaitHandle.WaitAny([signal, stop.Token.WaitHandle], 2000);
            var now = NetworkInterface.GetAllNetworkInterfaces();
            var seen = new HashSet<int>();
            foreach (var nic in now)
            {
                var index = IfIndex(nic);
                seen.Add(index);
                if (!known.TryGetValue(index, out var old) || old != nic.OperationalStatus) changed(nic);
            }
            foreach (var index in known.Keys.Where(k => !seen.Contains(k))) removed(index);
            known = now.ToDictionary(IfIndex, n => n.OperationalStatus);
        }
        return 0;
    }

    static bool Help(string[] args, string name, string text)
    {
        if (!args.Any(a => a is "-h" or "--help")) return false;
        Console.WriteLine(text.StartsWith("Usage") ? text : $"Usage: {name}\n{text}");
        return true;
    }

    // ---- Win32 ----

    const int AF_INET = 2, AF_INET6 = 23, TCP_TABLE_OWNER_PID_ALL = 5;

    [StructLayout(LayoutKind.Sequential)]
    struct MIB_TCPROW_OWNER_PID { public uint dwState, dwLocalAddr, dwLocalPort, dwRemoteAddr, dwRemotePort, dwOwningPid; }

    [StructLayout(LayoutKind.Sequential)]
    struct MIB_TCP6ROW_OWNER_PID
    {
        public fixed byte ucLocalAddr[16];
        public uint dwLocalScopeId, dwLocalPort;
        public fixed byte ucRemoteAddr[16];
        public uint dwRemoteScopeId, dwRemotePort, dwState, dwOwningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MIB_TCPROW { public uint dwState, dwLocalAddr, dwLocalPort, dwRemoteAddr, dwRemotePort; }

    [StructLayout(LayoutKind.Sequential)]
    struct MIB_TCPSTATS
    {
        public uint dwRtoAlgorithm, dwRtoMin, dwRtoMax, dwMaxConn, dwActiveOpens, dwPassiveOpens, dwAttemptFails, dwEstabResets,
                    dwCurrEstab, dwInSegs, dwOutSegs, dwRetransSegs, dwInErrs, dwOutRsts, dwNumConns;
    }

    [DllImport("iphlpapi.dll")]
    static extern uint GetExtendedTcpTable(void* table, ref uint size, [MarshalAs(UnmanagedType.Bool)] bool sort, int family, int tableClass, uint reserved);

    [DllImport("iphlpapi.dll")]
    static extern uint SetTcpEntry(ref MIB_TCPROW row);

    [DllImport("iphlpapi.dll")]
    static extern uint GetTcpStatisticsEx(ref MIB_TCPSTATS stats, uint family);

    [DllImport("iphlpapi.dll")]
    static extern uint GetIpForwardTable2(ushort family, void** table);

    [DllImport("iphlpapi.dll")]
    static extern uint GetIpNetTable2(ushort family, void** table);

    [DllImport("iphlpapi.dll")]
    static extern void FreeMibTable(void* table);

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    static extern int SHLoadIndirectString(string source, char* output, uint size, IntPtr reserved);
}
