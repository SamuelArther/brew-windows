using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Graphics.Imaging;
using Windows.Media.FaceAnalysis;
using Windows.Security.Cryptography;

namespace WinTools;

// faceprints: detect faces and label them against a local index (~\.faceprints\<label>\), like
// Nexuist's faceprints. Faces are found by Windows' built-in FaceDetector (Windows.Media.
// FaceAnalysis). Vision's neural feature prints have no Windows counterpart, so a faceprint here is
// a classic face descriptor computed on the aligned, equalised 64x64 face crop: HOG (gradient
// orientations) plus uniform-LBP texture histograms, compared by cosine similarity. Each label's
// avg.faceprint is the mean of its faces' prints, as in the original. Windows' detector reports no
// per-face confidence, so faceConfidence is left out.
static class Faceprints
{
    [ModuleInitializer]
    internal static void Register() => Commands.Add("faceprints", "Detect faces and label them against a local faceprint index", Run);

    const int Crop = 64;

    static int Run(string[] args)
    {
        var plain = false;
        var rest = new List<string>();
        foreach (var arg in args)
        {
            switch (arg)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        USAGE: faceprints [-p] <image path | image URL>
                               faceprints add <label> <image>
                               faceprints remove <label> <file name>
                               faceprints list
                               faceprints extract <image>

                        Classifies the faces in an image against the faceprint index in ~/.faceprints
                        (one folder per label holding face crops and their average, avg.faceprint).
                        Faces are detected with Windows' FaceDetector.

                        OPTIONS:
                          -p, --plain     Plain-text output for classify
                          --version       Show the version.
                          -h, --help      Show help information.
                        """);
                    return 0;
                case "--version": Console.WriteLine($"faceprints {Commands.Version} (wintools)"); return 0;
                case "-p" or "--plain": plain = true; break;
                default:
                    if (arg.StartsWith('-') && arg.Length > 1) throw new UsageException($"unknown option {arg}");
                    rest.Add(arg);
                    break;
            }
        }
        if (rest.Count == 0) throw new UsageException("missing an image path or URL (see faceprints --help)");
        switch (rest[0])
        {
            case "add":
                if (rest.Count != 3) throw new UsageException("usage: faceprints add <label> <image>");
                return Add(Label(rest[1]), rest[2]);
            case "remove":
                if (rest.Count != 3) throw new UsageException("usage: faceprints remove <label> <file name>");
                return Remove(Label(rest[1]), rest[2]);
            case "list":
                if (rest.Count != 1) throw new UsageException("usage: faceprints list");
                return List();
            case "extract":
                if (rest.Count != 2) throw new UsageException("usage: faceprints extract <image>");
                return Extract(rest[1]);
            default:
                if (rest.Count != 1) throw new UsageException($"unexpected argument '{rest[1]}'");
                return Classify(rest[0], plain);
        }
    }

    static string Label(string label) =>
        label.Length == 0 || label is "." or ".." || label.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            ? throw new UsageException($"invalid label '{label}'") : label;

    static string IndexDir()
    {
        var home = Environment.GetEnvironmentVariable("USERPROFILE") is { Length: > 0 } p ? p
            : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".faceprints");
    }

    // ---- operations ----

    static int Classify(string input, bool plain)
    {
        var image = LoadImage(input);
        var faces = DetectFaces(image);
        var labels = LoadLabels();
        var json = new StringBuilder();
        json.Append("{\n  \"operation\" : \"classify\",\n  \"input\" : ").Append(Str(input)).Append(",\n  \"faces\" : [");
        for (var f = 0; f < faces.Count; f++)
        {
            var box = faces[f];
            var print = Faceprint(CropFace(image, box));
            var ranks = labels.Select(l => (l.Name, Score: Cosine(print, l.Print))).OrderByDescending(r => r.Score).ToList();
            if (plain) Console.WriteLine(ranks.Count > 0 ? $"{ranks[0].Name} ({Num((float)ranks[0].Score)})" : "unknown");
            json.Append(f == 0 ? "\n" : ",\n").Append("    {\n");
            json.Append("      \"topLabel\" : ").Append(ranks.Count > 0 ? Str(ranks[0].Name) : "null").Append(",\n");
            json.Append("      \"topConfidence\" : ").Append(ranks.Count > 0 ? Num(ranks[0].Score) : "null").Append(",\n");
            json.Append("      \"ranks\" : {");
            for (var r = 0; r < ranks.Count; r++)
                json.Append(r == 0 ? "\n" : ",\n").Append("        ").Append(Str(ranks[r].Name)).Append(" : ").Append(Num(ranks[r].Score));
            json.Append(ranks.Count > 0 ? "\n      },\n" : "\n      },\n");
            // Vision-style normalised box: origin at the bottom left of the image.
            json.Append("      \"boundingBox\" : {\n");
            json.Append("        \"width\" : ").Append(Num(box.Width / (double)image.W)).Append(",\n");
            json.Append("        \"x\" : ").Append(Num(box.X / (double)image.W)).Append(",\n");
            json.Append("        \"y\" : ").Append(Num(1 - (box.Y + box.Height) / (double)image.H)).Append(",\n");
            json.Append("        \"height\" : ").Append(Num(box.Height / (double)image.H)).Append("\n      }\n    }");
        }
        json.Append(faces.Count > 0 ? "\n  ]\n}" : "\n\n  ]\n}");
        if (!plain) Console.WriteLine(json);
        if (faces.Count == 0 && plain) Console.Error.WriteLine("faceprints: no faces detected");
        return 0;
    }

    static int Add(string label, string input)
    {
        var image = LoadImage(input);
        var faces = DetectFaces(image);
        if (faces.Count == 0) { Console.Error.WriteLine($"faceprints: no faces detected in {input}"); return 1; }
        // The largest face is the subject.
        var face = CropFace(image, faces.OrderByDescending(b => b.Width * b.Height).First());
        var dir = Path.Combine(IndexDir(), label);
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, Guid.NewGuid().ToString().ToUpperInvariant() + ".png");
        File.WriteAllBytes(file, Icons.EncodePng(face.W, face.H, face.Bgra));
        UpdateAverage(dir);
        Console.WriteLine($"{{\n  \"operation\" : \"add\",\n  \"label\" : {Str(label)},\n  \"input\" : {Str(input)},\n  \"output\" : {Str(FileUrl(file))}\n}}");
        return 0;
    }

    static int Remove(string label, string name)
    {
        var dir = Path.Combine(IndexDir(), label);
        if (!Directory.Exists(dir)) throw new IOException($"no label '{label}' in {IndexDir()}");
        var file = Path.Combine(dir, Path.GetFileName(name));
        if (!File.Exists(file) || Path.GetFileName(file).Equals("avg.faceprint", StringComparison.OrdinalIgnoreCase))
            throw new IOException($"no faceprint file '{name}' under label '{label}'");
        File.Delete(file);
        UpdateAverage(dir);
        Console.WriteLine($"{{\n  \"operation\" : \"remove\",\n  \"label\" : {Str(label)},\n  \"input\" : {Str(FileUrl(file))}\n}}");
        return 0;
    }

    static int List()
    {
        var labels = LoadLabels().Select(l => l.Name).ToList();
        var sb = new StringBuilder("{\n  \"operation\" : \"list\",\n  \"labels\" : [");
        for (var i = 0; i < labels.Count; i++) sb.Append(i == 0 ? "\n" : ",\n").Append("    ").Append(Str(labels[i]));
        sb.Append(labels.Count > 0 ? "\n  ]\n}" : "\n\n  ]\n}");
        Console.WriteLine(sb);
        return 0;
    }

    static int Extract(string input)
    {
        var image = LoadImage(input);
        var faces = DetectFaces(image);
        if (faces.Count == 0) { Console.Error.WriteLine($"faceprints: no faces detected in {input}"); return 1; }
        Console.WriteLine(Vector(Faceprint(CropFace(image, faces[0]))));
        return 0;
    }

    // Recomputes <label>/avg.faceprint from the face crops in the folder (removed when none are left).
    static void UpdateAverage(string dir)
    {
        var avgFile = Path.Combine(dir, "avg.faceprint");
        float[] sum = null;
        var count = 0;
        foreach (var png in Directory.EnumerateFiles(dir, "*.png").Order(StringComparer.Ordinal))
        {
            float[] print;
            try
            {
                var (w, h, bgra) = Icons.Decode(File.ReadAllBytes(png));
                print = Faceprint(new Img(w, h, bgra));
            }
            catch (InvalidOperationException) { continue; }
            sum ??= new float[print.Length];
            for (var i = 0; i < print.Length; i++) sum[i] += print[i];
            count++;
        }
        if (count == 0) { File.Delete(avgFile); return; }
        for (var i = 0; i < sum.Length; i++) sum[i] /= count;
        File.WriteAllText(avgFile, Vector(sum, compact: true));
    }

    static List<(string Name, float[] Print)> LoadLabels()
    {
        var index = IndexDir();
        var labels = new List<(string, float[])>();
        if (!Directory.Exists(index)) return labels;
        foreach (var dir in Directory.EnumerateDirectories(index).Order(StringComparer.Ordinal))
        {
            var file = Path.Combine(dir, "avg.faceprint");
            if (!File.Exists(file)) continue;
            var print = ParseVector(File.ReadAllText(file));
            if (print != null) labels.Add((Path.GetFileName(dir), print));
        }
        return labels;
    }

    // ---- images and detection ----

    internal readonly record struct Img(int W, int H, byte[] Bgra);
    internal readonly record struct Box(int X, int Y, int Width, int Height);

    static Img LoadImage(string input)
    {
        byte[] data;
        if (input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || input.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd($"faceprints/{Commands.Version}");
            try { data = http.GetByteArrayAsync(input).GetAwaiter().GetResult(); }
            catch (HttpRequestException e) { throw new IOException($"can't download {input}: {e.Message}"); }
        }
        else
        {
            if (!File.Exists(input)) throw new IOException($"{input}: No such file");
            data = File.ReadAllBytes(input);
        }
        // Very large photos are scaled down; faces stay far above the detector's minimum size.
        var (w, h, bgra) = Icons.Decode(data);
        if (Math.Max(w, h) > 2400) (w, h, bgra) = Icons.Decode(data, 2400);
        return new Img(w, h, bgra);
    }

    static List<Box> DetectFaces(Img image) => DetectAsync(image).GetAwaiter().GetResult();

    static async Task<List<Box>> DetectAsync(Img image)
    {
        if (!FaceDetector.IsSupported) throw new InvalidOperationException("face detection is not available on this Windows installation");
        // FaceDetector wants Gray8 frames with even dimensions.
        int w = image.W & ~1, h = image.H & ~1;
        if (w < 2 || h < 2) return [];
        var gray = new byte[w * h];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var p = (y * image.W + x) * 4;
                gray[y * w + x] = (byte)((image.Bgra[p] * 29 + image.Bgra[p + 1] * 150 + image.Bgra[p + 2] * 77) >> 8);
            }
        var detector = await FaceDetector.CreateAsync();
        using var bitmap = SoftwareBitmap.CreateCopyFromBuffer(CryptographicBuffer.CreateFromByteArray(gray), BitmapPixelFormat.Gray8, w, h);
        var faces = await detector.DetectFacesAsync(bitmap);
        // Reading order: left to right, then top to bottom.
        return faces.Select(f => new Box((int)f.FaceBox.X, (int)f.FaceBox.Y, (int)f.FaceBox.Width, (int)f.FaceBox.Height))
            .OrderBy(b => b.X).ThenBy(b => b.Y).ToList();
    }

    // The face box squared up and grown by 10% (the detector's boxes are tight), clipped to the image.
    static Img CropFace(Img image, Box box)
    {
        var side = (int)Math.Round(Math.Max(box.Width, box.Height) * 1.1);
        var cx = box.X + box.Width / 2;
        var cy = box.Y + box.Height / 2;
        int x0 = Math.Max(0, cx - side / 2), y0 = Math.Max(0, cy - side / 2);
        int x1 = Math.Min(image.W, x0 + side), y1 = Math.Min(image.H, y0 + side);
        int w = Math.Max(1, x1 - x0), h = Math.Max(1, y1 - y0);
        var bgra = new byte[w * h * 4];
        for (var y = 0; y < h; y++) Buffer.BlockCopy(image.Bgra, ((y + y0) * image.W + x0) * 4, bgra, y * w * 4, w * 4);
        return new Img(w, h, bgra);
    }

    // ---- the faceprint ----

    // HOG (8x8 cells of 8 px, 9 unsigned orientations) and uniform LBP (4x4 cells, 59 bins) on the
    // histogram-equalised 64x64 grey face, each half L2-normalised; the whole print has length 1.
    internal static float[] Faceprint(Img face)
    {
        var g = Equalise(Resample(face));
        var hog = new float[8 * 8 * 9];
        for (var y = 1; y < Crop - 1; y++)
            for (var x = 1; x < Crop - 1; x++)
            {
                double gx = g[y * Crop + x + 1] - g[y * Crop + x - 1], gy = g[(y + 1) * Crop + x] - g[(y - 1) * Crop + x];
                var mag = Math.Sqrt(gx * gx + gy * gy);
                if (mag == 0) continue;
                var angle = Math.Atan2(gy, gx) * 180 / Math.PI;
                if (angle < 0) angle += 180;
                var bin = angle / 20 - 0.5;
                var b0 = (int)Math.Floor(bin);
                var frac = bin - b0;
                var cell = (y / 8 * 8 + x / 8) * 9;
                hog[cell + (b0 + 9) % 9] += (float)(mag * (1 - frac));
                hog[cell + (b0 + 1) % 9] += (float)(mag * frac);
            }
        // L2-Hys: normalise, clip at 0.2, normalise again.
        Normalise(hog);
        for (var i = 0; i < hog.Length; i++) hog[i] = Math.Min(hog[i], 0.2f);
        Normalise(hog);

        var lbp = new float[4 * 4 * 59];
        for (var y = 1; y < Crop - 1; y++)
            for (var x = 1; x < Crop - 1; x++)
            {
                var c = g[y * Crop + x];
                var code = 0;
                ReadOnlySpan<(int Dx, int Dy)> ring = [(-1, -1), (0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0)];
                for (var k = 0; k < 8; k++)
                    if (g[(y + ring[k].Dy) * Crop + x + ring[k].Dx] >= c) code |= 1 << k;
                lbp[(y / 16 * 4 + x / 16) * 59 + Uniform[code]]++;
            }
        // Each cell's histogram as a distribution, square-rooted (Hellinger), then the whole vector normalised.
        for (var cell = 0; cell < 16; cell++)
        {
            var total = 0f;
            for (var b = 0; b < 59; b++) total += lbp[cell * 59 + b];
            if (total > 0) for (var b = 0; b < 59; b++) lbp[cell * 59 + b] = MathF.Sqrt(lbp[cell * 59 + b] / total);
        }
        Normalise(lbp);

        var print = new float[hog.Length + lbp.Length];
        var half = 1 / MathF.Sqrt(2);
        for (var i = 0; i < hog.Length; i++) print[i] = hog[i] * half;
        for (var i = 0; i < lbp.Length; i++) print[hog.Length + i] = lbp[i] * half;
        return print;
    }

    // Uniform LBP codes (at most two 0/1 transitions around the circle) get bins 0-57, the rest 58.
    static readonly byte[] Uniform = BuildUniform();

    static byte[] BuildUniform()
    {
        var map = new byte[256];
        byte next = 0;
        for (var code = 0; code < 256; code++)
        {
            var transitions = 0;
            for (var k = 0; k < 8; k++) if (((code >> k) & 1) != ((code >> ((k + 1) % 8)) & 1)) transitions++;
            map[code] = transitions <= 2 ? next++ : (byte)58;
        }
        return map;
    }

    // Grey 64x64 by area averaging (box filter over each output pixel's footprint).
    static float[] Resample(Img face)
    {
        var g = new float[Crop * Crop];
        double sx = face.W / (double)Crop, sy = face.H / (double)Crop;
        for (var y = 0; y < Crop; y++)
            for (var x = 0; x < Crop; x++)
            {
                int x0 = (int)(x * sx), x1 = Math.Max(x0 + 1, (int)((x + 1) * sx)), y0 = (int)(y * sy), y1 = Math.Max(y0 + 1, (int)((y + 1) * sy));
                x1 = Math.Min(x1, face.W); y1 = Math.Min(y1, face.H);
                double sum = 0;
                var n = 0;
                for (var yy = y0; yy < y1; yy++)
                    for (var xx = x0; xx < x1; xx++)
                    {
                        var p = (yy * face.W + xx) * 4;
                        sum += 0.114 * face.Bgra[p] + 0.587 * face.Bgra[p + 1] + 0.299 * face.Bgra[p + 2];
                        n++;
                    }
                g[y * Crop + x] = n > 0 ? (float)(sum / n) : 0;
            }
        return g;
    }

    // Histogram equalisation, for lighting independence.
    static float[] Equalise(float[] g)
    {
        var hist = new int[256];
        foreach (var v in g) hist[Math.Clamp((int)v, 0, 255)]++;
        var cdf = new float[256];
        var run = 0;
        for (var i = 0; i < 256; i++) { run += hist[i]; cdf[i] = run / (float)g.Length; }
        var result = new float[g.Length];
        for (var i = 0; i < g.Length; i++) result[i] = cdf[Math.Clamp((int)g[i], 0, 255)] * 255;
        return result;
    }

    static void Normalise(float[] v)
    {
        double sum = 0;
        foreach (var x in v) sum += x * x;
        if (sum <= 0) return;
        var inv = (float)(1 / Math.Sqrt(sum));
        for (var i = 0; i < v.Length; i++) v[i] *= inv;
    }

    internal static double Cosine(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0;
        double dot = 0, na = 0, nb = 0;
        for (var i = 0; i < a.Length; i++) { dot += a[i] * b[i]; na += a[i] * a[i]; nb += b[i] * b[i]; }
        return na > 0 && nb > 0 ? dot / Math.Sqrt(na * nb) : 0;
    }

    // ---- output ----

    static string Vector(float[] v, bool compact = false) =>
        "[" + string.Join(compact ? "," : ", ", v.Select(x => x.ToString("R", CultureInfo.InvariantCulture))) + "]";

    static float[] ParseVector(string text)
    {
        text = text.Trim();
        if (!text.StartsWith('[') || !text.EndsWith(']')) return null;
        var parts = text[1..^1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var v = new float[parts.Length];
        for (var i = 0; i < parts.Length; i++)
            if (!float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out v[i])) return null;
        return v;
    }

    static string Num(double v) => v.ToString("R", CultureInfo.InvariantCulture);
    static string Num(float v) => v.ToString("R", CultureInfo.InvariantCulture);

    static string FileUrl(string path) => new Uri(Path.GetFullPath(path)).AbsoluteUri;

    // JSON string in Foundation's JSONEncoder style (slashes escaped).
    static string Str(string s)
    {
        var sb = new StringBuilder("\"");
        foreach (var ch in s)
        {
            switch (ch)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '/': sb.Append("\\/"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (ch < 0x20) sb.Append($"\\u{(int)ch:x4}");
                    else sb.Append(ch);
                    break;
            }
        }
        return sb.Append('"').ToString();
    }
}
