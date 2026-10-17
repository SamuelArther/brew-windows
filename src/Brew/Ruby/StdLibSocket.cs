using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Brew.Ruby;

// socket: TCPServer / TCPSocket / UNIXServer / UNIXSocket / Socket.tcp / Socket.gethostname / Addrinfo,
// over System.Net.Sockets, with Ruby's IO reading methods (gets, read, readpartial, read_nonblock ...).
// Every wait (accept, connect, reads) goes through Interp.WaitFor, which polls in short slices so
// Timeout.timeout can interrupt it and releases the interpreter lock so other Ruby threads (fork
// children) run meanwhile.
public sealed class RSocket
{
    public Socket S;
    public readonly List<byte> Pending = new();
    public bool Eof, Closed, ReadClosed, WriteClosed;
    public string UnixPath;
}

public sealed partial class Interp
{
    public RClass BasicSocketClass, SocketClass, TcpSocketClass, TcpServerClass, UnixSocketClass, UnixServerClass, AddrinfoClass;

    RClass ErrnoClass(string name)
    {
        if (ErrnoModule.Consts.GetValueOrDefault(name) is RClass c) return c;
        return DefineClass(name, SystemCallError, ErrnoModule);
    }

    RClass EofErrorClass() => ObjectClass.Consts.GetValueOrDefault("EOFError") as RClass ?? DefineClass("EOFError", IOError);

    RubyError SocketErr(SocketException e, string what)
    {
        var (name, text) = e.SocketErrorCode switch
        {
            SocketError.ConnectionRefused => ("ECONNREFUSED", "Connection refused"),
            SocketError.AddressAlreadyInUse => ("EADDRINUSE", "Address already in use"),
            SocketError.ConnectionReset => ("ECONNRESET", "Connection reset by peer"),
            SocketError.ConnectionAborted => ("ECONNABORTED", "Software caused connection abort"),
            SocketError.Shutdown => ("EPIPE", "Broken pipe"),
            SocketError.TimedOut => ("ETIMEDOUT", "Connection timed out"),
            SocketError.HostUnreachable => ("EHOSTUNREACH", "No route to host"),
            SocketError.NetworkUnreachable => ("ENETUNREACH", "Network is unreachable"),
            SocketError.AddressNotAvailable => ("EADDRNOTAVAIL", "Can't assign requested address"),
            SocketError.NotConnected => ("ENOTCONN", "Socket is not connected"),
            SocketError.AccessDenied => ("EACCES", "Permission denied"),
            SocketError.InvalidArgument => ("EINVAL", "Invalid argument"),
            SocketError.WouldBlock => ("EAGAIN", "Resource temporarily unavailable"),
            SocketError.HostNotFound or SocketError.NoData or SocketError.TryAgain => (null, null),
            _ => ("ECONNREFUSED", e.Message),
        };
        if (name == null) return Error(SocketErrorClass(), $"getaddrinfo: {e.Message}");
        return Error(ErrnoClass(name), text + (what != null ? " - " + what : ""));
    }

    RClass SocketErrorClass() => ObjectClass.Consts.GetValueOrDefault("SocketError") as RClass ?? DefineClass("SocketError", StandardError);

    RSocket SockOf(object o) => o is RObject { Host: RSocket s } ? s : throw TypeErr("not a socket");

    RSocket OpenSock(object o)
    {
        var s = SockOf(o);
        if (s.Closed) throw Error(IOError, "closed stream");
        return s;
    }

    // Wait until the socket is readable (data, EOF, or a pending connection), checking Timeout's deadline.
    void WaitReadable(RSocket s)
    {
        WaitFor(ms => s.S.Poll(ms * 1000, SelectMode.SelectRead));
    }

    // One blocking read into Pending; false at end of stream.
    bool Fill(RSocket s)
    {
        if (s.Eof || s.ReadClosed) return false;
        WaitReadable(s);
        var buf = new byte[65536];
        int n;
        try { n = s.S.Receive(buf); }
        catch (SocketException e) when (e.SocketErrorCode is SocketError.ConnectionReset or SocketError.ConnectionAborted or SocketError.Shutdown)
        {
            n = 0;
        }
        catch (SocketException e) { throw SocketErr(e, "recvfrom(2)"); }
        if (n == 0) { s.Eof = true; return false; }
        s.Pending.AddRange(new ArraySegment<byte>(buf, 0, n));
        return true;
    }

    static RString BytesToStr(byte[] bytes)
    {
        try { return new RString(new UTF8Encoding(false, true).GetString(bytes)); }
        catch (DecoderFallbackException) { return new RString(Encoding.Latin1.GetString(bytes)) { Binary = true }; }
    }

