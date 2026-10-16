using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace WinTools;

// setWeblocThumb: give web link files an icon showing a thumbnail of the page they point to.
// macOS stores that icon in the .webloc's resource fork; on Windows the web link file Explorer shows
// with a custom icon is the Internet Shortcut (.url), whose IconFile entry names an .ico. So .url
// files get their IconFile set, and each .webloc gets a .url twin next to it (same URL, with the
// icon) that Explorer can show and open. The thumbnail is a screenshot of the page taken with
// headless Microsoft Edge (built into Windows), or with -t (or when Edge can't load the page) a tile
// with the site's initial drawn with GDI. Icons are kept in %LOCALAPPDATA%\setWeblocThumb\icons.
static class SetWeblocThumb
{
    const string Version = "1.0.0";
    const int IconSize = 256;

    [ModuleInitializer]
    internal static void Register() => Commands.Add("setWeblocThumb", "Give .webloc and .url web links an icon showing the page", Run);

    static int Run(string[] args)
    {
        bool force = false, watch = false, tile = false, debug = false;
        var files = new List<string>();
        foreach (var a in args)
        {
            switch (a)
            {
                case "-h" or "--help":
                    Console.WriteLine($"""
                        setWeblocThumb {Version}
                        Usage: setWeblocThumb [-f] [-t] [-w] [-d] <file or folder>...
                        Give .webloc and .url files an icon with a thumbnail of the web page they link to.
                        A .webloc gets a .url twin next to it (Windows shows icons on .url shortcuts).

                          -f   also re-do files that already have a thumbnail icon
                          -t   draw a tile with the site's initial instead of a page screenshot
                          -w   keep watching the given folders and handle new link files
                          -d   print what is done
                        """);
                    return 0;
                case "--version" or "-v": Console.WriteLine($"setWeblocThumb {Version}"); return 0;
                case "-f" or "+f": force = true; break;
                case "-w": watch = true; break;
                case "-t": tile = true; break;
                case "-d": debug = true; break;
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unknown option {a}");
                    files.Add(a);
                    break;
            }
        }
        if (files.Count == 0) throw new UsageException("no files given (see setWeblocThumb --help)");
        var status = 0;
        foreach (var f in files)
        {
            if (Directory.Exists(f))
                foreach (var link in Directory.EnumerateFiles(f).Where(IsLink)) status |= Handle(link, force, tile, debug);
            else if (File.Exists(f)) status |= Handle(f, force, tile, debug);
            else { Console.Error.WriteLine($"setWeblocThumb: {f}: No such file or directory"); status = 1; }
        }
        if (watch) Watch(files.Where(Directory.Exists).ToList(), tile, debug);
        return status;
    }

    static bool IsLink(string path) => Path.GetExtension(path).ToLowerInvariant() is ".webloc" or ".url";

