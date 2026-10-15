using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Brew;

public sealed class Downloader(string cache, HttpClient httpClient = null)
{
    sealed class ChecksumMismatch(string message) : InvalidOperationException(message);
    static readonly HttpClient Client = CreateClient();
    public static string FileName(string url)
    {
        var uri = new Uri(url);
        return Path.GetFileName(ApacheMirror.IsPicker(uri) ? ApacheMirror.PathInfo(uri) : uri.LocalPath);
    }

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
        var urls = new[] { uri }.Concat((mirrors ?? []).Select(mirror => new Uri(mirror))).Distinct().ToList();
        if (urls.Any(candidate => candidate.Scheme != "https")) throw new InvalidOperationException("Formula URL must use HTTPS.");
        var name = FileName(url);
        if (string.IsNullOrEmpty(name)) throw new InvalidOperationException("Download URL has no filename.");
        Directory.CreateDirectory(cache);
        var destination = Path.Combine(cache, sha256.ToLowerInvariant() + "--" + name);
        if (File.Exists(destination))
        {
            await VerifyAsync(destination, sha256);
            Console.WriteLine($"Already downloaded: {destination}");
            return destination;
        }
        for (var i = 0; i < urls.Count; i++)
        {
            Console.WriteLine($"==> Downloading {urls[i]}");
            var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".incomplete";
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
                using var response = await (httpClient ?? Client).GetAsync(actual, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    await response.Content.CopyToAsync(output);
                await VerifyAsync(temporary, sha256);
                File.Move(temporary, destination, overwrite: true);
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

    static async Task VerifyAsync(string file, string expected)
    {
        await using var input = File.OpenRead(file);
        var actual = Convert.ToHexStringLower(await SHA256.HashDataAsync(input));
        if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
            throw new ChecksumMismatch($"SHA256 mismatch\nExpected: {expected}\n  Actual: {actual}\n    File: {file}");
    }
}
