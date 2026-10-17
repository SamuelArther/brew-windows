using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Brew.Ruby;

// Windows process plumbing behind the interpreter's spawn/exec/fork/Open3/IO.popen/PTY:
//  - CreateProcess passes the child exactly the handles it should get (PROC_THREAD_ATTRIBUTE_HANDLE_LIST), the
//    way Ruby's close-on-exec default does, so a pipe end never leaks into an unrelated child and hides EOF.
//  - Every child gets its own job object, so killing it takes its whole tree (a .cmd wrapper and the server
//    it started); during a `brew test` run all children are also in one session job that is killed at the end.
//  - PTY.spawn uses a ConPTY pseudoconsole (Windows 10 1809+).
static class Win32Process
{
    const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000, CREATE_UNICODE_ENVIRONMENT = 0x00000400, CREATE_SUSPENDED = 0x00000004,
        CREATE_NEW_PROCESS_GROUP = 0x00000200, CREATE_NO_WINDOW = 0x08000000;
    const int STARTF_USESTDHANDLES = 0x00000100;
    static readonly IntPtr PROC_THREAD_ATTRIBUTE_HANDLE_LIST = 0x00020002, PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = 0x00020016;
    const uint DUPLICATE_SAME_ACCESS = 2;
    const uint INFINITE = 0xFFFFFFFF, WAIT_OBJECT_0 = 0;
    const int JobObjectExtendedLimitInformation = 9;
    const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;

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
    struct STARTUPINFOEX { public STARTUPINFO StartupInfo; public IntPtr lpAttributeList; }

    [StructLayout(LayoutKind.Sequential)]
    struct PROCESS_INFORMATION { public IntPtr hProcess, hThread; public int dwProcessId, dwThreadId; }

    [StructLayout(LayoutKind.Sequential)]
    struct COORD { public short X, Y; }

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
    struct IO_COUNTERS { public ulong a, b, c, d, e, f; }

