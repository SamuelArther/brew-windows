using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace WinTools;

// avfsd / mountavfs / umountavfs: AVFS, a virtual file system for looking inside archives, on the
// Projected File System. The mount (~/.avfs by default, or $AVFSBASE) mirrors every drive as a
// folder (C, D, ...), and appending # to an archive's name opens it: ~/.avfs/C/Users/me/src.zip#/
// lists the zip, ~/.avfs/C/logs/app.log.gz# is the decompressed log. Handlers: zip/jar (uzip), tar,
// tar.gz/tgz (utar, ugz) and single .gz files; read-only. Note that ProjFS keeps what was read in the
// mount folder (its local cache) until it is deleted.
static class Avfs
{
    const string Version = "1.4.0";

    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("avfsd", "AVFS daemon: browse inside archives through a mounted folder (ProjFS)", Avfsd);
        Commands.Add("mountavfs", "Mount AVFS on ~/.avfs (look inside archives with file.zip#)", Mountavfs);
        Commands.Add("umountavfs", "Unmount AVFS from ~/.avfs", Umountavfs);
    }

    static string Base => Environment.GetEnvironmentVariable("AVFSBASE") is { Length: > 0 } b ? Path.GetFullPath(b)
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".avfs");

    const string Help = """
        usage: avfsd [options] mountpoint
               avfsd -u mountpoint

        AVFS: every drive appears as a folder under mountpoint, and a trailing # opens an archive:
          mountpoint/C/Users/me/backup.tar.gz#/etc/hosts
        Handlers: zip and jar (#uzip), tar and tar.gz/tgz (#utar, #ugz), single .gz files (#ugz).

        options:
            -h, --help          print help
            -V, --version       print version
            -f                  foreground operation
            -d                  debug (implies -f)
            -o opt,[opt...]     mount options (accepted for compatibility)
            -u                  unmount
        Needs the Windows Projected File System (the Client-ProjFS optional feature).
        """;

    static int Avfsd(string[] args)
    {
        var positional = new List<string>();
        var foreground = false;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help": Console.WriteLine(Help); return 0;
                case "-V" or "--version": Console.WriteLine($"AVFS version {Version}\nFUSE library version: ProjFS (Windows Projected File System)"); return 0;
                case "-u":
                    if (++i >= args.Length) throw new UsageException("-u needs a mount point");
                    return Projfs.Unmount("avfsd", args[i]);
                case "-f" or "-d" or "--debug": foreground = true; break;
                case "-o": if (++i >= args.Length) throw new UsageException("-o needs options"); break;
                default:
                    if (args[i].StartsWith("-o")) break;
                    if (args[i].StartsWith('-')) throw new UsageException($"unknown option {args[i]}");
                    positional.Add(args[i]);
                    break;
            }
        }
        if (positional.Count != 1) throw new UsageException("missing mountpoint (see avfsd --help)");
        return Serve(positional[0], foreground, args.Where(a => a is not ("-f" or "-d" or "--debug")).ToArray());
    }

    static int Serve(string target, bool foreground, string[] childArgs)
    {
        var mountpoint = Projfs.MountPath(target);
        if (Projfs.Unavailable() is { } why) { Console.Error.WriteLine("avfsd: " + why); return 1; }
        Directory.CreateDirectory(mountpoint);
        return Projfs.Mount("avfsd", mountpoint, mountpoint, junction: false, foreground, childArgs, () => new Source(), clean: true);
    }

    static int Mountavfs(string[] args)
    {
        if (args.Length > 0 && args[0] is "-h" or "--help") { Console.WriteLine("usage: mountavfs\nMounts AVFS on $AVFSBASE (default ~/.avfs)."); return 0; }
        if (Projfs.Load(Projfs.MountPath(Base)) != null) { Console.WriteLine($"AVFS already mounted on {Base}"); return 0; }
        Console.WriteLine($"Mounting AVFS on {Base}...");
        return Serve(Base, false, [Base]);
    }

    static int Umountavfs(string[] args)
    {
        if (args.Length > 0 && args[0] is "-h" or "--help") { Console.WriteLine("usage: umountavfs\nUnmounts AVFS from $AVFSBASE (default ~/.avfs)."); return 0; }
        if (Projfs.Load(Projfs.MountPath(Base)) == null) { Console.WriteLine($"AVFS not mounted on {Base}"); return 0; }
        Console.WriteLine($"Unmounting AVFS from {Base}...");
        return Projfs.Unmount("umountavfs", Base);
    }

    // ---- the projection ----

    internal sealed class Source : ProjfsSource, IDisposable
    {
        readonly Dictionary<string, AvfsArchive> archives = new(StringComparer.OrdinalIgnoreCase);

        public override bool ReadOnly => true;

        public void Dispose()
        {
            lock (archives) foreach (var a in archives.Values) a.Dispose();
        }

        // "C\dir\a.zip#\x\y" -> (C:\dir\a.zip, archive, "x/y"); "C\dir" -> (C:\dir, null, null).
        (string Real, AvfsArchive Archive, string Inner) Resolve(string path)
        {
            var parts = path.Split('\\', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return (null, null, null);
            if (parts[0].Length != 1 || !char.IsLetter(parts[0][0])) return ("?", null, null);
            var real = parts[0].ToUpperInvariant() + ":\\";
            for (var i = 1; i < parts.Length; i++)
            {
                var hash = parts[i].IndexOf('#');
                if (hash > 0)
                {
                    var file = Path.Combine(real, parts[i][..hash]);
                    if (File.Exists(file))
                    {
                        var archive = Open(file, parts[i][(hash + 1)..]);
                        if (archive == null) return ("?", null, null);
                        return (file, archive, string.Join('/', parts[(i + 1)..]));
                    }
                }
                real = Path.Combine(real, parts[i]);
            }
            return (real, null, null);
        }

        AvfsArchive Open(string file, string handler)
        {
            var key = file + "#" + handler + "|" + File.GetLastWriteTimeUtc(file).Ticks;
            lock (archives)
            {
                if (archives.TryGetValue(key, out var a)) return a;
                a = AvfsArchive.Open(file, handler);
                if (a != null) archives[key] = a;
                return a;
            }
        }

        public override List<ProjfsEntry> List(string dir)
        {
            if (dir.Length == 0)
                return DriveInfo.GetDrives().Where(d => d.IsReady).Select(d => new ProjfsEntry(d.Name[..1], true, 0, DateTime.UtcNow)).ToList();
            var (real, archive, inner) = Resolve(dir);
            if (archive != null) return archive.List(inner);
            if (real == null || !Directory.Exists(real)) return null;
            try { return new DirectoryInfo(real).EnumerateFileSystemInfos().Select(Entry).ToList(); }
            catch (UnauthorizedAccessException) { return []; }
        }

        public override ProjfsEntry Stat(string path)
        {
            var name = path[(path.LastIndexOf('\\') + 1)..];
            if (!path.Contains('\\') && name.Length == 1 && char.IsLetter(name[0]))
                return Directory.Exists(name + ":\\") ? new ProjfsEntry(name.ToUpperInvariant(), true, 0, DateTime.UtcNow) : null;
            var (real, archive, inner) = Resolve(path);
            if (archive != null)
            {
                var e = archive.Stat(inner);
                // The archive itself ("a.zip#") keeps the name it was asked for.
                return e == null ? null : inner.Length == 0 ? new ProjfsEntry(name, e.IsDir, e.Size, e.Written) : e;
            }
            if (real == null || real == "?") return null;
            if (Directory.Exists(real)) return Entry(new DirectoryInfo(real));
            if (File.Exists(real)) return Entry(new FileInfo(real));
            return null;
        }

        public override int Read(string path, long offset, Span<byte> buffer)
        {
            var (real, archive, inner) = Resolve(path);
            if (archive != null) return archive.Read(inner, offset, buffer);
            using var file = new FileStream(real, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            file.Position = offset;
            var total = 0;
            while (total < buffer.Length && file.Read(buffer[total..]) is var n and > 0) total += n;
            return total;
        }

        static ProjfsEntry Entry(FileSystemInfo info) =>
            new(info.Name, info is DirectoryInfo, info is FileInfo f ? f.Length : 0, info.LastWriteTimeUtc);
    }
}

// One opened archive: its listing, and its members' contents on demand. Compressed tars and .gz
// files are decompressed once into a temporary file; zip members are inflated when first read.
sealed class AvfsArchive : IDisposable
{
    sealed class Member
    {
        public string Path;
        public bool IsDir;
        public long Size;
        public DateTime Written;
        public int Ordinal = -1;        // tar: position in the archive
    }

    readonly string file;
    readonly string kind;               // zip, tar, gz
    string temp;                        // decompressed tar or gz payload
    readonly Dictionary<string, Member> members = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, List<string>> children = new(StringComparer.OrdinalIgnoreCase);
    readonly object gate = new();
    (string Path, byte[] Data) cached;

    AvfsArchive(string file, string kind)
    {
        this.file = file;
        this.kind = kind;
    }

    public static AvfsArchive Open(string file, string handler)
    {
        var lower = file.ToLowerInvariant();
        var kind = handler switch
        {
            "uzip" => "zip",
            "utar" => lower.EndsWith(".gz") || lower.EndsWith(".tgz") ? "tgz" : "tar",
            "ugz" or "ugzip" or "gz" => lower.EndsWith(".tar.gz") || lower.EndsWith(".tgz") ? "tgz" : "gz",
            "" => lower.EndsWith(".zip") || lower.EndsWith(".jar") || lower.EndsWith(".war") || lower.EndsWith(".apk") || lower.EndsWith(".nupkg") || lower.EndsWith(".whl") ? "zip"
                : lower.EndsWith(".tar.gz") || lower.EndsWith(".tgz") ? "tgz"
                : lower.EndsWith(".tar") ? "tar"
                : lower.EndsWith(".gz") ? "gz" : null,
            _ => null,
        };
        if (kind == null) return null;
        var archive = new AvfsArchive(file, kind);
        try { archive.Index(); }
        catch (Exception e) when (e is InvalidDataException or IOException or FormatException) { archive.Dispose(); return null; }
        return archive;
    }

    public void Dispose()
    {
        if (temp != null && File.Exists(temp)) File.Delete(temp);
    }

    void Add(Member m)
    {
        m.Path = m.Path.Replace('\\', '/').Trim('/');
        while (m.Path.StartsWith("./")) m.Path = m.Path[2..];
        if (m.Path is "" or ".") return;
        // Parent folders that the archive doesn't list itself.
        var parent = m.Path.Contains('/') ? m.Path[..m.Path.LastIndexOf('/')] : "";
        if (parent.Length > 0 && !members.ContainsKey(parent)) Add(new Member { Path = parent, IsDir = true, Written = m.Written });
        if (!members.ContainsKey(m.Path))
        {
            if (!children.TryGetValue(parent, out var list)) children[parent] = list = [];
            list.Add(m.Path);
        }
        members[m.Path] = m;
    }

    void Index()
    {
        switch (kind)
        {
            case "zip":
                using (var zip = ZipFile.OpenRead(file))
                    foreach (var e in zip.Entries)
                        Add(new Member { Path = e.FullName, IsDir = e.FullName.EndsWith('/'), Size = e.Length, Written = e.LastWriteTime.UtcDateTime });
                break;
            case "gz":
                Decompress();
                break;
            default:
                if (kind == "tgz") Decompress();
                using (var stream = File.OpenRead(temp ?? file))
                using (var tar = new TarReader(stream))
                {
                    var ordinal = 0;
                    while (tar.GetNextEntry() is { } e)
                    {
                        var isDir = e.EntryType == TarEntryType.Directory;
                        if (isDir || e.EntryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile or TarEntryType.ContiguousFile)
                            Add(new Member { Path = e.Name, IsDir = isDir, Size = isDir ? 0 : e.Length, Written = e.ModificationTime.UtcDateTime, Ordinal = ordinal });
                        ordinal++;
                    }
                }
                break;
        }
    }

    void Decompress()
    {
        temp = Path.Combine(Path.GetTempPath(), "wintools-avfs-" + Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(file + File.GetLastWriteTimeUtc(file).Ticks)))[..16]);
        if (!File.Exists(temp))
        {
            using var input = new GZipStream(File.OpenRead(file), CompressionMode.Decompress);
            using var output = File.Create(temp + ".part");
            input.CopyTo(output);
            output.Close();
            File.Move(temp + ".part", temp, overwrite: true);
        }
    }

    public List<ProjfsEntry> List(string inner)
    {
        if (kind == "gz") return null;   // a single compressed file, not a folder
        if (inner.Length > 0 && (!members.TryGetValue(inner, out var m) || !m.IsDir)) return null;
        return children.GetValueOrDefault(inner, []).Select(p => Entry(members[p])).ToList();
    }

    public ProjfsEntry Stat(string inner)
    {
        if (inner.Length == 0)
            return kind == "gz" ? new ProjfsEntry("", false, new FileInfo(temp).Length, File.GetLastWriteTimeUtc(file))
                                : new ProjfsEntry("", true, 0, File.GetLastWriteTimeUtc(file));
        return members.TryGetValue(inner, out var m) ? Entry(m) : null;
    }

    static ProjfsEntry Entry(Member m) => new(m.Path[(m.Path.LastIndexOf('/') + 1)..], m.IsDir, m.Size, m.Written);

    public int Read(string inner, long offset, Span<byte> buffer)
    {
        if (kind == "gz" || kind != "zip" && inner.Length > 0)
        {
            if (kind == "gz")
            {
                using var f = File.OpenRead(temp);
                f.Position = offset;
                return ReadFull(f, buffer);
            }
            // tar members: walk to the member's header, then seek inside its data.
            var m = members[inner];
            using var stream = File.OpenRead(temp ?? file);
            using var tar = new TarReader(stream);
            TarEntry entry = null;
            for (var i = 0; i <= m.Ordinal; i++) entry = tar.GetNextEntry();
            var data = entry?.DataStream;
            if (data == null) return 0;
            data.Seek(offset, SeekOrigin.Begin);
            return ReadFull(data, buffer);
        }
        // zip members: inflated once and kept while they are being read.
        byte[] bytes;
        lock (gate)
        {
            if (!string.Equals(cached.Path, inner, StringComparison.OrdinalIgnoreCase))
            {
                using var zip = ZipFile.OpenRead(file);
                var name = members[inner].Path;
                var e = zip.GetEntry(name) ?? zip.Entries.First(x => x.FullName.Replace('\\', '/').Trim('/') == name);
                using var s = e.Open();
                var ms = new MemoryStream((int)Math.Min(e.Length, int.MaxValue));
                s.CopyTo(ms);
                cached = (inner, ms.ToArray());
            }
            bytes = cached.Data;
        }
        if (offset >= bytes.Length) return 0;
        var n = (int)Math.Min(buffer.Length, bytes.Length - offset);
        bytes.AsSpan((int)offset, n).CopyTo(buffer);
        return n;
    }

    static int ReadFull(Stream s, Span<byte> buffer)
    {
        var total = 0;
        while (total < buffer.Length && s.Read(buffer[total..]) is var n and > 0) total += n;
        return total;
    }
}
