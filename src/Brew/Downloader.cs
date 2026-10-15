using System.Security.Cryptography;

namespace Brew;

public sealed class Downloader(string cache)
{
    static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(15) };

    public async Task<string> FetchAsync(string url, string sha256)
    {
        if (sha256 == null || !System.Text.RegularExpressions.Regex.IsMatch(sha256, @"\A[0-9a-fA-F]{64}\z"))
            throw new InvalidOperationException("Formula must supply a SHA-256 checksum.");
        var uri = new Uri(url);
        if (uri.Scheme != "https") throw new InvalidOperationException("Formula URL must use HTTPS.");
        var name = Path.GetFileName(uri.LocalPath);
        if (string.IsNullOrEmpty(name)) throw new InvalidOperationException("Download URL has no filename.");
        Directory.CreateDirectory(cache);
        var destination = Path.Combine(cache, sha256.ToLowerInvariant() + "--" + name);
        if (File.Exists(destination))
        {
            await VerifyAsync(destination, sha256);
            Console.WriteLine($"Already downloaded: {destination}");
            return destination;
        }
        Console.WriteLine($"==> Downloading {url}");
        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".incomplete";
        try
        {
            using var response = await Client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                await response.Content.CopyToAsync(output);
            await VerifyAsync(temporary, sha256);
            File.Move(temporary, destination, overwrite: true);
            return destination;
        }
        finally { File.Delete(temporary); }
    }

    static async Task VerifyAsync(string file, string expected)
    {
        await using var input = File.OpenRead(file);
        var actual = Convert.ToHexStringLower(await SHA256.HashDataAsync(input));
        if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"SHA256 mismatch\nExpected: {expected}\n  Actual: {actual}\n    File: {file}");
    }
}
