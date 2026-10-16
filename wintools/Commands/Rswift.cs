using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace WinTools;

// rswift (R.swift): generate R.generated.swift, strongly typed accessors for an app's resources:
// images and colors (asset catalogs and loose image files), fonts (by PostScript name, read from the
// font files), localized strings (.strings, .stringsdict and .xcstrings tables, with the development
// value and locales in the doc comments), storyboards, nibs, and other bundled files. Resources come
// from the Xcode target's Copy Bundle Resources phase (--xcodeproj/--target, or Xcode's
// PROJECT_FILE_PATH/TARGET_NAME environment) or from --input-files. The generated code uses
// R.swift 7's RswiftResources types, so it compiles against the RswiftLibrary package.
static class Rswift
{
    const string Version = "7.8.0";
    const int ValidationExit = 64;   // swift-argument-parser's exit code for validation errors

    [ModuleInitializer]
    internal static void Register() => Commands.Add("rswift", "Generate strongly typed Swift accessors for app resources (R.swift)", Run);

    static readonly string[] AllGenerators = ["image", "string", "color", "data", "file", "font", "nib", "segue", "storyboard", "reuseIdentifier", "entitlements", "info", "id"];

    sealed class Options
    {
        public HashSet<string> Generators = [.. AllGenerators];
        public string Access = "internal", BundleSource = "finder", Xcodeproj, Target, Output, InputType;
        public List<string> Imports = [], InputFiles = [];
        public bool OmitMainLet;
    }

