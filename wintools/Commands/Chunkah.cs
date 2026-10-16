using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WinTools;

// chunkah: build an OCI image with content-based layers from a flat root file system. Files are
// grouped into components (a package and its files, from the rpm database (rpmdb.sqlite, read with
// Windows' own SQLite), pacman's local database, dpkg's file lists or apk's installed database;
// every file of 1 MiB or more on its own; everything else in chunkah/unclaimed), the components are
// packed into at most --max-layers layers (the biggest get their own), and the result is written
// as an OCI archive or OCI directory layout with chunkah's per-layer annotations. A root file system
// on a Windows volume has no Unix owners or modes, so entries are owned by root, directories are
// 0755, files 0644 (0755 for ELF binaries and #! scripts), and symbolic links (NTFS or WSL) stay
// links. Extended attributes (user.component) don't exist on NTFS and aren't read.
static class Chunkah
{
    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("chunkah", "Build OCI images with content-based layers from a root file system", Run);

    const string Version = "0.7.1";
    const long BigFile = 1024 * 1024;
    const string Unclaimed = "chunkah/unclaimed";

    sealed class Options
    {
        public string Rootfs, Output, ConfigFile, ConfigStr, Arch, Tag;
        public int MaxLayers = 64, Level = 6;
        public bool Compressed, SkipSpecial;
        public long? Epoch;
        public List<string> Labels = new(), Annotations = new(), Prune = new();
    }

    sealed class Entry
    {
        public string Path;          // relative, forward slashes, no leading slash
        public string Full;
        public bool Directory;
        public string LinkTarget;
        public long Size;
        public DateTimeOffset Modified;
    }

