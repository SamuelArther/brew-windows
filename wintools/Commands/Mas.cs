using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace WinTools;

// mas: the Mac App Store CLI, for the Microsoft Store. Searching and app details come from the
// Store's catalog services (the ones winget's msstore source and apps.microsoft.com use);
// installs, upgrades and removals go through `winget --source msstore`. App ids are Store
// product ids such as 9WZDNCRFJ3TJ.
static class Mas
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("mas", "Microsoft Store command-line interface (mas)", Run);
    }

    const string Help = """
        USAGE: mas <subcommand>

        Microsoft Store command-line interface. App ids are Store product ids (9WZDNCRFJ3TJ).

        SUBCOMMANDS:
          list                    Lists apps installed from the Microsoft Store
          outdated                Lists pending updates from the Microsoft Store
          search <term>           Searches the Microsoft Store (--price shows prices)
          info <id>               Displays app information from the Microsoft Store
          install <id>...         Installs apps from the Microsoft Store (also: get, purchase)
          upgrade [<id>...]       Upgrades outdated apps installed from the Microsoft Store
          uninstall <id>...       Uninstalls apps installed from the Microsoft Store (--dry-run)
          lucky <term>            Installs the first search result
          open [<id>]             Opens an app's page in the Microsoft Store app
          home <id>               Opens an app's page on apps.microsoft.com
          vendor <id>             Opens an app's vendor website
          account, signin, signout
                                  The Store account is managed in the Store app
          version                 Outputs the version of the mas tool
        """;

    static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help") { Console.WriteLine(Help); return args.Length == 0 ? 1 : 0; }
        var command = args[0];
        var rest = args[1..].Where(a => !a.StartsWith('-')).ToArray();
        var flags = args[1..].Where(a => a.StartsWith('-')).ToHashSet();
        foreach (var flag in flags)
            if (flag is not ("--price" or "--dry-run" or "--force" or "--verbose")) throw new UsageException($"unknown option '{flag}'");

        switch (command)
        {
            case "version" or "--version":
                Console.WriteLine(Commands.Version);
                return 0;

            case "account" or "signin" or "signout" or "reset":
                Console.Error.WriteLine("mas: the Microsoft Store doesn't let other programs manage its account; sign in or out from the profile button in the Store app");
                return 1;

            case "search":
            {
                if (rest.Length == 0) throw new UsageException("search needs a search term");
                var results = Search(string.Join(' ', rest));
                if (results.Count == 0) { Console.Error.WriteLine("No apps found"); return 1; }
                var width = results.Max(r => r.Name.Length);
                foreach (var r in results)
                {
                    var tail = flags.Contains("--price") && r.Price != null ? $"  {r.Price}" : "";
                    Console.WriteLine($"{r.Id,14}  {r.Name.PadRight(width)}{(r.Version != null ? $"  ({r.Version})" : "")}{tail}");
                }
                return 0;
            }

            case "info":
            {
                if (rest.Length != 1) throw new UsageException("info needs one app id");
                var app = Info(rest[0]);
                if (app == null) { Console.Error.WriteLine($"mas: no app found with id {rest[0]}"); return 1; }
                Console.WriteLine($"{app.Name}{(app.Version != null ? " " + app.Version : "")} [{app.Price ?? "Free"}]");
                Console.WriteLine($"By: {app.Publisher}");
                if (app.Released != null) Console.WriteLine($"Released: {app.Released}");
                if (app.Category != null) Console.WriteLine($"Category: {app.Category}");
                if (app.Size != null) Console.WriteLine($"Size: {app.Size}");
                Console.WriteLine($"From: https://apps.microsoft.com/detail/{app.Id}");
                return 0;
            }

            case "list":
            {
                Winget.Require("mas");
                foreach (var r in Winget.Table("mas", "list", "--source", "msstore"))
                    Console.WriteLine($"{r.Id,-14}  {r.Name}  ({r.Version})");
                return 0;
            }

            case "outdated":
            {
                Winget.Require("mas");
                foreach (var r in Winget.Table("mas", "upgrade", "--source", "msstore"))
                    Console.WriteLine($"{r.Id,-14}  {r.Name}  ({r.Version} -> {r.Available})");
                return 0;
            }

            case "install" or "get" or "purchase" or "lucky":
            {
                if (rest.Length == 0) throw new UsageException($"{command} needs an app id");
                var ids = rest;
                if (command == "lucky")
                {
                    var first = Search(string.Join(' ', rest)).FirstOrDefault();
                    if (first == null) { Console.Error.WriteLine("No apps found"); return 1; }
                    ids = [first.Id];
                }
                Winget.Require("mas");
                var status = 0;
                foreach (var id in ids)
                {
                    string[] wargs = ["install", "--id", id, "--source", "msstore", "--accept-package-agreements", "--accept-source-agreements"];
                    if (flags.Contains("--force")) wargs = [.. wargs, "--force"];
                    if (Winget.Pass(wargs) != 0) status = 1;
                }
                return status;
            }

            case "upgrade":
            {
                Winget.Require("mas");
                if (rest.Length == 0)
                    return Winget.Pass(["upgrade", "--all", "--source", "msstore", "--accept-package-agreements", "--accept-source-agreements"]) == 0 ? 0 : 1;
                var status = 0;
                foreach (var id in rest)
                    if (Winget.Pass(["upgrade", "--id", id, "--source", "msstore", "--accept-package-agreements", "--accept-source-agreements"]) != 0) status = 1;
                return status;
            }

            case "uninstall":
            {
                if (rest.Length == 0) throw new UsageException("uninstall needs an app id");
                Winget.Require("mas");
                var installed = Winget.Table("mas", "list", "--source", "msstore");
                var status = 0;
                foreach (var id in rest)
                {
                    var app = installed.FirstOrDefault(r => r.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
                    if (app == null) { Console.Error.WriteLine($"mas: not installed: {id}"); status = 1; continue; }
                    if (flags.Contains("--dry-run")) { Console.WriteLine($"==> Dry run. Would have uninstalled {app.Name} ({app.Id})"); continue; }
                    if (Winget.Pass(["uninstall", "--id", app.Id, "--source", "msstore"]) != 0) status = 1;
                }
                return status;
            }

            case "open":
                Launch(rest.Length > 0 ? $"ms-windows-store://pdp/?ProductId={rest[0]}" : "ms-windows-store:");
                return 0;

            case "home":
                if (rest.Length != 1) throw new UsageException("home needs one app id");
                Launch($"https://apps.microsoft.com/detail/{rest[0]}");
                return 0;

            case "vendor":
            {
                if (rest.Length != 1) throw new UsageException("vendor needs one app id");
                var app = Info(rest[0]);
                if (app?.Website == null) { Console.Error.WriteLine($"mas: no vendor website for {rest[0]}"); return 1; }
                Launch(app.Website);
                return 0;
            }

            default:
                throw new UsageException($"unknown subcommand '{command}' (see mas --help)");
        }
    }

    sealed class App
    {
        public string Id, Name, Version, Price, Publisher, Released, Category, Size, Website;
    }

    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

    // winget's msstore search first; the Store web catalog when that comes back empty.
    static List<App> Search(string term)
    {
        var results = new List<App>();
        var json = "{\"Query\":{\"KeyWord\":" + JsonString(term) + ",\"MatchType\":\"Substring\"}}";
        try
        {
            using var response = Http.PostAsync("https://storeedgefd.dsx.mp.microsoft.com/v9.0/manifestSearch",
                new StringContent(json, Encoding.UTF8, "application/json")).Result;
            using var doc = JsonDocument.Parse(response.Content.ReadAsStringAsync().Result);
            if (doc.RootElement.TryGetProperty("Data", out var data) && data.ValueKind == JsonValueKind.Array)
                foreach (var item in data.EnumerateArray())
                    results.Add(new App
                    {
                        Id = Str(item, "PackageIdentifier"),
                        Name = Str(item, "PackageName"),
                        Publisher = Str(item, "Publisher"),
                    });
        }
        catch (Exception e) when (e is HttpRequestException or AggregateException or JsonException) { }
        if (results.Count > 0) return results;

        var url = $"https://apps.microsoft.com/api/products/search?query={Uri.EscapeDataString(term)}&mediaType=all&age=all&price=all&category=all&subscription=all&gl={Market()}&hl=en-us";
        using var doc2 = Get(url);
        if (doc2 != null && doc2.RootElement.TryGetProperty("productsList", out var list))
            foreach (var item in list.EnumerateArray())
                results.Add(new App
                {
                    Id = Str(item, "productId")?.ToUpperInvariant(),
                    Name = Str(item, "title"),
                    Publisher = Str(item, "publisherName"),
                    Price = Str(item, "displayPrice"),
                });
        return results.Where(r => r.Id != null && r.Name != null).ToList();
    }

    // The Store web catalog's product details, else the msstore manifest winget reads.
    static App Info(string id)
    {
        id = id.ToUpperInvariant();
        using (var doc = Get($"https://apps.microsoft.com/api/ProductsDetails/GetProductDetailsById/{Uri.EscapeDataString(id)}?hl=en-US&gl={Market()}"))
        {
            if (doc != null && doc.RootElement.ValueKind == JsonValueKind.Object && Str(doc.RootElement, "title") is { } title)
            {
                var root = doc.RootElement;
                var app = new App
                {
                    Id = id,
                    Name = title,
                    Version = NullIfEmpty(Str(root, "version")),
                    Price = Str(root, "displayPrice"),
                    Publisher = Str(root, "publisherName"),
                    Website = NullIfEmpty(Str(root, "appWebsiteUrl")),
                };
                if (Str(root, "releaseDateUtc") is { Length: >= 10 } released) app.Released = released[..10];
                if (root.TryGetProperty("categories", out var cats) && cats.ValueKind == JsonValueKind.Array && cats.GetArrayLength() > 0)
                    app.Category = cats[0].GetString();
                if (root.TryGetProperty("approximateSizeInBytes", out var size) && size.TryGetInt64(out var bytes) && bytes > 0)
                    app.Size = FormatSize(bytes);
                return app;
            }
        }
        using var manifest = Get($"https://storeedgefd.dsx.mp.microsoft.com/v9.0/packageManifests/{Uri.EscapeDataString(id)}");
        if (manifest == null || !manifest.RootElement.TryGetProperty("Data", out var data) || data.ValueKind != JsonValueKind.Object) return null;
        var version = data.GetProperty("Versions")[0];
        var locale = version.GetProperty("DefaultLocale");
        return new App
        {
            Id = id,
            Name = Str(locale, "PackageName"),
            Version = Str(version, "PackageVersion") is { } v && v != "Unknown" ? v : null,
            Publisher = Str(locale, "Publisher"),
            Website = Str(locale, "PublisherUrl"),
        };
    }

    static JsonDocument Get(string url)
    {
        try
        {
            using var response = Http.GetAsync(url).Result;
            if (!response.IsSuccessStatusCode) return null;
            return JsonDocument.Parse(response.Content.ReadAsStringAsync().Result);
        }
        catch (Exception e) when (e is HttpRequestException or AggregateException or JsonException or TaskCanceledException)
        {
            return null;
        }
    }

    static string Str(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    static string NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

    static string JsonString(string s)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) writer.WriteStringValue(s);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    static string FormatSize(long bytes) =>
        bytes >= 1L << 30 ? $"{bytes / (double)(1L << 30):0.0} GB" : bytes >= 1 << 20 ? $"{bytes / (double)(1 << 20):0.0} MB" : $"{bytes / 1024.0:0.0} KB";

    // The Windows region setting decides the storefront, like mas uses the App Store country.
    static string Market()
    {
        var buffer = new char[16];
        var n = GetUserDefaultGeoName(buffer, buffer.Length);
        var geo = n > 1 ? new string(buffer, 0, n - 1) : "";
        return geo.Length == 2 && geo.All(char.IsAsciiLetter) ? geo : "US";
    }

    static void Launch(string target) => Process.Start(new ProcessStartInfo(target) { UseShellExecute = true })?.Dispose();

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern int GetUserDefaultGeoName(char[] geoName, int count);
}
