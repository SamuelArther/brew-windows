using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace WinTools;

// reattach-to-user-namespace (tmux-MacOSX-pasteboard) / reattach-to-session-namespace (pam-reattach):
// run a program in the user's interactive logon session. On macOS, processes that outlive or start
// outside the GUI login (tmux servers, ssh logins) can't reach the pasteboard or Touch ID; on Windows the
// same happens to processes in a non-interactive session (OpenSSH logons, services, scheduled tasks),
// which can't reach the user's clipboard, desktop or Windows Hello prompts. The program is started in
// the user's active console or Remote Desktop session with the token of one of the user's processes
// there (or WTSQueryUserToken when running as SYSTEM), its output piped back here. Already in that
// session, or with no interactive session to move to, the program simply runs in place.
// pam_reattach.so itself, the PAM module, has no counterpart: Windows has no PAM stack.
static class Reattach
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("reattach-to-user-namespace", "Run a program in the user's interactive session (e.g. tmux, ssh)", RunUserNamespace);
        Commands.Add("reattach-to-session-namespace", "Run a program in a user's interactive logon session", RunSessionNamespace);
    }

    const string UsageText = """

            Reattach to the user's interactive logon session (the console or Remote
            Desktop session where the clipboard, desktop and Windows Hello live), then
            run the program with args. If "-l" is given, start a shell as a login shell.
        """;

    static int RunUserNamespace(string[] args)
    {
        var login = false;
        if (args.Length > 0)
        {
            if (args[0] == "-l") { login = true; args = args[1..]; }
            else if (args[0] is "-v" or "--version")
            {
                Console.WriteLine("reattach-to-user-namespace version 2.9\n    Supported OSes: Windows 10-11");
                return 0;
            }
            else if (args[0] is "-h" or "--help")
            {
                Console.WriteLine("usage: reattach-to-user-namespace [-l] <program> [args...]\n" + UsageText);
                return 0;
            }
            else if (args[0].StartsWith('-'))
            {
                Console.Error.WriteLine($"reattach-to-user-namespace: unknown option: {args[0]}");
                Console.Error.WriteLine("usage: reattach-to-user-namespace [-l] <program> [args...]\n" + UsageText);
                return 2;
            }
        }
        if (args.Length == 0)
        {
            Console.Error.WriteLine("usage: reattach-to-user-namespace [-l] <program> [args...]\n" + UsageText);
            return 1;
        }
        if (login) args = LoginShell(args);
        return Execute("reattach-to-user-namespace", null, args, mustAttach: false);
    }

    static int RunSessionNamespace(string[] args)
    {
        string user = null;
        var i = 0;
        for (; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg is "-h" or "--help")
            {
                Console.WriteLine("Usage: reattach-to-session-namespace [-u user] <program> [args...]\n" +
                                  "Run the program in the user's interactive logon session (default: the current user).");
                return 0;
            }
            if (arg is "-u" or "--uid")
            {
                if (++i == args.Length) throw new UsageException($"option {arg} requires an argument");
                user = args[i];
            }
            else if (arg.StartsWith("--uid=")) user = arg[6..];
            else if (arg == "--") { i++; break; }
            else if (arg.StartsWith('-')) throw new UsageException($"unknown option {arg}");
            else break;
        }
        if (i == args.Length)
        {
            Console.Error.WriteLine("Usage: reattach-to-session-namespace [-u user] <program> [args...]");
            return 1;
        }
        return Execute("reattach-to-session-namespace", user, args[i..], mustAttach: user != null);
    }

    // `-l`: login shells. Unix shells take -l; PowerShell 7 takes -Login; cmd has no such notion.
    internal static string[] LoginShell(string[] args)
    {
        var name = Path.GetFileNameWithoutExtension(args[0]).ToLowerInvariant();
        string flag = name switch
        {
            "bash" or "sh" or "zsh" or "ksh" or "mksh" or "dash" or "fish" or "tcsh" or "csh" or "nu" => "-l",
            "pwsh" => "-Login",
            _ => null,
        };
        return flag == null ? args : [args[0], flag, .. args[1..]];
    }

    static int Execute(string tool, string user, string[] command, bool mustAttach)
    {
        var (app, arguments) = ResolveCommand(command);
        var me = WindowsIdentity.GetCurrent();
        var targetUser = user ?? me.Name;
        ProcessIdToSessionId((uint)Environment.ProcessId, out var current);

        var session = FindSession(targetUser);
        if (session == null)
        {
            if (mustAttach) throw new InvalidOperationException($"{targetUser} has no interactive session to attach to");
            Console.Error.WriteLine($"{tool}: warning: no interactive session for {targetUser}; running in place");
            return RunHere(app, arguments);
        }
        if (session.Value == current) return RunHere(app, arguments);

        var token = UserToken(session.Value, targetUser, me.IsSystem);
        if (token == IntPtr.Zero)
        {
            if (mustAttach) throw new InvalidOperationException($"can't get a logon token for session {session.Value} (run as SYSTEM to attach to another user)");
            Console.Error.WriteLine($"{tool}: warning: can't reach session {session.Value}; running in place");
            return RunHere(app, arguments);
        }
        try { return RunInSession(tool, token, app, arguments); }
        finally { CloseHandle(token); }
    }

    // The active console or Remote Desktop session where `user` is logged on; the console wins ties.
    static uint? FindSession(string user)
    {
        var console = WTSGetActiveConsoleSessionId();
        uint? found = null;
        if (!WTSEnumerateSessionsW(IntPtr.Zero, 0, 1, out var info, out var count)) return null;
        try
        {
            var size = Marshal.SizeOf<WTS_SESSION_INFO>();
            for (var i = 0; i < count; i++)
            {
                var s = Marshal.PtrToStructure<WTS_SESSION_INFO>(info + i * size);
                if (s.State != WTSActive || s.SessionId == 0) continue;
                var name = SessionString(s.SessionId, WTSUserName);
                if (string.IsNullOrEmpty(name)) continue;
                var domain = SessionString(s.SessionId, WTSDomainName);
                if (!SameUser(user, domain, name)) continue;
                if (s.SessionId == console) return s.SessionId;
                found ??= s.SessionId;
            }
        }
        finally { WTSFreeMemory(info); }
        return found;
    }

    internal static bool SameUser(string wanted, string domain, string name)
    {
        var slash = wanted.IndexOf('\\');
        return slash < 0
            ? wanted.Equals(name, StringComparison.OrdinalIgnoreCase)
            : wanted[(slash + 1)..].Equals(name, StringComparison.OrdinalIgnoreCase)
              && wanted[..slash].Equals(domain, StringComparison.OrdinalIgnoreCase);
    }

    static string SessionString(uint session, int infoClass)
    {
        if (!WTSQuerySessionInformationW(IntPtr.Zero, session, infoClass, out var buffer, out _)) return null;
        try { return Marshal.PtrToStringUni(buffer); }
        finally { WTSFreeMemory(buffer); }
    }

    // A primary token for the session: SYSTEM asks WTS for it; anyone else borrows the token of one of
    // their own processes in that session (explorer first), which their own token's DACL allows.
    static IntPtr UserToken(uint session, string user, bool isSystem)
    {
        if (isSystem && WTSQueryUserToken(session, out var wtsToken)) return wtsToken;
        var me = WindowsIdentity.GetCurrent().User;
        var candidates = Process.GetProcesses().Where(p => p.SessionId == session)
            .OrderBy(p => p.ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase) ? 0
                        : p.ProcessName.Equals("sihost", StringComparison.OrdinalIgnoreCase) ? 1 : 2);
        foreach (var process in candidates)
        {
            var handle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)process.Id);
            if (handle == IntPtr.Zero) continue;
            try
            {
                if (!OpenProcessToken(handle, TOKEN_QUERY | TOKEN_DUPLICATE | TOKEN_ASSIGN_PRIMARY, out var token)) continue;
                try
                {
                    using var identity = new WindowsIdentity(token);
                    var matches = isSystem ? SameUser(user, "", identity.Name.Split('\\')[^1]) || identity.Name.Equals(user, StringComparison.OrdinalIgnoreCase)
                                           : identity.User == me;
                    if (!matches) continue;
                    if (DuplicateTokenEx(token, MAXIMUM_ALLOWED, IntPtr.Zero, SecurityImpersonation, TokenPrimary, out var primary)) return primary;
                }
                finally { CloseHandle(token); }
            }
            finally { CloseHandle(handle); }
        }
        return IntPtr.Zero;
    }

    static int RunHere(string app, string arguments)
    {
        var psi = new ProcessStartInfo(app, arguments) { UseShellExecute = false };
        using var p = Process.Start(psi)!;
        p.WaitForExit();
        return p.ExitCode;
    }

    // Started in the other session with our own pipes for stdio: console handles don't cross sessions.
    static unsafe int RunInSession(string tool, IntPtr token, string app, string arguments)
    {
        using var stdin = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
        using var stdout = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
        using var stderr = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
        var si = new STARTUPINFO
        {
            cb = sizeof(STARTUPINFO),
            dwFlags = STARTF_USESTDHANDLES,
            hStdInput = stdin.ClientSafePipeHandle.DangerousGetHandle(),
            hStdOutput = stdout.ClientSafePipeHandle.DangerousGetHandle(),
            hStdError = stderr.ClientSafePipeHandle.DangerousGetHandle(),
        };
        // CreateProcess may write into the command-line buffer, so each attempt gets its own copy.
        var full = (Quote(app) + " " + arguments).TrimEnd() + "\0";
        fixed (char* desktop = "winsta0\\default")
        fixed (char* first = full.ToCharArray())
        fixed (char* second = full.ToCharArray())
        {
            si.lpDesktop = desktop;
            var flags = CREATE_UNICODE_ENVIRONMENT | CREATE_NO_WINDOW;
            var cwd = Environment.CurrentDirectory;
            if (!CreateProcessAsUserW(token, app, first, IntPtr.Zero, IntPtr.Zero, true, flags, IntPtr.Zero, cwd, ref si, out var pi))
            {
                var error = Marshal.GetLastWin32Error();
                if (error != ERROR_PRIVILEGE_NOT_HELD
                    || !CreateProcessWithTokenW(token, 0, app, second, flags, IntPtr.Zero, cwd, ref si, out pi))
                {
                    Console.Error.WriteLine($"{tool}: warning: reattaching failed ({new Win32Exception(Marshal.GetLastWin32Error()).Message}); running in place");
                    return RunHere(app, arguments);
                }
            }
            stdin.DisposeLocalCopyOfClientHandle();
            stdout.DisposeLocalCopyOfClientHandle();
            stderr.DisposeLocalCopyOfClientHandle();
            CloseHandle(pi.hThread);
            var outPump = Task.Run(() => stdout.CopyTo(Console.OpenStandardOutput()));
            var errPump = Task.Run(() => stderr.CopyTo(Console.OpenStandardError()));
            var input = new Thread(() =>
            {
                try { Console.OpenStandardInput().CopyTo(stdin); stdin.Dispose(); }
                catch (IOException) { }
                catch (ObjectDisposedException) { }
            }) { IsBackground = true };
            input.Start();
            WaitForSingleObject(pi.hProcess, uint.MaxValue);
            GetExitCodeProcess(pi.hProcess, out var code);
            CloseHandle(pi.hProcess);
            Task.WaitAll([outPump, errPump], 5000);
            return (int)code;
        }
    }

    // (application path, quoted argument string). Batch files run through cmd.exe.
    internal static (string App, string Arguments) ResolveCommand(string[] command)
    {
        var path = FindProgram(command[0]) ?? throw new InvalidOperationException($"{command[0]}: command not found");
        var rest = string.Join(" ", command[1..].Select(Quote));
        if (Path.GetExtension(path).ToLowerInvariant() is ".cmd" or ".bat")
        {
            var cmdExe = Path.Combine(Environment.SystemDirectory, "cmd.exe");
            return (cmdExe, "/d /s /c \"" + Quote(path) + (rest.Length > 0 ? " " + rest : "") + "\"");
        }
        return (path, rest);
    }

    static string FindProgram(string name)
    {
        var exts = Path.HasExtension(name) ? [""]
            : (Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD").Split(';', StringSplitOptions.RemoveEmptyEntries);
        IEnumerable<string> dirs = name.Contains('\\') || name.Contains('/')
            ? [""]
            : (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries).Prepend(Environment.CurrentDirectory);
        foreach (var dir in dirs)
            foreach (var ext in exts)
            {
                var candidate = Path.GetFullPath(Path.Combine(dir, name + ext));
                if (File.Exists(candidate)) return candidate;
            }
        return null;
    }

    // MSVCRT command-line quoting.
    internal static string Quote(string arg)
    {
        if (arg.Length > 0 && arg.IndexOfAny([' ', '\t', '"']) < 0) return arg;
        var sb = new StringBuilder("\"");
        var slashes = 0;
        foreach (var c in arg)
        {
            if (c == '\\') { slashes++; continue; }
            if (c == '"') sb.Append('\\', slashes * 2 + 1);
            else sb.Append('\\', slashes);
            slashes = 0;
            sb.Append(c);
        }
        return sb.Append('\\', slashes * 2).Append('"').ToString();
    }

    const int WTSActive = 0, WTSUserName = 5, WTSDomainName = 7;
    const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
    const uint TOKEN_ASSIGN_PRIMARY = 0x1, TOKEN_DUPLICATE = 0x2, TOKEN_QUERY = 0x8, MAXIMUM_ALLOWED = 0x02000000;
    const int SecurityImpersonation = 2, TokenPrimary = 1;
    const uint CREATE_UNICODE_ENVIRONMENT = 0x400, CREATE_NO_WINDOW = 0x08000000;
    const int STARTF_USESTDHANDLES = 0x100;
    const int ERROR_PRIVILEGE_NOT_HELD = 1314;

    [StructLayout(LayoutKind.Sequential)]
    struct WTS_SESSION_INFO
    {
        public uint SessionId;
        public IntPtr pWinStationName;
        public int State;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct STARTUPINFO
    {
        public int cb;
        public char* lpReserved;
        public char* lpDesktop;
        public char* lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PROCESS_INFORMATION
    {
        public IntPtr hProcess, hThread;
        public uint dwProcessId, dwThreadId;
    }

    [DllImport("kernel32.dll")]
    static extern bool ProcessIdToSessionId(uint pid, out uint session);

    [DllImport("kernel32.dll")]
    static extern uint WTSGetActiveConsoleSessionId();

    [DllImport("wtsapi32.dll", SetLastError = true)]
    static extern bool WTSEnumerateSessionsW(IntPtr server, int reserved, int version, out IntPtr info, out int count);

    [DllImport("wtsapi32.dll", SetLastError = true)]
    static extern bool WTSQuerySessionInformationW(IntPtr server, uint session, int infoClass, out IntPtr buffer, out int bytes);

    [DllImport("wtsapi32.dll")]
    static extern void WTSFreeMemory(IntPtr memory);

    [DllImport("wtsapi32.dll", SetLastError = true)]
    static extern bool WTSQueryUserToken(uint session, out IntPtr token);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool DuplicateTokenEx(IntPtr token, uint access, IntPtr attributes, int level, int type, out IntPtr newToken);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern unsafe bool CreateProcessAsUserW(IntPtr token, string app, char* commandLine, IntPtr processAttributes,
        IntPtr threadAttributes, bool inheritHandles, uint flags, IntPtr environment, string cwd, ref STARTUPINFO si, out PROCESS_INFORMATION pi);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern unsafe bool CreateProcessWithTokenW(IntPtr token, uint logonFlags, string app, char* commandLine, uint flags,
        IntPtr environment, string cwd, ref STARTUPINFO si, out PROCESS_INFORMATION pi);

    [DllImport("kernel32.dll")]
    static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

    [DllImport("kernel32.dll")]
    static extern bool GetExitCodeProcess(IntPtr process, out uint code);

    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr handle);
}