    static int Handle(string path, bool force, bool tile, bool debug)
    {
        try
        {
            var webloc = Path.GetExtension(path).Equals(".webloc", StringComparison.OrdinalIgnoreCase);
            var url = webloc ? WeblocUrl(path) : ShortcutUrl(path);
            if (url == null) { Console.Error.WriteLine($"setWeblocThumb: {path}: no URL in file"); return 1; }
            var shortcut = webloc ? Path.ChangeExtension(path, ".url") : path;
            if (!force && File.Exists(shortcut) && ShortcutIcon(shortcut) is { } existing && File.Exists(existing)
                && existing.StartsWith(IconDir, StringComparison.OrdinalIgnoreCase))
            {
                if (debug) Console.WriteLine($"{path}: already has a thumbnail icon (use -f to redo)");
                return 0;
            }
            var png = tile ? null : Screenshot(url, debug);
            png ??= Tile(url);
            Directory.CreateDirectory(IconDir);
            var icon = Path.Combine(IconDir, Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(url)))[..16] + ".ico");
            File.WriteAllBytes(icon, Ico(png));
            WriteShortcut(shortcut, url, icon);
            if (debug || webloc) Console.WriteLine(webloc ? $"{path}: thumbnail icon set on {Path.GetFileName(shortcut)}" : $"{path}: thumbnail icon set");
            return 0;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Console.Error.WriteLine($"setWeblocThumb: {path}: {e.Message}");
            return 1;
        }
    }

    static string IconDir => Path.Combine(
        Environment.GetEnvironmentVariable("LOCALAPPDATA") is { Length: > 0 } local ? local : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "setWeblocThumb", "icons");

    static string WeblocUrl(string path) => XcPlist.Load(path) is OrderedDictionary<string, object> d ? XcPlist.Str(d, "URL") : null;

    // [InternetShortcut] URL=..., IconFile=..., IconIndex=...
    static string ShortcutUrl(string path) => IniValue(path, "URL");
    static string ShortcutIcon(string path) => IniValue(path, "IconFile");

    static string IniValue(string path, string key)
    {
        var section = "";
        foreach (var line in File.ReadAllLines(path))
        {
            var t = line.Trim();
            if (t.StartsWith('[')) { section = t; continue; }
            if (section.Equals("[InternetShortcut]", StringComparison.OrdinalIgnoreCase) && t.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                return t[(key.Length + 1)..];
        }
        return null;
    }

    // Writes or updates the shortcut, keeping any other keys it has.
    static void WriteShortcut(string path, string url, string icon)
    {
        var lines = File.Exists(path) ? File.ReadAllLines(path).ToList() : ["[InternetShortcut]"];
        var start = lines.FindIndex(l => l.Trim().Equals("[InternetShortcut]", StringComparison.OrdinalIgnoreCase));
        if (start < 0) { lines.Insert(0, "[InternetShortcut]"); start = 0; }
        var end = lines.FindIndex(start + 1, l => l.TrimStart().StartsWith('['));
        if (end < 0) end = lines.Count;
        var section = lines.GetRange(start + 1, end - start - 1)
            .Where(l => !l.StartsWith("URL=", StringComparison.OrdinalIgnoreCase) && !l.StartsWith("IconFile=", StringComparison.OrdinalIgnoreCase)
                        && !l.StartsWith("IconIndex=", StringComparison.OrdinalIgnoreCase) && l.Trim().Length > 0).ToList();
        section.InsertRange(0, [$"URL={url}", $"IconFile={icon}", "IconIndex=0"]);
        lines.RemoveRange(start + 1, end - start - 1);
        lines.InsertRange(start + 1, section);
        File.WriteAllText(path, string.Join("\r\n", lines) + "\r\n");
    }

    // ---- thumbnails ----

    // A 256x256 PNG of the page's top 1024x1024 CSS pixels, from headless Edge; null if unavailable.
    static byte[] Screenshot(string url, bool debug)
    {
        var edge = new[] { Environment.SpecialFolder.ProgramFilesX86, Environment.SpecialFolder.ProgramFiles }
            .Select(f => Path.Combine(Environment.GetFolderPath(f), "Microsoft", "Edge", "Application", "msedge.exe"))
            .FirstOrDefault(File.Exists);
        if (edge == null) { if (debug) Console.WriteLine("Microsoft Edge not found; drawing a tile"); return null; }
        var temp = Path.Combine(Path.GetTempPath(), "setWeblocThumb-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(temp);
        var png = Path.Combine(temp, "shot.png");
        try
        {
            var psi = new ProcessStartInfo(edge) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
            foreach (var a in new[] { "--headless=new", "--disable-gpu", "--hide-scrollbars", "--no-first-run", "--no-default-browser-check",
                                      "--disable-extensions", "--mute-audio", $"--user-data-dir={Path.Combine(temp, "profile")}",
                                      "--window-size=1024,1024", "--force-device-scale-factor=0.25", "--virtual-time-budget=5000",
                                      $"--screenshot={png}", url })
                psi.ArgumentList.Add(a);
            using var p = Process.Start(psi);
            if (p == null) return null;
            p.StandardOutput.ReadToEndAsync();
            p.StandardError.ReadToEndAsync();
            if (!p.WaitForExit(45000)) { try { p.Kill(true); } catch (InvalidOperationException) { } return null; }
            if (!File.Exists(png) || new FileInfo(png).Length < 100) return null;
            return File.ReadAllBytes(png);
        }
        catch (System.ComponentModel.Win32Exception) { return null; }
        finally
        {
            try { Directory.Delete(temp, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }

    // A tile in a color picked from the host name, with the host's first letter, drawn with GDI.
    internal static byte[] Tile(string url)
    {
        var host = Uri.TryCreate(url, UriKind.Absolute, out var u) && !string.IsNullOrEmpty(u.Host) ? u.Host : url;
        if (host.StartsWith("www.")) host = host[4..];
        var letter = host.FirstOrDefault(char.IsLetterOrDigit);
        var text = letter == default ? "?" : char.ToUpperInvariant(letter).ToString();
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(host));
        var hue = hash[0] * 360 / 256.0;
        var (r, g, b) = Hsl(hue, 0.55, 0.45);
        var pixels = new byte[IconSize * IconSize * 4];
        var bmi = new BITMAPINFOHEADER { biSize = 40, biWidth = IconSize, biHeight = -IconSize, biPlanes = 1, biBitCount = 32 };
        var dc = CreateCompatibleDC(IntPtr.Zero);
        var dib = CreateDIBSection(dc, ref bmi, 0, out var bits, IntPtr.Zero, 0);
        if (dib == IntPtr.Zero) throw new InvalidOperationException("could not create a bitmap");
        try
        {
            var old = SelectObject(dc, dib);
            var brush = CreateSolidBrush((uint)(r | g << 8 | b << 16));
            var rect = new RECT { right = IconSize, bottom = IconSize };
            FillRect(dc, ref rect, brush);
            DeleteObject(brush);
            var font = CreateFontW(-160, 0, 0, 0, 700, 0, 0, 0, 1, 0, 0, 5, 0, "Segoe UI");
            var oldFont = SelectObject(dc, font);
            SetBkMode(dc, 1);
            SetTextColor(dc, 0xffffff);
            DrawTextW(dc, text, -1, ref rect, 0x1 | 0x4 | 0x20);   // DT_CENTER | DT_VCENTER | DT_SINGLELINE
            SelectObject(dc, oldFont);
            DeleteObject(font);
            GdiFlush();
            Marshal.Copy(bits, pixels, 0, pixels.Length);
            SelectObject(dc, old);
        }
        finally
        {
            DeleteObject(dib);
            DeleteDC(dc);
        }
        // GDI leaves alpha at 0; BGRA -> opaque RGBA rows for the PNG.
        var rgba = new byte[pixels.Length];
        for (var i = 0; i < pixels.Length; i += 4)
        {
            rgba[i] = pixels[i + 2];
            rgba[i + 1] = pixels[i + 1];
            rgba[i + 2] = pixels[i];
            rgba[i + 3] = 255;
        }
        return Png(rgba, IconSize, IconSize);
    }

    static (int R, int G, int B) Hsl(double h, double s, double l)
    {
        var c = (1 - Math.Abs(2 * l - 1)) * s;
        var x = c * (1 - Math.Abs(h / 60 % 2 - 1));
        var m = l - c / 2;
        var (r, g, b) = h switch { < 60 => (c, x, 0.0), < 120 => (x, c, 0.0), < 180 => (0.0, c, x), < 240 => (0.0, x, c), < 300 => (x, 0.0, c), _ => (c, 0.0, x) };
        return ((int)((r + m) * 255), (int)((g + m) * 255), (int)((b + m) * 255));
    }

    // ---- PNG and ICO ----

    static byte[] Png(byte[] rgba, int width, int height)
    {
        var raw = new MemoryStream();
        for (var y = 0; y < height; y++)
        {
            raw.WriteByte(0);   // filter: none
            raw.Write(rgba, y * width * 4, width * 4);
        }
        var compressed = new MemoryStream();
        using (var z = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true)) raw.WriteTo(z);
        var png = new MemoryStream();
        png.Write([0x89, (byte)'P', (byte)'N', (byte)'G', 13, 10, 26, 10]);
        var ihdr = new byte[13];
        Be(ihdr, 0, width);
        Be(ihdr, 4, height);
        ihdr[8] = 8;   // bit depth
        ihdr[9] = 6;   // RGBA
        Chunk(png, "IHDR", ihdr);
        Chunk(png, "IDAT", compressed.ToArray());
        Chunk(png, "IEND", []);
        return png.ToArray();
    }

    static void Chunk(Stream s, string type, byte[] data)
    {
        var len = new byte[4];
        Be(len, 0, data.Length);
        s.Write(len);
        var body = Encoding.ASCII.GetBytes(type).Concat(data).ToArray();
        s.Write(body);
        var crc = new byte[4];
        Be(crc, 0, (int)Crc32(body));
        s.Write(crc);
    }

    static void Be(byte[] b, int p, int v) { b[p] = (byte)(v >> 24); b[p + 1] = (byte)(v >> 16); b[p + 2] = (byte)(v >> 8); b[p + 3] = (byte)v; }

    static uint Crc32(byte[] data)
    {
        var crc = 0xffffffffu;
        foreach (var b in data)
        {
            crc ^= b;
            for (var k = 0; k < 8; k++) crc = (crc & 1) != 0 ? 0xedb88320u ^ (crc >> 1) : crc >> 1;
        }
        return ~crc;
    }

    // An .ico holding one PNG image (Windows Vista and later read PNG icon entries).
    static byte[] Ico(byte[] png)
    {
        var width = png.Length > 24 ? png[16] << 24 | png[17] << 16 | png[18] << 8 | png[19] : IconSize;
        var height = png.Length > 24 ? png[20] << 24 | png[21] << 16 | png[22] << 8 | png[23] : IconSize;
        var ico = new byte[22 + png.Length];
        ico[2] = 1;                                   // type: icon
        ico[4] = 1;                                   // one image
        ico[6] = (byte)(width >= 256 ? 0 : width);
        ico[7] = (byte)(height >= 256 ? 0 : height);
        ico[10] = 1;                                  // planes
        ico[12] = 32;                                 // bits per pixel
        BitConverter.TryWriteBytes(ico.AsSpan(14), png.Length);
        BitConverter.TryWriteBytes(ico.AsSpan(18), 22);
        png.CopyTo(ico, 22);
        return ico;
    }

    // ---- watching ----

    static void Watch(List<string> folders, bool tile, bool debug)
    {
        if (folders.Count == 0) throw new UsageException("-w needs at least one folder");
        var watchers = folders.Select(f =>
        {
            var w = new FileSystemWatcher(f) { IncludeSubdirectories = false, EnableRaisingEvents = true };
            w.Created += (_, e) => { if (IsLink(e.FullPath)) { Thread.Sleep(500); Handle(e.FullPath, false, tile, debug); } };
            w.Renamed += (_, e) => { if (IsLink(e.FullPath)) Handle(e.FullPath, false, tile, debug); };
            return w;
        }).ToList();
        Console.WriteLine($"Watching {string.Join(", ", folders)} (Ctrl+C to stop)");
        using var stop = new ManualResetEventSlim();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Set(); };
        stop.Wait();
        foreach (var w in watchers) w.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    struct BITMAPINFOHEADER
    {
        public int biSize, biWidth, biHeight;
        public short biPlanes, biBitCount;
        public int biCompression, biSizeImage, biXPelsPerMeter, biYPelsPerMeter, biClrUsed, biClrImportant;
        public int bmiColors;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int left, top, right, bottom; }

    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateDIBSection(IntPtr dc, ref BITMAPINFOHEADER bmi, uint usage, out IntPtr bits, IntPtr section, uint offset);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateSolidBrush(uint color);
    [DllImport("gdi32.dll")] static extern int SetBkMode(IntPtr dc, int mode);
    [DllImport("gdi32.dll")] static extern uint SetTextColor(IntPtr dc, uint color);
    [DllImport("gdi32.dll")] static extern bool GdiFlush();
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr CreateFontW(int height, int width, int escapement, int orientation, int weight, uint italic, uint underline,
                                     uint strikeOut, uint charSet, uint outPrecision, uint clipPrecision, uint quality, uint pitchAndFamily, string face);
    [DllImport("user32.dll")] static extern int FillRect(IntPtr dc, ref RECT rect, IntPtr brush);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int DrawTextW(IntPtr dc, string text, int length, ref RECT rect, uint format);
}
