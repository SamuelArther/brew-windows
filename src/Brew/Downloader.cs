using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Brew;

public sealed class Downloader(string cache, HttpClient httpClient = null)
{
    sealed class ChecksumMismatch(string message) : InvalidOperationException(message);
    static readonly HttpClient Client = CreateClient();
    // Homebrew's Utils.safe_filename drops control characters and path separators from the unescaped
    // URL basename; Windows also refuses <>:"|?* (a ':' would even name an NTFS alternate data stream),
    // and silently drops trailing dots and spaces, so those go too.
    public static string FileName(string url)
    {
        var uri = new Uri(url);
        var name = Path.GetFileName(ApacheMirror.IsPicker(uri) ? ApacheMirror.PathInfo(uri) : uri.LocalPath);
        return SafeFileName(name);
    }

    public static string SafeFileName(string name) =>
        new string(name.Where(c => !char.IsControl(c) && Array.IndexOf(Path.GetInvalidFileNameChars(), c) < 0 && c is not (':' or '*' or '?')).ToArray())
            .TrimEnd('.', ' ');

    static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
        var backend = $".NET/{Environment.Version}";
        var curl = Path.Combine(Environment.SystemDirectory, "curl.exe");
        if (File.Exists(curl))
        {
            using var process = Process.Start(new ProcessStartInfo(curl, "--version")
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true });
            var output = process.StandardOutput.ReadToEnd(); process.WaitForExit();
            var version = Regex.Match(output, @"\Acurl ([\w.]+)");
            if (version.Success) backend = "curl/" + version.Groups[1].Value;
        }
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"Homebrew/7.0.9 (Windows; {RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant()}) {backend}");
        return client;
    }

    public async Task<string> FetchAsync(string url, string sha256, IEnumerable<string> mirrors = null)
    {
        if (sha256 == null || !System.Text.RegularExpressions.Regex.IsMatch(sha256, @"\A[0-9a-fA-F]{64}\z"))
            throw new InvalidOperationException("Formula must supply a SHA-256 checksum.");
        var uri = new Uri(url);
        if (uri.Scheme != "https") throw new InvalidOperationException("Formula URL must use HTTPS.");
        // Mirrors may be plain http (openssl@4's is): like Homebrew they're only fallbacks, and the SHA-256
        // check still decides what's accepted. Anything else (ftp:, file:) is left out.
        var urls = new[] { uri }.Concat((mirrors ?? []).Select(mirror => new Uri(mirror)).Where(m => m.Scheme is "https" or "http"))
            .Distinct().ToList();
        var name = FileName(url);
        if (string.IsNullOrEmpty(name)) throw new InvalidOperationException("Download URL has no filename.");
        Directory.CreateDirectory(cache);
        var destination = Path.Combine(cache, sha256.ToLowerInvariant() + "--" + name);
        using var downloadLock = await DownloadLock.Acquire(cache, Path.GetFileName(destination));
        var temporary = destination + ".incomplete";
        File.Delete(temporary); // A previous writer may have exited before publishing its download.
        if (File.Exists(destination))
        {
            try
            {
                await VerifyAsync(destination, sha256);
                Console.WriteLine($"Already downloaded: {destination}");
                return destination;
            }
            // Like Homebrew, a known-bad download is removed so it can't be reused; fetch it again.
            catch (ChecksumMismatch ex) { CommandOutput.Warning(ex.Message.Split('\n')[0] + $" for cached {destination}; downloading it again."); File.Delete(destination); }
        }
        for (var i = 0; i < urls.Count; i++)
        {
            Console.WriteLine($"==> Downloading {urls[i]}");
            try
            {
                var actual = urls[i];
                if (ApacheMirror.IsPicker(actual))
                {
                    var resolved = await ApacheMirror.Resolve(httpClient ?? Client, actual);
                    actual = resolved.Preferred;
                    foreach (var backup in resolved.Backups) if (!urls.Contains(backup)) urls.Add(backup);
                    Console.WriteLine($"==> Downloading from {actual}");
                }
                using var response = await GetFollowingRedirectsAsync(httpClient ?? Client, actual);
                response.EnsureSuccessStatusCode();
                await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    await response.Content.CopyToAsync(output);
                await VerifyAsync(temporary, sha256);
                File.Move(temporary, destination);
                return destination;
            }
            catch (Exception ex) when (i < urls.Count - 1 && ex is HttpRequestException or TaskCanceledException or IOException or ChecksumMismatch)
            {
                Console.WriteLine("Trying a mirror...");
            }
            finally { File.Delete(temporary); }
        }
        throw new InvalidOperationException("No download URL is available.");
    }

    // HttpClient won't follow an https -> http redirect (www.x.org sends every download to http://);
    // curl, and so Homebrew, does. The SHA-256 check still guards what arrives.
    static async Task<HttpResponseMessage> GetFollowingRedirectsAsync(HttpClient client, Uri uri)
    {
        for (var hops = 0; ; hops++)
        {
            var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            var status = (int)response.StatusCode;
            if (status is < 300 or > 399 || response.Headers.Location is not { } location || hops == 10) return response;
            uri = location.IsAbsoluteUri ? location : new Uri(uri, location);
            response.Dispose();
        }
    }

    static async Task VerifyAsync(string file, string expected)
    {
        await using var input = File.OpenRead(file);
        var actual = Convert.ToHexStringLower(await SHA256.HashDataAsync(input));
        if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
            throw new ChecksumMismatch($"SHA256 mismatch\nExpected: {expected}\n  Actual: {actual}\n    File: {file}");
    }
}
