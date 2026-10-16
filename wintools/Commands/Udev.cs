using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// libgudev's device queries and umockdev's recorder/testbeds, on the Windows Plug and Play tree.
// Each present devnode becomes a udev device: a sysfs-style DEVPATH from its ancestors, a subsystem
// from its setup class or bus (pci, usb, hid, net, block, tty, input, sound...), udev properties
// (DRIVER, ID_VENDOR_ID, PCI_ID, MODALIAS...) and sysfs-style attributes (idVendor, vendor, class...).
// gudev-query lists and monitors them the way GUdevClient does; umockdev-record writes them in
// umockdev's format; umockdev-run/umockdev-wrapper build a testbed directory ($UMOCKDEV_DIR) from
// recordings and run a program with it.
static unsafe class Udev
{
    const string UmockdevVersion = "0.19.9";

    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("gudev-query", "Query and monitor devices the way GUdevClient does", Query);
        Commands.Add("umockdev-record", "Record devices in umockdev's format", Record);
        Commands.Add("umockdev-run", "Run a program with an umockdev testbed of recorded devices", RunTestbed);
        Commands.Add("umockdev-wrapper", "Run a program with an empty umockdev testbed", Wrapper);
    }

    sealed class Device
    {
        public string InstanceId, Parent, Class, Enumerator, Description, FriendlyName, Manufacturer, Service, Location;
        public string[] HardwareIds = [], CompatibleIds = [];
        public uint Bus = uint.MaxValue, Address = uint.MaxValue;
        public string DevPath, Subsystem, DevType, Name;
        public SortedDictionary<string, string> Properties = new(StringComparer.Ordinal);
        public SortedDictionary<string, string> Attributes = new(StringComparer.Ordinal);
    }

    // ---- the device tree ----

    static List<Device> LoadDevices()
    {
        var byId = new Dictionary<string, Device>(StringComparer.OrdinalIgnoreCase);
        var set = SetupDiGetClassDevsW(IntPtr.Zero, null, IntPtr.Zero, DIGCF_ALLCLASSES | DIGCF_PRESENT);
        if (set == INVALID_HANDLE_VALUE) return [];
        try
        {
            var info = new SP_DEVINFO_DATA { cbSize = (uint)sizeof(SP_DEVINFO_DATA) };
            for (uint i = 0; SetupDiEnumDeviceInfo(set, i, ref info); i++)
            {
                var d = new Device { InstanceId = DeviceId(info.DevInst) };
                if (d.InstanceId == null) continue;
                if (CM_Get_Parent(out var parent, info.DevInst, 0) == 0) d.Parent = DeviceId(parent);
                d.Class = RegistryString(set, ref info, SPDRP_CLASS);
                d.Enumerator = RegistryString(set, ref info, SPDRP_ENUMERATOR_NAME) ?? d.InstanceId.Split('\\')[0];
                d.Description = RegistryString(set, ref info, SPDRP_DEVICEDESC);
                d.FriendlyName = RegistryString(set, ref info, SPDRP_FRIENDLYNAME);
                d.Manufacturer = RegistryString(set, ref info, SPDRP_MFG);
                d.Service = RegistryString(set, ref info, SPDRP_SERVICE);
                d.Location = RegistryString(set, ref info, SPDRP_LOCATION_INFORMATION);
                d.HardwareIds = RegistryStrings(set, ref info, SPDRP_HARDWAREID);
                d.CompatibleIds = RegistryStrings(set, ref info, SPDRP_COMPATIBLEIDS);
                if (RegistryDword(set, ref info, SPDRP_BUSNUMBER) is { } bus) d.Bus = bus;
                if (RegistryDword(set, ref info, SPDRP_ADDRESS) is { } address) d.Address = address;
                byId[d.InstanceId] = d;
            }
        }
        finally { SetupDiDestroyDeviceInfoList(set); }

        foreach (var d in byId.Values) Describe(d, byId);
        // The root devnode is the tree itself, not a device.
        return byId.Values.Where(d => !d.InstanceId.StartsWith(@"HTREE\", StringComparison.OrdinalIgnoreCase))
            .OrderBy(d => d.DevPath, StringComparer.Ordinal).ToList();
    }

    static void Describe(Device d, Dictionary<string, Device> byId)
    {
        // DEVPATH: the chain of ancestors below the root devnode, like /sys/devices.
        var chain = new List<Device>();
        for (var cur = d; cur != null && chain.Count < 64; cur = cur.Parent != null ? byId.GetValueOrDefault(cur.Parent) : null)
        {
            if (cur.InstanceId.StartsWith(@"HTREE\ROOT", StringComparison.OrdinalIgnoreCase)) break;
            chain.Add(cur);
        }
        chain.Reverse();
        d.Name = Segment(d);
        d.DevPath = "/devices/" + string.Join("/", chain.Select(Segment));
        var cls = (d.Class ?? "").ToLowerInvariant();
        var bus = d.Enumerator.ToLowerInvariant();
        d.Subsystem = cls switch
        {
            "net" => "net",
            "diskdrive" or "cdrom" or "volume" => "block",
            "ports" => "tty",
            "keyboard" or "mouse" => "input",
            "media" or "audioendpoint" => "sound",
            "camera" or "image" => "video4linux",
            "battery" => "power_supply",
            "bluetooth" => "bluetooth",
            "monitor" => "drm",
            _ => bus switch
            {
                "pci" => "pci",
                "usb" or "usbstor" => "usb",
                "hid" => "hid",
                "acpi" or "acpi_hal" => "acpi",
                "scsi" or "storage" => "scsi",
                "root" or "swd" or "sw" => "platform",
                _ when bus.StartsWith('{') => "platform",
                _ => bus,
            },
        };
        var hw = d.HardwareIds.FirstOrDefault() ?? "";
        var p = d.Properties;
        p["DEVPATH"] = d.DevPath;
        p["SUBSYSTEM"] = d.Subsystem;
        if (d.Service != null) p["DRIVER"] = d.Service;
        if (d.Manufacturer != null) p["ID_VENDOR_FROM_DATABASE"] = d.Manufacturer;
        if ((d.FriendlyName ?? d.Description) is { } model) p["ID_MODEL_FROM_DATABASE"] = model;
        if (d.Location != null) p["ID_PATH"] = d.Location;
        if (d.Class != null) p["WINDOWS_CLASS"] = d.Class;
        p["WINDOWS_INSTANCE_ID"] = d.InstanceId;

        var a = d.Attributes;
        if (bus == "pci" && Hex(hw, "VEN_", 4) is { } ven && Hex(hw, "DEV_", 4) is { } dev)
        {
            var subsys = Hex(hw, "SUBSYS_", 8) ?? 0;
            var cc = d.CompatibleIds.Select(c => Hex(c, "CC_", 6)).FirstOrDefault(c => c != null) ?? 0;
            var rev = Hex(hw, "REV_", 2) ?? 0;
            p["PCI_ID"] = $"{ven:X4}:{dev:X4}";
            p["PCI_SUBSYS_ID"] = $"{subsys & 0xffff:X4}:{subsys >> 16:X4}";
            p["PCI_CLASS"] = $"{cc:X}";
            if (d.Bus != uint.MaxValue && d.Address != uint.MaxValue) p["PCI_SLOT_NAME"] = d.Name;
            p["MODALIAS"] = $"pci:v{ven:X8}d{dev:X8}sv{subsys & 0xffff:X8}sd{subsys >> 16:X8}bc{cc >> 16:X2}sc{(cc >> 8) & 0xff:X2}i{cc & 0xff:X2}";
            a["vendor"] = $"0x{ven:x4}";
            a["device"] = $"0x{dev:x4}";
            a["subsystem_vendor"] = $"0x{subsys & 0xffff:x4}";
            a["subsystem_device"] = $"0x{subsys >> 16:x4}";
            a["class"] = $"0x{cc:x6}";
            a["revision"] = $"0x{rev:x2}";
        }
        else if (bus == "usb" && Hex(hw, "VID_", 4) is { } vid && Hex(hw, "PID_", 4) is { } pid)
        {
            var iface = Hex(hw, "MI_", 2);
            var rev = Hex(hw, "REV_", 4);
            d.DevType = iface == null ? "usb_device" : "usb_interface";
            p["DEVTYPE"] = d.DevType;
            p["ID_VENDOR_ID"] = $"{vid:x4}";
            p["ID_MODEL_ID"] = $"{pid:x4}";
            p["PRODUCT"] = $"{vid:x}/{pid:x}/{rev ?? 0:x}";
            p["MODALIAS"] = $"usb:v{vid:X4}p{pid:X4}d{rev ?? 0:X4}";
            // The last part of a USB instance id is the serial number when it has no '&'.
            var serial = d.InstanceId.Split('\\').Last();
            if (iface == null && !serial.Contains('&')) { p["ID_SERIAL_SHORT"] = serial; a["serial"] = serial; }
            if (iface == null)
            {
                a["idVendor"] = $"{vid:x4}";
                a["idProduct"] = $"{pid:x4}";
                if (rev != null) a["bcdDevice"] = $"{rev:x4}";
                if (d.Manufacturer != null) a["manufacturer"] = d.Manufacturer;
                if (d.Description != null) a["product"] = d.Description;
            }
            else a["bInterfaceNumber"] = $"{iface:x2}";
        }
        else if (bus == "hid" && Hex(hw, "VID_", 4) is { } hvid && Hex(hw, "PID_", 4) is { } hpid)
        {
            p["HID_ID"] = $"0003:{hvid:X8}:{hpid:X8}";
            if (d.Description != null) p["HID_NAME"] = d.Description;
            p["MODALIAS"] = $"hid:b0003g0001v{hvid:X8}p{hpid:X8}";
        }
        else if (d.CompatibleIds.Length > 0 || d.HardwareIds.Length > 0)
        {
            p["MODALIAS"] = $"{bus}:{hw}";
        }
    }

    // A path component: PCI functions as 0000:bb:dd.f, everything else its instance id with '\' as '!'.
    static string Segment(Device d)
    {
        if (d.Enumerator.Equals("PCI", StringComparison.OrdinalIgnoreCase) && d.Bus != uint.MaxValue && d.Address != uint.MaxValue)
            return $"0000:{d.Bus:x2}:{(d.Address >> 16) & 0x1f:x2}.{d.Address & 0x7:x}";
        return d.InstanceId.Replace('\\', '!');
    }

    static uint? Hex(string s, string tag, int digits)
    {
        var at = s.IndexOf(tag, StringComparison.OrdinalIgnoreCase);
        if (at < 0) return null;
        at += tag.Length;
        var end = at;
        while (end < s.Length && end - at < digits && Uri.IsHexDigit(s[end])) end++;
        return end > at ? Convert.ToUInt32(s[at..end], 16) : null;
    }

    static string DeviceId(uint devinst)
    {
        var buf = new char[MAX_DEVICE_ID_LEN];
        fixed (char* p = buf)
            return CM_Get_Device_IDW(devinst, p, MAX_DEVICE_ID_LEN, 0) == 0 ? new string(p) : null;
    }

    static string RegistryString(IntPtr set, ref SP_DEVINFO_DATA info, uint prop) => RegistryStrings(set, ref info, prop).FirstOrDefault();

    static string[] RegistryStrings(IntPtr set, ref SP_DEVINFO_DATA info, uint prop)
    {
        var buf = new byte[4096];
        fixed (byte* p = buf)
        {
            if (!SetupDiGetDeviceRegistryPropertyW(set, ref info, prop, out _, p, (uint)buf.Length - 4, out var size)) return [];
            var text = new string((char*)p, 0, (int)size / 2);
            return text.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        }
    }

    static uint? RegistryDword(IntPtr set, ref SP_DEVINFO_DATA info, uint prop)
    {
        uint value = 0;
        return SetupDiGetDeviceRegistryPropertyW(set, ref info, prop, out _, (byte*)&value, 4, out _) ? value : null;
    }

    static string SysPath(Device d) => "/sys" + d.DevPath;

    // ---- gudev-query ----

    static int Query(string[] args)
    {
        var subsystems = new List<string>();
        string name = null, sysfs = null, property = null;
        bool monitor = false, brief = false;
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Value() => ++i < args.Length ? args[i] : throw new UsageException($"{a} needs a value");
            switch (a)
            {
                case "-h" or "--help":
                    Console.WriteLine("Usage: gudev-query [options]\n\nList devices as GUdevClient reports them (GUdevDevice fields, properties and\nsysfs attributes), optionally waiting for uevents.\n\n" +
                                      "  -s, --subsystem SUBSYSTEM[/DEVTYPE]  Only this subsystem (repeatable), as g_udev_client_query_by_subsystem\n" +
                                      "  -n, --name NAME                      Only devices with this name\n  -p, --sysfs-path PATH                Only the device at this sysfs path\n" +
                                      "  -P, --property KEY=VALUE             Only devices with this property\n  -b, --brief                          One line per device\n" +
                                      "  -m, --monitor                        Print uevents (add, bind, remove) until Ctrl+C\n      --version                        Show the libgudev version");
                    return 0;
                case "--version": Console.WriteLine("libgudev 238"); return 0;
                case "-s" or "--subsystem": subsystems.Add(Value()); break;
                case "-n" or "--name": name = Value(); break;
                case "-p" or "--sysfs-path": sysfs = Value(); break;
                case "-P" or "--property": property = Value(); break;
                case "-b" or "--brief": brief = true; break;
                case "-m" or "--monitor": monitor = true; break;
                default: throw new UsageException($"unknown option {a}");
            }
        }
        bool Matches(Device d)
        {
            if (subsystems.Count > 0 && !subsystems.Any(s =>
                {
                    var parts = s.Split('/', 2);
                    return parts[0] == d.Subsystem && (parts.Length == 1 || parts[1] == d.DevType);
                })) return false;
            if (name != null && name != d.Name) return false;
            if (sysfs != null && sysfs != SysPath(d) && sysfs != d.DevPath) return false;
            if (property != null)
            {
                var kv = property.Split('=', 2);
                if (!d.Properties.TryGetValue(kv[0], out var v) || (kv.Length == 2 && v != kv[1])) return false;
            }
            return true;
        }
        if (monitor) return Monitor(Matches, brief);
        foreach (var d in LoadDevices().Where(Matches))
        {
            if (brief) Console.WriteLine($"{d.Subsystem,-12} {SysPath(d)}");
            else PrintDevice(d, null);
        }
        return 0;
    }

    static void PrintDevice(Device d, string action)
    {
        var o = new StringBuilder();
        o.Append($"Initialized: true\nSubsystem: {d.Subsystem}\nDevtype: {d.DevType ?? "(null)"}\nName: {d.Name}\n");
        var number = new string(d.Name.Reverse().TakeWhile(char.IsAsciiDigit).Reverse().ToArray());
        o.Append($"Number: {(number.Length > 0 ? number : "(null)")}\nSysfs path: {SysPath(d)}\nDriver: {d.Service ?? "(null)"}\n");
        o.Append($"Action: {action ?? "(null)"}\nSeqnum: 0\nDevice type: none\nDevice number: 0:0\nDevice file: (null)\nDevice file symlinks:\n");
        o.Append("Properties:\n");
        foreach (var (k, v) in d.Properties) o.Append($"  {k}={v}\n");
        o.Append("Sysfs attributes:\n");
        foreach (var (k, v) in d.Attributes) o.Append($"  {k}={v}\n");
        o.Append('\n');
        Console.Write(o.ToString());
    }

    // uevents from CM_Register_Notification: every device instance enumerated, started or removed.
    static readonly System.Collections.Concurrent.BlockingCollection<(int Action, string Id)> Events = new();

    static int Monitor(Func<Device, bool> matches, bool brief)
    {
        var filter = stackalloc byte[416];
        new Span<byte>(filter, 416).Clear();
        *(uint*)filter = 416;                                   // cbSize
        *(uint*)(filter + 4) = CM_NOTIFY_FILTER_FLAG_ALL_DEVICE_INSTANCES;
        *(uint*)(filter + 8) = CM_NOTIFY_FILTER_TYPE_DEVICEINSTANCE;
        delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int, IntPtr, uint, uint> callback = &OnNotify;
        var cr = CM_Register_Notification(filter, IntPtr.Zero, (IntPtr)callback, out var handle);
        if (cr != 0) throw new InvalidOperationException($"could not watch devices (CONFIGRET {cr})");
        Console.Error.WriteLine("monitoring device events; Ctrl+C stops");
        using var stop = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
        var known = LoadDevices().ToDictionary(d => d.Properties["WINDOWS_INSTANCE_ID"], StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var (action, id) in Events.GetConsumingEnumerable(stop.Token))
            {
                var verb = action switch { CM_NOTIFY_ACTION_DEVICEINSTANCEENUMERATED => "add", CM_NOTIFY_ACTION_DEVICEINSTANCESTARTED => "bind", _ => "remove" };
                if (verb != "remove") known = LoadDevices().ToDictionary(d => d.Properties["WINDOWS_INSTANCE_ID"], StringComparer.OrdinalIgnoreCase);
                if (!known.TryGetValue(id, out var d) || !matches(d)) continue;
                if (brief) Console.WriteLine($"UDEV  [{Environment.TickCount64 / 1000.0:0.000000}] {verb,-8} {d.DevPath} ({d.Subsystem})");
                else PrintDevice(d, verb);
            }
        }
        catch (OperationCanceledException) { }
        finally { CM_Unregister_Notification(handle); }
        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    static uint OnNotify(IntPtr notify, IntPtr context, int action, IntPtr data, uint size)
    {
        if (action is CM_NOTIFY_ACTION_DEVICEINSTANCEENUMERATED or CM_NOTIFY_ACTION_DEVICEINSTANCESTARTED or CM_NOTIFY_ACTION_DEVICEINSTANCEREMOVED)
            Events.Add((action, new string((char*)(data + 8))));
        return 0;
    }

    // ---- umockdev-record ----

    static int Record(string[] args)
    {
        bool all = false;
        var wanted = new List<string>();
        foreach (var a in args)
        {
            switch (a)
            {
                case "-h" or "--help":
                    Console.WriteLine("Usage:\n  umockdev-record [OPTION...] [DEVICE...] - record Linux-style device descriptions of Windows devices\n\n" +
                                      "  -a, --all       Record all devices\n      --version   Show the umockdev version\n\n" +
                                      "DEVICE is a sysfs path (/sys/devices/...), a DEVPATH, or a Windows device instance id; each\n" +
                                      "device is written with its parents. ioctl and evdev recording (--ioctl, --script,\n" +
                                      "--evemu-events) need Linux device nodes and aren't available.");
                    return 0;
                case "--version": Console.WriteLine(UmockdevVersion); return 0;
                case "-a" or "--all": all = true; break;
                case "-i" or "--ioctl" or "-s" or "--script" or "-e" or "--evemu-events":
                    Console.Error.WriteLine($"umockdev-record: {a} records Linux device-node traffic, which Windows devices don't have");
                    return 1;
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unknown option {a}");
                    wanted.Add(a);
                    break;
            }
        }
        if (!all && wanted.Count == 0) throw new UsageException("give devices or --all (see --help)");
        var devices = LoadDevices();
        var byPath = devices.ToDictionary(d => d.DevPath, StringComparer.Ordinal);
        var output = new List<Device>();
        if (all) output.AddRange(devices.OrderByDescending(d => d.DevPath, StringComparer.Ordinal));
        else
        {
            foreach (var w in wanted)
            {
                var d = devices.FirstOrDefault(x => SysPath(x) == w || x.DevPath == w || x.Properties["WINDOWS_INSTANCE_ID"].Equals(w, StringComparison.OrdinalIgnoreCase));
                if (d == null) { Console.Error.WriteLine($"Invalid device {w}, has no sysfs path"); return 1; }
                // The device, then its parents up the tree.
                for (var path = d.DevPath; path.Length > "/devices".Length; path = path[..path.LastIndexOf('/')])
                    if (byPath.TryGetValue(path, out var p) && !output.Contains(p)) output.Add(p);
            }
        }
        var o = new StringBuilder();
        foreach (var d in output)
        {
            o.Append($"P: {d.DevPath}\n");
            foreach (var (k, v) in d.Properties.Where(kv => kv.Key != "DEVPATH")) o.Append($"E: {k}={v}\n");
            foreach (var (k, v) in d.Attributes) o.Append($"A: {k}={Escape(v)}\n");
            if (d.Service != null) o.Append($"L: driver=../../../bus/{d.Subsystem}/drivers/{d.Service}\n");
            o.Append('\n');
        }
        Console.Write(o.ToString());
        return 0;
    }

    static string Escape(string v) => v.Replace("\\", "\\\\").Replace("\n", "\\n");

    // ---- umockdev-run / umockdev-wrapper ----

    static int RunTestbed(string[] args)
    {
        var recordings = new List<string>();
        var command = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a == "--") { command.AddRange(args[(i + 1)..]); break; }
            switch (a)
            {
                case "-h" or "--help":
                    Console.WriteLine("Usage:\n  umockdev-run [OPTION...] -- program [args..] - run a program under an umockdev testbed\n\n" +
                                      "  -d, --device FILE   Load a device description file into the testbed (repeatable)\n      --version       Show the umockdev version\n\n" +
                                      "The testbed is a directory tree (sys/devices/..., uevent and attribute files, dev/) passed to\n" +
                                      "the program as $UMOCKDEV_DIR. Windows has no LD_PRELOAD, so only programs that read the\n" +
                                      "testbed through $UMOCKDEV_DIR see the mocked devices.");
                    return 0;
                case "--version": Console.WriteLine(UmockdevVersion); return 0;
                case "-d" or "--device": recordings.Add(++i < args.Length ? args[i] : throw new UsageException("-d needs a file")); break;
                case "-i" or "--ioctl" or "-s" or "--script" or "-p" or "--pcap" or "-e" or "--evemu-events" or "-u" or "--unix-stream":
                    Console.Error.WriteLine($"umockdev-run: {a} replays Linux device-node traffic, which Windows programs don't use");
                    return 1;
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unknown option {a}");
                    command.AddRange(args[i..]);
                    i = args.Length;
                    break;
            }
        }
        if (command.Count == 0) throw new UsageException("no program given (see --help)");
        return WithTestbed(recordings, command);
    }

    static int Wrapper(string[] args)
    {
        if (args.Length == 0) throw new UsageException("usage: umockdev-wrapper program [args...]");
        if (args[0] is "-h" or "--help")
        {
            Console.WriteLine("Usage: umockdev-wrapper program [args...]\nRun a program with an empty umockdev testbed in $UMOCKDEV_DIR.");
            return 0;
        }
        return WithTestbed([], args.ToList());
    }

    static int WithTestbed(List<string> recordings, List<string> command)
    {
        var root = Path.Combine(Path.GetTempPath(), "umockdev." + Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(root, "sys", "devices"));
        Directory.CreateDirectory(Path.Combine(root, "dev"));
        try
        {
            foreach (var file in recordings) LoadRecording(root, File.ReadAllText(file), file);
            var psi = new System.Diagnostics.ProcessStartInfo(command[0]) { UseShellExecute = false };
            foreach (var a in command.Skip(1)) psi.ArgumentList.Add(a);
            psi.Environment["UMOCKDEV_DIR"] = root;
            using var p = System.Diagnostics.Process.Start(psi);
            p.WaitForExit();
            return p.ExitCode;
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }

    // A recording's P:/E:/A:/H:/N:/S:/L: lines become sys/devices/<path>/{uevent,<attr>} and dev/ entries.
    static void LoadRecording(string root, string text, string file)
    {
        string dir = null;
        var uevent = new StringBuilder();
        void Flush()
        {
            if (dir != null) File.WriteAllText(Path.Combine(dir, "uevent"), uevent.ToString());
            uevent.Clear();
        }
        var lineNo = 0;
        foreach (var raw in text.Split('\n'))
        {
            lineNo++;
            var line = raw.TrimEnd('\r');
            if (line.Length == 0) continue;
            if (line.Length < 3 || line[1] != ':' || line[2] != ' ') throw new InvalidOperationException($"{file}:{lineNo}: invalid line");
            var value = line[3..];
            var eq = value.IndexOf('=');
            switch (line[0])
            {
                case 'P':
                    Flush();
                    var rel = value.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    dir = Path.Combine(root, "sys", SafePath(rel));
                    Directory.CreateDirectory(dir);
                    break;
                case 'E' when dir != null && eq > 0:
                    uevent.Append(value).Append('\n');
                    break;
                case 'A' when dir != null && eq > 0:
                    File.WriteAllText(Path.Combine(dir, SafePath(value[..eq])), Unescape(value[(eq + 1)..]));
                    break;
                case 'H' when dir != null && eq > 0:
                    File.WriteAllBytes(Path.Combine(dir, SafePath(value[..eq])), Convert.FromHexString(value[(eq + 1)..]));
                    break;
                case 'N':
                    var node = eq > 0 ? value[..eq] : value;
                    var nodePath = Path.Combine(root, "dev", SafePath(node.Replace('/', Path.DirectorySeparatorChar)));
                    Directory.CreateDirectory(Path.GetDirectoryName(nodePath));
                    File.WriteAllBytes(nodePath, eq > 0 ? Convert.FromHexString(value[(eq + 1)..]) : []);
                    break;
                case 'S' or 'L':
                    // Symlinks need Developer Mode or an administrator on Windows; the targets are kept as text.
                    if (dir != null && eq > 0) File.WriteAllText(Path.Combine(dir, SafePath(value[..eq]) + ".link"), value[(eq + 1)..]);
                    break;
            }
        }
        Flush();
    }

    static string SafePath(string p)
    {
        foreach (var c in Path.GetInvalidFileNameChars().Where(c => c != Path.DirectorySeparatorChar)) p = p.Replace(c, '_');
        if (p.Split(Path.DirectorySeparatorChar).Any(s => s == "..")) throw new InvalidOperationException($"invalid path {p}");
        return p;
    }

    static string Unescape(string v)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < v.Length; i++)
        {
            if (v[i] == '\\' && i + 1 < v.Length)
            {
                var n = v[++i];
                sb.Append(n switch { 'n' => '\n', 't' => '\t', '0' => '\0', _ => n });
            }
            else sb.Append(v[i]);
        }
        return sb.ToString();
    }

    // ---- Win32 ----

    const uint DIGCF_PRESENT = 0x2, DIGCF_ALLCLASSES = 0x4;
    const uint SPDRP_DEVICEDESC = 0, SPDRP_HARDWAREID = 1, SPDRP_COMPATIBLEIDS = 2, SPDRP_SERVICE = 4, SPDRP_CLASS = 7,
               SPDRP_MFG = 11, SPDRP_FRIENDLYNAME = 12, SPDRP_LOCATION_INFORMATION = 13, SPDRP_BUSNUMBER = 21,
               SPDRP_ENUMERATOR_NAME = 22, SPDRP_ADDRESS = 28;
    const int MAX_DEVICE_ID_LEN = 200;
    const uint CM_NOTIFY_FILTER_FLAG_ALL_DEVICE_INSTANCES = 2, CM_NOTIFY_FILTER_TYPE_DEVICEINSTANCE = 2;
    const int CM_NOTIFY_ACTION_DEVICEINSTANCEENUMERATED = 7, CM_NOTIFY_ACTION_DEVICEINSTANCESTARTED = 8, CM_NOTIFY_ACTION_DEVICEINSTANCEREMOVED = 9;
    static readonly IntPtr INVALID_HANDLE_VALUE = new(-1);

    [StructLayout(LayoutKind.Sequential)]
    struct SP_DEVINFO_DATA
    {
        public uint cbSize;
        public Guid ClassGuid;
        public uint DevInst;
        public IntPtr Reserved;
    }

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr SetupDiGetClassDevsW(IntPtr classGuid, string enumerator, IntPtr parent, uint flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SetupDiEnumDeviceInfo(IntPtr set, uint index, ref SP_DEVINFO_DATA info);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SetupDiGetDeviceRegistryPropertyW(IntPtr set, ref SP_DEVINFO_DATA info, uint property, out uint type, byte* buffer, uint size, out uint required);

    [DllImport("setupapi.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);

    [DllImport("cfgmgr32.dll")]
    static extern uint CM_Get_Parent(out uint parent, uint devinst, uint flags);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    static extern uint CM_Get_Device_IDW(uint devinst, char* buffer, int size, uint flags);

    [DllImport("cfgmgr32.dll")]
    static extern uint CM_Register_Notification(void* filter, IntPtr context, IntPtr callback, out IntPtr notify);

    [DllImport("cfgmgr32.dll")]
    static extern uint CM_Unregister_Notification(IntPtr notify);
}
