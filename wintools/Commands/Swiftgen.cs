using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace WinTools;

// swiftgen: generate Swift code for an app's resources so they are type-safe constants. Parsers:
// colors (.txt, .json, .xml, .clr), strings (.strings, .stringsdict), xcassets (images, colors,
// data, symbols), fonts (.ttf/.otf, names read from the font files), files, ib (storyboard scenes
// and segues), coredata (entities and attributes), json, yaml and plist. Each parser has SwiftGen's
// stock swift5 templates (swift5, structured-swift5/flat-swift5, scenes-swift5/segues-swift5,
// inline-swift5) with their common params (enumName, publicAccess, ...). Runs one parser
// (`swiftgen run <parser> ...`) or a swiftgen.yml config (`swiftgen config run|lint`). Custom
// Stencil templates (--templatePath) are not supported: the stock templates are built in.
static class Swiftgen
{
    const string Version = "6.6.3";

    [ModuleInitializer]
    internal static void Register() => Commands.Add("swiftgen", "Generate Swift code for colors, strings, assets, fonts and more (SwiftGen)", Run);

    static readonly Dictionary<string, string[]> Templates = new()
    {
        ["colors"] = ["swift5"],
        ["coredata"] = ["swift5"],
        ["files"] = ["structured-swift5", "flat-swift5"],
        ["fonts"] = ["swift5"],
        ["ib"] = ["scenes-swift5", "segues-swift5"],
        ["json"] = ["inline-swift5"],
        ["plist"] = ["inline-swift5"],
        ["strings"] = ["structured-swift5", "flat-swift5"],
        ["xcassets"] = ["swift5"],
        ["yaml"] = ["inline-swift5"],
    };

