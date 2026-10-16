using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WinTools;

// sleepwatcher / Zzz: run commands on sleep, wake, display dim/sleep/wake, idle and power plug events,
// and put the PC to sleep. Events come from PowerRegisterSuspendResumeNotification and
// PowerSettingRegisterNotification (GUID_CONSOLE_DISPLAY_STATE, GUID_ACDC_POWER_SOURCE) callbacks, idle
// time from GetLastInputInfo. Zzz uses SetSuspendState on S3 machines; Modern Standby (S0) machines
// sleep by turning the display off, as their power button does.
static unsafe class Sleepwatcher
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("sleepwatcher", "Run commands when the PC sleeps, wakes or goes idle", Run);
        Commands.Add("Zzz", "Put the PC to sleep", RunZzz);
    }

    // ---- Zzz -------------------------------------------------------------------------------------

    static int RunZzz(string[] args)
    {
        var dryRun = false;
        foreach (var arg in args)
        {
            switch (arg)
            {
                case "-h" or "--help":
                    Console.WriteLine("Usage: Zzz [-n]\nPuts the PC to sleep. -n prints how it would sleep without sleeping.");
                    return 0;
                case "-n" or "--dry-run": dryRun = true; break;
                default: throw new UsageException($"unknown option {arg}");
            }
        }
        var caps = new byte[256];
        fixed (byte* c = caps)
            if (!GetPwrCapabilities(c)) throw new InvalidOperationException("could not read the power capabilities");
        bool s1 = caps[3] != 0, s2 = caps[4] != 0, s3 = caps[5] != 0, modernStandby = caps[20] != 0;
        if (s1 || s2 || s3)
        {
            if (dryRun) { Console.WriteLine($"sleep: SetSuspendState ({(s3 ? "S3" : s2 ? "S2" : "S1")})"); return 0; }
            EnableShutdownPrivilege();
            if (!SetSuspendState(false, false, false)) throw new System.ComponentModel.Win32Exception();
            return 0;
        }
        if (modernStandby)
        {
            if (dryRun) { Console.WriteLine("sleep: Modern Standby (display off)"); return 0; }
            PostMessageW(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, 2);
            return 0;
        }
        throw new InvalidOperationException("this PC doesn't support sleep (no S1-S3 or Modern Standby; check the power settings or drivers)");
    }

    static void EnableShutdownPrivilege()
    {
        if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out var token)) return;
        try
        {
            if (!LookupPrivilegeValueW(null, "SeShutdownPrivilege", out var luid)) return;
            var privileges = new TOKEN_PRIVILEGES { PrivilegeCount = 1, Luid = luid, Attributes = SE_PRIVILEGE_ENABLED };
            AdjustTokenPrivileges(token, false, &privileges, 0, IntPtr.Zero, IntPtr.Zero);
        }
        finally { CloseHandle(token); }
    }

    // ---- sleepwatcher ----------------------------------------------------------------------------

    static readonly Dictionary<char, string> commands = [];
    static bool verbose;
    static int displayState = -1;   // 0 off, 1 on, 2 dimmed; -1 until the first (current-state) notification
    static bool asleep;
    static int powerSource = -1;

    static readonly Dictionary<string, char> LongOptions = new()
    {
        ["--sleep"] = 's', ["--wakeup"] = 'w', ["--cantsleep"] = 'c', ["--displaydim"] = 'D', ["--displayundim"] = 'E',
        ["--displaysleep"] = 'S', ["--displaywakeup"] = 'W', ["--idle"] = 'i', ["--idleresume"] = 'R', ["--plug"] = 'a',
        ["--unplug"] = 'u', ["--timeout"] = 't', ["--break"] = 'b', ["--pidfile"] = 'p', ["--config"] = 'f',
    };

    static int Run(string[] args)
    {
        var options = new List<(char Option, string Value)>();
        var daemon = false;
        var queue = new Queue<string>(args);
        while (queue.Count > 0)
        {
            var arg = queue.Dequeue();
            switch (arg)
            {
                case "-h" or "--help": Usage(); return 0;
                case "-v" or "--version": Console.WriteLine($"sleepwatcher {Commands.Version}"); Usage(); return 2;
                case "-V" or "--verbose": verbose = true; continue;
                case "-d" or "--daemon": daemon = true; continue;
                case "-n" or "--notifyoff": continue;
                case "-g" or "--getidletime": Console.WriteLine(IdleTenths()); return 0;
            }
            char option;
            string value = null;
            if (arg.StartsWith("--"))
            {
                var eq = arg.IndexOf('=');
                var name = eq < 0 ? arg : arg[..eq];
                if (!LongOptions.TryGetValue(name, out option)) throw new UsageException($"unknown option {arg}");
                if (eq >= 0) value = arg[(eq + 1)..];
            }
            else if (arg.Length >= 2 && arg[0] == '-' && "swcDESWiRautbpf".Contains(arg[1]))
            {
                option = arg[1];
                if (arg.Length > 2) value = arg[2..];
            }
            else throw new UsageException($"unknown option {arg}");
            value ??= queue.Count > 0 ? queue.Dequeue() : throw new UsageException($"option {arg} needs a value");
            if (option == 'f')
            {
                // A config file holds more options, one per line ("-s ~/.sleep", "--wakeup=...").
                foreach (var line in File.ReadAllLines(value).Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith('#')))
                {
                    var space = line.IndexOf(' ');
                    if (space < 0) queue.Enqueue(line);
                    else { queue.Enqueue(line[..space]); queue.Enqueue(line[(space + 1)..].Trim().Trim('"')); }
                }
                continue;
            }
            options.Add((option, value));
        }
        int timeout = 0, breakTenths = 0;
        string pidfile = null;
        foreach (var (option, value) in options)
        {
            switch (option)
            {
                case 't': timeout = int.TryParse(value, out var t) && t > 0 ? t : throw new UsageException("-t needs tenths of a second"); break;
                case 'b': breakTenths = int.TryParse(value, out var b) && b > 0 ? b : throw new UsageException("-b needs tenths of a second"); break;
                case 'p': pidfile = value; break;
                default: commands[option] = Expand(value); break;
            }
        }
        if (commands.ContainsKey('i') && timeout == 0) throw new UsageException("-i needs -t (idle timeout in tenths of a second)");
        if (commands.Count == 0) { Usage(); return 2; }
        if (daemon) return Detach(args);

        if (pidfile != null) File.WriteAllText(pidfile, Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
        try { return Watch(timeout, breakTenths); }
        finally { if (pidfile != null) try { File.Delete(pidfile); } catch (IOException) { } }
    }

    static void Usage() => Console.WriteLine("""
        usage: sleepwatcher [-n] [-v] [-V] [-d] [-g] [-f configfile] [-p pidfile] [-s sleepcommand]
                            [-w wakeupcommand] [-c cantsleepcommand] [-D displaydimcommand]
                            [-E displayundimcommand] [-S displaysleepcommand] [-W displaywakeupcommand]
                            [-t timeout -i idlecommand [-R idleresumecommand] [-b break]]
                            [-a plugcommand] [-u unplugcommand]
          -d  run in the background             -V  verbose (log events)
          -g  print the idle time in 1/10 s     -p  write the process id to a file
          -t  idle timeout in 1/10 s            -b  repeat the idle command every break 1/10 s while idle
          -a/-u  run when switching to AC power / battery
        Commands run with cmd /c; ~ is your profile folder. -c is accepted, but Windows apps can't refuse sleep,
        so it never runs.
        """);

    static string Expand(string command) =>
        command.StartsWith('~') ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + command[1..] : command;

    static long IdleTenths()
    {
        var info = new LASTINPUTINFO { cbSize = (uint)sizeof(LASTINPUTINFO) };
        if (!GetLastInputInfo(&info)) return 0;
        return (uint)(Environment.TickCount - (int)info.dwTime) / 100;
    }

    static int Detach(string[] args)
    {
        var exe = Environment.ProcessPath ?? throw new InvalidOperationException("cannot find own executable");
        var info = new ProcessStartInfo(exe) { UseShellExecute = false, CreateNoWindow = true };
        if (Path.GetFileNameWithoutExtension(exe).Equals("wintools", StringComparison.OrdinalIgnoreCase)) info.ArgumentList.Add("sleepwatcher");
        foreach (var a in args.Where(a => a is not ("-d" or "--daemon"))) info.ArgumentList.Add(a);
        var child = Process.Start(info) ?? throw new InvalidOperationException("could not start the background process");
        if (verbose) Console.WriteLine($"sleepwatcher: running in the background (pid {child.Id})");
        return 0;
    }

    static int Watch(int timeout, int breakTenths)
    {
        var parameters = new DEVICE_NOTIFY_SUBSCRIBE_PARAMETERS { Callback = &PowerCallback };
        var settingHandles = new List<nint>();
        if (PowerRegisterSuspendResumeNotification(DEVICE_NOTIFY_CALLBACK, &parameters, out var suspendHandle) != 0)
        {
            suspendHandle = 0;
            Console.Error.WriteLine("sleepwatcher: could not register for sleep/wake notifications");
        }
        foreach (var guid in new[] { GUID_CONSOLE_DISPLAY_STATE, GUID_ACDC_POWER_SOURCE })
        {
            var g = guid;
            if (PowerSettingRegisterNotification(&g, DEVICE_NOTIFY_CALLBACK, &parameters, out var handle) == 0) settingHandles.Add(handle);
        }
        using var stop = new ManualResetEventSlim();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Set(); };
        if (verbose) Console.WriteLine("sleepwatcher: watching (Ctrl+C to stop)");
        var idle = false;
        long lastIdleRun = 0;
        while (!stop.Wait(500))
        {
            if (timeout == 0) continue;
            var tenths = IdleTenths();
            if (!idle && tenths >= timeout)
            {
                idle = true;
                lastIdleRun = tenths;
                Fire('i', "idle");
            }
            else if (idle && breakTenths > 0 && tenths - lastIdleRun >= breakTenths)
            {
                lastIdleRun = tenths;
                Fire('i', "idle (again)");
            }
            else if (idle && tenths < timeout)
            {
                idle = false;
                Fire('R', "idle resume");
            }
        }
        if (suspendHandle != 0) PowerUnregisterSuspendResumeNotification(suspendHandle);
        foreach (var h in settingHandles) PowerSettingUnregisterNotification(h);
        return 0;
    }

    [UnmanagedCallersOnly]
    static uint PowerCallback(nint context, uint type, nint setting)
    {
        try
        {
            switch (type)
            {
                case PBT_APMSUSPEND:
                    asleep = true;
                    Fire('s', "sleep", wait: true);   // Windows gives the system a moment before suspending
                    break;
                case PBT_APMRESUMEAUTOMATIC or PBT_APMRESUMESUSPEND:
                    if (asleep) { asleep = false; Fire('w', "wakeup"); }
                    break;
                case PBT_POWERSETTINGCHANGE:
                    var s = (POWERBROADCAST_SETTING*)setting;
                    var value = s->DataLength >= 4 ? *(int*)&s->Data : 0;
                    if (s->PowerSetting == GUID_CONSOLE_DISPLAY_STATE) DisplayChanged(value);
                    else if (s->PowerSetting == GUID_ACDC_POWER_SOURCE)
                    {
                        // The first notification just reports the current source.
                        if (powerSource >= 0 && value != powerSource) Fire(value == 0 ? 'a' : 'u', value == 0 ? "plug" : "unplug");
                        powerSource = value;
                    }
                    break;
            }
        }
        catch (Exception) { }   // never let an exception reach the power manager
        return 0;
    }

    static void DisplayChanged(int state)
    {
        var old = displayState;
        displayState = state;
        if (old == state || old < 0) return;
        switch (state)
        {
            case 2: Fire('D', "display dim"); break;
            case 0: Fire('S', "display sleep"); break;
            case 1:
                if (old == 2) Fire('E', "display undim");
                else Fire('W', "display wakeup");
                break;
        }
    }

    static void Fire(char option, string what, bool wait = false)
    {
        if (verbose) Console.WriteLine($"sleepwatcher: {what}{(commands.ContainsKey(option) ? "" : " (no command)")}");
        if (!commands.TryGetValue(option, out var command)) return;
        // Foreground: commands share our console. Background (-d, no console): no window pops up.
        var info = new ProcessStartInfo(Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe") { UseShellExecute = false, CreateNoWindow = GetConsoleWindow() == IntPtr.Zero };
        info.ArgumentList.Add("/c");
        info.ArgumentList.Add(command);
        try
        {
            using var p = Process.Start(info);
            if (wait) p?.WaitForExit(30_000);
        }
        catch (System.ComponentModel.Win32Exception e) { Console.Error.WriteLine($"sleepwatcher: {command}: {e.Message}"); }
    }

    // ---- Win32 -----------------------------------------------------------------------------------

    static readonly Guid GUID_CONSOLE_DISPLAY_STATE = new("6FE69556-704A-47A0-8F24-C28D936FDA47");
    static readonly Guid GUID_ACDC_POWER_SOURCE = new("5D3E9A59-E9D5-4B00-A6BD-FF34FF516548");
    const uint DEVICE_NOTIFY_CALLBACK = 2, PBT_APMSUSPEND = 4, PBT_APMRESUMESUSPEND = 7, PBT_APMRESUMEAUTOMATIC = 0x12, PBT_POWERSETTINGCHANGE = 0x8013;
    const uint TOKEN_ADJUST_PRIVILEGES = 0x20, TOKEN_QUERY = 0x8, SE_PRIVILEGE_ENABLED = 0x2, WM_SYSCOMMAND = 0x112;
    const nuint SC_MONITORPOWER = 0xF170;
    static readonly IntPtr HWND_BROADCAST = 0xffff;

    [StructLayout(LayoutKind.Sequential)]
    struct DEVICE_NOTIFY_SUBSCRIBE_PARAMETERS { public delegate* unmanaged<nint, uint, nint, uint> Callback; public nint Context; }

    [StructLayout(LayoutKind.Sequential)]
    struct POWERBROADCAST_SETTING { public Guid PowerSetting; public uint DataLength; public byte Data; }

    [StructLayout(LayoutKind.Sequential)]
    struct LASTINPUTINFO { public uint cbSize, dwTime; }

    [StructLayout(LayoutKind.Sequential)]
    struct TOKEN_PRIVILEGES { public uint PrivilegeCount; public long Luid; public uint Attributes; }

    [DllImport("powrprof.dll")] static extern uint PowerRegisterSuspendResumeNotification(uint flags, DEVICE_NOTIFY_SUBSCRIBE_PARAMETERS* recipient, out nint handle);
    [DllImport("powrprof.dll")] static extern uint PowerUnregisterSuspendResumeNotification(nint handle);
    [DllImport("powrprof.dll")] static extern uint PowerSettingRegisterNotification(Guid* setting, uint flags, DEVICE_NOTIFY_SUBSCRIBE_PARAMETERS* recipient, out nint handle);
    [DllImport("powrprof.dll")] static extern uint PowerSettingUnregisterNotification(nint handle);
    [DllImport("powrprof.dll")] static extern bool GetPwrCapabilities(byte* capabilities);
    [DllImport("powrprof.dll")] static extern bool SetSuspendState(bool hibernate, bool force, bool disableWakeEvent);
    [DllImport("user32.dll")] static extern bool GetLastInputInfo(LASTINPUTINFO* info);
    [DllImport("user32.dll")] static extern bool PostMessageW(IntPtr hwnd, uint msg, nuint wParam, nint lParam);
    [DllImport("advapi32.dll")] static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode)] static extern bool LookupPrivilegeValueW(string system, string name, out long luid);
    [DllImport("advapi32.dll")] static extern bool AdjustTokenPrivileges(IntPtr token, bool disableAll, TOKEN_PRIVILEGES* state, uint length, IntPtr previous, IntPtr returned);
    [DllImport("kernel32.dll")] static extern IntPtr GetCurrentProcess();
    [DllImport("kernel32.dll")] static extern IntPtr GetConsoleWindow();
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
}
