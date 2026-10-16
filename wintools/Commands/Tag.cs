using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// tag: Finder-style file tags on NTFS. macOS keeps them in the com.apple.metadata:_kMDItemUserTags
// extended attribute as a binary plist array ("Red\n6", "work"); here the same plist lives in the
// alternate data stream com.apple.metadata_kMDItemUserTags (':' can't appear in a stream name), so
// `getfattr -d` shows it as user.com.apple.metadata_kMDItemUserTags. Spotlight's --find becomes a
// walk of the search scope.
static class Tag
{
    const string Version = "0.10.0";
    const string Stream = "com.apple.metadata_kMDItemUserTags";

    // Finder's label colors: the index macOS stores after the tag name.
    static readonly string[] ColorNames = ["", "Gray", "Green", "Purple", "Blue", "Yellow", "Red", "Orange"];
    static readonly string[] AnsiColors = ["", "\e[90m", "\e[32m", "\e[35m", "\e[34m", "\e[33m", "\e[31m", "\e[38;5;208m"];

    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("tag", "Manipulate and query Finder-style tags on files (NTFS streams)", Run);

    const string Help = """
        tag - A tool for manipulating and querying file tags.
          usage:
            tag -a | --add <tags> <path>...     Add tags to file
            tag -r | --remove <tags> <path>...  Remove tags from file
            tag -s | --set <tags> <path>...     Set tags on file
            tag -m | --match <tags> <path>...   Display files with matching tags
            tag -f | --find <tags> <path>...    Find all files with tags (-A, -e, -R ignored)
            tag -u | --usage <tags> <path>...   Display tags used, with usage counts
            tag -l | --list <path>...           List the tags on file
          <tags> is a comma-separated list of tag names; use * to match any tag for match and find.
          additional options:
                -v | --version      Display version
                -h | --help         Display this help
                -A | --all          Display invisible files while enumerating
                -e | --enter        Enter and enumerate directories provided
                -R | --recursive    Recursively process directories
                -n | --name         Turn on filename display in output (default)
                -N | --no-name      Turn off filename display in output (list, find, match)
                -t | --tags         Turn on tags display in output (find, match)
                -T | --no-tags      Turn off tags display in output (list)
                -g | --garrulous    Display tags each on own line (list, find, match)
                -G | --no-garrulous Display tags comma-separated after filename (default)
                -c | --color        Display tags in color
                -p | --slash        Terminate each directory name with a slash
                -0 | --nul          Terminate lines with NUL (\0) for use with xargs -0
                     --home         Find tagged files in user home directory
                     --local        Find tagged files in home + local filesystems
                     --network      Find tagged files in home + local + network filesystems
        Tags are stored in the NTFS stream com.apple.metadata_kMDItemUserTags (a binary plist, as on macOS).
        """;

    enum Op { None, Add, Remove, Set, Match, Find, Usage, List }

    sealed class Options
    {
        public Op Op;
        public string TagArg;
        public bool All, Enter, Recursive, Color, Slash, Nul;
        public bool? Name, Tags, Garrulous;
        public int Scope;   // 0 default, 1 home, 2 local, 3 network
        public List<string> Paths = [];
    }

