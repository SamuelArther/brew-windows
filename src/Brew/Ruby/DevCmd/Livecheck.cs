using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Brew.Ruby.DevCmd;

// GitHub::API errors: the message is GitHub's own `message` (or Homebrew's rate-limit text).
sealed class GitHubApiError(string message, int status) : Exception(message)
{
    public int Status { get; } = status;
}

// HTTP for livecheck / bump / create: Homebrew's user agent, GitHub API with credentials.
static class DevHttp
{
    public const string HomebrewVersion = "7.0.9";
    public const string FakeSafari = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 " +
                                     "(KHTML, like Gecko) Version/27.0 Safari/605.1.15";
    public const string ApiUrl = "https://api.github.com";

    public static readonly string UserAgent =
        Environment.GetEnvironmentVariable("HOMEBREW_USER_AGENT_CURL") is { Length: > 0 } ua ? ua
            : $"Homebrew/{HomebrewVersion} (Windows; {(RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "arm64" : "x86_64")} Windows {Environment.OSVersion.Version.ToString(3)})";

    static readonly HttpClient Client = Make(true, 15);
    static readonly HttpClient NoRedirect = Make(false, 15);
    static readonly HttpClient Downloads = Make(true, 900);

    static HttpClient Make(bool redirects, int timeout) => new(new HttpClientHandler
    {
        AllowAutoRedirect = redirects,
        MaxAutomaticRedirections = 5,
        AutomaticDecompression = DecompressionMethods.All,
    }) { Timeout = TimeSpan.FromSeconds(timeout) };

    static string token;
    static bool tokenLooked, tokenFromGh;

    // GitHub::API.credentials: $HOMEBREW_GITHUB_API_TOKEN, then $GITHUB_TOKEN, then `gh auth token`.
    public static string GitHubToken
    {
        get
        {
            if (tokenLooked) return token;
            tokenLooked = true;
            token = Environment.GetEnvironmentVariable("HOMEBREW_GITHUB_API_TOKEN");
            if (string.IsNullOrEmpty(token)) token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (string.IsNullOrEmpty(token) && DevCommands.Which("gh") is { } gh)
            {
                try
                {
                    var (code, stdout, _) = DevCommands.Capture(gh, new[] { "auth", "token" });
                    if (code == 0 && stdout.Trim().Length > 0) { token = stdout.Trim(); tokenFromGh = true; }
                }
                catch (Exception e) when (e is IOException or System.ComponentModel.Win32Exception) { }
            }
            if (string.IsNullOrEmpty(token)) token = null;
            return token;
        }
    }

    public static bool NoGitHubApi => Environment.GetEnvironmentVariable("HOMEBREW_NO_GITHUB_API") is { Length: > 0 };

    // GitHub::API.open_rest; returns the raw response body.
    public static string GitHubRest(string url, string method = "GET", JsonNode data = null)
    {
        using var req = new HttpRequestMessage(new HttpMethod(method), url);
        req.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
        req.Headers.TryAddWithoutValidation("Accept", "application/vnd.github+json");
        req.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
        if (GitHubToken != null) req.Headers.TryAddWithoutValidation("Authorization", $"bearer {GitHubToken}");
        if (data != null) req.Content = new StringContent(data.ToJsonString(), Encoding.UTF8, "application/json");
        HttpResponseMessage resp;
        try { resp = Client.Send(req); }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            throw new GitHubApiError($"curl failed! {e.Message}", 0);
        }
        using (resp)
        {
            var body = resp.Content.ReadAsStringAsync().Result;
            int code = (int)resp.StatusCode;
            if (code < 400) return body;
            string message = null;
            try { message = JsonNode.Parse(body)?["message"]?.GetValue<string>(); } catch (JsonException) { }
            message ??= $"curl failed! The requested URL returned error: {code}";
            if (code == 403 && resp.Headers.TryGetValues("x-ratelimit-remaining", out var rem) && rem.First() == "0")
            {
                var reset = long.Parse(resp.Headers.GetValues("x-ratelimit-reset").First());
                var resource = resp.Headers.TryGetValues("x-ratelimit-resource", out var r) ? r.First() : "core";
                var limit = resp.Headers.TryGetValues("x-ratelimit-limit", out var l) ? l.First() : "60";
                var mins = Math.Max(0, (int)Math.Ceiling((DateTimeOffset.FromUnixTimeSeconds(reset) - DateTimeOffset.UtcNow).TotalMinutes));
                var pat = GitHubToken == null
                    ? ", or:\nCreate a GitHub personal access token:\n  https://github.com/settings/tokens\nand then set it as `HOMEBREW_GITHUB_API_TOKEN`."
                    : "";
                message = $"GitHub API Error: {message}\nRate limit exceeded for {resource} resource ({limit} limit).\nTry again in {mins} {Out.Pluralize("minute", mins)}{pat}";
            }
            else if (code == 401 || code == 403)
            {
                message = $"GitHub API Error: {message}";
                if (GitHubToken == null) { }
                else if (tokenFromGh)
                    message += "\nYour GitHub CLI login session may be invalid.\nRefresh it with:\n  gh auth login --hostname github.com";
                else
                    message += $"\n`$HOMEBREW_GITHUB_API_TOKEN` may be invalid or expired; check:\n  {Formatter.Url("https://github.com/settings/tokens")}";
            }
            throw new GitHubApiError(message, code);
        }
    }

    public static JsonNode GitHubJson(string url, string method = "GET", JsonNode data = null) =>
        JsonNode.Parse(GitHubRest(url, method, data));

    // Strategy.page_content: (content, final_url, messages)
    public static (string content, string finalUrl, List<string> messages) PageContent(string url)
    {
        string lastError = null;
        foreach (var ua in new[] { UserAgent, FakeSafari })
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.TryAddWithoutValidation("User-Agent", ua);
                using var resp = Client.Send(req);
                if (!resp.IsSuccessStatusCode)
                {
                    lastError = $"curl: (22) The requested URL returned error: {(int)resp.StatusCode}";
                    continue;
                }
                var content = resp.Content.ReadAsStringAsync().Result;
                var final = resp.RequestMessage?.RequestUri?.ToString();
                return (content, final != null && final != url ? final : null, null);
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException or InvalidOperationException)
            {
                lastError = e is TaskCanceledException ? "curl: (28) Operation timed out" : $"curl: (6) {e.Message}";
            }
        }
        return (null, null, new List<string> { lastError ?? "cURL failed without a detectable error" });
    }

    // Strategy.page_headers: the headers of every response in the redirect chain (lowercase names).
    public static List<Dictionary<string, string>> PageHeaders(string url)
    {
        foreach (var ua in new[] { UserAgent, FakeSafari })
        {
            var all = new List<Dictionary<string, string>>();
            try
            {
                var current = new Uri(url);
                for (int i = 0; i <= 5; i++)
                {
                    using var req = new HttpRequestMessage(HttpMethod.Head, current);
                    req.Headers.TryAddWithoutValidation("User-Agent", ua);
                    using var resp = NoRedirect.Send(req);
                    var h = new Dictionary<string, string>();
                    foreach (var kv in resp.Headers.Concat(resp.Content.Headers)) h[kv.Key.ToLowerInvariant()] = string.Join(", ", kv.Value);
                    all.Add(h);
                    if ((int)resp.StatusCode is >= 300 and < 400 && resp.Headers.Location is { } loc) current = new Uri(current, loc);
                    else break;
                }
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException) { continue; }
            if (all.Count > 0) return all;
        }
        return new List<Dictionary<string, string>>();
    }

    // Downloads url into dest (following redirects). Throws BrewError on failure.
    public static void Download(string url, string dest)
    {
        var tmp = dest + ".incomplete";
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
            using var resp = Downloads.Send(req, HttpCompletionOption.ResponseHeadersRead);
            if (!resp.IsSuccessStatusCode)
                throw new BrewError($"Download failed: {url}\ncurl: (22) The requested URL returned error: {(int)resp.StatusCode}");
            using (var o = File.Create(tmp)) resp.Content.ReadAsStream().CopyTo(o);
            File.Move(tmp, dest, true);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            throw new BrewError($"Download failed: {url}\n{e.Message}");
        }
        finally { if (File.Exists(tmp)) File.Delete(tmp); }
    }

    // Resource#fetch for a URL without a checksum: "==> Downloading", cache, then Homebrew's
    // missing-checksum warning. Returns (cached file, sha256).
    public static (string file, string sha256) FetchWithoutChecksum(string url, string name, string version)
    {
        var cache = DevCommands.Paths.Cache;
        Directory.CreateDirectory(cache);
        var uri = new Uri(url);
        var basename = Path.GetFileName(Uri.UnescapeDataString(uri.AbsolutePath));
        if (string.IsNullOrEmpty(basename)) basename = "download";
        var urlHash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(url)));
        var ext = Regex.Match(basename, @"(\.tar\.[a-z0-9]+|\.[A-Za-z0-9]+)$").Value;
        var cached = Path.Combine(cache, $"{urlHash}--{name}--{version}{ext}");
        if (File.Exists(cached)) Out.Puts($"Already downloaded: {cached}");
        else
        {
            Out.Ohai($"Downloading {url}");
            Download(url, cached);
        }
        string sha;
        using (var s = File.OpenRead(cached)) sha = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(s));
        Out.Opoo($"Cannot verify integrity of '{Path.GetFileName(cached)}'.\nNo checksum was provided.\nFor your reference, the checksum is:\n  sha256 \"{sha}\"");
        return (cached, sha);
    }

    // "owner/repo" from a GitHub URL, or null.
    public static (string owner, string repo)? GitHubRepo(string url)
    {
        var m = Regex.Match(url ?? "", @"^https?://github\.com/(?:downloads/)?(?<u>[^/]+)/(?<r>[^/#?]+)", RegexOptions.IgnoreCase);
        return m.Success ? (m.Groups["u"].Value, Regex.Replace(m.Groups["r"].Value, @"\.git$", "")) : null;
    }
}

