using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WinTools;

// runc / crun: an OCI runtime for Windows processes. A container is a bundle (config.json + root
// file system) whose process runs in its own job object: the job is the cgroup (memory, CPU rate
// and process-count limits from linux.resources or windows.resources, accounting for `events
// --stats`, everything the process starts belongs to it and dies with it). The lifecycle is the OCI
// one: `create` starts a monitor (this program, detached) that creates the process suspended and
// waits; `start` resumes it; `kill` sends Ctrl+Break (SIGTERM/SIGINT/SIGHUP/SIGQUIT) to its process
// group, or terminates the job (SIGKILL); `delete` releases it. State lives under --root as runc's
// state.json. Bind mounts become directory junctions inside the root file system, and hooks run with
// the state on stdin. Windows has no mount, PID or network namespaces, so the root file system is
// where the process starts (args[0], cwd and PATH resolve inside it) rather than a chroot, Linux
// kernel file systems (proc, sysfs, tmpfs, cgroup ...) are skipped, and Linux (ELF) executables are
// refused. Checkpoint/restore (CRIU) has no Windows counterpart.
static class Oci
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("runc", "Run OCI containers as Windows processes in job objects (runc CLI)", a => Run("runc", a));
        Commands.Add("crun", "Run OCI containers as Windows processes in job objects (crun CLI)", a => Run("crun", a));
    }

    const string RuncVersion = "1.5.2", CrunVersion = "1.30.1", SpecVersion = "1.2.1";

    sealed class Globals
    {
        public string Tool, Root, Log, LogFormat = "text";
        public bool Debug;
    }

    static int Run(string tool, string[] args)
    {
        var g = new Globals { Tool = tool };
        var i = 0;
        for (; i < args.Length; i++)
        {
            var a = args[i];
            if (!a.StartsWith('-')) break;
            var (name, inline) = SplitOption(a);
            switch (name)
            {
                case "-h" or "--help": Usage(tool); return 0;
                case "-v" or "--version": PrintVersion(tool); return 0;
                case "--debug": g.Debug = true; break;
                case "--root": g.Root = inline ?? Next(args, ref i, name); break;
                case "--log": g.Log = inline ?? Next(args, ref i, name); break;
                case "--log-format": g.LogFormat = inline ?? Next(args, ref i, name); break;
                case "--criu" or "--cgroup-manager": if (inline == null) Next(args, ref i, name); break;
                case "--systemd-cgroup" or "--rootless" or "--no-pivot" or "--no-new-keyring": break;   // no cgroup managers or keyrings
                default: throw new UsageException($"flag provided but not defined: {a}");
            }
        }
        g.Root ??= DefaultRoot(tool);
        if (i >= args.Length) { Usage(tool); return 0; }
        var command = args[i];
        var rest = args[(i + 1)..];
        if (rest.Length == 1 && rest[0] is "-h" or "--help") { CommandUsage(tool, command); return 0; }
        return command switch
        {
            "spec" => Spec(rest),
            "create" => Create(g, rest),
            "run" => RunContainer(g, rest),
            "start" => Start(g, rest),
            "state" => State(g, rest),
            "list" or "ls" => List(g, rest),
            "kill" => Kill(g, rest),
            "delete" or "rm" => Delete(g, rest),
            "ps" => Ps(g, rest),
            "exec" => Exec(g, rest),
            "pause" => PauseResume(g, rest, pause: true),
            "resume" => PauseResume(g, rest, pause: false),
            "update" => Update(g, rest),
            "events" => Events(g, rest),
            "features" => Features(),
            "__monitor" => Monitor(g, rest),
            "checkpoint" or "restore" => Refuse(tool, command, "Windows has no process checkpoint/restore (CRIU)"),
            "mount" or "umount" => Refuse(tool, command, "containers have no mount namespace on Windows; bind mounts are junctions made at create"),
            "help" => Help(tool, rest),
            _ => throw new UsageException($"No help topic for '{command}'"),
        };
    }

    static int Refuse(string tool, string command, string why)
    {
        Console.Error.WriteLine($"{tool}: {command}: {why}");
        return 1;
    }

    static int Help(string tool, string[] rest)
    {
        if (rest.Length == 0) Usage(tool);
        else CommandUsage(tool, rest[0]);
        return 0;
    }

    static (string, string) SplitOption(string a)
    {
        var eq = a.StartsWith("--") ? a.IndexOf('=') : -1;
        return eq > 0 ? (a[..eq], a[(eq + 1)..]) : (a, null);
    }

    static string Next(string[] args, ref int i, string name) =>
        ++i < args.Length ? args[i] : throw new UsageException($"flag needs an argument: {name}");

    static string DefaultRoot(string tool)
    {
        var runtime = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
        if (!string.IsNullOrEmpty(runtime)) return Path.Combine(runtime, tool);
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), tool);
    }

    static void PrintVersion(string tool)
    {
        if (tool == "runc")
            Console.WriteLine($"runc version {RuncVersion}\ncommit: wintools-{Commands.Version}\nspec: {SpecVersion}\ngo: (none: Windows job objects)\nlibseccomp: (none)");
        else
            Console.WriteLine($"crun version {CrunVersion}\ncommit: wintools-{Commands.Version}\nrundir: {DefaultRoot("crun")}\nspec: 1.0.0\n-SYSTEMD -SELINUX -APPARMOR -CAP -SECCOMP -EBPF -CRIU +JOBOBJECT");
    }

    static void Usage(string tool) => Console.WriteLine($"""
        NAME:
           {tool} - Open Container Initiative runtime for Windows processes

        {tool} runs containers packaged in the OCI bundle format: a directory with a config.json
        and a root file system. On Windows each container's process tree runs in a job object
        (resource limits, accounting, and everything dies together).

        USAGE:
           {tool} [global options] command [command options] [arguments...]

        COMMANDS:
           create      create a container
           delete      delete any resources held by the container
           events      display container events such as stats (--stats)
           exec        execute new process inside the container
           features    show the enabled features
           kill        send a signal to the container's process (TERM: Ctrl+Break, KILL: terminate)
           list        lists containers started by {tool} with the given root
           pause       suspend all processes inside the container
           ps          ps displays the processes running inside a container
           resume      resumes all processes that have been previously paused
           run         create and run a container
           spec        create a new specification file
           start       executes the user defined process in a created container
           state       output the state of a container
           update      update container resource constraints

        GLOBAL OPTIONS:
           --debug             enable debug logging
           --log value         set the log file to write logs to
           --log-format value  set the log format ('text' (default), or 'json')
           --root value        root directory for storage of container state (default: "{DefaultRoot(tool)}")
           --help, -h          show help
           --version, -v       print the version
        """);

    static void CommandUsage(string tool, string command) => Console.WriteLine(command switch
    {
        "spec" => $"usage: {tool} spec [--bundle <dir>] [--rootless]\nCreate a new config.json (Windows process container) in the bundle.",
        "create" => $"usage: {tool} create [--bundle <dir>] [--pid-file <file>] [--no-new-keyring] <container-id>",
        "run" => $"usage: {tool} run [--bundle <dir>] [--pid-file <file>] [--detach] [--keep] <container-id>",
        "start" => $"usage: {tool} start <container-id>",
        "state" => $"usage: {tool} state <container-id>",
        "list" => $"usage: {tool} list [--format table|json] [--quiet]",
        "kill" => $"usage: {tool} kill [--all] <container-id> [signal]\nTERM/INT/HUP/QUIT send Ctrl+Break to the process group; KILL terminates the job.",
        "delete" => $"usage: {tool} delete [--force] <container-id>",
        "ps" => $"usage: {tool} ps [--format table|json] <container-id>",
        "exec" => $"usage: {tool} exec [--process <process.json>] [--cwd <dir>] [--env K=V] [--detach] <container-id> <command> [args...]",
        "pause" or "resume" => $"usage: {tool} {command} <container-id>",
        "update" => $"usage: {tool} update [--memory <bytes>] [--cpu-quota <us>] [--cpu-period <us>] [--pids-limit <n>] <container-id>",
        "events" => $"usage: {tool} events [--stats] [--interval <duration>] <container-id>",
        _ => $"No help topic for '{command}'",
    });

    // ---- container state ----

    sealed class ContainerState
    {
        public string Id, Status, Bundle, Rootfs, Created, Owner;
        public int Pid, MonitorPid;
        public int? ExitCode;
        public Dictionary<string, string> Annotations = new();
        public List<string> Junctions = new();
    }

    static string Key(Globals g, string id) =>
        Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(g.Root).ToUpperInvariant() + "\0" + id)))[..16];

    static string JobName(Globals g, string id) => $"Local\\oci-{Key(g, id)}-job";
    static string EventName(Globals g, string id, string what) => $"Local\\oci-{Key(g, id)}-{what}";
    static string StateDir(Globals g, string id) => Path.Combine(g.Root, id);
    static string StatePath(Globals g, string id) => Path.Combine(StateDir(g, id), "state.json");

    static void ValidateId(string id)
    {
        if (string.IsNullOrEmpty(id) || id.Length > 1024 || id.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '_' or '+' or '-' or '.')) || id is "." or "..")
            throw new UsageException($"invalid id format: {id}");
    }

    static void Save(Globals g, ContainerState s)
    {
        Directory.CreateDirectory(StateDir(g, s.Id));
        using var buffer = new MemoryStream();
        using (var w = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            w.WriteStartObject();
            w.WriteString("ociVersion", SpecVersion);
            w.WriteString("id", s.Id);
            w.WriteString("status", s.Status);
            w.WriteNumber("pid", s.Pid);
            w.WriteString("bundle", s.Bundle);
            w.WriteString("rootfs", s.Rootfs);
            w.WriteString("created", s.Created);
            w.WriteString("owner", s.Owner);
            w.WriteNumber("monitorPid", s.MonitorPid);
            if (s.ExitCode is int code) w.WriteNumber("exitCode", code);
            w.WriteStartObject("annotations");
            foreach (var (k, v) in s.Annotations) w.WriteString(k, v);
            w.WriteEndObject();
            w.WriteStartArray("junctions");
            foreach (var j in s.Junctions) w.WriteStringValue(j);
            w.WriteEndArray();
            w.WriteEndObject();
        }
        var temp = StatePath(g, s.Id) + ".tmp";
        File.WriteAllBytes(temp, buffer.ToArray());
        File.Move(temp, StatePath(g, s.Id), overwrite: true);
    }

    static ContainerState Load(Globals g, string id)
    {
        ValidateId(id);
        var path = StatePath(g, id);
        if (!File.Exists(path)) throw new InvalidOperationException($"container does not exist: {id}");
        JsonDocument doc = null;
        for (var attempt = 0; doc == null; attempt++)
        {
            try { doc = JsonDocument.Parse(File.ReadAllBytes(path)); }
            catch (IOException) when (attempt < 20) { Thread.Sleep(25); }   // the monitor is replacing it
        }
        using (doc)
        {
            var r = doc.RootElement;
            string S(string n) => r.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : "";
            var s = new ContainerState
            {
                Id = S("id"), Status = S("status"), Bundle = S("bundle"), Rootfs = S("rootfs"), Created = S("created"), Owner = S("owner"),
                Pid = r.TryGetProperty("pid", out var p) ? p.GetInt32() : 0,
                MonitorPid = r.TryGetProperty("monitorPid", out var m) ? m.GetInt32() : 0,
                ExitCode = r.TryGetProperty("exitCode", out var e) ? e.GetInt32() : null,
            };
            if (r.TryGetProperty("annotations", out var an) && an.ValueKind == JsonValueKind.Object)
                foreach (var kv in an.EnumerateObject()) s.Annotations[kv.Name] = kv.Value.GetString();
            if (r.TryGetProperty("junctions", out var js) && js.ValueKind == JsonValueKind.Array)
                foreach (var j in js.EnumerateArray()) s.Junctions.Add(j.GetString());
            // A container whose process (or monitor) is gone has stopped, whatever was last written.
            if (s.Status is "running" or "paused" && !Alive(s.Pid)) s.Status = "stopped";
            if (s.Status == "created" && !Alive(s.MonitorPid)) s.Status = "stopped";
            return s;
        }
    }

    static bool Alive(int pid)
    {
        if (pid <= 0) return false;
        var h = OpenProcess(SYNCHRONIZE | PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (h == IntPtr.Zero) return false;
        var alive = WaitForSingleObject(h, 0) == WAIT_TIMEOUT;
        CloseHandle(h);
        return alive;
    }

    static void WriteStateJson(TextWriter output, ContainerState s)
    {
        using var buffer = new MemoryStream();
        using (var w = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            w.WriteStartObject();
            w.WriteString("ociVersion", SpecVersion);
            w.WriteString("id", s.Id);
            w.WriteNumber("pid", s.Status == "stopped" ? 0 : s.Pid);
            w.WriteString("status", s.Status);
            w.WriteString("bundle", s.Bundle);
            w.WriteString("rootfs", s.Rootfs);
            w.WriteString("created", s.Created);
            w.WriteString("owner", s.Owner);
            if (s.Annotations.Count > 0)
            {
                w.WriteStartObject("annotations");
                foreach (var (k, v) in s.Annotations) w.WriteString(k, v);
                w.WriteEndObject();
            }
            w.WriteEndObject();
        }
        output.WriteLine(Encoding.UTF8.GetString(buffer.ToArray()));
    }

    // ---- spec ----

    static int Spec(string[] args)
    {
        var bundle = ".";
        for (var i = 0; i < args.Length; i++)
        {
            var (name, inline) = SplitOption(args[i]);
            switch (name)
            {
                case "-b" or "--bundle": bundle = inline ?? Next(args, ref i, name); break;
                case "--rootless": break;
                default: throw new UsageException($"flag provided but not defined: {args[i]}");
            }
        }
        var path = Path.Combine(bundle, "config.json");
        if (File.Exists(path)) throw new InvalidOperationException("File config.json exists. Remove it first");
        Directory.CreateDirectory(bundle);
        File.WriteAllText(path, $$"""
            {
                "ociVersion": "{{SpecVersion}}",
                "process": {
                    "terminal": true,
                    "user": {
                        "uid": 0,
                        "gid": 0
                    },
                    "args": [
                        "cmd.exe"
                    ],
                    "env": [
                        "PATH=/Windows/System32;/Windows",
                        "TERM=xterm"
                    ],
                    "cwd": "/"
                },
                "root": {
                    "path": "rootfs",
                    "readonly": false
                },
                "hostname": "runc",
                "mounts": [],
                "windows": {
                    "resources": {
                        "memory": {
                            "limit": 0
                        }
                    }
                }
            }

            """.Replace("\r\n", "\n"));
        return 0;
    }

    // ---- bundle configuration ----

    sealed class ProcessSpec
    {
        public List<string> Args = new();
        public List<string> Env = new();
        public string Cwd = "/";
    }

    sealed class Bundle
    {
        public string Dir, Config, Rootfs;
        public ProcessSpec Process;
        public ulong MemoryLimit;
        public uint CpuRate;        // 1-10000 (hundredths of a percent of all CPUs), 0 = none
        public uint PidsLimit;
        public Dictionary<string, string> Annotations = new();
        public List<(string Source, string Destination, string Type, List<string> Options)> Mounts = new();
        public Dictionary<string, List<(string Path, List<string> Args, List<string> Env, int Timeout)>> Hooks = new();
    }

    static Bundle ReadBundle(string dir)
    {
        var config = Path.Combine(dir, "config.json");
        if (!File.Exists(config)) throw new InvalidOperationException($"JSON specification file {config} not found");
        using var doc = JsonDocument.Parse(File.ReadAllBytes(config));
        var r = doc.RootElement;
        var b = new Bundle { Dir = Path.GetFullPath(dir), Config = config };
        var rootPath = r.TryGetProperty("root", out var root) && root.TryGetProperty("path", out var rp) ? rp.GetString() : "rootfs";
        b.Rootfs = Path.GetFullPath(Path.IsPathRooted(rootPath) ? rootPath : Path.Combine(b.Dir, rootPath));
        if (!r.TryGetProperty("process", out var proc)) throw new InvalidOperationException("config.json has no process");
        b.Process = ReadProcess(proc);
        if (r.TryGetProperty("annotations", out var an) && an.ValueKind == JsonValueKind.Object)
            foreach (var kv in an.EnumerateObject()) b.Annotations[kv.Name] = kv.Value.ToString();
        ReadResources(r, b);
        if (r.TryGetProperty("mounts", out var mounts) && mounts.ValueKind == JsonValueKind.Array)
            foreach (var m in mounts.EnumerateArray())
            {
                string Str(string n) => m.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : "";
                var options = m.TryGetProperty("options", out var o) && o.ValueKind == JsonValueKind.Array
                    ? o.EnumerateArray().Select(x => x.GetString()).ToList() : new List<string>();
                b.Mounts.Add((Str("source"), Str("destination"), Str("type"), options));
            }
        if (r.TryGetProperty("hooks", out var hooks) && hooks.ValueKind == JsonValueKind.Object)
            foreach (var stage in hooks.EnumerateObject())
            {
                var list = new List<(string, List<string>, List<string>, int)>();
                foreach (var h in stage.Value.EnumerateArray())
                    list.Add((h.GetProperty("path").GetString(),
                        h.TryGetProperty("args", out var ha) ? ha.EnumerateArray().Select(x => x.GetString()).ToList() : new List<string>(),
                        h.TryGetProperty("env", out var he) ? he.EnumerateArray().Select(x => x.GetString()).ToList() : new List<string>(),
                        h.TryGetProperty("timeout", out var ht) ? ht.GetInt32() : 0));
                b.Hooks[stage.Name] = list;
            }
        return b;
    }

    static ProcessSpec ReadProcess(JsonElement proc)
    {
        var p = new ProcessSpec();
        if (proc.TryGetProperty("args", out var args) && args.ValueKind == JsonValueKind.Array)
            p.Args = args.EnumerateArray().Select(a => a.GetString()).ToList();
        else if (proc.TryGetProperty("commandLine", out var line) && line.ValueKind == JsonValueKind.String)
            p.Args = SplitCommandLine(line.GetString());
        if (proc.TryGetProperty("env", out var env) && env.ValueKind == JsonValueKind.Array)
            p.Env = env.EnumerateArray().Select(e => e.GetString()).ToList();
        if (proc.TryGetProperty("cwd", out var cwd) && cwd.ValueKind == JsonValueKind.String) p.Cwd = cwd.GetString();
        if (p.Args.Count == 0) throw new InvalidOperationException("args must not be empty");
        return p;
    }

    static List<string> SplitCommandLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool quoted = false, any = false;
        foreach (var c in line)
        {
            if (c == '"') { quoted = !quoted; any = true; continue; }
            if (char.IsWhiteSpace(c) && !quoted)
            {
                if (any || current.Length > 0) result.Add(current.ToString());
                current.Clear();
                any = false;
                continue;
            }
            current.Append(c);
        }
        if (any || current.Length > 0) result.Add(current.ToString());
        return result;
    }

    static void ReadResources(JsonElement r, Bundle b)
    {
        if (r.TryGetProperty("linux", out var linux) && linux.TryGetProperty("resources", out var lr))
        {
            if (lr.TryGetProperty("memory", out var mem) && mem.TryGetProperty("limit", out var limit) && limit.TryGetInt64(out var l) && l > 0)
                b.MemoryLimit = (ulong)l;
            if (lr.TryGetProperty("cpu", out var cpu) && cpu.TryGetProperty("quota", out var quota) && quota.TryGetInt64(out var q) && q > 0)
            {
                var period = cpu.TryGetProperty("period", out var pe) && pe.TryGetInt64(out var pv) && pv > 0 ? pv : 100000;
                // quota/period CPUs out of all of them, in hundredths of a percent of the machine
                b.CpuRate = (uint)Math.Clamp(q * 10000L / period / Environment.ProcessorCount, 1, 10000);
            }
            if (lr.TryGetProperty("pids", out var pids) && pids.TryGetProperty("limit", out var pl) && pl.TryGetInt64(out var p) && p > 0)
                b.PidsLimit = (uint)Math.Min(p, uint.MaxValue);
        }
        if (r.TryGetProperty("windows", out var windows) && windows.TryGetProperty("resources", out var wr))
        {
            if (wr.TryGetProperty("memory", out var mem) && mem.TryGetProperty("limit", out var limit) && limit.TryGetInt64(out var l) && l > 0)
                b.MemoryLimit = (ulong)l;
            if (wr.TryGetProperty("cpu", out var cpu))
            {
                if (cpu.TryGetProperty("maximum", out var max) && max.TryGetInt32(out var m) && m > 0) b.CpuRate = (uint)Math.Clamp(m, 1, 10000);
                else if (cpu.TryGetProperty("count", out var count) && count.TryGetInt32(out var c) && c > 0)
                    b.CpuRate = (uint)Math.Clamp(c * 10000 / Environment.ProcessorCount, 1, 10000);
            }
        }
    }

    // ---- job objects ----

    static IntPtr CreateJob(Globals g, string id, Bundle b)
    {
        var job = CreateJobObjectW(IntPtr.Zero, JobName(g, id));
        if (job == IntPtr.Zero) throw new InvalidOperationException($"creating the job object: {new System.ComponentModel.Win32Exception().Message}");
        if (Marshal.GetLastWin32Error() == ERROR_ALREADY_EXISTS)
        {
            CloseHandle(job);
            throw new InvalidOperationException($"container with id exists: {id}");
        }
        ApplyLimits(job, b.MemoryLimit, b.CpuRate, b.PidsLimit);
        return job;
    }

    static void ApplyLimits(IntPtr job, ulong memory, uint cpuRate, uint pids)
    {
        var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
        QueryInformationJobObject(job, JobObjectExtendedLimitInformation, ref info, Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>(), IntPtr.Zero);
        info.BasicLimitInformation.LimitFlags |= JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;
        if (memory > 0)
        {
            info.BasicLimitInformation.LimitFlags |= JOB_OBJECT_LIMIT_JOB_MEMORY;
            info.JobMemoryLimit = (UIntPtr)memory;
        }
        if (pids > 0)
        {
            info.BasicLimitInformation.LimitFlags |= JOB_OBJECT_LIMIT_ACTIVE_PROCESS;
            info.BasicLimitInformation.ActiveProcessLimit = pids;
        }
        if (!SetInformationJobObject(job, JobObjectExtendedLimitInformation, ref info, Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>()))
            throw new InvalidOperationException($"setting job limits: {new System.ComponentModel.Win32Exception().Message}");
        if (cpuRate > 0)
        {
            var cpu = new JOBOBJECT_CPU_RATE_CONTROL_INFORMATION { ControlFlags = JOB_OBJECT_CPU_RATE_CONTROL_ENABLE | JOB_OBJECT_CPU_RATE_CONTROL_HARD_CAP, Value = cpuRate };
            SetInformationJobObjectCpu(job, JobObjectCpuRateControlInformation, ref cpu, Marshal.SizeOf<JOBOBJECT_CPU_RATE_CONTROL_INFORMATION>());
        }
    }

    static IntPtr OpenJob(Globals g, string id) => OpenJobObjectW(JOB_OBJECT_ALL_ACCESS, false, JobName(g, id));

    static List<int> JobPids(IntPtr job)
    {
        var buffer = new byte[8 + 8 * 4096];
        var pins = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            if (!QueryInformationJobObjectRaw(job, JobObjectBasicProcessIdList, pins.AddrOfPinnedObject(), buffer.Length, IntPtr.Zero)) return new List<int>();
            var count = BitConverter.ToInt32(buffer, 4);
            return Enumerable.Range(0, count).Select(k => (int)BitConverter.ToInt64(buffer, 8 + 8 * k)).ToList();
        }
        finally { pins.Free(); }
    }

    // ---- create / start / run ----

    static (string Bundle, string PidFile, bool Detach, bool Keep, string Id) ParseCreateArgs(string[] args)
    {
        string bundle = ".", pidFile = null, id = null;
        bool detach = false, keep = false;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (!a.StartsWith('-') || a == "-") { id ??= a; continue; }
            var (name, inline) = SplitOption(a);
            switch (name)
            {
                case "-b" or "--bundle": bundle = inline ?? Next(args, ref i, name); break;
                case "--pid-file": pidFile = inline ?? Next(args, ref i, name); break;
                case "-d" or "--detach": detach = true; break;
                case "--keep": keep = true; break;
                case "--console-socket" or "--preserve-fds": if (inline == null) Next(args, ref i, name); break;
                case "--no-pivot" or "--no-new-keyring" or "--no-subreaper": break;
                default: throw new UsageException($"flag provided but not defined: {a}");
            }
        }
        if (id == null) throw new UsageException("\"create\" requires exactly 1 argument(s)");
        ValidateId(id);
        return (bundle, pidFile, detach, keep, id);
    }

    static int Create(Globals g, string[] args)
    {
        var (bundleDir, pidFile, _, _, id) = ParseCreateArgs(args);
        return CreateContainer(g, id, bundleDir, pidFile, startNow: false);
    }

    // Starts the monitor (this program, `__monitor`), which creates the process suspended and keeps the
    // job; waits until the container is created (or failed).
    static int CreateContainer(Globals g, string id, string bundleDir, string pidFile, bool startNow)
    {
        if (File.Exists(StatePath(g, id)) && Load(g, id).Status != "stopped") throw new InvalidOperationException($"container with id exists: {id}");
        var bundle = ReadBundle(bundleDir);
        Directory.CreateDirectory(StateDir(g, id));
        File.Delete(Path.Combine(StateDir(g, id), "error"));
        using var ready = new EventWaitHandle(false, EventResetMode.ManualReset, EventName(g, id, "ready"));
        var self = Environment.ProcessPath ?? throw new InvalidOperationException("cannot find this program");
        var monitorArgs = new List<string> { g.Tool, "--root", g.Root, "__monitor", "--bundle", bundle.Dir, id };
        if (pidFile != null) monitorArgs.InsertRange(4, new[] { "--pid-file", Path.GetFullPath(pidFile) });
        if (startNow) monitorArgs.Insert(4, "--start");
        var commandLine = new StringBuilder(Quote(self));
        if (!Path.GetFileNameWithoutExtension(self).Equals(g.Tool, StringComparison.OrdinalIgnoreCase))
            commandLine.Append(' ').Append(Quote(g.Tool));
        foreach (var a in monitorArgs.Skip(1)) commandLine.Append(' ').Append(Quote(a));
        var monitor = Spawn(self, commandLine.ToString(), null, null, CREATE_NEW_PROCESS_GROUP, breakaway: true, out _);
        var handles = new WaitHandle[] { ready, new ProcessWaitHandle(monitor) };
        var which = WaitHandle.WaitAny(handles, TimeSpan.FromSeconds(60));
        CloseHandle(monitor);
        var errorFile = Path.Combine(StateDir(g, id), "error");
        if (which != 0)
        {
            var message = File.Exists(errorFile) ? File.ReadAllText(errorFile).Trim() : "the container monitor exited";
            throw new InvalidOperationException(message);
        }
        return 0;
    }

    sealed class ProcessWaitHandle : WaitHandle
    {
        public ProcessWaitHandle(IntPtr process) => SafeWaitHandle = new Microsoft.Win32.SafeHandles.SafeWaitHandle(process, ownsHandle: false);
    }

    static int Start(Globals g, string[] args)
    {
        if (args.Length != 1) throw new UsageException("\"start\" requires exactly 1 argument(s)");
        var s = Load(g, args[0]);
        if (s.Status != "created") throw new InvalidOperationException($"cannot start a container that has {(s.Status == "stopped" ? "stopped" : "already been started")}");
        using var start = EventWaitHandle.OpenExisting(EventName(g, s.Id, "start"));
        using var started = new EventWaitHandle(false, EventResetMode.ManualReset, EventName(g, s.Id, "started"));
        start.Set();
        started.WaitOne(TimeSpan.FromSeconds(30));
        return 0;
    }

    static int RunContainer(Globals g, string[] args)
    {
        var (bundleDir, pidFile, detach, keep, id) = ParseCreateArgs(args);
        if (detach) return CreateContainer(g, id, bundleDir, pidFile, startNow: true);
        // Foreground: this process is the monitor; the container's exit status is ours.
        var code = MonitorContainer(g, id, Path.GetFullPath(bundleDir), pidFile, startNow: true, foreground: true);
        if (!keep) Destroy(g, Load(g, id), force: true);
        return code;
    }

    static int Monitor(Globals g, string[] args)
    {
        string bundle = null, pidFile = null, id = null;
        var startNow = false;
        for (var i = 0; i < args.Length; i++)
            switch (args[i])
            {
                case "--bundle": bundle = args[++i]; break;
                case "--pid-file": pidFile = args[++i]; break;
                case "--start": startNow = true; break;
                default: id = args[i]; break;
            }
        try { return MonitorContainer(g, id, bundle, pidFile, startNow, foreground: false); }
        catch (Exception e) when (e is InvalidOperationException or IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            Directory.CreateDirectory(StateDir(g, id));
            File.WriteAllText(Path.Combine(StateDir(g, id), "error"), e.Message);
            return 1;
        }
    }

    // Creates the job and the suspended process, records the state, waits for `start` (unless
    // startNow), resumes it, and waits for it (then for `delete`, when detached).
    static int MonitorContainer(Globals g, string id, string bundleDir, string pidFile, bool startNow, bool foreground)
    {
        var bundle = ReadBundle(bundleDir);
        var job = CreateJob(g, id, bundle);
        var state = new ContainerState
        {
            Id = id, Status = "creating", Bundle = bundle.Dir, Rootfs = bundle.Rootfs,
            Created = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z"),
            Owner = Environment.UserName, MonitorPid = Environment.ProcessId, Annotations = bundle.Annotations,
        };
        state.Junctions = MakeMounts(bundle, g.Debug);
        RunHooks(bundle, "createRuntime", state);
        var (application, commandLine) = ResolveProgram(bundle);
        var cwd = MapPath(bundle.Process.Cwd, bundle.Rootfs);
        if (!Directory.Exists(cwd)) throw new InvalidOperationException($"chdir to cwd (\"{bundle.Process.Cwd}\") set in config.json failed: no such directory");
        var environment = EnvironmentBlock(bundle.Process.Env, bundle.Rootfs);
        var process = Spawn(application, commandLine, environment, cwd, CREATE_SUSPENDED | CREATE_NEW_PROCESS_GROUP, breakaway: false, out var thread, out var pid);
        if (!AssignProcessToJobObject(job, process))
        {
            TerminateProcess(process, 1);
            throw new InvalidOperationException($"adding the process to its job: {new System.ComponentModel.Win32Exception().Message}");
        }
        state.Pid = pid;
        state.Status = "created";
        Save(g, state);
        if (pidFile != null) File.WriteAllText(pidFile, pid.ToString());
        RunHooks(bundle, "createContainer", state);

        if (!foreground)
        {
            // The container owns its standard streams now: a monitor holding copies would keep pipes
            // (conmon's log pipes) open after the container exits.
            foreach (var std in new[] { STD_INPUT_HANDLE, STD_OUTPUT_HANDLE, STD_ERROR_HANDLE })
            {
                var h = GetStdHandle(std);
                SetStdHandle(std, IntPtr.Zero);
                if (h != IntPtr.Zero && h != new IntPtr(-1)) CloseHandle(h);
            }
        }
        using (var ready = new EventWaitHandle(false, EventResetMode.ManualReset, EventName(g, id, "ready")))
        using (var start = new EventWaitHandle(false, EventResetMode.ManualReset, EventName(g, id, "start")))
        using (var started = new EventWaitHandle(false, EventResetMode.ManualReset, EventName(g, id, "started")))
        {
            ready.Set();
            if (!startNow) start.WaitOne();
            RunHooks(bundle, "prestart", state);
            RunHooks(bundle, "startContainer", state);
            ResumeThread(thread);
            CloseHandle(thread);
            state.Status = "running";
            Save(g, state);
            started.Set();
            RunHooks(bundle, "poststart", state);
            WaitForSingleObject(process, INFINITE);
        }
        GetExitCodeProcess(process, out var code);
        CloseHandle(process);
        state.Status = "stopped";
        state.ExitCode = (int)code;
        Save(g, state);
        if (!foreground)
        {
            // Keep the job (and its name) until `delete`, like runc keeps the container's cgroup.
            using var delete = new EventWaitHandle(false, EventResetMode.ManualReset, EventName(g, id, "delete"));
            delete.WaitOne();
        }
        TerminateJobObject(job, 137);
        CloseHandle(job);
        return (int)code;
    }

    // args[0] inside the root file system ("/bin/app" or "app" via PATH), then the host's search path.
    static (string Application, string CommandLine) ResolveProgram(Bundle b)
    {
        var p = b.Process;
        var program = p.Args[0];
        var candidates = new List<string>();
        bool hasDirectory = program.Contains('/') || program.Contains('\\');
        if (hasDirectory) candidates.Add(MapPath(program, b.Rootfs));
        else
        {
            var path = p.Env.FirstOrDefault(e => e.StartsWith("PATH=", StringComparison.OrdinalIgnoreCase))?[5..];
            if (path != null)
                foreach (var dir in path.Split(path.Contains(';') ? ';' : ':', StringSplitOptions.RemoveEmptyEntries))
                    candidates.Add(Path.Combine(MapPath(dir, b.Rootfs), program));
            candidates.Add(Path.Combine(b.Rootfs, program));
        }
        string found = null;
        foreach (var c in candidates)
        {
            foreach (var ext in new[] { "", ".exe", ".com", ".cmd", ".bat" })
                if (File.Exists(c + ext) && (ext.Length > 0 || Path.HasExtension(c))) { found = c + ext; break; }
            if (found == null && File.Exists(c)) found = c;
            if (found != null) break;
        }
        if (found == null && !hasDirectory)
        {
            // Not in the root file system: the host's programs (cmd.exe, powershell.exe ...).
            var sb = new StringBuilder(260);
            if (SearchPathW(null, program, ".exe", sb.Capacity, sb, IntPtr.Zero) > 0) found = sb.ToString();
        }
        if (found == null) throw new InvalidOperationException($"exec: \"{program}\": executable file not found in $PATH");
        if (IsElf(found))
            throw new InvalidOperationException($"exec: \"{program}\": {found} is a Linux (ELF) executable; this runtime runs Windows programs (Linux images run in WSL, e.g. with `container`)");
        var line = new StringBuilder();
        string application = found;
        if (Path.GetExtension(found).ToLowerInvariant() is ".cmd" or ".bat")
        {
            application = Path.Combine(Environment.SystemDirectory, "cmd.exe");
            line.Append("cmd.exe /d /s /c \"").Append(Quote(found));
            foreach (var a in p.Args.Skip(1)) line.Append(' ').Append(Quote(a));
            line.Append('"');
        }
        else
        {
            line.Append(Quote(found));
            foreach (var a in p.Args.Skip(1)) line.Append(' ').Append(Quote(a));
        }
        return (application, line.ToString());
    }

    static bool IsElf(string path)
    {
        try
        {
            using var f = File.OpenRead(path);
            Span<byte> magic = stackalloc byte[4];
            return f.Read(magic) == 4 && magic[0] == 0x7f && magic[1] == (byte)'E' && magic[2] == (byte)'L' && magic[3] == (byte)'F';
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    // A container path: "/x/y" (or "\x\y") is inside the root file system; "C:\x" is a host path.
    static string MapPath(string path, string rootfs)
    {
        if (string.IsNullOrEmpty(path)) return rootfs;
        if (path.Length >= 2 && path[1] == ':') return Path.GetFullPath(path);
        return Path.GetFullPath(Path.Combine(rootfs, path.TrimStart('/', '\\').Replace('/', '\\')));
    }

    static string EnvironmentBlock(List<string> env, string rootfs)
    {
        var vars = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in env)
        {
            var eq = e.IndexOf('=');
            if (eq > 0) vars[e[..eq]] = e[(eq + 1)..];
        }
        // PATH entries are container paths; Windows programs also need SystemRoot to start.
        if (!vars.ContainsKey("PATH")) vars["PATH"] = "";
        if (vars.TryGetValue("PATH", out var path))
            vars["PATH"] = string.Join(';', path.Split(path.Contains(';') ? ';' : ':', StringSplitOptions.RemoveEmptyEntries)
                .Select(d => d.StartsWith('/') ? MapPath(d, rootfs) : d)
                .Append(Environment.SystemDirectory).Append(Environment.GetFolderPath(Environment.SpecialFolder.Windows)).Distinct(StringComparer.OrdinalIgnoreCase));
        foreach (var name in new[] { "SystemRoot", "windir", "SystemDrive", "ComSpec" })
            if (!vars.ContainsKey(name) && Environment.GetEnvironmentVariable(name) is { } value) vars[name] = value;
        var sb = new StringBuilder();
        foreach (var (k, v) in vars) sb.Append(k).Append('=').Append(v).Append('\0');
        return sb.Append('\0').ToString();
    }

    // Bind mounts as junctions inside the root file system; kernel file systems don't exist here.
    static List<string> MakeMounts(Bundle b, bool debug)
    {
        var made = new List<string>();
        foreach (var (source, destination, type, options) in b.Mounts)
        {
            var bind = type == "bind" || options.Contains("bind") || options.Contains("rbind");
            if (!bind)
            {
                if (debug) Console.Error.WriteLine($"skipping {type} mount at {destination}: no Linux kernel file systems on Windows");
                continue;
            }
            var src = Path.GetFullPath(source);
            var dst = MapPath(destination, b.Rootfs);
            if (!Directory.Exists(src))
            {
                if (debug) Console.Error.WriteLine($"skipping bind mount of {source}: only directories can be mounted (as junctions)");
                continue;
            }
            if (Directory.Exists(dst) && Directory.EnumerateFileSystemEntries(dst).Any())
                throw new InvalidOperationException($"mount destination {destination} exists and is not empty");
            if (Directory.Exists(dst)) Directory.Delete(dst);
            Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
            CreateJunction(dst, src);
            made.Add(dst);
        }
        return made;
    }

    static void CreateJunction(string link, string target)
    {
        Directory.CreateDirectory(link);
        var substitute = "\\??\\" + target;
        var subBytes = Encoding.Unicode.GetBytes(substitute);
        var printBytes = Encoding.Unicode.GetBytes(target);
        var data = new MemoryStream();
        var w = new BinaryWriter(data);
        w.Write(IO_REPARSE_TAG_MOUNT_POINT);
        w.Write((ushort)(8 + subBytes.Length + 2 + printBytes.Length + 2));
        w.Write((ushort)0);
        w.Write((ushort)0);
        w.Write((ushort)subBytes.Length);
        w.Write((ushort)(subBytes.Length + 2));
        w.Write((ushort)printBytes.Length);
        w.Write(subBytes);
        w.Write((ushort)0);
        w.Write(printBytes);
        w.Write((ushort)0);
        var buffer = data.ToArray();
        var h = CreateFileW(link, GENERIC_WRITE, 7, IntPtr.Zero, 3, FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OPEN_REPARSE_POINT, IntPtr.Zero);
        if (h == new IntPtr(-1)) throw new InvalidOperationException($"mount {target} at {link}: {new System.ComponentModel.Win32Exception().Message}");
        try
        {
            if (!DeviceIoControl(h, FSCTL_SET_REPARSE_POINT, buffer, buffer.Length, IntPtr.Zero, 0, out _, IntPtr.Zero))
                throw new InvalidOperationException($"mount {target} at {link}: {new System.ComponentModel.Win32Exception().Message}");
        }
        finally { CloseHandle(h); }
    }

    static void RunHooks(Bundle b, string stage, ContainerState s)
    {
        if (!b.Hooks.TryGetValue(stage, out var hooks)) return;
        var stdin = new StringWriter();
        WriteStateJson(stdin, s);
        foreach (var (path, args, env, timeout) in hooks)
        {
            var psi = new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = false, RedirectStandardInput = true };
            foreach (var a in args.Skip(1)) psi.ArgumentList.Add(a);
            if (env.Count > 0)
            {
                psi.Environment.Clear();
                foreach (var e in env)
                {
                    var eq = e.IndexOf('=');
                    if (eq > 0) psi.Environment[e[..eq]] = e[(eq + 1)..];
                }
            }
            using var p = System.Diagnostics.Process.Start(psi)!;
            p.StandardInput.Write(stdin.ToString());
            p.StandardInput.Close();
            if (!p.WaitForExit(timeout > 0 ? timeout * 1000 : -1))
            {
                p.Kill(entireProcessTree: true);
                throw new InvalidOperationException($"{stage} hook {path} timed out after {timeout}s");
            }
            if (p.ExitCode != 0 && stage is "prestart" or "createRuntime" or "createContainer" or "startContainer")
                throw new InvalidOperationException($"{stage} hook {path} failed with exit status {p.ExitCode}");
        }
    }

    // CreateProcess with this process's standard handles; returns the process handle.
    static IntPtr Spawn(string application, string commandLine, string environment, string cwd, uint flags, bool breakaway, out IntPtr thread, out int pid)
    {
        var startup = new STARTUPINFOW { cb = Marshal.SizeOf<STARTUPINFOW>(), dwFlags = STARTF_USESTDHANDLES };
        startup.hStdInput = Inheritable(GetStdHandle(STD_INPUT_HANDLE));
        startup.hStdOutput = Inheritable(GetStdHandle(STD_OUTPUT_HANDLE));
        startup.hStdError = Inheritable(GetStdHandle(STD_ERROR_HANDLE));
        var line = new StringBuilder(commandLine);
        var env = environment != null ? Marshal.StringToHGlobalUni(environment) : IntPtr.Zero;
        try
        {
            flags |= CREATE_UNICODE_ENVIRONMENT;
            PROCESS_INFORMATION pi;
            if (!(breakaway && CreateProcessW(application, line, IntPtr.Zero, IntPtr.Zero, true, flags | CREATE_BREAKAWAY_FROM_JOB, env, cwd, ref startup, out pi))
                && !CreateProcessW(application, line, IntPtr.Zero, IntPtr.Zero, true, flags, env, cwd, ref startup, out pi))
            {
                var error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"exec: \"{application}\": {new System.ComponentModel.Win32Exception(error).Message}");
            }
            thread = pi.hThread;
            pid = pi.dwProcessId;
            return pi.hProcess;
        }
        finally { if (env != IntPtr.Zero) Marshal.FreeHGlobal(env); }
    }

    static IntPtr Spawn(string application, string commandLine, string environment, string cwd, uint flags, bool breakaway, out int pid)
    {
        var process = Spawn(application, commandLine, environment, cwd, flags, breakaway, out IntPtr thread, out pid);
        CloseHandle(thread);
        return process;
    }

    static IntPtr Inheritable(IntPtr h)
    {
        if (h != IntPtr.Zero && h != new IntPtr(-1)) SetHandleInformation(h, HANDLE_FLAG_INHERIT, HANDLE_FLAG_INHERIT);
        return h;
    }

    static string Quote(string arg)
    {
        if (arg.Length > 0 && arg.IndexOfAny(new[] { ' ', '\t', '"' }) < 0) return arg;
        var sb = new StringBuilder("\"");
        var backslashes = 0;
        foreach (var c in arg)
        {
            if (c == '\\') { backslashes++; continue; }
            if (c == '"') sb.Append('\\', backslashes * 2 + 1).Append('"');
            else sb.Append('\\', backslashes).Append(c);
            backslashes = 0;
        }
        return sb.Append('\\', backslashes * 2).Append('"').ToString();
    }

    // ---- state / list / ps ----

    static int State(Globals g, string[] args)
    {
        if (args.Length != 1) throw new UsageException("\"state\" requires exactly 1 argument(s)");
        WriteStateJson(Console.Out, Load(g, args[0]));
        return 0;
    }

    static int List(Globals g, string[] args)
    {
        var quiet = false;
        var format = "table";
        for (var i = 0; i < args.Length; i++)
        {
            var (name, inline) = SplitOption(args[i]);
            switch (name)
            {
                case "-q" or "--quiet": quiet = true; break;
                case "-f" or "--format": format = inline ?? Next(args, ref i, name); break;
                default: throw new UsageException($"flag provided but not defined: {args[i]}");
            }
        }
        var states = new List<ContainerState>();
        if (Directory.Exists(g.Root))
            foreach (var dir in Directory.GetDirectories(g.Root).Order(StringComparer.Ordinal))
                if (File.Exists(Path.Combine(dir, "state.json")))
                    try { states.Add(Load(g, Path.GetFileName(dir))); }
                    catch (InvalidOperationException) { }
                    catch (UsageException) { }
        if (quiet) { foreach (var s in states) Console.WriteLine(s.Id); return 0; }
        if (format == "json")
        {
            using var buffer = new MemoryStream();
            using (var w = new Utf8JsonWriter(buffer))
            {
                w.WriteStartArray();
                foreach (var s in states)
                {
                    w.WriteStartObject();
                    w.WriteString("ociVersion", SpecVersion);
                    w.WriteString("id", s.Id);
                    w.WriteNumber("pid", s.Status == "stopped" ? 0 : s.Pid);
                    w.WriteString("status", s.Status);
                    w.WriteString("bundle", s.Bundle);
                    w.WriteString("rootfs", s.Rootfs);
                    w.WriteString("created", s.Created);
                    w.WriteString("owner", s.Owner);
                    w.WriteEndObject();
                }
                w.WriteEndArray();
            }
            Console.WriteLine(states.Count == 0 ? "null" : Encoding.UTF8.GetString(buffer.ToArray()));
            return 0;
        }
        if (format != "table") throw new UsageException($"invalid format option: {format}");
        // Like runc's tabwriter: columns as wide as their widest cell plus padding.
        var rows = new List<string[]> { new[] { "ID", "PID", "STATUS", "BUNDLE", "CREATED", "OWNER" } };
        rows.AddRange(states.Select(s => new[] { s.Id, (s.Status == "stopped" ? 0 : s.Pid).ToString(), s.Status, s.Bundle, s.Created, s.Owner }));
        var widths = Enumerable.Range(0, 5).Select(c => rows.Max(r => r[c].Length) + 3).ToArray();
        foreach (var r in rows)
            Console.WriteLine(string.Concat(Enumerable.Range(0, 5).Select(c => r[c].PadRight(widths[c]))) + r[5]);
        return 0;
    }

    static int Ps(Globals g, string[] args)
    {
        var format = "table";
        string id = null;
        for (var i = 0; i < args.Length; i++)
        {
            var (name, inline) = SplitOption(args[i]);
            if (name is "-f" or "--format") format = inline ?? Next(args, ref i, name);
            else if (!args[i].StartsWith('-')) { id = args[i]; break; }
            else throw new UsageException($"flag provided but not defined: {args[i]}");
        }
        if (id == null) throw new UsageException("\"ps\" requires a minimum of 1 argument(s)");
        var s = Load(g, id);
        if (s.Status is not ("running" or "paused")) throw new InvalidOperationException("container is not running");
        var job = OpenJob(g, id);
        var pids = job != IntPtr.Zero ? JobPids(job) : new List<int>();
        if (job != IntPtr.Zero) CloseHandle(job);
        if (format == "json") { Console.WriteLine("[" + string.Join(",", pids) + "]"); return 0; }
        Console.WriteLine($"{"UID",-10}{"PID",8}{"PPID",8}  {"TIME",-10}CMD");
        foreach (var pid in pids)
        {
            string name = "?", time = "00:00:00";
            int ppid = 0;
            try
            {
                using var p = System.Diagnostics.Process.GetProcessById(pid);
                name = p.ProcessName;
                time = p.TotalProcessorTime.ToString(@"hh\:mm\:ss");
            }
            catch (ArgumentException) { }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) { }
            ppid = ParentOf(pid);
            Console.WriteLine($"{Environment.UserName,-10}{pid,8}{ppid,8}  {time,-10}{name}");
        }
        return 0;
    }

    static unsafe int ParentOf(int pid)
    {
        var h = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (h == IntPtr.Zero) return 0;
        var info = new PROCESS_BASIC_INFORMATION();
        var r = NtQueryInformationProcess(h, 0, ref info, sizeof(PROCESS_BASIC_INFORMATION), out _);
        CloseHandle(h);
        return r == 0 ? (int)info.InheritedFromUniqueProcessId : 0;
    }

    // ---- kill / delete / pause / update / exec / events ----

    static int ParseSignal(string s)
    {
        var name = s.ToUpperInvariant();
        if (name.StartsWith("SIG")) name = name[3..];
        if (int.TryParse(name, out var n)) return n;
        return name switch
        {
            "HUP" => 1, "INT" => 2, "QUIT" => 3, "KILL" => 9, "USR1" => 10, "USR2" => 12, "TERM" => 15,
            "STOP" => 19, "CONT" => 18, "WINCH" => 28,
            _ => throw new UsageException($"unknown signal \"{s}\""),
        };
    }

    static int Kill(Globals g, string[] args)
    {
        var positional = args.Where(a => !a.StartsWith('-')).ToList();
        if (positional.Count is < 1 or > 2) throw new UsageException("\"kill\" requires a minimum of 1 argument(s)");
        var signal = positional.Count == 2 ? ParseSignal(positional[1]) : 15;
        var s = Load(g, positional[0]);
        if (s.Status is not ("running" or "paused" or "created")) throw new InvalidOperationException("container not running");
        var job = OpenJob(g, s.Id);
        if (job == IntPtr.Zero) throw new InvalidOperationException("container not running");
        try
        {
            switch (signal)
            {
                case 9:
                    TerminateJobObject(job, 137);
                    break;
                case 19:
                    foreach (var pid in JobPids(job)) Suspend(pid, true);
                    break;
                case 18:
                    foreach (var pid in JobPids(job)) Suspend(pid, false);
                    break;
                case 1 or 2 or 3 or 15:
                    // The process group's Ctrl+Break (Ctrl+C is off for new process groups). Without a console
                    // to deliver it to, terminating is all Windows offers.
                    if (!SendCtrlBreak(s.Pid)) TerminateJobObject(job, (uint)(128 + signal));
                    break;
                default:
                    throw new InvalidOperationException($"signal {signal} has no Windows equivalent (TERM, INT, HUP, QUIT, KILL, STOP and CONT do)");
            }
        }
        finally { CloseHandle(job); }
        return 0;
    }

    static bool SendCtrlBreak(int pid)
    {
        FreeConsole();
        var ok = AttachConsole(pid);
        if (ok)
        {
            SetConsoleCtrlHandler(IntPtr.Zero, true);
            ok = GenerateConsoleCtrlEvent(CTRL_BREAK_EVENT, pid);
            FreeConsole();
            SetConsoleCtrlHandler(IntPtr.Zero, false);
        }
        AttachConsole(ATTACH_PARENT_PROCESS);
        return ok;
    }

    static void Suspend(int pid, bool suspend)
    {
        var h = OpenProcess(PROCESS_SUSPEND_RESUME, false, pid);
        if (h == IntPtr.Zero) return;
        if (suspend) NtSuspendProcess(h);
        else NtResumeProcess(h);
        CloseHandle(h);
    }

    static int Delete(Globals g, string[] args)
    {
        var force = args.Contains("-f") || args.Contains("--force");
        var ids = args.Where(a => !a.StartsWith('-')).ToList();
        if (ids.Count == 0) throw new UsageException("\"delete\" requires a minimum of 1 argument(s)");
        var status = 0;
        foreach (var id in ids)
        {
            ContainerState s;
            try { s = Load(g, id); }
            catch (InvalidOperationException) when (force) { continue; }
            if (s.Status is "running" or "paused" && !force)
            {
                Console.Error.WriteLine($"{g.Tool}: cannot delete container {id} that is not stopped: {s.Status}");
                status = 1;
                continue;
            }
            Destroy(g, s, force);
        }
        return status;
    }

    static void Destroy(Globals g, ContainerState s, bool force)
    {
        var job = OpenJob(g, s.Id);
        if (job != IntPtr.Zero)
        {
            if (force) TerminateJobObject(job, 137);
            CloseHandle(job);
        }
        try
        {
            using var delete = EventWaitHandle.OpenExisting(EventName(g, s.Id, "delete"));
            delete.Set();
        }
        catch (WaitHandleCannotBeOpenedException) { }
        // Wait for the monitor to let go of the job, then run poststop hooks and clean up.
        for (var i = 0; i < 100 && Alive(s.MonitorPid) && s.MonitorPid != Environment.ProcessId; i++) Thread.Sleep(50);
        foreach (var j in s.Junctions)
            try { if (Directory.Exists(j)) Directory.Delete(j); }
            catch (IOException) { }
        try
        {
            var bundle = ReadBundle(s.Bundle);
            RunHooks(bundle, "poststop", s);
        }
        catch (InvalidOperationException) { }
        catch (IOException) { }
        try { Directory.Delete(StateDir(g, s.Id), true); }
        catch (IOException) { }
    }

    static int PauseResume(Globals g, string[] args, bool pause)
    {
        if (args.Length != 1) throw new UsageException($"\"{(pause ? "pause" : "resume")}\" requires exactly 1 argument(s)");
        var s = Load(g, args[0]);
        if (pause && s.Status != "running") throw new InvalidOperationException("container not running");
        if (!pause && s.Status != "paused") throw new InvalidOperationException("container not paused");
        var job = OpenJob(g, s.Id);
        if (job == IntPtr.Zero) throw new InvalidOperationException("container not running");
        foreach (var pid in JobPids(job)) Suspend(pid, pause);
        CloseHandle(job);
        s.Status = pause ? "paused" : "running";
        Save(g, s);
        return 0;
    }

    static ulong ParseBytes(string s)
    {
        var m = System.Text.RegularExpressions.Regex.Match(s.Trim(), @"^(\d+)\s*([kKmMgGtT]?)[bB]?$");
        if (!m.Success) throw new UsageException($"invalid value for memory: {s}");
        var n = ulong.Parse(m.Groups[1].Value);
        return m.Groups[2].Value.ToLowerInvariant() switch { "k" => n << 10, "m" => n << 20, "g" => n << 30, "t" => n << 40, _ => n };
    }

    static int Update(Globals g, string[] args)
    {
        ulong memory = 0;
        long quota = 0, period = 100000;
        uint pids = 0;
        string id = null, resourcesFile = null;
        for (var i = 0; i < args.Length; i++)
        {
            var (name, inline) = SplitOption(args[i]);
            switch (name)
            {
                case "-m" or "--memory": memory = ParseBytes(inline ?? Next(args, ref i, name)); break;
                case "--cpu-quota": quota = long.Parse(inline ?? Next(args, ref i, name)); break;
                case "--cpu-period": period = long.Parse(inline ?? Next(args, ref i, name)); break;
                case "--pids-limit": pids = uint.Parse(inline ?? Next(args, ref i, name)); break;
                case "-r" or "--resources": resourcesFile = inline ?? Next(args, ref i, name); break;
                case "--memory-swap" or "--memory-reservation" or "--cpu-shares" or "--cpuset-cpus" or "--cpuset-mems" or "--blkio-weight" or "--kernel-memory":
                    if (inline == null) Next(args, ref i, name);
                    break;
                default:
                    if (args[i].StartsWith('-')) throw new UsageException($"flag provided but not defined: {args[i]}");
                    id = args[i];
                    break;
            }
        }
        if (id == null) throw new UsageException("\"update\" requires exactly 1 argument(s)");
        if (resourcesFile != null)
        {
            using var doc = JsonDocument.Parse(resourcesFile == "-" ? Console.In.ReadToEnd() : File.ReadAllText(resourcesFile));
            var r = doc.RootElement;
            if (r.TryGetProperty("memory", out var mem) && mem.TryGetProperty("limit", out var l) && l.TryGetInt64(out var lv) && lv > 0) memory = (ulong)lv;
            if (r.TryGetProperty("cpu", out var cpu) && cpu.TryGetProperty("quota", out var q) && q.TryGetInt64(out var qv)) quota = qv;
            if (r.TryGetProperty("cpu", out var cpu2) && cpu2.TryGetProperty("period", out var p) && p.TryGetInt64(out var pv) && pv > 0) period = pv;
            if (r.TryGetProperty("pids", out var pi) && pi.TryGetProperty("limit", out var pl) && pl.TryGetInt64(out var plv) && plv > 0) pids = (uint)plv;
        }
        var s = Load(g, id);
        if (s.Status == "stopped") throw new InvalidOperationException("container not running");
        var job = OpenJob(g, s.Id);
        if (job == IntPtr.Zero) throw new InvalidOperationException("container not running");
        var rate = quota > 0 ? (uint)Math.Clamp(quota * 10000L / period / Environment.ProcessorCount, 1, 10000) : 0;
        try { ApplyLimits(job, memory, rate, pids); }
        finally { CloseHandle(job); }
        return 0;
    }

    static int Exec(Globals g, string[] args)
    {
        string processFile = null, cwd = null, id = null, pidFile = null;
        var env = new List<string>();
        var detach = false;
        var i = 0;
        for (; i < args.Length; i++)
        {
            var a = args[i];
            if (!a.StartsWith('-')) { id = a; i++; break; }
            var (name, inline) = SplitOption(a);
            switch (name)
            {
                case "-p" or "--process": processFile = inline ?? Next(args, ref i, name); break;
                case "--cwd": cwd = inline ?? Next(args, ref i, name); break;
                case "-e" or "--env": env.Add(inline ?? Next(args, ref i, name)); break;
                case "-d" or "--detach": detach = true; break;
                case "--pid-file": pidFile = inline ?? Next(args, ref i, name); break;
                case "-t" or "--tty" or "--no-new-privs": break;
                case "--console-socket" or "-u" or "--user" or "--cap" or "--apparmor" or "--process-label" or "--preserve-fds" or "--additional-gids":
                    if (inline == null) Next(args, ref i, name);
                    break;
                default: throw new UsageException($"flag provided but not defined: {a}");
            }
        }
        if (id == null) throw new UsageException("\"exec\" requires a minimum of 1 argument(s)");
        var s = Load(g, id);
        if (s.Status != "running") throw new InvalidOperationException("cannot exec in a stopped container");
        var bundle = ReadBundle(s.Bundle);
        if (processFile != null)
        {
            using var doc = JsonDocument.Parse(File.ReadAllBytes(processFile));
            bundle.Process = ReadProcess(doc.RootElement);
        }
        else
        {
            if (i >= args.Length) throw new UsageException("exec: no command given");
            bundle.Process.Args = args[i..].ToList();
        }
        if (cwd != null) bundle.Process.Cwd = cwd;
        foreach (var e in env)
        {
            var key = e.Split('=')[0] + "=";
            bundle.Process.Env.RemoveAll(x => x.StartsWith(key, StringComparison.OrdinalIgnoreCase));
            bundle.Process.Env.Add(e);
        }
        var job = OpenJob(g, s.Id);
        if (job == IntPtr.Zero) throw new InvalidOperationException("container not running");
        var (application, commandLine) = ResolveProgram(bundle);
        var process = Spawn(application, commandLine, EnvironmentBlock(bundle.Process.Env, bundle.Rootfs), MapPath(bundle.Process.Cwd, bundle.Rootfs),
            CREATE_SUSPENDED, breakaway: false, out var thread, out var pid);
        AssignProcessToJobObject(job, process);
        CloseHandle(job);
        ResumeThread(thread);
        CloseHandle(thread);
        if (pidFile != null) File.WriteAllText(pidFile, pid.ToString());
        if (detach) { CloseHandle(process); return 0; }
        WaitForSingleObject(process, INFINITE);
        GetExitCodeProcess(process, out var code);
        CloseHandle(process);
        return (int)code;
    }

    static int Events(Globals g, string[] args)
    {
        var stats = false;
        var interval = TimeSpan.FromSeconds(5);
        string id = null;
        for (var i = 0; i < args.Length; i++)
        {
            var (name, inline) = SplitOption(args[i]);
            switch (name)
            {
                case "--stats": stats = true; break;
                case "--interval": interval = ParseDuration(inline ?? Next(args, ref i, name)); break;
                default:
                    if (args[i].StartsWith('-')) throw new UsageException($"flag provided but not defined: {args[i]}");
                    id = args[i];
                    break;
            }
        }
        if (id == null) throw new UsageException("\"events\" requires exactly 1 argument(s)");
        for (;;)
        {
            var s = Load(g, id);
            if (s.Status == "stopped") { if (stats) throw new InvalidOperationException("container not running"); return 0; }
            var job = OpenJob(g, id);
            if (job == IntPtr.Zero) return 0;
            Console.WriteLine(StatsJson(id, job));
            CloseHandle(job);
            if (stats) return 0;
            Thread.Sleep(interval);
        }
    }

    static TimeSpan ParseDuration(string s)
    {
        var m = System.Text.RegularExpressions.Regex.Match(s, @"^(\d+(?:\.\d+)?)(ms|s|m|h)?$");
        if (!m.Success) throw new UsageException($"invalid duration: {s}");
        var n = double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        return m.Groups[2].Value switch { "ms" => TimeSpan.FromMilliseconds(n), "m" => TimeSpan.FromMinutes(n), "h" => TimeSpan.FromHours(n), _ => TimeSpan.FromSeconds(n) };
    }

    static string StatsJson(string id, IntPtr job)
    {
        var accounting = new JOBOBJECT_BASIC_ACCOUNTING_INFORMATION();
        QueryInformationJobObjectAccounting(job, JobObjectBasicAccountingInformation, ref accounting, Marshal.SizeOf<JOBOBJECT_BASIC_ACCOUNTING_INFORMATION>(), IntPtr.Zero);
        var limits = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
        QueryInformationJobObject(job, JobObjectExtendedLimitInformation, ref limits, Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>(), IntPtr.Zero);
        ulong usage = 0;
        foreach (var pid in JobPids(job))
            try { using var p = System.Diagnostics.Process.GetProcessById(pid); usage += (ulong)p.PrivateMemorySize64; }
            catch (ArgumentException) { }
            catch (InvalidOperationException) { }
        using var buffer = new MemoryStream();
        using (var w = new Utf8JsonWriter(buffer))
        {
            w.WriteStartObject();
            w.WriteString("type", "stats");
            w.WriteString("id", id);
            w.WriteStartObject("data");
            w.WriteStartObject("cpu");
            w.WriteStartObject("usage");
            w.WriteNumber("total", (accounting.TotalUserTime + accounting.TotalKernelTime) * 100);
            w.WriteNumber("user", accounting.TotalUserTime * 100);
            w.WriteNumber("kernel", accounting.TotalKernelTime * 100);
            w.WriteEndObject();
            w.WriteEndObject();
            w.WriteStartObject("memory");
            w.WriteStartObject("usage");
            w.WriteNumber("usage", usage);
            w.WriteNumber("max", (ulong)limits.PeakJobMemoryUsed);
            w.WriteNumber("limit", (limits.BasicLimitInformation.LimitFlags & JOB_OBJECT_LIMIT_JOB_MEMORY) != 0 ? (ulong)limits.JobMemoryLimit : 0);
            w.WriteEndObject();
            w.WriteEndObject();
            w.WriteStartObject("pids");
            w.WriteNumber("current", accounting.ActiveProcesses);
            w.WriteNumber("limit", (limits.BasicLimitInformation.LimitFlags & JOB_OBJECT_LIMIT_ACTIVE_PROCESS) != 0 ? limits.BasicLimitInformation.ActiveProcessLimit : 0);
            w.WriteEndObject();
            w.WriteEndObject();
            w.WriteEndObject();
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    static int Features()
    {
        Console.WriteLine($$"""
            {
                "ociVersionMin": "1.0.0",
                "ociVersionMax": "{{SpecVersion}}",
                "hooks": ["prestart", "createRuntime", "createContainer", "startContainer", "poststart", "poststop"],
                "mountOptions": ["bind", "rbind"],
                "annotations": {
                    "org.opencontainers.runc.version": "{{RuncVersion}}",
                    "io.github.samuelarther.wintools.isolation": "job-object"
                }
            }
            """);
        return 0;
    }

    // ---- Win32 ----

    const uint CREATE_SUSPENDED = 0x4, CREATE_NEW_PROCESS_GROUP = 0x200, CREATE_UNICODE_ENVIRONMENT = 0x400, CREATE_BREAKAWAY_FROM_JOB = 0x01000000;
    const uint SYNCHRONIZE = 0x100000, PROCESS_QUERY_LIMITED_INFORMATION = 0x1000, PROCESS_SUSPEND_RESUME = 0x800, INFINITE = 0xFFFFFFFF, WAIT_TIMEOUT = 0x102;
    const uint JOB_OBJECT_ALL_ACCESS = 0x1F001F, JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000, JOB_OBJECT_LIMIT_JOB_MEMORY = 0x200, JOB_OBJECT_LIMIT_ACTIVE_PROCESS = 0x8;
    const uint JOB_OBJECT_CPU_RATE_CONTROL_ENABLE = 0x1, JOB_OBJECT_CPU_RATE_CONTROL_HARD_CAP = 0x4;
    const int JobObjectBasicAccountingInformation = 1, JobObjectBasicProcessIdList = 3, JobObjectExtendedLimitInformation = 9, JobObjectCpuRateControlInformation = 15;
    const int ERROR_ALREADY_EXISTS = 183, STARTF_USESTDHANDLES = 0x100, HANDLE_FLAG_INHERIT = 1;
    const int STD_INPUT_HANDLE = -10, STD_OUTPUT_HANDLE = -11, STD_ERROR_HANDLE = -12, ATTACH_PARENT_PROCESS = -1;
    const uint CTRL_BREAK_EVENT = 1, GENERIC_WRITE = 0x40000000, FILE_FLAG_BACKUP_SEMANTICS = 0x02000000, FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;
    const uint IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003, FSCTL_SET_REPARSE_POINT = 0x000900A4;

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
    struct JOBOBJECT_CPU_RATE_CONTROL_INFORMATION { public uint ControlFlags, Value; }

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

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr OpenJobObjectW(uint access, bool inherit, string name);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool SetInformationJobObject(IntPtr job, int infoClass, ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION info, int length);

    [DllImport("kernel32.dll", EntryPoint = "SetInformationJobObject", SetLastError = true)]
    static extern bool SetInformationJobObjectCpu(IntPtr job, int infoClass, ref JOBOBJECT_CPU_RATE_CONTROL_INFORMATION info, int length);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool QueryInformationJobObject(IntPtr job, int infoClass, ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION info, int length, IntPtr returned);

    [DllImport("kernel32.dll", EntryPoint = "QueryInformationJobObject", SetLastError = true)]
    static extern bool QueryInformationJobObjectAccounting(IntPtr job, int infoClass, ref JOBOBJECT_BASIC_ACCOUNTING_INFORMATION info, int length, IntPtr returned);

    [DllImport("kernel32.dll", EntryPoint = "QueryInformationJobObject", SetLastError = true)]
    static extern bool QueryInformationJobObjectRaw(IntPtr job, int infoClass, IntPtr info, int length, IntPtr returned);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool TerminateJobObject(IntPtr job, uint exitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool TerminateProcess(IntPtr process, uint exitCode);

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
    static extern IntPtr GetStdHandle(int which);

    [DllImport("kernel32.dll")]
    static extern bool SetStdHandle(int which, IntPtr handle);

    [DllImport("kernel32.dll")]
    static extern bool SetHandleInformation(IntPtr handle, int mask, int flags);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern int SearchPathW(string path, string fileName, string extension, int bufferLength, StringBuilder buffer, IntPtr filePart);

    [DllImport("kernel32.dll")]
    static extern bool FreeConsole();

    [DllImport("kernel32.dll")]
    static extern bool AttachConsole(int pid);

    [DllImport("kernel32.dll")]
    static extern bool SetConsoleCtrlHandler(IntPtr handler, bool add);

    [DllImport("kernel32.dll")]
    static extern bool GenerateConsoleCtrlEvent(uint ctrlEvent, int processGroupId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr CreateFileW(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool DeviceIoControl(IntPtr device, uint code, byte[] input, int inputSize, IntPtr output, int outputSize, out int returned, IntPtr overlapped);

    [DllImport("ntdll.dll")]
    static extern int NtSuspendProcess(IntPtr process);

    [DllImport("ntdll.dll")]
    static extern int NtResumeProcess(IntPtr process);

    [DllImport("ntdll.dll")]
    static extern int NtQueryInformationProcess(IntPtr process, int infoClass, ref PROCESS_BASIC_INFORMATION info, int length, out int returned);
}
