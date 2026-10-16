using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace WinTools;

// socket_vmnet: give QEMU VMs a real network without root, through a UNIX socket that QEMU connects
// to (-netdev stream,addr.type=unix,addr.path=SOCKET, or socket_vmnet_client). On macOS the
// frames go to vmnet.framework; on Windows this daemon is the vmnet: an Ethernet switch between all
// connected VMs plus a gateway that does what vmnet's shared mode does. The gateway answers ARP,
// serves DHCP (--vmnet-gateway, --vmnet-dhcp-end, --vmnet-mask), replies to ping, relays DNS to the
// host's resolver, and NATs the VMs' TCP and UDP (and ping) out through the host's own sockets, so
// VMs reach the internet and each other, and reach the host itself at the gateway address. Host mode
// keeps the VMs on the switch and gateway without NAT. Bridged mode would need a packet driver on a
// physical adapter, which Windows only has with third-party drivers, so it is refused.
static class SocketVmnet
{
    const string Version = "1.2.1";

    [ModuleInitializer]
    internal static void Register() => Commands.Add("socket_vmnet", "vmnet-style VM network (switch, DHCP, NAT) on a UNIX socket for QEMU", Run);

    sealed class Config
    {
        public string Mode = "shared";
        public IPAddress Gateway = IPAddress.Parse("192.168.105.1"), DhcpEnd = IPAddress.Parse("192.168.105.254"), Mask = IPAddress.Parse("255.255.255.0");
        public string Socket, PidFile;
    }

