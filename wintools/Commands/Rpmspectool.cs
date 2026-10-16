using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace WinTools;

// rpmspectool: list or download the sources and patches an RPM spec file names. Upstream hands the
// spec to rpm's own parser; Windows has no rpm, so this reads the spec itself with rpm's macro
// language: %define/%global/%undefine, ~/.rpmmacros and -d definitions, %{name}/%name,
// %{?x}/%{!?x}/%{?x:text}/%{!?x:text}, %{expand:}, %{lower:}/%{upper:}/%{len:}/%{basename:}/
// %{dirname:}/%{suffix:}/%{quote:}/%{shrink:}, %if/%elif/%else/%endif expressions and %ifarch/%ifos,
// the Name/Version/Release/Epoch/URL tags, numbered and unnumbered Source/Patch tags and the
// %sourcelist/%patchlist sections. Shell (%(...)) and Lua (%{lua:}) macros aren't run; they expand to
// nothing, as rpmspectool's sandboxed parse would without them. `get` downloads http(s) URLs.
static class Rpmspectool
{
    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("rpmspectool", "List and download the sources and patches of RPM spec files", Run);

    const string Version = "1.100.0";

    static int Run(string[] args)
    {
        var debug = false;
        var i = 0;
        for (; i < args.Length && args[i].StartsWith('-'); i++)
        {
            switch (args[i])
            {
                case "-h" or "--help": Usage(); return 0;
                case "-D" or "--debug": debug = true; break;
                default: throw new UsageException($"unrecognized arguments: {args[i]}");
            }
        }
        if (i >= args.Length) { Console.Error.WriteLine("usage: rpmspectool [-h] [--debug] {get,list,version} ..."); return 2; }
        var command = args[i];
        var rest = args[(i + 1)..];
        if (command == "version") { Console.WriteLine($"rpmspectool {Version}"); return 0; }
        if (command is not ("list" or "get")) throw new UsageException($"argument cmd: invalid choice: '{command}' (choose from 'get', 'list', 'version')");

        bool verbose = false, sources = false, patches = false, force = false, dryRun = false, sourcedir = false;
        List<int> sourceNumbers = null, patchNumbers = null;
        string directory = null, specfile = null;
        var defines = new List<string>();
        for (var k = 0; k < rest.Length; k++)
        {
            var a = rest[k];
            string Value() => ++k < rest.Length ? rest[k] : throw new UsageException($"argument {a}: expected one argument");
            switch (a)
            {
                case "-h" or "--help": CommandUsage(command); return 0;
                case "-v" or "--verbose": verbose = true; break;
                case "-d" or "--define": defines.Add(Value()); break;
                case "-S" or "--sources": sources = true; break;
                case "-s" or "--source": (sourceNumbers ??= new()).AddRange(IntList(Value(), a)); break;
                case "-P" or "--patches": patches = true; break;
                case "-p" or "--patch": (patchNumbers ??= new()).AddRange(IntList(Value(), a)); break;
                case "--insecure" when command == "get": break;   // certificate checks stay on: HttpClient verifies TLS
                case "-f" or "--force" when command == "get": force = true; break;
                case "-n" or "--dry-run" or "--dryrun" when command == "get": dryRun = true; break;
                case "-C" or "--directory" when command == "get": directory = Value(); break;
                case "-R" or "--sourcedir" when command == "get": sourcedir = true; break;
                default:
                    if (a.StartsWith('-') && a != "-") throw new UsageException($"unrecognized arguments: {a}");
                    specfile = a;
                    break;
            }
        }
        if (specfile == null) throw new UsageException("the following arguments are required: specfile");
        if (!File.Exists(specfile))
        {
            Console.Error.WriteLine($"Can’t open {specfile}: [Errno 2] No such file or directory: '{specfile}'");
            return 1;
        }

        var spec = new Spec(debug);
        foreach (var d in defines)
        {
            var parts = d.Trim().Split((char[])null, 2, StringSplitOptions.RemoveEmptyEntries);
            spec.Macros[parts[0].TrimStart('%')] = parts.Length > 1 ? parts[1] : "";
        }
        try { spec.Parse(File.ReadAllText(specfile)); }
        catch (FormatException e)
        {
            Console.Error.WriteLine("Error parsing intermediate spec file.");
            if (verbose || debug) Console.Error.WriteLine($"RPM error:\n{e.Message}");
            return 2;
        }

        // Neither selector: everything.
        Dictionary<int, string> chosenSources, chosenPatches;
        if (sourceNumbers == null && !sources && patchNumbers == null && !patches) { chosenSources = spec.Sources; chosenPatches = spec.Patches; }
        else
        {
            chosenSources = sourceNumbers != null ? spec.Sources.Where(kv => sourceNumbers.Contains(kv.Key)).ToDictionary() : sources ? spec.Sources : new();
            chosenPatches = patchNumbers != null ? spec.Patches.Where(kv => patchNumbers.Contains(kv.Key)).ToDictionary() : patches ? spec.Patches : new();
        }

        if (command == "list")
        {
            foreach (var (prefix, what) in new[] { ("Source", chosenSources), ("Patch", chosenPatches) })
                foreach (var n in what.Keys.Order()) Console.WriteLine($"{prefix}{n}: {what[n]}");
            return 0;
        }

        var where = sourcedir ? spec.Expand("%{_sourcedir}") : directory ?? Directory.GetCurrentDirectory();
        Directory.CreateDirectory(where);
        var status = 0;
        foreach (var what in new[] { chosenSources, chosenPatches })
            foreach (var n in what.Keys.Order())
            {
                var url = what[n];
                if (!Regex.IsMatch(url, "^(?:ftp|https?)://", RegexOptions.IgnoreCase)) continue;
                try { Download(url, where, dryRun, force); }
                catch (Exception e) when (e is HttpRequestException or IOException or InvalidOperationException or TaskCanceledException)
                {
                    Console.Error.WriteLine($"ERROR:root:{e.Message}");
                    status = 1;
                }
            }
        return status;
    }

