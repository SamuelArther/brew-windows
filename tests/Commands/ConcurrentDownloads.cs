using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Brew;

static class ConcurrentDownloads
{
    static readonly byte[] Payload = Encoding.UTF8.GetBytes("shared-cache-payload");
    static readonly string Sha = Convert.ToHexStringLower(SHA256.HashData(Payload));

    public static async Task Worker(string[] args)
    {
        using var client = new HttpClient(new Handler(args[1], args[2] == "crash"));
        await new Downloader(args[1], client).FetchAsync("https://example.invalid/shared.zip", Sha);
    }

    public static async Task<int> Run(string root)
    {
        var cache = Path.Combine(root, "concurrent-cache"); Directory.CreateDirectory(cache);
        var children = Enumerable.Range(0, 4).Select(_ => Start(cache, "normal")).ToArray();
        await Task.WhenAll(children.Select(p => p.WaitForExitAsync()));
        foreach (var p in children) { if (p.ExitCode != 0) throw new Exception("Concurrent download worker failed: " + await p.StandardError.ReadToEndAsync()); p.Dispose(); }
        if (File.ReadAllLines(Path.Combine(cache, "requests.txt")).Length != 1) throw new Exception("Concurrent workers downloaded more than once");
        var file = Path.Combine(cache, Sha + "--shared.zip");
        if (!File.ReadAllBytes(file).SequenceEqual(Payload)) throw new Exception("Shared cache was corrupted");
        File.Delete(file);
        using (var interrupted = Start(cache, "crash"))
        {
            var timer = Stopwatch.StartNew();
            while (!File.Exists(Path.Combine(cache, "partial-ready")))
            { if (timer.Elapsed > TimeSpan.FromSeconds(15)) throw new Exception("Partial download never started"); await Task.Delay(20); }
            interrupted.Kill(entireProcessTree: true); await interrupted.WaitForExitAsync();
        }
        using (var recovered = Start(cache, "normal"))
        { await recovered.WaitForExitAsync(); if (recovered.ExitCode != 0) throw new Exception("Interrupted download did not recover"); }
        if (!File.ReadAllBytes(file).SequenceEqual(Payload) || File.Exists(file + ".incomplete")) throw new Exception("Partial download recovery was not atomic");
        return 4;
    }

    static Process Start(string cache, string mode)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in new[] { "--download-worker", cache, mode }) start.ArgumentList.Add(arg);
        return Process.Start(start);
    }

    sealed class Handler(string cache, bool interrupt) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            File.AppendAllText(Path.Combine(cache, "requests.txt"), "request\n");
            if (interrupt)
            {
                File.WriteAllText(Path.Combine(cache, Sha + "--shared.zip.incomplete"), "partial");
                File.WriteAllText(Path.Combine(cache, "partial-ready"), "ready");
                await Task.Delay(TimeSpan.FromMinutes(1), token);
            }
            await Task.Delay(200, token);
            return new(HttpStatusCode.OK) { Content = new ByteArrayContent(Payload) };
        }
    }
}
