using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace WinTools;

// The rest of systemd's command-line tools: systemd-path (Windows known folders), hostnamectl
// (computer name, OS and firmware from the registry), timedatectl (time zone, W32Time) and
// loginctl (Remote Desktop Services sessions).
static class SystemdTools
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("systemd-path", "List and query Windows' well-known paths", Path_);
        Commands.Add("hostnamectl", "Show or set the computer name and OS details", Hostnamectl);
        Commands.Add("timedatectl", "Show or set the system time zone and clock settings", Timedatectl);
        Commands.Add("loginctl", "List and control Windows logon sessions", Loginctl);
    }

    // ---- systemd-path ----

    static List<(string Key, string Path)> KnownPaths()
    {
        string Env(string name) => Environment.GetEnvironmentVariable(name) ?? "";
        string Folder(Environment.SpecialFolder f) => Environment.GetFolderPath(f, Environment.SpecialFolderOption.DoNotVerify);
        var temp = Path.GetTempPath().TrimEnd('\\');
        var windows = Folder(Environment.SpecialFolder.Windows);
        var system = Folder(Environment.SpecialFolder.System);
        var programData = Folder(Environment.SpecialFolder.CommonApplicationData);
        var home = Folder(Environment.SpecialFolder.UserProfile);
        var local = Folder(Environment.SpecialFolder.LocalApplicationData);
        return
        [
            ("temporary", temp),
            ("temporary-large", temp),
            ("system-binaries", system),
            ("system-library-private", system),
            ("system-library-arch", system),
            ("system-shared", programData),
            ("system-configuration", programData),
            ("system-runtime", programData),
            ("system-runtime-logs", Path.Combine(windows, "Logs")),
            ("system-state-private", programData),
            ("system-state-logs", Path.Combine(windows, "Logs")),
            ("system-state-cache", Path.Combine(windows, "Temp")),
            ("system-state-spool", Path.Combine(system, "spool")),
            ("user-binaries", Path.Combine(home, ".local", "bin")),
            ("user-library-private", local),
            ("user-shared", local),
            ("user-configuration", Folder(Environment.SpecialFolder.ApplicationData)),
            ("user-runtime", temp),
            ("user-state-cache", local),
            ("user-state-private", local),
            ("user", home),
            ("user-documents", Folder(Environment.SpecialFolder.MyDocuments)),
            ("user-music", Folder(Environment.SpecialFolder.MyMusic)),
            ("user-pictures", Folder(Environment.SpecialFolder.MyPictures)),
            ("user-videos", Folder(Environment.SpecialFolder.MyVideos)),
            ("user-download", KnownFolder(new Guid("374DE290-123F-4565-9164-39C4925E467B")) ?? Path.Combine(home, "Downloads")),
            ("user-public", Env("PUBLIC")),
            ("user-templates", Folder(Environment.SpecialFolder.Templates)),
            ("user-desktop", Folder(Environment.SpecialFolder.DesktopDirectory)),
            ("search-binaries", Env("PATH")),
        ];
    }

    static int Path_(string[] args)
    {
        string suffix = null;
        var names = new List<string>();
        foreach (var a in args)
        {
            if (a is "-h" or "--help")
            {
                Console.WriteLine("Usage: systemd-path [--suffix=SUFFIX] [NAME...]\nShow Windows' well-known paths (all of them when no NAME is given).");
                return 0;
            }
            if (a == "--version") { Console.WriteLine($"systemd 256 (wintools {Commands.Version})"); return 0; }
            if (a.StartsWith("--suffix=")) suffix = a["--suffix=".Length..];
            else if (a.StartsWith('-')) throw new UsageException($"unrecognized option '{a}'");
            else names.Add(a);
        }
        var known = KnownPaths();
        string With(string p) => suffix == null ? p : Path.Combine(p, suffix);
        if (names.Count == 0)
        {
            foreach (var (k, p) in known) Console.WriteLine($"{k}: {With(p)}");
            return 0;
        }
        var status = 0;
        foreach (var n in names)
        {
            var hit = known.FirstOrDefault(k => k.Key == n);
            if (hit.Key == null) { Console.Error.WriteLine($"Failed to query {n}: No such device or address"); status = 1; }
            else Console.WriteLine(With(hit.Path));
        }
        return status;
    }

    static string KnownFolder(Guid id)
    {
        if (SHGetKnownFolderPath(id, 0, IntPtr.Zero, out var p) != 0) return null;
        try { return Marshal.PtrToStringUni(p); }
        finally { Marshal.FreeCoTaskMem(p); }
    }

    // ---- hostnamectl ----

    static int Hostnamectl(string[] args)
    {
        var verb = "status";
        var rest = new List<string>();
        foreach (var a in args)
        {
            switch (a)
            {
                case "-h" or "--help":
                    Console.WriteLine("Usage: hostnamectl [status|hostname [NAME]|set-hostname NAME|chassis|icon-name|machine-id]\nShow or set the computer name and OS details.");
                    return 0;
                case "--version": Console.WriteLine($"systemd 256 (wintools {Commands.Version})"); return 0;
                case "--static" or "--transient" or "--pretty" or "--no-ask-password": break;
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unrecognized option '{a}'");
                    if (rest.Count == 0 && verb == "status" && args[0] == a) verb = a; else rest.Add(a);
                    break;
            }
        }
        var info = HostInfo();
        switch (verb)
        {
            case "status":
                var w = info.Max(i => i.Key.Length);
                foreach (var (k, v) in info) Console.WriteLine($"{k.PadLeft(w)}: {v}");
                return 0;
            case "hostname" when rest.Count == 0:
                Console.WriteLine(System.Net.Dns.GetHostName());
                return 0;
            case "hostname" or "set-hostname":
                if (rest.Count != 1) throw new UsageException($"{verb}: exactly one host name expected");
                // ComputerNamePhysicalDnsHostname: also sets the NetBIOS name; takes effect after a restart.
                if (!SetComputerNameExW(5, rest[0])) throw new System.ComponentModel.Win32Exception();
                Console.WriteLine($"Host name set to {rest[0]} (takes effect after a restart).");
                return 0;
            case "chassis": Console.WriteLine(Chassis()); return 0;
            case "icon-name": Console.WriteLine("computer-" + Chassis()); return 0;
            case "machine-id": Console.WriteLine(MachineId()); return 0;
            default: throw new UsageException($"unknown command verb '{verb}'");
        }
    }

    static List<(string Key, string Value)> HostInfo()
    {
        using var nt = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        using var bios = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS");
        var build = int.TryParse(nt?.GetValue("CurrentBuildNumber") as string, out var b) ? b : 0;
        var product = nt?.GetValue("ProductName") as string ?? "Windows";
        // ProductName still says "Windows 10" on Windows 11.
        if (build >= 22000) product = product.Replace("Windows 10", "Windows 11");
        var display = nt?.GetValue("DisplayVersion") as string;
        var list = new List<(string, string)>
        {
            ("Static hostname", System.Net.Dns.GetHostName()),
            ("Icon name", "computer-" + Chassis()),
            ("Chassis", Chassis()),
            ("Machine ID", MachineId()),
            ("Operating System", display == null ? product : $"{product} {display}"),
            ("Kernel", $"Windows NT {Environment.OSVersion.Version.Major}.{Environment.OSVersion.Version.Minor}.{build}.{nt?.GetValue("UBR") ?? 0}"),
            ("Architecture", RuntimeInformation.OSArchitecture switch { Architecture.X64 => "x86-64", Architecture.Arm64 => "arm64", Architecture.X86 => "x86", var a => a.ToString().ToLowerInvariant() }),
        };
        void Add(string key, string name)
        {
            if (bios?.GetValue(name) is string v && v.Trim().Length > 0) list.Add((key, v.Trim()));
        }
        Add("Hardware Vendor", "SystemManufacturer");
        Add("Hardware Model", "SystemProductName");
        Add("Firmware Version", "BIOSVersion");
        Add("Firmware Date", "BIOSReleaseDate");
        return list;
    }

    static string MachineId()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
        return (key?.GetValue("MachineGuid") as string ?? "").Replace("-", "").ToLowerInvariant();
    }

    // Laptops have a system battery (BatteryFlag 128 = no battery).
    static string Chassis() => GetSystemPowerStatus(out var s) && s.BatteryFlag != 128 && s.BatteryFlag != 255 ? "laptop" : "desktop";

    // ---- timedatectl ----

    static int Timedatectl(string[] args)
    {
        var rest = args.Where(a => !a.StartsWith('-')).ToList();
        foreach (var a in args.Where(a => a.StartsWith('-')))
        {
            if (a is "-h" or "--help")
            {
                Console.WriteLine("Usage: timedatectl [status|show|list-timezones|set-timezone ZONE|set-time TIME|set-ntp BOOL|set-local-rtc BOOL]\nShow or set the system clock settings.");
                return 0;
            }
            if (a == "--version") { Console.WriteLine($"systemd 256 (wintools {Commands.Version})"); return 0; }
            if (a is not ("--no-pager" or "--no-ask-password" or "-a" or "--all")) throw new UsageException($"unrecognized option '{a}'");
        }
        var verb = rest.Count > 0 ? rest[0] : "status";
        string Arg() => rest.Count == 2 ? rest[1] : throw new UsageException($"{verb}: one argument expected");
        var tz = TimeZoneInfo.Local;
        switch (verb)
        {
            case "status":
            {
                var now = DateTimeOffset.Now;
                var ntp = NtpActive();
                Console.WriteLine($"               Local time: {now:ddd yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"           Universal time: {now.UtcDateTime:ddd yyyy-MM-dd HH:mm:ss} UTC");
                Console.WriteLine("                 RTC time: n/a");
                Console.WriteLine($"                Time zone: {ZoneId(tz)} ({(tz.IsDaylightSavingTime(now) ? tz.DaylightName : tz.StandardName)}, {Offset(now.Offset)})");
                Console.WriteLine($"System clock synchronized: {(ntp && LastSyncOk() ? "yes" : "no")}");
                Console.WriteLine($"              NTP service: {(ntp ? "active" : "inactive")}");
                Console.WriteLine($"          RTC in local TZ: {(RtcLocal() ? "yes" : "no")}");
                return 0;
            }
            case "show":
                Console.WriteLine($"Timezone={ZoneId(tz)}");
                Console.WriteLine($"LocalRTC={(RtcLocal() ? "yes" : "no")}");
                Console.WriteLine($"CanNTP=yes");
                Console.WriteLine($"NTP={(NtpActive() ? "yes" : "no")}");
                Console.WriteLine($"TimeUSec={DateTime.Now:ddd yyyy-MM-dd HH:mm:ss}");
                return 0;
            case "list-timezones":
                foreach (var z in TimeZoneInfo.GetSystemTimeZones().Select(ZoneId).Order(StringComparer.Ordinal)) Console.WriteLine(z);
                return 0;
            case "set-timezone":
            {
                var zone = Arg();
                var id = Icu(zone, toIana: false) ?? zone;
                if (!TimeZoneInfo.GetSystemTimeZones().Any(z => z.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
                    throw new UsageException($"invalid or unknown time zone '{zone}' (see timedatectl list-timezones)");
                return Exec("tzutil.exe", "/s", id);
            }
            case "set-time":
            {
                var t = Journal.ParseTime(Arg());
                var st = new SYSTEMTIME { Year = (ushort)t.Year, Month = (ushort)t.Month, Day = (ushort)t.Day, Hour = (ushort)t.Hour, Minute = (ushort)t.Minute, Second = (ushort)t.Second };
                if (!SetLocalTime(ref st)) throw new System.ComponentModel.Win32Exception();
                return 0;
            }
            case "set-ntp":
                // NTP on Windows is the W32Time service.
                return Bool(Arg())
                    ? Exec("sc.exe", "config", "w32time", "start=", "auto") is 0 ? Exec("net.exe", "start", "w32time") is 0 or 2 ? 0 : 1 : 1
                    : Exec("net.exe", "stop", "w32time") is var r && r is 0 or 2 ? Exec("sc.exe", "config", "w32time", "start=", "demand") : 1;
            case "set-local-rtc":
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\TimeZoneInformation", writable: true);
                key.SetValue("RealTimeIsUniversal", Bool(Arg()) ? 0 : 1, RegistryValueKind.DWord);
                return 0;
            }
            default: throw new UsageException($"unknown command verb '{verb}'");
        }
    }

    static bool Bool(string s) => s.ToLowerInvariant() switch
    {
        "1" or "yes" or "true" or "on" => true,
        "0" or "no" or "false" or "off" => false,
        _ => throw new UsageException($"failed to parse boolean '{s}'"),
    };

    static string ZoneId(TimeZoneInfo z) => Icu(z.Id, toIana: true) ?? z.Id;

    // Windows <-> IANA zone names through the ICU that ships with Windows 10 1903+ (wintools runs
    // with invariant globalization, so .NET's own conversion isn't available). Null when unknown.
    static string Icu(string zone, bool toIana)
    {
        try
        {
            var buffer = new char[128];
            var status = 0;
            var n = toIana
                ? ucal_getTimeZoneIDForWindowsID(zone, zone.Length, "001", buffer, buffer.Length, ref status)
                : ucal_getWindowsTimeZoneID(zone, zone.Length, buffer, buffer.Length, ref status);
            return status <= 0 && n > 0 ? new string(buffer, 0, n) : null;
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException) { return null; }
    }

    [DllImport("icu.dll", CharSet = CharSet.Unicode)]
    static extern int ucal_getTimeZoneIDForWindowsID(string windowsId, int length, [MarshalAs(UnmanagedType.LPStr)] string region,
        [Out] char[] id, int capacity, ref int status);

    [DllImport("icu.dll", CharSet = CharSet.Unicode)]
    static extern int ucal_getWindowsTimeZoneID(string id, int length, [Out] char[] windowsId, int capacity, ref int status);

    static string Offset(TimeSpan o) => $"{(o < TimeSpan.Zero ? "-" : "+")}{Math.Abs(o.Hours):00}{Math.Abs(o.Minutes):00}";

    static bool RtcLocal()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\TimeZoneInformation");
        return key?.GetValue("RealTimeIsUniversal") is not int v || v == 0;
    }

    static bool NtpActive()
    {
        using var s = Systemd.Open("W32Time", Systemd.SERVICE_QUERY_STATUS);
        return s != null && Systemd.QueryUnit(s, "W32Time").State == Systemd.SERVICE_RUNNING;
    }

    // W32Time records the result of its last sync in its time-provider state.
    static bool LastSyncOk()
    {
        try
        {
            var psi = new ProcessStartInfo("w32tm.exe", "/query /status") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi);
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            // "Leap Indicator: 3(not synchronized)" when it has never synced.
            return p.ExitCode == 0 && !output.Contains("3(", StringComparison.Ordinal);
        }
        catch (System.ComponentModel.Win32Exception) { return false; }
    }

    static int Exec(string file, params string[] args)
    {
        var psi = new ProcessStartInfo(file) { UseShellExecute = false };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi);
        p.WaitForExit();
        return p.ExitCode;
    }

    // ---- loginctl ----

    sealed record Session(uint Id, string Station, string State, string User, string Domain, string Client);

    static int Loginctl(string[] args)
    {
        var noLegend = args.Contains("--no-legend");
        var rest = new List<string>();
        foreach (var a in args)
        {
            if (a is "-h" or "--help")
            {
                Console.WriteLine("Usage: loginctl [list-sessions|list-users|show-session ID|session-status [ID]|lock-session [ID]|terminate-session ID...]\nList and control Windows logon sessions.");
                return 0;
            }
            if (a == "--version") { Console.WriteLine($"systemd 256 (wintools {Commands.Version})"); return 0; }
            if (a.StartsWith('-')) { if (a is not ("--no-legend" or "--no-pager")) throw new UsageException($"unrecognized option '{a}'"); }
            else rest.Add(a);
        }
        var verb = rest.Count > 0 ? rest[0] : "list-sessions";
        var sessions = Sessions();
        ProcessIdToSessionId((uint)Environment.ProcessId, out var current);
        Session Find(string id) =>
            sessions.FirstOrDefault(s => s.Id.ToString() == (id ?? current.ToString())) ?? throw new InvalidOperationException($"No session '{id}' known");
        switch (verb)
        {
            case "list-sessions":
            {
                var users = sessions.Where(s => s.User != "").ToList();
                if (!noLegend) Console.WriteLine("SESSION USER                 SEAT  TTY              STATE");
                foreach (var s in users)
                    Console.WriteLine($"{s.Id,7} {Qualified(s),-20} {(s.Station == "Console" ? "seat0" : ""),-5} {s.Station,-16} {s.State}");
                if (!noLegend) Console.WriteLine($"\n{users.Count} sessions listed.");
                return 0;
            }
            case "list-users":
            {
                var users = sessions.Where(s => s.User != "").GroupBy(Qualified).ToList();
                if (!noLegend) Console.WriteLine("USER                 STATE");
                foreach (var g in users) Console.WriteLine($"{g.Key,-20} {(g.Any(s => s.State == "active") ? "active" : "online")}");
                if (!noLegend) Console.WriteLine($"\n{users.Count} users listed.");
                return 0;
            }
            case "show-session":
            {
                var s = Find(rest.ElementAtOrDefault(1));
                Console.WriteLine($"Id={s.Id}\nName={s.User}\nTTY={s.Station}\nSeat={(s.Station == "Console" ? "seat0" : "")}\n" +
                                  $"Remote={(s.Station.StartsWith("RDP", StringComparison.OrdinalIgnoreCase) ? "yes" : "no")}\nRemoteHost={s.Client}\n" +
                                  $"State={s.State}\nActive={(s.State == "active" ? "yes" : "no")}\nType={(s.Station.StartsWith("RDP", StringComparison.OrdinalIgnoreCase) ? "rdp" : "console")}");
                return 0;
            }
            case "session-status":
            {
                var s = Find(rest.ElementAtOrDefault(1));
                Console.WriteLine($"{s.Id} - {Qualified(s)}");
                Console.WriteLine($"     TTY: {s.Station}");
                if (s.Client != "") Console.WriteLine($"  Remote: {s.Client}");
                Console.WriteLine($"   State: {s.State}");
                return 0;
            }
            case "lock-session" or "lock-sessions":
                var target = rest.ElementAtOrDefault(1);
                if (target != null && target != current.ToString())
                    throw new InvalidOperationException("Windows can only lock the caller's own session");
                return LockWorkStation() ? 0 : throw new System.ComponentModel.Win32Exception();
            case "terminate-session" or "kill-session":
                if (rest.Count < 2) throw new UsageException($"{verb}: session ID required");
                foreach (var id in rest.Skip(1))
                    if (!WTSLogoffSession(IntPtr.Zero, Find(id).Id, false)) throw new System.ComponentModel.Win32Exception();
                return 0;
            case "unlock-session" or "unlock-sessions":
                Console.Error.WriteLine("loginctl: Windows sessions can only be unlocked by signing in");
                return 1;
            default: throw new UsageException($"unknown command verb '{verb}'");
        }
    }

    static string Qualified(Session s) => s.Domain == "" || s.Domain.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) ? s.User : $"{s.Domain}\\{s.User}";

    static List<Session> Sessions()
    {
        if (!WTSEnumerateSessionsW(IntPtr.Zero, 0, 1, out var buffer, out var count)) throw new System.ComponentModel.Win32Exception();
        var list = new List<Session>();
        try
        {
            // WTS_SESSION_INFOW: DWORD SessionId, LPWSTR pWinStationName, WTS_CONNECTSTATE_CLASS State.
            var size = IntPtr.Size == 8 ? 24 : 12;
            for (var i = 0; i < count; i++)
            {
                var p = buffer + i * size;
                var id = (uint)Marshal.ReadInt32(p);
                var station = Marshal.PtrToStringUni(Marshal.ReadIntPtr(p + IntPtr.Size)) ?? "";
                var state = Marshal.ReadInt32(p + IntPtr.Size * 2) switch
                {
                    0 => "active", 1 => "online", 4 => "closing", 5 => "online", 6 => "listening", _ => "opening",
                };
                list.Add(new Session(id, station, state, Info(id, 5), Info(id, 7), Info(id, 10)));
            }
        }
        finally { WTSFreeMemory(buffer); }
        return list;
    }

    static string Info(uint session, int infoClass)
    {
        if (!WTSQuerySessionInformationW(IntPtr.Zero, session, infoClass, out var p, out _)) return "";
        try { return Marshal.PtrToStringUni(p) ?? ""; }
        finally { WTSFreeMemory(p); }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SYSTEMTIME { public ushort Year, Month, DayOfWeek, Day, Hour, Minute, Second, Milliseconds; }

    [StructLayout(LayoutKind.Sequential)]
    struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus, BatteryFlag, BatteryLifePercent, SystemStatusFlag;
        public int BatteryLifeTime, BatteryFullLifeTime;
    }

    [DllImport("shell32.dll")]
    static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid id, uint flags, IntPtr token, out IntPtr path);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool SetComputerNameExW(int type, string name);

    [DllImport("kernel32.dll")]
    static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS status);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool SetLocalTime(ref SYSTEMTIME time);

    [DllImport("kernel32.dll")]
    static extern bool ProcessIdToSessionId(uint pid, out uint session);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool LockWorkStation();

    [DllImport("wtsapi32.dll", SetLastError = true)]
    static extern bool WTSEnumerateSessionsW(IntPtr server, uint reserved, uint version, out IntPtr info, out int count);

    [DllImport("wtsapi32.dll", SetLastError = true)]
    static extern bool WTSQuerySessionInformationW(IntPtr server, uint session, int infoClass, out IntPtr buffer, out uint bytes);

    [DllImport("wtsapi32.dll", SetLastError = true)]
    static extern bool WTSLogoffSession(IntPtr server, uint session, bool wait);

    [DllImport("wtsapi32.dll")]
    static extern void WTSFreeMemory(IntPtr memory);
}
