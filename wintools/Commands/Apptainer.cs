using System.Diagnostics;
using System.Formats.Tar;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WinTools;

// apptainer / singularity / run-singularity: run SIF containers on Windows. Containers need a Linux
// kernel, which Windows provides through WSL 2: the image's squashfs root file system is converted to
// a tar and imported once as its own WSL distribution (apptainer-<image id>), then exec/run/shell
// start the image's /.singularity.d action scripts in it, from the current folder. docker:// images are
// pulled from the registry and flattened the same way. Images are read natively: `inspect`, `sif`
// (list/header/info/dump) and `cache` need no WSL. Building images needs Linux's mksquashfs and
// isn't offered.
static class Apptainer
{
    const string Version = "1.5.4";

    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("apptainer", "Run SIF and Docker containers in WSL 2; inspect SIF images", Run);
        Commands.Add("singularity", "Run SIF and Docker containers in WSL 2 (apptainer's old name)", Run);
        Commands.Add("run-singularity", "Run a SIF container's runscript (apptainer run)", args => Run(["run", .. args]));
    }

    const string Help = """
        Linux container platform optimized for High Performance Computing (HPC) and
        Enterprise Performance Computing (EPC)

        Usage:
          apptainer [global options...] <command>

        Available Commands:
          cache       Manage the local cache
          exec        Run a command within a container
          inspect     Show metadata for an image
          pull        Pull an image from a URI
          run         Run the user-defined default command within a container
          shell       Run a shell within a container
          sif         Manipulate Singularity Image Format (SIF) images
          version     Show the version for Apptainer
          build       (not available on Windows: needs Linux's mksquashfs)

        Containers run in WSL 2: each image is imported once as the WSL distribution
        apptainer-<id> (removed again by `apptainer cache clean`). Supported sources: local
        .sif files, https://, oras:// and docker:// (public images, linux/amd64, gzip layers).

        Exec/run/shell options:
          -e, --cleanenv        don't pass APPTAINERENV_* variables
              --env K=V         set an environment variable in the container
          -B, --bind src[:dst]  bind a Windows folder into the container
              --pwd DIR         initial working directory in the container
        """;

    static int Run(string[] args)
    {
        var i = 0;
        for (; i < args.Length && args[i].StartsWith('-'); i++)
        {
            switch (args[i])
            {
                case "-h" or "--help": Console.WriteLine(Help); return 0;
                case "--version": Console.WriteLine($"apptainer version {Version}"); return 0;
                case "-d" or "--debug" or "-q" or "--quiet" or "-s" or "--silent" or "-v" or "--verbose" or "--nocolor": break;
                default: throw new UsageException($"unknown flag: {args[i]}");
            }
        }
        if (i >= args.Length) { Console.WriteLine(Help); return 0; }
        var rest = args[(i + 1)..];
        return args[i] switch
        {
            "version" => Print($"{Version}"),
            "help" => Print(Help),
            "cache" => Cache(rest),
            "inspect" => Inspect(rest),
            "sif" => Sif(rest),
            "pull" => Pull(rest),
            "exec" or "run" or "shell" or "test" => Action(args[i], rest),
            "build" => Fail("apptainer build is not available on Windows: it needs Linux's mksquashfs and kernel (build in WSL or on Linux, then copy the .sif here)"),
            "instance" or "oci" or "overlay" or "plugin" or "remote" or "key" or "sign" or "verify" or "push" or "keyserver" or "registry" or "checkpoint" or "config" or "capability" =>
                Fail($"apptainer {args[i]} is not available on Windows; exec, run, shell, pull, inspect, sif and cache are"),
            _ => throw new UsageException($"unknown command \"{args[i]}\" for \"apptainer\""),
        };
    }

    static int Print(string text) { Console.WriteLine(text); return 0; }
    static int Fail(string text) { Console.Error.WriteLine("FATAL:   " + text); return 1; }

    // ---- cache ----

    static string CacheDir => Environment.GetEnvironmentVariable("APPTAINER_CACHEDIR") is { Length: > 0 } dir ? dir
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".apptainer", "cache");

    static int Cache(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            Console.WriteLine("Usage:\n  apptainer cache list [-v]\n  apptainer cache clean [-f] [-n|--dry-run]\nManage the local cache (pulled images, Docker layers and the WSL root file systems of imported images).");
            return 0;
        }
        static (int Count, long Bytes) Measure(string dir, string pattern = "*")
        {
            if (!Directory.Exists(dir)) return (0, 0);
            var files = Directory.EnumerateFiles(dir, pattern, SearchOption.AllDirectories).Select(f => new FileInfo(f)).ToList();
            return (files.Count, files.Sum(f => f.Length));
        }
        var images = new[] { "library", "oras", "net" }.Select(d => Measure(Path.Combine(CacheDir, d))).Aggregate((0, 0L), (a, b) => (a.Item1 + b.Count, a.Item2 + b.Bytes));
        var blobs = Measure(Path.Combine(CacheDir, "blob"));
        var wsl = Directory.Exists(Path.Combine(CacheDir, "wsl")) ? Directory.GetDirectories(Path.Combine(CacheDir, "wsl")) : [];
        var wslBytes = Measure(Path.Combine(CacheDir, "wsl")).Bytes;
        switch (args[0])
        {
            case "list":
                Console.WriteLine($"There are {images.Item1} container file(s) using {Size(images.Item2)} and {blobs.Count} oci blob file(s) using {Size(blobs.Bytes)} of space");
                if (wsl.Length > 0) Console.WriteLine($"There are {wsl.Length} WSL root file system(s) using {Size(wslBytes)} of space");
                Console.WriteLine($"Total space used: {Size(images.Item2 + blobs.Bytes + wslBytes)}");
                if (args.Contains("-v") || args.Contains("--verbose"))
                    foreach (var f in Directory.Exists(CacheDir) ? Directory.EnumerateFiles(CacheDir, "*", SearchOption.AllDirectories) : [])
                        Console.WriteLine($"  {Path.GetRelativePath(CacheDir, f),-60} {Size(new FileInfo(f).Length)}");
                return 0;
            case "clean":
                var dry = args.Contains("-n") || args.Contains("--dry-run");
                var force = args.Contains("-f") || args.Contains("--force");
                if (!dry && !force)
                {
                    Console.Write("This will delete everything in your cache (containers from all sources and OCI blobs).\nHopefully you know what you are doing!!!\nDo you want to continue? [y/N] ");
                    if (Console.ReadLine()?.Trim().ToLowerInvariant() is not ("y" or "yes")) return 0;
                }
                foreach (var dir in wsl)
                {
                    var name = "apptainer-" + Path.GetFileName(dir);
                    Console.WriteLine(dry ? $"Would unregister WSL distribution {name}" : $"Unregistering WSL distribution {name}");
                    if (!dry) RunWsl(["--unregister", name], capture: true);
                }
                if (!dry && Directory.Exists(CacheDir)) Directory.Delete(CacheDir, recursive: true);
                else if (dry) Console.WriteLine($"Would remove {CacheDir}");
                return 0;
            default: throw new UsageException($"unknown command \"{args[0]}\" for \"apptainer cache\"");
        }
    }

    static string Size(long bytes) => bytes switch
    {
        < 1L << 20 => (bytes / 1024.0).ToString("0.00", CultureInfo.InvariantCulture) + " KiB",
        < 1L << 30 => (bytes / 1048576.0).ToString("0.00", CultureInfo.InvariantCulture) + " MiB",
        _ => (bytes / 1073741824.0).ToString("0.00", CultureInfo.InvariantCulture) + " GiB",
    };

    // ---- sif ----

    static int Sif(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            Console.WriteLine("Usage:\n  apptainer sif list <image>\n  apptainer sif header <image>\n  apptainer sif info <id> <image>\n  apptainer sif dump <id> <image>");
            return 0;
        }
        switch (args[0])
        {
            case "list" when args.Length == 2:
            {
                var sif = ApptainerSif.Open(args[1]);
                var line = new string('-', 78);
                Console.WriteLine(line);
                Console.WriteLine("ID   |GROUP   |LINK    |SIF POSITION (start-end)  |TYPE");
                Console.WriteLine(line);
                foreach (var d in sif.Descriptors)
                {
                    var group = (d.GroupId & 0x0FFFFFFF) == 0 ? "NONE" : (d.GroupId & 0x0FFFFFFF).ToString();
                    var link = d.LinkedId == 0 ? "NONE" : (d.LinkedId & 0xF0000000) != 0 ? $"{d.LinkedId & 0x0FFFFFFF} (G)" : d.LinkedId.ToString();
                    Console.WriteLine($"{d.Id,-4} |{group,-7} |{link,-7} |{$"{d.Offset}-{d.Offset + d.Size}",-25} |{ApptainerSif.TypeName(d)}");
                }
                return 0;
            }
            case "header" when args.Length == 2:
            {
                var sif = ApptainerSif.Open(args[1]);
                Console.WriteLine($"Launch:            #!/usr/bin/env run-singularity");
                Console.WriteLine($"Version:           {sif.Version}");
                Console.WriteLine($"Primary Arch:      {sif.Arch}");
                Console.WriteLine($"ID:                {new Guid(sif.Id, bigEndian: true)}");
                Console.WriteLine($"Created on:        {DateTimeOffset.FromUnixTimeSeconds(sif.Created).UtcDateTime:yyyy-MM-dd HH:mm:ss} +0000 UTC");
                Console.WriteLine($"Modified on:       {DateTimeOffset.FromUnixTimeSeconds(sif.Modified).UtcDateTime:yyyy-MM-dd HH:mm:ss} +0000 UTC");
                Console.WriteLine($"Descriptors Free:  {sif.DescriptorsFree}");
                Console.WriteLine($"Descriptors Total: {sif.DescriptorsTotal}");
                Console.WriteLine($"Descriptors Offset:{sif.DescriptorsOffset}");
                Console.WriteLine($"Descriptors Size:  {sif.DescriptorsSize}");
                Console.WriteLine($"Data Offset:       {sif.DataOffset}");
                Console.WriteLine($"Data Size:         {sif.DataSize}");
                return 0;
            }
            case "info" or "dump" when args.Length == 3:
            {
                if (!uint.TryParse(args[1], out var id)) throw new UsageException($"invalid descriptor id \"{args[1]}\"");
                var sif = ApptainerSif.Open(args[2]);
                var d = sif.Descriptors.FirstOrDefault(x => x.Id == id) ?? throw new IOException($"no descriptor with id {id}");
                if (args[0] == "dump")
                {
                    using var stdout = Console.OpenStandardOutput();
                    stdout.Write(sif.Read(d));
                    return 0;
                }
                Console.WriteLine($"  Data type:    {ApptainerSif.TypeName(d)}");
                Console.WriteLine($"  ID:           {d.Id}");
                Console.WriteLine($"  Group ID:     {((d.GroupId & 0x0FFFFFFF) == 0 ? "NONE" : (d.GroupId & 0x0FFFFFFF).ToString())}");
                Console.WriteLine($"  Linked ID:    {(d.LinkedId == 0 ? "NONE" : (d.LinkedId & 0x0FFFFFFF).ToString())}");
                Console.WriteLine($"  Offset:       {d.Offset}");
                Console.WriteLine($"  Size:         {d.Size}");
                Console.WriteLine($"  Name:         {d.Name}");
                return 0;
            }
            default: throw new UsageException("usage: apptainer sif list|header <image>, apptainer sif info|dump <id> <image>");
        }
    }

    // ---- inspect ----

    static int Inspect(string[] args)
    {
        bool labels = false, deffile = false, runscript = false, environment = false, test = false, help = false, json = false, all = false;
        string image = null;
        foreach (var a in args)
            switch (a)
            {
                case "-h" or "--help": Console.WriteLine("Usage: apptainer inspect [-l] [-d] [-r] [-e] [-t] [-H] [-j] [--all] <image path>"); return 0;
                case "-l" or "--labels": labels = true; break;
                case "-d" or "--deffile": deffile = true; break;
                case "-r" or "--runscript": runscript = true; break;
                case "-e" or "--environment": environment = true; break;
                case "-t" or "--test": test = true; break;
                case "-H" or "--helpfile": help = true; break;
                case "-j" or "--json": json = true; break;
                case "--all": all = true; break;
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unknown flag: {a}");
                    image = image == null ? a : throw new UsageException("accepts 1 arg(s)");
                    break;
            }
        if (image == null) throw new UsageException("requires exactly 1 arg(s), only received 0");
        if (!(labels || deffile || runscript || environment || test || help)) labels = true;
        if (all) labels = deffile = runscript = environment = test = help = true;

        var sif = ApptainerSif.Open(image);
        var rootfs = sif.RootFs ?? throw new IOException($"{image}: no root file system partition");
        using var fs = new ApptainerSquash(image, rootfs.Offset);
        string Text(string path) => fs.ReadFile(path) is { } b ? Encoding.UTF8.GetString(b) : null;

        var labelPairs = new List<(string, string)>();
        if (labels && Text("/.singularity.d/labels.json") is { } labelJson)
        {
            using var doc = JsonDocument.Parse(labelJson);
            foreach (var p in doc.RootElement.EnumerateObject()) labelPairs.Add((p.Name, p.Value.ToString()));
        }
        var def = deffile ? Text("/.singularity.d/Singularity") ?? DefFromDescriptor(sif) : null;
        var run = runscript ? Text("/.singularity.d/runscript") : null;
        var tst = test ? Text("/.singularity.d/test") : null;
        var hlp = help ? Text("/.singularity.d/runscript.help") : null;
        var env = new List<(string Path, string Text)>();
        if (environment && fs.Lookup("/.singularity.d/env") is { IsDir: true } envDir)
            foreach (var (name, _) in fs.List(envDir).OrderBy(e => e.Name, StringComparer.Ordinal))
                if (Text("/.singularity.d/env/" + name) is { } t && name.EndsWith(".sh")) env.Add(("/.singularity.d/env/" + name, t));

        if (json)
        {
            using var stdout = Console.OpenStandardOutput();
            using var w = new Utf8JsonWriter(stdout, new JsonWriterOptions { Indented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            w.WriteStartObject();
            w.WriteStartObject("data");
            w.WriteStartObject("attributes");
            if (labels) { w.WriteStartObject("labels"); foreach (var (k, v) in labelPairs) w.WriteString(k, v); w.WriteEndObject(); }
            if (def != null) w.WriteString("deffile", def);
            if (run != null) w.WriteString("runscript", run);
            if (tst != null) w.WriteString("test", tst);
            if (hlp != null) w.WriteString("helpfile", hlp);
            if (environment) w.WriteString("environment", string.Concat(env.Select(e => e.Text)));
            w.WriteEndObject();
            w.WriteEndObject();
            w.WriteString("type", "container");
            w.WriteEndObject();
            w.Flush();
            Console.WriteLine();
            return 0;
        }
        foreach (var (k, v) in labelPairs) Console.WriteLine($"{k}: {v}");
        if (def != null) Console.WriteLine(def.TrimEnd());
        if (run != null) Console.WriteLine(run.TrimEnd());
        if (tst != null) Console.WriteLine(tst.TrimEnd());
        if (hlp != null) Console.WriteLine(hlp.TrimEnd());
        foreach (var (path, text) in env) Console.WriteLine($"=== {path} ===\n{text.TrimEnd()}\n");
        return 0;
    }

    static string DefFromDescriptor(ApptainerSif sif) =>
        sif.Descriptors.FirstOrDefault(d => d.DataType == 0x4001) is { } d ? Encoding.UTF8.GetString(sif.Read(d)) : null;

    // ---- pull ----

    static int Pull(string[] args)
    {
        bool force = false;
        string dir = null;
        var positional = new List<string>();
        for (var i = 0; i < args.Length; i++)
            switch (args[i])
            {
                case "-h" or "--help": Console.WriteLine("Usage: apptainer pull [-F] [--dir DIR] [output.sif] <https://...|oras://...|docker://...>"); return 0;
                case "-F" or "--force": force = true; break;
                case "--dir": dir = ++i < args.Length ? args[i] : throw new UsageException("--dir needs a folder"); break;
                default:
                    if (args[i].StartsWith('-')) throw new UsageException($"unknown flag: {args[i]}");
                    positional.Add(args[i]);
                    break;
            }
        if (positional.Count is 0 or > 2) throw new UsageException("requires 1 or 2 args: [output file] <URI>");
        var uri = positional[^1];
        var output = positional.Count == 2 ? positional[0] : DefaultName(uri);
        if (dir != null) output = Path.Combine(dir, output);
        if (File.Exists(output) && !force) return Fail($"Image file already exists: \"{output}\" - will not overwrite");
        if (uri.StartsWith("docker://"))
            return Fail("building a SIF from a Docker image needs Linux's mksquashfs; run it directly instead (apptainer exec/run/shell docker://...), which imports it into WSL");
        var cached = Fetch(uri);
        File.Copy(cached, output, overwrite: true);
        Console.Error.WriteLine("INFO:    Download complete: " + output);
        return 0;
    }

    static string DefaultName(string uri)
    {
        var rest = uri.Contains("://") ? uri[(uri.IndexOf("://") + 3)..] : uri;
        if (uri.StartsWith("http")) return Path.GetFileName(new Uri(uri).AbsolutePath) is { Length: > 0 } f ? f : "image.sif";
        var name = rest[(rest.LastIndexOf('/') + 1)..];
        if (!name.Contains(':') && !name.Contains('@')) name += ":latest";
        return name.Replace(':', '_').Replace('@', '_') + ".sif";
    }

    // A SIF source in the cache: https:// and oras:// are downloaded once.
    static string Fetch(string uri)
    {
        if (uri.StartsWith("library://"))
            throw new InvalidOperationException("library:// needs a Sylabs library remote, which has no default on Apptainer; use oras:// or https://");
        if (uri.StartsWith("shub://")) throw new InvalidOperationException("Singularity Hub (shub://) has been retired");
        var key = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(uri)));
        if (uri.StartsWith("https://") || uri.StartsWith("http://"))
        {
            var path = Path.Combine(CacheDir, "net", key);
            if (File.Exists(path)) return path;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            Console.Error.WriteLine($"INFO:    Downloading network image");
            using (var http = new HttpClient { Timeout = TimeSpan.FromMinutes(30) })
            using (var response = http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult())
            {
                if (!response.IsSuccessStatusCode) throw new IOException($"{uri}: {(int)response.StatusCode} {response.ReasonPhrase}");
                using var file = File.Create(path + ".part");
                response.Content.CopyToAsync(file).GetAwaiter().GetResult();
            }
            File.Move(path + ".part", path, overwrite: true);
            ApptainerSif.Open(path);   // fails clearly if it isn't a SIF
            return path;
        }
        if (uri.StartsWith("oras://"))
        {
            var path = Path.Combine(CacheDir, "oras", key);
            if (File.Exists(path)) return path;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            Console.Error.WriteLine($"INFO:    Downloading oras image");
            new ApptainerRegistry(uri).PullSif(path).GetAwaiter().GetResult();
            return path;
        }
        if (uri.Contains("://")) throw new InvalidOperationException($"unsupported transport type: {uri[..uri.IndexOf("://")]}");
        if (!File.Exists(uri)) throw new IOException($"could not open image {uri}: no such file");
        return uri;
    }

    // ---- exec / run / shell ----

    static int Action(string action, string[] args)
    {
        var env = new List<(string, string)>();
        var binds = new List<string>();
        string pwd = null;
        var clean = false;
        var i = 0;
        for (; i < args.Length && args[i].StartsWith('-'); i++)
        {
            string Value() => ++i < args.Length ? args[i] : throw new UsageException($"flag needs an argument: {args[i - 1]}");
            var a = args[i];
            var eq = a.IndexOf('=');
            if (a.StartsWith("--") && eq > 0) { args = [.. args[..i], a[..eq], a[(eq + 1)..], .. args[(i + 1)..]]; a = args[i]; }
            switch (a)
            {
                case "-h" or "--help":
                    Console.WriteLine($"Usage: apptainer {action} [options] <container> {(action == "exec" ? "<command> [args...]" : "[args...]")}\n(see apptainer --help for options)");
                    return 0;
                case "-e" or "--cleanenv": clean = true; break;
                case "--env":
                    foreach (var pair in Value().Split(','))
                        if (pair.IndexOf('=') is var p and > 0) env.Add((pair[..p], pair[(p + 1)..]));
                    break;
                case "-B" or "--bind" or "--mount": binds.AddRange(Value().Split(',')); break;
                case "--pwd": pwd = Value(); break;
                case "-c" or "--contain" or "-C" or "--containall" or "--writable-tmpfs" or "--fakeroot" or "-f" or "--nv" or "--no-home" or "-u" or "--userns": break;
                case "-H" or "--home" or "-W" or "--workdir" or "-o" or "--overlay" or "--app" or "--network" or "--security": Value(); break;
                default: throw new UsageException($"unknown flag: {a}");
            }
        }
        if (i >= args.Length) throw new UsageException($"requires at least 1 arg(s)");
        var image = args[i];
        var rest = args[(i + 1)..];
        if (action == "exec" && rest.Length == 0) throw new UsageException("requires at least 2 arg(s)");
        if (!clean)
            foreach (System.Collections.DictionaryEntry e in Environment.GetEnvironmentVariables())
            {
                var k = (string)e.Key;
                foreach (var prefix in new[] { "APPTAINERENV_", "SINGULARITYENV_" })
                    if (k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && k.Length > prefix.Length) env.Add((k[prefix.Length..], (string)e.Value));
            }

        if (!WslAvailable())
            return Fail("running containers on Windows needs WSL 2 (install it with: wsl --install --no-distribution)");
        var distro = Import(image);

        // In the container: binds, environment, working folder, then the image's action script.
        var script = new StringBuilder();
        foreach (var bind in binds)
        {
            var parts = bind.Split(':');
            // C:\data:/data splits at the drive colon too.
            string src = parts[0], dst = parts.Length > 1 ? parts[1] : null;
            if (parts.Length >= 2 && parts[0].Length == 1 && char.IsLetter(parts[0][0])) { src = parts[0] + ":" + parts[1]; dst = parts.Length > 2 ? parts[2] : null; }
            var linux = ToWslPath(Path.GetFullPath(src));
            dst ??= linux;
            script.Append($"mkdir -p {Quote(dst)} && mount --bind {Quote(linux)} {Quote(dst)} || echo \"WARNING: could not bind {src}\" >&2\n");
        }
        foreach (var (k, v) in env) script.Append($"export {k}={Quote(v)}\n");
        if (pwd != null) script.Append($"cd {Quote(pwd)} || exit 1\n");
        script.Append(action switch
        {
            "exec" => "[ -x /.singularity.d/actions/exec ] && exec /.singularity.d/actions/exec \"$@\"\nexec \"$@\"\n",
            "run" => "[ -x /.singularity.d/actions/run ] && exec /.singularity.d/actions/run \"$@\"\n[ -x /.singularity.d/runscript ] && exec /.singularity.d/runscript \"$@\"\nexec /bin/sh \"$@\"\n",
            "test" => "[ -x /.singularity.d/actions/test ] && exec /.singularity.d/actions/test \"$@\"\n[ -x /.singularity.d/test ] && exec /.singularity.d/test \"$@\"\necho 'No test script found in container, exiting'; exit 0\n",
            _ => "[ -x /.singularity.d/actions/shell ] && exec /.singularity.d/actions/shell \"$@\"\nexec /bin/sh \"$@\"\n",
        });
        return RunWsl(["-d", distro, "--cd", Environment.CurrentDirectory, "--exec", "/bin/sh", "-c", script.ToString(), "apptainer", .. rest], capture: false).Code;
    }

    static string Quote(string s) => "'" + s.Replace("'", "'\\''") + "'";

    static string ToWslPath(string windows) =>
        windows.Length >= 2 && windows[1] == ':' ? "/mnt/" + char.ToLowerInvariant(windows[0]) + windows[2..].Replace('\\', '/') : windows.Replace('\\', '/');

    // Imports the image as its own WSL distribution the first time; later runs reuse it.
    static string Import(string image)
    {
        var wslRoot = Path.Combine(CacheDir, "wsl");
        string id, tar = null;
        if (image.StartsWith("docker://"))
        {
            var registry = new ApptainerRegistry(image);
            id = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(image)))[..16];
            if (Registered("apptainer-" + id)) return "apptainer-" + id;
            Directory.CreateDirectory(wslRoot);
            tar = Path.Combine(wslRoot, id + ".tar");
            Console.Error.WriteLine($"INFO:    Converting OCI blobs to a WSL root file system");
            registry.WriteRootFs(tar, Path.Combine(CacheDir, "blob")).GetAwaiter().GetResult();
        }
        else
        {
            var path = Fetch(image);
            var sif = ApptainerSif.Open(path);
            id = sif.IdHex[..16];
            if (Registered("apptainer-" + id)) return "apptainer-" + id;
            var rootfs = sif.RootFs ?? throw new IOException($"{image}: no root file system partition");
            if (rootfs.FsType != 1) throw new IOException($"{image}: the root file system isn't squashfs (encrypted and ext3 images aren't supported)");
            Directory.CreateDirectory(wslRoot);
            tar = Path.Combine(wslRoot, id + ".tar");
            Console.Error.WriteLine($"INFO:    Converting SIF file to a WSL root file system");
            using var fs = new ApptainerSquash(path, rootfs.Offset);
            using var output = File.Create(tar);
            using var writer = new TarWriter(output, TarEntryFormat.Pax, leaveOpen: true);
            fs.WriteTar(writer);
        }
        var name = "apptainer-" + id;
        try
        {
            var (code, text) = RunWsl(["--import", name, Path.Combine(wslRoot, id), tar, "--version", "2"], capture: true);
            if (code != 0) throw new InvalidOperationException($"wsl --import failed: {text.Trim()}");
        }
        finally { File.Delete(tar); }
        return name;
    }

    static bool WslAvailable()
    {
        if (!File.Exists(Path.Combine(Environment.SystemDirectory, "wsl.exe"))) return false;
        try { return RunWsl(["--status"], capture: true).Code == 0; }
        catch (System.ComponentModel.Win32Exception) { return false; }
    }

    static bool Registered(string name) =>
        RunWsl(["--list", "--quiet"], capture: true).Output.Split('\n').Any(l => l.Trim().Trim('\0').Equals(name, StringComparison.OrdinalIgnoreCase));

    static (int Code, string Output) RunWsl(string[] args, bool capture)
    {
        var psi = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "wsl.exe")) { UseShellExecute = false };
        foreach (var a in args) psi.ArgumentList.Add(a);
        psi.Environment["WSL_UTF8"] = "1";
        if (capture) { psi.RedirectStandardOutput = true; psi.RedirectStandardError = true; psi.StandardOutputEncoding = Encoding.UTF8; }
        using var p = Process.Start(psi);
        var output = capture ? p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd() : "";
        p.WaitForExit();
        return (p.ExitCode, output);
    }
}
