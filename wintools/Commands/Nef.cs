using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace WinTools;

// nef: renders Xcode Playgrounds into documentation. `markdown` and `jekyll` are text
// transformations of the pages' Contents.swift (markup comments become prose, code becomes fenced
// Swift, nef:begin:hidden blocks are dropped, nef:begin:header becomes Jekyll front matter), and
// `playground` writes a new playground skeleton; all of that runs here. `compile`, `clean`,
// `carbon` and `playground-book` drive Xcode, the Swift toolchain or WebKit, so they say so.
static partial class Nef
{
    [ModuleInitializer]
    internal static void Register() => Commands.Add("nef", "Render Xcode Playgrounds as Markdown or Jekyll sites", Run);

    const string Usage = """
        Usage: nef <command> [options]

        Commands:
          markdown --project <path> --output <path>
                  Render Markdown files for the given Xcode Playgrounds
          jekyll --project <path> --output <path> [--main-page <README.md>]
                  Render Jekyll pages (with sidebar data) for the given Xcode Playgrounds
          playground --name <name> --output <path> [--platform ios|macos]
                  Create a new Xcode Playground
          compile, clean, carbon, playground-book
                  Need Xcode, the Swift toolchain or WebKit (macOS only)
          version Show nef's version
        """;

    static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help") { Console.WriteLine(Usage); return 0; }
        var command = args[0];
        if (command is "version" or "--version") { Console.WriteLine("0.7.1"); return 0; }
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 1; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg is "-h" or "--help") { Console.WriteLine(Usage); return 0; }
            if (!arg.StartsWith("--")) throw new UsageException($"unexpected argument {arg}");
            var eq = arg.IndexOf('=');
            if (eq > 0) options[arg[2..eq]] = arg[(eq + 1)..];
            else if (i + 1 < args.Length && !args[i + 1].StartsWith("--")) options[arg[2..]] = args[++i];
            else options[arg[2..]] = "true";
        }
        string Need(string key) => options.TryGetValue(key, out var v) ? v : throw new UsageException($"{command}: --{key} is required");
        switch (command)
        {
            case "markdown":
                return Render(Need("project"), Need("output"), jekyll: false, mainPage: null);
            case "jekyll":
                return Render(Need("project"), Need("output"), jekyll: true, mainPage: options.GetValueOrDefault("main-page"));
            case "playground":
                return NewPlayground(Need("name"), options.GetValueOrDefault("output") ?? ".", options.GetValueOrDefault("platform") ?? "ios");
            case "compile" or "clean":
                Console.Error.WriteLine($"nef {command}: compiling playgrounds needs Xcode and its playground frameworks, which exist only on macOS");
                return 1;
            case "carbon":
                Console.Error.WriteLine("nef carbon: rendering Carbon snippets needs macOS's WebKit; use `nef markdown` for text output");
                return 1;
            case "playground-book":
                Console.Error.WriteLine("nef playground-book: building a Swift Playgrounds book needs Xcode's Swift package tooling on macOS");
                return 1;
            default:
                throw new UsageException($"unknown command '{command}' (see nef --help)");
        }
    }

    sealed record Page(string Name, string File);

    static List<string> FindPlaygrounds(string project)
    {
        if (project.TrimEnd('/', '\\').EndsWith(".playground", StringComparison.OrdinalIgnoreCase))
            return Directory.Exists(project) ? [project] : throw new InvalidOperationException($"{project}: no such playground");
        if (!Directory.Exists(project)) throw new InvalidOperationException($"{project}: no such directory");
        var found = Directory.EnumerateDirectories(project, "*.playground", SearchOption.AllDirectories)
            .Where(p => !p.Contains(Path.DirectorySeparatorChar + "Carthage" + Path.DirectorySeparatorChar) && !p.Contains(Path.DirectorySeparatorChar + "Pods" + Path.DirectorySeparatorChar))
            .Order(StringComparer.Ordinal).ToList();
        if (found.Count == 0) throw new InvalidOperationException($"{project}: no Xcode Playgrounds found");
        return found;
    }

    // Page order comes from contents.xcplayground; single-page playgrounds have a root Contents.swift.
    static List<Page> Pages(string playground)
    {
        var pages = new List<Page>();
        var manifest = Path.Combine(playground, "contents.xcplayground");
        if (File.Exists(manifest))
        {
            foreach (var p in XDocument.Load(manifest).Descendants("page"))
            {
                var name = (string)p.Attribute("name");
                var file = Path.Combine(playground, "Pages", name + ".xcplaygroundpage", "Contents.swift");
                if (File.Exists(file)) pages.Add(new Page(name, file));
            }
        }
        if (pages.Count == 0 && Directory.Exists(Path.Combine(playground, "Pages")))
            foreach (var dir in Directory.EnumerateDirectories(Path.Combine(playground, "Pages"), "*.xcplaygroundpage").Order(StringComparer.Ordinal))
                if (File.Exists(Path.Combine(dir, "Contents.swift")))
                    pages.Add(new Page(Path.GetFileNameWithoutExtension(dir), Path.Combine(dir, "Contents.swift")));
        if (pages.Count == 0 && File.Exists(Path.Combine(playground, "Contents.swift")))
            pages.Add(new Page(Path.GetFileNameWithoutExtension(playground.TrimEnd('/', '\\')), Path.Combine(playground, "Contents.swift")));
        return pages;
    }

    static int Render(string project, string output, bool jekyll, string mainPage)
    {
        var sidebar = new StringBuilder("options:\n");
        var count = 0;
        foreach (var playground in FindPlaygrounds(project))
        {
            var pgName = Path.GetFileNameWithoutExtension(playground.TrimEnd('/', '\\'));
            var pages = Pages(playground);
            if (jekyll) sidebar.Append($"\n  - title: {pgName}\n\n    nested_options:\n");
            for (var i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                string Link(int index) =>
                    index < 0 || index >= pages.Count ? "" : jekyll ? $"../{Slug(pages[index].Name)}/" : $"{pages[index].Name.Replace(" ", "%20")}.md";
                var (header, body) = Convert(File.ReadAllText(page.File));
                body = body.Replace("(@next)", $"({Link(i + 1)})").Replace("(@previous)", $"({Link(i - 1)})");
                string path;
                if (jekyll)
                {
                    var permalink = $"/{Slug(pgName)}/{Slug(page.Name)}/";
                    header ??= $"layout: docs\ntitle: {page.Name}\npermalink: {permalink}";
                    body = $"---\n{header}\n---\n\n{body}";
                    path = Path.Combine(output, Slug(pgName), Slug(page.Name), "README.md");
                    sidebar.Append($"\n      - title: {page.Name}\n        url: {permalink}\n");
                }
                else path = Path.Combine(output, pgName, page.Name + ".md");
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, body);
                Console.WriteLine($"rendered {path}");
                count++;
            }
        }
        if (jekyll)
        {
            Directory.CreateDirectory(Path.Combine(output, "_data"));
            File.WriteAllText(Path.Combine(output, "_data", "sidebar.yml"), sidebar.ToString());
            if (mainPage != null)
            {
                var text = File.ReadAllText(mainPage);
                if (!text.StartsWith("---")) text = "---\nlayout: docs\ntitle: Home\npermalink: /docs/\n---\n\n" + text;
                File.WriteAllText(Path.Combine(output, "README.md"), text);
            }
        }
        if (count == 0) { Console.Error.WriteLine("nef: no playground pages to render"); return 1; }
        return 0;
    }

    // Contents.swift -> (Jekyll header or null, Markdown body).
    internal static (string Header, string Body) Convert(string source)
    {
        var lines = source.Replace("\r\n", "\n").Split('\n');
        var md = new StringBuilder();
        var code = new List<string>();
        string header = null;
        void FlushCode()
        {
            while (code.Count > 0 && code[0].Trim().Length == 0) code.RemoveAt(0);
            while (code.Count > 0 && code[^1].Trim().Length == 0) code.RemoveAt(code.Count - 1);
            if (code.Count > 0)
            {
                if (md.Length > 0 && !md.ToString().EndsWith("\n\n")) md.Append('\n');
                md.Append("```swift\n").Append(string.Join('\n', code)).Append("\n```\n\n");
            }
            code.Clear();
        }
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.Trim();
            var begin = NefBegin().Match(trimmed);
            if (begin.Success)
            {
                var block = new List<string>();
                for (i++; i < lines.Length && !NefEnd().IsMatch(lines[i].Trim()); i++) block.Add(lines[i]);
                if (begin.Groups[1].Value == "header")
                {
                    // The header is a /* ... */ comment holding front-matter lines.
                    var text = string.Join('\n', block).Trim();
                    if (text.StartsWith("/*")) text = text[2..];
                    if (text.EndsWith("*/")) text = text[..^2];
                    header = string.Join('\n', text.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
                }
                continue;
            }
            if (trimmed.StartsWith("//:"))
            {
                FlushCode();
                md.Append(StripOne(trimmed[3..])).Append('\n');
                if (i + 1 >= lines.Length || !lines[i + 1].Trim().StartsWith("//:")) md.Append('\n');
                continue;
            }
            if (trimmed.StartsWith("/*:"))
            {
                FlushCode();
                var first = trimmed[3..];
                var block = new List<string>();
                if (first.Contains("*/")) block.Add(first[..first.IndexOf("*/")]);
                else
                {
                    if (first.Trim().Length > 0) block.Add(first);
                    for (i++; i < lines.Length; i++)
                    {
                        var l = lines[i];
                        var end = l.IndexOf("*/");
                        if (end >= 0) { if (l[..end].Trim().Length > 0) block.Add(l[..end]); break; }
                        block.Add(l);
                    }
                }
                md.Append(Dedent(block).Trim('\n')).Append("\n\n");
                continue;
            }
            code.Add(line);
        }
        FlushCode();
        return (header, md.ToString().TrimEnd('\n') + "\n");
    }

    static string StripOne(string s) => s.StartsWith(' ') ? s[1..] : s;

    static string Dedent(List<string> block)
    {
        var indent = block.Where(l => l.Trim().Length > 0).Select(l => l.Length - l.TrimStart().Length).DefaultIfEmpty(0).Min();
        return string.Join('\n', block.Select(l => l.Length >= indent ? l[indent..] : l.TrimStart()));
    }

    static string Slug(string s) => SlugChars().Replace(s.ToLowerInvariant(), "-").Trim('-');

    static int NewPlayground(string name, string output, string platform)
    {
        if (platform is not ("ios" or "macos")) throw new UsageException("--platform must be ios or macos");
        var root = Path.Combine(output, name, name + ".playground");
        if (Directory.Exists(root)) throw new InvalidOperationException($"{root} already exists");
        var page = Path.Combine(root, "Pages", "README.xcplaygroundpage");
        Directory.CreateDirectory(page);
        File.WriteAllText(Path.Combine(root, "contents.xcplayground"),
            $"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<playground version='6.0' target-platform='{platform}' display-mode='rendered' buildActiveScheme='true' executeOnSourceChanges='false' importAppTypes='true'>\n    <pages>\n        <page name='README'/>\n    </pages>\n</playground>\n");
        File.WriteAllText(Path.Combine(page, "Contents.swift"),
            $"// nef:begin:header\n/*\n layout: docs\n title: {name}\n */\n// nef:end\n//: # {name}\n//: Write your documentation here; code below is rendered as Swift.\nimport Foundation\n\nprint(\"Hello, {name}!\")\n");
        Console.WriteLine($"created {root}");
        return 0;
    }

    [GeneratedRegex(@"^//\s*nef:begin:(\w+)")]
    private static partial Regex NefBegin();

    [GeneratedRegex(@"^//\s*nef:end")]
    private static partial Regex NefEnd();

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex SlugChars();
}
