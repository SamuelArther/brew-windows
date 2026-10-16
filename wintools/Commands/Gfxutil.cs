using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

namespace WinTools;

// gfxutil: EFI device-properties blobs (the "DeviceProperties" of OpenCore/Clover and macOS's
// device-properties NVRAM variable) converted between hex, binary and XML plist, and EFI device
// path text <-> binary. -a/-f list PCI devices with their EFI device paths, built from the
// location paths Windows' PnP manager records (PCIROOT(0)#PCI(1F03) and the ACPI names) instead of
// macOS's IODeviceTree.
static class Gfxutil
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("gfxutil", "Convert EFI device properties and device paths; list PCI device paths", Run);
    }

    const string Help = """
        gfxutil: [command_option] [other_options] infile outfile
        Command options are:
          -f name   finds the device path of devices with the given name (ACPI name like HDAS or GFX0,
                    or part of the Windows device name)
          -a        show all PCI devices with their device paths
          -c str    convert a device path: text (PciRoot(0x0)/Pci(0x2,0x0)) to binary, or hex to text
          -h        print this help
        Other options are:
          -i fmt    infile type, fmt is one of (default is hex): xml bin hex
          -o fmt    outfile type, fmt is one of (default is xml): xml bin hex
          -v        verbose (with -a/-f: also print the Windows device name)
        infile/outfile may be '-' (or omitted) for standard input/output.
        """;

    static int Run(string[] args)
    {
        string inFormat = null, outFormat = null, find = null, convert = null;
        bool all = false, verbose = false;
        var files = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            string Value() => ++i < args.Length ? args[i] : throw new UsageException($"option {args[i - 1]} requires an argument");
            switch (args[i])
            {
                case "-h" or "--help": Console.WriteLine(Help); return 0;
                case "-i": inFormat = Format(Value()); break;
                case "-o": outFormat = Format(Value()); break;
                case "-f": find = Value(); break;
                case "-c": convert = Value(); break;
                case "-a": all = true; break;
                case "-v": verbose = true; break;
                case "-s" or "-n": break;   // string-detection/NVRAM tweaks of the macOS build; no effect
                case var a when a.StartsWith('-') && a != "-": throw new UsageException($"unknown option {a}");
                default: files.Add(args[i]); break;
            }
        }

        if (convert != null) return ConvertPath(convert, outFormat);
        if (all || find != null) return ListDevices(find, verbose);
        if (files.Count == 0 && !Console.IsInputRedirected) { Console.WriteLine(Help); return 2; }
        if (files.Count > 2) throw new UsageException("too many arguments");

        var input = files.Count > 0 && files[0] != "-" ? File.ReadAllBytes(files[0]) : ReadStdin();
        var devices = (inFormat ?? Guess(input)) switch
        {
            "xml" => FromPlist(Encoding.UTF8.GetString(input)),
            "hex" => ParseProperties(FromHex(Encoding.ASCII.GetString(input))),
            _ => ParseProperties(input),
        };
        byte[] output = (outFormat ?? "xml") switch
        {
            "bin" => BuildProperties(devices),
            "hex" => Encoding.ASCII.GetBytes(Convert.ToHexStringLower(BuildProperties(devices))),
            _ => Encoding.UTF8.GetBytes(ToPlist(devices)),
        };
        if (files.Count == 2 && files[1] != "-") File.WriteAllBytes(files[1], output);
        else
        {
            using var stdout = Console.OpenStandardOutput();
            stdout.Write(output);
        }
        return 0;
    }

    static string Format(string f) => f is "xml" or "bin" or "hex" ? f : throw new UsageException($"unknown format '{f}' (xml, bin or hex)");

    static byte[] ReadStdin()
    {
        using var ms = new MemoryStream();
        Console.OpenStandardInput().CopyTo(ms);
        return ms.ToArray();
    }

    static string Guess(byte[] input)
    {
        var text = Encoding.ASCII.GetString(input).Trim();
        if (text.StartsWith('<')) return "xml";
        return text.Length > 0 && text.All(Uri.IsHexDigit) ? "hex" : "bin";
    }

    static byte[] FromHex(string text)
    {
        var hex = new string(text.Where(c => !char.IsWhiteSpace(c)).ToArray());
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex[2..];
        if (hex.Length % 2 != 0 || !hex.All(Uri.IsHexDigit)) throw new InvalidOperationException("input is not valid hex");
        return Convert.FromHexString(hex);
    }

    // ---- device-properties blob ----------------------------------------------------------------
    // Header { u32 total size, u32 version (1), u32 device count }, then per device
    // { u32 entry size, u32 property count, device path ending in an End node, properties },
    // each property { u32 name size (incl. itself), UTF-16LE name with NUL, u32 value size (incl.
    // itself), value bytes }.

    internal sealed record Device(string Path, List<(string Name, byte[] Value)> Properties);

    internal static List<Device> ParseProperties(byte[] b)
    {
        if (b.Length < 12) throw new InvalidOperationException("input is too short to be a device-properties blob");
        var size = BitConverter.ToUInt32(b, 0);
        var count = BitConverter.ToUInt32(b, 8);
        if (size > b.Length || BitConverter.ToUInt32(b, 4) != 1) throw new InvalidOperationException("not a device-properties blob (bad header)");
        var devices = new List<Device>();
        var pos = 12;
        for (var d = 0; d < count; d++)
        {
            Need(b, pos, 8);
            var entryEnd = pos + (int)BitConverter.ToUInt32(b, pos);
            var props = BitConverter.ToUInt32(b, pos + 4);
            pos += 8;
            var pathStart = pos;
            while (true)
            {
                Need(b, pos, 4);
                var len = BitConverter.ToUInt16(b, pos + 2);
                if (len < 4) throw new InvalidOperationException("corrupt device path");
                var end = b[pos] == 0x7F && b[pos + 1] == 0xFF;
                pos += len;
                if (end) break;
            }
            var path = PathToText(b[pathStart..pos]);
            var list = new List<(string, byte[])>();
            for (var p = 0; p < props; p++)
            {
                Need(b, pos, 4);
                var nameLen = (int)BitConverter.ToUInt32(b, pos);
                Need(b, pos, nameLen);
                var name = Encoding.Unicode.GetString(b, pos + 4, nameLen - 4).TrimEnd('\0');
                pos += nameLen;
                Need(b, pos, 4);
                var valueLen = (int)BitConverter.ToUInt32(b, pos);
                Need(b, pos, valueLen);
                list.Add((name, b[(pos + 4)..(pos + valueLen)]));
                pos += valueLen;
            }
            if (pos != entryEnd) throw new InvalidOperationException($"corrupt entry for {path}");
            devices.Add(new Device(path, list));
        }
        return devices;
    }

    static void Need(byte[] b, int pos, int n)
    {
        if (n < 0 || pos + n > b.Length) throw new InvalidOperationException("truncated device-properties blob");
    }

    internal static byte[] BuildProperties(List<Device> devices)
    {
        var body = new MemoryStream();
        foreach (var device in devices)
        {
            var entry = new MemoryStream();
            entry.Write(TextToPath(device.Path));
            foreach (var (name, value) in device.Properties)
            {
                var n = Encoding.Unicode.GetBytes(name + "\0");
                entry.Write(BitConverter.GetBytes((uint)n.Length + 4));
                entry.Write(n);
                entry.Write(BitConverter.GetBytes((uint)value.Length + 4));
                entry.Write(value);
            }
            body.Write(BitConverter.GetBytes((uint)entry.Length + 8));
            body.Write(BitConverter.GetBytes((uint)device.Properties.Count));
            entry.WriteTo(body);
        }
        var result = new MemoryStream();
        result.Write(BitConverter.GetBytes((uint)body.Length + 12));
        result.Write(BitConverter.GetBytes(1u));
        result.Write(BitConverter.GetBytes((uint)devices.Count));
        body.WriteTo(result);
        return result.ToArray();
    }

    internal static string ToPlist(List<Device> devices)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        sb.Append("<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n");
        sb.Append("<plist version=\"1.0\">\n<dict>\n");
        foreach (var device in devices)
        {
            sb.Append($"\t<key>{Escape(device.Path)}</key>\n\t<dict>\n");
            foreach (var (name, value) in device.Properties)
            {
                sb.Append($"\t\t<key>{Escape(name)}</key>\n");
                // Printable NUL-terminated values are strings, everything else raw data.
                var printable = value.Length > 1 && value[^1] == 0 && value[..^1].All(c => c is >= 0x20 and < 0x7F);
                sb.Append(printable
                    ? $"\t\t<string>{Escape(Encoding.ASCII.GetString(value, 0, value.Length - 1))}</string>\n"
                    : $"\t\t<data>{Convert.ToBase64String(value)}</data>\n");
            }
            sb.Append("\t</dict>\n");
        }
        sb.Append("</dict>\n</plist>\n");
        return sb.ToString();
    }

    static string Escape(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    internal static List<Device> FromPlist(string xml)
    {
        var root = XDocument.Parse(xml).Root?.Element("dict") ?? throw new InvalidOperationException("plist has no top-level <dict>");
        var devices = new List<Device>();
        foreach (var (path, value) in Pairs(root))
        {
            if (value.Name != "dict") throw new InvalidOperationException($"{path}: expected a <dict> of properties");
            var props = new List<(string, byte[])>();
            foreach (var (name, v) in Pairs(value))
            {
                props.Add((name, v.Name.LocalName switch
                {
                    "string" => Encoding.ASCII.GetBytes(v.Value + "\0"),
                    "data" => Convert.FromBase64String(new string(v.Value.Where(c => !char.IsWhiteSpace(c)).ToArray())),
                    "integer" => BitConverter.GetBytes((uint)ParseNumber(v.Value)),
                    "true" => [1],
                    "false" => [0],
                    _ => throw new InvalidOperationException($"{path}/{name}: unsupported value type <{v.Name}>"),
                }));
            }
            devices.Add(new Device(PathToText(TextToPath(path)), props));
        }
        return devices;
    }

    static IEnumerable<(string Key, XElement Value)> Pairs(XElement dict)
    {
        var items = dict.Elements().ToList();
        for (var i = 0; i + 1 < items.Count; i += 2)
        {
            if (items[i].Name != "key") throw new InvalidOperationException("malformed plist <dict>");
            yield return (items[i].Value, items[i + 1]);
        }
    }

    // ---- EFI device paths ------------------------------------------------------------------

    static int ConvertPath(string text, string outFormat)
    {
        var trimmed = text.Trim();
        if (trimmed.Length > 0 && trimmed.All(Uri.IsHexDigit) && trimmed.Length % 2 == 0 && outFormat == null)
        {
            Console.WriteLine(PathToText(Convert.FromHexString(trimmed)));
            return 0;
        }
        var binary = TextToPath(trimmed);
        if (outFormat == "bin")
        {
            using var stdout = Console.OpenStandardOutput();
            stdout.Write(binary);
        }
        else Console.Write(Convert.ToHexStringLower(binary));
        return 0;
    }

    const uint PNP0A03 = 0x0A0341D0, PNP0A08 = 0x0A0841D0;

    internal static string PathToText(byte[] b)
    {
        var nodes = new List<string>();
        for (var pos = 0; pos + 4 <= b.Length;)
        {
            int type = b[pos], sub = b[pos + 1], len = BitConverter.ToUInt16(b, pos + 2);
            if (len < 4 || pos + len > b.Length) throw new InvalidOperationException("corrupt device path");
            if (type == 0x7F && sub == 0xFF) break;
            var data = b[(pos + 4)..(pos + len)];
            nodes.Add((type, sub, len) switch
            {
                (2, 1, 12) when BitConverter.ToUInt32(data, 0) == PNP0A03 => $"PciRoot(0x{BitConverter.ToUInt32(data, 4):x})",
                (2, 1, 12) when BitConverter.ToUInt32(data, 0) == PNP0A08 => $"PcieRoot(0x{BitConverter.ToUInt32(data, 4):x})",
                (2, 1, 12) => $"Acpi({EisaId(BitConverter.ToUInt32(data, 0))},0x{BitConverter.ToUInt32(data, 4):x})",
                (2, 3, >= 8) => "AcpiAdr(" + string.Join(",", Enumerable.Range(0, data.Length / 4).Select(i => $"0x{BitConverter.ToUInt32(data, i * 4):x}")) + ")",
                (1, 1, 6) => $"Pci(0x{data[1]:x},0x{data[0]:x})",
                _ => $"Path({type},{sub},{Convert.ToHexStringLower(data)})",
            });
            pos += len;
        }
        return string.Join("/", nodes);
    }

    internal static byte[] TextToPath(string text)
    {
        var ms = new MemoryStream();
        void Node(byte type, byte sub, params byte[][] parts)
        {
            var data = parts.SelectMany(p => p).ToArray();
            ms.WriteByte(type);
            ms.WriteByte(sub);
            ms.Write(BitConverter.GetBytes((ushort)(data.Length + 4)));
            ms.Write(data);
        }
        foreach (var raw in text.Trim().Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            var open = raw.IndexOf('(');
            if (open < 1 || !raw.EndsWith(')')) throw new InvalidOperationException($"bad device path node '{raw}'");
            var name = raw[..open].Trim();
            var args = raw[(open + 1)..^1].Split(',', StringSplitOptions.TrimEntries);
            switch (name.ToLowerInvariant())
            {
                case "pciroot": Node(2, 1, BitConverter.GetBytes(PNP0A03), BitConverter.GetBytes((uint)Arg(args, 0, raw))); break;
                case "pcieroot": Node(2, 1, BitConverter.GetBytes(PNP0A08), BitConverter.GetBytes((uint)Arg(args, 0, raw))); break;
                case "acpi":
                    var hid = args.Length > 0 && args[0].Length == 7 && char.IsLetter(args[0][0]) ? EisaId(args[0]) : (uint)Arg(args, 0, raw);
                    Node(2, 1, BitConverter.GetBytes(hid), BitConverter.GetBytes((uint)Arg(args, 1, raw)));
                    break;
                case "acpiadr": Node(2, 3, args.Select((_, i) => BitConverter.GetBytes((uint)Arg(args, i, raw))).ToArray()); break;
                case "pci": Node(1, 1, [(byte)Arg(args, 1, raw), (byte)Arg(args, 0, raw)]); break;
                case "path" when args.Length == 3:
                    Node((byte)Arg(args, 0, raw), (byte)Arg(args, 1, raw), Convert.FromHexString(args[2]));
                    break;
                default: throw new InvalidOperationException($"unsupported device path node '{raw}'");
            }
        }
        Node(0x7F, 0xFF);
        return ms.ToArray();
    }

    static long Arg(string[] args, int i, string node) =>
        i < args.Length && args[i].Length > 0 ? ParseNumber(args[i]) : throw new InvalidOperationException($"missing argument in '{node}'");

    static long ParseNumber(string s)
    {
        s = s.Trim();
        var ok = s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? long.TryParse(s[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v)
            : long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v);
        return ok ? v : throw new InvalidOperationException($"bad number '{s}'");
    }

    // EISA ids: three 5-bit letters in the low word, the product number in the high word.
    static string EisaId(uint id) =>
        $"{(char)('@' + ((id >> 10) & 0x1F))}{(char)('@' + ((id >> 5) & 0x1F))}{(char)('@' + (id & 0x1F))}{id >> 16:X4}";

    static uint EisaId(string s)
    {
        var u = s.ToUpperInvariant();
        return (uint)(((u[0] - '@') << 10) | ((u[1] - '@') << 5) | (u[2] - '@')) | (uint.Parse(u[3..], NumberStyles.HexNumber, CultureInfo.InvariantCulture) << 16);
    }

    // ---- PCI devices from the PnP manager ----------------------------------------------------

    static int ListDevices(string find, bool verbose)
    {
        var found = 0;
        foreach (var d in PciDevices())
        {
            if (find != null)
            {
                if (!(d.AcpiName.Equals(find, StringComparison.OrdinalIgnoreCase)
                      || d.Name.Contains(find, StringComparison.OrdinalIgnoreCase))) continue;
                Console.WriteLine($"DevicePath = {d.DevicePath}" + (verbose ? $"  [{d.Name}]" : ""));
            }
            else Console.WriteLine($"{d.Bus:x2}:{d.Dev:x2}.{d.Fn:x} {d.Ids} {d.IoPath} = {d.DevicePath}" + (verbose ? $"  [{d.Name}]" : ""));
            found++;
        }
        if (find != null && found == 0) { Console.Error.WriteLine($"gfxutil: {find} not found"); return 1; }
        return 0;
    }

    sealed record Pci(uint Bus, uint Dev, uint Fn, string Ids, string Name, string AcpiName, string IoPath, string DevicePath);

    // PCIROOT(0)#PCI(1C00)#PCI(0000) -> PciRoot(0x0)/Pci(0x1c,0x0)/Pci(0x0,0x0)
    internal static string LocationToDevicePath(string location)
    {
        var nodes = new List<string>();
        foreach (var part in location.Split('#'))
        {
            if (part.StartsWith("PCIROOT(", StringComparison.OrdinalIgnoreCase))
                nodes.Add($"PciRoot(0x{uint.Parse(part[8..^1], NumberStyles.HexNumber, CultureInfo.InvariantCulture):x})");
            else if (part.StartsWith("PCI(", StringComparison.OrdinalIgnoreCase) && part.Length == 9)
                nodes.Add($"Pci(0x{Convert.ToByte(part[4..6], 16):x},0x{Convert.ToByte(part[6..8], 16):x})");
            else return null;
        }
        return nodes.Count > 0 ? string.Join("/", nodes) : null;
    }

    static unsafe List<Pci> PciDevices()
    {
        var list = new List<Pci>();
        var set = SetupDiGetClassDevsW(IntPtr.Zero, "PCI", IntPtr.Zero, DIGCF_PRESENT | DIGCF_ALLCLASSES);
        if (set == new IntPtr(-1)) throw new System.ComponentModel.Win32Exception();
        try
        {
            var info = new SP_DEVINFO_DATA { cbSize = (uint)sizeof(SP_DEVINFO_DATA) };
            for (uint i = 0; SetupDiEnumDeviceInfo(set, i, ref info); i++)
            {
                var locations = Property(set, ref info, DEVPKEY_Device_LocationPaths, 37)?.Split('\0', StringSplitOptions.RemoveEmptyEntries) ?? [];
                var pciLocation = locations.FirstOrDefault(l => l.StartsWith("PCIROOT", StringComparison.OrdinalIgnoreCase));
                var devicePath = pciLocation == null ? null : LocationToDevicePath(pciLocation);
                if (devicePath == null) continue;
                var acpi = locations.FirstOrDefault(l => l.StartsWith("ACPI(", StringComparison.OrdinalIgnoreCase));
                var instance = Property(set, ref info, DEVPKEY_Device_InstanceId, 256) ?? "";
                var ven = Between(instance, "VEN_", 4).ToLowerInvariant();
                var dev = Between(instance, "DEV_", 4).ToLowerInvariant();
                var name = Property(set, ref info, DEVPKEY_Device_Base, 14) ?? Property(set, ref info, DEVPKEY_Device_Base, 2) ?? instance;
                var bus = PropertyUInt(set, ref info, 23);
                var address = PropertyUInt(set, ref info, 30);
                var pciNodes = pciLocation!.Split('#').Skip(1).ToList();
                var acpiNames = acpi?.Split('#').Select(a => a[5..^1].TrimEnd('_')).Where(a => a != "_SB").ToList() ?? [];
                // IODeviceTree-style path: ACPI names where Windows knows them, pci<ven>,<dev> otherwise.
                var io = new StringBuilder("/" + (acpiNames.Count > 0 ? acpiNames[0] : "PCI0") + "@0");
                for (var n = 0; n < pciNodes.Count; n++)
                {
                    var d = Convert.ToByte(pciNodes[n][4..6], 16);
                    var f = Convert.ToByte(pciNodes[n][6..8], 16);
                    var label = n + 1 < acpiNames.Count ? acpiNames[n + 1] : n == pciNodes.Count - 1 ? $"pci{ven},{dev}" : "pci-bridge";
                    io.Append($"/{label}@{d:X}" + (f != 0 ? $",{f:X}" : ""));
                }
                list.Add(new Pci(bus, address >> 16, address & 0xFFFF, $"{ven}:{dev}", name,
                    acpiNames.Count > 1 ? acpiNames[^1] : "", io.ToString(), devicePath));
            }
        }
        finally { SetupDiDestroyDeviceInfoList(set); }
        return list.OrderBy(p => p.Bus).ThenBy(p => p.Dev).ThenBy(p => p.Fn).ToList();
    }

    static string Between(string s, string marker, int length)
    {
        var i = s.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return i >= 0 && i + marker.Length + length <= s.Length ? s.Substring(i + marker.Length, length) : "????";
    }

    static readonly Guid DEVPKEY_Device_Base = new("a45c254e-df1c-4efd-8020-67d146a850e0");
    static readonly Guid DEVPKEY_Device_LocationPaths = DEVPKEY_Device_Base;
    static readonly Guid DEVPKEY_Device_InstanceId = new("78c34fc8-104a-4aca-9ea4-524d52996e57");

    static unsafe string Property(IntPtr set, ref SP_DEVINFO_DATA info, Guid fmtid, uint pid)
    {
        var key = new DEVPROPKEY { fmtid = fmtid, pid = pid };
        var buffer = new byte[4096];
        if (!SetupDiGetDevicePropertyW(set, ref info, ref key, out _, buffer, (uint)buffer.Length, out var size, 0) || size < 2) return null;
        return Encoding.Unicode.GetString(buffer, 0, (int)size).TrimEnd('\0');
    }

    static uint PropertyUInt(IntPtr set, ref SP_DEVINFO_DATA info, uint pid)
    {
        var key = new DEVPROPKEY { fmtid = DEVPKEY_Device_Base, pid = pid };
        var buffer = new byte[4];
        return SetupDiGetDevicePropertyW(set, ref info, ref key, out _, buffer, 4, out _, 0) ? BitConverter.ToUInt32(buffer) : 0;
    }

    const uint DIGCF_PRESENT = 2, DIGCF_ALLCLASSES = 4;

    [StructLayout(LayoutKind.Sequential)]
    struct SP_DEVINFO_DATA { public uint cbSize; public Guid ClassGuid; public uint DevInst; public IntPtr Reserved; }

    [StructLayout(LayoutKind.Sequential)]
    struct DEVPROPKEY { public Guid fmtid; public uint pid; }

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr SetupDiGetClassDevsW(IntPtr classGuid, string enumerator, IntPtr parent, uint flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    static extern bool SetupDiEnumDeviceInfo(IntPtr set, uint index, ref SP_DEVINFO_DATA data);

    [DllImport("setupapi.dll", SetLastError = true)]
    static extern bool SetupDiGetDevicePropertyW(IntPtr set, ref SP_DEVINFO_DATA data, ref DEVPROPKEY key, out uint type,
        byte[] buffer, uint size, out uint required, uint flags);

    [DllImport("setupapi.dll")]
    static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
}
