using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace WinTools;

// s3backer: a block device stored in Amazon S3 (or any S3-compatible store), one object per
// block, all-zero blocks not stored. On Linux the device is a file in a FUSE mount; Windows' own
// way to attach a network block device is iSCSI, so s3backer serves the device as an iSCSI target
// and connects Windows' built-in iSCSI initiator to it: the bucket shows up as a local disk
// (initialize and format it the first time). --test keeps the blocks in a local directory, and
// --erase, --reset-mounted-flag and --listBlocks work on the bucket directly.
static class S3Backer
{
    const string Version = "2.1.6";

    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("s3backer", "Block device stored in Amazon S3, attached as an iSCSI disk", Run);

    sealed class Options
    {
        public string Bucket, Subdir = "", Prefix = "", BaseUrl = "https://s3.amazonaws.com/", Region = "us-east-1";
        public string AccessId, AccessKey, AccessFile, Listen = "127.0.0.1:3260", Mountpoint;
        public long Size;
        public int BlockSize = 4096, CacheSize = 1000;
        public bool Test, ReadOnly, Force, Erase, ResetMounted, ListBlocks, Vhost, Connect = true, Debug;
        public string FileMode;
    }

    static int Run(string[] args)
    {
        var o = new Options();
        var positional = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (!a.StartsWith("--") || a == "--")
            {
                switch (a)
                {
                    case "-h": Console.WriteLine(Help); return 0;
                    case "-f" or "-d" or "-s": if (a == "-d") o.Debug = true; continue;
                    case "-o": i++; continue;   // FUSE options
                    default:
                        if (a.StartsWith('-')) throw new UsageException($"unknown flag `{a}'");
                        positional.Add(a);
                        continue;
                }
            }
            var eq = a.IndexOf('=');
            var name = eq > 0 ? a[2..eq] : a[2..];
            string Value() => eq > 0 ? a[(eq + 1)..] : throw new UsageException($"option `--{name}' requires a value (--{name}=value)");
            switch (name)
            {
                case "help": Console.WriteLine(Help); return 0;
                case "version": Console.Error.WriteLine($"s3backer version {Version} (windows)"); return 0;
                case "accessId": o.AccessId = Value(); break;
                case "accessKey": o.AccessKey = Value(); break;
                case "accessFile": o.AccessFile = Value(); break;
                case "baseURL": o.BaseUrl = Value().EndsWith('/') ? Value() : Value() + "/"; break;
                case "region": o.Region = Value(); o.BaseUrl = $"https://s3.{o.Region}.amazonaws.com/"; break;
                case "vhost": o.Vhost = true; break;
                case "prefix": o.Prefix = Value(); break;
                case "size": o.Size = ParseSize(Value(), "size"); break;
                case "blockSize": o.BlockSize = checked((int)ParseSize(Value(), "blockSize")); break;
                case "blockCacheSize": o.CacheSize = int.TryParse(Value(), out var c) && c >= 0 ? c : throw new UsageException("invalid blockCacheSize"); break;
                case "readOnly": o.ReadOnly = true; break;
                case "force": o.Force = true; break;
                case "erase": o.Erase = true; break;
                case "reset-mounted-flag": o.ResetMounted = true; break;
                case "listBlocks": o.ListBlocks = true; break;
                case "test": o.Test = true; break;
                case "debug": o.Debug = true; break;
                case "listen": o.Listen = Value(); break;
                case "no-connect": o.Connect = false; break;
                case "filename": o.FileMode = Value(); break;
                case "compress" or "encrypt" or "password" or "passwordFile" or "nbd" or "accessEC2IAM" or "sse":
                    Console.Error.WriteLine($"s3backer: --{name} isn't available in this build");
                    return 1;
                case "authVersion":
                    if (Value() != "aws4") { Console.Error.WriteLine("s3backer: only --authVersion=aws4 is available"); return 1; }
                    break;
                case "storageClass" or "timeout" or "initialRetryPause" or "maxRetryPause" or "blockCacheThreads" or "blockCacheWriteDelay"
                    or "blockCacheTimeout" or "md5CacheSize" or "md5CacheTime" or "minWriteDelay" or "cacert" or "insecure" or "blockHashPrefix"
                    or "listBlocksThreads" or "fileMode" or "statsFilename" or "blockCacheSync" or "blockCacheRecoverDirtyBlocks" or "blockCacheFile":
                    break;   // tuning that doesn't change what is stored
                default: throw new UsageException($"unknown option `--{name}'");
            }
        }
        if (positional.Count == 0) { Console.Error.WriteLine("s3backer: no S3 bucket specified"); return 1; }
        // With --test the bucket is a local directory, so a path like C:/x/bucket isn't split into bucket/subdir.
        var bucketParts = o.Test ? [positional[0]] : positional[0].Split('/', 2);
        o.Bucket = bucketParts[0];
        if (bucketParts.Length > 1) o.Subdir = bucketParts[1].TrimEnd('/') + "/";
        o.Mountpoint = positional.Count > 1 ? positional[1] : null;
        if ((o.BlockSize & (o.BlockSize - 1)) != 0 || o.BlockSize < 512) throw new UsageException("block size must be a power of two of at least 512");

