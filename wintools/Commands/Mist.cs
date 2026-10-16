using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace WinTools;

// mist (mist-cli): lists and downloads macOS installers and firmwares. Installers come from Apple's
// software update catalogs (products whose ExtendedMetaInfo names InstallAssistant packages; name,
// version and build from each product's English .dist), firmwares (Apple silicon .ipsw restore
// images) from the ipsw.me API. Downloads write the .ipsw, or the full-installer InstallAssistant.pkg
// for the "package" output; building an .app, disk image, ISO or bootable installer needs macOS's own
// tools (hdiutil, createinstallmedia), so those outputs explain that and exit 1.
static class Mist
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("mist", "List and download macOS installers and firmwares", Run);
    }

    const string Usage = """
        OVERVIEW: macOS Installer Super Tool.

        Automatically download macOS Firmwares / Installers.

        USAGE: mist <subcommand>

        OPTIONS:
          --version               Show the version.
          -h, --help              Show help information.

        SUBCOMMANDS:
          list                    List all macOS Firmwares / Installers available to download.
          download                Download a macOS Firmware / Installer.
          version                 Display the version of mist.

          See 'mist help <subcommand>' for detailed help.
        """;

    const string ListUsage = """
        OVERVIEW: List all macOS Firmwares / Installers available to download.

        USAGE: mist list <firmware|installer> [<search-string>] [options]

        OPTIONS:
          -b, --include-betas     Include beta macOS Firmwares / Installers in search results.
          -l, --latest            Only list the latest version (first match).
          -e, --export <path>     Export the list to a file (format from --output-type, or the extension).
          -o, --output-type <t>   ascii (default), csv, json, plist or yaml.
          -q, --quiet             Suppress verbose output.
          --catalog-url <url>     Override the software update catalog URL (installers).
          -h, --help              Show help information.
        """;

    const string DownloadUsage = """
        OVERVIEW: Download a macOS Firmware / Installer.

        USAGE: mist download firmware <search-string> [options]
               mist download installer <search-string> <output-type> ... [options]

        Output types (installer): package. (application, image, iso and bootableinstaller
        need macOS's hdiutil/createinstallmedia.)

        OPTIONS:
          -b, --include-betas         Include betas when searching.
          -o, --output-directory <d>  Where to save (default: Downloads\Mist).
          --firmware-name <name>      Default: Install %NAME% %VERSION%-%BUILD%.ipsw
          --package-name <name>       Default: Install %NAME% %VERSION%-%BUILD%.pkg
          -f, --force                 Overwrite existing files.
          -q, --quiet                 Suppress verbose output.
          --catalog-url <url>         Override the software update catalog URL.
          -h, --help                  Show help information.
        """;

    const string CatalogBase = "https://swscan.apple.com/content/catalogs/others/";
    const string CatalogSuffix = "26-15-14-13-12-10.16-10.15-10.14-10.13-10.12-10.11-10.10-10.9-mountainlion-lion-snowleopard-leopard.merged-1.sucatalog";
    static readonly string[] Catalogs = ["index-", "index-26customerseed-", "index-26seed-", "index-26beta-"];

    static readonly HttpClient Http = CreateHttp();

    static HttpClient CreateHttp()
    {
        var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"mist-wintools/{Commands.Version}");
        return client;
    }

    static int Run(string[] args)
    {
        if (args.Length == 0) { Console.WriteLine(Usage); return 0; }
        switch (args[0])
        {
            case "-h" or "--help": Console.WriteLine(Usage); return 0;
            case "--version" or "version": Console.WriteLine(Commands.Version); return 0;
            case "help":
                Console.WriteLine(args.ElementAtOrDefault(1) switch { "list" => ListUsage, "download" => DownloadUsage, _ => Usage });
                return 0;
            case "list": return List(Parse(args[1..], ListUsage));
            case "download": return Download(Parse(args[1..], DownloadUsage));
            default: throw new UsageException($"unknown subcommand '{args[0]}' (see mist --help)");
        }
    }

    sealed class Options
    {
        public string Kind, Search, Export, OutputType, OutputDirectory, CatalogUrl, FirmwareName, PackageName;
        public bool Betas, Latest, Quiet, Force, Help;
        public List<string> Outputs = [];
    }

    static Options Parse(string[] args, string usage)
    {
        var o = new Options();
        var positional = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string value = null;
            if (arg.StartsWith("--", StringComparison.Ordinal) && arg.Contains('='))
            {
                value = arg[(arg.IndexOf('=') + 1)..];
                arg = arg[..arg.IndexOf('=')];
            }
            string Next() => value ?? (i + 1 < args.Length ? args[++i] : throw new UsageException($"{arg} takes a value"));
            switch (arg)
            {
                case "-h" or "--help": Console.WriteLine(usage); o.Help = true; return o;
                case "-b" or "--include-betas": o.Betas = true; break;
                case "-l" or "--latest": o.Latest = true; break;
                case "-q" or "--quiet": o.Quiet = true; break;
                case "-f" or "--force": o.Force = true; break;
                case "-e" or "--export": o.Export = Next(); break;
                case "-o" when usage == ListUsage: o.OutputType = Next(); break;
                case "--output-type": o.OutputType = Next(); break;
                case "-o" or "--output-directory": o.OutputDirectory = Next(); break;
                case "--catalog-url": o.CatalogUrl = Next(); break;
                case "--firmware-name": o.FirmwareName = Next(); break;
                case "--package-name": o.PackageName = Next(); break;
                case "-c" or "--compatible" or "--no-ansi" or "-k" or "--cache-downloads" or "--retries" or "--retry-delay"
                    or "--application-name" or "--image-name" or "--iso-name" or "--bootable-installer-name" or "--package-identifier"
                    or "--temporary-directory" or "-t" or "--keychain" or "--package-identity" or "--image-signing-identity" or "--export-path" or "--volume":
                    if (arg is "--retries" or "--retry-delay" or "--application-name" or "--image-name" or "--iso-name" or "--bootable-installer-name"
                        or "--package-identifier" or "--temporary-directory" or "-t" or "--keychain" or "--package-identity" or "--image-signing-identity"
                        or "--export-path" or "--volume") Next();
                    break;
                default:
                    if (arg.StartsWith('-') && arg.Length > 1) throw new UsageException($"unknown option '{arg}'");
                    positional.Add(arg);
                    break;
            }
        }
        if (positional.Count == 0) throw new UsageException("missing expected argument '<firmware|installer>'");
        o.Kind = positional[0] switch
        {
            "firmware" or "firmwares" => "firmware",
            "installer" or "installers" => "installer",
            _ => throw new UsageException($"unknown platform type '{positional[0]}' (firmware or installer)"),
        };
        o.Search = positional.ElementAtOrDefault(1);
        o.Outputs = positional.Skip(2).ToList();
        o.OutputType = (o.OutputType ?? (o.Export != null ? Path.GetExtension(o.Export).TrimStart('.') : "ascii")).ToLowerInvariant();
        if (o.OutputType is "yml") o.OutputType = "yaml";
        if (o.OutputType is not ("ascii" or "csv" or "json" or "plist" or "yaml"))
            throw new UsageException($"invalid output type '{o.OutputType}' (ascii, csv, json, plist or yaml)");
        return o;
    }

    // ---- model ----

    sealed class Item
    {
        public string Identifier, Name, Version, Build, Date, Url, Sha1, Sha256;
        public long Size;
        public bool Signed, Beta;
        public List<(string Url, long Size)> Packages = [];
        public string PackageUrl => Packages.FirstOrDefault(p => p.Url.EndsWith("/InstallAssistant.pkg", StringComparison.Ordinal)).Url;
    }

    static readonly Dictionary<int, string> Names = new()
    {
        [11] = "macOS Big Sur", [12] = "macOS Monterey", [13] = "macOS Ventura", [14] = "macOS Sonoma", [15] = "macOS Sequoia", [26] = "macOS Tahoe",
    };

    static string NameFor(string version)
    {
        var parts = version.Split('.');
        var major = int.TryParse(parts[0], out var m) ? m : 0;
        if (major == 10 && parts.Length > 1)
            return parts[1] switch
            {
                "15" => "macOS Catalina", "14" => "macOS Mojave", "13" => "macOS High Sierra", "12" => "macOS Sierra",
                "11" => "OS X El Capitan", "10" => "OS X Yosemite", _ => "OS X " + version,
            };
        return Names.TryGetValue(major, out var name) ? name : $"macOS {major}";
    }

    // Apple's beta builds end in a lowercase letter (23A5257q).
    static bool IsBetaBuild(string build) => build.Length > 0 && char.IsAsciiLetterLower(build[^1]);

    static int CompareVersions(string a, string b)
    {
        var x = a.Split('.').Select(p => int.TryParse(p, out var n) ? n : 0).ToArray();
        var y = b.Split('.').Select(p => int.TryParse(p, out var n) ? n : 0).ToArray();
        for (var i = 0; i < Math.Max(x.Length, y.Length); i++)
        {
            var c = x.ElementAtOrDefault(i).CompareTo(y.ElementAtOrDefault(i));
            if (c != 0) return c;
        }
        return 0;
    }

    static List<Item> Sorted(IEnumerable<Item> items) =>
        items.OrderByDescending(i => i.Version, Comparer<string>.Create(CompareVersions))
             .ThenByDescending(i => i.Build, StringComparer.Ordinal).ToList();

    static IEnumerable<Item> Matching(IEnumerable<Item> items, string search)
    {
        if (string.IsNullOrEmpty(search)) return items;
        var s = search.Trim();
        var exact = items.Where(i => i.Build.Equals(s, StringComparison.OrdinalIgnoreCase) || i.Version == s).ToList();
        if (exact.Count > 0) return exact;
        return items.Where(i => i.Build.Equals(s, StringComparison.OrdinalIgnoreCase) || i.Version == s
                                || i.Version.StartsWith(s + ".", StringComparison.Ordinal)
                                || i.Name.Contains(s, StringComparison.OrdinalIgnoreCase)
                                || i.Name.Replace("macOS ", "").Equals(s, StringComparison.OrdinalIgnoreCase));
    }

    // ---- firmwares (ipsw.me) ----

    static List<Item> Firmwares(bool quiet)
    {
        if (!quiet) Console.Error.WriteLine("Retrieving macOS Firmwares from ipsw.me...");
        using var doc = JsonDocument.Parse(GetString("https://api.ipsw.me/v4/device/VirtualMac2,1?type=ipsw"));
        var list = new List<Item>();
        foreach (var f in doc.RootElement.GetProperty("firmwares").EnumerateArray())
        {
            var version = f.GetProperty("version").GetString();
            var build = f.GetProperty("buildid").GetString();
            list.Add(new Item
            {
                Identifier = f.GetProperty("identifier").GetString(),
                Name = NameFor(version),
                Version = version,
                Build = build,
                Url = f.GetProperty("url").GetString(),
                Sha1 = f.TryGetProperty("sha1sum", out var sha1) ? sha1.GetString() : null,
                Sha256 = f.TryGetProperty("sha256sum", out var sha256) ? sha256.GetString() : null,
                Size = f.TryGetProperty("filesize", out var size) ? size.GetInt64() : 0,
                Date = f.TryGetProperty("releasedate", out var date) && date.ValueKind == JsonValueKind.String ? date.GetString()[..10] : "",
                Signed = f.TryGetProperty("signed", out var signed) && signed.ValueKind == JsonValueKind.True,
                Beta = IsBetaBuild(build),
            });
        }
        return Sorted(list);
    }

    // ---- installers (software update catalogs) ----

    static List<Item> Installers(Options o)
    {
        var urls = o.CatalogUrl != null ? [o.CatalogUrl]
            : (o.Betas ? Catalogs : Catalogs.Take(1)).Select(c => CatalogBase + c + CatalogSuffix).ToList();
        var products = new Dictionary<string, (Dictionary<string, object> Product, bool Seed)>();
        foreach (var url in urls)
        {
            if (!o.Quiet) Console.Error.WriteLine($"Retrieving macOS Installers from {url[(url.LastIndexOf('/') + 1)..]}...");
            Dictionary<string, object> catalog;
            using (var stream = GetStream(url)) catalog = ReadPlist(stream) as Dictionary<string, object>;
            if (catalog?.GetValueOrDefault("Products") is not Dictionary<string, object> all) continue;
            foreach (var (id, value) in all)
            {
                if (value is not Dictionary<string, object> product) continue;
                if (product.GetValueOrDefault("ExtendedMetaInfo") is not Dictionary<string, object> meta) continue;
                if (meta.GetValueOrDefault("InstallAssistantPackageIdentifiers") is not Dictionary<string, object> ids) continue;
                if (!ids.ContainsKey("SharedSupport") && !ids.ContainsKey("OSInstall")) continue;
                products.TryAdd(id, (product, url != urls[0]));
            }
        }

        var items = new Item[products.Count];
        Parallel.ForEach(products.Select((p, i) => (p, i)), new ParallelOptions { MaxDegreeOfParallelism = 8 }, entry =>
        {
            var ((id, (product, seed)), index) = entry;
            var item = new Item { Identifier = id };
            if (product.GetValueOrDefault("PostDate") is string date) item.Date = date.Length >= 10 ? date[..10] : date;
            if (product.GetValueOrDefault("Packages") is List<object> packages)
                foreach (var p in packages.OfType<Dictionary<string, object>>())
                    if (p.GetValueOrDefault("URL") is string u)
                        item.Packages.Add((u, p.GetValueOrDefault("Size") is long s ? s : 0));
            item.Size = item.Packages.Sum(p => p.Size);
            if (product.GetValueOrDefault("Distributions") is Dictionary<string, object> dists
                && (dists.GetValueOrDefault("English") ?? dists.GetValueOrDefault("en")) is string dist)
            {
                try
                {
                    var text = GetString(dist);
                    item.Name = System.Text.RegularExpressions.Regex.Match(text, "<title>(.*?)</title>").Groups[1].Value;
                    item.Version = System.Text.RegularExpressions.Regex.Match(text, "<key>VERSION</key>\\s*<string>(.*?)</string>").Groups[1].Value;
                    item.Build = System.Text.RegularExpressions.Regex.Match(text, "<key>BUILD</key>\\s*<string>(.*?)</string>").Groups[1].Value;
                }
                catch (HttpRequestException) { }
            }
            if (string.IsNullOrEmpty(item.Name) || item.Name.StartsWith("SU_TITLE", StringComparison.Ordinal)) item.Name = item.Version.Length > 0 ? NameFor(item.Version) : "macOS";
            item.Beta = seed || IsBetaBuild(item.Build ?? "") || item.Name.Contains("beta", StringComparison.OrdinalIgnoreCase);
            items[index] = item;
        });
        return Sorted(items.Where(i => i != null && !string.IsNullOrEmpty(i.Version) && !string.IsNullOrEmpty(i.Build))
            .GroupBy(i => i.Build).Select(g => g.First()));
    }

    // ---- list ----

    static int List(Options o)
    {
        if (o.Help) return 0;
        var all = o.Kind == "firmware" ? Firmwares(o.Quiet) : Installers(o);
        var items = Matching(all, o.Search).Where(i => o.Betas || !i.Beta).ToList();
        if (o.Latest) items = items.Take(1).ToList();
        var output = Render(o, items);
        if (o.Export != null)
        {
            var path = Path.GetFullPath(o.Export);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, output, new UTF8Encoding(false));
            if (!o.Quiet) Console.Error.WriteLine($"Exported list as {o.OutputType.ToUpperInvariant()}: '{path}'");
        }
        if (!o.Quiet || o.Export == null) Console.Write(output);
        if (items.Count == 0 && !o.Quiet) Console.Error.WriteLine($"No macOS {(o.Kind == "firmware" ? "Firmwares" : "Installers")} found with '{o.Search}', exiting...");
        return 0;
    }

    static string Render(Options o, List<Item> items)
    {
        var firmware = o.Kind == "firmware";
        var columns = firmware ? new[] { "signed", "name", "version", "build", "size", "url", "date", "shasum", "beta" }
            : new[] { "identifier", "name", "version", "build", "size", "url", "date", "beta" };
        object Field(Item i, string c) => c switch
        {
            "signed" => i.Signed, "identifier" => i.Identifier, "name" => i.Name, "version" => i.Version, "build" => i.Build,
            "size" => i.Size, "url" => firmware ? i.Url : i.PackageUrl ?? "", "date" => i.Date ?? "", "shasum" => i.Sha1 ?? "", "beta" => i.Beta,
            _ => "",
        };
        var sb = new StringBuilder();
        switch (o.OutputType)
        {
            case "json":
            {
                using var stream = new MemoryStream();
                using (var w = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                {
                    w.WriteStartArray();
                    foreach (var i in items)
                    {
                        w.WriteStartObject();
                        foreach (var c in columns)
                            switch (Field(i, c))
                            {
                                case bool b: w.WriteBoolean(c, b); break;
                                case long n: w.WriteNumber(c, n); break;
                                case var v: w.WriteString(c, v?.ToString()); break;
                            }
                        w.WriteEndObject();
                    }
                    w.WriteEndArray();
                }
                return Encoding.UTF8.GetString(stream.ToArray()) + "\n";
            }
            case "csv":
                sb.AppendLine(string.Join(',', columns.Select(c => c == "shasum" ? "SHA1" : char.ToUpperInvariant(c[0]) + c[1..])));
                foreach (var i in items)
                    sb.AppendLine(string.Join(',', columns.Select(c => Csv(Field(i, c) is bool b ? (b ? "True" : "False") : Field(i, c).ToString()))));
                return sb.ToString();
            case "plist":
                sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n<plist version=\"1.0\">\n<array>\n");
                foreach (var i in items)
                {
                    sb.Append("\t<dict>\n");
                    foreach (var c in columns)
                    {
                        sb.Append($"\t\t<key>{c}</key>\n");
                        sb.Append(Field(i, c) switch
                        {
                            bool b => b ? "\t\t<true/>\n" : "\t\t<false/>\n",
                            long n => $"\t\t<integer>{n}</integer>\n",
                            var v => $"\t\t<string>{System.Security.SecurityElement.Escape(v?.ToString() ?? "")}</string>\n",
                        });
                    }
                    sb.Append("\t</dict>\n");
                }
                return sb.Append("</array>\n</plist>\n").ToString();
            case "yaml":
                if (items.Count == 0) return "[]\n";
                foreach (var i in items)
                {
                    var first = true;
                    foreach (var c in columns)
                    {
                        var v = Field(i, c);
                        var text = v is bool b ? (b ? "true" : "false") : v is long n ? n.ToString(CultureInfo.InvariantCulture) : "'" + (v?.ToString() ?? "").Replace("'", "''") + "'";
                        sb.Append(first ? "- " : "  ").Append(c).Append(": ").Append(text).Append('\n');
                        first = false;
                    }
                }
                return sb.ToString();
            default:
            {
                var headers = firmware ? new[] { "SIGNED", "NAME", "VERSION", "BUILD", "SIZE", "DATE" } : new[] { "IDENTIFIER", "NAME", "VERSION", "BUILD", "SIZE", "DATE" };
                var rows = new List<string[]> { headers };
                foreach (var i in items)
                    rows.Add([firmware ? (i.Signed ? "True" : "False") : i.Identifier, i.Name + (i.Beta ? " beta" : ""), i.Version, i.Build, FormatSize(i.Size), i.Date ?? ""]);
                var widths = headers.Select((_, c) => rows.Max(r => r[c].Length)).ToArray();
                var line = "+" + string.Join("+", widths.Select(w => new string('-', w + 2))) + "+\n";
                sb.Append(line);
                for (var r = 0; r < rows.Count; r++)
                {
                    sb.Append("| ").Append(string.Join(" | ", rows[r].Select((cell, c) => cell.PadRight(widths[c])))).Append(" |\n");
                    if (r == 0) sb.Append(line);
                }
                sb.Append(line);
                sb.Append($"Total: {items.Count}\n");
                return sb.ToString();
            }
        }
    }

    static string Csv(string s) => s.IndexOfAny([',', '"', '\n']) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;

    static string FormatSize(long bytes) => bytes >= 1_000_000_000 ? $"{bytes / 1e9:0.00} GB" : $"{bytes / 1e6:0.00} MB";

    // ---- download ----

    static int Download(Options o)
    {
        if (o.Help) return 0;
        if (string.IsNullOrEmpty(o.Search)) throw new UsageException("missing expected argument '<search-string>'");
        if (o.Kind == "installer" && o.Outputs.Count == 0) throw new UsageException("missing expected argument '<output-type> ...' (package)");
        foreach (var output in o.Outputs)
            if (output is not ("application" or "image" or "iso" or "package" or "bootableinstaller"))
                throw new UsageException($"invalid output type '{output}'");

        var items = Matching(o.Kind == "firmware" ? Firmwares(o.Quiet) : Installers(o), o.Search).Where(i => o.Betas || !i.Beta).ToList();
        var item = items.FirstOrDefault();
        if (item == null)
        {
            Console.Error.WriteLine($"Error: No macOS {(o.Kind == "firmware" ? "Firmware" : "Installer")} found with '{o.Search}', exiting...");
            return 1;
        }
        var directory = Path.GetFullPath(o.OutputDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "Mist"));
        Directory.CreateDirectory(directory);
        if (!o.Quiet) Console.Error.WriteLine($"Found {item.Name} {item.Version} ({item.Build}), {FormatSize(item.Size)}");

        if (o.Kind == "firmware")
        {
            var path = Path.Combine(directory, Template(o.FirmwareName ?? "Install %NAME% %VERSION%-%BUILD%.ipsw", item));
            if (!item.Signed && !o.Quiet) Console.Error.WriteLine("Warning: this firmware is no longer signed by Apple, so it can't be restored to a Mac");
            Fetch(item.Url, path, item.Size, o, item.Sha256 != null ? ("SHA256", item.Sha256) : item.Sha1 != null ? ("SHA1", item.Sha1) : default);
            Console.WriteLine(path);
            return 0;
        }

        var status = 0;
        foreach (var output in o.Outputs.Distinct())
        {
            if (output != "package")
            {
                Console.Error.WriteLine($"Error: the {output} output needs macOS ({(output == "application" ? "expanding the installer package" : output == "bootableinstaller" ? "createinstallmedia" : "hdiutil")}); download the package and build it on a Mac");
                status = 1;
                continue;
            }
            if (item.PackageUrl == null)
            {
                Console.Error.WriteLine($"Error: {item.Name} {item.Version} predates the full-installer package (InstallAssistant.pkg); its parts can only be assembled on macOS");
                status = 1;
                continue;
            }
            var path = Path.Combine(directory, Template(o.PackageName ?? "Install %NAME% %VERSION%-%BUILD%.pkg", item));
            var size = item.Packages.First(p => p.Url == item.PackageUrl).Size;
            Fetch(item.PackageUrl, path, size, o, default);
            Console.WriteLine(path);
        }
        return status;
    }

    static string Template(string template, Item item) =>
        string.Concat(template.Replace("%NAME%", item.Name).Replace("%VERSION%", item.Version).Replace("%BUILD%", item.Build)
            .Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '-' : c));

    // Streams url to path (via a .download file), checking size and, when known, the checksum.
    static void Fetch(string url, string path, long expectedSize, Options o, (string Algorithm, string Hex) checksum)
    {
        if (File.Exists(path) && !o.Force) throw new InvalidOperationException($"{path} already exists (use --force to overwrite)");
        var partial = path + ".download";
        using var response = Http.Send(new HttpRequestMessage(HttpMethod.Get, url), HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"{url}: {(int)response.StatusCode} {response.ReasonPhrase}");
        var total = response.Content.Headers.ContentLength ?? expectedSize;
        using (var source = response.Content.ReadAsStream())
        using (var target = File.Create(partial))
        using (var hash = checksum.Algorithm == null ? null : IncrementalHash.CreateHash(checksum.Algorithm == "SHA256" ? HashAlgorithmName.SHA256 : HashAlgorithmName.SHA1))
        {
            var buffer = new byte[1 << 20];
            long done = 0;
            var shown = DateTime.MinValue;
            int n;
            while ((n = source.Read(buffer)) > 0)
            {
                target.Write(buffer, 0, n);
                hash?.AppendData(buffer, 0, n);
                done += n;
                if (!o.Quiet && (DateTime.UtcNow - shown).TotalMilliseconds > 500)
                {
                    shown = DateTime.UtcNow;
                    Console.Error.Write($"\rDownloading {Path.GetFileName(path)}: {FormatSize(done)} of {FormatSize(total)}" + (total > 0 ? $" ({done * 100 / total}%)" : "") + "   ");
                }
            }
            if (!o.Quiet) Console.Error.WriteLine();
            if (total > 0 && done != total) throw new InvalidOperationException($"download incomplete ({done} of {total} bytes)");
            if (hash != null && !Convert.ToHexStringLower(hash.GetHashAndReset()).Equals(checksum.Hex, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"{checksum.Algorithm} checksum mismatch for {Path.GetFileName(path)}");
        }
        File.Move(partial, path, overwrite: true);
    }

    // ---- HTTP and plists ----

    static string GetString(string url)
    {
        using var response = Http.Send(new HttpRequestMessage(HttpMethod.Get, url));
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"{url}: {(int)response.StatusCode} {response.ReasonPhrase}");
        return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }

    static Stream GetStream(string url)
    {
        var response = Http.Send(new HttpRequestMessage(HttpMethod.Get, url), HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"{url}: {(int)response.StatusCode} {response.ReasonPhrase}");
        return response.Content.ReadAsStream();
    }

    // XML property list -> Dictionary<string, object> / List<object> / string / long / double / bool.
    internal static object ReadPlist(Stream stream)
    {
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, IgnoreWhitespace = true, IgnoreComments = true });
        reader.MoveToContent();
        if (reader.Name != "plist") throw new InvalidOperationException("not an XML property list");
        reader.Read();
        reader.MoveToContent();
        return ReadValue(reader);
    }

    static object ReadValue(XmlReader r)
    {
        switch (r.Name)
        {
            case "dict":
            {
                var dict = new Dictionary<string, object>();
                if (r.IsEmptyElement) { r.Read(); return dict; }
                r.Read();
                while (r.NodeType == XmlNodeType.Element)
                {
                    var key = r.ReadElementContentAsString();
                    r.MoveToContent();
                    dict[key] = ReadValue(r);
                    r.MoveToContent();
                }
                r.ReadEndElement();
                return dict;
            }
            case "array":
            {
                var list = new List<object>();
                if (r.IsEmptyElement) { r.Read(); return list; }
                r.Read();
                while (r.NodeType == XmlNodeType.Element) { list.Add(ReadValue(r)); r.MoveToContent(); }
                r.ReadEndElement();
                return list;
            }
            case "true": r.Read(); return true;
            case "false": r.Read(); return false;
            case "integer": return long.TryParse(r.ReadElementContentAsString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var l) ? l : 0L;
            case "real": return double.TryParse(r.ReadElementContentAsString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0.0;
            default: return r.ReadElementContentAsString(); // string, date, data
        }
    }
}