// Ruby values for livecheck `strategy` blocks.
static class RubyJson
{
    public static object ToRuby(JsonNode n) => n switch
    {
        null => null,
        JsonObject o => ToHash(o),
        JsonArray a => new RArray(a.Select(ToRuby)),
        JsonValue v when v.GetValueKind() == JsonValueKind.String => Interp.S(v.GetValue<string>()),
        JsonValue v when v.GetValueKind() == JsonValueKind.True => true,
        JsonValue v when v.GetValueKind() == JsonValueKind.False => false,
        JsonValue v when v.TryGetValue<long>(out var l) => l,
        JsonValue v => v.GetValue<double>(),
        _ => n.ToJsonString(),
    };

    static RHash ToHash(JsonObject o)
    {
        var h = new RHash();
        foreach (var kv in o) h.D[Interp.S(kv.Key)] = ToRuby(kv.Value);
        return h;
    }
}

// The result of Livecheck.latest_version.
sealed class LatestVersionInfo
{
    public string Latest;
    public List<string> Messages;   // strategy messages when no versions matched
    public string Strategy, Url, ProcessedUrl, StrategyUrl, FinalUrl, Regex, UrlSymbol;
    public List<string> Strategies;
}

// Port of Homebrew::Livecheck (Library/Homebrew/livecheck/livecheck.rb, skip_conditions.rb and the
// Git, GithubLatest, GithubReleases, HeaderMatch, Json and PageMatch strategies).
static class LivecheckEngine
{
    public const string NoCurrentVersionMsg = "Unable to identify current version";
    public const string NoVersionsMsg = "Unable to get versions";
    static readonly string[] UnstableVersionKeywords = { "alpha", "beta", "bpo", "dev", "experimental", "prerelease", "preview", "rc" };

    sealed class Strat
    {
        public string Name, Symbol;
        public int Priority;
        public Func<string, bool> Match;
        public Func<string, string> Preprocess;
        public bool NeedsUrl = true;
    }

    // Strategy.constants.sort order
    static readonly Strat[] Strategies =
    {
        new() { Name = "Apache", Symbol = "apache", Priority = 5, Match = u => ApacheRegex.IsMatch(u) },
        new() { Name = "Git", Symbol = "git", Priority = 8, Match = GitMatch, Preprocess = GitPreprocess },
        new() { Name = "GithubLatest", Symbol = "github_latest", Priority = 0, Match = GithubMatch },
        new() { Name = "GithubReleases", Symbol = "github_releases", Priority = 0, Match = GithubMatch },
        new() { Name = "Gnu", Symbol = "gnu", Priority = 5, Match = u => GnuRegex.IsMatch(u) && !u.Contains("savannah.") },
        new() { Name = "HeaderMatch", Symbol = "header_match", Priority = 0, Match = HttpMatch },
        new() { Name = "Json", Symbol = "json", Priority = 5, Match = HttpMatch },
        new() { Name = "PageMatch", Symbol = "page_match", Priority = 0, Match = HttpMatch },
        new() { Name = "Sourceforge", Symbol = "sourceforge", Priority = 5, Match = u => SourceforgeRegex.IsMatch(u) },
    };

