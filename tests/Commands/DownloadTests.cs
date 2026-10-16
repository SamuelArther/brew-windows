using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.IO.Compression;
using Brew;

static class DownloadTests
{
    public static async Task<int> Run(string root)
    {
        var output = Console.Out; Console.SetOut(new StringWriter());
        try
        {
            var bytes = Encoding.UTF8.GetBytes("verified mirror payload");
            var sha = Convert.ToHexStringLower(SHA256.HashData(bytes));
            var requests = new List<string>();
            using var handler = new Handler(request =>
            {
                requests.Add(request.RequestUri.Host);
                return new(request.RequestUri.Host == "primary.invalid" ? HttpStatusCode.Forbidden : HttpStatusCode.OK)
                    { Content = new ByteArrayContent(bytes) };
            });
            using var client = new HttpClient(handler);
            var cache = Path.Combine(root, "mirror-cache");
            var downloader = new Downloader(cache, client);
            var file = await downloader.FetchAsync("https://primary.invalid/package.zip", sha, ["https://mirror.invalid/other.zip"]);
            Check(requests.SequenceEqual(new[] { "primary.invalid", "mirror.invalid" }), "mirror attempted after HTTP failure");
            Check(Path.GetFileName(file) == sha + "--package.zip" && File.ReadAllBytes(file).SequenceEqual(bytes), "mirror keeps canonical filename and verifies payload");
            await downloader.FetchAsync("https://primary.invalid/package.zip", sha, ["https://mirror.invalid/other.zip"]);
            Check(requests.Count == 2, "verified mirror cache reused");
            File.WriteAllText(file, "corrupted after download");
            var error = Console.Error; Console.SetError(new StringWriter());
            try { await downloader.FetchAsync("https://primary.invalid/package.zip", sha, ["https://mirror.invalid/other.zip"]); }
            finally { Console.SetError(error); }
            Check(requests.Count == 4 && File.ReadAllBytes(file).SequenceEqual(bytes), "corrupt cached download is fetched again");
            var before = requests.Count;
            try
            {
                await downloader.FetchAsync("https://mirror.invalid/corrupt.zip", new string('0', 64), ["https://unused.invalid/payload.zip"]);
                throw new Exception("Checksum mismatch accepted");
            }
            catch (InvalidOperationException ex) when (ex.Message.StartsWith("SHA256 mismatch")) { }
            Check(requests.Count == before + 2, "all mirrors are tried and rejected on checksum mismatch");
            try
            {
                await downloader.FetchAsync("https://primary.invalid/missing.zip", sha, ["https://primary.invalid/also-missing.zip"]);
                throw new Exception("Failed mirrors accepted");
            }
            catch (HttpRequestException) { }
            Check(!Directory.EnumerateFiles(cache, "*.incomplete").Any(), "failed downloads leave no incomplete files");
            var apacheRequests = new List<Uri>();
            using var apacheHandler = new Handler(request =>
            {
                var uri = request.RequestUri; apacheRequests.Add(uri);
                if (uri.Host == "www.apache.org")
                {
                    if (!uri.Query.Contains("asjson=1")) throw new Exception("Requested HTML mirror picker");
                    var attic = uri.Query.Contains("attic/");
                    var metadata = attic
                        ? "{\"in_attic\":true,\"path_info\":\"attic/package.zip\",\"preferred\":\"https://wrong.invalid/\"}"
                        : "{\"in_attic\":false,\"path_info\":\"project/package.zip\",\"preferred\":\"https://preferred.invalid/\",\"backup\":[\"https://backup.invalid/\"]}";
                    return new(HttpStatusCode.OK) { Content = new StringContent(metadata) };
                }
                return new(HttpStatusCode.OK) { Content = new ByteArrayContent(uri.Host == "preferred.invalid" ? Encoding.UTF8.GetBytes("HTML mirror error") : bytes) };
            });
            using var apacheClient = new HttpClient(apacheHandler);
            var apache = new Downloader(Path.Combine(root, "apache-cache"), apacheClient);
            const string picker = "https://www.apache.org/dyn/closer.lua?path=project/package.zip";
            var artifact = await apache.FetchAsync(picker, sha);
            Check(apacheRequests.Count == 3 && apacheRequests[1].Host == "preferred.invalid" && apacheRequests[2].Host == "backup.invalid", "Apache preferred checksum mismatch falls back to JSON backup");
            Check(Path.GetFileName(artifact) == sha + "--package.zip", "Apache cache uses artifact filename");
            var count = apacheRequests.Count;
            await apache.FetchAsync(picker, sha);
            Check(apacheRequests.Count == count, "Apache verified cache reused without querying mirrors");
            await new Downloader(Path.Combine(root, "attic-cache"), apacheClient).FetchAsync("https://www.apache.org/dyn/closer.cgi?filename=attic/package.zip", sha);
            Check(apacheRequests[^1].AbsoluteUri == "https://archive.apache.org/dist/attic/package.zip", "Apache attic resolves directly to archive");
            Check(Downloader.FileName("https://www.apache.org/dyn/closer.lua?path=project%2Fpackage.zip") == "package.zip", "Apache filename decodes path query");
            var zip = Path.Combine(root, "apache-package.zip");
            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
            using (var writer = new StreamWriter(archive.CreateEntry("package/payload.txt").Open())) writer.Write("extracted Apache artifact");
            var zipSha = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(zip)));
            var cachedZip = Path.Combine(root, zipSha + "--package.zip"); File.Copy(zip, cachedZip);
            var staged = await Archive.StageAsync(cachedZip, "closer.lua", Path.Combine(root, "apache-staged"));
            Check(File.ReadAllText(Path.Combine(staged, "payload.txt")) == "extracted Apache artifact", "closer URL stages actual ZIP contents instead of HTML filename");
            using var brokenMetadata = new Handler(request => new(HttpStatusCode.OK)
                { Content = request.RequestUri.Host == "www.apache.org" ? new StringContent("<html>unavailable</html>") : new ByteArrayContent(bytes) });
            using var brokenClient = new HttpClient(brokenMetadata);
            var fallback = await new Downloader(Path.Combine(root, "broken-apache"), brokenClient).FetchAsync(picker, sha, ["https://explicit.invalid/package.zip"]);
            Check(File.ReadAllBytes(fallback).SequenceEqual(bytes), "invalid Apache JSON falls back to explicit formula mirror");
            return 12;
        }
        finally { Console.SetOut(output); }
    }
    static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception("FAIL " + label);
    }
    sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => Task.FromResult(respond(request));
    }
}
