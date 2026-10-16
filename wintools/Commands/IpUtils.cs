using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static WinTools.IpHelper;

namespace WinTools;

// ping / tracepath / arping (iputils): Linux's iputils output over Windows' ICMP API (the Ping class,
// which needs no admin rights) and the IP Helper neighbour API. Round-trip times come from a
// high-resolution stopwatch around each request, since Windows itself only reports whole milliseconds.
static class IpUtils
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("ping", "Send ICMP ECHO_REQUEST to network hosts (iputils)", RunPing);
        Commands.Add("tracepath", "Trace the path to a network host (iputils)", RunTracepath);
        Commands.Add("arping", "Send ARP requests to a neighbour host (iputils)", RunArping);
    }

    static string Version(string tool) => $"{tool} from iputils (wintools {Commands.Version})";

    // getopt-style parsing: "-c 3", "-c3" and "-nq" all work; letters in withValue take an argument.
    static List<(char opt, string value)> GetOpt(string[] args, string withValue, string flags, List<string> operands)
    {
        var result = new List<(char, string)>();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg == "--") { operands.AddRange(args[(i + 1)..]); break; }
            if (arg == "--help") { result.Add(('h', null)); continue; }
            if (arg == "--version") { result.Add(('V', null)); continue; }
            if (!arg.StartsWith('-') || arg.Length == 1) { operands.Add(arg); continue; }
            for (var j = 1; j < arg.Length; j++)
            {
                var c = arg[j];
                if (withValue.Contains(c))
                {
                    var value = j + 1 < arg.Length ? arg[(j + 1)..] : ++i < args.Length ? args[i] : throw new UsageException($"option requires an argument -- '{c}'");
                    result.Add((c, value));
                    break;
                }
                if (!flags.Contains(c)) throw new UsageException($"invalid option -- '{c}'");
                result.Add((c, null));
            }
        }
        return result;
    }

    static double Number(char opt, string value, double min = 0)
    {
        if (!double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var n) || n < min)
            throw new UsageException($"invalid argument for -{opt}: '{value}'");
        return n;
    }

    static IPAddress Resolve(string tool, string host, AddressFamily family)
    {
        try { return IpRoute.ResolveOne(host, family); }
        catch (InvalidOperationException)
        {
            throw new PingFailure($"{tool}: {host}: {(family == AddressFamily.Unspecified ? "Name or service not known" : "Address family for hostname not supported")}");
        }
    }

    // A fatal error that, like iputils, exits with status 2.
    sealed class PingFailure(string message) : Exception(message);

    // iputils prints 3 significant decimals below 1 ms, then fewer as the time grows.
    static string Ms(double ms) => ms >= 100 ? $"{ms:0}" : ms >= 10 ? $"{ms:0.0}" : ms >= 1 ? $"{ms:0.00}" : $"{ms:0.000}";

    // ---- ping ----

    const string PingUsage =
        "\nUsage\n  ping [options] <destination>\n\nOptions:\n" +
        "  <destination>      DNS name or IP address\n" +
        "  -c <count>         stop after <count> replies\n" +
        "  -D                 print timestamps\n" +
        "  -i <interval>      seconds between sending each packet\n" +
        "  -M <pmtud opt>     define path MTU discovery, can be one of <do|dont|want>\n" +
        "  -n                 no dns name resolution\n" +
        "  -O                 report outstanding replies\n" +
        "  -q                 quiet output\n" +
        "  -s <size>          use <size> as number of data bytes to be sent\n" +
        "  -t <ttl>           define time to live\n" +
        "  -V                 print version and exit\n" +
        "  -w <deadline>      reply wait <deadline> in seconds\n" +
        "  -W <timeout>       time to wait for response\n\n" +
        "IPv4 options:\n  -4                 use IPv4\n\nIPv6 options:\n  -6                 use IPv6";

    static int RunPing(string[] args)
    {
        try { return Ping(args); }
        catch (PingFailure e) { Console.Error.WriteLine(e.Message); return 2; }
    }

    static int Ping(string[] args)
    {
        var operands = new List<string>();
        int count = 0, size = 56, ttl = 0;
        double interval = 1, deadline = 0, timeout = 0;
        bool quiet = false, timestamps = false, outstanding = false, dontFragment = false;
        var family = AddressFamily.Unspecified;
        foreach (var (opt, value) in GetOpt(args, "ciWwstIMlpQSF", "hV46nqDOaAbBdfLRrUv", operands))
        {
            switch (opt)
            {
                case 'h': Console.WriteLine(PingUsage); return 0;
                case 'V': Console.WriteLine(Version("ping")); return 0;
                case 'c': count = (int)Number(opt, value, 1); break;
                case 'i': interval = Number(opt, value); break;
                case 'W': timeout = Number(opt, value); break;
                case 'w': deadline = Number(opt, value); break;
                case 's': size = (int)Number(opt, value); if (size > 65500) throw new UsageException($"invalid argument: '{value}': out of range: 0 <= value <= 65500"); break;
                case 't': ttl = (int)Number(opt, value, 1); if (ttl > 255) throw new UsageException($"invalid argument: '{value}': out of range: 1 <= value <= 255"); break;
                case 'M': dontFragment = value is "do" or "probe"; break;
                case '4': family = AddressFamily.InterNetwork; break;
                case '6': family = AddressFamily.InterNetworkV6; break;
                case 'q': quiet = true; break;
                case 'D': timestamps = true; break;
                case 'O': outstanding = true; break;
                case 'I':
                    Console.Error.WriteLine($"ping: Warning: -I {value} ignored; Windows picks the interface and source address from the routing table");
                    break;
                default: break;   // -n -a -A -b -B -d -f -L -R -r -U -v -l -p -Q -S -F: accepted, no effect here
            }
        }
        if (operands.Count == 0) throw new UsageException("usage error: Destination address required");
        var host = operands[^1];
        var target = Resolve("ping", host, family);
        var v6 = target.AddressFamily == AddressFamily.InterNetworkV6;
        Console.WriteLine(v6 ? $"PING {host} ({target}) {size} data bytes" : $"PING {host} ({target}) {size}({size + 28}) bytes of data.");

        var payload = new byte[size];
        for (var i = 0; i < size; i++) payload[i] = (byte)(i + 8);
        var options = ttl > 0 || dontFragment ? new PingOptions(ttl > 0 ? ttl : 128, dontFragment) : null;
        var timeoutMs = (int)((timeout > 0 ? timeout : 10) * 1000);

        var gate = new object();
        int transmitted = 0, received = 0, errors = 0;
        var rtts = new List<double>();
        var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler onCancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += onCancel;
        var clock = Stopwatch.StartNew();
        double lastSend = 0;
        var pending = new List<Task>();

        string Stamp() => timestamps ? $"[{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0:0.000000}] " : "";

        // Each request blocks a pool thread, so the stopwatch times only the ICMP round trip.
        void SendOne(int seq)
        {
            using var ping = new System.Net.NetworkInformation.Ping();
            PingReply reply;
            var watch = Stopwatch.StartNew();
            try { reply = ping.Send(target, timeoutMs, payload, options); }
            catch (PingException e)
            {
                lock (gate) { errors++; Console.Error.WriteLine($"ping: sendmsg: {e.InnerException?.Message ?? e.Message}"); }
                return;
            }
            var ms = watch.Elapsed.TotalMilliseconds;
            lock (gate)
            {
                switch (reply.Status)
                {
                    case IPStatus.Success:
                        received++;
                        rtts.Add(ms);
                        if (!quiet)
                            Console.WriteLine($"{Stamp()}{reply.Buffer.Length + 8} bytes from {reply.Address}: icmp_seq={seq}" +
                                              (reply.Options != null ? $" ttl={reply.Options.Ttl}" : "") + $" time={Ms(ms)} ms");
                        break;
                    case IPStatus.TimedOut:
                        if (outstanding && !quiet) Console.WriteLine($"{Stamp()}no answer yet for icmp_seq={seq}");
                        break;
                    default:
                        errors++;
                        if (!quiet) Console.WriteLine($"{Stamp()}From {reply.Address} icmp_seq={seq} {StatusText(reply.Status)}");
                        break;
                }
            }
        }

        try
        {
            for (var seq = 1; !stop.IsCancellationRequested; seq++)
            {
                if (deadline > 0 && clock.Elapsed.TotalSeconds >= deadline) break;
                lock (gate) { transmitted++; }
                lastSend = clock.Elapsed.TotalMilliseconds;
                var current = seq;
                pending.Add(Task.Factory.StartNew(() => SendOne(current), TaskCreationOptions.LongRunning));
                if (count > 0 && seq >= count && deadline == 0) break;
                try { Task.Delay(TimeSpan.FromSeconds(interval), stop.Token).Wait(); }
                catch (AggregateException) { break; }
                lock (gate) { if (count > 0 && (deadline > 0 ? received >= count : seq >= count)) break; }
            }
            // Linger for the last replies, up to the reply timeout (or what is left of the deadline).
            if (!stop.IsCancellationRequested)
            {
                var linger = deadline > 0 ? Math.Max(0, deadline * 1000 - clock.Elapsed.TotalMilliseconds) : timeoutMs;
                try { Task.WaitAll(pending.ToArray(), (int)linger, stop.Token); }
                catch (OperationCanceledException) { }
            }
        }
        finally { Console.CancelKeyPress -= onCancel; }

        lock (gate)
        {
            var loss = transmitted == 0 ? 0 : (transmitted - received) * 100.0 / transmitted;
            Console.WriteLine();
            Console.WriteLine($"--- {host} ping statistics ---");
            Console.WriteLine($"{transmitted} packets transmitted, {received} received, " + (errors > 0 ? $"+{errors} errors, " : "") +
                              $"{loss.ToString("G6", System.Globalization.CultureInfo.InvariantCulture)}% packet loss, time {(long)lastSend}ms");
            if (rtts.Count > 0)
            {
                var avg = rtts.Average();
                var mdev = Math.Sqrt(Math.Max(0, rtts.Average(x => x * x) - avg * avg));
                Console.WriteLine($"rtt min/avg/max/mdev = {rtts.Min():0.000}/{avg:0.000}/{rtts.Max():0.000}/{mdev:0.000} ms");
            }
            return received > 0 ? 0 : 1;
        }
    }

    static string StatusText(IPStatus status) => status switch
    {
        IPStatus.TtlExpired => "Time to live exceeded",
        IPStatus.DestinationHostUnreachable => "Destination Host Unreachable",
        IPStatus.DestinationNetworkUnreachable => "Destination Net Unreachable",
        IPStatus.DestinationPortUnreachable => "Destination Port Unreachable",
        IPStatus.DestinationProtocolUnreachable => "Destination Protocol Unreachable",
        IPStatus.PacketTooBig => "Frag needed and DF set",
        IPStatus.ParameterProblem => "Parameter problem",
        IPStatus.SourceQuench => "Source Quench",
        IPStatus.TtlReassemblyTimeExceeded => "Frag reassembly time exceeded",
        _ => status.ToString(),
    };

    // ---- tracepath ----

    static int RunTracepath(string[] args)
    {
        try { return Tracepath(args); }
        catch (PingFailure e) { Console.Error.WriteLine(e.Message); return 2; }
    }

    static int Tracepath(string[] args)
    {
        var operands = new List<string>();
        int maxHops = 30, length = 0;
        bool numeric = false, both = false;
        var family = AddressFamily.Unspecified;
        foreach (var (opt, value) in GetOpt(args, "lmp", "h46nbV", operands))
        {
            switch (opt)
            {
                case 'h':
                    Console.WriteLine("\nUsage\n  tracepath [options] <destination>\n\nOptions:\n  -4             use IPv4\n  -6             use IPv6\n" +
                                      "  -b             print both name and ip\n  -l <length>    use packet <length>\n  -m <hops>      use maximum <hops>\n" +
                                      "  -n             no reverse DNS name resolution\n  -p <port>      ignored: probes are ICMP echo requests on Windows\n" +
                                      "  -V             print version and exit");
                    return 0;
                case 'V': Console.WriteLine(Version("tracepath")); return 0;
                case '4': family = AddressFamily.InterNetwork; break;
                case '6': family = AddressFamily.InterNetworkV6; break;
                case 'n': numeric = true; break;
                case 'b': both = true; break;
                case 'l': length = (int)Number(opt, value, 0); break;
                case 'm': maxHops = (int)Number(opt, value, 1); break;
                case 'p': break;
            }
        }
        if (operands.Count != 1) throw new UsageException("usage error: exactly one destination required");
        var host = operands[0];
        var target = Resolve("tracepath", host, family);
        var v6 = target.AddressFamily == AddressFamily.InterNetworkV6;

        var (route, _) = BestRoute(target);
        var mtu = route == null ? 1500 : Interfaces().FirstOrDefault(i => i.Index == route.IfIndex)?.Mtu ?? 1500;
        var payload = new byte[Math.Max(0, (length > 0 ? length : v6 ? 128 : 44) - (v6 ? 48 : 28))];
        Console.WriteLine($"{" 1?: [LOCALHOST]",-38}pmtu {mtu}");

        for (var ttl = 1; ttl <= maxHops; ttl++)
        {
            PingReply reply = null;
            double ms = 0;
            for (var attempt = 0; attempt < 2 && reply == null; attempt++)
            {
                using var ping = new System.Net.NetworkInformation.Ping();
                var watch = Stopwatch.StartNew();
                PingReply r;
                try { r = ping.Send(target, 2000, payload, new PingOptions(ttl, true)); }
                catch (PingException e) { throw new PingFailure($"tracepath: {e.InnerException?.Message ?? e.Message}"); }
                ms = watch.Elapsed.TotalMilliseconds;
                if (r.Status != IPStatus.TimedOut) reply = r;
            }
            if (reply == null) { Console.WriteLine($"{ttl,2}:  no reply"); continue; }
            var name = HopName(reply.Address, numeric, both);
            var reached = reply.Status == IPStatus.Success;
            var note = reached ? " reached" : reply.Status is IPStatus.TtlExpired ? "" : $" {StatusText(reply.Status).ToLowerInvariant()}";
            Console.WriteLine($"{ttl,2}:  {name,-48}{Ms(ms),8}ms{note}");
            if (reached || note.Length > 0)
            {
                var back = reply.Options != null ? InitialTtl(reply.Options.Ttl) - reply.Options.Ttl + 1 : ttl;
                Console.WriteLine($"     Resume: pmtu {mtu} hops {ttl} back {back} ");
                return reached ? 0 : 1;
            }
        }
        Console.WriteLine($"     Too many hops: pmtu {mtu}");
        Console.WriteLine($"     Resume: pmtu {mtu} ");
        return 1;
    }

    // Hosts start their TTL at 64, 128 or 255; the smallest one at or above what arrived is the guess.
    static int InitialTtl(int ttl) => ttl <= 64 ? 64 : ttl <= 128 ? 128 : 255;

    static string HopName(IPAddress address, bool numeric, bool both)
    {
        if (numeric) return address.ToString();
        string name = null;
        try { name = Dns.GetHostEntry(address).HostName; }
        catch (SocketException) { }
        if (string.IsNullOrEmpty(name) || name == address.ToString()) return address.ToString();
        return both ? $"{name} ({address})" : name;
    }

    // ---- arping ----

    static int RunArping(string[] args)
    {
        try { return Arping(args); }
        catch (PingFailure e) { Console.Error.WriteLine(e.Message); return 2; }
    }

    static unsafe int Arping(string[] args)
    {
        var operands = new List<string>();
        int count = 0;
        double deadline = 0, interval = 1, timeout = 1;
        bool quiet = false, firstOnly = false;
        string dev = null, source = null;
        foreach (var (opt, value) in GetOpt(args, "cwiIsWT", "hbfqVDUA0", operands))
        {
            switch (opt)
            {
                case 'h':
                    Console.WriteLine("\nUsage:\n  arping [options] <destination>\n\nOptions:\n" +
                                      "  -f            quit on first reply\n  -q            be quiet\n  -b            keep on broadcasting, do not unicast\n" +
                                      "  -V            print version and exit\n  -c <count>    how many packets to send\n" +
                                      "  -w <timeout>  how long to wait for a reply\n  -i <interval> set interval between packets (default: 1 second)\n" +
                                      "  -I <device>   which ethernet device to use (Windows name or index)\n  -s <source>   source IP address\n" +
                                      "  <destination> ask for what ip address\n" +
                                      "-D, -U and -A (duplicate detection and unsolicited ARP) aren't possible from Windows user space.");
                    return 0;
                case 'V': Console.WriteLine(Version("arping")); return 0;
                case 'c': count = (int)Number(opt, value, 1); break;
                case 'w': deadline = Number(opt, value); break;
                case 'W': timeout = Number(opt, value); break;
                case 'i': interval = Number(opt, value); break;
                case 'I': dev = value; break;
                case 's': source = value; break;
                case 'q': quiet = true; break;
                case 'f': firstOnly = true; break;
                case 'D' or 'U' or 'A':
                    Console.Error.WriteLine($"arping: -{opt} needs raw ARP frames, which Windows doesn't let user programs send");
                    return 1;
                default: break;
            }
        }
        if (operands.Count != 1) throw new UsageException("usage error: exactly one destination required");
        var target = Resolve("arping", operands[0], AddressFamily.InterNetwork);
        var ifaces = Interfaces();
        Iface iface;
        IPAddress from;
        if (dev != null)
        {
            iface = Find(ifaces, dev) ?? throw new PingFailure($"arping: Device {dev} not available.");
            from = iface.Addrs.FirstOrDefault(a => !a.V6 && InPrefix(target, a.Address, a.Prefix))?.Address
                   ?? iface.Addrs.FirstOrDefault(a => !a.V6)?.Address;
        }
        else
        {
            var (route, src) = BestRoute(target);
            iface = route == null ? null : ifaces.FirstOrDefault(i => i.Index == route.IfIndex);
            from = src;
            if (iface == null) throw new PingFailure($"arping: no route to {target}");
        }
        if (source != null && !IPAddress.TryParse(source, out from)) throw new UsageException($"invalid source {source}");
        if (from == null) throw new PingFailure($"arping: {iface.Name} has no IPv4 address");

        Console.WriteLine($"ARPING {target} from {from} {iface.Name}");
        int sent = 0, received = 0;
        var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler onCancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += onCancel;
        var clock = Stopwatch.StartNew();
        try
        {
            while (!stop.IsCancellationRequested)
            {
                if (count > 0 && sent >= count) break;
                if (deadline > 0 && clock.Elapsed.TotalSeconds >= deadline) break;
                sent++;
                var watch = Stopwatch.StartNew();
                var mac = Probe(target, from, iface.Index);
                var ms = watch.Elapsed.TotalMilliseconds;
                if (mac != null)
                {
                    received++;
                    if (!quiet) Console.WriteLine($"Unicast reply from {target} [{string.Join(":", mac.Select(b => b.ToString("X2")))}]  {ms:0.000}ms");
                    if (firstOnly) break;
                }
                var wait = TimeSpan.FromSeconds(interval) - watch.Elapsed;
                if (wait > TimeSpan.Zero && !(count > 0 && sent >= count))
                    try { Task.Delay(wait, stop.Token).Wait(); } catch (AggregateException) { break; }
            }
        }
        finally { Console.CancelKeyPress -= onCancel; }
        Console.WriteLine($"Sent {sent} probes ({sent} broadcast(s))");
        Console.WriteLine($"Received {received} response(s)");
        return received > 0 ? 0 : 1;
    }

    // One ARP probe. ResolveIpNetEntry2 drops any cached entry and asks the wire; if that isn't allowed
    // (it needs an elevated prompt on some systems) SendARP is used, which may answer from the cache.
    static unsafe byte[] Probe(IPAddress target, IPAddress from, int ifIndex)
    {
        var row = stackalloc byte[88];
        new Span<byte>(row, 88).Clear();
        *(ushort*)row = AF_INET;
        target.GetAddressBytes().CopyTo(new Span<byte>(row + 4, 4));
        *(int*)(row + 28) = ifIndex;
        var src = stackalloc byte[28];
        new Span<byte>(src, 28).Clear();
        *(ushort*)src = AF_INET;
        from.GetAddressBytes().CopyTo(new Span<byte>(src + 4, 4));
        var rc = ResolveIpNetEntry2(row, src);
        if (rc == 0)
        {
            var length = Math.Min(*(int*)(row + 72), 32);
            var mac = new ReadOnlySpan<byte>(row + 40, length).ToArray();
            return mac.Length > 0 && mac.Any(b => b != 0) ? mac : null;
        }
        if (rc != 5 && rc != 50) return null;   // ERROR_ACCESS_DENIED / ERROR_NOT_SUPPORTED: fall back
        var buffer = new byte[6];
        var size = buffer.Length;
        var dest = BitConverter.ToUInt32(target.GetAddressBytes());
        var srcAddr = BitConverter.ToUInt32(from.GetAddressBytes());
        return SendARP(dest, srcAddr, buffer, ref size) == 0 && size == 6 ? buffer : null;
    }

    [DllImport("iphlpapi.dll")]
    static extern unsafe int ResolveIpNetEntry2(byte* row, byte* sourceAddress);
}
