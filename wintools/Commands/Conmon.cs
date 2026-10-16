using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// conmon: the OCI container monitor podman and CRI-O run next to each container. It starts the
// runtime (`<runtime> create`, or `exec` with --exec) with pipes for the container's stdout and
// stderr, returns once the container exists (the monitor carries on in the background, as conmon's
// double fork does), writes everything the container prints to the log file in the k8s-file format
// ("<RFC 3339 time> stdout F <line>", P for partial lines, --log-size-max rotation), and when the
// container exits writes its status to --exit-dir/<cid> and --persist-dir/exit and runs
// --exit-command. Attach and control sockets (podman attach, resize) are UNIX socket protocols
// with a TTY behind them; they aren't served here, the log carries the output.
static class Conmon
{
    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("conmon", "Monitor an OCI container: run its runtime, log its output, record its exit", Run);

    const string Version = "2.2.1";

    sealed class Options
    {
        public string Cid, Cuuid, Name, Runtime, Bundle = ".", ContainerPidFile, ConmonPidFile, LogPath, LogTag;
        public string ExitDir, ExitCommand, PersistDir, ExecProcessSpec, LogLevel = "info";
        public long LogSizeMax = -1;
        public int ExitDelay, Timeout;
        public bool Exec, Terminal, Stdin, Sync, Background;
        public List<string> RuntimeArgs = new(), RuntimeOpts = new(), ExitCommandArgs = new();
    }

