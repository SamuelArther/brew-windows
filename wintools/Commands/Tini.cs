using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// tini / tini-static / docker-init: a tiny init for (Windows) containers. The child runs inside a
// kill-on-close job object, Windows' equivalent of a PID namespace: everything it starts belongs to the
// job, and whatever is still running when the child exits is killed with tini, the way a container's
// processes die with its PID 1. Ctrl+C / Ctrl+Break reach the child through the shared console (tini
// itself ignores them); a close/logoff/shutdown event terminates the child's job. The child's exit
// status is passed through, with -e remapping chosen codes to 0.
static class Tini
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("tini", "Tiny but valid init for containers (job-object supervisor)", Run);
        Commands.Add("tini-static", "Tiny but valid init for containers (job-object supervisor)", Run);
        Commands.Add("docker-init", "Tiny but valid init for containers (tini, as used by docker run --init)", Run);
    }

    const string TiniVersion = "0.19.0";
    static int verbosity = 1;

    static int Run(string[] args)
    {
        verbosity = int.TryParse(Environment.GetEnvironmentVariable("TINI_VERBOSITY"), out var v) ? v : 1;
        var killGroup = Environment.GetEnvironmentVariable("TINI_KILL_PROCESS_GROUP") is { Length: > 0 };
        var warnReap = false;
        string parentDeathSignal = null;
        var remap = new HashSet<int>();
        var i = 0;
        // Options end at the first non-option (getopt "+"), or after "--".
        for (; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg == "--") { i++; break; }
            if (arg == "--version") { Console.WriteLine($"tini version {TiniVersion} - wintools {Commands.Version}"); return 0; }
            if (!arg.StartsWith('-') || arg.Length < 2) break;
            for (var j = 1; j < arg.Length; j++)
            {
                switch (arg[j])
                {
                    case 'h': Usage(Console.Out); return 0;
                    case 'l': Console.WriteLine(License); return 0;
                    case 's': break;        // a job object already holds every descendant (subreaper)
                    case 'v': verbosity++; break;
                    case 'w': warnReap = true; break;
                    case 'g': killGroup = true; break;
                    case 'p' or 'e':
                        var value = j + 1 < arg.Length ? arg[(j + 1)..] : ++i < args.Length ? args[i] : throw new UsageException($"option requires an argument -- '{arg[j]}'");
                        if (arg[j] == 'p') parentDeathSignal = ParseSignal(value);
                        else if (!int.TryParse(value, out var code) || code is < 0 or > 255) throw new UsageException($"Not a valid option for -e: {value}");
                        else remap.Add(code);
                        j = arg.Length;
                        break;
                    default: throw new UsageException($"invalid option -- '{arg[j]}' (see tini -h)");
                }
            }
        }
        if (i >= args.Length) throw new UsageException("no program given (see tini -h)");
        var program = args[i];
        var childArgs = args[(i + 1)..];
        var status = Supervise(program, childArgs, killGroup, warnReap, parentDeathSignal);
        return remap.Contains(status) ? 0 : status;
    }

    static void Usage(TextWriter w) => w.WriteLine($"""
        tini (tini version {TiniVersion} - wintools {Commands.Version})
        Usage: tini [OPTIONS] PROGRAM -- [ARGS] | --version

        Execute a program under the supervision of a valid init process (tini)

        Command line options:

          --version: Show version and exit.
          -h: Show this help message and exit.
          -s: Register as a process subreaper (always on: the child runs in a job object).
          -p SIGNAL: Trigger SIGNAL when parent dies, e.g. "-p SIGKILL".
          -v: Generate more verbose output. Repeat up to 3 times.
          -w: Print a warning when processes are left behind by the child.
          -g: Send signals to the child's whole job (process group).
          -e EXIT_CODE: Remap EXIT_CODE (from 0 to 255) to 0.
          -l: Show license and exit.

        Environment variables:

          TINI_SUBREAPER: Register as a process subreaper (always on).
          TINI_VERBOSITY: Set the verbosity level (default: 1).
          TINI_KILL_PROCESS_GROUP: Send signals to the child's process group.
        """);

    static string ParseSignal(string name)
    {
        var known = new[] { "SIGHUP", "SIGINT", "SIGQUIT", "SIGKILL", "SIGTERM", "SIGUSR1", "SIGUSR2" };
        var upper = name.ToUpperInvariant();
        if (!upper.StartsWith("SIG")) upper = "SIG" + upper;
        return known.Contains(upper) ? upper : throw new UsageException($"Invalid signal name: {name}");
    }

    static void Log(int level, string kind, string message)
    {
        if (verbosity >= level) Console.Error.WriteLine($"[{kind,-5} tini ({Environment.ProcessId})] {message}");
    }

    static int Supervise(string program, string[] args, bool killGroup, bool warnReap, string parentDeathSignal)
    {
        var path = Sandbox.FindProgram(program);
        if (path == null)
        {
            Log(1, "FATAL", $"exec {program} failed: No such file or directory");
            return 127;
        }
        var job = CreateJobObjectW(IntPtr.Zero, null);
        if (job == IntPtr.Zero) throw new System.ComponentModel.Win32Exception();
        var limits = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
        limits.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;
        SetInformationJobObject(job, JobObjectExtendedLimitInformation, ref limits, Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>());

        // .cmd/.bat children run through cmd.exe, the way CreateProcess itself would start them.
        var ext = Path.GetExtension(path).ToLowerInvariant();
        var commandLine = new StringBuilder();
        string application = path;
        if (ext is ".cmd" or ".bat")
        {
            application = Path.Combine(Environment.SystemDirectory, "cmd.exe");
            commandLine.Append("cmd.exe /d /s /c \"").Append(Sandbox.Quote(path));
            foreach (var a in args) commandLine.Append(' ').Append(Sandbox.Quote(a));
            commandLine.Append('"');
        }
        else
        {
            commandLine.Append(Sandbox.Quote(path));
            foreach (var a in args) commandLine.Append(' ').Append(Sandbox.Quote(a));
        }

        var startup = new STARTUPINFOW { cb = Marshal.SizeOf<STARTUPINFOW>() };
        var flags = CREATE_SUSPENDED | CREATE_UNICODE_ENVIRONMENT | (killGroup ? CREATE_NEW_PROCESS_GROUP : 0u);
        if (!CreateProcessW(application, commandLine, IntPtr.Zero, IntPtr.Zero, true, flags, IntPtr.Zero, null, ref startup, out var pi))
        {
            var error = Marshal.GetLastWin32Error();
            Log(1, "FATAL", $"exec {program} failed: {new System.ComponentModel.Win32Exception(error).Message}");
            return error == 2 ? 127 : 126;
        }
        AssignProcessToJobObject(job, pi.hProcess);
        ResumeThread(pi.hThread);
        CloseHandle(pi.hThread);
        Log(2, "INFO", $"Spawned child process '{program}' with pid '{pi.dwProcessId}'");

        // Console events: Ctrl+C/Break already reach the child through the console; tini outlives them.
        // With -g, Ctrl+Break is also sent explicitly to the child's process group.
        handler = ctrl =>
        {
            if (ctrl is CTRL_C_EVENT or CTRL_BREAK_EVENT)
            {
                if (killGroup && ctrl == CTRL_BREAK_EVENT) GenerateConsoleCtrlEvent(CTRL_BREAK_EVENT, pi.dwProcessId);
                Log(2, "DEBUG", $"Passing signal: '{(ctrl == CTRL_C_EVENT ? "Interrupt" : "Quit")}'");
                return true;
            }
            TerminateJobObject(job, 143);   // close / logoff / shutdown: SIGTERM semantics
            return false;
        };
        SetConsoleCtrlHandler(handler, true);

        // -p: watch our own parent; when it goes away, deliver the signal to the child (terminate the job).
        if (parentDeathSignal != null && ParentProcessId() is int ppid and > 0)
        {
            var parent = OpenProcess(SYNCHRONIZE, false, ppid);
            if (parent != IntPtr.Zero)
                new Thread(() =>
                {
                    WaitForSingleObject(parent, INFINITE);
                    Log(2, "INFO", $"Parent died, sending {parentDeathSignal} to the child");
                    TerminateJobObject(job, parentDeathSignal == "SIGKILL" ? 137u : 143u);
                }) { IsBackground = true }.Start();
        }

        WaitForSingleObject(pi.hProcess, INFINITE);
        GetExitCodeProcess(pi.hProcess, out var code);
        CloseHandle(pi.hProcess);
        Log(2, "INFO", $"Main child exited normally (with status '{code}')");

        // Anything still in the job is an orphan of the child: report it (-w), then the job dies with us.
        var accounting = new JOBOBJECT_BASIC_ACCOUNTING_INFORMATION();
        if (QueryInformationJobObject(job, JobObjectBasicAccountingInformation, ref accounting, Marshal.SizeOf<JOBOBJECT_BASIC_ACCOUNTING_INFORMATION>(), IntPtr.Zero)
            && accounting.ActiveProcesses > 0)
            Log(warnReap ? 1 : 3, "WARN", $"Killing {accounting.ActiveProcesses} process(es) the child left behind");
        TerminateJobObject(job, 137);
        CloseHandle(job);
        GC.KeepAlive(handler);
        return (int)code;
    }

    static ConsoleCtrlHandler handler;

    static unsafe int? ParentProcessId()
    {
        var info = new PROCESS_BASIC_INFORMATION();
        return NtQueryInformationProcess(GetCurrentProcess(), 0, ref info, sizeof(PROCESS_BASIC_INFORMATION), out _) == 0
            ? (int)info.InheritedFromUniqueProcessId : null;
    }

    const string License = """
        The MIT License (MIT)

        Copyright (c) 2015 Thomas Orozco <thomas@orozco.fr>

        Permission is hereby granted, free of charge, to any person obtaining a copy
        of this software and associated documentation files (the "Software"), to deal
        in the Software without restriction, including without limitation the rights
        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        copies of the Software, and to permit persons to whom the Software is
        furnished to do so, subject to the following conditions:

        The above copyright notice and this permission notice shall be included in
        all copies or substantial portions of the Software.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
        THE SOFTWARE.
        """;

    // ---- Win32 ----

    const uint CREATE_SUSPENDED = 0x4, CREATE_NEW_PROCESS_GROUP = 0x200, CREATE_UNICODE_ENVIRONMENT = 0x400;
    const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000, SYNCHRONIZE = 0x100000, INFINITE = 0xFFFFFFFF;
    const int JobObjectBasicAccountingInformation = 1, JobObjectExtendedLimitInformation = 9;
    const uint CTRL_C_EVENT = 0, CTRL_BREAK_EVENT = 1;

    delegate bool ConsoleCtrlHandler(uint ctrlType);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct STARTUPINFOW
    {
        public int cb;
        public string lpReserved, lpDesktop, lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }

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
    struct IO_COUNTERS { public ulong a, b, c, d, e, f; }

    [StructLayout(LayoutKind.Sequential)]
    struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public UIntPtr ProcessMemoryLimit, JobMemoryLimit, PeakProcessMemoryUsed, PeakJobMemoryUsed;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct JOBOBJECT_BASIC_ACCOUNTING_INFORMATION
    {
        public long TotalUserTime, TotalKernelTime, ThisPeriodTotalUserTime, ThisPeriodTotalKernelTime;
        public uint TotalPageFaultCount, TotalProcesses, ActiveProcesses, TotalTerminatedProcesses;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PROCESS_BASIC_INFORMATION
    {
        public IntPtr ExitStatus, PebBaseAddress, AffinityMask, BasePriority, UniqueProcessId, InheritedFromUniqueProcessId;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool CreateProcessW(string application, StringBuilder commandLine, IntPtr processAttributes, IntPtr threadAttributes,
        bool inheritHandles, uint flags, IntPtr environment, string directory, ref STARTUPINFOW startup, out PROCESS_INFORMATION info);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr CreateJobObjectW(IntPtr attributes, string name);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool SetInformationJobObject(IntPtr job, int infoClass, ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION info, int length);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool QueryInformationJobObject(IntPtr job, int infoClass, ref JOBOBJECT_BASIC_ACCOUNTING_INFORMATION info, int length, IntPtr returned);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool TerminateJobObject(IntPtr job, uint exitCode);

    [DllImport("kernel32.dll")]
    static extern uint ResumeThread(IntPtr thread);

    [DllImport("kernel32.dll")]
    static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

    [DllImport("kernel32.dll")]
    static extern bool GetExitCodeProcess(IntPtr process, out uint exitCode);

    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(uint access, bool inherit, int pid);

    [DllImport("kernel32.dll")]
    static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll")]
    static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandler handler, bool add);

    [DllImport("kernel32.dll")]
    static extern bool GenerateConsoleCtrlEvent(uint ctrlEvent, int processGroupId);

    [DllImport("ntdll.dll")]
    static extern int NtQueryInformationProcess(IntPtr process, int infoClass, ref PROCESS_BASIC_INFORMATION info, int length, out int returned);
}
