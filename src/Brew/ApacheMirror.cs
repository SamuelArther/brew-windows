using System.Text.Json;

namespace Brew;

// Homebrew CurlApacheMirrorDownloadStrategy behavior (BSD-2-Clause; THIRD_PARTY_NOTICES.txt).
internal static class ApacheMirror
{
    public static bool IsPicker(Uri uri) => uri.Host is "www.apache.org" or "apache.org"
        && uri.AbsolutePath is "/dyn/closer.lua" or "/dyn/closer.cgi";

    public static string PathInfo(Uri uri)
    {
        foreach (var part in uri.Query.TrimStart('?').Split('&'))
        {
            var pair = part.Split('=', 2);
            if (pair.Length == 2 && pair[0] is "path" or "filename")
                return Uri.UnescapeDataString(pair[1].Replace("+", " "));
        }
        throw new InvalidOperationException("Apache mirror URL requires a path or filename parameter.");
    }

    public static async Task<(Uri Preferred, Uri[] Backups)> Resolve(HttpClient client, Uri uri)
    {
        try
        {
            var builder = new UriBuilder(uri) { Query = uri.Query.TrimStart('?') + "&asjson=1" };
            using var json = JsonDocument.Parse(await client.GetStringAsync(builder.Uri));
            var root = json.RootElement;
            var path = root.GetProperty("path_info").GetString();
            if (string.IsNullOrEmpty(path)) throw new JsonException("Missing Apache artifact path.");
            var attic = root.TryGetProperty("in_attic", out var archived) && archived.ValueKind == JsonValueKind.True;
            Uri Artifact(string prefix)
            {
                if (string.IsNullOrEmpty(prefix)) throw new JsonException("Missing Apache mirror URL.");
                var result = new Uri(prefix.TrimEnd('/') + "/" + path.TrimStart('/'));
                if (result.Scheme != "https") throw new JsonException("Apache mirror must use HTTPS.");
                return result;
            }
            var preferred = Artifact(attic ? "https://archive.apache.org/dist/" : root.GetProperty("preferred").GetString());
            var backups = !attic && root.TryGetProperty("backup", out var backup)
                ? backup.EnumerateArray().Select(v => v.GetString()).Where(v => v?.StartsWith("https://", StringComparison.OrdinalIgnoreCase) == true)
                    .Select(Artifact).ToArray() : [];
            return (preferred, backups);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or ArgumentException or InvalidOperationException)
        { throw new IOException("Couldn't determine mirror, try again later.", ex); }
    }
}
