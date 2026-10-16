using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace WinTools;

// libdrm's tools: drmdevice lists the GPUs (their PCI identity and location) and modetest/modeprint
// show kernel mode-setting state: connectors with their type, status and modes, encoders, CRTCs.
// Linux reads those through DRM ioctls on /dev/dri; on Windows the same facts come from the display
// driver model: the Display device class in SetupAPI (PCI ids, bus location), the GDI display
// devices (nodes) and QueryDisplayConfig paths (source = CRTC, target = connector, with the
// connector's output technology, the active timing and the monitor's name). modetest -s sets a mode
// temporarily (until Enter); planes and properties (-P, -w) have no user-settable counterpart.
static class Libdrm
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("drmdevice", "List the GPUs as DRM devices (libdrm drmdevice)", RunDrmdevice);
        Commands.Add("modetest", "Show display connectors, encoders, CRTCs and modes (libdrm modetest)", RunModetest);
        Commands.Add("modeprint", "Print display connectors and modes (libdrm modeprint)", RunModeprint);
    }

    // ---- GPUs ----

    sealed class Gpu
    {
        public string Name, HardwareId, Location;
        public int Vendor, Device, SubVendor, SubDevice, Revision;
        public int Bus = -1, Dev = -1, Func = -1;
        public bool Pci;
        public List<string> Nodes = [];
    }

    static List<Gpu> Gpus()
    {
        var list = new List<Gpu>();
        var guid = new Guid("4d36e968-e325-11ce-bfc1-08002be10318");   // GUID_DEVCLASS_DISPLAY
        var set = SetupDiGetClassDevsW(ref guid, null, IntPtr.Zero, 2);   // DIGCF_PRESENT
        if (set == new IntPtr(-1)) return list;
        try
        {
            var data = new SP_DEVINFO_DATA { cbSize = Marshal.SizeOf<SP_DEVINFO_DATA>() };
            for (uint i = 0; SetupDiEnumDeviceInfo(set, i, ref data); i++)
            {
                var g = new Gpu
                {
                    Name = Property(set, ref data, 0x0C) ?? Property(set, ref data, 0),   // friendly name, description
                    HardwareId = Property(set, ref data, 1),
                    Location = Property(set, ref data, 0x0D),
                };
                // PCI\VEN_8086&DEV_5916&SUBSYS_224517AA&REV_02
                var m = Regex.Match(g.HardwareId ?? "", @"VEN_([0-9A-F]{4})&DEV_([0-9A-F]{4})(?:&SUBSYS_([0-9A-F]{4})([0-9A-F]{4}))?(?:&REV_([0-9A-F]{2}))?", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    g.Pci = true;
                    g.Vendor = Convert.ToInt32(m.Groups[1].Value, 16);
                    g.Device = Convert.ToInt32(m.Groups[2].Value, 16);
                    if (m.Groups[3].Success) { g.SubDevice = Convert.ToInt32(m.Groups[3].Value, 16); g.SubVendor = Convert.ToInt32(m.Groups[4].Value, 16); }
                    if (m.Groups[5].Success) g.Revision = Convert.ToInt32(m.Groups[5].Value, 16);
                }
                // "PCI bus 0, device 2, function 0"
                var loc = Regex.Match(g.Location ?? "", @"bus (\d+), device (\d+), function (\d+)");
                if (loc.Success) { g.Bus = int.Parse(loc.Groups[1].Value); g.Dev = int.Parse(loc.Groups[2].Value); g.Func = int.Parse(loc.Groups[3].Value); }
                list.Add(g);
            }
        }
        finally { SetupDiDestroyDeviceInfoList(set); }
        // GDI display devices (\\.\DISPLAYn) belong to the adapter whose hardware id they carry.
        foreach (var d in DisplayDevices())
        {
            var owner = list.FirstOrDefault(g => g.HardwareId != null && d.DeviceId.StartsWith(g.HardwareId.Split('\0')[0], StringComparison.OrdinalIgnoreCase))
                        ?? list.FirstOrDefault(g => g.Name == d.String);
            owner?.Nodes.Add(d.Name);
        }
        return list;
    }

    static string Property(IntPtr set, ref SP_DEVINFO_DATA data, uint prop)
    {
        var buf = new byte[2048];
        if (!SetupDiGetDeviceRegistryPropertyW(set, ref data, prop, out _, buf, buf.Length, out var size)) return null;
        return Encoding.Unicode.GetString(buf, 0, (int)Math.Max(0, size - 2)).TrimEnd('\0');
    }

    static int RunDrmdevice(string[] args)
    {
        if (args.Length > 0 && args[0] is "-h" or "--help")
        {
            Console.WriteLine("Usage: drmdevice\nList the GPUs (DRM devices) with their PCI bus location and ids.");
            return 0;
        }
        if (args.Length > 0) throw new UsageException($"unexpected argument {args[0]}");
        var gpus = Gpus();
        Console.WriteLine("--- Checking the number of DRM device available ---");
        Console.WriteLine($"--- Devices reported {gpus.Count} ---");
        Console.WriteLine("--- Retrieving devices information (PCI device revision is ignored) ---");
        for (var i = 0; i < gpus.Count; i++)
        {
            var g = gpus[i];
            Console.WriteLine($"device[{i}]");
            Console.WriteLine($"+-> name {g.Name}");
            Console.WriteLine($"+-> available_nodes 0x{(g.Nodes.Count > 0 ? 1 : 0):x2}");
            Console.WriteLine("+-> nodes");
            foreach (var (n, k) in g.Nodes.Select((n, k) => (n, k))) Console.WriteLine($"|   +-> nodes[{k}] {n}");
            Console.WriteLine($"+-> bustype {(g.Pci ? "0000" : "0004")}");
            if (g.Pci)
            {
                Console.WriteLine("|   +-> pci");
                Console.WriteLine("|       +-> domain 0000");
                Console.WriteLine($"|       +-> bus    {(g.Bus < 0 ? "??" : g.Bus.ToString("x2"))}");
                Console.WriteLine($"|       +-> dev    {(g.Dev < 0 ? "??" : g.Dev.ToString("x2"))}");
                Console.WriteLine($"|       +-> func   {(g.Func < 0 ? "?" : g.Func.ToString("x1"))}");
                Console.WriteLine("+-> deviceinfo");
                Console.WriteLine("    +-> pci");
                Console.WriteLine($"        +-> vendor_id     {g.Vendor:x4}");
                Console.WriteLine($"        +-> device_id     {g.Device:x4}");
                Console.WriteLine($"        +-> subvendor_id  {g.SubVendor:x4}");
                Console.WriteLine($"        +-> subdevice_id  {g.SubDevice:x4}");
                Console.WriteLine($"        +-> revision_id   {g.Revision:x2}");
            }
            else
            {
                Console.WriteLine("|   +-> platform");
                Console.WriteLine($"|       +-> fullname  {g.HardwareId?.Split('\0')[0]}");
            }
            Console.WriteLine();
        }
        return 0;
    }

    // ---- display paths ----

    sealed class Output
    {
        public int Crtc, Connector;
        public string Gpu, Device, Monitor, Type;
        public int Width, Height, X, Y;
        public double Refresh;
        public long PixelRate;
        public int HTotal, VTotal;
        public List<(int W, int H, int Hz, int Bpp)> Modes = [];
        public bool Connected = true;
    }

    static List<Output> Outputs()
    {
        var outputs = new List<Output>();
        if (GetDisplayConfigBufferSizes(2, out var nPaths, out var nModes) != 0) return outputs;   // QDC_ONLY_ACTIVE_PATHS
        var paths = new byte[nPaths * 72];
        var modes = new byte[nModes * 64];
        if (QueryDisplayConfig(2, ref nPaths, paths, ref nModes, modes, IntPtr.Zero) != 0) return outputs;
        var devices = DisplayDevices();
        for (var i = 0; i < nPaths; i++)
        {
            var p = i * 72;
            var o = new Output { Crtc = 31 + i, Connector = 41 + i };
            var srcAdapter = BitConverter.ToInt64(paths, p);
            var srcId = BitConverter.ToUInt32(paths, p + 8);
            var srcMode = BitConverter.ToUInt32(paths, p + 12);
            var tgtAdapter = BitConverter.ToInt64(paths, p + 20);
            var tgtId = BitConverter.ToUInt32(paths, p + 28);
            var tgtMode = BitConverter.ToUInt32(paths, p + 32);
            var tech = BitConverter.ToInt32(paths, p + 36);
            var refreshNum = BitConverter.ToUInt32(paths, p + 48);
            var refreshDen = BitConverter.ToUInt32(paths, p + 52);
            if (refreshDen != 0) o.Refresh = (double)refreshNum / refreshDen;
            o.Type = Technology(tech);
            if (srcMode < nModes)
            {
                var m = (int)srcMode * 64 + 16;
                o.Width = BitConverter.ToInt32(modes, m);
                o.Height = BitConverter.ToInt32(modes, m + 4);
                o.X = BitConverter.ToInt32(modes, m + 12);
                o.Y = BitConverter.ToInt32(modes, m + 16);
            }
            if (tgtMode < nModes)
            {
                var m = (int)tgtMode * 64 + 16;
                o.PixelRate = BitConverter.ToInt64(modes, m);
                o.HTotal = BitConverter.ToInt32(modes, m + 32);
                o.VTotal = BitConverter.ToInt32(modes, m + 36);
            }
            // The source's GDI name (\\.\DISPLAY1) and the monitor's friendly name.
            var source = new byte[84];   // DISPLAYCONFIG_SOURCE_DEVICE_NAME: header(20) + viewGdiDeviceName[32]
            BitConverter.TryWriteBytes(source.AsSpan(0), 1);
            BitConverter.TryWriteBytes(source.AsSpan(4), source.Length);
            BitConverter.TryWriteBytes(source.AsSpan(8), srcAdapter);
            BitConverter.TryWriteBytes(source.AsSpan(16), srcId);
            if (DisplayConfigGetDeviceInfo(source) == 0) o.Device = Encoding.Unicode.GetString(source, 20, 64).TrimEnd('\0');
            var target = new byte[420];
            BitConverter.TryWriteBytes(target.AsSpan(0), 2);
            BitConverter.TryWriteBytes(target.AsSpan(4), target.Length);
            BitConverter.TryWriteBytes(target.AsSpan(8), tgtAdapter);
            BitConverter.TryWriteBytes(target.AsSpan(16), tgtId);
            if (DisplayConfigGetDeviceInfo(target) == 0)
            {
                o.Monitor = Encoding.Unicode.GetString(target, 36, 128).TrimEnd('\0');
                var connectorInstance = BitConverter.ToUInt32(target, 32);
                o.Type += $"-{connectorInstance + 1}";
            }
            else o.Type += "-1";
            o.Gpu = devices.FirstOrDefault(d => d.Name == o.Device).String;
            if (o.Device != null) o.Modes = Modes(o.Device);
            outputs.Add(o);
        }
        return outputs;
    }

    static string Technology(int t) => t switch
    {
        0 => "VGA",
        1 => "SVIDEO",
        2 => "Composite",
        3 => "Component",
        4 => "DVI-D",
        5 => "HDMI-A",
        6 => "LVDS",
        8 => "9PinDIN",
        9 => "SDI",
        10 => "DP",
        11 => "eDP",
        12 or 13 => "UDI",
        14 => "TV",
        15 => "Miracast",
        16 => "Virtual",
        unchecked((int)0x80000000) => "eDP",
        _ => "Unknown",
    };

    // The distinct modes the display device lists (EnumDisplaySettings).
    static List<(int W, int H, int Hz, int Bpp)> Modes(string device)
    {
        var list = new List<(int, int, int, int)>();
        var dm = new byte[220];
        BitConverter.TryWriteBytes(dm.AsSpan(68), (ushort)220);
        for (var i = 0; EnumDisplaySettingsExW(device, i, dm, 0); i++)
        {
            var mode = (BitConverter.ToInt32(dm, 172), BitConverter.ToInt32(dm, 176), BitConverter.ToInt32(dm, 184), BitConverter.ToInt32(dm, 168));
            if (mode.Item4 == 32 && !list.Contains(mode)) list.Add(mode);
        }
        return list.OrderByDescending(m => m.Item1 * m.Item2).ThenByDescending(m => m.Item3).ToList();
    }

    static int RunModetest(string[] args)
    {
        bool connectors = false, encoders = false, crtcs = false, planes = false, framebuffers = false;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        usage: modetest [-acDdefMPpsCvrw]

                         Query options:

                        	-c	list connectors
                        	-e	list encoders
                        	-f	list framebuffers
                        	-p	list CRTCs and planes (pipes)

                         Test options:

                        	-s <connector_id>[@<crtc_id>]:<mode>[-<vrefresh>]	set a mode until Enter is pressed

                         Generic options:

                        	-d	drop master after mode set
                        	-M module	use the given driver (ignored: Windows has one display stack)
                        	-D device	use the given device (ignored)

                        	Default is to dump all info.
                        """);
                    return 0;
                case "-c": connectors = true; break;
                case "-e": encoders = true; break;
                case "-p": crtcs = planes = true; break;
                case "-f": framebuffers = true; break;
                case "-a" or "-d" or "-r": break;
                case "-M" or "-D": i++; break;
                case "-s": return SetMode(++i < args.Length ? args[i] : throw new UsageException("-s needs <connector_id>[@<crtc_id>]:<mode>[-<vrefresh>]"));
                case "-P" or "-v" or "-w" or "-C":
                    Console.Error.WriteLine($"modetest: {args[i]}: Windows has no user-settable planes or properties; only modes can be set (-s)");
                    return 1;
                default: throw new UsageException($"invalid option {args[i]}");
            }
        }
        if (!connectors && !encoders && !crtcs && !planes && !framebuffers) connectors = encoders = crtcs = planes = framebuffers = true;
        var outputs = Outputs();
        if (encoders)
        {
            Console.WriteLine("Encoders:\nid\tcrtc\ttype\tpossible crtcs\tpossible clones\t");
            foreach (var o in outputs)
                Console.WriteLine($"{o.Connector - 10}\t{o.Crtc}\t{EncoderType(o.Type)}\t0x{(1 << outputs.Count) - 1:x8}\t0x00000000");
            Console.WriteLine();
        }
        if (connectors)
        {
            Console.WriteLine("Connectors:\nid\tencoder\tstatus\t\tname\t\tsize (mm)\tmodes\tencoders");
            foreach (var o in outputs)
            {
                Console.WriteLine($"{o.Connector}\t{o.Connector - 10}\t{(o.Connected ? "connected" : "disconnected")}\t{o.Type,-15}\t0x0\t\t{o.Modes.Count}\t{o.Connector - 10}");
                if (!string.IsNullOrEmpty(o.Monitor)) Console.WriteLine($"  monitor: {o.Monitor} ({o.Gpu})");
                Console.WriteLine("  modes:\n\tindex name refresh (Hz) hdisp hss hse htot vdisp vss vse vtot");
                for (var k = 0; k < o.Modes.Count; k++)
                {
                    var (w, h, hz, _) = o.Modes[k];
                    var current = w == o.Width && h == o.Height && Math.Abs(hz - o.Refresh) < 1;
                    var timing = current && o.HTotal > 0 ? $"{w} - - {o.HTotal} {h} - - {o.VTotal} {o.PixelRate / 1000}" : $"{w} - - - {h} - - - -";
                    Console.WriteLine($"  #{k} {w}x{h} {hz:0.00} {timing} flags: ; type: {(k == 0 ? "preferred, " : "")}driver{(current ? " (current)" : "")}");
                }
                Console.WriteLine();
            }
        }
        if (crtcs)
        {
            Console.WriteLine("CRTCs:\nid\tfb\tpos\tsize");
            foreach (var o in outputs)
            {
                Console.WriteLine($"{o.Crtc}\t{o.Crtc + 20}\t({o.X},{o.Y})\t({o.Width}x{o.Height})");
                var timing = o.HTotal > 0 ? $"{o.Width} - - {o.HTotal} {o.Height} - - {o.VTotal} {o.PixelRate / 1000}" : $"{o.Width} - - - {o.Height} - - - -";
                Console.WriteLine($"  #0 {o.Width}x{o.Height} {o.Refresh:0.00} {timing} flags: ; type: ");
                Console.WriteLine($"  device: {o.Device}");
            }
            Console.WriteLine();
        }
        if (planes)
        {
            Console.WriteLine("Planes:\nid\tcrtc\tfb\tCRTC x,y\tx,y\tgamma size\tpossible crtcs");
            foreach (var o in outputs)
                Console.WriteLine($"{o.Crtc + 10}\t{o.Crtc}\t{o.Crtc + 20}\t{o.X},{o.Y}\t\t0,0\t0       \t0x{1 << (o.Crtc - 31):x8}\n  formats: XR24 AR24");
            Console.WriteLine();
        }
        if (framebuffers)
        {
            Console.WriteLine("Frame buffers:\nid\tsize\tpitch");
            foreach (var o in outputs) Console.WriteLine($"{o.Crtc + 20}\t({o.Width}x{o.Height})\t{o.Width * 4}");
            Console.WriteLine();
        }
        return 0;
    }

    // modetest -s <connector_id>[@<crtc_id>]:<WxH>[-<vrefresh>]: switch the connector's display to
    // that mode until Enter is pressed, then restore it. The change is temporary (CDS_FULLSCREEN, not
    // saved to the registry), like a KMS mode set that ends with the program.
    static int SetMode(string spec)
    {
        var m = Regex.Match(spec, @"^(\d+)(?:@\d+)?:(\d+)x(\d+)(?:[ie])?(?:-(\d+(?:\.\d+)?))?$");
        if (!m.Success) throw new UsageException($"invalid mode specifier '{spec}'");
        var output = Outputs().FirstOrDefault(o => o.Connector == int.Parse(m.Groups[1].Value));
        if (output?.Device == null) { Console.Error.WriteLine($"failed to find connector {m.Groups[1].Value}"); return 1; }
        int w = int.Parse(m.Groups[2].Value), h = int.Parse(m.Groups[3].Value);
        var hz = m.Groups[4].Success ? (int)Math.Round(double.Parse(m.Groups[4].Value, System.Globalization.CultureInfo.InvariantCulture)) : 0;
        var mode = output.Modes.FirstOrDefault(x => x.W == w && x.H == h && (hz == 0 || x.Hz == hz));
        if (mode.W == 0) { Console.Error.WriteLine($"failed to find mode \"{w}x{h}{(hz > 0 ? $"-{hz}" : "")}\" for connector {output.Connector}"); return 1; }
        var dm = new byte[220];
        BitConverter.TryWriteBytes(dm.AsSpan(68), (ushort)220);
        BitConverter.TryWriteBytes(dm.AsSpan(72), 0x00040000u | 0x00080000u | 0x00100000u | 0x00400000u);   // BITSPERPEL | PELSWIDTH | PELSHEIGHT | DISPLAYFREQUENCY
        BitConverter.TryWriteBytes(dm.AsSpan(168), mode.Bpp);
        BitConverter.TryWriteBytes(dm.AsSpan(172), mode.W);
        BitConverter.TryWriteBytes(dm.AsSpan(176), mode.H);
        BitConverter.TryWriteBytes(dm.AsSpan(184), mode.Hz);
        var r = ChangeDisplaySettingsExW(output.Device, dm, IntPtr.Zero, 0x4, IntPtr.Zero);   // CDS_FULLSCREEN
        if (r != 0) { Console.Error.WriteLine($"failed to set mode: error {r}"); return 1; }
        Console.WriteLine($"setting mode {mode.W}x{mode.H}-{mode.Hz}Hz on connectors {output.Connector}, crtc {output.Crtc}");
        Console.WriteLine("press enter to restore the previous mode");
        Console.ReadLine();
        ChangeDisplaySettingsExW(output.Device, null, IntPtr.Zero, 0, IntPtr.Zero);
        return 0;
    }

    static string EncoderType(string connector) => connector.Split('-')[0] switch
    {
        "VGA" => "DAC",
        "HDMI" or "DVI" => "TMDS",
        "DP" or "eDP" => "DPMST",
        "LVDS" => "LVDS",
        "Virtual" or "Miracast" => "Virtual",
        "SVIDEO" or "Composite" or "Component" or "TV" => "TVDAC",
        _ => "none",
    };

    static int RunModeprint(string[] args)
    {
        if (args.Length > 0 && args[0] is "-h" or "--help") { Console.WriteLine("Usage: modeprint [-full] [-crtcs] [-connectors] [-modes]"); return 0; }
        var outputs = Outputs();
        Console.WriteLine($"Resources\n\ncount_connectors : {outputs.Count}\ncount_encoders   : {outputs.Count}\ncount_crtcs      : {outputs.Count}\ncount_fbs        : {outputs.Count}\n");
        foreach (var o in outputs)
        {
            Console.WriteLine($"Connector: {o.Type}\n\tid             : {o.Connector}\n\tencoder id     : {o.Connector - 10}\n\tconn           : connected\n\tsize           : 0x0 (mm)\n\tcount_modes    : {o.Modes.Count}");
            foreach (var (w, h, hz, _) in o.Modes) Console.WriteLine($"Mode: \"{w}x{h}\"\n\tclock       : -\n\thdisplay    : {w}\n\tvdisplay    : {h}\n\tvrefresh    : {hz}");
            Console.WriteLine($"Crtc\n\tid             : {o.Crtc}\n\tx              : {o.X}\n\ty              : {o.Y}\n\twidth          : {o.Width}\n\theight         : {o.Height}\n\tmode           : {o.Width}x{o.Height}@{o.Refresh:0}\n");
        }
        return 0;
    }

    // ---- Win32 ----

    readonly record struct DisplayDevice(string Name, string String, string DeviceId);

    static List<DisplayDevice> DisplayDevices()
    {
        var list = new List<DisplayDevice>();
        var dd = new byte[840];   // DISPLAY_DEVICEW: cb, DeviceName[32], DeviceString[128], StateFlags, DeviceID[128], DeviceKey[128]
        for (uint i = 0; ; i++)
        {
            Array.Clear(dd);
            BitConverter.TryWriteBytes(dd.AsSpan(0), dd.Length);
            if (!EnumDisplayDevicesW(null, i, dd, 0)) break;
            string W(int off, int chars) => Encoding.Unicode.GetString(dd, off, chars * 2).Split('\0')[0];
            list.Add(new DisplayDevice(W(4, 32), W(68, 128), W(328, 128)));
        }
        return list;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SP_DEVINFO_DATA
    {
        public int cbSize;
        public Guid ClassGuid;
        public uint DevInst;
        public IntPtr Reserved;
    }

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr SetupDiGetClassDevsW(ref Guid classGuid, string enumerator, IntPtr parent, uint flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    static extern bool SetupDiEnumDeviceInfo(IntPtr set, uint index, ref SP_DEVINFO_DATA data);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool SetupDiGetDeviceRegistryPropertyW(IntPtr set, ref SP_DEVINFO_DATA data, uint property, out uint type, byte[] buffer, int size, out uint required);

    [DllImport("setupapi.dll")]
    static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern bool EnumDisplayDevicesW(string device, uint index, [In, Out] byte[] displayDevice, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern bool EnumDisplaySettingsExW(string device, int mode, [In, Out] byte[] devMode, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int ChangeDisplaySettingsExW(string device, byte[] devMode, IntPtr hwnd, uint flags, IntPtr param);

    [DllImport("user32.dll")]
    static extern int GetDisplayConfigBufferSizes(uint flags, out int numPaths, out int numModes);

    [DllImport("user32.dll")]
    static extern int QueryDisplayConfig(uint flags, ref int numPaths, [Out] byte[] paths, ref int numModes, [Out] byte[] modes, IntPtr topology);

    [DllImport("user32.dll")]
    static extern int DisplayConfigGetDeviceInfo([In, Out] byte[] packet);
}