    static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            Console.WriteLine("""
                Usage: swiftgen <command> [options]

                Commands:
                  run <parser> [--templateName <name>] [--param <k=v>]... [--output <file>] [--filter <regex>] <paths>...
                      parsers: colors, coredata, files, fonts, ib, json, plist, strings, xcassets, yaml
                  config run [--config swiftgen.yml] [--verbose]
                  config lint [--config swiftgen.yml]
                  config init [--config swiftgen.yml]
                  template list
                  --version
                """);
            return 0;
        }
        if (args[0] == "--version") { Console.WriteLine($"SwiftGen v{Version} (Stencil v0.15.1, StencilSwiftKit v2.10.1, SwiftGenKit v{Version})"); return 0; }
        try
        {
            return args[0] switch
            {
                "run" => RunParser(args[1..]),
                "config" => Config(args[1..]),
                "template" => Template(args[1..]),
                _ => throw new UsageException($"unknown command '{args[0]}' (see swiftgen --help)"),
            };
        }
        catch (SwiftgenError e)
        {
            Console.Error.WriteLine($"error: {e.Message}");
            return 1;
        }
    }

    sealed class SwiftgenError(string message) : Exception(message);

    sealed class Job
    {
        public string Parser, Template = null, Output, Filter;
        public List<string> Inputs = [];
        public Dictionary<string, object> Params = [];
    }

    static int RunParser(string[] args)
    {
        if (args.Length == 0 || args[0].StartsWith('-')) throw new UsageException("missing parser (colors, coredata, files, fonts, ib, json, plist, strings, xcassets, yaml)");
        var job = new Job { Parser = args[0] };
        if (!Templates.ContainsKey(job.Parser)) throw new UsageException($"unknown parser '{job.Parser}'");
        for (var i = 1; i < args.Length; i++)
        {
            var a = args[i];
            string Value() => ++i < args.Length ? args[i] : throw new UsageException($"missing value for {a}");
            switch (a)
            {
                case "--templateName" or "-n": job.Template = Value(); break;
                case "--templatePath" or "-p": throw new SwiftgenError("custom Stencil templates (--templatePath) aren't supported; use one of the stock templates (--templateName)");
                case "--param":
                    var kv = Value();
                    var eq = kv.IndexOf('=');
                    if (eq < 0) job.Params[kv] = true; else job.Params[kv[..eq]] = kv[(eq + 1)..];
                    break;
                case "--output" or "-o": job.Output = Value(); break;
                case "--filter" or "-f": job.Filter = Value(); break;
                case "--options": Value(); break;
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unknown option {a}");
                    job.Inputs.Add(a);
                    break;
            }
        }
        if (job.Inputs.Count == 0) throw new UsageException("missing input paths");
        job.Template ??= Templates[job.Parser][0];
        Execute(job, verbose: false);
        return 0;
    }

    static void Execute(Job job, bool verbose)
    {
        if (!Templates[job.Parser].Contains(job.Template))
            throw new SwiftgenError($"Template named {job.Template} not found. Use `swiftgen template list` to list available named templates or use --templatePath to specify a template by its full path.");
        foreach (var input in job.Inputs.Where(i => !File.Exists(i) && !Directory.Exists(i)))
            throw new SwiftgenError($"Unable to parse file at {input}. File not found.");
        var code = Generate(job);
        if (job.Output == null) { Console.Write(code); return; }
        var dir = Path.GetDirectoryName(Path.GetFullPath(job.Output));
        if (dir != null) Directory.CreateDirectory(dir);
        if (File.Exists(job.Output) && File.ReadAllText(job.Output) == code)
        {
            if (verbose) Console.WriteLine($"Not writing the file as content is unchanged");
            return;
        }
        File.WriteAllText(job.Output, code);
        if (verbose) Console.WriteLine($"File written: {job.Output}");
    }

    // ---- swiftgen.yml ----

    static int Config(string[] args)
    {
        if (args.Length == 0) throw new UsageException("missing config subcommand (run, lint, init)");
        var sub = args[0];
        var path = "swiftgen.yml";
        var verbose = false;
        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--config" or "-c": path = ++i < args.Length ? args[i] : throw new UsageException("--config needs a path"); break;
                case "--verbose" or "-v": verbose = true; break;
                case "--quiet" or "-q": break;
                default: throw new UsageException($"unknown option {args[i]}");
            }
        }
        if (sub == "init")
        {
            if (File.Exists(path)) throw new SwiftgenError($"Config file already exists at {path}");
            File.WriteAllText(path, """
                # Note: all of the config entries below are just examples with placeholders. Be sure to edit and adjust to your needs when uncommenting.

                ## In case your config entries all use a common input/output parent directory, you can specify those here.
                # input_dir: MyLib/Sources/
                # output_dir: MyLib/Generated/

                ## Generate constants for your localized strings.
                # strings:
                #   inputs:
                #     - Resources/en.lproj
                #   outputs:
                #     - templateName: structured-swift5
                #       output: Strings+Generated.swift

                ## Generate constants for your Assets Catalogs.
                # xcassets:
                #   inputs:
                #     - Main.xcassets
                #   outputs:
                #     - templateName: swift5
                #       output: Assets+Generated.swift

                """);
            Console.WriteLine($"Example configuration file created: {path}");
            return 0;
        }
        if (!File.Exists(path)) throw new SwiftgenError($"Config file not found at {path}");
        var config = XcYaml.Parse(File.ReadAllText(path)) as OrderedDictionary<string, object> ?? throw new SwiftgenError("Config file is not a mapping");
        var baseDir = Path.GetDirectoryName(Path.GetFullPath(path))!;
        var inputDir = XcPlist.Str(config, "input_dir") ?? "";
        var outputDir = XcPlist.Str(config, "output_dir") ?? "";
        var jobs = new List<Job>();
        foreach (var (parser, value) in config)
        {
            if (parser is "input_dir" or "output_dir") continue;
            if (!Templates.ContainsKey(parser)) throw new SwiftgenError($"Unknown parser '{parser}' in config file");
            foreach (var entry in value is List<object> list ? list : [value])
            {
                var e = entry as OrderedDictionary<string, object> ?? throw new SwiftgenError($"Invalid entry for {parser}");
                var inputs = e.TryGetValue("inputs", out var inp) ? inp is List<object> il ? il.Select(XcPlist.Scalar).ToList() : [XcPlist.Scalar(inp)] : [];
                var filter = XcPlist.Str(e, "filter");
                var outputs = e.TryGetValue("outputs", out var outp) ? outp is List<object> ol ? ol : [outp] : [];
                foreach (var o in outputs.OfType<OrderedDictionary<string, object>>())
                {
                    if (XcPlist.Str(o, "templatePath") != null) throw new SwiftgenError("custom Stencil templates (templatePath) aren't supported; use templateName");
                    var job = new Job
                    {
                        Parser = parser,
                        Template = XcPlist.Str(o, "templateName") ?? throw new SwiftgenError($"Missing templateName for {parser}"),
                        Output = Path.Combine(baseDir, outputDir, XcPlist.Str(o, "output") ?? throw new SwiftgenError($"Missing output for {parser}")),
                        Filter = filter,
                        Inputs = inputs.Select(x => Path.Combine(baseDir, inputDir, x)).ToList(),
                    };
                    foreach (var (k, v) in XcPlist.Dict(o, "params") ?? []) job.Params[k] = v;
                    jobs.Add(job);
                }
            }
        }
        if (sub == "lint")
        {
            foreach (var j in jobs)
            {
                Console.WriteLine($"> Common parent directory used for all input paths:  {inputDir}");
                Console.WriteLine($"> Common parent directory used for all output paths: {outputDir}");
                Console.WriteLine($"> [{j.Parser}] Inputs: {string.Join(", ", j.Inputs)}; template: {j.Template}; output: {j.Output}");
                if (!Templates[j.Parser].Contains(j.Template)) throw new SwiftgenError($"Template named {j.Template} not found for {j.Parser}");
            }
            return 0;
        }
        if (sub != "run") throw new UsageException($"unknown config subcommand '{sub}'");
        foreach (var j in jobs)
        {
            if (verbose) Console.WriteLine($"Executing configuration entry for {j.Parser}");
            Execute(j, verbose);
        }
        return 0;
    }

    static int Template(string[] args)
    {
        if (args.Length == 0 || args[0] != "list") throw new UsageException("usage: swiftgen template list");
        foreach (var (parser, names) in Templates.OrderBy(kv => kv.Key))
        {
            Console.WriteLine($"{parser}:");
            Console.WriteLine("  custom:");
            Console.WriteLine("  bundled:");
            foreach (var n in names) Console.WriteLine($"   - {n}");
        }
        return 0;
    }

    // ---- generation ----

    sealed class Ctx(Job job)
    {
        public readonly Job Job = job;
        public string Access => P("publicAccess") is "true" or "YES" or "True" ? "public" : "internal";
        public string P(string key, string fallback = null) => Job.Params.TryGetValue(key, out var v) ? v is bool b ? (b ? "true" : "false") : XcPlist.Scalar(v) : fallback;
        public bool Flag(string key) => P(key) is "true" or "YES" or "True";
        public readonly StringBuilder Sb = new();
        public Ctx L(string line = "") { Sb.Append(line).Append('\n'); return this; }
    }

    static string Generate(Job job)
    {
        var c = new Ctx(job);
        c.L("// swiftlint:disable all").L("// Generated using SwiftGen — https://github.com/SwiftGen/SwiftGen").L();
        switch (job.Parser)
        {
            case "colors": Colors(c); break;
            case "strings": Strings(c); break;
            case "xcassets": Assets(c); break;
            case "fonts": Fonts(c); break;
            case "files": FilesGen(c); break;
            case "ib": Ib(c); break;
            case "coredata": CoreData(c); break;
            case "json" or "yaml" or "plist": Data(c); break;
        }
        return c.Sb.ToString();
    }

    static IEnumerable<string> Expand(Job job, params string[] exts)
    {
        var filter = job.Filter != null ? new Regex(job.Filter) : null;
        foreach (var input in job.Inputs)
        {
            IEnumerable<string> files = Directory.Exists(input) && !exts.Contains(Path.GetExtension(input).ToLowerInvariant())
                ? Directory.EnumerateFiles(input, "*", SearchOption.AllDirectories).Concat(Directory.EnumerateDirectories(input, "*", SearchOption.AllDirectories))
                    .Where(f => exts.Length == 0 || exts.Contains(Path.GetExtension(f).ToLowerInvariant())).Order(StringComparer.Ordinal)
                : [input];
            foreach (var f in files)
                if (filter == null || filter.IsMatch(f.Replace('\\', '/'))) yield return f;
        }
    }

    // ---- colors ----

    static void Colors(Ctx c)
    {
        var colors = new SortedDictionary<string, uint>(StringComparer.Ordinal);
        foreach (var f in Expand(c.Job, ".txt", ".json", ".xml", ".clr"))
        {
            switch (Path.GetExtension(f).ToLowerInvariant())
            {
                case ".json":
                    using (var doc = JsonDocument.Parse(File.ReadAllText(f)))
                        foreach (var p in doc.RootElement.EnumerateObject()) colors[p.Name] = ParseColor(p.Value.GetString(), colors);
                    break;
                case ".xml":
                    foreach (var e in XDocument.Load(f).Descendants("color"))
                        colors[(string)e.Attribute("name") ?? ""] = ParseColor(e.Value, colors);
                    break;
                case ".clr": throw new SwiftgenError($"{f}: .clr color lists are NSKeyedArchiver archives from macOS; export them as .txt, .json or .xml");
                default:
                    foreach (var line in File.ReadAllLines(f))
                    {
                        var t = line.Trim();
                        if (t.Length == 0 || t.StartsWith('#') || t.StartsWith("//")) continue;
                        var sep = t.IndexOf(':');
                        if (sep < 0) continue;
                        colors[t[..sep].Trim()] = ParseColor(t[(sep + 1)..].Trim(), colors);
                    }
                    break;
            }
        }
        var a = c.Access;
        var name = c.P("enumName", "ColorName");
        c.L("#if os(macOS)").L("  import AppKit.NSColor").L($"  {a} typealias Color = NSColor")
         .L("#elseif os(iOS) || os(tvOS) || os(watchOS)").L("  import UIKit.UIColor").L($"  {a} typealias Color = UIColor").L("#endif").L()
         .L("// swiftlint:disable superfluous_disable_command file_length implicit_return").L().L("// MARK: - Colors").L()
         .L("// swiftlint:disable identifier_name line_length type_body_length")
         .L($"{a} struct {name} {{").L($"  {a} let rgbaValue: UInt32").L($"  {a} var color: Color {{ return Color(named: self) }}");
        foreach (var (key, rgba) in colors)
        {
            var hex = (rgba >> 8).ToString("x6");
            c.L().L($"  /// <span style=\"display:block;width:3em;height:2em;border:1px solid black;background:#{hex}\"></span>")
             .L($"  /// Alpha: {(rgba & 0xff) * 100 / 255}% <br/> (0x{rgba:x8})")
             .L($"  {a} static let {LowerCamel(key)} = {name}(rgbaValue: 0x{rgba:x8})");
        }
        c.L("}").L("// swiftlint:enable identifier_name line_length type_body_length").L().L("// MARK: - Implementation Details").L()
         .L($"{a} extension Color {{").L("  convenience init(rgbaValue: UInt32) {").L("    let components = RGBAComponents(rgbaValue: rgbaValue).normalized")
         .L("    self.init(red: components[0], green: components[1], blue: components[2], alpha: components[3])").L("  }").L("}").L()
         .L("private struct RGBAComponents {").L("  let rgbaValue: UInt32").L().L("  private var shifts: [UInt32] {").L("    [")
         .L("      rgbaValue >> 24, // red").L("      rgbaValue >> 16, // green").L("      rgbaValue >> 8,  // blue").L("      rgbaValue        // alpha")
         .L("    ]").L("  }").L().L("  private var components: [CGFloat] {").L("    shifts.map { CGFloat($0 & 0xff) }").L("  }").L()
         .L("  fileprivate var normalized: [CGFloat] {").L("    components.map { $0 / 255.0 }").L("  }").L("}").L()
         .L($"{a} extension Color {{").L($"  convenience init(named color: {name}) {{").L("    self.init(rgbaValue: color.rgbaValue)").L("  }").L("}");
    }

    // #rgb, #rrggbb, #rrggbbaa (with or without #/0x), or another color's name.
    static uint ParseColor(string s, IDictionary<string, uint> known)
    {
        var t = s.Trim();
        if (known.TryGetValue(t, out var other)) return other;
        if (t.StartsWith('#')) t = t[1..];
        else if (t.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) t = t[2..];
        if (t.Length == 3) t = string.Concat(t.Select(ch => $"{ch}{ch}"));
        if (t.Length == 6) t += "ff";
        if (t.Length != 8 || !uint.TryParse(t, NumberStyles.HexNumber, null, out var v)) throw new SwiftgenError($"Invalid color value: {s}");
        return v;
    }

    // ---- strings ----

    sealed record Entry(string Key, string Value, List<string> Types);

    static void Strings(Ctx c)
    {
        var tables = new SortedDictionary<string, List<Entry>>(StringComparer.Ordinal);
        foreach (var f in Expand(c.Job, ".strings", ".stringsdict"))
        {
            if (Directory.Exists(f)) continue;
            var table = Path.GetFileNameWithoutExtension(f);
            if (!tables.TryGetValue(table, out var list)) tables[table] = list = [];
            if (f.EndsWith(".stringsdict", StringComparison.OrdinalIgnoreCase))
            {
                if (XcPlist.Load(f) is not OrderedDictionary<string, object> d) continue;
                foreach (var (key, v) in d)
                {
                    var format = XcPlist.Str(v as OrderedDictionary<string, object>, "NSStringLocalizedFormatKey") ?? "";
                    // Variables of the plural rules contribute their own format types.
                    var types = new List<string>();
                    foreach (var (vk, vv) in v as OrderedDictionary<string, object> ?? [])
                        if (XcPlist.Str(vv as OrderedDictionary<string, object>, "NSStringFormatValueTypeKey") is { } vt) types.Add(FormatType(vt));
                    if (types.Count == 0) types = Placeholders(format);
                    list.RemoveAll(e => e.Key == key);
                    list.Add(new Entry(key, format, types));
                }
            }
            else
            {
                foreach (var (key, v) in XcPlist.ParseStrings(XcPlist.Decode(File.ReadAllBytes(f)), f))
                {
                    var value = XcPlist.Scalar(v) ?? "";
                    if (list.Any(e => e.Key == key)) continue;
                    list.Add(new Entry(key, value, Placeholders(value)));
                }
            }
        }
        var a = c.Access;
        var enumName = c.P("enumName", "L10n");
        var structured = c.Job.Template == "structured-swift5";
        c.L("import Foundation").L().L("// swiftlint:disable superfluous_disable_command file_length implicit_return prefer_self_in_static_references").L()
         .L("// MARK: - Strings").L().L("// swiftlint:disable explicit_type_interface function_parameter_count identifier_name line_length")
         .L("// swiftlint:disable nesting type_body_length type_name vertical_whitespace_opening_braces")
         .L($"{a} enum {enumName} {{");
        var single = tables.Count == 1;
        foreach (var (table, entries) in tables)
        {
            var indent = single ? "  " : "    ";
            if (!single) c.L($"  {a} enum {UpperCamel(table)} {{");
            if (structured) StructuredStrings(c, Tree(entries), table, enumName, indent);
            else foreach (var e in entries.OrderBy(e => e.Key, StringComparer.Ordinal)) StringEntry(c, e, LowerCamel(e.Key), table, enumName, indent);
            if (!single) c.L("  }");
        }
        c.L("}").L("// swiftlint:enable explicit_type_interface function_parameter_count identifier_name line_length")
         .L("// swiftlint:enable nesting type_body_length type_name vertical_whitespace_opening_braces").L()
         .L("// MARK: - Implementation Details").L().L($"extension {enumName} {{")
         .L("  private static func tr(_ table: String, _ key: String, _ args: CVarArg..., fallback value: String) -> String {")
         .L("    let format = BundleToken.bundle.localizedString(forKey: key, value: value, table: table)")
         .L("    return String(format: format, locale: Locale.current, arguments: args)").L("  }").L("}").L()
         .L("// swiftlint:disable convenience_type").L("private final class BundleToken {").L("  static let bundle: Bundle = {")
         .L("    #if SWIFT_PACKAGE").L("    return Bundle.module").L("    #else").L("    return Bundle(for: BundleToken.self)").L("    #endif")
         .L("  }()").L("}").L("// swiftlint:enable convenience_type");
    }

    sealed class Node
    {
        public readonly SortedDictionary<string, Node> Children = new(StringComparer.Ordinal);
        public readonly List<(Entry Entry, string Name)> Entries = [];
    }

    // Keys split on "." become nested enums (structured-swift5).
    static Node Tree(List<Entry> entries)
    {
        var root = new Node();
        foreach (var e in entries.OrderBy(e => e.Key, StringComparer.Ordinal))
        {
            var parts = e.Key.Split('.');
            var n = root;
            foreach (var p in parts[..^1].Where(p => p.Length > 0))
            {
                if (!n.Children.TryGetValue(p, out var child)) n.Children[p] = child = new Node();
                n = child;
            }
            n.Entries.Add((e, parts[^1].Length > 0 ? parts[^1] : e.Key));
        }
        return root;
    }

    static void StructuredStrings(Ctx c, Node n, string table, string enumName, string indent)
    {
        foreach (var (e, name) in n.Entries) StringEntry(c, e, LowerCamel(name), table, enumName, indent);
        foreach (var (name, child) in n.Children)
        {
            c.L($"{indent}{c.Access} enum {UpperCamel(name)} {{");
            StructuredStrings(c, child, table, enumName, indent + "  ");
            c.L($"{indent}}}");
        }
    }

    static void StringEntry(Ctx c, Entry e, string name, string table, string enumName, string indent)
    {
        var a = c.Access;
        foreach (var line in e.Value.Split('\n')) c.L($"{indent}/// {line.Replace("*/", "*\\/")}");
        var fallback = SwiftString(e.Value);
        if (e.Types.Count == 0)
        {
            c.L($"{indent}{a} static let {name} = {enumName}.tr(\"{Esc(table)}\", \"{Esc(e.Key)}\", fallback: {fallback})");
            return;
        }
        var parameters = string.Join(", ", e.Types.Select((t, i) => $"_ p{i + 1}: {t}"));
        var args = string.Join(", ", e.Types.Select((t, i) => t == "Any" ? $"String(describing: p{i + 1})" : $"p{i + 1}"));
        c.L($"{indent}{a} static func {name}({parameters}) -> String {{")
         .L($"{indent}  return {enumName}.tr(\"{Esc(table)}\", \"{Esc(e.Key)}\", {args}, fallback: {fallback})").L($"{indent}}}");
    }

    static readonly Regex Placeholder = new(@"%(?:(\d+)\$)?[-+ #0]*\d*(?:\.\d+)?(hh|h|ll|l|q|L|z|t|j)?([@dDiuUxXoOfeEgGcCsSpaAF%])", RegexOptions.Compiled);

    // The Swift parameter types of a format string's placeholders, positional ones in order.
    static List<string> Placeholders(string format)
    {
        var byPos = new SortedDictionary<int, string>();
        var next = 1;
        foreach (Match m in Placeholder.Matches(format))
        {
            var conv = m.Groups[3].Value;
            if (conv == "%") continue;
            var pos = m.Groups[1].Success ? int.Parse(m.Groups[1].Value) : next++;
            byPos[pos] = FormatType(conv);
        }
        return byPos.Values.ToList();
    }

    static string FormatType(string conv) => conv.TrimStart('l', 'h', 'q', 'L') switch
    {
        "@" => "Any",
        "d" or "D" or "i" or "u" or "U" or "x" or "X" or "o" or "O" => "Int",
        "f" or "e" or "E" or "g" or "G" or "a" or "A" or "F" => "Float",
        "c" or "C" => "CChar",
        "s" or "S" => "UnsafePointer<CChar>",
        "p" => "UnsafeRawPointer",
        _ => "Any",
    };

    // ---- xcassets ----

    sealed record Asset(string Name, string Kind, string Path);

    static void Assets(Ctx c)
    {
        var a = c.Access;
        var enumName = c.P("enumName", "Asset");
        var catalogs = Expand(c.Job, ".xcassets").Where(Directory.Exists).ToList();
        c.L("#if os(macOS)").L("  import AppKit").L("#elseif os(iOS)").L("  import UIKit").L("#elseif os(tvOS) || os(watchOS)").L("  import UIKit").L("#endif")
         .L("#if canImport(SwiftUI)").L("  import SwiftUI").L("#endif").L()
         .L("// swiftlint:disable superfluous_disable_command file_length implicit_return").L().L("// MARK: - Asset Catalogs").L()
         .L("// swiftlint:disable identifier_name line_length nesting type_body_length type_name").L($"{a} enum {enumName} {{");
        var kinds = new HashSet<string>();
        var single = catalogs.Count == 1 && !c.Flag("forceFileNameEnum");
        foreach (var cat in catalogs)
        {
            var indent = single ? "  " : "    ";
            if (!single) c.L($"  {a} enum {UpperCamel(Path.GetFileNameWithoutExtension(cat))} {{");
            CatalogLevel(c, cat, "", indent, kinds);
            if (!single) c.L("  }");
        }
        c.L("}").L("// swiftlint:enable identifier_name line_length nesting type_body_length type_name").L().L("// MARK: - Implementation Details").L();
        if (kinds.Contains("color"))
            c.L($"{a} final class ColorAsset {{").L($"  {a} fileprivate(set) var name: String").L()
             .L("  #if os(macOS)").L($"  {a} typealias Color = NSColor").L("  #elseif os(iOS) || os(tvOS) || os(watchOS)").L($"  {a} typealias Color = UIColor").L("  #endif").L()
             .L($"  {a} private(set) lazy var color: Color = {{").L("    guard let color = Color(asset: self) else {").L("      fatalError(\"Unable to load color asset named \\(name).\")").L("    }").L("    return color").L("  }()").L()
             .L("  #if canImport(SwiftUI)").L($"  {a} private(set) lazy var swiftUIColor: SwiftUI.Color = {{").L("    SwiftUI.Color(asset: self)").L("  }()").L("  #endif").L()
             .L("  fileprivate init(name: String) {").L("    self.name = name").L("  }").L("}").L()
             .L($"{a} extension ColorAsset.Color {{").L("  convenience init?(asset: ColorAsset) {").L("    let bundle = BundleToken.bundle")
             .L("    #if os(iOS) || os(tvOS)").L("    self.init(named: asset.name, in: bundle, compatibleWith: nil)").L("    #elseif os(macOS)")
             .L("    self.init(named: NSColor.Name(asset.name), bundle: bundle)").L("    #elseif os(watchOS)").L("    self.init(named: asset.name)").L("    #endif").L("  }").L("}").L()
             .L("#if canImport(SwiftUI)").L($"{a} extension SwiftUI.Color {{").L("  init(asset: ColorAsset) {").L("    let bundle = BundleToken.bundle")
             .L("    self.init(asset.name, bundle: bundle)").L("  }").L("}").L("#endif").L();
        if (kinds.Contains("data"))
            c.L($"{a} struct DataAsset {{").L($"  {a} fileprivate(set) var name: String").L().L("  #if os(iOS) || os(tvOS) || os(macOS)")
             .L($"  {a} var data: NSDataAsset {{").L("    guard let data = NSDataAsset(asset: self) else {").L("      fatalError(\"Unable to load data asset named \\(name).\")")
             .L("    }").L("    return data").L("  }").L("  #endif").L("}").L()
             .L("#if os(iOS) || os(tvOS) || os(macOS)").L($"{a} extension NSDataAsset {{").L("  convenience init?(asset: DataAsset) {").L("    let bundle = BundleToken.bundle")
             .L("    #if os(iOS) || os(tvOS)").L("    self.init(name: asset.name, bundle: bundle)").L("    #elseif os(macOS)").L("    self.init(name: NSDataAsset.Name(asset.name), bundle: bundle)")
             .L("    #endif").L("  }").L("}").L("#endif").L();
        if (kinds.Contains("image") || kinds.Contains("symbol"))
            c.L($"{a} struct ImageAsset {{").L($"  {a} fileprivate(set) var name: String").L().L("  #if os(macOS)").L($"  {a} typealias Image = NSImage")
             .L("  #elseif os(iOS) || os(tvOS) || os(watchOS)").L($"  {a} typealias Image = UIImage").L("  #endif").L()
             .L($"  {a} var image: Image {{").L("    let bundle = BundleToken.bundle").L("    #if os(iOS) || os(tvOS)").L("    let image = Image(named: name, in: bundle, compatibleWith: nil)")
             .L("    #elseif os(macOS)").L("    let name = NSImage.Name(self.name)").L("    let image = (bundle == .main) ? NSImage(named: name) : bundle.image(forResource: name)")
             .L("    #elseif os(watchOS)").L("    let image = Image(named: name)").L("    #endif").L("    guard let result = image else {")
             .L("      fatalError(\"Unable to load image asset named \\(name).\")").L("    }").L("    return result").L("  }").L()
             .L("  #if canImport(SwiftUI)").L($"  {a} var swiftUIImage: SwiftUI.Image {{").L("    SwiftUI.Image(asset: self)").L("  }").L("  #endif").L("}").L()
             .L("#if canImport(SwiftUI)").L($"{a} extension SwiftUI.Image {{").L("  init(asset: ImageAsset) {").L("    let bundle = BundleToken.bundle")
             .L("    self.init(asset.name, bundle: bundle)").L("  }").L("}").L("#endif").L();
        c.L("// swiftlint:disable convenience_type").L("private final class BundleToken {").L("  static let bundle: Bundle = {").L("    #if SWIFT_PACKAGE")
         .L("    return Bundle.module").L("    #else").L("    return Bundle(for: BundleToken.self)").L("    #endif").L("  }()").L("}").L("// swiftlint:enable convenience_type");
    }

    static void CatalogLevel(Ctx c, string dir, string ns, string indent, HashSet<string> kinds)
    {
        var a = c.Access;
        foreach (var sub in Directory.EnumerateDirectories(dir).Order(StringComparer.Ordinal))
        {
            var name = Path.GetFileNameWithoutExtension(sub);
            var full = ns.Length == 0 ? name : ns + "/" + name;
            switch (Path.GetExtension(sub).ToLowerInvariant())
            {
                case ".imageset":
                    kinds.Add("image");
                    c.L($"{indent}{a} static let {LowerCamel(name)} = ImageAsset(name: \"{Esc(full)}\")");
                    break;
                case ".symbolset":
                    kinds.Add("symbol");
                    c.L($"{indent}{a} static let {LowerCamel(name)} = ImageAsset(name: \"{Esc(full)}\")");
                    break;
                case ".colorset":
                    kinds.Add("color");
                    c.L($"{indent}{a} static let {LowerCamel(name)} = ColorAsset(name: \"{Esc(full)}\")");
                    break;
                case ".dataset":
                    kinds.Add("data");
                    c.L($"{indent}{a} static let {LowerCamel(name)} = DataAsset(name: \"{Esc(full)}\")");
                    break;
                case "":
                {
                    var contents = Path.Combine(sub, "Contents.json");
                    var provides = File.Exists(contents) && Regex.IsMatch(File.ReadAllText(contents), "\"provides-namespace\"\\s*:\\s*true");
                    c.L($"{indent}{a} enum {UpperCamel(name)} {{");
                    CatalogLevel(c, sub, provides ? full : ns, indent + "  ", kinds);
                    c.L($"{indent}}}");
                    break;
                }
            }
        }
    }

    // ---- fonts ----

    static void Fonts(Ctx c)
    {
        var a = c.Access;
        var families = new SortedDictionary<string, SortedDictionary<string, (string PostScript, string Path)>>(StringComparer.Ordinal);
        foreach (var f in Expand(c.Job, ".ttf", ".otf", ".ttc"))
        {
            if (!File.Exists(f) || FontNames(f) is not { } n) continue;
            if (!families.TryGetValue(n.Family, out var styles)) families[n.Family] = styles = new(StringComparer.Ordinal);
            var root = c.Job.Inputs.FirstOrDefault(i => Directory.Exists(i) && Path.GetFullPath(f).StartsWith(Path.GetFullPath(i), StringComparison.OrdinalIgnoreCase));
            styles[n.Style] = (n.PostScript, root != null ? Path.GetRelativePath(root, f).Replace('\\', '/') : Path.GetFileName(f));
        }
        var enumName = c.P("enumName", "FontFamily");
        c.L("#if os(macOS)").L("  import AppKit.NSFont").L("#elseif os(iOS) || os(tvOS) || os(watchOS)").L("  import UIKit.UIFont").L("#endif")
         .L("#if canImport(SwiftUI)").L("  import SwiftUI").L("#endif").L().L("// swiftlint:disable superfluous_disable_command implicit_return").L()
         .L("// MARK: - Fonts").L().L("// swiftlint:disable identifier_name line_length type_body_length").L($"{a} enum {enumName} {{");
        foreach (var (family, styles) in families)
        {
            c.L($"  {a} enum {UpperCamel(family)} {{");
            foreach (var (style, (ps, path)) in styles)
                c.L($"    {a} static let {LowerCamel(style)} = FontConvertible(name: \"{Esc(ps)}\", family: \"{Esc(family)}\", path: \"{Esc(path)}\")");
            c.L($"    {a} static let all: [FontConvertible] = [{string.Join(", ", styles.Keys.Select(LowerCamel))}]").L("  }");
        }
        c.L($"  {a} static let allCustomFonts: [FontConvertible] = [{string.Join(", ", families.Keys.Select(f => UpperCamel(f) + ".all"))}].flatMap {{ $0 }}")
         .L($"  {a} static func registerAllCustomFonts() {{").L("    allCustomFonts.forEach { $0.register() }").L("  }").L("}")
         .L("// swiftlint:enable identifier_name line_length type_body_length").L().L("// MARK: - Implementation Details").L()
         .L($"{a} struct FontConvertible {{").L($"  {a} let name: String").L($"  {a} let family: String").L($"  {a} let path: String").L()
         .L("  #if os(macOS)").L($"  {a} typealias Font = NSFont").L("  #elseif os(iOS) || os(tvOS) || os(watchOS)").L($"  {a} typealias Font = UIFont").L("  #endif").L()
         .L($"  {a} func font(size: CGFloat) -> Font {{").L("    guard let font = Font(font: self, size: size) else {")
         .L("      fatalError(\"Unable to initialize font '\\(name)' (\\(family))\")").L("    }").L("    return font").L("  }").L()
         .L("  #if canImport(SwiftUI)").L($"  {a} func swiftUIFont(size: CGFloat) -> SwiftUI.Font {{").L("    return SwiftUI.Font.custom(self, size: size)").L("  }").L("  #endif").L()
         .L($"  {a} func register() {{").L("    // swiftlint:disable:next conditional_returns_on_newline").L("    guard let url = url else { return }")
         .L("    CTFontManagerRegisterFontsForURL(url as CFURL, .process, nil)").L("  }").L()
         .L("  fileprivate var url: URL? {").L("    return BundleToken.bundle.url(forResource: path, withExtension: nil)").L("  }").L("}").L()
         .L($"{a} extension FontConvertible.Font {{").L("  convenience init?(font: FontConvertible, size: CGFloat) {")
         .L("    #if os(iOS) || os(tvOS) || os(watchOS)").L("    if !UIFont.fontNames(forFamilyName: font.family).contains(font.name) {").L("      font.register()").L("    }")
         .L("    #elseif os(macOS)").L("    if let url = font.url, CTFontManagerGetScopeForURL(url as CFURL) == .none {").L("      font.register()").L("    }").L("    #endif").L()
         .L("    self.init(name: font.name, size: size)").L("  }").L("}").L()
         .L("#if canImport(SwiftUI)").L($"{a} extension SwiftUI.Font {{").L("  static func custom(_ font: FontConvertible, size: CGFloat) -> SwiftUI.Font {")
         .L("    return SwiftUI.Font.custom(font.name, size: size)").L("  }").L("}").L("#endif").L()
         .L("// swiftlint:disable convenience_type").L("private final class BundleToken {").L("  static let bundle: Bundle = {").L("    #if SWIFT_PACKAGE")
         .L("    return Bundle.module").L("    #else").L("    return Bundle(for: BundleToken.self)").L("    #endif").L("  }()").L("}").L("// swiftlint:enable convenience_type");
    }

    // Family (name ID 1, or 16), style (2, or 17) and PostScript name (6) from a font's name table.
    static (string Family, string Style, string PostScript)? FontNames(string path)
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
                var names = new Dictionary<int, string>();
                for (var n = 0; n < count; n++)
                {
                    var r = off + 6 + n * 12;
                    var platform = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(r));
                    var lang = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(r + 4));
                    var id = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(r + 6));
                    var len = BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(r + 8));
                    var start = strings + BinaryPrimitives.ReadUInt16BigEndian(d.AsSpan(r + 10));
                    if (start + len > d.Length) continue;
                    if (platform == 3 && lang == 0x409) names[id] = Encoding.BigEndianUnicode.GetString(d, start, len);
                    else if (platform == 1 && lang == 0) names.TryAdd(id, Encoding.Latin1.GetString(d, start, len));
                }
                var family = names.GetValueOrDefault(16) ?? names.GetValueOrDefault(1);
                var style = names.GetValueOrDefault(17) ?? names.GetValueOrDefault(2) ?? "Regular";
                var ps = names.GetValueOrDefault(6);
                return family == null || ps == null ? null : (family, style, ps);
            }
        }
        catch (ArgumentOutOfRangeException) { }
        return null;
    }

    // ---- files ----

    static void FilesGen(Ctx c)
    {
        var a = c.Access;
        var enumName = c.P("enumName", "Files");
        c.L("import Foundation").L().L("// swiftlint:disable superfluous_disable_command file_length line_length implicit_return").L().L("// MARK: - Files").L()
         .L("// swiftlint:disable explicit_type_interface identifier_name").L("// swiftlint:disable nesting type_body_length type_name").L($"{a} enum {enumName} {{");
        foreach (var input in c.Job.Inputs)
        {
            var root = Directory.Exists(input) ? input : Path.GetDirectoryName(input) ?? ".";
            var files = (Directory.Exists(input) ? Directory.EnumerateFiles(input, "*", SearchOption.AllDirectories) : [input])
                .Where(f => !Path.GetFileName(f).StartsWith('.'))
                .Where(f => c.Job.Filter == null || Regex.IsMatch(f.Replace('\\', '/'), c.Job.Filter)).Order(StringComparer.Ordinal).ToList();
            if (c.Job.Template == "flat-swift5")
                foreach (var f in files) FileLine(c, f, root, "  ");
            else
            {
                c.L($"  {a} enum {UpperCamel(Path.GetFileName(Path.GetFullPath(root).TrimEnd('\\', '/')))} {{");
                FileTree(c, files, root, root, "    ");
                c.L("  }");
            }
        }
        c.L("}").L("// swiftlint:enable explicit_type_interface identifier_name").L("// swiftlint:enable nesting type_body_length type_name").L()
         .L("// MARK: - Implementation Details").L().L($"{a} struct File {{").L($"  {a} let name: String").L($"  {a} let ext: String?")
         .L($"  {a} let relativePath: String").L($"  {a} let mimeType: String").L().L($"  {a} var url: URL {{").L("    return url(locale: nil)").L("  }").L()
         .L($"  {a} func url(locale: Locale?) -> URL {{").L("    let bundle = BundleToken.bundle").L("    let url = bundle.url(").L("      forResource: name,")
         .L("      withExtension: ext,").L("      subdirectory: relativePath,").L("      localization: locale?.identifier").L("    )")
         .L("    guard let result = url else {").L("      let file = name + (ext.flatMap { \".\\($0)\" } ?? \"\")").L("      fatalError(\"Could not locate file named \\(file)\")").L("    }")
         .L("    return result").L("  }").L().L($"  {a} var path: String {{").L("    return path(locale: nil)").L("  }").L()
         .L($"  {a} func path(locale: Locale?) -> String {{").L("    return url(locale: locale).path").L("  }").L("}").L()
         .L("// swiftlint:disable convenience_type explicit_type_interface").L("private final class BundleToken {").L("  static let bundle: Bundle = {")
         .L("    #if SWIFT_PACKAGE").L("    return Bundle.module").L("    #else").L("    return Bundle(for: BundleToken.self)").L("    #endif").L("  }()").L("}")
         .L("// swiftlint:enable convenience_type explicit_type_interface");
    }

    static void FileTree(Ctx c, List<string> files, string dir, string root, string indent)
    {
        foreach (var f in files.Where(f => Path.GetDirectoryName(f) == dir.TrimEnd('\\', '/') || Path.GetDirectoryName(f) == dir)) FileLine(c, f, root, indent);
        foreach (var sub in files.Select(f => Path.GetDirectoryName(f)!).Where(d => d.Length > dir.TrimEnd('\\', '/').Length && d.StartsWith(dir.TrimEnd('\\', '/')))
                     .Select(d => Path.Combine(dir.TrimEnd('\\', '/'), Path.GetRelativePath(dir, d).Split(Path.DirectorySeparatorChar)[0])).Distinct().Order(StringComparer.Ordinal))
        {
            c.L($"{indent}{c.Access} enum {UpperCamel(Path.GetFileName(sub))} {{");
            FileTree(c, files.Where(f => f.StartsWith(sub + Path.DirectorySeparatorChar) || f.StartsWith(sub + "/")).ToList(), sub, root, indent + "  ");
            c.L($"{indent}}}");
        }
    }

    static void FileLine(Ctx c, string f, string root, string indent)
    {
        var name = Path.GetFileNameWithoutExtension(f);
        var ext = Path.GetExtension(f).TrimStart('.');
        var rel = Path.GetRelativePath(Path.GetDirectoryName(Path.GetFullPath(root).TrimEnd('\\', '/'))!, Path.GetDirectoryName(Path.GetFullPath(f))!).Replace('\\', '/');
        var mime = ext.ToLowerInvariant() switch
        {
            "json" => "application/json", "txt" => "text/plain", "html" or "htm" => "text/html", "css" => "text/css", "js" => "text/javascript",
            "png" => "image/png", "jpg" or "jpeg" => "image/jpeg", "gif" => "image/gif", "svg" => "image/svg+xml", "pdf" => "application/pdf",
            "mp3" => "audio/mpeg", "mp4" => "video/mp4", "mov" => "video/quicktime", "xml" => "application/xml", "plist" => "application/x-plist",
            "csv" => "text/csv", "md" => "text/markdown", "wav" => "audio/wav", "zip" => "application/zip",
            _ => "application/octet-stream",
        };
        c.L($"{indent}/// {Path.GetRelativePath(root, f).Replace('\\', '/')}")
         .L($"{indent}{c.Access} static let {LowerCamel(Path.GetFileName(f))} = File(name: \"{Esc(name)}\", ext: {(ext.Length == 0 ? "nil" : $"\"{Esc(ext)}\"")}, relativePath: \"{(rel == "." ? "" : Esc(rel))}\", mimeType: \"{mime}\")");
    }

    // ---- ib (storyboards) ----

    static void Ib(Ctx c)
    {
        var a = c.Access;
        var boards = Expand(c.Job, ".storyboard").Where(File.Exists).Select(f => (Name: Path.GetFileNameWithoutExtension(f), Doc: XDocument.Load(f))).ToList();
        var platform = boards.Select(b => b.Doc.Root?.Attribute("targetRuntime")?.Value ?? "").FirstOrDefault() ?? "";
        var mac = platform.Contains("MacOSX");
        var module = mac ? "AppKit" : "UIKit";
        if (c.Job.Template == "segues-swift5")
        {
            c.L("// swiftlint:disable sorted_imports").L("import Foundation").L($"import {module}").L().L("// swiftlint:disable superfluous_disable_command").L("// swiftlint:disable file_length implicit_return").L()
             .L("// MARK: - Storyboard Segues").L().L("// swiftlint:disable explicit_type_interface identifier_name line_length type_body_length type_name")
             .L($"{a} enum {c.P("enumName", "StoryboardSegue")} {{");
            foreach (var (name, doc) in boards)
            {
                var segues = doc.Descendants("segue").Select(s => (string)s.Attribute("identifier")).Where(s => !string.IsNullOrEmpty(s)).Distinct().Order(StringComparer.Ordinal).ToList();
                if (segues.Count == 0) continue;
                c.L($"  {a} enum {UpperCamel(name)}: String, SegueType {{");
                foreach (var s in segues) c.L($"    case {LowerCamel(s)}{(LowerCamel(s) == s ? "" : $" = \"{Esc(s)}\"")}");
                c.L("  }");
            }
            c.L("}").L("// swiftlint:enable explicit_type_interface identifier_name line_length type_body_length type_name").L().L("// MARK: - Implementation Details").L()
             .L($"{a} protocol SegueType: RawRepresentable {{}}").L().L($"{a} extension {(mac ? "NSSeguePerforming" : "UIViewController")} {{")
             .L($"  func perform<S: SegueType>(segue: S, sender: Any? = nil) where S.RawValue == String {{").L("    let identifier = segue.rawValue")
             .L($"    performSegue{(mac ? "?" : "")}(withIdentifier: identifier, sender: sender)").L("  }").L("}");
            return;
        }
        var controllerBase = mac ? "NSViewController" : "UIViewController";
        c.L("// swiftlint:disable sorted_imports").L("import Foundation").L($"import {module}").L().L("// swiftlint:disable superfluous_disable_command").L("// swiftlint:disable file_length implicit_return").L()
         .L("// MARK: - Storyboard Scenes").L().L("// swiftlint:disable explicit_type_interface identifier_name line_length prefer_self_in_static_references")
         .L("// swiftlint:disable type_body_length type_name").L($"{a} enum {c.P("enumName", "StoryboardScene")} {{");
        foreach (var (name, doc) in boards)
        {
            c.L($"  {a} enum {UpperCamel(name)}: StoryboardType {{").L($"    {a} static let storyboardName = \"{Esc(name)}\"");
            var initial = (string)doc.Root?.Attribute("initialViewController");
            var scenes = doc.Descendants("scene").SelectMany(s => s.Element("objects")?.Elements() ?? []).Where(e => e.Attribute("id") != null && e.Name.LocalName.EndsWith("Controller")).ToList();
            string TypeOf(XElement e) => (string)e.Attribute("customClass") is { } cls ? ((string)e.Attribute("customModule") is { } m ? $"{m}.{cls}" : cls)
                : $"{module}.{(mac ? "NS" : "UI")}{char.ToUpperInvariant(e.Name.LocalName[0])}{e.Name.LocalName[1..]}";
            if (initial != null && scenes.FirstOrDefault(s => (string)s.Attribute("id") == initial) is { } init)
                c.L().L($"    {a} static let initialScene = InitialSceneType<{TypeOf(init)}>(storyboard: {UpperCamel(name)}.self)");
            foreach (var s in scenes.Where(s => s.Attribute("storyboardIdentifier") != null).OrderBy(s => (string)s.Attribute("storyboardIdentifier"), StringComparer.Ordinal))
            {
                var id = (string)s.Attribute("storyboardIdentifier");
                c.L().L($"    {a} static let {LowerCamel(id)} = SceneType<{TypeOf(s)}>(storyboard: {UpperCamel(name)}.self, identifier: \"{Esc(id)}\")");
            }
            c.L("  }");
        }
        var sb = mac ? "NSStoryboard" : "UIStoryboard";
        c.L("}").L("// swiftlint:enable explicit_type_interface identifier_name line_length prefer_self_in_static_references").L("// swiftlint:enable type_body_length type_name").L()
         .L("// MARK: - Implementation Details").L().L($"{a} protocol StoryboardType {{").L("  static var storyboardName: String { get }").L("}").L()
         .L($"{a} extension StoryboardType {{").L($"  static var storyboard: {sb} {{").L("    let name = self.storyboardName")
         .L($"    return {sb}(name: name, bundle: BundleToken.bundle)").L("  }").L("}").L()
         .L($"{a} struct SceneType<T: {controllerBase}> {{").L($"  {a} let storyboard: StoryboardType.Type").L($"  {a} let identifier: String").L()
         .L($"  {a} func instantiate() -> T {{").L("    let identifier = self.identifier")
         .L($"    guard let controller = storyboard.storyboard.instantiate{(mac ? "Controller" : "ViewController")}(withIdentifier: identifier) as? T else {{")
         .L("      fatalError(\"ViewController '\\(identifier)' is not of the expected class \\(T.self).\")").L("    }").L("    return controller").L("  }").L("}").L()
         .L($"{a} struct InitialSceneType<T: {controllerBase}> {{").L($"  {a} let storyboard: StoryboardType.Type").L()
         .L($"  {a} func instantiate() -> T {{").L($"    guard let controller = storyboard.storyboard.instantiateInitial{(mac ? "Controller" : "ViewController")}() as? T else {{")
         .L("      fatalError(\"ViewController is not of the expected class \\(T.self).\")").L("    }").L("    return controller").L("  }").L("}").L()
         .L("// swiftlint:disable convenience_type").L("private final class BundleToken {").L("  static let bundle: Bundle = {").L("    #if SWIFT_PACKAGE")
         .L("    return Bundle.module").L("    #else").L("    return Bundle(for: BundleToken.self)").L("    #endif").L("  }()").L("}").L("// swiftlint:enable convenience_type");
    }

    // ---- coredata ----

    static void CoreData(Ctx c)
    {
        var a = c.Access;
        c.L("// swiftlint:disable superfluous_disable_command implicit_return").L("// swiftlint:disable sorted_imports").L("import CoreData").L("import Foundation").L()
         .L("// swiftlint:disable attributes file_length vertical_whitespace_closing_braces").L("// swiftlint:disable identifier_name line_length type_body_length").L();
        foreach (var model in Expand(c.Job, ".xcdatamodeld", ".xcdatamodel"))
        {
            // A .xcdatamodeld holds versions; .xccurrentversion names the current one.
            var dir = model;
            if (model.EndsWith(".xcdatamodeld", StringComparison.OrdinalIgnoreCase))
            {
                var current = Path.Combine(model, ".xccurrentversion");
                var name = File.Exists(current) && XcPlist.Load(current) is OrderedDictionary<string, object> d ? XcPlist.Str(d, "_XCCurrentVersionName") : null;
                dir = name != null ? Path.Combine(model, name) : Directory.EnumerateDirectories(model, "*.xcdatamodel").Order().LastOrDefault() ?? model;
            }
            var contents = Path.Combine(dir, "contents");
            if (!File.Exists(contents)) throw new SwiftgenError($"{model}: no model contents found");
            foreach (var e in XDocument.Load(contents).Root!.Elements("entity").OrderBy(e => (string)e.Attribute("name"), StringComparer.Ordinal))
            {
                var name = (string)e.Attribute("name");
                var cls = (string)e.Attribute("representedClassName") ?? name;
                var parent = (string)e.Attribute("parentEntity");
                c.L($"// MARK: - {name}").L().L($"@objc({cls})").L($"{a} class {cls}: {(parent ?? "NSManagedObject")} {{")
                 .L($"  {(parent != null ? "override " : "")}{a} class var entityName: String {{").L($"    return \"{Esc(name)}\"").L("  }").L()
                 .L($"  {(parent != null ? "override " : "")}{a} class func entity(in managedObjectContext: NSManagedObjectContext) -> NSEntityDescription? {{")
                 .L("    return NSEntityDescription.entity(forEntityName: entityName, in: managedObjectContext)").L("  }").L()
                 .L("  @available(*, deprecated, renamed: \"makeFetchRequest\", message: \"To avoid collisions with the less concrete method in `NSManagedObject`, please use `makeFetchRequest()` instead.\")")
                 .L("  @nonobjc").L($"  {a} class func fetchRequest() -> NSFetchRequest<{cls}> {{").L("    return NSFetchRequest<" + cls + ">(entityName: entityName)").L("  }").L()
                 .L("  @nonobjc").L($"  {a} class func makeFetchRequest() -> NSFetchRequest<{cls}> {{").L("    return NSFetchRequest<" + cls + ">(entityName: entityName)").L("  }").L();
                foreach (var attr in e.Elements("attribute"))
                {
                    var optional = (string)attr.Attribute("optional") == "YES";
                    var scalar = (string)attr.Attribute("usesScalarValueType") == "YES";
                    var type = ((string)attr.Attribute("attributeType"), scalar) switch
                    {
                        ("String", _) => "String", ("Boolean", true) => "Bool", ("Boolean", false) => "NSNumber",
                        ("Integer 16", true) => "Int16", ("Integer 32", true) => "Int32", ("Integer 64", true) => "Int64",
                        ("Integer 16" or "Integer 32" or "Integer 64", false) => "NSNumber",
                        ("Double", true) => "Double", ("Float", true) => "Float", ("Double" or "Float", false) => "NSNumber",
                        ("Decimal", _) => "NSDecimalNumber", ("Date", _) => "Date", ("Binary", _) => "Data", ("UUID", _) => "UUID", ("URI", _) => "URL",
                        ("Transformable", _) => (string)attr.Attribute("customClassName") ?? "NSObject",
                        _ => "Any",
                    };
                    var opt = optional && !(scalar && type is "Bool" or "Int16" or "Int32" or "Int64" or "Double" or "Float") ? "?" : "";
                    c.L($"  @NSManaged {a} var {SwiftIdent((string)attr.Attribute("name"))}: {type}{opt}");
                }
                foreach (var rel in e.Elements("relationship"))
                {
                    var toMany = (string)rel.Attribute("toMany") == "YES";
                    var dest = (string)rel.Attribute("destinationEntity");
                    var optional = (string)rel.Attribute("optional") == "YES";
                    var ordered = (string)rel.Attribute("ordered") == "YES";
                    var type = toMany ? (ordered ? "NSOrderedSet" : $"Set<{dest}>") : dest;
                    c.L($"  @NSManaged {a} var {SwiftIdent((string)rel.Attribute("name"))}: {type}{(optional ? "?" : "")}");
                }
                c.L("}").L();
            }
        }
        c.L("// swiftlint:enable identifier_name line_length type_body_length");
    }

    // ---- json, yaml, plist ----

    static void Data(Ctx c)
    {
        var a = c.Access;
        var (exts, defaultEnum) = c.Job.Parser switch
        {
            "json" => (new[] { ".json" }, "JSONFiles"),
            "yaml" => (new[] { ".yaml", ".yml" }, "YAMLFiles"),
            _ => (new[] { ".plist" }, "PlistFiles"),
        };
        c.L("import Foundation").L().L("// swiftlint:disable superfluous_disable_command").L("// swiftlint:disable file_length").L()
         .L("// MARK: - " + c.Job.Parser.ToUpperInvariant() + " Files").L().L("// swiftlint:disable identifier_name line_length number_separator type_body_length")
         .L($"{a} enum {c.P("enumName", defaultEnum)} {{");
        foreach (var f in Expand(c.Job, exts).Where(File.Exists))
        {
            var docs = c.Job.Parser switch
            {
                "json" => [FromJson(JsonDocument.Parse(File.ReadAllText(f)).RootElement)],
                "yaml" => XcYaml.ParseAll(File.ReadAllText(f)),
                _ => [XcPlist.Load(f)],
            };
            c.L($"  {a} enum {UpperCamel(Path.GetFileNameWithoutExtension(f))} {{");
            if (docs.Count == 1 && docs[0] is OrderedDictionary<string, object> map)
                foreach (var (k, v) in map) c.L($"    {a} static let {LowerCamel(k)}: {SwiftType(v)} = {SwiftLiteral(v)}");
            else if (docs.Count == 1)
                c.L($"    {a} static let value: {SwiftType(docs[0])} = {SwiftLiteral(docs[0])}");
            else
                c.L($"    {a} static let documents: [Any] = [{string.Join(", ", docs.Select(SwiftLiteral))}]");
            c.L("  }");
        }
        c.L("}").L("// swiftlint:enable identifier_name line_length number_separator type_body_length");
    }

    static object FromJson(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.Object => e.EnumerateObject().Aggregate(new OrderedDictionary<string, object>(), (d, p) => { d[p.Name] = FromJson(p.Value); return d; }),
        JsonValueKind.Array => e.EnumerateArray().Select(FromJson).ToList(),
        JsonValueKind.String => e.GetString(),
        JsonValueKind.Number => e.TryGetInt64(out var l) ? (object)l : e.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null,
    };

    static string SwiftType(object v) => v switch
    {
        string => "String",
        bool => "Bool",
        long or int => "Int",
        double => "Double",
        DateTime => "Date",
        byte[] => "Data",
        List<object> l when l.Count > 0 && l.All(x => x is string) => "[String]",
        List<object> l when l.Count > 0 && l.All(x => x is long) => "[Int]",
        List<object> l when l.Count > 0 && l.All(x => x is double or long) => "[Double]",
        List<object> l when l.Count > 0 && l.All(x => x is bool) => "[Bool]",
        List<object> => "[Any]",
        OrderedDictionary<string, object> => "[String: Any]",
        _ => "Any?",
    };

    static string SwiftLiteral(object v) => v switch
    {
        null => "nil",
        string s => SwiftString(s),
        bool b => b ? "true" : "false",
        long l => l.ToString(CultureInfo.InvariantCulture),
        int i => i.ToString(CultureInfo.InvariantCulture),
        double d => d.ToString("R", CultureInfo.InvariantCulture),
        DateTime t => $"Date(timeIntervalSinceReferenceDate: {(t.ToUniversalTime() - new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds.ToString(CultureInfo.InvariantCulture)})",
        byte[] bytes => $"Data(base64Encoded: \"{Convert.ToBase64String(bytes)}\")!",
        List<object> l => "[" + string.Join(", ", l.Select(SwiftLiteral)) + "]",
        OrderedDictionary<string, object> d => d.Count == 0 ? "[:]" : "[" + string.Join(", ", d.Select(kv => $"{SwiftString(kv.Key)}: {SwiftLiteral(kv.Value)}")) + "]",
        _ => SwiftString(XcPlist.Scalar(v)),
    };

    // ---- names ----

    static readonly HashSet<string> Keywords =
    [
        "associatedtype", "class", "deinit", "enum", "extension", "fileprivate", "func", "import", "init", "inout", "internal", "let", "open",
        "operator", "private", "protocol", "public", "rethrows", "static", "struct", "subscript", "typealias", "var", "break", "case", "continue",
        "default", "defer", "do", "else", "fallthrough", "for", "guard", "if", "in", "repeat", "return", "switch", "where", "while", "as", "Any",
        "catch", "false", "is", "nil", "super", "self", "Self", "throw", "throws", "true", "try", "Type", "Protocol",
    ];

    static List<string> Words(string s)
    {
        var words = new List<string>();
        var current = new StringBuilder();
        foreach (var ch in s)
        {
            if (char.IsLetterOrDigit(ch)) current.Append(ch);
            else if (current.Length > 0) { words.Add(current.ToString()); current.Clear(); }
        }
        if (current.Length > 0) words.Add(current.ToString());
        return words;
    }

    // SwiftGen's swiftIdentifier filter: words joined in UpperCamelCase (only the first letter of
    // each word changes case), a leading digit prefixed with "_", keywords escaped.
    static string UpperCamel(string s)
    {
        var id = string.Concat(Words(s).Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
        return SwiftIdent(id.Length == 0 ? "_" : id);
    }

    // lowerFirstWord: the leading capitals lowercased (except the last before a lowercase letter).
    static string LowerCamel(string s)
    {
        var upper = UpperCamel(s).Trim('`');
        var caps = 0;
        while (caps < upper.Length && char.IsUpper(upper[caps])) caps++;
        var lower = caps > 1 && caps < upper.Length ? caps - 1 : caps;
        return SwiftIdent(upper[..lower].ToLowerInvariant() + upper[lower..]);
    }

    static string SwiftIdent(string id)
    {
        if (id.Length == 0) return "_";
        if (char.IsDigit(id[0])) id = "_" + id;
        return Keywords.Contains(id) ? $"`{id}`" : id;
    }

    static string Esc(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    static string SwiftString(string s) => "\"" + Esc(s ?? "") + "\"";
}
