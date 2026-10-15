using System.Net;
using System.Security.Cryptography;
using System.Text;
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
            var before = requests.Count;
            try
            {
                await downloader.FetchAsync("https://mirror.invalid/corrupt.zip", new string('0', 64), ["https://unused.invalid/payload.zip"]);
                throw new Exception("Checksum mismatch accepted");
            }
            catch (InvalidOperationException ex) when (ex.Message.StartsWith("SHA256 mismatch")) { }
            Check(requests.Count == before + 1, "checksum mismatch stops without trying another mirror");
            try
            {
                await downloader.FetchAsync("https://primary.invalid/missing.zip", sha, ["https://primary.invalid/also-missing.zip"]);
                throw new Exception("Failed mirrors accepted");
            }
            catch (HttpRequestException) { }
            Check(!Directory.EnumerateFiles(cache, "*.incomplete").Any(), "failed downloads leave no incomplete files");
            return 5;
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
