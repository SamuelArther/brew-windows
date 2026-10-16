using System.Buffers.Binary;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;

namespace WinTools;

// ios-deploy and mobiledevice: talk to connected iOS devices. Both need Apple's Mobile Device
// support (installed with iTunes or the "Apple Devices" app); its usbmuxd listens on TCP
// 127.0.0.1:27015 on Windows. Device discovery (list/detect) speaks the usbmux plist protocol
// directly. Anything past discovery - installing apps, reading lockdownd properties, file
// transfer - needs a paired lockdownd/TLS session that only Apple's full stack provides, so those
// subcommands report that clearly and exit 1 (matching the brief: implement what can exist).
static class MobileDevice
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("ios-deploy", "Detect connected iOS devices (install/debug need Apple's full stack)", IosDeploy);
        Commands.Add("mobiledevice", "List connected iOS devices and query the Apple Mobile Device service", Mobiledevice);
    }

    // ---- usbmux client ----

    sealed record Device(int DeviceId, string Udid, string ConnectionType);

    const int UsbmuxPort = 27015;

    static List<Device> ListDevices(int timeoutMs)
    {
        using var client = new TcpClient();
        var connect = client.BeginConnect("127.0.0.1", UsbmuxPort, null, null);
        if (!connect.AsyncWaitHandle.WaitOne(Math.Max(300, timeoutMs)))
            throw new NoAppleStackException();
        try { client.EndConnect(connect); }
        catch (SocketException) { throw new NoAppleStackException(); }

        var stream = client.GetStream();
        stream.ReadTimeout = Math.Max(1000, timeoutMs);
        var request = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">" +
            "<plist version=\"1.0\"><dict>" +
            "<key>MessageType</key><string>ListDevices</string>" +
            "<key>ClientVersionString</key><string>wintools-usbmux</string>" +
            "<key>ProgName</key><string>wintools</string>" +
            "<key>kLibUSBMuxVersion</key><integer>3</integer>" +
            "</dict></plist>";
        Send(stream, request);
        var reply = Receive(stream);
        return reply == null ? [] : ParseDeviceList(reply);
    }

    static void Send(NetworkStream stream, string plist)
    {
        var payload = Encoding.UTF8.GetBytes(plist);
        var header = new byte[16];
        BinaryPrimitives.WriteUInt32LittleEndian(header, (uint)(16 + payload.Length));   // length
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(4), 1);                    // version
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(8), 8);                    // message type: plist
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(12), 1);                   // tag
        stream.Write(header);
        stream.Write(payload);
    }

    static string Receive(NetworkStream stream)
    {
        var header = ReadExact(stream, 16);
        if (header == null) return null;
        var length = BinaryPrimitives.ReadUInt32LittleEndian(header);
        if (length is < 16 or > 1 << 20) return null;
        var payload = ReadExact(stream, (int)length - 16);
        return payload == null ? null : Encoding.UTF8.GetString(payload);
    }

    static byte[] ReadExact(NetworkStream stream, int count)
    {
        var buffer = new byte[count];
        var read = 0;
        try
        {
            while (read < count)
            {
                var n = stream.Read(buffer, read, count - read);
                if (n <= 0) return null;
                read += n;
            }
        }
        catch (IOException) { return null; }
        return buffer;
    }

    // Walk the reply plist, pulling DeviceID / SerialNumber / ConnectionType out of each entry.
    static List<Device> ParseDeviceList(string xml)
    {
        var devices = new List<Device>();
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null };
        try
        {
            using var reader = XmlReader.Create(new StringReader(xml), settings);
            string key = null;
            int deviceId = -1;
            string udid = null, connection = null;
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "key")
                {
                    key = reader.ReadElementContentAsString();
                    continue;
                }
                if (reader.NodeType == XmlNodeType.Element && reader.Name is "string" or "integer")
                {
                    var value = reader.ReadElementContentAsString();
                    switch (key)
                    {
                        case "DeviceID" when int.TryParse(value, out var id): deviceId = id; break;
                        case "SerialNumber": udid = value; break;
                        case "ConnectionType": connection = value; break;
                    }
                    // SerialNumber closes an entry in usbmuxd's Properties dict.
                    if (key == "SerialNumber" && udid != null)
                    {
                        devices.Add(new Device(deviceId, udid, connection ?? "USB"));
                        deviceId = -1; udid = null; connection = null;
                    }
                    key = null;
                }
            }
        }
        catch (XmlException) { }
        return devices;
    }

    sealed class NoAppleStackException() : Exception(
        "Apple Mobile Device support is not available (install iTunes or the Apple Devices app, " +
        "and make sure Apple Mobile Device Service is running).");

    // For detection/listing, a missing Apple stack is reported on stderr but counts as "no devices"
    // (exit 0): the tool ran and simply found nothing, like running it with no device attached.
    static int WithDevices(int timeoutMs, Func<List<Device>, int> body)
    {
        List<Device> devices;
        try { devices = ListDevices(timeoutMs); }
        catch (NoAppleStackException e) { Console.Error.WriteLine(e.Message); devices = []; }
        return body(devices);
    }

    // ---- ios-deploy ----

    const string IosDeployVersion = "1.12.2";

    static int IosDeploy(string[] args)
    {
        var timeout = 1000;
        string unsupported = null;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            switch (a)
            {
                case "-h" or "--help": IosDeployHelp(); return 0;
                case "-V" or "--version": Console.WriteLine(IosDeployVersion); return 0;
                case "-c" or "--detect": break;   // detection is the default action
                case "-t" or "--timeout": if (++i < args.Length && int.TryParse(args[i], out var t)) timeout = t * 1000; break;
                case "-W" or "--no-wifi" or "-u" or "--unbuffered" or "-j" or "--json": break;
                case "-i" or "--id": i++; break;
                // Everything past discovery needs a paired lockdownd session.
                case "-b" or "--bundle" or "-d" or "--debug" or "-I" or "--noninteractive" or "-L" or "--justlaunch"
                     or "-r" or "--uninstall" or "-9" or "--uninstall_only" or "-1" or "--bundle_id" or "-l" or "--list"
                     or "-o" or "--upload" or "-w" or "--download" or "-D" or "--mkdir" or "-R" or "--rm" or "-X" or "--rmtree"
                     or "-e" or "--exists" or "-B" or "--list_bundle_id" or "-P" or "--list_profiles" or "--check-developer-mode":
                    unsupported = a;
                    break;
                default:
                    if (a.StartsWith('-')) unsupported ??= a;
                    break;
            }
        }
        if (unsupported != null)
        {
            Console.Error.WriteLine($"ios-deploy: {unsupported} needs a paired lockdownd session, which only Apple's Mobile Device");
            Console.Error.WriteLine("stack provides. On Windows, ios-deploy supports device detection (-c) only; use the Apple");
            Console.Error.WriteLine("Devices app, Xcode on a Mac, or pymobiledevice3 for install/debug/file operations.");
            return 1;
        }
        // Default behaviour, like ios-deploy with no action, is to detect.
        return WithDevices(timeout, devices =>
        {
            if (devices.Count == 0)
            {
                Console.WriteLine("[....] Waiting up to " + timeout / 1000 + "s for iOS device to be connected");
                Console.WriteLine("[....] No iOS device found.");
                return 0;
            }
            foreach (var d in devices)
                Console.WriteLine($"[....] Found {d.Udid} ({d.ConnectionType}) connected through {(d.ConnectionType == "Network" ? "WiFi" : "USB")}.");
            return 0;
        });
    }

    static void IosDeployHelp() => Console.WriteLine("""
        Usage: ios-deploy [OPTION]...

        Detect connected iOS devices on Windows (via Apple Mobile Device usbmuxd).

          -c, --detect                 list all connected devices
          -t, --timeout <timeout>      seconds to wait for a device to be connected
          -i, --id <device_id>         the id of the device to connect to
          -W, --no-wifi                ignore wifi devices
          -V, --version                print the executable version
          -h, --help                   show this help

        Install, debug, upload, download and bundle-id operations require a paired lockdownd
        session that only Apple's full Mobile Device stack provides; they are not available on
        Windows. Use the Apple Devices app, Xcode on a Mac, or pymobiledevice3 for those.
        """);

    // ---- mobiledevice ----

    const string MobiledeviceVersion = "2.0.0";

    static int Mobiledevice(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help") { MobiledeviceHelp(); return 0; }
        var command = args[0];
        var (timeout, limit) = (1000, int.MaxValue);
        for (var i = 1; i < args.Length; i++)
        {
            if (args[i] == "-t" && i + 1 < args.Length && int.TryParse(args[++i], out var t)) timeout = t;
            else if (args[i] == "-n" && i + 1 < args.Length && int.TryParse(args[++i], out var n)) limit = n;
        }
        switch (command)
        {
            case "version":
                Console.WriteLine(args.Contains("-r") ? MobiledeviceVersion + " (wintools)" : MobiledeviceVersion);
                return 0;
            case "list_devices":
                return WithDevices(timeout, devices =>
                {
                    foreach (var d in devices.Take(limit)) Console.WriteLine(d.Udid);
                    return 0;
                });
            // Property, app and tunnel commands all need a paired lockdownd session.
            case "list_device_props" or "get_device_prop" or "list_apps" or "list_app_props"
                 or "get_app_prop" or "install_app" or "uninstall_app" or "tunnel" or "get_bundle_id":
                Console.Error.WriteLine($"mobiledevice: {command} needs a paired lockdownd session, which only Apple's Mobile Device");
                Console.Error.WriteLine("stack provides. On Windows, mobiledevice supports list_devices and version only; use the");
                Console.Error.WriteLine("Apple Devices app, Xcode on a Mac, or pymobiledevice3 for app and property operations.");
                return 1;
            default:
                throw new UsageException($"unknown command '{command}' (see mobiledevice help)");
        }
    }

    static void MobiledeviceHelp() => Console.WriteLine("""
        mobiledevice help
          Display this help screen

        mobiledevice version [options]
          Display program version. Options: -r (include revision)

        mobiledevice list_devices [options]
          Display UDID of each connected device.
          Options: -t <timeout> (ms to wait), -n <count> (limit)

        Detection speaks the Apple Mobile Device usbmuxd protocol (TCP 127.0.0.1:27015, installed
        with iTunes or the Apple Devices app). Property, app-management and tunnel commands need a
        paired lockdownd session that only Apple's full stack provides, and are not available on
        Windows; use the Apple Devices app, Xcode on a Mac, or pymobiledevice3 for those.
        """);
}
