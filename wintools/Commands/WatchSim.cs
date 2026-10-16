using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace WinTools;

// watch-sim: launch a watch app in a simulator. Apple's watchOS simulator doesn't exist on Windows;
// the watch platform whose emulator does is Wear OS (the Android Emulator). watch-sim installs an APK
// on a running Wear OS emulator with adb and launches it: the main activity (read from the APK's
// binary AndroidManifest.xml), or a tile, complication or watch face through Wear OS's debug surface,
// or posts a notification built from an APNs-style payload (watch-sim's -n).
static class WatchSim
{
    const string Version = "1.0.0";

    [ModuleInitializer]
    internal static void Register() => Commands.Add("watch-sim", "Launch a watch app (APK) on a running Wear OS emulator", Run);

    const string Usage = """
        Usage: watch-sim path/to/app.apk [-t main|tile|complication|watchface|notification]
                         [-n notification.json] [-s api-level] [-d serial] [--list]

          -t, --type          what to launch (default: main)
          -n, --notification  APNs-style payload ({"aps":{"alert":{"title":"..","body":".."}}}) to post
          -s, --sdk           use the Wear OS emulator running this API level
          -d, --device        use this adb serial (default: the first Wear OS emulator)
          -l, --list          list running Wear OS emulators/devices
          -i, --info          print what the APK offers to launch (no emulator needed)
          -h, --help          show this help

        Needs adb (Android SDK platform-tools: ANDROID_HOME, ANDROID_SDK_ROOT or PATH) and a running
        Wear OS emulator (emulator -avd <your Wear OS AVD>).
        """;

    static int Run(string[] args)
    {
        if (args.Length == 0) { Console.Error.WriteLine(Usage); return 1; }
        string apk = null, type = "main", payload = null, sdk = null, serial = null;
        bool list = false, info = false;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Next() => i + 1 < args.Length ? args[++i] : throw new UsageException($"{a} needs a value");
            switch (a)
            {
                case "-h" or "--help": Console.WriteLine(Usage); return 0;
                case "-v" or "--version": Console.WriteLine(Version); return 0;
                case "-t" or "--type": type = Next().ToLowerInvariant(); break;
                case "-n" or "--notification": payload = Next(); type = "notification"; break;
                case "-s" or "--sdk": sdk = Next(); break;
                case "-d" or "--device": serial = Next(); break;
                case "-l" or "--list": list = true; break;
                case "-i" or "--info": info = true; break;
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unknown option {a}");
                    apk = a;
                    break;
            }
        }
        if (type == "glance") type = "tile";   // watchOS glances became Wear OS tiles
        if (type is not ("main" or "tile" or "complication" or "watchface" or "notification"))
            throw new UsageException($"unknown launch type {type}");

        WsManifest manifest = null;
        if (!list)
        {
            if (apk == null) { Console.Error.WriteLine(Usage); return 1; }
            if (!File.Exists(apk)) throw new IOException($"{apk}: No such file");
            manifest = WsManifest.Read(apk);
            if (info)
            {
                Console.WriteLine($"package:      {manifest.Package}");
                Console.WriteLine($"main:         {manifest.LauncherActivity ?? "(none)"}");
                Console.WriteLine($"tile:         {manifest.Tile ?? "(none)"}");
                Console.WriteLine($"complication: {manifest.Complication ?? "(none)"}");
                Console.WriteLine($"watchface:    {manifest.WatchFace ?? "(none)"}");
                return 0;
            }
        }