    static readonly Regex ApacheRegex = new(@"^https?://(?:www\.apache\.org/dyn/.+(?:path|filename)=/?|archive\.apache\.org/dist/|dlcdn\.apache\.org/|downloads\.apache\.org/)(?<path>.+?)/(?<prefix>[^/]*?)v?\d+(?:\.\d+)+(?<suffix>/|[^/]*)", RegexOptions.IgnoreCase);
    static readonly Regex GnuRegex = new(@"^https?://(?:(?:[^/]+?\.)*gnu\.org/(?:gnu|software)/(?<project_name>[^/]+)/|ftpmirror\.gnu\.org/(?!non-gnu/)(?<project_name>[^/]+)/|(?<project_name>[^/]+)\.gnu\.org/?$)", RegexOptions.IgnoreCase);
    static readonly Regex SourceforgeRegex = new(@"^https?://(?:[^/]+?\.)*(?:sourceforge|sf)\.net(?:/projects?/(?<project_name>[^/]+)/|/p/(?<project_name>[^/]+)/|(?::/cvsroot)?/(?<project_name>[^/]+))", RegexOptions.IgnoreCase);
    static readonly Regex TarballExtension = new(@"\.t(?:ar(?:\.(?:bz2|gz|lz|lzma|lzo|xz|Z|zst))?|b2|bz2?|z2|az|gz|lz|lzma|xz|Z|aZ|zst)$", RegexOptions.IgnoreCase);

    static string RubyEscape(string s) => Regex.Escape(s).Replace("\\ ", " ").Replace("\\#", "#");

    // Apache / Gnu / Sourceforge: generated page URL + regex, then PageMatch.
    static (string url, string regex) GeneratedInput(string strategy, string url)
    {
        switch (strategy)
        {
            case "Apache":
            {
                var m = ApacheRegex.Match(url);
                if (!m.Success) return (null, null);
                var suffix = TarballExtension.Replace(m.Groups["suffix"].Value, ".t", 1);
                return ($"https://archive.apache.org/dist/{m.Groups["path"].Value}/",
                    $"href=[\"']?{RubyEscape(m.Groups["prefix"].Value)}v?(\\d+(?:\\.\\d+)+){RubyEscape(suffix)}");
            }
            case "Gnu":
            {
                var name = GnuRegex.Match(url).Groups["project_name"].Value;
                if (name.Length == 0) return (null, null);
                return ($"https://ftpmirror.gnu.org/{name}/", $"href=.*?{RubyEscape(name)}[._-]v?(\\d+(?:\\.\\d+)*)(?:\\.[a-z]+|/)");
            }
            case "Sourceforge":
            {
                var name = SourceforgeRegex.Match(url).Groups["project_name"].Value;
                if (name.Length == 0) return (null, null);
                var page = Regex.IsMatch(url, @"/rss(?:/?$|\?)") ? url : $"https://sourceforge.net/projects/{name}/rss";
                return (page, $"url=.*?/{RubyEscape(name)}/files/.*?[-_/](\\d+(?:[-.]\\d+)+)[-_/%.]");
            }
        }
        return (null, null);
    }

    static readonly Regex GithubUrlRegex = new(@"^https?://github\.com/(?:downloads/)?(?<username>[^/]+)/(?<repository>[^/]+)", RegexOptions.IgnoreCase);
    static readonly Regex GithubDefaultRegex = new(@"v?(\d+(?:\.\d+)+)", RegexOptions.IgnoreCase);
    const string GithubDefaultRegexInspect = @"/v?(\d+(?:\.\d+)+)/i";
    static readonly Regex GitTagRegex = new(@"^[0-9a-fA-F]+\s+refs/tags/(.+?)(?:\^\{\})?$", RegexOptions.Multiline);
    static readonly Regex GitDefaultRegex = new(@"\D*(.+)");
    const string GitDefaultRegexInspect = @"/\D*(.+)/";

    static bool HttpMatch(string url) => Regex.IsMatch(url, "^https?://", RegexOptions.IgnoreCase);
    static bool GithubMatch(string url) => GithubUrlRegex.IsMatch(url);

    static readonly string[] GiteaInstances = { "codeberg.org", "gitea.com", "opendev.org", "tildegit.org" };
    static readonly string[] GogsInstances = { "lolg.it" };

    static string GitPreprocess(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return url;
        var host = uri.Host;
        var path = uri.AbsolutePath;
        if (string.IsNullOrEmpty(host) || path is "" or "/") return url;
        if (host == "github.s3.amazonaws.com") host = "github.com";
        path = path.TrimStart('/');
        if (path.EndsWith(".git")) path = path[..^4];
        var scheme = uri.Scheme;
        string processed = null;
        string[] Parts(string p) => p.Split('/');
        if (host == "github.com")
        {
            if (Regex.IsMatch(path, "/releases/latest/?$")) return url;
            var p = Parts(path.StartsWith("downloads/") ? path["downloads/".Length..] : path);
            processed = $"{scheme}://{host}/{p[0]}/{(p.Length > 1 ? p[1] : "")}.git";
        }
        else if (GiteaInstances.Contains(host))
        {
            if (Regex.IsMatch(path, "/releases/latest/?$")) return url;
            var p = Parts(path);
            processed = $"{scheme}://{host}/{p[0]}/{(p.Length > 1 ? p[1] : "")}.git";
        }
        else if (GogsInstances.Contains(host))
        {
            var p = Parts(path);
            processed = $"{scheme}://{host}/{p[0]}/{(p.Length > 1 ? p[1] : "")}.git";
        }
        else if (host == "git.sr.ht")
        {
            var p = Parts(path);
            processed = $"{scheme}://{host}/{p[0]}/{(p.Length > 1 ? p[1] : "")}";
        }
        else if (path.Contains("/-/archive/"))
            processed = Regex.Replace(url, "/-/archive/.*$", ".git", RegexOptions.IgnoreCase);
        return processed ?? url;
    }

