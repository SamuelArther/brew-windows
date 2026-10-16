using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace WinTools;

// awww / awww-daemon: "an answer to your wayland wallpaper woes", for the Windows desktop. As on
// Wayland, the daemon owns the backgrounds and the client sends it commands (here over a named pipe,
// \\.\pipe\awww-daemon<namespace>-<user>). The daemon renders each image for each monitor itself,
// with awww's resize modes (crop with gravity, fit, stretch, no) and filters, padding with the fill
// color, and hands the result to Windows through IDesktopWallpaper per monitor; animated GIFs are
// played frame by frame (pause/unpause/toggle stop and resume them), `clear` paints a solid color,
// and the last image per output is cached so `restore` and a restarted daemon bring it back.
// Windows' wallpaper engine has no programmable transitions, so the --transition-* options are
// accepted and the switch is immediate.
static partial class Awww
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("awww", "Set per-monitor wallpapers (and animated GIFs) through awww-daemon", Client);
        Commands.Add("awww-daemon", "The awww wallpaper daemon (owns the desktop backgrounds)", Daemon);
    }

    const string Version = "0.12.1";

    static string PipeName(string ns) => $"awww-daemon{ns}-{Environment.UserName}";
    static string CacheDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "awww");

    // ---- client ----

    static int Client(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help") { ClientUsage(); return args.Length == 0 ? 2 : 0; }
        if (args[0] is "-V" or "--version") { Console.WriteLine($"awww {Version}"); return 0; }
        var command = args[0];
        var options = new Dictionary<string, string>();
        var namespaces = new List<string>();
        var positional = new List<string>();
        bool all = false, json = false;
        for (var i = 1; i < args.Length; i++)
        {
            var a = args[i];
            var eq = a.StartsWith("--") ? a.IndexOf('=') : -1;
            var name = eq > 0 ? a[..eq] : a;
            string Value() => eq > 0 ? a[(eq + 1)..] : ++i < args.Length ? args[i] : throw new UsageException($"a value is required for '{name}'");
            switch (name)
            {
                case "-h" or "--help": CommandUsage(command); return 0;
                case "-a" or "--all": all = true; break;
                case "-n" or "--namespace": namespaces.Add(Value()); break;
                case "-o" or "--outputs": options["outputs"] = Value(); break;
                case "-j" or "--json": json = true; break;
                case "--no-resize": options["resize"] = "no"; break;
                case "--resize": options["resize"] = Value(); break;
                case "--crop-gravity": options["gravity"] = Value(); break;
                case "--fill-color": options["fill"] = Value(); break;
                case "-f" or "--filter": options["filter"] = Value(); break;
                case "-t" or "--transition-type" or "--transition-step" or "--transition-duration" or "--transition-fps" or "--transition-angle"
                    or "--transition-pos" or "--transition-bezier" or "--transition-wave" or "--invert-y" or "--transition-easing":
                    if (name is not "--invert-y") Value();
                    break;   // Windows switches wallpapers without transitions
                default:
                    if (a.StartsWith('-') && a != "-") throw new UsageException($"unexpected argument '{a}' found");
                    positional.Add(a);
                    break;
            }
        }
        if (command == "clear-cache")
        {
            if (Directory.Exists(CacheDir)) Directory.Delete(CacheDir, true);
            return 0;
        }
        if (command is not ("clear" or "restore" or "img" or "toggle" or "pause" or "unpause" or "kill" or "query"))
            throw new UsageException($"unrecognized subcommand '{command}'");
        if (namespaces.Count == 0) namespaces.Add("");
        if (all) namespaces = AllNamespaces();

        var request = new List<string> { command };
        switch (command)
        {
            case "clear":
                request.Add(Hex(positional.Count > 0 ? positional[0] : "000000"));
                request.Add(options.GetValueOrDefault("outputs", ""));
                break;
            case "restore":
                request.Add(options.GetValueOrDefault("outputs", ""));
                break;
            case "img":
            {
                if (positional.Count == 0) throw new UsageException("the following required arguments were not provided:\n  <IMAGE>");
                var image = positional[0];
                if (image == "-")
                {
                    // stdin: the daemon reads files, so the image goes through a cache file.
                    Directory.CreateDirectory(CacheDir);
                    image = Path.Combine(CacheDir, "stdin-" + Guid.NewGuid().ToString("N"));
                    using var file = File.Create(image);
                    Console.OpenStandardInput().CopyTo(file);
                }
                else if (image.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) image = "color:" + Hex(image[2..]);
                else
                {
                    image = Path.GetFullPath(image);
                    if (!File.Exists(image)) throw new InvalidOperationException($"failed to open image: {positional[0]}: No such file or directory");
                }
                request.Add(image);
                request.Add(options.GetValueOrDefault("outputs", ""));
                request.Add(options.GetValueOrDefault("resize", "crop"));
                request.Add(options.GetValueOrDefault("gravity", "center"));
                request.Add(Hex(options.GetValueOrDefault("fill", "000000")));
                request.Add(options.GetValueOrDefault("filter", "Lanczos3"));
                break;
            }
        }
        if (command is "img" or "clear") Validate(request);

        var results = new List<(string Namespace, List<string> Lines)>();
        foreach (var ns in namespaces)
        {
            using var pipe = new NamedPipeClientStream(".", PipeName(ns), PipeDirection.InOut, PipeOptions.CurrentUserOnly);
            try { pipe.Connect(200); }
            catch (TimeoutException)
            {
                Console.Error.WriteLine($"Error: \"Socket file '\\\\.\\pipe\\{PipeName(ns)}' not found. Make sure awww-daemon is running, " +
                                        "and that the --namespace argument matches for the client and the daemon\"");
                return 1;
            }
            var lines = Exchange(pipe, string.Join('\t', request));
            if (lines.Count > 0 && lines[^1].StartsWith("ERR "))
            {
                Console.Error.WriteLine($"Error: \"{lines[^1][4..]}\"");
                return 1;
            }
            results.Add((ns, lines.Take(lines.Count - 1).ToList()));
        }
        if (command == "query")
        {
            if (json)
            {
                var sb = new StringBuilder("{");
                var first = true;
                foreach (var (ns, lines) in results)
                {
                    sb.Append(first ? "\n" : ",\n").Append('"').Append(ns).Append("\": [");
                    first = false;
                    var items = lines.Select(l => l.Split('\t')).Select(f =>
                        $"\n    {{\n        \"name\": \"{f[0]}\",\n        \"width\": {f[1]},\n        \"height\": {f[2]},\n        \"scale\": {f[3]},\n        \"displaying\": {{\n            \"{(f[4] == "color" ? "color" : "image")}\": \"{f[5].Replace("\\", "\\\\")}\"\n        }}\n    }}");
                    sb.Append(string.Join(",", items)).Append("\n]");
                }
                Console.WriteLine(sb.Append("\n}").ToString());
            }
            else
                foreach (var (ns, lines) in results)
                    foreach (var f in lines.Select(l => l.Split('\t')))
                        Console.WriteLine($"{ns}: {f[0]}: {f[1]}x{f[2]}, scale: {f[3]}, currently displaying: {(f[4] == "color" ? "color: " + f[5] : "image: " + f[5])}");
        }
        return 0;
    }

    static List<string> AllNamespaces()
    {
        var prefix = "awww-daemon";
        var suffix = "-" + Environment.UserName;
        var found = new List<string>();
        foreach (var pipe in Directory.GetFiles(@"\\.\pipe\"))
        {
            var name = Path.GetFileName(pipe);
            if (name.StartsWith(prefix) && name.EndsWith(suffix)) found.Add(name[prefix.Length..^suffix.Length]);
        }
        return found;
    }

    static string Hex(string color)
    {
        var c = color.TrimStart('#');
        if (c.Length is not (6 or 8) || !c.All(Uri.IsHexDigit)) throw new UsageException($"invalid value '{color}': color must be in rrggbb format");
        return c[..6].ToUpperInvariant();
    }

    static void Validate(List<string> request)
    {
        if (request[0] != "img") return;
        if (request[3] is not ("no" or "crop" or "fit" or "stretch")) throw new UsageException($"invalid value '{request[3]}' for '--resize <RESIZE>'");
        if (!Gravities.ContainsKey(request[4])) throw new UsageException($"invalid value '{request[4]}' for '--crop-gravity <CROP_GRAVITY>'");
        if (!Filters.ContainsKey(request[6].ToLowerInvariant())) throw new UsageException($"invalid value '{request[6]}' for '--filter <FILTER>'");
    }

    static List<string> Exchange(PipeStream pipe, string request)
    {
        var writer = new StreamWriter(pipe, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
        writer.WriteLine(request);
        var reader = new StreamReader(pipe, Encoding.UTF8, false, 1024, leaveOpen: true);
        var lines = new List<string>();
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            lines.Add(line);
            if (line == "OK" || line.StartsWith("ERR ")) break;
        }
        return lines;
    }

    static void ClientUsage() => Console.WriteLine("""
        An Answer to your Wayland Wallpaper Woes (on the Windows desktop)

        Change what your monitors display as a background by controlling the awww daemon at runtime.
        Supports animated gifs and putting different stuff in different monitors.

        Usage: awww <COMMAND>

        Commands:
          clear        Fills the specified outputs with the given color
          restore      Restores the last displayed image on the specified outputs
          clear-cache  Clears the awww cache
          img          Sends an image (or animated gif) for the daemon to display
          toggle       Toggles the daemon (pauses or unpauses animations)
          pause        Pauses the daemon (animations)
          unpause      Unpauses the daemon (animations)
          kill         Kills the daemon
          query        Asks the daemon to print output information (names and dimensions)
          help         Print this message or the help of the given subcommand(s)

        Options:
          -h, --help     Print help
          -V, --version  Print version
        """);

    static void CommandUsage(string command) => Console.WriteLine(command switch
    {
        "img" => """
            Sends an image (or animated gif) for the daemon to display.

            Use `-` to read from stdin

            Usage: awww img [OPTIONS] <IMAGE>

            Arguments:
              <IMAGE>  Path of image or hexcode (starting with 0x) to display

            Options:
              -a, --all                     Set the image for all awww-daemon instances (all namespaces)
              -o, --outputs <OUTPUTS>       Comma separated list of outputs to display the image at [default: all]
              -n, --namespace <NAMESPACE>   The daemon's namespace
                  --no-resize               Do not resize the image. Equivalent to `--resize=no`
                  --resize <RESIZE>         Whether to resize the image and the method by which to resize it
                                            [default: crop] [possible values: no, crop, fit, stretch]
                  --crop-gravity <GRAVITY>  Which portion of the image to anchor when cropping [default: center]
                  --fill-color <FILL_COLOR> Which color to fill the padding with (rrggbb) [default: 000000]
              -f, --filter <FILTER>         Filter to use when scaling images [default: Lanczos3]
                                            [possible values: Nearest, Bilinear, CatmullRom, Mitchell, Lanczos3]
              -t, --transition-type <TYPE>  Accepted for compatibility; Windows changes wallpapers without transitions
            """,
        "clear" => "Fills the specified outputs with the given color.\n\nUsage: awww clear [OPTIONS] [COLOR]\n\nArguments:\n  [COLOR]  Color to fill the screen with, in rrggbb format [default: 000000]\n\nOptions:\n  -a, --all\n  -n, --namespace <NAMESPACE>\n  -o, --outputs <OUTPUTS>",
        "query" => "Asks the daemon to print output information (names and dimensions).\n\nUsage: awww query [OPTIONS]\n\nOptions:\n  -a, --all\n  -n, --namespace <NAMESPACE>\n  -j, --json  Print the information in json format",
        _ => $"Usage: awww {command} [OPTIONS]\n\nOptions:\n  -a, --all\n  -n, --namespace <NAMESPACE>" + (command == "restore" ? "\n  -o, --outputs <OUTPUTS>" : ""),
    });

    // ---- daemon ----

    sealed class Output
    {
        public string Id, Name;      // IDesktopWallpaper's monitor id; "DISPLAY1"
        public int X, Y, Width, Height;
        public double Scale = 1;
        public string Kind = "color", What = "000000";
        public Animation Animation;
    }

    sealed class Animation
    {
        public List<(string File, int Delay)> Frames = new();
        public int Index;
        public DateTime Next;
    }

    static readonly object Lock = new();
    static IDesktopWallpaper wallpaper;
    static bool paused, quit, quiet, useCache = true;

    static int Daemon(string[] args)
    {
        var ns = "";
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help":
                    Console.WriteLine($"""
                        The awww wallpaper daemon for the Windows desktop

                        Usage: awww-daemon [OPTIONS]

                        Options:
                          -f, --format <FORMAT>        Accepted for compatibility (Windows takes 32-bit pixels)
                          -q, --quiet                  Don't print anything
                              --no-cache               Don't restore the last image of each output on startup
                              --layer <LAYER>          Accepted for compatibility (the desktop background)
                          -n, --namespace <NAMESPACE>  Namespace, appended to the pipe name awww-daemon<namespace>
                          -h, --help                   Print help
                          -V, --version                Print version
                        """);
                    return 0;
                case "-V" or "--version": Console.WriteLine($"awww-daemon {Version}"); return 0;
                case "-q" or "--quiet": quiet = true; break;
                case "--no-cache": useCache = false; break;
                case "-n" or "--namespace": ns = args[++i]; break;
                case "-f" or "--format" or "--layer": i++; break;
                default: throw new UsageException($"unexpected argument '{args[i]}' found");
            }
        }
        if (File.Exists($@"\\.\pipe\{PipeName(ns)}")) throw new InvalidOperationException("There is an awww-daemon instance already running on this namespace!");
        wallpaper = CreateWallpaper() ?? throw new InvalidOperationException("the desktop wallpaper service (IDesktopWallpaper) isn't available in this session");
        var outputs = Outputs();
        if (outputs.Count == 0) throw new InvalidOperationException("no monitors found");
        if (useCache) foreach (var o in outputs) Restore(o);
        Log($"initialized outputs: {string.Join(", ", outputs.Select(o => o.Name))}");

        var animator = new Thread(() => Animate(outputs)) { IsBackground = true };
        animator.Start();
        while (!quit)
        {
            using var pipe = new NamedPipeServerStream(PipeName(ns), PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.CurrentUserOnly);
            pipe.WaitForConnection();
            try
            {
                var reader = new StreamReader(pipe, Encoding.UTF8, false, 4096, leaveOpen: true);
                var writer = new StreamWriter(pipe, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
                var request = reader.ReadLine();
                if (request == null) continue;
                List<string> reply;
                try
                {
                    lock (Lock) reply = Handle(request.Split('\t'), outputs);
                    reply.Add("OK");
                }
                catch (Exception e) when (e is InvalidOperationException or IOException or COMException or UnauthorizedAccessException or ArgumentException)
                {
                    reply = new List<string> { "ERR " + e.Message.Replace('\n', ' ') };
                }
                foreach (var line in reply) writer.WriteLine(line);
            }
            catch (IOException) { }
        }
        Log("goodbye");
        return 0;
    }

    static void Log(string message)
    {
        if (!quiet) Console.Error.WriteLine($"awww-daemon: {message}");
    }

    static List<string> Handle(string[] r, List<Output> outputs)
    {
        IEnumerable<Output> Selected(string list)
        {
            if (string.IsNullOrEmpty(list)) return outputs;
            var names = list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var chosen = outputs.Where(o => names.Contains(o.Name, StringComparer.OrdinalIgnoreCase)).ToList();
            var unknown = names.Where(n => !outputs.Any(o => o.Name.Equals(n, StringComparison.OrdinalIgnoreCase))).ToList();
            if (unknown.Count > 0) throw new InvalidOperationException($"output(s) not found: {string.Join(", ", unknown)}");
            return chosen;
        }
        switch (r[0])
        {
            case "query":
                return outputs.Select(o => $"{o.Name}\t{o.Width}\t{o.Height}\t{o.Scale.ToString(System.Globalization.CultureInfo.InvariantCulture)}\t{o.Kind}\t{o.What}").ToList();
            case "kill":
                quit = true;
                return new List<string>();
            case "pause": paused = true; return new List<string>();
            case "unpause": paused = false; return new List<string>();
            case "toggle": paused = !paused; return new List<string>();
            case "clear":
                foreach (var o in Selected(r[2])) ShowColor(o, r[1]);
                return new List<string>();
            case "restore":
                foreach (var o in Selected(r[1])) Restore(o);
                return new List<string>();
            case "img":
            {
                var (image, outs, resize, gravity, fill, filter) = (r[1], r[2], r[3], r[4], r[5], r[6]);
                foreach (var o in Selected(outs))
                {
                    if (image.StartsWith("color:")) { ShowColor(o, image[6..]); continue; }
                    ShowImage(o, image, resize, gravity, ParseColor(fill), filter);
                    if (useCache) SaveCache(o, image, resize, gravity, fill, filter);
                }
                return new List<string>();
            }
        }
        throw new InvalidOperationException($"unknown request {r[0]}");
    }

    static void ShowColor(Output o, string hex)
    {
        o.Animation = null;
        var (r, g, b) = ParseColor(hex);
        var pixels = new byte[o.Width * o.Height * 4];
        for (var k = 0; k < pixels.Length; k += 4) { pixels[k] = b; pixels[k + 1] = g; pixels[k + 2] = r; pixels[k + 3] = 255; }
        Apply(o, WriteBitmap(o, pixels, "color"));
        o.Kind = "color";
        o.What = hex;
    }

    static (byte R, byte G, byte B) ParseColor(string hex) =>
        (Convert.ToByte(hex[..2], 16), Convert.ToByte(hex[2..4], 16), Convert.ToByte(hex[4..6], 16));

    static void ShowImage(Output o, string path, string resize, string gravity, (byte R, byte G, byte B) fill, string filter)
    {
        o.Animation = null;
        var frames = Render(path, o.Width, o.Height, resize, gravity, fill, filter);
        if (frames.Count == 1) Apply(o, WriteBitmap(o, frames[0].Pixels, "image"));
        else
        {
            var animation = new Animation();
            for (var k = 0; k < frames.Count; k++) animation.Frames.Add((WriteBitmap(o, frames[k].Pixels, $"frame{k}"), Math.Max(frames[k].Delay, 100)));
            Apply(o, animation.Frames[0].File);
            animation.Next = DateTime.UtcNow.AddMilliseconds(animation.Frames[0].Delay);
            o.Animation = animation;
        }
        o.Kind = "image";
        o.What = path;
    }

    static void Apply(Output o, string bitmap)
    {
        // The bitmap is the monitor's exact size; centering shows it pixel for pixel.
        wallpaper.SetPosition(0);   // DWPOS_CENTER
        wallpaper.SetWallpaper(o.Id, bitmap);
    }

    static void Animate(List<Output> outputs)
    {
        while (!quit)
        {
            Thread.Sleep(20);
            if (paused) continue;
            lock (Lock)
                foreach (var o in outputs)
                {
                    var a = o.Animation;
                    if (a == null || DateTime.UtcNow < a.Next) continue;
                    a.Index = (a.Index + 1) % a.Frames.Count;
                    try { wallpaper.SetWallpaper(o.Id, a.Frames[a.Index].File); }
                    catch (COMException) { }
                    a.Next = DateTime.UtcNow.AddMilliseconds(a.Frames[a.Index].Delay);
                }
        }
    }

    // ---- cache ----

    static string CacheFile(Output o) => Path.Combine(CacheDir, "cache", o.Name);

    static void SaveCache(Output o, string image, string resize, string gravity, string fill, string filter)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(CacheFile(o))!);
        File.WriteAllText(CacheFile(o), string.Join('\t', image, resize, gravity, fill, filter));
    }

    static void Restore(Output o)
    {
        if (!File.Exists(CacheFile(o))) return;
        var f = File.ReadAllText(CacheFile(o)).Trim().Split('\t');
        if (f.Length < 5 || !File.Exists(f[0])) return;
        try { ShowImage(o, f[0], f[1], f[2], ParseColor(f[3]), f[4]); }
        catch (Exception e) when (e is IOException or COMException or InvalidOperationException or ArgumentException) { Log($"failed to restore {o.Name}: {e.Message}"); }
    }

    // ---- rendering ----

    static readonly Dictionary<string, BitmapInterpolationMode> Filters = new()
    {
        ["nearest"] = BitmapInterpolationMode.NearestNeighbor, ["bilinear"] = BitmapInterpolationMode.Linear,
        ["catmullrom"] = BitmapInterpolationMode.Cubic, ["mitchell"] = BitmapInterpolationMode.Cubic, ["lanczos3"] = BitmapInterpolationMode.Fant,
    };

    static readonly Dictionary<string, (double X, double Y)> Gravities = new()
    {
        ["center"] = (0.5, 0.5), ["top"] = (0.5, 0), ["bottom"] = (0.5, 1), ["left"] = (0, 0.5), ["right"] = (1, 0.5),
        ["top-left"] = (0, 0), ["top-right"] = (1, 0), ["bottom-left"] = (0, 1), ["bottom-right"] = (1, 1),
    };

    static T Await<T>(Windows.Foundation.IAsyncOperation<T> op) => op.AsTask().GetAwaiter().GetResult();

    // Every frame, composited (GIF frames are patches over the previous one) and fitted to the output.
    static List<(byte[] Pixels, int Delay)> Render(string path, int width, int height, string resize, string gravity,
        (byte R, byte G, byte B) fill, string filter)
    {
        using var stream = Await(FileRandomAccessStream.OpenAsync(path, FileAccessMode.Read));
        var decoder = Await(BitmapDecoder.CreateAsync(stream));
        int imageW = (int)decoder.PixelWidth, imageH = (int)decoder.PixelHeight;
        var frames = new List<(byte[], int)>();
        var canvas = new byte[imageW * imageH * 4];
        var count = Math.Min(decoder.FrameCount, 1000u);
        for (uint f = 0; f < count; f++)
        {
            var frame = Await(decoder.GetFrameAsync(f));
            var data = Await(frame.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, new BitmapTransform(),
                ExifOrientationMode.RespectExifOrientation, ColorManagementMode.ColorManageToSRgb)).DetachPixelData();
            int left = 0, top = 0, delay = 100, disposal = 0;
            int fw = (int)frame.PixelWidth, fh = (int)frame.PixelHeight;
            if (count > 1)
            {
                try
                {
                    var props = Await(frame.BitmapProperties.GetPropertiesAsync(new[] { "/imgdesc/Left", "/imgdesc/Top", "/grctlext/Delay", "/grctlext/Disposal" }));
                    int Prop(string key) => props.TryGetValue(key, out var v) && v.Value != null ? Convert.ToInt32(v.Value) : 0;
                    left = Prop("/imgdesc/Left");
                    top = Prop("/imgdesc/Top");
                    delay = Prop("/grctlext/Delay") * 10;
                    disposal = Prop("/grctlext/Disposal");
                }
                catch (COMException) { }
            }
            var previous = disposal == 3 ? (byte[])canvas.Clone() : null;
            for (var y = 0; y < fh && top + y < imageH; y++)
                for (var x = 0; x < fw && left + x < imageW; x++)
                {
                    var s = (y * fw + x) * 4;
                    if (data[s + 3] == 0) continue;
                    var d = ((top + y) * imageW + left + x) * 4;
                    System.Buffer.BlockCopy(data, s, canvas, d, 4);
                }
            frames.Add((Fit(canvas, imageW, imageH, width, height, resize, gravity, fill, filter), delay > 0 ? delay : 100));
            if (disposal == 2)
                for (var y = 0; y < fh && top + y < imageH; y++)
                    Array.Clear(canvas, ((top + y) * imageW + left) * 4, Math.Min(fw, imageW - left) * 4);
            else if (disposal == 3 && previous != null) canvas = previous;
        }
        return frames;
    }

    // Scale (with WIC's filters) and place the image on an output-sized canvas filled with the fill color.
    static byte[] Fit(byte[] image, int iw, int ih, int ow, int oh, string resize, string gravity, (byte R, byte G, byte B) fill, string filter)
    {
        int sw = iw, sh = ih;
        switch (resize)
        {
            case "crop": { var s = Math.Max((double)ow / iw, (double)oh / ih); sw = (int)Math.Ceiling(iw * s); sh = (int)Math.Ceiling(ih * s); break; }
            case "fit": { var s = Math.Min((double)ow / iw, (double)oh / ih); sw = Math.Max(1, (int)(iw * s)); sh = Math.Max(1, (int)(ih * s)); break; }
            case "stretch": sw = ow; sh = oh; break;
        }
        var scaled = sw == iw && sh == ih ? image : Scale(image, iw, ih, sw, sh, Filters.GetValueOrDefault(filter.ToLowerInvariant(), BitmapInterpolationMode.Fant));
        var (gx, gy) = resize == "crop" ? Gravities.GetValueOrDefault(gravity, (0.5, 0.5)) : (0.5, 0.5);
        // Offset of the scaled image on the output (negative when it's larger and gets cropped).
        var ox = (int)Math.Round((ow - sw) * gx);
        var oy = (int)Math.Round((oh - sh) * gy);
        var output = new byte[ow * oh * 4];
        for (var y = 0; y < oh; y++)
            for (var x = 0; x < ow; x++)
            {
                var d = (y * ow + x) * 4;
                int sx = x - ox, sy = y - oy;
                byte b = fill.B, g = fill.G, r = fill.R;
                if (sx >= 0 && sy >= 0 && sx < sw && sy < sh)
                {
                    var s = (sy * sw + sx) * 4;
                    int a = scaled[s + 3];
                    b = (byte)((scaled[s] * a + fill.B * (255 - a)) / 255);
                    g = (byte)((scaled[s + 1] * a + fill.G * (255 - a)) / 255);
                    r = (byte)((scaled[s + 2] * a + fill.R * (255 - a)) / 255);
                }
                output[d] = b; output[d + 1] = g; output[d + 2] = r; output[d + 3] = 255;
            }
        return output;
    }

    static byte[] Scale(byte[] pixels, int w, int h, int nw, int nh, BitmapInterpolationMode mode)
    {
        using var memory = new InMemoryRandomAccessStream();
        var encoder = Await(BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, memory));
        encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, (uint)w, (uint)h, 96, 96, pixels);
        encoder.FlushAsync().AsTask().GetAwaiter().GetResult();
        memory.Seek(0);
        var decoder = Await(BitmapDecoder.CreateAsync(memory));
        var transform = new BitmapTransform { ScaledWidth = (uint)nw, ScaledHeight = (uint)nh, InterpolationMode = mode };
        return Await(decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, transform,
            ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage)).DetachPixelData();
    }

    // A top-down 32-bit BMP in the cache, which IDesktopWallpaper takes as is.
    static string WriteBitmap(Output o, byte[] pixels, string what)
    {
        var dir = Path.Combine(CacheDir, "frames", o.Name);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, what + ".bmp");
        using var file = new BinaryWriter(File.Create(path));
        file.Write((ushort)0x4D42);
        file.Write(54 + pixels.Length);
        file.Write(0);
        file.Write(54);
        file.Write(40);
        file.Write(o.Width);
        file.Write(-o.Height);
        file.Write((ushort)1);
        file.Write((ushort)32);
        file.Write(0);
        file.Write(pixels.Length);
        file.Write(2835);
        file.Write(2835);
        file.Write(0);
        file.Write(0);
        file.Write(pixels);
        return path;
    }

    // ---- monitors ----

    static List<Output> Outputs()
    {
        var list = new List<Output>();
        var monitors = new List<(string Device, RECT Rect, nint Handle)>();
        EnumDisplayMonitors(0, 0, (h, _, _, _) =>
        {
            var info = new MONITORINFOEXW { cbSize = Marshal.SizeOf<MONITORINFOEXW>() };
            if (GetMonitorInfoW(h, ref info)) monitors.Add((info.szDevice, info.rcMonitor, h));
            return true;
        }, 0);
        var count = wallpaper.GetMonitorDevicePathCount();
        for (uint k = 0; k < count; k++)
        {
            var id = wallpaper.GetMonitorDevicePathAt(k);
            RECT rect;
            try { rect = wallpaper.GetMonitorRECT(id); }
            catch (COMException) { continue; }   // a monitor that isn't attached
            var match = monitors.FirstOrDefault(m => m.Rect.left == rect.left && m.Rect.top == rect.top);
            var output = new Output
            {
                Id = id,
                Name = match.Device != null ? match.Device.TrimStart('\\', '.') : $"DISPLAY{k + 1}",
                X = rect.left, Y = rect.top, Width = rect.right - rect.left, Height = rect.bottom - rect.top,
            };
            if (match.Handle != 0 && GetDpiForMonitor(match.Handle, 0, out var dpi, out _) == 0) output.Scale = dpi / 96.0;
            list.Add(output);
        }
        return list;
    }

    // ---- COM ----

    static readonly StrategyBasedComWrappers Wrappers = new();

    static IDesktopWallpaper CreateWallpaper()
    {
        CoInitializeEx(0, 0);
        var clsid = new Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD");
        var iid = new Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B");
        if (CoCreateInstance(in clsid, 0, 0x1 | 0x4, in iid, out var ptr) != 0) return null;
        try { return (IDesktopWallpaper)Wrappers.GetOrCreateObjectForComInstance(ptr, CreateObjectFlags.UniqueInstance); }
        finally { Marshal.Release(ptr); }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT { public int left, top, right, bottom; }

    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16), Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
    internal partial interface IDesktopWallpaper
    {
        void SetWallpaper(string monitorId, string wallpaper);
        string GetWallpaper(string monitorId);
        string GetMonitorDevicePathAt(uint index);
        uint GetMonitorDevicePathCount();
        RECT GetMonitorRECT(string monitorId);
        void SetBackgroundColor(uint color);
        uint GetBackgroundColor();
        void SetPosition(int position);
        int GetPosition();
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct MONITORINFOEXW
    {
        public int cbSize;
        public RECT rcMonitor, rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string szDevice;
    }

    delegate bool MonitorEnumProc(nint monitor, nint hdc, nint rect, nint data);

    [DllImport("user32.dll")]
    static extern bool EnumDisplayMonitors(nint hdc, nint clip, MonitorEnumProc callback, nint data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern bool GetMonitorInfoW(nint monitor, ref MONITORINFOEXW info);

    [DllImport("shcore.dll")]
    static extern int GetDpiForMonitor(nint monitor, int type, out uint dpiX, out uint dpiY);

    [DllImport("ole32.dll")]
    static extern int CoInitializeEx(nint reserved, uint coinit);

    [DllImport("ole32.dll")]
    static extern int CoCreateInstance(in Guid clsid, nint outer, uint context, in Guid iid, out nint obj);
}