        var adb = FindAdb() ?? throw new InvalidOperationException("adb was not found; install Android SDK platform-tools and set ANDROID_HOME");
        var watches = Watches(adb);
        if (list)
        {
            foreach (var (s, api, model) in watches) Console.WriteLine($"{s}\tAPI {api}\t{model}");
            return 0;
        }
        var target = watches.FirstOrDefault(w => (serial == null || w.Serial == serial) && (sdk == null || w.Api == sdk));
        if (target.Serial == null)
        {
            Console.Error.WriteLine("watch-sim: no running Wear OS emulator" + (sdk != null ? $" with API {sdk}" : "") + (serial != null ? $" named {serial}" : "") + " was found.");
            Console.Error.WriteLine("Start one with: emulator -avd <Wear OS AVD>   (create it in Android Studio's Device Manager)");
            return 1;
        }

        Console.WriteLine($"Installing {manifest.Package} on {target.Serial} (API {target.Api})...");
        var (code, output) = Adb(adb, "-s", target.Serial, "install", "-r", Path.GetFullPath(apk));
        if (code != 0 || !output.Contains("Success")) { Console.Error.Write(output); return 1; }

        switch (type)
        {
            case "main":
                if (manifest.LauncherActivity is { } activity)
                    return Shell(adb, target.Serial, "am", "start", "-n", $"{manifest.Package}/{activity}");
                return Shell(adb, target.Serial, "monkey", "-p", manifest.Package, "-c", "android.intent.category.LAUNCHER", "1");
            case "tile" or "complication" or "watchface":
            {
                var service = type switch
                {
                    "tile" => manifest.Tile,
                    "complication" => manifest.Complication,
                    _ => manifest.WatchFace,
                } ?? throw new InvalidOperationException($"{manifest.Package} declares no {type} service");
                var operation = type switch { "tile" => "add-tile", "complication" => "set-complication", _ => "set-watchface" };
                var extra = type == "complication" ? new[] { "--ei", "slot", "0", "--ei", "type", "3" } : [];
                return Shell(adb, target.Serial, ["am", "broadcast", "-a", "com.google.android.wearable.app.DEBUG_SURFACE",
                    "--es", "operation", operation, "--ecn", "component", $"{manifest.Package}/{service}", .. extra]);
            }
            default:
            {
                var (title, body) = NotificationText(payload, manifest.Package);
                return Shell(adb, target.Serial, "cmd", "notification", "post", "-S", "bigtext", "-t", title, "watch-sim", body);
            }
        }
    }

    // {"aps": {"alert": "text"}} or {"aps": {"alert": {"title": .., "body": ..}}}
    static (string Title, string Body) NotificationText(string payload, string fallbackTitle)
    {
        if (payload == null) return (fallbackTitle, "Test notification from watch-sim");
        using var doc = JsonDocument.Parse(File.ReadAllText(payload));
        var root = doc.RootElement;
        if (root.TryGetProperty("aps", out var aps) && aps.TryGetProperty("alert", out var alert))
        {
            if (alert.ValueKind == JsonValueKind.String) return (fallbackTitle, alert.GetString());
            string Str(string name) => alert.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            return (Str("title") ?? fallbackTitle, Str("body") ?? "");
        }
        throw new UsageException($"{payload}: no aps.alert in the notification payload");
    }

    // ---- adb ---------------------------------------------------------------------------------------

    static string FindAdb()
    {
        var dirs = new List<string>();
        foreach (var env in new[] { "ANDROID_HOME", "ANDROID_SDK_ROOT" })
            if (Environment.GetEnvironmentVariable(env) is { Length: > 0 } sdk) dirs.Add(Path.Combine(sdk, "platform-tools"));
        dirs.AddRange((Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator));
        dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk", "platform-tools"));
        foreach (var dir in dirs.Where(d => !string.IsNullOrWhiteSpace(d)))
        {
            try { var adb = Path.Combine(dir.Trim('"'), "adb.exe"); if (File.Exists(adb)) return adb; }
            catch (ArgumentException) { }
        }
        return null;
    }

    static (int Code, string Output) Adb(string adb, params string[] args)
    {
        var psi = new ProcessStartInfo(adb) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var a in args) psi.ArgumentList.Add(a);
        try
        {
            using var p = Process.Start(psi);
            var stderr = p.StandardError.ReadToEndAsync();
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return (p.ExitCode, output + stderr.Result);
        }
        catch (Win32Exception e) { throw new InvalidOperationException($"could not run adb: {e.Message}"); }
    }

    static int Shell(string adb, string serial, params string[] command)
    {
        var (code, output) = Adb(adb, ["-s", serial, "shell", .. command]);
        Console.Write(output);
        return code;
    }

    // Devices whose build characteristics include "watch".
    static List<(string Serial, string Api, string Model)> Watches(string adb)
    {
        var result = new List<(string, string, string)>();
        var (_, devices) = Adb(adb, "devices");
        foreach (var line in devices.Split('\n').Skip(1))
        {
            var parts = line.Trim().Split('\t');
            if (parts.Length < 2 || parts[1] != "device") continue;
            var s = parts[0];
            string Prop(string name) => Adb(adb, "-s", s, "shell", "getprop", name).Output.Trim();
            if (!Prop("ro.build.characteristics").Contains("watch")) continue;
            result.Add((s, Prop("ro.build.version.sdk"), Prop("ro.product.model")));
        }
        return result;
    }

    // ---- binary AndroidManifest.xml ----------------------------------------------------------------

    sealed class WsElement
    {
        public string Name;
        public readonly Dictionary<string, string> Attributes = [];
        public readonly List<WsElement> Children = [];
        public string Attr(string name) => Attributes.GetValueOrDefault(name);
        public IEnumerable<WsElement> All(string name) => Children.Where(c => c.Name == name);
    }

    internal sealed class WsManifest
    {
        public string Package, LauncherActivity, Tile, Complication, WatchFace;

        public static WsManifest Read(string apk)
        {
            byte[] data;
            try
            {
                using var zip = ZipFile.OpenRead(apk);
                var entry = zip.GetEntry("AndroidManifest.xml") ?? throw new InvalidOperationException($"{apk}: AndroidManifest.xml not found (not an APK?)");
                using var stream = entry.Open();
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                data = ms.ToArray();
            }
            catch (InvalidDataException) { throw new InvalidOperationException($"{apk}: not an APK (zip) file"); }

            var root = ParseAxml(data) ?? throw new InvalidOperationException($"{apk}: unreadable AndroidManifest.xml");
            var m = new WsManifest { Package = root.Attr("package") };
            if (root.All("application").FirstOrDefault() is not { } app) return m;
            string Full(string name) => name == null ? null : name.StartsWith('.') ? m.Package + name : name.Contains('.') ? name : $"{m.Package}.{name}";
            static bool HasAction(WsElement component, string action) =>
                component.All("intent-filter").Any(f => f.All("action").Any(a => a.Attr("name") == action));
            static bool HasCategory(WsElement component, string category) =>
                component.All("intent-filter").Any(f => f.All("category").Any(a => a.Attr("name") == category));

            foreach (var activity in app.All("activity").Concat(app.All("activity-alias")))
                if (HasAction(activity, "android.intent.action.MAIN") && HasCategory(activity, "android.intent.category.LAUNCHER"))
                { m.LauncherActivity ??= Full(activity.Attr("name")); }
            foreach (var service in app.All("service"))
            {
                var name = Full(service.Attr("name"));
                if (HasAction(service, "androidx.wear.tiles.action.BIND_TILE_PROVIDER")) m.Tile ??= name;
                if (HasAction(service, "android.support.wearable.complications.ACTION_COMPLICATION_UPDATE_REQUEST")) m.Complication ??= name;
                if (HasAction(service, "android.service.wallpaper.WallpaperService") && HasCategory(service, "com.google.android.wearable.watchface.category.WATCH_FACE")) m.WatchFace ??= name;
            }
            return m;
        }

        // Android's compiled XML: chunks (type u16, header size u16, size u32); a string pool, a resource
        // id map (names of android: attributes may be blank and only identified by id), then
        // start/end element chunks.
        static WsElement ParseAxml(byte[] d)
        {
            if (d.Length < 8 || BitConverter.ToUInt16(d, 0) != 0x0003) return null;
            string[] strings = [];
            uint[] resourceIds = [];
            var stack = new Stack<WsElement>();
            WsElement root = null;
            int pos = BitConverter.ToUInt16(d, 2);
            while (pos + 8 <= d.Length)
            {
                int type = BitConverter.ToUInt16(d, pos), headerSize = BitConverter.ToUInt16(d, pos + 2);
                var size = (int)BitConverter.ToUInt32(d, pos + 4);
                if (size < 8 || pos + size > d.Length) break;
                switch (type)
                {
                    case 0x0001: strings = StringPool(d, pos); break;
                    case 0x0180: resourceIds = Enumerable.Range(0, (size - headerSize) / 4).Select(i => BitConverter.ToUInt32(d, pos + headerSize + i * 4)).ToArray(); break;
                    case 0x0102:
                    {
                        var ext = pos + headerSize;
                        var element = new WsElement { Name = Str(strings, BitConverter.ToInt32(d, ext + 4)) };
                        int attrStart = BitConverter.ToUInt16(d, ext + 8), attrSize = BitConverter.ToUInt16(d, ext + 10), attrCount = BitConverter.ToUInt16(d, ext + 12);
                        for (var i = 0; i < attrCount; i++)
                        {
                            var at = ext + attrStart + i * attrSize;
                            var nameIndex = BitConverter.ToInt32(d, at + 4);
                            var name = Str(strings, nameIndex);
                            if (string.IsNullOrEmpty(name) && nameIndex >= 0 && nameIndex < resourceIds.Length)
                                name = resourceIds[nameIndex] switch { 0x01010003 => "name", _ => $"0x{resourceIds[nameIndex]:x8}" };
                            var raw = BitConverter.ToInt32(d, at + 8);
                            var dataType = d[at + 15];
                            var value = BitConverter.ToInt32(d, at + 16);
                            element.Attributes[name] = raw >= 0 ? Str(strings, raw) : dataType == 0x03 ? Str(strings, value) : value.ToString();
                        }
                        if (stack.Count > 0) stack.Peek().Children.Add(element); else root ??= element;
                        stack.Push(element);
                        break;
                    }
                    case 0x0103: if (stack.Count > 0) stack.Pop(); break;
                }
                pos += size;
            }
            return root;
        }

        static string Str(string[] strings, int index) => index >= 0 && index < strings.Length ? strings[index] : null;

        static string[] StringPool(byte[] d, int pos)
        {
            var count = (int)BitConverter.ToUInt32(d, pos + 8);
            var utf8 = (BitConverter.ToUInt32(d, pos + 16) & 0x100) != 0;
            var start = pos + (int)BitConverter.ToUInt32(d, pos + 20);
            var result = new string[count];
            for (var i = 0; i < count; i++)
            {
                var at = start + (int)BitConverter.ToUInt32(d, pos + BitConverter.ToUInt16(d, pos + 2) + i * 4);
                if (utf8)
                {
                    // character length then byte length, each 1 or 2 bytes (high bit = 2 bytes)
                    at += (d[at] & 0x80) != 0 ? 2 : 1;
                    int length = d[at];
                    if ((length & 0x80) != 0) { length = ((length & 0x7F) << 8) | d[at + 1]; at += 2; } else at += 1;
                    result[i] = Encoding.UTF8.GetString(d, at, length);
                }
                else
                {
                    int length = BitConverter.ToUInt16(d, at);
                    if ((length & 0x8000) != 0) { length = ((length & 0x7FFF) << 16) | BitConverter.ToUInt16(d, at + 2); at += 4; } else at += 2;
                    result[i] = Encoding.Unicode.GetString(d, at, length * 2);
                }
            }
            return result;
        }
    }
}
