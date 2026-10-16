using System.Net;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace WinTools;

// license-plist (licenseplist): collects the dependencies of an iOS project - CocoaPods
// acknowledgements, Carthage's Cartfile.resolved, Mint's Mintfile, Swift Package Manager's
// Package.resolved and manual entries from license_plist.yml - downloads each GitHub-hosted
// library's license through the GitHub API and writes the Settings.bundle plists
// (com.mono0926.LicensePlist.plist plus one child pane per library), and optionally Markdown/HTML.
// Everything here is files and HTTPS, so it is a full port.
static partial class LicensePlist
{
    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("license-plist", "Generate a license list of all your dependencies for iOS apps", Run);

    const string Version = "3.28.3";

    const string Usage = """
        Usage: license-plist [options]
          --cartfile-path <path>        Cartfile (Cartfile.resolved is read next to it)   [Cartfile]
          --mintfile-path <path>        Mintfile                                          [Mintfile]
          --pods-path <path>            CocoaPods Pods directory                          [Pods]
          --package-path <path>         Package.swift (Package.resolved is read next to it) [Package.swift]
          --package-paths <paths...>    Several Package.swift files
          --xcodeproj-path <path>       .xcodeproj whose SwiftPM Package.resolved to read [*.xcodeproj]
          --xcworkspace-path <path>     .xcworkspace whose SwiftPM Package.resolved to read [*.xcworkspace]
          --output-path <path>          Output directory                [com.mono0926.LicensePlist.Output]
          --github-token <token>        GitHub token (or LICENSE_PLIST_GITHUB_TOKEN)
          --config-path <path>          Configuration file                                [license_plist.yml]
          --prefix <prefix>             Output file prefix                       [com.mono0926.LicensePlist]
          --html-path <path>            Also write an HTML page
          --markdown-path <path>        Also write a Markdown file
          --force                       Regenerate even if nothing changed
          --add-version-numbers         Add version numbers to the titles
          --add-sources                 Add the source (repository URL) to each license page
          --suppress-opening-directory  Don't open the output directory in Explorer
          --single-page                 Put every license on one page
          --fail-if-missing-license     Exit with an error if a license can't be found
          --silence                     Print errors only
          --version                     Show the version
        """;

    sealed class Library
    {
        public string Source, Owner, Name, Title, Version, Body, Url, LicenseType;
    }

    static bool silence;

