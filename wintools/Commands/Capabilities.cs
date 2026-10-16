using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace WinTools;

// libcap (capsh, getpcaps, getcap, setcap) and libcap-ng (pscap, netcap, filecap, captest) on Windows
// token privileges, the Windows counterpart of POSIX capabilities: a process's access token holds a
// set of privileges (SeDebugPrivilege, SeShutdownPrivilege, ...) that are present ("permitted") and
// either enabled ("effective") or not; children inherit them. In libcap's text form an enabled
// privilege is "=eip" and a present-but-disabled one "=ip". capsh --drop runs a program with a
// restricted token that no longer holds the dropped privileges. Linux capability masks decode with
// capsh --decode. Files carry no capabilities on Windows (privileges belong to accounts), so getcap
// and filecap find none and setcap explains that.
static unsafe class Capabilities
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("capsh", "Capability shell wrapper: show, check and drop Windows token privileges", Capsh);
        Commands.Add("getpcaps", "List the privileges (capabilities) of processes", Getpcaps);
        Commands.Add("getcap", "Examine file capabilities (Windows files carry none)", Getcap);
        Commands.Add("setcap", "Set file capabilities (not available on Windows)", Setcap);
        Commands.Add("pscap", "Report processes running with privileges enabled (libcap-ng)", Pscap);
        Commands.Add("netcap", "Report network-facing processes and their privileges (libcap-ng)", Netcap);
        Commands.Add("filecap", "Report files with capabilities (libcap-ng; Windows files carry none)", Filecap);
        Commands.Add("captest", "Show the current process's privileges and test privilege boundaries (libcap-ng)", Captest);
    }

    const string LibcapVersion = "2.78", LibcapNgVersion = "0.9.6";

    // Linux capability numbers, for capsh --decode / --supports.
    static readonly string[] LinuxCaps =
    [
        "cap_chown", "cap_dac_override", "cap_dac_read_search", "cap_fowner", "cap_fsetid", "cap_kill", "cap_setgid",
        "cap_setuid", "cap_setpcap", "cap_linux_immutable", "cap_net_bind_service", "cap_net_broadcast", "cap_net_admin",
        "cap_net_raw", "cap_ipc_lock", "cap_ipc_owner", "cap_sys_module", "cap_sys_rawio", "cap_sys_chroot", "cap_sys_ptrace",
        "cap_sys_pacct", "cap_sys_admin", "cap_sys_boot", "cap_sys_nice", "cap_sys_resource", "cap_sys_time",
        "cap_sys_tty_config", "cap_mknod", "cap_lease", "cap_audit_write", "cap_audit_control", "cap_setfcap",
        "cap_mac_override", "cap_mac_admin", "cap_syslog", "cap_wake_alarm", "cap_block_suspend", "cap_audit_read",
        "cap_perfmon", "cap_bpf", "cap_checkpoint_restore",
    ];

    sealed record Privilege(string Name, bool Enabled, long Luid);

    // ---- token plumbing ----

    static IntPtr OpenToken(int pid, uint access = TOKEN_QUERY)
    {
        var process = pid == Environment.ProcessId ? GetCurrentProcess() : OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (process == IntPtr.Zero) return IntPtr.Zero;
        try { return OpenProcessToken(process, access, out var token) ? token : IntPtr.Zero; }
        finally { if (pid != Environment.ProcessId) CloseHandle(process); }
    }

    static byte[] TokenInfo(IntPtr token, int infoClass)
    {
        GetTokenInformation(token, infoClass, null, 0, out var size);
        if (size == 0) return null;
        // Pinned: the SIDs inside are absolute pointers into this buffer.
        var buffer = GC.AllocateUninitializedArray<byte>(size, pinned: true);
        return GetTokenInformation(token, infoClass, buffer, size, out _) ? buffer : null;
    }

    static List<Privilege> Privileges(IntPtr token)
    {
        var list = new List<Privilege>();
        var data = TokenInfo(token, TokenPrivileges);
        if (data == null) return list;
        var count = BitConverter.ToInt32(data, 0);
        for (var i = 0; i < count; i++)
        {
            var luid = BitConverter.ToInt64(data, 4 + i * 12);
            var attributes = BitConverter.ToUInt32(data, 12 + i * 12);
            if ((attributes & SE_PRIVILEGE_REMOVED) != 0) continue;
            list.Add(new Privilege(PrivilegeName(luid), (attributes & SE_PRIVILEGE_ENABLED) != 0, luid));
        }
        return list;
    }

    static string PrivilegeName(long luid)
    {
        var buffer = new StringBuilder(128);
        var length = buffer.Capacity;
        return LookupPrivilegeNameW(null, ref luid, buffer, ref length) ? buffer.ToString() : $"luid:{luid}";
    }

    static SecurityIdentifier Sid(byte[] data, int pointerOffset)
    {
        var p = (IntPtr)BitConverter.ToInt64(data, pointerOffset);
        fixed (byte* basePtr = data)
        {
            var offset = (long)p - (long)basePtr;
            return offset >= 0 && offset < data.Length ? new SecurityIdentifier(data, (int)offset) : null;
        }
    }

    static string Account(SecurityIdentifier sid)
    {
        if (sid == null) return "?";
        try { return sid.Translate(typeof(NTAccount)).Value; }
        catch (Exception e) when (e is IdentityNotMappedException or SystemException) { return sid.Value; }
    }

    static (SecurityIdentifier User, string Integrity, bool Elevated) Identity(IntPtr token)
    {
        SecurityIdentifier user = null;
        string integrity = "?";
        var data = TokenInfo(token, TokenUser);
        if (data != null) user = Sid(data, 0);
        var label = TokenInfo(token, TokenIntegrityLevel);
        if (label != null && Sid(label, 0) is { } il)
        {
            var rid = il.Value.Split('-')[^1];
            integrity = rid switch
            {
                "0" => "Untrusted", "4096" => "Low", "8192" => "Medium", "8448" => "Medium Plus", "12288" => "High",
                "16384" => "System", "20480" => "Protected Process", _ => il.Value,
            } + $" ({il.Value})";
        }
        var elevation = TokenInfo(token, TokenElevation);
        return (user, integrity, elevation != null && BitConverter.ToInt32(elevation, 0) != 0);
    }

    // libcap's text form: "A,B=eip C,D=ip" (enabled, then present but disabled).
    static string Text(List<Privilege> privileges)
    {
        if (privileges.Count == 0) return "=";
        var parts = new List<string>();
        var enabled = privileges.Where(p => p.Enabled).Select(p => p.Name).ToList();
        var disabled = privileges.Where(p => !p.Enabled).Select(p => p.Name).ToList();
        if (enabled.Count > 0) parts.Add(string.Join(",", enabled) + "=eip");
        if (disabled.Count > 0) parts.Add(string.Join(",", disabled) + "=ip");
        return string.Join(" ", parts);
    }

    static bool KnownPrivilege(string name, out long luid) => LookupPrivilegeValueW(null, name, out luid);

    // "SeDebugPrivilege", "debug", "sedebugprivilege" -> canonical privilege name.
    static string Canonical(string name)
    {
        foreach (var candidate in new[] { name, "Se" + name + "Privilege", "Se" + name })
            if (KnownPrivilege(candidate, out var luid)) return PrivilegeName(luid);
        return null;
    }

    // ---- capsh ----

    static int Capsh(string[] args)
    {
        var drop = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            var eq = arg.IndexOf('=');
            var (name, value) = eq > 0 ? (arg[..eq], arg[(eq + 1)..]) : (arg, null);
            switch (name)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        usage: capsh [args ...]
                          --help, -h     this message (or try 'man capsh')
                          --license      display license info
                          --print        display capability relevant state
                          --current      display current capabilities
                          --decode=xxx   decode a hex string to a list of Linux caps
                          --supports=xxx exit 1 if privilege xxx not known
                          --has-p=xxx    exit 1 if privilege xxx not present (permitted)
                          --has-e=xxx    exit 1 if privilege xxx not enabled (effective)
                          --drop=xxx     remove xxx,.. privileges from the token used by -- and ==
                          --             remaining arguments are for cmd.exe ("-c" works like bash's)
                          ==             re-run capsh with remaining arguments
                        Privileges are Windows token privileges (SeDebugPrivilege, or just Debug).
                        """);
                    return 0;
                case "--version": Console.WriteLine($"capsh libcap {LibcapVersion} (wintools {Commands.Version})"); return 0;
                case "--license": Console.WriteLine("capsh: libcap's command-line interface (BSD-3-Clause or GPL-2.0-or-later), reimplemented for Windows token privileges"); break;
                case "--print": PrintState(); break;
                case "--current":
                {
                    var token = OpenToken(Environment.ProcessId);
                    Console.WriteLine($"Current: {Text(Privileges(token))}");
                    CloseHandle(token);
                    break;
                }
                case "--decode":
                {
                    var text = (value ?? "").StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
                    if (!ulong.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out var mask)) throw new UsageException($"invalid hex value: {value}");
                    var names = Enumerable.Range(0, 64).Where(b => (mask & (1UL << b)) != 0).Select(b => b < LinuxCaps.Length ? LinuxCaps[b] : $"{b}");
                    Console.WriteLine($"0x{mask:x16}={string.Join(",", names)}");
                    break;
                }
                case "--supports":
                    if (value == null) throw new UsageException("--supports needs a name");
                    if (Canonical(value) == null && !LinuxCaps.Contains(value.ToLowerInvariant()))
                    {
                        Console.Error.WriteLine($"privilege {value} not supported");
                        return 1;
                    }
                    break;
                case "--has-p" or "--has-e":
                {
                    var canonical = Canonical(value ?? "") ?? throw new UsageException($"unknown privilege: {value}");
                    var token = OpenToken(Environment.ProcessId);
                    var match = Privileges(token).FirstOrDefault(p => p.Name == canonical);
                    CloseHandle(token);
                    if (match == null || name == "--has-e" && !match.Enabled)
                    {
                        Console.Error.WriteLine($"{canonical} not {(name == "--has-p" ? "in permitted" : "effective")}");
                        return 1;
                    }
                    break;
                }
                case "--drop":
                    foreach (var p in (value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
                        drop.Add(Canonical(p) ?? throw new UsageException($"unknown privilege: {p}"));
                    break;
                case "--" or "==":
                {
                    var rest = args[(i + 1)..];
                    string application, commandLine;
                    if (name == "==")
                    {
                        application = Environment.ProcessPath;
                        commandLine = Sandbox.Quote(application) + (Path.GetFileNameWithoutExtension(application).Equals("wintools", StringComparison.OrdinalIgnoreCase) ? " capsh" : "")
                                      + string.Concat(rest.Select(a => " " + Sandbox.Quote(a)));
                    }
                    else
                    {
                        application = Path.Combine(Environment.SystemDirectory, "cmd.exe");
                        var shellArgs = rest.Length > 0 && rest[0] == "-c" ? ["/c", .. rest[1..]] : rest;
                        commandLine = "cmd.exe" + string.Concat(shellArgs.Select(a => " " + (a == "/c" ? a : Sandbox.Quote(a))));
                    }
                    return RunRestricted(application, commandLine, drop);
                }
                case "--inh" or "--caps" or "--keep" or "--uid" or "--gid" or "--groups" or "--user" or "--chroot" or "--secbits"
                    or "--addamb" or "--delamb" or "--noamb" or "--iab" or "--mode" or "--killit" or "--forkfor":
                    Console.Error.WriteLine($"capsh: {name}: Windows has no equivalent for this Linux process state (privileges can only be dropped)");
                    return 1;
                default: throw new UsageException($"unknown option '{arg}' (see capsh --help)");
            }
        }
        return 0;
    }

    static void PrintState()
    {
        var token = OpenToken(Environment.ProcessId);
        var privileges = Privileges(token);
        var (user, integrity, elevated) = Identity(token);
        Console.WriteLine($"Current: {Text(privileges)}");
        Console.WriteLine($"Bounding set ={string.Join(",", privileges.Select(p => p.Name))}");
        Console.WriteLine("Ambient set =");
        Console.WriteLine($"Integrity level: {integrity}");
        Console.WriteLine($"Elevated: {(elevated ? "yes" : "no")}");
        Console.WriteLine($"uid={user?.Value}({Account(user)})");
        var groups = TokenInfo(token, TokenGroups);
        if (groups != null)
        {
            var count = BitConverter.ToInt32(groups, 0);
            var names = new List<string>();
            for (var i = 0; i < count; i++)
            {
                var attributes = BitConverter.ToUInt32(groups, 8 + i * 16 + 8);
                if ((attributes & SE_GROUP_ENABLED) == 0 || (attributes & SE_GROUP_LOGON_ID) != 0) continue;
                var sid = Sid(groups, 8 + i * 16);
                if (sid != null) names.Add($"{sid.Value}({Account(sid)})");
            }
            Console.WriteLine($"groups={string.Join(",", names)}");
        }
        CloseHandle(token);
    }

    // Run with our own token minus the dropped privileges (a restricted token needs no extra rights).
    static int RunRestricted(string application, string commandLine, List<string> drop)
    {
        var token = OpenToken(Environment.ProcessId, TOKEN_QUERY | TOKEN_DUPLICATE | TOKEN_ASSIGN_PRIMARY);
        if (token == IntPtr.Zero) throw new System.ComponentModel.Win32Exception();
        var deletes = new LUID_AND_ATTRIBUTES[Math.Max(drop.Count, 1)];
        for (var i = 0; i < drop.Count; i++) { KnownPrivilege(drop[i], out var luid); deletes[i].Luid = luid; }
        if (!CreateRestrictedToken(token, 0, 0, IntPtr.Zero, (uint)drop.Count, deletes, 0, IntPtr.Zero, out var restricted))
            throw new System.ComponentModel.Win32Exception();
        CloseHandle(token);
        var startup = new STARTUPINFOW { cb = sizeof(STARTUPINFOW) };
        var line = new StringBuilder(commandLine);
        if (!CreateProcessAsUserW(restricted, application, line, IntPtr.Zero, IntPtr.Zero, true, CREATE_UNICODE_ENVIRONMENT, IntPtr.Zero, null, ref startup, out var pi))
            throw new System.ComponentModel.Win32Exception();
        CloseHandle(restricted);
        CloseHandle(pi.hThread);
        WaitForSingleObject(pi.hProcess, 0xFFFFFFFF);
        GetExitCodeProcess(pi.hProcess, out var code);
        CloseHandle(pi.hProcess);
        return (int)code;
    }

    // ---- getpcaps ----

    static int Getpcaps(string[] args)
    {
        var pids = new List<int>();
        var verbose = false;
        foreach (var a in args)
        {
            if (a is "-h" or "--help") { Console.WriteLine("usage: getpcaps [opts] <pid> [<pid> ...]\n\n  This program displays the privileges (capabilities) of the queried process(es).\n  Options:\n    --help, -h     this message\n    --verbose      also show the account and integrity level\n    --legacy, --ugly, --iab  accepted"); return 0; }
            if (a is "--verbose" or "-v") { verbose = true; continue; }
            if (a is "--legacy" or "--ugly" or "--iab") continue;
            if (!int.TryParse(a, out var pid)) throw new UsageException($"bad pid: {a}");
            pids.Add(pid);
        }
        if (pids.Count == 0) throw new UsageException("usage: getpcaps [opts] <pid> [<pid> ...]");
        var status = 0;
        foreach (var pid in pids)
        {
            var token = OpenToken(pid);
            if (token == IntPtr.Zero)
            {
                Console.Error.WriteLine($"Failed to get cap for pid {pid}: {new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message}");
                status = 1;
                continue;
            }
            Console.WriteLine($"{pid}: {Text(Privileges(token))}");
            if (verbose)
            {
                var (user, integrity, elevated) = Identity(token);
                Console.WriteLine($"  account: {Account(user)}, integrity: {integrity}, elevated: {(elevated ? "yes" : "no")}");
            }
            CloseHandle(token);
        }
        return status;
    }

    // ---- getcap / setcap / filecap ----

    static int Getcap(string[] args)
    {
        var files = new List<string>();
        bool recursive = false, verbose = false;
        foreach (var a in args)
        {
            if (a == "-h") { Console.WriteLine("usage: getcap [-h] [-l] [-n] [-r] [-v] <filename> [<filename> ...]\n\n\tdisplays the capabilities on the queried file(s).\n\tWindows files carry no capabilities, so nothing is shown except with -v."); return 0; }
            if (a == "-r") recursive = true;
            else if (a == "-v") verbose = true;
            else if (a is "-n" or "-l") { }
            else if (a.StartsWith('-')) throw new UsageException("usage: getcap [-h] [-l] [-n] [-r] [-v] <filename> [<filename> ...]");
            else files.Add(a);
        }
        if (files.Count == 0) throw new UsageException("usage: getcap [-h] [-l] [-n] [-r] [-v] <filename> [<filename> ...]");
        var status = 0;
        foreach (var f in files)
        {
            if (!File.Exists(f) && !Directory.Exists(f)) { Console.Error.WriteLine($"Failed to get capabilities of file '{f}' (No such file or directory)"); status = 1; continue; }
            IEnumerable<string> all = recursive && Directory.Exists(f)
                ? Directory.EnumerateFileSystemEntries(f, "*", new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true })
                : [f];
            if (verbose) foreach (var x in all) Console.WriteLine(x);
        }
        return status;
    }

    static int Setcap(string[] args)
    {
        if (args.Length > 0 && args[0] is "-h" or "--help")
        {
            Console.WriteLine("usage: setcap [-h] [-q] [-v] [-n <rootid>] (-r|-|<caps>) <filename> [ ... (-r|-|<capsN>) <filenameN> ]\n" +
                              "Windows files cannot carry capabilities; privileges are granted to accounts (Local Security Policy).");
            return 0;
        }
        if (args.Length < 2) throw new UsageException("usage: setcap [-h] [-q] [-v] (-r|-|<caps>) <filename>");
        Console.Error.WriteLine("setcap: Windows files cannot carry capabilities; grant privileges to an account with Local Security Policy (secpol.msc) instead");
        return 1;
    }

    static int Filecap(string[] args)
    {
        var paths = new List<string>();
        foreach (var a in args)
        {
            switch (a)
            {
                case "-h" or "--help":
                    Console.WriteLine("usage: filecap [-a | -d | path|file [cap1 cap2 ...]]\nWindows files carry no capabilities, so no file is reported; -d lists the privilege names.");
                    return 0;
                case "-d":
                    for (var i = 2; i < 64; i++)
                    {
                        var luid = (long)i;
                        var name = new StringBuilder(128);
                        var length = name.Capacity;
                        if (LookupPrivilegeNameW(null, ref luid, name, ref length)) Console.WriteLine(name);
                    }
                    return 0;
                case "-a": break;
                default: paths.Add(a); break;
            }
        }
        if (paths.Count > 1)
        {
            Console.Error.WriteLine("filecap: Windows files cannot carry capabilities; grant privileges to an account with Local Security Policy instead");
            return 1;
        }
        // Scanning finds nothing to report: no Windows file has capabilities.
        if (paths.Count == 1 && !File.Exists(paths[0]) && !Directory.Exists(paths[0]))
        {
            Console.Error.WriteLine($"filecap: {paths[0]}: No such file or directory");
            return 1;
        }
        return 0;
    }

    // ---- pscap / netcap ----

    sealed record ProcessEntry(int Pid, int Ppid, string Name);

    static List<ProcessEntry> Processes()
    {
        var list = new List<ProcessEntry>();
        var snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
        if (snapshot == -1) return list;
        try
        {
            var entry = new PROCESSENTRY32W { dwSize = (uint)sizeof(PROCESSENTRY32W) };
            for (var ok = Process32FirstW(snapshot, ref entry); ok; ok = Process32NextW(snapshot, ref entry))
                list.Add(new ProcessEntry((int)entry.th32ProcessID, (int)entry.th32ParentProcessID, new string(entry.szExeFile)));
        }
        finally { CloseHandle(snapshot); }
        return list;
    }

    // Privileges a process has turned on, minus SeChangeNotifyPrivilege (every token has that enabled).
    static (string Account, List<string> Enabled)? ProcessPrivileges(int pid)
    {
        var token = OpenToken(pid);
        if (token == IntPtr.Zero) return null;
        try
        {
            var enabled = Privileges(token).Where(p => p.Enabled && p.Name != "SeChangeNotifyPrivilege").Select(p => p.Name).ToList();
            var (user, _, _) = Identity(token);
            var account = Account(user);
            return (account[(account.IndexOf('\\') + 1)..], enabled);
        }
        finally { CloseHandle(token); }
    }

    static string Short(List<string> privileges) =>
        string.Join(", ", privileges.Select(p => p.StartsWith("Se") && p.EndsWith("Privilege") ? p[2..^9].ToLowerInvariant() : p));

    static int Pscap(string[] args)
    {
        var all = false;
        int? only = null;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help": Console.WriteLine("usage: pscap [-a] [-p pid] [--tree]\nLists processes with privileges enabled beyond SeChangeNotifyPrivilege; -a includes those without."); return 0;
                case "-a": all = true; break;
                case "-p": only = ++i < args.Length && int.TryParse(args[i], out var p) ? p : throw new UsageException("-p needs a pid"); break;
                case "--tree": break;
                default: throw new UsageException($"unknown option {args[i]}");
            }
        }
        Console.WriteLine($"{"ppid",-5} {"pid",-5} {"name",-10} {"command",-20} capabilities");
        foreach (var p in Processes().Where(p => p.Pid > 4 && p.Pid != Environment.ProcessId && (only == null || p.Pid == only)))
        {
            if (ProcessPrivileges(p.Pid) is not var (account, enabled)) continue;
            if (enabled.Count == 0 && !all) continue;
            Console.WriteLine($"{p.Ppid,-5} {p.Pid,-5} {Trim(account, 10),-10} {Trim(p.Name, 20),-20} {(enabled.Count == 0 ? "none" : Short(enabled))}");
        }
        return 0;
    }

    static string Trim(string s, int n) => s.Length > n ? s[..n] : s;

    static int Netcap(string[] args)
    {
        foreach (var a in args)
        {
            if (a is "-h" or "--help") { Console.WriteLine("usage: netcap\nLists processes with listening sockets and the privileges they have enabled."); return 0; }
            throw new UsageException($"unknown option {a}");
        }
        var processes = Processes().ToDictionary(p => p.Pid);
        var sockets = IpHelper.Sockets(true, true, true, true)
            .Where(s => !s.Tcp || s.State == IpHelper.TcpListen)
            .GroupBy(s => (s.Pid, s.Tcp, s.V6, s.LocalPort)).Select(g => g.First())
            .OrderBy(s => s.Pid).ThenBy(s => s.LocalPort);
        var cache = new Dictionary<int, (string, List<string>)?>();
        var any = false;
        foreach (var s in sockets)
        {
            if (s.Pid <= 4) continue;
            if (!cache.TryGetValue(s.Pid, out var info)) cache[s.Pid] = info = ProcessPrivileges(s.Pid);
            if (info is not var (account, enabled) || enabled.Count == 0) continue;
            if (!any)
            {
                Console.WriteLine($"{"ppid",-5} {"pid",-5} {"acct",-10} {"command",-20} {"type",-5} {"port",-6} capabilities");
                any = true;
            }
            var proc = processes.GetValueOrDefault(s.Pid);
            Console.WriteLine($"{proc?.Ppid ?? 0,-5} {s.Pid,-5} {Trim(account, 10),-10} {Trim(proc?.Name ?? "?", 20),-20} {(s.Tcp ? "tcp" : "udp") + (s.V6 ? "6" : ""),-5} {s.LocalPort,-6} {Short(enabled)}");
        }
        return 0;
    }

    // ---- captest ----

    static int Captest(string[] args)
    {
        var dropAll = false;
        foreach (var a in args)
        {
            switch (a)
            {
                case "-h" or "--help": Console.WriteLine("usage: captest [--drop-all|--drop-caps|--id] [--text] [--init-grp]"); return 0;
                case "--drop-all" or "--drop-caps": dropAll = true; break;
                case "--text" or "--id" or "--init-grp" or "--lock": break;
                default: throw new UsageException($"unknown option {a}");
            }
        }
        var token = OpenToken(Environment.ProcessId, TOKEN_QUERY | TOKEN_DUPLICATE | TOKEN_IMPERSONATE);
        var (user, integrity, elevated) = Identity(token);
        Console.WriteLine($"User credentials: {Account(user)} ({user?.Value})");
        Console.WriteLine($"Integrity level: {integrity}, elevated: {(elevated ? "yes" : "no")}");
        var privileges = Privileges(token);
        Console.WriteLine($"Current capabilities: {(privileges.Count == 0 ? "none" : Text(privileges))}");
        IntPtr test = IntPtr.Zero;
        if (dropAll)
        {
            // A restricted copy of our token without any privilege, impersonated by this thread.
            if (CreateRestrictedToken(token, DISABLE_MAX_PRIVILEGE, 0, IntPtr.Zero, 0, null, 0, IntPtr.Zero, out var restricted)
                && DuplicateToken(restricted, 2, out test) && SetThreadToken(IntPtr.Zero, test))
                Console.WriteLine("Dropped all privileges (restricted token)");
            CloseHandle(restricted);
        }
        // Reading the SAM hive needs SeBackupPrivilege; the hive is also locked while Windows runs.
        Console.Write("Attempting direct access to the SAM hive...");
        try
        {
            using var f = new FileStream(Path.Combine(Environment.SystemDirectory, "config", "SAM"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            Console.WriteLine("SUCCESS");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Console.WriteLine($"FAILED ({e.Message.TrimEnd('.', ' ')})"); }
        Console.Write("Attempting to enable SeDebugPrivilege...");
        var current = test != IntPtr.Zero ? test : OpenToken(Environment.ProcessId, TOKEN_QUERY | TOKEN_ADJUST_PRIVILEGES);
        if (KnownPrivilege("SeDebugPrivilege", out var debug))
        {
            var tp = new TOKEN_PRIVILEGES { PrivilegeCount = 1, Luid = debug, Attributes = SE_PRIVILEGE_ENABLED };
            var ok = AdjustTokenPrivileges(current, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero) && Marshal.GetLastWin32Error() == 0;
            Console.WriteLine(ok ? "SUCCESS" : "FAILED");
        }
        if (test != IntPtr.Zero) { SetThreadToken(IntPtr.Zero, IntPtr.Zero); CloseHandle(test); }
        else CloseHandle(current);
        var child = OpenToken(Environment.ProcessId);
        Console.WriteLine($"Child capabilities: {(dropAll ? "none" : Text(Privileges(child)))}");
        CloseHandle(child);
        CloseHandle(token);
        return 0;
    }

    // ---- Win32 ----

    const uint TOKEN_ASSIGN_PRIMARY = 0x1, TOKEN_DUPLICATE = 0x2, TOKEN_IMPERSONATE = 0x4, TOKEN_QUERY = 0x8, TOKEN_ADJUST_PRIVILEGES = 0x20;
    const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000, CREATE_UNICODE_ENVIRONMENT = 0x400, TH32CS_SNAPPROCESS = 0x2;
    const uint SE_PRIVILEGE_ENABLED = 0x2, SE_PRIVILEGE_REMOVED = 0x4, SE_GROUP_ENABLED = 0x4, SE_GROUP_LOGON_ID = 0xC0000000;
    const uint DISABLE_MAX_PRIVILEGE = 0x1;
    const int TokenUser = 1, TokenGroups = 2, TokenPrivileges = 3, TokenElevation = 20, TokenIntegrityLevel = 25;

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct LUID_AND_ATTRIBUTES { public long Luid; public uint Attributes; }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct TOKEN_PRIVILEGES { public uint PrivilegeCount; public long Luid; public uint Attributes; }

    [StructLayout(LayoutKind.Sequential)]
    struct STARTUPINFOW
    {
        public int cb;
        public IntPtr lpReserved, lpDesktop, lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PROCESS_INFORMATION { public IntPtr hProcess, hThread; public int dwProcessId, dwThreadId; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct PROCESSENTRY32W
    {
        public uint dwSize, cntUsage, th32ProcessID;
        public IntPtr th32DefaultHeapID;
        public uint th32ModuleID, cntThreads, th32ParentProcessID;
        public int pcPriClassBase;
        public uint dwFlags;
        public fixed char szExeFile[260];
    }

    [DllImport("kernel32.dll")] static extern IntPtr GetCurrentProcess();
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
    [DllImport("advapi32.dll", SetLastError = true)] static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);
    [DllImport("advapi32.dll", SetLastError = true)] static extern bool GetTokenInformation(IntPtr token, int infoClass, byte[] buffer, int length, out int returned);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern bool LookupPrivilegeNameW(string system, ref long luid, StringBuilder name, ref int length);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern bool LookupPrivilegeValueW(string system, string name, out long luid);
    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool CreateRestrictedToken(IntPtr token, uint flags, uint disableSidCount, IntPtr sidsToDisable, uint deletePrivilegeCount,
        LUID_AND_ATTRIBUTES[] privilegesToDelete, uint restrictedSidCount, IntPtr sidsToRestrict, out IntPtr newToken);
    [DllImport("advapi32.dll", SetLastError = true)] static extern bool DuplicateToken(IntPtr token, int level, out IntPtr duplicate);
    [DllImport("advapi32.dll", SetLastError = true)] static extern bool SetThreadToken(IntPtr thread, IntPtr token);
    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool AdjustTokenPrivileges(IntPtr token, bool disableAll, ref TOKEN_PRIVILEGES newState, int length, IntPtr previous, IntPtr returnLength);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool CreateProcessAsUserW(IntPtr token, string application, StringBuilder commandLine, IntPtr processAttributes, IntPtr threadAttributes,
        bool inheritHandles, uint flags, IntPtr environment, string directory, ref STARTUPINFOW startup, out PROCESS_INFORMATION info);
    [DllImport("kernel32.dll")] static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
    [DllImport("kernel32.dll")] static extern bool GetExitCodeProcess(IntPtr process, out uint code);
    [DllImport("kernel32.dll", SetLastError = true)] static extern nint CreateToolhelp32Snapshot(uint flags, uint pid);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool Process32FirstW(nint snapshot, ref PROCESSENTRY32W entry);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool Process32NextW(nint snapshot, ref PROCESSENTRY32W entry);
}