    // DownloadStrategyDetector.detect(url) <= GitDownloadStrategy
    static bool GitMatch(string url)
    {
        url = GitPreprocess(url);
        return Regex.IsMatch(url, @"^https?://github\.com/[^/]+/[^/]+\.git$")
               || Regex.IsMatch(url, @"^https?://.+\.git$")
               || url.StartsWith("git://")
               || Regex.IsMatch(url, @"^https?://git\.sr\.ht/[^/]+/[^/]+$")
               || url.StartsWith("ssh://git");
    }

    static List<Strat> FromUrl(string url, string livecheckStrategy, bool regexProvided, bool blockProvided) =>
        Strategies.Where(s =>
        {
            if (s.Name == "PageMatch") { if (!regexProvided && !blockProvided) return false; }
            else if (s.Name == "Json") { if (livecheckStrategy != s.Symbol || !blockProvided) return false; }
            else if (s.Priority <= 0 && livecheckStrategy != s.Symbol) return false;
            return s.Match(url);
        }).OrderByDescending(s => s.Priority).ToList();

    static Strat FromSymbol(string symbol) => symbol == null ? null : Strategies.FirstOrDefault(s => s.Symbol == symbol);

    public static string Name(FormulaSpec f, bool fullName) => fullName && !f.Path.Contains("homebrew-core") ? FullName(f) : f.Name;

    // Formula#full_name: tap-qualified unless in homebrew/core.
    public static string FullName(FormulaSpec f)
    {
        var tap = DevTap.FromPath(f.Path);
        return tap == null || tap == "homebrew/core" ? f.Name : $"{tap}/{f.Name}";
    }

    public static bool Versioned(FormulaSpec f) => f.Name.Contains('@');

    public static JsonObject StatusHash(FormulaSpec f, string name, string status, List<string> messages = null)
    {
        var h = new JsonObject { ["formula"] = name, ["status"] = status };
        if (messages != null) h["messages"] = new JsonArray(messages.Select(m => (JsonNode)m).ToArray());
        h["meta"] = new JsonObject { ["livecheck_defined"] = f.HasLivecheck };
        return h;
    }

    // SkipConditions.skip_information (formula checks); null when not skipped.
    public static JsonObject SkipInformation(FormulaSpec f, string name)
    {
        bool defined = f.HasLivecheck;
        var url = f.Url ?? "";
        string msg = null;
        if (!string.IsNullOrEmpty(f.Livecheck?.Skip)) msg = f.Livecheck.Skip;
        else if (!defined)
        {
            if (Regex.IsMatch(url, @"https?://storage\.googleapis\.com/google-code-archive-downloads/", RegexOptions.IgnoreCase)) msg = "Stable URL is from Google Code Archive";
            else if (Regex.IsMatch(url, @"https?://web\.archive\.org/", RegexOptions.IgnoreCase)) msg = "Stable URL is from Internet Archive";
            else if (Regex.IsMatch(url, @"https?://gist\.github(?:usercontent)?\.com/", RegexOptions.IgnoreCase)) msg = "Stable URL is a GitHub Gist";
        }
        if (f.Livecheck?.Skip != null || msg != null)
            return StatusHash(f, name, "skipped", msg != null ? new List<string> { msg } : null);
        if (f.Disabled && !defined) return StatusHash(f, name, "disabled");
        if (f.Deprecated && !defined) return StatusHash(f, name, "deprecated");
        if (Versioned(f) && !defined) return StatusHash(f, name, "versioned");
        return null;
    }

    // SkipConditions.referenced_skip_information
    public static JsonObject ReferencedSkipInformation(FormulaSpec referenced, string originalName, bool fullName)
    {
        var refName = Name(referenced, fullName);
        var info = SkipInformation(referenced, refName);
        if (info == null) return null;
        var status = (string)info["status"];
        if (status != "error" && !(status == "skipped" && referenced.Livecheck?.Skip != null))
        {
            var end = status == "skipped" ? "automatically skipped" : $"skipped as {status}";
            throw new BrewError($"Referenced formula ({refName}) is {end}");
        }
        info["formula"] = originalName;
        return info;
    }

    public static void PrintSkipInformation(JsonObject skip)
    {
        var name = (string)skip["formula"];
        if (name == null) return;
        var messages = skip["messages"] as JsonArray;
        var status = (string)skip["status"];
        if (messages is { Count: > 0 })
        {
            var joined = string.Join("; ", messages.Select(m => (string)m));
            Out.Puts(status == "skipped" ? $"{Tty.Red}{name}{Tty.Reset}: skipped - {joined}" : $"{Tty.Red}{name}{Tty.Reset}: {joined}");
        }
        else if (!string.IsNullOrEmpty(status)) Out.Puts($"{Tty.Red}{name}{Tty.Reset}: {status}");
    }

    // Livecheck.resolve_livecheck_reference: follows `formula "other"` references.
    public static (FormulaSpec referenced, List<FormulaSpec> chain) ResolveReference(FormulaSpec f, bool fullName, bool debug)
    {
        var chain = new List<FormulaSpec>();
        var current = f;
        while (current.Livecheck?.Formula is { } refName)
        {
            if (refName == "parent") break;
            FormulaSpec next;
            try { next = DevTap.LoadFormula(refName); }
            catch (BrewError) { throw new BrewError($"No available formula with the name \"{refName}\"."); }
            if (chain.Any(c => c.Path == next.Path) || next.Path == f.Path)
            {
                Out.Onoe($"Circular formula reference: {string.Join(" -> ", chain.Prepend(f).Append(next).Select(x => Name(x, fullName)))}");
                throw new BrewError("Circular formula reference");
            }
            chain.Add(next);
            current = next;
        }
        if (chain.Count == 0) return (null, chain);
        if (debug && chain.Count > 1)
        {
            Out.Puts("Reference Chain:");
            Out.Puts(Name(f, fullName));
            foreach (var c in chain) Out.Puts(Name(c, fullName));
        }
        return (chain[^1], chain);
    }

    static string UrlToString(string url, string symbol, FormulaSpec pkg)
    {
        if (url != null) return url;
        var s = symbol switch
        {
            "stable" => pkg.Url,
            "head" => pkg.HeadUrl,
            "homepage" => pkg.Homepage,
            _ => null,
        };
        if (s == null) throw new BrewError($"`url :{symbol}` does not reference a checkable URL");
        return s;
    }

    static List<string> CheckableUrls(FormulaSpec f)
    {
        var urls = new List<string>();
        if (f.Url != null) { urls.Add(f.Url); urls.AddRange(f.Mirrors); }
        if (f.HeadUrl != null) urls.Add(f.HeadUrl);
        if (f.Homepage != null) urls.Add(f.Homepage);
        return urls.Where(u => u != null).Distinct().ToList();
    }

