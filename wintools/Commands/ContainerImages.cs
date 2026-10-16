using System.Formats.Tar;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WinTools;

// container's image half: OCI/Docker registry pulls over HTTPS (anonymous or credential bearer tokens,
// manifest lists resolved to linux/<this machine's arch>), a content-addressed blob store under the
// container root, and layer flattening (whiteouts and opaque directories applied) into the single
// root file system tar that `wsl --import` takes.
static partial class Container
{
    // ---- store ----

    // %LOCALAPPDATA%\container, or $CONTAINER_ROOT (handy for tests and for keeping images elsewhere).
    internal static string Root =>
        Environment.GetEnvironmentVariable("CONTAINER_ROOT") is { Length: > 0 } root ? Path.GetFullPath(root)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "container");

    static string BlobPath(string digest) => Path.Combine(Root, "blobs", "sha256", Hex(digest));
    static string ImagesFile => Path.Combine(Root, "images.txt");

    static string Hex(string digest) =>
        digest.StartsWith("sha256:", StringComparison.Ordinal) && digest.Length == 71 && digest[7..].All(Uri.IsHexDigit)
            ? digest[7..] : throw new InvalidOperationException($"unsupported digest {digest}");

    internal sealed class ImageInfo
    {
        public string Reference, Digest, Config;
        public long Size;
        public string Name => Reference[..Reference.LastIndexOf(':')];
        public string Tag => Reference[(Reference.LastIndexOf(':') + 1)..];
    }

    // images.txt: one "<reference>\t<manifest digest>\t<config digest>\t<size>" line per tag.
    internal static List<ImageInfo> ListImages()
    {
        var list = new List<ImageInfo>();
        if (!File.Exists(ImagesFile)) return list;
        foreach (var line in File.ReadAllLines(ImagesFile))
        {
            var f = line.Split('\t');
            if (f.Length < 4) continue;
            list.Add(new ImageInfo { Reference = f[0], Digest = f[1], Config = f[2], Size = long.TryParse(f[3], out var s) ? s : 0 });
        }
        return list;
    }

    static void SaveImages(List<ImageInfo> images)
    {
        Directory.CreateDirectory(Root);
        File.WriteAllLines(ImagesFile, images.Select(i => $"{i.Reference}\t{i.Digest}\t{i.Config}\t{i.Size}"));
    }

    internal static ImageInfo FindImage(string reference)
    {
        var images = ListImages();
        if (reference.StartsWith("sha256:", StringComparison.Ordinal) || (reference.Length >= 6 && reference.All(Uri.IsHexDigit)))
        {
            var hex = reference.StartsWith("sha256:", StringComparison.Ordinal) ? reference[7..] : reference;
            var byDigest = images.FirstOrDefault(i => i.Digest[7..].StartsWith(hex, StringComparison.OrdinalIgnoreCase));
            if (byDigest != null) return byDigest;
        }
        var full = ImageRef.Parse(reference).ToString();
        return images.FirstOrDefault(i => i.Reference == full);
    }

    // ---- references ----

    // docker.io/library/alpine:latest from "alpine", the way Docker and Apple's container normalise names.
    internal sealed class ImageRef
    {
        public string Registry, Repository, Tag, Digest;

        public static ImageRef Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new UsageException("missing image reference");
            var r = new ImageRef();
            var at = text.IndexOf('@');
            if (at >= 0) { r.Digest = text[(at + 1)..]; text = text[..at]; }
            var slash = text.LastIndexOf('/');
            var colon = text.LastIndexOf(':');
            if (colon > slash) { r.Tag = text[(colon + 1)..]; text = text[..colon]; }
            var first = text.IndexOf('/');
            if (first > 0 && (text[..first].Contains('.') || text[..first].Contains(':') || text[..first] == "localhost"))
            {
                r.Registry = text[..first];
                r.Repository = text[(first + 1)..];
            }
            else
            {
                r.Registry = "docker.io";
                r.Repository = first < 0 ? "library/" + text : text;
            }
            if (r.Tag == null && r.Digest == null) r.Tag = "latest";
            if (r.Repository.Length == 0 || r.Repository.Any(c => char.IsUpper(c) || char.IsWhiteSpace(c)))
                throw new UsageException($"invalid image reference '{text}'");
            return r;
        }

        public string ApiHost => Registry == "docker.io" ? "registry-1.docker.io" : Registry;
        public string Name => $"{Registry}/{Repository}";
        public override string ToString() => $"{Name}:{Tag ?? "latest"}";
    }

    // ---- registry client ----

    static readonly HttpClient Http = CreateHttp();
    static readonly Dictionary<string, string> Tokens = [];

    static HttpClient CreateHttp()
    {
        var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true }) { Timeout = TimeSpan.FromMinutes(30) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"container-wintools/{Commands.Version}");
        return client;
    }

    const string ManifestTypes =
        "application/vnd.oci.image.index.v1+json, application/vnd.docker.distribution.manifest.list.v2+json, " +
        "application/vnd.oci.image.manifest.v1+json, application/vnd.docker.distribution.manifest.v2+json";

    internal static string Arch => RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "arm64" : "amd64";

    // GET with the registry's token dance: a 401 names a bearer realm, service and scope to fetch a token from.
    static HttpResponseMessage RegistryGet(ImageRef image, string path, string accept)
    {
        var url = $"https://{image.ApiHost}/v2/{image.Repository}/{path}";
        for (var attempt = 0; ; attempt++)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (accept != null) request.Headers.TryAddWithoutValidation("Accept", accept);
            if (Tokens.TryGetValue(image.Name, out var token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = Http.Send(request, HttpCompletionOption.ResponseHeadersRead);
            if (response.StatusCode != HttpStatusCode.Unauthorized || attempt > 0) return response;
            var challenge = response.Headers.WwwAuthenticate.FirstOrDefault(h => h.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase));
            response.Dispose();
            if (challenge?.Parameter == null) throw new InvalidOperationException($"{image.ApiHost}: authentication required");
            Tokens[image.Name] = FetchToken(image, challenge.Parameter);
        }
    }

    static string FetchToken(ImageRef image, string parameter)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(parameter, "(\\w+)=\"([^\"]*)\""))
            fields[m.Groups[1].Value] = m.Groups[2].Value;
        if (!fields.TryGetValue("realm", out var realm)) throw new InvalidOperationException($"{image.ApiHost}: no token realm");
        var query = new List<string>();
        if (fields.TryGetValue("service", out var service)) query.Add("service=" + Uri.EscapeDataString(service));
        query.Add("scope=" + Uri.EscapeDataString(fields.GetValueOrDefault("scope") ?? $"repository:{image.Repository}:pull"));
        var request = new HttpRequestMessage(HttpMethod.Get, realm + (realm.Contains('?') ? "&" : "?") + string.Join('&', query));
        if (Credentials(image.Registry) is { } basic) request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        using var response = Http.Send(request);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"{image.ApiHost}: token request failed ({(int)response.StatusCode} {response.ReasonPhrase})");
        using var doc = JsonDocument.Parse(response.Content.ReadAsStream());
        return doc.RootElement.TryGetProperty("token", out var t) ? t.GetString()
            : doc.RootElement.TryGetProperty("access_token", out var a) ? a.GetString()
            : throw new InvalidOperationException($"{image.ApiHost}: no token in reply");
    }

    static byte[] GetBytes(ImageRef image, string path, string accept, out string mediaType)
    {
        using var response = RegistryGet(image, path, accept);
        if (response.StatusCode == HttpStatusCode.NotFound) throw new InvalidOperationException($"{image}: not found");
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new InvalidOperationException($"{image}: access denied (private image? log in with `container registry login {image.Registry}`)");
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"{image}: registry returned {(int)response.StatusCode} {response.ReasonPhrase}");
        mediaType = response.Content.Headers.ContentType?.MediaType;
        return response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
    }

    // Pulls reference into the store and records the tag; returns the stored image.
    internal static ImageInfo Pull(string reference, string arch, bool quiet)
    {
        var image = ImageRef.Parse(reference);
        arch ??= Arch;
        if (!quiet) Console.Error.WriteLine($"Fetching {image}");
        var manifest = GetBytes(image, "manifests/" + (image.Digest ?? image.Tag), ManifestTypes, out var mediaType);
        var digest = image.Digest ?? Digest(manifest);
        using (var doc = JsonDocument.Parse(manifest))
        {
            var root = doc.RootElement;
            mediaType = root.TryGetProperty("mediaType", out var mt) ? mt.GetString() : mediaType;
            if (root.TryGetProperty("manifests", out var manifests))
            {
                string chosen = null;
                foreach (var m in manifests.EnumerateArray())
                {
                    if (!m.TryGetProperty("platform", out var p)) continue;
                    if (p.GetProperty("os").GetString() != "linux" || p.GetProperty("architecture").GetString() != arch) continue;
                    if (arch == "arm64" && p.TryGetProperty("variant", out var v) && v.GetString() is not ("v8" or null)) continue;
                    chosen = m.GetProperty("digest").GetString();
                    break;
                }
                if (chosen == null) throw new InvalidOperationException($"{image}: no linux/{arch} image in the manifest list");
                manifest = GetBytes(image, "manifests/" + chosen, ManifestTypes, out _);
                digest = chosen;
            }
        }
        if (Digest(manifest) != digest) throw new InvalidOperationException($"{image}: manifest digest mismatch");
        WriteBlob(digest, manifest);

        long size = 0;
        string config;
        using (var doc = JsonDocument.Parse(manifest))
        {
            var root = doc.RootElement;
            if (!root.TryGetProperty("layers", out var layers))
                throw new InvalidOperationException($"{image}: unsupported manifest ({mediaType ?? "schema 1"})");
            config = root.GetProperty("config").GetProperty("digest").GetString();
            FetchBlob(image, config, root.GetProperty("config").GetProperty("size").GetInt64(), quiet);
            foreach (var layer in layers.EnumerateArray())
            {
                var layerSize = layer.GetProperty("size").GetInt64();
                FetchBlob(image, layer.GetProperty("digest").GetString(), layerSize, quiet);
                size += layerSize;
            }
        }
        var info = new ImageInfo { Reference = image.Digest != null ? $"{image.Name}:{Hex(image.Digest)[..12]}" : image.ToString(), Digest = digest, Config = config, Size = size };
        var images = ListImages();
        images.RemoveAll(i => i.Reference == info.Reference);
        images.Add(info);
        SaveImages(images);
        return info;
    }

    static string Digest(byte[] data) => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(data));

    static void WriteBlob(string digest, byte[] data)
    {
        var path = BlobPath(digest);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, data);
    }

    // Streams a blob to the store, verifying its digest; blobs already there are skipped.
    static void FetchBlob(ImageRef image, string digest, long size, bool quiet)
    {
        var path = BlobPath(digest);
        if (File.Exists(path) && new FileInfo(path).Length == size) return;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using var response = RegistryGet(image, "blobs/" + digest, null);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"{image}: blob {digest[..19]} returned {(int)response.StatusCode} {response.ReasonPhrase}");
        var partial = path + ".partial";
        using (var source = response.Content.ReadAsStream())
        using (var target = File.Create(partial))
        using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
        {
            var buffer = new byte[1 << 16];
            long done = 0, lastShown = -1;
            int n;
            while ((n = source.Read(buffer)) > 0)
            {
                target.Write(buffer, 0, n);
                hash.AppendData(buffer, 0, n);
                done += n;
                if (!quiet && size > 1 << 20 && done * 20 / size != lastShown)
                {
                    lastShown = done * 20 / size;
                    Console.Error.Write($"\r  {digest[7..19]}  {FormatSize(done)} / {FormatSize(size)}");
                }
            }
            if (!quiet && size > 1 << 20) Console.Error.WriteLine();
            if ("sha256:" + Convert.ToHexStringLower(hash.GetHashAndReset()) != digest)
            {
                target.Close();
                File.Delete(partial);
                throw new InvalidOperationException($"{image}: blob {digest[..19]} failed verification");
            }
        }
        File.Move(partial, path, overwrite: true);
    }

    internal static string FormatSize(long bytes) =>
        bytes >= 1L << 30 ? $"{bytes / (double)(1L << 30):0.00} GB"
        : bytes >= 1 << 20 ? $"{bytes / (double)(1 << 20):0.0} MB"
        : bytes >= 1 << 10 ? $"{bytes / 1024.0:0.0} KB" : $"{bytes} B";

    // ---- image config ----

    internal sealed class ImageConfig
    {
        public List<string> Entrypoint = [], Cmd = [], Env = [], Ports = [];
        public string WorkingDir, User, Os = "linux", Architecture, Created;
        public List<string> Layers = [];
    }

    internal static ImageConfig ReadConfig(ImageInfo image)
    {
        var config = new ImageConfig();
        using (var doc = JsonDocument.Parse(File.ReadAllBytes(BlobPath(image.Config))))
        {
            var root = doc.RootElement;
            config.Os = Str(root, "os") ?? "linux";
            config.Architecture = Str(root, "architecture");
            config.Created = Str(root, "created");
            if (root.TryGetProperty("config", out var c) && c.ValueKind == JsonValueKind.Object)
            {
                config.Entrypoint = Strings(c, "Entrypoint");
                config.Cmd = Strings(c, "Cmd");
                config.Env = Strings(c, "Env");
                config.WorkingDir = Str(c, "WorkingDir");
                config.User = Str(c, "User");
                if (c.TryGetProperty("ExposedPorts", out var ports) && ports.ValueKind == JsonValueKind.Object)
                    config.Ports = ports.EnumerateObject().Select(p => p.Name).ToList();
            }
        }
        using (var doc = JsonDocument.Parse(File.ReadAllBytes(BlobPath(image.Digest))))
            foreach (var layer in doc.RootElement.GetProperty("layers").EnumerateArray())
                config.Layers.Add(layer.GetProperty("digest").GetString());
        return config;
    }

    static string Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    static List<string> Strings(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Array ? v.EnumerateArray().Select(x => x.GetString()).ToList() : [];

    // ---- flattening ----

    // Writes the union of the image's layers to writer: an entry survives unless a higher layer has the
    // same path, deletes it (.wh.<name>), makes a parent opaque (.wh..wh..opq) or replaces a parent with a
    // non-directory. Pass one walks the layers top-down to decide; pass two writes them bottom-up so hard
    // links always follow their targets. visit sees each surviving entry and may return false to drop it.
    internal static void Flatten(IList<string> layers, TarWriter writer, Func<string, TarEntry, bool> visit)
    {
        var keep = new List<bool>[layers.Count];
        var present = new HashSet<string>(StringComparer.Ordinal);
        var deleted = new HashSet<string>(StringComparer.Ordinal);
        var opaque = new HashSet<string>(StringComparer.Ordinal);
        var files = new HashSet<string>(StringComparer.Ordinal);
        for (var l = layers.Count - 1; l >= 0; l--)
        {
            keep[l] = [];
            var newPresent = new List<string>();
            var newDeleted = new List<string>();
            var newOpaque = new List<string>();
            var newFiles = new List<string>();
            foreach (var (name, entry) in ReadLayer(layers[l]))
            {
                var base_ = name[(name.LastIndexOf('/') + 1)..];
                var parent = name.Contains('/') ? name[..name.LastIndexOf('/')] : "";
                if (base_ == ".wh..wh..opq") { newOpaque.Add(parent); keep[l].Add(false); continue; }
                if (base_.StartsWith(".wh.", StringComparison.Ordinal))
                {
                    newDeleted.Add((parent.Length > 0 ? parent + "/" : "") + base_[4..]);
                    keep[l].Add(false);
                    continue;
                }
                var survives = !present.Contains(name) && !Hidden(name, deleted, opaque, files);
                keep[l].Add(survives);
                newPresent.Add(name);
                if (entry.EntryType != TarEntryType.Directory) newFiles.Add(name);
            }
            present.UnionWith(newPresent);
            deleted.UnionWith(newDeleted);
            opaque.UnionWith(newOpaque);
            files.UnionWith(newFiles);
        }
        for (var l = 0; l < layers.Count; l++)
        {
            var i = 0;
            foreach (var (name, entry) in ReadLayer(layers[l]))
            {
                if (keep[l][i++] && visit(name, entry))
                    writer.WriteEntry(entry.Format == TarEntryFormat.Pax ? entry : new PaxTarEntry(entry));
            }
        }
    }

    static bool Hidden(string path, HashSet<string> deleted, HashSet<string> opaque, HashSet<string> files)
    {
        if (deleted.Contains(path)) return true;
        for (var i = path.LastIndexOf('/'); i > 0; i = path.LastIndexOf('/', i - 1))
        {
            var ancestor = path[..i];
            if (deleted.Contains(ancestor) || opaque.Contains(ancestor) || files.Contains(ancestor)) return true;
        }
        return opaque.Contains("");
    }

    // Layer entries with normalised names ("./usr/bin/" -> "usr/bin"); gzip or plain tar.
    static IEnumerable<(string Name, TarEntry Entry)> ReadLayer(string digest)
    {
        using var file = File.OpenRead(BlobPath(digest));
        var magic = new byte[4];
        file.ReadExactly(magic);
        file.Position = 0;
        if (magic[0] == 0x28 && magic[1] == 0xB5 && magic[2] == 0x2F && magic[3] == 0xFD)
            throw new InvalidOperationException($"layer {digest[..19]} is zstd-compressed, which this build can't unpack");
        using Stream stream = magic[0] == 0x1F && magic[1] == 0x8B ? new GZipStream(file, CompressionMode.Decompress) : file;
        using var reader = new TarReader(stream);
        TarEntry entry;
        while ((entry = reader.GetNextEntry()) != null)
        {
            var name = NormalName(entry.Name);
            if (name.Length == 0) continue;
            yield return (name, entry);
        }
    }

    internal static string NormalName(string name)
    {
        name = name.Replace('\\', '/');
        while (name.StartsWith("./", StringComparison.Ordinal)) name = name[2..];
        return name.Trim('/') is "." ? "" : name.Trim('/');
    }

    // ---- credentials (Windows Credential Manager, target "container:<registry>") ----

    static string Credentials(string registry)
    {
        var target = "container:" + registry;
        if (CredRead(target, 1, 0, out var ptr))
        {
            try
            {
                var cred = Marshal.PtrToStructure<CREDENTIAL>(ptr);
                var secret = cred.CredentialBlobSize > 0 ? Marshal.PtrToStringUni(cred.CredentialBlob, (int)cred.CredentialBlobSize / 2) : "";
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(cred.UserName + ":" + secret));
            }
            finally { CredFree(ptr); }
        }
        // Docker's own login, when there is one (~/.docker/config.json "auths").
        var dockerConfig = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".docker", "config.json");
        if (!File.Exists(dockerConfig)) return null;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllBytes(dockerConfig));
            if (!doc.RootElement.TryGetProperty("auths", out var auths)) return null;
            foreach (var entry in auths.EnumerateObject())
            {
                var host = entry.Name.Replace("https://", "").Split('/')[0];
                if ((host == registry || (registry == "docker.io" && host == "index.docker.io")) && Str(entry.Value, "auth") is { Length: > 0 } auth)
                    return auth;
            }
        }
        catch (JsonException) { }
        return null;
    }

    static void StoreCredentials(string registry, string user, string password)
    {
        var blob = Marshal.StringToCoTaskMemUni(password);
        try
        {
            var cred = new CREDENTIAL
            {
                Type = 1, // CRED_TYPE_GENERIC
                TargetName = "container:" + registry,
                UserName = user,
                CredentialBlob = blob,
                CredentialBlobSize = (uint)password.Length * 2,
                Persist = 2, // CRED_PERSIST_LOCAL_MACHINE
            };
            if (!CredWrite(ref cred, 0)) throw new System.ComponentModel.Win32Exception();
        }
        finally { Marshal.ZeroFreeCoTaskMemUnicode(blob); }
    }

    static bool DeleteCredentials(string registry) => CredDelete("container:" + registry, 1, 0);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct CREDENTIAL
    {
        public uint Flags, Type;
        public string TargetName, Comment;
        public long LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist, AttributeCount;
        public IntPtr Attributes;
        public string TargetAlias, UserName;
    }

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CredReadW")]
    static extern bool CredRead(string target, uint type, uint flags, out IntPtr credential);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CredWriteW")]
    static extern bool CredWrite(ref CREDENTIAL credential, uint flags);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CredDeleteW")]
    static extern bool CredDelete(string target, uint type, uint flags);

    [DllImport("advapi32.dll")]
    static extern void CredFree(IntPtr buffer);
}
