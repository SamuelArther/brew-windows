using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// sensors / sensors-detect (lm-sensors) and osx-cpu-temp: temperatures and fans without a kernel driver.
// ACPI thermal zones come from the "Thermal Zone Information" performance counters (PDH; the same
// numbers as MSAcpi_ThermalZoneTemperature, but readable without admin); NVIDIA GPUs from NVML
// (nvml.dll, installed with the driver). Motherboard Super-I/O chips need a ring-0 driver on Windows,
// which these tools deliberately don't install.
static unsafe class LmSensors
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("sensors", "Show temperatures and fans (ACPI thermal zones, NVIDIA GPUs)", Run);
        Commands.Add("sensors-detect", "Detect the hardware monitoring sources Windows exposes", RunDetect);
        Commands.Add("osx-cpu-temp", "Output the current CPU temperature", RunCpuTemp);
    }

    sealed record Reading(string Label, string Kind, double Value);   // Kind: temp (°C), fan (%), power (W)
    sealed record Chip(string Name, string Adapter, List<Reading> Readings);

    // ---- sources ---------------------------------------------------------------------------------

    static List<Chip> Chips()
    {
        var chips = new List<Chip>();
        var zones = ThermalZones();
        for (var i = 0; i < zones.Count; i++)
            chips.Add(new Chip($"acpitz-acpi-{i}", $"ACPI interface ({zones[i].Zone})", [new Reading("temp1", "temp", zones[i].Celsius)]));
        chips.AddRange(NvidiaGpus());
        return chips;
    }

    // Thermal zone temperatures; "High Precision Temperature" is in tenths of a kelvin, "Temperature" in kelvin.
    static List<(string Zone, double Celsius)> ThermalZones()
    {
        var precise = Counter(@"\Thermal Zone Information(*)\High Precision Temperature");
        if (precise.Count > 0) return precise.Where(z => z.Value > 0).Select(z => (z.Name, Math.Round(z.Value / 10 - 273.15, 1))).ToList();
        return Counter(@"\Thermal Zone Information(*)\Temperature").Where(z => z.Value > 0).Select(z => (z.Name, Math.Round(z.Value - 273.15, 1))).ToList();
    }

    static List<(string Name, double Value)> Counter(string path)
    {
        var result = new List<(string, double)>();
        if (PdhOpenQueryW(null, 0, out var query) != 0) return result;
        try
        {
            if (PdhAddEnglishCounterW(query, path, 0, out var counter) != 0 || PdhCollectQueryData(query) != 0) return result;
            uint size = 0;
            if (PdhGetFormattedCounterArrayW(counter, PDH_FMT_DOUBLE, ref size, out _, null) != PDH_MORE_DATA || size == 0) return result;
            var buffer = new byte[size];
            fixed (byte* b = buffer)
            {
                if (PdhGetFormattedCounterArrayW(counter, PDH_FMT_DOUBLE, ref size, out var count, b) != 0) return result;
                for (var i = 0; i < count; i++)
                {
                    var item = (PDH_FMT_COUNTERVALUE_ITEM*)(b + i * sizeof(PDH_FMT_COUNTERVALUE_ITEM));
                    if (item->CStatus is 0 or 1) result.Add((new string(item->szName), item->Value));
                }
            }
        }
        finally { PdhCloseQuery(query); }
        return result.OrderBy(r => r.Item1, StringComparer.Ordinal).ToList();
    }

    static List<Chip> NvidiaGpus()
    {
        var chips = new List<Chip>();
        if (!NativeLibrary.TryLoad("nvml.dll", out var lib)
            && !NativeLibrary.TryLoad(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"NVIDIA Corporation\NVSMI\nvml.dll"), out lib))
            return chips;
        try
        {
            T Fn<T>(string name) where T : unmanaged => NativeLibrary.TryGetExport(lib, name, out var p) ? *(T*)&p : default;
            var init = Fn<nint>("nvmlInit_v2");
            if (init == 0 || ((delegate* unmanaged<int>)init)() != 0) return chips;
            var getCount = (delegate* unmanaged<uint*, int>)Fn<nint>("nvmlDeviceGetCount_v2");
            var getHandle = (delegate* unmanaged<uint, nint*, int>)Fn<nint>("nvmlDeviceGetHandleByIndex_v2");
            var getName = (delegate* unmanaged<nint, byte*, uint, int>)Fn<nint>("nvmlDeviceGetName");
            var getTemp = (delegate* unmanaged<nint, int, uint*, int>)Fn<nint>("nvmlDeviceGetTemperature");
            var getFan = (delegate* unmanaged<nint, uint*, int>)Fn<nint>("nvmlDeviceGetFanSpeed");
            var getPower = (delegate* unmanaged<nint, uint*, int>)Fn<nint>("nvmlDeviceGetPowerUsage");
            var getPci = (delegate* unmanaged<nint, byte*, int>)Fn<nint>("nvmlDeviceGetPciInfo_v3");
            uint count = 0;
            var nameBuffer = stackalloc byte[96];
            var pci = stackalloc byte[128];
            if (getCount != null) getCount(&count);
            for (uint i = 0; i < count; i++)
            {
                nint handle;
                if (getHandle(i, &handle) != 0) continue;

                var name = getName != null && getName(handle, nameBuffer, 96) == 0 ? Encoding.ASCII.GetString(nameBuffer, 96).TrimEnd('\0') : "NVIDIA GPU";
                var readings = new List<Reading>();
                uint value;
                if (getTemp != null && getTemp(handle, 0, &value) == 0) readings.Add(new Reading("temp1", "temp", value));
                if (getFan != null && getFan(handle, &value) == 0) readings.Add(new Reading("fan1", "fan", value));
                if (getPower != null && getPower(handle, &value) == 0) readings.Add(new Reading("power1", "power", value / 1000.0));
                // nvmlPciInfo_t: busIdLegacy[16], domain, bus, device, ... -> "nvidia-pci-0100" like Linux's chip names.

                var bus = getPci != null && getPci(handle, pci) == 0 ? $"{*(uint*)(pci + 20):x2}{*(uint*)(pci + 24):x2}" : $"{i:x4}";
                if (readings.Count > 0) chips.Add(new Chip($"nvidia-pci-{bus}", name, readings));
            }
            var shutdown = Fn<nint>("nvmlShutdown");
            if (shutdown != 0) ((delegate* unmanaged<int>)shutdown)();
        }
        finally { NativeLibrary.Free(lib); }
        return chips;
    }

    static string Degrees(double celsius, bool fahrenheit) =>
        (fahrenheit ? celsius * 9 / 5 + 32 : celsius).ToString("+0.0;-0.0", CultureInfo.InvariantCulture) + (fahrenheit ? "°F" : "°C");

    // ---- sensors ---------------------------------------------------------------------------------

    static int Run(string[] args)
    {
        bool fahrenheit = false, raw = false, json = false, noAdapter = false;
        var filters = new List<string>();
        foreach (var arg in args)
        {
            switch (arg)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage: sensors [OPTION]... [CHIP]...
                          -c, --config-file     Accepted for compatibility (no config is needed)
                          -h, --help            Display this help text
                          -s, --set             Not available (Windows has no writable sensor limits)
                          -A, --no-adapter      Do not show adapter for each chip
                              --bus-list        Not available on Windows
                          -u                    Raw output
                          -j                    Json output
                          -v, --version         Display the program version
                          -f, --fahrenheit      Show temperatures in degrees fahrenheit

                        Use `sensors-detect' to see which sources are available. CHIP may use * wildcards,
                        e.g. sensors 'acpitz-*'.
                        """);
                    return 0;
                case "-v" or "--version": Console.WriteLine($"sensors version {Commands.Version} (wintools: ACPI thermal zones, NVIDIA NVML)"); return 0;
                case "-f" or "--fahrenheit": fahrenheit = true; break;
                case "-u": raw = true; break;
                case "-j": json = true; break;
                case "-A" or "--no-adapter": noAdapter = true; break;
                case "-c" or "--config-file": break;
                case "-s" or "--set": Console.Error.WriteLine("sensors: Windows sensors have no settable limits"); return 1;
                case "--bus-list": Console.Error.WriteLine("sensors: Windows doesn't expose I2C buses"); return 1;
                default:
                    if (arg.StartsWith('-')) throw new UsageException($"invalid option {arg}");
                    filters.Add(arg);
                    break;
            }
        }
        var chips = Chips().Where(c => filters.Count == 0 || filters.Any(f => Wildcard(f, c.Name))).ToList();
        if (chips.Count == 0)
        {
            if (filters.Count > 0) { Console.Error.WriteLine($"Specified sensor(s) not found!"); return 1; }
            Console.Error.WriteLine("No sensors found!\nWindows exposes no ACPI thermal zones on this machine and no NVIDIA GPU was found (see sensors-detect).");
            return 1;
        }
        if (json) { Console.WriteLine(Json(chips, fahrenheit)); return 0; }
        foreach (var chip in chips)
        {
            Console.WriteLine(chip.Name);
            if (!noAdapter) Console.WriteLine($"Adapter: {chip.Adapter}");
            foreach (var r in chip.Readings)
            {
                if (raw)
                {
                    Console.WriteLine($"{r.Label}:");
                    var value = r.Kind == "temp" && fahrenheit ? r.Value * 9 / 5 + 32 : r.Value;
                    Console.WriteLine($"  {r.Label}_input: {value.ToString("0.000", CultureInfo.InvariantCulture)}");
                    continue;
                }
                var text = r.Kind switch
                {
                    "temp" => Degrees(r.Value, fahrenheit),
                    "fan" => $"{r.Value,4:0} %",
                    _ => $"{r.Value.ToString("0.00", CultureInfo.InvariantCulture)} W",
                };
                Console.WriteLine($"{(r.Label + ":"),-14}{text,9}");
            }
            Console.WriteLine();
        }
        return 0;
    }

    static bool Wildcard(string pattern, string name)
    {
        if (!pattern.Contains('*')) return pattern.Equals(name, StringComparison.OrdinalIgnoreCase);
        var parts = pattern.Split('*');
        var at = 0;
        for (var i = 0; i < parts.Length; i++)
        {
            var idx = name.IndexOf(parts[i], at, StringComparison.OrdinalIgnoreCase);
            if (idx < 0 || (i == 0 && idx != 0)) return false;
            at = idx + parts[i].Length;
        }
        return parts[^1].Length == 0 || at == name.Length;
    }

    static string Json(List<Chip> chips, bool fahrenheit)
    {
        var sb = new StringBuilder("{\n");
        for (var c = 0; c < chips.Count; c++)
        {
            var chip = chips[c];
            sb.Append($"   {MediaControl.JsonString(chip.Name)}:{{\n      \"Adapter\": {MediaControl.JsonString(chip.Adapter)}");
            foreach (var r in chip.Readings)
            {
                var value = r.Kind == "temp" && fahrenheit ? r.Value * 9 / 5 + 32 : r.Value;
                sb.Append($",\n      \"{r.Label}\":{{\n         \"{r.Label}_input\": {value.ToString("0.000", CultureInfo.InvariantCulture)}\n      }}");
            }
            sb.Append(c == chips.Count - 1 ? "\n   }\n" : "\n   },\n");
        }
        return sb.Append('}').ToString();
    }

    // ---- sensors-detect --------------------------------------------------------------------------

    static int RunDetect(string[] args)
    {
        if (args.Any(a => a is "-h" or "--help"))
        {
            Console.WriteLine("Usage: sensors-detect [--auto]\nReports the sensor sources Windows exposes. Nothing needs to be loaded or configured.");
            return 0;
        }
        Console.WriteLine($"# sensors-detect version {Commands.Version} (wintools)");
        Console.WriteLine($"# System: {RuntimeInformation.OSDescription}");
        Console.WriteLine();
        Console.WriteLine("This program reports the hardware monitoring sources Windows makes available without");
        Console.WriteLine("installing a kernel driver. Unlike Linux, there are no modules to load.");
        Console.WriteLine();
        var zones = ThermalZones();
        Console.WriteLine($"ACPI thermal zones (Thermal Zone Information counters)...   {(zones.Count > 0 ? $"Success! ({zones.Count} found)" : "No")}");
        foreach (var (zone, c) in zones) Console.WriteLine($"    {zone}: {Degrees(c, false)}");
        var gpus = NvidiaGpus();
        Console.WriteLine($"NVIDIA GPUs (NVML, nvml.dll)...                             {(gpus.Count > 0 ? $"Success! ({gpus.Count} found)" : "No")}");
        foreach (var g in gpus) Console.WriteLine($"    {g.Name}: {g.Adapter}");
        Console.WriteLine("Super-I/O and SMBus sensor chips...                         Not probed (needs a ring-0 driver on Windows)");
        Console.WriteLine();
        Console.WriteLine(zones.Count + gpus.Count > 0
            ? "Now run `sensors' to see the readings."
            : "Sorry, no sensors were detected. Vendor tools (or LibreHardwareMonitor) can read chips through their own drivers.");
        return 0;
    }

    // ---- osx-cpu-temp ----------------------------------------------------------------------------

    static int RunCpuTemp(string[] args)
    {
        bool fahrenheit = false, cpu = false, gpu = false, fans = false;
        foreach (var arg in args)
        {
            if (!arg.StartsWith('-') || arg.Length < 2) throw new UsageException($"invalid argument '{arg}'");
            foreach (var c in arg[1..])
            {
                switch (c)
                {
                    case 'F': fahrenheit = true; break;
                    case 'C': fahrenheit = false; break;
                    case 'c': cpu = true; break;
                    case 'g': gpu = true; break;
                    case 'f': fans = true; break;
                    case 'h':
                        Console.WriteLine("""
                            usage: osx-cpu-temp <options>
                            Options:
                              -F  Display temperatures in degrees Fahrenheit.
                              -C  Display temperatures in degrees Celsius (Default).
                              -c  Display CPU temperature (Default).
                              -g  Display GPU temperature.
                              -f  Display fan speeds.
                              -h  Display this help.
                            The CPU temperature is the hottest ACPI thermal zone; the GPU one comes from NVIDIA's NVML.
                            """);
                        return 0;
                    default: throw new UsageException($"invalid option -{c}");
                }
            }
        }
        if (!gpu && !fans) cpu = true;
        var status = 0;
        if (cpu)
        {
            var zones = ThermalZones();
            if (zones.Count == 0) { Console.Error.WriteLine("osx-cpu-temp: this machine's firmware exposes no ACPI thermal zones"); status = 1; }
            else Console.WriteLine(Degrees(zones.Max(z => z.Celsius), fahrenheit).TrimStart('+'));
        }
        var gpus = gpu || fans ? NvidiaGpus() : [];
        if (gpu)
        {
            var temp = gpus.SelectMany(g => g.Readings).Where(r => r.Kind == "temp").Select(r => (double?)r.Value).Max();
            if (temp is double t) Console.WriteLine(Degrees(t, fahrenheit).TrimStart('+'));
            else { Console.Error.WriteLine("osx-cpu-temp: no GPU temperature source (NVIDIA GPUs only)"); status = 1; }
        }
        if (fans)
        {
            var list = gpus.SelectMany(g => g.Readings.Where(r => r.Kind == "fan").Select(r => (g.Adapter, r.Value))).ToList();
            Console.WriteLine($"Num fans: {list.Count}");
            for (var i = 0; i < list.Count; i++) Console.WriteLine($"Fan {i} - {list[i].Adapter} at {list[i].Value:0}% of max speed");
        }
        return status;
    }

    // ---- PDH -------------------------------------------------------------------------------------

    const uint PDH_FMT_DOUBLE = 0x200, PDH_MORE_DATA = 0x800007D2;

    [StructLayout(LayoutKind.Sequential)]
    struct PDH_FMT_COUNTERVALUE_ITEM { public char* szName; public uint CStatus; public double Value; }

    [DllImport("pdh.dll", CharSet = CharSet.Unicode)] static extern uint PdhOpenQueryW(string source, nint user, out nint query);
    [DllImport("pdh.dll", CharSet = CharSet.Unicode)] static extern uint PdhAddEnglishCounterW(nint query, string path, nint user, out nint counter);
    [DllImport("pdh.dll")] static extern uint PdhCollectQueryData(nint query);
    [DllImport("pdh.dll")] static extern uint PdhGetFormattedCounterArrayW(nint counter, uint format, ref uint size, out uint count, byte* buffer);
    [DllImport("pdh.dll")] static extern uint PdhCloseQuery(nint query);
}
