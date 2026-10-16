using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace WinTools;

// swiftplantuml: generate PlantUML class diagrams from Swift sources: classes, structs, enums,
// protocols, actors and extensions with their properties, methods, initializers and enum cases
// (access level as + ~ -), and inheritance, protocol conformance and extension arrows. SwiftPlantUML
// reads the code through SourceKitten; this parses the declarations directly (comments, strings and
// function bodies are skipped), which needs no Swift toolchain. --output consoleOnly prints the
// diagram script; browser and browserImageOnly open it on planttext.com (encoded the PlantUML way)
// in the default browser. Reads .swiftplantuml.yml like the original (elements, exclude,
// skinparamCommands, hideShowCommands).
static class SwiftPlantUml
{
    const string Version = "0.8.1";

    [ModuleInitializer]
    internal static void Register() => Commands.Add("swiftplantuml", "Generate PlantUML class diagrams from Swift sources", Run);

    sealed class Decl
    {
        public string Kind, Name, Access = "internal", Generics;
        public List<string> Inherits = [];
        public List<string> Members = [];
        public bool IsExtension;
    }

    sealed class Settings
    {
        public string Output = "browser", Extensions = "all";
        public HashSet<string> TypeAccess = ["open", "public", "package", "internal", "fileprivate", "private"];
        public HashSet<string> MemberAccess = ["open", "public", "package", "internal", "fileprivate", "private"];
        public List<string> Exclude = [], Skinparams = [], HideShow = ["hide empty members"];
        public bool ShowGenerics = true, ShowNested = true;
    }

