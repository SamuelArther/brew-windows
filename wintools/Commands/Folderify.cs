using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace WinTools;

// folderify: folder icons with a mask image engraved into the front, like lgarron's folderify, but
// in Windows' own folder style (the Windows 11 manila folder, drawn here). Writes .ico files (plus
// .icns and .iconset like the original) and applies the icon to a folder through fileicon's
// desktop.ini handling. SVG masks are rasterized by Direct2D's SVG renderer (shapes and paths;
// it has no <text> support); every other image format goes through Windows Imaging.
static class Folderify
{
    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("folderify", "Generate folder icons with a mask image engraved (Windows folder style)", Run);

    // Everything is drawn on a 1024 px canvas, in a 256-unit design grid (Unit px per unit).
    const int Master = 1024;
    const double Unit = Master / 256.0;

    static int Run(string[] args)
    {
        string mask = null, target = null, icns = null, iconset = null, ico = null, scheme = "auto", badge = null;
        bool reveal = false, empty = false, trim = true, verbose = false;
        var positional = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string inline = null;
            if (arg.StartsWith("--") && arg.Contains('=')) { inline = arg[(arg.IndexOf('=') + 1)..]; arg = arg[..arg.IndexOf('=')]; }
            string Value()
            {
                if (inline != null) return inline;
                if (i + 1 >= args.Length) throw new UsageException($"{arg} needs a value");
                return args[++i];
            }
            switch (arg)
            {
                case "-h" or "--help": PrintHelp(); return 0;
                case "-V" or "--version": Console.WriteLine($"folderify {Commands.Version} (wintools)"); return 0;
                case "--output-icns": icns = Value(); break;
                case "--output-iconset": iconset = Value(); break;
                case "--output-ico": ico = Value(); break;
                case "-r" or "--reveal": reveal = true; break;
                case "--macOS" or "--macos": Value(); break;          // macOS folder generations don't apply
                case "--set-icon-using": Value(); break;              // folderify 3's option; desktop.ini is the only way here
                case "--empty-folder": empty = true; break;
                case "--color-scheme":
                    scheme = Value().ToLowerInvariant();
                    if (scheme is not ("auto" or "light" or "dark")) throw new UsageException($"invalid --color-scheme '{scheme}' (auto, light, dark)");
                    break;
                case "--no-trim": trim = false; break;
                case "--no-progress": break;
                case "--badge":
                    badge = Value().ToLowerInvariant();
                    if (badge is not ("alias" or "locked")) throw new UsageException($"invalid --badge '{badge}' (alias, locked)");
                    break;
                case "-v" or "--verbose": verbose = true; break;
                case "--completions": return Completions(Value());
                default:
                    if (arg.StartsWith('-') && arg.Length > 1) throw new UsageException($"unknown option {arg}");
                    positional.Add(arg);
                    break;
            }
        }
        if (positional.Count == 0) throw new UsageException("no mask image given (see folderify --help)");
        if (positional.Count > 2) throw new UsageException("expected a mask and at most one target");
        mask = positional[0];
        target = positional.Count > 1 ? positional[1] : null;
        if (!File.Exists(mask)) throw new IOException($"mask file '{mask}' does not exist");
        if (target != null && !Directory.Exists(target) && !File.Exists(target)) throw new IOException($"target '{target}' does not exist");

