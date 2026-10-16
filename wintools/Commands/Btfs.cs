using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace WinTools;

// btfs: mount a torrent as a folder. The torrent's files appear right away and their data is
// downloaded when it is read: reading a file fetches the pieces it covers (plus a little read-ahead)
// from the swarm, verifies them against the torrent's SHA-1 hashes and serves them. On Windows the
// folder is a Projected File System root (see Projfs.cs) instead of a FUSE mount. The BitTorrent
// side is a small client: .torrent files, magnet links (metadata fetched from peers with ut_metadata)
// and http(s) URLs of .torrent files; HTTP and UDP trackers; web seeds (BEP 19); the peer wire
// protocol with pipelined block requests. It only downloads (no seeding or listening port), and
// magnet links need tracker (tr=) entries, as there is no DHT.
static class Btfs
{
    const string Version = "3.1";

    [ModuleInitializer]
    internal static void Register() => Commands.Add("btfs", "Mount a torrent or magnet link as a folder, downloading on demand", Run);

    const string Help = """
        usage: btfs [options] metadata mountpoint
               btfs -u mountpoint

        metadata: a .torrent file, a magnet link or an http(s) URL of a .torrent file

        btfs options:
            --version -v           show version information
            --help -h              show this message
            --browse-only -b       list the files, download nothing
            --keep -k              keep the downloaded data after unmounting
            --data-directory=DIR   where downloaded data is kept (default: %TEMP%\btfs-<hash>)
            --max-download-rate=N  accepted for compatibility (KiB/s)
            --max-upload-rate=N    accepted (btfs here doesn't upload)
            --min-port=N --max-port=N --utp-only   accepted for compatibility
            -f                     stay in the foreground
            -o opt[,opt...]        mount options (accepted for compatibility)
            -u mountpoint          unmount

        Needs the Windows Projected File System (the Client-ProjFS optional feature).
        """;

    static int Run(string[] args)
    {
        var positional = new List<string>();
        bool foreground = false, browse = false, keep = false;
        string dataDir = null;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            var key = a.Contains('=') ? a[..a.IndexOf('=')] : a;
            string Value() => a.Contains('=') ? a[(a.IndexOf('=') + 1)..] : ++i < args.Length ? args[i] : throw new UsageException($"{a} needs a value");
            switch (key)
            {
                case "-h" or "--help": Console.WriteLine(Help); return 0;
                case "-v" or "--version": Console.WriteLine($"btfs version: {Version}\nFUSE library version: ProjFS (Windows Projected File System)"); return 0;
                case "-u":
                    if (++i >= args.Length) throw new UsageException("-u needs a mount point");
                    return Projfs.Unmount("btfs", args[i]);
                case "-f" or "-d": foreground = true; break;
                case "-b" or "--browse-only": browse = true; break;
                case "-k" or "--keep": keep = true; break;
                case "--data-directory": dataDir = Value(); break;
                case "--max-download-rate" or "--max-upload-rate" or "--min-port" or "--max-port": Value(); break;
                case "--utp-only": break;
                case "-o": if (++i >= args.Length) throw new UsageException("-o needs options"); break;
                default:
                    if (a.StartsWith("-o")) break;
                    if (a.StartsWith('-')) throw new UsageException($"unknown option {a}");
                    positional.Add(a);
                    break;
            }
        }
        if (positional.Count != 2) { Console.Error.WriteLine(Help); return 1; }
        var (metadata, target) = (positional[0], positional[1]);

