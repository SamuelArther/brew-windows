using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTools;

// lsusb: list USB devices like Linux's lsusb. Walks every USB hub through the same IOCTLs USBView uses
// (IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX for each port's device descriptor, address and speed;
// IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_CONNECTION for manufacturer/product strings), so bus numbers,
// names come from the devices themselves. One bus per host controller; the root hub is device 1 and
// each device is its USB address + 1 (xHCI hands out address 1 too, Linux's numbering never does).
// Also hosts the SetupAPI device enumeration pciutils uses.
static unsafe class Lsusb
{
    [ModuleInitializer]
    internal static void Register() => Commands.Add("lsusb", "List USB devices, just like the Linux lsusb command", Run);

    // ---- shared SetupAPI enumeration --------------------------------------------------------------

    internal sealed class PnpDevice
    {
        public uint DevInst;
        public string InstanceId, Description, BusReported, FriendlyName, Manufacturer, Service, DriverKey, Location;
        public string[] HardwareIds = [], CompatibleIds = [];
        public uint? BusNumber, Address;
        public string Name => FriendlyName ?? Description ?? BusReported ?? InstanceId;
    }

    // Present devices from one enumerator ("USB", "PCI"), or all when null.
    internal static List<PnpDevice> PnpDevices(string enumerator)
    {
        var list = new List<PnpDevice>();
        var set = SetupDiGetClassDevsW(null, enumerator, IntPtr.Zero, DIGCF_ALLCLASSES | DIGCF_PRESENT);
        if (set == -1) return list;
        try
        {
            var data = new SP_DEVINFO_DATA { cbSize = (uint)sizeof(SP_DEVINFO_DATA) };
            for (uint i = 0; SetupDiEnumDeviceInfo(set, i, &data); i++)
            {
                list.Add(new PnpDevice
                {
                    DevInst = data.DevInst,
                    InstanceId = DeviceId(data.DevInst),
                    Description = Property(set, &data, SPDRP_DEVICEDESC),
                    FriendlyName = Property(set, &data, SPDRP_FRIENDLYNAME),
                    Manufacturer = Property(set, &data, SPDRP_MFG),
                    Service = Property(set, &data, SPDRP_SERVICE),
                    DriverKey = Property(set, &data, SPDRP_DRIVER),
                    Location = Property(set, &data, SPDRP_LOCATION_INFORMATION),
                    HardwareIds = MultiProperty(set, &data, SPDRP_HARDWAREID),
                    CompatibleIds = MultiProperty(set, &data, SPDRP_COMPATIBLEIDS),
                    BusNumber = DwordProperty(set, &data, SPDRP_BUSNUMBER),
                    Address = DwordProperty(set, &data, SPDRP_ADDRESS),
                    BusReported = BusReportedDescription(set, &data),
                });
            }
        }
        finally { SetupDiDestroyDeviceInfoList(set); }
        return list;
    }

    internal static string DeviceId(uint devInst)
    {
        var buffer = stackalloc char[400];
        return CM_Get_Device_IDW(devInst, buffer, 400, 0) == 0 ? new string(buffer) : "";
    }

    internal static uint? Parent(uint devInst) => CM_Get_Parent(out var parent, devInst, 0) == 0 ? parent : null;

    static byte[] RawProperty(nint set, SP_DEVINFO_DATA* data, uint property)
    {
        var buffer = new byte[2048];
        fixed (byte* b = buffer)
            if (!SetupDiGetDeviceRegistryPropertyW(set, data, property, out _, b, (uint)buffer.Length, out var size)) return null;
            else return buffer[..(int)size];
    }

    static string Property(nint set, SP_DEVINFO_DATA* data, uint property)
    {
        var raw = RawProperty(set, data, property);
        if (raw == null) return null;
        var text = Encoding.Unicode.GetString(raw).TrimEnd('\0');
        return text.Length == 0 ? null : text;
    }

    static string[] MultiProperty(nint set, SP_DEVINFO_DATA* data, uint property)
    {
        var raw = RawProperty(set, data, property);
        return raw == null ? [] : Encoding.Unicode.GetString(raw).Split('\0', StringSplitOptions.RemoveEmptyEntries);
    }

    static uint? DwordProperty(nint set, SP_DEVINFO_DATA* data, uint property)
    {
        var raw = RawProperty(set, data, property);
        return raw is { Length: >= 4 } ? BitConverter.ToUInt32(raw, 0) : null;
    }