        // Like folderify: with nothing else asked for, write <mask>.iconset and the icon file next to the mask.
        if (target == null && icns == null && iconset == null && ico == null)
        {
            var stem = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(mask)), Path.GetFileNameWithoutExtension(mask));
            iconset = stem + ".iconset";
            ico = stem + ".ico";
        }
        var dark = scheme == "dark" || scheme == "auto" && SystemIsDark();
        if (verbose) Console.Error.WriteLine($"[{mask}] Color scheme: {(dark ? "dark" : "light")}");

        var coverage = LoadMask(mask, trim, out var mw, out var mh);
        if (verbose) Console.Error.WriteLine($"[{mask}] Mask: {mw}x{mh} after {(trim ? "trimming" : "no trimming")}");
        var master = Render(coverage, mw, mh, dark, empty, badge);
        var masterPng = Icons.EncodePng(Master, Master, master);
        var scaled = new Dictionary<int, byte[]> { [Master] = masterPng };
        byte[] Png(int size)
        {
            if (scaled.TryGetValue(size, out var png)) return png;
            var (w, h, bgra) = Icons.Decode(masterPng, size);
            return scaled[size] = Icons.EncodePng(w, h, bgra);
        }

        if (iconset != null)
        {
            Directory.CreateDirectory(iconset);
            foreach (var size in new[] { 16, 32, 128, 256, 512 })
            {
                File.WriteAllBytes(Path.Combine(iconset, $"icon_{size}x{size}.png"), Png(size));
                File.WriteAllBytes(Path.Combine(iconset, $"icon_{size}x{size}@2x.png"), Png(size * 2));
            }
            if (verbose) Console.Error.WriteLine($"[{mask}] Wrote {iconset}");
        }
        if (icns != null)
        {
            File.WriteAllBytes(icns, Icons.BuildIcns([("icp4", Png(16)), ("icp5", Png(32)), ("ic11", Png(32)), ("icp6", Png(64)),
                ("ic12", Png(64)), ("ic07", Png(128)), ("ic08", Png(256)), ("ic13", Png(256)), ("ic09", Png(512)),
                ("ic14", Png(1024)), ("ic10", Png(1024))]));
            if (verbose) Console.Error.WriteLine($"[{mask}] Wrote {icns}");
        }
        var icoBytes = Icons.BuildIco(new[] { 16, 20, 24, 32, 40, 48, 64, 96, 128, 256 }.Select(s => (s, Png(s))).ToList());
        if (ico != null)
        {
            File.WriteAllBytes(ico, icoBytes);
            if (verbose) Console.Error.WriteLine($"[{mask}] Wrote {ico}");
        }
        if (target != null)
        {
            // fileicon owns desktop.ini handling (hidden desktop.ico, read-only folder, shell refresh).
            var temp = Path.Combine(Path.GetTempPath(), $"folderify-{Guid.NewGuid():N}.ico");
            File.WriteAllBytes(temp, icoBytes);
            try
            {
                var status = Commands.All["fileicon"](["set", target, temp, "-q"]);
                if (status != 0) return status;
            }
            finally { File.Delete(temp); }
            if (verbose) Console.Error.WriteLine($"[{mask}] Assigned icon to {target}");
        }
        if (reveal)
        {
            var shown = target ?? ico ?? icns ?? iconset;
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{Path.GetFullPath(shown)}\"") { UseShellExecute = false });
        }
        return 0;
    }

    static void PrintHelp() => Console.WriteLine("""
        Usage: folderify [OPTIONS] [MASK] [TARGET]

        Generate a folder icon with MASK engraved into it, in the Windows folder style.

        Arguments:
          [MASK]    Mask image (PNG with a solid black design over transparency works best; SVG, JPEG,
                    BMP, GIF, TIFF also work - opaque images use their dark parts)
          [TARGET]  Folder (or .url shortcut) to apply the icon to. Without a target or output
                    option, <mask>.ico and <mask>.iconset are written next to the mask.

        Options:
              --output-ico <ICO_FILE>         Write a Windows .ico file
              --output-icns <ICNS_FILE>       Write a macOS .icns file
              --output-iconset <FOLDER>       Write a .iconset folder of PNGs
          -r, --reveal                        Show the target or output in Explorer
              --empty-folder                  Render the folder without the paper inside
              --color-scheme <auto|light|dark> Folder colors (auto follows the Windows app theme)
              --no-trim                       Don't trim transparent margins from the mask
              --badge <alias|locked>          Add a shortcut or padlock badge
              --no-progress                   Accepted for compatibility
              --macOS <VERSION>               Accepted for compatibility (Windows style only)
          -v, --verbose                       Detailed output
              --completions <SHELL>           Print completions (bash, zsh, fish, powershell)
          -h, --help                          Print help
          -V, --version                       Print version
        """);

    static int Completions(string shell)
    {
        const string options = "--output-ico --output-icns --output-iconset --reveal --macOS --empty-folder --color-scheme --no-trim --no-progress --badge --verbose --completions --help --version";
        switch (shell.ToLowerInvariant())
        {
            case "bash":
                Console.WriteLine($"complete -o default -W \"{options}\" folderify");
                return 0;
            case "zsh":
                Console.WriteLine($"#compdef folderify\n_arguments '*:file:_files' && compadd -- {options}");
                return 0;
            case "fish":
                foreach (var o in options.Split(' ')) Console.WriteLine($"complete -c folderify -l {o[2..]}");
                return 0;
            case "powershell":
                Console.WriteLine("Register-ArgumentCompleter -Native -CommandName folderify -ScriptBlock {\n" +
                                  "    param($word)\n" +
                                  $"    '{options.Replace(" ", "','")}' | Where-Object {{ $_ -like \"$word*\" }}\n}}");
                return 0;
            default: throw new UsageException($"unsupported shell '{shell}' (bash, zsh, fish, powershell)");
        }
    }

    static bool SystemIsDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int light && light == 0;
        }
        catch (Exception e) when (e is System.Security.SecurityException or UnauthorizedAccessException) { return false; }
    }

    // ---- mask ----

    // The mask as coverage values (0..1), trimmed to its content unless trim is off. Images with any
    // transparency use their alpha (folderify's black-on-transparent masks); opaque ones use darkness.
    internal static float[] LoadMask(string file, bool trim, out int w, out int h)
    {
        var data = File.ReadAllBytes(file);
        byte[] bgra;
        if (IsSvg(file, data)) (w, h, bgra) = RasterizeSvg(data, 2048);
        else (w, h, bgra) = Icons.Decode(data);
        // Very large bitmaps are scaled down first (Fant filtering) so later resampling stays cheap.
        if (Math.Max(w, h) > 2048 && !IsSvg(file, data)) (w, h, bgra) = Icons.Decode(data, 2048);
        var transparent = false;
        for (var i = 3; i < bgra.Length && !transparent; i += 4) transparent = bgra[i] < 250;
        var cov = new float[w * h];
        for (var p = 0; p < cov.Length; p++)
        {
            float a = bgra[p * 4 + 3] / 255f;
            if (transparent) cov[p] = a;
            else
            {
                var lum = (0.0722f * bgra[p * 4] + 0.7152f * bgra[p * 4 + 1] + 0.2126f * bgra[p * 4 + 2]) / 255f;
                cov[p] = 1 - lum;
            }
        }
        if (!trim) return cov;
        int x0 = w, y0 = h, x1 = -1, y1 = -1;
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                if (cov[y * w + x] > 0.02f) { x0 = Math.Min(x0, x); x1 = Math.Max(x1, x); y0 = Math.Min(y0, y); y1 = Math.Max(y1, y); }
        if (x1 < 0) throw new InvalidOperationException("the mask image is empty (no opaque or dark pixels)");
        int tw = x1 - x0 + 1, th = y1 - y0 + 1;
        var trimmed = new float[tw * th];
        for (var y = 0; y < th; y++) Array.Copy(cov, (y + y0) * w + x0, trimmed, y * tw, tw);
        w = tw; h = th;
        return trimmed;
    }

    static bool IsSvg(string file, byte[] data) =>
        file.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
        Encoding.UTF8.GetString(data, 0, Math.Min(data.Length, 512)).Contains("<svg", StringComparison.OrdinalIgnoreCase);

    // ---- drawing ----

    readonly record struct Rgb(double R, double G, double B)
    {
        public Rgb Scale(double f) => new(R * f, G * f, B * f);
    }

    // Premultiplied RGBA canvas (0..1).
    sealed class Canvas(int size)
    {
        public readonly int Size = size;
        public readonly double[] Px = new double[size * size * 4];

        public void Blend(int x, int y, Rgb c, double a)
        {
            if (a <= 0 || x < 0 || y < 0 || x >= Size || y >= Size) return;
            if (a > 1) a = 1;
            var i = (y * Size + x) * 4;
            Px[i] = c.R * a + Px[i] * (1 - a);
            Px[i + 1] = c.G * a + Px[i + 1] * (1 - a);
            Px[i + 2] = c.B * a + Px[i + 2] * (1 - a);
            Px[i + 3] = a + Px[i + 3] * (1 - a);
        }

        // Fills a shape given by its signed distance (in px, negative inside) with a 1 px soft edge.
        // The colour may vary with y (gradients).
        public void Fill(double ux0, double uy0, double ux1, double uy1, Func<double, double, double> sdf, Func<double, Rgb> color, double opacity = 1)
        {
            int x0 = (int)Math.Floor(ux0 * Unit) - 2, y0 = (int)Math.Floor(uy0 * Unit) - 2;
            int x1 = (int)Math.Ceiling(ux1 * Unit) + 2, y1 = (int)Math.Ceiling(uy1 * Unit) + 2;
            for (var y = Math.Max(0, y0); y < Math.Min(Size, y1); y++)
            {
                var c = color((y + 0.5) / Unit);
                for (var x = Math.Max(0, x0); x < Math.Min(Size, x1); x++)
                {
                    var d = sdf(x + 0.5, y + 0.5);
                    if (d < 0.5) Blend(x, y, c, Math.Clamp(0.5 - d, 0, 1) * opacity);
                }
            }
        }

        public byte[] ToBgra()
        {
            var bgra = new byte[Size * Size * 4];
            for (var p = 0; p < Size * Size; p++)
            {
                var a = Px[p * 4 + 3];
                if (a <= 0) continue;
                bgra[p * 4 + 2] = (byte)Math.Round(Math.Clamp(Px[p * 4] / a, 0, 1) * 255);
                bgra[p * 4 + 1] = (byte)Math.Round(Math.Clamp(Px[p * 4 + 1] / a, 0, 1) * 255);
                bgra[p * 4] = (byte)Math.Round(Math.Clamp(Px[p * 4 + 2] / a, 0, 1) * 255);
                bgra[p * 4 + 3] = (byte)Math.Round(Math.Clamp(a, 0, 1) * 255);
            }
            return bgra;
        }
    }

    static Rgb Hex(int rgb) => new(((rgb >> 16) & 255) / 255.0, ((rgb >> 8) & 255) / 255.0, (rgb & 255) / 255.0);

    // Signed distance to a rounded rectangle given in design units; the result is in pixels.
    static Func<double, double, double> RoundRect(double x0, double y0, double x1, double y1, double r) => (px, py) =>
    {
        double cx = (x0 + x1) / 2 * Unit, cy = (y0 + y1) / 2 * Unit, hw = (x1 - x0) / 2 * Unit, hh = (y1 - y0) / 2 * Unit, rr = r * Unit;
        double qx = Math.Abs(px - cx) - hw + rr, qy = Math.Abs(py - cy) - hh + rr;
        return Math.Sqrt(Math.Max(qx, 0) * Math.Max(qx, 0) + Math.Max(qy, 0) * Math.Max(qy, 0)) + Math.Min(Math.Max(qx, qy), 0) - rr;
    };

    static Func<double, double, double> Circle(double cx, double cy, double r) => (px, py) =>
        Math.Sqrt((px - cx * Unit) * (px - cx * Unit) + (py - cy * Unit) * (py - cy * Unit)) - r * Unit;

    // Polygon (design units) as an approximate signed distance: exact distance to the edges, sign
    // from the even-odd rule.
    static Func<double, double, double> Polygon(params (double X, double Y)[] pts) => (px, py) =>
    {
        double best = double.MaxValue;
        var inside = false;
        for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
        {
            double ax = pts[j].X * Unit, ay = pts[j].Y * Unit, bx = pts[i].X * Unit, by = pts[i].Y * Unit;
            if ((by > py) != (ay > py) && px < (ax - bx) * (py - by) / (ay - by) + bx) inside = !inside;
            double ex = bx - ax, ey = by - ay;
            var t = Math.Clamp(((px - ax) * ex + (py - ay) * ey) / (ex * ex + ey * ey), 0, 1);
            double dx = px - (ax + t * ex), dy = py - (ay + t * ey);
            best = Math.Min(best, dx * dx + dy * dy);
        }
        return inside ? -Math.Sqrt(best) : Math.Sqrt(best);
    };

    static Func<double, double, double> Union(Func<double, double, double> a, Func<double, double, double> b) => (x, y) => Math.Min(a(x, y), b(x, y));

    // The folder: back panel with its tab, a sheet of paper, the front panel, and the engraved mask.
    static byte[] Render(float[] mask, int mw, int mh, bool dark, bool empty, string badge)
    {
        var c = new Canvas(Master);
        Rgb back = dark ? Hex(0xB07A12) : Hex(0xE9A820);
        Rgb frontTop = dark ? Hex(0xD9A23A) : Hex(0xFFD86A), frontBottom = dark ? Hex(0xC78E26) : Hex(0xF9C240);
        Rgb Front(double y) => Lerp(frontTop, frontBottom, Math.Clamp((y - 80) / 132, 0, 1));

        // Soft shadow under the folder.
        c.Fill(18, 60, 238, 220, RoundRect(20, 66, 236, 218, 12), _ => new Rgb(0, 0, 0), 0.10);
        // Back panel and tab (the tab's right edge slopes down like Explorer's folder).
        var backShape = Union(RoundRect(20, 46, 236, 212, 10),
            Union(RoundRect(20, 34, 100, 70, 9), Polygon((90, 34), (104, 34), (122, 52), (90, 52))));
        c.Fill(20, 34, 236, 212, backShape, _ => back);
        if (!empty)
        {
            c.Fill(30, 54, 226, 110, RoundRect(30, 54, 226, 110, 4), _ => Hex(0xD9D9D9));
            c.Fill(32, 56, 224, 110, RoundRect(32, 56, 224, 110, 3), _ => Hex(0xFBFBFB));
        }
        c.Fill(20, 76, 236, 212, RoundRect(20, 76, 236, 212, 10), Front);
        // A thin highlight along the front panel's top edge.
        c.Fill(20, 76, 236, 80, RoundRect(21, 77, 235, 79, 1), _ => new Rgb(1, 1, 1), 0.35);

        Engrave(c, mask, mw, mh, frontBottom.Scale(0.62));
        if (badge == "alias") AliasBadge(c);
        else if (badge == "locked") LockBadge(c);
        return c.ToBgra();
    }

    static Rgb Lerp(Rgb a, Rgb b, double t) => new(a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t);

    // Fits the mask into the front panel and engraves it: a light edge below (the cut catching the
    // light) under a dark fill, as folderify does on macOS folders.
    static void Engrave(Canvas c, float[] mask, int mw, int mh, Rgb ink)
    {
        double rx0 = 52 * Unit, ry0 = 98 * Unit, rw = 152 * Unit, rh = 96 * Unit;
        var s = Math.Min(rw / mw, rh / mh);
        int tw = Math.Max(1, (int)Math.Round(mw * s)), th = Math.Max(1, (int)Math.Round(mh * s));
        int ox = (int)Math.Round(rx0 + (rw - tw) / 2), oy = (int)Math.Round(ry0 + (rh - th) / 2);
        var placed = new float[tw * th];
        // Area-average when shrinking, bilinear when enlarging.
        var step = Math.Max(1, (int)Math.Ceiling(1 / s));
        for (var y = 0; y < th; y++)
            for (var x = 0; x < tw; x++)
            {
                double sum = 0;
                for (var sy = 0; sy < step; sy++)
                    for (var sx = 0; sx < step; sx++)
                        sum += Sample(mask, mw, mh, (x + (sx + 0.5) / step) / s - 0.5, (y + (sy + 0.5) / step) / s - 0.5);
                placed[y * tw + x] = (float)(sum / (step * step));
            }
        float At(int x, int y) => x < 0 || y < 0 || x >= tw || y >= th ? 0 : placed[y * tw + x];
        var offset = (int)Math.Round(1.5 * Unit);
        for (var y = -offset; y < th + offset; y++)
            for (var x = 0; x < tw; x++)
            {
                var highlight = Math.Max(0, At(x, y - offset) - At(x, y));
                c.Blend(ox + x, oy + y, new Rgb(1, 0.98, 0.9), highlight * 0.55);
                c.Blend(ox + x, oy + y, ink, At(x, y) * 0.92);
            }
    }

    static double Sample(float[] m, int w, int h, double x, double y)
    {
        x = Math.Clamp(x, 0, w - 1); y = Math.Clamp(y, 0, h - 1);
        int x0 = (int)x, y0 = (int)y, x1 = Math.Min(x0 + 1, w - 1), y1 = Math.Min(y0 + 1, h - 1);
        double fx = x - x0, fy = y - y0;
        return (m[y0 * w + x0] * (1 - fx) + m[y0 * w + x1] * fx) * (1 - fy) + (m[y1 * w + x0] * (1 - fx) + m[y1 * w + x1] * fx) * fy;
    }

    // Explorer's shortcut overlay: a white tile with a blue curved arrow, bottom left.
    static void AliasBadge(Canvas c)
    {
        c.Fill(10, 150, 82, 222, RoundRect(12, 152, 80, 220, 8), _ => new Rgb(0, 0, 0), 0.25);
        c.Fill(12, 150, 78, 218, RoundRect(12, 150, 78, 216, 7), _ => Hex(0xFFFFFF));
        var blue = Hex(0x1A6FD0);
        // Shaft: a quarter ring from the bottom left curving up to the right, then the arrow head.
        Func<double, double, double> ring = (px, py) =>
        {
            double cx = 64 * Unit, cy = 206 * Unit;
            var d = Math.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
            var band = Math.Abs(d - 30 * Unit) - 6 * Unit;
            var inQuarter = px <= cx + 0.5 * Unit && py <= cy;
            return inQuarter ? band : Math.Max(band, Math.Max(px - cx, py - cy));
        };
        c.Fill(28, 168, 66, 210, ring, _ => blue);
        c.Fill(56, 160, 76, 196, Polygon((56, 162), (76, 176), (56, 190)), _ => blue);
    }

    // A padlock, bottom right.
    static void LockBadge(Canvas c)
    {
        var dark = Hex(0x3A3A44);
        Func<double, double, double> shackle = (px, py) =>
        {
            var ring = Math.Abs(Circle(204, 178, 16)(px, py)) - 4.5 * Unit;
            return py <= 186 * Unit ? ring : Math.Max(ring, py - 186 * Unit);
        };
        c.Fill(180, 154, 228, 190, shackle, _ => Hex(0x9AA0AA));
        c.Fill(174, 180, 234, 228, RoundRect(176, 182, 232, 226, 7), _ => new Rgb(0, 0, 0), 0.25);
        c.Fill(176, 180, 232, 224, RoundRect(176, 180, 232, 224, 7), _ => dark);
        c.Fill(198, 192, 210, 214, Union(Circle(204, 198, 5), RoundRect(201.5, 198, 206.5, 212, 2)), _ => Hex(0xE0E0E0));
    }

    // ---- SVG through Direct2D ----

    // Renders an SVG to straight BGRA whose longer side is `size` px. Direct2D renders SVG documents
    // into a WIC bitmap; a document without a viewBox gets one from its width/height so it scales.
    static unsafe (int W, int H, byte[] Bgra) RasterizeSvg(byte[] data, int size)
    {
        var text = Encoding.UTF8.GetString(data);
        // Direct2D doesn't know CSS's "transparent" colour keyword (it would paint black).
        text = Regex.Replace(text, @"\b(fill|stroke)(\s*[=:]\s*[""']?)transparent\b", "$1$2none", RegexOptions.IgnoreCase);
        var root =Regex.Match(text, @"<svg\b[^>]*>", RegexOptions.IgnoreCase);
        if (!root.Success) throw new InvalidOperationException("not an SVG document");
        double vw = 0, vh = 0;
        var viewBox = Regex.Match(root.Value, @"viewBox\s*=\s*[""']\s*([-\d.eE]+)[\s,]+([-\d.eE]+)[\s,]+([\d.eE]+)[\s,]+([\d.eE]+)", RegexOptions.IgnoreCase);
        if (viewBox.Success)
        {
            vw = double.Parse(viewBox.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);
            vh = double.Parse(viewBox.Groups[4].Value, System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
            vw = Length(root.Value, "width"); vh = Length(root.Value, "height");
            if (vw <= 0 || vh <= 0) { vw = vh = 100; }
            var inject = $" viewBox=\"0 0 {vw.ToString(System.Globalization.CultureInfo.InvariantCulture)} {vh.ToString(System.Globalization.CultureInfo.InvariantCulture)}\"";
            text = text[..(root.Index + 4)] + inject + text[(root.Index + 4)..];
        }
        if (vw <= 0 || vh <= 0) throw new InvalidOperationException("SVG has an empty viewBox");
        // The document's own width/height would fix its size; drop them so it fills the viewport.
        root = Regex.Match(text, @"<svg\b[^>]*>", RegexOptions.IgnoreCase);
        var cleaned = Regex.Replace(root.Value, @"\s(width|height)\s*=\s*(""[^""]*""|'[^']*')", "", RegexOptions.IgnoreCase);
        text = text[..root.Index] + cleaned + text[(root.Index + root.Length)..];
        var scale = size / Math.Max(vw, vh);
        int w = Math.Max(1, (int)Math.Round(vw * scale)), h = Math.Max(1, (int)Math.Round(vh * scale));
        var bytes = Encoding.UTF8.GetBytes(text);

        CoInitializeEx(IntPtr.Zero, 0);
        IntPtr factory = 0, wic = 0, bitmap = 0, target = 0, dc = 0, stream = 0, doc = 0;
        try
        {
            var iidFactory1 = new Guid("bb12d362-daee-4b9a-aa1d-14ba401cfa1f");
            Check(D2D1CreateFactory(0, &iidFactory1, IntPtr.Zero, &factory), "Direct2D");
            var clsidWic = new Guid("cacaf262-9370-4615-a13b-9f5539da4c0a");
            var iidWic = new Guid("ec5ec8a9-c395-4314-9c77-54d7a935ff70");
            Check(CoCreateInstance(&clsidWic, IntPtr.Zero, 1, &iidWic, &wic), "Windows Imaging");
            var pbgra = new Guid("6fddc324-4e03-4bfe-b185-3d77768dc910");
            // IWICImagingFactory::CreateBitmap
            Check(((delegate* unmanaged<IntPtr, uint, uint, Guid*, int, IntPtr*, int>)Slot(wic, 17))(wic, (uint)w, (uint)h, &pbgra, 1, &bitmap), "CreateBitmap");
            var props = new RenderTargetProperties { PixelFormat = 87, AlphaMode = 1, DpiX = 96, DpiY = 96 };
            // ID2D1Factory::CreateWicBitmapRenderTarget
            Check(((delegate* unmanaged<IntPtr, IntPtr, RenderTargetProperties*, IntPtr*, int>)Slot(factory, 13))(factory, bitmap, &props, &target), "render target");
            var iidDc5 = new Guid("7836d248-68cc-4df6-b9e8-de991bf62eb7");
            if (((delegate* unmanaged<IntPtr, Guid*, IntPtr*, int>)Slot(target, 0))(target, &iidDc5, &dc) < 0)
                throw new InvalidOperationException("SVG masks need Direct2D's SVG renderer (Windows 10 1703 or later)");
            fixed (byte* p = bytes) stream = SHCreateMemStream(p, (uint)bytes.Length);
            if (stream == 0) throw new InvalidOperationException("out of memory");
            // ID2D1DeviceContext5::CreateSvgDocument / DrawSvgDocument; ID2D1RenderTarget::BeginDraw/Clear/EndDraw
            Check(((delegate* unmanaged<IntPtr, IntPtr, SizeF, IntPtr*, int>)Slot(dc, 115))(dc, stream, new SizeF { W = w, H = h }, &doc), "SVG document");
            ((delegate* unmanaged<IntPtr, void>)Slot(target, 48))(target);
            var clear = new ColorF();
            ((delegate* unmanaged<IntPtr, ColorF*, void>)Slot(target, 47))(target, &clear);
            ((delegate* unmanaged<IntPtr, IntPtr, void>)Slot(dc, 116))(dc, doc);
            ulong tag1, tag2;
            Check(((delegate* unmanaged<IntPtr, ulong*, ulong*, int>)Slot(target, 49))(target, &tag1, &tag2), "SVG rendering");
            var pixels = new byte[w * h * 4];
            fixed (byte* p = pixels)
                Check(((delegate* unmanaged<IntPtr, IntPtr, uint, uint, byte*, int>)Slot(bitmap, 7))(bitmap, IntPtr.Zero, (uint)w * 4, (uint)pixels.Length, p), "CopyPixels");
            // Premultiplied to straight alpha.
            for (var i = 0; i < pixels.Length; i += 4)
            {
                var a = pixels[i + 3];
                if (a is 0 or 255) continue;
                for (var k = 0; k < 3; k++) pixels[i + k] = (byte)Math.Min(255, pixels[i + k] * 255 / a);
            }
            return (w, h, pixels);
        }
        finally
        {
            foreach (var o in new[] { doc, stream, dc, target, bitmap, wic, factory })
                if (o != 0) ((delegate* unmanaged<IntPtr, uint>)Slot(o, 2))(o);
        }
    }

    static double Length(string tag, string name)
    {
        var m = Regex.Match(tag, $@"\s{name}\s*=\s*[""']\s*([\d.eE]+)\s*(px)?\s*[""']", RegexOptions.IgnoreCase);
        return m.Success && double.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;
    }

    static unsafe IntPtr Slot(IntPtr com, int index) => (*(IntPtr**)com)[index];

    static void Check(int hr, string what)
    {
        if (hr < 0) throw new InvalidOperationException($"{what} failed (0x{hr:X8})");
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RenderTargetProperties
    {
        public int Type;
        public int PixelFormat;     // DXGI_FORMAT_B8G8R8A8_UNORM
        public int AlphaMode;       // D2D1_ALPHA_MODE_PREMULTIPLIED
        public float DpiX, DpiY;
        public int Usage, MinLevel;
    }

    [StructLayout(LayoutKind.Sequential)] struct SizeF { public float W, H; }
    [StructLayout(LayoutKind.Sequential)] struct ColorF { public float R, G, B, A; }

    [DllImport("d2d1.dll")]
    static extern unsafe int D2D1CreateFactory(int type, Guid* riid, IntPtr options, IntPtr* factory);

    [DllImport("ole32.dll")]
    static extern unsafe int CoCreateInstance(Guid* clsid, IntPtr outer, int context, Guid* iid, IntPtr* obj);

    [DllImport("ole32.dll")]
    static extern int CoInitializeEx(IntPtr reserved, int coinit);

    [DllImport("shlwapi.dll")]
    static extern unsafe IntPtr SHCreateMemStream(byte* data, uint size);
}
