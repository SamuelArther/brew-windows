using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// strace: run a program and report what it does at the OS boundary. Linux strace prints each system
// call; the stock Windows mechanism that needs no extra tooling or drivers is the Win32 debugging API
// (CreateProcess with DEBUG_PROCESS, then WaitForDebugEvent). It reports the events the OS surfaces to
// a debugger - process start/exit, thread start/exit, DLL load/unload, debug output and exceptions -
// which this prints strace-style, and the traced program's own exit status is passed through. Detailed
// per-call file and registry tracing is not in the debug API; -e trace=file points at the Windows
// Performance Recorder / Process Monitor for that. No privilege beyond launching the program is needed.
static unsafe class Strace
{
    const string Version = "7.2";

    [ModuleInitializer]
    internal static void Register() => Commands.Add("strace", "Run a program and trace its process, thread, DLL and exception events", Run);

    sealed class Options
    {
        public bool Follow;            // -f: trace child processes too
        public bool Summary;           // -c: only a summary count table
        public bool Timestamps;        // -t / -tt
        public bool Relative;          // -r
        public TextWriter Out = Console.Error;   // strace writes its trace to stderr by default
        public HashSet<string> Classes = ["process", "thread", "dll", "signal"];
    }

    static int Run(string[] args)
    {
        var o = new Options();
        var program = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (program.Count > 0) { program.Add(a); continue; }
            string Next() => i + 1 < args.Length ? args[++i] : throw new UsageException($"option {a} needs an argument");
            switch (a)
            {
                case "-h" or "--help": Help(); return 0;
                case "-V" or "--version": Console.WriteLine($"strace -- version {Version} (wintools, Win32 debug API)"); return 0;
                case "-f" or "--follow-forks": o.Follow = true; break;
                case "-c" or "--summary-only": o.Summary = true; break;
                case "-t": o.Timestamps = true; break;
                case "-tt" or "-ttt": o.Timestamps = true; break;
                case "-r": o.Relative = true; break;
                case "-T" or "-q" or "-qq" or "-v": break;   // accepted for compatibility
                case "-o" or "--output": o.Out = new StreamWriter(Next()) { AutoFlush = true }; break;
                case "-e" or "--expression": ParseExpr(Next(), o); break;
                case "-p" or "--attach":
                    throw new InvalidOperationException("attaching to a running process (-p) is not supported; strace launches the program itself");
                default:
                    if (a.StartsWith("-e")) { ParseExpr(a[2..], o); break; }
                    if (a.StartsWith("-o")) { o.Out = new StreamWriter(a[2..]) { AutoFlush = true }; break; }
                    if (a.StartsWith('-')) throw new UsageException($"invalid option '{a}' (see strace --help)");
                    program.Add(a);
                    break;
            }
        }
        if (program.Count == 0) throw new UsageException("must have PROG [ARGS] (see strace --help)");
        return Trace(program, o);
    }

    static void ParseExpr(string expr, Options o)
    {
        var value = expr.Contains('=') ? expr[(expr.IndexOf('=') + 1)..] : expr;
        o.Classes = [];
        foreach (var token in value.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            switch (token)
            {
                case "all": o.Classes.UnionWith(["process", "thread", "dll", "signal"]); break;
                case "process": o.Classes.UnionWith(["process", "thread"]); break;   // process management (exec/exit/clone)
                case "thread": o.Classes.Add("thread"); break;
                case "dll" or "memory" or "mmap": o.Classes.Add("dll"); break;
                case "signal" or "exception": o.Classes.Add("signal"); break;
                case "file" or "desc" or "network" or "trace":
                    Console.Error.WriteLine($"strace: per-call '{token}' tracing is not available through the Windows debug API;");
                    Console.Error.WriteLine("use `sysprof record` (Windows Performance Recorder) or Process Monitor for file/registry/network I/O.");
                    break;
            }
        }
        if (o.Classes.Count == 0) o.Classes = ["process", "thread", "dll", "signal"];
    }

    static int Trace(List<string> program, Options o)
    {
        var commandLine = BuildCommandLine(program);
        var si = new STARTUPINFOW { cb = (uint)sizeof(STARTUPINFOW) };
        var flags = (o.Follow ? 0x1u : 0x2u) | 0x00000200;   // DEBUG_PROCESS | DEBUG_ONLY_THIS_PROCESS; CREATE_NEW_PROCESS_GROUP
        var cmd = Marshal.StringToHGlobalUni(commandLine);
        PROCESS_INFORMATION pi;
        try
        {
            if (!CreateProcessW(null, cmd, IntPtr.Zero, IntPtr.Zero, false, flags, IntPtr.Zero, null, in si, out pi))
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"strace: {program[0]}: cannot run");
        }
        finally { Marshal.FreeHGlobal(cmd); }
        CloseHandle(pi.hThread);   // keep pi.hProcess open to read OutputDebugString text; closed at the end

        var counts = new Dictionary<string, int>();
        var start = DateTime.Now;
        var last = start;
        var exitCode = 0;
        var buffer = Marshal.AllocHGlobal(192);
        var running = 1;
        var firstBreakpoint = true;
        try
        {
            while (running > 0)
            {
                if (!WaitForDebugEvent(buffer, 0xFFFFFFFF)) break;
                var code = Marshal.ReadInt32(buffer, 0);
                var pid = (uint)Marshal.ReadInt32(buffer, 4);
                var continueStatus = 0x00010002u;   // DBG_CONTINUE
                string line = null, kind = null;
                switch (code)
                {
                    case 3:   // CREATE_PROCESS_DEBUG_EVENT
                        kind = "process";
                        var image = ResolvePath(Marshal.ReadIntPtr(buffer, 16)) ?? program[0];   // hFile
                        line = $"execve(\"{image}\", [{ArgList(program, pid == pi.dwProcessId)}], 0x0 /* env */) = 0";
                        CloseHandle(Marshal.ReadIntPtr(buffer, 16));
                        if (pid != pi.dwProcessId) running++;
                        break;
                    case 5:   // EXIT_PROCESS_DEBUG_EVENT
                        kind = "process";
                        var procExit = Marshal.ReadInt32(buffer, 16);
                        if (pid == pi.dwProcessId) exitCode = procExit;
                        line = $"+++ exited with {procExit} +++";
                        running--;
                        break;
                    case 2:   // CREATE_THREAD_DEBUG_EVENT
                        kind = "thread";
                        line = $"clone(child_tid={Marshal.ReadInt32(buffer, 8)}) = {Marshal.ReadInt32(buffer, 8)}";
                        break;
                    case 4:   // EXIT_THREAD_DEBUG_EVENT
                        kind = "thread";
                        line = $"thread {Marshal.ReadInt32(buffer, 8)} exited with {Marshal.ReadInt32(buffer, 16)}";
                        break;
                    case 6:   // LOAD_DLL_DEBUG_EVENT
                        kind = "dll";
                        var dll = ResolvePath(Marshal.ReadIntPtr(buffer, 16)) ?? "<unknown>";   // hFile
                        line = $"mmap(NULL, 0, PROT_READ|PROT_EXEC, MAP_FILE, \"{dll}\") = 0x{(ulong)Marshal.ReadIntPtr(buffer, 24):x}";
                        CloseHandle(Marshal.ReadIntPtr(buffer, 16));
                        break;
                    case 7:   // UNLOAD_DLL_DEBUG_EVENT
                        kind = "dll";
                        line = $"munmap(0x{(ulong)Marshal.ReadIntPtr(buffer, 16):x}) = 0";
                        break;
                    case 8:   // OUTPUT_DEBUG_STRING_EVENT
                        kind = "signal";
                        line = $"write(2, \"{ReadDebugString(pi.hProcess, buffer, pid)}\") /* OutputDebugString */";
                        break;
                    case 1:   // EXCEPTION_DEBUG_EVENT
                        kind = "signal";
                        var exCode = (uint)Marshal.ReadInt32(buffer, 16);
                        if (firstBreakpoint && exCode == 0x80000003) { firstBreakpoint = false; break; }   // initial breakpoint: swallow
                        line = $"--- {SignalName(exCode)} (code=0x{exCode:X8}) ---";
                        continueStatus = 0x80010001;   // DBG_EXCEPTION_NOT_HANDLED: let the app handle it
                        break;
                }
                if (kind != null) counts[kind] = counts.GetValueOrDefault(kind) + 1;
                if (line != null && !o.Summary && o.Classes.Contains(kind))
                {
                    var now = DateTime.Now;
                    var prefix = o.Relative ? $"{(now - last).TotalSeconds,11:0.000000} " : o.Timestamps ? now.ToString("HH:mm:ss.ffffff") + " " : "";
                    if (o.Follow) prefix = $"[pid {pid,5}] " + prefix;
                    o.Out.WriteLine(prefix + line);
                    last = now;
                }
                ContinueDebugEvent(pid, (uint)Marshal.ReadInt32(buffer, 8), continueStatus);
            }
        }
        finally { Marshal.FreeHGlobal(buffer); CloseHandle(pi.hProcess); }

        if (o.Summary) WriteSummary(o.Out, counts);
        if (o.Out != Console.Error) o.Out.Dispose();
        return exitCode;
    }

    static string ArgList(List<string> program, bool topLevel)
        => topLevel ? string.Join(", ", program.Select(a => "\"" + a.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"")) : "...";

    static void WriteSummary(TextWriter w, Dictionary<string, int> counts)
    {
        var total = counts.Values.Sum();
        w.WriteLine("% time     calls  event");
        w.WriteLine("------ --------- ----------------");
        foreach (var (kind, n) in counts.OrderByDescending(k => k.Value))
            w.WriteLine($"{(total == 0 ? 0 : 100.0 * n / total),6:0.00} {n,9}  {kind}");
        w.WriteLine("------ --------- ----------------");
        w.WriteLine($"100.00 {total,9}  total");
    }

    static string SignalName(uint code) => code switch
    {
        0xC0000005 => "SIGSEGV {EXCEPTION_ACCESS_VIOLATION}",
        0x80000003 => "SIGTRAP {BREAKPOINT}",
        0xC00000FD => "SIGSEGV {STACK_OVERFLOW}",
        0xC0000094 => "SIGFPE {INTEGER_DIVIDE_BY_ZERO}",
        0xC000001D => "SIGILL {ILLEGAL_INSTRUCTION}",
        0x40010006 => "DBG_PRINTEXCEPTION_C",
        _ => "exception",
    };

    // Resolve a file handle to a readable path (used for the process image and loaded DLLs).
    static string ResolvePath(IntPtr hFile)
    {
        if (hFile == IntPtr.Zero || hFile == new IntPtr(-1)) return null;
        var buf = new char[520];
        var n = GetFinalPathNameByHandleW(hFile, buf, (uint)buf.Length, 0);
        if (n == 0 || n > buf.Length) return null;
        var path = new string(buf, 0, (int)n);
        return path.StartsWith(@"\\?\") ? path[4..] : path;
    }

    static string ReadDebugString(IntPtr hProcess, IntPtr buffer, uint pid)
    {
        try
        {
            var addr = Marshal.ReadIntPtr(buffer, 16);
            var len = (ushort)Marshal.ReadInt16(buffer, 26);
            var unicode = Marshal.ReadInt16(buffer, 24) != 0;
            if (hProcess == IntPtr.Zero || len == 0 || len > 4096) return "";
            var bytes = new byte[len * (unicode ? 2 : 1)];
            fixed (byte* p = bytes)
                if (!ReadProcessMemory(hProcess, addr, (IntPtr)p, (nuint)bytes.Length, out _)) return "";
            var text = unicode ? Encoding.Unicode.GetString(bytes) : Encoding.ASCII.GetString(bytes);
            return text.TrimEnd('\0', '\n', '\r').Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
        catch (Exception e) when (e is AccessViolationException or ArgumentException) { return ""; }
    }

    // Join program + args into a single command line, quoting arguments that contain spaces.
    static string BuildCommandLine(List<string> program)
    {
        var sb = new StringBuilder();
        foreach (var arg in program)
        {
            if (sb.Length > 0) sb.Append(' ');
            if (arg.Length > 0 && !arg.Contains(' ') && !arg.Contains('"')) sb.Append(arg);
            else sb.Append('"').Append(arg.Replace("\"", "\\\"")).Append('"');
        }
        return sb.ToString();
    }

    static void Help() => Console.WriteLine("""
        usage: strace [-fcrt] [-o FILE] [-e trace=CLASS[,...]] PROG [ARGS]

        Run PROG and trace the OS-level events the Windows debugger interface reports: process and
        thread creation/exit, DLL load/unload, debug output and exceptions. The trace goes to stderr
        (or -o FILE); PROG's own output and exit status are preserved.

          -f            trace child processes as they are created
          -c            print only a summary count of events
          -t, -tt       prefix each line with a timestamp
          -r            prefix each line with the time since the previous line
          -o FILE       write the trace to FILE instead of stderr
          -e trace=SET  limit to event classes: process, thread, dll, signal (or all)
          -V            print version
          -h            print this help

        The debugger interface does not expose individual file/registry/network calls. For that
        level of detail use `sysprof record` (Windows Performance Recorder) or Process Monitor.
        """);

    // ---- interop ----

    [StructLayout(LayoutKind.Sequential)]
    struct PROCESS_INFORMATION { public IntPtr hProcess; public IntPtr hThread; public uint dwProcessId; public uint dwThreadId; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct STARTUPINFOW
    {
        public uint cb;
        public IntPtr lpReserved, lpDesktop, lpTitle;
        public uint dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public ushort wShowWindow, cbReserved2;
        public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool CreateProcessW(string app, IntPtr commandLine, IntPtr procAttrs, IntPtr threadAttrs, bool inherit, uint flags, IntPtr env, string cwd, in STARTUPINFOW si, out PROCESS_INFORMATION pi);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool WaitForDebugEvent(IntPtr debugEvent, uint ms);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool ContinueDebugEvent(uint pid, uint tid, uint status);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool CloseHandle(IntPtr h);
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern uint GetFinalPathNameByHandleW(IntPtr hFile, char[] path, uint len, uint flags);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr address, IntPtr buffer, nuint size, out nuint read);
}