    static string RegexInspect(LivecheckSpec lc) => lc?.Regex == null ? null : InspectRegex(lc.Regex, lc.RegexFlags);

    // Regexp#inspect: an unescaped "/" is shown as "\/".
    static string InspectRegex(string source, string flags) =>
        "/" + Regex.Replace(source, @"(?<!\\)((?:\\\\)*)/", @"$1\/") + "/" + (flags ?? "");

    // Livecheck.latest_version
    public static LatestVersionInfo LatestVersion(FormulaSpec f, FormulaSpec referenced, List<FormulaSpec> references,
        bool json, bool fullName, bool verbose, bool debug)
    {
        var lc = f.Livecheck ?? new LivecheckSpec();
        var rlc = referenced?.Livecheck;
        var lcUrl = lc.Url ?? (lc.UrlSymbol == null ? rlc?.Url : null);
        var lcUrlSymbol = lc.Url == null ? lc.UrlSymbol ?? (rlc?.Url == null ? rlc?.UrlSymbol : null) : null;
        var regexOwner = lc.Regex != null ? f : rlc?.Regex != null ? referenced : null;
        var regexSpec = regexOwner?.Livecheck;
        var strategySym = lc.Strategy ?? rlc?.Strategy;
        var block = lc.StrategyBlock ?? rlc?.StrategyBlock;
        var blockOwner = lc.StrategyBlock != null ? f : referenced;
        var pkg = referenced ?? f;

        string lcUrlString = lcUrl != null || lcUrlSymbol != null ? UrlToString(lcUrl, lcUrlSymbol, pkg) : null;
        var urls = lcUrlString != null ? new List<string> { lcUrlString } : CheckableUrls(pkg);

        if (debug)
        {
            Out.Puts($"Formula:          {Name(f, fullName)}");
            Out.Puts($"livecheck block?: {(f.HasLivecheck ? "Yes" : "No")}");
            foreach (var r in references) Out.Puts($"Formula Ref:      {Name(r, fullName)}");
        }

        Regex regex = regexSpec == null ? null : regexOwner.Interp.MakeRegex(regexSpec.Regex, regexSpec.RegexFlags ?? "").R;
        var regexInspect = RegexInspect(regexSpec);
        var checkedUrls = new List<string>();
        for (int i = 0; i < urls.Count; i++)
        {
            var original = urls[i];
            var url = original;
            if (checkedUrls.Contains(url)) continue;
            var strategies = FromUrl(url, strategySym, regex != null, block != null);
            var strategy = FromSymbol(strategySym) ?? strategies.FirstOrDefault();
            if (strategy == null) continue;
            if (strategy.Preprocess != null)
            {
                url = strategy.Preprocess(url);
                if (checkedUrls.Contains(url)) continue;
            }

            if (debug)
            {
                Out.Puts();
                if (lcUrlSymbol != null) Out.Puts($"URL ({lcUrlSymbol}):".PadRight(18) + original);
                else if (!string.IsNullOrEmpty(original) && original != "None") Out.Puts($"URL:              {original}");
                if (url != original) Out.Puts($"URL (processed):  {url}");
                if (strategies.Count > 0 && verbose) Out.Puts($"Strategies:       {string.Join(", ", strategies.Select(s => s.Name))}");
                Out.Puts($"Strategy:         {strategy.Name}");
                if (regexInspect != null) Out.Puts($"Regex:            {regexInspect}");
            }

            if (strategySym != null)
            {
                if (lcUrlString == null && strategy.NeedsUrl) continue;
                if (strategySym != "page_match" && !strategies.Contains(strategy)) continue;
            }

            var data = FindVersions(strategy, url, regex, regexInspect, block, blockOwner);
            checkedUrls.Add(url);

            if (data.Messages != null && data.Matches.Count == 0)
            {
                if (!json) foreach (var m in data.Messages) Out.Puts(m);
                if (i + 1 < urls.Count) continue;
                return new LatestVersionInfo { Messages = data.Messages };
            }

            if (debug)
            {
                if (data.Url != null && data.Url != url) Out.Puts($"URL (strategy):   {data.Url}");
                if (data.FinalUrl != null) Out.Puts($"URL (final):      {data.FinalUrl}");
                if (data.RegexInspect != null && data.RegexInspect != regexInspect) Out.Puts($"Regex (strategy): {data.RegexInspect}");
            }

            var matches = data.Matches.Where(kv =>
            {
                if (string.IsNullOrEmpty(kv.Value)) return false;
                if (f.HasLivecheck) return true;
                return !UnstableVersionKeywords.Any(k => kv.Value.Contains(k));
            }).ToList();
            if (matches.Count == 0) continue;

            if (debug)
            {
                Out.Puts();
                Out.Puts("Matched Versions:");
                if (verbose) foreach (var (m, v) in matches) Out.Puts($"{m} => #<Version {v}>");
                else Out.Puts(string.Join(", ", matches.Select(m => m.Value)));
            }

            var latest = matches.Select(m => m.Value).Aggregate((a, b) => Versions.Compare(b, a) > 0 ? b : a);
            return new LatestVersionInfo
            {
                Latest = latest,
                Strategy = strategy.Name,
                Strategies = strategies.Select(s => s.Name).ToList(),
                Url = original,
                ProcessedUrl = url != original ? url : null,
                StrategyUrl = data.Url != null && data.Url != url ? data.Url : null,
                FinalUrl = data.FinalUrl,
                Regex = data.RegexInspect,
                UrlSymbol = lcUrlSymbol,
            };
        }
        return null;
    }


    sealed class StrategyData
    {
        public List<KeyValuePair<string, string>> Matches = new();
        public List<string> Messages;
        public string Url, FinalUrl, RegexInspect;

        public void Add(IEnumerable<string> texts)
        {
            foreach (var t in texts)
                if (t != null && Matches.All(m => m.Key != t)) Matches.Add(new(t, t));
        }
    }

    static string Group1(Regex r, string s)
    {
        var m = r.Match(s);
        return m.Success && m.Groups.Count > 1 && m.Groups[1].Success ? m.Groups[1].Value : null;
    }

