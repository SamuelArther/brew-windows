using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WinTools;

// libselinux's utilities on Windows Mandatory Integrity Control, Windows' built-in mandatory access
// control: every process token and securable object carries an integrity label (Untrusted, Low,
// Medium, High, System) and a lower-labelled subject cannot write up (or read/execute up, when the
// label's policy says so). The label plays the part of an SELinux context: getfilecon/setfilecon read
// and write a file's label (SDDL "S:(ML;;NW;;;LW)"), getpidcon a process's, matchpathcon the label a
// new file at a path would inherit. MIC cannot be turned off, so the mode is always Enforcing.
static class Selinux
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("selinuxenabled", "Exit 0 when mandatory access control (Windows integrity levels) is on", Enabled);
        Commands.Add("getenforce", "Show the mandatory access control mode (Windows integrity levels: Enforcing)", Getenforce);
        Commands.Add("setenforce", "Set the mandatory access control mode (Windows only has Enforcing)", Setenforce);
        Commands.Add("getfilecon", "Show a file's mandatory integrity label (its security context)", Getfilecon);
        Commands.Add("setfilecon", "Set a file's mandatory integrity label (its security context)", Setfilecon);
        Commands.Add("getpidcon", "Show a process's integrity level (its security context)", Getpidcon);
        Commands.Add("matchpathcon", "Show the integrity label a new file at a path would get", Matchpathcon);
    }

    static readonly (string Sddl, string Name, string Sid)[] Levels =
    [
        ("S-1-16-0", "Untrusted", "S-1-16-0"),
        ("LW", "Low", "S-1-16-4096"),
        ("ME", "Medium", "S-1-16-8192"),
        ("MP", "MediumPlus", "S-1-16-8448"),
        ("HI", "High", "S-1-16-12288"),
        ("SI", "System", "S-1-16-16384"),
    ];

    static void Help(string usage, string text) => Console.WriteLine($"usage:  {usage}\n{text}");

    static int Enabled(string[] args)
    {
        if (args.Length > 0 && args[0] is "-h" or "--help") { Help("selinuxenabled", "Exit status 0: Windows Mandatory Integrity Control is always enabled."); return 0; }
        return 0;
    }

    static int Getenforce(string[] args)
    {
        if (args.Length > 0 && args[0] is "-h" or "--help") { Help("getenforce", "Windows Mandatory Integrity Control is always Enforcing."); return 0; }
        if (args.Length > 0) throw new UsageException("usage:  getenforce");
        Console.WriteLine("Enforcing");
        return 0;
    }

    static int Setenforce(string[] args)
    {
        if (args.Length != 1) throw new UsageException("usage:  setenforce [ Enforcing | Permissive | 1 | 0 ]");
        if (args[0] is "-h" or "--help") { Help("setenforce [ Enforcing | Permissive | 1 | 0 ]", "Windows Mandatory Integrity Control cannot be relaxed; only Enforcing is accepted."); return 0; }
        if (args[0] is "1" || args[0].Equals("Enforcing", StringComparison.OrdinalIgnoreCase)) return 0;
        if (args[0] is "0" || args[0].Equals("Permissive", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("setenforce: Windows Mandatory Integrity Control is always enforcing and cannot be switched to permissive");
            return 1;
        }
        throw new UsageException("usage:  setenforce [ Enforcing | Permissive | 1 | 0 ]");
    }

    // "S:(ML;OICI;NW;;;LW)" -> "Low", plus the policy flags beyond the default no-write-up.
    static string FromSddl(string sddl, bool inheritableOnly = false)
    {
        var start = sddl.IndexOf("(ML;", StringComparison.Ordinal);
        if (start < 0) return null;
        var end = sddl.IndexOf(')', start);
        var parts = sddl[(start + 1)..end].Split(';');
        if (parts.Length < 6) return null;
        if (inheritableOnly && !parts[1].Contains("OI") && !parts[1].Contains("CI")) return null;
        var level = Levels.FirstOrDefault(l => l.Sddl == parts[5] || l.Sid == parts[5]).Name ?? parts[5];
        var policy = new List<string>();
        if (parts[2].Contains("NR")) policy.Add("no-read-up");
        if (parts[2].Contains("NX")) policy.Add("no-execute-up");
        return policy.Count > 0 ? $"{level}:{string.Join(",", policy)}" : level;
    }

    static string FileLabel(string path)
    {
        var rc = GetNamedSecurityInfoW(path, SE_FILE_OBJECT, LABEL_SECURITY_INFORMATION, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, out var sd);
        if (rc != 0) throw new System.ComponentModel.Win32Exception(rc);
        try
        {
            if (!ConvertSecurityDescriptorToStringSecurityDescriptorW(sd, 1, LABEL_SECURITY_INFORMATION, out var text, IntPtr.Zero))
                throw new System.ComponentModel.Win32Exception();
            var sddl = Marshal.PtrToStringUni(text);
            LocalFree(text);
            return FromSddl(sddl) ?? "Medium";   // no label means the implicit Medium
        }
        finally { LocalFree(sd); }
    }

    static int Getfilecon(string[] args)
    {
        var paths = args.Where(a => a is not ("-r" or "--raw")).ToList();
        if (paths.Count > 0 && paths[0] is "-h" or "--help") { Help("getfilecon [-r] path...", "Show each path's integrity label (Untrusted, Low, Medium, High, System)."); return 0; }
        if (paths.Count == 0) throw new UsageException("usage:  getfilecon [-r] path...");
        var status = 0;
        foreach (var path in paths)
        {
            try { Console.WriteLine($"{path}\t{FileLabel(path)}"); }
            catch (System.ComponentModel.Win32Exception e)
            {
                Console.Error.WriteLine($"getfilecon: Could not get context for {path}: {e.Message}");
                status = 1;
            }
        }
        return status;
    }

    static int Setfilecon(string[] args)
    {
        if (args.Length > 0 && args[0] is "-h" or "--help")
        {
            Help("setfilecon context path...", "context is an integrity level (Untrusted, Low, Medium, MediumPlus, High, System), optionally\nfollowed by :no-read-up and/or :no-execute-up. Labels above the caller's own level need\nSeRelabelPrivilege.");
            return 0;
        }
        if (args.Length < 2) throw new UsageException("usage:  setfilecon context path...");
        var spec = args[0].Split(':', ',');
        var level = Levels.FirstOrDefault(l => l.Name.Equals(spec[0], StringComparison.OrdinalIgnoreCase) || l.Sddl.Equals(spec[0], StringComparison.OrdinalIgnoreCase));
        if (level.Name == null) throw new UsageException($"invalid context {args[0]} (expected Untrusted, Low, Medium, MediumPlus, High or System)");
        var mask = "NW";
        foreach (var flag in spec.Skip(1))
        {
            if (flag == "no-read-up") mask += "NR";
            else if (flag == "no-execute-up") mask += "NX";
            else if (flag != "no-write-up") throw new UsageException($"invalid policy flag {flag}");
        }
        var status = 0;
        foreach (var path in args[1..])
        {
            var inherit = Directory.Exists(path) ? "OICI" : "";
            var sddl = $"S:(ML;{inherit};{mask};;;{level.Sddl})";
            if (!ConvertStringSecurityDescriptorToSecurityDescriptorW(sddl, 1, out var sd, IntPtr.Zero)) throw new System.ComponentModel.Win32Exception();
            try
            {
                GetSecurityDescriptorSacl(sd, out _, out var sacl, out _);
                var rc = SetNamedSecurityInfoW(path, SE_FILE_OBJECT, LABEL_SECURITY_INFORMATION, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, sacl);
                if (rc != 0)
                {
                    Console.Error.WriteLine($"setfilecon: failed to set context {args[0]} on {path}: {new System.ComponentModel.Win32Exception(rc).Message}");
                    status = 1;
                }
            }
            finally { LocalFree(sd); }
        }
        return status;
    }

    static int Getpidcon(string[] args)
    {
        if (args.Length != 1) throw new UsageException("usage:  getpidcon pid");
        if (args[0] is "-h" or "--help") { Help("getpidcon pid", "Show the integrity level of a process."); return 0; }
        if (!int.TryParse(args[0], out var pid)) throw new UsageException($"invalid pid {args[0]}");
        var process = pid == Environment.ProcessId ? GetCurrentProcess() : OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        var token = IntPtr.Zero;
        if (process == IntPtr.Zero || !OpenProcessToken(process, TOKEN_QUERY, out token))
        {
            var error = Marshal.GetLastWin32Error();
            Console.Error.WriteLine($"getpidcon: getpidcon({pid}) failed: {new System.ComponentModel.Win32Exception(error == 0 ? 5 : error).Message}");
            return 1;
        }
        try
        {
            GetTokenInformation(token, TokenIntegrityLevel, null, 0, out var size);
            var buffer = GC.AllocateUninitializedArray<byte>(size, pinned: true);
            if (!GetTokenInformation(token, TokenIntegrityLevel, buffer, size, out _)) throw new System.ComponentModel.Win32Exception();
            var sidPointer = (IntPtr)BitConverter.ToInt64(buffer, 0);
            if (!ConvertSidToStringSidW(sidPointer, out var text)) throw new System.ComponentModel.Win32Exception();
            var sid = Marshal.PtrToStringUni(text);
            LocalFree(text);
            Console.WriteLine(Levels.FirstOrDefault(l => l.Sid == sid).Name ?? sid);
            return 0;
        }
        finally
        {
            CloseHandle(token);
            if (pid != Environment.ProcessId) CloseHandle(process);
        }
    }

    static int Matchpathcon(string[] args)
    {
        var paths = args.Where(a => !a.StartsWith('-')).ToList();
        if (args.Any(a => a is "-h" or "--help")) { Help("matchpathcon [-n] path...", "Show the integrity label a new file created at each path would inherit."); return 0; }
        if (paths.Count == 0) throw new UsageException("usage:  matchpathcon [-n] path...");
        var noPath = args.Contains("-n");
        foreach (var path in paths)
        {
            var parent = Path.GetDirectoryName(Path.GetFullPath(path));
            string label = null;
            // The nearest folder with an inheritable label decides; Medium otherwise.
            for (var dir = parent; dir != null && label == null; dir = Path.GetDirectoryName(dir))
            {
                if (GetNamedSecurityInfoW(dir, SE_FILE_OBJECT, LABEL_SECURITY_INFORMATION, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, out var sd) != 0) break;
                if (ConvertSecurityDescriptorToStringSecurityDescriptorW(sd, 1, LABEL_SECURITY_INFORMATION, out var text, IntPtr.Zero))
                {
                    label = FromSddl(Marshal.PtrToStringUni(text), inheritableOnly: true);
                    LocalFree(text);
                }
                LocalFree(sd);
            }
            Console.WriteLine(noPath ? label ?? "Medium" : $"{path}\t{label ?? "Medium"}");
        }
        return 0;
    }

    // ---- Win32 ----

    const int SE_FILE_OBJECT = 1;
    const uint LABEL_SECURITY_INFORMATION = 0x10, TOKEN_QUERY = 0x8, PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
    const int TokenIntegrityLevel = 25;

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
    static extern int GetNamedSecurityInfoW(string name, int type, uint info, IntPtr owner, IntPtr group, IntPtr dacl, IntPtr sacl, out IntPtr sd);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
    static extern int SetNamedSecurityInfoW(string name, int type, uint info, IntPtr owner, IntPtr group, IntPtr dacl, IntPtr sacl);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool ConvertSecurityDescriptorToStringSecurityDescriptorW(IntPtr sd, uint revision, uint info, out IntPtr text, IntPtr length);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool ConvertStringSecurityDescriptorToSecurityDescriptorW(string text, uint revision, out IntPtr sd, IntPtr size);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool GetSecurityDescriptorSacl(IntPtr sd, out bool present, out IntPtr sacl, out bool defaulted);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool ConvertSidToStringSidW(IntPtr sid, out IntPtr text);

    [DllImport("advapi32.dll", SetLastError = true)] static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);
    [DllImport("advapi32.dll", SetLastError = true)] static extern bool GetTokenInformation(IntPtr token, int infoClass, byte[] buffer, int length, out int returned);
    [DllImport("kernel32.dll")] static extern IntPtr GetCurrentProcess();
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
    [DllImport("kernel32.dll")] static extern IntPtr LocalFree(IntPtr memory);
}