    static IEnumerable<int> IntList(string value, string option)
    {
        foreach (var item in value.Split(','))
        {
            if (int.TryParse(item, out var n)) { yield return n; continue; }
            var range = item.Split('-', 2);
            if (range.Length == 2 && int.TryParse(range[0], out var a) && int.TryParse(range[1], out var b))
            {
                for (var x = a; x <= b; x++) yield return x;
                continue;
            }
            throw new UsageException($"argument {option}: can't convert '{item}' to list of ints");
        }
    }

    static void Usage() => Console.WriteLine("""
        usage: rpmspectool [-h] [--debug] {get,list,version} ...

        Utility for handling RPM spec files

        positional arguments:
          {get,list,version}
            get          Download files
            list         List files
            version      Show rpmspectool version

        options:
          -h, --help     show this help message and exit
          --debug, -D
        """);

    static void CommandUsage(string command) => Console.WriteLine(command == "get"
        ? "usage: rpmspectool get [-h] [--verbose] [--define DEFINE] [--sources | --source SOURCE] [--patches | --patch PATCH]\n" +
          "                       [--insecure] [--force] [--dry-run] [--directory DIRECTORY | --sourcedir] specfile"
        : "usage: rpmspectool list [-h] [--verbose] [--define DEFINE] [--sources | --source SOURCE] [--patches | --patch PATCH] specfile");

