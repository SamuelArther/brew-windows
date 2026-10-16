using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace WinTools;

// kmod (lsmod, modinfo, insmod, rmmod, modprobe, depmod) for Windows kernel drivers, the Windows
// counterpart of Linux kernel modules. The loaded-module list is the kernel's own
// (NtQuerySystemInformation SystemModuleInformation: name, size, load order); "Used by" and depends
// come from each driver's PE import table (a driver that imports from another depends on it, just as a
// module that uses another's exported symbols does). Drivers are loaded and unloaded as kernel-driver
// services through the Service Control Manager: insmod registers one for a .sys file and starts it,
// modprobe starts an already registered driver by name, rmmod / modprobe -r stop it. Disabled driver
// services are what modprobe -c reports as blacklisted.
static class Kmod
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("kmod", "Linux kmod's multi-call tool for Windows kernel drivers (list, help, insert, remove)", RunKmod);
        Commands.Add("lsmod", "Show the loaded Windows kernel drivers", args => Lsmod(args, "lsmod"));
        Commands.Add("modinfo", "Show information about a Windows kernel driver", Modinfo);
        Commands.Add("insmod", "Load a kernel driver (.sys) as a driver service", Insmod);
        Commands.Add("rmmod", "Unload a kernel driver", Rmmod);
        Commands.Add("modprobe", "Load or unload a registered kernel driver and what it depends on", Modprobe);
        Commands.Add("depmod", "Generate modules.dep from the drivers' import tables", Depmod);
    }

    const string KmodVersion = "34.2";
    static readonly string System32 = Environment.SystemDirectory;
    static readonly string Drivers = Path.Combine(Environment.SystemDirectory, "drivers");

    sealed record Loaded(string Name, string Path, uint Size, int LoadOrder);

    // ---- kmod ----

    static int RunKmod(string[] args)
    {
        if (args.Length == 0) { KmodHelp(Console.Error); return 1; }
        switch (args[0])
        {
            case "-h" or "--help" or "help": KmodHelp(Console.Out); return 0;
            case "-V" or "--version":
                Console.WriteLine($"kmod version {KmodVersion}\n-ZSTD -XZ -ZLIB -LIBCRYPTO -EXPERIMENTAL (wintools {Commands.Version}, Windows kernel drivers)");
                return 0;
            case "list": return Lsmod(args[1..], "kmod list");
            case "insert": return Modprobe(args[1..]);
            case "remove": return Modprobe(["-r", .. args[1..]]);
            case "static-nodes":
                Console.Error.WriteLine("kmod: static-nodes: Windows drivers create their device objects themselves; there are no /dev nodes to pre-create");
                return 1;
            default: throw new UsageException($"unknown command '{args[0]}' (see kmod help)");
        }
    }

    static void KmodHelp(TextWriter w) => w.WriteLine("""
        kmod - Manage kernel modules: list, load, unload, etc
        Usage:
        	kmod [options] command [command_options]

        Options:
        	-V, --version     show version
        	-h, --help        show this help

        Commands:
          help         Show help message
          list         list currently loaded modules
          static-nodes outputs the static-node information installed with the currently running kernel
          insert       insert a module into the kernel
          remove       remove a module from the kernel

        kmod also handles gracefully if called from following symlinks:
          lsmod        compat lsmod command
          rmmod        compat rmmod command
          insmod       compat insmod command
          modinfo      compat modinfo command
          modprobe     compat modprobe command
          depmod       compat depmod command

        On Windows the modules are kernel drivers (.sys), loaded as driver services.
        """);

    // ---- lsmod ----

    static int Lsmod(string[] args, string name)
    {
        foreach (var a in args)
        {
            if (a is "-h" or "--help") { Console.WriteLine($"Usage: {name}\nShow the loaded kernel drivers: name, image size, users and the drivers that use them."); return 0; }
            if (a is "-V" or "--version") { Console.WriteLine($"kmod version {KmodVersion}"); return 0; }
            throw new UsageException($"unrecognized option '{a}'");
        }
        var modules = LoadedModules();
        var users = UsersOf(modules);
        Console.WriteLine("Module                  Size  Used by");
        // Linux lists the most recently loaded module first.
        foreach (var m in modules.OrderByDescending(m => m.LoadOrder))
        {
            var holders = users.GetValueOrDefault(m.Name.ToLowerInvariant()) ?? [];
            var line = $"{m.Name,-19} {m.Size,8}  {holders.Count}";
            if (holders.Count > 0) line += " " + string.Join(",", holders);
            Console.WriteLine(line);
        }
        return 0;
    }

    // Module name as lsmod shows it: the file name without .sys (ntoskrnl.exe and DLLs keep theirs).
    static string ModuleName(string file)
    {
        var name = Path.GetFileName(file);
        return name.EndsWith(".sys", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
    }

    static unsafe List<Loaded> LoadedModules()
    {
        var size = 1 << 16;
        while (true)
        {
            var buffer = new byte[size];
            fixed (byte* p = buffer)
            {
                var status = NtQuerySystemInformation(SystemModuleInformation, p, size, out var needed);
                if (status == STATUS_INFO_LENGTH_MISMATCH) { size = Math.Max(size * 2, needed + 4096); continue; }
                if (status != 0) throw new InvalidOperationException($"cannot list kernel modules (NTSTATUS 0x{status:x8})");
                var count = *(uint*)p;
                var list = new List<Loaded>();
                const int entrySize = 296;   // RTL_PROCESS_MODULE_INFORMATION on 64-bit Windows
                for (var i = 0; i < count; i++)
                {
                    var e = p + 8 + i * entrySize;
                    var imageSize = *(uint*)(e + 24);
                    var loadOrder = *(ushort*)(e + 32);
                    var nameOffset = *(ushort*)(e + 38);
                    var raw = new ReadOnlySpan<byte>(e + 40, 256);
                    var length = raw.IndexOf((byte)0);
                    var full = Encoding.ASCII.GetString(length < 0 ? raw : raw[..length]);
                    var path = Win32Path(full);
                    list.Add(new Loaded(ModuleName(full[Math.Min(nameOffset, full.Length)..]), path, imageSize, loadOrder));
                }
                return list;
            }
        }
    }

    // "\SystemRoot\system32\drivers\x.sys", "\??\C:\...", "System32\drivers\x.sys" -> a Win32 path.
    static string Win32Path(string native)
    {
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (native.StartsWith(@"\SystemRoot\", StringComparison.OrdinalIgnoreCase)) return Path.Combine(windows, native[12..]);
        if (native.StartsWith(@"\??\")) return native[4..];
        if (native.StartsWith(@"\Windows\", StringComparison.OrdinalIgnoreCase)) return Path.Combine(windows, native[9..]);
        if (native.StartsWith(@"system32\", StringComparison.OrdinalIgnoreCase)) return Path.Combine(windows, native);
        if (native.StartsWith('\\')) return Path.GetPathRoot(windows) + native.TrimStart('\\');
        return Path.IsPathRooted(native) ? native : Path.Combine(windows, native);
    }

    // Lower-cased module name -> names of the loaded modules that import from it.
    static Dictionary<string, List<string>> UsersOf(List<Loaded> modules)
    {
        var users = new Dictionary<string, List<string>>();
        foreach (var m in modules)
            foreach (var dep in Dependencies(m.Path))
            {
                var key = ModuleName(dep).ToLowerInvariant();
                if (!users.TryGetValue(key, out var list)) users[key] = list = [];
                if (!list.Contains(m.Name)) list.Add(m.Name);
            }
        return users;
    }

    // DLL names a driver imports, except the kernel and HAL every driver links against.
    static List<string> Dependencies(string path)
    {
        try
        {
            return Abi.PeFile.Open(path).Imports.Select(i => i.Dll)
                .Where(d => !d.Equals("ntoskrnl.exe", StringComparison.OrdinalIgnoreCase) && !d.Equals("hal.dll", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException) { return []; }
    }

    static string ResolveImport(string dll)
    {
        foreach (var dir in new[] { Drivers, System32 })
        {
            var candidate = Path.Combine(dir, dll);
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    // ---- driver services ----

    sealed record Service(string Name, string ImagePath, int Start, int Type, string DisplayName, string Group);

    static List<Service> DriverServices()
    {
        var list = new List<Service>();
        using var services = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services");
        if (services == null) return list;
        foreach (var name in services.GetSubKeyNames())
        {
            using var key = services.OpenSubKey(name);
            if (key?.GetValue("Type") is not int type || (type & 0x3) == 0) continue;   // kernel (1) or file system (2) driver
            var image = key.GetValue("ImagePath", null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string;
            image = image == null ? Path.Combine(Drivers, name + ".sys") : Win32Path(Environment.ExpandEnvironmentVariables(image.Trim('"')));
            list.Add(new Service(name, image, key.GetValue("Start") is int s ? s : 3, type, key.GetValue("DisplayName") as string, key.GetValue("Group") as string));
        }
        return list;
    }

    // A module name, a service name or a path -> (service, driver file). Either can be null.
    static (Service Service, string File) Find(string module, List<Service> services = null)
    {
        services ??= DriverServices();
        if (module.Contains('\\') || module.Contains('/') || module.EndsWith(".sys", StringComparison.OrdinalIgnoreCase) && File.Exists(module))
        {
            var full = Path.GetFullPath(module);
            return (services.FirstOrDefault(s => s.ImagePath.Equals(full, StringComparison.OrdinalIgnoreCase)), File.Exists(full) ? full : null);
        }
        var bare = ModuleName(module).Replace('-', '_');
        bool Match(string n) => n.Equals(module, StringComparison.OrdinalIgnoreCase) || n.Replace('-', '_').Equals(bare, StringComparison.OrdinalIgnoreCase);
        var service = services.FirstOrDefault(s => Match(s.Name)) ?? services.FirstOrDefault(s => Match(ModuleName(s.ImagePath)));
        var file = service != null && File.Exists(service.ImagePath) ? service.ImagePath : null;
        if (file == null)
        {
            var loaded = LoadedModules().FirstOrDefault(m => Match(m.Name));
            file = loaded != null && File.Exists(loaded.Path) ? loaded.Path : null;
        }
        if (file == null && ResolveImport(bare + ".sys") is { } sys) file = sys;
        return (service, file);
    }

    // ---- modinfo ----

    static int Modinfo(string[] args)
    {
        string field = null;
        var zero = false;
        var modules = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            switch (a)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage:
                        	modinfo [options] <modulename|filename> [args]
                        Options:
                        	-a, --author                Print only 'author'
                        	-d, --description           Print only 'description'
                        	-l, --license               Print only 'license'
                        	-p, --parameters            Print only 'parm'
                        	-n, --filename              Print only 'filename'
                        	-0, --null                  Use \0 instead of \n
                        	-F, --field=FIELD           Print only provided FIELD
                        	-k, --set-version=VERSION   Ignored (the running kernel is used)
                        	-b, --basedir=DIR           Ignored
                        	-V, --version               show version
                        	-h, --help                  show this help
                        """);
                    return 0;
                case "-V" or "--version": Console.WriteLine($"kmod version {KmodVersion}"); return 0;
                case "-a" or "--author": field = "author"; break;
                case "-d" or "--description": field = "description"; break;
                case "-l" or "--license": field = "license"; break;
                case "-p" or "--parameters": field = "parm"; break;
                case "-n" or "--filename": field = "filename"; break;
                case "-0" or "--null": zero = true; break;
                case "-F" or "--field": field = ++i < args.Length ? args[i] : throw new UsageException("option requires an argument -- 'F'"); break;
                case "-k" or "--set-version" or "-b" or "--basedir": i++; break;
                default:
                    if (a.StartsWith("--field=")) field = a[8..];
                    else if (a.StartsWith("-F") && a.Length > 2) field = a[2..];
                    else if (a.StartsWith("--set-version=") || a.StartsWith("--basedir=")) { }
                    else if (a.StartsWith('-')) throw new UsageException($"unrecognized option '{a}'");
                    else modules.Add(a);
                    break;
            }
        }
        if (modules.Count == 0) throw new UsageException("missing module or filename.");
        var status = 0;
        var services = DriverServices();
        foreach (var module in modules)
        {
            var (service, file) = Find(module, services);
            if (file == null)
            {
                Console.Error.WriteLine($"modinfo: ERROR: Module {module} not found.");
                status = 1;
                continue;
            }
            var info = FileVersionInfo.GetVersionInfo(file);
            var fields = new List<(string Key, string Value)>
            {
                ("filename", file),
                ("description", info.FileDescription),
                ("author", info.CompanyName),
                ("license", info.LegalCopyright),
                ("version", info.FileVersion),
                ("depends", string.Join(",", Dependencies(file).Select(ModuleName))),
                ("name", ModuleName(file)),
            };
            if (service != null)
            {
                fields.Add(("alias", service.Name));
                if (service.DisplayName != null && !service.DisplayName.StartsWith('@')) fields.Add(("alias", service.DisplayName));
                fields.Add(("intree", "Y"));
                fields.Add(("start", service.Start switch { 0 => "boot", 1 => "system", 2 => "auto", 3 => "demand", 4 => "disabled", _ => service.Start.ToString() }));
                if (service.Group != null) fields.Add(("group", service.Group));
                using var parameters = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{service.Name}\Parameters");
                if (parameters != null)
                    foreach (var p in parameters.GetValueNames().Where(p => p.Length > 0))
                        fields.Add(("parm", $"{p}:{parameters.GetValue(p) switch
                        {
                            byte[] b => Convert.ToHexString(b.AsSpan(0, Math.Min(b.Length, 32))).ToLowerInvariant() + (b.Length > 32 ? "..." : ""),
                            string[] m => string.Join(",", m),
                            var v => v,
                        }} ({parameters.GetValueKind(p)})"));
            }
            var end = zero ? "\0" : "\n";
            foreach (var (key, value) in fields)
            {
                if (value == null) continue;
                if (field != null)
                {
                    if (key.Equals(field, StringComparison.OrdinalIgnoreCase)) Console.Write(value + end);
                    continue;
                }
                Console.Write($"{key + ":",-16}{value}{end}");
            }
        }
        return status;
    }

    // ---- insmod / rmmod / modprobe ----

    static int Insmod(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            if (args.Length == 0) throw new UsageException("missing filename.");
            Console.WriteLine("Usage:\n\tinsmod [options] filename [args]\nOptions:\n\t-V, --version     show version\n\t-h, --help        show this help\n" +
                              "The driver is registered as a demand-start kernel driver service named after the file and started;\nargs (name=value) go to the service's Parameters key.");
            return 0;
        }
        if (args[0] is "-V" or "--version") { Console.WriteLine($"kmod version {KmodVersion}"); return 0; }
        var file = Path.GetFullPath(args[0]);
        if (!File.Exists(file)) { Console.Error.WriteLine($"insmod: ERROR: could not load module {args[0]}: No such file or directory"); return 1; }
        var name = ModuleName(file);
        try { Load(name, file, args[1..], create: true); }
        catch (System.ComponentModel.Win32Exception e)
        {
            Console.Error.WriteLine($"insmod: ERROR: could not insert module {args[0]}: {e.Message}");
            return 1;
        }
        return 0;
    }

    static int Rmmod(string[] args)
    {
        var names = new List<string>();
        var verbose = false;
        foreach (var a in args)
        {
            switch (a)
            {
                case "-h" or "--help":
                    Console.WriteLine("Usage:\n\trmmod [options] modulename ...\nOptions:\n\t-f, --force       accepted\n\t-s, --syslog      accepted\n\t-v, --verbose     enables more messages\n\t-V, --version     show version\n\t-h, --help        show this help");
                    return 0;
                case "-V" or "--version": Console.WriteLine($"kmod version {KmodVersion}"); return 0;
                case "-v" or "--verbose": verbose = true; break;
                case "-f" or "--force" or "-s" or "--syslog": break;
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unrecognized option '{a}'");
                    names.Add(a);
                    break;
            }
        }
        if (names.Count == 0) throw new UsageException("missing module name.");
        var status = 0;
        var services = DriverServices();
        foreach (var name in names)
        {
            var (service, _) = Find(name, services);
            if (service == null || !IsRunning(service.Name))
            {
                Console.Error.WriteLine($"rmmod: ERROR: Module {name} is not currently loaded");
                status = 1;
                continue;
            }
            if (verbose) Console.WriteLine($"rmmod {service.Name}");
            try { Unload(service.Name); }
            catch (System.ComponentModel.Win32Exception e)
            {
                Console.Error.WriteLine($"rmmod: ERROR: could not remove module {name}: {e.Message}");
                status = 1;
            }
        }
        return status;
    }

    static int Modprobe(string[] args)
    {
        bool remove = false, dryRun = false, verbose = false, quiet = false, all = false, showDepends = false, config = false;
        var rest = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (rest.Count > 0 && !all) { rest.Add(a); continue; }   // module parameters follow the name
            switch (a)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage:
                        	modprobe [options] [-i] [-b] modulename
                        	modprobe [options] -a [-i] [-b] modulename [modulename...]
                        	modprobe [options] -r [-i] modulename
                        	modprobe [options] -r -a [-i] modulename [modulename...]
                        	modprobe [options] -c
                        	modprobe [options] --show-depends modulename
                        Options:
                        	-a, --all                   Consider every non-argument to be a module name
                        	-r, --remove                Remove modules instead of inserting
                        	-n, --dry-run               Do not execute operations, just print out
                        	-c, --show-config           Print out known configuration and exit
                        	-D, --show-depends          Only print module dependencies and exit
                        	-q, --quiet                 disable messages
                        	-v, --verbose               enables more messages
                        	-V, --version               show version
                        	-h, --help                  show this help
                        Module names are registered kernel-driver services (or their .sys file names).
                        """);
                    return 0;
                case "-V" or "--version": Console.WriteLine($"kmod version {KmodVersion}"); return 0;
                case "-r" or "--remove": remove = true; break;
                case "-n" or "--dry-run" or "--show": dryRun = true; break;
                case "-v" or "--verbose": verbose = true; break;
                case "-q" or "--quiet": quiet = true; break;
                case "-a" or "--all": all = true; break;
                case "-D" or "--show-depends": showDepends = true; break;
                case "-c" or "--showconfig" or "--show-config": config = true; break;
                case "-i" or "--ignore-install" or "--ignore-remove" or "-b" or "--use-blacklist" or "-f" or "--force"
                    or "--force-vermagic" or "--force-modversion" or "--first-time" or "-s" or "--syslog": break;
                case "-S" or "--set-version" or "-d" or "--dirname" or "-C" or "--config": i++; break;
                default:
                    if (a.StartsWith('-') && rest.Count == 0) throw new UsageException($"unrecognized option '{a}'");
                    rest.Add(a);
                    break;
            }
        }
        var services = DriverServices();
        if (config)
        {
            foreach (var s in services.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase))
            {
                var module = ModuleName(s.ImagePath);
                if (!module.Equals(s.Name, StringComparison.OrdinalIgnoreCase)) Console.WriteLine($"alias {s.Name} {module}");
                if (s.Start == 4) Console.WriteLine($"blacklist {module}");
            }
            return 0;
        }
        if (rest.Count == 0) throw new UsageException("missing module name.");
        var names = all ? rest : [rest[0]];
        var parameters = all ? [] : rest.Skip(1).ToArray();
        var status = 0;
        foreach (var name in names)
        {
            var (service, file) = Find(name, services);
            if (service == null && file == null)
            {
                if (!quiet) Console.Error.WriteLine($"modprobe: FATAL: Module {name} not found in directory {Drivers}");
                status = 1;
                continue;
            }
            if (showDepends)
            {
                foreach (var dep in DependencyOrder(file)) Console.WriteLine($"insmod {dep}");
                continue;
            }
            if (service == null)
            {
                if (!quiet) Console.Error.WriteLine($"modprobe: ERROR: {name} has no driver service; use insmod {file}");
                status = 1;
                continue;
            }
            try
            {
                if (remove)
                {
                    if (!IsRunning(service.Name)) continue;
                    if (verbose || dryRun) Console.WriteLine($"rmmod {service.Name}");
                    if (!dryRun) Unload(service.Name);
                }
                else
                {
                    if (IsRunning(service.Name)) continue;
                    if (verbose || dryRun) Console.WriteLine($"insmod {service.ImagePath} {string.Join(' ', parameters)}".TrimEnd());
                    if (!dryRun) Load(service.Name, service.ImagePath, parameters, create: false);
                }
            }
            catch (System.ComponentModel.Win32Exception e)
            {
                if (!quiet) Console.Error.WriteLine($"modprobe: ERROR: could not {(remove ? "remove" : "insert")} '{name}': {e.Message}");
                status = 1;
            }
        }
        return status;
    }

    // The driver's dependencies, deepest first, then the driver itself (modprobe --show-depends order).
    static List<string> DependencyOrder(string file)
    {
        var order = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Visit(string path)
        {
            if (path == null || !seen.Add(path)) return;
            foreach (var dep in Dependencies(path)) Visit(ResolveImport(dep));
            order.Add(path);
        }
        Visit(file);
        return order;
    }

    static void Load(string name, string file, string[] parameters, bool create)
    {
        var scm = OpenSCManagerW(null, null, SC_MANAGER_ALL_ACCESS);
        if (scm == IntPtr.Zero) throw new System.ComponentModel.Win32Exception();
        try
        {
            var service = OpenServiceW(scm, name, SERVICE_ALL_ACCESS);
            if (service == IntPtr.Zero && create)
                service = CreateServiceW(scm, name, name, SERVICE_ALL_ACCESS, SERVICE_KERNEL_DRIVER, SERVICE_DEMAND_START,
                    SERVICE_ERROR_NORMAL, file, null, IntPtr.Zero, null, null, null);
            if (service == IntPtr.Zero) throw new System.ComponentModel.Win32Exception();
            try
            {
                if (parameters.Length > 0)
                {
                    using var key = Registry.LocalMachine.CreateSubKey($@"SYSTEM\CurrentControlSet\Services\{name}\Parameters");
                    foreach (var p in parameters)
                    {
                        var eq = p.IndexOf('=');
                        var (k, v) = eq > 0 ? (p[..eq], p[(eq + 1)..]) : (p, "1");
                        if (int.TryParse(v, out var n)) key.SetValue(k, n, RegistryValueKind.DWord);
                        else key.SetValue(k, v);
                    }
                }
                if (!StartServiceW(service, 0, IntPtr.Zero) && Marshal.GetLastWin32Error() != ERROR_SERVICE_ALREADY_RUNNING)
                    throw new System.ComponentModel.Win32Exception();
            }
            finally { CloseServiceHandle(service); }
        }
        finally { CloseServiceHandle(scm); }
    }

    static void Unload(string name)
    {
        var scm = OpenSCManagerW(null, null, SC_MANAGER_CONNECT);
        if (scm == IntPtr.Zero) throw new System.ComponentModel.Win32Exception();
        try
        {
            var service = OpenServiceW(scm, name, SERVICE_STOP | SERVICE_QUERY_STATUS);
            if (service == IntPtr.Zero) throw new System.ComponentModel.Win32Exception();
            try
            {
                var status = new SERVICE_STATUS();
                if (!ControlService(service, SERVICE_CONTROL_STOP, ref status)) throw new System.ComponentModel.Win32Exception();
            }
            finally { CloseServiceHandle(service); }
        }
        finally { CloseServiceHandle(scm); }
    }

    static bool IsRunning(string name)
    {
        var scm = OpenSCManagerW(null, null, SC_MANAGER_CONNECT);
        if (scm == IntPtr.Zero) return false;
        try
        {
            var service = OpenServiceW(scm, name, SERVICE_QUERY_STATUS);
            if (service == IntPtr.Zero) return false;
            try
            {
                var status = new SERVICE_STATUS();
                return QueryServiceStatus(service, ref status) && status.dwCurrentState != SERVICE_STOPPED;
            }
            finally { CloseServiceHandle(service); }
        }
        finally { CloseServiceHandle(scm); }
    }

    // ---- depmod ----

    static int Depmod(string[] args)
    {
        string basedir = null, version = null;
        var dryRun = false;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            switch (a)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage:
                        	depmod -[aA] [options] [forced_version]

                        If no arguments (except options) are given, "depmod -a" is assumed

                        depmod will output a dependency list suitable for the modprobe utility.

                        Options:
                        	-a, --all            Probe all modules
                        	-A, --quick          Only does the work if there's a new module
                        	-n, --show           Write the dependency file on stdout only
                        	-b, --basedir=DIR    Write lib/modules/<version>/modules.dep under DIR
                        	-V, --version        show version
                        	-h, --help           show this help

                        Modules are the drivers in %SystemRoot%\System32\drivers; dependencies come from their
                        import tables. Without -b the file goes to %ProgramData%\kmod\lib\modules\<version>.
                        """);
                    return 0;
                case "-V" or "--version": Console.WriteLine($"kmod version {KmodVersion}"); return 0;
                case "-n" or "--show" or "--dry-run": dryRun = true; break;
                case "-a" or "--all" or "-A" or "--quick" or "-e" or "--errsyms" or "-w" or "--warn" or "-v" or "--verbose" or "-P" or "-q" or "--quiet": break;
                case "-b" or "--basedir": basedir = ++i < args.Length ? args[i] : throw new UsageException("option requires an argument -- 'b'"); break;
                case "-F" or "--filesyms" or "-E" or "--symvers" or "-C" or "--config" or "-o" or "--outdir": i++; break;
                default:
                    if (a.StartsWith("--basedir=")) basedir = a[10..];
                    else if (a.StartsWith('-')) throw new UsageException($"unrecognized option '{a}'");
                    else version = a;
                    break;
            }
        }
        version ??= Environment.OSVersion.Version.ToString();
        var sb = new StringBuilder();
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string Relative(string path) => Path.GetRelativePath(windows, path).Replace('\\', '/');
        foreach (var file in Directory.EnumerateFiles(Drivers, "*.sys").Order(StringComparer.OrdinalIgnoreCase))
        {
            var deps = Dependencies(file).Select(ResolveImport).Where(d => d != null).Select(Relative);
            sb.Append(Relative(file)).Append(':');
            foreach (var d in deps) sb.Append(' ').Append(d);
            sb.Append('\n');
        }
        if (dryRun) { Console.Write(sb.ToString()); return 0; }
        var root = basedir ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "kmod");
        var dir = Path.Combine(root, "lib", "modules", version);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "modules.dep"), sb.ToString());
        return 0;
    }

    // ---- Win32 ----

    const int SystemModuleInformation = 11;
    const int STATUS_INFO_LENGTH_MISMATCH = unchecked((int)0xC0000004);
    const uint SC_MANAGER_CONNECT = 0x1, SC_MANAGER_ALL_ACCESS = 0xF003F, SERVICE_ALL_ACCESS = 0xF01FF;
    const uint SERVICE_QUERY_STATUS = 0x4, SERVICE_STOP = 0x20, SERVICE_CONTROL_STOP = 1, SERVICE_STOPPED = 1;
    const uint SERVICE_KERNEL_DRIVER = 1, SERVICE_DEMAND_START = 3, SERVICE_ERROR_NORMAL = 1;
    const int ERROR_SERVICE_ALREADY_RUNNING = 1056;

    [StructLayout(LayoutKind.Sequential)]
    struct SERVICE_STATUS
    {
        public uint dwServiceType, dwCurrentState, dwControlsAccepted, dwWin32ExitCode, dwServiceSpecificExitCode, dwCheckPoint, dwWaitHint;
    }

    [DllImport("ntdll.dll")]
    static extern unsafe int NtQuerySystemInformation(int infoClass, byte* buffer, int length, out int returned);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr OpenSCManagerW(string machine, string database, uint access);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr OpenServiceW(IntPtr scm, string name, uint access);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr CreateServiceW(IntPtr scm, string name, string displayName, uint access, uint type, uint start, uint error,
        string binaryPath, string loadOrderGroup, IntPtr tagId, string dependencies, string account, string password);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool StartServiceW(IntPtr service, int argc, IntPtr argv);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool ControlService(IntPtr service, uint control, ref SERVICE_STATUS status);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool QueryServiceStatus(IntPtr service, ref SERVICE_STATUS status);

    [DllImport("advapi32.dll")]
    static extern bool CloseServiceHandle(IntPtr handle);
}