    static int Run(string[] args)
    {
        var o = new Options();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            var eq = a.StartsWith("--") ? a.IndexOf('=') : -1;
            var name = eq > 0 ? a[..eq] : a;
            string Value() => eq > 0 ? a[(eq + 1)..] : ++i < args.Length ? args[i] : throw new UsageException($"Missing argument for option {name}");
            switch (name)
            {
                case "-h" or "--help": Usage(); return 0;
                case "--version": Console.WriteLine($"conmon version {Version}\ncommit: wintools-{Commands.Version}"); return 0;
                case "-c" or "--cid": o.Cid = Value(); break;
                case "-u" or "--cuuid": o.Cuuid = Value(); break;
                case "-n" or "--name": o.Name = Value(); break;
                case "-r" or "--runtime": o.Runtime = Value(); break;
                case "-b" or "--bundle": o.Bundle = Value(); break;
                case "-p" or "--container-pidfile": o.ContainerPidFile = Value(); break;
                case "-P" or "--conmon-pidfile": o.ConmonPidFile = Value(); break;
                case "-l" or "--log-path": o.LogPath = Value(); break;
                case "--log-tag": o.LogTag = Value(); break;
                case "--log-size-max": o.LogSizeMax = long.Parse(Value()); break;
                case "--log-level": o.LogLevel = Value(); break;
                case "--exit-dir": o.ExitDir = Value(); break;
                case "--exit-command": o.ExitCommand = Value(); break;
                case "--exit-command-arg": o.ExitCommandArgs.Add(Value()); break;
                case "--exit-delay": o.ExitDelay = int.Parse(Value()); break;
                case "-T" or "--timeout": o.Timeout = int.Parse(Value()); break;
                case "--persist-dir": o.PersistDir = Value(); break;
                case "--runtime-arg": o.RuntimeArgs.Add(Value()); break;
                case "--runtime-opt": o.RuntimeOpts.Add(Value()); break;
                case "-e" or "--exec": o.Exec = true; break;
                case "--exec-process-spec": o.ExecProcessSpec = Value(); break;
                case "-t" or "--terminal": o.Terminal = true; break;
                case "-i" or "--stdin": o.Stdin = true; break;
                case "--sync": o.Sync = true; break;
                case "--__background": o.Background = true; break;
                case "-s" or "--systemd-cgroup" or "--no-pivot" or "--no-new-keyring" or "--leave-stdin-open" or "--replace-listen-pid"
                    or "--no-sync-log" or "--syslog" or "--full-attach" or "--no-container-partial-message":
                    break;
                case "--socket-dir-path" or "--log-global-size-max" or "--seccomp-notify-socket" or "--seccomp-notify-plugins"
                    or "--restore" or "--restore-arg" or "--api-version" or "-S" or "--attach" or "--log-label" or "--log-allowlist-dir":
                    Value();
                    break;
                default: throw new UsageException($"Unknown option {a}");
            }
        }
        // conmon's own checks, in its order.
        if (o.Cid == null) return Fail("Container ID not provided. Use --cid");
        if (!o.Exec && o.Cuuid == null) return Fail("Container UUID not provided. Use --cuuid");
        if (o.Runtime == null) return Fail("Runtime path not provided. Use --runtime");
        if (!File.Exists(o.Runtime) && !File.Exists(o.Runtime + ".exe")) return Fail($"Runtime path {o.Runtime} is not valid");
        if (o.Exec && o.ExecProcessSpec == null) return Fail("Exec process spec path not provided. Use --exec-process-spec");
        if (o.Terminal) Console.Error.WriteLine("conmon: [warning] no TTY on Windows; the container gets pipes");
        if (o.Stdin) Console.Error.WriteLine("conmon: [warning] stdin is only reachable through the attach socket, which isn't served on Windows");
        return o.Background || o.Sync ? Monitor(o, null) : Detach(args, o);
    }

    static int Fail(string message)
    {
        Console.Error.WriteLine($"conmon: {message}");
        return 1;
    }

    static void Usage() => Console.WriteLine("""
        Usage:
          conmon [OPTION?] - conmon utility

        Help Options:
          -h, --help                  Show help options

        Application Options:
          --api-version               Conmon API version to use
          -b, --bundle                Location of the OCI Bundle path
          -c, --cid                   Identification of Container
          -P, --conmon-pidfile        PID file for the conmon process
          -p, --container-pidfile     PID file for the initial pid inside of container
          -u, --cuuid                 Container UUID
          -e, --exec                  Exec a command into a running container
          --exec-process-spec         Path to the process spec for execution
          --exit-command              Path to the program to execute when the container terminates its execution
          --exit-command-arg          Additional arg to pass to the exit command.  Can be specified multiple times
          --exit-delay                Delay before invoking the exit command (in seconds)
          --exit-dir                  Path to the directory where exit files are written
          -l, --log-path              Log file path (k8s-file:PATH, or a plain path)
          --log-size-max              Maximum size of log file
          --log-tag                   Additional tag to use for logging
          -n, --name                  Container name
          --persist-dir               Persistent directory for a container that can be used for storing container data
          -r, --runtime               Path to store runtime data for the container
          --runtime-arg               Additional arg to pass to the runtime. Can be specified multiple times
          --runtime-opt               Additional opts to pass to the restore or exec command. Can be specified multiple times
          --sync                      Keep the main conmon process as its child by only forking once
          -t, --terminal              Allocate a pseudo-TTY. The default is false
          -T, --timeout               Kill container after specified timeout in seconds.
          --version                   Print the version and exit
        """);

    // conmon forks into the background once the container exists; here a detached copy of this
    // command does the monitoring and signals when the container has been created (or failed).
    static int Detach(string[] args, Options o)
    {
        var token = Guid.NewGuid().ToString("N");
        using var created = new EventWaitHandle(false, EventResetMode.ManualReset, $"Local\\conmon-{token}-created");
        var self = Environment.ProcessPath ?? throw new InvalidOperationException("cannot find this program");
        var psi = new ProcessStartInfo(self) { UseShellExecute = false, CreateNoWindow = true };
        if (!Path.GetFileNameWithoutExtension(self).Equals("conmon", StringComparison.OrdinalIgnoreCase)) psi.ArgumentList.Add("conmon");
        foreach (var a in args) psi.ArgumentList.Add(a);
        psi.ArgumentList.Add("--__background");
        psi.Environment["CONMON_WINTOOLS_TOKEN"] = token;
        psi.RedirectStandardError = true;
        using var monitor = Process.Start(psi)!;
        var errors = new StringBuilder();
        monitor.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (errors) errors.AppendLine(e.Data); };
        monitor.BeginErrorReadLine();
        var which = WaitHandle.WaitAny(new WaitHandle[] { created, new ProcessExited(monitor) }, TimeSpan.FromMinutes(5));
        if (which != 0)
        {
            monitor.WaitForExit(2000);
            lock (errors) Console.Error.Write(errors.ToString());
            return monitor.HasExited ? monitor.ExitCode : 1;
        }
        return 0;
    }

    sealed class ProcessExited : WaitHandle
    {
        public ProcessExited(Process p) => SafeWaitHandle = new Microsoft.Win32.SafeHandles.SafeWaitHandle(p.Handle, ownsHandle: false);
    }

    static int Monitor(Options o, string unused)
    {
        var token = Environment.GetEnvironmentVariable("CONMON_WINTOOLS_TOKEN");
        if (o.ConmonPidFile != null) File.WriteAllText(o.ConmonPidFile, Environment.ProcessId.ToString());
        var logPath = o.LogPath;
        if (logPath != null)
        {
            if (logPath.StartsWith("k8s-file:")) logPath = logPath[9..];
            else if (logPath.StartsWith("journald:") || logPath == "none" || logPath.StartsWith("passthrough"))
            {
                if (logPath.StartsWith("journald")) Console.Error.WriteLine("conmon: [warning] no journald on Windows; logging to the persist dir instead");
                logPath = o.PersistDir != null ? Path.Combine(o.PersistDir, "ctr.log") : null;
            }
        }
        var pidFile = o.ContainerPidFile ?? Path.Combine(Path.GetTempPath(), $"conmon-{o.Cid}.pid");
        File.Delete(pidFile);

        // The runtime, with pipes for the container's output.
        var psi = new ProcessStartInfo(o.Runtime) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = true };
        foreach (var a in o.RuntimeArgs) psi.ArgumentList.Add(a);
        if (o.Exec)
        {
            psi.ArgumentList.Add("exec");
            psi.ArgumentList.Add("--pid-file");
            psi.ArgumentList.Add(pidFile);
            psi.ArgumentList.Add("--process");
            psi.ArgumentList.Add(o.ExecProcessSpec);
            psi.ArgumentList.Add("--detach");
            foreach (var opt in o.RuntimeOpts) psi.ArgumentList.Add(opt);
        }
        else
        {
            psi.ArgumentList.Add("create");
            psi.ArgumentList.Add("--bundle");
            psi.ArgumentList.Add(o.Bundle);
            psi.ArgumentList.Add("--pid-file");
            psi.ArgumentList.Add(pidFile);
        }
        psi.ArgumentList.Add(o.Cid);
        Process runtime;
        try { runtime = Process.Start(psi)!; }
        catch (System.ComponentModel.Win32Exception e) { return Fail($"Failed to start runtime {o.Runtime}: {e.Message}"); }
        runtime.StandardInput.Close();
        using var log = logPath != null ? new LogWriter(logPath, o.LogSizeMax) : null;
        var outTask = Task.Run(() => Pump(runtime.StandardOutput.BaseStream, "stdout", log));
        var errTask = Task.Run(() => Pump(runtime.StandardError.BaseStream, "stderr", log));
        runtime.WaitForExit();
        if (runtime.ExitCode != 0 || !File.Exists(pidFile))
        {
            Task.WaitAll(new[] { outTask, errTask }, 2000);
            return Fail($"Failed to create container: exit status {runtime.ExitCode}");
        }
        var pid = int.Parse(File.ReadAllText(pidFile).Trim());
        var container = OpenProcess(SYNCHRONIZE | PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        // Tell the caller (podman) the container exists: our pid file and the creator's event.
        if (token != null)
            try { using var created = EventWaitHandle.OpenExisting($"Local\\conmon-{token}-created"); created.Set(); }
            catch (WaitHandleCannotBeOpenedException) { }
        // Nobody reads our stderr once the caller has returned.
        if (!o.Sync) Console.SetError(TextWriter.Null);

        int exitCode;
        if (container == IntPtr.Zero) exitCode = -1;
        else
        {
            var timedOut = WaitForSingleObject(container, o.Timeout > 0 ? (uint)o.Timeout * 1000 : INFINITE) == WAIT_TIMEOUT;
            if (timedOut)
            {
                RunRuntime(o, "kill", o.Cid, "KILL");
                WaitForSingleObject(container, INFINITE);
            }
            GetExitCodeProcess(container, out var code);
            CloseHandle(container);
            exitCode = timedOut ? 137 : (int)code;
        }
        Task.WaitAll(new[] { outTask, errTask }, 5000);
        if (o.ExitDir != null)
        {
            Directory.CreateDirectory(o.ExitDir);
            File.WriteAllText(Path.Combine(o.ExitDir, o.Cuuid ?? o.Cid), exitCode.ToString());
        }
        if (o.PersistDir != null)
        {
            Directory.CreateDirectory(o.PersistDir);
            File.WriteAllText(Path.Combine(o.PersistDir, "exit"), exitCode.ToString());
        }
        if (o.ExitCommand != null)
        {
            if (o.ExitDelay > 0) Thread.Sleep(o.ExitDelay * 1000);
            var exit = new ProcessStartInfo(o.ExitCommand) { UseShellExecute = false };
            foreach (var a in o.ExitCommandArgs) exit.ArgumentList.Add(a);
            try { Process.Start(exit)?.WaitForExit(); }
            catch (System.ComponentModel.Win32Exception e) { Console.Error.WriteLine($"conmon: exit command failed: {e.Message}"); }
        }
        return o.Sync ? exitCode : 0;
    }

    static void RunRuntime(Options o, params string[] args)
    {
        var psi = new ProcessStartInfo(o.Runtime) { UseShellExecute = false };
        foreach (var a in o.RuntimeArgs) psi.ArgumentList.Add(a);
        foreach (var a in args) psi.ArgumentList.Add(a);
        try { Process.Start(psi)?.WaitForExit(); }
        catch (System.ComponentModel.Win32Exception) { }
    }

    static void Pump(Stream stream, string pipe, LogWriter log)
    {
        var buffer = new byte[8192];
        int n;
        while ((n = stream.Read(buffer, 0, buffer.Length)) > 0) log?.Write(pipe, buffer.AsSpan(0, n));
    }

    // The k8s-file log: one record per line, "F" when it ended with a newline, "P" for a partial one.
    sealed class LogWriter : IDisposable
    {
        readonly string path;
        readonly long max;
        FileStream file;

        public LogWriter(string path, long max)
        {
            this.path = path;
            this.max = max;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            file = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
        }

        public void Write(string pipe, ReadOnlySpan<byte> data)
        {
            lock (this)
            {
                while (data.Length > 0)
                {
                    var newline = data.IndexOf((byte)'\n');
                    var line = newline >= 0 ? data[..newline] : data;
                    if (line.Length > 0 && line[^1] == '\r') line = line[..^1];
                    var stamp = DateTimeOffset.Now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff00zzz");
                    var header = Encoding.ASCII.GetBytes($"{stamp} {pipe} {(newline >= 0 ? 'F' : 'P')} ");
                    if (max > 0 && file.Length + header.Length + line.Length + 1 > max)
                    {
                        // conmon's rotation: start the file over.
                        file.Dispose();
                        file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
                    }
                    file.Write(header);
                    file.Write(line);
                    file.WriteByte((byte)'\n');
                    data = newline >= 0 ? data[(newline + 1)..] : ReadOnlySpan<byte>.Empty;
                }
                file.Flush();
            }
        }

        public void Dispose() => file.Dispose();
    }

    const uint SYNCHRONIZE = 0x100000, PROCESS_QUERY_LIMITED_INFORMATION = 0x1000, INFINITE = 0xFFFFFFFF, WAIT_TIMEOUT = 0x102;

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(uint access, bool inherit, int pid);

    [DllImport("kernel32.dll")]
    static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

    [DllImport("kernel32.dll")]
    static extern bool GetExitCodeProcess(IntPtr process, out uint exitCode);

    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr handle);
}