    static StrategyData FindVersions(Strat s, string url, Regex regex, string regexInspect, RProc block, FormulaSpec blockOwner)
    {
        var d = new StrategyData { Url = url, RegexInspect = regexInspect };
        switch (s.Name)
        {
            case "Git":
            {
                var git = DevCommands.Which("git") ?? throw new BrewError("Git is required for the Git strategy.");
                var (code, stdout, stderr) = CaptureWithTimeout(git, new[] { "ls-remote", "--tags", "--end-of-options", url }, 20);
                if (code == -1) { d.Messages = new List<string> { "git ls-remote timed out after 20 seconds." }; return d; }
                if (!string.IsNullOrWhiteSpace(stderr)) d.Messages = stderr.TrimEnd('\n').Split('\n').ToList();
                if (string.IsNullOrEmpty(stdout)) return d;
                var tags = GitTagRegex.Matches(stdout).Select(m => m.Groups[1].Value).Distinct().ToList();
                if (tags.Count == 0) return d;
                if (block != null)
                {
                    var rtags = new RArray(tags.Select(t => (object)Interp.S(t)));
                    var args = regex != null || BlockArity(block) == 2
                        ? new object[] { rtags, RubyRegex(blockOwner, regex, regexInspect ?? GitDefaultRegexInspect) }
                        : new object[] { rtags };
                    d.Add(CallBlock(blockOwner, block, args));
                }
                else d.Add(tags.Select(t => Group1(regex ?? GitDefaultRegex, t)).Distinct());
                return d;
            }
            case "GithubLatest":
            case "GithubReleases":
            {
                var latest = s.Name == "GithubLatest";
                if (regexInspect == null) d.RegexInspect = GithubDefaultRegexInspect;
                var m = GithubUrlRegex.Match(url.EndsWith(".git") ? url[..^4] : url);
                if (!m.Success) return d;
                d.Url = $"{DevHttp.ApiUrl}/repos/{m.Groups["username"].Value}/{m.Groups["repository"].Value}/releases{(latest ? "/latest" : "")}";
                var content = DevHttp.GitHubRest(d.Url);
                if (string.IsNullOrEmpty(content)) return d;
                var node = JsonNode.Parse(content);
                if (block != null)
                {
                    d.Add(CallBlock(blockOwner, block, new[] { RubyJson.ToRuby(node), RubyRegex(blockOwner, regex, d.RegexInspect) }));
                    return d;
                }
                var releases = node is JsonArray arr ? arr.ToList() : new List<JsonNode> { node };
                d.Add(releases.Where(r => r is JsonObject).Where(r => !(r["draft"]?.GetValue<bool>() ?? false) && !(r["prerelease"]?.GetValue<bool>() ?? false))
                    .Select(r => r["tag_name"]?.GetValue<string>() is { } tag ? Group1(regex ?? GithubDefaultRegex, tag) : null).Distinct());
                return d;
            }
            case "Apache":
            case "Gnu":
            case "Sourceforge":
            {
                var (pageUrl, genRegex) = GeneratedInput(s.Name, url);
                if (pageUrl == null) return d;
                if (regex == null)
                {
                    regex = new Regex(genRegex, RegexOptions.IgnoreCase);
                    regexInspect = InspectRegex(genRegex, "i");
                }
                var inner = FindVersions(Strategies.First(x => x.Name == "PageMatch"), pageUrl, regex, regexInspect, block, blockOwner);
                inner.Url = pageUrl;
                return inner;
            }
            case "PageMatch":
            case "Json":
            {
                if (s.Name == "PageMatch" && regex == null && block == null)
                    throw new BrewError("PageMatch requires a regex or `strategy` block");
                if (s.Name == "Json" && block == null) throw new BrewError("Json requires a `strategy` block");
                var (content, final, messages) = DevHttp.PageContent(url);
                d.FinalUrl = final;
                d.Messages = messages;
                if (string.IsNullOrEmpty(content)) return d;
                if (s.Name == "Json")
                {
                    JsonNode node;
                    try { node = JsonNode.Parse(content); }
                    catch (JsonException) { throw new BrewError("Content could not be parsed as JSON."); }
                    var args = regex != null ? new[] { RubyJson.ToRuby(node), RubyRegex(blockOwner, regex, regexInspect) } : new[] { RubyJson.ToRuby(node) };
                    d.Add(CallBlock(blockOwner, block, args));
                }
                else if (block != null)
                {
                    var args = regex != null ? new object[] { Interp.S(content), RubyRegex(blockOwner, regex, regexInspect) } : new object[] { Interp.S(content) };
                    d.Add(CallBlock(blockOwner, block, args));
                }
                else
                    d.Add(regex.Matches(content).Select(m => m.Groups.Count > 1 ? m.Groups[1].Value : m.Value).Distinct());
                return d;
            }
            case "HeaderMatch":
            {
                var headers = DevHttp.PageHeaders(url);
                if (headers.Count == 0) return d;
                var merged = new Dictionary<string, string>();
                foreach (var h in headers) foreach (var kv in h) merged[kv.Key] = kv.Value;
                if (block != null)
                {
                    var rh = new RHash();
                    foreach (var kv in merged) rh.D[Interp.S(kv.Key)] = Interp.S(kv.Value);
                    var args = regex != null ? new object[] { rh, RubyRegex(blockOwner, regex, regexInspect) } : new object[] { rh };
                    d.Add(CallBlock(blockOwner, block, args));
                    return d;
                }
                d.Add(new[] { "content-disposition", "location" }.Select(name =>
                {
                    if (!merged.TryGetValue(name, out var v) || string.IsNullOrEmpty(v)) return null;
                    if (regex != null) return Group1(regex, v);
                    return Versions.Parse(v);
                }).Where(v => v != null).Distinct());
                return d;
            }
        }
        return d;
    }

    static int BlockArity(RProc p) => p.Block?.Params.Count(x => !x.Block && !x.Keyword) ?? 0;

    static object RubyRegex(FormulaSpec owner, Regex regex, string inspect)
    {
        if (inspect == null) return null;
        var m = System.Text.RegularExpressions.Regex.Match(inspect, @"^/(.*)/([a-z]*)$", RegexOptions.Singleline);
        return owner.Interp.MakeRegex(m.Groups[1].Value, m.Groups[2].Value);
    }

