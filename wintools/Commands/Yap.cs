using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace WinTools;

// yap: on-device audio transcription. On macOS yap uses Apple's Speech.framework; the Windows route
// (per the formula) is whisper.cpp. `yap transcribe <file>` and `yap <file>` run whisper.cpp's
// whisper-cli (install it with `brew install whisperkit-cli`), translating yap's flags and output
// formats. A ggml model is required: pass --model, or set YAP_MODEL / WHISPER_MODEL. Live capture of
// system or microphone audio (`yap listen`, `yap listen-and-dictate`) is not provided on Windows.
static class Yap
{
    const string Version = "1.2.1";

    [ModuleInitializer]
    internal static void Register() => Commands.Add("yap", "Transcribe audio/video files with whisper.cpp", Run);

    static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help") { Help(); return 0; }
        if (args[0] is "--version" or "-v") { Console.WriteLine(Version); return 0; }

        var command = args[0];
        var rest = args;
        if (command is "transcribe" or "listen" or "listen-and-dictate") rest = args[1..];
        else command = "transcribe";   // `yap <file>` is shorthand for `yap transcribe <file>`

        if (command is "listen" or "listen-and-dictate")
        {
            Console.Error.WriteLine("yap: live capture of system or microphone audio is not available on Windows.");
            Console.Error.WriteLine("Transcribe a recorded file instead: yap <audio-or-video-file>");
            return 1;
        }
        return Transcribe(rest);
    }

    static int Transcribe(string[] args)
    {
        string input = null, output = null, locale = null, model = Environment.GetEnvironmentVariable("YAP_MODEL") ?? Environment.GetEnvironmentVariable("WHISPER_MODEL");
        var formats = new List<string>();
        var wordTimestamps = false;
        int? maxLength = null;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Next() => i + 1 < args.Length ? args[++i] : throw new UsageException($"{a} needs a value");
            switch (a)
            {
                case "-h" or "--help": Help(); return 0;
                case "--txt" or "--srt" or "--vtt" or "--json": formats.Add(a.TrimStart('-')); break;
                case "-o" or "--output-file": output = Next(); break;
                case "-l" or "--locale": locale = Next(); break;
                case "-m" or "--model": model = Next(); break;
                case "--max-length": maxLength = int.Parse(Next()); break;
                case "--word-timestamps": wordTimestamps = true; break;
                case "--censor": break;   // accepted; whisper.cpp has no equivalent
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unknown option {a} (see yap --help)");
                    input = a;
                    break;
            }
        }
        if (input == null) throw new UsageException("no input file given (see yap --help)");
        if (!File.Exists(input)) throw new IOException($"{input}: No such file");
        if (formats.Count == 0) formats.Add("txt");

        var whisper = FindWhisper();
        if (whisper == null)
        {
            Console.Error.WriteLine("yap: whisper.cpp was not found on PATH. Install it with: brew install whisperkit-cli");
            return 1;
        }
        if (model == null)
        {
            Console.Error.WriteLine("yap: a Whisper model is required. Pass --model <ggml-model.bin> or set YAP_MODEL,");
            Console.Error.WriteLine("e.g. download https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.en.bin");
            return 1;
        }

        // Translate yap's options to whisper-cli's.
        var whisperArgs = new List<string> { "-m", model, "-f", input };
        foreach (var f in formats.Distinct())
            whisperArgs.Add(f switch { "srt" => "-osrt", "vtt" => "-ovtt", "json" => "-oj", _ => "-otxt" });
        if (output != null) whisperArgs.AddRange(["-of", Path.ChangeExtension(output, null)]);
        if (locale != null) whisperArgs.AddRange(["-l", locale.Split('-', '_')[0]]);
        if (wordTimestamps) whisperArgs.Add("-oj");
        if (maxLength is int ml) whisperArgs.AddRange(["-ml", ml.ToString()]);

        try
        {
            var psi = new ProcessStartInfo(whisper) { UseShellExecute = false };
            foreach (var a in whisperArgs) psi.ArgumentList.Add(a);
            using var p = Process.Start(psi);
            p.WaitForExit();
            return p.ExitCode;
        }
        catch (Win32Exception e) { throw new InvalidOperationException($"could not run whisper.cpp: {e.Message}"); }
    }

    // Look for whisper.cpp's CLI under its common names on PATH.
    static string FindWhisper()
    {
        string[] names = ["whisper-cli.exe", "whisperkit-cli.exe", "main.exe", "whisper-cli", "whisperkit-cli"];
        var dirs = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator);
        foreach (var dir in dirs)
        {
            if (string.IsNullOrWhiteSpace(dir)) continue;
            foreach (var name in names)
            {
                try { var full = Path.Combine(dir, name); if (File.Exists(full)) return full; }
                catch (ArgumentException) { }
            }
        }
        return null;
    }

    static void Help() => Console.WriteLine("""
        On-device audio transcription (whisper.cpp on Windows)

        USAGE: yap transcribe [--locale <locale>] <input-file> [--txt] [--srt] [--vtt] [--json]
                              [--output-file <output-file>] [--max-length <max-length>] [--word-timestamps]
                              [--model <ggml-model.bin>]
               yap <input-file> [options]          (shorthand for `yap transcribe`)

        ARGUMENTS:
          <input-file>            Audio or video file to transcribe.

        OPTIONS:
          -l, --locale <locale>   Language, e.g. en or en-US (default: auto)
          --txt/--srt/--vtt/--json
                                  Output format(s). (default: --txt)
          -o, --output-file <f>   Write the transcription to this path (prefix).
          -m, --model <file>      ggml Whisper model (or set YAP_MODEL / WHISPER_MODEL).
          --max-length <n>        Maximum segment length in characters.
          --word-timestamps       Include word-level timestamps (JSON).
          -h, --help              Show help information.

        Transcription runs whisper.cpp's whisper-cli; install it with `brew install whisperkit-cli`
        and download a ggml model. Live capture (yap listen / listen-and-dictate) is not available
        on Windows.
        """);
}
