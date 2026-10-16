using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;

namespace WinTools;

// batt: battery status and charge limiting. Status comes from the battery driver
// (IOCTL_BATTERY_QUERY_INFORMATION/STATUS). Windows has no generic "stop charging" switch, so the
// limit goes to the laptop firmware through the vendor WMI interfaces that ship in it: ASUS's ATK
// WMI (charge limit 20-100%) and Dell's BIOS attribute WMI (custom charge start/stop). The firmware
// then enforces the limit itself, even while the machine sleeps; `batt daemon` re-applies the saved
// limit at logon (ASUS firmware forgets it on reboot) and `batt install` schedules that.
static class Batt
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("batt", "Show battery status and limit battery charging through the laptop firmware", Run);
    }

    const string Help = """
        batt - battery charge limiter

        Usage:
          batt [command]

        Commands:
          status                  Show battery, charging and configuration status
          limit <percent>         Stop charging at this percentage (10-100)
          disable                 Remove the limit (charge to 100%)
          lower-limit-delta <n>   Charging restarts n% below the limit where the firmware allows it (Dell)
          daemon                  Re-apply the saved limit (run at logon; see install)
          install                 Run `batt daemon` at every logon (scheduled task, needs admin)
          uninstall               Remove that scheduled task
          version                 Print version
          adapter, magsafe-led, prevent-idle-sleep, disable-charging-pre-sleep
                                  Mac-only controls; explained when used

        Charge limits need firmware support: ASUS (ATK WMI) and Dell (BIOS attribute WMI) laptops.
        """;

    static string ConfigPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "batt", "config.json");

    static int Run(string[] args)
    {
        var command = args.Length > 0 ? args[0] : "--help";
        var rest = args.Skip(1).Where(a => !a.StartsWith("--config", StringComparison.Ordinal) && !a.StartsWith("--log-level", StringComparison.Ordinal)
                                         && a != "--always-allow-non-root-access").ToArray();
        switch (command)
        {
            case "-h" or "--help" or "help": Console.WriteLine(Help); return 0;
            case "version" or "--version" or "-v": Console.WriteLine($"batt version v{Commands.Version}"); return 0;
            case "status": return Status();
            case "limit":
                if (rest.Length != 1) throw new UsageException("usage: batt limit <percent>");
                return SetLimit(ParsePercent(rest[0], 10, 100));
            case "disable": return SetLimit(100);
            case "lower-limit-delta":
                if (rest.Length != 1) throw new UsageException("usage: batt lower-limit-delta <percent>");
                var (limit, _) = LoadConfig();
                SaveConfig(limit, ParsePercent(rest[0], 0, 50));
                Console.WriteLine("Lower limit delta saved; it applies the next time the limit is set.");
                return 0;
            case "daemon": return Daemon();
            case "install": return Schtasks("/Create", "/F", "/SC", "ONLOGON", "/RL", "HIGHEST", "/TN", "batt", "/TR", $"\"{Environment.ProcessPath}\" batt daemon");
            case "uninstall": return Schtasks("/Delete", "/F", "/TN", "batt");
            case "adapter":
                Console.Error.WriteLine("batt: Windows cannot switch the power adapter off; use `batt limit` to stop charging instead");
                return 1;
            case "magsafe-led":
                Console.Error.WriteLine("batt: there is no MagSafe LED on this machine");
                return 1;
            case "prevent-idle-sleep" or "disable-charging-pre-sleep" or "prevent-system-sleep":
                Console.Error.WriteLine($"batt: {command} is not needed on Windows: the firmware enforces the limit while asleep");
                return 1;
            default: throw new UsageException($"unknown command \"{command}\" (see batt --help)");
        }
    }

    internal static int ParsePercent(string s, int min, int max) =>
        int.TryParse(s.TrimEnd('%'), NumberStyles.None, CultureInfo.InvariantCulture, out var v) && v >= min && v <= max
            ? v : throw new UsageException($"invalid value {s}: must be between {min} and {max}");

    // ---- configuration -----------------------------------------------------------------------

    static (int Limit, int Delta) LoadConfig()
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(ConfigPath));
            var root = doc.RootElement;
            return (root.TryGetProperty("limit", out var l) ? l.GetInt32() : 100,
                    root.TryGetProperty("lowerLimitDelta", out var d) ? d.GetInt32() : 2);
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException or InvalidOperationException) { return (100, 2); }
    }

    static void SaveConfig(int limit, int delta)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        File.WriteAllText(ConfigPath, $"{{\n  \"limit\": {limit},\n  \"lowerLimitDelta\": {delta}\n}}\n");
    }

    // ---- firmware interfaces -----------------------------------------------------------------

    enum Vendor { None, Asus, Dell }

    static Vendor Detect()
    {
        var output = PowerShell("""
            if (Get-CimClass -Namespace root/wmi -ClassName AsusAtkWmi_WMNB -ErrorAction SilentlyContinue) { 'asus' }
            elseif (Get-CimClass -Namespace root/dcim/sysman/biosattributes -ClassName BIOSAttributeInterface -ErrorAction SilentlyContinue) { 'dell' }
            """, out _);
        return output.Trim() switch { "asus" => Vendor.Asus, "dell" => Vendor.Dell, _ => Vendor.None };
    }

    static string VendorName(Vendor v) => v switch
    {
        Vendor.Asus => "ASUS ATK WMI",
        Vendor.Dell => "Dell BIOS attribute WMI",
        _ => "none",
    };

    static int Apply(int limit, int delta, Vendor vendor)
    {
        string script;
        switch (vendor)
        {
            case Vendor.Asus:
                // DEVS(0x00120057, limit): the ATK battery charge-limit device.
                script = $"$i = Get-CimInstance -Namespace root/wmi -ClassName AsusAtkWmi_WMNB -ErrorAction Stop | Select-Object -First 1; " +
                         $"Invoke-CimMethod -InputObject $i -MethodName DEVS -Arguments @{{Device_ID=[uint32]0x00120057; Control_Status=[uint32]{limit}}} -ErrorAction Stop | Out-Null";
                break;
            case Vendor.Dell:
                // Dell needs stop in 55-100 and start 50-95, at least 5 apart.
                var stop = Math.Max(55, limit);
                var start = Math.Clamp(stop - Math.Max(5, delta), 50, 95);
                var mode = limit >= 100 ? "Standard" : "Custom";
                script = "$i = Get-CimInstance -Namespace root/dcim/sysman/biosattributes -ClassName BIOSAttributeInterface -ErrorAction Stop; " +
                         "function Set-Attr($n, $v) { $r = Invoke-CimMethod -InputObject $i -MethodName SetAttribute -Arguments @{SecType=[uint32]0; SecHndCount=[uint32]0; SecHandle=[byte[]]@(); AttributeName=$n; AttributeValue=$v} -ErrorAction Stop; if ($r.Status -ne 0) { throw \"$n=$v refused by the BIOS (status $($r.Status); is a BIOS password set?)\" } }; " +
                         (limit < 100 ? $"Set-Attr 'CustomChargeStart' '{start}'; Set-Attr 'CustomChargeStop' '{stop}'; " : "") +
                         $"Set-Attr 'PrimaryBattChargeCfg' '{mode}'";
                break;
            default:
                Console.Error.WriteLine("batt: this laptop's firmware exposes no charge-limit interface batt can use (supported: ASUS ATK WMI, Dell BIOS WMI); use the manufacturer's app instead");
                return 1;
        }
        PowerShell(script, out var exit, out var error);
        if (exit != 0)
        {
            Console.Error.WriteLine($"batt: {VendorName(vendor)} refused the charge limit: {error.Trim().Split('\n')[0].Trim()}");
            return 1;
        }
        return 0;
    }

    static int SetLimit(int limit)
    {
        var (_, delta) = LoadConfig();
        var vendor = Detect();
        var status = Apply(limit, delta, vendor);
        if (status != 0) return status;
        SaveConfig(limit, delta);
        Console.WriteLine(limit >= 100
            ? "Charge limit removed: the battery charges to 100%."
            : $"Charge limit set to {limit}% ({VendorName(vendor)}); the firmware stops charging there.");
        return 0;
    }

    static int Daemon()
    {
        Console.WriteLine($"batt daemon version=v{Commands.Version}");
        if (!File.Exists(ConfigPath)) { Console.WriteLine("batt daemon: no saved limit"); return 0; }
        var (limit, delta) = LoadConfig();
        var status = Apply(limit, delta, Detect());
        if (status == 0) Console.WriteLine($"batt daemon: applied {limit}% charge limit");
        return status;
    }

    static int Schtasks(params string[] args)
    {
        var info = new ProcessStartInfo("schtasks.exe") { UseShellExecute = false };
        foreach (var a in args) info.ArgumentList.Add(a);
        using var p = Process.Start(info)!;
        p.WaitForExit();
        return p.ExitCode == 0 ? 0 : 1;
    }

    static string PowerShell(string script, out int exit) => PowerShell(script, out exit, out _);

    static string PowerShell(string script, out int exit, out string error)
    {
        var info = new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
        };
        foreach (var a in new[] { "-NoProfile", "-NonInteractive", "-Command", script }) info.ArgumentList.Add(a);
        using var p = Process.Start(info)!;
        var errTask = p.StandardError.ReadToEndAsync();
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        error = errTask.Result;
        exit = p.ExitCode;
        return output;
    }

    // ---- status ------------------------------------------------------------------------------

    internal sealed record Battery(string Name, string Manufacturer, uint DesignCapacity, uint FullCapacity, uint CycleCount,
        uint PowerState, uint Capacity, uint Voltage, int Rate);

    static int Status()
    {
        GetSystemPowerStatus(out var power);
        var batteries = Batteries();
        var (limit, delta) = LoadConfig();
        var percent = power.BatteryLifePercent <= 100 ? $"{power.BatteryLifePercent}%" : "unknown";
        var charging = batteries.Any(b => (b.PowerState & 4) != 0);
        Console.WriteLine("Charging status:");
        Console.WriteLine($"  Use power adapter: {(power.ACLineStatus == 1 ? "true" : "false")}");
        Console.WriteLine($"  Is charging: {(charging ? "true" : "false")}");
        Console.WriteLine($"  Current charge: {percent}");
        Console.WriteLine("Battery status:");
        if (batteries.Count == 0) Console.WriteLine("  No battery found");
        foreach (var b in batteries)
        {
            if (batteries.Count > 1) Console.WriteLine($"  {b.Name}:");
            var state = (b.PowerState & 4) != 0 ? "charging" : (b.PowerState & 2) != 0 ? "discharging" : "not charging";
            Console.WriteLine($"  State: {state}");
            Console.WriteLine($"  Full capacity: {b.FullCapacity} mWh");
            Console.WriteLine($"  Charge rate: {(b.Rate == unchecked((int)0x80000000) ? "unknown" : (b.Rate / 1000.0).ToString("0.0", CultureInfo.InvariantCulture) + " W")}");
            if (b.Voltage != 0xFFFFFFFF) Console.WriteLine($"  Voltage: {(b.Voltage / 1000.0).ToString("0.00", CultureInfo.InvariantCulture)} V");
            Console.WriteLine($"  Design capacity: {b.DesignCapacity} mWh");
            if (b.DesignCapacity > 0) Console.WriteLine($"  Health: {Health(b.FullCapacity, b.DesignCapacity)}");
            Console.WriteLine($"  Cycle count: {b.CycleCount}");
        }
        Console.WriteLine("Batt configuration:");
        Console.WriteLine($"  Upper limit: {limit}%");
        if (limit < 100) Console.WriteLine($"  Lower limit: {limit - delta}% ({delta}% delta)");
        Console.WriteLine($"  Charge-limit interface: {VendorName(Detect())}");
        return 0;
    }

    internal static string Health(uint full, uint design) =>
        (100.0 * full / design).ToString("0.0", CultureInfo.InvariantCulture) + "%";

    static readonly Guid GUID_DEVCLASS_BATTERY = new("72631e54-78a4-11d0-bcf7-00aa00b7b32a");

    static unsafe List<Battery> Batteries()
    {
        var list = new List<Battery>();
        var guid = GUID_DEVCLASS_BATTERY;
        var set = SetupDiGetClassDevsW(ref guid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
        if (set == new IntPtr(-1)) return list;
        try
        {
            for (uint i = 0; i < 8; i++)
            {
                var iface = new SP_DEVICE_INTERFACE_DATA { cbSize = (uint)sizeof(SP_DEVICE_INTERFACE_DATA) };
                if (!SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref guid, i, ref iface)) break;
                // SP_DEVICE_INTERFACE_DETAIL_DATA_W: DWORD cbSize (8 on x64) then the path.
                var detail = new byte[1024];
                fixed (byte* d = detail)
                {
                    *(uint*)d = IntPtr.Size == 8 ? 8u : 6u;
                    if (!SetupDiGetDeviceInterfaceDetailW(set, ref iface, d, (uint)detail.Length, out _, IntPtr.Zero)) continue;
                    var path = new string((char*)(d + 4));
                    if (Query(path) is { } battery) list.Add(battery);
                }
            }
        }
        finally { SetupDiDestroyDeviceInfoList(set); }
        return list;
    }

    static unsafe Battery Query(string path)
    {
        using var h = CreateFileW(path, 0xC0000000, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
        if (h.IsInvalid) return null;
        uint wait = 0, tag;
        if (!DeviceIoControl(h, IOCTL_BATTERY_QUERY_TAG, &wait, 4, &tag, 4, out _, IntPtr.Zero) || tag == 0) return null;

        var query = stackalloc uint[3] { tag, 0 /* BatteryInformation */, 0 };
        var info = stackalloc byte[36];
        if (!DeviceIoControl(h, IOCTL_BATTERY_QUERY_INFORMATION, query, 12, info, 36, out _, IntPtr.Zero)) return null;
        string Text(uint level)
        {
            query[1] = level;
            var buffer = stackalloc char[128];
            return DeviceIoControl(h, IOCTL_BATTERY_QUERY_INFORMATION, query, 12, buffer, 256, out var n, IntPtr.Zero)
                ? new string(buffer, 0, n / 2).TrimEnd('\0') : "";
        }
        var name = Text(4);           // BatteryDeviceName
        var manufacturer = Text(6);   // BatteryManufactureName

        var wait2 = stackalloc uint[5] { tag, 0, 0, 0, 0 };
        var status = stackalloc uint[4];
        if (!DeviceIoControl(h, IOCTL_BATTERY_QUERY_STATUS, wait2, 20, status, 16, out _, IntPtr.Zero)) return null;
        var u = (uint*)info;
        // BATTERY_INFORMATION: Capabilities, Technology+Reserved, Chemistry, DesignedCapacity,
        // FullChargedCapacity, DefaultAlert1, DefaultAlert2, CriticalBias, CycleCount.
        return new Battery(name, manufacturer, u[3], u[4], u[8], status[0], status[1], status[2], (int)status[3]);
    }

    const uint IOCTL_BATTERY_QUERY_TAG = 0x294040, IOCTL_BATTERY_QUERY_INFORMATION = 0x294044, IOCTL_BATTERY_QUERY_STATUS = 0x29404C;
    const uint DIGCF_PRESENT = 2, DIGCF_DEVICEINTERFACE = 0x10;

    [StructLayout(LayoutKind.Sequential)]
    struct SYSTEM_POWER_STATUS { public byte ACLineStatus, BatteryFlag, BatteryLifePercent, SystemStatusFlag; public uint BatteryLifeTime, BatteryFullLifeTime; }

    [StructLayout(LayoutKind.Sequential)]
    struct SP_DEVICE_INTERFACE_DATA { public uint cbSize; public Guid InterfaceClassGuid; public uint Flags; public IntPtr Reserved; }

    [DllImport("kernel32.dll")] static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS status);

    [DllImport("setupapi.dll", SetLastError = true)]
    static extern IntPtr SetupDiGetClassDevsW(ref Guid classGuid, IntPtr enumerator, IntPtr parent, uint flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    static extern bool SetupDiEnumDeviceInterfaces(IntPtr set, IntPtr devInfo, ref Guid guid, uint index, ref SP_DEVICE_INTERFACE_DATA data);

    [DllImport("setupapi.dll", SetLastError = true)]
    static extern unsafe bool SetupDiGetDeviceInterfaceDetailW(IntPtr set, ref SP_DEVICE_INTERFACE_DATA data, byte* detail, uint size, out uint required, IntPtr devInfo);

    [DllImport("setupapi.dll")] static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern SafeFileHandle CreateFileW(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern unsafe bool DeviceIoControl(SafeFileHandle h, uint code, void* input, int inputSize, void* output, int outputSize, out int returned, IntPtr overlapped);
}