    static int Run(string[] args)
    {
        var o = new Options();
        var options = true;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (options && arg == "--") { options = false; continue; }
            if (!options || !arg.StartsWith('-') || arg == "-") { o.Paths.Add(arg); continue; }
            // Long options, with an optional =value for the operations.
            if (arg.StartsWith("--"))
            {
                var eq = arg.IndexOf('=');
                var name = eq < 0 ? arg[2..] : arg[2..eq];
                string inline = eq < 0 ? null : arg[(eq + 1)..];
                Op op = name switch
                {
                    "add" => Op.Add, "remove" => Op.Remove, "set" => Op.Set, "match" => Op.Match,
                    "find" => Op.Find, "usage" => Op.Usage, "list" => Op.List, _ => Op.None,
                };
                if (op != Op.None)
                {
                    SetOp(o, op);
                    if (op != Op.List) o.TagArg = inline ?? (++i < args.Length ? args[i] : throw new UsageException($"option '--{name}' requires an argument"));
                    continue;
                }
                switch (name)
                {
                    case "help": Console.Write(Help.Replace("\n", Environment.NewLine) + Environment.NewLine); return 0;
                    case "version": Console.WriteLine($"tag v{Version}"); return 0;
                    case "all": o.All = true; break;
                    case "enter": o.Enter = true; break;
                    case "recursive": o.Recursive = true; break;
                    case "name": o.Name = true; break;
                    case "no-name": o.Name = false; break;
                    case "tags": o.Tags = true; break;
                    case "no-tags": o.Tags = false; break;
                    case "garrulous": o.Garrulous = true; break;
                    case "no-garrulous": o.Garrulous = false; break;
                    case "color": o.Color = true; break;
                    case "slash": o.Slash = true; break;
                    case "nul": o.Nul = true; break;
                    case "home": o.Scope = 1; break;
                    case "local": o.Scope = 2; break;
                    case "network": o.Scope = 3; break;
                    default: throw new UsageException($"unrecognized option '{arg}'");
                }
                continue;
            }
            // Bundled short options: -RN, -a tag, -atag.
            for (var j = 1; j < arg.Length; j++)
            {
                var c = arg[j];
                Op op = c switch
                {
                    'a' => Op.Add, 'r' => Op.Remove, 's' => Op.Set, 'm' => Op.Match,
                    'f' => Op.Find, 'u' => Op.Usage, 'l' => Op.List, _ => Op.None,
                };
                if (op != Op.None)
                {
                    SetOp(o, op);
                    if (op == Op.List) continue;
                    o.TagArg = j + 1 < arg.Length ? arg[(j + 1)..] : (++i < args.Length ? args[i] : throw new UsageException($"option requires an argument -- {c}"));
                    break;
                }
                switch (c)
                {
                    case 'h': Console.Write(Help.Replace("\n", Environment.NewLine) + Environment.NewLine); return 0;
                    case 'v': Console.WriteLine($"tag v{Version}"); return 0;
                    case 'A': o.All = true; break;
                    case 'e': o.Enter = true; break;
                    case 'R': o.Recursive = true; break;
                    case 'n': o.Name = true; break;
                    case 'N': o.Name = false; break;
                    case 't': o.Tags = true; break;
                    case 'T': o.Tags = false; break;
                    case 'g': o.Garrulous = true; break;
                    case 'G': o.Garrulous = false; break;
                    case 'c': o.Color = true; break;
                    case 'p': o.Slash = true; break;
                    case '0': o.Nul = true; break;
                    default: throw new UsageException($"invalid option -- {c}");
                }
            }
        }
        if (o.Op == Op.None) o.Op = Op.List;   // `tag file` lists, like upstream
        var tags = o.TagArg == null ? [] : ParseTags(o.TagArg);