    // Strategy.handle_block_return
    static IEnumerable<string> CallBlock(FormulaSpec owner, RProc block, object[] args)
    {
        var it = owner.Interp;
        object result;
        try { result = FormulaLoader.OnBigStack(() => it.CallProc(block, args)); }
        catch (RubyError e) { throw new BrewError(e.Message); }
        return result switch
        {
            null => Array.Empty<string>(),
            RString s => new[] { s.V },
            RArray a => a.L.Where(x => x != null).Select(it.ToS).Distinct().ToList(),
            RObject o when o.Class?.Name == "Version" => new[] { it.ToS(o) },
            _ => throw new BrewError("Return value of a strategy block must be a string or array of strings."),
        };
    }

    static (int code, string stdout, string stderr) CaptureWithTimeout(string file, string[] args, int seconds)
    {
        var psi = new System.Diagnostics.ProcessStartInfo(file)
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        psi.Environment["GIT_TERMINAL_PROMPT"] = "0";
        using var p = System.Diagnostics.Process.Start(psi)!;
        var outTask = p.StandardOutput.ReadToEndAsync();
        var errTask = p.StandardError.ReadToEndAsync();
        if (!p.WaitForExit(seconds * 1000))
        {
            try { p.Kill(true); } catch (InvalidOperationException) { }
            return (-1, "", "");
        }
        return (p.ExitCode, outTask.Result.Replace("\r\n", "\n"), errTask.Result.Replace("\r\n", "\n"));
    }

    public sealed class Options
    {
        public bool Json, FullName, NewerOnly, Quiet, Debug, Verbose;
    }

    // Livecheck.run_checks
    public static void RunChecks(List<FormulaSpec> formulae, Options o)
    {
        var ambiguous = o.FullName ? new HashSet<FormulaSpec>()
            : formulae.GroupBy(f => f.Name).Where(g => g.Count() > 1).SelectMany(g => g).ToHashSet();
        bool hasNewer = false;
        var results = new List<JsonObject>();
        bool progress = o.Json && !o.Quiet && !Console.IsErrorRedirected;
        if (progress) Console.Error.WriteLine(Formatter.Headline("Running checks", Tty.Blue));
        int done = 0;
        void Tick()
        {
            if (!progress) return;
            done++;
            int width = 40, filled = formulae.Count == 0 ? width : done * width / formulae.Count;
            Console.Error.Write($"\r Progress: [{new string('#', filled)}{new string('.', width - filled)}] {done}/{formulae.Count} ");
        }

        for (int i = 0; i < formulae.Count; i++)
        {
            var f = formulae[i];
            bool useFull = o.FullName || ambiguous.Contains(f);
            var name = useFull ? FullName(f) : f.Name;
            try
            {
                var (referenced, refs) = ResolveReference(f, useFull, o.Debug);
                if (o.Debug && i > 0) Out.Puts("\n----------\n");
                else if (o.Debug) Out.Puts();

                var skip = referenced != null ? ReferencedSkipInformation(referenced, name, useFull) : null;
                skip ??= SkipInformation(f, name);
                if (skip != null)
                {
                    if (o.Json && !o.NewerOnly) { results.Add(skip); Tick(); continue; }
                    if (!o.NewerOnly && !o.Quiet) PrintSkipInformation(skip);
                    Tick();
                    continue;
                }

                var current = f.Version;
                if (string.IsNullOrEmpty(current))
                {
                    if (!o.Json) throw new LivecheckError(NoCurrentVersionMsg);
                    if (!o.Quiet) results.Add(StatusHash(f, name, "error", new List<string> { NoCurrentVersionMsg }));
                    Tick();
                    continue;
                }

                var info = LatestVersion(f, referenced, refs, o.Json, useFull, o.Verbose, o.Debug);
                var latest = info?.Latest;
                if (string.IsNullOrEmpty(latest))
                {
                    if (!o.Json) throw new LivecheckError(NoVersionsMsg);
                    Tick();
                    if (o.Quiet) continue;
                    results.Add(StatusHash(f, name, "error", info?.Messages ?? new List<string> { NoVersionsMsg }));
                    continue;
                }

                var rel = Regex.Match(latest, "(.*)-release$");
                if (rel.Success && !Regex.IsMatch(current, ".*-release$")) latest = rel.Groups[1].Value;

                bool outdated = Versions.Compare(current, latest) < 0;
                bool newerThanUpstream = Versions.Compare(current, latest) > 0;
                if (o.NewerOnly && !outdated) { Tick(); continue; }
                hasNewer = true;

                if (o.Json)
                {
                    Tick();
                    var h = new JsonObject
                    {
                        ["formula"] = name,
                        ["version"] = new JsonObject
                        {
                            ["current"] = current, ["latest"] = latest, ["outdated"] = outdated, ["newer_than_upstream"] = newerThanUpstream,
                        },
                    };
                    if (o.Verbose) h["meta"] = Meta(f, info, refs, useFull);
                    results.Add(h);
                    continue;
                }
                if (o.Debug) Out.Puts();
                var nameS = $"{Tty.Blue}{name}{Tty.Reset}" + (o.Verbose && !f.HasLivecheck ? " (guessed)" : "");
                var currentS = newerThanUpstream ? $"{Tty.Red}{current}{Tty.Reset}" : current;
                var latestS = outdated ? $"{Tty.Green}{latest}{Tty.Reset}" : latest;
                Out.Puts($"{nameS}: {currentS} ==> {latestS}");
            }
            catch (Exception e) when (e is BrewError or LivecheckError or GitHubApiError or InvalidOperationException or IOException or HttpRequestException)
            {
                DevCommands.Failed = true;
                Tick();
                if (o.Json)
                {
                    if (!o.Quiet) results.Add(StatusHash(f, name, "error", new List<string> { e.Message }));
                }
                else if (!o.Quiet)
                {
                    Out.Onoe($"{Tty.Blue}{name}{Tty.Reset}: {e.Message}");
                    if (o.Debug && e is not LivecheckError) Console.Error.WriteLine(e.StackTrace);
                }
            }
        }

        if (o.NewerOnly && !hasNewer && !o.Debug && !o.Json && !o.Quiet) Out.Puts("No newer upstream versions.");
        if (!o.Json) return;
        if (progress) Console.Error.Write("\r\u001b[K\u001b[1A\u001b[K");
        Out.Puts(new JsonArray(results.Select(r => (JsonNode)r).ToArray()).ToJsonString(PrettyJson));
    }

