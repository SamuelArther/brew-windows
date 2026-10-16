using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WinTools;

// killswitch: a VPN kill switch built from Windows Firewall block rules. Block rules beat every allow rule,
// so `killswitch -e` adds outbound blocks for all traffic except: traffic from the VPN tunnel's own
// addresses, traffic to the VPN server (the peer), loopback, and with -local the local subnets. If the
// VPN drops, nothing leaks out of the physical adapters. `killswitch -d` removes the rules again.
static class Killswitch
{
    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("killswitch", "VPN kill switch using Windows Firewall block rules", Run);

    const string RulePrefix = "killswitch: ";

    sealed class Iface
    {
        public NetworkInterface Nic;
        public List<IPAddress> V4 = [], V6 = [];
        public List<(IPAddress addr, int prefix)> Subnets = [];
        public bool Vpn;
        public int Index;
    }

    static int Run(string[] args)
    {
        bool enable = false, disable = false, print = false, local = false, leak = false;
        var peers = new List<IPAddress>();
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help" or "-help":
                    Console.WriteLine("""
                        Usage: killswitch [-e | -d | -p] [-ip <peer>[,<peer>...]] [-local] [-leak]

                          (no option)  show interfaces, the VPN interface and its peer, and whether the switch is on
                          -e           enable: block all outbound traffic that doesn't go through the VPN
                          -d           disable: remove the kill switch's firewall rules
                          -p           print the netsh commands -e would run
                          -ip          the VPN server's address(es), if it can't be found from the routing table
                          -local       still allow traffic to the local network (LAN, DHCP, printers)
                          -leak        still allow ICMP and DNS (UDP 53) outside the VPN
                          -v           print the version
                        Changing firewall rules needs an elevated (Administrator) prompt.
                        """);
                    return 0;
                case "-v" or "--version" or "-version": Console.WriteLine(Commands.Version); return 0;
                case "-e": enable = true; break;
                case "-d": disable = true; break;
                case "-p": print = true; break;
                case "-local": local = true; break;
                case "-leak": leak = true; break;
                case "-ip":
                    if (++i >= args.Length) throw new UsageException("-ip requires an address");
                    foreach (var s in args[i].Split(','))
                        peers.Add(IPAddress.TryParse(s, out var ip) ? ip : throw new UsageException($"invalid IP address '{s}'"));
                    break;
                default: throw new UsageException($"unknown option {args[i]}");
            }
        }
        if ((enable ? 1 : 0) + (disable ? 1 : 0) + (print ? 1 : 0) > 1) throw new UsageException("use only one of -e, -d and -p");

        if (disable)
        {
            RequireAdmin();
            var names = OurRules();
            foreach (var name in names) Netsh("advfirewall", "firewall", "delete", "rule", "name=" + name);
            Console.WriteLine(names.Count > 0 ? "Kill switch disabled" : "Kill switch was not enabled");
            return 0;
        }

        var ifaces = Interfaces();
        var vpn = ifaces.Where(i => i.Vpn).ToList();
        if (peers.Count == 0) peers = FindPeers(ifaces);

        if (!enable && !print)
        {
            Console.WriteLine($"{"Interface",-24} {"MAC address",-19} IP");
            foreach (var i in ifaces.Where(i => i.V4.Count + i.V6.Count > 0))
            {
                var mac = i.Nic.GetPhysicalAddress().GetAddressBytes();
                var macText = mac.Length == 6 ? string.Join(':', mac.Select(b => b.ToString("x2"))) : "";
                Console.WriteLine($"{i.Nic.Name,-24} {macText,-19} {string.Join(", ", i.V4.Concat(i.V6))}".TrimEnd());
            }
            Console.WriteLine();
            if (vpn.Count == 0)
            {
                Console.WriteLine("No VPN interface found, verify VPN is connected");
                return 0;
            }
            Console.WriteLine($"VPN interface:   {string.Join(", ", vpn.Select(v => v.Nic.Name))}");
            Console.WriteLine($"PEER IP address: {(peers.Count > 0 ? string.Join(", ", peers) : "not found (pass it with -ip)")}");
            Console.WriteLine($"Kill switch:     {(OurRules().Count > 0 ? "enabled" : "disabled")}");
            if (OurRules().Count == 0) Console.WriteLine("\nTo enable the kill switch run (as Administrator): killswitch -e");
            return 0;
        }

        if (vpn.Count == 0) throw new InvalidOperationException("No VPN interface found, verify VPN is connected");
        if (peers.Count == 0) throw new InvalidOperationException("can't find the VPN peer address in the routing table; pass it with -ip");
        var commands = RuleCommands(ifaces, peers, local, leak);
        if (print)
        {
            foreach (var c in commands) Console.WriteLine("netsh " + string.Join(' ', c.Select(a => a.Contains(' ') ? $"\"{a}\"" : a)));
            return 0;
        }
        RequireAdmin();
        foreach (var name in OurRules()) Netsh("advfirewall", "firewall", "delete", "rule", "name=" + name);
        foreach (var c in commands) Netsh(c);
        Console.WriteLine($"Kill switch enabled: only {string.Join(", ", vpn.Select(v => v.Nic.Name))} and {string.Join(", ", peers)} can be reached");
        return 0;
    }

    // Two (or with -leak, four) outbound block rules, one set per address family. A packet is blocked when
    // it is neither from a tunnel address nor to an allowed remote address.
    static List<string[]> RuleCommands(List<Iface> ifaces, List<IPAddress> peers, bool local, bool leak)
    {
        var tunnel4 = ifaces.Where(i => i.Vpn).SelectMany(i => i.V4).Select(a => Host(a)).ToList();
        var tunnel6 = ifaces.Where(i => i.Vpn).SelectMany(i => i.V6).Select(a => Host(a)).ToList();
        var allowed4 = new List<(UInt128, UInt128)> { Cidr(IPAddress.Parse("127.0.0.0"), 8) };
        var allowed6 = new List<(UInt128, UInt128)> { Host(IPAddress.IPv6Loopback) };
        tunnel4.Add(allowed4[0]);
        tunnel6.Add(allowed6[0]);
        foreach (var p in peers) (p.AddressFamily == AddressFamily.InterNetwork ? allowed4 : allowed6).Add(Host(p));
        if (local)
        {
            foreach (var (addr, prefix) in ifaces.Where(i => !i.Vpn).SelectMany(i => i.Subnets)) allowed4.Add(Cidr(addr, prefix));
            allowed4.Add(Host(IPAddress.Broadcast));
            allowed4.Add(Cidr(IPAddress.Parse("224.0.0.0"), 4));
            allowed6.Add(Cidr(IPAddress.Parse("fe80::"), 10));
            allowed6.Add(Cidr(IPAddress.Parse("ff00::"), 8));
            tunnel6.Add(Cidr(IPAddress.Parse("fe80::"), 10));
        }
        var commands = new List<string[]>();
        foreach (var (family, from, to, bits) in new[] { ("IPv4", tunnel4, allowed4, 32), ("IPv6", tunnel6, allowed6, 128) })
        {
            var localip = Complement(from, bits);
            var remoteip = Complement(to, bits);
            if (localip.Length == 0 || remoteip.Length == 0) continue;
            var protocols = leak ? new[] { ("tcp", (string)null), ("udp", "1-52,54-65535") } : [("any", null)];
            foreach (var (proto, ports) in protocols)
            {
                var c = new List<string>
                {
                    "advfirewall", "firewall", "add", "rule", $"name={RulePrefix}block {family}{(leak ? " " + proto : "")}",
                    "dir=out", "action=block", "enable=yes", "profile=any", "protocol=" + proto, "localip=" + localip, "remoteip=" + remoteip,
                };
                if (ports != null) c.Add("remoteport=" + ports);
                commands.Add([.. c]);
            }
        }
        return commands;
    }

    // ---- address ranges -------------------------------------------------------------------------------

    static UInt128 ToNumber(IPAddress ip)
    {
        UInt128 n = 0;
        foreach (var b in ip.GetAddressBytes()) n = (n << 8) | b;
        return n;
    }

    static IPAddress FromNumber(UInt128 n, int bits)
    {
        var bytes = new byte[bits / 8];
        for (var i = bytes.Length - 1; i >= 0; i--) { bytes[i] = (byte)(n & 0xFF); n >>= 8; }
        return new IPAddress(bytes);
    }

    static (UInt128, UInt128) Host(IPAddress ip) => (ToNumber(ip), ToNumber(ip));

    static (UInt128, UInt128) Cidr(IPAddress ip, int prefix)
    {
        var bits = ip.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
        var hostBits = bits - prefix;
        var size = hostBits >= 128 ? UInt128.MaxValue : (UInt128.One << hostBits) - 1;
        var lo = ToNumber(ip) & ~size;
        if (bits == 32) lo &= uint.MaxValue;
        return (lo, lo + size);
    }

    // Everything in the address family except the given ranges, as a netsh address list ("a-b,c,...").
    static string Complement(List<(UInt128 lo, UInt128 hi)> ranges, int bits)
    {
        var max = bits == 32 ? (UInt128)uint.MaxValue : UInt128.MaxValue;
        var parts = new List<string>();
        UInt128 next = 0;
        var done = false;
        foreach (var (lo, hi) in ranges.OrderBy(r => r.lo))
        {
            if (lo > next) parts.Add(Format(next, lo - 1, bits));
            if (hi >= next)
            {
                if (hi == max) { done = true; break; }
                next = hi + 1;
            }
        }
        if (!done) parts.Add(Format(next, max, bits));
        return string.Join(',', parts);
    }

    static string Format(UInt128 lo, UInt128 hi, int bits) =>
        lo == hi ? FromNumber(lo, bits).ToString() : $"{FromNumber(lo, bits)}-{FromNumber(hi, bits)}";

    // ---- interfaces and routes ------------------------------------------------------------------------

    static readonly string[] VpnHints = ["WireGuard", "Wintun", "TAP-Windows", "TAP-Win32", "OpenVPN", "VPN", "NordLynx", "Tailscale", "ZeroTier"];
    static readonly string[] NotVpn = ["Teredo", "ISATAP", "6to4", "IP-HTTPS", "Hyper-V", "vEthernet", "Loopback"];

    static List<Iface> Interfaces()
    {
        var list = new List<Iface>();
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            // NDIS filter layers ("Wi-Fi-QoS Packet Scheduler-0000") show up as interfaces too; skip them.
            if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback
                || System.Text.RegularExpressions.Regex.IsMatch(nic.Name, @"-\d{4}$")) continue;
            var props = nic.GetIPProperties();
            var iface = new Iface { Nic = nic };
            try { iface.Index = props.GetIPv4Properties()?.Index ?? 0; } catch (NetworkInformationException) { }
            foreach (var u in props.UnicastAddresses)
            {
                if (u.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    iface.V4.Add(u.Address);
                    iface.Subnets.Add((u.Address, u.PrefixLength));
                }
                else if (u.Address.AddressFamily == AddressFamily.InterNetworkV6 && !u.Address.IsIPv6LinkLocal) iface.V6.Add(u.Address);
            }
            var text = nic.Name + " " + nic.Description;
            iface.Vpn = !NotVpn.Any(h => text.Contains(h, StringComparison.OrdinalIgnoreCase))
                && (nic.NetworkInterfaceType == NetworkInterfaceType.Ppp
                    || VpnHints.Any(h => text.Contains(h, StringComparison.OrdinalIgnoreCase)))
                && iface.V4.Count + iface.V6.Count > 0;
            list.Add(iface);
        }
        return list;
    }

    // VPN clients pin a host route (/32) to the VPN server through the physical gateway.
    static List<IPAddress> FindPeers(List<Iface> ifaces)
    {
        var vpnIndexes = ifaces.Where(i => i.Vpn).Select(i => i.Index).ToHashSet();
        var peers = new List<IPAddress>();
        uint size = 0;
        GetIpForwardTable(IntPtr.Zero, ref size, false);
        if (size == 0) return peers;
        var buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            if (GetIpForwardTable(buffer, ref size, false) != 0) return peers;
            var count = Marshal.ReadInt32(buffer);
            for (var i = 0; i < count; i++)
            {
                var row = buffer + 4 + i * 56;   // MIB_IPFORWARDROW: 14 DWORDs
                var dest = (uint)Marshal.ReadInt32(row);
                var mask = (uint)Marshal.ReadInt32(row, 4);
                var ifIndex = Marshal.ReadInt32(row, 16);
                var type = Marshal.ReadInt32(row, 20);
                if (mask != uint.MaxValue || type != 4 || vpnIndexes.Contains(ifIndex)) continue;   // 4 = indirect (via a gateway)
                var ip = new IPAddress(dest);
                var first = ip.GetAddressBytes()[0];
                if (first == 127 || first >= 224) continue;
                peers.Add(ip);
            }
        }
        finally { Marshal.FreeHGlobal(buffer); }
        return peers;
    }

    // ---- firewall -------------------------------------------------------------------------------------

    static List<string> OurRules()
    {
        var names = new List<string>();
        using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\FirewallRules");
        if (key == null) return names;
        foreach (var v in key.GetValueNames())
        {
            if (key.GetValue(v) is not string data) continue;
            var start = data.IndexOf("|Name=" + RulePrefix, StringComparison.Ordinal);
            if (start < 0) continue;
            start += 6;
            var end = data.IndexOf('|', start);
            var name = end < 0 ? data[start..] : data[start..end];
            if (!names.Contains(name)) names.Add(name);
        }
        return names;
    }

    static void RequireAdmin()
    {
        if (!IsUserAnAdmin()) throw new UnauthorizedAccessException("changing firewall rules needs an elevated (Administrator) prompt");
    }

    static void Netsh(params string[] args)
    {
        var psi = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "netsh.exe"))
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi);
        var output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit();
        if (p.ExitCode != 0) throw new InvalidOperationException("netsh: " + output.Trim());
    }

    [DllImport("shell32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsUserAnAdmin();

    [DllImport("iphlpapi.dll")]
    static extern int GetIpForwardTable(IntPtr table, ref uint size, [MarshalAs(UnmanagedType.Bool)] bool order);
}
