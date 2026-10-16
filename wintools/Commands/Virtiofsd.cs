using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace WinTools;

// virtiofsd: share a host directory with a VM guest. Upstream is the vhost-user backend of a virtio-fs
// device, which a Windows host can't be (vhost-user hands the backend guest memory and eventfds over
// SCM_RIGHTS, and no Windows VMM has vhost-user virtio-fs). The sharing itself works over 9P2000.L,
// the protocol Linux's v9fs client speaks: this serves --shared-dir over TCP (--listen) or an AF_UNIX
// socket (--socket-path) to `mount -t 9p -o trans=tcp,port=564,version=9p2000.L,aname=/ HOST /mnt`
// in a Hyper-V, VirtualBox, VMware or QEMU guest. A fid is a path under the shared dir; every open
// goes through NtCreateFile relative to a handle on the shared dir and every handle's final path must
// still be inside it, so neither `..` nor a symlink or junction leads out (whatever --sandbox says).
// Guest symlinks become WSL (LX_SYMLINK) reparse points, which Windows itself never follows; names
// with characters NTFS reserves are stored the way WSL stores them (U+F000 + the character).
static class Virtiofsd
{
    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("virtiofsd", "Share a directory with VM guests (a 9P2000.L server in virtiofsd's place)", Run);

    const string Version = "1.14.0";
    static readonly string[] LogLevels = ["off", "error", "warn", "info", "debug", "trace"];
    const int LogError = 1, LogInfo = 3, LogDebug = 4;

