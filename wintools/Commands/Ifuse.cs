using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace WinTools;

// ifuse: finds iOS devices through Apple's usbmuxd (the Apple Mobile Device Service that iTunes and
// the Apple Devices app install, on 127.0.0.1:27015). Mounting a device's files needs the lockdown
// pairing and AFC protocols of libimobiledevice on top of that, which wintools does not carry.
static class Ifuse
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("ifuse", "Find iOS devices for mounting (via Apple Mobile Device Service)", Run);
    }

    const string Usage = """
        Usage: ifuse MOUNTPOINT [OPTIONS]
        Mount directories of an iOS device locally.

        OPTIONS:
          -o opt,[opt...]       mount options
          -u, --udid UDID       mount specific device by UDID
          -n, --network         connect to network device
          -h, --help            print usage information
          -V, --version         print version information
          -d, --debug           enable communication debugging
          --documents APPID     mount 'Documents' folder of app identified by APPID
          --container APPID     mount sandbox root of an app identified by APPID
          --list-apps           list installed apps that have file sharing enabled
          --root                mount root file system (jailbroken device required)

        Devices are found through Apple Mobile Device Service (installed with the Apple Devices app or iTunes).
        """;

    static int Run(string[] args)
    {
        string udid = null;
        bool network = false, debug = false;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help": Console.WriteLine(Usage); return 0;
                case "-V" or "--version": Console.WriteLine($"ifuse {Commands.Version} (wintools)"); return 0;
                case "-u" or "--udid":
                    if (++i >= args.Length) throw new UsageException("--udid needs a UDID");
                    udid = args[i];
                    break;
                case "-n" or "--network": network = true; break;
                case "-d" or "--debug": debug = true; break;
                case "-o" or "--documents" or "--container":
                    if (++i >= args.Length) throw new UsageException($"{args[i - 1]} needs a value");
                    break;
                case "--list-apps" or "--root": break;
                default:
                    if (args[i].StartsWith('-') && !args[i].StartsWith("-o")) throw new UsageException($"unknown option {args[i]}\n{Usage}");
                    break;
            }
        }
        var devices = ListDevices(debug).Where(d => network ? d.Connection == "Network" : d.Connection != "Network").ToList();
        if (udid != null && !devices.Any(d => d.Udid.Equals(udid, StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine($"ERROR: Device {udid} not found!");
            return 1;
        }
        if (devices.Count == 0)
        {
            Console.WriteLine("ERROR: No device found!");
            return 1;
        }
        var device = udid ?? devices[0].Udid;
        Console.Error.WriteLine($"ifuse: found device {device}, but reading its files needs libimobiledevice's lockdown/AFC protocols, " +
                                "which aren't available here; use Explorer (Apple iPhone > Internal Storage) or the Apple Devices app.");
        return 1;
    }

    // usbmuxd's ListDevices request: a 16-byte header (length, version 1, type 8 = plist, tag) and an XML plist.
    internal static List<(string Udid, string Connection)> ListDevices(bool debug)
    {
        var devices = new List<(string, string)>();
        try
        {
            using var client = new TcpClient();
            if (!client.ConnectAsync("127.0.0.1", 27015).Wait(2000)) return devices;
            using var stream = client.GetStream();
            stream.ReadTimeout = 5000;
            var plist = Encoding.UTF8.GetBytes("""
                <?xml version="1.0" encoding="UTF-8"?>
                <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
                <plist version="1.0"><dict><key>MessageType</key><string>ListDevices</string><key>ClientVersionString</key><string>wintools-ifuse</string><key>ProgName</key><string>ifuse</string></dict></plist>
                """);
            var header = new byte[16];
            BitConverter.TryWriteBytes(header.AsSpan(0), 16 + plist.Length);
            BitConverter.TryWriteBytes(header.AsSpan(4), 1);
            BitConverter.TryWriteBytes(header.AsSpan(8), 8);
            BitConverter.TryWriteBytes(header.AsSpan(12), 1);
            stream.Write(header);
            stream.Write(plist);
            var replyHeader = new byte[16];
            stream.ReadExactly(replyHeader);
            var length = BitConverter.ToInt32(replyHeader, 0) - 16;
            if (length <= 0 || length > 1 << 20) return devices;
            var body = new byte[length];
            stream.ReadExactly(body);
            var xml = Encoding.UTF8.GetString(body);
            if (debug) Console.Error.WriteLine(xml);
            foreach (Match m in Regex.Matches(xml, @"<key>Properties</key>\s*<dict>(.*?)</dict>", RegexOptions.Singleline))
            {
                var serial = Regex.Match(m.Groups[1].Value, @"<key>SerialNumber</key>\s*<string>([^<]*)</string>");
                var connection = Regex.Match(m.Groups[1].Value, @"<key>ConnectionType</key>\s*<string>([^<]*)</string>");
                if (serial.Success) devices.Add((serial.Groups[1].Value, connection.Success ? connection.Groups[1].Value : "USB"));
            }
        }
        catch (Exception e) when (e is SocketException or IOException or AggregateException)
        {
            if (debug) Console.Error.WriteLine($"ifuse: usbmuxd: {e.Message}");
        }
        return devices;
    }
}
