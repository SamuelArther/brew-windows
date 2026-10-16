using System.Diagnostics;
using System.Formats.Tar;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;

namespace WinTools;

// container: Apple's `container` CLI on WSL2, Windows' lightweight Linux utility VM. Images are pulled
// from OCI registries into a local store (ContainerImages.cs); a container is a WSL2 distribution
// imported from the image's flattened root file system ("container-<id>"), and its process is the
// image's entrypoint + command, run through wsl.exe with the image's environment, working directory
// and user. Published ports reach localhost through WSL2's port forwarding; volumes are bind mounts
// of Windows folders written into the container's /etc/fstab.
static partial class Container
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("container", "Create and run Linux containers in lightweight VMs (WSL2)", Run);
    }

    const string Usage = """
        OVERVIEW: A container platform for Windows (WSL2)

        USAGE: container [--debug] <subcommand>

        OPTIONS:
          --version               Show the version.
          -h, --help              Show help information.

        CONTAINER SUBCOMMANDS:
          create                  Create a new container
          delete, rm              Delete one or more containers
          exec                    Run a new command in a running container
          inspect                 Display information about one or more containers
          kill                    Kill one or more running containers
          list, ls                List containers
          logs                    Fetch container logs
          prune                   Remove all stopped containers
          run                     Run a container
          start                   Start a container
          stop                    Stop one or more running containers

        IMAGE SUBCOMMANDS:
          build                   Build an image from a Dockerfile
          image, i                Manage images (pull, list, inspect, delete, tag, save, load, prune)
          registry, r             Manage registry logins

        VOLUME SUBCOMMANDS:
          volume, v               Manage named volumes (create, list, inspect, delete)

        SYSTEM SUBCOMMANDS:
          system, s               Manage the container system (start, stop, status)
        """;

    internal static int Run(string[] args)
    {
        args = args.Where(a => a != "--debug").ToArray();
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help") { Console.WriteLine(Usage); return args.Length == 0 ? 1 : 0; }
        var rest = args[1..];
        return args[0] switch
        {
            "--version" or "-v" => Version(),
            "run" => RunCommand(rest),
            "create" => CreateCommand(rest),
            "start" => StartCommand(rest),
            "stop" => StopCommand(rest),
            "kill" => KillCommand(rest),
            "delete" or "rm" => DeleteCommand(rest),
            "list" or "ls" => ListCommand(rest),
            "exec" => ExecCommand(rest),
            "logs" => LogsCommand(rest),
            "inspect" => InspectCommand(rest),
            "prune" => PruneCommand(rest),
            "image" or "i" or "images" => ImageCommand(args[0] == "images" ? ["ls", .. rest] : rest),
            "pull" => ImageCommand(["pull", .. rest]),
            "registry" or "r" => RegistryCommand(rest),
            "volume" or "v" => VolumeCommand(rest),
            "system" or "s" => SystemCommand(rest),
            "build" or "builder" => Fail($"container {args[0]}: building images needs BuildKit, which wintools doesn't carry; build with `docker build` or `buildah` (inside a container) and `container image load` the OCI archive"),
            "network" or "n" => Fail("container network: all WSL2 containers share the WSL virtual network; their ports are forwarded to localhost"),
            _ => throw new UsageException($"unknown subcommand '{args[0]}' (see container --help)"),
        };
    }

    static int Version()
    {
        Console.WriteLine($"container CLI version {Commands.Version} (wintools, WSL2)");
        return 0;
    }

    static int Fail(string message) { Console.Error.WriteLine("Error: " + message); return 1; }

    static bool Help(string[] args, string text)
    {
        if (!args.Any(a => a is "-h" or "--help")) return false;
        Console.WriteLine(text);
        return true;
    }

    // ---- container records ----

    internal sealed class Box
    {
        public string Id, Image, ImageDigest, Arch, User, WorkDir, Created;
        public List<string> Command = [], Env = [], Ports = [], Volumes = [], Labels = [];
        public bool AutoRemove, Shell, Interactive;
        public int Pid;
        public long PidStart;
        public string Dir => Path.Combine(Root, "containers", Id);
        public string Distro => "container-" + Id;
        public string LogFile => Path.Combine(Dir, "stdio.log");
    }

    internal static List<Box> ListBoxes()
    {
        var dir = Path.Combine(Root, "containers");
        if (!Directory.Exists(dir)) return [];
        var list = new List<Box>();
        foreach (var d in Directory.GetDirectories(dir))
        {
            var file = Path.Combine(d, "config.json");
            if (!File.Exists(file)) continue;
            try { list.Add(LoadBox(file)); }
            catch (JsonException) { }
        }
        return list.OrderBy(b => b.Created, StringComparer.Ordinal).ToList();
    }

    internal static Box FindBox(string id)
    {
        var boxes = ListBoxes();
        return boxes.FirstOrDefault(b => b.Id == id)
               ?? (boxes.Count(b => b.Id.StartsWith(id, StringComparison.Ordinal)) == 1 ? boxes.First(b => b.Id.StartsWith(id, StringComparison.Ordinal)) : null)
               ?? throw new InvalidOperationException($"container {id} not found");
    }

    static Box LoadBox(string file)
    {
        using var doc = JsonDocument.Parse(File.ReadAllBytes(file));
        var r = doc.RootElement;
        return new Box
        {
            Id = Str(r, "id"), Image = Str(r, "image"), ImageDigest = Str(r, "imageDigest"), Arch = Str(r, "arch"),
            User = Str(r, "user"), WorkDir = Str(r, "workingDirectory"), Created = Str(r, "created"),
            Command = Strings(r, "command"), Env = Strings(r, "env"), Ports = Strings(r, "publishedPorts"),
            Volumes = Strings(r, "volumes"), Labels = Strings(r, "labels"),
            AutoRemove = r.TryGetProperty("autoRemove", out var a) && a.ValueKind == JsonValueKind.True,
            Shell = r.TryGetProperty("shell", out var s) && s.ValueKind == JsonValueKind.True,
            Interactive = r.TryGetProperty("interactive", out var t) && t.ValueKind == JsonValueKind.True,
            Pid = r.TryGetProperty("pid", out var p) ? p.GetInt32() : 0,
            PidStart = r.TryGetProperty("pidStart", out var ps) ? ps.GetInt64() : 0,
        };
    }

    static void SaveBox(Box box)
    {
        Directory.CreateDirectory(box.Dir);
        using var stream = File.Create(Path.Combine(box.Dir, "config.json"));
        using var w = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        WriteBox(w, box);
    }

    static void WriteBox(Utf8JsonWriter w, Box box)
    {
        w.WriteStartObject();
        w.WriteString("id", box.Id);
        w.WriteString("image", box.Image);
        w.WriteString("imageDigest", box.ImageDigest);
        w.WriteString("os", "linux");
        w.WriteString("arch", box.Arch);
        w.WriteString("created", box.Created);
        w.WriteString("wslDistribution", box.Distro);
        WriteList(w, "command", box.Command);
        WriteList(w, "env", box.Env);
        w.WriteString("workingDirectory", box.WorkDir);
        if (box.User != null) w.WriteString("user", box.User);
        WriteList(w, "publishedPorts", box.Ports);
        WriteList(w, "volumes", box.Volumes);
        WriteList(w, "labels", box.Labels);
        w.WriteBoolean("autoRemove", box.AutoRemove);
        w.WriteBoolean("interactive", box.Interactive);
        w.WriteBoolean("shell", box.Shell);
        w.WriteNumber("pid", box.Pid);
        w.WriteNumber("pidStart", box.PidStart);
        w.WriteEndObject();
    }

    static void WriteList(Utf8JsonWriter w, string name, List<string> items)
    {
        w.WriteStartArray(name);
        foreach (var item in items) w.WriteStringValue(item);
        w.WriteEndArray();
    }

    // Running = the process that ran its entrypoint (wsl.exe) is still alive.
    internal static bool IsRunning(Box box)
    {
        if (box.Pid == 0) return false;
        try
        {
            using var p = Process.GetProcessById(box.Pid);
            return !p.HasExited && p.StartTime.ToUniversalTime().Ticks == box.PidStart;
        }
        catch (ArgumentException) { return false; }
        catch (InvalidOperationException) { return false; }
        catch (System.ComponentModel.Win32Exception) { return false; }
    }

    internal static string State(Box box) => IsRunning(box) ? "running" : "stopped";

    // ---- options shared by run and create ----

    sealed class RunOptions
    {
        public string Name, Image, Entrypoint, WorkDir, User, Arch;
        public bool Detach, Interactive, Tty, Remove, Quiet;
        public List<string> Env = [], Ports = [], Volumes = [], Labels = [], Args = [];
    }

    const string RunUsage = """
        OVERVIEW: Run a container

        USAGE: container run [<options>] <image> [<arguments> ...]

        OPTIONS:
          -d, --detach            Run the container and detach from the process
          -i, --interactive       Keep standard input open
          -t, --tty               Open a TTY with the process
          --name <name>           Use the specified name as the container ID
          --rm, --remove          Remove the container after it stops
          -e, --env <env>         Set environment variables (KEY=VALUE, or KEY to copy it from Windows)
          --env-file <file>       Read in a file of environment variables
          -w, --cwd, --workdir <dir>  Initial working directory inside the container
          -u, --user <user>       Set the user for the process
          --entrypoint <cmd>      Override the entrypoint of the image
          -p, --publish <spec>    Publish a port ([host-ip:]host-port:container-port[/protocol])
          -v, --volume <spec>     Bind mount a Windows folder or named volume (source:target[:ro])
          -l, --label <label>     Add a key=value label to the container
          -a, --arch <arch>       Set arch if image can target multiple architectures (amd64, arm64)
          --platform <platform>   Platform for the image (linux/amd64, linux/arm64)
          -c, --cpus, -m, --memory  Accepted; WSL2 shares one VM, sized in %UserProfile%\.wslconfig
          -h, --help              Show help information.
        """;

    static RunOptions ParseRun(string[] args)
    {
        var o = new RunOptions();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string value = null;
            if (arg.StartsWith("--", StringComparison.Ordinal) && arg.Contains('='))
            {
                value = arg[(arg.IndexOf('=') + 1)..];
                arg = arg[..arg.IndexOf('=')];
            }
            string Next() => value ?? (i + 1 < args.Length ? args[++i] : throw new UsageException($"{arg} takes an argument"));
            if (o.Image != null) { o.Args.Add(args[i]); continue; }
            switch (arg)
            {
                case "-d" or "--detach": o.Detach = true; break;
                case "-i" or "--interactive": o.Interactive = true; break;
                case "-t" or "--tty": o.Tty = true; break;
                case "-it" or "-ti": o.Interactive = o.Tty = true; break;
                case "--rm" or "--remove": o.Remove = true; break;
                case "-q" or "--quiet": o.Quiet = true; break;
                case "--name": o.Name = Next(); break;
                case "-e" or "--env": o.Env.Add(EnvValue(Next())); break;
                case "--env-file": o.Env.AddRange(ReadEnvFile(Next())); break;
                case "-w" or "--cwd" or "--workdir": o.WorkDir = Next(); break;
                case "-u" or "--user" or "--uid": o.User = Next(); break;
                case "--entrypoint": o.Entrypoint = Next(); break;
                case "-p" or "--publish": o.Ports.Add(Next()); break;
                case "-v" or "--volume": o.Volumes.Add(Next()); break;
                case "--mount": o.Volumes.Add(MountToVolume(Next())); break;
                case "-l" or "--label": o.Labels.Add(Next()); break;
                case "-a" or "--arch": o.Arch = Next(); break;
                case "--platform": o.Arch = Next().Split('/').ElementAtOrDefault(1) ?? throw new UsageException("--platform takes os/arch"); break;
                case "--os": if (Next() != "linux") throw new UsageException("only linux containers are supported"); break;
                case "-c" or "--cpus" or "-m" or "--memory" or "--dns" or "--dns-search" or "--dns-option" or "--network"
                    or "--kernel" or "--gid" or "--cidfile" or "--scheme" or "-k": Next(); break;
                case "--ssh" or "--rosetta" or "--virtualization" or "--no-dns" or "--disable-progress-updates" or "--read-only": break;
                default:
                    if (arg.StartsWith('-') && arg.Length > 1) throw new UsageException($"unknown option '{arg}'");
                    o.Image = args[i];
                    break;
            }
        }
        if (o.Image == null) throw new UsageException("missing expected argument '<image>'");
        if (o.Name != null && !System.Text.RegularExpressions.Regex.IsMatch(o.Name, "^[a-zA-Z0-9][a-zA-Z0-9_.-]{0,62}$"))
            throw new UsageException($"invalid container name '{o.Name}'");
        if (o.Arch is not (null or "amd64" or "arm64"))
            o.Arch = o.Arch switch { "x86_64" or "x64" => "amd64", "aarch64" => "arm64", _ => throw new UsageException($"unsupported arch '{o.Arch}'") };
        return o;
    }

    static string EnvValue(string spec) =>
        spec.Contains('=') ? spec : spec + "=" + (Environment.GetEnvironmentVariable(spec) ?? "");

    static IEnumerable<string> ReadEnvFile(string file) =>
        File.ReadAllLines(file).Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith('#')).Select(EnvValue);

    // --mount type=bind,source=X,target=Y[,readonly] -> X:Y[:ro]
    static string MountToVolume(string spec)
    {
        string source = null, target = null;
        var ro = false;
        foreach (var part in spec.Split(','))
        {
            var kv = part.Split('=', 2);
            switch (kv[0])
            {
                case "source" or "src": source = kv.ElementAtOrDefault(1); break;
                case "target" or "dst" or "destination": target = kv.ElementAtOrDefault(1); break;
                case "readonly" or "ro": ro = kv.Length == 1 || kv[1] == "true"; break;
            }
        }
        if (source == null || target == null) throw new UsageException($"--mount needs source= and target= ('{spec}')");
        return $"{source}:{target}" + (ro ? ":ro" : "");
    }

    // ---- run / create ----

    static int RunCommand(string[] args)
    {
        if (Help(args, RunUsage)) return 0;
        var o = ParseRun(args);
        var box = Create(o);
        if (o.Detach)
        {
            StartDetached(box);
            Console.WriteLine(box.Id);
            return 0;
        }
        try { return Attach(box, o.Interactive || o.Tty); }
        finally
        {
            if (box.AutoRemove) Delete(box, force: true);
        }
    }

    static int CreateCommand(string[] args)
    {
        if (Help(args, RunUsage.Replace("Run a container", "Create a new container").Replace("container run", "container create"))) return 0;
        var box = Create(ParseRun(args));
        Console.WriteLine(box.Id);
        return 0;
    }

    static Box Create(RunOptions o)
    {
        RequireWsl();
        var image = FindImage(o.Image) ?? Pull(o.Image, o.Arch, o.Quiet);
        var config = ReadConfig(image);
        var id = o.Name ?? Guid.NewGuid().ToString();
        if (ListBoxes().Any(b => b.Id == id)) throw new InvalidOperationException($"container with id {id} already exists");

        var command = new List<string>();
        if (o.Entrypoint != null) command.AddRange(o.Entrypoint.Length > 0 ? [o.Entrypoint] : []);
        else command.AddRange(config.Entrypoint);
        command.AddRange(o.Args.Count > 0 || o.Entrypoint != null ? o.Args : config.Cmd);
        if (command.Count == 0) throw new InvalidOperationException($"{image.Reference} has no command; give one after the image name");

        var env = new List<string>(config.Env);
        foreach (var e in o.Env)
        {
            var key = e[..e.IndexOf('=')];
            env.RemoveAll(x => x.StartsWith(key + "=", StringComparison.Ordinal));
            env.Add(e);
        }
        var box = new Box
        {
            Id = id, Image = image.Reference, ImageDigest = image.Digest, Arch = config.Architecture ?? Arch,
            Created = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"), Command = command, Env = env,
            WorkDir = o.WorkDir ?? (string.IsNullOrEmpty(config.WorkingDir) ? "/" : config.WorkingDir),
            User = o.User ?? (string.IsNullOrEmpty(config.User) ? null : config.User),
            Ports = o.Ports, Labels = o.Labels, AutoRemove = o.Remove, Interactive = o.Interactive || o.Tty,
            Volumes = o.Volumes.Select(ResolveVolume).ToList(),
        };
        foreach (var port in box.Ports) ParsePort(port);

        Directory.CreateDirectory(box.Dir);
        var rootfs = Path.Combine(box.Dir, "rootfs.tar");
        try
        {
            if (!o.Quiet) Console.Error.WriteLine($"Unpacking {image.Reference}");
            string passwd = null, fstab = null;
            using (var file = File.Create(rootfs))
            using (var writer = new TarWriter(file, TarEntryFormat.Pax))
            {
                Flatten(config.Layers, writer, (name, entry) =>
                {
                    if (name is "bin/sh" or "usr/bin/sh") box.Shell = true;
                    if (name == "etc/wsl.conf") return false;
                    if (name is "etc/fstab" or "etc/passwd" && entry.DataStream != null)
                    {
                        var text = new StreamReader(entry.DataStream).ReadToEnd();
                        if (name == "etc/fstab") { fstab = text; return false; }
                        passwd = text;
                        entry.DataStream = new MemoryStream(Encoding.UTF8.GetBytes(text));
                    }
                    return true;
                });
                AddFile(writer, "etc/wsl.conf", WslConf(box));
                var mounts = new StringBuilder(fstab ?? "");
                foreach (var v in box.Volumes)
                {
                    var (source, target, ro) = SplitVolume(v);
                    AddDirectory(writer, target.TrimStart('/'));
                    mounts.Append($"{WslPath(source)} {target} none bind{(ro ? ",ro" : "")} 0 0\n");
                }
                AddFile(writer, "etc/fstab", mounts.ToString());
            }
            box.User = ResolveUser(box.User, passwd);
            if (!o.Quiet) Console.Error.WriteLine($"Creating WSL2 distribution {box.Distro}");
            var (code, output) = WslCapture(["--import", box.Distro, Path.Combine(box.Dir, "vm"), rootfs, "--version", "2"], 600000);
            if (code != 0) throw new InvalidOperationException($"wsl --import failed: {output.Trim()}");
        }
        catch
        {
            try { Directory.Delete(box.Dir, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            throw;
        }
        finally { File.Delete(rootfs); }
        SaveBox(box);
        return box;
    }

    static string WslConf(Box box) => $"""
        # Written by container (wintools): this WSL distribution is container {box.Id}.
        [automount]
        enabled = true
        root = /mnt/
        mountFsTab = true

        [interop]
        appendWindowsPath = false

        [network]
        hostname = {(box.Id.Length > 63 ? box.Id[..63] : box.Id)}
        generateHosts = true
        generateResolvConf = true

        [user]
        default = root

        """.Replace("\r\n", "\n");

    static void AddFile(TarWriter writer, string name, string text)
    {
        var entry = new PaxTarEntry(TarEntryType.RegularFile, name)
        {
            Mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead,
            DataStream = new MemoryStream(Encoding.UTF8.GetBytes(text)),
        };
        writer.WriteEntry(entry);
    }

    static void AddDirectory(TarWriter writer, string name)
    {
        var path = "";
        foreach (var part in name.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            path += part + "/";
            writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, path)
            {
                Mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead
                       | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute,
            });
        }
    }

    // wsl.exe -u wants a user name; numeric users ("1000", "1000:1000") are looked up in the image's /etc/passwd.
    internal static string ResolveUser(string user, string passwd)
    {
        if (string.IsNullOrEmpty(user)) return null;
        var name = user.Split(':')[0];
        if (!name.All(char.IsAsciiDigit)) return name;
        if (name == "0") return "root";
        foreach (var line in (passwd ?? "").Split('\n'))
        {
            var f = line.Split(':');
            if (f.Length > 2 && f[2] == name) return f[0];
        }
        throw new InvalidOperationException($"user id {name} isn't in the image's /etc/passwd (WSL runs processes as named users)");
    }

    // Volumes: "C:\src:/app[:ro]", "./src:/app", or a named volume "data:/var/lib/data".
    static string ResolveVolume(string spec)
    {
        var (source, target, ro) = SplitVolume(spec);
        if (!target.StartsWith('/')) throw new UsageException($"volume target must be an absolute path ('{spec}')");
        if (!(source.Contains('\\') || source.Contains('/') || source.StartsWith('.') || (source.Length == 2 && source[1] == ':')))
        {
            var named = Path.Combine(Root, "volumes", source);
            if (!Directory.Exists(named)) Directory.CreateDirectory(named);
            source = named;
        }
        source = Path.GetFullPath(source);
        if (!Directory.Exists(source) && !File.Exists(source)) throw new InvalidOperationException($"volume source {source} doesn't exist");
        return $"{source}|{target}" + (ro ? "|ro" : "");
    }

    // Splits "C:\x:/y:ro" (Windows drive colons included) or the stored "C:\x|/y|ro".
    internal static (string Source, string Target, bool ReadOnly) SplitVolume(string spec)
    {
        if (spec.Contains('|'))
        {
            var p = spec.Split('|');
            return (p[0], p[1], p.Length > 2 && p[2] == "ro");
        }
        var start = spec.Length > 2 && spec[1] == ':' && char.IsLetter(spec[0]) ? 2 : 0;
        var colon = spec.IndexOf(':', start);
        if (colon < 0) throw new UsageException($"invalid volume '{spec}' (expected source:target)");
        var source = spec[..colon];
        var target = spec[(colon + 1)..];
        var ro = false;
        if (target.EndsWith(":ro", StringComparison.Ordinal)) { ro = true; target = target[..^3]; }
        else if (target.EndsWith(":rw", StringComparison.Ordinal)) target = target[..^3];
        return (source, target, ro);
    }

    // C:\Users\me\src -> /mnt/c/Users/me/src (WSL's automount root, set in the container's wsl.conf).
    internal static string WslPath(string windowsPath)
    {
        var full = Path.GetFullPath(windowsPath);
        if (full.Length < 3 || full[1] != ':') throw new InvalidOperationException($"{full}: only drive-letter paths can be mounted");
        return "/mnt/" + char.ToLowerInvariant(full[0]) + full[2..].Replace('\\', '/').Replace(" ", "\\040");
    }

    // "8080:80", "127.0.0.1:8080:80/tcp", "80"
    internal static (int Host, int Container) ParsePort(string spec)
    {
        var s = spec.Split('/')[0];
        var parts = s.Split(':');
        int Num(string x) => int.TryParse(x, out var n) && n is > 0 and < 65536 ? n : throw new UsageException($"invalid port '{spec}'");
        var container = Num(parts[^1]);
        var host = parts.Length >= 2 ? Num(parts[^2]) : container;
        return (host, container);
    }

    // ---- starting processes ----

    // The wsl.exe command line for the container's process, and the environment it needs.
    static (List<string> Args, Dictionary<string, string> Env) ProcessArgs(Box box, List<string> command, List<string> extraEnv, string workdir, string user, bool forceExecEnv = false)
    {
        var args = new List<string> { "-d", box.Distro, "--cd", workdir ?? box.WorkDir ?? "/" };
        if ((user ?? box.User) is { } u) args.AddRange(["-u", u]);
        var env = box.Env.Concat(extraEnv).ToList();
        var childEnv = new Dictionary<string, string>();
        args.Add("--exec");
        if (box.Shell && !forceExecEnv)
        {
            // /bin/sh sets the image environment (PATH included, which WSLENV can't carry) and execs the command.
            var script = new StringBuilder();
            foreach (var e in env)
            {
                var key = e[..e.IndexOf('=')];
                if (System.Text.RegularExpressions.Regex.IsMatch(key, "^[A-Za-z_][A-Za-z0-9_]*$"))
                    script.Append("export ").Append(key).Append('=').Append(ShellQuote(e[(e.IndexOf('=') + 1)..])).Append("; ");
            }
            script.Append("exec \"$@\"");
            args.AddRange(["/bin/sh", "-c", script.ToString(), "sh"]);
        }
        else
        {
            // No shell in the image: WSLENV hands the variables to the process (PATH stays WSL's default).
            var names = new List<string>();
            foreach (var e in env)
            {
                var key = e[..e.IndexOf('=')];
                if (key.Equals("PATH", StringComparison.OrdinalIgnoreCase)) continue;
                childEnv[key] = e[(e.IndexOf('=') + 1)..];
                names.Add(key + "/u");
            }
            var existing = Environment.GetEnvironmentVariable("WSLENV");
            childEnv["WSLENV"] = string.Join(':', new[] { existing }.Where(x => !string.IsNullOrEmpty(x)).Concat(names));
        }
        args.AddRange(command);
        return (args, childEnv);
    }

    internal static string ShellQuote(string s) => "'" + s.Replace("'", "'\\''") + "'";

    // Foreground: with -i/-t the console is the process's own; otherwise output also goes to the log.
    static int Attach(Box box, bool console)
    {
        var (args, env) = ProcessArgs(box, box.Command, [], null, null);
        var start = new ProcessStartInfo(Wsl) { UseShellExecute = false, RedirectStandardOutput = !console, RedirectStandardError = !console };
        foreach (var a in args) start.ArgumentList.Add(a);
        foreach (var (k, v) in env) start.Environment[k] = v;
        using var proxies = StartPortProxies(box);
        using var p = Process.Start(start);
        box.Pid = p.Id;
        box.PidStart = p.StartTime.ToUniversalTime().Ticks;
        SaveBox(box);
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; Terminate(box); };
        if (!console)
        {
            using var log = new FileStream(box.LogFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            var gate = new object();
            void Pump(Stream from, Stream to)
            {
                var buffer = new byte[8192];
                int n;
                while ((n = from.Read(buffer)) > 0)
                    lock (gate) { to.Write(buffer, 0, n); to.Flush(); log.Write(buffer, 0, n); log.Flush(); }
            }
            var err = Task.Run(() => Pump(p.StandardError.BaseStream, Console.OpenStandardError()));
            Pump(p.StandardOutput.BaseStream, Console.OpenStandardOutput());
            err.Wait();
        }
        p.WaitForExit();
        return p.ExitCode;
    }

    // Detached: wsl.exe runs in the background with stdout/stderr going straight to the container's log file.
    static void StartDetached(Box box)
    {
        var (args, env) = ProcessArgs(box, box.Command, [], null, null);
        var pid = SpawnLogged(Wsl, args, env, box.LogFile);
        using var p = Process.GetProcessById(pid);
        box.Pid = pid;
        box.PidStart = p.StartTime.ToUniversalTime().Ticks;
        SaveBox(box);
        if (box.Ports.Any(port => ParsePort(port) is var (h, c) && h != c))
            Console.Error.WriteLine("Warning: WSL2 forwards container ports to the same localhost port; remapped ports (-p host:container) only work for attached runs");
    }

    // Ports published under a different host number get a localhost TCP relay for the life of an attached run.
    static PortProxies StartPortProxies(Box box)
    {
        var proxies = new PortProxies();
        foreach (var spec in box.Ports)
        {
            var (host, container) = ParsePort(spec);
            if (host == container || spec.EndsWith("/udp", StringComparison.Ordinal)) continue;
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, host);
            listener.Start();
            proxies.Listeners.Add(listener);
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    System.Net.Sockets.TcpClient client;
                    try { client = await listener.AcceptTcpClientAsync(); }
                    catch (Exception) { return; }
                    _ = Task.Run(async () =>
                    {
                        using var c = client;
                        using var upstream = new System.Net.Sockets.TcpClient();
                        try
                        {
                            await upstream.ConnectAsync(System.Net.IPAddress.Loopback, container);
                            var a = c.GetStream();
                            var b = upstream.GetStream();
                            await Task.WhenAny(a.CopyToAsync(b), b.CopyToAsync(a));
                        }
                        catch (Exception) { }
                    });
                }
            });
        }
        return proxies;
    }

    sealed class PortProxies : IDisposable
    {
        public readonly List<System.Net.Sockets.TcpListener> Listeners = [];
        public void Dispose() { foreach (var l in Listeners) l.Stop(); }
    }

    // CreateProcess with inherited file handles for stdout/stderr and NUL for stdin, no console window.
    internal static int SpawnLogged(string exe, List<string> args, Dictionary<string, string> extraEnv, string logFile)
    {
        var sa = new SECURITY_ATTRIBUTES { nLength = Marshal.SizeOf<SECURITY_ATTRIBUTES>(), bInheritHandle = true };
        using var log = CreateFile(logFile, FILE_APPEND_DATA | SYNCHRONIZE, 7, ref sa, OPEN_ALWAYS, 0x80, IntPtr.Zero);
        using var nul = CreateFile("NUL", GENERIC_READ, 7, ref sa, OPEN_EXISTING, 0x80, IntPtr.Zero);
        if (log.IsInvalid || nul.IsInvalid) throw new System.ComponentModel.Win32Exception();
        var env = Environment.GetEnvironmentVariables().Cast<System.Collections.DictionaryEntry>()
            .ToDictionary(e => (string)e.Key, e => (string)e.Value, StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in extraEnv) env[k] = v;
        var block = new StringBuilder();
        foreach (var (k, v) in env.OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase)) block.Append(k).Append('=').Append(v).Append('\0');
        block.Append('\0');
        var si = new STARTUPINFO
        {
            cb = Marshal.SizeOf<STARTUPINFO>(),
            dwFlags = STARTF_USESTDHANDLES,
            hStdInput = nul.DangerousGetHandle(),
            hStdOutput = log.DangerousGetHandle(),
            hStdError = log.DangerousGetHandle(),
        };
        var commandLine = new StringBuilder(Sandbox.Quote(exe));
        foreach (var a in args) commandLine.Append(' ').Append(Sandbox.Quote(a));
        if (!CreateProcess(null, commandLine, IntPtr.Zero, IntPtr.Zero, true,
                CREATE_NO_WINDOW | CREATE_NEW_PROCESS_GROUP | CREATE_UNICODE_ENVIRONMENT, block.ToString(), null, ref si, out var pi))
            throw new System.ComponentModel.Win32Exception();
        CloseHandle(pi.hThread);
        CloseHandle(pi.hProcess);
        return pi.dwProcessId;
    }

    // ---- start / stop / kill / delete ----

    static List<string> Ids(string[] args, string usage, out bool all, params string[] flags)
    {
        all = false;
        var ids = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg is "-a" or "--all") all = true;
            else if (flags.Contains(arg)) { if (arg.StartsWith("--", StringComparison.Ordinal) || arg is "-t" or "-s" or "-n") i++; }
            else if (flags.Any(f => arg.StartsWith(f + "=", StringComparison.Ordinal))) { }
            else if (arg.StartsWith('-') && arg.Length > 1 && !flags.Contains(arg)) throw new UsageException($"unknown option '{arg}'");
            else ids.Add(arg);
        }
        if (!all && ids.Count == 0) throw new UsageException($"missing container ID (usage: {usage})");
        return ids;
    }

    static string Option(string[] args, string longName, string shortName = null)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if ((args[i] == longName || args[i] == shortName) && i + 1 < args.Length) return args[i + 1];
            if (args[i].StartsWith(longName + "=", StringComparison.Ordinal)) return args[i][(longName.Length + 1)..];
        }
        return null;
    }

    static int StartCommand(string[] args)
    {
        if (Help(args, "USAGE: container start [--attach] [--interactive] <container-id>\nStart a stopped container.")) return 0;
        var attach = args.Any(a => a is "-a" or "--attach");
        var ids = args.Where(a => !a.StartsWith('-')).ToList();
        if (ids.Count != 1) throw new UsageException("start takes one container ID");
        RequireWsl();
        var box = FindBox(ids[0]);
        if (IsRunning(box)) { Console.WriteLine(box.Id); return 0; }
        if (attach) return Attach(box, args.Any(a => a is "-i" or "--interactive") || box.Interactive);
        StartDetached(box);
        Console.WriteLine(box.Id);
        return 0;
    }

    internal static void Start(Box box) { if (!IsRunning(box)) StartDetached(box); }

    static int StopCommand(string[] args)
    {
        if (Help(args, "USAGE: container stop [--all] [--signal <signal>] [--time <seconds>] <container-id> ...\nStop running containers (SIGTERM, then the VM distribution is terminated).")) return 0;
        var ids = Ids(args, "container stop <id>...", out var all, "-s", "--signal", "-t", "--time");
        var seconds = int.TryParse(Option(args, "--time", "-t"), out var t) ? t : 5;
        var signal = Option(args, "--signal", "-s") ?? "TERM";
        var boxes = all ? ListBoxes().Where(IsRunning).ToList() : ids.Select(FindBox).ToList();
        foreach (var box in boxes) { Stop(box, signal, seconds); Console.WriteLine(box.Id); }
        return 0;
    }

    internal static void Stop(Box box, string signal = "TERM", int seconds = 5)
    {
        if (!IsRunning(box)) return;
        // Signal every process in the container but WSL's init, give them time, then end the distribution.
        WslCapture(["-d", box.Distro, "-u", "root", "--exec", "kill", "-" + signal.TrimStart('-').Replace("SIG", ""), "-1"], 10000);
        for (var waited = 0; waited < seconds * 10 && IsRunning(box); waited++) Thread.Sleep(100);
        Terminate(box);
    }

    static void Terminate(Box box)
    {
        WslCapture(["--terminate", box.Distro], 30000);
        if (IsRunning(box)) try { using var p = Process.GetProcessById(box.Pid); p.Kill(); } catch (ArgumentException) { } catch (InvalidOperationException) { }
    }

    static int KillCommand(string[] args)
    {
        if (Help(args, "USAGE: container kill [--all] [--signal <signal>] <container-id> ...\nKill running containers.")) return 0;
        var ids = Ids(args, "container kill <id>...", out var all, "-s", "--signal");
        var signal = Option(args, "--signal", "-s") ?? "KILL";
        var boxes = all ? ListBoxes().Where(IsRunning).ToList() : ids.Select(FindBox).ToList();
        foreach (var box in boxes)
        {
            if (!IsRunning(box)) throw new InvalidOperationException($"container {box.Id} is not running");
            if (signal.TrimStart('-').Replace("SIG", "") is "KILL" or "9") Terminate(box);
            else WslCapture(["-d", box.Distro, "-u", "root", "--exec", "kill", "-" + signal.TrimStart('-').Replace("SIG", ""), "-1"], 10000);
            Console.WriteLine(box.Id);
        }
        return 0;
    }

    static int DeleteCommand(string[] args)
    {
        if (Help(args, "USAGE: container delete [--all] [--force] <container-id> ...\nDelete containers (their WSL distributions and disks).")) return 0;
        var force = args.Any(a => a is "-f" or "--force");
        var ids = Ids(args.Where(a => a is not ("-f" or "--force")).ToArray(), "container delete <id>...", out var all);
        var boxes = all ? ListBoxes().Where(b => force || !IsRunning(b)).ToList() : ids.Select(FindBox).ToList();
        var status = 0;
        foreach (var box in boxes)
        {
            if (IsRunning(box) && !force) { Console.Error.WriteLine($"Error: container {box.Id} is running (stop it or use --force)"); status = 1; continue; }
            Delete(box, force);
            Console.WriteLine(box.Id);
        }
        return status;
    }

    internal static void Delete(Box box, bool force)
    {
        if (IsRunning(box)) { if (force) Terminate(box); else throw new InvalidOperationException($"container {box.Id} is running"); }
        if (WslExists) WslCapture(["--unregister", box.Distro], 120000);
        Directory.Delete(box.Dir, true);
    }

    static int PruneCommand(string[] args)
    {
        if (Help(args, "USAGE: container prune\nRemove all stopped containers.")) return 0;
        long freed = 0;
        foreach (var box in ListBoxes().Where(b => !IsRunning(b)))
        {
            freed += DirectorySize(box.Dir);
            Delete(box, false);
            Console.WriteLine(box.Id);
        }
        Console.WriteLine($"Reclaimed {FormatSize(freed)} in disk space");
        return 0;
    }

    static long DirectorySize(string dir) =>
        Directory.Exists(dir) ? Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length) : 0;

    // ---- list / inspect / logs / exec ----

    static int ListCommand(string[] args)
    {
        if (Help(args, "USAGE: container list [--all] [--quiet] [--format json|table]\nList containers (running ones unless --all).")) return 0;
        var all = args.Any(a => a is "-a" or "--all");
        var quiet = args.Any(a => a is "-q" or "--quiet");
        var format = Option(args, "--format") ?? "table";
        var boxes = ListBoxes().Where(b => all || IsRunning(b)).ToList();
        if (quiet) { foreach (var b in boxes) Console.WriteLine(b.Id); return 0; }
        if (format == "json") { PrintJson(boxes); return 0; }
        var rows = new List<string[]> { new[] { "ID", "IMAGE", "OS", "ARCH", "STATE", "ADDR" } };
        rows.AddRange(boxes.Select(b => new[] { b.Id, b.Image, "linux", b.Arch, State(b), IsRunning(b) ? "127.0.0.1" : "" }));
        PrintTable(rows);
        return 0;
    }

    internal static void PrintTable(List<string[]> rows)
    {
        var widths = Enumerable.Range(0, rows[0].Length).Select(c => rows.Max(r => r[c].Length)).ToArray();
        foreach (var row in rows)
            Console.WriteLine(string.Join("  ", row.Select((cell, c) => c == row.Length - 1 ? cell : cell.PadRight(widths[c]))).TrimEnd());
    }

    static void PrintJson(List<Box> boxes)
    {
        using var stdout = Console.OpenStandardOutput();
        using (var w = new Utf8JsonWriter(stdout, new JsonWriterOptions { Indented = true }))
        {
            w.WriteStartArray();
            foreach (var box in boxes)
            {
                w.WriteStartObject();
                w.WriteString("status", State(box));
                w.WritePropertyName("configuration");
                WriteBox(w, box);
                w.WriteEndObject();
            }
            w.WriteEndArray();
        }
        stdout.Write("\n"u8);
    }

    static int InspectCommand(string[] args)
    {
        if (Help(args, "USAGE: container inspect <container-id> ...\nDisplay information about containers as JSON.")) return 0;
        var ids = args.Where(a => !a.StartsWith('-')).ToList();
        if (ids.Count == 0) throw new UsageException("missing container ID");
        PrintJson(ids.Select(FindBox).ToList());
        return 0;
    }

    static int LogsCommand(string[] args)
    {
        if (Help(args, "USAGE: container logs [--follow] [-n <lines>] <container-id>\nFetch a container's stdout/stderr.")) return 0;
        if (args.Contains("--boot")) return Fail("container logs --boot: WSL2's VM boot log isn't per container; see `wsl --debug-shell` or dmesg inside it");
        var follow = args.Any(a => a is "-f" or "--follow");
        var lines = int.TryParse(Option(args, "-n"), out var n) ? n : -1;
        var ids = Ids(args.Where(a => a is not ("-f" or "--follow")).ToArray(), "container logs <id>", out _, "-n");
        var box = FindBox(ids[0]);
        if (!File.Exists(box.LogFile)) { if (!follow) return 0; File.WriteAllText(box.LogFile, ""); }
        using var stream = new FileStream(box.LogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();
        if (lines >= 0)
        {
            var all = text.Split('\n');
            var count = all.Length - (text.EndsWith('\n') ? 1 : 0);
            text = string.Join('\n', all.Skip(Math.Max(0, count - lines)));
        }
        Console.Write(text);
        while (follow && (IsRunning(box) || !reader.EndOfStream))
        {
            var more = reader.ReadToEnd();
            if (more.Length > 0) Console.Write(more); else Thread.Sleep(250);
        }
        return 0;
    }

    static int ExecCommand(string[] args)
    {
        if (Help(args, "USAGE: container exec [-i] [-t] [-d] [-e <env>] [-w <dir>] [-u <user>] <container-id> <command> ...\nRun a new command in a running container.")) return 0;
        var env = new List<string>();
        string workdir = null, user = null;
        var detach = false;
        var i = 0;
        for (; i < args.Length && args[i].StartsWith('-'); i++)
        {
            string Next() => i + 1 < args.Length ? args[++i] : throw new UsageException($"{args[i]} takes an argument");
            switch (args[i])
            {
                case "-i" or "--interactive" or "-t" or "--tty" or "-it" or "-ti": break;
                case "-d" or "--detach": detach = true; break;
                case "-e" or "--env": env.Add(EnvValue(Next())); break;
                case "--env-file": env.AddRange(ReadEnvFile(Next())); break;
                case "-w" or "--cwd" or "--workdir": workdir = Next(); break;
                case "-u" or "--user": user = Next(); break;
                default: throw new UsageException($"unknown option '{args[i]}'");
            }
        }
        if (args.Length - i < 2) throw new UsageException("exec needs a container ID and a command");
        RequireWsl();
        var box = FindBox(args[i]);
        if (!IsRunning(box)) throw new InvalidOperationException($"container {box.Id} is not running");
        if (user != null && user.Split(':')[0].All(char.IsAsciiDigit))
            user = ResolveUser(user, WslCapture(["-d", box.Distro, "-u", "root", "--exec", "cat", "/etc/passwd"], 10000).Output);
        var (wslArgs, childEnv) = ProcessArgs(box, args[(i + 1)..].ToList(), env, workdir, user);
        if (detach)
        {
            SpawnLogged(Wsl, wslArgs, childEnv, box.LogFile);
            return 0;
        }
        var start = new ProcessStartInfo(Wsl) { UseShellExecute = false };
        foreach (var a in wslArgs) start.ArgumentList.Add(a);
        foreach (var (k, v) in childEnv) start.Environment[k] = v;
        using var p = Process.Start(start);
        p.WaitForExit();
        return p.ExitCode;
    }

    // ---- images ----

    const string ImageUsage = """
        OVERVIEW: Manage images

        USAGE: container image <subcommand>

        SUBCOMMANDS:
          pull <reference>              Pull an image (--arch/--platform to pick one)
          list, ls [--quiet] [--verbose] [--format json]  List images
          inspect <image> ...           Display information about images
          delete, rm <image> ...        Delete images
          tag <source> <target>         Tag an image
          save --output <file> <image>  Save an image as an OCI archive
          load --input <file>           Load images from an OCI archive
          prune                         Remove blobs no image uses
          export --output <file> <image>  Write the image's flattened root file system (a tar for wsl --import)
        """;

    static int ImageCommand(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help") { Console.WriteLine(ImageUsage); return args.Length == 0 ? 1 : 0; }
        var rest = args[1..];
        switch (args[0])
        {
            case "pull":
            {
                var refs = rest.Where((a, i) => !a.StartsWith('-') && (i == 0 || rest[i - 1] is not ("-a" or "--arch" or "--platform"))).ToList();
                if (refs.Count != 1) throw new UsageException("pull takes one image reference");
                var arch = Option(rest, "--arch", "-a") ?? Option(rest, "--platform")?.Split('/').ElementAtOrDefault(1);
                var info = Pull(refs[0], arch, rest.Contains("--quiet") || rest.Contains("-q"));
                Console.WriteLine(info.Reference);
                return 0;
            }
            case "list" or "ls":
            {
                var images = ListImages();
                if (rest.Any(a => a is "-q" or "--quiet")) { foreach (var i in images) Console.WriteLine(i.Reference); return 0; }
                if (Option(rest, "--format") == "json")
                {
                    using var stdout = Console.OpenStandardOutput();
                    using (var w = new Utf8JsonWriter(stdout, new JsonWriterOptions { Indented = true }))
                    {
                        w.WriteStartArray();
                        foreach (var i in images) WriteImage(w, i);
                        w.WriteEndArray();
                    }
                    stdout.Write("\n"u8);
                    return 0;
                }
                var verbose = rest.Any(a => a is "-v" or "--verbose");
                var rows = new List<string[]> { verbose ? new[] { "NAME", "TAG", "DIGEST", "SIZE" } : new[] { "NAME", "TAG", "DIGEST" } };
                foreach (var i in images)
                    rows.Add(verbose ? [i.Name, i.Tag, i.Digest[7..19], FormatSize(i.Size)] : [i.Name, i.Tag, i.Digest[7..19]]);
                PrintTable(rows);
                return 0;
            }
            case "inspect":
            {
                using var stdout = Console.OpenStandardOutput();
                using (var w = new Utf8JsonWriter(stdout, new JsonWriterOptions { Indented = true }))
                {
                    w.WriteStartArray();
                    foreach (var name in rest.Where(a => !a.StartsWith('-')))
                        WriteImage(w, FindImage(name) ?? throw new InvalidOperationException($"image {name} not found"), detail: true);
                    w.WriteEndArray();
                }
                stdout.Write("\n"u8);
                return 0;
            }
            case "delete" or "rm":
            {
                var images = ListImages();
                var targets = rest.Any(a => a is "-a" or "--all") ? images.ToList()
                    : rest.Where(a => !a.StartsWith('-')).Select(n => FindImage(n) ?? throw new InvalidOperationException($"image {n} not found")).ToList();
                foreach (var t in targets)
                {
                    images.RemoveAll(i => i.Reference == t.Reference);
                    Console.WriteLine(t.Reference);
                }
                SaveImages(images);
                return 0;
            }
            case "tag":
            {
                if (rest.Length != 2) throw new UsageException("tag takes a source and a target reference");
                var source = FindImage(rest[0]) ?? throw new InvalidOperationException($"image {rest[0]} not found");
                var target = ImageRef.Parse(rest[1]).ToString();
                var images = ListImages();
                images.RemoveAll(i => i.Reference == target);
                images.Add(new ImageInfo { Reference = target, Digest = source.Digest, Config = source.Config, Size = source.Size });
                SaveImages(images);
                Console.WriteLine(target);
                return 0;
            }
            case "save": return SaveImage(rest);
            case "load": return LoadImage(rest);
            case "prune": return PruneImages();
            case "export":
            {
                var output = Option(rest, "--output", "-o") ?? throw new UsageException("export needs --output <file>");
                var name = rest.Where((a, i) => !a.StartsWith('-') && (i == 0 || rest[i - 1] is not ("-o" or "--output"))).FirstOrDefault()
                           ?? throw new UsageException("export needs an image");
                var image = FindImage(name) ?? throw new InvalidOperationException($"image {name} not found");
                using (var file = File.Create(output))
                using (var writer = new TarWriter(file, TarEntryFormat.Pax))
                    Flatten(ReadConfig(image).Layers, writer, (_, _) => true);
                Console.WriteLine(output);
                return 0;
            }
            case "push": return Fail("container image push: pushing to registries isn't supported by this build; use `oras` or `crane` with an archive from `container image save`");
            default: throw new UsageException($"unknown image subcommand '{args[0]}'");
        }
    }

    static void WriteImage(Utf8JsonWriter w, ImageInfo image, bool detail = false)
    {
        w.WriteStartObject();
        w.WriteString("reference", image.Reference);
        w.WriteString("digest", image.Digest);
        w.WriteString("configDigest", image.Config);
        w.WriteNumber("size", image.Size);
        if (detail && File.Exists(BlobPath(image.Config)))
        {
            var c = ReadConfig(image);
            w.WriteString("os", c.Os);
            w.WriteString("architecture", c.Architecture);
            if (c.Created != null) w.WriteString("created", c.Created);
            WriteList(w, "entrypoint", c.Entrypoint);
            WriteList(w, "cmd", c.Cmd);
            WriteList(w, "env", c.Env);
            w.WriteString("workingDir", c.WorkingDir ?? "");
            w.WriteString("user", c.User ?? "");
            WriteList(w, "exposedPorts", c.Ports);
            WriteList(w, "layers", c.Layers);
        }
        w.WriteEndObject();
    }

    // OCI image layout archive: oci-layout, index.json (refs in org.opencontainers.image.ref.name) and blobs.
    static int SaveImage(string[] args)
    {
        var output = Option(args, "--output", "-o") ?? throw new UsageException("save needs --output <file>");
        var names = args.Where((a, i) => !a.StartsWith('-') && (i == 0 || args[i - 1] is not ("-o" or "--output" or "--platform" or "--arch"))).ToList();
        if (names.Count == 0) throw new UsageException("save needs an image");
        var images = names.Select(n => FindImage(n) ?? throw new InvalidOperationException($"image {n} not found")).ToList();
        using var file = File.Create(output);
        using var writer = new TarWriter(file, TarEntryFormat.Pax);
        AddFile(writer, "oci-layout", "{\"imageLayoutVersion\":\"1.0.0\"}");
        var index = new MemoryStream();
        using (var w = new Utf8JsonWriter(index))
        {
            w.WriteStartObject();
            w.WriteNumber("schemaVersion", 2);
            w.WriteString("mediaType", "application/vnd.oci.image.index.v1+json");
            w.WriteStartArray("manifests");
            foreach (var image in images)
            {
                w.WriteStartObject();
                w.WriteString("mediaType", ManifestMediaType(image));
                w.WriteString("digest", image.Digest);
                w.WriteNumber("size", new FileInfo(BlobPath(image.Digest)).Length);
                w.WriteStartObject("annotations");
                w.WriteString("org.opencontainers.image.ref.name", image.Reference);
                w.WriteString("io.containerd.image.name", image.Reference);
                w.WriteEndObject();
                w.WriteEndObject();
            }
            w.WriteEndArray();
            w.WriteEndObject();
        }
        AddFile(writer, "index.json", Encoding.UTF8.GetString(index.ToArray()));
        var written = new HashSet<string>();
        foreach (var image in images)
        {
            var config = ReadConfig(image);
            foreach (var digest in new[] { image.Digest, image.Config }.Concat(config.Layers))
                if (written.Add(digest)) writer.WriteEntry(BlobPath(digest), "blobs/sha256/" + Hex(digest));
        }
        Console.WriteLine(output);
        return 0;
    }

    static string ManifestMediaType(ImageInfo image)
    {
        using var doc = JsonDocument.Parse(File.ReadAllBytes(BlobPath(image.Digest)));
        return Str(doc.RootElement, "mediaType") ?? "application/vnd.oci.image.manifest.v1+json";
    }

    static int LoadImage(string[] args)
    {
        var input = Option(args, "--input", "-i") ?? args.FirstOrDefault(a => !a.StartsWith('-')) ?? throw new UsageException("load needs --input <file>");
        string indexJson = null;
        using (var file = File.OpenRead(input))
        using (var reader = new TarReader(file))
        {
            TarEntry entry;
            while ((entry = reader.GetNextEntry()) != null)
            {
                var name = NormalName(entry.Name);
                if (name == "index.json" && entry.DataStream != null) indexJson = new StreamReader(entry.DataStream).ReadToEnd();
                else if (name.StartsWith("blobs/sha256/", StringComparison.Ordinal) && entry.DataStream != null)
                {
                    var path = BlobPath("sha256:" + name[13..]);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    using var target = File.Create(path);
                    entry.DataStream.CopyTo(target);
                }
            }
        }
        if (indexJson == null) throw new InvalidOperationException($"{input} isn't an OCI image layout archive (no index.json)");
        var images = ListImages();
        using var doc = JsonDocument.Parse(indexJson);
        foreach (var m in doc.RootElement.GetProperty("manifests").EnumerateArray())
        {
            var digest = m.GetProperty("digest").GetString();
            var reference = m.TryGetProperty("annotations", out var ann) ? Str(ann, "io.containerd.image.name") ?? Str(ann, "org.opencontainers.image.ref.name") : null;
            reference = reference == null || !reference.Contains('/') && !reference.Contains(':') ? $"loaded/{Hex(digest)[..12]}:latest" : ImageRef.Parse(reference).ToString();
            using var manifest = JsonDocument.Parse(File.ReadAllBytes(BlobPath(digest)));
            if (!manifest.RootElement.TryGetProperty("config", out var config)) continue;
            var size = manifest.RootElement.GetProperty("layers").EnumerateArray().Sum(l => l.GetProperty("size").GetInt64());
            images.RemoveAll(i => i.Reference == reference);
            images.Add(new ImageInfo { Reference = reference, Digest = digest, Config = config.GetProperty("digest").GetString(), Size = size });
            Console.WriteLine(reference);
        }
        SaveImages(images);
        return 0;
    }

    static int PruneImages()
    {
        var used = new HashSet<string>();
        foreach (var image in ListImages())
        {
            used.Add(Hex(image.Digest));
            used.Add(Hex(image.Config));
            foreach (var layer in ReadConfig(image).Layers) used.Add(Hex(layer));
        }
        long freed = 0;
        var dir = Path.Combine(Root, "blobs", "sha256");
        if (Directory.Exists(dir))
            foreach (var blob in Directory.GetFiles(dir).Where(f => !used.Contains(Path.GetFileName(f))))
            {
                freed += new FileInfo(blob).Length;
                File.Delete(blob);
            }
        Console.WriteLine($"Reclaimed {FormatSize(freed)} in disk space");
        return 0;
    }

    // ---- registry logins ----

    static int RegistryCommand(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            Console.WriteLine("USAGE: container registry <subcommand>\n\n  login [--username <user>] [--password-stdin] <server>\n  logout <server>\n\nLogins are kept in Windows Credential Manager (container:<server>).");
            return args.Length == 0 ? 1 : 0;
        }
        var server = args.Skip(1).Where((a, i) => !a.StartsWith('-') && (i == 0 || args[i] is not ("-u" or "--username"))).LastOrDefault()
                     ?? throw new UsageException("missing registry server");
        if (server is "index.docker.io" or "registry-1.docker.io") server = "docker.io";
        switch (args[0])
        {
            case "login":
            {
                var user = Option(args, "--username", "-u");
                if (user == null) { Console.Error.Write("Username: "); user = Console.ReadLine(); }
                string password;
                if (args.Contains("--password-stdin")) password = Console.In.ReadToEnd().TrimEnd('\r', '\n');
                else { Console.Error.Write("Password: "); password = ReadSecret(); }
                if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password)) throw new UsageException("username and password are required");
                StoreCredentials(server, user, password);
                Console.WriteLine("Login succeeded");
                return 0;
            }
            case "logout":
                if (!DeleteCredentials(server)) return Fail($"not logged in to {server}");
                Console.WriteLine($"Removed login credentials for {server}");
                return 0;
            default: throw new UsageException($"unknown registry subcommand '{args[0]}'");
        }
    }

    static string ReadSecret()
    {
        if (Console.IsInputRedirected) return Console.ReadLine();
        var sb = new StringBuilder();
        ConsoleKeyInfo key;
        while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)
        {
            if (key.Key == ConsoleKey.Backspace) { if (sb.Length > 0) sb.Length--; }
            else sb.Append(key.KeyChar);
        }
        Console.Error.WriteLine();
        return sb.ToString();
    }

    // ---- volumes ----

    static int VolumeCommand(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            Console.WriteLine("USAGE: container volume <subcommand>\n\n  create <name>\n  list, ls [--quiet]\n  inspect <name> ...\n  delete, rm <name> ...\n\nNamed volumes are folders under the container root, bind-mounted with -v <name>:<path>.");
            return args.Length == 0 ? 1 : 0;
        }
        var dir = Path.Combine(Root, "volumes");
        var names = args.Skip(1).Where(a => !a.StartsWith('-')).ToList();
        switch (args[0])
        {
            case "create":
                if (names.Count != 1 || !System.Text.RegularExpressions.Regex.IsMatch(names[0], "^[a-zA-Z0-9][a-zA-Z0-9_.-]*$"))
                    throw new UsageException("create takes one volume name");
                Directory.CreateDirectory(Path.Combine(dir, names[0]));
                Console.WriteLine(names[0]);
                return 0;
            case "list" or "ls":
            {
                var volumes = Directory.Exists(dir) ? Directory.GetDirectories(dir).Select(Path.GetFileName).Order().ToList() : [];
                if (args.Contains("-q") || args.Contains("--quiet")) { volumes.ForEach(Console.WriteLine); return 0; }
                var rows = new List<string[]> { new[] { "NAME", "SOURCE" } };
                rows.AddRange(volumes.Select(v => new[] { v, Path.Combine(dir, v) }));
                PrintTable(rows);
                return 0;
            }
            case "inspect":
                foreach (var n in names)
                {
                    var path = Path.Combine(dir, n);
                    if (!Directory.Exists(path)) throw new InvalidOperationException($"volume {n} not found");
                    Console.WriteLine($"{{\"name\":{JsonString(n)},\"source\":{JsonString(path)},\"size\":{DirectorySize(path)}}}");
                }
                return 0;
            case "delete" or "rm":
                foreach (var n in names)
                {
                    var path = Path.Combine(dir, n);
                    if (!Directory.Exists(path)) throw new InvalidOperationException($"volume {n} not found");
                    if (ListBoxes().Any(b => b.Volumes.Any(v => SplitVolume(v).Source.Equals(path, StringComparison.OrdinalIgnoreCase))))
                        throw new InvalidOperationException($"volume {n} is in use by a container");
                    Directory.Delete(path, true);
                    Console.WriteLine(n);
                }
                return 0;
            default: throw new UsageException($"unknown volume subcommand '{args[0]}'");
        }
    }

    static string JsonString(string s) => "\"" + JsonEncodedText.Encode(s, System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping) + "\"";

    // ---- system ----

    static int SystemCommand(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            Console.WriteLine("USAGE: container system <subcommand>\n\n  start     Check WSL2 and prepare the container store\n  stop      Stop all running containers\n  status    Show WSL2 and container store status\n  df        Show disk usage");
            return args.Length == 0 ? 1 : 0;
        }
        switch (args[0])
        {
            case "start":
                RequireWsl();
                Directory.CreateDirectory(Root);
                Console.WriteLine($"Verifying WSL2... {WslVersion() ?? "installed"}");
                Console.WriteLine($"Container store: {Root}");
                return 0;
            case "stop":
                foreach (var box in ListBoxes().Where(IsRunning)) { Stop(box); Console.WriteLine(box.Id); }
                return 0;
            case "status":
            {
                if (!WslExists) { Console.WriteLine("WSL is not installed (run `wsl --install` in an administrator prompt)"); return 1; }
                var version = WslVersion();
                if (version == null) { Console.WriteLine("WSL2 is not available (wsl --version failed; run `wsl --install --no-distribution`)"); return 1; }
                Console.WriteLine($"container system is running (WSL {version})");
                Console.WriteLine($"store: {Root}");
                var boxes = ListBoxes();
                Console.WriteLine($"containers: {boxes.Count} ({boxes.Count(IsRunning)} running), images: {ListImages().Count}");
                return 0;
            }
            case "df":
            {
                var rows = new List<string[]> { new[] { "TYPE", "TOTAL", "SIZE" } };
                rows.Add(["Images", ListImages().Count.ToString(), FormatSize(DirectorySize(Path.Combine(Root, "blobs")))]);
                rows.Add(["Containers", ListBoxes().Count.ToString(), FormatSize(DirectorySize(Path.Combine(Root, "containers")))]);
                rows.Add(["Volumes", (Directory.Exists(Path.Combine(Root, "volumes")) ? Directory.GetDirectories(Path.Combine(Root, "volumes")).Length : 0).ToString(),
                    FormatSize(DirectorySize(Path.Combine(Root, "volumes")))]);
                PrintTable(rows);
                return 0;
            }
            case "kernel" or "dns" or "property":
                return Fail($"container system {args[0]}: WSL2 manages the Linux kernel and DNS for all distributions (settings live in %UserProfile%\\.wslconfig)");
            case "logs":
                return Fail("container system logs: there is no container API server on Windows; each container's output is in `container logs <id>`");
            default: throw new UsageException($"unknown system subcommand '{args[0]}'");
        }
    }

    // ---- wsl.exe ----

    static string Wsl => Path.Combine(Environment.SystemDirectory, "wsl.exe");
    internal static bool WslExists => File.Exists(Wsl);

    static void RequireWsl()
    {
        if (!WslExists) throw new InvalidOperationException("WSL is not installed (run `wsl --install` in an administrator prompt first)");
    }

    static string WslVersion()
    {
        var (code, output) = WslCapture(["--version"], 15000);
        if (code != 0) return null;
        var line = output.Split('\n').FirstOrDefault(l => l.Contains(':'));
        return line?[(line.IndexOf(':') + 1)..].Trim();
    }

    // WSL_UTF8=1 makes wsl.exe write UTF-8 instead of UTF-16 when its output is redirected.
    internal static (int Code, string Output) WslCapture(string[] args, int timeout)
    {
        var start = new ProcessStartInfo(Wsl)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        start.Environment["WSL_UTF8"] = "1";
        foreach (var a in args) start.ArgumentList.Add(a);
        using var p = Process.Start(start);
        var stderr = p.StandardError.ReadToEndAsync();
        var output = p.StandardOutput.ReadToEnd();
        if (!p.WaitForExit(timeout)) { p.Kill(); return (-1, "timed out"); }
        return (p.ExitCode, (output + stderr.Result).Replace("\0", "").Replace("\r", ""));
    }

    // ---- Win32 ----

    const uint GENERIC_READ = 0x80000000, FILE_APPEND_DATA = 0x4, SYNCHRONIZE = 0x100000;
    const uint OPEN_EXISTING = 3, OPEN_ALWAYS = 4;
    const int STARTF_USESTDHANDLES = 0x100;
    const uint CREATE_NO_WINDOW = 0x08000000, CREATE_NEW_PROCESS_GROUP = 0x200, CREATE_UNICODE_ENVIRONMENT = 0x400;

    [StructLayout(LayoutKind.Sequential)]
    struct SECURITY_ATTRIBUTES
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor;
        [MarshalAs(UnmanagedType.Bool)] public bool bInheritHandle;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct STARTUPINFO
    {
        public int cb;
        public string lpReserved, lpDesktop, lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PROCESS_INFORMATION
    {
        public IntPtr hProcess, hThread;
        public int dwProcessId, dwThreadId;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CreateFileW")]
    static extern SafeFileHandle CreateFile(string name, uint access, uint share, ref SECURITY_ATTRIBUTES sa, uint disposition, uint flags, IntPtr template);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CreateProcessW")]
    static extern bool CreateProcess(string application, StringBuilder commandLine, IntPtr processAttributes, IntPtr threadAttributes,
        bool inheritHandles, uint flags, string environment, string directory, ref STARTUPINFO startup, out PROCESS_INFORMATION info);

    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr handle);
}