    static int Run(string[] args)
    {
        string sharedDir = null, socketPath = null, listen = null, sandbox = "namespace", cache = "auto", logLevel = "info", selfTest = null;
        var readOnly = false;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            var eq = a.StartsWith("--") ? a.IndexOf('=') : -1;
            var name = eq > 0 ? a[..eq] : a;
            string Value() => eq > 0 ? a[(eq + 1)..] : ++i < args.Length ? args[i] : throw new UsageException($"a value is required for '{name}'");
            switch (name)
            {
                case "-h" or "--help": Usage(); return 0;
                case "-V" or "--version": Console.WriteLine($"virtiofsd {Version}"); return 0;
                case "--print-capabilities":
                    Console.WriteLine("{\n  \"type\": \"fs\",\n  \"features\": [\n    \"migrate-precopy\",\n    \"separate-options\"\n  ]\n}");
                    return 0;
                case "--shared-dir": sharedDir = Value(); break;
                case "--socket-path" or "--socket": socketPath = Value(); break;
                case "--listen": listen = Value(); break;
                case "--readonly": readOnly = true; break;
                case "--sandbox": sandbox = Value(); break;
                case "--cache": cache = Value(); break;
                case "--log-level": logLevel = Value(); break;
                case "--self-test": selfTest = Value(); break;
                case "--fd":
                    Value();
                    throw new UsageException("--fd needs an inherited socket descriptor, which Windows doesn't hand over by number; use --socket-path or --listen");
                case "-o":
                    foreach (var opt in Value().Split(','))
                        if (opt.StartsWith("source=")) sharedDir ??= opt[7..];
                        else if (opt.StartsWith("cache=")) cache = opt[6..];
                    break;
                case "--thread-pool-size" or "--rlimit-nofile" or "--modcaps" or "--inode-file-handles" or "--socket-group"
                    or "--migration-mode" or "--migration-on-error" or "--translate-uid" or "--translate-gid" or "--shared-dir-id":
                    Value();
                    break;
                case "--announce-submounts" or "--xattr" or "--no-xattr" or "--posix-acl" or "--no-posix-acl" or "--allow-direct-io"
                    or "--writeback" or "--killpriv-v2" or "--no-killpriv-v2" or "--security-label" or "--syslog"
                    or "--allow-mmap" or "--preserve-noatime" or "-f" or "-d":
                    break;
                default: throw new UsageException($"unexpected argument '{a}' found");
            }
        }
        var level = Array.IndexOf(LogLevels, logLevel);
        if (level < 0) throw new UsageException($"invalid value '{logLevel}' for '--log-level <LOG_LEVEL>'");
        if (sandbox is not ("namespace" or "chroot" or "none")) throw new UsageException($"invalid value '{sandbox}' for '--sandbox <SANDBOX>'");
        if (cache is not ("never" or "metadata" or "auto" or "always")) throw new UsageException($"invalid value '{cache}' for '--cache <CACHE>'");
        // TCP unless only --socket-path was asked for.
        var endPoint = listen != null || socketPath == null ? ParseListen(listen ?? "127.0.0.1:564") : null;
        if (selfTest != null) return SelfTest(selfTest, listen != null ? endPoint : null, socketPath);
        if (sharedDir == null) throw new UsageException("the following required arguments were not provided:\n  --shared-dir <SHARED_DIR>");
        if (!Directory.Exists(sharedDir)) throw new InvalidOperationException($"shared directory {sharedDir} is not a directory");
        return Serve(new Server(sharedDir, readOnly, level), endPoint, socketPath);
    }

    static IPEndPoint ParseListen(string s)
    {
        var colon = s.LastIndexOf(':');
        var host = colon > 0 ? s[..colon].Trim('[', ']') : "";
        if (colon <= 0 || !ushort.TryParse(s[(colon + 1)..], out var port)
            || !(IPAddress.TryParse(host, out var ip) || (host == "localhost" && (ip = IPAddress.Loopback) != null)))
            throw new UsageException($"invalid value '{s}' for '--listen <ADDR:PORT>'");
        return new IPEndPoint(ip, port);
    }

    static void Usage() => Console.WriteLine($"""
        virtiofsd backend {Version}
        Share a host directory with a VM guest.

        Usage: virtiofsd [OPTIONS] --shared-dir <SHARED_DIR>

        Options:
              --shared-dir <SHARED_DIR>    Shared directory path
              --socket-path <SOCKET_PATH>  Serve on this AF_UNIX socket
              --listen <ADDR:PORT>         Serve on TCP [default: 127.0.0.1:564; none with only --socket-path]
              --readonly                   Share the directory read-only
              --sandbox <SANDBOX>          Sandbox mechanism (namespace, chroot, none) [default: namespace]
              --cache <CACHE>              The caching policy (auto, always, metadata, never) [default: auto]
              --log-level <LOG_LEVEL>      Log level (error, warn, info, debug, trace, off) [default: info]
              --print-capabilities         Print vhost-user.json backend program capabilities and exit
          -h, --help                       Print help
          -V, --version                    Print version

        Windows has no vhost-user virtio-fs, so the directory is served over 9P2000.L, which a Linux
        guest mounts with:
          mount -t 9p -o trans=tcp,port=564,version=9p2000.L,aname=/ HOST /mnt
        Every --sandbox mode keeps the guest inside the shared directory (no `..`, symlink or junction
        leads out). Caching is the guest's choice (mount -o cache=...).
        """);

    // ---- listeners ----

    static int Serve(Server server, IPEndPoint endPoint, string socketPath)
    {
        var listeners = new List<Socket>();
        var where = socketPath;
        try
        {
            if (socketPath != null)
            {
                var unix = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                listeners.Add(unix);
                unix.Bind(new UnixDomainSocketEndPoint(socketPath));
                unix.Listen(16);
                server.Log(LogInfo, $"serving {server.RootDos} on {socketPath}");
            }
            if (endPoint != null)
            {
                where = endPoint.ToString();
                var tcp = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listeners.Add(tcp);
                tcp.Bind(endPoint);
                tcp.Listen(16);
                server.Log(LogInfo, $"serving {server.RootDos} on {tcp.LocalEndPoint} (9P2000.L{(server.ReadOnly ? ", read-only" : "")})");
            }
        }
        catch (SocketException e)
        {
            Console.Error.WriteLine($"virtiofsd: can't listen on {where}: {(e.SocketErrorCode == SocketError.AddressAlreadyInUse ? "Address already in use" : e.Message)}");
            foreach (var l in listeners) l.Dispose();
            return 1;
        }
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            foreach (var l in listeners) l.Close();
        };
        var threads = listeners.Select(l => new Thread(() => Accept(server, l)) { IsBackground = true }).ToList();
        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());
        if (socketPath != null)
            try { File.Delete(socketPath); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        return 0;
    }

    static void Accept(Server server, Socket listener)
    {
        try
        {
            while (true)
            {
                var s = listener.Accept();
                var peer = "unix socket";
                if (s.AddressFamily != AddressFamily.Unix)
                {
                    s.NoDelay = true;
                    peer = s.RemoteEndPoint?.ToString() ?? "tcp";
                }
                new Thread(() => server.Serve(new NetworkStream(s, ownsSocket: true), peer)) { IsBackground = true }.Start();
            }
        }
        catch (Exception e) when (e is SocketException or ObjectDisposedException) { }
    }

    static bool ReadFully(Stream s, Span<byte> buf)
    {
        for (var got = 0; got < buf.Length;)
        {
            var n = s.Read(buf[got..]);
            if (n == 0) return false;
            got += n;
        }
        return true;
    }

    // ---- 9P2000.L ----

    const byte Rlerror = 7, Tstatfs = 8, Tlopen = 12, Tlcreate = 14, Tsymlink = 16, Tmknod = 18, Trename = 20, Treadlink = 22,
        Tgetattr = 24, Tsetattr = 26, Txattrwalk = 30, Txattrcreate = 32, Treaddir = 40, Tfsync = 50, Tlock = 52, Tgetlock = 54,
        Tlink = 70, Tmkdir = 72, Trenameat = 74, Tunlinkat = 76, Tversion = 100, Tauth = 102, Tattach = 104, Tflush = 108,
        Twalk = 110, Tread = 116, Twrite = 118, Tclunk = 120, Tremove = 122;
    const uint NOFID = 0xFFFFFFFF, MaxMsize = 1 << 20;
    const byte QTDIR = 0x80, QTSYMLINK = 0x02, DT_DIR = 4, DT_REG = 8, DT_LNK = 10;
    const uint O_ACCMODE = 3, O_WRONLY = 1, O_RDWR = 2, O_CREAT = 0x40, O_EXCL = 0x80, O_TRUNC = 0x200, O_APPEND = 0x400, O_DIRECTORY = 0x10000;
    const uint AT_REMOVEDIR = 0x200;
    const uint SET_MODE = 0x1, SET_SIZE = 0x8, SET_ATIME = 0x10, SET_MTIME = 0x20, SET_ATIME_SET = 0x80, SET_MTIME_SET = 0x100;

    // Linux errno values.
    const uint EPERM = 1, ENOENT = 2, EIO = 5, EBADF = 9, EAGAIN = 11, ENOMEM = 12, EACCES = 13, EBUSY = 16, EEXIST = 17, EXDEV = 18,
        ENOTDIR = 20, EISDIR = 21, EINVAL = 22, EMFILE = 24, EFBIG = 27, ENOSPC = 28, EROFS = 30, EMLINK = 31, ENAMETOOLONG = 36,
        ENOTEMPTY = 39, ELOOP = 40, EBADMSG = 74, EOPNOTSUPP = 95;

    sealed class P9Error(uint errno) : Exception($"errno {errno}")
    {
        public readonly uint Errno = errno;
    }

    static uint Errno(Exception e) => e switch
    {
        P9Error p => p.Errno,
        UnauthorizedAccessException => EACCES,
        FileNotFoundException or DirectoryNotFoundException => ENOENT,
        IOException io when (io.HResult & unchecked((int)0xFFFF0000)) == unchecked((int)0x80070000) => Win32Errno(io.HResult & 0xFFFF),
        NotSupportedException => EOPNOTSUPP,
        ArgumentException => EINVAL,
        OutOfMemoryException => ENOMEM,
        _ => EIO,
    };

    static uint Win32Errno(int error) => error switch
    {
        2 or 3 or 15 or 161 or 303 => ENOENT,
        4 => EMFILE,
        5 or 1920 => EACCES,
        6 => EBADF,
        8 or 14 => ENOMEM,
        17 => EXDEV,
        19 => EROFS,
        32 => EBUSY,
        33 => EAGAIN,
        39 or 112 => ENOSPC,
        50 => EOPNOTSUPP,
        80 or 183 => EEXIST,
        87 or 123 or 4390 => EINVAL,
        145 => ENOTEMPTY,
        206 => ENAMETOOLONG,
        223 => EFBIG,
        267 => ENOTDIR,
        1142 => EMLINK,
        1314 => EPERM,
        1921 => ELOOP,
        _ => EIO,
    };

    static P9Error NtError(int status) => new((uint)status switch
    {
        0xC000000F or 0xC0000034 or 0xC000003A or 0xC0000056 => ENOENT,
        0xC0000035 => EEXIST,
        0xC0000101 => ENOTEMPTY,
        0xC00000BA => EISDIR,
        0xC0000103 => ENOTDIR,
        0xC0000022 or 0xC0000121 => EACCES,
        0xC0000043 => EBUSY,
        0xC000007F => ENOSPC,
        0xC00000D4 => EXDEV,
        0xC0000033 or 0xC000003B => EINVAL,
        0xC0000279 => ELOOP,
        0xC00000A2 => EROFS,
        _ => Win32Errno(RtlNtStatusToDosError(status)),
    });

    static P9Error LastError() => new(Win32Errno(Marshal.GetLastPInvokeError()));

    // Linux names may hold characters NTFS reserves; like WSL, they're stored as U+F000 + the character.
    const string Reserved = "\"*:<>?\\|";

    static string ToHost(string name)
    {
        if (name.Length == 0 || name.Contains('/') || name.Contains('\0')) throw new P9Error(EINVAL);
        StringBuilder sb = null;
        for (var k = 0; k < name.Length; k++)
        {
            var c = name[k];
            if (c < 32 || Reserved.Contains(c))
            {
                sb ??= new StringBuilder(name, 0, k, name.Length);
                sb.Append((char)(0xF000 + c));
            }
            else sb?.Append(c);
        }
        var host = sb?.ToString() ?? name;
        return host.Length > 255 ? throw new P9Error(ENAMETOOLONG) : host;
    }

    static string FromHost(string name)
    {
        if (!name.Any(IsMapped)) return name;
        var chars = name.ToCharArray();
        for (var k = 0; k < chars.Length; k++)
            if (IsMapped(chars[k])) chars[k] = (char)(chars[k] - 0xF000);
        return new string(chars);
    }

    static bool IsMapped(char c) => c is >= '\uF000' and < '\uF080' && (c < '\uF020' || Reserved.Contains((char)(c - 0xF000)));

    // Fid paths are relative to the shared dir, '\'-separated, "" for the shared dir itself.
    static string Child(string dir, string name)
    {
        if (name is "." or "..") throw new P9Error(EINVAL);
        name = ToHost(name);
        return dir.Length == 0 ? name : dir + "\\" + name;
    }

    static string Parent(string path) => path.LastIndexOf('\\') is var k and >= 0 ? path[..k] : "";

    static string Leaf(string path) => path[(path.LastIndexOf('\\') + 1)..];

    sealed class Fid(string path, uint uid)
    {
        public volatile string Path = path;
        public readonly uint Uid = uid;
        public SafeFileHandle Handle;
        public bool IsDir, Writable, Append;
        public List<Entry> Listing;

        public void Close()
        {
            Handle?.Dispose();
            Handle = null;
        }
    }

    readonly record struct Entry(string Name, byte QidType, ulong QidPath, byte DType);

    struct Attr
    {
        public ulong Id, Size;
        public uint Volume, Attributes, Tag, Links;
        public long Atime, Mtime, Ctime, Btime;

        public readonly bool IsLink => (Attributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0 && (Tag & 0x20000000) != 0;   // a name surrogate
        public readonly bool IsDir => (Attributes & FILE_ATTRIBUTE_DIRECTORY) != 0 && !IsLink;
        public readonly bool IsReparse => (Attributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0;
    }

    // ---- the server: one shared directory, any number of connections ----

    sealed class Server : IDisposable
    {
        public readonly SafeFileHandle Root;
        public readonly string RootNt, RootDos;
        public readonly uint RootVolume;
        public readonly bool ReadOnly;
        public readonly int Level;
        readonly List<Session> sessions = [];

        public Server(string dir, bool readOnly, int level)
        {
            ReadOnly = readOnly;
            Level = level;
            Root = CreateFileW(Path.GetFullPath(dir), FILE_LIST_DIRECTORY | FILE_TRAVERSE | FILE_READ_ATTRIBUTES | SYNCHRONIZE,
                7, IntPtr.Zero, 3 /* OPEN_EXISTING */, 0x02000000 /* FILE_FLAG_BACKUP_SEMANTICS */, IntPtr.Zero);
            if (Root.IsInvalid) throw new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError(), $"can't open shared directory {dir}");
            RootNt = FinalPath(Root, VOLUME_NAME_NT) ?? throw new IOException($"can't resolve shared directory {dir}");
            var dos = FinalPath(Root, VOLUME_NAME_DOS) ?? Path.GetFullPath(dir).TrimEnd('\\');
            RootDos = dos.StartsWith(@"\\?\UNC\") ? @"\\" + dos[8..] : dos.StartsWith(@"\\?\") ? dos[4..] : dos;
            RootVolume = Stat(Root).Volume;
        }

        public void Dispose() => Root.Dispose();

        public void Log(int level, string message)
        {
            if (level <= Level) Console.Error.WriteLine($"virtiofsd: {message}");
        }

        public void Serve(Stream stream, string peer)
        {
            var s = new Session(this, stream, peer);
            lock (sessions) sessions.Add(s);
            Log(LogInfo, $"{peer}: connected");
            try { s.Run(); }
            finally
            {
                lock (sessions) sessions.Remove(s);
                Log(LogInfo, $"{peer}: disconnected");
            }
        }

        // Opens a fid path relative to the shared dir, not following a final symlink unless asked, and
        // refuses a handle that ended up outside it (through a junction or symlink on the way).
        public SafeFileHandle OpenPath(string path, uint access, uint options, bool follow)
        {
            var h = OpenAt(Root, path, access, FILE_OPEN, options | (follow ? 0 : FILE_OPEN_REPARSE_POINT));
            var final = FinalPath(h, VOLUME_NAME_NT);
            if (final == null || !(final.Equals(RootNt, StringComparison.OrdinalIgnoreCase) || final.StartsWith(RootNt + "\\", StringComparison.OrdinalIgnoreCase)))
            {
                h.Dispose();
                throw new P9Error(EACCES);
            }
            return h;
        }

        public SafeFileHandle OpenDir(string path) =>
            OpenPath(path, FILE_LIST_DIRECTORY | FILE_TRAVERSE | FILE_READ_ATTRIBUTES, FILE_DIRECTORY_FILE, follow: true);

        public Attr StatPath(string path)
        {
            using var h = OpenPath(path, FILE_READ_ATTRIBUTES, 0, follow: false);
            return Stat(h);
        }

        public ulong QidPath(ulong id, uint volume) => volume == RootVolume ? id : id ^ (ulong)volume << 32;

        public void Rename(string from, string to)
        {
            if (from.Length == 0 || to.Length == 0) throw new P9Error(EBUSY);
            if (to.StartsWith(from + "\\", StringComparison.OrdinalIgnoreCase)) throw new P9Error(EINVAL);
            using var oldDir = OpenDir(Parent(from));
            using var newDir = OpenDir(Parent(to));
            using var h = OpenAt(oldDir, Leaf(from), DELETE | FILE_READ_ATTRIBUTES, FILE_OPEN, FILE_OPEN_REPARSE_POINT);
            var a = Stat(h);
            SafeFileHandle target = null;
            try { target = OpenAt(newDir, Leaf(to), DELETE | FILE_READ_ATTRIBUTES, FILE_OPEN, FILE_OPEN_REPARSE_POINT); }
            catch (P9Error e) when (e.Errno == ENOENT) { }
            if (target != null)
                using (target)
                {
                    var b = Stat(target);
                    if (b.Id == a.Id && b.Volume == a.Volume)
                    {
                        // The same file: a no-op, unless it's a change of case in the same directory.
                        if (from == to || !from.Equals(to, StringComparison.OrdinalIgnoreCase)) return;
                    }
                    else if (b.IsDir && !a.IsDir) throw new P9Error(EISDIR);
                    else if (!b.IsDir && a.IsDir) throw new P9Error(ENOTDIR);
                    else if (b.IsDir) Delete(target);   // a directory replaces an empty one (ENOTEMPTY otherwise)
                }
            SetName(h, newDir, Leaf(to), link: false);
            lock (sessions)
                foreach (var s in sessions) s.Renamed(from, to);
        }

        public byte[] ReadLink(SafeFileHandle h, string path)
        {
            var buf = new byte[16 * 1024];
            unsafe
            {
                fixed (byte* b = buf)
                    if (!DeviceIoControl(h, FSCTL_GET_REPARSE_POINT, null, 0, b, (uint)buf.Length, out _, IntPtr.Zero))
                        throw Marshal.GetLastPInvokeError() == 4390 ? new P9Error(EINVAL) : LastError();
            }
            var tag = BinaryPrimitives.ReadUInt32LittleEndian(buf);
            var length = BinaryPrimitives.ReadUInt16LittleEndian(buf.AsSpan(4));
            if (tag == IO_REPARSE_TAG_LX_SYMLINK) return buf[12..(8 + length)];
            if (tag is not (IO_REPARSE_TAG_SYMLINK or IO_REPARSE_TAG_MOUNT_POINT)) throw new P9Error(EINVAL);
            var header = tag == IO_REPARSE_TAG_SYMLINK ? 20 : 16;
            string Name(int at) => Encoding.Unicode.GetString(buf, header + BinaryPrimitives.ReadUInt16LittleEndian(buf.AsSpan(at)),
                BinaryPrimitives.ReadUInt16LittleEndian(buf.AsSpan(at + 2)));
            var target = Name(12);
            if (target.Length == 0) target = Name(8);
            if (target.StartsWith(@"\??\")) target = target[4..];
            if (tag == IO_REPARSE_TAG_SYMLINK && (BinaryPrimitives.ReadUInt32LittleEndian(buf.AsSpan(16)) & 1) != 0)
                return Encoding.UTF8.GetBytes(GuestPath(target));   // relative already
            // An absolute Windows target is shown relative to the link, and only if it stays inside.
            if (target.StartsWith(@"UNC\")) target = @"\\" + target[4..];
            target = target.TrimEnd('\\');
            if (!(target.Equals(RootDos, StringComparison.OrdinalIgnoreCase) || target.StartsWith(RootDos + "\\", StringComparison.OrdinalIgnoreCase)))
                throw new P9Error(EACCES);
            var dir = Parent(path);
            return Encoding.UTF8.GetBytes(GuestPath(Path.GetRelativePath(dir.Length == 0 ? RootDos : RootDos + "\\" + dir, target)));
        }

        static string GuestPath(string windows) => string.Join('/', windows.Split('\\').Select(FromHost));
    }

    // ---- one connection: requests run concurrently, replies are written whole ----

    sealed class Session(Server server, Stream stream, string peer)
    {
        readonly ConcurrentDictionary<uint, Fid> fids = new();
        readonly ConcurrentDictionary<ushort, Task> inflight = new();
        readonly object writeLock = new();
        uint msize = MaxMsize;

        public void Run()
        {
            var head = new byte[4];
            try
            {
                while (ReadFully(stream, head))
                {
                    var size = BinaryPrimitives.ReadUInt32LittleEndian(head);
                    if (size < 7 || size > msize)
                    {
                        server.Log(LogError, $"{peer}: bad message size {size}, closing the connection");
                        break;
                    }
                    var body = new byte[size - 4];
                    if (!ReadFully(stream, body)) break;
                    var type = body[0];
                    var tag = BinaryPrimitives.ReadUInt16LittleEndian(body.AsSpan(1));
                    if (type == Tversion)
                    {
                        WaitAll();
                        Reply(Handle(type, tag, body));
                        continue;
                    }
                    var task = new Task(() => Reply(Handle(type, tag, body)));
                    inflight[tag] = task;
                    task.ContinueWith(t => inflight.TryRemove(new KeyValuePair<ushort, Task>(tag, t)), TaskScheduler.Default);
                    task.Start(TaskScheduler.Default);
                }
            }
            catch (Exception e) when (e is IOException or ObjectDisposedException) { }
            finally
            {
                WaitAll();
                foreach (var f in fids.Values) f.Close();
                fids.Clear();
                stream.Dispose();
            }
        }

        void WaitAll()
        {
            try { Task.WaitAll(inflight.Values.ToArray()); } catch (AggregateException) { }
        }

        void Reply(Out o)
        {
            lock (writeLock)
                try { stream.Write(o.Finish()); }
                catch (Exception e) when (e is IOException or ObjectDisposedException) { }
        }

        public void Renamed(string from, string to)
        {
            foreach (var f in fids.Values)
            {
                var p = f.Path;
                if (p.Equals(from, StringComparison.OrdinalIgnoreCase)) f.Path = to;
                else if (p.StartsWith(from + "\\", StringComparison.OrdinalIgnoreCase)) f.Path = to + p[from.Length..];
            }
        }

        Fid Get(uint id) => fids.TryGetValue(id, out var f) ? f : throw new P9Error(EBADF);

        void Writable()
        {
            if (server.ReadOnly) throw new P9Error(EROFS);
        }

        void Qid(Out o, Attr a)
        {
            o.U8(a.IsLink ? QTSYMLINK : a.IsDir ? QTDIR : (byte)0);
            o.U32(a.IsDir ? 0 : (uint)(a.Mtime ^ a.Mtime >> 32));
            o.U64(server.QidPath(a.Id, a.Volume));
        }

        Out Handle(byte type, ushort tag, byte[] body)
        {
            var i = new In(body, 3);
            try
            {
                var o = new Out((byte)(type + 1), tag);
                Dispatch(type, tag, i, o);
                return o;
            }
            catch (Exception e)
            {
                var errno = Errno(e);
                server.Log(LogDebug, $"{peer}: message {type} (tag {tag}): errno {errno}");
                var r = new Out(Rlerror, tag);
                r.U32(errno);
                return r;
            }
        }

        void Dispatch(byte type, ushort tag, In i, Out o)
        {
            switch (type)
            {
                case Tversion:
                {
                    var size = i.U32();
                    var version = i.Str();
                    foreach (var f in fids.Values) f.Close();
                    fids.Clear();
                    msize = Math.Clamp(size, 4096, MaxMsize);
                    o.U32(msize);
                    o.Str(version.StartsWith("9P2000.L") ? "9P2000.L" : "unknown");
                    break;
                }
                case Tauth: throw new P9Error(EOPNOTSUPP);
                case Tattach:
                {
                    var id = i.U32();
                    i.U32();
                    i.Str();
                    var aname = i.Str();
                    var uid = i.Remaining >= 4 ? i.U32() : NOFID;
                    if (fids.ContainsKey(id)) throw new P9Error(EBADF);
                    var path = "";
                    foreach (var part in aname.Split('/', StringSplitOptions.RemoveEmptyEntries))
                        if (part == "..") throw new P9Error(EACCES);
                        else if (part != ".") path = Child(path, part);
                    var a = server.StatPath(path);
                    if (!a.IsDir) throw new P9Error(ENOTDIR);
                    fids[id] = new Fid(path, uid == NOFID ? 0 : uid);
                    Qid(o, a);
                    break;
                }
                case Tflush:
                {
                    var old = i.U16();
                    if (old != tag && inflight.TryGetValue(old, out var t))
                        try { t.Wait(); } catch (AggregateException) { }
                    break;
                }
                case Twalk:
                {
                    var id = i.U32();
                    var fid = Get(id);
                    var newId = i.U32();
                    var n = i.U16();
                    if (n > 16) throw new P9Error(EINVAL);
                    var names = new string[n];
                    for (var k = 0; k < n; k++) names[k] = i.Str();
                    if (newId != id && fids.ContainsKey(newId)) throw new P9Error(EBADF);
                    var path = fid.Path;
                    var walked = new List<Attr>();
                    for (var k = 0; k < n; k++)
                    {
                        try
                        {
                            if (k > 0 && !walked[k - 1].IsDir) throw new P9Error(ENOTDIR);
                            // `..` stops at the shared dir, like `..` at /.
                            var next = names[k] switch { ".." => Parent(path), "." => path, var s => Child(path, s) };
                            walked.Add(server.StatPath(next));
                            path = next;
                        }
                        catch (Exception) when (k > 0) { break; }
                    }
                    if (walked.Count == n && !(n == 0 && newId == id))
                    {
                        if (newId == id) fid.Close();
                        fids[newId] = new Fid(path, fid.Uid);
                    }
                    o.U16(walked.Count);
                    foreach (var a in walked) Qid(o, a);
                    break;
                }
                case Tgetattr:
                {
                    var fid = Get(i.U32());
                    var a = fid.Handle != null ? Stat(fid.Handle) : server.StatPath(fid.Path);
                    o.U64(0xFFF);   // P9_GETATTR_BASIC | P9_GETATTR_BTIME
                    Qid(o, a);
                    o.U32(a.IsLink ? 0xA1FFu : a.IsDir ? 0x41EDu : (a.Attributes & FILE_ATTRIBUTE_READONLY) != 0 ? 0x816Du : 0x81EDu);
                    o.U32(fid.Uid);
                    o.U32(fid.Uid);
                    o.U64(a.Links);
                    o.U64(0);
                    o.U64(a.Size);
                    o.U64(4096);
                    o.U64((a.Size + 511) / 512);
                    Time(o, a.Atime);
                    Time(o, a.Mtime);
                    Time(o, a.Ctime);
                    Time(o, a.Btime);
                    o.U64(0);
                    o.U64(0);
                    break;
                }
                case Tsetattr:
                {
                    var fid = Get(i.U32());
                    var valid = i.U32();
                    var mode = i.U32();
                    i.U32();
                    i.U32();
                    var size = i.U64();
                    var atime = FileTime(i.U64(), i.U64());
                    var mtime = FileTime(i.U64(), i.U64());
                    if ((valid & (SET_MODE | SET_SIZE | SET_ATIME | SET_MTIME)) == 0) break;   // chown and ctime: nothing NTFS keeps
                    Writable();
                    using var h = server.OpenPath(fid.Path, FILE_READ_ATTRIBUTES | FILE_WRITE_ATTRIBUTES, 0, follow: false);
                    var a = Stat(h);
                    if ((valid & SET_MODE) != 0 && !a.IsDir && !a.IsLink)
                    {
                        // Only the owner's write bit has an NTFS counterpart: the read-only attribute.
                        var attrs = a.Attributes & SettableAttributes;
                        attrs = (mode & 0x80) == 0 ? attrs | FILE_ATTRIBUTE_READONLY : attrs & ~FILE_ATTRIBUTE_READONLY;
                        if (attrs != (a.Attributes & SettableAttributes)) SetBasic(h, 0, 0, attrs == 0 ? FILE_ATTRIBUTE_NORMAL : attrs);
                    }
                    if ((valid & SET_SIZE) != 0)
                    {
                        if (a.IsDir) throw new P9Error(EISDIR);
                        if (size > long.MaxValue) throw new P9Error(EFBIG);
                        if (fid.Handle != null && fid.Writable) SetEof(fid.Handle, (long)size);
                        else
                        {
                            using var d = server.OpenPath(fid.Path, FILE_WRITE_DATA, FILE_NON_DIRECTORY_FILE, follow: false);
                            SetEof(d, (long)size);
                        }
                    }
                    if ((valid & (SET_ATIME | SET_MTIME)) != 0)
                    {
                        var now = DateTime.UtcNow.ToFileTimeUtc();
                        SetBasic(h,
                            (valid & SET_ATIME) == 0 ? 0 : (valid & SET_ATIME_SET) != 0 ? atime : now,
                            (valid & SET_MTIME) == 0 ? 0 : (valid & SET_MTIME_SET) != 0 ? mtime : now, 0);
                    }
                    break;
                }
                case Tlopen:
                {
                    var fid = Get(i.U32());
                    var flags = i.U32();
                    if (fid.Handle != null) throw new P9Error(EINVAL);
                    var write = (flags & O_ACCMODE) is O_WRONLY or O_RDWR;
                    if ((write || (flags & O_TRUNC) != 0) && server.ReadOnly) throw new P9Error(EROFS);
                    var a = server.StatPath(fid.Path);
                    if (a.IsLink) throw new P9Error(ELOOP);
                    SafeFileHandle h;
                    if (a.IsDir)
                    {
                        if (write) throw new P9Error(EISDIR);
                        h = server.OpenDir(fid.Path);
                    }
                    else
                    {
                        if ((flags & O_DIRECTORY) != 0) throw new P9Error(ENOTDIR);
                        // Other reparse points (OneDrive placeholders, dedup) are opened through.
                        h = server.OpenPath(fid.Path, DataAccess(flags), FILE_NON_DIRECTORY_FILE, follow: a.IsReparse);
                        if (write && (flags & O_TRUNC) != 0)
                            try { SetEof(h, 0); } catch { h.Dispose(); throw; }
                    }
                    fid.Handle = h;
                    fid.IsDir = a.IsDir;
                    fid.Writable = write;
                    fid.Append = (flags & O_APPEND) != 0;
                    Qid(o, a);
                    o.U32(0);
                    break;
                }
                case Tlcreate:
                {
                    var fid = Get(i.U32());
                    var name = i.Str();
                    var flags = i.U32();
                    i.U32();
                    i.U32();
                    Writable();
                    if (fid.Handle != null) throw new P9Error(EINVAL);
                    var path = Child(fid.Path, name);
                    var disposition = (flags & O_EXCL) != 0 ? FILE_CREATE : (flags & O_TRUNC) != 0 ? FILE_OVERWRITE_IF : FILE_OPEN_IF;
                    SafeFileHandle h;
                    using (var dir = server.OpenDir(fid.Path))
                        h = OpenAt(dir, Leaf(path), DataAccess(flags), disposition, FILE_NON_DIRECTORY_FILE | FILE_OPEN_REPARSE_POINT);
                    var a = Stat(h);
                    if (a.IsLink)
                    {
                        h.Dispose();
                        throw new P9Error(ELOOP);
                    }
                    fid.Path = path;
                    fid.Handle = h;
                    fid.IsDir = false;
                    fid.Writable = (flags & O_ACCMODE) is O_WRONLY or O_RDWR;
                    fid.Append = (flags & O_APPEND) != 0;
                    Qid(o, a);
                    o.U32(0);
                    break;
                }
                case Tread:
                {
                    var fid = Get(i.U32());
                    var offset = i.U64();
                    var count = Math.Min(i.U32(), msize - 11);
                    var h = fid.Handle ?? throw new P9Error(EBADF);
                    if (fid.IsDir) throw new P9Error(EISDIR);
                    if (offset > long.MaxValue) throw new P9Error(EINVAL);
                    var at = o.Length;
                    o.U32(0);
                    var n = RandomAccess.Read(h, o.Take((int)count), (long)offset);
                    o.Truncate(at + 4 + n);
                    o.Patch(at, (uint)n);
                    break;
                }
                case Twrite:
                {
                    var fid = Get(i.U32());
                    var offset = i.U64();
                    var count = i.U32();
                    var data = i.Bytes(count);
                    var h = fid.Handle ?? throw new P9Error(EBADF);
                    if (!fid.Writable) throw new P9Error(EBADF);
                    if (offset > long.MaxValue) throw new P9Error(EFBIG);
                    if (fid.Append)
                        lock (fid) RandomAccess.Write(h, data, RandomAccess.GetLength(h));
                    else RandomAccess.Write(h, data, (long)offset);
                    o.U32(count);
                    break;
                }
                case Treaddir:
                {
                    var fid = Get(i.U32());
                    var offset = i.U64();
                    var count = Math.Min(i.U32(), msize - 11);
                    var h = fid.Handle ?? throw new P9Error(EBADF);
                    if (!fid.IsDir) throw new P9Error(ENOTDIR);
                    lock (fid)
                    {
                        if (offset == 0 || fid.Listing == null) fid.Listing = List(fid, h);
                        var at = o.Length;
                        o.U32(0);
                        for (var k = offset; k < (ulong)fid.Listing.Count; k++)
                        {
                            var e = fid.Listing[(int)k];
                            if (o.Length - at - 4 + 24 + Encoding.UTF8.GetByteCount(e.Name) > count) break;
                            o.U8(e.QidType);
                            o.U32(0);
                            o.U64(e.QidPath);
                            o.U64(k + 1);
                            o.U8(e.DType);
                            o.Str(e.Name);
                        }
                        o.Patch(at, (uint)(o.Length - at - 4));
                    }
                    break;
                }
                case Tmkdir:
                {
                    var fid = Get(i.U32());
                    var name = i.Str();
                    Writable();
                    var path = Child(fid.Path, name);
                    using var dir = server.OpenDir(fid.Path);
                    using var h = OpenAt(dir, Leaf(path), FILE_LIST_DIRECTORY | FILE_READ_ATTRIBUTES, FILE_CREATE, FILE_DIRECTORY_FILE | FILE_OPEN_REPARSE_POINT);
                    Qid(o, Stat(h));
                    break;
                }
                case Tunlinkat:
                {
                    var fid = Get(i.U32());
                    var name = i.Str();
                    var flags = i.U32();
                    Writable();
                    using var dir = server.OpenDir(fid.Path);
                    Remove(dir, Leaf(Child(fid.Path, name)), (flags & AT_REMOVEDIR) != 0 ? 1 : 0);
                    break;
                }
                case Tremove:
                {
                    var id = i.U32();
                    var fid = Get(id);
                    try
                    {
                        Writable();
                        if (fid.Path.Length == 0) throw new P9Error(EBUSY);
                        using var dir = server.OpenDir(Parent(fid.Path));
                        Remove(dir, Leaf(fid.Path), 2);
                    }
                    finally
                    {
                        if (fids.TryRemove(id, out var f)) f.Close();
                    }
                    break;
                }
                case Trenameat:
                {
                    var oldDir = Get(i.U32());
                    var oldName = i.Str();
                    var newDir = Get(i.U32());
                    var newName = i.Str();
                    Writable();
                    server.Rename(Child(oldDir.Path, oldName), Child(newDir.Path, newName));
                    break;
                }
                case Trename:
                {
                    var fid = Get(i.U32());
                    var newDir = Get(i.U32());
                    var name = i.Str();
                    Writable();
                    server.Rename(fid.Path, Child(newDir.Path, name));
                    break;
                }
                case Tlink:
                {
                    var dirFid = Get(i.U32());
                    var fid = Get(i.U32());
                    var name = i.Str();
                    Writable();
                    var path = Child(dirFid.Path, name);
                    using var dir = server.OpenDir(dirFid.Path);
                    using var h = server.OpenPath(fid.Path, FILE_READ_ATTRIBUTES | FILE_WRITE_ATTRIBUTES, 0, follow: false);
                    if (Stat(h).IsDir) throw new P9Error(EPERM);
                    SetName(h, dir, Leaf(path), link: true);
                    break;
                }
                case Tsymlink:
                {
                    var fid = Get(i.U32());
                    var name = i.Str();
                    var target = i.StrBytes();
                    Writable();
                    if (target.Length == 0) throw new P9Error(ENOENT);
                    if (target.Length > 4095) throw new P9Error(ENAMETOOLONG);
                    var path = Child(fid.Path, name);
                    using var dir = server.OpenDir(fid.Path);
                    using var h = OpenAt(dir, Leaf(path), FILE_WRITE_DATA | FILE_WRITE_ATTRIBUTES | FILE_READ_ATTRIBUTES | DELETE, FILE_CREATE,
                        FILE_NON_DIRECTORY_FILE | FILE_OPEN_REPARSE_POINT);
                    try { SetLxSymlink(h, target); }
                    catch
                    {
                        try { Delete(h); } catch (P9Error) { }
                        throw;
                    }
                    Qid(o, Stat(h));
                    break;
                }
                case Treadlink:
                {
                    var fid = Get(i.U32());
                    using var h = server.OpenPath(fid.Path, FILE_READ_ATTRIBUTES, 0, follow: false);
                    o.StrBytes(server.ReadLink(h, fid.Path));
                    break;
                }
                case Tmknod:
                    Get(i.U32());
                    throw new P9Error(EPERM);
                case Tstatfs:
                {
                    Get(i.U32());
                    if (!GetDiskFreeSpaceExW(server.RootDos + "\\", out var avail, out var total, out var free)) throw LastError();
                    o.U32(0x01021997);   // V9FS_MAGIC
                    o.U32(4096);
                    o.U64(total / 4096);
                    o.U64(free / 4096);
                    o.U64(avail / 4096);
                    o.U64(total / 4096);
                    o.U64(free / 4096);
                    o.U64(server.RootVolume);
                    o.U32(255);
                    break;
                }
                case Tfsync:
                {
                    var fid = Get(i.U32());
                    if (fid.Handle != null && fid.Writable && !FlushFileBuffers(fid.Handle)) throw LastError();
                    break;
                }
                case Tlock:
                    Get(i.U32());
                    o.U8(0);   // P9_LOCK_SUCCESS
                    break;
                case Tgetlock:
                {
                    Get(i.U32());
                    i.U8();
                    var start = i.U64();
                    var length = i.U64();
                    var proc = i.U32();
                    var client = i.StrBytes();
                    o.U8(2);   // F_UNLCK: nothing conflicts
                    o.U64(start);
                    o.U64(length);
                    o.U32(proc);
                    o.StrBytes(client);
                    break;
                }
                case Tclunk:
                {
                    if (!fids.TryRemove(i.U32(), out var f)) throw new P9Error(EBADF);
                    f.Close();
                    break;
                }
                case Txattrwalk or Txattrcreate: throw new P9Error(EOPNOTSUPP);
                default: throw new P9Error(EOPNOTSUPP);
            }
        }

        unsafe List<Entry> List(Fid fid, SafeFileHandle h)
        {
            var self = Stat(h);
            var parent = fid.Path.Length == 0 ? self : server.StatPath(Parent(fid.Path));
            var list = new List<Entry>
            {
                new(".", QTDIR, server.QidPath(self.Id, self.Volume), DT_DIR),
                new("..", QTDIR, server.QidPath(parent.Id, parent.Volume), DT_DIR),
            };
            var buf = new byte[64 * 1024];
            fixed (byte* b = buf)
                for (var cls = FileIdBothDirectoryRestartInfo; ; cls = FileIdBothDirectoryInfo)
                {
                    if (!GetFileInformationByHandleEx(h, cls, b, (uint)buf.Length))
                    {
                        if (Marshal.GetLastPInvokeError() == 18) break;   // ERROR_NO_MORE_FILES
                        throw LastError();
                    }
                    for (var p = 0; ;)
                    {
                        // FILE_ID_BOTH_DIR_INFO: attributes at 56, name length 60, reparse tag 64, file ID 96, name 104.
                        var next = *(uint*)(b + p);
                        var attrs = *(uint*)(b + p + 56);
                        var name = new string((char*)(b + p + 104), 0, (int)(*(uint*)(b + p + 60) / 2));
                        if (name is not ("." or ".."))
                        {
                            var link = (attrs & FILE_ATTRIBUTE_REPARSE_POINT) != 0 && (*(uint*)(b + p + 64) & 0x20000000) != 0;
                            var dir = (attrs & FILE_ATTRIBUTE_DIRECTORY) != 0 && !link;
                            list.Add(new(FromHost(name), link ? QTSYMLINK : dir ? QTDIR : (byte)0, server.QidPath(*(ulong*)(b + p + 96), self.Volume),
                                link ? DT_LNK : dir ? DT_DIR : DT_REG));
                        }
                        if (next == 0) break;
                        p += (int)next;
                    }
                }
            return list;
        }
    }

    static uint DataAccess(uint flags) => FILE_READ_ATTRIBUTES | (flags & O_ACCMODE) switch
    {
        0 => FILE_READ_DATA,
        O_WRONLY => FILE_WRITE_DATA | FILE_APPEND_DATA | FILE_WRITE_ATTRIBUTES,
        O_RDWR => FILE_READ_DATA | FILE_WRITE_DATA | FILE_APPEND_DATA | FILE_WRITE_ATTRIBUTES,
        _ => 0,
    };

    static void Time(Out o, long fileTime)
    {
        var t = fileTime - 116444736000000000L;
        var sec = t / 10_000_000;
        var rem = t % 10_000_000;
        if (rem < 0)
        {
            sec--;
            rem += 10_000_000;
        }
        o.U64((ulong)sec);
        o.U64((ulong)(rem * 100));
    }

    static long FileTime(ulong sec, ulong nsec) =>
        (long)Math.Min(sec, 900_000_000_000UL) * 10_000_000 + (long)(nsec % 1_000_000_000) / 100 + 116444736000000000L;

    // Unlinks `name` in `dir`: kind 0 a non-directory, 1 a directory, 2 either.
    static void Remove(SafeFileHandle dir, string name, int kind)
    {
        using var h = OpenAt(dir, name, DELETE | FILE_READ_ATTRIBUTES, FILE_OPEN, FILE_OPEN_REPARSE_POINT);
        var a = Stat(h);
        if (kind == 1 && !a.IsDir) throw new P9Error(ENOTDIR);
        if (kind == 0 && a.IsDir) throw new P9Error(EISDIR);
        Delete(h);
    }

    // POSIX semantics where NTFS has them: the name goes now, open handles keep working.
    static unsafe void Delete(SafeFileHandle h)
    {
        IO_STATUS_BLOCK io;
        var status = 0;
        foreach (var flags in (ReadOnlySpan<uint>)[0x13, 0x3])   // DELETE | POSIX_SEMANTICS (| IGNORE_READONLY_ATTRIBUTE)
        {
            var f = flags;
            status = NtSetInformationFile(h, &io, &f, 4, FileDispositionInformationEx);
            if (!Unsupported(status)) break;
        }
        if (Unsupported(status))
        {
            byte delete = 1;
            status = NtSetInformationFile(h, &io, &delete, 1, FileDispositionInformation);
        }
        if (status < 0) throw NtError(status);
    }

    static bool Unsupported(int status) => (uint)status is 0xC000000D or 0xC00000BB or 0xC0000003;

    // Renames (or hard-links) the file behind `h` to `name` in `dir`.
    static unsafe void SetName(SafeFileHandle h, SafeFileHandle dir, string name, bool link)
    {
        var at = 2 * IntPtr.Size + 4;   // FILE_RENAME_INFORMATION / FILE_LINK_INFORMATION: flags, root, length, name
        var size = at + name.Length * 2;
        var buf = new byte[size + 2];
        fixed (byte* b = buf)
        fixed (char* n = name)
        {
            *(IntPtr*)(b + IntPtr.Size) = dir.DangerousGetHandle();
            *(uint*)(b + 2 * IntPtr.Size) = (uint)(name.Length * 2);
            Buffer.MemoryCopy(n, b + at, name.Length * 2, name.Length * 2);
            IO_STATUS_BLOCK io;
            int status;
            if (link) status = NtSetInformationFile(h, &io, b, (uint)size, FileLinkInformation);
            else
            {
                *(uint*)b = 0x43;   // REPLACE_IF_EXISTS | POSIX_SEMANTICS | IGNORE_READONLY_ATTRIBUTE
                status = NtSetInformationFile(h, &io, b, (uint)size, FileRenameInformationEx);
                if (Unsupported(status))
                {
                    *(uint*)b = 1;
                    status = NtSetInformationFile(h, &io, b, (uint)size, FileRenameInformation);
                }
            }
            if (status < 0) throw NtError(status);
        }
    }

    static unsafe void SetLxSymlink(SafeFileHandle h, byte[] target)
    {
        var buf = new byte[12 + target.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(buf, IO_REPARSE_TAG_LX_SYMLINK);
        BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(4), (ushort)(4 + target.Length));
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(8), 2);   // version
        target.CopyTo(buf, 12);
        fixed (byte* b = buf)
            if (!DeviceIoControl(h, FSCTL_SET_REPARSE_POINT, b, (uint)buf.Length, null, 0, out _, IntPtr.Zero)) throw LastError();
    }

    static unsafe void SetBasic(SafeFileHandle h, long atime, long mtime, uint attrs)
    {
        var info = stackalloc long[5];   // FILE_BASIC_INFORMATION: creation, access, write, change, attributes; 0 = unchanged
        info[0] = 0;
        info[1] = atime;
        info[2] = mtime;
        info[3] = 0;
        info[4] = attrs;
        IO_STATUS_BLOCK io;
        var status = NtSetInformationFile(h, &io, info, 40, FileBasicInformation);
        if (status < 0) throw NtError(status);
    }

    static unsafe void SetEof(SafeFileHandle h, long size)
    {
        IO_STATUS_BLOCK io;
        var status = NtSetInformationFile(h, &io, &size, 8, FileEndOfFileInformation);
        if (status < 0) throw NtError(status);
    }

    static unsafe Attr Stat(SafeFileHandle h)
    {
        if (!GetFileInformationByHandle(h, out var info)) throw LastError();
        FILE_BASIC_INFO basic;
        if (!GetFileInformationByHandleEx(h, FileBasicInfo, &basic, (uint)sizeof(FILE_BASIC_INFO))) throw LastError();
        var a = new Attr
        {
            Id = (ulong)info.IndexHigh << 32 | info.IndexLow,
            Size = (ulong)info.SizeHigh << 32 | info.SizeLow,
            Volume = info.VolumeSerial,
            Attributes = info.Attributes,
            Links = info.Links,
            Atime = basic.LastAccessTime,
            Mtime = basic.LastWriteTime,
            Ctime = basic.ChangeTime,
            Btime = basic.CreationTime,
        };
        if (a.IsReparse)
        {
            FILE_ATTRIBUTE_TAG_INFO tag;
            if (GetFileInformationByHandleEx(h, FileAttributeTagInfo, &tag, (uint)sizeof(FILE_ATTRIBUTE_TAG_INFO))) a.Tag = tag.ReparseTag;
        }
        return a;
    }

    static unsafe SafeFileHandle OpenAt(SafeFileHandle dir, string name, uint access, uint disposition, uint options)
    {
        if (name.Length > 16000) throw new P9Error(ENAMETOOLONG);
        fixed (char* p = name)
        {
            var us = new UNICODE_STRING { Length = (ushort)(name.Length * 2), MaximumLength = (ushort)(name.Length * 2), Buffer = (IntPtr)p };
            var oa = new OBJECT_ATTRIBUTES
            {
                Length = (uint)sizeof(OBJECT_ATTRIBUTES),
                RootDirectory = dir.DangerousGetHandle(),
                ObjectName = &us,
                Attributes = 0x40,   // OBJ_CASE_INSENSITIVE, as Win32 opens are
            };
            IO_STATUS_BLOCK io;
            var status = NtCreateFile(out var handle, access | SYNCHRONIZE, &oa, &io, null, FILE_ATTRIBUTE_NORMAL, 7, disposition,
                options | FILE_SYNCHRONOUS_IO_NONALERT, null, 0);
            if (status < 0) throw NtError(status);
            return new SafeFileHandle(handle, true);
        }
    }

    static unsafe string FinalPath(SafeFileHandle h, uint flags)
    {
        var buf = new char[1024];
        while (true)
        {
            uint n;
            fixed (char* p = buf) n = GetFinalPathNameByHandleW(h, p, (uint)buf.Length, flags);
            if (n == 0) return null;
            if (n < buf.Length) return new string(buf, 0, (int)n).TrimEnd('\\');
            buf = new char[n + 1];
        }
    }

    // ---- wire encoding ----

    sealed class Out
    {
        byte[] b = new byte[256];
        public int Length { get; private set; }

        public Out(byte type, ushort tag)
        {
            Length = 4;
            U8(type);
            U16(tag);
        }

        public Span<byte> Take(int n)
        {
            if (Length + n > b.Length) Array.Resize(ref b, Math.Max(b.Length * 2, Length + n));
            var s = b.AsSpan(Length, n);
            Length += n;
            return s;
        }

        public void U8(byte v) => Take(1)[0] = v;
        public void U16(int v) => BinaryPrimitives.WriteUInt16LittleEndian(Take(2), (ushort)v);
        public void U32(uint v) => BinaryPrimitives.WriteUInt32LittleEndian(Take(4), v);
        public void U64(ulong v) => BinaryPrimitives.WriteUInt64LittleEndian(Take(8), v);
        public void Str(string s) => StrBytes(Encoding.UTF8.GetBytes(s));

        public void StrBytes(ReadOnlySpan<byte> s)
        {
            U16(s.Length);
            s.CopyTo(Take(s.Length));
        }

        public void Bytes(ReadOnlySpan<byte> s) => s.CopyTo(Take(s.Length));
        public void Truncate(int length) => Length = length;
        public void Patch(int at, uint v) => BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(at), v);

        public ReadOnlySpan<byte> Finish()
        {
            Patch(0, (uint)Length);
            return b.AsSpan(0, Length);
        }
    }

    sealed class In(byte[] b, int start)
    {
        int p = start;

        public int Remaining => b.Length - p;

        ReadOnlySpan<byte> Need(long n)
        {
            if (n < 0 || n > b.Length - p) throw new P9Error(EBADMSG);
            var s = b.AsSpan(p, (int)n);
            p += (int)n;
            return s;
        }

        public byte U8() => Need(1)[0];
        public ushort U16() => BinaryPrimitives.ReadUInt16LittleEndian(Need(2));
        public uint U32() => BinaryPrimitives.ReadUInt32LittleEndian(Need(4));
        public ulong U64() => BinaryPrimitives.ReadUInt64LittleEndian(Need(8));
        public string Str() => Encoding.UTF8.GetString(Need(U16()));
        public byte[] StrBytes() => Need(U16()).ToArray();
        public ReadOnlySpan<byte> Bytes(uint n) => Need(n);
    }

    // ---- self-test: the server against an in-process client ----

    // A one-way in-memory byte pipe; two make the duplex stream a connection would be.
    sealed class Pipe
    {
        readonly Queue<byte[]> chunks = new();
        int offset;
        bool closed;

        public void Write(ReadOnlySpan<byte> data)
        {
            var copy = data.ToArray();
            lock (chunks)
            {
                if (closed) throw new IOException("pipe closed");
                chunks.Enqueue(copy);
                Monitor.PulseAll(chunks);
            }
        }

        public int Read(Span<byte> into)
        {
            lock (chunks)
            {
                while (chunks.Count == 0 && !closed) Monitor.Wait(chunks);
                if (chunks.Count == 0) return 0;
                var c = chunks.Peek();
                var n = Math.Min(into.Length, c.Length - offset);
                c.AsSpan(offset, n).CopyTo(into);
                offset += n;
                if (offset == c.Length)
                {
                    chunks.Dequeue();
                    offset = 0;
                }
                return n;
            }
        }

        public void Close()
        {
            lock (chunks)
            {
                closed = true;
                Monitor.PulseAll(chunks);
            }
        }
    }

    sealed class PipeStream(Pipe input, Pipe output) : Stream
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => input.Read(buffer.AsSpan(offset, count));
        public override int Read(Span<byte> buffer) => input.Read(buffer);
        public override void Write(byte[] buffer, int offset, int count) => output.Write(buffer.AsSpan(offset, count));
        public override void Write(ReadOnlySpan<byte> buffer) => output.Write(buffer);
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            output.Close();
            input.Close();
            base.Dispose(disposing);
        }
    }

    sealed class Client
    {
        readonly Stream s;
        readonly ConcurrentDictionary<ushort, TaskCompletionSource<byte[]>> pending = new();
        int nextTag;

        public Client(Stream s)
        {
            this.s = s;
            new Thread(ReadLoop) { IsBackground = true }.Start();
        }

        void ReadLoop()
        {
            var head = new byte[4];
            try
            {
                while (ReadFully(s, head))
                {
                    var body = new byte[BinaryPrimitives.ReadUInt32LittleEndian(head) - 4];
                    if (!ReadFully(s, body)) break;
                    if (pending.TryRemove(BinaryPrimitives.ReadUInt16LittleEndian(body.AsSpan(1)), out var t)) t.SetResult(body);
                }
            }
            catch (Exception e) when (e is IOException or ObjectDisposedException) { }
            foreach (var t in pending.Values) t.TrySetException(new IOException("connection closed"));
        }

        public Task<byte[]> Send(byte type, Action<Out> fill, ushort tag = 0)
        {
            if (tag == 0) tag = (ushort)(Interlocked.Increment(ref nextTag) & 0x7FFF);
            var o = new Out(type, tag);
            fill(o);
            var t = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            pending[tag] = t;
            lock (s) s.Write(o.Finish());
            return t.Task;
        }

        public static In Result(byte type, Task<byte[]> reply)
        {
            if (!reply.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("no reply in 10 seconds");
            var body = reply.Result;
            if (body[0] == Rlerror) throw new P9Error(BinaryPrimitives.ReadUInt32LittleEndian(body.AsSpan(3)));
            if (body[0] != type + 1) throw new InvalidOperationException($"reply type {body[0]} to message {type}");
            return new In(body, 3);
        }

        public In Call(byte type, Action<Out> fill, ushort tag = 0) => Result(type, Send(type, fill, tag));

        public uint Errno(byte type, Action<Out> fill)
        {
            try
            {
                Call(type, fill);
                return 0;
            }
            catch (P9Error e) { return e.Errno; }
        }
    }

    static int SelfTest(string dir, IPEndPoint endPoint, string socketPath)
    {
        var top = Path.GetFullPath(dir);
        var failures = 0;
        var transports = new List<string>();
        if (endPoint != null) transports.Add("tcp");
        if (socketPath != null) transports.Add("unix");
        if (transports.Count == 0) transports.Add("memory");
        foreach (var transport in transports)
        {
            Console.WriteLine($"-- over {(transport == "memory" ? "an in-memory stream pair" : transport == "tcp" ? $"TCP {endPoint}" : $"AF_UNIX {socketPath}")}");
            failures += Suite(top, server =>
            {
                switch (transport)
                {
                    case "memory":
                    {
                        Pipe up = new(), down = new();
                        new Thread(() => server.Serve(new PipeStream(up, down), "self-test")) { IsBackground = true }.Start();
                        return new PipeStream(down, up);
                    }
                    default:
                    {
                        EndPoint ep = transport == "tcp" ? endPoint : new UnixDomainSocketEndPoint(socketPath);
                        if (transport == "unix" && File.Exists(socketPath)) File.Delete(socketPath);
                        var listener = new Socket(ep.AddressFamily, SocketType.Stream, transport == "tcp" ? ProtocolType.Tcp : ProtocolType.Unspecified);
                        listener.Bind(ep);
                        listener.Listen(1);
                        new Thread(() =>
                        {
                            var conn = listener.Accept();
                            listener.Dispose();
                            server.Serve(new NetworkStream(conn, ownsSocket: true), transport);
                        }) { IsBackground = true }.Start();
                        var client = new Socket(ep.AddressFamily, SocketType.Stream, transport == "tcp" ? ProtocolType.Tcp : ProtocolType.Unspecified);
                        client.Connect(listener.LocalEndPoint);
                        return new NetworkStream(client, ownsSocket: true);
                    }
                }
            });
        }
        Console.WriteLine(failures == 0 ? "self-test passed" : $"self-test failed: {failures} check(s)");
        return failures == 0 ? 0 : 1;
    }

    static int Suite(string top, Func<Server, Stream> connect)
    {
        var share = Path.Combine(top, "share");
        var outside = Path.Combine(top, "outside");
        foreach (var d in new[] { share, outside })
            if (Directory.Exists(d)) DeleteTree(d);
        Directory.CreateDirectory(Path.Combine(share, "hostdir"));
        Directory.CreateDirectory(outside);
        File.WriteAllText(Path.Combine(outside, "secret.txt"), "outside the share\n");

        var failures = 0;
        void Check(string what, Func<bool> test)
        {
            string detail = null;
            bool ok;
            try { ok = test(); }
            catch (Exception e)
            {
                ok = false;
                detail = e is P9Error p ? $"errno {p.Errno}" : e.Message;
            }
            Console.WriteLine(ok ? $"ok: {what}" : $"FAIL: {what}{(detail != null ? $" ({detail})" : "")}");
            if (!ok) failures++;
        }

        var server = new Server(share, false, LogError);
        MakeJunction(Path.Combine(share, "escape"), outside);
        MakeJunction(Path.Combine(share, "inlink"), server.RootDos + "\\hostdir");
        var stream = connect(server);
        var c = new Client(stream);

        static Action<Out> Arg(uint fid) => o => o.U32(fid);
        In Walk(uint fid, uint newFid, params string[] names) => c.Call(Twalk, o =>
        {
            o.U32(fid);
            o.U32(newFid);
            o.U16(names.Length);
            foreach (var n in names) o.Str(n);
        });
        (byte Type, ulong Path) Qid(In r)
        {
            var t = r.U8();
            r.U32();
            return (t, r.U64());
        }
        (uint Mode, uint Uid, ulong Links, ulong Size, ulong Mtime, ulong QidPath) Getattr(uint fid)
        {
            var r = c.Call(Tgetattr, o => { o.U32(fid); o.U64(0x7FF); });
            r.U64();
            var q = Qid(r);
            var mode = r.U32();
            var uid = r.U32();
            r.U32();
            var links = r.U64();
            r.U64();
            var size = r.U64();
            r.U64();
            r.U64();
            r.U64();
            r.U64();
            return (mode, uid, links, size, r.U64(), q.Path);
        }
        In Lcreate(uint fid, string name, uint flags) => c.Call(Tlcreate, o => { o.U32(fid); o.Str(name); o.U32(flags); o.U32(0x1A4); o.U32(0); });
        uint Write(uint fid, string text)
        {
            var data = Encoding.UTF8.GetBytes(text);
            return c.Call(Twrite, o => { o.U32(fid); o.U64(0); o.U32((uint)data.Length); o.Bytes(data); }).U32();
        }
        string Read(uint fid)
        {
            var r = c.Call(Tread, o => { o.U32(fid); o.U64(0); o.U32(4096); });
            return Encoding.UTF8.GetString(r.Bytes(r.U32()));
        }
        List<string> Readdir(uint dirFid, uint fid)
        {
            Walk(dirFid, fid);
            c.Call(Tlopen, o => { o.U32(fid); o.U32(O_DIRECTORY); });
            var names = new List<string>();
            for (ulong offset = 0; ;)
            {
                var r = c.Call(Treaddir, o => { o.U32(fid); o.U64(offset); o.U32(200); });   // small, to page
                var count = r.U32();
                if (count == 0) break;
                var data = new In(r.Bytes(count).ToArray(), 0);
                while (data.Remaining > 0)
                {
                    Qid(data);
                    offset = data.U64();
                    data.U8();
                    names.Add(data.Str());
                }
            }
            c.Call(Tclunk, Arg(fid));
            return names;
        }
        uint Unlinkat(uint dirFid, string name, uint flags) => c.Errno(Tunlinkat, o => { o.U32(dirFid); o.Str(name); o.U32(flags); });
        uint Renameat(uint oldDir, string oldName, uint newDir, string newName) =>
            c.Errno(Trenameat, o => { o.U32(oldDir); o.Str(oldName); o.U32(newDir); o.Str(newName); });
        uint Errno(Action a)
        {
            try
            {
                a();
                return 0;
            }
            catch (P9Error e) { return e.Errno; }
        }

        (byte Type, ulong Path) root = default;
        Check("version negotiates 9P2000.L", () =>
        {
            var r = c.Call(Tversion, o => { o.U32(65536 + 24); o.Str("9P2000.L"); }, 0xFFFF);
            return r.U32() == 65536 + 24 && r.Str() == "9P2000.L";
        });
        Check("attach to the shared dir", () =>
        {
            root = Qid(c.Call(Tattach, o => { o.U32(0); o.U32(NOFID); o.Str("tester"); o.Str("/"); o.U32(1000); }));
            return root.Type == QTDIR;
        });
        Check("walk to a missing name fails with ENOENT", () => Errno(() => Walk(0, 1, "missing")) == ENOENT);
        Check("lcreate makes a file", () =>
        {
            Walk(0, 1);
            return Qid(Lcreate(1, "hello.txt", O_RDWR | O_CREAT)).Type == 0;
        });
        Check("write", () => Write(1, "hello, 9p\n") == 10);
        Check("read back", () => Read(1) == "hello, 9p\n");
        Check("the host sees the file", () =>
        {
            using var f = new FileStream(Path.Combine(share, "hello.txt"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return new StreamReader(f).ReadToEnd() == "hello, 9p\n";
        });
        Check("getattr: a regular file of 10 bytes owned by the attaching uid", () =>
        {
            var a = Getattr(1);
            return (a.Mode & 0xF000) == 0x8000 && a.Size == 10 && a.Uid == 1000;
        });
        Check("fsync, lock and getlock succeed", () =>
        {
            c.Call(Tfsync, o => { o.U32(1); o.U32(0); });
            var locked = c.Call(Tlock, o => { o.U32(1); o.U8(1); o.U32(0); o.U64(0); o.U64(0); o.U32(1); o.Str("test"); }).U8() == 0;
            return locked && c.Call(Tgetlock, o => { o.U32(1); o.U8(1); o.U64(0); o.U64(0); o.U32(1); o.Str("test"); }).U8() == 2;
        });
        c.Call(Tclunk, Arg(1));
        Check("mkdir", () => Qid(c.Call(Tmkdir, o => { o.U32(0); o.Str("sub"); o.U32(0x1ED); o.U32(0); })).Type == QTDIR);
        Check("readdir lists ., .., files, directories and links", () =>
        {
            var names = Readdir(0, 2);
            return new[] { ".", "..", "hello.txt", "sub", "hostdir", "escape", "inlink" }.All(names.Contains) && names.Count == 7;
        });
        Check("concurrent requests on one connection", () =>
        {
            var replies = Enumerable.Range(0, 32).Select(_ => c.Send(Tgetattr, o => { o.U32(0); o.U64(0x7FF); })).ToList();
            return replies.All(r => Client.Result(Tgetattr, r) != null);
        });
        Check("renameat into a subdirectory", () =>
        {
            Walk(0, 3, "sub");
            return Renameat(0, "hello.txt", 3, "moved.txt") == 0 && File.Exists(Path.Combine(share, "sub", "moved.txt"))
                && !File.Exists(Path.Combine(share, "hello.txt"));
        });
        Check("rename moves a fid's file back, and the fid follows", () =>
        {
            Walk(0, 4, "sub", "moved.txt");
            c.Call(Trename, o => { o.U32(4); o.U32(0); o.Str("back.txt"); });
            Walk(0, 5, "back.txt");
            c.Call(Tclunk, Arg(5));
            c.Call(Tlopen, o => { o.U32(4); o.U32(0); });
            return Read(4) == "hello, 9p\n";
        });
        c.Call(Tclunk, Arg(4));
        Check("fids inside a renamed directory follow it", () =>
        {
            Walk(3, 5);
            Lcreate(5, "inner.txt", O_WRONLY | O_CREAT);
            c.Call(Tclunk, Arg(5));
            Walk(3, 6, "inner.txt");
            return Renameat(0, "sub", 0, "sub2") == 0 && Getattr(6).Size == 0 && Directory.Exists(Path.Combine(share, "sub2"));
        });
        c.Call(Tclunk, Arg(6));
        Check("symlink and readlink round-trip the guest's target", () =>
        {
            var q = Qid(c.Call(Tsymlink, o => { o.U32(0); o.Str("link"); o.Str("sub2/inner.txt"); o.U32(0); }));
            Walk(0, 7, "link");
            var target = c.Call(Treadlink, Arg(7)).Str();
            return q.Type == QTSYMLINK && target == "sub2/inner.txt" && (Getattr(7).Mode & 0xF000) == 0xA000;
        });
        Check("lopen on a symlink fails with ELOOP", () => c.Errno(Tlopen, o => { o.U32(7); o.U32(0); }) == ELOOP);
        c.Call(Tclunk, Arg(7));
        Check("readlink of a junction inside the share is relative", () =>
        {
            Walk(0, 7, "inlink");
            var target = c.Call(Treadlink, Arg(7)).Str();
            c.Call(Tclunk, Arg(7));
            return target == "hostdir";
        });
        Check("walking through a junction out of the share stops there", () =>
        {
            var r = Walk(0, 8, "escape", "secret.txt");
            return r.U16() == 1 && c.Errno(Tgetattr, o => { o.U32(8); o.U64(0x7FF); }) == EBADF;
        });
        Check("readlink of a junction out of the share is refused", () =>
        {
            Walk(0, 8, "escape");
            return c.Errno(Treadlink, Arg(8)) == EACCES;
        });
        Check("creating through a junction out of the share is refused", () =>
            c.Errno(Tlcreate, o => { o.U32(8); o.Str("planted.txt"); o.U32(O_WRONLY | O_CREAT); o.U32(0x1A4); o.U32(0); }) == EACCES
            && !File.Exists(Path.Combine(outside, "planted.txt")));
        c.Call(Tclunk, Arg(8));
        Check(".. stops at the shared dir", () =>
        {
            var r = Walk(0, 9, "..", "..");
            r.U16();
            Qid(r);
            var up = Qid(r);
            c.Call(Tclunk, Arg(9));
            return up.Path == root.Path;
        });
        Check("names with / are refused", () => Errno(() => Walk(0, 9, "a/b")) == EINVAL);
        Check("attaching above the shared dir is refused", () =>
            c.Errno(Tattach, o => { o.U32(9); o.U32(NOFID); o.Str("tester"); o.Str("/../outside"); o.U32(1000); }) == EACCES);
        Check("names with NTFS-reserved characters round-trip", () =>
        {
            Walk(0, 9);
            Lcreate(9, "a:b*c?.txt", O_WRONLY | O_CREAT);
            c.Call(Tclunk, Arg(9));
            return Readdir(0, 9).Contains("a:b*c?.txt") && File.Exists(Path.Combine(share, "a\uF03Ab\uF02Ac\uF03F.txt"));
        });
        Check("link makes a hard link", () =>
        {
            Walk(0, 10, "back.txt");
            c.Call(Tlink, o => { o.U32(0); o.U32(10); o.Str("hard.txt"); });
            return Getattr(10).Links == 2 && File.ReadAllText(Path.Combine(share, "hard.txt")) == "hello, 9p\n";
        });
        Check("setattr truncates and sets mtime", () =>
        {
            c.Call(Tsetattr, o =>
            {
                o.U32(10);
                o.U32(SET_SIZE | SET_MTIME | SET_MTIME_SET);
                o.U32(0); o.U32(0); o.U32(0);
                o.U64(3);
                o.U64(0); o.U64(0);
                o.U64(1_000_000_000); o.U64(0);
            });
            var a = Getattr(10);
            return a.Size == 3 && a.Mtime == 1_000_000_000;
        });
        Check("chmod a-w sets the read-only attribute and back", () =>
        {
            void Chmod(uint mode) => c.Call(Tsetattr, o => { o.U32(10); o.U32(SET_MODE); o.U32(mode); o.U32(0); o.U32(0); o.U64(0); o.U64(0); o.U64(0); o.U64(0); o.U64(0); });
            Chmod(0x124);
            var ro = File.GetAttributes(Path.Combine(share, "back.txt")).HasFlag(FileAttributes.ReadOnly) && (Getattr(10).Mode & 0x80) == 0;
            Chmod(0x1A4);
            return ro && !File.GetAttributes(Path.Combine(share, "back.txt")).HasFlag(FileAttributes.ReadOnly);
        });
        Check("an unlinked file stays readable through its open fid", () =>
        {
            Walk(0, 11);
            Lcreate(11, "open.txt", O_RDWR | O_CREAT);
            Write(11, "still here");
            return Unlinkat(0, "open.txt", 0) == 0 && !File.Exists(Path.Combine(share, "open.txt")) && Read(11) == "still here";
        });
        c.Call(Tclunk, Arg(11));
        Check("unlinkat refuses the wrong kind and non-empty directories", () =>
            Unlinkat(0, "hostdir", 0) == EISDIR && Unlinkat(0, "hard.txt", AT_REMOVEDIR) == ENOTDIR && Unlinkat(0, "sub2", AT_REMOVEDIR) == ENOTEMPTY);
        Check("unlinkat removes files, links and empty directories", () =>
            Unlinkat(3, "inner.txt", 0) == 0 && Unlinkat(0, "sub2", AT_REMOVEDIR) == 0 && Unlinkat(0, "link", 0) == 0
            && Unlinkat(0, "hard.txt", 0) == 0 && !Directory.Exists(Path.Combine(share, "sub2")));
        Check("remove removes and clunks", () =>
        {
            c.Call(Tremove, Arg(10));
            return !File.Exists(Path.Combine(share, "back.txt")) && c.Errno(Tclunk, Arg(10)) == EBADF;
        });
        Check("statfs", () =>
        {
            var r = c.Call(Tstatfs, Arg(0));
            return r.U32() == 0x01021997 && r.U32() == 4096 && r.U64() > 0;
        });
        Check("mknod is refused with EPERM, xattrs with EOPNOTSUPP", () =>
            c.Errno(Tmknod, o => { o.U32(0); o.Str("fifo"); o.U32(0x11A4); o.U32(0); o.U32(0); o.U32(0); }) == EPERM
            && c.Errno(Txattrwalk, o => { o.U32(0); o.U32(12); o.Str("user.test"); }) == EOPNOTSUPP);
        Check("flush", () => c.Call(Tflush, o => o.U16(1234)) != null);
        c.Call(Tclunk, Arg(3));
        stream.Dispose();

        var ro = new Server(share, true, LogError);
        var roStream = connect(ro);
        c = new Client(roStream);
        File.WriteAllText(Path.Combine(share, "ro.txt"), "read me");
        Check("--readonly: reads work, writes fail with EROFS", () =>
        {
            c.Call(Tversion, o => { o.U32(8192); o.Str("9P2000.L"); }, 0xFFFF);
            c.Call(Tattach, o => { o.U32(0); o.U32(NOFID); o.Str("tester"); o.Str(""); o.U32(1000); });
            Walk(0, 1, "ro.txt");
            var refused = c.Errno(Tlopen, o => { o.U32(1); o.U32(O_WRONLY); }) == EROFS;
            c.Call(Tlopen, o => { o.U32(1); o.U32(0); });
            Walk(0, 2);
            return refused && Read(1) == "read me" && c.Errno(Tlcreate, o => { o.U32(2); o.Str("new.txt"); o.U32(O_WRONLY | O_CREAT); o.U32(0x1A4); o.U32(0); }) == EROFS
                && c.Errno(Tmkdir, o => { o.U32(0); o.Str("d"); o.U32(0x1ED); o.U32(0); }) == EROFS;
        });
        roStream.Dispose();
        Thread.Sleep(100);
        ro.Dispose();
        server.Dispose();
        if (failures == 0)
            foreach (var d in new[] { share, outside })
                try { DeleteTree(d); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        return failures;
    }

    // Deletes a self-test tree, removing junctions and symlinks themselves rather than what they point to.
    static void DeleteTree(string dir)
    {
        foreach (var e in new DirectoryInfo(dir).EnumerateFileSystemInfos())
        {
            if (e.Attributes.HasFlag(FileAttributes.ReadOnly)) e.Attributes &= ~FileAttributes.ReadOnly;
            if (e is DirectoryInfo d && !d.Attributes.HasFlag(FileAttributes.ReparsePoint)) DeleteTree(d.FullName);
            else e.Delete();
        }
        Directory.Delete(dir);
    }

    // A junction (mount point reparse point) at `path` to the directory `target`.
    static unsafe void MakeJunction(string path, string target)
    {
        Directory.CreateDirectory(path);
        var subst = Encoding.Unicode.GetBytes(@"\??\" + target);
        var print = Encoding.Unicode.GetBytes(target);
        var buf = new byte[16 + subst.Length + 2 + print.Length + 2];
        BinaryPrimitives.WriteUInt32LittleEndian(buf, IO_REPARSE_TAG_MOUNT_POINT);
        BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(4), (ushort)(buf.Length - 8));
        BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(8), 0);
        BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(10), (ushort)subst.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(12), (ushort)(subst.Length + 2));
        BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(14), (ushort)print.Length);
        subst.CopyTo(buf, 16);
        print.CopyTo(buf, 16 + subst.Length + 2);
        using var h = CreateFileW(path, FILE_WRITE_DATA | FILE_WRITE_ATTRIBUTES, 7, IntPtr.Zero, 3, 0x02000000 | 0x00200000, IntPtr.Zero);
        if (h.IsInvalid) throw new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError(), $"can't open {path}");
        fixed (byte* b = buf)
            if (!DeviceIoControl(h, FSCTL_SET_REPARSE_POINT, b, (uint)buf.Length, null, 0, out _, IntPtr.Zero))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError(), $"can't make junction {path}");
    }

    // ---- Windows ----

    const uint FILE_READ_DATA = 0x1, FILE_LIST_DIRECTORY = 0x1, FILE_WRITE_DATA = 0x2, FILE_APPEND_DATA = 0x4, FILE_TRAVERSE = 0x20,
        FILE_READ_ATTRIBUTES = 0x80, FILE_WRITE_ATTRIBUTES = 0x100, DELETE = 0x10000, SYNCHRONIZE = 0x100000;
    const uint FILE_OPEN = 1, FILE_CREATE = 2, FILE_OPEN_IF = 3, FILE_OVERWRITE_IF = 5;
    const uint FILE_DIRECTORY_FILE = 0x1, FILE_SYNCHRONOUS_IO_NONALERT = 0x20, FILE_NON_DIRECTORY_FILE = 0x40, FILE_OPEN_REPARSE_POINT = 0x200000;
    const uint FILE_ATTRIBUTE_READONLY = 0x1, FILE_ATTRIBUTE_DIRECTORY = 0x10, FILE_ATTRIBUTE_NORMAL = 0x80, FILE_ATTRIBUTE_REPARSE_POINT = 0x400;
    const uint SettableAttributes = 0x1 | 0x2 | 0x4 | 0x20 | 0x100 | 0x1000 | 0x2000;
    const uint IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003, IO_REPARSE_TAG_SYMLINK = 0xA000000C, IO_REPARSE_TAG_LX_SYMLINK = 0xA000001D;
    const uint FSCTL_SET_REPARSE_POINT = 0x000900A4, FSCTL_GET_REPARSE_POINT = 0x000900A8;
    const uint VOLUME_NAME_DOS = 0, VOLUME_NAME_NT = 2;
    const int FileBasicInformation = 4, FileRenameInformation = 10, FileLinkInformation = 11, FileDispositionInformation = 13,
        FileEndOfFileInformation = 20, FileDispositionInformationEx = 64, FileRenameInformationEx = 65;
    const int FileBasicInfo = 0, FileAttributeTagInfo = 9, FileIdBothDirectoryInfo = 10, FileIdBothDirectoryRestartInfo = 11;

    [StructLayout(LayoutKind.Sequential)]
    struct UNICODE_STRING
    {
        public ushort Length, MaximumLength;
        public IntPtr Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct OBJECT_ATTRIBUTES
    {
        public uint Length;
        public IntPtr RootDirectory;
        public UNICODE_STRING* ObjectName;
        public uint Attributes;
        public IntPtr SecurityDescriptor, SecurityQualityOfService;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct IO_STATUS_BLOCK
    {
        public IntPtr Status, Information;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct BY_HANDLE_FILE_INFORMATION
    {
        public uint Attributes;
        public long CreationTime, LastAccessTime, LastWriteTime;
        public uint VolumeSerial, SizeHigh, SizeLow, Links, IndexHigh, IndexLow;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct FILE_BASIC_INFO
    {
        public long CreationTime, LastAccessTime, LastWriteTime, ChangeTime;
        public uint FileAttributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct FILE_ATTRIBUTE_TAG_INFO
    {
        public uint FileAttributes, ReparseTag;
    }

    [DllImport("ntdll.dll")]
    static extern unsafe int NtCreateFile(out IntPtr handle, uint access, OBJECT_ATTRIBUTES* attributes, IO_STATUS_BLOCK* io, long* allocationSize,
        uint fileAttributes, uint share, uint disposition, uint options, void* ea, uint eaLength);

    [DllImport("ntdll.dll")]
    static extern unsafe int NtSetInformationFile(SafeFileHandle handle, IO_STATUS_BLOCK* io, void* info, uint length, int infoClass);

    [DllImport("ntdll.dll")]
    static extern int RtlNtStatusToDosError(int status);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern SafeFileHandle CreateFileW(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GetFileInformationByHandle(SafeFileHandle handle, out BY_HANDLE_FILE_INFORMATION info);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern unsafe bool GetFileInformationByHandleEx(SafeFileHandle handle, int infoClass, void* info, uint size);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern unsafe uint GetFinalPathNameByHandleW(SafeFileHandle handle, char* path, uint length, uint flags);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern unsafe bool DeviceIoControl(SafeFileHandle handle, uint code, void* input, uint inputLength, void* output, uint outputLength,
        out uint returned, IntPtr overlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool FlushFileBuffers(SafeFileHandle handle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool GetDiskFreeSpaceExW(string directory, out ulong available, out ulong total, out ulong free);
}