    static int Run(string[] args)
    {
        var o = new Dictionary<string, string>(StringComparer.Ordinal);
        var flags = new HashSet<string>(StringComparer.Ordinal);
        var packagePaths = new List<string>();
        silence = false;
        string[] valued = ["cartfile-path", "mintfile-path", "pods-path", "package-path", "xcodeproj-path", "xcworkspace-path",
            "output-path", "github-token", "config-path", "prefix", "html-path", "markdown-path", "license-file-names"];
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg is "-h" or "--help") { Console.WriteLine(Usage); return 0; }
            if (arg == "--version") { Console.WriteLine(Version); return 0; }
            if (!arg.StartsWith("--")) throw new UsageException($"unexpected argument {arg}");
            var name = arg[2..];
            if (name == "package-paths")
            {
                while (i + 1 < args.Length && !args[i + 1].StartsWith("--")) packagePaths.Add(args[++i]);
                continue;
            }
            if (valued.Contains(name))
            {
                o[name] = i + 1 < args.Length ? args[++i] : throw new UsageException($"{arg} needs a value");
                continue;
            }
            if (name is "force" or "add-version-numbers" or "add-sources" or "suppress-opening-directory" or "single-page"
                or "fail-if-missing-license" or "silence" or "sandbox-mode" or "verbose" or "no-color")
            {
                flags.Add(name);
                continue;
            }
            throw new UsageException($"unknown option {arg} (see license-plist --help)");
        }
        silence = flags.Contains("silence");
        string Opt(string k, string d) => o.TryGetValue(k, out var v) ? v : d;
        var prefix = Opt("prefix", "com.mono0926.LicensePlist");
        var output = Opt("output-path", "com.mono0926.LicensePlist.Output");
        var token = Opt("github-token", Environment.GetEnvironmentVariable("LICENSE_PLIST_GITHUB_TOKEN"));
        var config = ReadConfig(Opt("config-path", "license_plist.yml"));

        Log("Start");
        var libraries = new List<Library>();
        void Section(string label, List<Library> found)
        {
            Log($"{label}: {(found.Count == 0 ? "None" : string.Join(", ", found.Select(l => l.Version == null ? l.Name : $"{l.Name} ({l.Version})")))}");
            libraries.AddRange(found);
        }
        Section("CocoaPods", Pods(Opt("pods-path", "Pods")));
        Section("Carthage", Carthage(Opt("cartfile-path", "Cartfile")));
        Section("Mint", Mint(Opt("mintfile-path", "Mintfile")));
        var resolvedFiles = new List<string>();
        foreach (var pkg in packagePaths.Count > 0 ? packagePaths : [Opt("package-path", "Package.swift")])
            resolvedFiles.Add(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(pkg))!, "Package.resolved"));
        foreach (var proj in Glob(Opt("xcodeproj-path", "*.xcodeproj")))
            resolvedFiles.Add(Path.Combine(proj, "project.xcworkspace", "xcshareddata", "swiftpm", "Package.resolved"));
        foreach (var ws in Glob(Opt("xcworkspace-path", "*.xcworkspace")))
            resolvedFiles.Add(Path.Combine(ws, "xcshareddata", "swiftpm", "Package.resolved"));
        Section("Swift Package Manager", resolvedFiles.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).SelectMany(SwiftPm)
            .GroupBy(l => l.Name, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList());
        Section("GitHub (config)", config.GitHub);
        Section("Manual", config.Manual);

        libraries = libraries
            .Where(l => !config.Exclude.Any(x => (x.Name == null || Match(x.Name, l.Name)) && (x.Owner == null || Match(x.Owner, l.Owner ?? ""))))
            .GroupBy(l => l.Name, StringComparer.OrdinalIgnoreCase).Select(g => g.First())
            .OrderBy(l => l.Name, StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var lib in libraries)
            if (config.Rename.TryGetValue(lib.Name, out var renamed)) lib.Title = renamed;

        var missing = new List<string>();
        using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
        {
            http.DefaultRequestHeaders.UserAgent.ParseAdd("license-plist-windows");
            http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            if (!string.IsNullOrEmpty(token)) http.DefaultRequestHeaders.Authorization = new("Bearer", token);
            foreach (var lib in libraries.Where(l => l.Body == null && l.Owner != null))
                FetchLicense(http, lib);
        }
        foreach (var lib in libraries.Where(l => string.IsNullOrEmpty(l.Body)))
        {
            missing.Add(lib.Name);
            Log($"License not found: {lib.Name}", error: true);
        }
        libraries.RemoveAll(l => string.IsNullOrEmpty(l.Body));
        if (flags.Contains("fail-if-missing-license") && missing.Count > 0)
        {
            Console.Error.WriteLine($"license-plist: missing licenses: {string.Join(", ", missing)}");
            return 1;
        }

        var addVersion = flags.Contains("add-version-numbers");
        string Title(Library l) => (l.Title ?? l.Name) + (addVersion && l.Version != null ? $" ({l.Version})" : "");
        var summary = string.Join("\n", libraries.Select(l => $"name: {l.Name}, nameSpecified: {l.Title}, version: {l.Version}\nbody: {l.Body}"));
        var resultFile = Path.Combine(output, prefix + ".latest_result.txt");
        if (!flags.Contains("force") && File.Exists(resultFile) && File.ReadAllText(resultFile) == summary)
        {
            Log("Completed because no diff. You can execute force by `--force` flag.");
            return 0;
        }

        if (Directory.Exists(output)) Directory.Delete(output, recursive: true);
        Directory.CreateDirectory(output);
        var addSources = flags.Contains("add-sources");
        string Text(Library l) => addSources && l.Url != null ? $"{l.Url}\n\n{l.Body}" : l.Body;
        if (flags.Contains("single-page"))
        {
            File.WriteAllText(Path.Combine(output, prefix + ".plist"),
                Plist(libraries.Select(l => Group(Title(l), Text(l)))));
        }
        else
        {
            var root = new List<string>();
            Directory.CreateDirectory(Path.Combine(output, prefix));
            foreach (var l in libraries)
            {
                var file = FileName(l.Name);
                root.Add(Child(Title(l), $"{prefix}/{file}"));
                File.WriteAllText(Path.Combine(output, prefix, file + ".plist"), Plist([Group(null, Text(l))]));
            }
            File.WriteAllText(Path.Combine(output, prefix + ".plist"), Plist(root));
        }
        File.WriteAllText(resultFile, summary);

        if (o.TryGetValue("markdown-path", out var markdown))
        {
            var md = new StringBuilder("# Acknowledgements\nThis application makes use of the following third party libraries:\n");
            foreach (var l in libraries) md.Append($"\n## {Title(l)}\n\n```\n{Text(l).TrimEnd()}\n```\n");
            File.WriteAllText(markdown, md.ToString());
        }
        if (o.TryGetValue("html-path", out var html))
        {
            var h = new StringBuilder("<!DOCTYPE html>\n<html>\n<head>\n<meta charset=\"utf-8\">\n<title>Acknowledgements</title>\n</head>\n<body>\n<h1>Acknowledgements</h1>\n<p>This application makes use of the following third party libraries:</p>\n");
            foreach (var l in libraries)
                h.Append($"<h2>{WebUtility.HtmlEncode(Title(l))}</h2>\n<pre>{WebUtility.HtmlEncode(Text(l).TrimEnd())}</pre>\n");
            h.Append("</body>\n</html>\n");
            File.WriteAllText(html, h.ToString());
        }
        Log($"Generated {libraries.Count} license(s) in {output}");
        if (!flags.Contains("suppress-opening-directory"))
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path.GetFullPath(output)) { UseShellExecute = true });
        Log("End");
        return 0;
    }

    static void Log(string message, bool error = false)
    {
        if (error) Console.Error.WriteLine($"[WARNING] {message}");
        else if (!silence) Console.WriteLine($"[INFO] {message}");
    }

    static bool Match(string pattern, string value) =>
        pattern.StartsWith('/') && pattern.EndsWith('/') && pattern.Length > 1
            ? Regex.IsMatch(value, pattern[1..^1])
            : string.Equals(pattern, value, StringComparison.OrdinalIgnoreCase);

    static IEnumerable<string> Glob(string pattern)
    {
        if (!pattern.Contains('*')) return Directory.Exists(pattern) ? [pattern] : [];
        var dir = Path.GetDirectoryName(pattern);
        dir = string.IsNullOrEmpty(dir) ? "." : dir;
        return Directory.Exists(dir) ? Directory.EnumerateDirectories(dir, Path.GetFileName(pattern)).Order(StringComparer.Ordinal) : [];
    }

    static string FileName(string name) => InvalidFileChars().Replace(name, "_");

    // ---- sources ----

    static List<Library> Pods(string podsPath)
    {
        var list = new List<Library>();
        var support = Path.Combine(podsPath, "Target Support Files");
        if (!Directory.Exists(support)) return list;
        var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var manifest = Path.Combine(podsPath, "Manifest.lock");
        if (File.Exists(manifest))
            foreach (Match m in PodVersion().Matches(File.ReadAllText(manifest)))
                versions.TryAdd(m.Groups[1].Value.Split('/')[0], m.Groups[2].Value);
        foreach (var plist in Directory.EnumerateFiles(support, "*-acknowledgements.plist", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            XDocument doc;
            try
            {
                using var reader = XmlReader.Create(plist, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null });
                doc = XDocument.Load(reader);
            }
            catch (XmlException) { continue; }
            var specs = doc.Descendants("key").FirstOrDefault(k => k.Value == "PreferenceSpecifiers")?.ElementsAfterSelf("array").FirstOrDefault();
            if (specs == null) continue;
            foreach (var dict in specs.Elements("dict"))
            {
                var pairs = new Dictionary<string, string>(StringComparer.Ordinal);
                string key = null;
                foreach (var e in dict.Elements())
                    if (e.Name.LocalName == "key") key = e.Value;
                    else if (key != null) { pairs[key] = e.Value; key = null; }
                // The first and last groups are CocoaPods' own header and footer.
                if (!pairs.TryGetValue("License", out _) || !pairs.TryGetValue("Title", out var title)) continue;
                if (list.Any(l => l.Name == title)) continue;
                list.Add(new Library { Source = "pods", Name = title, Body = pairs.GetValueOrDefault("FooterText"), Version = versions.GetValueOrDefault(title), LicenseType = pairs["License"] });
            }
        }
        return list;
    }

    static List<Library> Carthage(string cartfile)
    {
        var resolved = cartfile.EndsWith(".resolved", StringComparison.Ordinal) ? cartfile : cartfile + ".resolved";
        var list = new List<Library>();
        if (!File.Exists(resolved)) return list;
        foreach (var raw in File.ReadAllLines(resolved))
        {
            var m = CartfileLine().Match(raw.Trim());
            if (!m.Success) continue;
            var (kind, location, version) = (m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value);
            if (kind == "binary") continue;
            var gh = kind == "github" ? location : GitHubPath(location);
            if (gh == null) continue;
            var parts = gh.Split('/');
            if (parts.Length != 2) continue;
            list.Add(new Library { Source = "carthage", Owner = parts[0], Name = parts[1], Version = version, Url = $"https://github.com/{gh}" });
        }
        return list;
    }

    static List<Library> Mint(string mintfile)
    {
        var list = new List<Library>();
        if (!File.Exists(mintfile)) return list;
        foreach (var raw in File.ReadAllLines(mintfile))
        {
            var line = raw.Split('#')[0].Trim();
            if (line.Length == 0) continue;
            var at = line.IndexOf('@');
            var repo = at >= 0 ? line[..at] : line;
            var gh = repo.Contains("://") ? GitHubPath(repo) : repo;
            var parts = gh?.Split('/');
            if (parts is not { Length: 2 }) continue;
            list.Add(new Library { Source = "mint", Owner = parts[0], Name = parts[1], Version = at >= 0 ? line[(at + 1)..] : null, Url = $"https://github.com/{gh}" });
        }
        return list;
    }

    // Package.resolved v1 {"object":{"pins":[{"package","repositoryURL","state"}]}} and v2/v3 {"pins":[{"identity","location","state"}]}.
    static IEnumerable<Library> SwiftPm(string resolved)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(resolved));
        var root = doc.RootElement;
        var pins = root.TryGetProperty("object", out var obj) ? obj.GetProperty("pins") : root.GetProperty("pins");
        var list = new List<Library>();
        foreach (var pin in pins.EnumerateArray())
        {
            var url = pin.TryGetProperty("repositoryURL", out var u) ? u.GetString() : pin.TryGetProperty("location", out var loc) ? loc.GetString() : null;
            var name = pin.TryGetProperty("package", out var p) ? p.GetString() : null;
            string version = null;
            if (pin.TryGetProperty("state", out var state))
                version = state.TryGetProperty("version", out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()
                    : state.TryGetProperty("branch", out var b) && b.ValueKind == JsonValueKind.String ? b.GetString()
                    : state.TryGetProperty("revision", out var r) && r.ValueKind == JsonValueKind.String ? r.GetString()?[..Math.Min(7, r.GetString()!.Length)] : null;
            var gh = url == null ? null : GitHubPath(url);
            var parts = gh?.Split('/');
            name ??= parts?[1] ?? Path.GetFileNameWithoutExtension(url ?? "");
            list.Add(new Library { Source = "swift-package-manager", Owner = parts?[0], Name = parts?[1] ?? name, Title = name, Version = version, Url = url });
        }
        return list;
    }

    static string GitHubPath(string url)
    {
        var m = GitHubUrl().Match(url);
        return m.Success ? $"{m.Groups[1].Value}/{m.Groups[2].Value}" : null;
    }

    static void FetchLicense(HttpClient http, Library lib)
    {
        try
        {
            using var response = http.GetAsync($"https://api.github.com/repos/{lib.Owner}/{lib.Name}/license").GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                Log($"{lib.Owner}/{lib.Name}: GitHub answered {(int)response.StatusCode} {response.ReasonPhrase}", error: true);
                return;
            }
            using var doc = JsonDocument.Parse(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            var content = doc.RootElement.GetProperty("content").GetString() ?? "";
            lib.Body = Encoding.UTF8.GetString(Convert.FromBase64String(content.Replace("\n", "")));
            if (doc.RootElement.TryGetProperty("license", out var license) && license.ValueKind == JsonValueKind.Object &&
                license.TryGetProperty("spdx_id", out var spdx))
                lib.LicenseType = spdx.GetString();
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException or FormatException or KeyNotFoundException)
        {
            Log($"{lib.Owner}/{lib.Name}: could not download the license ({e.Message})", error: true);
        }
    }

    // ---- license_plist.yml ----

    sealed class Config
    {
        public List<Library> GitHub = [], Manual = [];
        public List<(string Name, string Owner)> Exclude = [];
        public Dictionary<string, string> Rename = new(StringComparer.Ordinal);
    }

    // The YAML subset license_plist.yml uses: top-level keys holding lists of flat maps (or
    // "owner/name" strings), with `|` block scalars for manual license bodies.
    static Config ReadConfig(string path)
    {
        var config = new Config();
        if (!File.Exists(path)) return config;
        var lines = File.ReadAllLines(path);
        string section = null;
        Dictionary<string, string> item = null;
        var items = new List<(string Section, Dictionary<string, string> Item)>();
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.TrimStart().StartsWith('#') || line.Trim().Length == 0) continue;
            var indent = line.Length - line.TrimStart().Length;
            var text = line.Trim();
            if (indent == 0)
            {
                section = text.TrimEnd(':');
                item = null;
                continue;
            }
            if (section == "rename" && !text.StartsWith('-'))
            {
                var c = text.IndexOf(':');
                if (c > 0) config.Rename[Unquote(text[..c])] = Unquote(text[(c + 1)..]);
                continue;
            }
            if (text.StartsWith("- "))
            {
                item = new Dictionary<string, string>(StringComparer.Ordinal);
                items.Add((section, item));
                text = text[2..].Trim();
                if (!text.Contains(':')) { item["repo"] = Unquote(text); continue; }
            }
            if (item == null) continue;
            var colon = text.IndexOf(':');
            if (colon < 0) continue;
            var key = text[..colon].Trim();
            var value = text[(colon + 1)..].Trim();
            if (value is "|" or "|-" or ">" or "|+")
            {
                var block = new List<string>();
                var blockIndent = -1;
                while (i + 1 < lines.Length && (lines[i + 1].Trim().Length == 0 || lines[i + 1].Length - lines[i + 1].TrimStart().Length > indent))
                {
                    var l = lines[++i];
                    if (blockIndent < 0 && l.Trim().Length > 0) blockIndent = l.Length - l.TrimStart().Length;
                    block.Add(l.Length >= blockIndent && blockIndent > 0 ? l[blockIndent..] : l.Trim());
                }
                value = string.Join("\n", block).TrimEnd() + "\n";
            }
            else value = Unquote(value);
            item[key] = value;
        }
        foreach (var (sec, it) in items)
        {
            switch (sec)
            {
                case "github":
                    var owner = it.GetValueOrDefault("owner");
                    var name = it.GetValueOrDefault("name");
                    if (it.TryGetValue("repo", out var repo) && repo.Contains('/')) (owner, name) = (repo.Split('/')[0], repo.Split('/')[1]);
                    if (owner != null && name != null)
                        config.GitHub.Add(new Library { Source = "github", Owner = owner, Name = name, Version = it.GetValueOrDefault("version"), Url = $"https://github.com/{owner}/{name}" });
                    break;
                case "manual":
                    var body = it.GetValueOrDefault("body");
                    if (body == null && it.TryGetValue("file", out var file) && File.Exists(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, file)))
                        body = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, file));
                    if (it.TryGetValue("name", out var manualName))
                        config.Manual.Add(new Library { Source = "manual", Name = manualName, Version = it.GetValueOrDefault("version"), Body = body, Url = it.GetValueOrDefault("source") });
                    break;
                case "exclude":
                    config.Exclude.Add((it.GetValueOrDefault("name") ?? it.GetValueOrDefault("repo"), it.GetValueOrDefault("owner")));
                    break;
            }
        }
        return config;
    }

    static string Unquote(string s)
    {
        s = s.Trim();
        return s.Length >= 2 && (s[0] == '"' && s[^1] == '"' || s[0] == '\'' && s[^1] == '\'') ? s[1..^1] : s;
    }

    // ---- Settings.bundle plists ----

    static string Child(string title, string file) =>
        $"\t\t<dict>\n\t\t\t<key>File</key>\n\t\t\t<string>{Esc(file)}</string>\n\t\t\t<key>Title</key>\n\t\t\t<string>{Esc(title)}</string>\n\t\t\t<key>Type</key>\n\t\t\t<string>PSChildPaneSpecifier</string>\n\t\t</dict>\n";

    static string Group(string title, string footer) =>
        "\t\t<dict>\n\t\t\t<key>FooterText</key>\n\t\t\t<string>" + Esc(footer) + "</string>\n" +
        (title == null ? "" : $"\t\t\t<key>Title</key>\n\t\t\t<string>{Esc(title)}</string>\n") +
        "\t\t\t<key>Type</key>\n\t\t\t<string>PSGroupSpecifier</string>\n\t\t</dict>\n";

    static string Plist(IEnumerable<string> specifiers) =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n" +
        "<plist version=\"1.0\">\n<dict>\n\t<key>PreferenceSpecifiers</key>\n\t<array>\n" + string.Concat(specifiers) + "\t</array>\n</dict>\n</plist>\n";

    static string Esc(string s) => SecurityElement.Escape(s ?? "");

    [GeneratedRegex("^(github|git|binary)\\s+\"([^\"]+)\"\\s+\"([^\"]+)\"")]
    private static partial Regex CartfileLine();

    [GeneratedRegex(@"github\.com[:/]+([^/\s]+)/([^/\s]+?)(?:\.git)?/?$")]
    private static partial Regex GitHubUrl();

    [GeneratedRegex(@"^\s+- ""?([^\s""(]+)""? \(([^)]+)\)", RegexOptions.Multiline)]
    private static partial Regex PodVersion();

    [GeneratedRegex(@"[\\/:*?""<>|]")]
    private static partial Regex InvalidFileChars();
}
