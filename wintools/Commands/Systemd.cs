using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace WinTools;

// systemd's systemctl on the Windows Service Control Manager: every Windows service is a
// "<name>.service" unit. enable = Automatic start, disable = Manual, mask = Disabled.
// journalctl, hostnamectl, timedatectl, loginctl and systemd-path live in Journal.cs/SystemdTools.cs.
static class Systemd
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("systemctl", "Control Windows services the systemd way", Run);
    }

    const string Help = """
        Usage: systemctl [OPTIONS...] COMMAND [UNIT...]
        Query or send control commands to Windows services (units are <service>.service).

        Unit commands:
          list-units [PATTERN...]       List running (or with --all, all) services
          list-unit-files [PATTERN...]  List services and their start type
          status [UNIT...]              Show runtime status and recent log lines
          show [UNIT...]                Show properties (-p NAME, --value)
          cat UNIT...                   Show a unit file synthesized from the service config
          start|stop|restart UNIT...    Start, stop or restart services
          try-restart|reload UNIT...    Restart if running / ask a service to reload its settings
          is-active|is-failed UNIT...   Check whether units are active / failed
        Unit file commands:
          enable|disable UNIT... [--now]  Automatic start / Manual start (--now also starts/stops)
          mask|unmask UNIT...           Disabled start type / back to Manual
          is-enabled UNIT...            Print enabled, disabled, masked or static
          daemon-reload                 Nothing to reload on Windows (accepted)
        System commands:
          reboot|poweroff|halt|suspend|hibernate

        Options: -a --all, -t --type=service, --state=STATE, -q --quiet, --no-legend, --no-pager,
                 --plain, -l --full, -n --lines=N, -p --property=NAME, --value, --now, --user, --system
        """;

    sealed class Options
    {
        public bool All, Quiet, NoLegend, Now, Value, Full;
        public int Lines = 10;
        public string Type, State;
        public List<string> Properties = [];
        public string Verb;
        public List<string> Args = [];
    }

    static int Run(string[] args)
    {
        var o = new Options();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Next() => i + 1 < args.Length ? args[++i] : throw new UsageException($"option '{a}' requires an argument");
            if (a is "-h" or "--help") { Console.WriteLine(Help); return 0; }
            if (a == "--version") { Console.WriteLine($"systemd 256 (wintools {Commands.Version})\n+WINDOWS-SCM"); return 0; }
            if (a.StartsWith("--") && a.Contains('='))
            {
                var (k, v) = (a[..a.IndexOf('=')], a[(a.IndexOf('=') + 1)..]);
                switch (k)
                {
                    case "--type": o.Type = v; break;
                    case "--state": o.State = v; break;
                    case "--property": o.Properties.AddRange(v.Split(',')); break;
                    case "--lines": o.Lines = ParseInt(v); break;
                    default: throw new UsageException($"unrecognized option '{a}'");
                }
                continue;
            }
            switch (a)
            {
                case "-a" or "--all": o.All = true; break;
                case "-q" or "--quiet": o.Quiet = true; break;
                case "--no-legend": o.NoLegend = true; break;
                case "--now": o.Now = true; break;
                case "--value": o.Value = true; break;
                case "-l" or "--full": o.Full = true; break;
                case "-t" or "--type": o.Type = Next(); break;
                case "--state": o.State = Next(); break;
                case "-p" or "--property": o.Properties.AddRange(Next().Split(',')); break;
                case "-n" or "--lines": o.Lines = ParseInt(Next()); break;
                // Accepted for compatibility; Windows has one service manager and no pager here.
                case "--no-pager" or "--plain" or "--user" or "--system" or "--no-block" or "--no-ask-password"
                    or "-f" or "--force" or "--runtime": break;
                default:
                    if (a.StartsWith('-') && a.Length > 1) throw new UsageException($"invalid option -- '{a.TrimStart('-')}'");
                    if (o.Verb == null) o.Verb = a; else o.Args.Add(a);
                    break;
            }
        }
        if (o.Type != null && !o.Type.Split(',').Contains("service"))
            throw new UsageException($"unit type '{o.Type}' isn't available on Windows: every unit is a .service");
        switch (o.Verb ?? "list-units")
        {
            case "list-units" or "list": return ListUnits(o);
            case "list-unit-files": return ListUnitFiles(o);
            case "status": return Status(o);
            case "show": return Show(o);
            case "cat": return Cat(o);
            case "start" or "stop" or "restart" or "try-restart" or "reload" or "reload-or-restart" or "condrestart":
                return Control(o);
            case "is-active" or "is-failed": return IsActive(o);
            case "is-enabled": return IsEnabled(o);
            case "enable" or "disable" or "mask" or "unmask" or "reenable": return SetStartType(o);
            case "daemon-reload" or "daemon-reexec": return 0;
            case "reboot": return Exec("shutdown.exe", "/r /t 0");
            case "poweroff" or "halt": return Exec("shutdown.exe", "/s /t 0");
            case "suspend": return SetSuspendState(false, false, false) ? 0 : Fail("Failed to suspend system");
            case "hibernate": return SetSuspendState(true, false, false) ? 0 : Fail("Failed to hibernate system");
            case "kill":
                throw new UsageException("kill: use 'systemctl stop' (Windows services are stopped through the SCM)");
            case "list-timers" or "list-sockets":
                Console.Error.WriteLine($"systemctl: {o.Verb}: Windows has no timer/socket units (scheduled tasks: schtasks /query)");
                return 1;
            default: throw new UsageException($"unknown command verb '{o.Verb}'");
        }
    }

    static int ParseInt(string s) => int.TryParse(s, out var n) && n >= 0 ? n : throw new UsageException($"invalid number '{s}'");

    static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }

    static int Exec(string file, string arguments)
    {
        using var p = Process.Start(new ProcessStartInfo(file, arguments) { UseShellExecute = false });
        p.WaitForExit();
        return p.ExitCode;
    }

    // ---- unit model ----

    internal sealed class Unit
    {
        public string Name, DisplayName;
        public uint Type, State, ExitCode, SpecificExitCode, Pid, ControlsAccepted;
        public string Id => Name + ".service";
        public bool Failed => State == SERVICE_STOPPED && ExitCode != 0 && ExitCode != 1077;
        public string Active => State switch
        {
            SERVICE_RUNNING or SERVICE_PAUSED => "active",
            SERVICE_START_PENDING or SERVICE_CONTINUE_PENDING => "activating",
            SERVICE_STOP_PENDING or SERVICE_PAUSE_PENDING => "deactivating",
            _ => Failed ? "failed" : "inactive",
        };
        public string Sub => State switch
        {
            SERVICE_RUNNING => "running",
            SERVICE_PAUSED => "paused",
            SERVICE_START_PENDING => "start",
            SERVICE_CONTINUE_PENDING => "continue",
            SERVICE_STOP_PENDING => "stop",
            SERVICE_PAUSE_PENDING => "pause",
            _ => Failed ? "failed" : "dead",
        };
    }

    internal sealed class Config
    {
        public uint StartType, ServiceType;
        public string BinaryPath, StartName, DisplayName, Description, Group;
        public bool Delayed, Triggered;
        public List<string> Dependencies = [];
        // systemd's unit-file states: Automatic = enabled, Manual = disabled, Disabled = masked,
        // boot/system drivers = static.
        public string FileState => StartType switch
        {
            2 => "enabled",
            3 => Triggered ? "indirect" : "disabled",
            4 => "masked",
            _ => "static",
        };
    }

    // "wuauserv.service" or "wuauserv" -> "wuauserv"; other unit types don't exist on Windows.
    internal static string ServiceName(string unit)
    {
        var dot = unit.LastIndexOf('.');
        if (dot <= 0) return unit;
        var suffix = unit[(dot + 1)..].ToLowerInvariant();
        if (suffix == "service") return unit[..dot];
        if (suffix is "target" or "timer" or "socket" or "mount" or "automount" or "device" or "path" or "slice" or "scope" or "swap")
            throw new UsageException($"unit {unit}: Windows only has .service units");
        return unit;
    }

    internal static List<Unit> Enumerate()
    {
        var scm = OpenSCManagerW(null, null, SC_MANAGER_CONNECT | SC_MANAGER_ENUMERATE_SERVICE);
        if (scm == IntPtr.Zero) throw new System.ComponentModel.Win32Exception();
        try
        {
            var units = new List<Unit>();
            uint resume = 0;
            var entrySize = (IntPtr.Size * 2 + 36 + IntPtr.Size - 1) & ~(IntPtr.Size - 1);
            while (true)
            {
                EnumServicesStatusExW(scm, 0, SERVICE_WIN32, SERVICE_STATE_ALL, IntPtr.Zero, 0, out var needed, out _, ref resume, null);
                var buffer = Marshal.AllocHGlobal((int)needed + 4096);
                try
                {
                    var ok = EnumServicesStatusExW(scm, 0, SERVICE_WIN32, SERVICE_STATE_ALL, buffer, needed + 4096, out _, out var count, ref resume, null);
                    var error = Marshal.GetLastWin32Error();
                    if (!ok && error != ERROR_MORE_DATA) throw new System.ComponentModel.Win32Exception(error);
                    for (var i = 0; i < count; i++)
                    {
                        var p = buffer + i * entrySize;
                        var s = p + IntPtr.Size * 2;
                        units.Add(new Unit
                        {
                            Name = Marshal.PtrToStringUni(Marshal.ReadIntPtr(p)),
                            DisplayName = Marshal.PtrToStringUni(Marshal.ReadIntPtr(p + IntPtr.Size)),
                            Type = (uint)Marshal.ReadInt32(s),
                            State = (uint)Marshal.ReadInt32(s, 4),
                            ControlsAccepted = (uint)Marshal.ReadInt32(s, 8),
                            ExitCode = (uint)Marshal.ReadInt32(s, 12),
                            SpecificExitCode = (uint)Marshal.ReadInt32(s, 16),
                            Pid = (uint)Marshal.ReadInt32(s, 28),
                        });
                    }
                    if (ok) break;
                }
                finally { Marshal.FreeHGlobal(buffer); }
            }
            units.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));
            return units;
        }
        finally { CloseServiceHandle(scm); }
    }

    // Opens one service; null when it doesn't exist.
    internal static SafeService Open(string name, uint access)
    {
        var scm = OpenSCManagerW(null, null, SC_MANAGER_CONNECT);
        if (scm == IntPtr.Zero) throw new System.ComponentModel.Win32Exception();
        var h = OpenServiceW(scm, name, access);
        var error = Marshal.GetLastWin32Error();
        if (h == IntPtr.Zero && error == ERROR_SERVICE_DOES_NOT_EXIST)
        {
            // Accept a display name too ("Windows Update").
            var size = 512u;
            var key = new StringBuilder(512);
            if (GetServiceKeyNameW(scm, name, key, ref size)) h = OpenServiceW(scm, key.ToString(), access);
            error = Marshal.GetLastWin32Error();
        }
        CloseServiceHandle(scm);
        if (h != IntPtr.Zero) return new SafeService(h);
        if (error == ERROR_SERVICE_DOES_NOT_EXIST || error == ERROR_INVALID_NAME) return null;
        throw new System.ComponentModel.Win32Exception(error);
    }

    internal sealed class SafeService(IntPtr handle) : IDisposable
    {
        public readonly IntPtr Handle = handle;
        public void Dispose() => CloseServiceHandle(Handle);
    }

    internal static Unit QueryUnit(SafeService s, string name)
    {
        var buf = new byte[36];
        if (!QueryServiceStatusEx(s.Handle, 0, buf, 36, out _)) throw new System.ComponentModel.Win32Exception();
        return new Unit
        {
            Name = name,
            Type = BitConverter.ToUInt32(buf, 0),
            State = BitConverter.ToUInt32(buf, 4),
            ControlsAccepted = BitConverter.ToUInt32(buf, 8),
            ExitCode = BitConverter.ToUInt32(buf, 12),
            SpecificExitCode = BitConverter.ToUInt32(buf, 16),
            Pid = BitConverter.ToUInt32(buf, 28),
        };
    }

    internal static Config QueryConfig(SafeService s)
    {
        QueryServiceConfigW(s.Handle, IntPtr.Zero, 0, out var needed);
        var buf = Marshal.AllocHGlobal((int)needed);
        try
        {
            if (!QueryServiceConfigW(s.Handle, buf, needed, out _)) throw new System.ComponentModel.Win32Exception();
            // QUERY_SERVICE_CONFIGW: three DWORDs, then pointers (x64 pads dwTagId to 8 bytes).
            int[] at = IntPtr.Size == 8 ? [16, 24, 40, 48, 56] : [12, 16, 24, 28, 32];
            var c = new Config
            {
                ServiceType = (uint)Marshal.ReadInt32(buf, 0),
                StartType = (uint)Marshal.ReadInt32(buf, 4),
                BinaryPath = Str(buf + at[0]),
                Group = Str(buf + at[1]),
                StartName = Str(buf + at[3]),
                DisplayName = Str(buf + at[4]),
            };
            // lpDependencies: double-NUL-terminated list; "+name" entries are load-order groups.
            var deps = Marshal.ReadIntPtr(buf + at[2]);
            while (deps != IntPtr.Zero)
            {
                var d = Marshal.PtrToStringUni(deps);
                if (string.IsNullOrEmpty(d)) break;
                c.Dependencies.Add(d);
                deps += (d.Length + 1) * 2;
            }
            c.Description = Config2String(s, SERVICE_CONFIG_DESCRIPTION);
            c.Delayed = Config2Int(s, SERVICE_CONFIG_DELAYED_AUTO_START_INFO) != 0;
            c.Triggered = Config2Int(s, SERVICE_CONFIG_TRIGGER_INFO) != 0;
            return c;
        }
        finally { Marshal.FreeHGlobal(buf); }

        static string Str(IntPtr at) => Marshal.ReadIntPtr(at) is var p && p != IntPtr.Zero ? Marshal.PtrToStringUni(p) : null;
    }

    static string Config2String(SafeService s, uint level)
    {
        QueryServiceConfig2W(s.Handle, level, IntPtr.Zero, 0, out var needed);
        if (needed == 0) return null;
        var buf = Marshal.AllocHGlobal((int)needed);
        try
        {
            if (!QueryServiceConfig2W(s.Handle, level, buf, needed, out _)) return null;
            var p = Marshal.ReadIntPtr(buf);
            var text = p == IntPtr.Zero ? null : Marshal.PtrToStringUni(p);
            // Many descriptions are indirect strings ("@%SystemRoot%\system32\x.dll,-101").
            return text != null && text.StartsWith('@') ? IndirectString(text) : text;
        }
        finally { Marshal.FreeHGlobal(buf); }
    }

    static int Config2Int(SafeService s, uint level)
    {
        QueryServiceConfig2W(s.Handle, level, IntPtr.Zero, 0, out var needed);
        if (needed == 0) return 0;
        var buf = Marshal.AllocHGlobal((int)needed);
        try { return QueryServiceConfig2W(s.Handle, level, buf, needed, out _) ? Marshal.ReadInt32(buf) : 0; }
        finally { Marshal.FreeHGlobal(buf); }
    }

    internal static string IndirectString(string source)
    {
        var sb = new StringBuilder(1024);
        return SHLoadIndirectString(source, sb, sb.Capacity, IntPtr.Zero) == 0 ? sb.ToString() : source;
    }

    // ---- verbs ----

    static Regex[] Patterns(List<string> patterns) => patterns
        .Select(p => new Regex("^" + Regex.Escape(p.Contains('.') ? p : p + "*").Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        .ToArray();

    static bool Matches(Regex[] patterns, Unit u) =>
        patterns.Length == 0 || patterns.Any(r => r.IsMatch(u.Id) || r.IsMatch(u.Name));

    static int ListUnits(Options o)
    {
        var patterns = Patterns(o.Args);
        var states = o.State?.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var units = Enumerate().Where(u => Matches(patterns, u)).Where(u =>
            states != null ? states.Any(s => s.Equals(u.Active, StringComparison.OrdinalIgnoreCase) || s.Equals(u.Sub, StringComparison.OrdinalIgnoreCase))
            : o.All || patterns.Length > 0 || u.Active != "inactive").ToList();
        var w = Math.Max(4, units.Select(u => u.Id.Length).DefaultIfEmpty(0).Max());
        if (!o.NoLegend) Console.WriteLine($"  {"UNIT".PadRight(w)} LOAD   ACTIVE     SUB      DESCRIPTION");
        foreach (var u in units)
            Console.WriteLine($"{(u.Failed ? Bullet + " " : "  ")}{u.Id.PadRight(w)} loaded {u.Active,-10} {u.Sub,-8} {u.DisplayName}");
        if (!o.NoLegend)
        {
            Console.WriteLine();
            Console.WriteLine("LOAD   = Reflects whether the unit definition was properly loaded.");
            Console.WriteLine("ACTIVE = The high-level unit activation state, i.e. generalization of SUB.");
            Console.WriteLine("SUB    = The low-level unit activation state, values depend on unit type.");
            Console.WriteLine();
            Console.WriteLine($"{units.Count} loaded units listed." + (o.All ? "" : " Pass --all to see loaded but inactive units, too."));
            Console.WriteLine("To show all installed unit files use 'systemctl list-unit-files'.");
        }
        return 0;
    }

    static int ListUnitFiles(Options o)
    {
        var patterns = Patterns(o.Args);
        var states = o.State?.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var rows = new List<(string Id, string State)>();
        foreach (var u in Enumerate().Where(u => Matches(patterns, u)))
        {
            using var s = Open(u.Name, SERVICE_QUERY_CONFIG);
            var state = s == null ? "bad" : QueryConfig(s).FileState;
            if (states == null || states.Contains(state, StringComparer.OrdinalIgnoreCase)) rows.Add((u.Id, state));
        }
        var w = Math.Max(9, rows.Select(r => r.Id.Length).DefaultIfEmpty(0).Max());
        if (!o.NoLegend) Console.WriteLine($"{"UNIT FILE".PadRight(w)} STATE");
        foreach (var (id, state) in rows) Console.WriteLine($"{id.PadRight(w)} {state}");
        if (!o.NoLegend) Console.WriteLine($"\n{rows.Count} unit files listed.");
        return 0;
    }

    static string Bullet => Console.IsOutputRedirected ? "*" : "●";

    static int Status(Options o)
    {
        if (o.Args.Count == 0) return SystemStatus();
        var status = 0;
        var first = true;
        foreach (var arg in o.Args)
        {
            var name = ServiceName(arg);
            using var s = Open(name, SERVICE_QUERY_CONFIG | SERVICE_QUERY_STATUS);
            if (s == null)
            {
                Console.Error.WriteLine($"Unit {name}.service could not be found.");
                status = Math.Max(status, 4);
                continue;
            }
            if (!first) Console.WriteLine();
            first = false;
            var key = KeyName(name);
            var u = QueryUnit(s, key);
            var c = QueryConfig(s);
            Console.WriteLine($"{Bullet} {u.Id} - {c.DisplayName}");
            Console.WriteLine($"     Loaded: loaded ({c.BinaryPath}; {c.FileState}; start type: {StartTypeName(c)})");
            var since = "";
            Process proc = null;
            if (u.Pid != 0)
            {
                try { proc = Process.GetProcessById((int)u.Pid); } catch (ArgumentException) { }
                try { if (proc != null) since = $" since {proc.StartTime:ddd yyyy-MM-dd HH:mm:ss}; {Ago(DateTime.Now - proc.StartTime)} ago"; }
                catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException) { }
            }
            Console.WriteLine($"     Active: {u.Active} ({u.Sub}){since}");
            if (u.Failed)
                Console.WriteLine($"    Process: exited, code={u.ExitCode}{(u.ExitCode == 1066 ? $", status={u.SpecificExitCode}" : "")}");
            if (!string.IsNullOrEmpty(c.Description)) Console.WriteLine($"       Docs: {Shorten(c.Description, o.Full)}");
            if (proc != null)
            {
                Console.WriteLine($"   Main PID: {u.Pid} ({proc.ProcessName}.exe)");
                try
                {
                    Console.WriteLine($"      Tasks: {proc.Threads.Count}");
                    Console.WriteLine($"     Memory: {Size(proc.WorkingSet64)}");
                    Console.WriteLine($"        CPU: {(long)proc.TotalProcessorTime.TotalMilliseconds}ms");
                }
                catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException) { }
                if ((u.Type & SERVICE_WIN32_SHARE_PROCESS) != 0)
                {
                    var shared = Enumerate().Where(x => x.Pid == u.Pid && x.Name != key).Select(x => x.Id).ToList();
                    if (shared.Count > 0) Console.WriteLine($"     Shares: {string.Join(" ", shared)}");
                }
            }
            if (o.Lines > 0)
            {
                var lines = Journal.ForUnit(key, c.DisplayName, o.Lines);
                if (lines.Count > 0) Console.WriteLine();
                foreach (var line in lines) Console.WriteLine(line);
            }
            if (u.Active != "active") status = Math.Max(status, 3);
        }
        return status;
    }

    static int SystemStatus()
    {
        var units = Enumerate();
        var failed = units.Count(u => u.Failed);
        var boot = DateTime.Now - TimeSpan.FromMilliseconds(Environment.TickCount64);
        Console.WriteLine($"{Bullet} {Environment.MachineName}");
        Console.WriteLine($"    State: {(failed > 0 ? "degraded" : "running")}");
        Console.WriteLine($"    Units: {units.Count} loaded (incl. loaded aliases)");
        Console.WriteLine("     Jobs: 0 queued");
        Console.WriteLine($"   Failed: {failed} units");
        Console.WriteLine($"    Since: {boot:ddd yyyy-MM-dd HH:mm:ss}; {Ago(DateTime.Now - boot)} ago");
        return 0;
    }

    static string KeyName(string name)
    {
        // Canonical key name (the user may have typed a display name or another case).
        var match = Enumerate().FirstOrDefault(u => u.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                                                    || u.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase));
        return match?.Name ?? name;
    }

    static string StartTypeName(Config c) => c.StartType switch
    {
        0 => "boot",
        1 => "system",
        2 => c.Delayed ? "automatic (delayed)" : "automatic",
        3 => c.Triggered ? "manual (trigger)" : "manual",
        _ => "disabled",
    };

    static string Shorten(string text, bool full)
    {
        text = text.ReplaceLineEndings(" ").Trim();
        return full || text.Length <= 100 ? text : text[..97] + "...";
    }

    internal static string Size(long bytes) => bytes switch
    {
        >= 1L << 30 => $"{bytes / (double)(1L << 30):0.#}G",
        >= 1L << 20 => $"{bytes / (double)(1L << 20):0.#}M",
        >= 1L << 10 => $"{bytes / 1024.0:0.#}K",
        _ => $"{bytes}B",
    };

    internal static string Ago(TimeSpan t) =>
        t.TotalDays >= 1 ? $"{(int)t.TotalDays} day{(t.TotalDays >= 2 ? "s" : "")} {t.Hours}h" :
        t.TotalHours >= 1 ? $"{t.Hours}h {t.Minutes}min" :
        t.TotalMinutes >= 1 ? $"{t.Minutes}min {t.Seconds}s" : $"{Math.Max(0, t.Seconds)}s";

    static int Show(Options o)
    {
        var names = o.Args.Count > 0 ? o.Args : throw new UsageException("show: unit name required (the manager itself has no properties on Windows)");
        var status = 0;
        var first = true;
        foreach (var arg in names)
        {
            var name = ServiceName(arg);
            using var s = Open(name, SERVICE_QUERY_CONFIG | SERVICE_QUERY_STATUS);
            var props = new List<(string, string)>();
            if (s == null)
            {
                props.Add(("Id", name + ".service"));
                props.Add(("LoadState", "not-found"));
                props.Add(("ActiveState", "inactive"));
                props.Add(("SubState", "dead"));
            }
            else
            {
                var u = QueryUnit(s, KeyName(name));
                var c = QueryConfig(s);
                props.AddRange([
                    ("Id", u.Id), ("Names", u.Id), ("Description", c.DisplayName),
                    ("LoadState", "loaded"), ("ActiveState", u.Active), ("SubState", u.Sub),
                    ("UnitFileState", c.FileState), ("MainPID", u.Pid.ToString()),
                    ("ExecMainStatus", u.ExitCode.ToString()),
                    ("Type", (c.ServiceType & SERVICE_WIN32_SHARE_PROCESS) != 0 ? "shared" : "simple"),
                    ("ExecStart", c.BinaryPath), ("User", c.StartName ?? ""),
                    ("Requires", string.Join(" ", c.Dependencies.Where(d => !d.StartsWith('+')).Select(d => d + ".service"))),
                    ("Slice", c.Group ?? ""), ("Documentation", c.Description?.ReplaceLineEndings(" ") ?? ""),
                    ("CanStart", "yes"), ("CanStop", (u.ControlsAccepted & 1) != 0 ? "yes" : "no"),
                    ("CanReload", (u.ControlsAccepted & 8) != 0 ? "yes" : "no"),
                ]);
            }
            if (!first && o.Properties.Count == 0) Console.WriteLine();
            first = false;
            foreach (var (k, v) in props)
            {
                if (o.Properties.Count > 0 && !o.Properties.Contains(k, StringComparer.Ordinal)) continue;
                Console.WriteLine(o.Value ? v : $"{k}={v}");
            }
        }
        return status;
    }

    static int Cat(Options o)
    {
        if (o.Args.Count == 0) throw new UsageException("cat: unit name required");
        var status = 0;
        foreach (var arg in o.Args)
        {
            var name = ServiceName(arg);
            using var s = Open(name, SERVICE_QUERY_CONFIG);
            if (s == null) { Console.Error.WriteLine($"No files found for {name}.service."); status = 1; continue; }
            var c = QueryConfig(s);
            Console.WriteLine($"# {KeyName(name)}.service (synthesized from the Windows service configuration)");
            Console.WriteLine("[Unit]");
            Console.WriteLine($"Description={c.DisplayName}");
            if (!string.IsNullOrEmpty(c.Description)) Console.WriteLine($"Documentation={c.Description.ReplaceLineEndings(" ")}");
            var deps = c.Dependencies.Where(d => !d.StartsWith('+')).Select(d => d + ".service").ToList();
            if (deps.Count > 0)
            {
                Console.WriteLine($"Requires={string.Join(" ", deps)}");
                Console.WriteLine($"After={string.Join(" ", deps)}");
            }
            Console.WriteLine();
            Console.WriteLine("[Service]");
            Console.WriteLine($"ExecStart={c.BinaryPath}");
            if (!string.IsNullOrEmpty(c.StartName)) Console.WriteLine($"User={c.StartName}");
            Console.WriteLine();
            Console.WriteLine("[Install]");
            if (c.StartType == 2) Console.WriteLine("WantedBy=multi-user.target");
            Console.WriteLine();
        }
        return status;
    }

    static int Control(Options o)
    {
        if (o.Args.Count == 0) throw new UsageException($"{o.Verb}: unit name required");
        var status = 0;
        foreach (var arg in o.Args)
        {
            var name = ServiceName(arg);
            using var s = Open(name, SERVICE_QUERY_STATUS | SERVICE_START | SERVICE_STOP | SERVICE_PAUSE_CONTINUE);
            if (s == null)
            {
                Console.Error.WriteLine($"Failed to {o.Verb} {name}.service: Unit {name}.service not found.");
                status = 5;
                continue;
            }
            try
            {
                var u = QueryUnit(s, name);
                switch (o.Verb)
                {
                    case "start": Start(s); break;
                    case "stop": Stop(s); break;
                    case "restart": Stop(s); Start(s); break;
                    case "try-restart" or "condrestart": if (u.Active == "active") { Stop(s); Start(s); } break;
                    case "reload" or "reload-or-restart":
                        if ((u.ControlsAccepted & SERVICE_ACCEPT_PARAMCHANGE) != 0)
                        {
                            if (!ControlService(s.Handle, SERVICE_CONTROL_PARAMCHANGE, new byte[28]))
                                throw new System.ComponentModel.Win32Exception();
                        }
                        else if (o.Verb == "reload-or-restart") { Stop(s); Start(s); }
                        else throw new InvalidOperationException("Job type reload is not applicable for this unit.");
                        break;
                }
            }
            catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException or TimeoutException)
            {
                var message = e is System.ComponentModel.Win32Exception { NativeErrorCode: 5 }
                    ? "Access denied (run from an elevated prompt)" : e.Message;
                Console.Error.WriteLine($"Failed to {o.Verb} {name}.service: {message}");
                status = 1;
            }
        }
        return status;
    }

    static void Start(SafeService s)
    {
        if (QueryUnit(s, "").State == SERVICE_RUNNING) return;
        if (!StartServiceW(s.Handle, 0, IntPtr.Zero) && Marshal.GetLastWin32Error() is var e && e != ERROR_SERVICE_ALREADY_RUNNING)
            throw new System.ComponentModel.Win32Exception(e);
        WaitFor(s, SERVICE_RUNNING);
    }

    static void Stop(SafeService s)
    {
        if (QueryUnit(s, "").State == SERVICE_STOPPED) return;
        if (!ControlService(s.Handle, SERVICE_CONTROL_STOP, new byte[28]) && Marshal.GetLastWin32Error() is var e && e != ERROR_SERVICE_NOT_ACTIVE)
            throw new System.ComponentModel.Win32Exception(e);
        WaitFor(s, SERVICE_STOPPED);
    }

    // systemctl blocks until the job finishes; so do we (up to 90s, systemd's default timeout).
    static void WaitFor(SafeService s, uint state)
    {
        var deadline = Environment.TickCount64 + 90_000;
        while (true)
        {
            var u = QueryUnit(s, "");
            if (u.State == state) return;
            if (state == SERVICE_RUNNING && u.State == SERVICE_STOPPED)
                throw new InvalidOperationException($"the service exited with code {u.ExitCode}" +
                                                    (u.ExitCode == 1066 ? $" (service-specific {u.SpecificExitCode})" : ""));
            if (Environment.TickCount64 > deadline) throw new TimeoutException("Timed out waiting for the service");
            Thread.Sleep(250);
        }
    }

    static int IsActive(Options o)
    {
        if (o.Args.Count == 0) throw new UsageException($"{o.Verb}: unit name required");
        var all = true;
        foreach (var arg in o.Args)
        {
            using var s = Open(ServiceName(arg), SERVICE_QUERY_STATUS);
            var u = s == null ? null : QueryUnit(s, arg);
            var state = u?.Active ?? "inactive";
            if (!o.Quiet) Console.WriteLine(state);
            all &= o.Verb == "is-failed" ? state == "failed" : state == "active";
        }
        return all ? 0 : o.Verb == "is-failed" ? 1 : 3;
    }

    static int IsEnabled(Options o)
    {
        if (o.Args.Count == 0) throw new UsageException("is-enabled: unit name required");
        var all = true;
        foreach (var arg in o.Args)
        {
            using var s = Open(ServiceName(arg), SERVICE_QUERY_CONFIG);
            if (s == null)
            {
                Console.Error.WriteLine($"Failed to get unit file state for {ServiceName(arg)}.service: No such file or directory");
                all = false;
                continue;
            }
            var state = QueryConfig(s).FileState;
            if (!o.Quiet) Console.WriteLine(state);
            all &= state is "enabled" or "static" or "indirect";
        }
        return all ? 0 : 1;
    }

    static int SetStartType(Options o)
    {
        if (o.Args.Count == 0) throw new UsageException($"{o.Verb}: unit name required");
        var (startType, verb) = o.Verb switch
        {
            "enable" or "reenable" => (2u, "enable"),
            "disable" or "unmask" => (3u, o.Verb),
            _ => (4u, "mask"),
        };
        var status = 0;
        foreach (var arg in o.Args)
        {
            var name = ServiceName(arg);
            using var s = Open(name, SERVICE_QUERY_CONFIG | SERVICE_CHANGE_CONFIG | SERVICE_QUERY_STATUS | SERVICE_START | SERVICE_STOP);
            if (s == null) { Console.Error.WriteLine($"Failed to {verb} unit: Unit file {name}.service does not exist."); status = 1; continue; }
            if (verb == "unmask" && QueryConfig(s).StartType != 4) continue;
            if (!ChangeServiceConfigW(s.Handle, SERVICE_NO_CHANGE, startType, SERVICE_NO_CHANGE, null, null, IntPtr.Zero, null, null, null, null))
            {
                var e = Marshal.GetLastWin32Error();
                Console.Error.WriteLine($"Failed to {verb} unit: {(e == 5 ? "Access denied (run from an elevated prompt)" : new System.ComponentModel.Win32Exception(e).Message)}");
                status = 1;
                continue;
            }
            if (!o.Quiet)
                Console.WriteLine(verb switch
                {
                    "enable" => $"Set start type of {name}.service to Automatic.",
                    "mask" => $"Set start type of {name}.service to Disabled (masked).",
                    _ => $"Set start type of {name}.service to Manual.",
                });
            try
            {
                if (o.Now && verb == "enable") Start(s);
                if (o.Now && verb is "disable" or "mask") Stop(s);
            }
            catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException or TimeoutException)
            {
                Console.Error.WriteLine($"Failed to {(verb == "enable" ? "start" : "stop")} {name}.service: {e.Message}");
                status = 1;
            }
        }
        return status;
    }

    const uint SC_MANAGER_CONNECT = 1, SC_MANAGER_ENUMERATE_SERVICE = 4;
    internal const uint SERVICE_QUERY_CONFIG = 1, SERVICE_CHANGE_CONFIG = 2, SERVICE_QUERY_STATUS = 4,
        SERVICE_START = 0x10, SERVICE_STOP = 0x20, SERVICE_PAUSE_CONTINUE = 0x40;
    const uint SERVICE_WIN32 = 0x30, SERVICE_WIN32_SHARE_PROCESS = 0x20, SERVICE_STATE_ALL = 3;
    internal const uint SERVICE_STOPPED = 1, SERVICE_START_PENDING = 2, SERVICE_STOP_PENDING = 3, SERVICE_RUNNING = 4,
        SERVICE_CONTINUE_PENDING = 5, SERVICE_PAUSE_PENDING = 6, SERVICE_PAUSED = 7;
    const uint SERVICE_CONTROL_STOP = 1, SERVICE_CONTROL_PARAMCHANGE = 6, SERVICE_ACCEPT_PARAMCHANGE = 8;
    const uint SERVICE_CONFIG_DESCRIPTION = 1, SERVICE_CONFIG_DELAYED_AUTO_START_INFO = 3, SERVICE_CONFIG_TRIGGER_INFO = 8;
    const uint SERVICE_NO_CHANGE = 0xFFFFFFFF;
    const int ERROR_MORE_DATA = 234, ERROR_SERVICE_DOES_NOT_EXIST = 1060, ERROR_INVALID_NAME = 123,
        ERROR_SERVICE_ALREADY_RUNNING = 1056, ERROR_SERVICE_NOT_ACTIVE = 1062;

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr OpenSCManagerW(string machine, string database, uint access);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr OpenServiceW(IntPtr scm, string name, uint access);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool CloseServiceHandle(IntPtr handle);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool GetServiceKeyNameW(IntPtr scm, string displayName, StringBuilder keyName, ref uint size);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool EnumServicesStatusExW(IntPtr scm, int infoLevel, uint type, uint state, IntPtr buffer, uint size,
        out uint needed, out uint returned, ref uint resume, string group);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool QueryServiceStatusEx(IntPtr service, int infoLevel, byte[] buffer, uint size, out uint needed);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool QueryServiceConfigW(IntPtr service, IntPtr buffer, uint size, out uint needed);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool QueryServiceConfig2W(IntPtr service, uint level, IntPtr buffer, uint size, out uint needed);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool ChangeServiceConfigW(IntPtr service, uint type, uint startType, uint errorControl, string binaryPath,
        string group, IntPtr tagId, string dependencies, string startName, string password, string displayName);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool StartServiceW(IntPtr service, uint argc, IntPtr argv);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool ControlService(IntPtr service, uint control, byte[] status);

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    static extern int SHLoadIndirectString(string source, StringBuilder output, int size, IntPtr reserved);

    [DllImport("powrprof.dll", SetLastError = true)]
    static extern bool SetSuspendState(bool hibernate, bool force, bool disableWakeEvent);
}
