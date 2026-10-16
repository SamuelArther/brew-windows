using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace WinTools;

// ios-sim: the iOS Simulator launcher. The simulator itself (CoreSimulator and its runtimes) ships
// only inside Xcode on macOS, so there are no SDKs or device types to list here, and launch/install/
// start stop with that explanation after checking the app bundle they were given (Info.plist, bundle
// id, executable, simulator vs device build). Also holds the plist reader (XML and binary) that
// bluepill and baguette share.
static class IosSim
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("ios-sim", "Command-line application launcher for the iOS Simulator", Run);
    }

    internal const string NoSimulator = "the iOS Simulator is part of Xcode and only runs on macOS";

    const string Usage = """
        Usage: ios-sim <command> <options> [--args ...]

        Commands:
          showsdks                        List the available iOS SDK versions
          showdevicetypes                 List the available device types
          launch <application path>       Launch the application at the specified path on the iOS Simulator
          start                           Launch iOS Simulator without an app
          install <application path>      Install the application at the specified path on the iOS Simulator without launching the app

        Options:
          --version                       Print the version of ios-sim
          --help                          Show this help text
          --exit                          Exit after startup
          --log <log file path>           The path where log of the app running in the Simulator will be redirected to
          --devicetypeid <device type>    The id of the device type that should be simulated (Xcode6+). Use 'showdevicetypes' to list devices.
                                          e.g "com.apple.CoreSimulator.SimDeviceType.iPhone-15, 17.0"
          --timeout <seconds>             The timeout time to wait for a response from the Simulator. Default value: 30 seconds
          --setenv NAME=VALUE             Set an environment variable
          --args <...>                    All following arguments will be passed on to the application

        The iOS Simulator is part of Xcode, so on Windows there are no SDKs or device types and apps
        are checked but cannot be launched.
        """;

    static int Run(string[] args)
    {
        string command = null, app = null, deviceType = null;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string Next() => ++i < args.Length ? args[i] : throw new UsageException($"{arg} needs a value");
            switch (arg)
            {
                case "--help" or "-h" or "help": Console.WriteLine(Usage); return 0;
                case "--version" or "-v": Console.WriteLine("9.0.0"); return 0;
                case "--exit" or "--verbose": break;
                case "--log" or "--timeout" or "--setenv" or "--env" or "--sdk" or "--family" or "--stdout" or "--stderr": Next(); break;
                case "--devicetypeid":
                    deviceType = Next();
                    if (!Regex.IsMatch(deviceType, @"^(com\.apple\.CoreSimulator\.SimDeviceType\.)?[\w.-]+(,\s*\d+(\.\d+)*)?$"))
                        throw new UsageException($"invalid --devicetypeid '{deviceType}' (e.g. \"com.apple.CoreSimulator.SimDeviceType.iPhone-15, 17.0\")");
                    break;
                case "--args": i = args.Length; break;
                default:
                    if (arg.StartsWith('-')) throw new UsageException($"unknown option {arg}\n{Usage}");
                    if (command == null) command = arg;
                    else if (app == null) app = arg;
                    else throw new UsageException($"unexpected argument {arg}");
                    break;
            }
        }
        switch (command)
        {
            case null: Console.WriteLine(Usage); return 1;
            case "showsdks" or "showdevicetypes":
                // CoreSimulator runtimes live in Xcode; Windows has none to list.
                Console.Error.WriteLine($"ios-sim: no simulator runtimes are installed: {NoSimulator}");
                return 0;
            case "start":
                Console.Error.WriteLine($"ios-sim: cannot start a simulator: {NoSimulator}");
                return 1;
            case "launch" or "install":
                if (app == null) throw new UsageException($"{command} needs an application path");
                var info = ReadAppInfo(app);
                Console.Error.WriteLine($"ios-sim: {info.Id} ({Path.GetFileName(app.TrimEnd('/', '\\'))}, {info.Platform} build) " +
                                        $"cannot be {(command == "launch" ? "launched" : "installed")}: {NoSimulator}");
                return 1;
            default: throw new UsageException($"unknown command {command}\n{Usage}");
        }
    }

    internal record AppInfo(string Id, string Name, string Executable, string Version, string Platform, Dictionary<string, object> Plist);

    // A .app bundle's Info.plist: iOS bundles keep it at the top level (macOS ones in Contents/).
    internal static AppInfo ReadAppInfo(string app)
    {
        if (!Directory.Exists(app)) throw new InvalidOperationException($"{app}: application bundle not found");
        var plistPath = new[] { Path.Combine(app, "Info.plist"), Path.Combine(app, "Contents", "Info.plist") }.FirstOrDefault(File.Exists)
                        ?? throw new InvalidOperationException($"{app}: no Info.plist; not an application bundle");
        if (ReadPlist(File.ReadAllBytes(plistPath)) is not Dictionary<string, object> plist)
            throw new InvalidOperationException($"{plistPath}: Info.plist is not a dictionary");
        var id = plist.GetValueOrDefault("CFBundleIdentifier") as string ?? throw new InvalidOperationException($"{plistPath}: no CFBundleIdentifier");
        var platforms = plist.GetValueOrDefault("CFBundleSupportedPlatforms") as List<object>;
        var platform = platforms?.OfType<string>().FirstOrDefault() ?? plist.GetValueOrDefault("DTPlatformName") as string ?? "unknown platform";
        return new AppInfo(id, plist.GetValueOrDefault("CFBundleName") as string ?? Path.GetFileNameWithoutExtension(app),
                           plist.GetValueOrDefault("CFBundleExecutable") as string, plist.GetValueOrDefault("CFBundleShortVersionString") as string,
                           platform, plist);
    }

    // ---- property lists ----
    // Values come back as Dictionary<string, object>, List<object>, string, long, double, bool,
    // byte[] and DateTime.

    internal static object ReadPlist(byte[] data)
    {
        if (data.Length >= 8 && Encoding.ASCII.GetString(data, 0, 8) == "bplist00") return new BinaryPlist(data).Read();
        var doc = XDocument.Parse(Encoding.UTF8.GetString(data), LoadOptions.None);
        var root = doc.Root?.Name.LocalName == "plist" ? doc.Root.Elements().FirstOrDefault() : doc.Root;
        return root == null ? null : FromXml(root);
    }

    static object FromXml(XElement e) => e.Name.LocalName switch
    {
        "dict" => DictFromXml(e),
        "array" => e.Elements().Select(FromXml).ToList(),
        "string" => e.Value,
        "integer" => long.Parse(e.Value.Trim()),
        "real" => double.Parse(e.Value.Trim(), System.Globalization.CultureInfo.InvariantCulture),
        "true" => true,
        "false" => false,
        "date" => DateTime.Parse(e.Value.Trim(), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal),
        "data" => Convert.FromBase64String(Regex.Replace(e.Value, @"\s", "")),
        _ => throw new InvalidOperationException($"unknown plist element <{e.Name.LocalName}>"),
    };

    static Dictionary<string, object> DictFromXml(XElement e)
    {
        var dict = new Dictionary<string, object>();
        var children = e.Elements().ToList();
        for (var i = 0; i + 1 < children.Count; i += 2)
            if (children[i].Name.LocalName == "key") dict[children[i].Value] = FromXml(children[i + 1]);
        return dict;
    }

    // bplist00: objects addressed through an offset table described by the 32-byte trailer.
    sealed class BinaryPlist(byte[] data)
    {
        int offsetSize, refSize;
        long[] offsets;

        public object Read()
        {
            if (data.Length < 40) throw new InvalidOperationException("truncated binary plist");
            var t = data.AsSpan(data.Length - 32);
            offsetSize = t[6];
            refSize = t[7];
            var count = (long)BinaryPrimitives.ReadUInt64BigEndian(t[8..]);
            var top = (long)BinaryPrimitives.ReadUInt64BigEndian(t[16..]);
            var table = (long)BinaryPrimitives.ReadUInt64BigEndian(t[24..]);
            if (count <= 0 || count > data.Length || table + count * offsetSize > data.Length) throw new InvalidOperationException("corrupt binary plist");
            offsets = new long[count];
            for (var i = 0; i < count; i++) offsets[i] = (long)ReadUInt(table + i * offsetSize, offsetSize);
            return Object(top, 0);
        }

        ulong ReadUInt(long pos, int size)
        {
            ulong v = 0;
            for (var i = 0; i < size; i++) v = (v << 8) | data[pos + i];
            return v;
        }

        // Count from the low nibble, or from the int object that follows when it is 0xF.
        (long Count, long Start) Length(long pos)
        {
            var n = data[pos] & 0xF;
            if (n != 0xF) return (n, pos + 1);
            var size = 1 << (data[pos + 1] & 0xF);
            return ((long)ReadUInt(pos + 2, size), pos + 2 + size);
        }

        object Object(long index, int depth)
        {
            if (depth > 64 || index < 0 || index >= offsets.Length) throw new InvalidOperationException("corrupt binary plist");
            var pos = offsets[index];
            var marker = data[pos];
            switch (marker >> 4)
            {
                case 0x0: return marker switch { 0x08 => false, 0x09 => true, _ => null };
                case 0x1:
                    return (long)ReadUInt(pos + 1, Math.Min(8, 1 << (marker & 0xF)));
                case 0x2:
                    return (marker & 0xF) == 2
                        ? (double)BinaryPrimitives.ReadSingleBigEndian(data.AsSpan((int)pos + 1))
                        : BinaryPrimitives.ReadDoubleBigEndian(data.AsSpan((int)pos + 1));
                case 0x3:
                    return new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(BinaryPrimitives.ReadDoubleBigEndian(data.AsSpan((int)pos + 1)));
                case 0x4:
                {
                    var (count, start) = Length(pos);
                    return data.AsSpan((int)start, (int)count).ToArray();
                }
                case 0x5:
                {
                    var (count, start) = Length(pos);
                    return Encoding.ASCII.GetString(data, (int)start, (int)count);
                }
                case 0x6:
                {
                    var (count, start) = Length(pos);
                    return Encoding.BigEndianUnicode.GetString(data, (int)start, (int)count * 2);
                }
                case 0x8: return (long)ReadUInt(pos + 1, (marker & 0xF) + 1);
                case 0xA:
                {
                    var (count, start) = Length(pos);
                    var list = new List<object>();
                    for (var i = 0; i < count; i++) list.Add(Object((long)ReadUInt(start + i * refSize, refSize), depth + 1));
                    return list;
                }
                case 0xD:
                {
                    var (count, start) = Length(pos);
                    var dict = new Dictionary<string, object>();
                    for (var i = 0; i < count; i++)
                    {
                        var key = Object((long)ReadUInt(start + i * refSize, refSize), depth + 1)?.ToString() ?? "";
                        dict[key] = Object((long)ReadUInt(start + (count + i) * refSize, refSize), depth + 1);
                    }
                    return dict;
                }
                default: throw new InvalidOperationException($"unsupported binary plist object 0x{marker:x2}");
            }
        }
    }
}
