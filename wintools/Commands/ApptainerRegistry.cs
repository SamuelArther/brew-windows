using System.Formats.Tar;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace WinTools;

// OCI registry client for apptainer's docker:// and oras:// sources: anonymous bearer tokens, image
// indexes (picks linux/amd64), manifests and blobs. docker:// images are flattened into one root
// file system tar (whiteouts applied) with the /.singularity.d scaffolding apptainer would add.
sealed class ApptainerRegistry
{
    static readonly HttpClient Http = new(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.None }) { Timeout = TimeSpan.FromMinutes(30) };

    readonly string registry, repository, reference;
    string token;

    public ApptainerRegistry(string uri)
    {
        // docker://alpine, docker://ghcr.io/org/img:tag, oras://ghcr.io/org/img:tag, img@sha256:...
        var rest = uri[(uri.IndexOf("://") + 3)..].TrimStart('/');
        var slash = rest.IndexOf('/');
        var first = slash < 0 ? "" : rest[..slash];
        if (slash > 0 && (first.Contains('.') || first.Contains(':') || first == "localhost")) { registry = first; rest = rest[(slash + 1)..]; }
        else registry = "docker.io";
        var at = rest.IndexOf('@');
        if (at >= 0) { reference = rest[(at + 1)..]; rest = rest[..at]; }
        else
        {
            var colon = rest.LastIndexOf(':');
            if (colon > 0 && !rest[colon..].Contains('/')) { reference = rest[(colon + 1)..]; rest = rest[..colon]; }
            else reference = "latest";
        }
        if (registry == "docker.io")
        {
            registry = "registry-1.docker.io";
            if (!rest.Contains('/')) rest = "library/" + rest;
        }
        repository = rest;
    }

    public string Repository => repository;
    public string Reference => reference;

    async Task<HttpResponseMessage> Get(string url, params string[] accept)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            foreach (var a in accept) request.Headers.Accept.ParseAdd(a);
            if (token != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 0 && response.Headers.WwwAuthenticate.FirstOrDefault() is { Scheme: "Bearer" } challenge)
            {
                await Authenticate(challenge.Parameter);
                continue;
            }
            if (!response.IsSuccessStatusCode) throw new IOException($"{url}: {(int)response.StatusCode} {response.ReasonPhrase}");
            return response;
        }
        throw new IOException($"{url}: authentication failed (only public images can be pulled)");
    }

    async Task Authenticate(string parameters)
    {
        var values = new Dictionary<string, string>();
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(parameters ?? "", "(\\w+)=\"([^\"]*)\""))
            values[m.Groups[1].Value] = m.Groups[2].Value;
        if (!values.TryGetValue("realm", out var realm)) throw new IOException("registry asked for an unknown kind of authentication");
        var query = new List<string>();
        if (values.TryGetValue("service", out var service)) query.Add("service=" + Uri.EscapeDataString(service));
        query.Add("scope=" + Uri.EscapeDataString(values.GetValueOrDefault("scope") ?? $"repository:{repository}:pull"));
        var json = await Http.GetStringAsync(realm + "?" + string.Join("&", query));
        using var doc = JsonDocument.Parse(json);
        token = doc.RootElement.TryGetProperty("token", out var t) ? t.GetString()
              : doc.RootElement.TryGetProperty("access_token", out var a) ? a.GetString() : null;
    }

    const string OciIndex = "application/vnd.oci.image.index.v1+json", DockerList = "application/vnd.docker.distribution.manifest.list.v2+json",
                 OciManifest = "application/vnd.oci.image.manifest.v1+json", DockerManifest = "application/vnd.docker.distribution.manifest.v2+json";

    // The image manifest for linux/amd64, with its digest.
    public async Task<(JsonDocument Manifest, string Digest)> Manifest()
    {
        var url = $"https://{registry}/v2/{repository}/manifests/{reference}";
        for (var hop = 0; hop < 2; hop++)
        {
            using var response = await Get(url, OciIndex, DockerList, OciManifest, DockerManifest);
            var digest = response.Headers.TryGetValues("Docker-Content-Digest", out var d) ? d.First() : null;
            var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (!doc.RootElement.TryGetProperty("manifests", out var manifests)) return (doc, digest ?? reference);
            string pick = null;
            foreach (var m in manifests.EnumerateArray())
                if (m.TryGetProperty("platform", out var p) && p.GetProperty("os").GetString() == "linux"
                    && p.GetProperty("architecture").GetString() == "amd64") { pick = m.GetProperty("digest").GetString(); break; }
            doc.Dispose();
            if (pick == null) throw new IOException($"{repository}:{reference} has no linux/amd64 image");
            url = $"https://{registry}/v2/{repository}/manifests/{pick}";
        }
        throw new IOException("nested image indexes aren't supported");
    }

    public async Task Blob(string digest, Stream output)
    {
        using var response = await Get($"https://{registry}/v2/{repository}/blobs/{digest}");
        await response.Content.CopyToAsync(output);
    }

    // oras://: the SIF file is the artifact's single layer.
    public async Task PullSif(string destination)
    {
        var (manifest, _) = await Manifest();
        using (manifest)
        {
            var layer = manifest.RootElement.GetProperty("layers").EnumerateArray()
                .FirstOrDefault(l => l.GetProperty("mediaType").GetString()?.Contains("sif") == true);
            if (layer.ValueKind == JsonValueKind.Undefined) throw new IOException($"{repository}:{reference} is not a SIF artifact");
            var temp = destination + ".part";
            await using (var file = File.Create(temp)) await Blob(layer.GetProperty("digest").GetString(), file);
            File.Move(temp, destination, overwrite: true);
        }
    }

    // docker://: layers -> one flattened root file system tar. Returns the manifest digest.
    public async Task<string> WriteRootFs(string tarPath, string blobCache)
    {
        var (manifest, digest) = await Manifest();
        using (manifest)
        {
            Directory.CreateDirectory(blobCache);
            var configDigest = manifest.RootElement.GetProperty("config").GetProperty("digest").GetString();
            var configFile = Path.Combine(blobCache, configDigest.Replace(':', '_'));
            if (!File.Exists(configFile)) await using (var f = File.Create(configFile)) await Blob(configDigest, f);
            var layers = new List<string>();
            foreach (var layer in manifest.RootElement.GetProperty("layers").EnumerateArray())
            {
                var layerDigest = layer.GetProperty("digest").GetString();
                var path = Path.Combine(blobCache, layerDigest.Replace(':', '_'));
                if (!File.Exists(path))
                {
                    Console.Error.WriteLine($"INFO:    Fetching blob {layerDigest}");
                    await using (var f = File.Create(path + ".part")) await Blob(layerDigest, f);
                    File.Move(path + ".part", path, overwrite: true);
                }
                layers.Add(path);
            }
            Flatten(layers, tarPath, File.ReadAllText(configFile));
        }
        return digest;
    }

    static Stream OpenLayer(string path)
    {
        var file = File.OpenRead(path);
        var magic = new byte[4];
        var n = file.Read(magic);
        file.Position = 0;
        if (n >= 2 && magic[0] == 0x1F && magic[1] == 0x8B) return new GZipStream(file, CompressionMode.Decompress);
        if (n == 4 && magic[0] == 0x28 && magic[1] == 0xB5 && magic[2] == 0x2F && magic[3] == 0xFD)
        {
            file.Dispose();
            throw new IOException("the image has zstd-compressed layers, which Windows can't decompress; use a gzip image");
        }
        return file;
    }

    static string Clean(string name)
    {
        name = name.Replace('\\', '/');
        while (name.StartsWith("./")) name = name[2..];
        return name.TrimStart('/').TrimEnd('/');
    }

    // Two passes: work out which layer's copy of every path survives (whiteouts delete, opaque
    // markers hide lower layers' children), then copy just those entries.
    static void Flatten(List<string> layers, string tarPath, string configJson)
    {
        var winner = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < layers.Count; i++)
        {
            using var stream = OpenLayer(layers[i]);
            using var reader = new TarReader(stream);
            while (reader.GetNextEntry(copyData: false) is { } entry)
            {
                var name = Clean(entry.Name);
                if (name.Length == 0) continue;
                var dir = name.Contains('/') ? name[..name.LastIndexOf('/')] : "";
                var file = name[(name.LastIndexOf('/') + 1)..];
                if (file == ".wh..wh..opq")
                {
                    var prefix = dir + "/";
                    foreach (var key in winner.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal) && winner[k] < i).ToList()) winner.Remove(key);
                }
                else if (file.StartsWith(".wh."))
                {
                    var gone = (dir.Length > 0 ? dir + "/" : "") + file[4..];
                    winner.Remove(gone);
                    foreach (var key in winner.Keys.Where(k => k.StartsWith(gone + "/", StringComparison.Ordinal)).ToList()) winner.Remove(key);
                }
                else winner[name] = i;
            }
        }

        using var output = File.Create(tarPath);
        using var tar = new TarWriter(output, TarEntryFormat.Pax, leaveOpen: true);
        var written = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < layers.Count; i++)
        {
            using var stream = OpenLayer(layers[i]);
            using var reader = new TarReader(stream);
            while (reader.GetNextEntry(copyData: false) is { } entry)
            {
                var name = Clean(entry.Name);
                if (name.Length == 0 || !winner.TryGetValue(name, out var layer) || layer != i) continue;
                if (entry.EntryType != TarEntryType.Directory && !written.Add(name)) continue;
                tar.WriteEntry(entry);
            }
        }
        foreach (var (name, content, mode) in Scaffolding(configJson))
        {
            var e = new PaxTarEntry(TarEntryType.RegularFile, name) { Mode = mode, DataStream = new MemoryStream(Encoding.UTF8.GetBytes(content)) };
            tar.WriteEntry(e);
        }
    }

    static string Quote(string s) => "'" + s.Replace("'", "'\\''") + "'";

    // What apptainer adds when it converts a Docker image: the image's ENV, a runscript for
    // ENTRYPOINT/CMD, and the action scripts.
    static IEnumerable<(string Name, string Content, UnixFileMode Mode)> Scaffolding(string configJson)
    {
        using var doc = JsonDocument.Parse(configJson);
        var config = doc.RootElement.TryGetProperty("config", out var c) ? c : default;
        List<string> Strings(string key) => config.ValueKind == JsonValueKind.Object && config.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Array
            ? v.EnumerateArray().Select(x => x.GetString()).ToList() : [];
        var env = new StringBuilder("#!/bin/sh\n");
        foreach (var e in Strings("Env"))
        {
            var eq = e.IndexOf('=');
            if (eq > 0) env.Append($"export {e[..eq]}=\"${{{e[..eq]}:-{e[(eq + 1)..].Replace("\"", "\\\"")}}}\"\n");
        }
        var entrypoint = string.Join(" ", Strings("Entrypoint").Select(Quote));
        var cmd = string.Join(" ", Strings("Cmd").Select(Quote));
        var workdir = config.ValueKind == JsonValueKind.Object && config.TryGetProperty("WorkingDir", out var w) ? w.GetString() : "";
        var run = new StringBuilder("#!/bin/sh\n");
        if (!string.IsNullOrEmpty(workdir)) run.Append($"cd {Quote(workdir)} 2>/dev/null\n");
        run.Append($"OCI_ENTRYPOINT={Quote(entrypoint)}\nOCI_CMD={Quote(cmd)}\n");
        run.Append("if [ $# -gt 0 ]; then eval \"set -- $OCI_ENTRYPOINT \\\"\\$@\\\"\"; else eval \"set -- $OCI_ENTRYPOINT $OCI_CMD\"; fi\n");
        run.Append("[ $# -eq 0 ] && set -- /bin/sh\nexec \"$@\"\n");
        const string source = "for script in /.singularity.d/env/*.sh; do [ -f \"$script\" ] && . \"$script\"; done\n";
        var rx = (UnixFileMode)0b111_101_101;
        yield return (".singularity.d/env/10-docker2singularity.sh", env.ToString(), rx);
        yield return (".singularity.d/runscript", run.ToString(), rx);
        yield return (".singularity.d/actions/exec", "#!/bin/sh\n" + source + "exec \"$@\"\n", rx);
        yield return (".singularity.d/actions/run", "#!/bin/sh\n" + source + "exec /.singularity.d/runscript \"$@\"\n", rx);
        yield return (".singularity.d/actions/shell", "#!/bin/sh\n" + source + "if [ -x /bin/bash ]; then exec /bin/bash --norc \"$@\"; fi\nexec /bin/sh \"$@\"\n", rx);
    }
}