    static int Run(string[] args)
    {
        var list = args.ToList();
        if (list.Count > 0 && list[0] == "classdiagram") list.RemoveAt(0);
        var s = new Settings();
        var paths = new List<string>();
        string config = null;
        for (var i = 0; i < list.Count; i++)
        {
            var a = list[i];
            string Value() => ++i < list.Count ? list[i] : throw new UsageException($"missing value for '{a}'");
            switch (a)
            {
                case "-h" or "--help" or "help":
                    Console.WriteLine("""
                        OVERVIEW: Generate PlantUML class diagrams from Swift sources

                        USAGE: swiftplantuml [classdiagram] [<paths> ...] [--config <config>] [--exclude <exclude> ...]
                                             [--output <format>] [--sdk <sdk>] [--hide-extensions | --merge-extensions | --show-extensions]

                        OPTIONS:
                          --config <config>       path to a .swiftplantuml.yml (default: ./.swiftplantuml.yml if present)
                          --exclude <exclude>     paths to ignore (globs)
                          --output <format>       browser (default), browserImageOnly or consoleOnly
                          --sdk <sdk>             accepted for compatibility (no SourceKit needed)
                          --hide-extensions       leave extensions out
                          --merge-extensions      merge extension members into their types
                          --show-extensions       show extensions as their own boxes (default)
                          --version               show the version
                        """);
                    return 0;
                case "--version": Console.WriteLine(Version); return 0;
                case "--config": config = Value(); break;
                case "--exclude": s.Exclude.Add(Value()); break;
                case "--output": s.Output = Value(); break;
                case "--sdk": Value(); break;
                case "--hide-extensions": s.Extensions = "none"; break;
                case "--merge-extensions": s.Extensions = "merged"; break;
                case "--show-extensions": s.Extensions = "all"; break;
                case "--verbose": break;
                default:
                    if (a.StartsWith('-')) throw new UsageException($"Unknown option '{a}'");
                    paths.Add(a);
                    break;
            }
        }
        if (s.Output is not ("browser" or "browserImageOnly" or "consoleOnly"))
            throw new UsageException($"The value '{s.Output}' is invalid for '--output <format>'");
        config ??= File.Exists(".swiftplantuml.yml") ? ".swiftplantuml.yml" : null;
        if (config != null) LoadConfig(config, s);
        if (paths.Count == 0) paths.Add(".");

        var files = new List<string>();
        foreach (var p in paths)
        {
            if (File.Exists(p)) files.Add(p);
            else if (Directory.Exists(p)) files.AddRange(Directory.EnumerateFiles(p, "*.swift", SearchOption.AllDirectories).Order(StringComparer.Ordinal));
            else { Console.Error.WriteLine($"swiftplantuml: {p}: no such file or directory"); return 1; }
        }
        var excludes = s.Exclude.Select(Glob).ToList();
        files = files.Where(f => !excludes.Any(x => x(f.Replace('\\', '/')))).ToList();
        var decls = new List<Decl>();
        foreach (var f in files) Parse(Strip(File.ReadAllText(f)), "", decls, s);
        var script = Script(decls, s);
        if (s.Output == "consoleOnly") { Console.Write(script); return 0; }
        var encoded = Encode(script);
        var url = s.Output == "browserImageOnly" ? $"https://www.planttext.com/api/plantuml/svg/{encoded}" : $"https://www.planttext.com/?text={encoded}";
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })?.Dispose();
        return 0;
    }

    static void LoadConfig(string path, Settings s)
    {
        if (XcYaml.Parse(File.ReadAllText(path)) is not OrderedDictionary<string, object> c) return;
        HashSet<string> Levels(object v) => v is List<object> l ? [.. l.Select(XcPlist.Scalar)] : [XcPlist.Scalar(v)];
        if (XcPlist.Dict(c, "elements") is { } el)
        {
            if (el.TryGetValue("havingAccessLevel", out var t)) s.TypeAccess = Levels(t);
            if (el.TryGetValue("showMembersWithAccessLevel", out var m)) s.MemberAccess = Levels(m);
            if (XcPlist.Str(el, "showGenerics") is { } g) s.ShowGenerics = g == "YES";
            if (XcPlist.Str(el, "showNestedTypes") is { } n) s.ShowNested = n == "YES";
            if (XcPlist.Str(el, "showExtensions") is { } e) s.Extensions = e == "NO" ? "none" : e;
            if (XcPlist.List(el, "exclude") is { } ex) s.Exclude.AddRange(ex.Select(XcPlist.Scalar));
        }
        if (XcPlist.List(c, "files") is { } files) s.Exclude.AddRange(files.OfType<OrderedDictionary<string, object>>().SelectMany(d => XcPlist.List(d, "exclude") ?? []).Select(XcPlist.Scalar));
        if (XcPlist.Dict(c, "files") is { } fd && XcPlist.List(fd, "exclude") is { } fx) s.Exclude.AddRange(fx.Select(XcPlist.Scalar));
        if (XcPlist.List(c, "skinparamCommands") is { } sk) s.Skinparams.AddRange(sk.Select(XcPlist.Scalar));
        if (XcPlist.List(c, "hideShowCommands") is { } hs) s.HideShow = hs.Select(XcPlist.Scalar).ToList();
    }

    static Func<string, bool> Glob(string pattern)
    {
        var rx = "(^|/)" + Regex.Escape(pattern.Replace('\\', '/').TrimStart('.', '/'))
            .Replace(@"\*\*/", "(.*/)?").Replace(@"\*\*", ".*").Replace(@"\*", "[^/]*").Replace(@"\?", "[^/]") + "($|/)";
        var re = new Regex(rx, RegexOptions.IgnoreCase);
        return re.IsMatch;
    }

    // ---- parsing ----

    // Comments and string literal contents removed, so braces and keywords inside them don't count.
    static string Strip(string src)
    {
        var sb = new StringBuilder(src.Length);
        for (var i = 0; i < src.Length; i++)
        {
            if (src[i] == '/' && i + 1 < src.Length && src[i + 1] == '/') { while (i < src.Length && src[i] != '\n') i++; sb.Append('\n'); continue; }
            if (src[i] == '/' && i + 1 < src.Length && src[i + 1] == '*')
            {
                var depth = 0;
                for (; i < src.Length - 1; i++)
                {
                    if (src[i] == '/' && src[i + 1] == '*') { depth++; i++; }
                    else if (src[i] == '*' && src[i + 1] == '/') { depth--; i++; if (depth == 0) break; }
                }
                sb.Append(' ');
                continue;
            }
            if (src[i] == '"')
            {
                var triple = i + 2 < src.Length && src[i + 1] == '"' && src[i + 2] == '"';
                i += triple ? 3 : 1;
                for (; i < src.Length; i++)
                {
                    if (src[i] == '\\') { i++; continue; }
                    if (triple ? i + 2 < src.Length && src[i] == '"' && src[i + 1] == '"' && src[i + 2] == '"' : src[i] == '"' || src[i] == '\n') { if (triple) i += 2; break; }
                }
                sb.Append("\"\"");
                continue;
            }
            sb.Append(src[i]);
        }
        return sb.ToString();
    }

    static readonly Regex TypeDecl = new(
        @"(?<attrs>(?:@\w+(?:\([^)]*\))?\s+)*)(?<mods>(?:(?:public|open|internal|private|fileprivate|package|final|indirect|nonisolated|static)(?:\(set\))?\s+)*)(?<kind>class|struct|enum|protocol|extension|actor)\s+(?<name>[\w.]+)(?<gen>\s*<[^{]*?>)?(?<inh>\s*:[^{]*?)?(?:\s+where[^{]*)?\s*\{",
        RegexOptions.Compiled);

    static void Parse(string src, string ns, List<Decl> decls, Settings s)
    {
        var pos = 0;
        while (pos < src.Length)
        {
            var m = TypeDecl.Match(src, pos);
            if (!m.Success) return;
            // Skip matches that sit inside a function or other nested braces: only top-level text counts.
            var open = m.Index + m.Length - 1;
            var close = Matching(src, open);
            var kind = m.Groups["kind"].Value;
            // "class func"/"class var" inside a body are members, not declarations.
            if (kind == "class" && Regex.IsMatch(m.Groups["name"].Value, "^(func|var|let)$")) { pos = m.Index + m.Length; continue; }
            var name = m.Groups["name"].Value;
            var d = new Decl
            {
                Kind = kind == "actor" ? "class" : kind,
                Name = kind == "extension" ? name : (ns.Length > 0 ? ns + "." : "") + name,
                IsExtension = kind == "extension",
                Generics = m.Groups["gen"].Success ? m.Groups["gen"].Value.Trim() : null,
                Access = Access(m.Groups["mods"].Value),
            };
            if (m.Groups["inh"].Success)
                d.Inherits = SplitTopLevel(m.Groups["inh"].Value.Trim().TrimStart(':'), ',').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
            var body = src[(open + 1)..close];
            Members(body, d, s);
            decls.Add(d);
            if (s.ShowNested || kind == "extension") Parse(TopLevelBlocksOnly(body), kind == "extension" ? name : d.Name, decls, s);
            pos = close + 1;
        }
    }

    // The body text with function/property bodies blanked, so nested type search stays at this level.
    static string TopLevelBlocksOnly(string body)
    {
        var sb = new StringBuilder(body);
        var pos = 0;
        while (pos < body.Length)
        {
            var brace = body.IndexOf('{', pos);
            if (brace < 0) break;
            var close = Matching(body, brace);
            var head = body[..brace];
            var lastLine = head[(Math.Max(head.LastIndexOf(';'), head.LastIndexOf('}')) + 1)..];
            if (!TypeDecl.IsMatch(lastLine + "{"))
                for (var i = brace + 1; i < close && i < sb.Length; i++) if (sb[i] != '\n') sb[i] = ' ';
            pos = close + 1;
        }
        return sb.ToString();
    }

    static int Matching(string s, int open)
    {
        var depth = 0;
        for (var i = open; i < s.Length; i++)
        {
            if (s[i] == '{') depth++;
            else if (s[i] == '}' && --depth == 0) return i;
        }
        return s.Length - 1;
    }

    static List<string> SplitTopLevel(string s, char sep)
    {
        var parts = new List<string>();
        var depth = 0;
        var start = 0;
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] is '<' or '(' or '[') depth++;
            else if (s[i] is '>' or ')' or ']') depth--;
            else if (s[i] == sep && depth == 0) { parts.Add(s[start..i]); start = i + 1; }
        }
        parts.Add(s[start..]);
        return parts;
    }

    static string Access(string mods) =>
        Regex.Match(mods, @"\b(open|public|package|fileprivate|private|internal)\b(?!\(set\))") is { Success: true } m ? m.Groups[1].Value : "internal";

    static readonly Regex MemberDecl = new(
        @"^\s*(?<attrs>(?:@\w+(?:\([^)]*\))?\s+)*)(?<mods>(?:(?:public|open|internal|private|fileprivate|package|static|class|final|override|lazy|weak|unowned|mutating|nonmutating|dynamic|optional|required|convenience|nonisolated|async)(?:\(set\))?\s+)*)(?<kind>var|let|func|init\??|init!|case|subscript|typealias|associatedtype|deinit)\b(?<rest>[^\n]*)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    static void Members(string body, Decl d, Settings s)
    {
        // Only lines at the body's own level: blank everything inside nested braces.
        var flat = new StringBuilder(body);
        for (var i = 0; i < body.Length; i++)
            if (body[i] == '{') { var c = Matching(body, i); for (var k = i; k <= c && k < flat.Length; k++) if (flat[k] != '\n') flat[k] = ' '; i = c; }
        foreach (Match m in MemberDecl.Matches(flat.ToString().Replace(';', '\n')))
        {
            var mods = m.Groups["mods"].Value;
            var access = Access(mods);
            if (d.Kind == "protocol") access = d.Access;
            if (!s.MemberAccess.Contains(access)) continue;
            var mark = access switch { "open" or "public" => "+", "private" or "fileprivate" => "-", "package" => "~", _ => "~" };
            var isStatic = Regex.IsMatch(mods, @"\b(static|class)\b") ? "{static} " : "";
            var rest = m.Groups["rest"].Value.Trim().TrimEnd('{', ' ');
            switch (m.Groups["kind"].Value)
            {
                case "var" or "let":
                {
                    // "var x, y: T" declares both as T: a name without a type takes the next one's.
                    var vars = new List<(string Name, string Type)>();
                    foreach (var part in SplitTopLevel(rest, ','))
                    {
                        var pm = Regex.Match(part.Trim(), @"^(\w+)\s*(?::\s*([^=]+))?");
                        if (pm.Success) vars.Add((pm.Groups[1].Value, pm.Groups[2].Success ? pm.Groups[2].Value.Trim() : null));
                    }
                    for (var i = vars.Count - 2; i >= 0; i--)
                        if (vars[i].Type == null && !rest.Contains('=')) vars[i] = (vars[i].Name, vars[i + 1].Type);
                    foreach (var (vname, type) in vars)
                        d.Members.Add($"{mark}{isStatic}{vname}{(type != null ? " : " + type : "")}");
                    break;
                }
                case "func":
                {
                    var fm = Regex.Match(rest, @"^([^\s(<]+)\s*(<[^>]*>)?\s*(\(.*?\))\s*((?:async\s*)?(?:throws|rethrows)?(?:\([^)]*\))?)?\s*(?:->\s*(.+?))?\s*(?:where\s.*)?$");
                    if (!fm.Success) break;
                    var ret = fm.Groups[5].Success && fm.Groups[5].Value.Length > 0 ? " : " + fm.Groups[5].Value.Trim() : "";
                    d.Members.Add($"{mark}{isStatic}{fm.Groups[1].Value}{fm.Groups[3].Value}{ret}");
                    break;
                }
                case var k when k.StartsWith("init"):
                {
                    var im = Regex.Match(rest, @"^\s*(<[^>]*>)?\s*(\(.*?\))");
                    d.Members.Add($"{mark}{k}{(im.Success ? im.Groups[2].Value : "()")}");
                    break;
                }
                case "case":
                    if (d.Kind != "enum") break;
                    foreach (var c in SplitTopLevel(rest, ','))
                    {
                        var name = c.Trim();
                        var eq = name.IndexOf('=');
                        if (eq > 0) name = name[..eq].Trim();
                        if (name.Length > 0) d.Members.Add(name);
                    }
                    break;
                case "subscript":
                    d.Members.Add($"{mark}subscript{rest}");
                    break;
                case "associatedtype":
                    d.Members.Add($"{mark}associatedtype {rest.Split(':', '=')[0].Trim()}");
                    break;
            }
        }
    }

    // ---- the diagram ----

    static string Script(List<Decl> decls, Settings s)
    {
        var sb = new StringBuilder();
        sb.Append("@startuml\n' STYLE START\n");
        foreach (var h in s.HideShow) sb.Append(h).Append('\n');
        sb.Append("skinparam shadowing false\n");
        foreach (var k in s.Skinparams) sb.Append(k).Append('\n');
        sb.Append("' STYLE END\nset namespaceSeparator none\n\n\n");
        var types = decls.Where(d => !d.IsExtension && s.TypeAccess.Contains(d.Access)).ToList();
        var known = types.ToDictionary(d => d.Name, d => d, StringComparer.Ordinal);
        var extensions = decls.Where(d => d.IsExtension).ToList();
        if (s.Extensions == "merged")
            foreach (var e in extensions)
                if (known.TryGetValue(e.Name, out var t)) { t.Members.AddRange(e.Members); t.Inherits.AddRange(e.Inherits); }
        var relations = new List<string>();
        foreach (var t in types)
        {
            var (letter, color, label) = t.Kind switch
            {
                "class" => ("C", "DarkSeaGreen", ""),
                "struct" => ("S", "SkyBlue", " struct"),
                "enum" => ("E", "LightSteelBlue", " enum"),
                _ => ("P", "GoldenRod", " protocol"),
            };
            var display = t.Name + (s.ShowGenerics && t.Generics != null ? t.Generics : "");
            sb.Append($"{(t.Kind == "protocol" ? "protocol" : t.Kind == "enum" ? "enum" : t.Kind == "struct" ? "struct" : "class")} \"{display}\" as {Alias(t.Name)} << ({letter}, {color}){label} >> {{ \n");
            foreach (var m in t.Members) sb.Append("  ").Append(m).Append(" \n");
            sb.Append("}\n");
            for (var i = 0; i < t.Inherits.Count; i++)
            {
                var parent = Generic(t.Inherits[i]);
                var isClassParent = t.Kind == "class" && i == 0 && known.TryGetValue(parent, out var p) && p.Kind == "class";
                relations.Add(isClassParent ? $"{Alias(t.Name)} --|> {Alias(parent)} : inherits" : $"{Alias(t.Name)} ..|> {Alias(parent)} : conforms to");
            }
        }
        if (s.Extensions == "all")
        {
            var n = 0;
            foreach (var e in extensions)
            {
                var alias = $"{Alias(e.Name)}_ext{n++}";
                sb.Append($"extension \"{e.Name}\" as {alias} << (X, Orchid) extension >> {{ \n");
                foreach (var m in e.Members) sb.Append("  ").Append(m).Append(" \n");
                sb.Append("}\n");
                relations.Add($"{Alias(e.Name)} <.. {alias} : ext");
                foreach (var i in e.Inherits) relations.Add($"{alias} ..|> {Alias(Generic(i))} : conforms to");
            }
        }
        foreach (var r in relations.Distinct()) sb.Append(r).Append('\n');
        sb.Append("@enduml\n");
        return sb.ToString();
    }

    static string Generic(string type) => type.Contains('<') ? type[..type.IndexOf('<')].Trim() : type.Trim();
    static string Alias(string name) => Regex.Replace(name, @"[^\w]", "_");

    // PlantUML's text encoding: raw deflate, then its own base64 alphabet.
    static string Encode(string text)
    {
        var ms = new MemoryStream();
        using (var z = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true)) z.Write(Encoding.UTF8.GetBytes(text));
        var data = ms.ToArray();
        const string alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-_";
        var sb = new StringBuilder();
        for (var i = 0; i < data.Length; i += 3)
        {
            int b1 = data[i], b2 = i + 1 < data.Length ? data[i + 1] : 0, b3 = i + 2 < data.Length ? data[i + 2] : 0;
            sb.Append(alphabet[b1 >> 2]).Append(alphabet[((b1 & 3) << 4) | (b2 >> 4)]).Append(alphabet[((b2 & 0xf) << 2) | (b3 >> 6)]).Append(alphabet[b3 & 0x3f]);
        }
        return sb.ToString();
    }
}
