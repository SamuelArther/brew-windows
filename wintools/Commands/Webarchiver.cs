using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace WinTools;

// webarchiver: save a web page as a Safari .webarchive, the binary property list Safari (and
// WebKit, Pages, Mail) open as a self-contained page: WebMainResource holds the page itself,
// WebSubresources every image, stylesheet, script and frame it loads, each with its URL, MIME type
// and text encoding. The page is fetched with HttpClient (http, https, or file:// for local pages),
// its subresources are found in the HTML (and url(...) in its stylesheets), and the archive is
// written with a binary plist writer.
static class Webarchiver
{
    const string Version = "0.13";

    [ModuleInitializer]
    internal static void Register() => Commands.Add("webarchiver", "Save a web page as a Safari .webarchive file", Run);

    sealed class Resource
    {
        public string Url, MimeType, Encoding, FrameName;
        public byte[] Data;
    }

    static int Run(string[] args)
    {
        string url = null, output = null;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "-help" or "--help":
                    Console.WriteLine($"""
                        webarchiver {Version}
                        Usage: webarchiver -url URL -output FILE
                        Example: webarchiver -url https://www.google.com -output google.webarchive
                          -url     http(s) or file:// URL of the page to archive
                          -output  the .webarchive file to write
                        """);
                    return 0;
                case "-url" or "--url": url = ++i < args.Length ? args[i] : throw new UsageException("-url needs a value"); break;
                case "-output" or "--output": output = ++i < args.Length ? args[i] : throw new UsageException("-output needs a value"); break;
                default: throw new UsageException($"unknown argument {args[i]} (usage: webarchiver -url URL -output FILE)");
            }
        }
        if (url == null || output == null) throw new UsageException("usage: webarchiver -url URL -output FILE");
        if (File.Exists(url)) url = new Uri(Path.GetFullPath(url)).AbsoluteUri;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var pageUri) || pageUri.Scheme is not ("http" or "https" or "file"))
        {
            Console.Error.WriteLine($"Error: invalid URL {url}");
            return 1;
        }
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/605.1.15 (KHTML, like Gecko) webarchiver/" + Version);
        Resource main;
        try { main = Fetch(http, pageUri); }
        catch (Exception e) when (e is HttpRequestException or IOException or TaskCanceledException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Error: could not load {url}: {e.Message}");
            return 1;
        }
        main.FrameName = "";
        var subresources = new List<Resource>();
        var seen = new HashSet<string>(StringComparer.Ordinal) { main.Url };
        var queue = new Queue<(Uri Url, bool Css)>();
        var text = Decode(main);
        foreach (var u in HtmlLinks(text, pageUri)) queue.Enqueue((u, false));
        if (main.MimeType == "text/css") foreach (var u in CssLinks(text, pageUri)) queue.Enqueue((u, true));
        while (queue.Count > 0 && subresources.Count < 500)
        {
            var (u, _) = queue.Dequeue();
            if (!seen.Add(u.AbsoluteUri)) continue;
            try
            {
                var r = Fetch(http, u);
                subresources.Add(r);
                // Stylesheets pull in fonts, images and other stylesheets.
                if (r.MimeType == "text/css")
                    foreach (var c in CssLinks(Decode(r), u)) queue.Enqueue((c, true));
            }
            catch (Exception e) when (e is HttpRequestException or IOException or TaskCanceledException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Warning: skipped {u}: {e.Message}");
            }
        }
        File.WriteAllBytes(output, Archive(main, subresources));
        return 0;
    }

    static Resource Fetch(HttpClient http, Uri uri)
    {
        if (uri.IsFile)
        {
            var path = uri.LocalPath;
            return new Resource { Url = uri.AbsoluteUri, Data = File.ReadAllBytes(path), MimeType = MimeFromName(path), Encoding = null };
        }
        using var response = http.GetAsync(uri).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        var data = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
        var type = response.Content.Headers.ContentType;
        return new Resource
        {
            Url = (response.RequestMessage?.RequestUri ?? uri).AbsoluteUri,
            Data = data,
            MimeType = type?.MediaType ?? MimeFromName(uri.AbsolutePath),
            Encoding = type?.CharSet?.Trim('"'),
        };
    }

    static string MimeFromName(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".html" or ".htm" or "" => "text/html",
        ".xhtml" => "application/xhtml+xml",
        ".css" => "text/css",
        ".js" or ".mjs" => "text/javascript",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".svg" => "image/svg+xml",
        ".webp" => "image/webp",
        ".ico" => "image/vnd.microsoft.icon",
        ".woff" => "font/woff",
        ".woff2" => "font/woff2",
        ".ttf" => "font/ttf",
        ".otf" => "font/otf",
        ".json" => "application/json",
        ".txt" => "text/plain",
        ".mp4" => "video/mp4",
        ".webm" => "video/webm",
        ".mp3" => "audio/mpeg",
        _ => "application/octet-stream",
    };

    // The page's text, in its declared encoding (HTTP charset, then <meta charset>), else UTF-8;
    // also records the encoding name WebKit stores for text resources.
    static string Decode(Resource r)
    {
        if (!r.MimeType.StartsWith("text/") && !r.MimeType.Contains("xml") && !r.MimeType.Contains("javascript")) return "";
        var enc = Encoding.UTF8;
        var name = r.Encoding;
        if (name == null && r.MimeType == "text/html")
        {
            var head = Encoding.ASCII.GetString(r.Data, 0, Math.Min(r.Data.Length, 2048));
            var m = Regex.Match(head, "<meta[^>]+charset=[\"']?([\\w-]+)", RegexOptions.IgnoreCase);
            if (m.Success) name = m.Groups[1].Value;
        }
        if (name != null)
        {
            try { enc = Encoding.GetEncoding(name); }
            catch (ArgumentException) { }
        }
        r.Encoding = name ?? "UTF-8";
        return enc.GetString(r.Data);
    }

    static readonly Regex HtmlAttr = new(
        "<(?:img|script|link|iframe|frame|source|embed|input|video|audio|track)\\b[^>]*?\\s(?:src|href|poster|data-src)\\s*=\\s*(?:\"([^\"]*)\"|'([^']*)'|([^\\s>]+))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex SrcSet = new("\\ssrcset\\s*=\\s*(?:\"([^\"]*)\"|'([^']*)')", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex LinkRel = new("<link\\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex CssUrl = new("url\\(\\s*(?:\"([^\"]*)\"|'([^']*)'|([^)\\s]*))\\s*\\)|@import\\s+(?:\"([^\"]*)\"|'([^']*)')", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static IEnumerable<Uri> HtmlLinks(string html, Uri page)
    {
        var baseUri = page;
        var b = Regex.Match(html, "<base\\b[^>]*href\\s*=\\s*[\"']([^\"']+)", RegexOptions.IgnoreCase);
        if (b.Success && Uri.TryCreate(page, b.Groups[1].Value, out var bu)) baseUri = bu;
        // <link> elements other than stylesheets and icons (canonical, alternate ...) aren't loaded.
        var skip = new HashSet<int>();
        foreach (Match l in LinkRel.Matches(html))
            if (!Regex.IsMatch(l.Value, "rel\\s*=\\s*[\"']?[^\"'>]*(stylesheet|icon|preload|apple-touch-icon)", RegexOptions.IgnoreCase))
                skip.Add(l.Index);
        foreach (Match m in HtmlAttr.Matches(html))
        {
            if (skip.Contains(m.Index)) continue;
            if (Resolve(baseUri, Group(m)) is { } u) yield return u;
        }
        foreach (Match m in SrcSet.Matches(html))
            foreach (var candidate in Group(m).Split(','))
                if (Resolve(baseUri, candidate.Trim().Split(' ')[0]) is { } u) yield return u;
        foreach (Match m in Regex.Matches(html, "<style[^>]*>(.*?)</style>", RegexOptions.IgnoreCase | RegexOptions.Singleline))
            foreach (var u in CssLinks(m.Groups[1].Value, baseUri)) yield return u;
        foreach (Match m in Regex.Matches(html, "style\\s*=\\s*\"([^\"]*url\\([^\"]*)\"", RegexOptions.IgnoreCase))
            foreach (var u in CssLinks(m.Groups[1].Value, baseUri)) yield return u;
    }

    static IEnumerable<Uri> CssLinks(string css, Uri sheet)
    {
        foreach (Match m in CssUrl.Matches(css))
            if (Resolve(sheet, Group(m)) is { } u) yield return u;
    }

    static string Group(Match m)
    {
        for (var i = 1; i < m.Groups.Count; i++) if (m.Groups[i].Success) return System.Net.WebUtility.HtmlDecode(m.Groups[i].Value.Trim());
        return "";
    }

    static Uri Resolve(Uri baseUri, string link)
    {
        if (string.IsNullOrWhiteSpace(link) || link.StartsWith('#') || link.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            || link.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) || link.StartsWith("about:", StringComparison.OrdinalIgnoreCase)
            || link.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)) return null;
        if (!Uri.TryCreate(baseUri, link, out var u) || u.Scheme is not ("http" or "https" or "file")) return null;
        // A web page can't pull local files: only file:// pages load file:// resources.
        if (u.IsFile && !baseUri.IsFile) return null;
        return new UriBuilder(u) { Fragment = "" }.Uri;
    }

    // ---- the archive ----

    static byte[] Archive(Resource main, List<Resource> subs)
    {
        OrderedDictionary<string, object> Dict(Resource r)
        {
            var d = new OrderedDictionary<string, object> { ["WebResourceData"] = r.Data };
            if (r.FrameName != null) d["WebResourceFrameName"] = r.FrameName;
            d["WebResourceMIMEType"] = r.MimeType;
            if (r.Encoding != null) d["WebResourceTextEncodingName"] = r.Encoding;
            d["WebResourceURL"] = r.Url;
            return d;
        }
        var root = new OrderedDictionary<string, object> { ["WebMainResource"] = Dict(main) };
        if (subs.Count > 0) root["WebSubresources"] = subs.Select(s => (object)Dict(s)).ToList();
        return BplistWriter.Write(root);
    }
}

// Writer for Apple's binary property list format (bplist00): dictionaries, arrays, strings, data,
// integers and booleans; objects are written once each, referenced through an offset table.
static class BplistWriter
{
    public static byte[] Write(object root)
    {
        var objects = new List<object>();
        Flatten(root, objects);
        var count = objects.Count;
        var refSize = count < 256 ? 1 : count < 65536 ? 2 : 4;
        var index = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);
        for (var i = 0; i < count; i++) index[objects[i]] = i;
        var ms = new MemoryStream();
        ms.Write("bplist00"u8);
        var offsets = new long[count];
        for (var i = 0; i < count; i++)
        {
            offsets[i] = ms.Position;
            switch (objects[i])
            {
                case OrderedDictionary<string, object> d:
                    Marker(ms, 0xd, d.Count);
                    foreach (var k in d.Keys) Ref(ms, index[KeyObject(objects, d, k)], refSize);
                    foreach (var v in d.Values) Ref(ms, index[v], refSize);
                    break;
                case List<object> a:
                    Marker(ms, 0xa, a.Count);
                    foreach (var v in a) Ref(ms, index[v], refSize);
                    break;
                case byte[] b:
                    Marker(ms, 0x4, b.Length);
                    ms.Write(b);
                    break;
                case BoxedKey key: WriteString(ms, key.Value); break;
                case string s: WriteString(ms, s); break;
                case bool f: ms.WriteByte(f ? (byte)0x09 : (byte)0x08); break;
                case long n:
                    ms.WriteByte(0x13);
                    var buf = new byte[8];
                    BinaryPrimitives.WriteInt64BigEndian(buf, n);
                    ms.Write(buf);
                    break;
                default: throw new InvalidOperationException($"cannot write {objects[i]?.GetType().Name} to a property list");
            }
        }
        var tableOffset = ms.Position;
        var offsetSize = tableOffset < 256 ? 1 : tableOffset < 65536 ? 2 : tableOffset < 1L << 32 ? 4 : 8;
        foreach (var o in offsets) Sized(ms, o, offsetSize);
        var trailer = new byte[32];
        trailer[6] = (byte)offsetSize;
        trailer[7] = (byte)refSize;
        BinaryPrimitives.WriteInt64BigEndian(trailer.AsSpan(8), count);
        BinaryPrimitives.WriteInt64BigEndian(trailer.AsSpan(16), 0);
        BinaryPrimitives.WriteInt64BigEndian(trailer.AsSpan(24), tableOffset);
        ms.Write(trailer);
        return ms.ToArray();
    }

    // Dictionary keys are string objects of their own; they are boxed so equal strings in different
    // places stay separate entries in the reference table.
    sealed class BoxedKey(string value) { public readonly string Value = value; }

    static readonly ConditionalWeakTable<OrderedDictionary<string, object>, Dictionary<string, BoxedKey>> keyBoxes = new();

    static object KeyObject(List<object> objects, OrderedDictionary<string, object> d, string key) => keyBoxes.GetOrCreateValue(d)[key];

    static void Flatten(object o, List<object> objects)
    {
        if (o is string s) o = s;   // strings are referenced by identity like everything else
        objects.Add(o);
        switch (o)
        {
            case OrderedDictionary<string, object> d:
                var boxes = keyBoxes.GetOrCreateValue(d);
                foreach (var k in d.Keys) { var box = new BoxedKey(k); boxes[k] = box; objects.Add(box); }
                foreach (var v in d.Values) Flatten(v, objects);
                break;
            case List<object> a:
                foreach (var v in a) Flatten(v, objects);
                break;
        }
    }

    static void WriteString(MemoryStream ms, string s)
    {
        if (s.All(c => c < 0x80))
        {
            Marker(ms, 0x5, s.Length);
            ms.Write(Encoding.ASCII.GetBytes(s));
        }
        else
        {
            Marker(ms, 0x6, s.Length);
            ms.Write(Encoding.BigEndianUnicode.GetBytes(s));
        }
    }

    static void Marker(MemoryStream ms, int kind, int length)
    {
        if (length < 15) { ms.WriteByte((byte)(kind << 4 | length)); return; }
        ms.WriteByte((byte)(kind << 4 | 0xf));
        // The length follows as an integer object.
        if (length < 256) { ms.WriteByte(0x10); ms.WriteByte((byte)length); }
        else if (length < 65536) { ms.WriteByte(0x11); Sized(ms, length, 2); }
        else { ms.WriteByte(0x12); Sized(ms, length, 4); }
    }

    static void Ref(MemoryStream ms, int i, int size) => Sized(ms, i, size);

    static void Sized(MemoryStream ms, long v, int size)
    {
        for (var b = size - 1; b >= 0; b--) ms.WriteByte((byte)(v >> (8 * b)));
    }
}