    [StructLayout(LayoutKind.Sequential)]
    struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public UIntPtr ProcessMemoryLimit, JobMemoryLimit, PeakProcessMemoryUsed, PeakJobMemoryUsed;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SECURITY_ATTRIBUTES { public int nLength; public IntPtr lpSecurityDescriptor; public int bInheritHandle; }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool CreateProcessW(string app, char[] cmdLine, IntPtr pa, IntPtr ta, bool inherit, uint flags, IntPtr env,
        string cwd, ref STARTUPINFOEX si, out PROCESS_INFORMATION pi);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool InitializeProcThreadAttributeList(IntPtr list, int count, int flags, ref IntPtr size);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool UpdateProcThreadAttribute(IntPtr list, uint flags, IntPtr attr, IntPtr value, IntPtr size, IntPtr prev, IntPtr retSize);
    [DllImport("kernel32.dll")]
    static extern void DeleteProcThreadAttributeList(IntPtr list);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool DuplicateHandle(IntPtr srcProc, IntPtr src, IntPtr dstProc, out IntPtr dst, uint access, bool inherit, uint options);
    [DllImport("kernel32.dll")]
    static extern IntPtr GetCurrentProcess();
    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool CloseHandle(IntPtr h);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern uint ResumeThread(IntPtr thread);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern uint WaitForSingleObject(IntPtr h, uint ms);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GetExitCodeProcess(IntPtr h, out uint code);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool TerminateProcess(IntPtr h, uint code);
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern IntPtr CreateJobObjectW(IntPtr sa, string name);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool SetInformationJobObject(IntPtr job, int cls, ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION info, int size);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool AssignProcessToJobObject(IntPtr job, IntPtr proc);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool TerminateJobObject(IntPtr job, uint code);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool CreatePipe(out IntPtr read, out IntPtr write, IntPtr sa, int size);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr GetStdHandle(int which);
    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern int CreatePseudoConsole(COORD size, IntPtr input, IntPtr output, uint flags, out IntPtr hpc);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern int ResizePseudoConsole(IntPtr hpc, COORD size);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern void ClosePseudoConsole(IntPtr hpc);
    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool ReadFile(IntPtr h, byte[] buf, int n, out int read, IntPtr overlapped);
    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool WriteFile(IntPtr h, byte[] buf, int n, out int written, IntPtr overlapped);
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern IntPtr CreateFileW(string name, uint access, uint share, IntPtr sa, uint disposition, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool SetFilePointerEx(IntPtr h, long distance, IntPtr newPos, uint method);
    [DllImport("kernel32.dll")]
    internal static extern int GetCurrentThreadId();
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenThread(uint access, bool inherit, int id);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool CancelSynchronousIo(IntPtr thread);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool PeekNamedPipe(IntPtr h, IntPtr buf, int size, IntPtr read, out int avail, IntPtr left);

    static readonly IntPtr Invalid = new(-1);

    // ---- the `brew test` session job: everything a test starts dies with it ----

    static readonly object sessionLock = new();
    static IntPtr sessionJob;

    public static void BeginSession()
    {
        lock (sessionLock)
        {
            if (sessionJob != IntPtr.Zero) return;
            sessionJob = NewJob(killOnClose: true);
        }
    }

    // Kills every process started during the session (and their descendants), then waits briefly for them to go.
    public static void EndSession()
    {
        IntPtr job;
        lock (sessionLock) { job = sessionJob; sessionJob = IntPtr.Zero; }
        if (job == IntPtr.Zero) return;
        TerminateJobObject(job, 1);
        CloseHandle(job);
    }

    static IntPtr NewJob(bool killOnClose)
    {
        var job = CreateJobObjectW(IntPtr.Zero, null);
        if (job == IntPtr.Zero || !killOnClose) return job;
        var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
        info.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;
        SetInformationJobObject(job, JobObjectExtendedLimitInformation, ref info, Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>());
        return job;
    }

    // A process started some other way (System.Diagnostics.Process for system/shell_output) joins the session.
    public static void Track(System.Diagnostics.Process p)
    {
        lock (sessionLock)
        {
            if (sessionJob == IntPtr.Zero) return;
            try { AssignProcessToJobObject(sessionJob, p.Handle); }
            catch (InvalidOperationException) { }
        }
    }

    // ---- handles ----

    public static IntPtr StdHandle(int fd)
    {
        var h = GetStdHandle(fd switch { 0 => -10, 1 => -11, _ => -12 });
        return h == Invalid ? IntPtr.Zero : h;
    }

    // An anonymous pipe; neither end is inheritable (spawn hands a child its own inheritable copy).
    public static (IntPtr Read, IntPtr Write) Pipe()
    {
        if (!CreatePipe(out var r, out var w, IntPtr.Zero, 65536)) throw new Win32Exception();
        return (r, w);
    }

    // How many bytes a pipe holds right now (-1 when it isn't a pipe or is broken).
    public static int Available(IntPtr h) => PeekNamedPipe(h, IntPtr.Zero, 0, IntPtr.Zero, out var n, IntPtr.Zero) ? n : -1;

    // A file opened for a redirect: "r", "w" (truncate), "a" (append), "r+"/"w+"/"a+". Shared, so the
    // test can read the log while the child still writes it.
    public static IntPtr OpenFile(string path, string mode)
    {
        const uint GENERIC_READ = 0x80000000, GENERIC_WRITE = 0x40000000, FILE_SHARE_ALL = 7;
        const uint OPEN_EXISTING = 3, CREATE_ALWAYS = 2, OPEN_ALWAYS = 4;
        if (string.Equals(path, "NUL", StringComparison.OrdinalIgnoreCase) || path == "/dev/null") path = @"\\.\NUL";
        var m = mode.Replace("b", "").Replace("t", "");
        uint access = m switch { "r" => GENERIC_READ, "w" or "a" => GENERIC_WRITE, _ => GENERIC_READ | GENERIC_WRITE };
        uint disposition = m[0] switch { 'r' => OPEN_EXISTING, 'w' => CREATE_ALWAYS, _ => OPEN_ALWAYS };
        var h = CreateFileW(path, access, FILE_SHARE_ALL, IntPtr.Zero, disposition, 0x80, IntPtr.Zero);
        if (h == Invalid) throw new Win32Exception();
        if (m[0] == 'a') SetFilePointerEx(h, 0, IntPtr.Zero, 2);
        return h;
    }

    // File.read of a log a running child still writes: allowed in Ruby, a sharing violation with File.ReadAllBytes.
    public static byte[] ReadAllShared(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 1, FileOptions.SequentialScan);
        var ms = new MemoryStream();
        fs.CopyTo(ms);
        return ms.ToArray();
    }

    // Stops a synchronous ReadFile that another thread is blocked in (closing a pipe end being read).
    public static void CancelRead(int threadId)
    {
        const uint THREAD_TERMINATE = 0x0001;
        var t = OpenThread(THREAD_TERMINATE, false, threadId);
        if (t == IntPtr.Zero) return;
        CancelSynchronousIo(t);
        CloseHandle(t);
    }

    // ---- CreateProcess ----

    public sealed class Spec
    {
        public string CommandLine;
        public string Cwd;
        public IDictionary<string, string> Env;
        public IntPtr StdIn, StdOut, StdErr;   // handles in this process; Zero = the NUL device
        public PseudoConsole Pty;              // ConPTY: the child's console (std handles only when given)
        public bool NewProcessGroup;
    }