        switch (o.Op)
        {
            case Op.Add or Op.Remove or Op.Set:
                if (o.Paths.Count == 0) throw new UsageException("no files specified");
                var status = 0;
                foreach (var path in Enumerate(o, o.Paths))
                {
                    try
                    {
                        var current = ReadEntries(path);
                        var next = o.Op switch
                        {
                            Op.Add => current.Concat(tags.Where(t => !current.Any(c => Same(EntryName(c), t))).Select(Entry)).ToList(),
                            Op.Remove => tags.Any(t => t == "*") ? []
                                : current.Where(c => !tags.Any(t => Same(EntryName(c), t))).ToList(),
                            _ => tags.Select(t => current.FirstOrDefault(c => Same(EntryName(c), t)) ?? Entry(t)).ToList(),
                        };
                        WriteEntries(path, next);
                    }
                    catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                    {
                        Console.Error.WriteLine($"tag: {path}: {e.Message}");
                        status = 1;
                    }
                }
                return status;

            case Op.List:
                if (o.Paths.Count == 0) o.Paths.Add(".");
                if (o.Paths.Count == 1 && o.Paths[0] == "." && !o.Enter && !o.Recursive) o.Enter = true;
                foreach (var path in Enumerate(o, o.Paths))
                    Print(o, path, ReadTags(path), o.Name ?? true, o.Tags ?? true);
                return 0;

            case Op.Match or Op.Find:
                IEnumerable<string> files;
                if (o.Op == Op.Find)
                {
                    var roots = FindRoots(o);
                    files = roots.SelectMany(r => Walk(r, recursive: true, all: true));
                }
                else
                {
                    if (o.Paths.Count == 0) { o.Paths.Add("."); o.Enter = true; }
                    files = Enumerate(o, o.Paths);
                }
                foreach (var path in files)
                {
                    var have = ReadTags(path);
                    if (Matches(have, tags)) Print(o, path, have, o.Name ?? true, o.Tags ?? false);
                }
                return 0;

            case Op.Usage:
                var counts = new SortedDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                if (o.Paths.Count == 0) { o.Paths.Add("."); o.Enter = true; }
                foreach (var path in Enumerate(o, o.Paths))
                {
                    var have = ReadTags(path);
                    if (!Matches(have, tags)) continue;
                    foreach (var t in have) counts[t] = counts.GetValueOrDefault(t) + 1;
                }
                foreach (var (t, n) in counts) Console.Write($"{n,4} {Colored(o, t)}{(o.Nul ? "\0" : Environment.NewLine)}");
                return 0;
        }
        return 0;
    }

    static void SetOp(Options o, Op op)
    {
        if (o.Op != Op.None && o.Op != op) throw new UsageException("only one operation may be given");
        o.Op = op;
    }

    static List<string> ParseTags(string arg) =>
        arg.Split(',').Select(t => t.Trim()).Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    // A file matches when it carries every requested tag (shell-style * and ? allowed); "*" alone
    // means any tag at all, and an empty tag list matches untagged files.
    static bool Matches(List<string> have, List<string> want)
    {
        if (want.Count == 0) return have.Count == 0;
        return want.All(w => w == "*" ? have.Count > 0 : have.Any(h => Glob(w, h)));
    }

    static bool Glob(string pattern, string text)
    {
        if (!pattern.Contains('*') && !pattern.Contains('?')) return Same(pattern, text);
        var regex = "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(text, regex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    // ---- output ----

    static void Print(Options o, string path, List<string> tags, bool name, bool showTags)
    {
        var eol = o.Nul ? "\0" : Environment.NewLine;
        var shown = path;
        if (o.Slash && Directory.Exists(path) && !shown.EndsWith('/') && !shown.EndsWith('\\')) shown += "/";
        var sb = new StringBuilder();
        if (o.Garrulous == true)
        {
            if (name) sb.Append(shown).Append(eol);
            if (showTags) foreach (var t in tags) sb.Append(name ? "    " : "").Append(Colored(o, t)).Append(eol);
        }
        else
        {
            if (name) sb.Append(shown);
            if (showTags && tags.Count > 0) sb.Append(name ? "\t" : "").Append(string.Join(",", tags.Select(t => Colored(o, t))));
            if (name || (showTags && tags.Count > 0)) sb.Append(eol);
        }
        Console.Write(sb.ToString());
    }

    static string Colored(Options o, string tag)
    {
        if (!o.Color) return tag;
        var index = Array.FindIndex(ColorNames, c => c.Length > 0 && Same(c, tag));
        return index > 0 ? AnsiColors[index] + tag + "\e[0m" : tag;
    }

    // ---- enumeration ----

    static IEnumerable<string> Enumerate(Options o, List<string> paths)
    {
        foreach (var path in paths)
        {
            var full = Path.GetFullPath(path);
            if (!File.Exists(full) && !Directory.Exists(full))
            {
                Console.Error.WriteLine($"tag: The file “{path}” couldn’t be opened because there is no such file.");
                continue;
            }
            if (Directory.Exists(full) && (o.Enter || o.Recursive))
            {
                if (o.Recursive && !o.Enter) yield return path;
                foreach (var child in Walk(path, o.Recursive, o.All)) yield return child;
            }
            else yield return path;
        }
    }

    static IEnumerable<string> Walk(string dir, bool recursive, bool all)
    {
        IEnumerable<string> entries;
        try { entries = Directory.EnumerateFileSystemEntries(dir).Order(StringComparer.OrdinalIgnoreCase).ToList(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { yield break; }
        foreach (var entry in entries)
        {
            var shown = dir == "." ? Path.GetFileName(entry) : entry;
            if (!all && Hidden(entry)) continue;
            yield return shown;
            if (recursive && Directory.Exists(entry) && !File.GetAttributes(entry).HasFlag(FileAttributes.ReparsePoint))
                foreach (var child in Walk(shown, true, all)) yield return child;
        }
    }

    static bool Hidden(string path)
    {
        if (Path.GetFileName(path).StartsWith('.')) return true;
        try { return File.GetAttributes(path).HasFlag(FileAttributes.Hidden); }
        catch (IOException) { return false; }
    }

    // --find's scope: the given folders, else the user's home (--home, the default), plus the fixed
    // drives (--local) and network drives (--network).
    static List<string> FindRoots(Options o)
    {
        if (o.Paths.Count > 0 && o.Scope == 0) return o.Paths;
        var roots = new List<string>(o.Paths) { Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) };
        if (o.Scope >= 2)
            foreach (var d in DriveInfo.GetDrives())
                if (d.IsReady && (d.DriveType == DriveType.Fixed || (o.Scope == 3 && d.DriveType == DriveType.Network)))
                    roots.Add(d.RootDirectory.FullName);
        return roots;
    }

    // ---- storage ----

    static string EntryName(string entry) => entry.Split('\n')[0];

    // A new tag named after a Finder color gets that color's index, as Finder does.
    static string Entry(string tag)
    {
        var index = Array.FindIndex(ColorNames, c => c.Length > 0 && Same(c, tag));
        return index > 0 ? $"{ColorNames[index]}\n{index}" : tag;
    }

    internal static List<string> ReadTags(string path) => ReadEntries(path).Select(EntryName).ToList();

    static List<string> ReadEntries(string path)
    {
        var stream = Path.GetFullPath(path) + ":" + Stream;
        byte[] data;
        try { data = File.ReadAllBytes(stream); }
        catch (FileNotFoundException) { return []; }
        catch (DirectoryNotFoundException) { return []; }
        catch (IOException) { return []; }
        catch (UnauthorizedAccessException) { return []; }
        try { return ReadPlistStrings(data); }
        catch (Exception e) when (e is IndexOutOfRangeException or ArgumentException or FormatException) { return []; }
    }

    static void WriteEntries(string path, List<string> entries)
    {
        var full = Path.GetFullPath(path);
        if (entries.Count == 0)
        {
            DeleteFileW(full + ":" + Stream);   // nothing to remove is fine
            return;
        }
        // Writing a stream must not touch the file's own timestamps' meaning; keep LastWriteTime.
        var written = File.GetLastWriteTimeUtc(full);
        File.WriteAllBytes(full + ":" + Stream, WritePlistStrings(entries));
        try { if (File.Exists(full)) File.SetLastWriteTimeUtc(full, written); else Directory.SetLastWriteTimeUtc(full, written); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }

    // ---- binary plist (bplist00), arrays of strings only ----

    internal static byte[] WritePlistStrings(List<string> items)
    {
        var count = items.Count + 1;
        var refSize = count < 256 ? 1 : 2;
        var body = new MemoryStream();
        body.Write("bplist00"u8);
        var offsets = new List<long>();
        offsets.Add(body.Position);
        WriteMarker(body, 0xA0, items.Count);
        for (var i = 1; i <= items.Count; i++)
            if (refSize == 1) body.WriteByte((byte)i); else { body.WriteByte((byte)(i >> 8)); body.WriteByte((byte)i); }
        foreach (var item in items)
        {
            offsets.Add(body.Position);
            if (item.All(ch => ch < 0x80))
            {
                WriteMarker(body, 0x50, item.Length);
                body.Write(Encoding.ASCII.GetBytes(item));
            }
            else
            {
                WriteMarker(body, 0x60, item.Length);
                body.Write(Encoding.BigEndianUnicode.GetBytes(item));
            }
        }
        var tableOffset = body.Position;
        var offsetSize = tableOffset < 256 ? 1 : tableOffset < 65536 ? 2 : 4;
        foreach (var off in offsets)
            for (var b = offsetSize - 1; b >= 0; b--) body.WriteByte((byte)(off >> (8 * b)));
        var trailer = new byte[32];
        trailer[6] = (byte)offsetSize;
        trailer[7] = (byte)refSize;
        BinaryPrimitives.WriteInt64BigEndian(trailer.AsSpan(8), count);
        BinaryPrimitives.WriteInt64BigEndian(trailer.AsSpan(16), 0);
        BinaryPrimitives.WriteInt64BigEndian(trailer.AsSpan(24), tableOffset);
        body.Write(trailer);
        return body.ToArray();
    }

    static void WriteMarker(Stream s, int type, int length)
    {
        if (length < 15) { s.WriteByte((byte)(type | length)); return; }
        s.WriteByte((byte)(type | 0xF));
        if (length < 256) { s.WriteByte(0x10); s.WriteByte((byte)length); }
        else if (length < 65536) { s.WriteByte(0x11); s.WriteByte((byte)(length >> 8)); s.WriteByte((byte)length); }
        else { s.WriteByte(0x12); var b = new byte[4]; BinaryPrimitives.WriteInt32BigEndian(b, length); s.Write(b); }
    }

    internal static List<string> ReadPlistStrings(byte[] data)
    {
        if (data.Length < 40 || Encoding.ASCII.GetString(data, 0, 6) != "bplist") throw new FormatException("not a binary plist");
        var t = data.AsSpan(data.Length - 32);
        int offsetSize = t[6], refSize = t[7];
        var count = (int)BinaryPrimitives.ReadInt64BigEndian(t[8..]);
        var top = (int)BinaryPrimitives.ReadInt64BigEndian(t[16..]);
        var table = (int)BinaryPrimitives.ReadInt64BigEndian(t[24..]);
        long Offset(int index) => ReadUInt(data, table + index * offsetSize, offsetSize);

        var list = new List<string>();
        var pos = (int)Offset(top);
        var (type, length, start) = ReadMarker(data, pos);
        if (type != 0xA) throw new FormatException("top object is not an array");
        for (var i = 0; i < length; i++)
        {
            var objRef = (int)ReadUInt(data, start + i * refSize, refSize);
            if (objRef >= count) throw new FormatException("bad object reference");
            var (st, sl, ss) = ReadMarker(data, (int)Offset(objRef));
            if (st == 0x5) list.Add(Encoding.ASCII.GetString(data, ss, sl));
            else if (st == 0x6) list.Add(Encoding.BigEndianUnicode.GetString(data, ss, sl * 2));
            else if (st == 0x7) list.Add(Encoding.UTF8.GetString(data, ss, sl));
        }
        return list;
    }

    static (int Type, int Length, int Start) ReadMarker(byte[] data, int pos)
    {
        var marker = data[pos];
        int type = marker >> 4, length = marker & 0xF, start = pos + 1;
        if (length == 0xF)
        {
            var size = 1 << (data[start] & 0xF);
            length = (int)ReadUInt(data, start + 1, size);
            start += 1 + size;
        }
        return (type, length, start);
    }

    static long ReadUInt(byte[] data, int pos, int size)
    {
        long value = 0;
        for (var i = 0; i < size; i++) value = (value << 8) | data[pos + i];
        return value;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool DeleteFileW(string path);
}
