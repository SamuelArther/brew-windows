using System.Globalization;
using System.Runtime.CompilerServices;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;

namespace WinTools;

// imagesnap: capture a still image from a webcam with WinRT MediaCapture (the Camera app's API), or
// list video devices. Images are JPEG, PNG or BMP straight from the camera pipeline; TIFF and GIF are
// re-encoded with Windows Imaging Component.
static class Imagesnap
{
    [ModuleInitializer]
    internal static void Register() => Commands.Add("imagesnap", "Capture still images from a webcam or other video source", a => MediaControl.WinRT(() => Run(a), "the camera"));

    static int Run(string[] args)
    {
        bool list = false, quiet = false, verbose = false;
        string device = null, file = null;
        double warmup = 0, interval = 0;
        int limit = 0;
        for (var i = 0; i < args.Length; i++)
        {
            string Value() => ++i < args.Length ? args[i] : throw new UsageException($"{args[i - 1]} needs a value");
            double Seconds() => double.TryParse(Value(), NumberStyles.Float, CultureInfo.InvariantCulture, out var s) && s >= 0 ? s : throw new UsageException($"{args[i - 1]} needs seconds, e.g. 1.5");
            switch (args[i])
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        USAGE: imagesnap [options] [filename]
                        Captures an image from a video device and saves it in a file.
                        If no device is specified, the system default will be used.
                        If no filename is specified, snapshot.jpg will be used.
                        Supported image types: JPEG, PNG, BMP, TIFF, GIF (from the file extension)
                          -h          This help message
                          -v          Verbose mode
                          -l          List available video devices
                          -t x.xx     Take a picture every x.xx seconds (files named <name>-00001.jpg, ...)
                          -n num      With -t, stop after num pictures
                          -q          Quiet mode. Do not output any text
                          -w x.xx     Warmup. Delay snapshot x.xx seconds after turning on camera
                          -d device   Use the named video device
                        """);
                    return 0;
                case "-l": list = true; break;
                case "-q": quiet = true; break;
                case "-v": verbose = true; break;
                case "-d": device = Value(); break;
                case "-w": warmup = Seconds(); break;
                case "-t": interval = Seconds(); break;
                case "-n": limit = int.TryParse(Value(), out var n) && n > 0 ? n : throw new UsageException("-n needs a positive number"); break;
                default:
                    if (args[i].StartsWith('-')) throw new UsageException($"unknown option {args[i]}");
                    if (file != null) throw new UsageException("only one filename may be given");
                    file = args[i];
                    break;
            }
        }
        List<DeviceInformation> devices;
        try { devices = DeviceInformation.FindAllAsync(DeviceClass.VideoCapture).AsTask().GetAwaiter().GetResult().ToList(); }
        catch (Exception) { devices = []; }   // no camera stack (server editions, service sessions)
        if (list)
        {
            Console.WriteLine("Video Devices:");
            foreach (var d in devices) Console.WriteLine($"=> {d.Name}");
            if (devices.Count == 0) Console.WriteLine("(none)");
            return 0;
        }
        if (devices.Count == 0) { Console.Error.WriteLine("No video devices found."); return 1; }
        var chosen = device == null
            ? devices.FirstOrDefault(d => d.IsDefault) ?? devices[0]
            : devices.FirstOrDefault(d => d.Name == device) ?? devices.FirstOrDefault(d => d.Name.Contains(device, StringComparison.OrdinalIgnoreCase));
        if (chosen == null) { Console.Error.WriteLine($"Device \"{device}\" not found."); return 1; }

        file ??= "snapshot.jpg";
        var extension = Path.GetExtension(file).ToLowerInvariant();
        if (extension == "") { extension = ".jpg"; file += extension; }
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".bmp" or ".tif" or ".tiff" or ".gif"))
            throw new UsageException($"unsupported image type '{extension}' (jpg, png, bmp, tiff, gif)");

        using var capture = new MediaCapture();
        try
        {
            capture.InitializeAsync(new MediaCaptureInitializationSettings
            {
                VideoDeviceId = chosen.Id,
                StreamingCaptureMode = StreamingCaptureMode.Video,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu,
            }).AsTask().GetAwaiter().GetResult();
        }
        catch (UnauthorizedAccessException)
        {
            throw new InvalidOperationException("camera access is turned off for desktop apps (Settings > Privacy & security > Camera)");
        }
        if (verbose) Console.WriteLine($"Initialized \"{chosen.Name}\" ({chosen.Id})");
        if (warmup > 0) Thread.Sleep(TimeSpan.FromSeconds(warmup));
        for (var count = 1; ; count++)
        {
            var target = interval > 0 ? Path.Combine(Path.GetDirectoryName(file) ?? "", $"{Path.GetFileNameWithoutExtension(file)}-{count:00000}{extension}") : file;
            if (!quiet) Console.Write($"Capturing image from device \"{chosen.Name}\"...");
            Capture(capture, target, extension);
            if (!quiet) Console.WriteLine(target);
            if (interval <= 0 || (limit > 0 && count >= limit)) break;
            Thread.Sleep(TimeSpan.FromSeconds(interval));
        }
        return 0;
    }

    static void Capture(MediaCapture capture, string target, string extension)
    {
        using var stream = new InMemoryRandomAccessStream();
        var direct = extension switch
        {
            ".jpg" or ".jpeg" => ImageEncodingProperties.CreateJpeg(),
            ".png" => ImageEncodingProperties.CreatePng(),
            _ => ImageEncodingProperties.CreateBmp(),
        };
        capture.CapturePhotoToStreamAsync(direct, stream).AsTask().GetAwaiter().GetResult();
        stream.Seek(0);
        IRandomAccessStream result = stream;
        InMemoryRandomAccessStream converted = null;
        if (extension is ".tif" or ".tiff" or ".gif")
        {
            var decoder = BitmapDecoder.CreateAsync(stream).AsTask().GetAwaiter().GetResult();
            var bitmap = decoder.GetSoftwareBitmapAsync().AsTask().GetAwaiter().GetResult();
            converted = new InMemoryRandomAccessStream();
            var encoder = BitmapEncoder.CreateAsync(extension == ".gif" ? BitmapEncoder.GifEncoderId : BitmapEncoder.TiffEncoderId, converted).AsTask().GetAwaiter().GetResult();
            encoder.SetSoftwareBitmap(bitmap);
            encoder.FlushAsync().AsTask().GetAwaiter().GetResult();
            converted.Seek(0);
            result = converted;
        }
        using (var input = result.AsStreamForRead())
        using (var output = File.Create(target))
            input.CopyTo(output);
        converted?.Dispose();
    }
}
