using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Win32;

namespace WinTools;

// distrobox: Linux distributions inside your terminal, on WSL instead of podman. A "container" is a
// WSL distribution: create installs one from the WSL catalog (images like ubuntu:24.04 or
// fedora map to its names), enter opens a shell in it in the current directory, and so on.
static class Distrobox
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("distrobox", "Use any Linux distribution inside your terminal (WSL)", Run);
        Commands.Add("distrobox-create", "Create a distrobox (installs a WSL distribution)", Create);
        Commands.Add("distrobox-enter", "Enter a distrobox", Enter);
        Commands.Add("distrobox-list", "List distroboxes (WSL distributions)", List);
        Commands.Add("distrobox-rm", "Remove distroboxes", Remove);
        Commands.Add("distrobox-stop", "Stop running distroboxes", Stop);
        Commands.Add("distrobox-upgrade", "Upgrade the packages inside distroboxes", Upgrade);
        Commands.Add("distrobox-ephemeral", "Create a temporary distrobox, enter it, and remove it on exit", Ephemeral);
        Commands.Add("distrobox-export", "Export a program from a distrobox to Windows", Export);
    }

    const string DefaultName = "my-distrobox";
    const string DefaultImage = "ubuntu";

    static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            Console.WriteLine("""
                distrobox version: 1.0.0 (WSL)

                Choose one of the available commands:
                    create
                    enter
                    list | ls
                    rm
                    stop
                    upgrade
                    ephemeral
                    export
                    version
                """.Replace("1.0.0", Commands.Version));
            return args.Length == 0 ? 1 : 0;
        }
        var rest = args[1..];
        return args[0] switch
        {
            "create" => Create(rest),
            "enter" => Enter(rest),
            "list" or "ls" => List(rest),
            "rm" => Remove(rest),
            "stop" => Stop(rest),
            "upgrade" => Upgrade(rest),
            "ephemeral" => Ephemeral(rest),
            "export" => Export(rest),
            "version" or "-V" or "--version" => Version(),
            "assemble" or "generate-entry" or "host-exec" or "init" =>
                Fail($"distrobox {args[0]}: not needed on Windows: WSL starts distributions itself, adds their GUI apps to the Start menu, and runs Windows programs from inside"),
            _ => throw new UsageException($"invalid command '{args[0]}' (see distrobox --help)"),
        };
    }

    static int Version() { Console.WriteLine($"distrobox: {Commands.Version}"); return 0; }

    static int Fail(string message) { Console.Error.WriteLine(message); return 1; }

    // ---- create ----

    static int Create(string[] args)
    {
        string name = null, image = null, clone = null;
        bool dryRun = false, yes = false;
        for (var i = 0; i < args.Length; i++)
        {
            string Next() => i + 1 < args.Length ? args[++i] : throw new UsageException($"{args[i]} takes an argument");
            switch (args[i])
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        distrobox version: VERSION

                        Usage:
                            distrobox create --image ubuntu:24.04 --name test
                            distrobox create --clone ubuntu --name ubuntu-copy

                        Options:
                            --image/-i:     image to use; mapped to a WSL distribution (ubuntu, ubuntu:22.04, debian,
                                            fedora, archlinux, opensuse/tumbleweed, almalinux:9, kali, oraclelinux:9,
                                            rocky) or an exact name from wsl --list --online
                            --name/-n:      name for the distrobox (default: my-distrobox)
                            --clone/-c:     name of the distrobox to clone
                            --yes/-Y:       non-interactive
                            --dry-run/-d:   only print the wsl command
                            --version/-V:   show version
                        """.Replace("VERSION", Commands.Version));
                    return 0;
                case "-V" or "--version": return Version();
                case "-i" or "--image": image = Next(); break;
                case "-n" or "--name": name = Next(); break;
                case "-c" or "--clone": clone = Next(); break;
                case "-d" or "--dry-run": dryRun = true; break;
                case "-Y" or "--yes": yes = true; break;
                // podman/docker container settings have no WSL meaning.
                case "-p" or "--pull" or "-r" or "--root" or "-I" or "--init" or "--nvidia" or "--no-entry" or "-v" or "--verbose"
                    or "--unshare-all" or "--unshare-devsys" or "--unshare-groups" or "--unshare-ipc" or "--unshare-netns" or "--unshare-process":
                    break;
                case "-H" or "--home" or "-a" or "--additional-flags" or "-ap" or "--additional-packages" or "--volume"
                    or "--init-hooks" or "--pre-init-hooks" or "--platform" or "--hostname":
                    Next();
                    Console.Error.WriteLine($"distrobox-create: {args[i - 1]} is ignored: WSL distributions share your Windows files under /mnt instead");
                    break;
                default:
                    if (args[i].StartsWith('-')) throw new UsageException($"Invalid argument {args[i]}");
                    name ??= args[i];
                    break;
            }
        }
        name ??= DefaultName;

        if (clone != null)
        {
            var tar = Path.Combine(Path.GetTempPath(), $"distrobox-{clone}-{Guid.NewGuid():N}.tar");
            var location = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "distrobox", name);
            string[] export = ["--export", clone, tar];
            string[] import = ["--import", name, location, tar];
            if (dryRun) { Console.WriteLine(WslLine(export)); Console.WriteLine(WslLine(import)); return 0; }
            RequireWsl();
            if (Distributions().Any(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                return Fail($"Distrobox named '{name}' already exists.");
            try
            {
                if (Wsl(export) != 0) return 1;
                Directory.CreateDirectory(location);
                if (Wsl(import) != 0) return 1;
            }
            finally { File.Delete(tar); }
            Console.WriteLine($"Distrobox '{name}' successfully created.\nTo enter, run:\n\ndistrobox enter {name}");
            return 0;
        }

        var catalog = dryRun ? null : OnlineDistributions();
        var distro = ResolveImage(image ?? DefaultImage, catalog);
        if (distro == null) return Fail($"distrobox-create: no WSL distribution matches image '{image}' (see wsl --list --online)");
        string[] install = ["--install", distro, "--name", name, "--no-launch"];
        if (dryRun) { Console.WriteLine(WslLine(install)); return 0; }
        RequireWsl();
        if (Distributions().Any(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return Fail($"Distrobox named '{name}' already exists.");
        if (!yes && !Console.IsInputRedirected)
        {
            Console.Write($"Install {distro} from the WSL catalog as '{name}'? [Y/n]: ");
            var answer = Console.ReadLine()?.Trim() ?? "";
            if (answer.Length > 0 && !answer.StartsWith('y') && !answer.StartsWith('Y')) return Fail("Aborted.");
        }
        Console.Error.WriteLine($"Creating '{name}' using image {distro}");
        if (Wsl(install) != 0) return 1;
        Console.WriteLine($"Distrobox '{name}' successfully created.\nTo enter, run:\n\ndistrobox enter {name}");
        return 0;
    }

    // The WSL catalog as of this release, for --dry-run and when the online list can't be fetched.
    static readonly string[] KnownDistributions =
    [
        "Ubuntu", "Ubuntu-26.04", "Ubuntu-24.04", "Ubuntu-22.04", "openSUSE-Tumbleweed", "openSUSE-Leap-16.0", "RLCPlus-9", "RLCPlus-10",
        "SUSE-Linux-Enterprise-15-SP7", "SUSE-Linux-Enterprise-16.0", "kali-linux", "Debian", "AlmaLinux-8", "AlmaLinux-9",
        "AlmaLinux-Kitten-10", "AlmaLinux-10", "archlinux", "FedoraLinux-44", "FedoraLinux-43", "eLxr", "OracleLinux_7_9",
        "OracleLinux_8_10", "OracleLinux_9_5",
    ];

    // Container image names → the WSL catalog's distribution prefixes.
    static readonly Dictionary<string, string> ImageNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ubuntu"] = "Ubuntu", ["debian"] = "Debian", ["fedora"] = "FedoraLinux", ["arch"] = "archlinux", ["archlinux"] = "archlinux",
        ["opensuse-tumbleweed"] = "openSUSE-Tumbleweed", ["tumbleweed"] = "openSUSE-Tumbleweed", ["opensuse-leap"] = "openSUSE-Leap",
        ["leap"] = "openSUSE-Leap", ["almalinux"] = "AlmaLinux", ["alma"] = "AlmaLinux", ["kali"] = "kali-linux",
        ["kalilinux"] = "kali-linux", ["kali-rolling"] = "kali-linux", ["kali-linux"] = "kali-linux", ["oraclelinux"] = "OracleLinux",
        ["oracle"] = "OracleLinux", ["rocky"] = "RLCPlus", ["rockylinux"] = "RLCPlus", ["sles"] = "SUSE-Linux-Enterprise",
        ["suse"] = "SUSE-Linux-Enterprise", ["elxr"] = "eLxr",
    };

    internal static string ResolveImage(string image, List<string> catalog)
    {
        catalog ??= [.. KnownDistributions];
        var exact = catalog.FirstOrDefault(d => d.Equals(image, StringComparison.OrdinalIgnoreCase));
        if (exact != null) return exact;

        // quay.io/toolbx-images/ubuntu-toolbox:24.04, docker.io/library/debian:12, opensuse/tumbleweed
        var parts = image.ToLowerInvariant().Split('/').ToList();
        if (parts.Count > 1 && (parts[0].Contains('.') || parts[0].Contains(':'))) parts.RemoveAt(0);
        var last = parts[^1];
        var tag = "";
        var colon = last.IndexOf(':');
        if (colon >= 0) { tag = last[(colon + 1)..]; last = last[..colon]; }
        var baseName = parts.Count > 1 && parts[^2] == "opensuse" ? "opensuse-" + last : last;
        foreach (var suffix in new[] { "-toolbox", "-toolbx" })
            if (baseName.EndsWith(suffix)) baseName = baseName[..^suffix.Length];
        if (tag is "latest" or "rolling" or "stable") tag = "";
        if (!ImageNames.TryGetValue(baseName, out var prefix)) prefix = baseName;

        var candidates = catalog.Where(d => d.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
        if (candidates.Count == 0) return null;
        static string Normal(string s) => s.Replace('_', '.').Replace('-', '.').ToLowerInvariant();
        if (tag.Length > 0)
        {
            // Debian and Arch have one entry whatever the tag.
            if (candidates.Count == 1 && !candidates[0].Any(char.IsDigit)) return candidates[0];
            // ubuntu:22.04 → Ubuntu-22.04, oraclelinux:9 → OracleLinux_9_5 (the release starts with the tag).
            var wanted = Normal(tag);
            return candidates
                .Where(d => Normal(d[prefix.Length..]).Trim('.') is var v && (v == wanted || v.StartsWith(wanted + ".")))
                .OrderByDescending(d => VersionKey(d[prefix.Length..]))
                .FirstOrDefault();
        }
        var plain = candidates.FirstOrDefault(d => d.Equals(prefix, StringComparison.OrdinalIgnoreCase));
        if (plain != null) return plain;
        // No tag: the newest release that isn't a preview (AlmaLinux-10 rather than AlmaLinux-Kitten-10).
        return candidates
            .Where(d => d[prefix.Length..].All(c => char.IsDigit(c) || c is '-' or '_' or '.'))
            .OrderByDescending(d => VersionKey(d[prefix.Length..]))
            .FirstOrDefault() ?? candidates[0];
    }

    static string VersionKey(string suffix) =>
        string.Join('.', suffix.Split(['-', '_', '.'], StringSplitOptions.RemoveEmptyEntries).Select(p => p.PadLeft(6, '0')));

    // `wsl --list --online`: NAME and FRIENDLY NAME columns after the header line.
    static List<string> OnlineDistributions()
    {
        if (WslPath == null) return null;
        var (code, output) = WslCapture(["--list", "--online"]);
        if (code != 0) return null;
        var names = new List<string>();
        var table = false;
        foreach (var line in output.Split('\n').Select(l => l.Trim()))
        {
            if (line.StartsWith("NAME ")) { table = true; continue; }
            if (table && line.Length > 0) names.Add(line.Split(' ', 2)[0]);
        }
        return names.Count > 0 ? names : null;
    }

    // ---- enter / ephemeral ----

    static int Enter(string[] args)
    {
        string name = null;
        bool root = false, noWorkdir = false;
        var command = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help":
                    Console.WriteLine("Usage:\n    distrobox enter [--name NAME] [--root] [--no-workdir] [-- command args...]");
                    return 0;
                case "-V" or "--version": return Version();
                case "-n" or "--name": name = i + 1 < args.Length ? args[++i] : throw new UsageException("--name takes an argument"); break;
                case "-r" or "--root": root = true; break;
                case "-nw" or "--no-workdir": noWorkdir = true; break;
                case "-T" or "-H" or "--no-tty" or "-d" or "--dry-run" or "-e" or "--exec" or "--clean-path" or "-v" or "--verbose": break;
                case "--":
                    command.AddRange(args[(i + 1)..]);
                    i = args.Length;
                    break;
                default:
                    if (args[i].StartsWith('-')) throw new UsageException($"Invalid argument {args[i]}");
                    if (name == null) name = args[i];
                    else { command.AddRange(args[i..]); i = args.Length; }
                    break;
            }
        }
        name ??= DefaultName;
        RequireWsl();
        if (!Distributions().Any(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return Fail($"Cannot find container {name}. Create it now, out of image ubuntu? Run: distrobox create --name {name}");
        var wsl = new List<string> { "-d", name };
        if (root) wsl.AddRange(["-u", "root"]);
        if (noWorkdir) wsl.AddRange(["--cd", "~"]);
        if (command.Count > 0) { wsl.Add("--"); wsl.AddRange(command); }
        return Wsl([.. wsl]);
    }

    static int Ephemeral(string[] args)
    {
        var create = new List<string>();
        var command = new List<string>();
        string name = null;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--") { command.AddRange(args[(i + 1)..]); break; }
            if (args[i] is "-n" or "--name" && i + 1 < args.Length) { name = args[++i]; continue; }
            if (args[i] is "-h" or "--help") { Console.WriteLine("Usage:\n    distrobox ephemeral [--image IMAGE] [-- command args...]"); return 0; }
            create.Add(args[i]);
        }
        name ??= "distrobox-" + Guid.NewGuid().ToString("N")[..10];
        if (create.Contains("-d") || create.Contains("--dry-run")) return Create([.. create, "--name", name]);
        var status = Create([.. create, "--name", name, "--yes"]);
        if (status != 0) return status;
        try { return Enter(command.Count > 0 ? ["--name", name, "--", .. command] : ["--name", name]); }
        finally { Wsl(["--unregister", name]); }
    }

    // ---- list / rm / stop / upgrade ----

    sealed class Distribution
    {
        public string Id, Name, Image, BasePath;
        public bool Running;
    }

    // Registered distributions, from WSL's registry entries (HKCU\...\Lxss\{guid}).
    static List<Distribution> Distributions()
    {
        var list = new List<Distribution>();
        using var lxss = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Lxss");
        if (lxss == null) return list;
        var running = RunningDistributions();
        foreach (var id in lxss.GetSubKeyNames())
        {
            using var key = lxss.OpenSubKey(id);
            if (key?.GetValue("DistributionName") is not string name) continue;
            var flavor = key.GetValue("Flavor") as string;
            var version = key.GetValue("OsVersion") as string;
            var package = key.GetValue("PackageFamilyName") as string;
            var image = !string.IsNullOrEmpty(flavor) ? flavor + (string.IsNullOrEmpty(version) ? "" : ":" + version)
                : !string.IsNullOrEmpty(package) ? package : "imported";
            list.Add(new Distribution
            {
                Id = id.Trim('{', '}').Replace("-", "")[..12],
                Name = name,
                Image = image,
                BasePath = key.GetValue("BasePath") as string,
                Running = running.Contains(name),
            });
        }
        return list;
    }

    static HashSet<string> RunningDistributions()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (WslPath == null) return set;
        var (code, output) = WslCapture(["--list", "--running", "--quiet"]);
        if (code == 0)
            foreach (var line in output.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0))
                set.Add(line);
        return set;
    }

    static int List(string[] args)
    {
        var noColor = false;
        foreach (var arg in args)
        {
            switch (arg)
            {
                case "-h" or "--help": Console.WriteLine("Usage:\n    distrobox list [--no-color]"); return 0;
                case "-V" or "--version": return Version();
                case "--no-color": noColor = true; break;
                case "-v" or "--verbose" or "-r" or "--root": break;
                default: throw new UsageException($"Invalid argument {arg}");
            }
        }
        var boxes = Distributions();
        var nameWidth = Math.Max(20, boxes.Select(b => b.Name.Length).DefaultIfEmpty(0).Max());
        Console.WriteLine($"{"ID",-12} | {"NAME".PadRight(nameWidth)} | {"STATUS",-18} | IMAGE");
        var color = !noColor && !Console.IsOutputRedirected;
        foreach (var b in boxes)
        {
            var line = $"{b.Id,-12} | {b.Name.PadRight(nameWidth)} | {(b.Running ? "Up" : "Exited"),-18} | {b.Image}";
            Console.WriteLine(color ? (b.Running ? "\e[32m" : "\e[33m") + line + "\e[0m" : line);
        }
        return 0;
    }

    // Names from the arguments, or every distribution with --all.
    static List<string> Targets(string[] args, string usage, out bool force, out bool yes)
    {
        force = yes = false;
        var all = false;
        var names = new List<string>();
        foreach (var arg in args)
        {
            switch (arg)
            {
                case "-h" or "--help": throw new HelpException(usage);
                case "-f" or "--force": force = true; break;
                case "-Y" or "--yes": yes = true; break;
                case "-a" or "--all": all = true; break;
                case "-r" or "--root" or "-v" or "--verbose" or "--rm-home": break;
                default:
                    if (arg.StartsWith('-')) throw new UsageException($"Invalid argument {arg}");
                    names.Add(arg);
                    break;
            }
        }
        RequireWsl();
        var known = Distributions();
        if (all) return known.Select(d => d.Name).ToList();
        if (names.Count == 0) names.Add(DefaultName);
        foreach (var n in names)
            if (!known.Any(d => d.Name.Equals(n, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Cannot find container {n}.");
        return names;
    }

    sealed class HelpException(string text) : Exception(text);

    static int Remove(string[] args)
    {
        List<string> names;
        bool force, yes;
        try { names = Targets(args, "Usage:\n    distrobox rm [--force] [--yes] [--all] NAME...", out force, out yes); }
        catch (HelpException h) { Console.WriteLine(h.Message); return 0; }
        if (!force && !yes)
        {
            Console.Write($"Do you really want to delete containers:{string.Concat(names.Select(n => "\n\t" + n))}\nThis removes the distribution and every file in it. [y/N]: ");
            var answer = Console.IsInputRedirected ? "" : Console.ReadLine()?.Trim() ?? "";
            if (!answer.StartsWith('y') && !answer.StartsWith('Y')) return Fail("Aborted.");
        }
        var status = 0;
        foreach (var n in names)
            if (Wsl(["--unregister", n]) != 0) status = 1;
        return status;
    }

    static int Stop(string[] args)
    {
        List<string> names;
        bool force, yes;
        try { names = Targets(args, "Usage:\n    distrobox stop [--yes] [--all] NAME...", out force, out yes); }
        catch (HelpException h) { Console.WriteLine(h.Message); return 0; }
        var running = RunningDistributions();
        var status = 0;
        foreach (var n in names.Where(running.Contains))
            if (Wsl(["--terminate", n]) != 0) status = 1;
        return status;
    }

    // Detects the distribution's package manager and runs its full upgrade as root.
    const string UpgradeScript = """
        if command -v apt-get >/dev/null; then apt-get update && DEBIAN_FRONTEND=noninteractive apt-get -y dist-upgrade
        elif command -v dnf >/dev/null; then dnf -y upgrade --refresh
        elif command -v pacman >/dev/null; then pacman -Syu --noconfirm
        elif command -v zypper >/dev/null; then zypper -n dup
        elif command -v apk >/dev/null; then apk upgrade --update-cache
        elif command -v yum >/dev/null; then yum -y update
        elif command -v tdnf >/dev/null; then tdnf -y upgrade
        else echo "distrobox-upgrade: no supported package manager found" >&2; exit 1
        fi
        """;

    static int Upgrade(string[] args)
    {
        List<string> names;
        bool force, yes;
        try { names = Targets(args, "Usage:\n    distrobox upgrade [--all] NAME...", out force, out yes); }
        catch (HelpException h) { Console.WriteLine(h.Message); return 0; }
        var status = 0;
        foreach (var n in names)
        {
            Console.Error.WriteLine($"Upgrading {n}...");
            if (Wsl(["-d", n, "-u", "root", "--", "sh", "-c", UpgradeScript.Replace("\r", "")]) != 0) status = 1;
        }
        return status;
    }

    // ---- export ----

    // distrobox-export runs inside the container on Linux; here it runs on Windows and names the
    // container with --name. --bin writes a .cmd shim that runs the program through wsl.exe.
    static int Export(string[] args)
    {
        string name = null, bin = null, app = null, exportPath = null;
        var delete = false;
        var extraFlags = "";
        for (var i = 0; i < args.Length; i++)
        {
            string Next() => i + 1 < args.Length ? args[++i] : throw new UsageException($"{args[i]} takes an argument");
            switch (args[i])
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage:
                            distrobox export --name NAME --bin /usr/bin/htop [--export-path DIR]
                            distrobox export --name NAME --bin /usr/bin/htop --delete

                        Options:
                            --name/-n:          the distrobox to export from (default: my-distrobox)
                            --bin/-b:           program inside the distrobox to export
                            --export-path/-ep:  where to write the Windows shim (default: ~\.local\bin)
                            --extra-flags/-ef:  extra arguments added to the exported program
                            --delete/-d:        remove the exported shim
                            --app/-a:           not needed: WSL adds Linux GUI apps to the Start menu itself
                        """);
                    return 0;
                case "-V" or "--version": return Version();
                case "-n" or "--name": name = Next(); break;
                case "-b" or "--bin": bin = Next(); break;
                case "-a" or "--app": app = Next(); break;
                case "-ep" or "--export-path": exportPath = Next(); break;
                case "-ef" or "--extra-flags": extraFlags = Next(); break;
                case "-d" or "--delete": delete = true; break;
                case "-el" or "--export-label": Next(); break;
                case "-S" or "--sudo": break;
                default: throw new UsageException($"Invalid argument {args[i]}");
            }
        }
        if (app != null)
        {
            Console.WriteLine($"distrobox-export: nothing to do for {app}: WSL already lists a distribution's GUI apps in the Start menu (under the distribution's name)");
            return 0;
        }
        if (bin == null) throw new UsageException("Invalid arguments, choose an action: --bin or --app");
        name ??= DefaultName;
        exportPath ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin");
        var shim = Path.Combine(exportPath, Path.GetFileName(bin.TrimEnd('/')) + ".cmd");
        if (delete)
        {
            if (File.Exists(shim)) File.Delete(shim);
            Console.WriteLine($"{bin} from {name} removed successfully from {exportPath}.");
            return 0;
        }
        Directory.CreateDirectory(exportPath);
        var extra = extraFlags.Length > 0 ? " " + extraFlags : "";
        File.WriteAllText(shim, $"@echo off\r\nrem distrobox_binary\r\nrem name: {name}\r\nwsl.exe -d {Sandbox.Quote(name)} -- {bin}{extra} %*\r\n", new UTF8Encoding(false));
        Console.WriteLine($"{bin} from {name} exported successfully in {exportPath}.\nOK!");
        return 0;
    }

    // ---- wsl.exe ----

    static string WslPath => Sandbox.FindProgram(Path.Combine(Environment.SystemDirectory, "wsl.exe"));

    static void RequireWsl()
    {
        if (WslPath == null) throw new InvalidOperationException("WSL isn't installed (run `wsl --install` in an administrator prompt first)");
    }

    static string WslLine(string[] args) => "wsl.exe " + string.Join(' ', args.Select(Sandbox.Quote));

    // Runs wsl.exe on the console.
    static int Wsl(string[] args)
    {
        var start = new ProcessStartInfo(WslPath) { UseShellExecute = false };
        start.Environment["WSL_UTF8"] = "1";
        foreach (var a in args) start.ArgumentList.Add(a);
        using var p = Process.Start(start);
        p.WaitForExit();
        return p.ExitCode;
    }

    // WSL_UTF8=1 makes wsl.exe write UTF-8 instead of UTF-16 when its output is redirected.
    static (int Code, string Output) WslCapture(string[] args)
    {
        var start = new ProcessStartInfo(WslPath)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
        };
        start.Environment["WSL_UTF8"] = "1";
        foreach (var a in args) start.ArgumentList.Add(a);
        using var p = Process.Start(start);
        var stderr = p.StandardError.ReadToEndAsync();
        var output = p.StandardOutput.ReadToEnd();
        if (!p.WaitForExit(30000)) { p.Kill(); return (-1, ""); }
        _ = stderr.Result;
        return (p.ExitCode, output.Replace("\0", "").Replace("\r", ""));
    }
}
