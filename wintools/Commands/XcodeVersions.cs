using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WinTools;

// xcodes / xcenv: Xcode version management, where Windows' Xcode is Visual Studio (IDE, compilers,
// SDKs and the developer command prompt). Installed instances come from the Visual Studio Setup
// Configuration COM API (falling back to the instance state.json files it reads). The selected
// version lives in xcenv's global version file (%XCENV_ROOT%\version, default ~\.xcenv\version), so
// `xcodes select` and `xcenv global` agree; `xcenv local` writes .xcode-version and `xcenv exec`
// runs a command inside that Visual Studio's developer environment (VsDevCmd.bat).
static unsafe class XcodeVersions
{
    const string XcodesVersion = "2.1.0";
    const string XcenvVersion = "1.2.0";

    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("xcodes", "Install, list and switch between Visual Studio versions (Windows' Xcode)", Xcodes);
        Commands.Add("xcenv", "Per-project Visual Studio version manager (rbenv-style, Windows' Xcode)", Xcenv);
    }

    // ---- Visual Studio instances --------------------------------------------------------------------

    internal sealed class XcVsInstance
    {
        public string Id, Path, Version, DisplayName;
        public DateTime InstallDate;
        public int Major => int.TryParse(Version.Split('.')[0], out var m) ? m : 0;
        public string ShortVersion => string.Join('.', Version.Split('.').Take(2));
        public string Year => YearOf(Major);
        public string Edition => Regex.Match(DisplayName ?? "", @"(Community|Professional|Enterprise|Build Tools|Team Explorer|Test Agent)").Value;
    }

    static string YearOf(int major) => major switch { 15 => "2017", 16 => "2019", 17 => "2022", 18 => "2026", _ => "" };
    static int MajorOf(string year) => year switch { "2017" => 15, "2019" => 16, "2022" => 17, "2026" => 18, _ => 0 };

    internal static List<XcVsInstance> Instances()
    {
        var list = ComInstances() ?? StateFileInstances();
        return list.OrderByDescending(i => System.Version.TryParse(i.Version, out var v) ? v : new Version()).ToList();
    }

    // Matches "17", "17.14", "17.14.36518.9", "2022", an instance id, or "Community 2022"-style names.
    internal static XcVsInstance Match(IEnumerable<XcVsInstance> instances, string spec)
    {
        spec = spec.Trim();
        foreach (var i in instances)
        {
            if (i.Version == spec || i.Id.Equals(spec, StringComparison.OrdinalIgnoreCase)) return i;
        }
        foreach (var i in instances)
        {
            if (i.Version.StartsWith(spec + ".", StringComparison.Ordinal) || i.Year == spec) return i;
            if (i.DisplayName != null && i.DisplayName.Contains(spec, StringComparison.OrdinalIgnoreCase)) return i;
        }
        return null;
    }

    static readonly Guid CLSID_SetupConfiguration = new("177F0C4A-1CD3-4DE7-A32C-71DBBB9FA36D");
    static readonly Guid IID_ISetupConfiguration2 = new("26AAB78C-4A60-49D6-AF3B-3C35BC93365D");

    // ISetupConfiguration2::EnumAllInstances -> IEnumSetupInstances::Next -> ISetupInstance getters,
    // called through their vtables (built-in COM interop is off under NativeAOT).
    static List<XcVsInstance> ComInstances()
    {
        CoInitializeEx(IntPtr.Zero, 0);
        var clsid = CLSID_SetupConfiguration;
        var iid = IID_ISetupConfiguration2;
        if (CoCreateInstance(ref clsid, IntPtr.Zero, 1, ref iid, out var config) != 0 || config == IntPtr.Zero) return null;
        var list = new List<XcVsInstance>();
        IntPtr e = IntPtr.Zero;
        try
        {
            // ISetupConfiguration2 vtable: 3 EnumInstances, 4 GetInstanceForCurrentProcess, 5 GetInstanceForPath, 6 EnumAllInstances
            if (((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)VTable(config, 6))(config, &e) != 0 || e == IntPtr.Zero) return list;
            while (true)
            {
                IntPtr instance = IntPtr.Zero;
                uint fetched = 0;
                // IEnumSetupInstances: 3 Next(celt, rgelt, pceltFetched)
                var hr = ((delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr*, uint*, int>)VTable(e, 3))(e, 1, &instance, &fetched);
                if (hr != 0 || fetched == 0) break;
                try
                {
                    // ISetupInstance: 3 GetInstanceId, 4 GetInstallDate, 5 GetInstallationName, 6 GetInstallationPath,
                    // 7 GetInstallationVersion, 8 GetDisplayName(lcid)
                    long fileTime = 0;
                    ((delegate* unmanaged[Stdcall]<IntPtr, long*, int>)VTable(instance, 4))(instance, &fileTime);
                    list.Add(new XcVsInstance
                    {
                        Id = Bstr(instance, 3),
                        Path = Bstr(instance, 6),
                        Version = Bstr(instance, 7) ?? "0.0",
                        DisplayName = BstrLcid(instance, 8, 0x409),
                        InstallDate = fileTime > 0 ? DateTime.FromFileTime(fileTime) : default,
                    });
                }
                finally { Marshal.Release(instance); }
            }
            return list;
        }
        finally
        {
            if (e != IntPtr.Zero) Marshal.Release(e);
            Marshal.Release(config);
        }
    }

    static IntPtr VTable(IntPtr obj, int slot) => (*(IntPtr**)obj)[slot];

    static string Bstr(IntPtr obj, int slot)
    {
        IntPtr b = IntPtr.Zero;
        if (((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)VTable(obj, slot))(obj, &b) != 0 || b == IntPtr.Zero) return null;
        try { return Marshal.PtrToStringBSTR(b); } finally { Marshal.FreeBSTR(b); }
    }

    static string BstrLcid(IntPtr obj, int slot, uint lcid)
    {
        IntPtr b = IntPtr.Zero;
        if (((delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr*, int>)VTable(obj, slot))(obj, lcid, &b) != 0 || b == IntPtr.Zero) return null;
        try { return Marshal.PtrToStringBSTR(b); } finally { Marshal.FreeBSTR(b); }
    }

    // The same data, read from %ProgramData%\Microsoft\VisualStudio\Packages\_Instances\<id>\state.json.
    static List<XcVsInstance> StateFileInstances()
    {
        var list = new List<XcVsInstance>();
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "VisualStudio", "Packages", "_Instances");
        if (!Directory.Exists(root)) return list;
        foreach (var dir in Directory.GetDirectories(root))
        {
            var state = Path.Combine(dir, "state.json");
            if (!File.Exists(state)) continue;
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(state));
                var r = doc.RootElement;
                string Str(JsonElement el, string name) => el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
                string title = null;
                if (r.TryGetProperty("localizedResources", out var res) && res.ValueKind == JsonValueKind.Array)
                    title = res.EnumerateArray().Select(x => Str(x, "title")).FirstOrDefault(t => t != null);
                list.Add(new XcVsInstance
                {
                    Id = Path.GetFileName(dir),
                    Path = Str(r, "installationPath"),
                    Version = Str(r, "installationVersion") ?? "0.0",
                    DisplayName = title,
                    InstallDate = DateTime.TryParse(Str(r, "installDate"), out var d) ? d : default,
                });
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) { }
        }
        return list;
    }

    // ---- selection (shared by xcodes and xcenv) ----------------------------------------------------

    static string XcenvRoot => Environment.GetEnvironmentVariable("XCENV_ROOT") is { Length: > 0 } r
        ? r : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".xcenv");
    static string GlobalVersionFile => Path.Combine(XcenvRoot, "version");

    static string ReadVersionFile(string path)
    {
        try { return File.Exists(path) ? File.ReadAllText(path).Split((char[])['\r', '\n', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() : null; }
        catch (IOException) { return null; }
    }

    // XCENV_VERSION, then .xcode-version up the directory tree, then the global file.
    static (string Version, string Origin) CurrentVersion()
    {
        if (Environment.GetEnvironmentVariable("XCENV_VERSION") is { Length: > 0 } env) return (env, "XCENV_VERSION environment variable");
        if (LocalVersionFile() is { } local && ReadVersionFile(local) is { } v) return (v, local);
        return (ReadVersionFile(GlobalVersionFile) ?? "system", GlobalVersionFile);
    }

    static string LocalVersionFile()
    {
        for (var dir = new DirectoryInfo(Environment.CurrentDirectory); dir != null; dir = dir.Parent)
        {
            var file = Path.Combine(dir.FullName, ".xcode-version");
            if (File.Exists(file)) return file;
        }
        return null;
    }

    // "system" means the newest installed Visual Studio.
    static XcVsInstance Resolve(List<XcVsInstance> instances, string version) =>
        version is null or "system" ? instances.FirstOrDefault() : Match(instances, version);

    // ---- xcodes -----------------------------------------------------------------------------------

    // Release lines and their latest version known to this build; `xcodes list` refreshes them from
    // the Visual Studio channel manifests when online.
    static readonly (int Major, string Latest, string Channel)[] KnownReleases =
    [
        (15, "15.9", "release"), (16, "16.11", "release"), (17, "17.14", "release"), (18, "18.11", "stable"),
    ];

    static int Xcodes(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help") { XcodesHelp(); return 0; }
        var rest = args[1..];
        switch (args[0])
        {
            case "--version" or "version": Console.WriteLine(XcodesVersion); return 0;
            case "list" or "update": return XcodesList(rest, args[0] == "update");
            case "installed": return XcodesInstalled(rest);
            case "select": return XcodesSelect(rest);
            case "install": return XcodesInstall(rest);
            case "uninstall": return XcodesUninstall(rest);
            case "runtimes": return XcodesRuntimes(rest);
            case "signout":
                Console.WriteLine("Visual Studio downloads need no Apple ID; nothing to sign out of.");
                return 0;
            default: throw new UsageException($"unknown subcommand '{args[0]}' (see xcodes --help)");
        }
    }

    static int XcodesList(string[] args, bool update)
    {
        var online = !args.Contains("--offline");
        var instances = Instances();
        var selected = Resolve(instances, ReadVersionFile(GlobalVersionFile));
        foreach (var (major, known, channel) in KnownReleases)
        {
            var latest = online ? ChannelVersion(major, channel) ?? known : known;
            var installed = instances.Where(i => i.Major == major).ToList();
            var tags = new List<string>();
            if (installed.Count > 0) tags.Add("Installed");
            if (selected != null && selected.Major == major) tags.Add("Selected");
            Console.WriteLine($"{latest} (Visual Studio {YearOf(major)}){(tags.Count > 0 ? $" ({string.Join(", ", tags)})" : "")}");
        }
        if (update) Console.Error.WriteLine("Release list refreshed from the Visual Studio channel manifests.");
        return 0;
    }

    // https://aka.ms/vs/<major>/<channel>/channel -> info.productDisplayVersion
    static string ChannelVersion(int major, string channel)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
            var json = http.GetStringAsync($"https://aka.ms/vs/{major}/{channel}/channel").GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("info", out var info) && info.TryGetProperty("productDisplayVersion", out var v))
                return v.GetString();
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException) { }
        return null;
    }

    static int XcodesInstalled(string[] args)
    {
        var instances = Instances();
        if (args.Length > 0 && !args[0].StartsWith('-'))
        {
            var match = Match(instances, args[0]);
            if (match == null) { Console.Error.WriteLine($"Visual Studio {args[0]} is not installed."); return 1; }
            Console.WriteLine(match.Path);
            return 0;
        }
        var selected = Resolve(instances, ReadVersionFile(GlobalVersionFile));
        foreach (var i in instances)
            Console.WriteLine($"{i.Version} ({i.DisplayName ?? "Visual Studio " + i.Year}){(i == selected ? " (Selected)" : "")}\t{i.Path}");
        return 0;
    }

    static int XcodesSelect(string[] args)
    {
        var instances = Instances();
        if (args.Contains("-p") || args.Contains("--print-path"))
        {
            var current = Resolve(instances, ReadVersionFile(GlobalVersionFile));
            if (current == null) { Console.Error.WriteLine("No Visual Studio is installed."); return 1; }
            Console.WriteLine(current.Path);
            return 0;
        }
        var spec = args.FirstOrDefault(a => !a.StartsWith('-'));
        if (spec == null)
        {
            // Interactive pick, like xcodes' numbered menu.
            if (instances.Count == 0) { Console.Error.WriteLine("No Visual Studio is installed."); return 1; }
            for (var n = 0; n < instances.Count; n++) Console.WriteLine($"{n + 1}) {instances[n].Version} ({instances[n].DisplayName})");
            Console.Write("Select a number: ");
            if (!int.TryParse(Console.ReadLine(), out var pick) || pick < 1 || pick > instances.Count) throw new UsageException("not a listed number");
            spec = instances[pick - 1].Version;
        }
        var match = spec == "system" ? instances.FirstOrDefault() : Match(instances, spec);
        if (match == null) { Console.Error.WriteLine($"Visual Studio {spec} is not installed. Install it with: xcodes install {spec}"); return 1; }
        Directory.CreateDirectory(XcenvRoot);
        File.WriteAllText(GlobalVersionFile, (spec == "system" ? "system" : match.Version) + "\n");
        Console.WriteLine($"Selected {match.DisplayName ?? "Visual Studio " + match.Year} {match.Version} at {match.Path}");
        return 0;
    }

    // Visual Studio's own bootstrapper (aka.ms/vs/<major>/<channel>/vs_<edition>.exe) does the install.
    static int XcodesInstall(string[] args)
    {
        string spec = null, edition = "community";
        bool select = false, prerelease = false, latest = false;
        var extra = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--latest": latest = true; break;
                case "--latest-prerelease": latest = prerelease = true; break;
                case "--select": select = true; break;
                case "--edition": edition = i + 1 < args.Length ? args[++i].ToLowerInvariant() : throw new UsageException("--edition needs a value"); break;
                case "--add": extra.Add("--add"); extra.Add(i + 1 < args.Length ? args[++i] : throw new UsageException("--add needs a workload id")); break;
                default:
                    if (args[i].StartsWith('-')) throw new UsageException($"unknown option {args[i]}");
                    spec = args[i];
                    break;
            }
        }
        if (spec == null && !latest) throw new UsageException("give a version (e.g. 17 or 2022) or --latest");
        var major = spec == null ? KnownReleases[^1].Major
            : MajorOf(spec) is > 0 and var m ? m
            : int.TryParse(spec.Split('.')[0], out var n) && KnownReleases.Any(r => r.Major == n) ? n
            : throw new UsageException($"unknown Visual Studio version {spec} (try 2017, 2019, 2022 or 2026)");
        var file = edition switch
        {
            "community" or "professional" or "enterprise" => $"vs_{edition}.exe",
            "buildtools" or "build-tools" => "vs_buildtools.exe",
            _ => throw new UsageException("--edition is community, professional, enterprise or buildtools"),
        };
        var channel = prerelease ? (major >= 18 ? "insiders" : "pre") : KnownReleases.First(r => r.Major == major).Channel;
        var url = $"https://aka.ms/vs/{major}/{channel}/{file}";
        var bootstrapper = Path.Combine(Path.GetTempPath(), $"xcodes-{major}-{file}");
        Console.WriteLine($"Downloading {url}");
        using (var http = new HttpClient())
        {
            try { File.WriteAllBytes(bootstrapper, http.GetByteArrayAsync(url).GetAwaiter().GetResult()); }
            catch (HttpRequestException e) { throw new IOException($"download failed: {e.Message}"); }
        }
        Console.WriteLine($"Installing Visual Studio {YearOf(major)} ({edition}); the Visual Studio Installer shows progress.");
        var psi = new ProcessStartInfo(bootstrapper) { UseShellExecute = false };
        foreach (var a in new[] { "--passive", "--wait", "--norestart" }.Concat(extra)) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi);
        p.WaitForExit();
        // 3010: success, reboot required.
        if (p.ExitCode is not (0 or 3010)) { Console.Error.WriteLine($"xcodes: the Visual Studio Installer failed (exit {p.ExitCode})"); return 1; }
        if (select) return XcodesSelect([major.ToString()]);
        return 0;
    }

    static int XcodesUninstall(string[] args)
    {
        var spec = args.FirstOrDefault(a => !a.StartsWith('-')) ?? throw new UsageException("give the version to uninstall");
        var match = Match(Instances(), spec);
        if (match == null) { Console.Error.WriteLine($"Visual Studio {spec} is not installed."); return 1; }
        var setup = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Visual Studio", "Installer", "setup.exe");
        if (!File.Exists(setup)) throw new IOException($"the Visual Studio Installer was not found at {setup}");
        var psi = new ProcessStartInfo(setup) { UseShellExecute = false };
        foreach (var a in new[] { "uninstall", "--installPath", match.Path, "--passive", "--norestart" }) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi);
        p.WaitForExit();
        return p.ExitCode is 0 or 3010 ? 0 : 1;
    }

    // Simulator runtimes' counterpart: the Windows SDKs the toolchains build against.
    static int XcodesRuntimes(string[] args)
    {
        if (args.Length > 0 && args[0] == "install")
        {
            Console.Error.WriteLine("Windows SDKs are Visual Studio components: add one with `xcodes install <version> --add Microsoft.VisualStudio.Component.Windows11SDK.26100`.");
            return 1;
        }
        var include = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Windows Kits", "10", "Include");
        Console.WriteLine("-- Windows SDKs --");
        if (Directory.Exists(include))
            foreach (var dir in Directory.GetDirectories(include).Select(Path.GetFileName).Where(d => d.StartsWith("10.")).OrderBy(d => new Version(d)))
                Console.WriteLine($"Windows SDK {dir} (Installed)");
        return 0;
    }

    static void XcodesHelp() => Console.WriteLine("""
        OVERVIEW: Manage the Visual Studio versions installed on this PC (Windows' Xcode).

        USAGE: xcodes <subcommand>

        SUBCOMMANDS:
          list [--offline]        List Visual Studio releases, marking installed and selected ones
          installed [<version>]   List installed instances (or print one's path)
          install <version>       Install with Visual Studio's bootstrapper
                                  [--latest] [--latest-prerelease] [--select]
                                  [--edition community|professional|enterprise|buildtools] [--add <workload>]
          uninstall <version>     Remove an instance with the Visual Studio Installer
          select [<version>] [-p] Select the default instance (shared with `xcenv global`)
          runtimes                List installed Windows SDKs
          update                  Refresh the release list
          version                 Print xcodes' version

        Versions are 17, 17.14, 17.14.36518.9, a year (2022) or an instance id.
        """);

    // ---- xcenv ------------------------------------------------------------------------------------

    static int Xcenv(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help") { XcenvHelp(); return 0; }
        var rest = args[1..];
        switch (args[0])
        {
            case "--version" or "-v": Console.WriteLine($"xcenv {XcenvVersion}"); return 0;
            case "init": return XcenvInit(rest);
            case "root": Console.WriteLine(XcenvRoot); return 0;
            case "commands":
                foreach (var c in new[] { "commands", "exec", "global", "help", "init", "local", "prefix", "root", "shell", "version", "version-file", "version-name", "versions", "which" })
                    Console.WriteLine(c);
                return 0;
            case "versions": return XcenvVersions(rest);
            case "version":
            {
                var (v, origin) = CurrentVersion();
                Console.WriteLine($"{v} (set by {origin})");
                return Resolve(Instances(), v) != null || v == "system" ? 0 : 1;
            }
            case "version-name": Console.WriteLine(CurrentVersion().Version); return 0;
            case "version-file": Console.WriteLine(Environment.GetEnvironmentVariable("XCENV_VERSION") is { Length: > 0 } ? "" : LocalVersionFile() ?? GlobalVersionFile); return 0;
            case "global": return XcenvSetFile(GlobalVersionFile, rest, "global");
            case "local": return XcenvSetFile(Path.Combine(Environment.CurrentDirectory, ".xcode-version"), rest, "local");
            case "shell":
                // The shell function from `xcenv init` evaluates this output.
                if (rest.Length == 0) { Console.WriteLine(Environment.GetEnvironmentVariable("XCENV_VERSION") ?? "xcenv: no shell-specific version configured"); return 0; }
                RequireInstalled(rest[0]);
                Console.WriteLine(rest[0] == "--unset" ? "unset XCENV_VERSION" : $"export XCENV_VERSION=\"{rest[0]}\"");
                return 0;
            case "prefix":
            {
                var instance = RequireInstalled(rest.Length > 0 ? rest[0] : CurrentVersion().Version);
                Console.WriteLine(instance.Path);
                return 0;
            }
            case "which": return XcenvWhich(rest);
            case "exec": return XcenvExec(rest);
            default: throw new UsageException($"no such command '{args[0]}'");
        }
    }

    static XcVsInstance RequireInstalled(string version)
    {
        if (version == "--unset") return null;
        var instance = Resolve(Instances(), version);
        return instance ?? throw new InvalidOperationException(version == "system"
            ? "no Visual Studio is installed (install one with `xcodes install --latest`)"
            : $"version '{version}' not installed");
    }

    static int XcenvInit(string[] args)
    {
        var shell = args.FirstOrDefault(a => a != "-") ?? (Environment.GetEnvironmentVariable("PSModulePath") != null && Environment.GetEnvironmentVariable("SHELL") == null ? "pwsh" : "bash");
        if (!args.Contains("-"))
        {
            Console.WriteLine(shell is "pwsh" or "powershell"
                ? "# Load xcenv automatically by appending this to your PowerShell $PROFILE:\n\nxcenv init - pwsh | Out-String | Invoke-Expression"
                : "# Load xcenv automatically by appending this to ~/.bashrc:\n\neval \"$(xcenv init -)\"");
            return 0;
        }
        if (shell is "pwsh" or "powershell")
            Console.WriteLine("""
                $env:XCENV_SHELL = 'pwsh'
                function global:xcenv {
                  $exe = (Get-Command xcenv -CommandType Application | Select-Object -First 1).Source
                  if ($args.Count -gt 0 -and $args[0] -eq 'shell') {
                    if ($args.Count -eq 1) { & $exe @args; return }
                    if ($args[1] -eq '--unset') { Remove-Item Env:XCENV_VERSION -ErrorAction SilentlyContinue; return }
                    & $exe shell $args[1] *> $null; if ($LASTEXITCODE -eq 0) { $env:XCENV_VERSION = $args[1] } else { & $exe shell $args[1] }
                  } else { & $exe @args }
                }
                """);
        else
            Console.WriteLine("""
                export XCENV_SHELL=bash
                xcenv() {
                  local command="${1:-}"
                  if [ "$command" = "shell" ] && [ "$#" -gt 1 ]; then
                    shift
                    eval "$(command xcenv shell "$@")"
                  else
                    command xcenv "$@"
                  fi
                }
                """);
        return 0;
    }

    static int XcenvVersions(string[] args)
    {
        var bare = args.Contains("--bare");
        var (current, origin) = CurrentVersion();
        var instances = Instances();
        var active = Resolve(instances, current);
        if (!bare && instances.Count > 0)
            Console.WriteLine($"{(current == "system" ? "* " : "  ")}system{(current == "system" ? $" (set by {origin})" : "")}");
        foreach (var i in instances)
        {
            if (bare) { Console.WriteLine(i.Version); continue; }
            var mark = current != "system" && i == active;
            Console.WriteLine($"{(mark ? "* " : "  ")}{i.Version} ({i.DisplayName ?? "Visual Studio " + i.Year}){(mark ? $" (set by {origin})" : "")}");
        }
        if (!bare && instances.Count == 0) Console.Error.WriteLine("xcenv: no Visual Studio instances are installed");
        return 0;
    }

    static int XcenvSetFile(string file, string[] args, string scope)
    {
        if (args.Length == 0)
        {
            var v = ReadVersionFile(file);
            if (v == null) { Console.Error.WriteLine(scope == "local" ? "xcenv: no local version configured for this directory" : "system"); return scope == "local" ? 1 : 0; }
            Console.WriteLine(v);
            return 0;
        }
        if (args[0] == "--unset") { if (File.Exists(file)) File.Delete(file); return 0; }
        if (args[0] != "system") RequireInstalled(args[0]);
        Directory.CreateDirectory(Path.GetDirectoryName(file));
        File.WriteAllText(file, args[0] + "\n");
        return 0;
    }

    static int XcenvWhich(string[] args)
    {
        if (args.Length == 0) throw new UsageException("usage: xcenv which <command>");
        var env = DevEnvironment(RequireInstalled(CurrentVersion().Version));
        var name = args[0];
        foreach (var dir in env.GetValueOrDefault("PATH", "").Split(';', StringSplitOptions.RemoveEmptyEntries))
            foreach (var ext in new[] { "", ".exe", ".cmd", ".bat" })
            {
                var full = Path.Combine(dir, name + ext);
                if (File.Exists(full)) { Console.WriteLine(full); return 0; }
            }
        Console.Error.WriteLine($"xcenv: {name}: command not found");
        return 127;
    }

    static int XcenvExec(string[] args)
    {
        if (args.Length == 0) throw new UsageException("usage: xcenv exec <command> [arg1 arg2...]");
        var instance = RequireInstalled(CurrentVersion().Version);
        var psi = new ProcessStartInfo(args[0]) { UseShellExecute = false };
        foreach (var a in args[1..]) psi.ArgumentList.Add(a);
        foreach (var (k, v) in DevEnvironment(instance)) psi.Environment[k] = v;
        psi.Environment["DEVELOPER_DIR"] = instance.Path;
        psi.Environment["XCENV_VERSION"] = instance.Version;
        try
        {
            using var p = Process.Start(psi);
            p.WaitForExit();
            return p.ExitCode;
        }
        catch (System.ComponentModel.Win32Exception) { Console.Error.WriteLine($"xcenv: {args[0]}: command not found"); return 127; }
    }

    // The environment VsDevCmd.bat sets up: run it in cmd and read back `set`.
    static Dictionary<string, string> DevEnvironment(XcVsInstance instance)
    {
        var devcmd = Path.Combine(instance.Path, "Common7", "Tools", "VsDevCmd.bat");
        if (!File.Exists(devcmd)) throw new IOException($"{devcmd} not found");
        var arch = RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "arm64" : "amd64";
        var psi = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe"))
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
            Arguments = $"/d /s /c \"\"{devcmd}\" -arch={arch} -host_arch={arch} -no_logo >nul && set\"",
        };
        using var p = Process.Start(psi);
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        if (p.ExitCode != 0) throw new InvalidOperationException($"VsDevCmd.bat failed (exit {p.ExitCode})");
        var env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in output.Split('\n'))
        {
            var eq = line.IndexOf('=');
            if (eq > 0) env[line[..eq]] = line[(eq + 1)..].TrimEnd('\r');
        }
        return env;
    }

    static void XcenvHelp() => Console.WriteLine("""
        Usage: xcenv <command> [<args>]

        Selects the Visual Studio (Windows' Xcode) to use, globally or per project.

        Some useful xcenv commands are:
           commands       List all available xcenv commands
           exec           Run a command inside the selected Visual Studio's developer environment
           global         Set or show the global Visual Studio version
           local          Set or show the local (per-directory) version (.xcode-version)
           shell          Set or show the shell-specific version
           version        Show the current version and its origin
           versions       List all installed Visual Studio versions
           prefix         Show the installation path of the current version
           which          Show the full path to a developer tool for the current version
           init           Print shell integration (bash, or pwsh: `xcenv init - pwsh`)

        Versions are 17, 17.14, a full version, a year (2022), an instance id, or "system" (the newest).
        """);

    [DllImport("ole32.dll")] static extern int CoInitializeEx(IntPtr reserved, uint coinit);
    [DllImport("ole32.dll")] static extern int CoCreateInstance(ref Guid clsid, IntPtr outer, uint ctx, ref Guid iid, out IntPtr obj);
}