    public static ChildProcess Spawn(Spec s)
    {
        var self = GetCurrentProcess();
        var dups = new List<IntPtr>();
        var opened = new List<IntPtr>();
        IntPtr Inheritable(IntPtr h)
        {
            if (h == IntPtr.Zero)
            {
                h = OpenFile("NUL", "r+");
                opened.Add(h);
            }
            if (!DuplicateHandle(self, h, self, out var d, 0, true, DUPLICATE_SAME_ACCESS)) throw new Win32Exception();
            dups.Add(d);
            return d;
        }
        IntPtr list = IntPtr.Zero, handleArray = IntPtr.Zero, envBlock = IntPtr.Zero;
        try
        {
            // Always STARTF_USESTDHANDLES: without it a console child gets our std handles even under a pseudoconsole;
            // a PTY child's NULL entries become its console.
            var si = new STARTUPINFOEX();
            si.StartupInfo.cb = Marshal.SizeOf<STARTUPINFOEX>();
            {
                // Each distinct handle once (a handle list rejects duplicates); `2>&1` reuses stdout's copy.
                var map = new Dictionary<IntPtr, IntPtr>();
                IntPtr Get(IntPtr h, int fd)
                {
                    if (h == IntPtr.Zero && s.Pty != null) return IntPtr.Zero;
                    if (h != IntPtr.Zero && map.TryGetValue(h, out var d)) return d;
                    d = Inheritable(h);
                    if (h != IntPtr.Zero) map[h] = d;
                    return d;
                }
                si.StartupInfo.dwFlags = STARTF_USESTDHANDLES;
                si.StartupInfo.hStdInput = Get(s.StdIn, 0);
                si.StartupInfo.hStdOutput = Get(s.StdOut, 1);
                si.StartupInfo.hStdError = Get(s.StdErr, 2);
            }
            int attrs = (dups.Count > 0 ? 1 : 0) + (s.Pty != null ? 1 : 0);
            if (attrs > 0)
            {
                var size = IntPtr.Zero;
                InitializeProcThreadAttributeList(IntPtr.Zero, attrs, 0, ref size);
                list = Marshal.AllocHGlobal(size);
                if (!InitializeProcThreadAttributeList(list, attrs, 0, ref size)) throw new Win32Exception();
                if (dups.Count > 0)
                {
                    handleArray = Marshal.AllocHGlobal(IntPtr.Size * dups.Count);
                    for (int k = 0; k < dups.Count; k++) Marshal.WriteIntPtr(handleArray, k * IntPtr.Size, dups[k]);
                    if (!UpdateProcThreadAttribute(list, 0, PROC_THREAD_ATTRIBUTE_HANDLE_LIST, handleArray, IntPtr.Size * dups.Count, IntPtr.Zero, IntPtr.Zero))
                        throw new Win32Exception();
                }
                if (s.Pty != null && !UpdateProcThreadAttribute(list, 0, PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE, s.Pty.Handle, IntPtr.Size, IntPtr.Zero, IntPtr.Zero))
                    throw new Win32Exception();
                si.lpAttributeList = list;
            }
            uint flags = EXTENDED_STARTUPINFO_PRESENT | CREATE_UNICODE_ENVIRONMENT | CREATE_SUSPENDED;
            if (s.NewProcessGroup) flags |= CREATE_NEW_PROCESS_GROUP;
            // Without a console of our own a console child would get a new (visible) one.
            if (s.Pty == null && GetConsoleWindow() == IntPtr.Zero) flags |= CREATE_NO_WINDOW;
            if (s.Env != null) envBlock = Marshal.StringToHGlobalUni(EnvBlock(s.Env));
            var cmd = (s.CommandLine + "\0").ToCharArray();
            if (!CreateProcessW(null, cmd, IntPtr.Zero, IntPtr.Zero, dups.Count > 0, flags, envBlock, s.Cwd, ref si, out var pi))
                throw new Win32Exception();
            IntPtr job;
            lock (sessionLock)
            {
                if (sessionJob != IntPtr.Zero) AssignProcessToJobObject(sessionJob, pi.hProcess);
                job = NewJob(killOnClose: false);
                if (job != IntPtr.Zero && !AssignProcessToJobObject(job, pi.hProcess)) { CloseHandle(job); job = IntPtr.Zero; }
            }
            ResumeThread(pi.hThread);
            CloseHandle(pi.hThread);
            return new ChildProcess(pi.dwProcessId, pi.hProcess, job);
        }
        finally
        {
            foreach (var d in dups) CloseHandle(d);
            foreach (var h in opened) CloseHandle(h);
            if (list != IntPtr.Zero) { DeleteProcThreadAttributeList(list); Marshal.FreeHGlobal(list); }
            if (handleArray != IntPtr.Zero) Marshal.FreeHGlobal(handleArray);
            if (envBlock != IntPtr.Zero) Marshal.FreeHGlobal(envBlock);
        }
    }