        // Validate what can be checked up front (a .torrent file); magnets resolve in the mount.
        Meta meta = null;
        try
        {
            if (File.Exists(metadata)) meta = Meta.FromTorrent(File.ReadAllBytes(metadata));
            else if (!metadata.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase) && !metadata.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            { Console.Error.WriteLine($"Failed to read metadata: {metadata}: No such file or directory"); return 1; }
            else if (metadata.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase)) Magnet.Parse(metadata);
        }
        catch (FormatException e) { Console.Error.WriteLine($"Failed to parse metadata: {e.Message}"); return 1; }

        var mountpoint = Projfs.MountPath(target);
        if (Projfs.Unavailable() is { } why) { Console.Error.WriteLine("btfs: " + why); return 1; }
        Directory.CreateDirectory(mountpoint);
        var childArgs = args.Where(x => x is not ("-f" or "-d")).ToArray();
        return Projfs.Mount("btfs", mountpoint, mountpoint, junction: false, foreground, childArgs, () =>
        {
            var m = meta ?? Resolve(metadata);
            var dir = dataDir ?? Path.Combine(Path.GetTempPath(), "btfs-" + Convert.ToHexString(m.InfoHash)[..12].ToLowerInvariant());
            return new Source(new Client(m, dir, keep), browse);
        }, clean: true, readyTimeout: meta == null ? 300 : 60);
    }

    // A magnet link or URL becomes full metadata (from peers or by download).
    static Meta Resolve(string metadata)
    {
        if (metadata.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            return Meta.FromTorrent(http.GetByteArrayAsync(metadata).GetAwaiter().GetResult());
        }
        var magnet = Magnet.Parse(metadata);
        if (magnet.Trackers.Count == 0) throw new IOException("the magnet link has no trackers (tr=); btfs on Windows finds peers through trackers, not the DHT");
        return Client.FetchMetadata(magnet);
    }

    // ---- bencoding ----

    sealed class Bencode(byte[] data)
    {
        int pos;
        public (int Start, int End) InfoSpan = (-1, -1);

        public static object Decode(byte[] data, out (int, int) infoSpan)
        {
            var b = new Bencode(data);
            var v = b.Value(0);
            infoSpan = b.InfoSpan;
            return v;
        }

        object Value(int depth)
        {
            if (pos >= data.Length || depth > 64) throw new FormatException("truncated bencoding");
            var c = data[pos];
            switch (c)
            {
                case (byte)'i':
                {
                    var end = Array.IndexOf(data, (byte)'e', pos);
                    if (end < 0) throw new FormatException("bad integer");
                    var v = long.Parse(Encoding.ASCII.GetString(data, pos + 1, end - pos - 1));
                    pos = end + 1;
                    return v;
                }
                case (byte)'l':
                {
                    pos++;
                    var list = new List<object>();
                    while (pos < data.Length && data[pos] != 'e') list.Add(Value(depth + 1));
                    pos++;
                    return list;
                }
                case (byte)'d':
                {
                    pos++;
                    var dict = new Dictionary<string, object>();
                    while (pos < data.Length && data[pos] != 'e')
                    {
                        var key = Encoding.UTF8.GetString((byte[])Value(depth + 1));
                        var start = pos;
                        dict[key] = Value(depth + 1);
                        if (key == "info" && depth == 0) InfoSpan = (start, pos);
                    }
                    pos++;
                    return dict;
                }
                default:
                {
                    if (c < '0' || c > '9') throw new FormatException($"unexpected byte 0x{c:x2} in bencoding");
                    var colon = Array.IndexOf(data, (byte)':', pos);
                    if (colon < 0) throw new FormatException("bad string");
                    var len = int.Parse(Encoding.ASCII.GetString(data, pos, colon - pos));
                    if (colon + 1 + len > data.Length) throw new FormatException("truncated string");
                    var s = data.AsSpan(colon + 1, len).ToArray();
                    pos = colon + 1 + len;
                    return s;
                }
            }
        }

        public static byte[] Encode(object v)
        {
            var ms = new MemoryStream();
            void W(object o)
            {
                switch (o)
                {
                    case long l: ms.Write(Encoding.ASCII.GetBytes($"i{l}e")); break;
                    case int i: ms.Write(Encoding.ASCII.GetBytes($"i{i}e")); break;
                    case string s: W(Encoding.UTF8.GetBytes(s)); break;
                    case byte[] b: ms.Write(Encoding.ASCII.GetBytes($"{b.Length}:")); ms.Write(b); break;
                    case List<object> l: ms.WriteByte((byte)'l'); foreach (var x in l) W(x); ms.WriteByte((byte)'e'); break;
                    case Dictionary<string, object> d:
                        ms.WriteByte((byte)'d');
                        foreach (var kv in d.OrderBy(kv => kv.Key, StringComparer.Ordinal)) { W(kv.Key); W(kv.Value); }
                        ms.WriteByte((byte)'e');
                        break;
                }
            }
            W(v);
            return ms.ToArray();
        }
    }

    static string S(object o) => o is byte[] b ? Encoding.UTF8.GetString(b) : null;
    static long L(object o) => o is long l ? l : 0;
    static object Get(object d, string key) => d is Dictionary<string, object> dict && dict.TryGetValue(key, out var v) ? v : null;

    // ---- torrent metadata ----

    sealed class FileEntry
    {
        public string Path;   // backslash-separated, relative to the mount
        public long Offset, Length;
    }

    sealed class Meta
    {
        public byte[] InfoHash;
        public string Name;
        public long PieceLength, Total;
        public byte[] Pieces;   // 20 bytes per piece
        public List<FileEntry> Files = [];
        public List<string> Trackers = [];
        public List<string> WebSeeds = [];
        public bool MultiFile;
        public int PieceCount => Pieces.Length / 20;

        public static Meta FromTorrent(byte[] data)
        {
            object root;
            (int Start, int End) span;
            try { root = Bencode.Decode(data, out span); }
            catch (Exception e) when (e is FormatException or OverflowException or IndexOutOfRangeException or InvalidCastException) { throw new FormatException($"not a valid torrent file ({e.Message})"); }
            var info = Get(root, "info") ?? throw new FormatException("no info dictionary");
            var m = FromInfo(info, SHA1.HashData(data.AsSpan(span.Start, span.End - span.Start)));
            if (S(Get(root, "announce")) is { } a) m.Trackers.Add(a);
            if (Get(root, "announce-list") is List<object> tiers)
                foreach (var t in tiers.OfType<List<object>>()) foreach (var u in t) if (S(u) is { } url && !m.Trackers.Contains(url)) m.Trackers.Add(url);
            if (Get(root, "url-list") is { } ws)
                foreach (var u in ws is List<object> l ? l : [ws]) if (S(u) is { } url) m.WebSeeds.Add(url);
            return m;
        }

        public static Meta FromInfo(object info, byte[] hash)
        {
            var m = new Meta
            {
                InfoHash = hash,
                Name = SafeName(S(Get(info, "name.utf-8")) ?? S(Get(info, "name")) ?? Convert.ToHexString(hash)),
                PieceLength = L(Get(info, "piece length")),
                Pieces = Get(info, "pieces") as byte[] ?? throw new FormatException("no piece hashes"),
            };
            if (m.PieceLength <= 0 || m.Pieces.Length % 20 != 0) throw new FormatException("bad piece length or piece hashes");
            if (Get(info, "files") is List<object> files)
            {
                m.MultiFile = true;
                long offset = 0;
                foreach (var f in files)
                {
                    var parts = (Get(f, "path.utf-8") ?? Get(f, "path")) as List<object> ?? throw new FormatException("file without a path");
                    var len = L(Get(f, "length"));
                    // BEP 47 padding files hold no data a reader wants.
                    var pad = S(Get(f, "attr"))?.Contains('p') == true;
                    if (!pad) m.Files.Add(new FileEntry { Path = m.Name + "\\" + string.Join('\\', parts.Select(p => SafeName(S(p)))), Offset = offset, Length = len });
                    offset += len;
                }
                m.Total = offset;
            }
            else
            {
                m.Total = L(Get(info, "length"));
                m.Files.Add(new FileEntry { Path = m.Name, Offset = 0, Length = m.Total });
            }
            if ((m.Total + m.PieceLength - 1) / m.PieceLength != m.PieceCount) throw new FormatException("piece count doesn't match the total size");
            return m;
        }

        // Characters Windows file names can't hold become '_'.
        static string SafeName(string s)
        {
            if (string.IsNullOrEmpty(s) || s is "." or "..") return "_";
            var invalid = Path.GetInvalidFileNameChars();
            var t = new string(s.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray()).TrimEnd('.', ' ');
            return t.Length == 0 ? "_" : t;
        }
    }

    sealed class Magnet
    {
        public byte[] InfoHash;
        public string Name;
        public List<string> Trackers = [];

        public static Magnet Parse(string uri)
        {
            var m = new Magnet();
            var q = uri.IndexOf('?');
            if (q < 0) throw new FormatException("magnet link without parameters");
            foreach (var part in uri[(q + 1)..].Split('&'))
            {
                var eq = part.IndexOf('=');
                if (eq < 0) continue;
                var (k, v) = (part[..eq], Uri.UnescapeDataString(part[(eq + 1)..].Replace('+', ' ')));
                if (k == "xt" && v.StartsWith("urn:btih:", StringComparison.OrdinalIgnoreCase))
                {
                    var h = v[9..];
                    m.InfoHash = h.Length == 40 ? Convert.FromHexString(h) : h.Length == 32 ? Base32(h) : throw new FormatException("bad info hash in magnet link");
                }
                else if (k == "dn") m.Name = v;
                else if (k == "tr" || k.StartsWith("tr.")) m.Trackers.Add(v);
            }
            if (m.InfoHash == null) throw new FormatException("magnet link without a BitTorrent info hash (xt=urn:btih:...)");
            return m;
        }

        static byte[] Base32(string s)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var bytes = new List<byte>();
            int buffer = 0, bits = 0;
            foreach (var ch in s.ToUpperInvariant())
            {
                var v = alphabet.IndexOf(ch);
                if (v < 0) throw new FormatException("bad base32 info hash");
                buffer = buffer << 5 | v;
                bits += 5;
                if (bits >= 8) { bytes.Add((byte)(buffer >> (bits - 8))); bits -= 8; }
            }
            return bytes.ToArray();
        }
    }

    // ---- the ProjFS view ----

    sealed class Source(Client client, bool browse) : ProjfsSource, IDisposable
    {
        public override bool ReadOnly => true;

        public void Dispose() => client.Dispose();

        public override List<ProjfsEntry> List(string dir)
        {
            var prefix = dir.Length == 0 ? "" : dir + "\\";
            if (dir.Length > 0 && !client.Meta.Files.Any(f => f.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))) return null;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var list = new List<ProjfsEntry>();
            foreach (var f in client.Meta.Files.Where(f => f.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                var rest = f.Path[prefix.Length..];
                var slash = rest.IndexOf('\\');
                var name = slash < 0 ? rest : rest[..slash];
                if (seen.Add(name)) list.Add(new ProjfsEntry(name, slash >= 0, slash >= 0 ? 0 : f.Length, client.Created));
            }
            return list;
        }

        public override ProjfsEntry Stat(string path)
        {
            var f = client.Meta.Files.FirstOrDefault(x => x.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
            if (f != null) return new ProjfsEntry(path[(path.LastIndexOf('\\') + 1)..], false, f.Length, client.Created);
            var prefix = path + "\\";
            return client.Meta.Files.Any(x => x.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                ? new ProjfsEntry(path[(path.LastIndexOf('\\') + 1)..], true, 0, client.Created) : null;
        }

        public override int Read(string path, long offset, Span<byte> buffer)
        {
            if (browse) throw new IOException("browse-only mount: file contents aren't downloaded");
            var f = client.Meta.Files.FirstOrDefault(x => x.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
            if (f == null || offset >= f.Length) return 0;
            var n = (int)Math.Min(buffer.Length, f.Length - offset);
            return client.Read(f.Offset + offset, buffer[..n]);
        }
    }

    // ---- the client ----

    sealed class Client : IDisposable
    {
        public readonly Meta Meta;
        public readonly DateTime Created = DateTime.UtcNow;
        readonly string dir;
        readonly bool keep;
        readonly FileStream data;
        readonly bool[] have;
        readonly HashSet<int> wanted = [];
        readonly HashSet<int> inProgress = [];
        readonly object gate = new();
        readonly byte[] peerId = PeerId();
        readonly HashSet<string> knownPeers = [];
        readonly Queue<IPEndPoint> candidates = new();
        int activePeers;
        volatile bool stopped;
        const int MaxPeers = 30, Block = 16384;

        public Client(Meta meta, string directory, bool keepData)
        {
            Meta = meta;
            dir = directory;
            keep = keepData;
            Directory.CreateDirectory(dir);
            data = new FileStream(Path.Combine(dir, "data.bin"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            if (data.Length != meta.Total) data.SetLength(meta.Total);
            have = new bool[meta.PieceCount];
            // Pieces already downloaded by an earlier mount (--keep, --data-directory) count.
            var haveFile = Path.Combine(dir, "have.bin");
            if (File.Exists(haveFile))
            {
                var bits = File.ReadAllBytes(haveFile);
                for (var i = 0; i < have.Length && i / 8 < bits.Length; i++) have[i] = (bits[i / 8] & (0x80 >> (i % 8))) != 0;
            }
            new Thread(TrackerLoop) { IsBackground = true }.Start();
            new Thread(PeerLoop) { IsBackground = true }.Start();
            foreach (var seed in meta.WebSeeds) { var s = seed; new Thread(() => WebSeedLoop(s)) { IsBackground = true }.Start(); }
        }

        public void Dispose()
        {
            stopped = true;
            lock (gate) Monitor.PulseAll(gate);
            data.Dispose();
            if (!keep) try { Directory.Delete(dir, true); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        }

        static byte[] PeerId()
        {
            var id = Encoding.ASCII.GetBytes("-BW0310-");
            return [.. id, .. RandomNumberGenerator.GetBytes(12)];
        }

        // Blocks until the pieces covering [offset, offset+buffer) are verified, then reads them.
        public int Read(long offset, Span<byte> buffer)
        {
            var first = (int)(offset / Meta.PieceLength);
            var last = (int)((offset + buffer.Length - 1) / Meta.PieceLength);
            lock (gate)
            {
                for (var p = first; p <= Math.Min(last + 2, Meta.PieceCount - 1); p++) if (!have[p]) wanted.Add(p);   // plus read-ahead
                Monitor.PulseAll(gate);
                while (!stopped && Enumerable.Range(first, last - first + 1).Any(p => !have[p])) Monitor.Wait(gate, 1000);
                if (stopped) throw new IOException("unmounted");
                data.Position = offset;
                var total = 0;
                while (total < buffer.Length && data.Read(buffer[total..]) is var n and > 0) total += n;
                return total;
            }
        }

        // Verifies a downloaded piece and stores it; false when the hash doesn't match.
        bool Store(int piece, byte[] bytes)
        {
            if (!SHA1.HashData(bytes).AsSpan().SequenceEqual(Meta.Pieces.AsSpan(piece * 20, 20))) return false;
            lock (gate)
            {
                if (have[piece]) return true;
                data.Position = (long)piece * Meta.PieceLength;
                data.Write(bytes);
                data.Flush();
                have[piece] = true;
                wanted.Remove(piece);
                inProgress.Remove(piece);
                var bits = new byte[(have.Length + 7) / 8];
                for (var i = 0; i < have.Length; i++) if (have[i]) bits[i / 8] |= (byte)(0x80 >> (i % 8));
                File.WriteAllBytes(Path.Combine(dir, "have.bin"), bits);
                Monitor.PulseAll(gate);
            }
            return true;
        }

        int PieceSize(int piece) => (int)Math.Min(Meta.PieceLength, Meta.Total - (long)piece * Meta.PieceLength);

        // The next wanted piece this source can provide, claimed so others pick different ones
        // (when every wanted piece is claimed, a second source may help with one: endgame).
        int Claim(Func<int, bool> has)
        {
            lock (gate)
            {
                while (!stopped)
                {
                    var free = wanted.Where(p => !have[p] && !inProgress.Contains(p) && has(p)).OrderBy(p => p).ToList();
                    if (free.Count > 0) { inProgress.Add(free[0]); return free[0]; }
                    var endgame = wanted.Where(p => !have[p] && has(p)).OrderBy(p => p).FirstOrDefault(-1);
                    if (endgame >= 0) return endgame;
                    Monitor.Wait(gate, 2000);
                    return -1;
                }
                return -1;
            }
        }

        void Release(int piece) { lock (gate) { inProgress.Remove(piece); Monitor.PulseAll(gate); } }

        bool Wanting { get { lock (gate) return wanted.Any(p => !have[p]); } }

        // ---- trackers ----

        void TrackerLoop()
        {
            while (!stopped)
            {
                var interval = 1800;
                foreach (var t in Meta.Trackers)
                {
                    try
                    {
                        var (peers, every) = Announce(t, Meta.InfoHash, peerId, Meta.Total);
                        interval = Math.Min(interval, Math.Max(60, every));
                        AddPeers(peers);
                    }
                    catch (Exception e) when (e is IOException or SocketException or HttpRequestException or TaskCanceledException or FormatException or TimeoutException) { }
                    if (stopped) return;
                }
                for (var s = 0; s < interval && !stopped; s++) Thread.Sleep(1000);
            }
        }

        void AddPeers(IEnumerable<IPEndPoint> peers)
        {
            lock (gate)
            {
                foreach (var p in peers)
                    if (knownPeers.Add(p.ToString())) candidates.Enqueue(p);
                Monitor.PulseAll(gate);
            }
        }

        public static (List<IPEndPoint> Peers, int Interval) Announce(string tracker, byte[] hash, byte[] peerId, long left)
        {
            if (tracker.StartsWith("udp://", StringComparison.OrdinalIgnoreCase)) return UdpAnnounce(new Uri(tracker), hash, peerId, left);
            if (!tracker.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return ([], 1800);
            static string Enc(byte[] b) => string.Concat(b.Select(x => char.IsAsciiLetterOrDigit((char)x) || x is (byte)'-' or (byte)'_' or (byte)'.' or (byte)'~' ? ((char)x).ToString() : $"%{x:X2}"));
            var url = $"{tracker}{(tracker.Contains('?') ? '&' : '?')}info_hash={Enc(hash)}&peer_id={Enc(peerId)}&port=6881&uploaded=0&downloaded=0&left={left}&compact=1&numwant=80&event=started";
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            var body = http.GetByteArrayAsync(url).GetAwaiter().GetResult();
            var r = Bencode.Decode(body, out _);
            if (S(Get(r, "failure reason")) is { } failure) throw new IOException(failure);
            var peers = new List<IPEndPoint>();
            switch (Get(r, "peers"))
            {
                case byte[] compact:
                    for (var i = 0; i + 6 <= compact.Length; i += 6)
                        peers.Add(new IPEndPoint(new IPAddress(compact.AsSpan(i, 4)), BinaryPrimitives.ReadUInt16BigEndian(compact.AsSpan(i + 4))));
                    break;
                case List<object> list:
                    foreach (var p in list)
                        if (IPAddress.TryParse(S(Get(p, "ip")) ?? "", out var ip)) peers.Add(new IPEndPoint(ip, (int)L(Get(p, "port"))));
                    break;
            }
            if (Get(r, "peers6") is byte[] p6)
                for (var i = 0; i + 18 <= p6.Length; i += 18)
                    peers.Add(new IPEndPoint(new IPAddress(p6.AsSpan(i, 16)), BinaryPrimitives.ReadUInt16BigEndian(p6.AsSpan(i + 16))));
            return (peers, (int)Math.Max(60, L(Get(r, "interval"))));
        }

        // BEP 15: connect, then announce, over UDP.
        static (List<IPEndPoint>, int) UdpAnnounce(Uri uri, byte[] hash, byte[] peerId, long left)
        {
            using var udp = new UdpClient(uri.Host, uri.Port);
            udp.Client.ReceiveTimeout = 15000;
            var tx = RandomNumberGenerator.GetInt32(int.MaxValue);
            var connect = new byte[16];
            BinaryPrimitives.WriteInt64BigEndian(connect, 0x41727101980);
            BinaryPrimitives.WriteInt32BigEndian(connect.AsSpan(12), tx);
            udp.Send(connect);
            IPEndPoint from = null;
            var resp = udp.Receive(ref from);
            if (resp.Length < 16 || BinaryPrimitives.ReadInt32BigEndian(resp) != 0 || BinaryPrimitives.ReadInt32BigEndian(resp.AsSpan(4)) != tx) throw new IOException("bad UDP tracker connect reply");
            var conn = BinaryPrimitives.ReadInt64BigEndian(resp.AsSpan(8));
            var announce = new byte[98];
            BinaryPrimitives.WriteInt64BigEndian(announce, conn);
            BinaryPrimitives.WriteInt32BigEndian(announce.AsSpan(8), 1);
            BinaryPrimitives.WriteInt32BigEndian(announce.AsSpan(12), ++tx);
            hash.CopyTo(announce, 16);
            peerId.CopyTo(announce, 36);
            BinaryPrimitives.WriteInt64BigEndian(announce.AsSpan(64), left);
            BinaryPrimitives.WriteInt32BigEndian(announce.AsSpan(80), 2);   // started
            BinaryPrimitives.WriteInt32BigEndian(announce.AsSpan(92), 80);  // num_want
            BinaryPrimitives.WriteUInt16BigEndian(announce.AsSpan(96), 6881);
            udp.Send(announce);
            resp = udp.Receive(ref from);
            if (resp.Length < 20 || BinaryPrimitives.ReadInt32BigEndian(resp) != 1) throw new IOException("bad UDP tracker announce reply");
            var interval = BinaryPrimitives.ReadInt32BigEndian(resp.AsSpan(8));
            var peers = new List<IPEndPoint>();
            for (var i = 20; i + 6 <= resp.Length; i += 6)
                peers.Add(new IPEndPoint(new IPAddress(resp.AsSpan(i, 4)), BinaryPrimitives.ReadUInt16BigEndian(resp.AsSpan(i + 4))));
            return (peers, interval);
        }

        // ---- peers ----

        void PeerLoop()
        {
            while (!stopped)
            {
                IPEndPoint next = null;
                lock (gate)
                {
                    if (activePeers < MaxPeers && candidates.Count > 0 && wanted.Any(p => !have[p])) { next = candidates.Dequeue(); activePeers++; }
                    else Monitor.Wait(gate, 1000);
                }
                if (next == null) continue;
                var ep = next;
                new Thread(() =>
                {
                    try { new Peer(this, ep).Run(); }
                    catch (Exception e) when (e is IOException or SocketException or ObjectDisposedException or FormatException or InvalidDataException) { }
                    finally
                    {
                        lock (gate)
                        {
                            activePeers--;
                            knownPeers.Remove(ep.ToString());   // may be retried when trackers list it again
                            Monitor.PulseAll(gate);
                        }
                    }
                }) { IsBackground = true }.Start();
            }
        }

        // One peer connection: handshake, then download claimed pieces block by block.
        sealed class Peer(Client c, IPEndPoint ep)
        {
            Socket socket;
            NetworkStream stream;
            bool[] has;
            bool choked = true;

            public void Run()
            {
                socket = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                if (!socket.ConnectAsync(ep).Wait(8000)) throw new IOException("connect timeout");
                stream = new NetworkStream(socket, true) { ReadTimeout = 60000, WriteTimeout = 30000 };
                using var _ = stream;
                Handshake(stream, c.Meta.InfoHash, c.peerId, extensions: false);
                has = new bool[c.Meta.PieceCount];
                Send(2, []);   // interested
                while (!c.stopped)
                {
                    if (!c.Wanting && choked) { Thread.Sleep(500); if (!c.Wanting) return; }
                    var (id, payload) = ReadMessage(stream);
                    if (!Handle(id, payload)) continue;
                    if (choked) continue;
                    var piece = c.Claim(p => has[p]);
                    if (piece < 0) continue;
                    try
                    {
                        var bytes = Download(piece);
                        if (bytes == null || !c.Store(piece, bytes)) { c.Release(piece); if (bytes != null) return; }   // bad data: drop the peer
                    }
                    catch { c.Release(piece); throw; }
                }
            }

            // Processes one message; true when it may have changed what can be requested.
            bool Handle(int id, byte[] p)
            {
                switch (id)
                {
                    case -1: return false;                                     // keep-alive
                    case 0: choked = true; return false;
                    case 1: choked = false; return true;
                    case 4: if (p.Length >= 4 && BinaryPrimitives.ReadUInt32BigEndian(p) is var i && i < has.Length) has[i] = true; return true;
                    case 5:
                        for (var k = 0; k < has.Length && k / 8 < p.Length; k++) has[k] = (p[k / 8] & (0x80 >> (k % 8))) != 0;
                        return true;
                    case 14: Array.Fill(has, true); return true;                // have all (BEP 6)
                    default: return false;
                }
            }

            // Requests a piece's blocks with up to five in flight and assembles the piece.
            byte[] Download(int piece)
            {
                var size = c.PieceSize(piece);
                var bytes = new byte[size];
                var blocks = (size + Block - 1) / Block;
                int next = 0, received = 0, inFlight = 0;
                var got = new bool[blocks];
                while (received < blocks)
                {
                    while (inFlight < 5 && next < blocks && !choked)
                    {
                        var req = new byte[12];
                        BinaryPrimitives.WriteInt32BigEndian(req, piece);
                        BinaryPrimitives.WriteInt32BigEndian(req.AsSpan(4), next * Block);
                        BinaryPrimitives.WriteInt32BigEndian(req.AsSpan(8), Math.Min(Block, size - next * Block));
                        Send(6, req);
                        next++;
                        inFlight++;
                    }
                    var (id, p) = ReadMessage(stream);
                    if (id == 7 && p.Length >= 8 && BinaryPrimitives.ReadInt32BigEndian(p) == piece)
                    {
                        var begin = BinaryPrimitives.ReadInt32BigEndian(p.AsSpan(4));
                        var k = begin / Block;
                        if (begin % Block == 0 && k < blocks && !got[k] && begin + p.Length - 8 <= size)
                        {
                            p.AsSpan(8).CopyTo(bytes.AsSpan(begin));
                            got[k] = true;
                            received++;
                            inFlight--;
                        }
                    }
                    else
                    {
                        Handle(id, p);
                        if (id == 0)
                        {
                            // Choked mid-piece: outstanding requests are dropped; ask again after unchoke.
                            inFlight = 0;
                            next = Array.IndexOf(got, false);
                            while (choked && !c.stopped) { var (i2, p2) = ReadMessage(stream); Handle(i2, p2); }
                        }
                    }
                    if (c.stopped) return null;
                }
                return bytes;
            }

            void Send(byte id, byte[] payload)
            {
                var m = new byte[5 + payload.Length];
                BinaryPrimitives.WriteInt32BigEndian(m, 1 + payload.Length);
                m[4] = id;
                payload.CopyTo(m, 5);
                stream.Write(m);
            }
        }

        public static void Handshake(Stream s, byte[] hash, byte[] peerId, bool extensions)
        {
            var hs = new byte[68];
            hs[0] = 19;
            Encoding.ASCII.GetBytes("BitTorrent protocol").CopyTo(hs, 1);
            hs[25] = 0x10;                     // extension protocol (BEP 10)
            if (extensions) hs[27] |= 0x04;    // fast extension
            hash.CopyTo(hs, 28);
            peerId.CopyTo(hs, 48);
            s.Write(hs);
            var reply = ReadExactly(s, 68);
            if (reply[0] != 19 || !reply.AsSpan(28, 20).SequenceEqual(hash)) throw new IOException("bad handshake");
        }

        // (id, payload); id -1 for keep-alive.
        public static (int, byte[]) ReadMessage(Stream s)
        {
            var len = BinaryPrimitives.ReadInt32BigEndian(ReadExactly(s, 4));
            if (len == 0) return (-1, []);
            if (len < 0 || len > (1 << 21)) throw new InvalidDataException("oversized peer message");
            var m = ReadExactly(s, len);
            return (m[0], m[1..]);
        }

        static byte[] ReadExactly(Stream s, int n)
        {
            var b = new byte[n];
            var got = 0;
            while (got < n)
            {
                var r = s.Read(b, got, n - got);
                if (r <= 0) throw new IOException("peer closed the connection");
                got += r;
            }
            return b;
        }

        // ---- web seeds (BEP 19) ----

        void WebSeedLoop(string url)
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
            var failures = 0;
            while (!stopped && failures < 5)
            {
                var piece = Claim(_ => true);
                if (piece < 0) continue;
                try
                {
                    var bytes = new byte[PieceSize(piece)];
                    var start = (long)piece * Meta.PieceLength;
                    var end = start + bytes.Length;
                    long at = 0;
                    var files = Meta.MultiFile ? Meta.Files : [new FileEntry { Path = Meta.Name, Offset = 0, Length = Meta.Total }];
                    // BEP 47 padding files are zeros between files: only real files are fetched.
                    foreach (var f in files.Where(f => f.Offset < end && f.Offset + f.Length > start))
                    {
                        var from = Math.Max(start, f.Offset) - f.Offset;
                        var to = Math.Min(end, f.Offset + f.Length) - f.Offset;
                        var fileUrl = Meta.MultiFile
                            ? url.TrimEnd('/') + "/" + string.Join('/', f.Path.Split('\\').Select(Uri.EscapeDataString))
                            : url.EndsWith('/') ? url + Uri.EscapeDataString(Meta.Name) : url;
                        using var req = new HttpRequestMessage(HttpMethod.Get, fileUrl);
                        req.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(from, to - 1);
                        using var res = http.Send(req, HttpCompletionOption.ResponseHeadersRead);
                        res.EnsureSuccessStatusCode();
                        using var body = res.Content.ReadAsStream();
                        at = f.Offset + from - start;
                        var want = (int)(to - from);
                        var n = 0;
                        while (n < want && body.Read(bytes, (int)at + n, want - n) is var r and > 0) n += r;
                    }
                    if (!Store(piece, bytes)) { Release(piece); failures++; }
                    else failures = 0;
                }
                catch (Exception e) when (e is HttpRequestException or IOException or TaskCanceledException)
                {
                    Release(piece);
                    failures++;
                    Thread.Sleep(2000);
                }
            }
        }

        // ---- metadata from peers (BEP 9) ----

        public static Meta FetchMetadata(Magnet magnet)
        {
            var peerId = PeerId();
            var peers = new List<IPEndPoint>();
            foreach (var t in magnet.Trackers)
            {
                try { peers.AddRange(Announce(t, magnet.InfoHash, peerId, 1).Peers); }
                catch (Exception e) when (e is IOException or SocketException or HttpRequestException or TaskCanceledException or FormatException) { }
            }
            peers = peers.DistinctBy(p => p.ToString()).ToList();
            if (peers.Count == 0) throw new IOException("no peers found for the magnet link's torrent");
            Meta result = null;
            var done = new ManualResetEventSlim();
            var pending = peers.Count;
            foreach (var p in peers.Take(40))
            {
                var ep = p;
                new Thread(() =>
                {
                    try
                    {
                        var info = MetadataFrom(ep, magnet.InfoHash, peerId);
                        if (info != null)
                            lock (done)
                                if (result == null)
                                {
                                    result = Meta.FromInfo(Bencode.Decode(info, out _), magnet.InfoHash);
                                    result.Trackers.AddRange(magnet.Trackers);
                                    done.Set();
                                }
                    }
                    catch (Exception e) when (e is IOException or SocketException or FormatException or InvalidDataException or ObjectDisposedException or AggregateException) { }
                    finally { if (Interlocked.Decrement(ref pending) == 0) done.Set(); }
                }) { IsBackground = true }.Start();
            }
            done.Wait(TimeSpan.FromMinutes(4));
            return result ?? throw new IOException("could not get the torrent's metadata from its peers");
        }

        static byte[] MetadataFrom(IPEndPoint ep, byte[] hash, byte[] peerId)
        {
            using var socket = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            if (!socket.ConnectAsync(ep).Wait(8000)) return null;
            using var s = new NetworkStream(socket) { ReadTimeout = 20000 };
            Handshake(s, hash, peerId, extensions: false);
            var hello = Bencode.Encode(new Dictionary<string, object> { ["m"] = new Dictionary<string, object> { ["ut_metadata"] = 1L } });
            SendExtended(s, 0, hello);
            int utMetadata = -1;
            long size = 0;
            byte[] metadata = null;
            var received = 0;
            for (var messages = 0; messages < 500; messages++)
            {
                var (id, p) = ReadMessage(s);
                if (id != 20 || p.Length < 1) continue;
                if (p[0] == 0)
                {
                    var d = Bencode.Decode(p[1..], out _);
                    utMetadata = (int)L(Get(Get(d, "m"), "ut_metadata"));
                    size = L(Get(d, "metadata_size"));
                    if (utMetadata <= 0 || size <= 0 || size > 64 << 20) return null;
                    metadata = new byte[size];
                    for (var piece = 0; piece * 16384 < size; piece++)
                        SendExtended(s, (byte)utMetadata, Bencode.Encode(new Dictionary<string, object> { ["msg_type"] = 0L, ["piece"] = (long)piece }));
                    continue;
                }
                if (metadata == null) continue;
                // {msg_type: 1, piece: n, total_size: N} followed by the raw piece.
                var dictEnd = DictEnd(p, 1);
                var header = Bencode.Decode(p[1..dictEnd], out _);
                if (L(Get(header, "msg_type")) != 1) return null;
                var index = (int)L(Get(header, "piece"));
                var chunk = p[dictEnd..];
                if (index * 16384 + chunk.Length > size) return null;
                chunk.CopyTo(metadata, index * 16384);
                received += chunk.Length;
                if (received >= size) return SHA1.HashData(metadata).AsSpan().SequenceEqual(hash) ? metadata : null;
            }
            return null;
        }

        // The end of the bencoded dictionary starting at p[start].
        static int DictEnd(byte[] p, int start)
        {
            var depth = 0;
            var i = start;
            while (i < p.Length)
            {
                var ch = p[i];
                if (ch is (byte)'d' or (byte)'l') { depth++; i++; }
                else if (ch == 'e') { depth--; i++; if (depth == 0) return i; }
                else if (ch == 'i') i = Array.IndexOf(p, (byte)'e', i) + 1;
                else
                {
                    var colon = Array.IndexOf(p, (byte)':', i);
                    i = colon + 1 + int.Parse(Encoding.ASCII.GetString(p, i, colon - i));
                }
            }
            throw new FormatException("unterminated metadata message");
        }

        static void SendExtended(Stream s, byte ext, byte[] payload)
        {
            var m = new byte[6 + payload.Length];
            BinaryPrimitives.WriteInt32BigEndian(m, 2 + payload.Length);
            m[4] = 20;
            m[5] = ext;
            payload.CopyTo(m, 6);
            s.Write(m);
        }
    }
}
