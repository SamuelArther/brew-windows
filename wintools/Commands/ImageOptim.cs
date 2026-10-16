using System.Buffers.Binary;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace WinTools;

// imageoptim (ImageOptim-CLI): lossless image optimisation done in-process instead of by the
// ImageOptim app. PNG: metadata chunks dropped, lossless colour/bit-depth reductions, every filter
// strategy tried at maximum deflate. JPEG: metadata segments dropped (orientation and colour
// profile kept). GIF: comments and non-animation application blocks dropped. Files are only
// rewritten when they get smaller.
static class ImageOptim
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("imageoptim", "Losslessly optimise PNG, JPEG and GIF images (ImageOptim-CLI)", Run);
    }

    static bool color = true;
    static TextWriter output = Console.Out;
    static string check = "✓";

    static int Run(string[] args)
    {
        bool dryRun = false, stats = true, lossless = true;
        var patterns = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage: imageoptim [options] [patterns...]

                        Options:
                          -a, --imagealpha           enable ImageAlpha (lossy PNG quantisation; not available on Windows)
                          -j, --jpegmini             enable JPEGmini (not available on Windows)
                          -C, --no-color             output to the terminal without colors
                          -I, --no-imageoptim        disable the lossless optimisation
                          -Q, --no-quit              accepted for compatibility
                          -S, --no-stats             do not display file size savings
                          -D, --dry-run              list the images that would be optimised
                          -V, --version              output the version number
                          -h, --help                 output usage information

                        Patterns are files, folders (searched recursively) or globs such as "assets/**/*.png".
                        """);
                    return 0;
                case "-V" or "--version": Console.WriteLine(Commands.Version); return 0;
                case "-a" or "--imagealpha" or "--number-of-colors" or "--quality" or "--speed":
                    Console.Error.WriteLine("imageoptim: ImageAlpha (lossy PNG quantisation) is a macOS app and isn't available on Windows.");
                    return 1;
                case "-j" or "--jpegmini":
                    Console.Error.WriteLine("imageoptim: JPEGmini is a macOS app and isn't available on Windows.");
                    return 1;
                case "-C" or "--no-color": color = false; break;
                case "-I" or "--no-imageoptim": lossless = false; break;
                case "-Q" or "--no-quit": break;
                case "-S" or "--no-stats": stats = false; break;
                case "-D" or "--dry-run": dryRun = true; break;
                default:
                    if (arg.StartsWith('-')) throw new UsageException($"unknown option '{arg}'");
                    patterns.Add(arg);
                    break;
            }
        }
        if (patterns.Count == 0) patterns.Add(".");
        // Redirected output is UTF-8; a console that can't show the check mark gets a plain one.
        if (Console.IsOutputRedirected)
        {
            color = false;
            output = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true };
        }
        else if (Console.OutputEncoding.CodePage != 65001) check = "+";
        var files = patterns.SelectMany(Expand).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (files.Count == 0) { Info("No images found"); return 1; }
        if (dryRun)
        {
            Info($"Would optimise {files.Count} image{(files.Count == 1 ? "" : "s")}:");
            foreach (var f in files) output.WriteLine(f);
            return 0;
        }
        if (!lossless) { Info("Nothing to do (--no-imageoptim)"); return 0; }
        Info("Running lossless optimisation...");
        long before = 0, after = 0;
        var status = 0;
        foreach (var file in files)
        {
            try
            {
                var data = File.ReadAllBytes(file);
                var optimised = Optimise(data, file);
                var size = data.Length;
                if (optimised != null && optimised.Length < size)
                {
                    var temp = file + ".imageoptim-tmp";
                    File.WriteAllBytes(temp, optimised);
                    File.Move(temp, file, overwrite: true);
                }
                var now = optimised != null && optimised.Length < size ? optimised.Length : size;
                before += size;
                after += now;
                if (stats) Line(Green(check), file, size, now);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                Console.Error.WriteLine($"{Red("!")} {file}: {e.Message}");
                status = 1;
            }
        }
        if (stats) Line(Green(check), "TOTAL", before, after);
        output.WriteLine($"{Green(check)} Finished");
        return status;
    }

    static void Info(string s) => output.WriteLine($"{(color ? "\u001b[36mi\u001b[0m" : "i")} {s}");
    static string Green(string s) => color ? $"\u001b[32m{s}\u001b[0m" : s;
    static string Red(string s) => color ? $"\u001b[31m{s}\u001b[0m" : s;

    static void Line(string mark, string name, long was, long now)
    {
        static string Kb(long b) => (b / 1000.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "kB";
        var pct = was == 0 ? 0 : (was - now) * 100.0 / was;
        output.WriteLine($"{mark} {name} was: {Kb(was)} now: {Kb(now)} saving: {Kb(was - now)} ({pct.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}%)");
    }

    static readonly string[] Extensions = [".png", ".jpg", ".jpeg", ".gif"];

    static IEnumerable<string> Expand(string pattern)
    {
        if (File.Exists(pattern)) return [pattern];
        if (Directory.Exists(pattern))
            return Directory.EnumerateFiles(pattern, "*", SearchOption.AllDirectories)
                .Where(f => Extensions.Contains(Path.GetExtension(f).ToLowerInvariant())).Order();
        if (pattern.IndexOfAny(['*', '?']) < 0) { Console.Error.WriteLine($"imageoptim: {pattern}: no such file"); return []; }
        // Glob: the part before the first wildcard segment is the base folder; ** spans folders.
        var normalised = pattern.Replace('\\', '/');
        var segments = normalised.Split('/');
        var firstWild = Array.FindIndex(segments, s => s.IndexOfAny(['*', '?']) >= 0);
        var baseDir = firstWild == 0 ? "." : string.Join('/', segments[..firstWild]);
        if (baseDir.Length == 0) baseDir = "/";
        if (!Directory.Exists(baseDir)) return [];
        var rest = string.Join('/', segments[firstWild..]);
        var regex = new Regex("^" + Regex.Escape(rest).Replace(@"\*\*/", "(.*/)?").Replace(@"\*\*", ".*").Replace(@"\*", "[^/]*").Replace(@"\?", "[^/]") + "$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return Directory.EnumerateFiles(baseDir, "*", SearchOption.AllDirectories)
            .Where(f => regex.IsMatch(Path.GetRelativePath(baseDir, f).Replace('\\', '/')))
            .Select(f => baseDir == "." ? Path.GetRelativePath(".", f) : f).Order();
    }

    internal static byte[] Optimise(byte[] data, string name)
    {
        ReadOnlySpan<byte> d = data;
        if (d.StartsWith(Icons.PngSignature)) return Png(data);
        if (d.Length > 3 && d[0] == 0xFF && d[1] == 0xD8) return Jpeg(data);
        if (d.StartsWith("GIF87a"u8) || d.StartsWith("GIF89a"u8)) return Gif(data);
        throw new InvalidDataException("not a PNG, JPEG or GIF image");
    }

    // ---- PNG ----

    static readonly HashSet<string> KeepChunks = ["IHDR", "PLTE", "tRNS", "IDAT", "sRGB", "iCCP", "gAMA", "cHRM", "sBIT"];

    static byte[] Png(byte[] data)
    {
        var chunks = Icons.PngChunks(data) ?? throw new InvalidDataException("damaged PNG");
        // Animated PNGs keep their frame chunks as they are; only metadata goes.
        if (chunks.Any(c => c.Type == "acTL"))
            return Icons.PngFile(chunks.Where(c => KeepChunks.Contains(c.Type) || c.Type is "acTL" or "fcTL" or "fdAT"));
        var ihdr = chunks.First(c => c.Type == "IHDR").Data;
        int width = BinaryPrimitives.ReadInt32BigEndian(ihdr), height = BinaryPrimitives.ReadInt32BigEndian(ihdr.AsSpan(4));
        int depth = ihdr[8], colorType = ihdr[9], interlace = ihdr[12];
        var idat = chunks.Where(c => c.Type == "IDAT").SelectMany(c => c.Data).ToArray();
        var filtered = Icons.Inflate(idat);
        var kept = chunks.Where(c => KeepChunks.Contains(c.Type) && c.Type is not ("IDAT" or "IHDR")).ToList();
        byte[] best;
        if (interlace != 0)
            best = Icons.Deflate(filtered, CompressionLevel.SmallestSize);
        else
        {
            var channels = colorType switch { 0 => 1, 2 => 3, 3 => 1, 4 => 2, _ => 4 };
            var stride = (width * channels * depth + 7) / 8;
            var raw = Icons.Unfilter(filtered, stride, height, Math.Max(1, channels * depth / 8)) ?? throw new InvalidDataException("damaged PNG data");
            (raw, colorType, depth, channels) = Reduce(raw, width, height, colorType, depth, channels, kept);
            stride = (width * channels * depth + 7) / 8;
            var bpp = Math.Max(1, channels * depth / 8);
            best = null;
            // Palette and low-bit images usually do best unfiltered; try everything anyway.
            foreach (var type in new[] { -1, 0, 1, 2, 3, 4 })
            {
                var candidate = Icons.Deflate(Icons.Filter(raw, stride, height, bpp, type), CompressionLevel.SmallestSize);
                if (best == null || candidate.Length < best.Length) best = candidate;
            }
            ihdr = (byte[])ihdr.Clone();
            ihdr[8] = (byte)depth;
            ihdr[9] = (byte)colorType;
        }
        var output = new List<(string, byte[])> { ("IHDR", ihdr) };
        // Ancillary chunks keep their place relative to PLTE and IDAT.
        output.AddRange(kept.Where(c => c.Type is not "tRNS" && c.Type != "PLTE"));
        output.AddRange(kept.Where(c => c.Type == "PLTE"));
        output.AddRange(kept.Where(c => c.Type == "tRNS"));
        output.Add(("IDAT", best));
        return Icons.PngFile(output);
    }

    // Lossless reductions on 8/16-bit truecolour and greyscale: 16 -> 8 bits when the low bytes repeat
    // the high ones, alpha dropped when fully opaque, RGB -> grey when every pixel is grey.
    static (byte[], int, int, int) Reduce(byte[] raw, int width, int height, int colorType, int depth, int channels, List<(string Type, byte[] Data)> kept)
    {
        if (colorType == 3 || depth < 8 || kept.Any(c => c.Type is "tRNS" or "sBIT")) return (raw, colorType, depth, channels);
        if (depth == 16)
        {
            var reducible = true;
            for (var i = 0; i < raw.Length && reducible; i += 2) reducible = raw[i] == raw[i + 1];
            if (!reducible) return (raw, colorType, depth, channels);
            var eight = new byte[raw.Length / 2];
            for (var i = 0; i < eight.Length; i++) eight[i] = raw[i * 2];
            raw = eight;
            depth = 8;
        }
        var pixels = width * height;
        if (colorType is 4 or 6)
        {
            var opaque = true;
            for (var p = 0; p < pixels && opaque; p++) opaque = raw[p * channels + channels - 1] == 255;
            if (opaque)
            {
                var stripped = new byte[pixels * (channels - 1)];
                for (var p = 0; p < pixels; p++) Array.Copy(raw, p * channels, stripped, p * (channels - 1), channels - 1);
                raw = stripped;
                channels--;
                colorType = colorType == 6 ? 2 : 0;
            }
        }
        if (colorType is 2 or 6)
        {
            var grey = true;
            for (var p = 0; p < pixels && grey; p++) grey = raw[p * channels] == raw[p * channels + 1] && raw[p * channels] == raw[p * channels + 2];
            if (grey)
            {
                var newChannels = channels - 2;
                var g = new byte[pixels * newChannels];
                for (var p = 0; p < pixels; p++)
                {
                    g[p * newChannels] = raw[p * channels];
                    if (newChannels == 2) g[p * newChannels + 1] = raw[p * channels + 3];
                }
                raw = g;
                channels = newChannels;
                colorType = colorType == 6 ? 4 : 0;
            }
        }
        return (raw, colorType, depth, channels);
    }

    // ---- JPEG ----

    static byte[] Jpeg(byte[] data)
    {
        using var output = new MemoryStream();
        output.Write(data, 0, 2);
        var pos = 2;
        while (pos + 4 <= data.Length)
        {
            if (data[pos] != 0xFF) throw new InvalidDataException("damaged JPEG");
            var marker = data[pos + 1];
            if (marker == 0xFF) { pos++; continue; }
            if (marker == 0xDA)
            {
                // Start of scan: the rest is image data.
                output.Write(data, pos, data.Length - pos);
                return output.ToArray();
            }
            if (marker is >= 0xD0 and <= 0xD7 or 0x01) { output.Write(data, pos, 2); pos += 2; continue; }
            var length = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(pos + 2));
            if (pos + 2 + length > data.Length) throw new InvalidDataException("damaged JPEG");
            var segment = data.AsSpan(pos, 2 + length);
            var payload = segment[4..];
            var keep = marker switch
            {
                0xE0 => payload.StartsWith("JFIF\0"u8),                                // JFIF header
                0xE1 => payload.StartsWith("Exif\0\0"u8) && Orientation(payload[6..].ToArray()) is not (0 or 1),
                0xE2 => payload.StartsWith("ICC_PROFILE\0"u8),                         // colour profile
                0xEE => payload.StartsWith("Adobe"u8),                                 // colour transform
                0xFE => false,                                                         // comment
                >= 0xE3 and <= 0xEF => false,                                          // other APPn metadata
                _ => true,
            };
            if (keep) output.Write(segment);
            pos += 2 + length;
        }
        throw new InvalidDataException("damaged JPEG (no image data)");
    }

    // EXIF orientation tag (0x0112) from IFD0 of a TIFF header; 0 when absent.
    static int Orientation(byte[] tiff)
    {
        if (tiff.Length < 8) return 0;
        var little = tiff[0] == 'I';
        int U16(int at) => at + 2 <= tiff.Length ? (little ? BinaryPrimitives.ReadUInt16LittleEndian(tiff.AsSpan(at)) : BinaryPrimitives.ReadUInt16BigEndian(tiff.AsSpan(at))) : 0;
        long U32(int at) => at + 4 <= tiff.Length ? (little ? BinaryPrimitives.ReadUInt32LittleEndian(tiff.AsSpan(at)) : BinaryPrimitives.ReadUInt32BigEndian(tiff.AsSpan(at))) : 0;
        var ifd = (int)U32(4);
        var count = U16(ifd);
        for (var i = 0; i < count; i++)
        {
            var entry = ifd + 2 + i * 12;
            if (U16(entry) == 0x0112) return U16(entry + 8);
        }
        return 0;
    }

    // ---- GIF ----

    static byte[] Gif(byte[] data)
    {
        using var output = new MemoryStream();
        var pos = 13;
        if (data.Length < 13) throw new InvalidDataException("damaged GIF");
        if ((data[10] & 0x80) != 0) pos += 3 * (2 << (data[10] & 7));
        output.Write(data, 0, pos);
        int SubBlocks(int at)
        {
            while (at < data.Length && data[at] != 0) at += data[at] + 1;
            return at + 1;
        }
        while (pos < data.Length)
        {
            var start = pos;
            switch (data[pos])
            {
                case 0x3B:
                    output.WriteByte(0x3B);
                    return output.ToArray();
                case 0x2C:
                    pos += 10;
                    if ((data[start + 9] & 0x80) != 0) pos += 3 * (2 << (data[start + 9] & 7));
                    pos = SubBlocks(pos + 1);   // LZW minimum code size, then the image data
                    output.Write(data, start, pos - start);
                    break;
                case 0x21:
                    var label = data[pos + 1];
                    pos = SubBlocks(pos + 2);
                    var keep = label switch
                    {
                        0xFE => false,                                                    // comment
                        0xFF => data.AsSpan(start + 3, Math.Min(11, data.Length - start - 3)) is var id
                                && (id.SequenceEqual("NETSCAPE2.0"u8) || id.SequenceEqual("ANIMEXTS1.0"u8)),
                        _ => true,
                    };
                    if (keep) output.Write(data, start, pos - start);
                    break;
                default:
                    throw new InvalidDataException("damaged GIF");
            }
        }
        output.WriteByte(0x3B);
        return output.ToArray();
    }
}