    public static readonly JsonSerializerOptions PrettyJson = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    static JsonObject Meta(FormulaSpec f, LatestVersionInfo info, List<FormulaSpec> refs, bool fullName)
    {
        var meta = new JsonObject { ["livecheck_defined"] = f.HasLivecheck };
        if (refs.Count > 0) meta["references"] = new JsonArray(refs.Select(r => (JsonNode)new JsonObject { ["formula"] = Name(r, fullName) }).ToArray());
        if (info.Url != "None")
        {
            var url = new JsonObject();
            if (info.UrlSymbol != null) url["symbol"] = info.UrlSymbol;
            url["original"] = info.Url;
            if (info.ProcessedUrl != null) url["processed"] = info.ProcessedUrl;
            if (info.StrategyUrl != null) url["strategy"] = info.StrategyUrl;
            if (info.FinalUrl != null) url["final"] = info.FinalUrl;
            meta["url"] = url;
        }
        if (info.Strategy != null) meta["strategy"] = info.Strategy;
        if (info.Strategies is { Count: > 0 }) meta["strategies"] = new JsonArray(info.Strategies.Select(s => (JsonNode)s).ToArray());
        if (info.Regex != null) meta["regex"] = info.Regex;
        return meta;
    }
}

// Livecheck::Error
sealed class LivecheckError(string message) : Exception(message);

[DevCommand("livecheck", "lc")]
sealed class LivecheckCmd : DevCommand
{
    public override CommandParser Args() => new CommandParser("livecheck", "lc")
        .Description("""
            Check for newer versions of formulae and/or casks from upstream.
            If no formula or cask argument is passed, the list of formulae and
            casks to check is taken from `$HOMEBREW_LIVECHECK_WATCHLIST` or
            `${XDG_CONFIG_HOME}/homebrew/livecheck_watchlist.txt` if
            `$XDG_CONFIG_HOME` is set or `~/.homebrew/livecheck_watchlist.txt`
            otherwise.
            """)
        .Switch(new[] { "--full-name" }, "Print formulae and casks with fully-qualified names.")
        .Flag(new[] { "--tap=" }, "Check formulae and casks within the given tap, specified as <user>`/`<repo>.")
        .Switch(new[] { "--eval-all" }, null, hidden: true)
        .Switch(new[] { "--installed" }, "Check formulae and casks that are currently installed.")
        .Switch(new[] { "--newer-only" }, "Show the latest version only if it's newer than the current formula or cask version.")
        .Switch(new[] { "--json" }, "Output information in JSON format.")
        .Switch(new[] { "-r", "--resources" }, "Also check resources for formulae.")
        .Switch(new[] { "-q", "--quiet" }, "Suppress warnings, don't print a progress bar for JSON output.")
        .Switch(new[] { "--formula", "--formulae" }, "Only check formulae.")
        .Switch(new[] { "--cask", "--casks" }, "Only check casks.")
        .Switch(new[] { "--extract-plist" }, "Enable checking multiple casks with ExtractPlist strategy.")
        .Switch(new[] { "--autobump" }, "Include packages that are autobumped by BrewTestBot. By default these are skipped.")
        .Conflicts("--tap", "--installed", "--eval-all")
        .Conflicts("--json", "--debug")
        .Conflicts("--formula", "--cask")
        .Conflicts("--formula", "--extract-plist")
        .NamedArgs(new[] { "formula", "cask" });

    static string WatchlistPath()
    {
        var configured = Environment.GetEnvironmentVariable("HOMEBREW_LIVECHECK_WATCHLIST");
        string path;
        if (!string.IsNullOrEmpty(configured)) path = configured;
        else if (Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") is { Length: > 0 } xdg) path = Path.Combine(xdg, "homebrew", "livecheck_watchlist.txt");
        else path = "~/.homebrew/livecheck_watchlist.txt";
        if (path.StartsWith("~")) path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + path[1..];
        return Path.GetFullPath(path);
    }

    public override void Run(ParsedArgs args)
    {
        if (args.Switch("eval-all"))
            throw new BrewError("Calling `brew livecheck --eval-all` is disabled! Use the default trusted-tap behaviour instead.");
        var formulae = new List<FormulaSpec>();
        if (args.Flag("tap") is { } tapName)
        {
            var dir = DevTap.Path(tapName);
            if (!args.Switch("cask")) formulae.AddRange(DevTap.FormulaFiles(dir).Select(FormulaLoader.LoadSpec));
        }
        else if (args.Switch("installed"))
        {
            if (!args.Switch("cask") && Directory.Exists(DevCommands.Cellar))
                foreach (var rack in Directory.GetDirectories(DevCommands.Cellar).Order(StringComparer.Ordinal))
                {
                    if (Directory.GetDirectories(rack).Length == 0) continue;
                    if (DevCommands.FormulaPath(Path.GetFileName(rack)) is { } p) formulae.Add(FormulaLoader.LoadSpec(p));
                }
        }
        else if (args.Named.Count > 0)
        {
            foreach (var n in args.Named)
            {
                if (args.Switch("cask")) throw new BrewError($"No available cask with the name \"{n}\".");
                var p = DevTap.FormulaPath(n) ?? throw new BrewError($"No available formula or cask with the name \"{DevCommands.NameOf(n)}\".");
                formulae.Add(FormulaLoader.LoadSpec(p));
            }
        }
        else if (File.Exists(WatchlistPath()))
        {
            var names = File.ReadAllLines(WatchlistPath()).Select(line =>
            {
                var idx = line.IndexOf('#');
                if (idx == 0) return null;
                if (idx > 0) line = line[..idx];
                return line.Trim();
            }).Where(l => !string.IsNullOrEmpty(l));
            foreach (var n in names)
                if (DevTap.FormulaPath(n) is { } p) formulae.Add(FormulaLoader.LoadSpec(p));
        }
        else if (!args.Switch("cask"))
        {
            foreach (var file in DevCommands.AllFormulaFiles())
            {
                try { formulae.Add(FormulaLoader.LoadSpec(file)); }
                catch (InvalidOperationException e) { Out.Opoo($"{Path.GetFileNameWithoutExtension(file)}: {e.Message}"); }
            }
        }

        formulae = formulae.OrderBy(f => f.Name, StringComparer.Ordinal).ToList();
        if (formulae.Count == 0) throw new UsageError("No formulae or casks to check.");

        LivecheckEngine.RunChecks(formulae, new LivecheckEngine.Options
        {
            Json = args.Switch("json"),
            FullName = args.Switch("full-name"),
            NewerOnly = args.Switch("newer-only"),
            Quiet = args.Quiet,
            Debug = args.Debug,
            Verbose = args.Verbose,
        });
    }
}
