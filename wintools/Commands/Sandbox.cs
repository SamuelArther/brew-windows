using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace WinTools;

// bubblewrap (bwrap) and sandvault (sv): run programs in a Windows AppContainer, the sandbox UWP apps
// and browsers use. A container can't see the user's files, registry or other processes; paths are
// let in by granting the container's SID access for the run (bwrap's --bind / --ro-bind), and network
// access is a capability (bwrap's --unshare-net drops it). The child runs in a kill-on-close job.
static class Sandbox
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("bwrap", "Run a program in an AppContainer sandbox (bubblewrap)", RunBwrap);
        Commands.Add("sv", "Run AI agents or a shell in a sandbox with its own home (sandvault)", RunSandvault);
        Commands.Add("sandvault", "Run AI agents or a shell in a sandbox with its own home (sandvault)", RunSandvault);
    }

    // What one sandboxed run needs.
    sealed class Spec
    {
        public string ContainerName;
        public bool Network = true;
        public bool Persistent;   // keep the AppContainer profile (sandvault)
        // Persistent grants (sandvault's home) stay after the run; the rest are revoked.
        public readonly List<(string Path, bool Write, bool Persistent)> Grants = [];
        public readonly Dictionary<string, string> Env = new(StringComparer.OrdinalIgnoreCase);
        public string Directory;
        public string[] Argv;
    }

    // ---- bwrap ----

    const string BwrapHelp = """
        usage: bwrap [OPTIONS...] [--] COMMAND [ARGS...]

        Runs COMMAND in a Windows AppContainer: it can read Windows and Program Files but none of
        your files, except paths bound in for this run.

            --help                       Print this help
            --version                    Print version
            --bind SRC DEST              Give the sandbox read/write access to SRC (DEST must equal SRC)
            --bind-try SRC DEST          Like --bind, but ignore a missing SRC
            --dev-bind SRC DEST          Same as --bind
            --ro-bind SRC DEST           Give the sandbox read-only access to SRC
            --ro-bind-try SRC DEST       Like --ro-bind, but ignore a missing SRC
            --tmpfs DEST                 Give the sandbox a private, empty TEMP directory
            --unshare-net                No network access
            --unshare-all                Unshare everything, including the network
            --share-net                  Keep network access (with --unshare-all)
            --chdir DIR                  Change directory to DIR
            --setenv VAR VALUE           Set an environment variable
            --unsetenv VAR               Unset an environment variable
            --clearenv                   Unset all environment variables except PATH and the Windows basics
            --die-with-parent            Kill the sandbox if bwrap dies (always the case on Windows)
            --new-session                Accepted; the sandbox can't reach other processes anyway
            --unshare-user, --unshare-pid, --unshare-ipc, --unshare-uts, --unshare-cgroup
                                         Accepted; an AppContainer already has its own object namespace
            --dev DEST, --proc DEST, --dir DEST, --symlink SRC DEST, --remount-ro DEST
                                         Accepted and ignored: they shape a Linux mount tree
        """;

    static int RunBwrap(string[] args)
    {
        var spec = new Spec { ContainerName = "wintools.bwrap." + Guid.NewGuid().ToString("N") };
        bool unshareNet = false, shareNet = false, clearEnv = false, tmpfs = false;
        var unset = new List<string>();
        var i = 0;
        string Next(string option) => i + 1 < args.Length ? args[++i] : throw new UsageException($"{option} takes an argument");
        for (; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg == "--") { i++; break; }
            if (!arg.StartsWith("--")) break;
            switch (arg)
            {
                case "--help": Console.WriteLine(BwrapHelp); return 0;
                case "--version": Console.WriteLine($"bubblewrap {Commands.Version} (Windows AppContainer)"); return 0;
                case "--bind" or "--dev-bind" or "--bind-try" or "--dev-bind-try" or "--ro-bind" or "--ro-bind-try":
                {
                    var src = Next(arg);
                    var dest = Next(arg);
                    var full = Path.GetFullPath(src);
                    if (!SamePath(full, Path.GetFullPath(dest)))
                    {
                        Console.Error.WriteLine($"bwrap: can't bind {src} at {dest}: Windows has no mount namespaces, so a path can only be bound at itself");
                        return 1;
                    }
                    if (!File.Exists(full) && !System.IO.Directory.Exists(full))
                    {
                        if (arg.EndsWith("-try")) break;
                        Console.Error.WriteLine($"bwrap: Can't find source path {src}: No such file or directory");
                        return 1;
                    }
                    if (Path.GetPathRoot(full) == full)
                    {
                        Console.Error.WriteLine($"bwrap: can't bind the whole drive {full}; bind the directories the program needs (Windows and Program Files are already readable)");
                        return 1;
                    }
                    spec.Grants.Add((full, !arg.StartsWith("--ro"), false));
                    break;
                }
                case "--tmpfs": Next(arg); tmpfs = true; break;
                case "--dev" or "--proc" or "--dir" or "--remount-ro" or "--mqueue": Next(arg); break;
                case "--symlink": Next(arg); Next(arg); break;
                case "--unshare-net" or "--unshare-all": unshareNet = true; break;
                case "--share-net": shareNet = true; break;
                case "--unshare-user" or "--unshare-user-try" or "--unshare-pid" or "--unshare-ipc" or "--unshare-uts"
                    or "--unshare-cgroup" or "--unshare-cgroup-try" or "--new-session" or "--die-with-parent" or "--as-pid-1":
                    break;
                case "--chdir": spec.Directory = Path.GetFullPath(Next(arg)); break;
                case "--setenv": { var name = Next(arg); spec.Env[name] = Next(arg); break; }
                case "--unsetenv": unset.Add(Next(arg)); break;
                case "--clearenv": clearEnv = true; break;
                case "--hostname": spec.Env["COMPUTERNAME"] = Next(arg); break;
                case "--uid" or "--gid":
                    Next(arg);
                    Console.Error.WriteLine($"bwrap: {arg} is ignored: an AppContainer runs as your account with its own container identity");
                    break;
                case "--seccomp" or "--add-seccomp-fd" or "--cap-add" or "--userns" or "--userns2" or "--pidns"
                    or "--info-fd" or "--json-status-fd" or "--block-fd" or "--sync-fd" or "--lock-file":
                    Console.Error.WriteLine($"bwrap: {arg} has no Windows equivalent");
                    return 1;
                case "--cap-drop": Next(arg); break;   // an AppContainer holds no privileges to drop
                default: throw new UsageException($"Unknown option {arg}");
            }
        }
        if (i >= args.Length) throw new UsageException("No command specified (see bwrap --help)");
        spec.Argv = args[i..];
        spec.Network = !unshareNet || shareNet;

        if (clearEnv)
        {
            spec.Env["*clear*"] = "";   // sentinel: start from the minimal environment
        }
        foreach (var name in unset) spec.Env[name] = null;

        string temp = null;
        try
        {
            if (tmpfs)
            {
                temp = Path.Combine(Path.GetTempPath(), "bwrap-" + Guid.NewGuid().ToString("N")[..12]);
                System.IO.Directory.CreateDirectory(temp);
                spec.Grants.Add((temp, true, false));
                spec.Env["TEMP"] = spec.Env["TMP"] = spec.Env["TMPDIR"] = temp;
            }
            // Linux bwrap keeps the working directory when the sandbox can see it, else falls back.
            if (spec.Directory == null)
            {
                var cwd = Environment.CurrentDirectory;
                spec.Directory = spec.Grants.Any(g => IsUnder(cwd, g.Path)) ? cwd : Environment.SystemDirectory;
            }
            return Execute(spec, "bwrap");
        }
        finally
        {
            if (temp != null) try { System.IO.Directory.Delete(temp, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }

    // ---- sandvault ----

    const string SvHelp = """
        Usage: sv [options] <command> [args...]

        Runs an AI agent or a shell in an AppContainer sandbox with its own home folder
        (%LOCALAPPDATA%\sandvault\home) and read/write access to the current directory only.

        Commands:
          shell, s            Command Prompt (or $SHELL) in the sandbox
          claude, cl          Claude Code
          codex, co           OpenAI Codex CLI
          gemini, g           Gemini CLI
          run <cmd> [args]    Any program on PATH
          uninstall           Delete the sandbox home and its access grants

        Options:
          -n, --no-network    Run without network access
          -h, --help          Show this help
          -v, --version       Show the version
        """;

    static int RunSandvault(string[] args)
    {
        var network = true;
        var i = 0;
        for (; i < args.Length && args[i].StartsWith('-'); i++)
        {
            if (args[i] == "--") { i++; break; }
            switch (args[i])
            {
                case "-h" or "--help": Console.WriteLine(SvHelp); return 0;
                case "-v" or "--version": Console.WriteLine($"sv version {Commands.Version}"); return 0;
                case "-n" or "--no-network": network = false; break;
                default: throw new UsageException($"unknown option {args[i]}");
            }
        }
        var command = i < args.Length ? args[i++] : "shell";
        var rest = args[i..];

        var name = "sandvault." + Environment.UserName.ToLowerInvariant();
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "sandvault");
        var home = Path.Combine(root, "home");

        if (command == "uninstall")
        {
            if (System.IO.Directory.Exists(root))
            {
                using (var sid = ContainerSid.Derive(name)) Revoke(root, sid.Identifier);
                System.IO.Directory.Delete(root, true);
            }
            DeleteAppContainerProfile(name);
            Console.WriteLine("sandvault: removed " + root);
            return 0;
        }

        string[] argv = command switch
        {
            "shell" or "s" => [Environment.GetEnvironmentVariable("SHELL") ?? Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe", .. rest],
            "claude" or "cl" => ["claude", .. rest],
            "codex" or "co" => ["codex", .. rest],
            "gemini" or "g" => ["gemini", .. rest],
            "run" => rest.Length > 0 ? rest : throw new UsageException("run needs a command"),
            _ => throw new UsageException($"unknown command '{command}' (see sv --help)"),
        };

        var program = FindProgram(argv[0]) ?? throw new InvalidOperationException($"{argv[0]}: not found on PATH");
        argv[0] = program;

        foreach (var dir in new[] { "AppData\\Roaming", "AppData\\Local\\Temp", ".config" })
            System.IO.Directory.CreateDirectory(Path.Combine(home, dir));

        var workspace = Environment.CurrentDirectory;
        if (Path.GetPathRoot(workspace) == workspace || SamePath(workspace, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)))
        {
            Console.Error.WriteLine($"sv: run sv from a project folder, not {workspace}: the sandbox gets read/write access to the current directory");
            return 1;
        }
        var spec = new Spec { ContainerName = name, Persistent = true, Network = network, Directory = workspace, Argv = argv };
        spec.Grants.Add((home, true, true));
        spec.Grants.Add((workspace, true, false));
        // The agent's own install (and node.exe for npm-installed agents) must be readable.
        foreach (var dir in ToolDirectories(program))
            if (!IsUnder(dir, Environment.GetFolderPath(Environment.SpecialFolder.Windows)) && !spec.Grants.Any(g => IsUnder(dir, g.Path)))
                spec.Grants.Add((dir, false, false));

        spec.Env["HOME"] = spec.Env["USERPROFILE"] = home;
        spec.Env["APPDATA"] = Path.Combine(home, "AppData\\Roaming");
        spec.Env["LOCALAPPDATA"] = Path.Combine(home, "AppData\\Local");
        spec.Env["TEMP"] = spec.Env["TMP"] = Path.Combine(home, "AppData\\Local\\Temp");
        spec.Env["XDG_CONFIG_HOME"] = Path.Combine(home, ".config");
        spec.Env["SANDVAULT"] = "1";
        return Execute(spec, "sv");
    }

    // Directories an agent needs to start: its own, and for .cmd/.ps1 shims, node's and the shim's package dir.
    static IEnumerable<string> ToolDirectories(string program)
    {
        var dir = Path.GetDirectoryName(program);
        yield return dir;
        var ext = Path.GetExtension(program).ToLowerInvariant();
        if (ext is ".cmd" or ".bat" or ".ps1" or "")
        {
            var node = FindProgram("node");
            if (node != null) yield return Path.GetDirectoryName(node);
        }
    }

    // ---- the AppContainer launch ----

    static int Execute(Spec spec, string tool)
    {
        using var sid = ContainerSid.Create(spec.ContainerName, temporary: !spec.Persistent);
        var granted = new List<string>();
        var job = IntPtr.Zero;
        var attributes = IntPtr.Zero;
        var capabilities = new List<IntPtr>();
        var pinned = new List<GCHandle>();
        var commandLine = IntPtr.Zero;
        var environment = IntPtr.Zero;
        try
        {
            foreach (var (path, write, persistent) in spec.Grants)
            {
                if (!write && AppPackagesCanRead(path)) continue;
                Grant(path, sid.Identifier, write);
                if (!persistent) granted.Add(path);
            }

            var program = FindProgram(spec.Argv[0]);
            if (program == null)
            {
                Console.Error.WriteLine($"{tool}: execvp {spec.Argv[0]}: No such file or directory");
                return 1;
            }
            var argv = (string[])spec.Argv.Clone();
            var ext = Path.GetExtension(program).ToLowerInvariant();
            // Batch files and PowerShell scripts need their interpreter.
            if (ext is ".cmd" or ".bat") argv = [Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe", "/d", "/c", program, .. argv[1..]];
            else if (ext == ".ps1") argv = ["powershell.exe", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", program, .. argv[1..]];
            else argv[0] = program;
            var image = FindProgram(argv[0]);

            // SECURITY_CAPABILITIES: the container SID plus the network capabilities.
            var capabilitySids = spec.Network ? new[] { "S-1-15-3-1", "S-1-15-3-3" } : [];
            var sidAndAttributes = new SID_AND_ATTRIBUTES[capabilitySids.Length];
            for (var c = 0; c < capabilitySids.Length; c++)
            {
                if (!ConvertStringSidToSidW(capabilitySids[c], out var capSid)) throw new System.ComponentModel.Win32Exception();
                capabilities.Add(capSid);
                sidAndAttributes[c] = new SID_AND_ATTRIBUTES { Sid = capSid, Attributes = SE_GROUP_ENABLED };
            }
            var capsHandle = GCHandle.Alloc(sidAndAttributes, GCHandleType.Pinned);
            pinned.Add(capsHandle);
            var security = new SECURITY_CAPABILITIES
            {
                AppContainerSid = sid.Pointer,
                Capabilities = capabilitySids.Length > 0 ? capsHandle.AddrOfPinnedObject() : IntPtr.Zero,
                CapabilityCount = (uint)capabilitySids.Length,
            };
            var securityHandle = GCHandle.Alloc(security, GCHandleType.Pinned);
            pinned.Add(securityHandle);

            // Only the three standard handles are inherited.
            var handles = new[] { GetStdHandle(STD_INPUT_HANDLE), GetStdHandle(STD_OUTPUT_HANDLE), GetStdHandle(STD_ERROR_HANDLE) };
            var inheritable = handles.Where(h => h != IntPtr.Zero && h != new IntPtr(-1)).Distinct().ToArray();
            foreach (var h in inheritable) SetHandleInformation(h, HANDLE_FLAG_INHERIT, HANDLE_FLAG_INHERIT);
            var handleList = GCHandle.Alloc(inheritable, GCHandleType.Pinned);
            pinned.Add(handleList);

            var count = inheritable.Length > 0 ? 2 : 1;
            var size = IntPtr.Zero;
            InitializeProcThreadAttributeList(IntPtr.Zero, count, 0, ref size);
            attributes = Marshal.AllocHGlobal(size);
            if (!InitializeProcThreadAttributeList(attributes, count, 0, ref size)) throw new System.ComponentModel.Win32Exception();
            if (!UpdateProcThreadAttribute(attributes, 0, PROC_THREAD_ATTRIBUTE_SECURITY_CAPABILITIES, securityHandle.AddrOfPinnedObject(),
                    Marshal.SizeOf<SECURITY_CAPABILITIES>(), IntPtr.Zero, IntPtr.Zero))
                throw new System.ComponentModel.Win32Exception();
            if (inheritable.Length > 0 && !UpdateProcThreadAttribute(attributes, 0, PROC_THREAD_ATTRIBUTE_HANDLE_LIST, handleList.AddrOfPinnedObject(),
                    IntPtr.Size * inheritable.Length, IntPtr.Zero, IntPtr.Zero))
                throw new System.ComponentModel.Win32Exception();

            var startup = new STARTUPINFOEX { lpAttributeList = attributes };
            startup.StartupInfo.cb = Marshal.SizeOf<STARTUPINFOEX>();
            startup.StartupInfo.dwFlags = STARTF_USESTDHANDLES;
            startup.StartupInfo.hStdInput = handles[0];
            startup.StartupInfo.hStdOutput = handles[1];
            startup.StartupInfo.hStdError = handles[2];

            commandLine = Marshal.StringToHGlobalUni(string.Join(' ', argv.Select(Quote)));
            environment = Marshal.StringToHGlobalUni(BuildEnvironment(spec.Env));

            job = CreateJobObjectW(IntPtr.Zero, null);
            var limits = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
            limits.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;
            SetInformationJobObject(job, JobObjectExtendedLimitInformation, ref limits, Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>());

            if (!CreateProcessW(image, commandLine, IntPtr.Zero, IntPtr.Zero, inheritable.Length > 0,
                    EXTENDED_STARTUPINFO_PRESENT | CREATE_SUSPENDED | CREATE_UNICODE_ENVIRONMENT, environment, spec.Directory,
                    ref startup, out var process))
            {
                var error = Marshal.GetLastWin32Error();
                var reason = error == 5 ? "Permission denied (bind its directory with --ro-bind)" : new System.ComponentModel.Win32Exception(error).Message;
                Console.Error.WriteLine($"{tool}: execvp {spec.Argv[0]}: {reason}");
                return 1;
            }
            AssignProcessToJobObject(job, process.hProcess);
            // Ctrl+C reaches the sandbox through the shared console; bwrap waits for it to exit.
            ConsoleCancelEventHandler ignore = (_, e) => e.Cancel = true;
            Console.CancelKeyPress += ignore;
            ResumeThread(process.hThread);
            CloseHandle(process.hThread);
            WaitForSingleObject(process.hProcess, INFINITE);
            GetExitCodeProcess(process.hProcess, out var code);
            CloseHandle(process.hProcess);
            Console.CancelKeyPress -= ignore;
            return (int)code;
        }
        finally
        {
            if (job != IntPtr.Zero) CloseHandle(job);
            if (attributes != IntPtr.Zero) { DeleteProcThreadAttributeList(attributes); Marshal.FreeHGlobal(attributes); }
            foreach (var p in capabilities) LocalFree(p);
            foreach (var h in pinned) h.Free();
            if (commandLine != IntPtr.Zero) Marshal.FreeHGlobal(commandLine);
            if (environment != IntPtr.Zero) Marshal.FreeHGlobal(environment);
            foreach (var path in granted)
            {
                try { Revoke(path, sid.Identifier); }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    Console.Error.WriteLine($"{tool}: couldn't remove the sandbox's access to {path}: {e.Message}");
                }
            }
        }
    }

    // An AppContainer profile (Windows won't start a container without one) and its SID. bwrap's
    // per-run profiles are deleted again on Dispose; sandvault's stays until `sv uninstall`.
    sealed class ContainerSid : IDisposable
    {
        public IntPtr Pointer;
        public SecurityIdentifier Identifier;
        string temporaryProfile;

        public static ContainerSid Create(string name, bool temporary)
        {
            var hr = CreateAppContainerProfile(name, name, "wintools sandbox", IntPtr.Zero, 0, out var sid);
            if (hr == unchecked((int)0x800700B7)) return Derive(name);   // already exists
            if (hr != 0) throw new InvalidOperationException($"can't create an AppContainer profile (0x{hr:x8})");
            return new ContainerSid { Pointer = sid, Identifier = new SecurityIdentifier(sid), temporaryProfile = temporary ? name : null };
        }

        public static ContainerSid Derive(string name)
        {
            var hr = DeriveAppContainerSidFromAppContainerName(name, out var sid);
            if (hr != 0) throw new InvalidOperationException($"can't create an AppContainer identity (0x{hr:x8})");
            return new ContainerSid { Pointer = sid, Identifier = new SecurityIdentifier(sid) };
        }

        public void Dispose()
        {
            if (Pointer != IntPtr.Zero) FreeSid(Pointer);
            Pointer = IntPtr.Zero;
            if (temporaryProfile != null) DeleteAppContainerProfile(temporaryProfile);
            temporaryProfile = null;
        }
    }

    static void Grant(string path, SecurityIdentifier sid, bool write)
    {
        var rights = write ? FileSystemRights.Modify | FileSystemRights.Synchronize : FileSystemRights.ReadAndExecute | FileSystemRights.Synchronize;
        if (System.IO.Directory.Exists(path))
        {
            var dir = new DirectoryInfo(path);
            var acl = dir.GetAccessControl();
            acl.AddAccessRule(new FileSystemAccessRule(sid, rights, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None, AccessControlType.Allow));
            dir.SetAccessControl(acl);
        }
        else
        {
            var file = new FileInfo(path);
            var acl = file.GetAccessControl();
            acl.AddAccessRule(new FileSystemAccessRule(sid, rights, AccessControlType.Allow));
            file.SetAccessControl(acl);
        }
    }

    static void Revoke(string path, SecurityIdentifier sid)
    {
        if (System.IO.Directory.Exists(path))
        {
            var dir = new DirectoryInfo(path);
            var acl = dir.GetAccessControl();
            acl.PurgeAccessRules(sid);
            dir.SetAccessControl(acl);
        }
        else if (File.Exists(path))
        {
            var file = new FileInfo(path);
            var acl = file.GetAccessControl();
            acl.PurgeAccessRules(sid);
            file.SetAccessControl(acl);
        }
    }

    // Windows, Program Files and the like already let every AppContainer read them.
    static bool AppPackagesCanRead(string path)
    {
        try
        {
            FileSystemSecurity acl = System.IO.Directory.Exists(path) ? new DirectoryInfo(path).GetAccessControl() : new FileInfo(path).GetAccessControl();
            var allPackages = new SecurityIdentifier("S-1-15-2-1");
            foreach (FileSystemAccessRule rule in acl.GetAccessRules(true, true, typeof(SecurityIdentifier)))
                if (rule.IdentityReference.Equals(allPackages) && rule.AccessControlType == AccessControlType.Allow
                    && (rule.FileSystemRights & FileSystemRights.ReadAndExecute) == FileSystemRights.ReadAndExecute)
                    return true;
        }
        catch (UnauthorizedAccessException) { }
        return false;
    }

    // ---- helpers ----

    static string BuildEnvironment(Dictionary<string, string> changes)
    {
        var env = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (changes.ContainsKey("*clear*"))
        {
            foreach (var keep in new[] { "PATH", "PATHEXT", "SystemRoot", "SystemDrive", "windir", "ComSpec", "NUMBER_OF_PROCESSORS", "PROCESSOR_ARCHITECTURE" })
                if (Environment.GetEnvironmentVariable(keep) is { } value) env[keep] = value;
        }
        else
        {
            foreach (System.Collections.DictionaryEntry e in Environment.GetEnvironmentVariables())
                env[(string)e.Key] = (string)e.Value;
        }
        foreach (var (name, value) in changes)
        {
            if (name == "*clear*") continue;
            if (value == null) env.Remove(name);
            else env[name] = value;
        }
        var block = new StringBuilder();
        foreach (var (name, value) in env) block.Append(name).Append('=').Append(value).Append('\0');
        return block.Append('\0').ToString();
    }

    internal static string FindProgram(string name)
    {
        if (name.Contains('\\') || name.Contains('/') || Path.IsPathRooted(name))
        {
            var full = Path.GetFullPath(name);
            if (File.Exists(full)) return full;
            if (!Path.HasExtension(full) && File.Exists(full + ".exe")) return full + ".exe";
            return null;
        }
        var extensions = Path.HasExtension(name) ? [""] : (Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD").Split(';');
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
            foreach (var ext in extensions)
            {
                var candidate = Path.Combine(dir.Trim('"'), name + ext.ToLowerInvariant());
                if (File.Exists(candidate)) return candidate;
            }
        return null;
    }

    // Windows command-line quoting (the rules CommandLineToArgvW and the C runtime parse).
    internal static string Quote(string arg)
    {
        if (arg.Length > 0 && arg.IndexOfAny([' ', '\t', '"']) < 0) return arg;
        var sb = new StringBuilder("\"");
        var backslashes = 0;
        foreach (var c in arg)
        {
            if (c == '\\') { backslashes++; continue; }
            sb.Append('\\', c == '"' ? backslashes * 2 + 1 : backslashes);
            backslashes = 0;
            sb.Append(c);
        }
        return sb.Append('\\', backslashes * 2).Append('"').ToString();
    }

    static bool SamePath(string a, string b) =>
        string.Equals(Path.TrimEndingDirectorySeparator(a), Path.TrimEndingDirectorySeparator(b), StringComparison.OrdinalIgnoreCase);

    static bool IsUnder(string path, string root)
    {
        root = Path.TrimEndingDirectorySeparator(root);
        return SamePath(path, root) || path.StartsWith(root + "\\", StringComparison.OrdinalIgnoreCase);
    }

    // ---- Win32 ----

    const uint SE_GROUP_ENABLED = 4;
    const uint EXTENDED_STARTUPINFO_PRESENT = 0x80000, CREATE_SUSPENDED = 4, CREATE_UNICODE_ENVIRONMENT = 0x400;
    const int STARTF_USESTDHANDLES = 0x100;
    const int STD_INPUT_HANDLE = -10, STD_OUTPUT_HANDLE = -11, STD_ERROR_HANDLE = -12;
    const uint HANDLE_FLAG_INHERIT = 1;
    const uint INFINITE = 0xFFFFFFFF;
    const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;
    const int JobObjectExtendedLimitInformation = 9;
    static readonly IntPtr PROC_THREAD_ATTRIBUTE_SECURITY_CAPABILITIES = new(0x20009);
    static readonly IntPtr PROC_THREAD_ATTRIBUTE_HANDLE_LIST = new(0x20002);

    [StructLayout(LayoutKind.Sequential)]
    struct SID_AND_ATTRIBUTES { public IntPtr Sid; public uint Attributes; }

    [StructLayout(LayoutKind.Sequential)]
    struct SECURITY_CAPABILITIES
    {
        public IntPtr AppContainerSid;
        public IntPtr Capabilities;
        public uint CapabilityCount;
        public uint Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct STARTUPINFO
    {
        public int cb;
        public IntPtr lpReserved, lpDesktop, lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct STARTUPINFOEX { public STARTUPINFO StartupInfo; public IntPtr lpAttributeList; }

    [StructLayout(LayoutKind.Sequential)]
    struct PROCESS_INFORMATION { public IntPtr hProcess, hThread; public int dwProcessId, dwThreadId; }

    [StructLayout(LayoutKind.Sequential)]
    struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit, PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize, MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass, SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct IO_COUNTERS { public ulong ReadOperationCount, WriteOperationCount, OtherOperationCount, ReadTransferCount, WriteTransferCount, OtherTransferCount; }

    [StructLayout(LayoutKind.Sequential)]
    struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public UIntPtr ProcessMemoryLimit, JobMemoryLimit, PeakProcessMemoryUsed, PeakJobMemoryUsed;
    }

    [DllImport("userenv.dll", CharSet = CharSet.Unicode)]
    static extern int CreateAppContainerProfile(string name, string displayName, string description, IntPtr capabilities, int count, out IntPtr sid);

    [DllImport("userenv.dll", CharSet = CharSet.Unicode)]
    static extern int DeleteAppContainerProfile(string name);

    [DllImport("userenv.dll", CharSet = CharSet.Unicode)]
    static extern int DeriveAppContainerSidFromAppContainerName(string name, out IntPtr sid);

    [DllImport("advapi32.dll")]
    static extern IntPtr FreeSid(IntPtr sid);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool ConvertStringSidToSidW(string stringSid, out IntPtr sid);

    [DllImport("kernel32.dll")]
    static extern IntPtr LocalFree(IntPtr mem);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool InitializeProcThreadAttributeList(IntPtr list, int count, int flags, ref IntPtr size);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool UpdateProcThreadAttribute(IntPtr list, uint flags, IntPtr attribute, IntPtr value, nint size, IntPtr previous, IntPtr returnSize);

    [DllImport("kernel32.dll")]
    static extern void DeleteProcThreadAttributeList(IntPtr list);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool CreateProcessW(string application, IntPtr commandLine, IntPtr processAttributes, IntPtr threadAttributes,
        bool inheritHandles, uint flags, IntPtr environment, string directory, ref STARTUPINFOEX startup, out PROCESS_INFORMATION info);

    [DllImport("kernel32.dll")]
    static extern IntPtr GetStdHandle(int which);

    [DllImport("kernel32.dll")]
    static extern bool SetHandleInformation(IntPtr handle, uint mask, uint flags);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr CreateJobObjectW(IntPtr attributes, string name);

    [DllImport("kernel32.dll")]
    static extern bool SetInformationJobObject(IntPtr job, int infoClass, ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION info, int size);

    [DllImport("kernel32.dll")]
    static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

    [DllImport("kernel32.dll")]
    static extern uint ResumeThread(IntPtr thread);

    [DllImport("kernel32.dll")]
    static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

    [DllImport("kernel32.dll")]
    static extern bool GetExitCodeProcess(IntPtr process, out uint code);

    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr handle);
}
