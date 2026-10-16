using System.Runtime.CompilerServices;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage;

namespace WinTools;

// fx-upscale: video upscaling through Media Foundation's transcoder instead of MetalFX. The video
// processor scales on the GPU (Direct3D 11 video processing, high-quality MrfCrf444 mode) and
// the hardware encoder re-encodes; audio is re-encoded alongside. Output goes next to the input as
// "<name> Upscaled.mp4" (Media Foundation writes MP4, not QuickTime .mov). ProRes has no Windows
// encoder, so --codec prores is refused.
static class FxUpscale
{
    [ModuleInitializer]
    internal static void Register() => Commands.Add("fx-upscale", "Upscale videos on the GPU (Media Foundation)", Run);

    const int MaxSide = 16384;

    static int Run(string[] args)
    {
        string input = null, output = null, codec = null;
        uint width = 0, height = 0;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string Value() => i + 1 < args.Length ? args[++i] : throw new UsageException($"{arg} needs a value");
            switch (arg)
            {
                case "--help":
                    Console.WriteLine("""
                        OVERVIEW: GPU video upscaling (Media Foundation on Windows)

                        USAGE: fx-upscale <url> [--width <width>] [--height <height>] [--codec <codec>] [--output <file>]

                        ARGUMENTS:
                          <url>                   The video file to upscale

                        OPTIONS:
                          -w, --width <width>     The output file width
                          -h, --height <height>   The output file height
                          -c, --codec <codec>     Output codec: h264 or hevc (default: the input's, h264 when unknown;
                                                  hevc above 4096x2304, the H.264 limit)
                          -o, --output <file>     Output file (default: "<name> Upscaled.mp4" next to the input)
                          --version               Show the version.
                          --help                  Show help information.

                        If only one of width or height is given, the other follows the aspect ratio; with
                        neither, the video is upscaled 2x.
                        """);
                    return 0;
                case "--version": Console.WriteLine($"fx-upscale {Commands.Version} (wintools)"); return 0;
                case "-w" or "--width": width = Dimension(arg, Value()); break;
                case "-h" or "--height": height = Dimension(arg, Value()); break;
                case "-c" or "--codec":
                    codec = Value().ToLowerInvariant();
                    if (codec == "prores")
                    {
                        Console.Error.WriteLine("fx-upscale: ProRes has no encoder on Windows; use --codec hevc or h264.");
                        return 1;
                    }
                    if (codec is not ("h264" or "hevc")) throw new UsageException($"unknown codec '{codec}' (h264, hevc)");
                    break;
                case "-o" or "--output": output = Value(); break;
                default:
                    if (arg.StartsWith('-') && arg.Length > 1) throw new UsageException($"unknown option {arg}");
                    if (input != null) throw new UsageException($"unexpected argument '{arg}'");
                    input = arg;
                    break;
            }
        }
        if (input == null) throw new UsageException("missing expected argument '<url>' (see fx-upscale --help)");
        // Explicit sizes are checked before the file is even opened, like fx-upscale's validation.
        if (width > MaxSide || height > MaxSide) throw new UsageException($"Maximum supported width/height: {MaxSide}");
        var full = Path.GetFullPath(input);
        if (!File.Exists(full)) throw new IOException($"{input}: No such file");
        return Upscale(full, output != null ? Path.GetFullPath(output) : null, width, height, codec).GetAwaiter().GetResult();
    }

    static uint Dimension(string option, string value) =>
        uint.TryParse(value, out var v) && v > 0 ? v : throw new UsageException($"invalid value '{value}' for {option}");

    // Output size from the source and the requested sizes; encoders want even dimensions.
    internal static (uint W, uint H) TargetSize(uint srcW, uint srcH, uint width, uint height)
    {
        double w = width, h = height;
        if (width == 0 && height == 0) { w = srcW * 2.0; h = srcH * 2.0; }
        else if (width == 0) w = srcW * (height / (double)srcH);
        else if (height == 0) h = srcH * (width / (double)srcW);
        static uint Even(double v) => Math.Max(2, (uint)Math.Round(v / 2) * 2);
        return (Even(w), Even(h));
    }

    static async Task<int> Upscale(string input, string output, uint width, uint height, string codec)
    {
        StorageFile source;
        MediaEncodingProfile sourceProfile;
        try
        {
            source = await StorageFile.GetFileFromPathAsync(input);
            sourceProfile = await MediaEncodingProfile.CreateFromFileAsync(source);
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            Console.Error.WriteLine($"fx-upscale: {input}: not a video Windows can read (0x{e.HResult:X8})");
            return 1;
        }
        var video = sourceProfile.Video;
        if (video == null || video.Width == 0 || video.Height == 0)
        {
            Console.Error.WriteLine($"fx-upscale: {input}: no video track");
            return 1;
        }
        var (w, h) = TargetSize(video.Width, video.Height, width, height);
        if (w > MaxSide || h > MaxSide)
        {
            Console.Error.WriteLine($"fx-upscale: Maximum supported width/height: {MaxSide}");
            return 2;
        }
        codec ??= video.Subtype.Equals("HEVC", StringComparison.OrdinalIgnoreCase) || video.Subtype.Equals("H265", StringComparison.OrdinalIgnoreCase) ? "hevc" : "h264";
        // Windows' H.264 encoders stop at level 5.2 (4096x2304); larger frames go to HEVC, as
        // fx-upscale moves >4K output to ProRes.
        if (codec == "h264" && (w > 4096 || h > 4096 || (ulong)w * h > 4096UL * 2304)) codec = "hevc";

        // A fixed-quality preset, resized (VideoEncodingQuality.Auto profiles are refused by
        // PrepareFileTranscodeAsync once their size is changed).
        var profile = codec == "hevc" ? MediaEncodingProfile.CreateHevc(VideoEncodingQuality.HD1080p) : MediaEncodingProfile.CreateMp4(VideoEncodingQuality.HD1080p);
        profile.Video.Width = w;
        profile.Video.Height = h;
        if (video.FrameRate.Numerator > 0 && video.FrameRate.Denominator > 0)
        {
            profile.Video.FrameRate.Numerator = video.FrameRate.Numerator;
            profile.Video.FrameRate.Denominator = video.FrameRate.Denominator;
        }
        profile.Video.PixelAspectRatio.Numerator = 1;
        profile.Video.PixelAspectRatio.Denominator = 1;
        // The source's bitrate grown with the pixel count, but at least ~0.07 bit per output pixel per frame.
        var fps = video.FrameRate.Denominator > 0 && video.FrameRate.Numerator > 0 ? video.FrameRate.Numerator / (double)video.FrameRate.Denominator : 30;
        var bitrate = Math.Max(video.Bitrate * (w * (double)h) / (video.Width * (double)video.Height), w * (double)h * fps * 0.07);
        profile.Video.Bitrate = (uint)Math.Clamp(bitrate * (codec == "hevc" ? 0.7 : 1), 1_000_000, 400_000_000);
        if (sourceProfile.Audio == null) profile.Audio = null;
        else
        {
            if (sourceProfile.Audio.SampleRate is 44100 or 48000) profile.Audio.SampleRate = sourceProfile.Audio.SampleRate;
            if (sourceProfile.Audio.ChannelCount is 1 or 2 or 6) profile.Audio.ChannelCount = sourceProfile.Audio.ChannelCount;
        }

        output ??= Path.Combine(Path.GetDirectoryName(input), Path.GetFileNameWithoutExtension(input) + " Upscaled.mp4");
        if (string.Equals(output, input, StringComparison.OrdinalIgnoreCase)) throw new UsageException("the output would overwrite the input");
        var folder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(output));
        var destination = await folder.CreateFileAsync(Path.GetFileName(output), CreationCollisionOption.ReplaceExisting);

        var transcoder = new MediaTranscoder
        {
            HardwareAccelerationEnabled = true,
            VideoProcessingAlgorithm = MediaVideoProcessingAlgorithm.MrfCrf444,
        };
        Console.Error.WriteLine($"Upscaling from {video.Width}x{video.Height} to {w}x{h} ({codec})");
        var prepared = await transcoder.PrepareFileTranscodeAsync(source, destination, profile);
        if (!prepared.CanTranscode)
        {
            await destination.DeleteAsync();
            Console.Error.WriteLine($"fx-upscale: can't transcode {Path.GetFileName(input)} ({prepared.FailureReason})");
            return 1;
        }
        var interactive = !Console.IsErrorRedirected;
        var operation = prepared.TranscodeAsync();
        var last = -1;
        operation.Progress = (_, percent) =>
        {
            var p = (int)percent;
            if (p == last) return;
            last = p;
            if (interactive) Console.Error.Write($"\r{p,3}%");
        };
        try { await operation; }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            if (interactive) Console.Error.WriteLine();
            try { await destination.DeleteAsync(); } catch (Exception) { }
            Console.Error.WriteLine($"fx-upscale: transcoding failed (0x{e.HResult:X8})");
            return 1;
        }
        if (interactive) Console.Error.WriteLine("\r100%");
        Console.WriteLine(output);
        return 0;
    }
}