    static RString Take(RSocket s, int n)
    {
        n = Math.Min(n, s.Pending.Count);
        var bytes = s.Pending.GetRange(0, n).ToArray();
        s.Pending.RemoveRange(0, n);
        return BytesToStr(bytes);
    }

    // IO#gets: up to and including the separator (nil: the rest), nil at end of stream.
    object SockGets(RSocket s, object[] a)
    {
        var pos = Positional(a);
        string sep = pos.Length > 0 ? (pos[0] is RString rs ? rs.V : pos[0] == null ? null : (pos[0] is long ? "\n" : ToS(pos[0]))) : "\n";
        long limit = pos.Length > 1 && pos[1] is long l1 ? l1 : pos.Length > 0 && pos[0] is long l0 ? l0 : -1;
        bool chomp = Truthy(Kw(a, "chomp"));
        if (sep == "") sep = "\n\n";   // paragraph mode, roughly
        var sepBytes = sep == null ? null : RString.Utf8(sep);
        int scanned = 0;
        while (true)
        {
            if (sepBytes != null)
            {
                int idx = IndexOf(s.Pending, sepBytes, Math.Max(0, scanned - sepBytes.Length + 1));
                if (idx >= 0 && (limit < 0 || idx + sepBytes.Length <= limit))
                {
                    var line = Take(s, idx + sepBytes.Length);
                    if (chomp) line.V = line.V[..^sep.Length];
                    return line;
                }
            }
            if (limit >= 0 && s.Pending.Count >= limit) return Take(s, (int)limit);
            scanned = s.Pending.Count;
            if (!Fill(s))
            {
                if (s.Pending.Count == 0) return null;
                return Take(s, s.Pending.Count);
            }
        }
    }

    static int IndexOf(List<byte> hay, byte[] needle, int from)
    {
        for (int i = from; i <= hay.Count - needle.Length; i++)
        {
            int k = 0;
            while (k < needle.Length && hay[i + k] == needle[k]) k++;
            if (k == needle.Length) return i;
        }
        return -1;
    }

    void SockWrite(RSocket s, byte[] data)
    {
        if (s.WriteClosed) throw Error(IOError, "not opened for writing");
        int off = 0;
        while (off < data.Length)
        {
            try { off += s.S.Send(data, off, data.Length - off, SocketFlags.None); }
            catch (SocketException e) { throw SocketErr(e, null); }
        }
    }

    RObject NewSock(RClass cls, Socket sock, string unixPath = null) =>
        new() { Class = cls, Host = new RSocket { S = sock, UnixPath = unixPath } };

    // Ruby's host argument: nil/"" any, "localhost", a numeric address, or a name to resolve.
    IPAddress[] Resolve(string host, bool passive)
    {
        // A server with no host listens on loopback, not every interface as in Ruby: binding all
        // interfaces makes Windows Firewall prompt, and formula tests talk to it over localhost.
        if (string.IsNullOrEmpty(host)) return new[] { IPAddress.Loopback, IPAddress.IPv6Loopback };
        if (host is "localhost" or "localhost.localdomain") return new[] { IPAddress.Loopback, IPAddress.IPv6Loopback };
        if (host == "<any>") return new[] { IPAddress.Any };
        if (host == "<broadcast>") return new[] { IPAddress.Broadcast };
        if (IPAddress.TryParse(host.Trim('[', ']'), out var ip)) return new[] { ip };
        try { return Dns.GetHostAddresses(host).OrderBy(x => x.AddressFamily == AddressFamily.InterNetwork ? 0 : 1).ToArray(); }
        catch (SocketException e) { throw Error(SocketErrorClass(), $"getaddrinfo: {(e.SocketErrorCode == SocketError.HostNotFound ? "No such host is known. " : e.Message)}"); }
    }

    int PortArg(object o)
    {
        if (o == null) return 0;
        if (o is long l) return l is >= 0 and <= 65535 ? (int)l : throw Error(SocketErrorClass(), "getaddrinfo: Service not known");
        var s = ToS(o);
        if (int.TryParse(s, out var n)) return n;
        return s switch
        {
            "http" => 80, "https" => 443, "ftp" => 21, "ssh" => 22, "smtp" => 25, "telnet" => 23, "domain" => 53, "imap" => 143, "pop3" => 110,
            _ => throw Error(SocketErrorClass(), $"getaddrinfo: Servname not supported for ai_socktype"),
        };
    }

