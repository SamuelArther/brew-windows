using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Radios;

namespace WinTools;

// blueutil / BluetoothConnector: Bluetooth power, paired/connected devices, inquiry, pairing and
// connecting. Power uses Windows.Devices.Radios (the Settings toggle), devices and pairing use
// Windows.Devices.Bluetooth/Enumeration, and connect/disconnect toggle the device's Bluetooth services
// with BluetoothSetServiceState (bthprops), which is how Windows itself (re)connects a paired device.
static unsafe class Blueutil
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("blueutil", "Get/set Bluetooth power, list, pair and connect devices", a => MediaControl.WinRT(() => Run(a), "the Bluetooth service"));
        Commands.Add("BluetoothConnector", "Connect and disconnect Bluetooth devices", a => MediaControl.WinRT(() => RunConnector(a), "the Bluetooth service"));
    }

    // ---- shared ---------------------------------------------------------------------------------

    sealed class Device
    {
        public ulong Address;
        public string Name, Id;
        public bool Connected, Paired;
        public string AddressText => FormatAddress(Address);
    }

    internal static string FormatAddress(ulong a) =>
        string.Join("-", Enumerable.Range(0, 6).Select(i => ((a >> (40 - 8 * i)) & 0xff).ToString("x2", CultureInfo.InvariantCulture)));

    static bool TryParseAddress(string text, out ulong address)
    {
        address = 0;
        var hex = text.Replace("-", "").Replace(":", "");
        return hex.Length == 12 && ulong.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out address);
    }

    static T Await<T>(Windows.Foundation.IAsyncOperation<T> op) => op.AsTask().GetAwaiter().GetResult();

    static Radio BluetoothRadio()
    {
        try { return Await(Radio.GetRadiosAsync()).FirstOrDefault(r => r.Kind == RadioKind.Bluetooth); }
        catch (Exception) { return null; }   // no Bluetooth stack
    }

    static Radio RequireRadio() => BluetoothRadio() ?? throw new InvalidOperationException("no Bluetooth radio found");

    static List<Device> PairedDevices()
    {
        var list = new List<Device>();
        try
        {
            foreach (var info in Await(DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelectorFromPairingState(true))))
            {
                using var device = Await(BluetoothDevice.FromIdAsync(info.Id));
                if (device == null) continue;
                list.Add(new Device { Address = device.BluetoothAddress, Name = device.Name, Id = info.Id, Paired = true, Connected = device.ConnectionStatus == BluetoothConnectionStatus.Connected });
            }
            foreach (var info in Await(DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelectorFromPairingState(true))))
            {
                using var device = Await(BluetoothLEDevice.FromIdAsync(info.Id));
                if (device == null || list.Any(d => d.Address == device.BluetoothAddress)) continue;
                list.Add(new Device { Address = device.BluetoothAddress, Name = device.Name, Id = info.Id, Paired = true, Connected = device.ConnectionStatus == BluetoothConnectionStatus.Connected });
            }
        }
        catch (Exception) { }   // no Bluetooth stack
        return list;
    }

    // An address or a paired device's name.
    static Device Find(string id, bool mustBePaired)
    {
        var paired = PairedDevices();
        if (TryParseAddress(id, out var address))
        {
            var match = paired.FirstOrDefault(d => d.Address == address);
            if (match != null || mustBePaired) return match;
            try
            {
                using var device = Await(BluetoothDevice.FromBluetoothAddressAsync(address));
                if (device != null) return new Device { Address = address, Name = device.Name, Id = device.DeviceId, Connected = device.ConnectionStatus == BluetoothConnectionStatus.Connected };
            }
            catch (COMException) { }
            return null;
        }
        return paired.FirstOrDefault(d => d.Name == id) ?? paired.FirstOrDefault(d => d.Name.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    static void Connect(Device device)
    {
        var info = DeviceInfo(device.Address);
        var services = InstalledServices(info);
        foreach (var saved in SavedServices(device.Address)) if (!services.Contains(saved)) services.Add(saved);
        if (services.Count == 0) services.AddRange(CommonServices);
        // Re-enabling a service makes Windows open its connection, as Settings' "Connect" does.
        var ok = false;
        foreach (var guid in services)
        {
            var g = guid;
            BluetoothSetServiceState(IntPtr.Zero, &info, &g, BLUETOOTH_SERVICE_DISABLE);
            ok |= BluetoothSetServiceState(IntPtr.Zero, &info, &g, BLUETOOTH_SERVICE_ENABLE) == 0;
        }
        if (!ok) throw new InvalidOperationException($"could not connect {device.Name} (is it on and in range?)");
    }

    static void Disconnect(Device device)
    {
        var info = DeviceInfo(device.Address);
        var services = InstalledServices(info);
        if (services.Count == 0) return;
        SaveServices(device.Address, services);   // so --connect can bring the same services back
        foreach (var guid in services)
        {
            var g = guid;
            BluetoothSetServiceState(IntPtr.Zero, &info, &g, BLUETOOTH_SERVICE_DISABLE);
        }
    }

    static BLUETOOTH_DEVICE_INFO DeviceInfo(ulong address)
    {
        var info = new BLUETOOTH_DEVICE_INFO { dwSize = (uint)sizeof(BLUETOOTH_DEVICE_INFO), Address = address };
        var hr = BluetoothGetDeviceInfo(IntPtr.Zero, &info);
        if (hr != 0) throw new InvalidOperationException($"Windows doesn't know Bluetooth device {FormatAddress(address)} (error {hr})");
        return info;
    }

    static List<Guid> InstalledServices(BLUETOOTH_DEVICE_INFO info)
    {
        uint count = 32;
        var guids = new Guid[count];
        fixed (Guid* g = guids)
            if (BluetoothEnumerateInstalledServices(IntPtr.Zero, &info, &count, g) != 0) return [];
        return guids.Take((int)count).ToList();
    }

    // Audio sink, headset, hands-free, AVRCP and HID: what a headset, speaker or keyboard offers.
    static readonly Guid[] CommonServices =
    [
        new("0000110b-0000-1000-8000-00805f9b34fb"), new("00001108-0000-1000-8000-00805f9b34fb"), new("0000111e-0000-1000-8000-00805f9b34fb"),
        new("0000110e-0000-1000-8000-00805f9b34fb"), new("00001124-0000-1000-8000-00805f9b34fb"),
    ];

    static string ServiceFile(ulong address) =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "wintools", "bluetooth", $"{address:x12}.services");

    static List<Guid> SavedServices(ulong address)
    {
        try { return File.ReadAllLines(ServiceFile(address)).Select(l => Guid.TryParse(l, out var g) ? g : Guid.Empty).Where(g => g != Guid.Empty).ToList(); }
        catch (IOException) { return []; }
        catch (UnauthorizedAccessException) { return []; }
    }

    static void SaveServices(ulong address, List<Guid> services)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ServiceFile(address))!);
            File.WriteAllLines(ServiceFile(address), services.Select(g => g.ToString()));
        }
        catch (IOException) { }
    }

    static bool IsConnected(ulong address)
    {
        try
        {
            using var device = Await(BluetoothDevice.FromBluetoothAddressAsync(address));
            return device?.ConnectionStatus == BluetoothConnectionStatus.Connected;
        }
        catch (COMException) { return false; }
    }

    // ---- blueutil -------------------------------------------------------------------------------

    static int Run(string[] args)
    {
        var format = "default";
        var actions = new List<(string Option, string[] Values)>();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            // Optional values (-p [STATE], --inquiry [T]) are taken when the next word isn't an option.
            string Optional() => i + 1 < args.Length && !args[i + 1].StartsWith('-') ? args[++i] : null;
            string Required() => ++i < args.Length ? args[i] : throw new UsageException($"{arg} needs a value");
            switch (arg)
            {
                case "-h" or "--help": BlueutilHelp(); return 0;
                case "-v" or "--version": Console.WriteLine(Commands.Version); return 0;
                case "--format": format = Required(); if (format is not ("default" or "new-default" or "json" or "json-pretty")) throw new UsageException($"unknown format '{format}'"); break;
                case "-p" or "--power" or "-d" or "--discoverable": actions.Add((arg.TrimStart('-')[0] == 'p' ? "power" : "discoverable", [Optional()])); break;
                case "--inquiry": actions.Add(("inquiry", [Optional()])); break;
                case "--paired" or "--connected" or "--favourites" or "--favorites" or "--recent": actions.Add((arg[2..], [])); break;
                case "--info" or "--is-connected" or "--connect" or "--disconnect" or "--unpair" or "--add-favourite" or "--add-favorite"
                    or "--remove-favourite" or "--remove-favorite": actions.Add((arg[2..], [Required()])); break;
                case "--pair": actions.Add(("pair", [Required(), Optional()])); break;
                case "--wait-connect" or "--wait-disconnect": actions.Add((arg[2..], [Required(), Optional()])); break;
                case "--wait-rssi": throw new UsageException("--wait-rssi needs signal strength readings, which Windows doesn't expose for classic Bluetooth");
                default: throw new UsageException($"unknown option {arg} (see blueutil --help)");
            }
        }
        if (actions.Count == 0)
        {
            var radio = BluetoothRadio();
            Console.WriteLine($"Power: {(radio?.State == RadioState.On ? 1 : 0)}");
            Console.WriteLine("Discoverable: 0");
            return 0;
        }
        var status = 0;
        foreach (var (option, values) in actions) status = Math.Max(status, Do(option, values, format));
        return status;
    }

    static void BlueutilHelp() => Console.WriteLine("""
        Usage: blueutil [options]

        Without options outputs current state

            -p, --power               output power state as 1 or 0
            -p, --power STATE         set power state
            -d, --discoverable        output discoverable state as 1 or 0
            --favourites, --favorites list favourite devices (Windows has none; prints paired ones)
            --inquiry [T]             inquiry devices in range, 10 seconds duration by default excluding time for name updates
            --paired                  list paired devices
            --connected               list connected devices
            --info ID                 show information about device
            --is-connected ID         connected state of device as 1 or 0
            --connect ID              create a connection to device
            --disconnect ID           close the connection to device
            --pair ID [PIN]           pair with device, optional PIN of up to 16 characters will be used instead of interactive input if requested
            --unpair ID               unpair device
            --wait-connect ID [TIMEOUT]    EXPERIMENTAL wait for device to connect
            --wait-disconnect ID [TIMEOUT] EXPERIMENTAL wait for device to disconnect
            --format FORMAT           change output format of info and all listing commands (default, new-default, json, json-pretty)

            -h, --help                this help
            -v, --version             show version

        STATE can be one of: 1, on, 0, off, toggle
        ID can be either address in form xxxxxxxxxxxx, xx-xx-xx-xx-xx-xx or xx:xx:xx:xx:xx:xx, or name of paired device

        Windows makes the PC discoverable only while Settings > Bluetooth is open, so discoverable can't be set.
        """);

    static bool ParseState(string text, bool current) => text switch
    {
        "1" or "on" => true,
        "0" or "off" => false,
        "toggle" => !current,
        _ => throw new UsageException($"invalid state '{text}' (1, on, 0, off or toggle)"),
    };

    static int Do(string option, string[] values, string format)
    {
        switch (option)
        {
            case "power":
                var radio = RequireRadio();
                if (values[0] == null) { Console.WriteLine(radio.State == RadioState.On ? 1 : 0); return 0; }
                var on = ParseState(values[0], radio.State == RadioState.On);
                if (Await(Radio.RequestAccessAsync()) != RadioAccessStatus.Allowed) throw new InvalidOperationException("Windows denied access to the Bluetooth radio");
                var result = Await(radio.SetStateAsync(on ? RadioState.On : RadioState.Off));
                if (result != RadioAccessStatus.Allowed) throw new InvalidOperationException($"could not switch Bluetooth {(on ? "on" : "off")} ({result})");
                return 0;
            case "discoverable":
                if (values[0] == null) { Console.WriteLine(0); return 0; }
                Console.Error.WriteLine("blueutil: Windows makes the PC discoverable only while Settings > Bluetooth is open; it can't be set from the command line");
                return 1;
            case "paired" or "favourites" or "favorites" or "recent":
                Print(PairedDevices(), format);
                return 0;
            case "connected":
                Print(PairedDevices().Where(d => d.Connected).ToList(), format);
                return 0;
            case "inquiry":
                var seconds = values[0] == null ? 10 : double.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var t) && t > 0 ? t : throw new UsageException("--inquiry needs seconds");
                RequireRadio();
                Print(Inquiry(seconds), format);
                return 0;
            case "info":
                var device = Find(values[0], false) ?? throw new InvalidOperationException($"Device not found by ID: {values[0]}");
                if (format.StartsWith("json")) Console.WriteLine(Json(device, format == "json-pretty"));
                else Console.WriteLine(Line(device, format));
                return 0;
            case "is-connected":
                var d = Find(values[0], false) ?? throw new InvalidOperationException($"Device not found by ID: {values[0]}");
                Console.WriteLine(IsConnected(d.Address) ? 1 : 0);
                return 0;
            case "connect" or "disconnect":
                var target = Find(values[0], true) ?? throw new InvalidOperationException($"Device not paired: {values[0]}");
                if (option == "connect") Connect(target); else Disconnect(target);
                return 0;
            case "pair":
                return Pair(values[0], values[1]);
            case "unpair":
                var paired = Find(values[0], true) ?? throw new InvalidOperationException($"Device not paired: {values[0]}");
                var info = Await(DeviceInformation.CreateFromIdAsync(paired.Id));
                var status = Await(info.Pairing.UnpairAsync()).Status;
                if (status != DeviceUnpairingResultStatus.Unpaired && status != DeviceUnpairingResultStatus.AlreadyUnpaired)
                    throw new InvalidOperationException($"unpairing failed ({status})");
                return 0;
            case "wait-connect" or "wait-disconnect":
                var w = Find(values[0], false) ?? throw new InvalidOperationException($"Device not found by ID: {values[0]}");
                var timeout = values[1] == null ? double.PositiveInfinity : double.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var to) ? to : throw new UsageException("timeout must be seconds");
                var want = option == "wait-connect";
                var start = DateTime.UtcNow;
                while (IsConnected(w.Address) != want)
                {
                    if ((DateTime.UtcNow - start).TotalSeconds >= timeout) return 1;
                    Thread.Sleep(500);
                }
                return 0;
            default:   // add/remove favourite
                Console.Error.WriteLine("blueutil: Windows has no favourite Bluetooth devices");
                return 1;
        }
    }

    static List<Device> Inquiry(double seconds)
    {
        var found = new Dictionary<string, DeviceInformation>();
        var watcher = DeviceInformation.CreateWatcher(BluetoothDevice.GetDeviceSelectorFromPairingState(false), null, DeviceInformationKind.AssociationEndpoint);
        watcher.Added += (_, info) => { lock (found) found[info.Id] = info; };
        watcher.Updated += (_, _) => { };
        watcher.Start();
        Thread.Sleep(TimeSpan.FromSeconds(seconds));
        watcher.Stop();
        var list = new List<Device>();
        lock (found)
            foreach (var info in found.Values)
            {
                try
                {
                    using var device = Await(BluetoothDevice.FromIdAsync(info.Id));
                    if (device != null) list.Add(new Device { Address = device.BluetoothAddress, Name = device.Name, Id = info.Id, Connected = device.ConnectionStatus == BluetoothConnectionStatus.Connected });
                }
                catch (COMException) { }
            }
        return list;
    }

    static int Pair(string id, string pin)
    {
        var device = Find(id, false) ?? throw new InvalidOperationException($"Device not found by ID: {id} (run --inquiry first and make sure it's in pairing mode)");
        if (device.Paired) return 0;
        var info = Await(DeviceInformation.CreateFromIdAsync(device.Id));
        var custom = info.Pairing.Custom;
        custom.PairingRequested += (_, request) =>
        {
            switch (request.PairingKind)
            {
                case DevicePairingKinds.ProvidePin:
                    var code = pin;
                    if (code == null) { Console.Write("Enter PIN: "); code = Console.ReadLine()?.Trim(); }
                    request.Accept(code ?? "");
                    break;
                case DevicePairingKinds.ConfirmPinMatch:
                    Console.WriteLine($"Confirming passkey {request.Pin}");
                    request.Accept();
                    break;
                case DevicePairingKinds.DisplayPin:
                    Console.WriteLine($"Type {request.Pin} on the device");
                    request.Accept();
                    break;
                default:
                    request.Accept();
                    break;
            }
        };
        var kinds = DevicePairingKinds.ConfirmOnly | DevicePairingKinds.ProvidePin | DevicePairingKinds.ConfirmPinMatch | DevicePairingKinds.DisplayPin;
        var result = Await(custom.PairAsync(kinds));
        if (result.Status is DevicePairingResultStatus.Paired or DevicePairingResultStatus.AlreadyPaired) return 0;
        throw new InvalidOperationException($"pairing failed ({result.Status})");
    }

    static string Line(Device d, string format) => format == "new-default"
        ? $"{d.AddressText} {(d.Connected ? "connected" : "not connected")}, {(d.Paired ? "paired" : "not paired")}, name: \"{d.Name}\""
        : $"address: {d.AddressText}, {(d.Connected ? "connected" : "not connected")}, not favourite, {(d.Paired ? "paired" : "not paired")}, name: \"{d.Name}\"";

    static string Json(Device d, bool pretty)
    {
        var fields = new[]
        {
            $"\"address\":\"{d.AddressText}\"", $"\"name\":{MediaControl.JsonString(d.Name)}", $"\"connected\":{(d.Connected ? "true" : "false")}",
            $"\"paired\":{(d.Paired ? "true" : "false")}", "\"favourite\":false",
        };
        return pretty ? "{\n  " + string.Join(",\n  ", fields.Select(f => f.Replace("\":", "\": "))) + "\n}" : "{" + string.Join(",", fields) + "}";
    }

    static void Print(List<Device> devices, string format)
    {
        if (format.StartsWith("json"))
        {
            var pretty = format == "json-pretty";
            Console.WriteLine(devices.Count == 0 ? "[]" : pretty
                ? "[\n" + string.Join(",\n", devices.Select(d => "  " + Json(d, true).Replace("\n", "\n  "))) + "\n]"
                : "[" + string.Join(",", devices.Select(d => Json(d, false))) + "]");
            return;
        }
        foreach (var d in devices) Console.WriteLine(Line(d, format));
    }

    // ---- BluetoothConnector ---------------------------------------------------------------------

    static int RunConnector(string[] args)
    {
        string action = null, address = null;
        var notify = false;
        foreach (var arg in args)
        {
            switch (arg)
            {
                case "-h" or "--help": ConnectorUsage(PairedDevices()); return 0;
                case "-c" or "--connect": action = "connect"; break;
                case "-d" or "--disconnect": action = "disconnect"; break;
                case "-s" or "--status": action = "status"; break;
                case "-n" or "--notify": notify = true; break;
                default:
                    if (arg.StartsWith('-') || address != null) throw new UsageException($"unexpected argument {arg}");
                    address = arg;
                    break;
            }
        }
        if (address == null)
        {
            ConnectorUsage(PairedDevices());
            return 64;   // EX_USAGE, as upstream
        }
        if (!TryParseAddress(address, out var addr)) throw new UsageException($"invalid MAC address '{address}' (format 00-00-00-00-00-00)");
        var device = PairedDevices().FirstOrDefault(d => d.Address == addr);
        if (device == null)
        {
            Console.WriteLine("Not paired to device");
            return 252;
        }
        var connected = IsConnected(addr);
        if (action == "status")
        {
            Console.WriteLine(connected ? "Connected" : "Disconnected");
            return 0;
        }
        var connect = action == "connect" || (action == null && !connected);
        if (connect) Connect(device); else Disconnect(device);
        if (notify) TerminalNotifier.Notify(device.Name, connect ? "Connected" : "Disconnected");
        return 0;
    }

    static void ConnectorUsage(List<Device> paired)
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("BluetoothConnector [--connect|--disconnect|--status] [--notify] 00-00-00-00-00-00");
        Console.WriteLine();
        Console.WriteLine("Get the MAC address from the list below (if your device is missing, pair it with your computer first):");
        foreach (var d in paired) Console.WriteLine($"{d.AddressText} - {d.Name}");
    }

    // ---- bthprops -------------------------------------------------------------------------------

    const uint BLUETOOTH_SERVICE_DISABLE = 0, BLUETOOTH_SERVICE_ENABLE = 1;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct BLUETOOTH_DEVICE_INFO
    {
        public uint dwSize;
        public ulong Address;
        public uint ulClassofDevice;
        public int fConnected, fRemembered, fAuthenticated;
        public fixed ushort stLastSeen[8];
        public fixed ushort stLastUsed[8];
        public fixed char szName[248];
    }

    [DllImport("bthprops.cpl")]
    static extern uint BluetoothGetDeviceInfo(IntPtr radio, BLUETOOTH_DEVICE_INFO* info);

    [DllImport("bthprops.cpl")]
    static extern uint BluetoothEnumerateInstalledServices(IntPtr radio, BLUETOOTH_DEVICE_INFO* info, uint* count, Guid* services);

    [DllImport("bthprops.cpl")]
    static extern uint BluetoothSetServiceState(IntPtr radio, BLUETOOTH_DEVICE_INFO* info, Guid* service, uint flags);
}
