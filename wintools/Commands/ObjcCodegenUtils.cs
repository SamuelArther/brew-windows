using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace WinTools;

// objc-codegenutils: objc-assetgen (asset catalogs -> a class with one +xxxImage method per image
// set), objc-identifierconstants (storyboards -> NSString constants for every storyboard, reuse and
// segue identifier) and objc-colordump (.clr colour lists -> +xxxColor methods). Asset catalogs are
// folders of JSON and storyboards are XML, so those two are full ports. .clr files are AppKit
// NSColorList archives that only AppKit can decode, so objc-colordump explains that and exits 1.
static class ObjcCodegenUtils
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("objc-assetgen", "Generate image accessor classes from Xcode asset catalogs", args => Tool(args, "objc-assetgen", ".xcassets"));
        Commands.Add("objc-identifierconstants", "Generate identifier constants from storyboards", args => Tool(args, "objc-identifierconstants", ".storyboard"));
        Commands.Add("objc-colordump", "Generate color accessor classes from .clr color lists", args => Tool(args, "objc-colordump", ".clr"));
    }

    // All three share CGUCodeGenTool's command line.
    static int Tool(string[] args, string tool, string extension)
    {
        string output = ".", search = null, prefix = "";
        var ios6 = false;
        var inputs = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            string Value() => i + 1 < args.Length ? args[++i] : throw new UsageException($"{args[i]} needs a value");
            switch (args[i])
            {
                case "-h" or "--help":
                    Console.WriteLine($"""
                        Usage: {tool} [-6] [-o <path>] [-f <path>] [-p <prefix>] [<paths>]
                               {tool} -h

                            -o <path>   Output files at <path>
                            -f <path>   Search for *{extension} folders starting from <path>
                            -p <prefix> Use <prefix> as the class prefix in the generated code
                            -6          Use iOS 6 compatible code
                            -h          Print this help and exit
                            <paths>     Input files; this and/or -f are required.
                        """);
                    return 0;
                case "-o": output = Value(); break;
                case "-f": search = Value(); break;
                case "-p": prefix = Value(); break;
                case "-6": ios6 = true; break;
                default:
                    if (args[i].StartsWith('-')) throw new UsageException($"unknown option {args[i]}");
                    inputs.Add(args[i]);
                    break;
            }
        }
        if (search != null)
        {
            if (!Directory.Exists(search)) throw new InvalidOperationException($"{search}: no such directory");
            inputs.AddRange(Directory.EnumerateFileSystemEntries(search, "*" + extension, SearchOption.AllDirectories)
                .Where(p => !p.Contains(extension + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                .Order(StringComparer.Ordinal));
        }
        if (inputs.Count == 0) throw new UsageException($"no input files; pass paths or -f <path> (see {tool} -h)");
        if (tool == "objc-colordump")
        {
            Console.Error.WriteLine("objc-colordump: .clr files are AppKit NSColorList archives, which only macOS's AppKit can read");
            return 1;
        }
        Directory.CreateDirectory(output);
        foreach (var input in inputs)
        {
            if (!File.Exists(input) && !Directory.Exists(input)) throw new InvalidOperationException($"{input}: no such file or directory");
            if (tool == "objc-assetgen") AssetGen(input, output, prefix, ios6);
            else Identifiers(input, output, prefix);
        }
        return 0;
    }

    static void AssetGen(string catalog, string output, string prefix, bool ios6)
    {
        var name = Path.GetFileNameWithoutExtension(catalog.TrimEnd('/', '\\'));
        var className = prefix + Identifier(name, upperFirst: true) + "Catalog";
        var methods = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var set in Directory.EnumerateDirectories(catalog, "*.imageset", SearchOption.AllDirectories))
        {
            var image = Path.GetFileNameWithoutExtension(set);
            var method = Identifier(image, upperFirst: false) + "Image";
            var body = $"[UIImage imageNamed:@\"{image}\"]";
            // iOS 6 has no asset catalog slicing, so cap insets from the catalog are applied in code.
            if (ios6 && SlicingInsets(Path.Combine(set, "Contents.json")) is { } insets)
                body = $"[{body} resizableImageWithCapInsets:UIEdgeInsetsMake({insets})]";
            methods[method] = body;
        }
        var source = Path.GetFileName(catalog.TrimEnd('/', '\\'));
        var h = new StringBuilder(Banner(source, "objc-assetgen")).Append("#import <UIKit/UIKit.h>\n\n\n").Append($"@interface {className} : NSObject\n\n");
        var m = new StringBuilder(Banner(source, "objc-assetgen")).Append($"#import \"{className}.h\"\n\n\n@implementation {className}\n\n");
        foreach (var (method, body) in methods)
        {
            h.Append($"+ (UIImage *){method};\n");
            m.Append($"+ (UIImage *){method};\n{{\n    return {body};\n}}\n\n");
        }
        h.Append("\n@end\n");
        m.Append("@end\n");
        File.WriteAllText(Path.Combine(output, className + ".h"), h.ToString());
        File.WriteAllText(Path.Combine(output, className + ".m"), m.ToString());
    }

    // Contents.json: images[].resizing.capInsets {top, left, bottom, right} when slicing is set.
    static string SlicingInsets(string contents)
    {
        if (!File.Exists(contents)) return null;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(contents));
            if (!doc.RootElement.TryGetProperty("images", out var images) || images.ValueKind != JsonValueKind.Array) return null;
            foreach (var img in images.EnumerateArray())
            {
                if (!img.TryGetProperty("resizing", out var r) || !r.TryGetProperty("capInsets", out var c)) continue;
                double Get(string k) => c.TryGetProperty(k, out var v) && v.TryGetDouble(out var d) ? d : 0;
                return FormattableString.Invariant($"{Get("top")}, {Get("left")}, {Get("bottom")}, {Get("right")}");
            }
        }
        catch (JsonException) { }
        return null;
    }

    static void Identifiers(string storyboard, string output, string prefix)
    {
        var name = Path.GetFileNameWithoutExtension(storyboard);
        var baseName = prefix + Identifier(name, upperFirst: true);
        if (!baseName.EndsWith("Storyboard", StringComparison.Ordinal)) baseName += "Storyboard";
        var className = baseName + "Identifiers";
        XDocument doc;
        try
        {
            using var reader = XmlReader.Create(storyboard, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null });
            doc = XDocument.Load(reader);
        }
        catch (XmlException e) { throw new InvalidOperationException($"{storyboard}: not a storyboard ({e.Message})"); }
        var ids = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var el in doc.Descendants())
        {
            foreach (var attr in new[] { "storyboardIdentifier", "reuseIdentifier" })
                if ((string)el.Attribute(attr) is { Length: > 0 } v) ids.Add(v);
            if (el.Name.LocalName == "segue" && (string)el.Attribute("identifier") is { Length: > 0 } s) ids.Add(s);
        }
        var source = Path.GetFileName(storyboard);
        var h = new StringBuilder(Banner(source, "objc-identifierconstants")).Append("#import <Foundation/Foundation.h>\n\n");
        var m = new StringBuilder(Banner(source, "objc-identifierconstants")).Append($"#import \"{className}.h\"\n\n");
        foreach (var id in ids)
        {
            var constant = baseName + Identifier(id, upperFirst: true) + "Identifier";
            h.Append($"extern NSString *const {constant};\n");
            m.Append($"NSString *const {constant} = @\"{id.Replace("\\", "\\\\").Replace("\"", "\\\"")}\";\n");
        }
        File.WriteAllText(Path.Combine(output, className + ".h"), h.ToString());
        File.WriteAllText(Path.Combine(output, className + ".m"), m.ToString());
    }

    static string Banner(string source, string tool) =>
        $"//\n// This file is generated from {source} by {tool}.\n// Please do not edit.\n//\n\n";

    // "big blue-button" -> bigBlueButton / BigBlueButton; leading digits get an underscore.
    static string Identifier(string text, bool upperFirst)
    {
        var sb = new StringBuilder();
        var upperNext = upperFirst;
        foreach (var ch in text)
        {
            if (!char.IsAsciiLetterOrDigit(ch)) { upperNext = sb.Length > 0 || upperFirst; continue; }
            if (sb.Length == 0 && !upperFirst) sb.Append(char.ToLowerInvariant(ch));
            else sb.Append(upperNext ? char.ToUpperInvariant(ch) : ch);
            upperNext = false;
        }
        if (sb.Length == 0 || char.IsAsciiDigit(sb[0])) sb.Insert(0, '_');
        return sb.ToString();
    }
}