    static void Download(string url, string where, bool dryRun, bool force)
    {
        if (url.StartsWith("ftp:", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Couldn't download {url}: FTP isn't available (use the project's https:// URL)");
        var name = url.Split('/')[^1];
        var path = Path.Combine(where, name);
        if (dryRun) { Console.WriteLine($"NOT downloading '{url}' to '{path}'"); return; }
        if (File.Exists(path) && !force) throw new IOException($"File exists: {path}");
        Console.WriteLine($"Downloading '{url}' to '{path}'");
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true, MaxAutomaticRedirections = 20 });
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"rpmspectool/{Version}");
        using var response = http.GetAsync(url.Split('#')[0], HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Couldn't download {url}: {(int)response.StatusCode}");
        var temp = path + ".part";
        using (var output = File.Create(temp))
            response.Content.ReadAsStream().CopyTo(output);
        File.Move(temp, path, overwrite: true);
        if (response.Content.Headers.LastModified is { } modified) File.SetLastWriteTimeUtc(path, modified.UtcDateTime);
    }

    // ---- the spec reader ----

    sealed class Spec
    {
        public readonly Dictionary<string, string> Macros = new();
        public readonly Dictionary<int, string> Sources = new(), Patches = new();
        readonly bool debug;
        int depth;

        public Spec(bool debug)
        {
            this.debug = debug;
            var home = Environment.GetEnvironmentVariable("HOME") ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            Macros["_topdir"] = Path.Combine(home, "rpmbuild").Replace('\\', '/');
            Macros["_sourcedir"] = "%{_topdir}/SOURCES";
            Macros["_builddir"] = "%{_topdir}/BUILD";
            Macros["_specdir"] = "%{_topdir}/SPECS";
            Macros["_tmppath"] = "/var/tmp";
            Macros["_arch"] = Macros["_target_cpu"] = Macros["_host_cpu"] = "x86_64";
            Macros["_os"] = Macros["_target_os"] = Macros["_host_os"] = "linux";
            Macros["_prefix"] = "/usr";
            Macros["_exec_prefix"] = "%{_prefix}";
            Macros["_bindir"] = "%{_exec_prefix}/bin";
            Macros["_libdir"] = "%{_prefix}/lib64";
            Macros["_datadir"] = "%{_prefix}/share";
            Macros["_sysconfdir"] = "/etc";
            Macros["_mandir"] = "%{_datadir}/man";
            Macros["_infodir"] = "%{_datadir}/info";
            Macros["_docdir"] = "%{_datadir}/doc";
            Macros["nil"] = "";
            // ~/.rpmmacros, like rpm.
            var rc = Path.Combine(home, ".rpmmacros");
            if (File.Exists(rc))
                foreach (var line in File.ReadAllLines(rc))
                {
                    var m = Regex.Match(line, @"^\s*%(\w+)\s+(.*)$");
                    if (m.Success) Macros[m.Groups[1].Value] = m.Groups[2].Value.Trim();
                }
        }

        public void Parse(string text)
        {
            var lines = text.Replace("\r\n", "\n").Split('\n');
            var conditions = new Stack<(bool Active, bool Taken)>();
            bool Active() => conditions.All(c => c.Active);
            var section = "preamble";
            var autoSource = 0;
            var autoPatch = 0;
            for (var n = 0; n < lines.Length; n++)
            {
                var line = lines[n];
                // Line continuations (macro definitions).
                while (line.EndsWith('\\') && n + 1 < lines.Length) line = line[..^1] + "\n" + lines[++n];
                var trimmed = line.TrimStart();
                var keyword = Regex.Match(trimmed, @"^%(if|ifarch|ifnarch|ifos|ifnos|elif|elifarch|elifos|else|endif)\b(.*)$", RegexOptions.Singleline);
                if (keyword.Success)
                {
                    var kw = keyword.Groups[1].Value;
                    var rest = keyword.Groups[2].Value.Trim();
                    switch (kw)
                    {
                        case "if": { var v = Active() && Condition(rest); conditions.Push((v, v)); break; }
                        case "ifarch" or "ifnarch" or "ifos" or "ifnos":
                        {
                            var values = Expand(rest).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                            var current = kw.Contains("arch") ? new[] { "x86_64", "amd64" } : new[] { "linux" };
                            var match = values.Any(x => current.Contains(x));
                            var v = Active() && (kw.StartsWith("ifn") ? !match : match);
                            conditions.Push((v, v));
                            break;
                        }
                        case "elif" or "elifarch" or "elifos":
                        {
                            if (conditions.Count == 0) throw new FormatException($"line {n + 1}: %{kw} without %if");
                            var (_, taken) = conditions.Pop();
                            bool parent = conditions.All(c => c.Active);
                            bool v = !taken && parent && (kw == "elif" ? Condition(rest) : Expand(rest).Split(' ').Contains(kw == "elifarch" ? "x86_64" : "linux"));
                            conditions.Push((v, taken || v));
                            break;
                        }
                        case "else":
                        {
                            if (conditions.Count == 0) throw new FormatException($"line {n + 1}: %else without %if");
                            var (_, taken) = conditions.Pop();
                            var parent = conditions.All(c => c.Active);
                            conditions.Push((parent && !taken, true));
                            break;
                        }
                        case "endif":
                            if (conditions.Count == 0) throw new FormatException($"line {n + 1}: %endif without %if");
                            conditions.Pop();
                            break;
                    }
                    continue;
                }
                if (!Active()) continue;

                var define = Regex.Match(trimmed, @"^%(define|global)\s+(\w+)(\([^)]*\))?\s+(.*)$", RegexOptions.Singleline);
                if (define.Success)
                {
                    var body = define.Groups[4].Value.Trim();
                    // %global expands its body now; %define when used.
                    Macros[define.Groups[2].Value] = define.Groups[1].Value == "global" ? Expand(body) : body;
                    continue;
                }
                var undefine = Regex.Match(trimmed, @"^%undefine\s+(\w+)");
                if (undefine.Success) { Macros.Remove(undefine.Groups[1].Value); continue; }

                var sectionHeader = Regex.Match(trimmed, @"^%(package|description|prep|build|install|check|clean|files|changelog|pre|post|preun|postun|pretrans|posttrans|trigger\w*|verifyscript|sourcelist|patchlist|generate_buildrequires|conf)\b");
                if (sectionHeader.Success)
                {
                    var s = sectionHeader.Groups[1].Value;
                    section = s == "package" ? "preamble" : s;
                    continue;
                }
                if (section == "sourcelist" || section == "patchlist")
                {
                    var value = Expand(trimmed).Trim();
                    if (value.Length == 0 || value.StartsWith('#')) continue;
                    if (section == "sourcelist") Sources[NextFree(Sources, ref autoSource)] = value;
                    else Patches[NextFree(Patches, ref autoPatch)] = value;
                    continue;
                }
                if (section != "preamble") continue;

                var tag = Regex.Match(trimmed, @"^([A-Za-z]+)(\d*)\s*:\s*(.*)$");
                if (!tag.Success) { Expand(trimmed); continue; }   // expanded for side effects (%{expand:%%define ...})
                var name = tag.Groups[1].Value.ToLowerInvariant();
                var number = tag.Groups[2].Value;
                var expanded = Expand(tag.Groups[3].Value).Trim();
                switch (name)
                {
                    case "name" or "version" or "release" or "epoch" or "summary" or "license" or "url" or "group" or "vcs" or "bugurl":
                        if (!Macros.ContainsKey(name) || name is "name" or "version" or "release") Macros[name] = expanded;
                        if (name is "name" or "version" or "release" or "epoch") Macros[name.ToUpperInvariant()] = expanded;
                        break;
                    case "source":
                    {
                        var k = number.Length > 0 ? int.Parse(number) : NextFree(Sources, ref autoSource);
                        Sources[k] = expanded;
                        Macros["SOURCE" + k] = Expand("%{_sourcedir}/") + expanded.Split('/')[^1];
                        Macros["SOURCEURL" + k] = expanded;
                        break;
                    }
                    case "patch":
                    {
                        var k = number.Length > 0 ? int.Parse(number) : NextFree(Patches, ref autoPatch);
                        Patches[k] = expanded;
                        Macros["PATCH" + k] = Expand("%{_sourcedir}/") + expanded.Split('/')[^1];
                        Macros["PATCHURL" + k] = expanded;
                        break;
                    }
                }
            }
            if (conditions.Count > 0) throw new FormatException("unclosed %if");
        }

        static int NextFree(Dictionary<int, string> map, ref int next)
        {
            while (map.ContainsKey(next)) next++;
            return next++;
        }

        public string Expand(string s)
        {
            if (++depth > 64) throw new FormatException("Too many levels of recursion in macro expansion");
            try
            {
                var sb = new StringBuilder();
                for (var i = 0; i < s.Length; i++)
                {
                    var c = s[i];
                    if (c != '%' || i + 1 >= s.Length) { sb.Append(c); continue; }
                    var next = s[i + 1];
                    if (next == '%') { sb.Append('%'); i++; continue; }
                    if (next == '{')
                    {
                        var end = Matching(s, i + 1, '{', '}');
                        if (end < 0) throw new FormatException($"Unterminated {{: {s}");
                        sb.Append(Braced(s[(i + 2)..end]));
                        i = end;
                        continue;
                    }
                    if (next == '(')
                    {
                        var end = Matching(s, i + 1, '(', ')');
                        if (end < 0) throw new FormatException($"Unterminated (: {s}");
                        if (debug) Console.Error.WriteLine($"DEBUG: not running shell macro %({s[(i + 2)..end]})");
                        i = end;
                        continue;
                    }
                    if (next == '[')
                    {
                        var end = Matching(s, i + 1, '[', ']');
                        if (end < 0) throw new FormatException($"Unterminated [: {s}");
                        sb.Append(Evaluate(Expand(s[(i + 2)..end])));
                        i = end;
                        continue;
                    }
                    var m = Regex.Match(s[(i + 1)..], @"^[A-Za-z_]\w*");
                    if (m.Success && Macros.TryGetValue(m.Value, out var value))
                    {
                        sb.Append(Expand(value));
                        i += m.Length;
                        continue;
                    }
                    if (m.Success && (m.Value is "define" or "global" or "undefine")) { sb.Append(s[i..]); break; }
                    sb.Append(c);   // undefined: left as is, like rpm
                }
                return sb.ToString();
            }
            finally { depth--; }
        }

        static int Matching(string s, int open, char opening, char closing)
        {
            var level = 0;
            for (var i = open; i < s.Length; i++)
            {
                if (s[i] == opening) level++;
                else if (s[i] == closing && --level == 0) return i;
            }
            return -1;
        }

        string Braced(string body)
        {
            // %{?name}, %{!?name}, %{?name:text}, %{!?name:text}
            var cond = Regex.Match(body, @"^(!?)\?(\w+)(?::(.*))?$", RegexOptions.Singleline);
            if (cond.Success)
            {
                var defined = Macros.ContainsKey(cond.Groups[2].Value);
                var negate = cond.Groups[1].Value == "!";
                var hasText = cond.Groups[3].Success;
                if (negate) return !defined && hasText ? Expand(cond.Groups[3].Value) : "";
                if (!defined) return "";
                return hasText ? Expand(cond.Groups[3].Value) : Expand(Macros[cond.Groups[2].Value]);
            }
            var colon = body.IndexOf(':');
            if (colon > 0 && Regex.IsMatch(body[..colon], @"^\w+$"))
            {
                var fn = body[..colon];
                var arg = body[(colon + 1)..];
                switch (fn)
                {
                    case "expand": return Expand(Expand(arg));
                    case "lower": return Expand(arg).ToLowerInvariant();
                    case "upper": return Expand(arg).ToUpperInvariant();
                    case "len": return Expand(arg).Length.ToString();
                    case "basename": return Expand(arg).Split('/')[^1];
                    case "dirname": { var e = Expand(arg); var slash = e.LastIndexOf('/'); return slash > 0 ? e[..slash] : slash == 0 ? "/" : "."; }
                    case "suffix": { var e = Expand(arg).Split('/')[^1]; var dot = e.LastIndexOf('.'); return dot >= 0 ? e[(dot + 1)..] : ""; }
                    case "quote": return Expand(arg);
                    case "shrink": return Regex.Replace(Expand(arg).Trim(), @"\s+", " ");
                    case "url2path" or "u2p": return Regex.Replace(Expand(arg), @"^\w+://[^/]*", "");
                    case "getenv": return Environment.GetEnvironmentVariable(Expand(arg)) ?? "";
                    case "lua":
                        if (debug) Console.Error.WriteLine("DEBUG: not running Lua macro");
                        return "";
                    case "echo" or "warn" or "error":
                        if (fn == "error") throw new FormatException(Expand(arg));
                        Console.Error.WriteLine(Expand(arg));
                        return "";
                }
            }
            var name = Regex.Match(body, @"^\w+").Value;
            if (name.Length > 0 && Macros.TryGetValue(name, out var value)) return Expand(value);
            return "%{" + body + "}";
        }

        bool Condition(string expression)
        {
            var value = Evaluate(Expand(expression));
            return value != "0" && value.Length > 0 && value != "\"\"";
        }

        // rpm's expression language: integers, "strings", version literals v"1.2", ! - * / + - < > <= >= == != && || ?:
        string Evaluate(string expression)
        {
            var tokens = Regex.Matches(expression, "\"[^\"]*\"|v\"[^\"]*\"|\\d+|[A-Za-z_][\\w.]*|&&|\\|\\||==|!=|<=|>=|[!<>+\\-*/()?:]")
                .Select(m => m.Value).ToList();
            var pos = 0;
            object Primary()
            {
                if (pos >= tokens.Count) throw new FormatException($"syntax error in expression: {expression}");
                var t = tokens[pos++];
                if (t == "(") { var v = Ternary(); pos++; return v; }
                if (t == "!") { var v = Primary(); return Truth(v) ? 0L : 1L; }
                if (t == "-") return -(long)Number(Primary());
                if (t.StartsWith("v\"")) return t[2..^1];
                if (t.StartsWith('"')) return t[1..^1];
                if (long.TryParse(t, out var n)) return n;
                return t;   // a bare word (an unexpanded macro): a string
            }
            object Mul() { var v = Primary(); while (pos < tokens.Count && tokens[pos] is "*" or "/") { var op = tokens[pos++]; var r = Number(Primary()); v = op == "*" ? Number(v) * r : r == 0 ? throw new FormatException("division by zero") : Number(v) / r; } return v; }
            object Add() { var v = Mul(); while (pos < tokens.Count && tokens[pos] is "+" or "-") { var op = tokens[pos++]; var r = Mul(); v = op == "+" ? (v is string || r is string ? (object)(v.ToString() + r) : Number(v) + Number(r)) : Number(v) - Number(r); } return v; }
            object Compare()
            {
                var v = Add();
                while (pos < tokens.Count && tokens[pos] is "<" or ">" or "<=" or ">=" or "==" or "!=")
                {
                    var op = tokens[pos++];
                    var r = Add();
                    var c = v is long a && r is long b ? a.CompareTo(b) : string.CompareOrdinal(v.ToString(), r.ToString());
                    v = op switch { "<" => c < 0, ">" => c > 0, "<=" => c <= 0, ">=" => c >= 0, "==" => c == 0, _ => c != 0 } ? 1L : 0L;
                }
                return v;
            }
            object And() { var v = Compare(); while (pos < tokens.Count && tokens[pos] == "&&") { pos++; var r = Compare(); v = Truth(v) && Truth(r) ? 1L : 0L; } return v; }
            object Or() { var v = And(); while (pos < tokens.Count && tokens[pos] == "||") { pos++; var r = And(); v = Truth(v) || Truth(r) ? 1L : 0L; } return v; }
            object Ternary()
            {
                var v = Or();
                if (pos < tokens.Count && tokens[pos] == "?")
                {
                    pos++;
                    var a = Ternary();
                    pos++;   // ':'
                    var b = Ternary();
                    return Truth(v) ? a : b;
                }
                return v;
            }
            if (tokens.Count == 0) return "0";
            var result = Ternary();
            return result is long l ? l.ToString() : result.ToString();
        }

        static bool Truth(object v) => v is long n ? n != 0 : v.ToString().Length > 0;
        static long Number(object v) => v is long n ? n : long.TryParse(v.ToString(), out var x) ? x : throw new FormatException($"types must match: {v}");
    }
}