    static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            Console.WriteLine("""
                OVERVIEW: Generate R.generated.swift, strongly typed accessors for an app's resources.

                USAGE: rswift generate [options] <output-path>

                OPTIONS:
                  --generators <list>     image,string,color,data,file,font,nib,storyboard (default: all)
                  --omit-main-let         don't generate `let R`
                  --import <module>       extra import for the generated file (repeatable)
                  --access-level <level>  internal (default) or public
                  --bundle-source <src>   module (Bundle.module) or finder (default)
                  --input-type <type>     xcodeproj (default) or input-files
                  --xcodeproj <path>      the project (default: $PROJECT_FILE_PATH)
                  --target <name>         the target (default: $TARGET_NAME)
                  --input-files <files>   resource files, with --input-type input-files
                  --version               show the version
                """);
            return 0;
        }
        if (args[0] == "--version") { Console.WriteLine(Version); return 0; }
        if (args[0] != "generate") throw new UsageException($"unknown subcommand '{args[0]}' (see rswift --help)");
        var o = new Options();
        for (var i = 1; i < args.Length; i++)
        {
            var a = args[i];
            string Value() => ++i < args.Length ? args[i] : throw new UsageException($"missing value for '{a}'");
            switch (a)
            {
                case "--generators":
                    o.Generators = [.. Value().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
                    foreach (var g in o.Generators.Where(g => !AllGenerators.Contains(g)))
                        throw new UsageException($"unknown generator '{g}'");
                    break;
                case "--omit-main-let": o.OmitMainLet = true; break;
                case "--import": o.Imports.Add(Value()); break;
                case "--access-level": o.Access = Value() is "public" or "internal" ? args[i] : throw new UsageException("--access-level is public or internal"); break;
                case "--bundle-source": o.BundleSource = Value(); break;
                case "--input-type": o.InputType = Value(); break;
                case "--xcodeproj": o.Xcodeproj = Value(); break;
                case "--target": o.Target = Value(); break;
                case "--rswift-ignore": Value(); break;
                case "--": break;   // ends --input-files
                case "--input-files":
                    while (i + 1 < args.Length && !args[i + 1].StartsWith("--")) o.InputFiles.Add(args[++i]);
                    break;
                default:
                    if (a.StartsWith("--")) throw new UsageException($"unknown option '{a}'");
                    o.Output = a;
                    break;
            }
        }
        var env = Environment.GetEnvironmentVariables();
        string Env(string k) => env[k] as string;
        o.InputType ??= o.InputFiles.Count > 0 ? "input-files" : "xcodeproj";
        List<string> files;
        string targetName;
        if (o.InputType == "input-files")
        {
            files = o.InputFiles.Select(Path.GetFullPath).ToList();
            targetName = o.Target ?? Env("TARGET_NAME") ?? "App";
        }
        else
        {
            var proj = o.Xcodeproj ?? Env("PROJECT_FILE_PATH");
            if (proj == null) { Console.Error.WriteLine("Error: Missing argument PROJECT_FILE_PATH"); return ValidationExit; }
            targetName = o.Target ?? Env("TARGET_NAME");
            if (targetName == null) { Console.Error.WriteLine("Error: Missing argument TARGET_NAME"); return ValidationExit; }
            try { files = TargetResources(proj, targetName); }
            catch (XcProjException e) { Console.Error.WriteLine($"Error: {e.Message}"); return 1; }
        }
        if (o.Output == null)
        {
            // Xcode's build phase passes the output through SCRIPT_OUTPUT_FILE_0.
            o.Output = Env("SCRIPT_OUTPUT_FILE_0");
            if (o.Output == null) { Console.Error.WriteLine("Error: Missing expected argument '<output-path>'"); return ValidationExit; }
        }
        var output = o.Output;
        if (Directory.Exists(output)) output = Path.Combine(output, "R.generated.swift");
        var code = Generate(files, o);
        // Like R.swift, only touch the file when it changes, so Xcode doesn't rebuild needlessly.
        if (!File.Exists(output) || File.ReadAllText(output) != code) File.WriteAllText(output, code);
        return 0;
    }

    static List<string> TargetResources(string projPath, string target)
    {
        var p = XcProjFile.Load(projPath);
        var t = p.TargetNamed(target) ?? throw new XcProjException($"Target '{target}' not found in project file");
        var phase = p.Phase(t, "PBXResourcesBuildPhase");
        var files = new List<string>();
        if (phase == null) return files;
        foreach (var bf in p.PhaseFiles(phase))
        {
            var r = p.BuildFileRef(bf);
            if (r == null) continue;
            if (p.Isa(r) == "PBXVariantGroup")
                files.AddRange(p.Ids(p.Obj(r), "children").Select(p.AbsolutePath).Where(x => x != null));
            else if (p.AbsolutePath(r) is { } abs) files.Add(abs);
        }
        return files;
    }

    // ---- resources ----

    sealed record Image(string Name, string Path);
    sealed record StringTable(string Name, Dictionary<string, List<(string Locale, string Value)>> Keys);

    static string Generate(List<string> files, Options o)
    {
        var images = new List<Image>();
        var colors = new List<Image>();
        var data = new List<Image>();
        var fonts = new List<(string Name, string File)>();
        var other = new List<string>();
        var storyboards = new List<string>();
        var nibs = new List<string>();
        var strings = new SortedDictionary<string, StringTable>(StringComparer.Ordinal);
        foreach (var f in files.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var ext = Path.GetExtension(f).ToLowerInvariant();
            var name = Path.GetFileNameWithoutExtension(f);
            var locale = Path.GetFileName(Path.GetDirectoryName(f) ?? "") is var dir && dir.EndsWith(".lproj", StringComparison.OrdinalIgnoreCase) ? dir[..^6] : null;
            switch (ext)
            {
                case ".xcassets" when Directory.Exists(f): Catalog(f, f, "", images, colors, data); break;
                case ".png" or ".jpg" or ".jpeg" or ".pdf" or ".gif" or ".heic" or ".tif" or ".tiff" or ".svg":
                    images.Add(new Image(ImageName(Path.GetFileName(f)), ""));
                    break;
                case ".ttf" or ".otf" or ".ttc":
                    if (File.Exists(f) && FontName(f) is { } ps) fonts.Add((ps, Path.GetFileName(f)));
                    break;
                case ".strings" when File.Exists(f):
                    AddStrings(strings, name, locale, XcPlist.ParseStrings(XcPlist.Decode(File.ReadAllBytes(f)), f).Select(kv => (kv.Key, XcPlist.Scalar(kv.Value))));
                    break;
                case ".stringsdict" when File.Exists(f):
                    if (XcPlist.Load(f) is OrderedDictionary<string, object> sd) AddStrings(strings, name, locale, sd.Keys.Select(k => (k, (string)null)));
                    break;
                case ".xcstrings" when File.Exists(f): XcStrings(strings, name, f); break;
                case ".storyboard": storyboards.Add(name); break;
                case ".xib" or ".nib": nibs.Add(name); break;
                default:
                    if (File.Exists(f)) other.Add(Path.GetFileName(f));
                    break;
            }
        }

        var pub = o.Access == "public" ? "public " : "";
        var sb = new StringBuilder();
        sb.Append("//\n// This is a generated file, do not edit!\n// Generated by R.swift, see https://github.com/mac-cain13/R.swift\n//\n\n");
        sb.Append("import Foundation\nimport RswiftResources\n");
        foreach (var imp in o.Imports.Distinct()) sb.Append($"import {imp}\n");
        sb.Append("#if canImport(UIKit)\nimport UIKit\n#elseif canImport(AppKit)\nimport AppKit\n#endif\n\n");
        if (!o.OmitMainLet)
        {
            if (o.BundleSource == "module") sb.Append($"{pub}let R = _R(bundle: Bundle.module)\n\n");
            else sb.Append($"private class BundleFinder {{}}\n{pub}let R = _R(bundle: Bundle(for: BundleFinder.self))\n\n");
        }
        sb.Append($"{pub}struct _R {{\n  {pub}let bundle: Foundation.Bundle\n");
        var gens = o.Generators;
        if (gens.Contains("string")) sb.Append($"  {pub}var string: string {{ .init(bundle: bundle, preferredLanguages: nil, locale: nil) }}\n");
        if (gens.Contains("color")) sb.Append($"  {pub}var color: color {{ .init(bundle: bundle) }}\n");
        if (gens.Contains("image")) sb.Append($"  {pub}var image: image {{ .init(bundle: bundle) }}\n");
        if (gens.Contains("data")) sb.Append($"  {pub}var data: data {{ .init(bundle: bundle) }}\n");
        if (gens.Contains("font")) sb.Append($"  {pub}var font: font {{ .init(bundle: bundle) }}\n");
        if (gens.Contains("file")) sb.Append($"  {pub}var file: file {{ .init(bundle: bundle) }}\n");
        if (gens.Contains("nib")) sb.Append($"  {pub}var nib: nib {{ .init(bundle: bundle) }}\n");
        if (gens.Contains("storyboard")) sb.Append($"  {pub}var storyboard: storyboard {{ .init(bundle: bundle) }}\n");
        sb.Append($"\n  {pub}func string(bundle: Foundation.Bundle) -> string {{ .init(bundle: bundle, preferredLanguages: nil, locale: nil) }}\n");

        if (gens.Contains("string"))
        {
            sb.Append($"\n  /// This `_R.string` struct is generated, and contains static references to {strings.Count} localization tables.\n");
            sb.Append($"  {pub}struct string {{\n    let bundle: Foundation.Bundle\n    let preferredLanguages: [String]?\n    let locale: Locale?\n");
            foreach (var t in strings.Values)
                sb.Append($"    {pub}var {Ident(t.Name)}: {Ident(t.Name)} {{ .init(source: .init(bundle: bundle, tableName: \"{Esc(t.Name)}\", preferredLanguages: preferredLanguages, locale: locale)) }}\n");
            foreach (var t in strings.Values)
            {
                sb.Append($"\n    /// This `_R.string.{Ident(t.Name)}` struct is generated, and contains static references to {t.Keys.Count} localization keys.\n");
                sb.Append($"    {pub}struct {Ident(t.Name)} {{\n      let source: RswiftResources.StringResource.Source\n");
                foreach (var (key, values) in Unique(t.Keys.OrderBy(k => k.Key, StringComparer.Ordinal).Select(kv => (Ident(kv.Key), kv))))
                {
                    var dev = values.Value.FirstOrDefault(v => v.Locale is null or "Base" or "en").Value ?? values.Value.FirstOrDefault().Value;
                    if (dev != null) sb.Append($"\n      /// Value: {Doc(dev)}\n");
                    else sb.Append('\n');
                    var locales = values.Value.Select(v => v.Locale).Where(l => l != null).Distinct().Order().ToList();
                    if (locales.Count > 0) sb.Append($"      ///\n      /// Locales: {string.Join(", ", locales)}\n");
                    var devArg = dev == null ? "nil" : $"\"{Esc(dev)}\"";
                    sb.Append($"      {pub}var {key}: RswiftResources.StringResource {{ .init(key: \"{Esc(values.Key)}\", tableName: \"{Esc(t.Name)}\", source: source, developmentValue: {devArg}, comment: nil) }}\n");
                }
                sb.Append("    }\n");
            }
            sb.Append("  }\n");
        }
        void Simple(string gen, string kind, IEnumerable<(string Key, string Body, string Doc)> items)
        {
            if (!gens.Contains(gen)) return;
            var list = Unique(items.Select(i => (Ident(i.Key), i))).ToList();
            sb.Append($"\n  /// This `_R.{gen}` struct is generated, and contains static references to {list.Count} {kind}.\n");
            sb.Append($"  {pub}struct {gen} {{\n    let bundle: Foundation.Bundle\n");
            foreach (var (id, item) in list)
                sb.Append($"\n    /// {item.Doc}\n    {pub}var {id}: {item.Body}\n");
            sb.Append("  }\n");
        }
        Simple("color", "colors", colors.Select(c => (c.Name, $"RswiftResources.ColorResource {{ .init(name: \"{Esc(c.Name)}\", path: [{PathList(c.Path)}], bundle: bundle) }}", $"Color `{c.Name}`.")));
        Simple("image", "images", images.Select(c => (c.Name, $"RswiftResources.ImageResource {{ .init(name: \"{Esc(Join(c.Path, c.Name))}\", path: [{PathList(c.Path)}], bundle: bundle, locale: nil, onDemandResourceTags: nil) }}", $"Image `{c.Name}`.")));
        Simple("data", "data assets", data.Select(c => (c.Name, $"RswiftResources.DataResource {{ .init(name: \"{Esc(Join(c.Path, c.Name))}\", path: [{PathList(c.Path)}], bundle: bundle, onDemandResourceTags: nil) }}", $"Data asset `{c.Name}`.")));
        Simple("font", "fonts", fonts.Select(f => (f.Name, $"RswiftResources.FontResource {{ .init(name: \"{Esc(f.Name)}\", bundle: bundle, filename: \"{Esc(f.File)}\") }}", $"Font `{f.Name}`.")));
        Simple("file", "resource files", other.Select(f => (f, $"RswiftResources.FileResource {{ .init(name: \"{Esc(Path.GetFileNameWithoutExtension(f))}\", pathExtension: \"{Esc(Path.GetExtension(f).TrimStart('.'))}\", bundle: bundle, locale: LocaleReference.none) }}", $"Resource file `{f}`.")));
        Simple("nib", "nibs", nibs.Distinct().Select(n => (n, $"RswiftResources.NibReference<UIKit.UIView> {{ .init(name: \"{Esc(n)}\", bundle: bundle) }}", $"Nib `{n}`.")));
        Simple("storyboard", "storyboards", storyboards.Distinct().Select(n => (n, $"RswiftResources.StoryboardReference {{ .init(name: \"{Esc(n)}\", bundle: bundle) }}", $"Storyboard `{n}`.")));
        sb.Append("}\n");
        return sb.ToString();
    }

    // Identifiers that came out the same are dropped with a warning, as R.swift does.
    static IEnumerable<(string Id, T Item)> Unique<T>(IEnumerable<(string Id, T Item)> items)
    {
        var seen = new HashSet<string>();
        foreach (var (id, item) in items)
        {
            if (id.Length == 0 || !seen.Add(id)) { Console.Error.WriteLine($"warning: [R.swift] Skipping resource because its identifier '{id}' is a duplicate or empty"); continue; }
            yield return (id, item);
        }
    }

    static string Join(string path, string name) => path.Length == 0 ? name : path + "/" + name;
    static string PathList(string path) => path.Length == 0 ? "" : string.Join(", ", path.Split('/').Select(p => $"\"{Esc(p)}\""));

    static void AddStrings(SortedDictionary<string, StringTable> tables, string table, string locale, IEnumerable<(string Key, string Value)> entries)
    {
        if (!tables.TryGetValue(table, out var t)) tables[table] = t = new StringTable(table, []);
        foreach (var (k, v) in entries)
        {
            if (!t.Keys.TryGetValue(k, out var list)) t.Keys[k] = list = [];
            list.Add((locale, v));
        }
    }

    // A String Catalog: {"sourceLanguage": "en", "strings": {key: {"localizations": {lang: {"stringUnit": {"value": ...}}}}}}.
    static void XcStrings(SortedDictionary<string, StringTable> tables, string table, string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var source = doc.RootElement.TryGetProperty("sourceLanguage", out var sl) ? sl.GetString() : "en";
        if (!doc.RootElement.TryGetProperty("strings", out var strs)) return;
        foreach (var s in strs.EnumerateObject())
        {
            var entries = new List<(string, string)>();
            if (s.Value.TryGetProperty("localizations", out var locs))
                foreach (var l in locs.EnumerateObject())
                {
                    var value = l.Value.TryGetProperty("stringUnit", out var unit) && unit.TryGetProperty("value", out var v) ? v.GetString() : null;
                    entries.Add((l.Name, value));
                }
            if (entries.Count == 0) entries.Add((source, s.Name));
            if (!tables.TryGetValue(table, out var t)) tables[table] = t = new StringTable(table, []);
            t.Keys[s.Name] = entries.Select(e => ((string)e.Item1, e.Item2)).ToList();
        }
    }

    // Walks an asset catalog: *.imageset, *.colorset, *.dataset; folders that provide a namespace
    // add their name to the asset path.
    static void Catalog(string dir, string catalog, string ns, List<Image> images, List<Image> colors, List<Image> data)
    {
        foreach (var sub in Directory.EnumerateDirectories(dir).Order(StringComparer.Ordinal))
        {
            var name = Path.GetFileNameWithoutExtension(sub);
            switch (Path.GetExtension(sub).ToLowerInvariant())
            {
                case ".imageset" or ".symbolset": images.Add(new Image(name, ns)); break;
                case ".colorset": colors.Add(new Image(name, ns)); break;
                case ".dataset": data.Add(new Image(name, ns)); break;
                case "":
                    var contents = Path.Combine(sub, "Contents.json");
                    var provides = File.Exists(contents) && File.ReadAllText(contents).Contains("\"provides-namespace\" : true");
                    Catalog(sub, catalog, provides ? Join(ns, name) : ns, images, colors, data);
                    break;
            }
        }
    }

    // logo@2x~ipad.png -> logo
    static string ImageName(string file)
    {
        var name = Path.GetFileNameWithoutExtension(file);
        var tilde = name.IndexOf('~');
        if (tilde > 0) name = name[..tilde];
        var at = name.LastIndexOf('@');
        if (at > 0 && name[(at + 1)..] is "1x" or "2x" or "3x") name = name[..at];
        return name;
    }

    // The PostScript name (name ID 6) of a TrueType/OpenType font (first font of a collection).
    internal static string FontName(string path)
    {
        try
        {
            var d = File.ReadAllBytes(path);
            var font = 0;
            if (d.Length > 16 && Encoding.ASCII.GetString(d, 0, 4) == "ttcf") font = (int)BinaryPrimitives.ReadUInt32BigEndian(d.AsSpan(12));
            var tables = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(font + 4));
            for (var i = 0; i < tables; i++)
            {
                var rec = font + 12 + i * 16;
                if (Encoding.ASCII.GetString(d, rec, 4) != "name") continue;
                var off = (int)BinaryPrimitives.ReadUInt32BigEndian(d.AsSpan(rec + 8));
                var count = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(off + 2));
                var strings = off + BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(off + 4));
                string mac = null;
                for (var n = 0; n < count; n++)
                {
                    var r = off + 6 + n * 12;
                    var platform = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(r));
                    var nameId = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(r + 6));
                    var len = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(r + 8));
                    var start = strings + BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(r + 10));
                    if (nameId != 6 || start + len > d.Length) continue;
                    if (platform is 0 or 3) return Encoding.BigEndianUnicode.GetString(d, start, len);
                    if (platform == 1) mac ??= Encoding.Latin1.GetString(d, start, len);
                }
                return mac;
            }
        }
        catch (ArgumentOutOfRangeException) { }
        catch (IOException) { }
        return null;
    }

    // ---- Swift text ----

    static readonly HashSet<string> Keywords =
    [
        "associatedtype", "class", "deinit", "enum", "extension", "fileprivate", "func", "import", "init", "inout", "internal", "let", "open",
        "operator", "private", "protocol", "public", "rethrows", "static", "struct", "subscript", "typealias", "var", "break", "case", "continue",
        "default", "defer", "do", "else", "fallthrough", "for", "guard", "if", "in", "repeat", "return", "switch", "where", "while", "as", "Any",
        "catch", "false", "is", "nil", "super", "self", "Self", "throw", "throws", "true", "try", "Type",
    ];

    // "app-icon_large 2" -> appIcon_large2 (R.swift's lowerCamelCase sanitizing).
    internal static string Ident(string name)
    {
        var parts = new List<string>();
        var current = new StringBuilder();
        foreach (var c in name)
        {
            if (char.IsLetterOrDigit(c) || c == '_') current.Append(c);
            else if (current.Length > 0) { parts.Add(current.ToString()); current.Clear(); }
        }
        if (current.Length > 0) parts.Add(current.ToString());
        if (parts.Count == 0) return "";
        var sb = new StringBuilder();
        for (var i = 0; i < parts.Count; i++)
        {
            var p = parts[i];
            if (i == 0)
            {
                // Lowercase the leading run of capitals (URLSession -> urlSession, Logo -> logo).
                var upper = 0;
                while (upper < p.Length && char.IsUpper(p[upper])) upper++;
                var lower = upper > 1 && upper < p.Length ? upper - 1 : upper;
                sb.Append(p[..lower].ToLowerInvariant()).Append(p[lower..]);
            }
            else sb.Append(char.ToUpperInvariant(p[0])).Append(p[1..]);
        }
        var id = sb.ToString();
        if (char.IsDigit(id[0])) id = "_" + id;
        return Keywords.Contains(id) ? $"`{id}`" : id;
    }

    static string Esc(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    static string Doc(string s) => s.Replace("\n", " ").Replace("\r", "");
}
