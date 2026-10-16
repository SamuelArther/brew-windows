using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Devices.Radios;
using Windows.Storage.Streams;

namespace WinTools;

// bluez: BlueZ's command-line tools over Windows' Bluetooth stack. bluetoothctl (interactive or
// one-shot: list/show/devices/info/power/scan/pair/connect/disconnect/remove) and hciconfig/hcitool use
// Windows.Devices.Bluetooth and Radios (WinRT); rfcomm lists the Bluetooth serial (SPP) COM ports
// Windows creates; sdptool browses a device's RFCOMM services and their SDP records. btmon and
// bluemoon need raw HCI sockets, which Windows doesn't give applications, and say so.
static class Bluez
{
    const string Version = "5.87";

    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("bluetoothctl", "Bluetooth control (BlueZ bluetoothctl on Windows' Bluetooth stack)", a => MediaControl.WinRT(() => Bluetoothctl(a), "the Bluetooth service"));
        Commands.Add("hciconfig", "Show and power Bluetooth adapters", a => MediaControl.WinRT(() => Hciconfig(a), "the Bluetooth service"));
        Commands.Add("hcitool", "Bluetooth device discovery and information (BlueZ hcitool)", a => MediaControl.WinRT(() => Hcitool(a), "the Bluetooth service"));
        Commands.Add("rfcomm", "List the Bluetooth serial (RFCOMM) ports", Rfcomm);
        Commands.Add("sdptool", "Browse a Bluetooth device's services (SDP)", a => MediaControl.WinRT(() => Sdptool(a), "the Bluetooth service"));
        Commands.Add("btmon", "Bluetooth monitor (needs raw HCI access, which Windows doesn't offer)", Btmon);
        Commands.Add("bluemoon", "Broadcom Bluetooth configuration (needs raw HCI access)", Bluemoon);
    }

    static T Await<T>(Windows.Foundation.IAsyncOperation<T> op) => op.AsTask().GetAwaiter().GetResult();

    // BlueZ writes addresses as 00:1A:7D:DA:71:13.
    internal static string Address(ulong a) =>
        string.Join(":", Enumerable.Range(0, 6).Select(i => ((a >> (40 - 8 * i)) & 0xff).ToString("X2", CultureInfo.InvariantCulture)));

    internal static bool TryParseAddress(string text, out ulong address)
    {
        address = 0;
        var hex = text.Replace(":", "").Replace("-", "");
        return hex.Length == 12 && ulong.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out address);
    }

    sealed record Device(ulong Address, string Name, string Id, bool Paired, bool Connected, bool LowEnergy, uint Class);

    static BluetoothAdapter Adapter()
    {
        try { return Await(BluetoothAdapter.GetDefaultAsync()); }
        catch (COMException) { return null; }
    }

    static Radio AdapterRadio(BluetoothAdapter adapter)
    {
        try { return adapter == null ? null : Await(adapter.GetRadioAsync()); }
        catch (COMException) { return null; }
    }

    static List<Device> Devices(bool? paired)
    {
        var list = new List<Device>();
        void Add(string selector, bool le, bool isPaired)
        {
            foreach (var info in Await(DeviceInformation.FindAllAsync(selector)))
            {
                try
                {
                    if (le)
                    {
                        using var d = Await(BluetoothLEDevice.FromIdAsync(info.Id));
                        if (d == null || list.Any(x => x.Address == d.BluetoothAddress)) continue;
                        list.Add(new Device(d.BluetoothAddress, d.Name, info.Id, isPaired, d.ConnectionStatus == BluetoothConnectionStatus.Connected, true, 0));
                    }
                    else
                    {
                        using var d = Await(BluetoothDevice.FromIdAsync(info.Id));
                        if (d == null || list.Any(x => x.Address == d.BluetoothAddress)) continue;
                        list.Add(new Device(d.BluetoothAddress, d.Name, info.Id, isPaired, d.ConnectionStatus == BluetoothConnectionStatus.Connected, false, d.ClassOfDevice.RawValue));
                    }
                }
                catch (COMException) { }
            }
        }
        try
        {
            if (paired != false) { Add(BluetoothDevice.GetDeviceSelectorFromPairingState(true), false, true); Add(BluetoothLEDevice.GetDeviceSelectorFromPairingState(true), true, true); }
            if (paired != true) { Add(BluetoothDevice.GetDeviceSelectorFromPairingState(false), false, false); Add(BluetoothLEDevice.GetDeviceSelectorFromPairingState(false), true, false); }
        }
        catch (COMException) { }   // no Bluetooth stack
        return list;
    }

    // An address or a name; paired devices first (looking up unpaired ones means asking each of them).
    static Device Find(string text)
    {
        if (!TryParseAddress(text, out var address))
            return Devices(true).FirstOrDefault(d => d.Name.Equals(text, StringComparison.OrdinalIgnoreCase))
                ?? Devices(false).FirstOrDefault(d => d.Name.Equals(text, StringComparison.OrdinalIgnoreCase));
        var known = Devices(true).FirstOrDefault(d => d.Address == address);
        if (known != null) return known;
        try
        {
            using var d = Await(BluetoothDevice.FromBluetoothAddressAsync(address));
            if (d != null) return new Device(address, d.Name, d.DeviceId, d.DeviceInformation.Pairing.IsPaired, d.ConnectionStatus == BluetoothConnectionStatus.Connected, false, d.ClassOfDevice.RawValue);
            using var le = Await(BluetoothLEDevice.FromBluetoothAddressAsync(address));
            if (le != null) return new Device(address, le.Name, le.DeviceId, le.DeviceInformation.Pairing.IsPaired, le.ConnectionStatus == BluetoothConnectionStatus.Connected, true, 0);
        }
        catch (COMException) { }
        return null;
    }

    // Classic inquiry and LE advertisements for the given time; reports each new device as it appears.
    static void Discover(double seconds, Action<ulong, string, bool> found)
    {
        var seen = new HashSet<ulong>();
        var watcher = DeviceInformation.CreateWatcher(BluetoothDevice.GetDeviceSelectorFromPairingState(false), null, DeviceInformationKind.AssociationEndpoint);
        watcher.Added += (_, info) =>
        {
            try
            {
                using var d = Await(BluetoothDevice.FromIdAsync(info.Id));
                if (d != null) lock (seen) if (seen.Add(d.BluetoothAddress)) found(d.BluetoothAddress, d.Name, false);
            }
            catch (COMException) { }
        };
        watcher.Updated += (_, _) => { };
        var le = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };
        le.Received += (_, e) => { lock (seen) if (seen.Add(e.BluetoothAddress)) found(e.BluetoothAddress, e.Advertisement.LocalName, true); };
        watcher.Start();
        le.Start();
        Thread.Sleep(TimeSpan.FromSeconds(seconds));
        le.Stop();
        watcher.Stop();
    }

    // ---- bluetoothctl ---------------------------------------------------------------------------

    static int Bluetoothctl(string[] args)
    {
        var timeout = 5.0;
        var i = 0;
        for (; i < args.Length && args[i].StartsWith('-'); i++)
        {
            switch (args[i])
            {
                case "-v" or "--version": Console.WriteLine($"bluetoothctl: {Version}"); return 0;
                case "-h" or "--help": CtlHelp(); return 0;
                case "-t" or "--timeout":
                    if (++i == args.Length || !double.TryParse(args[i], CultureInfo.InvariantCulture, out timeout)) throw new UsageException("--timeout needs seconds");
                    break;
                case "-a" or "--agent" or "-e" or "--endpoints" or "-m" or "--monitor" or "-i" or "--init-script":
                    if (args[i] is "-a" or "--agent" or "-i" or "--init-script" && i + 1 < args.Length) i++;
                    break;
                default: throw new UsageException($"unrecognized option '{args[i]}'");
            }
        }
        if (i < args.Length) return CtlCommand(args[i..], timeout);

        // Interactive: one command per line, like bluetoothctl's shell.
        while (true)
        {
            if (!Console.IsInputRedirected) Console.Write("[bluetooth]# ");
            var line = Console.ReadLine();
            if (line == null) return 0;
            var words = line.Trim('﻿').Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) continue;
            if (words[0] is "quit" or "exit") return 0;
            try { CtlCommand(words, timeout); }
            catch (InvalidOperationException e) { Console.WriteLine(e.Message); }
            catch (UsageException e) { Console.WriteLine(e.Message); }
        }
    }

    static void CtlHelp() => Console.WriteLine("""
        bluetoothctl ver 5.87 (Windows Bluetooth stack)
        Usage:
        	bluetoothctl [--timeout seconds] [command [args]]

        Available commands:
          list                       List available controllers
          show [ctrl]                Controller information
          select <ctrl>              Select default controller
          devices [Paired/Connected] List available devices, with an optional property as the filter
          paired-devices             List paired devices
          info [dev]                 Device information
          power <on/off>             Set controller power
          scan <on/off>              Scan for devices (for --timeout seconds)
          pair <dev>                 Pair with device
          trust/untrust <dev>        Windows trusts every paired device
          connect <dev>              Connect device
          disconnect <dev>           Disconnect device
          remove <dev>               Remove (unpair) device
          version                    Display version
          quit                       Quit program
        """);

    static int CtlCommand(string[] w, double timeout)
    {
        string Arg(string what) => w.Length > 1 ? w[1] : throw new UsageException($"Missing {what} argument");
        var adapter = Adapter();
        switch (w[0])
        {
            case "help": CtlHelp(); return 0;
            case "version": Console.WriteLine($"Version {Version}"); return 0;
            case "list":
                if (adapter != null) Console.WriteLine($"Controller {Address(adapter.BluetoothAddress)} {Environment.MachineName} [default]");
                return 0;
            case "select" or "agent" or "default-agent" or "pairable" or "menu" or "back":
                return 0;
            case "show":
                if (adapter == null) { Console.WriteLine("No default controller available"); return 1; }
                var radio = AdapterRadio(adapter);
                Console.WriteLine($"""
                    Controller {Address(adapter.BluetoothAddress)} (public)
                    	Name: {Environment.MachineName}
                    	Alias: {Environment.MachineName}
                    	Powered: {YesNo(radio?.State == RadioState.On)}
                    	Pairable: yes
                    	Discovering: no
                    	Roles: {(adapter.IsCentralRoleSupported ? "central" : "")}{(adapter.IsPeripheralRoleSupported ? " peripheral" : "")}
                    	LE: {YesNo(adapter.IsLowEnergySupported)}
                    	BR/EDR: {YesNo(adapter.IsClassicSupported)}
                    """);
                return 0;
            case "devices" or "paired-devices":
                var filter = w[0] == "paired-devices" ? "Paired" : w.Length > 1 ? w[1] : null;
                foreach (var d in Devices(filter is "Paired" or "Bonded" or "Trusted" ? true : null))
                    if (filter != "Connected" || d.Connected) Console.WriteLine($"Device {Address(d.Address)} {d.Name}");
                return 0;
            case "info":
                var dev = Find(Arg("device")) ?? throw new InvalidOperationException($"Device {w[1]} not available");
                PrintInfo(dev);
                return 0;
            case "power":
                var on = Arg("on/off") is "on" or "yes";
                var r = AdapterRadio(adapter) ?? throw new InvalidOperationException("No default controller available");
                if (Await(Radio.RequestAccessAsync()) != RadioAccessStatus.Allowed || Await(r.SetStateAsync(on ? RadioState.On : RadioState.Off)) != RadioAccessStatus.Allowed)
                    throw new InvalidOperationException($"Failed to set power {(on ? "on" : "off")}");
                Console.WriteLine($"Changing power {(on ? "on" : "off")} succeeded");
                return 0;
            case "scan":
                if (Arg("on/off") is "off") { Console.WriteLine("Discovery stopped"); return 0; }
                Console.WriteLine("Discovery started");
                Discover(timeout, (address, name, le) =>
                    Console.WriteLine($"[NEW] Device {Address(address)} {(string.IsNullOrEmpty(name) ? Address(address).Replace(':', '-') : name)}"));
                Console.WriteLine("Discovery stopped");
                return 0;
            case "pair":
                return Pair(Find(Arg("device")) ?? throw new InvalidOperationException($"Device {w[1]} not available"));
            case "trust" or "untrust":
                var t = Find(Arg("device")) ?? throw new InvalidOperationException($"Device {w[1]} not available");
                Console.WriteLine(w[0] == "trust" ? $"Changing {Address(t.Address)} trust succeeded" : $"{w[0]}: Windows trusts every paired device; remove it to stop trusting it");
                return w[0] == "trust" ? 0 : 1;
            case "connect" or "disconnect":
                var c = Find(Arg("device")) ?? throw new InvalidOperationException($"Device {w[1]} not available");
                var code = Commands.Run("blueutil", [w[0] == "connect" ? "--connect" : "--disconnect", Address(c.Address).Replace(':', '-')]);
                Console.WriteLine(code == 0 ? (w[0] == "connect" ? "Connection successful" : "Successful disconnected") : $"Failed to {w[0]}");
                return code;
            case "remove":
                var gone = Find(Arg("device")) ?? throw new InvalidOperationException($"Device {w[1]} not available");
                var info = Await(DeviceInformation.CreateFromIdAsync(gone.Id));
                var result = Await(info.Pairing.UnpairAsync());
                if (result.Status is not (DeviceUnpairingResultStatus.Unpaired or DeviceUnpairingResultStatus.AlreadyUnpaired))
                    throw new InvalidOperationException($"Failed to remove device: {result.Status}");
                Console.WriteLine("Device has been removed");
                return 0;
            case "discoverable":
                Console.WriteLine("discoverable: Windows makes the PC discoverable while Settings > Bluetooth & devices is open");
                return 1;
            default:
                Console.WriteLine($"Invalid command in menu main: {w[0]}");
                return 1;
        }
    }

    static int Pair(Device device)
    {
        if (device.Paired) { Console.WriteLine("Failed to pair: org.bluez.Error.AlreadyExists"); return 1; }
        Console.WriteLine($"Attempting to pair with {Address(device.Address)}");
        var info = Await(DeviceInformation.CreateFromIdAsync(device.Id));
        var custom = info.Pairing.Custom;
        custom.PairingRequested += (_, request) =>
        {
            if (request.PairingKind is DevicePairingKinds.ConfirmPinMatch or DevicePairingKinds.DisplayPin)
                Console.WriteLine($"[agent] Passkey: {request.Pin}");
            if (request.PairingKind == DevicePairingKinds.ProvidePin)
            {
                Console.Write("[agent] Enter PIN code: ");
                request.Accept(Console.ReadLine()?.Trim() ?? "");
            }
            else request.Accept();
        };
        var result = Await(custom.PairAsync(DevicePairingKinds.ConfirmOnly | DevicePairingKinds.ProvidePin | DevicePairingKinds.ConfirmPinMatch | DevicePairingKinds.DisplayPin));
        if (result.Status is DevicePairingResultStatus.Paired or DevicePairingResultStatus.AlreadyPaired)
        {
            Console.WriteLine("Pairing successful");
            return 0;
        }
        Console.WriteLine($"Failed to pair: {result.Status}");
        return 1;
    }

    static void PrintInfo(Device d)
    {
        var sb = new StringBuilder($"Device {Address(d.Address)} (public)\n");
        sb.Append($"\tName: {d.Name}\n\tAlias: {d.Name}\n");
        if (d.Class != 0) sb.Append($"\tClass: 0x{d.Class:x8}\n\tIcon: {Icon(d.Class)}\n");
        sb.Append($"\tPaired: {YesNo(d.Paired)}\n\tBonded: {YesNo(d.Paired)}\n\tTrusted: {YesNo(d.Paired)}\n\tBlocked: no\n\tConnected: {YesNo(d.Connected)}\n");
        foreach (var uuid in ServiceUuids(d))
            sb.Append($"\tUUID: {(ServiceName(uuid) ?? "Vendor specific"),-26}({uuid})\n");
        Console.Write(sb.ToString());
    }

    static List<Guid> ServiceUuids(Device d)
    {
        var uuids = new List<Guid>();
        try
        {
            if (d.LowEnergy)
            {
                using var le = Await(BluetoothLEDevice.FromIdAsync(d.Id));
                var services = Await(le.GetGattServicesAsync(BluetoothCacheMode.Cached));
                uuids.AddRange(services.Services.Select(s => s.Uuid));
            }
            else
            {
                using var classic = Await(BluetoothDevice.FromIdAsync(d.Id));
                var services = Await(classic.GetRfcommServicesAsync(BluetoothCacheMode.Cached));
                uuids.AddRange(services.Services.Select(s => s.ServiceId.Uuid));
                uuids.AddRange(InstalledServices(d.Address).Where(g => !uuids.Contains(g)));
            }
        }
        catch (Exception e) when (e is COMException or UnauthorizedAccessException) { }
        return uuids.Distinct().ToList();
    }

    // Services Windows has enabled for a paired classic device, from the BTHENUM device tree.
    static List<Guid> InstalledServices(ulong address)
    {
        var list = new List<Guid>();
        var suffix = address.ToString("x12", CultureInfo.InvariantCulture);
        using var bthenum = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\BTHENUM");
        if (bthenum == null) return list;
        foreach (var name in bthenum.GetSubKeyNames())
        {
            if (!name.StartsWith('{') || !Guid.TryParse(name[..38], out var guid)) continue;
            using var key = bthenum.OpenSubKey(name);
            if (key?.GetSubKeyNames().Any(i => i.Contains(suffix, StringComparison.OrdinalIgnoreCase)) == true) list.Add(guid);
        }
        return list;
    }

    static readonly Dictionary<ushort, string> KnownServices = new()
    {
        [0x1101] = "Serial Port", [0x1103] = "Dialup Networking", [0x1105] = "OBEX Object Push", [0x1106] = "OBEX File Transfer",
        [0x1108] = "Headset", [0x110a] = "Audio Source", [0x110b] = "Audio Sink", [0x110c] = "A/V Remote Control Target",
        [0x110d] = "Advanced Audio Distribu...", [0x110e] = "A/V Remote Control", [0x110f] = "A/V Remote Control Cont...",
        [0x1112] = "Headset AG", [0x1115] = "PANU", [0x1116] = "NAP", [0x111e] = "Handsfree", [0x111f] = "Handsfree Audio Gateway",
        [0x1124] = "Human Interface Device", [0x112f] = "Phonebook Access Server", [0x1132] = "Message Access Server",
        [0x1200] = "PnP Information", [0x1203] = "Generic Audio", [0x1800] = "Generic Access Profile", [0x1801] = "Generic Attribute Profile",
        [0x180a] = "Device Information", [0x180f] = "Battery Service", [0x1812] = "Human Interface Device",
    };

    static readonly Guid BaseUuid = new("00000000-0000-1000-8000-00805f9b34fb");

    internal static string ServiceName(Guid uuid)
    {
        var bytes = uuid.ToByteArray();
        var b = BaseUuid.ToByteArray();
        // Bluetooth base UUID: only the first 4 bytes (Data1) differ.
        if (!bytes.AsSpan(4).SequenceEqual(b.AsSpan(4))) return null;
        var data1 = BitConverter.ToUInt32(bytes, 0);
        return data1 <= 0xffff && KnownServices.TryGetValue((ushort)data1, out var name) ? name : null;
    }

    // The Class of Device's major class, as BlueZ names the icon.
    internal static string Icon(uint cod) => ((cod >> 8) & 0x1f) switch
    {
        1 => "computer",
        2 => "phone",
        3 => "network-wireless",
        4 => ((cod >> 2) & 0x3f) is 1 or 2 ? "audio-headset" : ((cod >> 2) & 0x3f) == 6 ? "audio-headphones" : "audio-card",
        5 => ((cod >> 6) & 0x3) == 1 ? "input-keyboard" : ((cod >> 6) & 0x3) == 2 ? "input-mouse" : "input-gaming",
        6 => "camera-photo",
        _ => "",
    };

    static string YesNo(bool b) => b ? "yes" : "no";

    // ---- hciconfig / hcitool --------------------------------------------------------------------

    static int Hciconfig(string[] args)
    {
        var all = false;
        var rest = new List<string>();
        foreach (var a in args)
        {
            if (a is "-h" or "--help")
            {
                Console.WriteLine("hciconfig - HCI device configuration utility\nUsage:\n\thciconfig\n\thciconfig [-a] hciX [command ...]\nCommands:\n\tup\tOpen and initialize HCI device\n\tdown\tClose HCI device");
                return 0;
            }
            if (a == "-a" || a == "--all") all = true;
            else rest.Add(a);
        }
        var adapter = Adapter();
        if (rest.Count > 0 && rest[0] != "hci0")
        {
            Console.Error.WriteLine($"Can't get device info: No such device");
            return 1;
        }
        if (adapter == null) return rest.Count > 0 ? 1 : 0;
        var radio = AdapterRadio(adapter);
        if (rest.Count > 1)
        {
            var on = rest[1] switch { "up" => true, "down" => false, _ => throw new UsageException($"unknown command {rest[1]}") };
            if (radio == null || Await(radio.SetStateAsync(on ? RadioState.On : RadioState.Off)) != RadioAccessStatus.Allowed)
            {
                Console.Error.WriteLine($"Can't {(on ? "init" : "down")} device hci0: Operation not permitted");
                return 1;
            }
            return 0;
        }
        var up = radio?.State == RadioState.On;
        var bus = adapter.DeviceId.Contains("USB", StringComparison.OrdinalIgnoreCase) ? "USB"
            : adapter.DeviceId.Contains("PCI", StringComparison.OrdinalIgnoreCase) ? "PCI" : "UART";
        var sb = new StringBuilder();
        sb.Append($"hci0:\tType: Primary  Bus: {bus}\n");
        sb.Append($"\tBD Address: {Address(adapter.BluetoothAddress)}  ACL MTU: 1021:8  SCO MTU: 64:1\n");
        sb.Append(up ? "\tUP RUNNING PSCAN \n" : "\tDOWN \n");
        if (all)
        {
            sb.Append($"\tName: '{Environment.MachineName}'\n");
            sb.Append($"\tFeatures: BR/EDR {YesNo(adapter.IsClassicSupported)}  LE {YesNo(adapter.IsLowEnergySupported)}  ");
            sb.Append($"Central {YesNo(adapter.IsCentralRoleSupported)}  Peripheral {YesNo(adapter.IsPeripheralRoleSupported)}\n");
            sb.Append($"\tMax advertisement data: {adapter.MaxAdvertisementDataLength} bytes\n");
        }
        Console.WriteLine(sb.ToString());
        return 0;
    }

    static int Hcitool(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            Console.WriteLine("""
                hcitool - HCI Tool ver 5.87
                Usage:
                	hcitool [options] <command> [command parameters]
                Commands:
                	dev 	Display local devices
                	scan	Scan for remote devices
                	lescan	Start LE scan
                	name	Get name from remote device
                	info	Get information from remote device
                	con 	Display active connections
                """);
            return args.Length == 0 ? 1 : 0;
        }
        var rest = args.Where(a => !(a is "-i" or "hci0")).ToList();
        var adapter = Adapter();
        switch (rest[0])
        {
            case "dev":
                Console.WriteLine("Devices:");
                if (adapter != null) Console.WriteLine($"\thci0\t{Address(adapter.BluetoothAddress)}");
                return 0;
            case "scan" or "inq":
                if (adapter == null) { Console.Error.WriteLine("Device is not available: No such device"); return 1; }
                Console.WriteLine("Scanning ...");
                Discover(10, (address, name, le) => { if (!le) Console.WriteLine($"\t{Address(address)}\t{(string.IsNullOrEmpty(name) ? "n/a" : name)}"); });
                return 0;
            case "lescan":
                if (adapter == null) { Console.Error.WriteLine("Could not open device: No such device"); return 1; }
                var duration = rest.FirstOrDefault(a => a.StartsWith("--duration="))?[11..];
                Console.WriteLine("LE Scan ...");
                Discover(double.TryParse(duration, CultureInfo.InvariantCulture, out var s) ? s : 10,
                    (address, name, le) => { if (le) Console.WriteLine($"{Address(address)} {(string.IsNullOrEmpty(name) ? "(unknown)" : name)}"); });
                return 0;
            case "name" or "info":
                if (rest.Count < 2) throw new UsageException($"{rest[0]} needs a <bdaddr>");
                var d = Find(rest[1]);
                if (d == null) { Console.Error.WriteLine($"Can't create connection: Input/output error"); return 1; }
                if (rest[0] == "name") Console.WriteLine(d.Name);
                else
                {
                    Console.WriteLine($"Requesting information ...\n\tBD Address:  {Address(d.Address)}\n\tDevice Name: {d.Name}");
                    if (d.Class != 0) Console.WriteLine($"\tClass: 0x{d.Class:x6}");
                }
                return 0;
            case "con":
                Console.WriteLine("Connections:");
                foreach (var c in Devices(true).Where(x => x.Connected))
                    Console.WriteLine($"\t< {(c.LowEnergy ? "LE" : "ACL")} {Address(c.Address)} handle 0 state 1 lm CENTRAL");
                return 0;
            default:
                throw new UsageException($"Unknown command - \"{rest[0]}\"");
        }
    }

    // ---- rfcomm / sdptool -----------------------------------------------------------------------

    // The COM ports Windows created for paired devices' Serial Port services (BTHENUM\{00001101-...}).
    static int Rfcomm(string[] args)
    {
        if (args.Length > 0 && args[0] is "-h" or "--help")
        {
            Console.WriteLine("RFCOMM configuration utility ver 5.87\nUsage:\n\trfcomm [-a]\n\nWindows creates a COM port per paired device's Serial Port service; they are listed here.");
            return 0;
        }
        var command = args.FirstOrDefault(a => !a.StartsWith('-')) ?? "show";
        if (command is not ("show" or "list"))
        {
            Console.Error.WriteLine($"rfcomm: {command}: Windows binds Bluetooth serial ports itself (Settings > Bluetooth & devices > More Bluetooth settings > COM Ports)");
            return 1;
        }
        foreach (var (port, address, outgoing) in SerialPorts())
            Console.WriteLine($"{port}: {(address == 0 ? "00:00:00:00:00:00" : Address(address))} channel 1 {(outgoing ? "clean" : "listen")} ");
        return 0;
    }

    internal static List<(string Port, ulong Address, bool Outgoing)> SerialPorts()
    {
        var list = new List<(string, ulong, bool)>();
        using var bthenum = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\BTHENUM");
        if (bthenum == null) return list;
        foreach (var name in bthenum.GetSubKeyNames().Where(n => n.StartsWith("{00001101-", StringComparison.OrdinalIgnoreCase)))
        {
            using var service = bthenum.OpenSubKey(name);
            if (service == null) continue;
            foreach (var instance in service.GetSubKeyNames())
            {
                using var parameters = service.OpenSubKey(instance + @"\Device Parameters");
                if (parameters?.GetValue("PortName") is not string port) continue;
                // Instance ids end in "&<address>_C00000000" (outgoing) or "&000000000000_..." (incoming).
                var amp = instance.LastIndexOf('&');
                var hex = amp >= 0 && instance.Length >= amp + 13 ? instance.Substring(amp + 1, 12) : "";
                ulong.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var address);
                list.Add((port, address, address != 0));
            }
        }
        return list;
    }

    static int Sdptool(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            Console.WriteLine("sdptool - SDP tool v5.87\nUsage:\n\tsdptool [options] <command> [command parameters]\nCommands:\n\tbrowse\tBrowse all available services\n\trecords\tRequest all records");
            return args.Length == 0 ? 1 : 0;
        }
        if (args[0] is not ("browse" or "records")) throw new UsageException($"unknown command {args[0]}");
        var target = args.Skip(1).FirstOrDefault(a => !a.StartsWith('-'));
        var devices = target == null || target == "local" ? Devices(true).Where(d => !d.LowEnergy).ToList()
            : [Find(target) ?? throw new InvalidOperationException($"Failed to connect to SDP server on {target}: No such device")];
        foreach (var device in devices)
        {
            Console.WriteLine($"Browsing {Address(device.Address)} ...");
            using var classic = Await(BluetoothDevice.FromIdAsync(device.Id));
            if (classic == null) continue;
            var services = Await(classic.GetRfcommServicesAsync(BluetoothCacheMode.Uncached));
            foreach (var service in services.Services)
            {
                IReadOnlyDictionary<uint, IBuffer> attributes;
                try { attributes = Await(service.GetSdpRawAttributesAsync(BluetoothCacheMode.Cached)); }
                catch (COMException) { attributes = new Dictionary<uint, IBuffer>(); }
                var uuid = service.ServiceId.Uuid;
                var name = attributes.TryGetValue(0x0100, out var nameBuffer) ? SdpText(Bytes(nameBuffer)) : null;
                Console.WriteLine($"Service Name: {name ?? ServiceName(uuid) ?? uuid.ToString()}");
                Console.WriteLine("Service Class ID List:");
                Console.WriteLine($"  \"{ServiceName(uuid) ?? uuid.ToString()}\" (0x{ShortUuid(uuid)})");
                Console.WriteLine("Protocol Descriptor List:");
                Console.WriteLine("  \"L2CAP\" (0x0100)");
                Console.WriteLine("  \"RFCOMM\" (0x0003)");
                if (attributes.TryGetValue(0x0004, out var protocols) && RfcommChannel(Bytes(protocols)) is { } channel)
                    Console.WriteLine($"    Channel: {channel}");
                Console.WriteLine();
            }
        }
        return 0;
    }

    static byte[] Bytes(IBuffer buffer)
    {
        var data = new byte[buffer.Length];
        using var reader = DataReader.FromBuffer(buffer);
        reader.ReadBytes(data);
        return data;
    }

    static string ShortUuid(Guid uuid) => BitConverter.ToUInt32(uuid.ToByteArray(), 0).ToString(uuid.ToByteArray()[2] == 0 && uuid.ToByteArray()[3] == 0 ? "x4" : "x8", CultureInfo.InvariantCulture);

    // SDP data elements: a header byte (type << 3 | size index), then the size, then the data.
    internal static (int Type, int Offset, int Length) SdpElement(byte[] d, int at)
    {
        var type = d[at] >> 3;
        var index = d[at] & 7;
        return index switch
        {
            < 5 => (type, at + 1, type == 0 ? 0 : 1 << index),
            5 => (type, at + 2, d[at + 1]),
            6 => (type, at + 3, (d[at + 1] << 8) | d[at + 2]),
            _ => (type, at + 5, (d[at + 1] << 24) | (d[at + 2] << 16) | (d[at + 3] << 8) | d[at + 4]),
        };
    }

    internal static string SdpText(byte[] d)
    {
        if (d.Length == 0) return null;
        var (type, offset, length) = SdpElement(d, 0);
        return type == 4 && offset + length <= d.Length ? Encoding.UTF8.GetString(d, offset, length).TrimEnd('\0') : null;
    }

    // ProtocolDescriptorList: sequence of sequences; the RFCOMM one (UUID 0x0003) carries the channel.
    internal static int? RfcommChannel(byte[] d)
    {
        if (d.Length == 0) return null;
        var (type, offset, length) = SdpElement(d, 0);
        if (type != 6) return null;
        var end = Math.Min(d.Length, offset + length);
        for (var at = offset; at < end;)
        {
            var (itype, ioffset, ilength) = SdpElement(d, at);
            if (itype == 6 && ilength >= 3)
            {
                var (utype, uoffset, ulength) = SdpElement(d, ioffset);
                if (utype == 3 && ulength == 2 && ((d[uoffset] << 8) | d[uoffset + 1]) == 0x0003)
                {
                    var (ctype, coffset, _) = SdpElement(d, uoffset + ulength);
                    if (ctype == 1) return d[coffset];
                }
            }
            at = ioffset + ilength;
        }
        return null;
    }

    // ---- btmon / bluemoon -----------------------------------------------------------------------

    static int Btmon(string[] args)
    {
        if (args.Length > 0 && args[0] is "-h" or "--help") { Console.WriteLine("btmon - Bluetooth monitor ver 5.87\nUsage:\n\tbtmon [options]"); return 0; }
        if (args.Length > 0 && args[0] is "-v" or "--version") { Console.WriteLine(Version); return 0; }
        Console.Error.WriteLine("Failed to open channel: Windows doesn't let applications monitor raw HCI traffic (capture it with an ETW Bluetooth trace instead)");
        return 1;
    }

    static int Bluemoon(string[] args)
    {
        if (args.Length > 0 && args[0] is "-h" or "--help") { Console.WriteLine("bluemoon - Broadcom Bluetooth configuration utility ver 5.87"); return 0; }
        Console.Error.WriteLine("Failed to open HCI user channel: Windows doesn't give applications raw HCI access");
        return 1;
    }
}
