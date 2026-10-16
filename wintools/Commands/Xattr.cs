using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace WinTools;

// attr / getfattr / setfattr: extended attributes kept as NTFS alternate data streams. The user
// namespace maps one to one onto stream names, so `user.comment` is the stream file:comment (and a
// browser's file:Zone.Identifier shows up as user.Zone.Identifier). Other namespaces (trusted.,
// security., system.) have no Windows counterpart.
static class Xattr
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("getfattr", "Get extended attributes (NTFS alternate data streams) of files", GetFattr);
        Commands.Add("setfattr", "Set extended attributes (NTFS alternate data streams) of files", SetFattr);
        Commands.Add("attr", "Manipulate extended attributes (NTFS alternate data streams)", Attr);
    }

    // ---- streams ----

    static string Full(string path)
    {
        var full = Path.GetFullPath(path);
        if (!File.Exists(full) && !Directory.Exists(full)) throw new IOException($"{path}: No such file or directory");
        return full;
    }

    static string StreamName(string attr)
    {
        if (attr.StartsWith("user.", StringComparison.Ordinal) && attr.Length > 5) return attr[5..];
        if (attr is "user." or "") throw new InvalidOperationException("Invalid argument");
        throw new InvalidOperationException("Operation not supported");
    }

    static unsafe List<(string Name, long Size)> Streams(string path)
    {
        var list = new List<(string, long)>();
        var data = new WIN32_FIND_STREAM_DATA();
        var handle = FindFirstStreamW(path, 0, &data, 0);
        if (handle == new IntPtr(-1)) return list;   // ERROR_HANDLE_EOF: no streams at all
        try
        {
            do
            {
                var name = new string(data.cStreamName);
                // ":name:$DATA"; the unnamed "::$DATA" is the file's contents.
                if (name.StartsWith(':') && name.EndsWith(":$DATA") && name.Length > 7)
                    list.Add((name[1..^6], data.StreamSize));
            }
            while (FindNextStreamW(handle, &data));
        }
        finally { FindClose(handle); }
        return list;
    }

    static byte[] Get(string path, string attr)
    {
        var stream = StreamName(attr);
        if (!Streams(path).Any(s => s.Name.Equals(stream, StringComparison.OrdinalIgnoreCase))) return null;
        return File.ReadAllBytes(path + ":" + stream);
    }

    static void Set(string path, string attr, byte[] value) => File.WriteAllBytes(path + ":" + StreamName(attr), value);

    static bool Remove(string path, string attr)
    {
        var stream = StreamName(attr);
        if (!Streams(path).Any(s => s.Name.Equals(stream, StringComparison.OrdinalIgnoreCase))) return false;
        if (!DeleteFileW(path + ":" + stream)) throw new IOException(new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message);
        return true;
    }

    // ---- value encodings (getfattr -e) ----

    internal static string Encode(byte[] value, string encoding)
    {
        encoding ??= IsText(value) ? "text" : "base64";
        switch (encoding)
        {
            case "hex": return "0x" + Convert.ToHexString(value).ToLowerInvariant();
            case "base64": return "0s" + Convert.ToBase64String(value);
            default:
                var sb = new StringBuilder("\"");
                var bytes = value.Length > 0 && value[^1] == 0 ? value[..^1] : value;
                foreach (var b in bytes)
                {
                    if (b is (byte)'"' or (byte)'\\') sb.Append('\\').Append((char)b);
                    else if (b < 0x20 || b >= 0x7F) sb.Append('\\').Append(Convert.ToString(b, 8).PadLeft(3, '0'));
                    else sb.Append((char)b);
                }
                return sb.Append('"').ToString();
        }
    }

    static bool IsText(byte[] value)
    {
        var body = value.Length > 0 && value[^1] == 0 ? value[..^1] : value;
        return body.All(b => b >= 0x20 && b < 0x7F || b is (byte)'\t' or (byte)'\n' or (byte)'\r');
    }

    internal static byte[] Decode(string value)
    {
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) return Convert.FromHexString(value[2..]);
        if (value.StartsWith("0s", StringComparison.OrdinalIgnoreCase)) return Convert.FromBase64String(value[2..]);
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
        {
            var output = new List<byte>();
            var inner = value[1..^1];
            for (var i = 0; i < inner.Length; i++)
            {
                if (inner[i] == '\\' && i + 1 < inner.Length)
                {
                    if (i + 3 < inner.Length && inner.Substring(i + 1, 3).All(c => c is >= '0' and <= '7'))
                    {
                        output.Add(Convert.ToByte(inner.Substring(i + 1, 3), 8));
                        i += 3;
                    }
                    else output.Add((byte)inner[++i]);
                }
                else output.AddRange(Encoding.UTF8.GetBytes(inner[i].ToString()));
            }
            return [.. output];
        }
        return Encoding.UTF8.GetBytes(value);
    }

    static IEnumerable<string> Walk(string path, bool recursive)
    {
        yield return path;
        if (!recursive || !Directory.Exists(path)) yield break;
        foreach (var child in Directory.EnumerateFileSystemEntries(path, "*", new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = 0 }))
            yield return child;
    }

    // ---- getfattr ----

    static int GetFattr(string[] args)
    {
        string name = null, encoding = null, pattern = null;
        bool dump = false, onlyValues = false, recursive = false;
        var paths = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string Value()
            {
                var eq = arg.IndexOf('=');
                if (arg.StartsWith("--") && eq > 0) return arg[(eq + 1)..];
                if (!arg.StartsWith("--") && arg.Length > 2) return arg[2..];
                if (++i >= args.Length) throw new UsageException($"option requires an argument -- '{arg.TrimStart('-')}'");
                return args[i];
            }
            var key = arg.StartsWith("--") ? arg.Split('=')[0] : arg.Length >= 2 && arg[0] == '-' ? arg[..2] : arg;
            switch (key)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage: getfattr [-hRLP] [-n name|-d] [-e en] [-m pattern] path...
                          -n, --name=name         get the named extended attribute value
                          -d, --dump              get all extended attribute values
                          -e, --encoding=...      encode values (as 'text', 'hex' or 'base64')
                              --match=pattern     only get attributes with names matching pattern
                              --only-values       print the bare values only
                          -h, --no-dereference    do not dereference symbolic links
                              --absolute-names    don't strip leading '/' in pathnames
                          -R, --recursive         recurse into subdirectories
                          -L, --logical           logical walk, follow symbolic links
                          -P  --physical          physical walk, do not follow symbolic links
                              --version           print version and exit
                              --help              this help text
                        Attributes live in NTFS alternate data streams: user.NAME is the stream path:NAME.
                        """);
                    return 0;
                case "--version": Console.WriteLine($"getfattr {Commands.Version}"); return 0;
                case "-n" or "--name": name = Value(); break;
                case "-d" or "--dump": dump = true; break;
                case "-e" or "--encoding":
                    encoding = Value();
                    if (encoding is not ("text" or "hex" or "base64")) throw new UsageException($"unrecognized encoding '{encoding}'");
                    break;
                case "-m" or "--match": pattern = Value(); break;
                case "--only-values": onlyValues = true; break;
                case "-R" or "--recursive": recursive = true; break;
                case "--absolute-names" or "--no-dereference" or "-L" or "--logical" or "-P" or "--physical": break;
                default:
                    if (arg.StartsWith('-') && arg != "-") throw new UsageException($"invalid option -- '{arg.TrimStart('-')}'");
                    paths.Add(arg);
                    break;
            }
        }
        if (paths.Count == 0) throw new UsageException("Usage: getfattr [-hRLP] [-n name|-d] [-e en] [-m pattern] path...");
        var regex = new Regex(pattern ?? "^user\\.", RegexOptions.CultureInvariant);
        var status = 0;
        using var stdout = Console.OpenStandardOutput();
        void Out(string s) => stdout.Write(Encoding.UTF8.GetBytes(s));
        foreach (var path in paths.SelectMany(p => Walk(p, recursive)))
        {
            try
            {
                var full = Full(path);
                if (name != null)
                {
                    var value = Get(full, name);
                    if (value == null) { Console.Error.WriteLine($"{path}: {name}: No such attribute"); status = 1; continue; }
                    if (onlyValues) { stdout.Write(value); continue; }
                    Out($"# file: {path}\n{name}{(value.Length > 0 ? "=" + Encode(value, encoding) : "")}\n\n");
                    continue;
                }
                var names = Streams(full).Select(s => "user." + s.Name).Where(n => regex.IsMatch(n)).ToList();
                if (names.Count == 0) continue;
                if (!onlyValues) Out($"# file: {path}\n");
                foreach (var n in names)
                {
                    if (!dump) { Out(n + "\n"); continue; }
                    var value = File.ReadAllBytes(full + ":" + n[5..]);
                    if (onlyValues) stdout.Write(value);
                    else Out($"{n}{(value.Length > 0 ? "=" + Encode(value, encoding) : "")}\n");
                }
                if (!onlyValues) Out("\n");
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                Console.Error.WriteLine($"getfattr: {path}: {e.Message}");
                status = 1;
            }
        }
        return status;
    }

    // ---- setfattr ----

    static int SetFattr(string[] args)
    {
        string name = null, value = null, remove = null, restore = null;
        var paths = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string Value()
            {
                var eq = arg.IndexOf('=');
                if (arg.StartsWith("--") && eq > 0) return arg[(eq + 1)..];
                if (!arg.StartsWith("--") && arg.Length > 2) return arg[2..];
                if (++i >= args.Length) throw new UsageException($"option requires an argument -- '{arg.TrimStart('-')}'");
                return args[i];
            }
            var key = arg.StartsWith("--") ? arg.Split('=')[0] : arg.Length >= 2 && arg[0] == '-' ? arg[..2] : arg;
            switch (key)
            {
                case "--help":
                    Console.WriteLine("""
                        Usage: setfattr {-n name} [-v value] [-h] file...
                               setfattr {-x name} [-h] file...
                          -n, --name=name         set the value of the named extended attribute
                          -x, --remove=name       remove the named extended attribute
                          -v, --value=value       use value as the attribute value ("text", 0x hex or 0s base64)
                          -h, --no-dereference    do not dereference symbolic links
                              --restore=file      restore extended attributes (inverse of `getfattr -d')
                              --raw               accepted for compatibility
                              --version           print version and exit
                              --help              this help text
                        """);
                    return 0;
                case "--version": Console.WriteLine($"setfattr {Commands.Version}"); return 0;
                case "-n" or "--name": name = Value(); break;
                case "-v" or "--value": value = Value(); break;
                case "-x" or "--remove": remove = Value(); break;
                case "--restore": restore = Value(); break;
                case "-h" or "--no-dereference" or "--raw": break;
                default:
                    if (arg.StartsWith('-') && arg != "-") throw new UsageException($"invalid option -- '{arg.TrimStart('-')}'");
                    paths.Add(arg);
                    break;
            }
        }
        if (restore != null) return RestoreAttrs(restore);
        if ((name == null) == (remove == null) || paths.Count == 0)
            throw new UsageException("Usage: setfattr {-n name} [-v value] [-h] file...\n       setfattr {-x name} [-h] file...");
        var status = 0;
        foreach (var path in paths)
        {
            try
            {
                var full = Full(path);
                if (remove != null)
                {
                    if (!Remove(full, remove)) { Console.Error.WriteLine($"setfattr: {path}: No such attribute"); status = 1; }
                }
                else Set(full, name, value == null ? [] : Decode(value));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or FormatException)
            {
                Console.Error.WriteLine($"setfattr: {path}: {e.Message}");
                status = 1;
            }
        }
        return status;
    }

    static int RestoreAttrs(string file)
    {
        var status = 0;
        string current = null;
        var lines = (file == "-" ? Console.In.ReadToEnd() : File.ReadAllText(file)).Replace("\r\n", "\n").Split('\n');
        foreach (var line in lines)
        {
            if (line.StartsWith("# file: ")) { current = line[8..]; continue; }
            if (current == null || line.Length == 0 || line.StartsWith('#')) continue;
            var eq = line.IndexOf('=');
            var name = eq > 0 ? line[..eq] : line;
            try { Set(Full(current), name, eq > 0 ? Decode(line[(eq + 1)..]) : []); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or FormatException)
            {
                Console.Error.WriteLine($"setfattr: {current}: {e.Message}");
                status = 1;
            }
        }
        return status;
    }

    // ---- attr (IRIX style) ----

    static int Attr(string[] args)
    {
        string op = null, name = null, value = null;
        var quiet = false;
        var paths = new List<string>();
        const string usage = """
            Usage: attr [-LRSq] -s attrname [-V attrvalue] pathname  # set value
                   attr [-LRSq] -g attrname pathname                 # get value
                   attr [-LRSq] -r attrname pathname                 # remove attr
                   attr [-LRq]  -l pathname                          # list attrs
                  -s reads a value from stdin and -g writes a value to stdout
            """;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "-h" or "--help": Console.WriteLine(usage); return 0;
                case "--version": Console.WriteLine($"attr {Commands.Version}"); return 0;
                case "-s" or "-g" or "-r":
                    if (op != null) throw new UsageException("Only one of -s, -g, -r, or -l allowed\n" + usage);
                    if (++i >= args.Length) throw new UsageException(usage);
                    op = arg; name = args[i];
                    break;
                case "-l":
                    if (op != null) throw new UsageException("Only one of -s, -g, -r, or -l allowed\n" + usage);
                    op = arg;
                    break;
                case "-V":
                    if (++i >= args.Length) throw new UsageException(usage);
                    value = args[i];
                    break;
                case "-q": quiet = true; break;
                case "-L": break;
                case "-R" or "-S":
                    Console.Error.WriteLine("attr: the root and secure attribute namespaces don't exist on Windows; only user attributes (streams) do.");
                    return 1;
                default:
                    if (arg.StartsWith('-')) throw new UsageException(usage);
                    paths.Add(arg);
                    break;
            }
        }
        if (op == null || paths.Count != 1) throw new UsageException(usage);
        var path = paths[0];
        var full = Full(path);
        using var stdout = Console.OpenStandardOutput();
        void Out(string s) => stdout.Write(Encoding.UTF8.GetBytes(s));
        switch (op)
        {
            case "-s":
                byte[] data;
                if (value != null) data = Encoding.UTF8.GetBytes(value);
                else
                {
                    using var stdin = Console.OpenStandardInput();
                    using var ms = new MemoryStream();
                    stdin.CopyTo(ms);
                    data = ms.ToArray();
                }
                Set(full, "user." + name, data);
                if (!quiet)
                {
                    Out($"Attribute \"{name}\" set to a {data.Length} byte value for {path}:\n");
                    stdout.Write(data);
                    Out("\n");
                }
                return 0;
            case "-g":
                var got = Get(full, "user." + name);
                if (got == null)
                {
                    Console.Error.WriteLine($"attr_get: No data available\nCould not get \"{name}\" for {path}");
                    return 1;
                }
                if (!quiet) Out($"Attribute \"{name}\" had a {got.Length} byte value for {path}:\n");
                stdout.Write(got);
                if (!quiet) Out("\n");
                return 0;
            case "-r":
                if (!Remove(full, "user." + name))
                {
                    Console.Error.WriteLine($"attr_remove: No data available\nCould not remove \"{name}\" for {path}");
                    return 1;
                }
                return 0;
            default:
                foreach (var (stream, size) in Streams(full))
                    Out(quiet ? stream + "\n" : $"Attribute \"{stream}\" has a {size} byte value for {path}\n");
                return 0;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    unsafe struct WIN32_FIND_STREAM_DATA
    {
        public long StreamSize;
        public fixed char cStreamName[296];
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern unsafe IntPtr FindFirstStreamW(string fileName, int infoLevel, WIN32_FIND_STREAM_DATA* data, uint flags);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern unsafe bool FindNextStreamW(IntPtr handle, WIN32_FIND_STREAM_DATA* data);

    [DllImport("kernel32.dll")]
    static extern bool FindClose(IntPtr handle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool DeleteFileW(string path);
}
