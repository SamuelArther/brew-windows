using System.Globalization;
using System.IO.Compression;
using System.Runtime.CompilerServices;

namespace WinTools;

// pciutils: lspci lists PCI devices from Windows' device tree (SetupAPI: bus/device/function, vendor,
// device, subsystem, revision and class from the PCI hardware ids, the driver from its service). Names
// come from pci.ids when update-pciids has downloaded it, else from the Windows driver's description.
// setpci reads the ID registers; the rest of configuration space needs a kernel driver on Windows.
static class Pciutils
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("lspci", "List PCI devices", RunLspci);
        Commands.Add("setpci", "Query PCI device ID registers", RunSetpci);
        Commands.Add("update-pciids", "Download the current PCI ID database", RunUpdate);
    }

    sealed class Pci
    {
        public int Bus, Device, Function;
        public ushort Vendor, DeviceId, SubVendor, SubDevice;
        public byte Class, SubClass, ProgIf, Revision;
        public bool HasRevision;
        public string Name, Manufacturer, Driver, InstanceId;
        public uint DevInst;
        public string Slot(bool domain) => $"{(domain ? "0000:" : "")}{Bus:x2}:{Device:x2}.{Function:x}";
    }

    static List<Pci> Devices()
    {
        var list = new List<Pci>();
        foreach (var d in Lsusb.PnpDevices("PCI"))
        {
            var hw = d.HardwareIds.FirstOrDefault(h => h.Contains("SUBSYS_", StringComparison.OrdinalIgnoreCase)) ?? d.HardwareIds.FirstOrDefault() ?? d.InstanceId;
            var cc = d.CompatibleIds.Select(c => Field(c, "CC_", 6)).FirstOrDefault(c => c != null) ?? d.CompatibleIds.Select(c => Field(c, "CC_", 4)).FirstOrDefault(c => c != null);
            var pci = new Pci
            {
                Bus = (int)(d.BusNumber ?? 0),
                Device = (int)((d.Address ?? 0) >> 16),
                Function = (int)((d.Address ?? 0) & 0xffff),
                Vendor = Hex16(Field(hw, "VEN_", 4)),
                DeviceId = Hex16(Field(hw, "DEV_", 4)),
                Name = d.Name,
                Manufacturer = d.Manufacturer,
                Driver = d.Service,
                InstanceId = d.InstanceId,
                DevInst = d.DevInst,
            };
            var subsys = Field(hw, "SUBSYS_", 8);
            if (subsys != null) { pci.SubDevice = Hex16(subsys[..4]); pci.SubVendor = Hex16(subsys[4..]); }
            var rev = Field(hw, "REV_", 2);
            if (rev != null) { pci.Revision = (byte)Hex16(rev); pci.HasRevision = true; }
            if (cc != null)
            {
                pci.Class = (byte)Hex16(cc[..2]);
                pci.SubClass = (byte)Hex16(cc[2..4]);
                if (cc.Length == 6) pci.ProgIf = (byte)Hex16(cc[4..]);
            }
            list.Add(pci);
        }
        return list.OrderBy(p => p.Bus).ThenBy(p => p.Device).ThenBy(p => p.Function).ToList();
    }

    static string Field(string id, string key, int length)
    {
        var at = id.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (at < 0 || at + key.Length + length > id.Length) return null;
        var value = id.Substring(at + key.Length, length);
        return value.All(Uri.IsHexDigit) ? value : null;
    }

    static ushort Hex16(string text) => text != null && ushort.TryParse(text, NumberStyles.HexNumber, null, out var v) ? v : (ushort)0;

    // ---- pci.ids ---------------------------------------------------------------------------------

    static string IdsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "pciutils", "pci.ids");

    sealed class IdDb
    {
        public Dictionary<ushort, string> Vendors = [];
        public Dictionary<uint, string> Devices = [];          // vendor << 16 | device
        public Dictionary<ulong, string> Subsystems = [];      // vendor, device, subvendor, subdevice
        public Dictionary<int, string> Classes = [];           // class << 8 | subclass, class << 8 | 0xffff-marker for base
        public Dictionary<int, string> ProgIfs = [];
    }

    static IdDb LoadIds(string path)
    {
        var db = new IdDb();
        if (path == null || !File.Exists(path)) return null;
        ushort vendor = 0, device = 0;
        int cls = -1, sub = -1;
        foreach (var raw in File.ReadLines(path))
        {
            if (raw.Length == 0 || raw[0] == '#') continue;
            var tabs = raw.TakeWhile(c => c == '\t').Count();
            var line = raw[tabs..];
            if (tabs == 0 && line.StartsWith("C "))
            {
                cls = Hex16(line[2..4]); sub = -1; vendor = 0;
                db.Classes[cls << 8 | 0xff00 << 8] = line[4..].Trim();
                continue;
            }
            if (tabs == 0)
            {
                cls = -1;
                if (line.Length > 6 && ushort.TryParse(line[..4], NumberStyles.HexNumber, null, out vendor)) db.Vendors[vendor] = line[4..].Trim();
                continue;
            }
            if (cls >= 0)
            {
                if (tabs == 1 && line.Length > 3) { sub = Hex16(line[..2]); db.Classes[cls << 8 | sub] = line[2..].Trim(); }
                else if (tabs == 2 && sub >= 0 && line.Length > 3) db.ProgIfs[(cls << 16) | (sub << 8) | Hex16(line[..2])] = line[2..].Trim();
                continue;
            }
            if (tabs == 1 && line.Length > 5) { device = Hex16(line[..4]); db.Devices[(uint)vendor << 16 | device] = line[4..].Trim(); }
            else if (tabs == 2 && line.Length > 10)
                db.Subsystems[((ulong)vendor << 48) | ((ulong)device << 32) | ((ulong)Hex16(line[..4]) << 16) | Hex16(line[5..9])] = line[9..].Trim();
        }
        return db;
    }

    // ---- built-in names (used without pci.ids) ---------------------------------------------------

    static readonly Dictionary<int, string> BaseClasses = new()
    {
        [0x00] = "Unclassified device", [0x01] = "Mass storage controller", [0x02] = "Network controller", [0x03] = "Display controller",
        [0x04] = "Multimedia controller", [0x05] = "Memory controller", [0x06] = "Bridge", [0x07] = "Communication controller",
        [0x08] = "Generic system peripheral", [0x09] = "Input device controller", [0x0a] = "Docking station", [0x0b] = "Processor",
        [0x0c] = "Serial bus controller", [0x0d] = "Wireless controller", [0x0e] = "Intelligent controller", [0x0f] = "Satellite communications controller",
        [0x10] = "Encryption controller", [0x11] = "Signal processing controller", [0x12] = "Processing accelerators",
        [0x13] = "Non-Essential Instrumentation", [0x40] = "Coprocessor", [0xff] = "Unassigned class",
    };

    static readonly Dictionary<int, string> SubClasses = new()
    {
        [0x0000] = "Non-VGA unclassified device", [0x0001] = "VGA compatible unclassified device",
        [0x0100] = "SCSI storage controller", [0x0101] = "IDE interface", [0x0102] = "Floppy disk controller", [0x0104] = "RAID bus controller",
        [0x0105] = "ATA controller", [0x0106] = "SATA controller", [0x0107] = "Serial Attached SCSI controller", [0x0108] = "Non-Volatile memory controller",
        [0x0180] = "Mass storage controller",
        [0x0200] = "Ethernet controller", [0x0280] = "Network controller",
        [0x0300] = "VGA compatible controller", [0x0301] = "XGA compatible controller", [0x0302] = "3D controller", [0x0380] = "Display controller",
        [0x0400] = "Multimedia video controller", [0x0401] = "Multimedia audio controller", [0x0403] = "Audio device", [0x0480] = "Multimedia controller",
        [0x0500] = "RAM memory", [0x0501] = "FLASH memory", [0x0580] = "Memory controller",
        [0x0600] = "Host bridge", [0x0601] = "ISA bridge", [0x0604] = "PCI bridge", [0x0607] = "CardBus bridge", [0x0680] = "Bridge",
        [0x0700] = "Serial controller", [0x0701] = "Parallel controller", [0x0703] = "Modem", [0x0780] = "Communication controller",
        [0x0800] = "PIC", [0x0801] = "DMA controller", [0x0802] = "Timer", [0x0803] = "RTC", [0x0805] = "SD Host controller",
        [0x0806] = "IOMMU", [0x0880] = "System peripheral",
        [0x0900] = "Keyboard controller", [0x0902] = "Mouse controller", [0x0980] = "Input device controller",
        [0x0c00] = "FireWire (IEEE 1394)", [0x0c03] = "USB controller", [0x0c04] = "Fibre Channel", [0x0c05] = "SMBus",
        [0x0c07] = "IPMI Interface", [0x0c80] = "Serial bus controller",
        [0x0d11] = "Bluetooth", [0x0d80] = "Network controller", [0x1080] = "Encryption controller", [0x1180] = "Signal processing controller",
        [0x1200] = "Processing accelerators",
    };

    static readonly Dictionary<ushort, string> Vendors = new()
    {
        [0x8086] = "Intel Corporation", [0x10de] = "NVIDIA Corporation", [0x1002] = "Advanced Micro Devices, Inc. [AMD/ATI]",
        [0x1022] = "Advanced Micro Devices, Inc. [AMD]", [0x10ec] = "Realtek Semiconductor Co., Ltd.", [0x14e4] = "Broadcom Inc. and subsidiaries",
        [0x168c] = "Qualcomm Atheros", [0x17cb] = "Qualcomm Technologies, Inc", [0x144d] = "Samsung Electronics Co Ltd", [0x15b7] = "Sandisk Corp",
        [0x1c5c] = "SK hynix", [0x1e0f] = "KIOXIA Corporation", [0x1b4b] = "Marvell Technology Group Ltd.", [0x1414] = "Microsoft Corporation",
        [0x1af4] = "Red Hat, Inc.", [0x1b36] = "Red Hat, Inc.", [0x15ad] = "VMware", [0x80ee] = "InnoTek Systemberatung GmbH", [0x1d0f] = "Amazon.com, Inc.",
        [0x1344] = "Micron Technology Inc", [0x126f] = "Silicon Motion, Inc.", [0x1987] = "Phison Electronics Corporation",
        [0x2646] = "Kingston Technology Company, Inc.", [0x1cc1] = "ADATA Technology Co., Ltd.", [0x1217] = "O2 Micro, Inc.",
        [0x10b5] = "PLX Technology, Inc.", [0x1912] = "Renesas Electronics Corp.", [0x1b21] = "ASMedia Technology Inc.", [0x1102] = "Creative Labs",
        [0x104c] = "Texas Instruments", [0x1180] = "Ricoh Co Ltd", [0x14c3] = "MEDIATEK Corp.", [0x1106] = "VIA Technologies, Inc.",
        [0x1013] = "Cirrus Logic", [0x1d6a] = "Aquantia Corp.", [0x1425] = "Chelsio Communications Inc", [0x15b3] = "Mellanox Technologies",
        [0x1c00] = "Nanjing Qinheng Microelectronics", [0x1d97] = "Shenzhen Longsys Electronics Co., Ltd.", [0x1e4b] = "MAXIO Technology (Hangzhou) Ltd.",
        [0x1bb1] = "Seagate Technology PLC", [0x1c20] = "Realtek Semiconductor Co., Ltd.", [0x197b] = "JMicron Technology Corp.",
        [0x1aa8] = "Ciphertext Inc.", [0x17aa] = "Lenovo", [0x1028] = "Dell", [0x103c] = "Hewlett-Packard Company", [0x1043] = "ASUSTeK Computer Inc.",
        [0x1462] = "Micro-Star International Co., Ltd. [MSI]", [0x1458] = "Gigabyte Technology Co., Ltd", [0x1025] = "Acer Incorporated [ALI]",
        [0x1179] = "Toshiba Corporation", [0x104d] = "Sony Corporation", [0x152d] = "Quanta Computer Inc", [0x1558] = "CLEVO/KAPOK Computer",
        [0x1849] = "ASRock Incorporation", [0x106b] = "Apple Inc.", [0x1d72] = "Xiaomi", [0x1e50] = "IP3 Tech (HK) Limited", [0x1e83] = "Huawei",
    };

    static string ClassName(Pci p, IdDb db) =>
        db?.Classes.GetValueOrDefault(p.Class << 8 | p.SubClass) ?? SubClasses.GetValueOrDefault(p.Class << 8 | p.SubClass)
        ?? db?.Classes.GetValueOrDefault(p.Class << 8 | 0xff00 << 8) ?? BaseClasses.GetValueOrDefault(p.Class) ?? $"Class {p.Class:x2}{p.SubClass:x2}";

    static string VendorName(ushort vendor, IdDb db) => db?.Vendors.GetValueOrDefault(vendor) ?? Vendors.GetValueOrDefault(vendor) ?? $"Vendor {vendor:x4}";

    static string DeviceName(Pci p, IdDb db)
    {
        if (db?.Devices.GetValueOrDefault((uint)p.Vendor << 16 | p.DeviceId) is string name) return name;
        // Windows' description often repeats the vendor ("Intel(R) ..."); lspci shows "Device xxxx" when it knows nothing.
        return p.Name ?? $"Device {p.DeviceId:x4}";
    }

    static string SubsystemName(Pci p, IdDb db)
    {
        var sub = db?.Subsystems.GetValueOrDefault(((ulong)p.Vendor << 48) | ((ulong)p.DeviceId << 32) | ((ulong)p.SubVendor << 16) | p.SubDevice);
        return $"{VendorName(p.SubVendor, db)} {sub ?? $"Device {p.SubDevice:x4}"}";
    }

    // ---- lspci -----------------------------------------------------------------------------------

    static int RunLspci(string[] args)
    {
        int numeric = 0, verbose = 0, machine = 0;
        bool kernel = false, domain = false, tree = false;
        string slot = null, ids = null, idsFile = null;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg is "--version" or "-V") { Console.WriteLine($"lspci version {Commands.Version} (wintools)"); return 0; }
            if (arg is "-h" or "--help") { LspciHelp(); return 0; }
            if (arg is "-s" or "-d" or "-i" or "-A" or "-H" or "-F" or "-O" or "-p")
            {
                var value = ++i < args.Length ? args[i] : throw new UsageException($"option {arg} requires an argument");
                if (arg == "-s") slot = value; else if (arg == "-d") ids = value; else if (arg == "-i") idsFile = value;
                else if (arg is "-H" or "-A" or "-F" or "-p") throw new UsageException($"{arg}: direct hardware access and dump files aren't available on Windows");
                continue;
            }
            if (!arg.StartsWith('-') || arg.Length < 2) throw new UsageException($"invalid argument '{arg}'");
            foreach (var c in arg[1..])
            {
                switch (c)
                {
                    case 'n': numeric++; break;
                    case 'v': verbose++; break;
                    case 'm': machine = Math.Max(machine, 1); machine += arg.Count(x => x == 'm') > 1 ? 1 : 0; break;
                    case 'k': kernel = true; break;
                    case 'D': domain = true; break;
                    case 't': tree = true; break;
                    case 'q' or 'Q': throw new UsageException("-q/-Q query the online PCI ID service; run update-pciids instead");
                    case 'x' or 'b' or 'P' or 'M' or 'S' or 'y' or 'N' or 'G': break;   // hardware-level flags with no Windows equivalent
                    default: throw new UsageException($"invalid option -{c}");
                }
            }
        }
        var db = numeric == 1 ? null : LoadIds(idsFile ?? IdsPath);
        var devices = Devices().Where(p => MatchSlot(p, slot) && MatchIds(p, ids)).ToList();
        if (tree) { PrintTree(devices, db); return 0; }
        foreach (var p in devices)
        {
            if (machine > 0) { Console.WriteLine(Machine(p, db, numeric, domain, verbose)); continue; }
            Console.WriteLine(Line(p, db, numeric, domain, verbose > 0));
            if (verbose > 0 || kernel)
            {
                if (verbose > 0 && (p.SubVendor != 0 || p.SubDevice != 0))
                    Console.WriteLine(numeric == 1 ? $"\tSubsystem: {p.SubVendor:x4}:{p.SubDevice:x4}" : $"\tSubsystem: {SubsystemName(p, db)}{(numeric > 1 ? $" [{p.SubVendor:x4}:{p.SubDevice:x4}]" : "")}");
                if (verbose > 0 && p.Manufacturer != null) Console.WriteLine($"\tWindows device: {p.Name} ({p.Manufacturer})");
                if (verbose > 1) Console.WriteLine($"\tInstance: {p.InstanceId}");
                if (p.Driver != null) Console.WriteLine($"\tKernel driver in use: {p.Driver}");
                Console.WriteLine();
            }
        }
        return 0;
    }

    static void LspciHelp() => Console.WriteLine("""
        Usage: lspci [<switches>]

        Basic display modes:
        -mm		Produce machine-readable output (single -m for an obsolete format)
        -t		Show bus tree

        Display options:
        -v		Be verbose (-vv for very verbose)
        -k		Show kernel drivers handling each device (Windows driver service)
        -D		Always show domain numbers

        Resolving of device ID's to names:
        -n		Show numeric ID's
        -nn		Show both textual and numeric ID's (names & numbers)
        -i <file>	Use specified ID database instead of the one update-pciids downloaded

        Selection of devices:
        -s [[[[<domain>]:]<bus>]:][<slot>][.[<func>]]	Show only devices in selected slots
        -d [<vendor>]:[<device>][:<class>]		Show only devices with specified ID's

        Names come from pci.ids after running update-pciids, otherwise from Windows' device descriptions.
        """);

    static string Line(Pci p, IdDb db, int numeric, bool domain, bool verbose)
    {
        var rev = p.HasRevision ? $" (rev {p.Revision:x2})" : "";
        var progIf = verbose && p.ProgIf != 0 ? $" (prog-if {p.ProgIf:x2}{(db?.ProgIfs.GetValueOrDefault((p.Class << 16) | (p.SubClass << 8) | p.ProgIf) is string pi ? $" [{pi}]" : "")})" : "";
        if (numeric == 1) return $"{p.Slot(domain)} {p.Class:x2}{p.SubClass:x2}: {p.Vendor:x4}:{p.DeviceId:x4}{rev}{progIf}";
        var cls = ClassName(p, db) + (numeric > 1 ? $" [{p.Class:x2}{p.SubClass:x2}]" : "");
        var name = $"{VendorName(p.Vendor, db)} {DeviceName(p, db)}" + (numeric > 1 ? $" [{p.Vendor:x4}:{p.DeviceId:x4}]" : "");
        return $"{p.Slot(domain)} {cls}: {name}{rev}{progIf}";
    }

    static string Machine(Pci p, IdDb db, int numeric, bool domain, int verbose)
    {
        string Q(string s) => $"\"{s}\"";
        var cls = numeric == 1 ? $"{p.Class:x2}{p.SubClass:x2}" : ClassName(p, db) + (numeric > 1 ? $" [{p.Class:x2}{p.SubClass:x2}]" : "");
        var vendor = numeric == 1 ? $"{p.Vendor:x4}" : VendorName(p.Vendor, db) + (numeric > 1 ? $" [{p.Vendor:x4}]" : "");
        var device = numeric == 1 ? $"{p.DeviceId:x4}" : DeviceName(p, db) + (numeric > 1 ? $" [{p.DeviceId:x4}]" : "");
        var subVendor = p.SubVendor == 0 ? "" : numeric == 1 ? $"{p.SubVendor:x4}" : VendorName(p.SubVendor, db);
        var subDevice = p.SubDevice == 0 ? "" : numeric == 1 ? $"{p.SubDevice:x4}" : $"Device {p.SubDevice:x4}";
        var line = $"{p.Slot(domain)} {Q(cls)} {Q(vendor)} {Q(device)}{(p.HasRevision ? $" -r{p.Revision:x2}" : "")}{(p.ProgIf != 0 ? $" -p{p.ProgIf:x2}" : "")} {Q(subVendor)} {Q(subDevice)}";
        if (verbose > 0 && p.Driver != null) line += $"\nDriver:\t{p.Driver}";
        return line;
    }

    // -s [[domain:]bus:][slot][.func], hex, any part may be empty or *.
    static bool MatchSlot(Pci p, string spec)
    {
        if (spec == null) return true;
        var func = "";
        var dot = spec.LastIndexOf('.');
        if (dot >= 0) { func = spec[(dot + 1)..]; spec = spec[..dot]; }
        var parts = spec.Split(':');
        var slot = parts[^1];
        var bus = parts.Length >= 2 ? parts[^2] : "";
        bool Part(string text, int value) => text is "" or "*" || (int.TryParse(text, NumberStyles.HexNumber, null, out var v) && v == value);
        return Part(bus, p.Bus) && Part(slot, p.Device) && Part(func, p.Function);
    }

    static bool MatchIds(Pci p, string spec)
    {
        if (spec == null) return true;
        var parts = spec.Split(':');
        bool Part(int index, int value) => parts.Length <= index || parts[index] is "" or "*" || (int.TryParse(parts[index], NumberStyles.HexNumber, null, out var v) && v == value);
        if (parts.Length > 2 && parts[2].Length > 2)
            return Part(0, p.Vendor) && Part(1, p.DeviceId) && int.TryParse(parts[2], NumberStyles.HexNumber, null, out var cc) && cc == (p.Class << 8 | p.SubClass);
        return Part(0, p.Vendor) && Part(1, p.DeviceId);
    }

    // Bus tree from the device tree: devices whose parent is a PCI bridge hang under it.
    static void PrintTree(List<Pci> devices, IdDb db)
    {
        var byInst = devices.ToDictionary(d => d.DevInst);
        var children = devices.ToLookup(d => Lsusb.Parent(d.DevInst) is uint parent && byInst.ContainsKey(parent) ? parent : 0u);

        // lspci's layout: a bridge's first child continues its line, siblings hang off +- / \- branches.
        List<string> Render(Pci d)
        {
            var label = $"{d.Device:x2}.{d.Function:x}";
            var kids = children[d.DevInst].ToList();
            if (kids.Count == 0) return [label];
            var head = $"{label}-[{kids[0].Bus:x2}]-";
            var sub = RenderList(kids);
            return [head + sub[0], .. sub.Skip(1).Select(l => new string(' ', head.Length) + l)];
        }
        List<string> RenderList(List<Pci> items)
        {
            if (items.Count == 1)
            {
                var only = Render(items[0]);
                return ["-" + only[0], .. only.Skip(1).Select(l => " " + l)];
            }
            var lines = new List<string>();
            for (var i = 0; i < items.Count; i++)
            {
                var last = i == items.Count - 1;
                var r = Render(items[i]);
                lines.Add((last ? "\\-" : "+-") + r[0]);
                lines.AddRange(r.Skip(1).Select(l => (last ? "  " : "| ") + l));
            }
            return lines;
        }
        var top = children[0u].ToList();
        var rootBus = top.Select(d => d.Bus).DefaultIfEmpty(0).Min();
        var head = $"-[0000:{rootBus:x2}]-";
        if (top.Count == 0) { Console.WriteLine(head); return; }
        var tree = RenderList(top);
        Console.WriteLine(head + tree[0]);
        foreach (var line in tree.Skip(1)) Console.WriteLine(new string(' ', head.Length) + line);
    }

    // ---- setpci ----------------------------------------------------------------------------------

    static readonly Dictionary<string, (int Offset, int Width)> Registers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["VENDOR_ID"] = (0x00, 2), ["DEVICE_ID"] = (0x02, 2), ["REVISION_ID"] = (0x08, 1), ["CLASS_PROG"] = (0x09, 1),
        ["CLASS_DEVICE"] = (0x0a, 2), ["SUBSYSTEM_VENDOR_ID"] = (0x2c, 2), ["SUBSYSTEM_ID"] = (0x2e, 2),
    };

    static int RunSetpci(string[] args)
    {
        string slot = null, ids = null;
        var registers = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--version" or "-V": Console.WriteLine($"setpci version {Commands.Version} (wintools)"); return 0;
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage: setpci [<options>] (-s <slot> | -d <vendor>:<device>) <reg>[.(B|W|L)] ...
                        Reads PCI ID registers: VENDOR_ID, DEVICE_ID, REVISION_ID, CLASS_PROG, CLASS_DEVICE,
                        SUBSYSTEM_VENDOR_ID, SUBSYSTEM_ID (or their offsets 0.w, 2.w, 8.b, 9.b, a.w, 2c.w, 2e.w).
                        Windows only gives programs these values; reading other registers and writing any
                        register needs a kernel driver.
                        """);
                    return 0;
                case "-s": slot = ++i < args.Length ? args[i] : throw new UsageException("-s needs a slot"); break;
                case "-d": ids = ++i < args.Length ? args[i] : throw new UsageException("-d needs vendor:device"); break;
                case "-v" or "-f" or "-D" or "-r": break;
                default:
                    if (args[i].StartsWith('-')) throw new UsageException($"invalid option {args[i]}");
                    registers.Add(args[i]);
                    break;
            }
        }
        if (slot == null && ids == null) throw new UsageException("no device selected (use -s or -d)");
        if (registers.Count == 0) throw new UsageException("no registers given");
        var devices = Devices().Where(p => MatchSlot(p, slot) && MatchIds(p, ids)).ToList();
        if (devices.Count == 0) { Console.Error.WriteLine("setpci: Warning: No devices selected"); return 0; }
        foreach (var reg in registers)
        {
            if (reg.Contains('='))
            {
                Console.Error.WriteLine("setpci: writing PCI configuration space needs a kernel driver on Windows; nothing was changed");
                return 1;
            }
            var (offset, width) = Resolve(reg);
            foreach (var p in devices)
            {
                var value = offset switch
                {
                    0x00 => p.Vendor, 0x02 => p.DeviceId, 0x08 => p.Revision, 0x09 => p.ProgIf, 0x0a => (p.Class << 8) | p.SubClass,
                    0x0b => p.Class, 0x2c => p.SubVendor, 0x2e => p.SubDevice, _ => -1,
                };
                if (value < 0) throw new InvalidOperationException($"register {reg}: Windows only exposes the PCI ID registers");
                Console.WriteLine(width == 1 ? $"{value & 0xff:x2}" : $"{value:x4}");
            }
        }
        return 0;
    }

    static (int Offset, int Width) Resolve(string reg)
    {
        var name = reg;
        var width = 0;
        var dot = reg.LastIndexOf('.');
        if (dot > 0) { name = reg[..dot]; width = char.ToUpperInvariant(reg[dot + 1]) switch { 'B' => 1, 'W' => 2, 'L' => 4, _ => throw new UsageException($"bad width in '{reg}'") }; }
        if (Registers.TryGetValue(name, out var known)) return (known.Offset, width == 0 ? known.Width : width);
        if (int.TryParse(name, NumberStyles.HexNumber, null, out var offset))
        {
            if (width == 0) throw new UsageException($"missing width in '{reg}' (use .b, .w or .l)");
            if (offset == 0x0b) return (0x0b, 1);
            return (offset, width);
        }
        throw new UsageException($"unknown register '{reg}'");
    }

    // ---- update-pciids ---------------------------------------------------------------------------

    static int RunUpdate(string[] args)
    {
        var quiet = args.Contains("-q");
        if (args.Any(a => a is "-h" or "--help"))
        {
            Console.WriteLine($"Usage: update-pciids [-q]\nDownloads the PCI ID database from pci-ids.ucw.cz to {IdsPath}.");
            return 0;
        }
        if (args.Any(a => a != "-q")) throw new UsageException($"unknown argument {args.First(a => a != "-q")}");
        const string url = "https://pci-ids.ucw.cz/v2.2/pci.ids.gz";
        if (!quiet) Console.WriteLine($"Downloading {url}");
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        byte[] compressed;
        try { compressed = http.GetByteArrayAsync(url).GetAwaiter().GetResult(); }
        catch (HttpRequestException e) { throw new InvalidOperationException($"download failed: {e.Message}"); }
        using var input = new GZipStream(new MemoryStream(compressed), CompressionMode.Decompress);
        using var text = new MemoryStream();
        input.CopyTo(text);
        var content = text.ToArray();
        if (content.Length < 100_000 || !System.Text.Encoding.ASCII.GetString(content, 0, 200).Contains("pci.ids"))
            throw new InvalidOperationException("the downloaded file doesn't look like pci.ids");
        Directory.CreateDirectory(Path.GetDirectoryName(IdsPath)!);
        var temp = IdsPath + ".new";
        File.WriteAllBytes(temp, content);
        File.Move(temp, IdsPath, overwrite: true);
        if (!quiet) Console.WriteLine($"Done: {IdsPath}");
        return 0;
    }
}