    // DEVPKEY_Device_BusReportedDeviceDesc: the product string the device itself reports.
    static string BusReportedDescription(nint set, SP_DEVINFO_DATA* data)
    {
        var key = new DEVPROPKEY { fmtid = new Guid("540b947e-8b40-45bc-a8a2-6a0b894cbda2"), pid = 4 };
        var buffer = stackalloc byte[1024];
        if (!SetupDiGetDevicePropertyW(set, data, &key, out _, buffer, 1024, out var size, 0) || size < 4) return null;
        var text = new string((char*)buffer, 0, (int)size / 2).TrimEnd('\0');
        return text.Length == 0 ? null : text;
    }

    // ---- lsusb -----------------------------------------------------------------------------------

    sealed class UsbNode
    {
        public int Bus, Address, Port, Speed, Level;
        public bool IsHub, IsRoot;
        public byte[] Descriptor = new byte[18];
        public string Manufacturer, Product, Serial, Driver, Description;
        public int Ports;
        public ushort Vendor => BitConverter.ToUInt16(Descriptor, 8);
        public ushort ProductId => BitConverter.ToUInt16(Descriptor, 10);
    }

    static int Run(string[] args)
    {
        bool verbose = false, tree = false;
        string select = null, deviceFilter = null;
        for (var i = 0; i < args.Length; i++)
        {
            string Value() => ++i < args.Length ? args[i] : throw new UsageException($"{args[i - 1]} needs a value");
            switch (args[i])
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage: lsusb [options]...
                        List USB devices
                          -v, --verbose
                              Increase verbosity (show descriptors)
                          -s [[bus]:][devnum]
                              Show only devices with specified device and/or bus numbers (in decimal)
                          -d vendor:[product]
                              Show only devices with the specified vendor and product ID numbers (in hexadecimal)
                          -t, --tree
                              Dump the physical USB device hierarchy as a tree
                          -V, --version
                              Show version of program
                          -h, --help
                              Show usage and help
                        """);
                    return 0;
                case "-V" or "--version": Console.WriteLine($"lsusb (wintools) {Commands.Version}"); return 0;
                case "-v" or "--verbose": verbose = true; break;
                case "-t" or "--tree": tree = true; break;
                case "-s": select = Value(); break;
                case "-d": deviceFilter = Value(); break;
                default: throw new UsageException($"invalid option {args[i]}");
            }
        }
        var nodes = Walk();
        if (tree) { PrintTree(nodes); return 0; }
        var filtered = nodes.Where(n => Matches(n, select, deviceFilter)).OrderBy(n => n.Bus).ThenBy(n => n.Address).ToList();
        foreach (var n in filtered)
        {
            Console.WriteLine($"Bus {n.Bus:000} Device {n.Address:000}: ID {n.Vendor:x4}:{n.ProductId:x4} {Title(n)}");
            if (verbose) PrintDescriptor(n);
        }
        return (select != null || deviceFilter != null) && filtered.Count == 0 ? 1 : 0;
    }

    static string Title(UsbNode n)
    {
        if (n.IsRoot) return $"{n.Description} root hub";
        var parts = new List<string>();
        if (n.Manufacturer != null) parts.Add(n.Manufacturer);
        if (n.Product != null) parts.Add(n.Product);
        if (parts.Count == 0 && n.Description != null) parts.Add(n.Description);
        return string.Join(" ", parts);
    }

    static bool Matches(UsbNode n, string select, string device)
    {
        if (select != null)
        {
            var colon = select.IndexOf(':');
            var bus = colon < 0 ? "" : select[..colon];
            var dev = colon < 0 ? select : select[(colon + 1)..];
            if (bus.Length > 0 && (!int.TryParse(bus, out var b) || b != n.Bus)) return false;
            if (dev.Length > 0 && (!int.TryParse(dev, out var d) || d != n.Address)) return false;
        }
        if (device != null)
        {
            var parts = device.Split(':');
            if (parts[0].Length > 0 && (!ushort.TryParse(parts[0], NumberStyles.HexNumber, null, out var v) || v != n.Vendor)) return false;
            if (parts.Length > 1 && parts[1].Length > 0 && (!ushort.TryParse(parts[1], NumberStyles.HexNumber, null, out var p) || p != n.ProductId)) return false;
        }
        return true;
    }

    // Every root hub, then each hub's ports, depth first (the order -t prints).
    static List<UsbNode> Walk()
    {
        var devices = PnpDevices("USB");
        var byDriver = devices.Where(d => d.DriverKey != null).ToDictionary(d => d.DriverKey, StringComparer.OrdinalIgnoreCase);
        var hubs = HubInterfaces();   // instance id -> device path
        var roots = devices.Where(d => d.InstanceId.StartsWith(@"USB\ROOT_HUB", StringComparison.OrdinalIgnoreCase) && hubs.ContainsKey(d.InstanceId))
                           .OrderBy(d => d.InstanceId, StringComparer.OrdinalIgnoreCase).ToList();
        var nodes = new List<UsbNode>();
        for (var b = 0; b < roots.Count; b++)
        {
            var root = roots[b];
            var controller = Parent(root.DevInst) is uint p ? DeviceId(p) : "";
            var (ven, dev) = PciIds(controller);
            var node = new UsbNode { Bus = b + 1, Address = 1, IsHub = true, IsRoot = true, Driver = root.Service, Speed = root.InstanceId.Contains("30") ? 3 : 2 };
            BitConverter.TryWriteBytes(node.Descriptor.AsSpan(8), ven);
            BitConverter.TryWriteBytes(node.Descriptor.AsSpan(10), dev);
            node.Descriptor[0] = 18; node.Descriptor[1] = 1; node.Descriptor[4] = 9;   // hub class
            controllers ??= PnpDevices("PCI").Concat(PnpDevices("ACPI")).ToList();
            node.Description = controllers.FirstOrDefault(d => d.InstanceId.Equals(controller, StringComparison.OrdinalIgnoreCase))?.Name ?? "USB";
            nodes.Add(node);
            WalkHub(hubs[root.InstanceId], node, nodes, byDriver, hubs, 1);
        }
        return nodes;
    }

    static List<PnpDevice> controllers;

    static (ushort Vendor, ushort Device) PciIds(string instanceId)
    {
        ushort Hex(string key)
        {
            var at = instanceId.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            return at >= 0 && at + key.Length + 4 <= instanceId.Length && ushort.TryParse(instanceId.AsSpan(at + key.Length, 4), NumberStyles.HexNumber, null, out var v) ? v : (ushort)0;
        }
        return (Hex("VEN_"), Hex("DEV_"));
    }

    static void WalkHub(string path, UsbNode hub, List<UsbNode> nodes, Dictionary<string, PnpDevice> byDriver, Dictionary<string, string> hubs, int level)
    {
        var handle = CreateFileW(path, GENERIC_WRITE, FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
        if (handle == -1) return;
        try
        {
            var nodeInfo = stackalloc byte[128];
            if (!DeviceIoControl(handle, IOCTL_USB_GET_NODE_INFORMATION, nodeInfo, 128, nodeInfo, 128, out _, IntPtr.Zero)) return;
            hub.Ports = nodeInfo[6];
            var buffer = new byte[1024];
            for (var port = 1; port <= hub.Ports; port++)
            {
                fixed (byte* b = buffer)
                {
                    Array.Clear(buffer);
                    *(uint*)b = (uint)port;
                    if (!DeviceIoControl(handle, IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX, b, (uint)buffer.Length, b, (uint)buffer.Length, out _, IntPtr.Zero)) continue;
                    if (*(uint*)(b + 31) != 1) continue;   // DeviceConnected
                    var node = new UsbNode { Bus = hub.Bus, Port = port, Level = level, Speed = b[23], IsHub = b[24] != 0, Address = *(ushort*)(b + 25) + 1 };
                    Array.Copy(buffer, 4, node.Descriptor, 0, 18);
                    var driverKey = DriverKey(handle, port);
                    PnpDevice pnp = null;
                    if (driverKey != null && byDriver.TryGetValue(driverKey, out pnp))
                    {
                        node.Driver = pnp.Service;
                        node.Description = pnp.Name;
                    }
                    node.Manufacturer = StringDescriptor(handle, port, node.Descriptor[14]);
                    node.Product = StringDescriptor(handle, port, node.Descriptor[15]) ?? pnp?.BusReported;
                    node.Serial = StringDescriptor(handle, port, node.Descriptor[16]);
                    nodes.Add(node);
                    if (node.IsHub && pnp != null && hubs.TryGetValue(pnp.InstanceId, out var childPath))
                        WalkHub(childPath, node, nodes, byDriver, hubs, level + 1);
                }
            }
        }
        finally { CloseHandle(handle); }
    }

    static string DriverKey(nint hub, int port)
    {
        var buffer = stackalloc byte[1024];
        *(uint*)buffer = (uint)port;
        if (!DeviceIoControl(hub, IOCTL_USB_GET_NODE_CONNECTION_DRIVERKEY_NAME, buffer, 1024, buffer, 1024, out var size, IntPtr.Zero) || size <= 8) return null;
        return new string((char*)(buffer + 8)).TrimEnd('\0');
    }

    // String descriptors in US English; 0 means the device has none.
    static string StringDescriptor(nint hub, int port, byte index)
    {
        if (index == 0) return null;
        const int header = 12;
        var buffer = stackalloc byte[header + 255];
        new Span<byte>(buffer, header + 255).Clear();
        *(uint*)buffer = (uint)port;
        buffer[4] = 0x80;                                    // bmRequest: device-to-host
        buffer[5] = 6;                                       // GET_DESCRIPTOR
        *(ushort*)(buffer + 6) = (ushort)((3 << 8) | index); // string descriptor
        *(ushort*)(buffer + 8) = 0x0409;
        *(ushort*)(buffer + 10) = 255;
        if (!DeviceIoControl(hub, IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_CONNECTION, buffer, header + 255, buffer, header + 255, out var size, IntPtr.Zero)) return null;
        var length = buffer[header];
        if (size < header + 2 || buffer[header + 1] != 3 || length < 2) return null;
        var text = new string((char*)(buffer + header + 2), 0, (length - 2) / 2).Trim().TrimEnd('\0');
        return text.Length == 0 ? null : text;
    }

    static Dictionary<string, string> HubInterfaces()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var guid = new Guid("f18a0e88-c30c-11d0-8815-00a0c906bed8");   // GUID_DEVINTERFACE_USB_HUB
        var set = SetupDiGetClassDevsW(&guid, null, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
        if (set == -1) return map;
        try
        {
            var iface = new SP_DEVICE_INTERFACE_DATA { cbSize = (uint)sizeof(SP_DEVICE_INTERFACE_DATA) };
            var detail = stackalloc byte[2048];
            for (uint i = 0; SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, &guid, i, &iface); i++)
            {
                *(uint*)detail = 8;   // sizeof(SP_DEVICE_INTERFACE_DETAIL_DATA_W) on 64-bit
                var info = new SP_DEVINFO_DATA { cbSize = (uint)sizeof(SP_DEVINFO_DATA) };
                if (!SetupDiGetDeviceInterfaceDetailW(set, &iface, detail, 2048, out _, &info)) continue;
                map[DeviceId(info.DevInst)] = new string((char*)(detail + 4));
            }
        }
        finally { SetupDiDestroyDeviceInfoList(set); }
        return map;
    }

    static string SpeedText(int speed) => speed switch { 0 => "1.5M", 1 => "12M", 2 => "480M", 3 => "5000M", 4 => "10000M", _ => "?" };

    static string ClassName(UsbNode n)
    {
        var cls = n.Descriptor[4];
        return cls switch
        {
            0 => "[unknown]", 1 => "Audio", 2 => "Communications", 3 => "Human Interface Device", 6 => "Image", 7 => "Printer",
            8 => "Mass Storage", 9 => n.IsRoot ? "root_hub" : "Hub", 0x0a => "CDC Data", 0x0b => "Chip/SmartCard", 0x0e => "Video",
            0x10 => "Audio/Video", 0xdc => "Diagnostic", 0xe0 => "Wireless", 0xef => "Miscellaneous Device", 0xfe => "Application Specific Interface",
            0xff => "Vendor Specific Class", _ => $"0x{cls:x2}",
        };
    }

    static void PrintTree(List<UsbNode> nodes)
    {
        foreach (var n in nodes)
        {
            if (n.IsRoot)
                Console.WriteLine($"/:  Bus {n.Bus:00}.Port 1: Dev 1, Class=root_hub, Driver={n.Driver ?? "?"}/{n.Ports}p, {SpeedText(n.Speed)}");
            else
                Console.WriteLine($"{new string(' ', 4 * n.Level)}|__ Port {n.Port}: Dev {n.Address}, Class={ClassName(n)}, Driver={n.Driver ?? "[none]"}{(n.IsHub ? $"/{n.Ports}p" : "")}, {SpeedText(n.Speed)}");
        }
    }

    static void PrintDescriptor(UsbNode n)
    {
        var d = n.Descriptor;
        string Bcd(int offset) => $"{d[offset + 1]:x}.{d[offset]:x2}";
        Console.WriteLine("Device Descriptor:");
        Console.WriteLine($"  bLength            {d[0],5}");
        Console.WriteLine($"  bDescriptorType    {d[1],5}");
        Console.WriteLine($"  bcdUSB             {Bcd(2),5}");
        Console.WriteLine($"  bDeviceClass       {d[4],5} {ClassName(n)}");
        Console.WriteLine($"  bDeviceSubClass    {d[5],5}");
        Console.WriteLine($"  bDeviceProtocol    {d[6],5}");
        Console.WriteLine($"  bMaxPacketSize0    {d[7],5}");
        Console.WriteLine($"  idVendor          0x{n.Vendor:x4} {n.Manufacturer}");
        Console.WriteLine($"  idProduct         0x{n.ProductId:x4} {n.Product}");
        Console.WriteLine($"  bcdDevice          {Bcd(12),5}");
        Console.WriteLine($"  iManufacturer      {d[14],5} {n.Manufacturer}");
        Console.WriteLine($"  iProduct           {d[15],5} {n.Product}");
        Console.WriteLine($"  iSerial            {d[16],5} {n.Serial}");
        Console.WriteLine($"  bNumConfigurations {d[17],5}");
        if (n.Description != null) Console.WriteLine($"  Windows device:    {n.Description}{(n.Driver != null ? $" (driver {n.Driver})" : "")}");
        Console.WriteLine();
    }

    // ---- Win32 -----------------------------------------------------------------------------------

    const uint DIGCF_PRESENT = 0x2, DIGCF_ALLCLASSES = 0x4, DIGCF_DEVICEINTERFACE = 0x10;
    const uint SPDRP_DEVICEDESC = 0x0, SPDRP_HARDWAREID = 0x1, SPDRP_COMPATIBLEIDS = 0x2, SPDRP_SERVICE = 0x4, SPDRP_DRIVER = 0x9,
               SPDRP_MFG = 0xb, SPDRP_FRIENDLYNAME = 0xc, SPDRP_LOCATION_INFORMATION = 0xd, SPDRP_BUSNUMBER = 0x15, SPDRP_ADDRESS = 0x1c;
    const uint GENERIC_WRITE = 0x40000000, FILE_SHARE_WRITE = 0x2, OPEN_EXISTING = 3;
    const uint IOCTL_USB_GET_NODE_INFORMATION = 0x220408, IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_CONNECTION = 0x220410,
               IOCTL_USB_GET_NODE_CONNECTION_DRIVERKEY_NAME = 0x220420, IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX = 0x220448;

    [StructLayout(LayoutKind.Sequential)]
    struct SP_DEVINFO_DATA { public uint cbSize; public Guid ClassGuid; public uint DevInst; public nint Reserved; }

    [StructLayout(LayoutKind.Sequential)]
    struct SP_DEVICE_INTERFACE_DATA { public uint cbSize; public Guid InterfaceClassGuid; public uint Flags; public nint Reserved; }

    [StructLayout(LayoutKind.Sequential)]
    struct DEVPROPKEY { public Guid fmtid; public uint pid; }

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode)]
    static extern nint SetupDiGetClassDevsW(Guid* classGuid, string enumerator, IntPtr parent, uint flags);
    [DllImport("setupapi.dll")] static extern bool SetupDiEnumDeviceInfo(nint set, uint index, SP_DEVINFO_DATA* data);
    [DllImport("setupapi.dll")] static extern bool SetupDiDestroyDeviceInfoList(nint set);
    [DllImport("setupapi.dll")]
    static extern bool SetupDiGetDeviceRegistryPropertyW(nint set, SP_DEVINFO_DATA* data, uint property, out uint type, byte* buffer, uint size, out uint required);
    [DllImport("setupapi.dll")]
    static extern bool SetupDiGetDevicePropertyW(nint set, SP_DEVINFO_DATA* data, DEVPROPKEY* key, out uint type, byte* buffer, uint size, out uint required, uint flags);
    [DllImport("setupapi.dll")]
    static extern bool SetupDiEnumDeviceInterfaces(nint set, IntPtr data, Guid* guid, uint index, SP_DEVICE_INTERFACE_DATA* iface);
    [DllImport("setupapi.dll")]
    static extern bool SetupDiGetDeviceInterfaceDetailW(nint set, SP_DEVICE_INTERFACE_DATA* iface, byte* detail, uint size, out uint required, SP_DEVINFO_DATA* data);
    [DllImport("cfgmgr32.dll")] static extern uint CM_Get_Device_IDW(uint devInst, char* buffer, uint length, uint flags);
    [DllImport("cfgmgr32.dll")] static extern uint CM_Get_Parent(out uint parent, uint devInst, uint flags);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern nint CreateFileW(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
    [DllImport("kernel32.dll")]
    static extern bool DeviceIoControl(nint device, uint code, void* input, uint inputSize, void* output, uint outputSize, out uint returned, IntPtr overlapped);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(nint handle);
}
