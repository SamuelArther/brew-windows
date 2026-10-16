using System.Buffers.Binary;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace WinTools;

// fileicon / makeicns: custom folder icons through desktop.ini (Windows only lets folders and
// shortcuts carry their own icon), and .icns/.ico files built from any image. Also holds the image
// helpers the clipboard and optimiser commands share: a PNG encoder and WinRT decoding/scaling.
static class Icons
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("fileicon", "Set, get, test or remove custom icons of folders and shortcuts", FileIcon);
        Commands.Add("makeicns", "Create .icns (or .ico) icon files from images", MakeIcns);
    }

    // ---- makeicns ----

    static int MakeIcns(string[] args)
    {
        string input = null, output = null;
        var sized = new Dictionary<int, string>();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg is "-h" or "--help" or "-help")
            {
                Console.WriteLine("""
                    Usage: makeicns [-16 file] [-32 file] [-128 file] [-256 file] [-512 file] [-in file] [-out file.icns]
                    Creates an icon file from images. -in supplies every size not given explicitly; the
                    image is scaled as needed (any format Windows can decode: PNG, JPEG, BMP, GIF, TIFF, ICO).
                    -out defaults to the -in file with an .icns extension; an .ico extension writes a
                    Windows icon (16-256 px) instead.
                    """);
                return 0;
            }
            if (arg is "--version" or "-version") { Console.WriteLine($"makeicns {Commands.Version} (wintools)"); return 0; }
            if (i + 1 >= args.Length) throw new UsageException($"{arg} needs a file argument");
            var value = args[++i];
            switch (arg)
            {
                case "-in": input = value; break;
                case "-out": output = value; break;
                case "-16" or "-32" or "-48" or "-128" or "-256" or "-512" or "-1024": sized[int.Parse(arg[1..])] = value; break;
                default: throw new UsageException($"unknown option {arg}");
            }
        }
        if (input == null && sized.Count == 0) throw new UsageException("no input images (see makeicns -h)");
        output ??= Path.ChangeExtension(input ?? sized.Values.First(), ".icns");
        var ico = output.EndsWith(".ico", StringComparison.OrdinalIgnoreCase);
        int[] sizes = ico ? [16, 24, 32, 48, 64, 128, 256] : [16, 32, 64, 128, 256, 512, 1024];
        var sources = sizes.ToDictionary(s => s, s => sized.GetValueOrDefault(s) ?? input);
        // Without -in, every missing size comes from the nearest larger (else largest) explicit image.
        foreach (var s in sizes.Where(s => sources[s] == null))
        {
            var best = sized.Keys.Where(k => k >= s).DefaultIfEmpty(sized.Keys.Max()).Min();
            sources[s] = sized[best];
        }
        var cache = new Dictionary<string, byte[]>();
        byte[] Png(int size)
        {
            var file = sources[size];
            if (!cache.TryGetValue(file, out var data)) cache[file] = data = File.ReadAllBytes(file);
            var (w, h, bgra) = Square(data, size);
            return EncodePng(w, h, bgra);
        }
        byte[] result;
        if (ico) result = BuildIco(sizes.Select(s => (s, Png(s))).ToList());
        else
        {
            // icns PNG slots: icp4/icp5/icp6 (16/32/64), ic07-ic10 (128-1024), ic11-ic14 the @2x variants.
            var largest = sized.Keys.Concat(input != null ? [ImageSize(File.ReadAllBytes(input))] : []).Max();
            var entries = new List<(string, byte[])> { ("icp4", Png(16)), ("icp5", Png(32)), ("ic11", Png(32)), ("icp6", Png(64)),
                ("ic12", Png(64)), ("ic07", Png(128)), ("ic08", Png(256)), ("ic13", Png(256)), ("ic09", Png(512)), ("ic14", Png(512)) };
            if (largest >= 1024) entries.Add(("ic10", Png(1024)));
            result = BuildIcns(entries);
        }
        File.WriteAllBytes(output, result);
        return 0;
    }

    static int ImageSize(byte[] data)
    {
        var (w, h, _) = Decode(data);
        return Math.Max(w, h);
    }

    internal static byte[] BuildIco(List<(int Size, byte[] Png)> images)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write((ushort)0); bw.Write((ushort)1); bw.Write((ushort)images.Count);
        var offset = 6 + 16 * images.Count;
        foreach (var (size, png) in images)
        {
            bw.Write((byte)(size >= 256 ? 0 : size)); bw.Write((byte)(size >= 256 ? 0 : size));
            bw.Write((byte)0); bw.Write((byte)0); bw.Write((ushort)1); bw.Write((ushort)32);
            bw.Write(png.Length); bw.Write(offset);
            offset += png.Length;
        }
        foreach (var (_, png) in images) bw.Write(png);
        return ms.ToArray();
    }

    internal static byte[] BuildIcns(List<(string Type, byte[] Data)> entries)
    {
        using var ms = new MemoryStream();
        var length = 8 + entries.Sum(e => 8 + e.Data.Length);
        WriteTag(ms, "icns", length);
        foreach (var (type, data) in entries) { WriteTag(ms, type, 8 + data.Length); ms.Write(data); }
        return ms.ToArray();

        static void WriteTag(Stream s, string tag, int len)
        {
            s.Write(Encoding.ASCII.GetBytes(tag));
            Span<byte> b = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(b, len);
            s.Write(b);
        }
    }

    // ---- fileicon ----

    const string IconFileName = "desktop.ico";

    static int FileIcon(string[] args)
    {
        bool quiet = false, force = false;
        var rest = new List<string>();
        foreach (var arg in args)
        {
            switch (arg)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage: fileicon set  <folder> <imageFile> [-q]
                               fileicon get  <folder> [<iconOutputFile>] [-f] [-q]
                               fileicon rm   <folder> [-q]
                               fileicon test <folder> [-q]
                        Manage a folder's custom icon (desktop.ini). <imageFile> may be any image, an .ico, or an
                        .exe/.dll (its first icon). Shortcuts (.url) work too; Windows has no per-file icons.
                        """);
                    return 0;
                case "--version": Console.WriteLine($"fileicon {Commands.Version} (wintools)"); return 0;
                case "-q" or "--quiet": quiet = true; break;
                case "-f" or "--force": force = true; break;
                default:
                    if (arg.StartsWith('-') && arg.Length > 1) throw new UsageException($"unknown option {arg}");
                    rest.Add(arg);
                    break;
            }
        }
        if (rest.Count < 2) throw new UsageException("expected a command (set, get, rm, test) and a path; see fileicon -h");
        var command = rest[0];
        var target = Path.GetFullPath(rest[1]);
        if (Path.GetPathRoot(target) != target) target = Path.TrimEndingDirectorySeparator(target);
        var isUrl = target.EndsWith(".url", StringComparison.OrdinalIgnoreCase) && File.Exists(target);
        if (!Directory.Exists(target) && !isUrl)
        {
            if (!File.Exists(target)) throw new IOException($"'{rest[1]}' does not exist");
            Console.Error.WriteLine("fileicon: Windows can't give an individual file its own icon; use a folder or a .url shortcut.");
            return 1;
        }
        var kind = isUrl ? "file" : "folder";
        switch (command)
        {
            case "set":
                if (rest.Count != 3) throw new UsageException("set needs <folder> <imageFile>");
                SetIcon(target, Path.GetFullPath(rest[2]), isUrl);
                if (!quiet) Console.WriteLine($"Custom icon assigned to '{target}' based on '{rest[2]}'.");
                return 0;
            case "rm" or "remove":
                if (!RemoveIcon(target, isUrl)) { if (!quiet) Console.WriteLine($"(Nothing to do: no custom icon on {kind} '{target}'.)"); return 0; }
                if (!quiet) Console.WriteLine($"Custom icon removed from '{target}'.");
                return 0;
            case "test":
                var has = CurrentIcon(target, isUrl) != null;
                if (!quiet) Console.WriteLine(has ? $"HAS custom icon: {kind} '{target}'" : $"Has NO custom icon: {kind} '{target}'");
                return has ? 0 : 1;
            case "get":
                var icon = CurrentIcon(target, isUrl);
                if (icon == null) { Console.Error.WriteLine($"fileicon: {kind} '{target}' has no custom icon."); return 1; }
                var output = rest.Count > 2 ? rest[2] : Path.GetFileName(target.TrimEnd('\\')) is { Length: > 0 } n ? n + ".ico" : "icon.ico";
                if (output.EndsWith('/') || output.EndsWith('\\') || Directory.Exists(output))
                    output = Path.Combine(output, Path.GetFileName(target.TrimEnd('\\')) + ".ico");
                if (File.Exists(output) && !force) throw new IOException($"'{output}' already exists; use -f to overwrite");
                File.WriteAllBytes(output, IconBytes(icon.Value.File, icon.Value.Index));
                if (!quiet) Console.WriteLine($"Custom icon extracted to '{output}'.");
                return 0;
            default:
                throw new UsageException($"unknown command '{command}' (expected set, get, rm or test)");
        }
    }

    // Writes the icon next to the folder's desktop.ini (hidden+system, like macOS's Icon\r) so the folder
    // keeps it when copied, then marks the folder read-only, which is what makes Explorer read desktop.ini.
    static void SetIcon(string target, string image, bool isUrl)
    {
        if (!File.Exists(image)) throw new IOException($"image file '{image}' does not exist");
        var ico = ToIco(image);
        if (isUrl)
        {
            var icoPath = Path.ChangeExtension(target, ".ico");
            File.WriteAllBytes(icoPath, ico);
            Ini.Set(target, "InternetShortcut", "IconFile", icoPath);
            Ini.Set(target, "InternetShortcut", "IconIndex", "0");
        }
        else
        {
            var icoPath = Path.Combine(target, IconFileName);
            if (File.Exists(icoPath)) File.SetAttributes(icoPath, FileAttributes.Normal);
            File.WriteAllBytes(icoPath, ico);
            File.SetAttributes(icoPath, FileAttributes.Hidden | FileAttributes.System);
            var ini = Path.Combine(target, "desktop.ini");
            Ini.Set(ini, ".ShellClassInfo", "IconResource", IconFileName + ",0");
            Ini.Remove(ini, ".ShellClassInfo", "IconFile");
            Ini.Remove(ini, ".ShellClassInfo", "IconIndex");
            File.SetAttributes(ini, FileAttributes.Hidden | FileAttributes.System);
            var dir = new DirectoryInfo(target);
            dir.Attributes |= FileAttributes.ReadOnly;
        }
        SHChangeNotify(SHCNE_UPDATEITEM, SHCNF_PATHW, target, IntPtr.Zero);
    }

    static bool RemoveIcon(string target, bool isUrl)
    {
        var current = CurrentIcon(target, isUrl);
        if (current == null) return false;
        if (isUrl)
        {
            Ini.Remove(target, "InternetShortcut", "IconFile");
            Ini.Remove(target, "InternetShortcut", "IconIndex");
            if (current.Value.File.Equals(Path.ChangeExtension(target, ".ico"), StringComparison.OrdinalIgnoreCase)) File.Delete(current.Value.File);
        }
        else
        {
            var ini = Path.Combine(target, "desktop.ini");
            foreach (var key in new[] { "IconResource", "IconFile", "IconIndex" }) Ini.Remove(ini, ".ShellClassInfo", key);
            var ico = Path.Combine(target, IconFileName);
            if (current.Value.File.Equals(ico, StringComparison.OrdinalIgnoreCase) && File.Exists(ico))
            {
                File.SetAttributes(ico, FileAttributes.Normal);
                File.Delete(ico);
            }
            if (Ini.IsEmpty(ini))
            {
                File.SetAttributes(ini, FileAttributes.Normal);
                File.Delete(ini);
                var dir = new DirectoryInfo(target);
                dir.Attributes &= ~FileAttributes.ReadOnly;
            }
        }
        SHChangeNotify(SHCNE_UPDATEITEM, SHCNF_PATHW, target, IntPtr.Zero);
        return true;
    }

    static (string File, int Index)? CurrentIcon(string target, bool isUrl)
    {
        string file; int index = 0;
        if (isUrl)
        {
            file = Ini.Get(target, "InternetShortcut", "IconFile");
            int.TryParse(Ini.Get(target, "InternetShortcut", "IconIndex"), out index);
        }
        else
        {
            var ini = Path.Combine(target, "desktop.ini");
            if (!File.Exists(ini)) return null;
            var resource = Ini.Get(ini, ".ShellClassInfo", "IconResource");
            if (resource != null)
            {
                var comma = resource.LastIndexOf(',');
                file = comma > 0 ? resource[..comma] : resource;
                if (comma > 0) int.TryParse(resource[(comma + 1)..], out index);
            }
            else
            {
                file = Ini.Get(ini, ".ShellClassInfo", "IconFile");
                int.TryParse(Ini.Get(ini, ".ShellClassInfo", "IconIndex"), out index);
            }
        }
        if (string.IsNullOrWhiteSpace(file)) return null;
        file = Environment.ExpandEnvironmentVariables(file.Trim().Trim('"'));
        if (!Path.IsPathRooted(file)) file = Path.Combine(isUrl ? Path.GetDirectoryName(target) : target, file);
        return (Path.GetFullPath(file), index);
    }

    // An image file as .ico bytes: .ico as is, .exe/.dll via their icon resources, anything else rendered.
    static byte[] ToIco(string image)
    {
        var ext = Path.GetExtension(image).ToLowerInvariant();
        if (ext is ".ico" or ".exe" or ".dll" or ".icl" or ".cpl" or ".mun") return IconBytes(image, 0);
        var data = File.ReadAllBytes(image);
        return BuildIco(new[] { 16, 24, 32, 48, 64, 128, 256 }.Select(s =>
        {
            var (w, h, bgra) = Square(data, s);
            return (s, EncodePng(w, h, bgra));
        }).ToList());
    }

    static byte[] IconBytes(string file, int index)
    {
        if (!File.Exists(file)) throw new IOException($"icon file '{file}' does not exist");
        var ext = Path.GetExtension(file).ToLowerInvariant();
        if (ext == ".ico") return File.ReadAllBytes(file);
        if (ext is ".exe" or ".dll" or ".icl" or ".cpl" or ".mun" or ".ocx" or ".scr") return ExtractIcon(file, index);
        return ToIco(file);
    }

    // Rebuilds an .ico from a module's RT_GROUP_ICON resource: index >= 0 is the n-th group, < 0 a resource ID.
    static unsafe byte[] ExtractIcon(string module, int index)
    {
        var lib = LoadLibraryExW(module, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE | LOAD_LIBRARY_AS_IMAGE_RESOURCE);
        if (lib == IntPtr.Zero) throw new IOException($"cannot open '{module}' (error {Marshal.GetLastWin32Error()})");
        try
        {
            IntPtr group;
            if (index < 0) group = FindResourceW(lib, (IntPtr)(-index), RT_GROUP_ICON);
            else
            {
                var names = new List<IntPtr>();
                var handle = GCHandle.Alloc(names);
                try { EnumResourceNamesW(lib, RT_GROUP_ICON, &CollectName, GCHandle.ToIntPtr(handle)); }
                finally { handle.Free(); }
                if (index >= names.Count) throw new IOException($"'{module}' has no icon #{index}");
                group = FindResourceW(lib, names[index], RT_GROUP_ICON);
            }
            var dir = Resource(lib, group);
            if (dir == null || dir.Length < 6) throw new IOException($"'{module}' has no icon #{index}");
            var count = BitConverter.ToUInt16(dir, 4);
            var images = new List<byte[]>();
            for (var i = 0; i < count; i++)
                images.Add(Resource(lib, FindResourceW(lib, (IntPtr)BitConverter.ToUInt16(dir, 6 + i * 14 + 12), RT_ICON)) ?? []);
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(dir, 0, 6);
            var offset = 6 + 16 * count;
            for (var i = 0; i < count; i++)
            {
                bw.Write(dir, 6 + i * 14, 8);       // width, height, colours, reserved, planes, bit count
                bw.Write(images[i].Length);
                bw.Write(offset);
                offset += images[i].Length;
            }
            foreach (var img in images) bw.Write(img);
            return ms.ToArray();
        }
        finally { FreeLibrary(lib); }
    }

    [UnmanagedCallersOnly]
    static int CollectName(IntPtr module, IntPtr type, IntPtr name, IntPtr param)
    {
        var names = (List<IntPtr>)GCHandle.FromIntPtr(param).Target;
        // Integer IDs are kept as is; string names only live during the callback, so they are copied.
        names.Add(name.ToInt64() >> 16 == 0 ? name : Marshal.StringToHGlobalUni(Marshal.PtrToStringUni(name)));
        return 1;
    }

    static byte[] Resource(IntPtr lib, IntPtr res)
    {
        if (res == IntPtr.Zero) return null;
        var size = SizeofResource(lib, res);
        var data = LockResource(LoadResource(lib, res));
        if (data == IntPtr.Zero) return null;
        var bytes = new byte[size];
        Marshal.Copy(data, bytes, 0, (int)size);
        return bytes;
    }

    // Minimal INI editing that keeps everything else in the file (desktop.ini is usually UTF-16).
    static class Ini
    {
        public static string Get(string file, string section, string key)
        {
            if (!File.Exists(file)) return null;
            var inSection = false;
            foreach (var raw in File.ReadAllLines(file))
            {
                var line = raw.Trim();
                if (line.StartsWith('[')) { inSection = line.Equals($"[{section}]", StringComparison.OrdinalIgnoreCase); continue; }
                var eq = line.IndexOf('=');
                if (inSection && eq > 0 && line[..eq].Trim().Equals(key, StringComparison.OrdinalIgnoreCase)) return line[(eq + 1)..].Trim();
            }
            return null;
        }

        public static void Set(string file, string section, string key, string value) => Edit(file, section, key, value);
        public static void Remove(string file, string section, string key) { if (File.Exists(file)) Edit(file, section, key, null); }

        public static bool IsEmpty(string file) =>
            !File.Exists(file) || File.ReadAllLines(file).All(l => l.Trim().Length == 0 || l.Trim().StartsWith('[') || l.Trim().StartsWith(';'));

        static void Edit(string file, string section, string key, string value)
        {
            var lines = File.Exists(file) ? File.ReadAllLines(file).ToList() : [];
            var start = lines.FindIndex(l => l.Trim().Equals($"[{section}]", StringComparison.OrdinalIgnoreCase));
            if (start < 0)
            {
                if (value == null) return;
                if (lines.Count > 0 && lines[^1].Trim().Length > 0) lines.Add("");
                lines.Add($"[{section}]");
                lines.Add($"{key}={value}");
            }
            else
            {
                var end = lines.FindIndex(start + 1, l => l.Trim().StartsWith('['));
                if (end < 0) end = lines.Count;
                var at = lines.FindIndex(start + 1, end - start - 1, l => l.Contains('=') && l[..l.IndexOf('=')].Trim().Equals(key, StringComparison.OrdinalIgnoreCase));
                if (value == null) { if (at >= 0) lines.RemoveAt(at); else return; }
                else if (at >= 0) lines[at] = $"{key}={value}";
                else lines.Insert(end > start + 1 && lines[end - 1].Trim().Length == 0 ? end - 1 : end, $"{key}={value}");
                // Drop a section left empty.
                end = lines.FindIndex(start + 1, l => l.Trim().StartsWith('['));
                if (end < 0) end = lines.Count;
                if (lines.Skip(start + 1).Take(end - start - 1).All(l => l.Trim().Length == 0)) lines.RemoveRange(start, end - start);
            }
            var attributes = File.Exists(file) ? File.GetAttributes(file) : FileAttributes.Normal;
            if (File.Exists(file)) File.SetAttributes(file, FileAttributes.Normal);
            File.WriteAllLines(file, lines, Encoding.Unicode);
            File.SetAttributes(file, attributes);
        }
    }

    // ---- shared image helpers ----

    // Decodes any image Windows Imaging understands to straight-alpha BGRA, optionally scaled to fit `fit`.
    internal static (int W, int H, byte[] Bgra) Decode(byte[] data, int fit = 0) => DecodeAsync(data, fit).GetAwaiter().GetResult();

    static async Task<(int, int, byte[])> DecodeAsync(byte[] data, int fit)
    {
        using var stream = new InMemoryRandomAccessStream();
        using (var writer = new DataWriter(stream))
        {
            writer.WriteBytes(data);
            await writer.StoreAsync();
            writer.DetachStream();
        }
        stream.Seek(0);
        BitmapDecoder decoder;
        try { decoder = await BitmapDecoder.CreateAsync(stream); }
        catch (Exception e) when (e is not OutOfMemoryException) { throw new InvalidOperationException("not an image Windows can decode"); }
        uint w = decoder.PixelWidth, h = decoder.PixelHeight;
        var transform = new BitmapTransform();
        if (fit > 0)
        {
            var scale = (double)fit / Math.Max(w, h);
            w = Math.Max(1, (uint)Math.Round(w * scale));
            h = Math.Max(1, (uint)Math.Round(h * scale));
            transform.ScaledWidth = w;
            transform.ScaledHeight = h;
            transform.InterpolationMode = BitmapInterpolationMode.Fant;
        }
        var pixels = await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, transform,
            ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);
        return ((int)w, (int)h, pixels.DetachPixelData());
    }

    // Scales an image to fit a size x size square, centred on transparency.
    internal static (int W, int H, byte[] Bgra) Square(byte[] data, int size)
    {
        var (w, h, bgra) = Decode(data, size);
        if (w == size && h == size) return (w, h, bgra);
        var canvas = new byte[size * size * 4];
        int ox = (size - w) / 2, oy = (size - h) / 2;
        for (var y = 0; y < h; y++) System.Buffer.BlockCopy(bgra, y * w * 4, canvas, ((y + oy) * size + ox) * 4, w * 4);
        return (size, size, canvas);
    }

    // Encodes BGRA through Windows Imaging: "gif", "jpeg", "tiff", "bmp" or "png".
    internal static byte[] Encode(string format, int w, int h, byte[] bgra) => EncodeAsync(format, w, h, bgra).GetAwaiter().GetResult();

    static async Task<byte[]> EncodeAsync(string format, int w, int h, byte[] bgra)
    {
        var id = format switch
        {
            "gif" => BitmapEncoder.GifEncoderId,
            "jpeg" => BitmapEncoder.JpegEncoderId,
            "tiff" => BitmapEncoder.TiffEncoderId,
            "bmp" => BitmapEncoder.BmpEncoderId,
            _ => BitmapEncoder.PngEncoderId,
        };
        var alpha = BitmapAlphaMode.Straight;
        if (format == "jpeg")
        {
            // JPEG has no alpha: flatten onto white.
            bgra = (byte[])bgra.Clone();
            for (var i = 0; i < bgra.Length; i += 4)
            {
                var a = bgra[i + 3];
                for (var c = 0; c < 3; c++) bgra[i + c] = (byte)((bgra[i + c] * a + 255 * (255 - a)) / 255);
                bgra[i + 3] = 255;
            }
            alpha = BitmapAlphaMode.Ignore;
        }
        using var stream = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(id, stream);
        encoder.SetPixelData(BitmapPixelFormat.Bgra8, alpha, (uint)w, (uint)h, 96, 96, bgra);
        await encoder.FlushAsync();
        var bytes = new byte[stream.Size];
        using var reader = new DataReader(stream.GetInputStreamAt(0));
        await reader.LoadAsync((uint)stream.Size);
        reader.ReadBytes(bytes);
        return bytes;
    }

    // PNG writer for straight-alpha BGRA: RGB when fully opaque, else RGBA; adaptive filtering.
    internal static byte[] EncodePng(int w, int h, byte[] bgra)
    {
        var alpha = false;
        for (var i = 3; i < bgra.Length && !alpha; i += 4) alpha = bgra[i] != 255;
        var channels = alpha ? 4 : 3;
        var raw = new byte[w * h * channels];
        for (int p = 0, o = 0; p < w * h; p++)
        {
            raw[o++] = bgra[p * 4 + 2]; raw[o++] = bgra[p * 4 + 1]; raw[o++] = bgra[p * 4];
            if (alpha) raw[o++] = bgra[p * 4 + 3];
        }
        var ihdr = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr, w);
        BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(4), h);
        ihdr[8] = 8; ihdr[9] = (byte)(alpha ? 6 : 2);
        return PngFile([("IHDR", ihdr), ("IDAT", Deflate(Filter(raw, w * channels, h, channels, -1), CompressionLevel.SmallestSize))]);
    }

    internal static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

    internal static byte[] PngFile(IEnumerable<(string Type, byte[] Data)> chunks)
    {
        using var ms = new MemoryStream();
        ms.Write(PngSignature);
        foreach (var (type, data) in chunks) WriteChunk(ms, type, data);
        WriteChunk(ms, "IEND", []);
        return ms.ToArray();
    }

    static void WriteChunk(Stream s, string type, byte[] data)
    {
        Span<byte> b = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(b, data.Length);
        s.Write(b);
        var typeBytes = Encoding.ASCII.GetBytes(type);
        s.Write(typeBytes);
        s.Write(data);
        BinaryPrimitives.WriteUInt32BigEndian(b, Crc32(data, Crc32(typeBytes, 0xFFFFFFFF)) ^ 0xFFFFFFFF);
        s.Write(b);
    }

    // Splits a PNG into chunks (null when it isn't a PNG or is truncated).
    internal static List<(string Type, byte[] Data)> PngChunks(byte[] png)
    {
        if (png.Length < 8 || !png.AsSpan(0, 8).SequenceEqual(PngSignature)) return null;
        var chunks = new List<(string, byte[])>();
        var pos = 8;
        while (pos + 12 <= png.Length)
        {
            var len = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(pos));
            if (len < 0 || pos + 12 + len > png.Length) return null;
            var type = Encoding.ASCII.GetString(png, pos + 4, 4);
            if (type == "IEND") return chunks;
            chunks.Add((type, png[(pos + 8)..(pos + 8 + len)]));
            pos += 12 + len;
        }
        return null;
    }

    static uint[] crcTable;

    static uint Crc32(ReadOnlySpan<byte> data, uint crc)
    {
        if (crcTable == null)
        {
            var table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                var c = n;
                for (var k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
                table[n] = c;
            }
            crcTable = table;
        }
        foreach (var b in data) crc = crcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc;
    }

    internal static byte[] Deflate(byte[] data, CompressionLevel level)
    {
        using var ms = new MemoryStream();
        using (var z = new ZLibStream(ms, level)) z.Write(data);
        return ms.ToArray();
    }

    internal static byte[] Inflate(byte[] data)
    {
        using var ms = new MemoryStream();
        using (var z = new ZLibStream(new MemoryStream(data), CompressionMode.Decompress)) z.CopyTo(ms);
        return ms.ToArray();
    }

    // Applies PNG filters to raw scanlines: `type` 0-4 for one filter everywhere, -1 to pick per row the
    // filter with the smallest sum of absolute values (the libpng heuristic).
    internal static byte[] Filter(byte[] raw, int stride, int height, int bpp, int type)
    {
        var output = new byte[(stride + 1) * height];
        var candidate = new byte[stride];
        var best = new byte[stride];
        for (var y = 0; y < height; y++)
        {
            var row = raw.AsSpan(y * stride, stride);
            var prior = y > 0 ? raw.AsSpan((y - 1) * stride, stride) : new byte[stride];
            int bestType = 0; long bestScore = long.MaxValue;
            for (var t = type < 0 ? 0 : type; t <= (type < 0 ? 4 : type); t++)
            {
                long score = 0;
                for (var x = 0; x < stride; x++)
                {
                    int a = x >= bpp ? row[x - bpp] : 0, b = prior[x], c = x >= bpp ? prior[x - bpp] : 0;
                    var v = (byte)(t switch { 0 => row[x], 1 => row[x] - a, 2 => row[x] - b, 3 => row[x] - (a + b) / 2, _ => row[x] - Paeth(a, b, c) });
                    candidate[x] = v;
                    score += v < 128 ? v : 256 - v;
                }
                if (score < bestScore) { bestScore = score; bestType = t; candidate.CopyTo(best, 0); }
            }
            output[y * (stride + 1)] = (byte)bestType;
            best.CopyTo(output, y * (stride + 1) + 1);
        }
        return output;
    }

    // Undoes PNG filtering; returns null on a malformed stream.
    internal static byte[] Unfilter(byte[] filtered, int stride, int height, int bpp)
    {
        if (filtered.Length < (stride + 1) * height) return null;
        var raw = new byte[stride * height];
        for (var y = 0; y < height; y++)
        {
            var type = filtered[y * (stride + 1)];
            if (type > 4) return null;
            for (var x = 0; x < stride; x++)
            {
                int a = x >= bpp ? raw[y * stride + x - bpp] : 0, b = y > 0 ? raw[(y - 1) * stride + x] : 0;
                int c = x >= bpp && y > 0 ? raw[(y - 1) * stride + x - bpp] : 0;
                var v = filtered[y * (stride + 1) + 1 + x];
                raw[y * stride + x] = (byte)(type switch { 0 => v, 1 => v + a, 2 => v + b, 3 => v + (a + b) / 2, _ => v + Paeth(a, b, c) });
            }
        }
        return raw;
    }

    static int Paeth(int a, int b, int c)
    {
        int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }

    const uint LOAD_LIBRARY_AS_DATAFILE = 0x2, LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x20;
    static readonly IntPtr RT_ICON = 3, RT_GROUP_ICON = 14;
    const int SHCNE_UPDATEITEM = 0x2000;
    const uint SHCNF_PATHW = 0x5;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr LoadLibraryExW(string file, IntPtr reserved, uint flags);

    [DllImport("kernel32.dll")]
    static extern bool FreeLibrary(IntPtr module);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr FindResourceW(IntPtr module, IntPtr name, IntPtr type);

    [DllImport("kernel32.dll")]
    static extern unsafe bool EnumResourceNamesW(IntPtr module, IntPtr type, delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr, int> callback, IntPtr param);

    [DllImport("kernel32.dll")]
    static extern IntPtr LoadResource(IntPtr module, IntPtr res);

    [DllImport("kernel32.dll")]
    static extern IntPtr LockResource(IntPtr data);

    [DllImport("kernel32.dll")]
    static extern uint SizeofResource(IntPtr module, IntPtr res);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern void SHChangeNotify(int eventId, uint flags, string item1, IntPtr item2);
}
