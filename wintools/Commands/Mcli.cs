using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// m-cli: "Swiss Army Knife" of small system commands, dispatched as `m <command> [subcommand] [args]`.
// Each command maps to the Windows way of doing the same thing. Query forms (status, list, get) are
// read-only; the few that change settings shell out to the stock tool (netsh, powercfg, rundll32) and
// are clearly the user's intent, matching the macOS original. `m <command>` with no subcommand prints
// that command's usage, like m-cli.
static class Mcli
{
    const string Version = "2.0.9";

    [ModuleInitializer]
    internal static void Register() => Commands.Add("m", "Swiss Army Knife of system commands (network, disk, display, battery, ...)", Run);

    static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help") { Help(); return 0; }
        if (args[0] is "-v" or "--version" or "version") { Console.WriteLine($"m-cli version: v{Version}"); return 0; }
        var cmd = args[0];
        var sub = args.Length > 1 ? args[1] : null;
        var rest = args.Length > 2 ? args[2..] : [];
        return cmd switch
        {
            "info" => Info(),
            "battery" => Battery(),
            "disk" => Disk(sub),
            "network" => Network(sub),
            "wifi" => Wifi(sub, rest),
            "bluetooth" => Bluetooth(sub),
            "dns" => Dns(sub, rest),
            "firewall" => Firewall(sub),
            "hostname" => Hostname(sub, rest),
            "display" => Display(),
            "volume" => Volume(sub),
            "user" => User(sub),
            "dir" => Dir(sub, rest),
            "ntp" => Ntp(),
            "lock" => Lock(),
            "nosleep" => NoSleep(rest),
            "trash" => TrashShim(rest),
            "service" => Service(sub, rest),
            "update" => Update(),
            _ => throw new UsageException($"'{cmd}' is not an m command (see m --help)"),
        };
    }

    // Shell out to a stock read-only tool and return its stdout.
    static string Capture(string file, params string[] args)
    {
        try
        {
            var psi = new ProcessStartInfo(file) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            foreach (var a in args) psi.ArgumentList.Add(a);
            using var p = Process.Start(psi);
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return output;
        }
        catch (Win32Exception e) { throw new InvalidOperationException($"could not run {file}: {e.Message}"); }
    }

    // Run a stock command that changes a setting (the action commands only).
    static int RunTool(string file, params string[] args)
    {
        var psi = new ProcessStartInfo(file) { UseShellExecute = false };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi);
        p.WaitForExit();
        return p.ExitCode;
    }

    static int Info()
    {
        var mem = new PerfSampler(false).Take().Mem;
        Console.WriteLine($"Computer Name:   {Environment.MachineName}");
        Console.WriteLine($"User:            {Environment.UserName}");
        Console.WriteLine($"OS:              {PerfSampler.OsName} ({PerfSampler.OsVersion}), {PerfSampler.Arch}");
        Console.WriteLine($"Processor:       {PerfSampler.CpuName} ({Environment.ProcessorCount} logical)");
        Console.WriteLine($"Memory:          {mem.Total / 1073741824.0:F1} GB");
        var up = TimeSpan.FromMilliseconds(Environment.TickCount64);
        Console.WriteLine($"Uptime:          {(int)up.TotalDays}d {up.Hours}h {up.Minutes}m");
        return 0;
    }

    static int Battery()
    {
        var b = PerfSampler.Battery();
        if (!b.HasBattery) { Console.WriteLine("No battery (desktop or VM)."); return 0; }
        Console.WriteLine($"Charge:   {b.Percent}%");
        Console.WriteLine($"Power:    {(b.Charging ? "charging" : b.OnAc ? "on AC (not charging)" : "on battery")}");
        return 0;
    }

    static int Disk(string sub)
    {
        if (sub is "list" or null)
        {
            Console.WriteLine($"{"Drive",-6}{"Type",-10}{"Format",-8}{"Size",12}{"Free",12}  Label");
            foreach (var d in DriveInfo.GetDrives())
            {
                try
                {
                    if (!d.IsReady) { Console.WriteLine($"{d.Name,-6}{d.DriveType,-10}{"-",-8}{"",12}{"",12}  (not ready)"); continue; }
                    Console.WriteLine($"{d.Name,-6}{d.DriveType,-10}{d.DriveFormat,-8}{Human(d.TotalSize),12}{Human(d.AvailableFreeSpace),12}  {d.VolumeLabel}");
                }
                catch (IOException) { }
            }
            return 0;
        }
        throw new UsageException("usage: m disk list");
    }

    static int Network(string sub)
    {
        switch (sub)
        {
            case "ls" or "list" or null:
                Console.Write(Capture("netsh", "interface", "show", "interface"));
                return 0;
            case "ip":
                foreach (var line in Capture("ipconfig").Split('\n').Where(l => l.Contains("IPv4") || l.TrimEnd().EndsWith("adapter" ) || l.Contains("adapter ")))
                    Console.WriteLine(line.TrimEnd());
                return 0;
            case "status":
                Console.Write(Capture("netsh", "interface", "ipv4", "show", "interfaces"));
                return 0;
            default: throw new UsageException("usage: m network <ls|ip|status>");
        }
    }

    static int Wifi(string sub, string[] rest)
    {
        switch (sub)
        {
            case "status" or null: Console.Write(Capture("netsh", "wlan", "show", "interfaces")); return 0;
            case "scan" or "list": Console.Write(Capture("netsh", "wlan", "show", "networks")); return 0;
            case "on": return RunTool("netsh", "interface", "set", "interface", "Wi-Fi", "enable");
            case "off": return RunTool("netsh", "interface", "set", "interface", "Wi-Fi", "disable");
            default: throw new UsageException("usage: m wifi <status|scan|on|off>");
        }
    }

    static int Bluetooth(string sub)
    {
        // Windows has no stock bluetooth-radio CLI; report adapter presence read-only via PnP.
        if (sub is not (null or "status")) throw new InvalidOperationException("only `m bluetooth status` is available on Windows (toggling needs the Settings app or a radio API)");
        var output = Capture("powershell", "-NoProfile", "-Command", "Get-PnpDevice -Class Bluetooth -Status OK | Select-Object -ExpandProperty FriendlyName");
        Console.WriteLine(string.IsNullOrWhiteSpace(output) ? "No Bluetooth adapter found." : output.Trim());
        return 0;
    }

    static int Dns(string sub, string[] rest)
    {
        if (sub is "get" or "list" or null) { Console.Write(Capture("netsh", "interface", "ip", "show", "dns")); return 0; }
        throw new InvalidOperationException("setting DNS changes network state; run: netsh interface ip set dns ...");
    }

    static int Firewall(string sub)
    {
        if (sub is "status" or null) { Console.Write(Capture("netsh", "advfirewall", "show", "allprofiles", "state")); return 0; }
        throw new InvalidOperationException("changing the firewall changes system state; run: netsh advfirewall set ...");
    }

    static int Hostname(string sub, string[] rest)
    {
        if (sub is null or "get") { Console.WriteLine(Environment.MachineName); return 0; }
        throw new InvalidOperationException("renaming the computer changes system state; run: Rename-Computer");
    }

    static int Display()
    {
        var count = GetSystemMetrics(80);   // SM_CMONITORS
        Console.WriteLine($"Monitors: {count}");
        Console.WriteLine($"Virtual desktop: {GetSystemMetrics(78)} x {GetSystemMetrics(79)} px");   // SM_CXVIRTUALSCREEN / CY
        Console.WriteLine($"Primary: {GetSystemMetrics(0)} x {GetSystemMetrics(1)} px");
        return 0;
    }

    static int Volume(string sub)
    {
        if (sub is null or "get")
        {
            var vol = Audio.GetVolume();
            Console.WriteLine(double.IsNaN(vol) ? "No active audio output device." : $"Output volume: {vol * 100:F0}%{(Audio.GetMute() ? " (muted)" : "")}");
            return 0;
        }
        if (int.TryParse(sub, out var pct)) { Audio.SetVolume(Math.Clamp(pct, 0, 100) / 100f); Console.WriteLine($"Output volume set to {pct}%"); return 0; }
        if (sub == "mute") { Audio.SetMute(true); return 0; }
        if (sub == "unmute") { Audio.SetMute(false); return 0; }
        throw new UsageException("usage: m volume <get|0-100|mute|unmute>");
    }

    static int User(string sub)
    {
        if (sub is null or "whoami" or "info") { Console.Write(Capture("whoami", "/user", "/groups")); return 0; }
        if (sub == "list") { Console.Write(Capture("net", "user")); return 0; }
        throw new UsageException("usage: m user <info|list>");
    }

    static int Dir(string sub, string[] rest)
    {
        switch (sub)
        {
            case "size":
                var target = rest.FirstOrDefault() ?? ".";
                long bytes = 0;
                foreach (var f in Directory.EnumerateFiles(target, "*", SearchOption.AllDirectories))
                    try { bytes += new FileInfo(f).Length; } catch (IOException) { }
                Console.WriteLine($"{Human(bytes)}  {Path.GetFullPath(target)}");
                return 0;
            case "ls" or null:
                foreach (var e in Directory.EnumerateFileSystemEntries(rest.FirstOrDefault() ?? "."))
                    Console.WriteLine(Path.GetFileName(e));
                return 0;
            default: throw new UsageException("usage: m dir <ls|size> [path]");
        }
    }

    static int Ntp() { Console.Write(Capture("w32tm", "/query", "/status")); return 0; }

    static int Lock() { LockWorkStation(); return 0; }

    static int NoSleep(string[] rest)
    {
        Console.WriteLine("Keeping the system awake (ES_CONTINUOUS | ES_SYSTEM_REQUIRED). Press Enter to stop.");
        SetThreadExecutionState(0x80000000 | 0x00000001);
        Console.ReadLine();
        SetThreadExecutionState(0x80000000);
        return 0;
    }

    static int TrashShim(string[] rest) => Commands.Run("trash", rest);

    static int Service(string sub, string[] rest)
    {
        if (sub is "list" or null) { Console.Write(Capture("sc", "query", "type=", "service", "state=", "all")); return 0; }
        throw new InvalidOperationException("starting or stopping services changes system state; run: sc start/stop <name>");
    }

    static int Update() { Console.WriteLine("m-cli was installed by brew; run: brew update && brew upgrade m-cli"); return 0; }

    static string Human(long bytes)
    {
        string[] u = ["B", "KB", "MB", "GB", "TB"];
        double v = bytes;
        var i = 0;
        while (v >= 1024 && i < u.Length - 1) { v /= 1024; i++; }
        return i == 0 ? $"{v:0} B" : $"{v:0.0} {u[i]}";
    }

    static void Help() => Console.WriteLine($"""
        m-cli v{Version} - Swiss Army Knife for macOS, ported to Windows

        Usage: m <command> [subcommand] [arguments]

        Commands:
          battery              Battery charge and power source
          bluetooth status     Bluetooth adapter presence
          dir ls|size          Directory listing or total size
          disk list            Drives, size and free space
          display              Monitor count and resolution
          dns get              Configured DNS servers
          firewall status      Windows Firewall profile state
          hostname get         Computer name
          info                 System overview
          lock                 Lock the workstation
          network ls|ip|status Network interfaces
          nosleep              Keep the system awake until Enter
          ntp                  Time service status
          service list         Windows services
          trash <path>...      Move files to the Recycle Bin
          update               How to update m-cli
          user info|list       Current user / accounts
          volume get|0-100     Output volume
          wifi status|scan|on|off  Wi-Fi state and networks

        Query forms are read-only. Commands that change macOS-only settings report the
        Windows equivalent instead of guessing.
        """);

    [DllImport("user32.dll")] static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] static extern bool LockWorkStation();
    [DllImport("kernel32.dll")] static extern uint SetThreadExecutionState(uint flags);

    // Minimal Core Audio access for `m volume`, through raw COM vtable calls so it works under
    // NativeAOT (built-in COM interop is off): CoCreateInstance(MMDeviceEnumerator) ->
    // GetDefaultAudioEndpoint -> Activate(IAudioEndpointVolume), each method invoked by vtable slot.
    static unsafe class Audio
    {
        static readonly Guid CLSID_MMDeviceEnumerator = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
        static readonly Guid IID_IMMDeviceEnumerator = new("A95664D2-9614-4F35-A746-DE8DB63617E6");
        static readonly Guid IID_IAudioEndpointVolume = new("5CDF2C82-841E-4546-9722-0CF74078229A");

        public static float GetVolume()
        {
            var ep = Endpoint();
            if (ep == IntPtr.Zero) return float.NaN;
            try { return Call(ep, 9, out float level) == 0 ? level : float.NaN; }   // GetMasterVolumeLevelScalar
            finally { Release(ep); }
        }

        public static bool GetMute()
        {
            var ep = Endpoint();
            if (ep == IntPtr.Zero) return false;
            try { return Call(ep, 15, out int mute) == 0 && mute != 0; }   // GetMute
            finally { Release(ep); }
        }

        public static void SetVolume(float scalar)
        {
            var ep = Required();
            try { Check(SetScalar(ep, 7, scalar)); }   // SetMasterVolumeLevelScalar(level, &ctx)
            finally { Release(ep); }
        }

        public static void SetMute(bool mute)
        {
            var ep = Required();
            try
            {
                var ctx = Guid.Empty;
                Check(((delegate* unmanaged<IntPtr, int, Guid*, int>)Slot(ep, 14))(ep, mute ? 1 : 0, &ctx));
            }
            finally { Release(ep); }
        }

        static IntPtr Required() => Endpoint() is var ep && ep != IntPtr.Zero ? ep : throw new InvalidOperationException("no active audio output device");

        static IntPtr Endpoint()
        {
            CoInitializeEx(IntPtr.Zero, 0);   // COINIT_MULTITHREADED
            var clsid = CLSID_MMDeviceEnumerator;
            var iid = IID_IMMDeviceEnumerator;
            if (CoCreateInstance(ref clsid, IntPtr.Zero, 1, ref iid, out var enumerator) != 0 || enumerator == IntPtr.Zero) return IntPtr.Zero;
            try
            {
                // IMMDeviceEnumerator::GetDefaultAudioEndpoint(eRender=0, eMultimedia=1, **device) is slot 4.
                IntPtr device;
                if (((delegate* unmanaged<IntPtr, int, int, IntPtr*, int>)Slot(enumerator, 4))(enumerator, 0, 1, &device) != 0 || device == IntPtr.Zero)
                    return IntPtr.Zero;
                try
                {
                    // IMMDevice::Activate(iid, CLSCTX_ALL=1, null, **iface) is slot 3.
                    var volIid = IID_IAudioEndpointVolume;
                    IntPtr volume;
                    return ((delegate* unmanaged<IntPtr, Guid*, int, IntPtr, IntPtr*, int>)Slot(device, 3))(device, &volIid, 1, IntPtr.Zero, &volume) == 0 ? volume : IntPtr.Zero;
                }
                finally { Release(device); }
            }
            finally { Release(enumerator); }
        }

        static IntPtr Slot(IntPtr obj, int index) => *((IntPtr*)(*(IntPtr*)obj) + index);

        static int Call(IntPtr obj, int slot, out float value)
        {
            float v;
            var hr = ((delegate* unmanaged<IntPtr, float*, int>)Slot(obj, slot))(obj, &v);
            value = v;
            return hr;
        }

        static int Call(IntPtr obj, int slot, out int value)
        {
            int v;
            var hr = ((delegate* unmanaged<IntPtr, int*, int>)Slot(obj, slot))(obj, &v);
            value = v;
            return hr;
        }

        static int SetScalar(IntPtr obj, int slot, float value)
        {
            var ctx = Guid.Empty;
            return ((delegate* unmanaged<IntPtr, float, Guid*, int>)Slot(obj, slot))(obj, value, &ctx);
        }

        static void Release(IntPtr obj) { if (obj != IntPtr.Zero) ((delegate* unmanaged<IntPtr, uint>)Slot(obj, 2))(obj); }

        static void Check(int hr) { if (hr != 0) throw new InvalidOperationException($"audio endpoint call failed (0x{hr:X8})"); }

        [DllImport("ole32.dll")] static extern int CoInitializeEx(IntPtr reserved, uint flags);
        [DllImport("ole32.dll")] static extern int CoCreateInstance(ref Guid clsid, IntPtr outer, uint ctx, ref Guid iid, out IntPtr obj);
    }
}