    static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            Console.WriteLine("An OCI building tool for content-based layers\n\nUsage: chunkah [OPTIONS] <COMMAND>\n\nCommands:\n  build  Build an OCI image from a rootfs\n  help   Print this message or the help of the given subcommand(s)\n\nOptions:\n  -v, --verbose...  Increase verbosity\n  -h, --help        Print help\n  -V, --version     Print version");
            return args.Length == 0 ? 2 : 0;
        }
        if (args[0] is "-V" or "--version") { Console.WriteLine($"chunkah {Version}"); return 0; }
        var i = 0;
        while (i < args.Length && args[i] is "-v" or "--verbose" or "-vv" or "-vvv") i++;
        if (i >= args.Length || args[i] != "build") throw new UsageException($"unrecognized subcommand '{(i < args.Length ? args[i] : "")}'");
        var o = new Options();
        for (i++; i < args.Length; i++)
        {
            var a = args[i];
            var eq = a.StartsWith("--") ? a.IndexOf('=') : -1;
            var name = eq > 0 ? a[..eq] : a;
            string Value() => eq > 0 ? a[(eq + 1)..] : ++i < args.Length ? args[i] : throw new UsageException($"a value is required for '{name}'");
            switch (name)
            {
                case "-h" or "--help": BuildUsage(); return 0;
                case "--rootfs": o.Rootfs = Value(); break;
                case "-o" or "--output": o.Output = Value(); break;
                case "--max-layers": o.MaxLayers = int.Parse(Value()); break;
                case "--config": o.ConfigFile = Value(); break;
                case "--config-str": o.ConfigStr = Value(); break;
                case "--label": o.Labels.Add(Value()); break;
                case "--annotation": o.Annotations.Add(Value()); break;
                case "--source-date-epoch": o.Epoch = long.Parse(Value()); break;
                case "--compressed": o.Compressed = true; break;
                case "--compression-level": o.Level = int.Parse(Value()); break;
                case "--arch": o.Arch = Value(); break;
                case "--skip-special-files": o.SkipSpecial = true; break;
                case "--prune": o.Prune.Add(Value()); break;
                case "-t" or "--tag": o.Tag = Value(); break;
                case "-T" or "--threads" or "--write-peak-mem" or "--write-components": Value(); break;
                default: throw new UsageException($"unexpected argument '{a}' found");
            }
        }
        o.Rootfs ??= Environment.GetEnvironmentVariable("CHUNKAH_ROOTFS") ?? throw new UsageException("the following required arguments were not provided:\n  --rootfs <ROOTFS>");
        if (o.Epoch == null && long.TryParse(Environment.GetEnvironmentVariable("SOURCE_DATE_EPOCH"), out var epoch)) o.Epoch = epoch;
        if (o.MaxLayers < 1) throw new UsageException("--max-layers must be at least 1");
        if (o.Level is < 0 or > 9) throw new UsageException("--compression-level must be 0-9");
        if (o.ConfigFile != null && o.ConfigStr != null) throw new UsageException("the argument '--config <PATH>' cannot be used with '--config-str'");
        if (!Directory.Exists(o.Rootfs)) throw new InvalidOperationException($"rootfs {o.Rootfs} is not a directory");
        return Build(o);
    }

    static void BuildUsage() => Console.WriteLine("""
        Build an OCI image from a rootfs

        Usage: chunkah build [OPTIONS] --rootfs <ROOTFS>

        Options:
              --rootfs <ROOTFS>                    Path to the rootfs to build from [env: CHUNKAH_ROOTFS]
          -o, --output <[oci:|oci-archive:]PATH>   Output path; oci: for a directory layout, oci-archive: (default) for an archive; stdout if omitted
              --max-layers <MAX_LAYERS>            Maximum number of layers to output [default: 64]
              --config <PATH>                      Read image config from a JSON file (podman/docker inspect's .Config)
              --config-str <JSON>                  Read image config from a JSON string
              --label <KEY=VALUE|KEY-|->           Add or remove a label from the image
              --annotation <KEY=VALUE>             Add an annotation to the image manifest
              --source-date-epoch <EPOCH>          Creation time and maximum mtime [env: SOURCE_DATE_EPOCH]
              --compressed                         Compress layers (and the OCI archive) with gzip
              --compression-level <LEVEL>          Gzip compression level (0-9) [default: 6]
              --arch <ARCH>                        Target architecture for the output image
              --skip-special-files                 Skip special files (sockets, FIFOs, devices)
              --prune <PATH>                       Paths to exclude from the rootfs
          -t, --tag <NAME>                         Tag to apply to the image
          -h, --help                               Print help
        """);

    static int Build(Options o)
    {
        var rootfs = Path.GetFullPath(o.Rootfs);
        var entries = Scan(rootfs, o);
        var owners = Components(rootfs, entries);
        var maxMtime = o.Epoch is long e ? DateTimeOffset.FromUnixTimeSeconds(e) : (DateTimeOffset?)null;

        // Components: each file goes to its claimer; directories follow the files below them.
        var components = new Dictionary<string, List<Entry>>();
        foreach (var entry in entries.Where(x => !x.Directory))
        {
            var component = owners.TryGetValue(entry.Path, out var c) ? c : entry.Size >= BigFile && entry.LinkTarget == null ? "bigfiles/" + entry.Path : Unclaimed;
            if (!components.TryGetValue(component, out var list)) components[component] = list = new List<Entry>();
            list.Add(entry);
        }
        var directories = entries.Where(x => x.Directory).ToList();
        if (components.Count == 0) components[Unclaimed] = new List<Entry>();

        // Packing: the biggest components get their own layers; the rest share the last one.
        var ordered = components.OrderByDescending(kv => kv.Value.Sum(x => x.Size)).ThenBy(kv => kv.Key, StringComparer.Ordinal).ToList();
        var layers = new List<(List<string> Names, List<Entry> Files)>();
        foreach (var (name, files) in ordered)
        {
            if (layers.Count < o.MaxLayers) layers.Add((new List<string> { name }, new List<Entry>(files)));
            else { layers[^1].Names.Add(name); layers[^1].Files.AddRange(files); }
        }

        var work = Path.Combine(Path.GetTempPath(), "chunkah-" + Guid.NewGuid().ToString("N"));
        var blobs = Path.Combine(work, "blobs", "sha256");
        Directory.CreateDirectory(blobs);
        try
        {
            var layerDescriptors = new List<(string Digest, long Size, string DiffId, List<string> Names)>();
            foreach (var (names, files) in layers)
            {
                // Each layer carries the parent directories of its files.
                var dirs = new HashSet<string>(StringComparer.Ordinal);
                foreach (var f in files)
                    for (var p = Parent(f.Path); p != null; p = Parent(p)) dirs.Add(p);
                var content = directories.Where(d => dirs.Contains(d.Path)).Concat(files).OrderBy(x => x.Path, StringComparer.Ordinal).ToList();
                var (digest, size, diffId) = WriteLayer(content, blobs, o, maxMtime);
                layerDescriptors.Add((digest, size, diffId, names));
                Console.Error.WriteLine($"Wrote layer {digest[7..19]} for {(names.Count == 1 ? names[0] : $"{names.Count} components")} ({content.Count} entries)");
            }

            var created = (maxMtime ?? DateTimeOffset.UtcNow).UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var config = ConfigJson(o, layerDescriptors.Select(l => l.DiffId).ToList(), layerDescriptors.Select(l => l.Names).ToList(), created);
            var configDigest = WriteBlob(blobs, config);
            var manifest = ManifestJson(o, configDigest, config.Length, layerDescriptors);
            var manifestDigest = WriteBlob(blobs, manifest);
            File.WriteAllText(Path.Combine(work, "oci-layout"), "{\"imageLayoutVersion\":\"1.0.0\"}");
            File.WriteAllBytes(Path.Combine(work, "index.json"), IndexJson(o, manifestDigest, manifest.Length));
            Emit(work, o);
        }
        finally
        {
            try { Directory.Delete(work, true); }
            catch (IOException) { }
        }
        return 0;
    }

    static string Parent(string path)
    {
        var slash = path.LastIndexOf('/');
        return slash > 0 ? path[..slash] : null;
    }

    // ---- scanning ----

    static List<Entry> Scan(string rootfs, Options o)
    {
        var prune = o.Prune.Select(p => p.Trim('/')).ToList();
        var result = new List<Entry>();
        var stack = new Stack<string>();
        stack.Push(rootfs);
        while (stack.Count > 0)
        {
            var dir = stack.Pop();
            foreach (var path in Directory.EnumerateFileSystemEntries(dir))
            {
                var relative = Path.GetRelativePath(rootfs, path).Replace('\\', '/');
                if (prune.Contains(relative)) continue;
                var info = new FileInfo(path);
                var attributes = info.Attributes;
                var entry = new Entry { Path = relative, Full = path, Modified = info.LastWriteTimeUtc };
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                {
                    var target = LinkTarget(path, (attributes & FileAttributes.Directory) != 0);
                    if (target != null) { entry.LinkTarget = target; result.Add(entry); continue; }
                }
                if ((attributes & FileAttributes.Directory) != 0)
                {
                    entry.Directory = true;
                    if (!prune.Any(p => p.EndsWith('/') && p.TrimEnd('/') == relative)) stack.Push(path);
                    result.Add(entry);
                    continue;
                }
                entry.Size = info.Length;
                result.Add(entry);
            }
        }
        return result;
    }

    // An NTFS symbolic link's target, or a WSL symlink's (stored as a reparse point with a UTF-8 target).
    static string LinkTarget(string path, bool directory)
    {
        try
        {
            FileSystemInfo info = directory ? new DirectoryInfo(path) : new FileInfo(path);
            if (info.LinkTarget is { } target) return target.Replace('\\', '/');
        }
        catch (IOException) { }
        var h = CreateFileW(path, 0x80, 7, 0, 3, 0x02000000 | 0x00200000, 0);
        if (h == -1) return null;
        try
        {
            var buffer = new byte[16 * 1024];
            if (!DeviceIoControl(h, 0x000900A8, 0, 0, buffer, buffer.Length, out var returned, 0) || returned < 12) return null;
            var tag = BitConverter.ToUInt32(buffer, 0);
            if (tag != 0xA000001D) return null;   // IO_REPARSE_TAG_LX_SYMLINK
            var length = BitConverter.ToUInt16(buffer, 4);
            return Encoding.UTF8.GetString(buffer, 12, length - 4);
        }
        finally { CloseHandle(h); }
    }

    // ---- component repositories ----

    static Dictionary<string, string> Components(string rootfs, List<Entry> entries)
    {
        var owners = new Dictionary<string, string>(StringComparer.Ordinal);
        void Claim(string path, string component)
        {
            var p = path.Trim().TrimStart('/').TrimEnd('/');
            if (p.Length > 0) owners.TryAdd(p, component);
        }
        // rpm: rpmdb.sqlite's Packages table holds rpm headers.
        foreach (var db in new[] { "usr/lib/sysimage/rpm/rpmdb.sqlite", "var/lib/rpm/rpmdb.sqlite" })
        {
            var full = Path.Combine(rootfs, db);
            if (!File.Exists(full)) continue;
            foreach (var (name, files) in RpmPackages(full))
                foreach (var f in files) Claim(f, "rpm/" + name);
            break;
        }
        // pacman
        var pacman = Path.Combine(rootfs, "var/lib/pacman/local");
        if (Directory.Exists(pacman))
            foreach (var pkg in Directory.GetDirectories(pacman))
            {
                var files = Path.Combine(pkg, "files");
                var desc = Path.Combine(pkg, "desc");
                if (!File.Exists(files)) continue;
                var name = File.Exists(desc) ? DescField(File.ReadAllLines(desc), "%NAME%") ?? Path.GetFileName(pkg) : Path.GetFileName(pkg);
                var inFiles = false;
                foreach (var line in File.ReadAllLines(files))
                {
                    if (line.StartsWith('%')) { inFiles = line == "%FILES%"; continue; }
                    if (inFiles && line.Length > 0) Claim(line, "alpm/" + name);
                }
            }
        // dpkg
        var dpkg = Path.Combine(rootfs, "var/lib/dpkg/info");
        if (Directory.Exists(dpkg))
            foreach (var list in Directory.GetFiles(dpkg, "*.list"))
            {
                var name = Path.GetFileNameWithoutExtension(list).Split(':')[0];
                foreach (var line in File.ReadAllLines(list)) if (line != "/." && line.Length > 0) Claim(line, "deb/" + name);
            }
        // apk
        var apk = Path.Combine(rootfs, "lib/apk/db/installed");
        if (File.Exists(apk))
        {
            string package = null, dir = "";
            foreach (var line in File.ReadAllLines(apk))
            {
                if (line.StartsWith("P:")) { package = line[2..]; dir = ""; }
                else if (line.StartsWith("F:")) dir = line[2..];
                else if (line.StartsWith("R:") && package != null) Claim(dir.Length > 0 ? dir + "/" + line[2..] : line[2..], "apk/" + package);
            }
        }
        // Package databases name canonical paths (/bin/x); a merged-/usr rootfs keeps them under usr/.
        var present = new HashSet<string>(entries.Select(x => x.Path), StringComparer.Ordinal);
        foreach (var (path, component) in owners.ToList())
            foreach (var alias in new[] { "usr/" + path })
                if (!owners.ContainsKey(alias) && present.Contains(alias)) owners[alias] = component;
        return owners;
    }

    static string DescField(string[] lines, string field)
    {
        var at = Array.IndexOf(lines, field);
        return at >= 0 && at + 1 < lines.Length ? lines[at + 1] : null;
    }

    // ---- rpm database (SQLite + rpm header blobs) ----

    static List<(string Name, List<string> Files)> RpmPackages(string path)
    {
        var result = new List<(string, List<string>)>();
        if (sqlite3_open_v2(path, out var db, 1 /* SQLITE_OPEN_READONLY */, 0) != 0) return result;
        try
        {
            if (sqlite3_prepare_v2(db, "SELECT blob FROM Packages", -1, out var stmt, 0) != 0) return result;
            try
            {
                while (sqlite3_step(stmt) == 100 /* SQLITE_ROW */)
                {
                    var length = sqlite3_column_bytes(stmt, 0);
                    var pointer = sqlite3_column_blob(stmt, 0);
                    if (pointer == 0 || length < 8) continue;
                    var blob = new byte[length];
                    Marshal.Copy(pointer, blob, 0, length);
                    if (ParseHeader(blob) is { } package) result.Add(package);
                }
            }
            finally { sqlite3_finalize(stmt); }
        }
        finally { sqlite3_close(db); }
        return result;
    }

    // An rpm header: index count and data size (big-endian), 16-byte index entries, then the data.
    static (string, List<string>)? ParseHeader(byte[] blob)
    {
        int BE(int at) => (blob[at] << 24) | (blob[at + 1] << 16) | (blob[at + 2] << 8) | blob[at + 3];
        var count = BE(0);
        var dataStart = 8 + 16 * count;
        if (count <= 0 || dataStart > blob.Length) return null;
        string name = null;
        List<string> basenames = null, dirnames = null;
        List<int> dirindexes = null;
        List<string> Strings(int offset, int n)
        {
            var list = new List<string>();
            var at = dataStart + offset;
            for (var k = 0; k < n && at < blob.Length; k++)
            {
                var end = Array.IndexOf(blob, (byte)0, at);
                if (end < 0) break;
                list.Add(Encoding.UTF8.GetString(blob, at, end - at));
                at = end + 1;
            }
            return list;
        }
        for (var k = 0; k < count; k++)
        {
            var e = 8 + 16 * k;
            int tag = BE(e), offset = BE(e + 8), n = BE(e + 12);
            switch (tag)
            {
                case 1000: name = Strings(offset, 1).FirstOrDefault(); break;            // RPMTAG_NAME
                case 1117: basenames = Strings(offset, n); break;                        // RPMTAG_BASENAMES
                case 1118: dirnames = Strings(offset, n); break;                         // RPMTAG_DIRNAMES
                case 1116: dirindexes = Enumerable.Range(0, n).Select(x => BE(dataStart + offset + 4 * x)).ToList(); break;   // RPMTAG_DIRINDEXES
            }
        }
        if (name == null) return null;
        var files = new List<string>();
        if (basenames != null && dirnames != null && dirindexes != null)
            for (var k = 0; k < basenames.Count && k < dirindexes.Count; k++)
                if (dirindexes[k] < dirnames.Count) files.Add(dirnames[dirindexes[k]] + basenames[k]);
        return (name, files);
    }

    // ---- layers and blobs ----

    static (string Digest, long Size, string DiffId) WriteLayer(List<Entry> content, string blobs, Options o, DateTimeOffset? maxMtime)
    {
        var temp = Path.Combine(blobs, "layer-" + Guid.NewGuid().ToString("N"));
        string diffId;
        using (var file = File.Create(temp))
        {
            Stream target = file;
            GZipStream gzip = null;
            if (o.Compressed) target = gzip = new GZipStream(file, Level(o.Level), leaveOpen: true);
            using var hashing = new HashingStream(target);
            using (var tar = new TarWriter(hashing, TarEntryFormat.Pax, leaveOpen: true))
            {
                foreach (var e in content)
                {
                    var mtime = maxMtime is { } max && e.Modified > max ? max : e.Modified;
                    PaxTarEntry entry;
                    if (e.Directory) entry = new PaxTarEntry(TarEntryType.Directory, e.Path + "/") { Mode = (UnixFileMode)0b111_101_101 };
                    else if (e.LinkTarget != null) entry = new PaxTarEntry(TarEntryType.SymbolicLink, e.Path) { LinkName = e.LinkTarget, Mode = (UnixFileMode)0b111_111_111 };
                    else
                    {
                        entry = new PaxTarEntry(TarEntryType.RegularFile, e.Path) { Mode = (UnixFileMode)(Executable(e.Full) ? 0b111_101_101 : 0b110_100_100) };
                        entry.DataStream = File.OpenRead(e.Full);
                    }
                    entry.ModificationTime = mtime;
                    entry.Uid = 0;
                    entry.Gid = 0;
                    entry.UserName = "";
                    entry.GroupName = "";
                    tar.WriteEntry(entry);
                    entry.DataStream?.Dispose();
                }
            }
            diffId = "sha256:" + Convert.ToHexStringLower(hashing.Hash());
            gzip?.Dispose();
        }
        var digest = Sha256File(temp);
        var size = new FileInfo(temp).Length;
        File.Move(temp, Path.Combine(blobs, digest), overwrite: true);
        return ("sha256:" + digest, size, diffId);
    }

    static CompressionLevel Level(int level) => level switch
    {
        0 => CompressionLevel.NoCompression,
        <= 3 => CompressionLevel.Fastest,
        <= 6 => CompressionLevel.Optimal,
        _ => CompressionLevel.SmallestSize,
    };

    static bool Executable(string path)
    {
        try
        {
            using var f = File.OpenRead(path);
            Span<byte> magic = stackalloc byte[4];
            var n = f.Read(magic);
            return n >= 2 && magic[0] == '#' && magic[1] == '!' || n == 4 && magic[0] == 0x7f && magic[1] == 'E' && magic[2] == 'L' && magic[3] == 'F';
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    // Passes writes through and hashes them (the uncompressed layer's diff_id).
    sealed class HashingStream(Stream inner) : Stream
    {
        readonly IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        public byte[] Hash() => sha.GetHashAndReset();
        public override void Write(byte[] buffer, int offset, int count) { sha.AppendData(buffer, offset, count); inner.Write(buffer, offset, count); }
        public override void Write(ReadOnlySpan<byte> buffer) { sha.AppendData(buffer); inner.Write(buffer); }
        public override void Flush() => inner.Flush();
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }

    static string Sha256File(string path)
    {
        using var f = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(f));
    }

    static string WriteBlob(string blobs, byte[] data)
    {
        var digest = Convert.ToHexStringLower(SHA256.HashData(data));
        File.WriteAllBytes(Path.Combine(blobs, digest), data);
        return "sha256:" + digest;
    }

    // ---- image JSON ----

    // Media types keep their '+' (the default encoder would write +).
    static readonly JsonWriterOptions JsonOptions = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    static string HostArch() => RuntimeInformation.OSArchitecture switch { Architecture.Arm64 => "arm64", Architecture.X86 => "386", _ => "amd64" };

    static byte[] ConfigJson(Options o, List<string> diffIds, List<List<string>> names, string created)
    {
        JsonDocument baseConfig = null;
        if (o.ConfigFile != null) baseConfig = JsonDocument.Parse(File.ReadAllText(o.ConfigFile));
        else if (o.ConfigStr != null) baseConfig = JsonDocument.Parse(o.ConfigStr);
        var labels = new SortedDictionary<string, string>(StringComparer.Ordinal);
        string configArch = null;
        if (baseConfig != null && baseConfig.RootElement.TryGetProperty("Labels", out var l) && l.ValueKind == JsonValueKind.Object)
            foreach (var kv in l.EnumerateObject()) labels[kv.Name] = kv.Value.GetString();
        if (baseConfig != null && baseConfig.RootElement.TryGetProperty("Architecture", out var arch) && arch.ValueKind == JsonValueKind.String) configArch = arch.GetString();
        foreach (var label in o.Labels)
        {
            if (label == "-") labels.Clear();
            else if (label.EndsWith('-') && !label.Contains('=')) labels.Remove(label[..^1]);
            else if (label.IndexOf('=') is var eq and > 0) labels[label[..eq]] = label[(eq + 1)..];
            else throw new UsageException($"invalid label: {label}");
        }
        using var buffer = new MemoryStream();
        using (var w = new Utf8JsonWriter(buffer, JsonOptions))
        {
            w.WriteStartObject();
            w.WriteString("created", created);
            w.WriteString("architecture", o.Arch ?? configArch ?? HostArch());
            w.WriteString("os", "linux");
            w.WriteStartObject("config");
            if (baseConfig != null)
                foreach (var p in baseConfig.RootElement.EnumerateObject())
                    if (p.Name is not ("Labels" or "Architecture" or "Os")) p.WriteTo(w);
            if (labels.Count > 0)
            {
                w.WriteStartObject("Labels");
                foreach (var (k, v) in labels) w.WriteString(k, v);
                w.WriteEndObject();
            }
            w.WriteEndObject();
            w.WriteStartObject("rootfs");
            w.WriteString("type", "layers");
            w.WriteStartArray("diff_ids");
            foreach (var d in diffIds) w.WriteStringValue(d);
            w.WriteEndArray();
            w.WriteEndObject();
            w.WriteStartArray("history");
            foreach (var n in names)
            {
                w.WriteStartObject();
                w.WriteString("created", created);
                w.WriteString("created_by", "chunkah");
                w.WriteString("comment", n.Count == 1 ? n[0] : $"{n.Count} components");
                w.WriteEndObject();
            }
            w.WriteEndArray();
            w.WriteEndObject();
        }
        baseConfig?.Dispose();
        return buffer.ToArray();
    }

    static byte[] ManifestJson(Options o, string configDigest, long configSize, List<(string Digest, long Size, string DiffId, List<string> Names)> layers)
    {
        using var buffer = new MemoryStream();
        using (var w = new Utf8JsonWriter(buffer, JsonOptions))
        {
            w.WriteStartObject();
            w.WriteNumber("schemaVersion", 2);
            w.WriteString("mediaType", "application/vnd.oci.image.manifest.v1+json");
            w.WriteStartObject("config");
            w.WriteString("mediaType", "application/vnd.oci.image.config.v1+json");
            w.WriteString("digest", configDigest);
            w.WriteNumber("size", configSize);
            w.WriteEndObject();
            w.WriteStartArray("layers");
            foreach (var (digest, size, _, names) in layers)
            {
                w.WriteStartObject();
                w.WriteString("mediaType", o.Compressed ? "application/vnd.oci.image.layer.v1.tar+gzip" : "application/vnd.oci.image.layer.v1.tar");
                w.WriteString("digest", digest);
                w.WriteNumber("size", size);
                w.WriteStartObject("annotations");
                w.WriteString("org.chunkah.component", string.Join(",", names));
                w.WriteEndObject();
                w.WriteEndObject();
            }
            w.WriteEndArray();
            if (o.Annotations.Count > 0)
            {
                w.WriteStartObject("annotations");
                foreach (var a in o.Annotations)
                {
                    var eq = a.IndexOf('=');
                    if (eq <= 0) throw new UsageException($"invalid annotation: {a}");
                    w.WriteString(a[..eq], a[(eq + 1)..]);
                }
                w.WriteEndObject();
            }
            w.WriteEndObject();
        }
        return buffer.ToArray();
    }

    static byte[] IndexJson(Options o, string manifestDigest, long size)
    {
        using var buffer = new MemoryStream();
        using (var w = new Utf8JsonWriter(buffer, JsonOptions))
        {
            w.WriteStartObject();
            w.WriteNumber("schemaVersion", 2);
            w.WriteString("mediaType", "application/vnd.oci.image.index.v1+json");
            w.WriteStartArray("manifests");
            w.WriteStartObject();
            w.WriteString("mediaType", "application/vnd.oci.image.manifest.v1+json");
            w.WriteString("digest", manifestDigest);
            w.WriteNumber("size", size);
            if (o.Tag != null)
            {
                w.WriteStartObject("annotations");
                w.WriteString("org.opencontainers.image.ref.name", o.Tag);
                w.WriteEndObject();
            }
            w.WriteEndObject();
            w.WriteEndArray();
            w.WriteEndObject();
        }
        return buffer.ToArray();
    }

    // oci:DIR copies the layout; oci-archive:FILE (or a bare path, or stdout) tars it.
    static void Emit(string layout, Options o)
    {
        var output = o.Output;
        if (output != null && output.StartsWith("oci:"))
        {
            var dir = output[4..];
            Directory.CreateDirectory(dir);
            foreach (var file in Directory.GetFiles(layout, "*", SearchOption.AllDirectories))
            {
                var target = Path.Combine(dir, Path.GetRelativePath(layout, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target, overwrite: true);
            }
            return;
        }
        if (output != null && output.StartsWith("oci-archive:")) output = output[12..];
        using var stream = output == null || output == "-" ? Console.OpenStandardOutput() : File.Create(output);
        using var compressed = o.Compressed ? new GZipStream(stream, Level(o.Level), leaveOpen: true) : null;
        using var tar = new TarWriter(compressed ?? stream, TarEntryFormat.Pax, leaveOpen: true);
        foreach (var file in new[] { "oci-layout", "index.json" }.Select(f => Path.Combine(layout, f))
                     .Concat(Directory.GetFiles(Path.Combine(layout, "blobs", "sha256")).Order(StringComparer.Ordinal)))
        {
            var name = Path.GetRelativePath(layout, file).Replace('\\', '/');
            var entry = new PaxTarEntry(TarEntryType.RegularFile, name) { Mode = (UnixFileMode)0b110_100_100, ModificationTime = DateTimeOffset.UnixEpoch };
            using var data = File.OpenRead(file);
            entry.DataStream = data;
            tar.WriteEntry(entry);
        }
    }

    // ---- Win32 / SQLite ----

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern nint CreateFileW(string name, uint access, uint share, nint security, uint disposition, uint flags, nint template);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool DeviceIoControl(nint device, uint code, nint input, int inputSize, byte[] output, int outputSize, out int returned, nint overlapped);

    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(nint handle);

    [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.StdCall)]
    static extern int sqlite3_open_v2([MarshalAs(UnmanagedType.LPUTF8Str)] string filename, out nint db, int flags, nint vfs);

    [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.StdCall)]
    static extern int sqlite3_prepare_v2(nint db, [MarshalAs(UnmanagedType.LPUTF8Str)] string sql, int length, out nint stmt, nint tail);

    [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.StdCall)]
    static extern int sqlite3_step(nint stmt);

    [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.StdCall)]
    static extern nint sqlite3_column_blob(nint stmt, int column);

    [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.StdCall)]
    static extern int sqlite3_column_bytes(nint stmt, int column);

    [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.StdCall)]
    static extern int sqlite3_finalize(nint stmt);

    [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.StdCall)]
    static extern int sqlite3_close(nint db);
}