        IStore store = o.Test ? new DirStore(o.Bucket) : new S3Store(o, Credentials(o));
        try
        {
            if (o.ResetMounted)
            {
                store.Delete(o.Subdir + o.Prefix + "s3backer-mounted");
                Console.Error.WriteLine("s3backer: reset mounted flag");
                return 0;
            }
            if (o.Erase) return Erase(o, store);
            DetectSize(o, store);
            if (o.ListBlocks && o.Mountpoint == null)
            {
                foreach (var key in store.List(o.Subdir + o.Prefix).Where(k => BlockNumber(o, k) != null).Order(StringComparer.Ordinal)) Console.WriteLine(key);
                return 0;
            }
            if (o.Mountpoint == null) { Console.Error.WriteLine("s3backer: no mount point specified (use any name: the device is attached as a disk)"); return 1; }
            return Serve(o, store);
        }
        catch (StoreException e)
        {
            Console.Error.WriteLine($"s3backer: {e.Message}");
            return 1;
        }
    }

    const string Help = """
        Usage: s3backer [options] bucket[/subdir] /mount/point
               s3backer --test [options] directory /mount/point
               s3backer --erase [options] bucket[/subdir]
               s3backer --reset-mounted-flag [options] bucket[/subdir]
        Options:
          --accessFile=FILE       File containing `accessID:accessKey' pairs (default %USERPROFILE%\.s3backer_passwd)
          --accessId=ID           S3 access key ID (or AWS_ACCESS_KEY_ID)
          --accessKey=KEY         S3 secret access key (or AWS_SECRET_ACCESS_KEY)
          --baseURL=URL           Base URL for S3 (default https://s3.amazonaws.com/)
          --region=REGION         S3 region (default us-east-1)
          --vhost                 Use virtual host bucket style URL for all requests
          --prefix=STRING         Prefix for block object names
          --size=SIZE             File size (with optional suffix 'K', 'M', 'G', etc.)
          --blockSize=SIZE        Block size (with optional suffix 'K', 'M', 'G', etc.; default 4K)
          --blockCacheSize=NUM    Block cache size (in number of blocks; default 1000)
          --readOnly              Return `Read-only file system' error for write attempts
          --force                 Ignore different auto-detected block and file sizes, and the mounted flag
          --erase                 Erase all blocks in the filesystem
          --reset-mounted-flag    Reset `already mounted' flag in the filesystem
          --listBlocks            List the stored blocks (without a mount point: print them)
          --test                  Run in local test mode (bucket is a directory)
          --listen=ADDR:PORT      Where the iSCSI target listens (default 127.0.0.1:3260)
          --no-connect            Don't connect Windows' iSCSI initiator (serve other initiators)
          --debug                 Enable logging of debug messages
          --version               Show version information and exit
          --help                  Show this information and exit

        The device is attached through Windows' iSCSI initiator (administrator) and appears as a
        new disk; the mount point argument names the session. Ctrl+C detaches and stops.
        """;

    static long ParseSize(string s, string what)
    {
        var mult = 1L;
        var last = char.ToUpperInvariant(s[^1]);
        var idx = "KMGTPE".IndexOf(last);
        if (idx >= 0) { mult = 1L << (10 * (idx + 1)); s = s[..^1]; }
        return long.TryParse(s, out var n) && n > 0 ? checked(n * mult) : throw new UsageException($"invalid {what} `{s}'");
    }

    static (string Id, string Key) Credentials(Options o)
    {
        if (o.AccessId != null && o.AccessKey != null) return (o.AccessId, o.AccessKey);
        var file = o.AccessFile ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".s3backer_passwd");
        if (File.Exists(file))
            foreach (var line in File.ReadAllLines(file))
            {
                var parts = line.Trim().Split(':', 2);
                if (parts.Length == 2 && (o.AccessId == null || parts[0] == o.AccessId)) return (parts[0], parts[1]);
            }
        var envId = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        var envKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
        if (envId != null && envKey != null) return (envId, envKey);
        throw new StoreException($"no S3 credentials (give --accessId and --accessKey, or write {file})");
    }

    // ---- block naming and size detection ----

    static int Digits(Options o) => (o.Size / o.BlockSize) > uint.MaxValue ? 16 : 8;
    static string BlockKey(Options o, long n) => o.Subdir + o.Prefix + n.ToString("x" + Digits(o));

    static long? BlockNumber(Options o, string key)
    {
        var name = key[(o.Subdir + o.Prefix).Length..];
        return name.Length is 8 or 16 && long.TryParse(name, NumberStyles.HexNumber, null, out var n) ? n : null;
    }

    // Like s3backer: block and file size come from the blocks' metadata when not given.
    static void DetectSize(Options o, IStore store)
    {
        var first = store.List(o.Subdir + o.Prefix, 50).FirstOrDefault(k => BlockNumber(o, k) != null);
        var meta = first != null ? store.Metadata(first) : null;
        if (meta != null && meta.TryGetValue("s3backer-blocksize", out var bs) && meta.TryGetValue("s3backer-filesize", out var fs))
        {
            var blockSize = int.Parse(bs);
            var size = long.Parse(fs);
            if (o.Size != 0 && (o.Size != size || o.BlockSize != blockSize) && !o.Force)
                throw new StoreException($"auto-detected size {size} and block size {blockSize} differ from the ones given (use --force to override)");
            if (o.Size == 0) { o.Size = size; o.BlockSize = blockSize; }
        }
        if (o.Size == 0) throw new StoreException("can't auto-detect size; please specify --size");
        if (o.Size % o.BlockSize != 0) throw new StoreException("file size must be a multiple of the block size");
    }

    static int Erase(Options o, IStore store)
    {
        if (!o.Force)
        {
            if (Console.IsInputRedirected) throw new StoreException("--erase needs --force when not run interactively");
            Console.Write($"s3backer: really delete all blocks in bucket `{o.Bucket}', subdirectory `{o.Subdir}'? (type ERASE): ");
            if (Console.ReadLine()?.Trim() != "ERASE") { Console.Error.WriteLine("s3backer: not confirmed"); return 1; }
        }
        var count = 0;
        foreach (var key in store.List(o.Subdir + o.Prefix).ToList())
            if (BlockNumber(o, key) != null || key.EndsWith("s3backer-mounted")) { store.Delete(key); count++; }
        Console.Error.WriteLine($"s3backer: erased {count} objects");
        return 0;
    }

    // ---- stores ----

    sealed class StoreException(string message) : Exception(message);

    interface IStore
    {
        byte[] Get(string key);                               // null: absent
        void Put(string key, byte[] data, Dictionary<string, string> meta);
        void Delete(string key);
        IEnumerable<string> List(string prefix, int limit = int.MaxValue);
        Dictionary<string, string> Metadata(string key);      // null: absent
    }

    // --test: objects are files in a directory; metadata lives in a ".meta" file beside each.
    sealed class DirStore(string dir) : IStore
    {
        readonly string root = Path.GetFullPath(dir);

        string File(string key)
        {
            var path = Path.GetFullPath(Path.Combine(root, key.Replace('/', Path.DirectorySeparatorChar)));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new StoreException($"invalid key {key}");
            return path;
        }

        public byte[] Get(string key) => System.IO.File.Exists(File(key)) ? System.IO.File.ReadAllBytes(File(key)) : null;

        public void Put(string key, byte[] data, Dictionary<string, string> meta)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(File(key)));
            System.IO.File.WriteAllBytes(File(key), data);
            System.IO.File.WriteAllLines(File(key) + ".meta", meta.Select(kv => $"{kv.Key}={kv.Value}"));
        }

        public void Delete(string key)
        {
            System.IO.File.Delete(File(key));
            System.IO.File.Delete(File(key) + ".meta");
        }

        public IEnumerable<string> List(string prefix, int limit = int.MaxValue)
        {
            if (!Directory.Exists(root)) throw new StoreException($"{root}: no such directory");
            return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Where(f => !f.EndsWith(".meta"))
                .Select(f => Path.GetRelativePath(root, f).Replace(Path.DirectorySeparatorChar, '/'))
                .Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).Order(StringComparer.Ordinal).Take(limit);
        }

        public Dictionary<string, string> Metadata(string key)
        {
            var path = File(key) + ".meta";
            if (!System.IO.File.Exists(path)) return System.IO.File.Exists(File(key)) ? new() : null;
            return System.IO.File.ReadAllLines(path).Select(l => l.Split('=', 2)).Where(p => p.Length == 2).ToDictionary(p => p[0], p => p[1]);
        }
    }

    // S3 over HTTPS with AWS Signature Version 4.
    sealed class S3Store(Options o, (string Id, string Key) creds) : IStore
    {
        readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(60) };

        Uri Url(string key, string query = "")
        {
            var baseUri = new Uri(o.BaseUrl);
            var path = o.Vhost ? "/" + Escape(key) : $"{baseUri.AbsolutePath.TrimEnd('/')}/{o.Bucket}/{Escape(key)}";
            var host = o.Vhost ? $"{o.Bucket}.{baseUri.Host}" : baseUri.Host;
            var port = baseUri.IsDefaultPort ? "" : $":{baseUri.Port}";
            return new Uri($"{baseUri.Scheme}://{host}{port}{path}{(query.Length > 0 ? "?" + query : "")}");
        }

        static string Escape(string key) => string.Join("/", key.Split('/').Select(Uri.EscapeDataString));

        HttpResponseMessage Send(HttpMethod method, Uri url, byte[] body = null, Dictionary<string, string> headers = null)
        {
            for (var attempt = 0; ; attempt++)
            {
                var req = new HttpRequestMessage(method, url);
                var payload = body ?? [];
                var hash = Convert.ToHexStringLower(SHA256.HashData(payload));
                var now = DateTime.UtcNow;
                var amzDate = now.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
                var all = new SortedDictionary<string, string>(StringComparer.Ordinal)
                {
                    ["host"] = url.IsDefaultPort ? url.Host : $"{url.Host}:{url.Port}",
                    ["x-amz-content-sha256"] = hash,
                    ["x-amz-date"] = amzDate,
                };
                foreach (var (k, v) in headers ?? []) all[k.ToLowerInvariant()] = v;
                var auth = SigV4(method.Method, url.AbsolutePath, url.Query.TrimStart('?'), all, hash, now, o.Region, creds.Id, creds.Key);
                foreach (var (k, v) in all.Where(kv => kv.Key != "host" && !kv.Key.StartsWith("content-"))) req.Headers.TryAddWithoutValidation(k, v);
                req.Headers.TryAddWithoutValidation("Authorization", auth);
                if (body != null)
                {
                    req.Content = new ByteArrayContent(body);
                    foreach (var (k, v) in all.Where(kv => kv.Key.StartsWith("content-"))) req.Content.Headers.TryAddWithoutValidation(k, v);
                }
                try
                {
                    var resp = http.Send(req);
                    if ((int)resp.StatusCode >= 500 && attempt < 5) { Thread.Sleep(200 << attempt); continue; }
                    return resp;
                }
                catch (HttpRequestException e) when (attempt < 5)
                {
                    if (o.Debug) Console.Error.WriteLine($"s3backer: {method} {url}: {e.Message}; retrying");
                    Thread.Sleep(200 << attempt);
                }
                catch (HttpRequestException e) { throw new StoreException($"{method} {url}: {e.Message}"); }
            }
        }

        static void Check(HttpResponseMessage r, string what)
        {
            if (r.IsSuccessStatusCode) return;
            var body = r.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var code = System.Text.RegularExpressions.Regex.Match(body, "<Code>([^<]*)</Code>").Groups[1].Value;
            throw new StoreException($"{what}: HTTP {(int)r.StatusCode} {r.ReasonPhrase}{(code.Length > 0 ? $" ({code})" : "")}");
        }

        public byte[] Get(string key)
        {
            using var r = Send(HttpMethod.Get, Url(key));
            if (r.StatusCode == HttpStatusCode.NotFound) return null;
            Check(r, $"GET {key}");
            return r.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
        }

        public void Put(string key, byte[] data, Dictionary<string, string> meta)
        {
            var headers = meta.ToDictionary(kv => "x-amz-meta-" + kv.Key, kv => kv.Value);
            headers["content-md5"] = Convert.ToBase64String(MD5.HashData(data));
            headers["content-type"] = "application/x-s3backer-block";
            using var r = Send(HttpMethod.Put, Url(key), data, headers);
            Check(r, $"PUT {key}");
        }

        public void Delete(string key)
        {
            using var r = Send(HttpMethod.Delete, Url(key));
            if (r.StatusCode != HttpStatusCode.NotFound) Check(r, $"DELETE {key}");
        }

        public IEnumerable<string> List(string prefix, int limit = int.MaxValue)
        {
            string token = null;
            var count = 0;
            do
            {
                var q = new SortedDictionary<string, string>(StringComparer.Ordinal) { ["list-type"] = "2", ["prefix"] = prefix, ["max-keys"] = Math.Min(1000, limit - count).ToString() };
                if (token != null) q["continuation-token"] = token;
                var query = string.Join("&", q.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
                var baseUri = new Uri(o.BaseUrl);
                var url = o.Vhost ? new Uri($"{baseUri.Scheme}://{o.Bucket}.{baseUri.Authority}/?{query}") : new Uri($"{baseUri.Scheme}://{baseUri.Authority}{baseUri.AbsolutePath.TrimEnd('/')}/{o.Bucket}?{query}");
                using var r = Send(HttpMethod.Get, url);
                Check(r, $"list bucket {o.Bucket}");
                var doc = new XmlDocument();
                doc.LoadXml(r.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                var keys = new List<string>();
                foreach (XmlNode n in doc.GetElementsByTagName("Key")) keys.Add(n.InnerText);
                foreach (var k in keys) { yield return k; if (++count >= limit) yield break; }
                token = doc.GetElementsByTagName("NextContinuationToken").Cast<XmlNode>().FirstOrDefault()?.InnerText;
            } while (token != null);
        }

        public Dictionary<string, string> Metadata(string key)
        {
            using var r = Send(HttpMethod.Head, Url(key));
            if (r.StatusCode == HttpStatusCode.NotFound) return null;
            Check(r, $"HEAD {key}");
            return r.Headers.Where(h => h.Key.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(h => h.Key["x-amz-meta-".Length..].ToLowerInvariant(), h => h.Value.First());
        }
    }

    // AWS Signature Version 4 Authorization header for a request whose headers are all signed.
    static string SigV4(string method, string path, string query, SortedDictionary<string, string> headers, string payloadHash,
                        DateTime now, string region, string accessId, string secret, string service = "s3")
    {
        var date = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var amzDate = now.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var canonicalQuery = string.Join("&", query.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2)).Select(p => (K: Uri.EscapeDataString(Uri.UnescapeDataString(p[0])), V: p.Length > 1 ? Uri.EscapeDataString(Uri.UnescapeDataString(p[1])) : ""))
            .OrderBy(p => p.K, StringComparer.Ordinal).ThenBy(p => p.V, StringComparer.Ordinal).Select(p => $"{p.K}={p.V}"));
        var canonicalHeaders = string.Concat(headers.Select(kv => $"{kv.Key}:{kv.Value.Trim()}\n"));
        var signedHeaders = string.Join(";", headers.Keys);
        var canonical = $"{method}\n{path}\n{canonicalQuery}\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";
        var scope = $"{date}/{region}/{service}/aws4_request";
        var toSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{scope}\n{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))}";
        var key = HMACSHA256.HashData(Encoding.UTF8.GetBytes("AWS4" + secret), Encoding.UTF8.GetBytes(date));
        foreach (var part in new[] { region, service, "aws4_request" }) key = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(part));
        var signature = Convert.ToHexStringLower(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(toSign)));
        return $"AWS4-HMAC-SHA256 Credential={accessId}/{scope}, SignedHeaders={signedHeaders}, Signature={signature}";
    }

    // ---- the block device ----

    sealed class Device(Options o, IStore store)
    {
        readonly object sync = new();
        readonly Dictionary<long, LinkedListNode<(long Block, byte[] Data)>> index = new();
        readonly LinkedList<(long Block, byte[] Data)> lru = new();
        public long Reads, Writes, ZeroReads, Deletes;

        public long Size => o.Size;
        public bool ReadOnly => o.ReadOnly;

        byte[] Block(long n)
        {
            lock (sync)
                if (index.TryGetValue(n, out var node)) { lru.Remove(node); lru.AddFirst(node); return node.Value.Data; }
            var data = store.Get(BlockKey(o, n));
            if (data == null) { data = new byte[o.BlockSize]; Interlocked.Increment(ref ZeroReads); }
            else if (data.Length != o.BlockSize) Array.Resize(ref data, o.BlockSize);
            Interlocked.Increment(ref Reads);
            Cache(n, data);
            return data;
        }

        void Cache(long n, byte[] data)
        {
            if (o.CacheSize == 0) return;
            lock (sync)
            {
                if (index.TryGetValue(n, out var old)) lru.Remove(old);
                index[n] = lru.AddFirst((n, data));
                while (lru.Count > o.CacheSize) { index.Remove(lru.Last.Value.Block); lru.RemoveLast(); }
            }
        }

        public void Read(long offset, Span<byte> buffer)
        {
            while (buffer.Length > 0)
            {
                var n = offset / o.BlockSize;
                var at = (int)(offset % o.BlockSize);
                var len = Math.Min(buffer.Length, o.BlockSize - at);
                Block(n).AsSpan(at, len).CopyTo(buffer);
                buffer = buffer[len..];
                offset += len;
            }
        }

        // Write-through: each touched block is written back whole; an all-zero block is deleted.
        public void Write(long offset, ReadOnlySpan<byte> data)
        {
            if (o.ReadOnly) throw new IOException("read-only file system");
            while (data.Length > 0)
            {
                var n = offset / o.BlockSize;
                var at = (int)(offset % o.BlockSize);
                var len = Math.Min(data.Length, o.BlockSize - at);
                var block = len == o.BlockSize ? new byte[o.BlockSize] : (byte[])Block(n).Clone();
                data[..len].CopyTo(block.AsSpan(at));
                Store(n, block);
                data = data[len..];
                offset += len;
            }
        }

        public void Discard(long offset, long length)
        {
            if (o.ReadOnly) return;
            // Only whole blocks can be dropped; partial ones are zeroed.
            var zeros = new byte[Math.Min(length, o.BlockSize)];
            while (length > 0)
            {
                var at = (int)(offset % o.BlockSize);
                var len = (int)Math.Min(length, o.BlockSize - at);
                Write(offset, zeros.AsSpan(0, len));
                offset += len;
                length -= len;
            }
        }

        void Store(long n, byte[] block)
        {
            if (block.AsSpan().IndexOfAnyExcept((byte)0) < 0)
            {
                store.Delete(BlockKey(o, n));
                Interlocked.Increment(ref Deletes);
            }
            else
            {
                store.Put(BlockKey(o, n), block, new() { ["s3backer-filesize"] = o.Size.ToString(), ["s3backer-blocksize"] = o.BlockSize.ToString() });
                Interlocked.Increment(ref Writes);
            }
            Cache(n, block);
        }
    }

    // ---- serving: iSCSI target plus Windows' initiator ----

    static int Serve(Options o, IStore store)
    {
        var mounted = o.Subdir + o.Prefix + "s3backer-mounted";
        if (!o.ReadOnly)
        {
            if (store.Metadata(mounted) != null && !o.Force)
                throw new StoreException("file system appears to be mounted already (use --reset-mounted-flag or --force)");
            store.Put(mounted, Encoding.ASCII.GetBytes($"{Environment.MachineName} {DateTime.UtcNow:O}\n"), new());
        }
        var device = new Device(o, store);
        var colon = o.Listen.LastIndexOf(':');
        var endpoint = new IPEndPoint(IPAddress.Parse(o.Listen[..colon]), int.Parse(o.Listen[(colon + 1)..]));
        var iqn = $"iqn.2026-10.org.s3backer:{Sanitize(o.Bucket)}{(o.Subdir.Length > 0 ? "." + Sanitize(o.Subdir) : "")}";
        using var listener = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(endpoint);
        listener.Listen(4);
        Console.Error.WriteLine($"s3backer: {o.Size} bytes in {o.Size / o.BlockSize} blocks of {o.BlockSize} from {(o.Test ? "directory " : "bucket ")}{o.Bucket}");
        Console.Error.WriteLine($"s3backer: iSCSI target {iqn} on {endpoint}");
        using var stop = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); listener.Dispose(); };
        var server = Task.Run(() =>
        {
            while (!stop.IsCancellationRequested)
            {
                Socket client;
                try { client = listener.Accept(); }
                catch (Exception e) when (e is SocketException or ObjectDisposedException) { return; }
                client.NoDelay = true;
                _ = Task.Run(() =>
                {
                    using var c = client;
                    try { new Session(device, iqn, endpoint, o.Debug).Run(new NetworkStream(c)); }
                    catch (Exception e) when (e is IOException or SocketException or StoreException or InvalidDataException)
                    {
                        if (o.Debug) Console.Error.WriteLine($"s3backer: session ended: {e.Message}");
                    }
                });
            }
        });
        var connected = o.Connect && Initiator(true, endpoint, iqn);
        if (connected) Console.Error.WriteLine($"s3backer: attached; the device is a new disk ({o.Mountpoint} names the session). Ctrl+C detaches.");
        else Console.Error.WriteLine($"s3backer: serving; connect an initiator to {endpoint} ({iqn}). Ctrl+C stops.");
        stop.Token.WaitHandle.WaitOne();
        if (connected) Initiator(false, endpoint, iqn);
        try { server.Wait(2000); } catch (AggregateException) { }
        if (!o.ReadOnly) store.Delete(mounted);
        Console.Error.WriteLine($"s3backer: blocks read {device.Reads} (zero {device.ZeroReads}), written {device.Writes}, deleted {device.Deletes}");
        return 0;
    }

    static string Sanitize(string s) => new string(s.ToLowerInvariant().Select(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-' ? c : '-').ToArray()).Trim('-');

    // Windows' iSCSI initiator, through its PowerShell module (the MSiSCSI service is started if needed).
    static bool Initiator(bool attach, IPEndPoint endpoint, string iqn)
    {
        var script = attach
            ? "$ErrorActionPreference='Stop'; if ((Get-Service MSiSCSI).Status -ne 'Running') { Start-Service MSiSCSI }; " +
              $"New-IscsiTargetPortal -TargetPortalAddress {endpoint.Address} -TargetPortalPortNumber {endpoint.Port} | Out-Null; " +
              $"Connect-IscsiTarget -NodeAddress '{iqn}' -TargetPortalAddress {endpoint.Address} -TargetPortalPortNumber {endpoint.Port} | Out-Null"
            : $"Disconnect-IscsiTarget -NodeAddress '{iqn}' -Confirm:$false -ErrorAction SilentlyContinue; " +
              $"Remove-IscsiTargetPortal -TargetPortalAddress {endpoint.Address} -TargetPortalPortNumber {endpoint.Port} -Confirm:$false -ErrorAction SilentlyContinue";
        var psi = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, RedirectStandardError = true };
        foreach (var a in new[] { "-NoProfile", "-NonInteractive", "-Command", script }) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi);
        var err = p.StandardError.ReadToEnd();
        p.WaitForExit();
        if (p.ExitCode != 0 && attach) Console.Error.WriteLine($"s3backer: could not attach the iSCSI initiator (needs an administrator): {err.Trim()}");
        return p.ExitCode == 0;
    }

    // ---- an iSCSI target session (RFC 7143), one LUN of 512-byte sectors ----

    sealed class Session(Device device, string iqn, IPEndPoint portal, bool debug)
    {
        const int Sector = 512;
        uint statSn, expCmdSn = 1;
        int maxRecv = 8192, maxBurst = 262144;
        bool fullFeature, discovery;
        Stream s;
        byte[] sense;

        public void Run(Stream stream)
        {
            s = stream;
            var bhs = new byte[48];
            while (Fill(bhs))
            {
                var ahs = bhs[4] * 4;
                var dataLen = (bhs[5] << 16) | (bhs[6] << 8) | bhs[7];
                if (ahs > 0) Fill(new byte[ahs]);
                var data = new byte[dataLen];
                Fill(data);
                var pad = (4 - dataLen % 4) % 4;
                if (pad > 0) Fill(new byte[pad]);
                var opcode = bhs[0] & 0x3f;
                var immediate = (bhs[0] & 0x40) != 0;
                var cmdSn = BinaryPrimitives.ReadUInt32BigEndian(bhs.AsSpan(24));
                if (!immediate && opcode is 0x01 or 0x02 or 0x04 or 0x06 or 0x00 && opcode != 0x03) expCmdSn = Math.Max(expCmdSn, cmdSn + 1);
                switch (opcode)
                {
                    case 0x03: Login(bhs, data); break;
                    case 0x04: Text(bhs, data); break;
                    case 0x00: NopOut(bhs, data); break;
                    case 0x01: ScsiCommand(bhs, data); break;
                    case 0x02: Respond(0x22, bhs, b => { b[1] = 0x80; b[2] = 0; }); break;   // task management: function complete
                    case 0x06: Respond(0x26, bhs, b => b[1] = 0x80); return;
                    case 0x05: break;   // unsolicited Data-Out is never asked for (InitialR2T=Yes)
                    default: Reject(bhs, 0x04); break;
                }
            }
        }

        bool Fill(byte[] buffer)
        {
            var got = 0;
            while (got < buffer.Length)
            {
                var n = s.Read(buffer, got, buffer.Length - got);
                if (n == 0) return false;
                got += n;
            }
            return true;
        }

        void SendPdu(byte[] bhs, ReadOnlySpan<byte> data)
        {
            bhs[5] = (byte)(data.Length >> 16); bhs[6] = (byte)(data.Length >> 8); bhs[7] = (byte)data.Length;
            BinaryPrimitives.WriteUInt32BigEndian(bhs.AsSpan(28), expCmdSn);
            BinaryPrimitives.WriteUInt32BigEndian(bhs.AsSpan(32), expCmdSn + 31);
            s.Write(bhs);
            if (data.Length > 0)
            {
                s.Write(data);
                var pad = (4 - data.Length % 4) % 4;
                if (pad > 0) s.Write(new byte[pad]);
            }
        }

        // A response PDU echoing the request's ITT, with the next StatSN.
        void Respond(byte opcode, byte[] req, Action<byte[]> fill, byte[] data = null)
        {
            var b = new byte[48];
            b[0] = opcode;
            req.AsSpan(16, 4).CopyTo(b.AsSpan(16));
            BinaryPrimitives.WriteUInt32BigEndian(b.AsSpan(24), statSn++);
            fill(b);
            SendPdu(b, data ?? []);
        }

        void Reject(byte[] req, byte reason)
        {
            var b = new byte[48];
            b[0] = 0x3f; b[1] = 0x80; b[2] = reason;
            BinaryPrimitives.WriteUInt32BigEndian(b.AsSpan(16), 0xffffffff);
            BinaryPrimitives.WriteUInt32BigEndian(b.AsSpan(24), statSn++);
            SendPdu(b, req);
        }

        static Dictionary<string, string> Keys(byte[] data) =>
            Encoding.ASCII.GetString(data).Split('\0', StringSplitOptions.RemoveEmptyEntries)
                .Select(kv => kv.Split('=', 2)).Where(p => p.Length == 2).GroupBy(p => p[0]).ToDictionary(g => g.Key, g => g.Last()[1]);

        static byte[] Pairs(IEnumerable<(string, string)> kvs) => Encoding.ASCII.GetBytes(string.Concat(kvs.Select(kv => $"{kv.Item1}={kv.Item2}\0")));

        void Login(byte[] req, byte[] data)
        {
            var keys = Keys(data);
            expCmdSn = BinaryPrimitives.ReadUInt32BigEndian(req.AsSpan(24));   // the first command reuses the login's CmdSN
            var transit = (req[1] & 0x80) != 0;
            var csg = (req[1] >> 2) & 3;
            var nsg = req[1] & 3;
            if (keys.TryGetValue("SessionType", out var type)) discovery = type == "Discovery";
            if (!discovery && keys.TryGetValue("TargetName", out var target) && target != iqn)
            {
                Respond(0x23, req, b => { b[1] = (byte)(csg << 2); req.AsSpan(8, 6).CopyTo(b.AsSpan(8)); b[36] = 2; b[37] = 3; });   // target not found
                return;
            }
            var answer = new List<(string, string)>();
            foreach (var (k, v) in keys)
            {
                switch (k)
                {
                    case "AuthMethod": answer.Add((k, "None")); break;
                    case "HeaderDigest" or "DataDigest": answer.Add((k, "None")); break;
                    case "MaxRecvDataSegmentLength":
                        maxRecv = Math.Clamp(int.TryParse(v, out var m) ? m : 8192, 512, 1 << 20);
                        answer.Add((k, "262144"));
                        break;
                    case "MaxConnections": answer.Add((k, "1")); break;
                    case "InitialR2T": answer.Add((k, "Yes")); break;
                    case "ImmediateData": answer.Add((k, "No")); break;
                    case "MaxBurstLength":
                        maxBurst = Math.Clamp(int.TryParse(v, out var mb) ? mb : 262144, 512, 262144);
                        answer.Add((k, maxBurst.ToString()));
                        break;
                    case "FirstBurstLength": answer.Add((k, Math.Min(int.TryParse(v, out var fb) ? fb : 65536, 65536).ToString())); break;
                    case "DefaultTime2Wait": answer.Add((k, "2")); break;
                    case "DefaultTime2Retain": answer.Add((k, "0")); break;
                    case "MaxOutstandingR2T": answer.Add((k, "1")); break;
                    case "DataPDUInOrder" or "DataSequenceInOrder": answer.Add((k, "Yes")); break;
                    case "ErrorRecoveryLevel": answer.Add((k, "0")); break;
                    case "IFMarker" or "OFMarker": answer.Add((k, "No")); break;
                    case "InitiatorName" or "InitiatorAlias" or "SessionType" or "TargetName": break;
                    default: answer.Add((k, "NotUnderstood")); break;
                }
            }
            if (csg == 0) answer.Add(("TargetPortalGroupTag", "1"));
            if (transit && nsg == 3) fullFeature = true;
            Respond(0x23, req, b =>
            {
                b[1] = (byte)((transit ? 0x80 : 0) | (csg << 2) | (transit ? nsg : 0));
                req.AsSpan(8, 6).CopyTo(b.AsSpan(8));             // ISID
                BinaryPrimitives.WriteUInt16BigEndian(b.AsSpan(14), 1);   // TSIH
            }, Pairs(answer));
        }

        void Text(byte[] req, byte[] data)
        {
            var keys = Keys(data);
            var answer = new List<(string, string)>();
            if (keys.ContainsKey("SendTargets"))
            {
                answer.Add(("TargetName", iqn));
                answer.Add(("TargetAddress", $"{portal.Address}:{portal.Port},1"));
            }
            Respond(0x24, req, b => { b[1] = 0x80; BinaryPrimitives.WriteUInt32BigEndian(b.AsSpan(20), 0xffffffff); }, Pairs(answer));
        }

        void NopOut(byte[] req, byte[] data)
        {
            if (BinaryPrimitives.ReadUInt32BigEndian(req.AsSpan(16)) == 0xffffffff) return;   // a ping that wants no answer
            Respond(0x20, req, b => { b[1] = 0x80; req.AsSpan(8, 8).CopyTo(b.AsSpan(8)); BinaryPrimitives.WriteUInt32BigEndian(b.AsSpan(20), 0xffffffff); }, data);
        }

        void ScsiCommand(byte[] req, byte[] immediate)
        {
            if (!fullFeature || discovery) { Reject(req, 0x0b); return; }
            var cdb = req.AsSpan(32, 16).ToArray();
            var expected = BinaryPrimitives.ReadUInt32BigEndian(req.AsSpan(20));
            var lun = req.AsSpan(8, 8).ToArray();
            if (lun.Any(b => b != 0) && cdb[0] is not (0xa0 or 0x12 or 0x03)) { CheckCondition(req, 0x05, 0x25, 0); return; }   // LUN not supported
            try
            {
                switch (cdb[0])
                {
                    case 0x00 or 0x1b or 0x1e or 0x35 or 0x91 or 0x2f or 0x15 or 0x55: Good(req); break;   // TUR, start/stop, prevent, sync cache, verify, mode select
                    case 0x03: DataIn(req, expected, RequestSense()); break;
                    case 0x12: DataIn(req, expected, Inquiry(cdb, lun.Any(b => b != 0))); break;
                    case 0x1a: DataIn(req, expected, [3, 0, (byte)(device.Size > 0 && IsReadOnly ? 0x80 : 0), 0]); break;
                    case 0x5a: DataIn(req, expected, [0, 6, 0, (byte)(IsReadOnly ? 0x80 : 0), 0, 0, 0, 0]); break;
                    case 0x25:
                    {
                        var last = device.Size / Sector - 1;
                        var r = new byte[8];
                        BinaryPrimitives.WriteUInt32BigEndian(r, (uint)Math.Min(last, uint.MaxValue));
                        BinaryPrimitives.WriteUInt32BigEndian(r.AsSpan(4), Sector);
                        DataIn(req, expected, r);
                        break;
                    }
                    case 0x9e when (cdb[1] & 0x1f) == 0x10:
                    {
                        var r = new byte[32];
                        BinaryPrimitives.WriteUInt64BigEndian(r, (ulong)(device.Size / Sector - 1));
                        BinaryPrimitives.WriteUInt32BigEndian(r.AsSpan(8), Sector);
                        r[14] = 0x80;   // thin provisioning: UNMAP supported
                        DataIn(req, expected, r);
                        break;
                    }
                    case 0xa0:
                    {
                        var r = new byte[16];
                        BinaryPrimitives.WriteUInt32BigEndian(r, 8);
                        DataIn(req, expected, r);
                        break;
                    }
                    case 0x08 or 0x28 or 0x88:
                    {
                        var (lba, count) = Extent(cdb);
                        if (!InRange(req, lba, count)) break;
                        var buffer = new byte[count * Sector];
                        device.Read(lba * Sector, buffer);
                        DataIn(req, expected, buffer);
                        break;
                    }
                    case 0x0a or 0x2a or 0x8a:
                    {
                        var (lba, count) = Extent(cdb);
                        if (!InRange(req, lba, count)) break;
                        if (IsReadOnly) { CheckCondition(req, 0x07, 0x27, 0); break; }
                        var buffer = DataOut(req, count * Sector);
                        if (buffer == null) return;
                        device.Write(lba * Sector, buffer);
                        Good(req);
                        break;
                    }
                    case 0x42:
                    {
                        var param = DataOut(req, (int)expected);
                        if (param == null) return;
                        for (int at = 8; at + 16 <= param.Length; at += 16)
                        {
                            var lba = (long)BinaryPrimitives.ReadUInt64BigEndian(param.AsSpan(at));
                            var count = BinaryPrimitives.ReadUInt32BigEndian(param.AsSpan(at + 8));
                            if (lba >= 0 && (lba + count) * Sector <= device.Size) device.Discard(lba * Sector, (long)count * Sector);
                        }
                        Good(req);
                        break;
                    }
                    default:
                        CheckCondition(req, 0x05, 0x20, 0);   // ILLEGAL REQUEST, invalid command operation code
                        break;
                }
            }
            catch (IOException e)
            {
                if (debug) Console.Error.WriteLine($"s3backer: SCSI 0x{cdb[0]:x2}: {e.Message}");
                CheckCondition(req, 0x03, 0x11, 0);   // MEDIUM ERROR, unrecovered read error
            }
            catch (StoreException e)
            {
                Console.Error.WriteLine($"s3backer: {e.Message}");
                CheckCondition(req, 0x03, 0x11, 0);
            }
        }

        bool IsReadOnly => device.ReadOnly;

        static (long Lba, int Count) Extent(byte[] cdb) => cdb[0] switch
        {
            0x08 or 0x0a => ((long)(((cdb[1] & 0x1f) << 16) | (cdb[2] << 8) | cdb[3]), cdb[4] == 0 ? 256 : cdb[4]),
            0x28 or 0x2a => (BinaryPrimitives.ReadUInt32BigEndian(cdb.AsSpan(2)), BinaryPrimitives.ReadUInt16BigEndian(cdb.AsSpan(7))),
            _ => ((long)BinaryPrimitives.ReadUInt64BigEndian(cdb.AsSpan(2)), (int)BinaryPrimitives.ReadUInt32BigEndian(cdb.AsSpan(10))),
        };

        bool InRange(byte[] req, long lba, int count)
        {
            if (lba >= 0 && count >= 0 && (lba + count) * Sector <= device.Size) return true;
            CheckCondition(req, 0x05, 0x21, 0);   // LOGICAL BLOCK ADDRESS OUT OF RANGE
            return false;
        }

        byte[] Inquiry(byte[] cdb, bool badLun)
        {
            if ((cdb[1] & 1) == 0)
            {
                var r = new byte[36];
                r[0] = (byte)(badLun ? 0x7f : 0x00);   // direct-access block device (or no LUN)
                r[2] = 0x06; r[3] = 0x02; r[4] = 31; r[7] = 0x02;
                Encoding.ASCII.GetBytes("s3backer").CopyTo(r, 8);
                Encoding.ASCII.GetBytes("S3 block device ").CopyTo(r, 16);
                Encoding.ASCII.GetBytes(Version.PadRight(4)[..4]).CopyTo(r, 32);
                return r;
            }
            var serial = Encoding.ASCII.GetBytes(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(iqn)))[..16]);
            switch (cdb[2])
            {
                case 0x00: return [0, 0, 0, 4, 0x00, 0x80, 0x83, 0xb0];
                case 0x80: return [0, 0x80, 0, (byte)serial.Length, .. serial];
                case 0x83:
                {
                    // An NAA-5 identifier from the target name, and the SCSI name string (NUL-padded to 4 bytes).
                    var name = Encoding.ASCII.GetBytes(iqn + new string(' ', 4 - iqn.Length % 4));
                    var naa = new byte[8];
                    SHA256.HashData(Encoding.UTF8.GetBytes(iqn)).AsSpan(0, 8).CopyTo(naa);
                    naa[0] = (byte)(0x50 | (naa[0] & 0x0f));
                    var len = 4 + 8 + 4 + name.Length;
                    return [0, 0x83, (byte)(len >> 8), (byte)len, 0x01, 0x03, 0, 8, .. naa, 0x03, 0x08, 0, (byte)name.Length, .. name];
                }
                case 0xb0:
                {
                    var r = new byte[64];
                    r[1] = 0xb0; r[3] = 0x3c;
                    BinaryPrimitives.WriteUInt32BigEndian(r.AsSpan(8), 65535);    // maximum transfer length (sectors)
                    BinaryPrimitives.WriteUInt32BigEndian(r.AsSpan(20), 0xffffffff);   // maximum unmap LBA count
                    BinaryPrimitives.WriteUInt32BigEndian(r.AsSpan(24), 64);           // maximum unmap descriptors
                    return r;
                }
                default: return [0, cdb[2], 0, 0];
            }
        }

        byte[] RequestSense()
        {
            var r = new byte[18];
            r[0] = 0x70; r[7] = 10;
            if (sense != null) { r[2] = sense[0]; r[12] = sense[1]; r[13] = sense[2]; sense = null; }
            return r;
        }

        void Good(byte[] req) => Respond(0x21, req, b => { b[1] = 0x80; });

        void CheckCondition(byte[] req, byte key, byte asc, byte ascq)
        {
            sense = [key, asc, ascq];
            var data = new byte[2 + 18];
            data[1] = 18;
            data[2] = 0x70; data[4] = key; data[9] = 10; data[14] = asc; data[15] = ascq;
            Respond(0x21, req, b => { b[1] = 0x80; b[3] = 0x02; }, data);   // CHECK CONDITION with sense data
        }

        // Read data back in Data-In PDUs no larger than the initiator accepts; status rides on the last.
        void DataIn(byte[] req, uint expected, byte[] data)
        {
            var length = (int)Math.Min(expected, (uint)data.Length);
            var dataSn = 0u;
            var offset = 0;
            do
            {
                var len = Math.Min(maxRecv, length - offset);
                var last = offset + len >= length;
                var b = new byte[48];
                b[0] = 0x25;
                req.AsSpan(8, 8).CopyTo(b.AsSpan(8));
                req.AsSpan(16, 4).CopyTo(b.AsSpan(16));
                BinaryPrimitives.WriteUInt32BigEndian(b.AsSpan(20), 0xffffffff);
                BinaryPrimitives.WriteUInt32BigEndian(b.AsSpan(36), dataSn++);
                BinaryPrimitives.WriteUInt32BigEndian(b.AsSpan(40), (uint)offset);
                if (last)
                {
                    b[1] = 0x81;   // final, status included
                    BinaryPrimitives.WriteUInt32BigEndian(b.AsSpan(24), statSn++);
                    if (expected > data.Length) { b[1] |= 0x02; BinaryPrimitives.WriteUInt32BigEndian(b.AsSpan(44), expected - (uint)data.Length); }   // underflow
                }
                SendPdu(b, data.AsSpan(offset, len));
                offset += len;
            } while (offset < length);
        }

        // Write data comes after R2Ts (InitialR2T=Yes, no immediate data); null if the link dropped.
        byte[] DataOut(byte[] req, int length)
        {
            var buffer = new byte[length];
            var received = 0;
            var r2tSn = 0u;
            var burst = maxBurst;
            var ttt = 0x1000u;
            while (received < length)
            {
                var want = Math.Min(burst, length - received);
                var r2t = new byte[48];
                r2t[0] = 0x31; r2t[1] = 0x80;
                req.AsSpan(8, 8).CopyTo(r2t.AsSpan(8));
                req.AsSpan(16, 4).CopyTo(r2t.AsSpan(16));
                BinaryPrimitives.WriteUInt32BigEndian(r2t.AsSpan(20), ttt);
                BinaryPrimitives.WriteUInt32BigEndian(r2t.AsSpan(24), statSn);
                BinaryPrimitives.WriteUInt32BigEndian(r2t.AsSpan(36), r2tSn++);
                BinaryPrimitives.WriteUInt32BigEndian(r2t.AsSpan(40), (uint)received);
                BinaryPrimitives.WriteUInt32BigEndian(r2t.AsSpan(44), (uint)want);
                SendPdu(r2t, []);
                var got = 0;
                while (got < want)
                {
                    var bhs = new byte[48];
                    if (!Fill(bhs)) return null;
                    var dataLen = (bhs[5] << 16) | (bhs[6] << 8) | bhs[7];
                    if (bhs[4] > 0) Fill(new byte[bhs[4] * 4]);
                    var data = new byte[dataLen + (4 - dataLen % 4) % 4];
                    if (!Fill(data)) return null;
                    if ((bhs[0] & 0x3f) == 0x00) { NopOut(bhs, data[..dataLen]); continue; }
                    if ((bhs[0] & 0x3f) != 0x05) throw new InvalidDataException($"expected Data-Out, got opcode 0x{bhs[0] & 0x3f:x2}");
                    var offset = (int)BinaryPrimitives.ReadUInt32BigEndian(bhs.AsSpan(40));
                    if (offset < 0 || offset + dataLen > length) throw new InvalidDataException("Data-Out outside the transfer");
                    data.AsSpan(0, dataLen).CopyTo(buffer.AsSpan(offset));
                    got += dataLen;
                }
                received += want;
                ttt++;
            }
            return buffer;
        }
    }
}