    // CreateProcess wants the block sorted, case-insensitively.
    static string EnvBlock(IDictionary<string, string> env)
    {
        var sb = new StringBuilder();
        foreach (var kv in env.Where(kv => kv.Key.Length > 0 && kv.Value != null).OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
            sb.Append(kv.Key).Append('=').Append(kv.Value).Append('\0');
        if (sb.Length == 0) sb.Append('\0');
        return sb.Append('\0').ToString();
    }

    // The MSVCRT rules CommandLineToArgvW and C runtimes parse (what .NET's ArgumentList does).
    public static string Quote(string arg)
    {
        if (arg.Length > 0 && arg.IndexOfAny(new[] { ' ', '\t', '"', '\n', '\v' }) < 0) return arg;
        var sb = new StringBuilder("\"");
        for (int i = 0; i < arg.Length; i++)
        {
            int slashes = 0;
            while (i < arg.Length && arg[i] == '\\') { slashes++; i++; }
            if (i == arg.Length) { sb.Append('\\', slashes * 2); break; }
            if (arg[i] == '"') sb.Append('\\', slashes * 2 + 1).Append('"');
            else sb.Append('\\', slashes).Append(arg[i]);
        }
        return sb.Append('"').ToString();
    }

    // ---- processes ----

    public sealed class ChildProcess
    {
        public readonly int Pid;
        readonly IntPtr handle;
        IntPtr job;
        int? exitCode;
        public int? TermSig;   // set when Process.kill ended it
        public Action OnSignal;   // PTY children: INT is a ^C on their console

        public ChildProcess(int pid, IntPtr h, IntPtr j) { Pid = pid; handle = h; job = j; }

        public bool WaitForExit(int ms)
        {
            if (exitCode != null) return true;
            if (WaitForSingleObject(handle, ms < 0 ? INFINITE : (uint)ms) != WAIT_OBJECT_0) return false;
            GetExitCodeProcess(handle, out var code);
            exitCode = (int)code;
            return true;
        }

        public bool HasExited => WaitForExit(0);
        public int ExitCode => WaitForExit(0) ? exitCode.Value : throw new InvalidOperationException("still running");

        // The process and everything it started (its job), like a signal to a process group.
        public void Terminate(int sig)
        {
            if (HasExited) return;
            TermSig ??= sig;
            if (job == IntPtr.Zero || !TerminateJobObject(job, 1)) TerminateProcess(handle, 1);
            WaitForExit(5000);
        }
    }

    // ---- ConPTY ----

    public sealed class PseudoConsole
    {
        public IntPtr Handle;
        public readonly IntPtr Input, Output;   // our ends: we write the child's keyboard, read its screen
        public short Rows = 24, Cols = 80;
        int closed;

        PseudoConsole(IntPtr h, IntPtr input, IntPtr output) { Handle = h; Input = input; Output = output; }

        public static PseudoConsole Create(short rows, short cols)
        {
            var (inRead, inWrite) = Pipe();
            var (outRead, outWrite) = Pipe();
            int hr;
            IntPtr hpc;
            try { hr = CreatePseudoConsole(new COORD { X = cols, Y = rows }, inRead, outWrite, 0, out hpc); }
            catch (EntryPointNotFoundException) { hr = unchecked((int)0x80004001); hpc = IntPtr.Zero; }
            CloseHandle(inRead);
            CloseHandle(outWrite);
            if (hr != 0)
            {
                CloseHandle(inWrite);
                CloseHandle(outRead);
                throw new Win32Exception(hr, "CreatePseudoConsole failed (needs Windows 10 1809 or later)");
            }
            return new PseudoConsole(hpc, inWrite, outRead) { Rows = rows, Cols = cols };
        }

        public void Resize(short rows, short cols)
        {
            Rows = rows;
            Cols = cols;
            if (Volatile.Read(ref closed) == 0) ResizePseudoConsole(Handle, new COORD { X = cols, Y = rows });
        }

        // Ends the console (its last output is flushed, then our output pipe reads EOF). Called once the
        // child is gone; it must run while something drains the output pipe.
        public void Close()
        {
            if (Interlocked.Exchange(ref closed, 1) != 0) return;
            ClosePseudoConsole(Handle);
        }
    }
}