    static int Run(string[] args)
    {
        var c = new Config();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            var eq = a.IndexOf('=');
            var key = eq > 0 && a.StartsWith("--") ? a[..eq] : a;
            string Value() => eq > 0 && a.StartsWith("--") ? a[(eq + 1)..] : ++i < args.Length ? args[i] : throw new UsageException($"{a} needs a value");
            switch (key)
            {
                case "-h" or "--help":
                    Console.WriteLine($"""
                        Usage: socket_vmnet [OPTION]... SOCKET
                        vmnet-style networking for QEMU VMs on Windows: a switch for every VM that connects to
                        SOCKET (QEMU stream protocol), with DHCP, DNS and NAT to the host's network.

                          --vmnet-mode=(host|shared|bridged)  default: shared
                          --vmnet-interface=INTERFACE         bridged mode only (not available on Windows)
                          --vmnet-gateway=IP                  gateway and DHCP server (default 192.168.105.1)
                          --vmnet-dhcp-end=IP                 last DHCP address (default 192.168.105.254)
                          --vmnet-mask=MASK                   subnet mask (default 255.255.255.0)
                          --vmnet-interface-id=UUID           accepted for compatibility
                          --vmnet-nat66-prefix=PREFIX         accepted (IPv6 is not routed)
                          -p, --pidfile=PIDFILE               write the process id here
                          -v, --version                       show the version

                        QEMU: -netdev stream,id=net0,server=off,addr.type=unix,addr.path=SOCKET
                              -device virtio-net-pci,netdev=net0
                        """);
                    return 0;
                case "-v" or "--version": Console.WriteLine($"socket_vmnet version {Version}"); return 0;
                case "--vmnet-mode": c.Mode = Value(); break;
                case "--vmnet-interface" or "--vmnet-interface-id" or "--vmnet-nat66-prefix": Value(); break;
                case "--vmnet-gateway": c.Gateway = Ip(Value()); break;
                case "--vmnet-dhcp-end": c.DhcpEnd = Ip(Value()); break;
                case "--vmnet-mask": c.Mask = Ip(Value()); break;
                case "-p" or "--pidfile": c.PidFile = Value(); break;
                case "--self-test": return SelfTest();
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unrecognized option '{a}'");
                    if (c.Socket != null) throw new UsageException("only one SOCKET may be given");
                    c.Socket = a;
                    break;
            }
        }
        if (c.Mode is not ("host" or "shared" or "bridged")) throw new UsageException($"invalid --vmnet-mode: {c.Mode}");
        if (c.Mode == "bridged")
        {
            Console.Error.WriteLine("socket_vmnet: bridged mode needs a packet driver on a physical adapter, which Windows has no built-in one for; use shared or host mode");
            return 1;
        }
        if (c.Socket == null) throw new UsageException("missing SOCKET (see socket_vmnet --help)");
        return Serve(c);
    }

    static IPAddress Ip(string s) => IPAddress.TryParse(s, out var ip) && ip.AddressFamily == AddressFamily.InterNetwork ? ip : throw new UsageException($"invalid IPv4 address: {s}");

    // ---- the daemon ----

    static int Serve(Config c)
    {
        var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        try { listener.Bind(new UnixDomainSocketEndPoint(c.Socket)); }
        catch (SocketException e)
        {
            Console.Error.WriteLine($"bind: {(e.SocketErrorCode == SocketError.AddressAlreadyInUse ? "Address already in use" : e.Message)}");
            return 1;
        }
        listener.Listen(16);
        if (c.PidFile != null) File.WriteAllText(c.PidFile, Environment.ProcessId + "\n");
        var net = new Network(c);
        Console.Error.WriteLine($"socket_vmnet: {c.Mode} mode, gateway {c.Gateway}/{MaskBits(c.Mask)}, listening on {c.Socket}");
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            listener.Close();
        };
        try
        {
            while (true)
            {
                var s = listener.Accept();
                var port = new Port(s);
                net.Attach(port);
                new Thread(() => net.Pump(port)) { IsBackground = true }.Start();
            }
        }
        catch (Exception e) when (e is SocketException or ObjectDisposedException) { }
        finally
        {
            try { File.Delete(c.Socket); } catch (IOException) { }
            if (c.PidFile != null) try { File.Delete(c.PidFile); } catch (IOException) { }
        }
        return 0;
    }

    static int MaskBits(IPAddress mask) => System.Numerics.BitOperations.PopCount(BinaryPrimitives.ReadUInt32BigEndian(mask.GetAddressBytes()));

    // A switch port: one VM's connection, frames framed by a 4-byte big-endian length (QEMU stream).
    sealed class Port(Socket socket)
    {
        public readonly Socket Socket = socket;
        readonly object sendLock = new();
        public Action<byte[]> Sink;   // the self-test captures frames instead of a socket

        public void Send(byte[] frame)
        {
            if (Sink != null) { Sink(frame); return; }
            var buf = new byte[4 + frame.Length];
            BinaryPrimitives.WriteUInt32BigEndian(buf, (uint)frame.Length);
            frame.CopyTo(buf, 4);
            lock (sendLock)
            {
                try { Socket.Send(buf); }
                catch (Exception e) when (e is SocketException or ObjectDisposedException) { }
            }
        }

        public byte[] Receive()
        {
            var header = ReadExactly(4);
            if (header == null) return null;
            var len = (int)BinaryPrimitives.ReadUInt32BigEndian(header);
            return len is <= 0 or > 65550 ? null : ReadExactly(len);
        }

        byte[] ReadExactly(int n)
        {
            var buf = new byte[n];
            var got = 0;
            while (got < n)
            {
                int r;
                try { r = Socket.Receive(buf, got, n - got, SocketFlags.None); }
                catch (Exception e) when (e is SocketException or ObjectDisposedException) { return null; }
                if (r == 0) return null;
                got += r;
            }
            return buf;
        }
    }

    // ---- switch and gateway ----

    sealed class Network
    {
        readonly Config c;
        readonly object gate = new();
        readonly List<Port> ports = [];
        readonly Dictionary<ulong, Port> macTable = [];
        readonly Dictionary<ulong, uint> leases = [];
        readonly Dictionary<uint, ulong> arp = [];   // guest ip -> mac
        readonly Dictionary<string, TcpFlow> tcp = [];
        readonly Dictionary<string, UdpFlow> udp = [];
        public readonly byte[] GatewayMac = [0x5a, 0x94, 0xef, 0xe4, 0x0c, 0xdd];
        readonly uint gw, mask, first, last;
        readonly bool nat;
        IPAddress[] dns;

        public Network(Config config)
        {
            c = config;
            gw = U(c.Gateway);
            mask = U(c.Mask);
            first = gw + 1;
            last = U(c.DhcpEnd);
            nat = c.Mode == "shared";
        }

        static uint U(IPAddress ip) => BinaryPrimitives.ReadUInt32BigEndian(ip.GetAddressBytes());
        static IPAddress A(uint v) { var b = new byte[4]; BinaryPrimitives.WriteUInt32BigEndian(b, v); return new IPAddress(b); }
        static ulong Mac(byte[] f, int off) => (ulong)f[off] << 40 | (ulong)f[off + 1] << 32 | (ulong)f[off + 2] << 24 | (ulong)f[off + 3] << 16 | (ulong)f[off + 4] << 8 | f[off + 5];
        ulong GwMac => Mac(GatewayMac, 0);

        public void Attach(Port p) { lock (gate) ports.Add(p); }

        public void Pump(Port p)
        {
            while (p.Receive() is { } frame) Frame(p, frame);
            lock (gate)
            {
                ports.Remove(p);
                foreach (var k in macTable.Where(kv => kv.Value == p).Select(kv => kv.Key).ToList()) macTable.Remove(k);
            }
            try { p.Socket.Dispose(); } catch (ObjectDisposedException) { }
        }

        // A frame from a VM: learn its MAC, hand it to the gateway and/or the other ports.
        public void Frame(Port from, byte[] f)
        {
            if (f.Length < 14) return;
            var dst = Mac(f, 0);
            var src = Mac(f, 6);
            List<Port> targets;
            lock (gate)
            {
                macTable[src] = from;
                var broadcast = (f[0] & 1) != 0;
                if (broadcast) targets = ports.Where(p => p != from).ToList();
                else if (macTable.TryGetValue(dst, out var to)) targets = to == from ? [] : [to];
                else targets = dst == GwMac ? [] : ports.Where(p => p != from).ToList();
            }
            foreach (var t in targets) t.Send(f);
            if (dst == GwMac || (f[0] & 1) != 0) Gateway(f);
        }

        void Deliver(byte[] f)
        {
            var dst = Mac(f, 0);
            List<Port> targets;
            lock (gate) targets = (f[0] & 1) != 0 || !macTable.TryGetValue(dst, out var p) ? ports.ToList() : [p];
            foreach (var t in targets) t.Send(f);
        }

        void Gateway(byte[] f)
        {
            var type = BinaryPrimitives.ReadUInt16BigEndian(f.AsSpan(12));
            if (type == 0x0806) Arp(f);
            else if (type == 0x0800 && f.Length >= 34) Ipv4(f);
        }

        void Arp(byte[] f)
        {
            if (f.Length < 42 || BinaryPrimitives.ReadUInt16BigEndian(f.AsSpan(20)) != 1) return;   // requests only
            var senderMac = Mac(f, 22);
            var senderIp = BinaryPrimitives.ReadUInt32BigEndian(f.AsSpan(28));
            var targetIp = BinaryPrimitives.ReadUInt32BigEndian(f.AsSpan(38));
            lock (gate) if (senderIp != 0) arp[senderIp] = senderMac;
            if (targetIp != gw) return;
            var r = new byte[42];
            Array.Copy(f, 6, r, 0, 6);
            GatewayMac.CopyTo(r, 6);
            r[12] = 0x08; r[13] = 0x06;
            Array.Copy(f, 14, r, 14, 6);          // htype, ptype, hlen, plen
            r[21] = 2;                            // reply
            GatewayMac.CopyTo(r, 22);
            BinaryPrimitives.WriteUInt32BigEndian(r.AsSpan(28), gw);
            Array.Copy(f, 22, r, 32, 10);         // target = the asker
            Deliver(r);
        }

        void Ipv4(byte[] f)
        {
            var ihl = (f[14] & 0xf) * 4;
            var total = BinaryPrimitives.ReadUInt16BigEndian(f.AsSpan(16));
            if (14 + total > f.Length || total < ihl) return;
            var proto = f[23];
            var src = BinaryPrimitives.ReadUInt32BigEndian(f.AsSpan(26));
            var dst = BinaryPrimitives.ReadUInt32BigEndian(f.AsSpan(30));
            var srcMac = Mac(f, 6);
            lock (gate) if (src != 0 && (src & mask) == (gw & mask)) arp[src] = srcMac;
            var l4 = 14 + ihl;
            var payload = f.AsSpan(l4, total - ihl).ToArray();
            if ((BinaryPrimitives.ReadUInt16BigEndian(f.AsSpan(20)) & 0x3fff) != 0) return;   // fragments aren't reassembled
            switch (proto)
            {
                case 17:
                {
                    if (payload.Length < 8) return;
                    var sport = BinaryPrimitives.ReadUInt16BigEndian(payload);
                    var dport = BinaryPrimitives.ReadUInt16BigEndian(payload.AsSpan(2));
                    var data = payload.AsSpan(8, Math.Max(0, BinaryPrimitives.ReadUInt16BigEndian(payload.AsSpan(4)) - 8)).ToArray();
                    if (dport == 67) { Dhcp(srcMac, data); return; }
                    if ((f[0] & 1) != 0 && dst != gw) return;   // other broadcasts stay on the switch
                    if (dst == gw && dport == 53 && nat) { Dns(src, sport, data); return; }
                    if (nat || dst == gw) UdpOut(src, sport, dst == gw ? 0x7f000001 : dst, dport, dst, data);
                    return;
                }
                case 1:
                    if (payload.Length >= 8 && payload[0] == 8) Ping(src, dst, payload);
                    return;
                case 6:
                    if (nat || dst == gw) Tcp(src, dst, payload);
                    return;
            }
        }

        // ---- packets to the VMs ----

        void SendIp(uint src, uint dst, byte proto, byte[] l4, ulong? dstMac = null)
        {
            ulong mac;
            if (dstMac is { } m) mac = m;
            else lock (gate) if (!arp.TryGetValue(dst, out mac)) mac = 0xffffffffffff;
            var f = new byte[34 + l4.Length];
            for (var i = 0; i < 6; i++) f[i] = (byte)(mac >> (40 - 8 * i));
            GatewayMac.CopyTo(f, 6);
            f[12] = 0x08;
            f[14] = 0x45;
            BinaryPrimitives.WriteUInt16BigEndian(f.AsSpan(16), (ushort)(20 + l4.Length));
            f[22] = 64;
            f[23] = proto;
            BinaryPrimitives.WriteUInt32BigEndian(f.AsSpan(26), src);
            BinaryPrimitives.WriteUInt32BigEndian(f.AsSpan(30), dst);
            BinaryPrimitives.WriteUInt16BigEndian(f.AsSpan(24), Checksum(f.AsSpan(14, 20), 0));
            l4.CopyTo(f, 34);
            if (proto is 6 or 17)
            {
                var csumAt = 34 + (proto == 6 ? 16 : 6);
                BinaryPrimitives.WriteUInt16BigEndian(f.AsSpan(csumAt), 0);
                var sum = Checksum(f.AsSpan(34), Pseudo(src, dst, proto, l4.Length));
                if (proto == 17 && sum == 0) sum = 0xffff;
                BinaryPrimitives.WriteUInt16BigEndian(f.AsSpan(csumAt), sum);
            }
            Deliver(f);
        }

        static uint Pseudo(uint src, uint dst, byte proto, int len) => (src >> 16) + (src & 0xffff) + (dst >> 16) + (dst & 0xffff) + proto + (uint)len;

        internal static ushort Checksum(ReadOnlySpan<byte> data, uint sum)
        {
            for (var i = 0; i + 1 < data.Length; i += 2) sum += BinaryPrimitives.ReadUInt16BigEndian(data[i..]);
            if (data.Length % 2 == 1) sum += (uint)data[^1] << 8;
            while (sum >> 16 != 0) sum = (sum & 0xffff) + (sum >> 16);
            return (ushort)~sum;
        }

        void SendUdp(uint src, ushort sport, uint dst, ushort dport, byte[] data, ulong? dstMac = null)
        {
            var u = new byte[8 + data.Length];
            BinaryPrimitives.WriteUInt16BigEndian(u, sport);
            BinaryPrimitives.WriteUInt16BigEndian(u.AsSpan(2), dport);
            BinaryPrimitives.WriteUInt16BigEndian(u.AsSpan(4), (ushort)u.Length);
            data.CopyTo(u, 8);
            SendIp(src, dst, 17, u, dstMac);
        }

        // ---- DHCP ----

        void Dhcp(ulong mac, byte[] d)
        {
            if (d.Length < 240 || d[0] != 1 || BinaryPrimitives.ReadUInt32BigEndian(d.AsSpan(236)) != 0x63825363) return;
            byte type = 0;
            uint requested = 0;
            for (var i = 240; i < d.Length && d[i] != 255;)
            {
                if (d[i] == 0) { i++; continue; }
                if (i + 1 >= d.Length) break;
                var len = d[i + 1];
                if (i + 2 + len > d.Length) break;
                if (d[i] == 53 && len >= 1) type = d[i + 2];
                if (d[i] == 50 && len == 4) requested = BinaryPrimitives.ReadUInt32BigEndian(d.AsSpan(i + 2));
                i += 2 + len;
            }
            uint ip;
            lock (gate)
            {
                if (!leases.TryGetValue(mac, out ip))
                {
                    var used = leases.Values.ToHashSet();
                    ip = requested >= first && requested <= last && !used.Contains(requested) ? requested
                        : Enumerable.Range(0, (int)(last - first + 1)).Select(k => first + (uint)k).FirstOrDefault(a => !used.Contains(a));
                    if (ip == 0) return;   // pool exhausted
                    leases[mac] = ip;
                }
            }
            byte reply;
            if (type == 1) reply = 2;                                                  // DISCOVER -> OFFER
            else if (type == 3) reply = requested == 0 || requested == ip ? (byte)5 : (byte)6;   // REQUEST -> ACK / NAK
            else return;
            var r = new byte[300];
            r[0] = 2; r[1] = 1; r[2] = 6;
            Array.Copy(d, 4, r, 4, 4);                                                 // xid
            Array.Copy(d, 10, r, 10, 2);                                               // flags
            if (reply != 6) BinaryPrimitives.WriteUInt32BigEndian(r.AsSpan(16), ip);   // yiaddr
            BinaryPrimitives.WriteUInt32BigEndian(r.AsSpan(20), gw);                   // siaddr
            Array.Copy(d, 28, r, 28, 16);                                              // chaddr
            BinaryPrimitives.WriteUInt32BigEndian(r.AsSpan(236), 0x63825363);
            var o = 240;
            void Opt(byte code, params byte[] v) { r[o++] = code; r[o++] = (byte)v.Length; v.CopyTo(r, o); o += v.Length; }
            byte[] B(uint v) { var b = new byte[4]; BinaryPrimitives.WriteUInt32BigEndian(b, v); return b; }
            Opt(53, reply);
            Opt(54, B(gw));
            if (reply != 6)
            {
                Opt(51, B(86400));
                Opt(1, B(mask));
                if (nat) { Opt(3, B(gw)); Opt(6, B(gw)); }
            }
            r[o++] = 255;
            SendUdp(gw, 67, reply == 6 ? 0xffffffff : ip, 68, r[..Math.Max(o, 300)], mac);
        }

        // ---- ICMP echo ----

        void Ping(uint src, uint dst, byte[] icmp)
        {
            void Reply()
            {
                var r = (byte[])icmp.Clone();
                r[0] = 0;
                BinaryPrimitives.WriteUInt16BigEndian(r.AsSpan(2), 0);
                BinaryPrimitives.WriteUInt16BigEndian(r.AsSpan(2), Checksum(r, 0));
                SendIp(dst, src, 1, r);
            }
            if (dst == gw) { Reply(); return; }
            if (!nat || (dst & mask) == (gw & mask)) return;
            // Unprivileged echo through the system's ICMP API, answered when the host gets a reply.
            var data = icmp[8..];
            _ = Task.Run(async () =>
            {
                using var p = new Ping();
                try
                {
                    var res = await p.SendPingAsync(A(dst), 4000, data.Length > 0 ? data : [0]);
                    if (res.Status == IPStatus.Success) Reply();
                }
                catch (PingException) { }
            });
        }

        // ---- DNS: relayed to the host's resolver ----

        void Dns(uint src, ushort sport, byte[] query)
        {
            dns ??= NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .SelectMany(n => n.GetIPProperties().DnsAddresses).Where(a => a.AddressFamily == AddressFamily.InterNetwork).Distinct().ToArray();
            if (dns.Length == 0) return;
            UdpOut(src, sport, U(dns[0]), 53, gw, query, relayed: true);
        }

        // ---- UDP NAT ----

        sealed class UdpFlow
        {
            public Socket Socket;
            public DateTime Last;
        }

        void UdpOut(uint src, ushort sport, uint dst, ushort dport, uint shownDst, byte[] data, bool relayed = false)
        {
            var key = $"{src}:{sport}";
            UdpFlow flow;
            lock (gate)
            {
                if (!udp.TryGetValue(key, out flow))
                {
                    flow = new UdpFlow { Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp) };
                    flow.Socket.Bind(new IPEndPoint(IPAddress.Any, 0));
                    udp[key] = flow;
                    var f = flow;
                    new Thread(() => UdpIn(key, f, src, sport)) { IsBackground = true }.Start();
                }
                flow.Last = DateTime.UtcNow;
                if (relayed) dnsServers[key] = (dst, shownDst);
            }
            try { flow.Socket.SendTo(data, new IPEndPoint(A(dst), dport)); }
            catch (SocketException) { }
        }

        readonly Dictionary<string, (uint Real, uint Shown)> dnsServers = [];

        void UdpIn(string key, UdpFlow flow, uint guest, ushort guestPort)
        {
            var buf = new byte[65536];
            flow.Socket.ReceiveTimeout = 60000;
            while (true)
            {
                EndPoint from = new IPEndPoint(IPAddress.Any, 0);
                int n;
                try { n = flow.Socket.ReceiveFrom(buf, ref from); }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.TimedOut)
                {
                    lock (gate) if (DateTime.UtcNow - flow.Last < TimeSpan.FromSeconds(60)) continue;
                    break;
                }
                catch (Exception e) when (e is SocketException or ObjectDisposedException) { break; }
                var ep = (IPEndPoint)from;
                var srcIp = U(ep.Address);
                lock (gate)
                {
                    flow.Last = DateTime.UtcNow;
                    if (dnsServers.TryGetValue(key, out var d) && d.Real == srcIp) srcIp = d.Shown;   // relayed DNS answers come from the gateway
                    else if (srcIp == 0x7f000001) srcIp = gw;
                }
                SendUdp(srcIp, (ushort)ep.Port, guest, guestPort, buf[..n]);
            }
            lock (gate) { udp.Remove(key); dnsServers.Remove(key); }
            flow.Socket.Dispose();
        }

        // ---- TCP NAT ----

        sealed class TcpFlow
        {
            public Socket Socket;
            public uint Guest, Remote;      // remote is the address the VM sees
            public ushort GuestPort, RemotePort;
            public uint SndUna, SndNxt, RcvNxt;
            public int GuestWindow = 65535, Mss = 1460;
            public readonly List<byte> Unacked = [];
            public bool GuestFin, HostFin, Established, Closed;
            public DateTime LastSend;
            public readonly object Lock = new();
        }

        const byte FIN = 1, SYN = 2, RST = 4, PSH = 8, ACK = 16;

        void Tcp(uint src, uint dst, byte[] seg)
        {
            if (seg.Length < 20) return;
            var sport = BinaryPrimitives.ReadUInt16BigEndian(seg);
            var dport = BinaryPrimitives.ReadUInt16BigEndian(seg.AsSpan(2));
            var seq = BinaryPrimitives.ReadUInt32BigEndian(seg.AsSpan(4));
            var ack = BinaryPrimitives.ReadUInt32BigEndian(seg.AsSpan(8));
            var off = (seg[12] >> 4) * 4;
            var flags = seg[13];
            var window = BinaryPrimitives.ReadUInt16BigEndian(seg.AsSpan(14));
            if (off < 20 || off > seg.Length) return;
            var data = seg[off..];
            var key = $"{src}:{sport}>{dst}:{dport}";
            TcpFlow flow;
            lock (gate) tcp.TryGetValue(key, out flow);
            if (flow == null)
            {
                if ((flags & (SYN | ACK)) != SYN) { if ((flags & RST) == 0) Segment(dst, dport, src, sport, ack, seq + (uint)data.Length + ((flags & (SYN | FIN)) != 0 ? 1u : 0), RST | ACK, []); return; }
                flow = new TcpFlow { Guest = src, GuestPort = sport, Remote = dst, RemotePort = dport, RcvNxt = seq + 1, GuestWindow = window };
                flow.SndUna = flow.SndNxt = (uint)Random.Shared.Next();
                // MSS option from the VM's SYN.
                for (var i = 20; i + 1 < off;)
                {
                    if (seg[i] == 0) break;
                    if (seg[i] == 1) { i++; continue; }
                    if (seg[i] == 2 && seg[i + 1] == 4) flow.Mss = Math.Min(1460, (int)BinaryPrimitives.ReadUInt16BigEndian(seg.AsSpan(i + 2)));
                    i += Math.Max(2, (int)seg[i + 1]);
                }
                lock (gate) tcp[key] = flow;
                var f = flow;
                _ = Task.Run(() => Connect(key, f, dst == gw ? 0x7f000001 : dst, dport));
                return;
            }
            lock (flow.Lock)
            {
                if ((flags & RST) != 0) { CloseFlow(key, flow); return; }
                flow.GuestWindow = window;
                if ((flags & ACK) != 0 && Diff(ack, flow.SndUna) > 0 && Diff(ack, flow.SndNxt) <= 0)
                {
                    var acked = (int)Diff(ack, flow.SndUna);
                    if (!flow.Established && acked >= 1) { flow.Established = true; acked--; }   // our SYN
                    var dataAcked = Math.Min(acked, flow.Unacked.Count);
                    flow.Unacked.RemoveRange(0, dataAcked);
                    flow.SndUna = ack;
                }
                if (!flow.Established) return;
                if (data.Length > 0)
                {
                    if (seq == flow.RcvNxt)
                    {
                        try { flow.Socket.Send(data); }
                        catch (Exception e) when (e is SocketException or ObjectDisposedException) { Reset(key, flow); return; }
                        flow.RcvNxt += (uint)data.Length;
                    }
                    Segment(flow.Remote, flow.RemotePort, flow.Guest, flow.GuestPort, flow.SndNxt, flow.RcvNxt, ACK, []);
                }
                if ((flags & FIN) != 0 && seq + (uint)data.Length == flow.RcvNxt && !flow.GuestFin)
                {
                    flow.GuestFin = true;
                    flow.RcvNxt++;
                    try { flow.Socket.Shutdown(SocketShutdown.Send); } catch (Exception e) when (e is SocketException or ObjectDisposedException) { }
                    Segment(flow.Remote, flow.RemotePort, flow.Guest, flow.GuestPort, flow.SndNxt, flow.RcvNxt, ACK, []);
                }
                if (flow.GuestFin && flow.HostFin && flow.SndUna == flow.SndNxt) CloseFlow(key, flow);
                Monitor.PulseAll(flow.Lock);   // the window may have opened
            }
        }

        static int Diff(uint a, uint b) => (int)(a - b);

        void Connect(string key, TcpFlow flow, uint to, ushort port)
        {
            var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try { s.Connect(new IPEndPoint(A(to), port)); }
            catch (SocketException)
            {
                Segment(flow.Remote, flow.RemotePort, flow.Guest, flow.GuestPort, 0, flow.RcvNxt, RST | ACK, []);
                lock (gate) tcp.Remove(key);
                s.Dispose();
                return;
            }
            lock (flow.Lock)
            {
                flow.Socket = s;
                SynAck(flow);
                flow.SndNxt = flow.SndUna + 1;
            }
            new Thread(() => HostToGuest(key, flow)) { IsBackground = true }.Start();
            new Thread(() => Retransmit(key, flow)) { IsBackground = true }.Start();
        }

        void SynAck(TcpFlow flow)
        {
            var mss = new byte[] { 2, 4, (byte)(1460 >> 8), 1460 & 0xff };
            Segment(flow.Remote, flow.RemotePort, flow.Guest, flow.GuestPort, flow.SndUna, flow.RcvNxt, SYN | ACK, [], mss);
            flow.LastSend = DateTime.UtcNow;
        }

        void HostToGuest(string key, TcpFlow flow)
        {
            var buf = new byte[65536];
            while (true)
            {
                int room;
                lock (flow.Lock)
                {
                    // Wait while the VM's window is full.
                    while (!flow.Closed && (room = flow.GuestWindow - flow.Unacked.Count) <= 0) Monitor.Wait(flow.Lock, 1000);
                    if (flow.Closed) return;
                    room = Math.Max(1, flow.GuestWindow - flow.Unacked.Count);
                }
                int n;
                try { n = flow.Socket.Receive(buf, 0, Math.Min(buf.Length, room), SocketFlags.None); }
                catch (Exception e) when (e is SocketException or ObjectDisposedException) { Reset(key, flow); return; }
                lock (flow.Lock)
                {
                    if (flow.Closed) return;
                    if (n == 0)
                    {
                        flow.HostFin = true;
                        Segment(flow.Remote, flow.RemotePort, flow.Guest, flow.GuestPort, flow.SndNxt, flow.RcvNxt, FIN | ACK, []);
                        flow.SndNxt++;
                        flow.LastSend = DateTime.UtcNow;
                        return;
                    }
                    for (var o = 0; o < n; o += flow.Mss)
                    {
                        var chunk = buf[o..Math.Min(n, o + flow.Mss)];
                        Segment(flow.Remote, flow.RemotePort, flow.Guest, flow.GuestPort, flow.SndNxt, flow.RcvNxt, PSH | ACK, chunk);
                        flow.SndNxt += (uint)chunk.Length;
                        flow.Unacked.AddRange(chunk);
                    }
                    flow.LastSend = DateTime.UtcNow;
                }
            }
        }

        // Resends from the oldest unacknowledged byte when the VM hasn't acknowledged for a while.
        void Retransmit(string key, TcpFlow flow)
        {
            while (true)
            {
                Thread.Sleep(500);
                lock (flow.Lock)
                {
                    if (flow.Closed) return;
                    if (flow.SndUna == flow.SndNxt || DateTime.UtcNow - flow.LastSend < TimeSpan.FromSeconds(1)) continue;
                    if (!flow.Established) { SynAck(flow); continue; }
                    var seq = flow.SndUna;
                    for (var o = 0; o < flow.Unacked.Count && o < flow.GuestWindow; o += flow.Mss)
                    {
                        var chunk = flow.Unacked.GetRange(o, Math.Min(flow.Mss, flow.Unacked.Count - o)).ToArray();
                        Segment(flow.Remote, flow.RemotePort, flow.Guest, flow.GuestPort, seq + (uint)o, flow.RcvNxt, PSH | ACK, chunk);
                    }
                    if (flow.HostFin && flow.Unacked.Count == (int)(flow.SndNxt - flow.SndUna - 1))
                        Segment(flow.Remote, flow.RemotePort, flow.Guest, flow.GuestPort, flow.SndNxt - 1, flow.RcvNxt, FIN | ACK, []);
                    flow.LastSend = DateTime.UtcNow;
                }
            }
        }

        void Reset(string key, TcpFlow flow)
        {
            lock (flow.Lock)
            {
                if (flow.Closed) return;
                Segment(flow.Remote, flow.RemotePort, flow.Guest, flow.GuestPort, flow.SndNxt, flow.RcvNxt, RST | ACK, []);
                CloseFlow(key, flow);
            }
        }

        void CloseFlow(string key, TcpFlow flow)
        {
            flow.Closed = true;
            Monitor.PulseAll(flow.Lock);
            try { flow.Socket?.Dispose(); } catch (ObjectDisposedException) { }
            lock (gate) tcp.Remove(key);
        }

        void Segment(uint src, ushort sport, uint dst, ushort dport, uint seq, uint ack, byte flags, byte[] data, byte[] options = null)
        {
            var opt = options ?? [];
            var s = new byte[20 + opt.Length + data.Length];
            BinaryPrimitives.WriteUInt16BigEndian(s, sport);
            BinaryPrimitives.WriteUInt16BigEndian(s.AsSpan(2), dport);
            BinaryPrimitives.WriteUInt32BigEndian(s.AsSpan(4), seq);
            BinaryPrimitives.WriteUInt32BigEndian(s.AsSpan(8), ack);
            s[12] = (byte)((20 + opt.Length) / 4 << 4);
            s[13] = flags;
            BinaryPrimitives.WriteUInt16BigEndian(s.AsSpan(14), 65535);
            opt.CopyTo(s, 20);
            data.CopyTo(s, 20 + opt.Length);
            SendIp(src, dst, 6, s);
        }
    }

    // ---- self-test: the switch and gateway on synthetic frames, no sockets ----

    static int SelfTest()
    {
        var net = new Network(new Config());
        var got = new List<byte[]>();
        var vm = new Port(null) { Sink = got.Add };
        net.Attach(vm);
        byte[] vmMac = [0x52, 0x54, 0x00, 0x12, 0x34, 0x56];
        var failures = 0;
        void Check(bool ok, string what) { Console.WriteLine($"{(ok ? "ok" : "FAIL")}: {what}"); if (!ok) failures++; }

        // ARP who-has 192.168.105.1
        var arp = new byte[42];
        for (var i = 0; i < 6; i++) arp[i] = 0xff;
        vmMac.CopyTo(arp, 6);
        arp[12] = 8; arp[13] = 6; arp[15] = 1; arp[16] = 8; arp[18] = 6; arp[19] = 4; arp[21] = 1;
        vmMac.CopyTo(arp, 22);
        arp[38] = 192; arp[39] = 168; arp[40] = 105; arp[41] = 1;
        net.Frame(vm, arp);
        Check(got.Count == 1 && got[0][21] == 2 && got[0].AsSpan(22, 6).SequenceEqual(net.GatewayMac), "ARP reply with the gateway MAC");

        // DHCP DISCOVER
        got.Clear();
        var bootp = new byte[244];
        bootp[0] = 1; bootp[1] = 1; bootp[2] = 6; bootp[4] = 0xde; bootp[5] = 0xad;
        vmMac.CopyTo(bootp, 28);
        BinaryPrimitives.WriteUInt32BigEndian(bootp.AsSpan(236), 0x63825363);
        bootp[240] = 53; bootp[241] = 1; bootp[242] = 1; bootp[243] = 255;
        net.Frame(vm, Udp(vmMac, [0xff, 0xff, 0xff, 0xff, 0xff, 0xff], 0, 0xffffffff, 68, 67, bootp));
        var offer = got.FirstOrDefault();
        Check(offer != null && offer[42 + 242] == 2 && offer[42 + 16] == 192 && offer[42 + 19] == 2, "DHCP OFFER of 192.168.105.2");

        // ICMP echo to the gateway
        got.Clear();
        var icmp = new byte[12];
        icmp[0] = 8; icmp[6] = 0; icmp[7] = 1; icmp[8] = 0x61;
        BinaryPrimitives.WriteUInt16BigEndian(icmp.AsSpan(2), Network.Checksum(icmp, 0));
        net.Frame(vm, Ip(vmMac, net.GatewayMac, 0xc0a86902, 0xc0a86901, 1, icmp));
        var reply = got.FirstOrDefault();
        Check(reply != null && reply[34] == 0 && Network.Checksum(reply.AsSpan(34), 0) == 0, "ICMP echo reply from the gateway");
        Check(reply != null && Network.Checksum(reply.AsSpan(14, 20), 0) == 0, "IPv4 header checksum");

        // A second VM gets switched frames.
        var got2 = new List<byte[]>();
        var vm2 = new Port(null) { Sink = got2.Add };
        net.Attach(vm2);
        var unicast = new byte[60];
        vmMac.CopyTo(unicast, 0);
        byte[] mac2 = [0x52, 0x54, 0x00, 0xaa, 0xbb, 0xcc];
        mac2.CopyTo(unicast, 6);
        unicast[12] = 0x88; unicast[13] = 0xb5;
        net.Frame(vm2, unicast);
        Check(got.Count > 0 && got[^1].AsSpan(6, 6).SequenceEqual(mac2), "frames between VMs are switched");
        return failures == 0 ? 0 : 1;
    }

    static byte[] Ip(byte[] src, byte[] dst, uint sip, uint dip, byte proto, byte[] l4)
    {
        var f = new byte[34 + l4.Length];
        dst.CopyTo(f, 0);
        src.CopyTo(f, 6);
        f[12] = 8; f[14] = 0x45;
        BinaryPrimitives.WriteUInt16BigEndian(f.AsSpan(16), (ushort)(20 + l4.Length));
        f[22] = 64; f[23] = proto;
        BinaryPrimitives.WriteUInt32BigEndian(f.AsSpan(26), sip);
        BinaryPrimitives.WriteUInt32BigEndian(f.AsSpan(30), dip);
        l4.CopyTo(f, 34);
        return f;
    }

    static byte[] Udp(byte[] src, byte[] dst, uint sip, uint dip, ushort sport, ushort dport, byte[] data)
    {
        var u = new byte[8 + data.Length];
        BinaryPrimitives.WriteUInt16BigEndian(u, sport);
        BinaryPrimitives.WriteUInt16BigEndian(u.AsSpan(2), dport);
        BinaryPrimitives.WriteUInt16BigEndian(u.AsSpan(4), (ushort)u.Length);
        data.CopyTo(u, 8);
        return Ip(src, dst, sip, dip, 17, u);
    }
}