    Socket Connect(string host, int port, double? timeout)
    {
        Exception last = null;
        foreach (var ip in Resolve(host, false))
        {
            var sock = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                var task = sock.ConnectAsync(new IPEndPoint(ip, port));
                var until = timeout is double t ? DateTime.UtcNow.AddSeconds(t) : DateTime.MaxValue;
                WaitFor(ms => task.IsCompleted || DateTime.UtcNow >= until || ((IAsyncResult)task).AsyncWaitHandle.WaitOne(ms));
                if (!task.IsCompleted) throw new SocketException((int)SocketError.TimedOut);
                task.GetAwaiter().GetResult();   // a failed connect throws its SocketException
                return sock;
            }
            catch (AggregateException ae) when (ae.InnerException is SocketException se) { sock.Dispose(); last = se; }
            catch (SocketException se) { sock.Dispose(); last = se; }
            catch { sock.Dispose(); throw; }
        }
        if (last is SocketException e)
        {
            if (e.SocketErrorCode == SocketError.TimedOut && timeout != null)
                throw Error(ErrnoClass("ETIMEDOUT"), $"Connection timed out - user specified timeout");
            throw SocketErr(e, $"connect(2) for {InspectStr(host ?? "localhost")} port {port}");
        }
        throw Error(SocketErrorClass(), "getaddrinfo: No address associated with hostname");
    }

    Socket Listen(string host, int port)
    {
        var ip = Resolve(host, true)[0];
        Socket sock = null;
        try
        {
            sock = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sock.Bind(new IPEndPoint(ip, port));
            sock.Listen(128);
            return sock;
        }
        catch (SocketException e)
        {
            sock?.Dispose();
            throw SocketErr(e, $"bind(2) for {InspectStr(string.IsNullOrEmpty(host) ? ip.ToString() : host)} port {port}");
        }
    }

    RArray AddrArray(EndPoint ep)
    {
        if (ep is IPEndPoint ip)
        {
            var addr = ip.Address.IsIPv4MappedToIPv6 ? ip.Address.MapToIPv4() : ip.Address;
            var family = addr.AddressFamily == AddressFamily.InterNetworkV6 ? "AF_INET6" : "AF_INET";
            var text = addr.ToString();
            return new RArray(new object[] { S(family), (long)ip.Port, S(text), S(text) });
        }
        if (ep is UnixDomainSocketEndPoint u) return new RArray(new object[] { S("AF_UNIX"), S(u.ToString()) });
        return new RArray(new object[] { S("AF_UNSPEC"), S(ep?.ToString() ?? "") });
    }

    RObject NewAddrinfo(EndPoint ep, string socktype = "TCP") => new() { Class = AddrinfoClass, Host = (ep, socktype) };

    void InitSocket()
    {
        foreach (var n in new[] { "ECONNREFUSED", "EADDRINUSE", "ECONNRESET", "ECONNABORTED", "EPIPE", "ETIMEDOUT", "EHOSTUNREACH", "ENETUNREACH", "EADDRNOTAVAIL", "ENOTCONN", "EAGAIN", "EINVAL" })
            ErrnoClass(n);
        EofErrorClass();
        SocketErrorClass();
        var waitReadable = IOClass.Consts.GetValueOrDefault("WaitReadable") as RClass ?? DefineModule("WaitReadable", IOClass);
        var waitWritable = IOClass.Consts.GetValueOrDefault("WaitWritable") as RClass ?? DefineModule("WaitWritable", IOClass);
        var eagainR = IOClass.Consts.GetValueOrDefault("EAGAINWaitReadable") as RClass ?? DefineClass("EAGAINWaitReadable", ErrnoClass("EAGAIN"), IOClass);
        if (!eagainR.Includes.Contains(waitReadable)) eagainR.Includes.Add(waitReadable);
        _ = waitWritable;

        var B = BasicSocketClass = DefineClass("BasicSocket", IOClass);
        var ipSock = DefineClass("IPSocket", B);
        var T = TcpSocketClass = DefineClass("TCPSocket", ipSock);
        var TS = TcpServerClass = DefineClass("TCPServer", T);
        var U = UnixSocketClass = DefineClass("UNIXSocket", B);
        var US = UnixServerClass = DefineClass("UNIXServer", U);
        var So = SocketClass = DefineClass("Socket", B);
        var A = AddrinfoClass = DefineClass("Addrinfo");
        InvalidateMethods();

        var consts = new Dictionary<string, long>
        {
            ["AF_UNSPEC"] = 0, ["AF_INET"] = 2, ["AF_INET6"] = 23, ["AF_UNIX"] = 1, ["PF_UNSPEC"] = 0, ["PF_INET"] = 2, ["PF_INET6"] = 23, ["PF_UNIX"] = 1,
            ["SOCK_STREAM"] = 1, ["SOCK_DGRAM"] = 2, ["SOCK_RAW"] = 3, ["IPPROTO_IP"] = 0, ["IPPROTO_TCP"] = 6, ["IPPROTO_UDP"] = 17, ["IPPROTO_IPV6"] = 41,
            ["SOL_SOCKET"] = 0xffff, ["SO_REUSEADDR"] = 4, ["SO_KEEPALIVE"] = 8, ["SO_BROADCAST"] = 0x20, ["SO_LINGER"] = 0x80, ["SO_RCVBUF"] = 0x1002, ["SO_SNDBUF"] = 0x1001,
            ["SO_REUSEPORT"] = 0, ["TCP_NODELAY"] = 1, ["SHUT_RD"] = 0, ["SHUT_WR"] = 1, ["SHUT_RDWR"] = 2, ["SOMAXCONN"] = 0x7fffffff,
            ["AI_PASSIVE"] = 1, ["NI_NUMERICHOST"] = 2, ["INADDR_ANY"] = 0, ["INADDR_LOOPBACK"] = 0x7f000001, ["MSG_PEEK"] = 2,
        };
        var constMod = DefineModule("Constants", So);
        foreach (var kv in consts) { So.Consts[kv.Key] = kv.Value; constMod.Consts[kv.Key] = kv.Value; }

        // ---- servers ----
        Native tcpServerNew = (it, self, a, b) =>
        {
            var pos = Positional(a);
            string host = pos.Length >= 2 ? it.NilOrStr(pos[0]) : null;
            int port = it.PortArg(pos.Length >= 2 ? pos[1] : pos.Length == 1 ? pos[0] : null);
            return it.NewSock((RClass)self, it.Listen(host, port));
        };
        DefS(TS, "new open", tcpServerNew);
        Native accept = (it, self, a, b) =>
        {
            var s = it.OpenSock(self);
            it.WaitReadable(s);
            Socket client;
            try { client = s.S.Accept(); }
            catch (SocketException e) { throw it.SocketErr(e, "accept(2)"); }
            var cls = ((RObject)self).Class == it.UnixServerClass || it.IsA(self, it.UnixServerClass) ? it.UnixSocketClass : it.TcpSocketClass;
            return it.NewSock(cls, client, s.UnixPath);
        };
        Def(TS, "accept", accept);
        Def(US, "accept", accept);
        Def(TS, "accept_nonblock", (it, self, a, b) =>
        {
            var s = it.OpenSock(self);
            if (!s.S.Poll(0, SelectMode.SelectRead))
            {
                if (Kw(a, "exception") is false) return Sym("wait_readable");
                throw it.Error(eagainR, "Resource temporarily unavailable - accept(2) would block");
            }
            return accept(it, self, NoArgs, null);
        });
        Def(TS, "listen", (it, self, a, b) => 0L);
        DefS(US, "new open", (it, self, a, b) =>
        {
            var path = it.Abs(it.PathStr(a[0]));
            var sock = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            try
            {
                sock.Bind(new UnixDomainSocketEndPoint(path));
                sock.Listen(128);
            }
            catch (SocketException e) { sock.Dispose(); throw it.SocketErr(e, "bind(2) for " + InspectStr(it.PathStr(a[0]))); }
            var srv = it.NewSock((RClass)self, sock, it.PathStr(a[0]));
            if (b == null) return srv;
            try { return it.CallProc(b, new object[] { srv }); }
            finally { it.CloseSock(it.SockOf(srv)); }
        });

        // ---- clients ----
        Native tcpNew = (it, self, a, b) =>
        {
            var pos = Positional(a);
            if (pos.Length < 2) throw it.ArgCount(a, 2, 4);
            var timeout = Kw(a, "connect_timeout") is { } ct and not null ? it.Num(ct) : (double?)null;
            var sock = it.NewSock((RClass)self == it.SocketClass ? it.SocketClass : (RClass)self, it.Connect(it.NilOrStr(pos[0]), it.PortArg(pos[1]), timeout));
            if (b == null) return sock;
            try { return it.CallProc(b, new object[] { sock }); }
            finally { it.CloseSock(it.SockOf(sock)); }
        };
        DefS(T, "new open", tcpNew);
        DefS(So, "tcp", (it, self, a, b) => tcpNew(it, it.SocketClass, a, b));
        DefS(So, "unix", (it, self, a, b) => it.Invoke(it.UnixSocketClass, "new", a, b));
        DefS(So, "unix_server_socket", (it, self, a, b) => it.Invoke(it.UnixServerClass, "new", a, b));
        DefS(So, "tcp_server_sockets", (it, self, a, b) =>
        {
            var pos = Positional(a);
            var srv = tcpServerNew(it, it.TcpServerClass, pos, null);
            var list = new RArray(new[] { srv });
            if (b == null) return list;
            try { return it.CallProc(b, new object[] { list }); }
            finally { it.CloseSock(it.SockOf(srv)); }
        });
        DefS(U, "new open", (it, self, a, b) =>
        {
            var path = it.Abs(it.PathStr(a[0]));
            var sock = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            try
            {
                var task = sock.ConnectAsync(new UnixDomainSocketEndPoint(path));
                it.WaitFor(ms => task.IsCompleted || ((IAsyncResult)task).AsyncWaitHandle.WaitOne(ms));
                task.GetAwaiter().GetResult();
            }
            catch (AggregateException ae) when (ae.InnerException is SocketException se) { sock.Dispose(); throw it.SocketErr(se, "connect(2) for " + it.PathStr(a[0])); }
            catch (SocketException se) { sock.Dispose(); throw it.SocketErr(se, "connect(2) for " + it.PathStr(a[0])); }
            var us = it.NewSock((RClass)self, sock, it.PathStr(a[0]));
            if (b == null) return us;
            try { return it.CallProc(b, new object[] { us }); }
            finally { it.CloseSock(it.SockOf(us)); }
        });
        DefS(So, "gethostname", (it, self, a, b) => S(Dns.GetHostName()));
        DefS(So, "getservbyname", (it, self, a, b) => (long)it.PortArg(a[0]));
        DefS(T, "gethostbyname", (it, self, a, b) =>
        {
            var host = it.Str(a[0]);
            var ips = it.Resolve(host, false);
            var res = new RArray(new object[] { S(host), new RArray(), (long)(ips[0].AddressFamily == AddressFamily.InterNetworkV6 ? 23 : 2) });
            foreach (var ip in ips) res.L.Add(S(ip.ToString()));
            return res;
        });
        DefS(ipSock, "getaddress", (it, self, a, b) => S(it.Resolve(it.Str(a[0]), false)[0].ToString()));
        DefS(B, "do_not_reverse_lookup", (it, self, a, b) => true);
        DefS(B, "do_not_reverse_lookup=", (it, self, a, b) => a[0]);

        // ---- IO on sockets ----
        Def(B, "write syswrite write_nonblock", (it, self, a, b) =>
        {
            var s = it.OpenSock(self);
            long n = 0;
            foreach (var x in Positional(a))
            {
                var bytes = x is RString rs ? RString.Bytes(rs) : RString.Utf8(it.ToS(x));
                it.SockWrite(s, bytes);
                n += bytes.Length;
            }
            return n;
        });
        Def(B, "send", (it, self, a, b) =>
        {
            var bytes = a[0] is RString rs ? RString.Bytes(rs) : RString.Utf8(it.ToS(a[0]));
            it.SockWrite(it.OpenSock(self), bytes);
            return (long)bytes.Length;
        });
        Def(B, "print", (it, self, a, b) =>
        {
            var s = it.OpenSock(self);
            foreach (var x in a) it.SockWrite(s, x is RString rs ? RString.Bytes(rs) : RString.Utf8(it.ToS(x)));
            return null;
        });
        Def(B, "<<", (it, self, a, b) => { it.SockWrite(it.OpenSock(self), a[0] is RString rs ? RString.Bytes(rs) : RString.Utf8(it.ToS(a[0]))); return self; });
        Def(B, "puts", (it, self, a, b) =>
        {
            var sw = new StringWriter { NewLine = "\n" };
            it.Puts(sw, a);
            it.SockWrite(it.OpenSock(self), RString.Utf8(sw.ToString()));
            return null;
        });
        Def(B, "printf", (it, self, a, b) => { it.SockWrite(it.OpenSock(self), RString.Utf8(it.Format(it.Str(a[0]), a[1..]))); return null; });
        Def(B, "gets", (it, self, a, b) =>
        {
            var line = it.SockGets(it.OpenSock(self), a);
            it.Globals["$_"] = line;
            return line;
        });
        Def(B, "readline", (it, self, a, b) => it.SockGets(it.OpenSock(self), a) ?? throw it.Error(it.EofErrorClass(), "end of file reached"));
        Def(B, "each_line each", (it, self, a, b) =>
        {
            if (b == null) return it.MakeEnum(self, "each_line", a, null);
            object line;
            while ((line = it.SockGets(it.OpenSock(self), a)) != null) it.CallProc(b, new[] { line });
            return self;
        });
        Def(B, "readlines", (it, self, a, b) =>
        {
            var res = new RArray();
            object line;
            while ((line = it.SockGets(it.OpenSock(self), a)) != null) res.L.Add(line);
            return res;
        });
        Def(B, "read", (it, self, a, b) =>
        {
            var s = it.OpenSock(self);
            if (a.Length == 0 || a[0] == null)
            {
                while (it.Fill(s)) { }
                return Take(s, s.Pending.Count);
            }
            long n = it.Int(a[0]);
            if (n < 0) throw it.ArgError($"negative length {n} given");
            if (n == 0) return S("");
            while (s.Pending.Count < n && it.Fill(s)) { }
            if (s.Pending.Count == 0) return null;
            var r = Take(s, (int)Math.Min(n, s.Pending.Count));
            if (a.Length > 1 && a[1] is RString buf) { buf.V = r.V; buf.Binary = r.Binary; return buf; }
            return r;
        });
        Def(B, "readpartial sysread", (it, self, a, b) =>
        {
            var s = it.OpenSock(self);
            long n = it.Int(a[0]);
            if (s.Pending.Count == 0 && !it.Fill(s)) throw it.Error(it.EofErrorClass(), "end of file reached");
            return Take(s, (int)Math.Min(n, s.Pending.Count));
        });
        Def(B, "recv", (it, self, a, b) =>
        {
            var s = it.OpenSock(self);
            long n = it.Int(a[0]);
            if (s.Pending.Count == 0 && !it.Fill(s)) return S("");
            return Take(s, (int)Math.Min(n, s.Pending.Count));
        });
        Def(B, "read_nonblock recv_nonblock", (it, self, a, b) =>
        {
            var s = it.OpenSock(self);
            long n = it.Int(a[0]);
            bool raise = Kw(a, "exception") is not false;
            if (s.Pending.Count == 0)
            {
                if (s.Eof) return raise ? throw it.Error(it.EofErrorClass(), "end of file reached") : null;
                if (!s.S.Poll(0, SelectMode.SelectRead))
                    return raise ? throw it.Error(eagainR, "Resource temporarily unavailable - read would block") : Sym("wait_readable");
                if (!it.Fill(s)) return raise ? throw it.Error(it.EofErrorClass(), "end of file reached") : null;
            }
            return Take(s, (int)Math.Min(n, s.Pending.Count));
        });
        Def(B, "getc", (it, self, a, b) =>
        {
            var s = it.OpenSock(self);
            if (s.Pending.Count == 0 && !it.Fill(s)) return null;
            // one character: a UTF-8 lead byte takes its continuation bytes along
            int len = s.Pending[0] switch { >= 0xF0 => 4, >= 0xE0 => 3, >= 0xC0 => 2, _ => 1 };
            while (s.Pending.Count < len && it.Fill(s)) { }
            return Take(s, Math.Min(len, s.Pending.Count));
        });
        Def(B, "getbyte", (it, self, a, b) =>
        {
            var s = it.OpenSock(self);
            if (s.Pending.Count == 0 && !it.Fill(s)) return null;
            long v = s.Pending[0];
            s.Pending.RemoveAt(0);
            return v;
        });
        Def(B, "eof? eof", (it, self, a, b) => { var s = it.OpenSock(self); return s.Pending.Count == 0 && !it.Fill(s); });
        Def(B, "wait_readable", (it, self, a, b) =>
        {
            var s = it.OpenSock(self);
            if (s.Pending.Count > 0 || s.Eof) return self;
            if (a.Length == 0 || a[0] == null) { it.WaitReadable(s); return self; }
            var until = DateTime.UtcNow.AddSeconds(it.Num(a[0]));
            bool ready = false;
            it.WaitFor(ms =>
            {
                var left = (int)Math.Max(0, (until - DateTime.UtcNow).TotalMilliseconds);
                ready = s.S.Poll(Math.Min(ms, left) * 1000, SelectMode.SelectRead);
                return ready || DateTime.UtcNow >= until;
            });
            return ready ? self : null;
        });
        Def(B, "ready?", (it, self, a, b) => it.OpenSock(self) is var s && (s.Pending.Count > 0 || s.S.Poll(0, SelectMode.SelectRead)));
        Def(B, "close", (it, self, a, b) => { it.CloseSock(it.SockOf(self)); return null; });
        Def(B, "closed?", (it, self, a, b) => it.SockOf(self).Closed);
        Def(B, "close_write", (it, self, a, b) =>
        {
            var s = it.OpenSock(self);
            if (!s.WriteClosed) { try { s.S.Shutdown(SocketShutdown.Send); } catch (SocketException) { } s.WriteClosed = true; }
            if (s.ReadClosed) it.CloseSock(s);
            return null;
        });
        Def(B, "close_read", (it, self, a, b) =>
        {
            var s = it.OpenSock(self);
            if (!s.ReadClosed) { try { s.S.Shutdown(SocketShutdown.Receive); } catch (SocketException) { } s.ReadClosed = true; }
            if (s.WriteClosed) it.CloseSock(s);
            return null;
        });
        Def(B, "shutdown", (it, self, a, b) =>
        {
            var s = it.OpenSock(self);
            long how = a.Length > 0 ? a[0] is RSymbol y ? y.Name switch { "RD" or "SHUT_RD" => 0, "WR" or "SHUT_WR" => 1, _ => 2 } : it.Int(a[0]) : 2;
            try { s.S.Shutdown(how == 0 ? SocketShutdown.Receive : how == 1 ? SocketShutdown.Send : SocketShutdown.Both); }
            catch (SocketException e) { throw it.SocketErr(e, "shutdown(2)"); }
            if (how != 1) s.ReadClosed = true;
            if (how != 0) s.WriteClosed = true;
            return 0L;
        });
        Def(B, "addr", (it, self, a, b) => it.AddrArray(it.OpenSock(self).S.LocalEndPoint));
        Def(B, "peeraddr", (it, self, a, b) =>
        {
            try { return it.AddrArray(it.OpenSock(self).S.RemoteEndPoint); }
            catch (SocketException e) { throw it.SocketErr(e, "getpeername(2)"); }
        });
        Def(B, "local_address", (it, self, a, b) => it.NewAddrinfo(it.OpenSock(self).S.LocalEndPoint));
        Def(B, "remote_address", (it, self, a, b) => it.NewAddrinfo(it.OpenSock(self).S.RemoteEndPoint));
        Def(B, "setsockopt", (it, self, a, b) =>
        {
            var s = it.OpenSock(self);
            if (a.Length >= 3 && a[1] is long or RSymbol or RString)
            {
                var opt = a[1] is long o ? o : (long)consts.GetValueOrDefault(it.ToS(a[1]).StartsWith("SO_") || it.ToS(a[1]).StartsWith("TCP_") ? it.ToS(a[1]) : "SO_" + it.ToS(a[1]), -1);
                bool on = a[2] is bool bv ? bv : a[2] is long lv ? lv != 0 : Truthy(a[2]);
                try
                {
                    if (opt == consts["TCP_NODELAY"] && (a[0] is long lvl && lvl == 6 || a[0] is RSymbol { Name: "TCP" or "IPPROTO_TCP" })) s.S.NoDelay = on;
                    else if (opt == consts["SO_KEEPALIVE"]) s.S.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, on);
                    // SO_REUSEADDR on Windows would let another process steal the port; Ruby's own
                    // TCPServer doesn't set it there either.
                }
                catch (SocketException e) { throw it.SocketErr(e, "setsockopt(2)"); }
            }
            return 0L;
        });
        Def(B, "getsockopt", (it, self, a, b) => 0L);
        Def(B, "fileno to_i", (it, self, a, b) => (long)it.OpenSock(self).S.Handle);
        Def(B, "to_io binmode flush", (it, self, a, b) => self);
        Def(B, "sync", (it, self, a, b) => true);
        Def(B, "sync=", (it, self, a, b) => a[0]);
        Def(B, "autoclose= close_on_exec=", (it, self, a, b) => a[0]);
        Def(B, "tty? isatty", (it, self, a, b) => false);
        Def(B, "inspect", (it, self, a, b) =>
        {
            var s = it.SockOf(self);
            return S($"#<{((RObject)self).Class.Name}:{(s.Closed ? "(closed)" : "fd " + (long)s.S.Handle)}>");
        });
        Def(U, "path", (it, self, a, b) => S(it.SockOf(self).UnixPath ?? ""));

        // ---- Addrinfo ----
        (EndPoint ep, string type) Ai(object o) => ((EndPoint, string))((RObject)o).Host;
        DefS(A, "tcp", (it, self, a, b) => it.NewAddrinfo(new IPEndPoint(it.Resolve(it.NilOrStr(a[0]), false)[0], it.PortArg(a[1]))));
        DefS(A, "udp", (it, self, a, b) => it.NewAddrinfo(new IPEndPoint(it.Resolve(it.NilOrStr(a[0]), false)[0], it.PortArg(a[1])), "UDP"));
        DefS(A, "ip", (it, self, a, b) => it.NewAddrinfo(new IPEndPoint(it.Resolve(it.NilOrStr(a[0]), false)[0], 0), null));
        DefS(A, "unix", (it, self, a, b) => it.NewAddrinfo(new UnixDomainSocketEndPoint(it.Abs(it.PathStr(a[0]))), "UNIX"));
        DefS(A, "getaddrinfo", (it, self, a, b) =>
        {
            int port = a.Length > 1 ? it.PortArg(a[1]) : 0;
            return new RArray(it.Resolve(it.NilOrStr(a[0]), false).Select(ip => (object)it.NewAddrinfo(new IPEndPoint(ip, port))));
        });
        Def(A, "ip_address", (it, self, a, b) => Ai(self).ep is IPEndPoint ip ? S((ip.Address.IsIPv4MappedToIPv6 ? ip.Address.MapToIPv4() : ip.Address).ToString()) : throw it.Error(it.SocketErrorClass(), "need IPv4 or IPv6 address"));
        Def(A, "ip_port", (it, self, a, b) => Ai(self).ep is IPEndPoint ip ? (long)ip.Port : throw it.Error(it.SocketErrorClass(), "need IPv4 or IPv6 address"));
        Def(A, "ip_unpack", (it, self, a, b) => Ai(self).ep is IPEndPoint ip ? new RArray(new object[] { S(ip.Address.ToString()), (long)ip.Port }) : throw it.Error(it.SocketErrorClass(), "need IPv4 or IPv6 address"));
        Def(A, "ip?", (it, self, a, b) => Ai(self).ep is IPEndPoint);
        Def(A, "ipv4?", (it, self, a, b) => Ai(self).ep is IPEndPoint ip && (ip.AddressFamily == AddressFamily.InterNetwork || ip.Address.IsIPv4MappedToIPv6));
        Def(A, "ipv6?", (it, self, a, b) => Ai(self).ep is IPEndPoint ip && ip.AddressFamily == AddressFamily.InterNetworkV6 && !ip.Address.IsIPv4MappedToIPv6);
        Def(A, "ipv4_loopback?", (it, self, a, b) => Ai(self).ep is IPEndPoint ip && IPAddress.IsLoopback(ip.Address) && ip.AddressFamily == AddressFamily.InterNetwork);
        Def(A, "ipv6_loopback?", (it, self, a, b) => Ai(self).ep is IPEndPoint ip && ip.Address.Equals(IPAddress.IPv6Loopback));
        Def(A, "unix?", (it, self, a, b) => Ai(self).ep is UnixDomainSocketEndPoint);
        Def(A, "unix_path", (it, self, a, b) => Ai(self).ep is UnixDomainSocketEndPoint u ? S(u.ToString()) : throw it.Error(it.SocketErrorClass(), "need AF_UNIX address"));
        Def(A, "afamily pfamily", (it, self, a, b) => Ai(self).ep switch
        {
            IPEndPoint ip => ip.AddressFamily == AddressFamily.InterNetworkV6 ? 23L : 2L,
            UnixDomainSocketEndPoint => 1L,
            _ => 0L,
        });
        Def(A, "socktype", (it, self, a, b) => Ai(self).type == "UDP" ? 2L : 1L);
        Def(A, "inspect_sockaddr to_s", (it, self, a, b) => Ai(self).ep switch
        {
            IPEndPoint { AddressFamily: AddressFamily.InterNetworkV6 } ip => S($"[{ip.Address}]:{ip.Port}"),
            IPEndPoint ip => S($"{ip.Address}:{ip.Port}"),
            var ep => S(ep?.ToString() ?? ""),
        });
        Def(A, "inspect", (it, self, a, b) =>
        {
            var (ep, type) = Ai(self);
            var addr = ep switch
            {
                IPEndPoint { AddressFamily: AddressFamily.InterNetworkV6 } ip => $"[{ip.Address}]:{ip.Port}",
                IPEndPoint ip => ip.Port == 0 ? ip.Address.ToString() : $"{ip.Address}:{ip.Port}",
                _ => ep?.ToString() ?? "",
            };
            return S($"#<Addrinfo: {addr}{(type is "TCP" or "UDP" ? " " + type : "")}>");
        });
        Def(A, "connect", (it, self, a, b) =>
        {
            var (ep, _) = Ai(self);
            if (ep is IPEndPoint ip) return tcpNew(it, it.SocketClass, new object[] { S(ip.Address.ToString()), (long)ip.Port }, b);
            return it.Invoke(it.UnixSocketClass, "new", new object[] { S(((UnixDomainSocketEndPoint)ep).ToString()) }, b);
        });
    }

    void CloseSock(RSocket s)
    {
        if (s.Closed) return;
        s.Closed = true;
        try { s.S.Dispose(); } catch (SocketException) { }
    }
}
