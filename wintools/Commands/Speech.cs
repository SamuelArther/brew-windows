using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Windows.Media.SpeechSynthesis;

namespace WinTools;

// speech / speech-server: on-device speech toolkit. On macOS it runs MLX/Core ML models; on Windows
// speech recognition is whisper.cpp's whisper-cli (as yap does), text-to-speech is Windows' own
// synthesizer (Windows.Media.SpeechSynthesis, written to WAV), voice activity detection is an
// energy detector with hysteresis, and voice-chat chains them through an OpenAI-compatible chat
// server (rapid-mlx serve). speech-server exposes /health, /v1/models, /v1/audio/speech,
// /v1/audio/transcriptions, /speak and /transcribe. Speaker diarization and the other
// model-specific commands have no Windows engine and say so.
static class Speech
{
    const string Version = "0.0.28";

    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("speech", "On-device speech toolkit: transcribe, speak, VAD, voice chat", a => MediaControl.WinRT(() => Run(a), "Windows speech"));
        Commands.Add("speech-server", "HTTP server for speech recognition and synthesis (OpenAI audio API)", a => MediaControl.WinRT(() => Server(a), "Windows speech"));
    }

    // Commands of the macOS toolkit that need its own neural models (no Windows engine).
    static readonly string[] ModelOnly =
    [
        "transcribe-batch", "align", "csm", "respond", "translate-audio", "vad-stream", "turn", "diarize", "embed-speaker",
        "language-id", "denoise", "restore", "separate", "compose", "sa3-parity", "upsample", "kokoro", "qwen3-tts-coreml",
        "vibevoice", "vibevoice-encode", "translate", "wake", "avatar-motion",
    ];

    static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help") { Help(); return 0; }
        if (args[0] == "--version") { Console.WriteLine(Version); return 0; }
        var rest = args[1..];
        switch (args[0])
        {
            case "transcribe": return Transcribe(rest);
            case "speak": return Speak(rest);
            case "vad": return Vad(rest);
            case "voice-chat": return VoiceChat(rest);
            default:
                if (ModelOnly.Contains(args[0]))
                {
                    Console.Error.WriteLine($"Error: `speech {args[0]}` needs the toolkit's MLX/Core ML models, which only run on Apple Silicon.");
                    return 1;
                }
                throw new UsageException($"unknown subcommand '{args[0]}' (see speech --help)");
        }
    }

    static bool WantsHelp(string[] args, string text)
    {
        if (!args.Any(a => a is "-h" or "--help")) return false;
        Console.WriteLine(text);
        return true;
    }

    // ---- transcribe ---------------------------------------------------------------------------------

    const string TranscribeHelp = """
        OVERVIEW: Transcribe speech to text (whisper.cpp on Windows)

        USAGE: speech transcribe <audio-file> [--engine whisper] [--model <ggml-model.bin>] [--language <language>]

        ARGUMENTS:
          <audio-file>            Audio file to transcribe (WAV, any sample rate)

        OPTIONS:
          --engine <engine>       ASR engine: whisper (the only one on Windows)
          -m, --model <model>     ggml Whisper model file (or set SPEECH_MODEL / WHISPER_MODEL)
          --language <language>  Language hint (optional)
          -h, --help              Show help information.
        """;

    static int Transcribe(string[] args)
    {
        if (WantsHelp(args, TranscribeHelp)) return 0;
        string file = null, model = null, language = null;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Next() => i + 1 < args.Length ? args[++i] : throw new UsageException($"{a} needs a value");
            switch (a)
            {
                case "--engine":
                    var engine = Next();
                    if (engine != "whisper") { Console.Error.WriteLine($"Error: ASR engine '{engine}' is not available on Windows; use --engine whisper"); return 1; }
                    break;
                case "-m" or "--model": model = Next(); break;
                case "--language": language = Next(); break;
                case "--context" or "--max-segment" : Next(); break;
                case "--stream" or "--partial": break;
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unknown option {a}");
                    file = a;
                    break;
            }
        }
        if (file == null) throw new UsageException("missing <audio-file>");
        if (!File.Exists(file)) { Console.Error.WriteLine($"Error: audio file not found: {file}"); return 1; }
        Console.WriteLine($"Loading audio: {file}");
        var audio = SpeechWav.Read(File.ReadAllBytes(file));
        Console.WriteLine($"  Loaded {audio.Samples.Length} samples ({(audio.Duration).ToString("0.00", CultureInfo.InvariantCulture)}s)");
        var sw = Stopwatch.StartNew();
        var text = Whisper(file, model, language, out var error);
        if (text == null) { Console.Error.WriteLine($"Error: {error}"); return 1; }
        Console.WriteLine($"Result: {text}");
        var elapsed = sw.Elapsed.TotalSeconds;
        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "  Time: {0:0.00}s, RTF: {1:0.000}", elapsed, audio.Duration > 0 ? elapsed / audio.Duration : 0));
        return 0;
    }

    static string ModelFromEnvironment() =>
        Environment.GetEnvironmentVariable("SPEECH_MODEL") is { Length: > 0 } m ? m : Environment.GetEnvironmentVariable("WHISPER_MODEL");

    // whisper-cli -nt -np prints just the text.
    static string Whisper(string file, string model, string language, out string error)
    {
        error = null;
        model ??= ModelFromEnvironment();
        var whisper = FindTool("whisper-cli", "whisperkit-cli");
        if (whisper == null) { error = "whisper.cpp was not found; install it with: brew install whisperkit-cli"; return null; }
        if (model == null || !File.Exists(model))
        {
            error = "a ggml Whisper model is required: pass --model or set SPEECH_MODEL (https://huggingface.co/ggerganov/whisper.cpp)";
            return null;
        }
        var psi = new ProcessStartInfo(whisper) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var a in new[] { "-m", model, "-f", file, "-nt", "-np" }) psi.ArgumentList.Add(a);
        if (language != null) { psi.ArgumentList.Add("-l"); psi.ArgumentList.Add(language.Split('-', '_')[0]); }
        try
        {
            using var p = Process.Start(psi);
            var stderr = p.StandardError.ReadToEndAsync();
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode != 0) { error = $"whisper.cpp failed: {stderr.Result.Trim().Split('\n').LastOrDefault()}"; return null; }
            return string.Join(" ", output.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
        }
        catch (Win32Exception e) { error = $"could not run whisper.cpp: {e.Message}"; return null; }
    }

    static string FindTool(params string[] names)
    {
        var dirs = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator);
        foreach (var dir in dirs.Where(d => !string.IsNullOrWhiteSpace(d)))
            foreach (var name in names)
                foreach (var ext in new[] { ".exe", ".cmd" })
                {
                    try { var full = Path.Combine(dir.Trim('"'), name + ext); if (File.Exists(full)) return full; }
                    catch (ArgumentException) { }
                }
        return null;
    }

    // ---- speak --------------------------------------------------------------------------------------

    const string SpeakHelp = """
        OVERVIEW: Text-to-speech synthesis (Windows speech synthesizer)

        USAGE: speech speak [<text>] [--output <output>] [--voice <voice>] [--language <language>] [--play]
                            [--list-speakers] [--batch-file <batch-file>]

        OPTIONS:
          -o, --output <output>   Output WAV file path (default: output.wav)
          --voice <voice>         Voice name, or part of it (see --list-speakers)
          --language <language>   Language (english, german, ... or a tag like en-US)
          --play                  Play the audio through the default output device
          --list-speakers         List installed voices and exit
          --batch-file <file>     One text per line; writes <output>_1.wav, <output>_2.wav, ...
          -h, --help              Show help information.
        """;

    static int Speak(string[] args)
    {
        if (WantsHelp(args, SpeakHelp)) return 0;
        string text = null, output = "output.wav", voice = null, language = null, batch = null;
        bool play = false, list = false;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Next() => i + 1 < args.Length ? args[++i] : throw new UsageException($"{a} needs a value");
            switch (a)
            {
                case "-o" or "--output": output = Next(); break;
                case "--voice" or "--speaker": voice = Next(); break;
                case "--language": language = Next(); break;
                case "--engine":
                    var engine = Next();
                    if (engine is not ("windows" or "sapi")) Console.Error.WriteLine($"note: TTS engine '{engine}' is not available on Windows; using the Windows synthesizer");
                    break;
                case "--batch-file": batch = Next(); break;
                case "--play": play = true; break;
                case "--list-speakers": list = true; break;
                case "--stream": break;
                default:
                    if (a.StartsWith("--")) { if (i + 1 < args.Length && !args[i + 1].StartsWith('-')) i++; break; }   // model-specific tuning flags
                    if (a.StartsWith('-')) throw new UsageException($"unknown option {a}");
                    text = a;
                    break;
            }
        }
        if (list)
        {
            foreach (var v in SpeechSynthesizer.AllVoices)
                Console.WriteLine($"{v.DisplayName,-40}{v.Language,-8}{v.Gender}");
            return 0;
        }
        var texts = batch != null ? File.ReadAllLines(batch).Where(l => l.Trim().Length > 0).ToList()
            : text != null ? [text] : throw new UsageException("missing <text> (or --batch-file / --list-speakers)");
        for (var n = 0; n < texts.Count; n++)
        {
            var path = texts.Count == 1 ? output : $"{Path.ChangeExtension(output, null)}_{n + 1}.wav";
            var wav = Synthesize(texts[n], voice, language);
            File.WriteAllBytes(path, wav);
            var seconds = SpeechWav.Read(wav).Duration;
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Saved {0} ({1:0.00}s)", path, seconds));
            if (play) PlaySound(Path.GetFullPath(path), IntPtr.Zero, 0x20000 | 0x2);   // SND_FILENAME | SND_NODEFAULT, synchronous
        }
        return 0;
    }

    static readonly Dictionary<string, string> LanguageTags = new(StringComparer.OrdinalIgnoreCase)
    {
        ["english"] = "en", ["german"] = "de", ["french"] = "fr", ["spanish"] = "es", ["italian"] = "it", ["portuguese"] = "pt",
        ["japanese"] = "ja", ["chinese"] = "zh", ["korean"] = "ko", ["russian"] = "ru", ["hindi"] = "hi", ["dutch"] = "nl",
    };

    // WAV bytes from Windows' speech synthesizer.
    internal static byte[] Synthesize(string text, string voice, string language)
    {
        using var synth = new SpeechSynthesizer();
        var voices = SpeechSynthesizer.AllVoices;
        if (voices.Count == 0) throw new InvalidOperationException("no text-to-speech voices are installed (Settings > Time & language > Speech)");
        VoiceInformation pick = null;
        if (voice != null)
            pick = voices.FirstOrDefault(v => v.DisplayName.Contains(voice, StringComparison.OrdinalIgnoreCase) || v.Id.Contains(voice, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"no installed voice matches '{voice}' (see speech speak --list-speakers)");
        else if (language != null)
        {
            var tag = LanguageTags.GetValueOrDefault(language, language);
            pick = voices.FirstOrDefault(v => v.Language.StartsWith(tag, StringComparison.OrdinalIgnoreCase));
        }
        if (pick != null) synth.Voice = pick;
        using var stream = synth.SynthesizeTextToStreamAsync(text).AsTask().GetAwaiter().GetResult();
        using var s = stream.AsStreamForRead();
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        return ms.ToArray();
    }

    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    static extern bool PlaySound(string sound, IntPtr module, uint flags);

    // ---- vad ----------------------------------------------------------------------------------------

    const string VadHelp = """
        OVERVIEW: Detect speech segments in an audio file (energy-based VAD on Windows)

        USAGE: speech vad <audio-file> [--onset <onset>] [--offset <offset>] [--min-speech-duration <s>]
                          [--min-silence-duration <s>] [--json]

        OPTIONS:
          --onset <onset>                 Onset threshold, 0-1 (speech start, default 0.5)
          --offset <offset>               Offset threshold, 0-1 (speech end, default 0.35)
          --min-speech-duration <s>       Minimum speech duration in seconds (default 0.25)
          --min-silence-duration <s>      Minimum silence duration in seconds (default 0.1)
          --json                          Output as JSON
          -h, --help                      Show help information.
        """;

    static int Vad(string[] args)
    {
        if (WantsHelp(args, VadHelp)) return 0;
        string file = null;
        double onset = 0.5, offset = 0.35, minSpeech = 0.25, minSilence = 0.1;
        var json = false;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            double Num() => i + 1 < args.Length && double.TryParse(args[++i], NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : throw new UsageException($"{a} needs a number");
            switch (a)
            {
                case "--onset": onset = Num(); break;
                case "--offset": offset = Num(); break;
                case "--min-speech-duration": minSpeech = Num(); break;
                case "--min-silence-duration": minSilence = Num(); break;
                case "--json": json = true; break;
                case "--engine" or "-m" or "--model" or "--threshold" or "--smooth-window": i++; break;
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unknown option {a}");
                    file = a;
                    break;
            }
        }
        if (file == null) throw new UsageException("missing <audio-file>");
        if (!File.Exists(file)) { Console.Error.WriteLine($"Error: audio file not found: {file}"); return 1; }
        var audio = SpeechWav.Read(File.ReadAllBytes(file));
        var sw = Stopwatch.StartNew();
        var segments = DetectSpeech(audio, onset, offset, minSpeech, minSilence);
        if (json)
        {
            Console.WriteLine("[" + string.Join(",", segments.Select(s => string.Format(CultureInfo.InvariantCulture,
                "{{\"start\":{0:0.###},\"end\":{1:0.###},\"duration\":{2:0.###}}}", s.Start, s.End, s.End - s.Start))) + "]");
            return 0;
        }
        Console.WriteLine($"Loading audio: {file}");
        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "  Loaded {0} samples ({1:0.00}s)", audio.Samples.Length, audio.Duration));
        Console.WriteLine("Detecting speech segments...");
        if (segments.Count == 0) Console.WriteLine("No speech detected.");
        else
        {
            foreach (var (s, e) in segments)
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "[{0:0.00}s - {1:0.00}s] ({2:0.00}s)", s, e, e - s));
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "\n{0} segment(s), {1:0.00}s total speech", segments.Count, segments.Sum(x => x.End - x.Start)));
        }
        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Detection took {0:0.00}s", sw.Elapsed.TotalSeconds));
        return 0;
    }

    // 30 ms frames; each frame's level above the recording's noise floor (10th percentile) is mapped to
    // a 0-1 speech probability (0.5 at 12 dB above the floor); onset/offset give hysteresis, then short
    // gaps are bridged and short segments dropped.
    internal static List<(double Start, double End)> DetectSpeech(SpeechWav audio, double onset, double offset, double minSpeech, double minSilence)
    {
        var frame = Math.Max(1, audio.SampleRate * 30 / 1000);
        var count = audio.Samples.Length / frame;
        var segments = new List<(double Start, double End)>();
        if (count == 0) return segments;
        var db = new double[count];
        for (var f = 0; f < count; f++)
        {
            double sum = 0;
            for (var i = f * frame; i < (f + 1) * frame; i++) sum += audio.Samples[i] * (double)audio.Samples[i];
            db[f] = 10 * Math.Log10(sum / frame + 1e-10);
        }
        var sorted = db.Order().ToArray();
        var floor = Math.Max(sorted[count / 10], -70);   // digital silence would make everything "speech"
        var frameSeconds = frame / (double)audio.SampleRate;
        var inSpeech = false;
        var start = 0;
        for (var f = 0; f <= count; f++)
        {
            var p = f < count ? 1 / (1 + Math.Exp(-(db[f] - floor - 12) / 2)) : 0;
            if (!inSpeech && p >= onset) { inSpeech = true; start = f; }
            else if (inSpeech && p < offset) { inSpeech = false; segments.Add((start * frameSeconds, f * frameSeconds)); }
        }
        var merged = new List<(double Start, double End)>();
        foreach (var s in segments)
        {
            if (merged.Count > 0 && s.Start - merged[^1].End < minSilence) merged[^1] = (merged[^1].Start, s.End);
            else merged.Add(s);
        }
        return merged.Where(s => s.End - s.Start >= minSpeech).ToList();
    }

    // ---- voice-chat ---------------------------------------------------------------------------------

    const string VoiceChatHelp = """
        OVERVIEW: Talk with a local language model: speech in, speech out.

        USAGE: speech voice-chat --input <audio-file> [--model <model>] [--endpoint <url>] [--output <wav>]
                                 [--system-prompt <prompt>] [--asr-model <ggml-model.bin>] [--voice <voice>]

        OPTIONS:
          -m, --model <model>       Chat model name on the server (default: the server's first model)
          --endpoint <url>          OpenAI-compatible server (default: http://127.0.0.1:8000/v1, rapid-mlx serve)
          --input <audio-file>      Recorded question (WAV); live microphone capture is not available on Windows
          -o, --output <wav>        Spoken reply (default: voice-chat-reply.wav)
          --system-prompt <prompt>  Override the system prompt
          --asr-model <file>        ggml Whisper model for recognition (or SPEECH_MODEL)
          --voice <voice>           Windows voice for the reply
          --play                    Play the reply
          -h, --help                Show help information.

        Recognition runs whisper.cpp, the reply comes from the chat server (e.g. `rapid-mlx serve
        qwen3.5-4b-4bit`), and Windows' synthesizer speaks it.
        """;

    static int VoiceChat(string[] args)
    {
        if (WantsHelp(args, VoiceChatHelp)) return 0;
        string input = null, model = null, endpoint = "http://127.0.0.1:8000/v1", output = "voice-chat-reply.wav", system = null, asrModel = null, voice = null;
        var play = false;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Next() => i + 1 < args.Length ? args[++i] : throw new UsageException($"{a} needs a value");
            switch (a)
            {
                case "--input" or "-i": input = Next(); break;
                case "-m" or "--model": model = Next(); break;
                case "--endpoint": endpoint = Next().TrimEnd('/'); break;
                case "-o" or "--output": output = Next(); break;
                case "--system-prompt": system = Next(); break;
                case "--asr-model": asrModel = Next(); break;
                case "--voice": voice = Next(); break;
                case "--play": play = true; break;
                default: throw new UsageException($"unknown option {a}");
            }
        }
        if (input == null)
        {
            Console.Error.WriteLine("Error: live microphone capture is not available on Windows; pass a recorded question with --input <wav>");
            return 1;
        }
        if (!File.Exists(input)) { Console.Error.WriteLine($"Error: audio file not found: {input}"); return 1; }
        var question = Whisper(input, asrModel, null, out var error);
        if (question == null) { Console.Error.WriteLine($"Error: {error}"); return 1; }
        Console.WriteLine($"You: {question}");

        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        if (model == null)
        {
            try
            {
                using var models = JsonDocument.Parse(http.GetStringAsync($"{endpoint}/models").GetAwaiter().GetResult());
                model = models.RootElement.GetProperty("data")[0].GetProperty("id").GetString();
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or IndexOutOfRangeException or InvalidOperationException)
            {
                Console.Error.WriteLine($"Error: no chat server at {endpoint} (start one with: rapid-mlx serve qwen3.5-4b-4bit)");
                return 1;
            }
        }
        var body = $"{{\"model\":{MediaControl.JsonString(model)},\"messages\":[{{\"role\":\"system\",\"content\":{MediaControl.JsonString(system ?? "You are a helpful voice assistant. Answer briefly, in plain spoken sentences.")}}},{{\"role\":\"user\",\"content\":{MediaControl.JsonString(question)}}}]}}";
        string reply;
        try
        {
            using var response = http.PostAsync($"{endpoint}/chat/completions", new StringContent(body, Encoding.UTF8, "application/json")).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            reply = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()?.Trim() ?? "";
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            Console.Error.WriteLine($"Error: chat request to {endpoint} failed: {e.Message}");
            return 1;
        }
        Console.WriteLine($"Assistant: {reply}");
        File.WriteAllBytes(output, Synthesize(reply, voice, null));
        Console.WriteLine($"Saved {output}");
        if (play) PlaySound(Path.GetFullPath(output), IntPtr.Zero, 0x20000 | 0x2);
        return 0;
    }

    static void Help() => Console.WriteLine($"""
        OVERVIEW: On-device speech toolkit (Windows: whisper.cpp + Windows speech)

        USAGE: speech <subcommand>

        SUBCOMMANDS:
          transcribe              Transcribe speech to text (whisper.cpp)
          speak                   Text-to-speech synthesis to WAV (Windows voices)
          vad                     Detect speech segments in an audio file
          voice-chat              Spoken question -> local chat model -> spoken reply

          See 'speech help <subcommand>' for detailed help. The HTTP server is `speech-server`.
          Model-specific commands ({string.Join(", ", ModelOnly.Take(6))}, ...) need the Apple Silicon models.
        """);

    // ---- speech-server ------------------------------------------------------------------------------

    static int Server(string[] args)
    {
        string host = "127.0.0.1", model = null;
        var port = 8080;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Next() => i + 1 < args.Length ? args[++i] : throw new UsageException($"{a} needs a value");
            switch (a)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        OVERVIEW: HTTP speech server (OpenAI-compatible audio API)

                        USAGE: speech-server [--host <host>] [--port <port>] [--asr-model <ggml-model.bin>] [--preload]

                        OPTIONS:
                          --host <host>           Host to bind (default: 127.0.0.1)
                          --port <port>           Port to bind (default: 8080)
                          --asr-model <file>      ggml Whisper model for transcription (or SPEECH_MODEL)
                          --preload               Accepted for compatibility (nothing to preload)

                        ROUTES: GET /health, GET /v1/models, POST /v1/audio/speech, POST /v1/audio/transcriptions,
                                POST /speak, POST /transcribe
                        """);
                    return 0;
                case "--version": Console.WriteLine(Version); return 0;
                case "--host": host = Next(); break;
                case "--port": port = int.TryParse(Next(), out var p) ? p : throw new UsageException("--port needs a number"); break;
                case "--asr-model": model = Next(); break;
                case "--preload": break;
                default: throw new UsageException($"unknown option {a}");
            }
        }
        var address = host == "localhost" ? IPAddress.Loopback : IPAddress.Parse(host);
        var listener = new TcpListener(address, port);
        listener.Start();
        Console.WriteLine($"speech-server listening on http://{host}:{port}");
        while (true)
        {
            var client = listener.AcceptTcpClient();
            new Thread(() =>
            {
                using (client)
                {
                    try { Handle(client.GetStream(), model); }
                    catch (Exception e) when (e is IOException or SocketException or JsonException or InvalidOperationException or FormatException) { }
                }
            }) { IsBackground = true }.Start();
        }
    }

    sealed class SpRequest
    {
        public string Method, Path, ContentType;
        public byte[] Body;
    }

    static SpRequest ReadRequest(Stream s)
    {
        var head = new List<byte>();
        while (head.Count < 65536)
        {
            var b = s.ReadByte();
            if (b < 0) return null;
            head.Add((byte)b);
            if (head.Count >= 4 && head[^1] == '\n' && head[^2] == '\r' && head[^3] == '\n' && head[^4] == '\r') break;
        }
        var lines = Encoding.ASCII.GetString(head.ToArray()).Split("\r\n");
        var first = lines[0].Split(' ');
        if (first.Length < 2) return null;
        var request = new SpRequest { Method = first[0], Path = first[1].Split('?')[0] };
        var length = 0;
        foreach (var line in lines.Skip(1))
        {
            var colon = line.IndexOf(':');
            if (colon <= 0) continue;
            var name = line[..colon].Trim();
            var value = line[(colon + 1)..].Trim();
            if (name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) length = int.Parse(value);
            if (name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)) request.ContentType = value;
        }
        if (length > 100 << 20) throw new IOException("request too large");
        request.Body = new byte[length];
        s.ReadExactly(request.Body);
        return request;
    }

    static void Respond(Stream s, int status, string contentType, byte[] body)
    {
        var reason = status switch { 200 => "OK", 400 => "Bad Request", 404 => "Not Found", 500 => "Internal Server Error", _ => "Error" };
        s.Write(Encoding.ASCII.GetBytes($"HTTP/1.1 {status} {reason}\r\nContent-Type: {contentType}\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n"));
        s.Write(body);
    }

    static void Json(Stream s, int status, string json) => Respond(s, status, "application/json", Encoding.UTF8.GetBytes(json));
    static void Error(Stream s, int status, string message) => Json(s, status, $"{{\"error\":{{\"message\":{MediaControl.JsonString(message)}}}}}");

    static void Handle(Stream s, string asrModel)
    {
        var r = ReadRequest(s);
        if (r == null) return;
        switch ((r.Method, r.Path))
        {
            case ("GET", "/health"):
                Json(s, 200, "{\"status\":\"ok\"}");
                return;
            case ("GET", "/v1/models" or "/v1/realtime/models"):
            {
                var rows = new List<string>();
                if (FindTool("whisper-cli", "whisperkit-cli") != null)
                    rows.Add("{\"id\":\"whisper\",\"object\":\"model\",\"engine\":\"whisper.cpp\",\"kind\":\"asr\",\"model_id\":" + MediaControl.JsonString(asrModel ?? ModelFromEnvironment() ?? "") + ",\"aliases\":[\"whisper-1\"]}");
                foreach (var v in SpeechSynthesizer.AllVoices)
                    rows.Add($"{{\"id\":{MediaControl.JsonString(v.DisplayName)},\"object\":\"model\",\"engine\":\"windows\",\"kind\":\"tts\",\"model_id\":{MediaControl.JsonString(v.Id)},\"aliases\":[]}}");
                Json(s, 200, "{\"object\":\"list\",\"data\":[" + string.Join(",", rows) + "]}");
                return;
            }
            case ("POST", "/v1/audio/speech" or "/speak"):
            {
                using var doc = JsonDocument.Parse(r.Body);
                var root = doc.RootElement;
                string Str(string name) => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
                var text = Str("input") ?? Str("text");
                if (string.IsNullOrWhiteSpace(text)) { Error(s, 400, r.Path == "/speak" ? "Missing 'text' field" : "Missing required field: input"); return; }
                var format = Str("response_format") ?? "wav";
                if (format is not ("wav" or "pcm")) { Error(s, 400, $"response_format '{format}' is not supported (wav or pcm)"); return; }
                var voice = Str("voice");
                // OpenAI voice names (alloy, nova, ...) don't exist on Windows: use the default voice.
                if (voice != null && !SpeechSynthesizer.AllVoices.Any(v => v.DisplayName.Contains(voice, StringComparison.OrdinalIgnoreCase))) voice = null;
                var wav = Synthesize(text, voice, Str("language"));
                if (format == "pcm") Respond(s, 200, "audio/pcm", SpeechWav.PcmData(wav));
                else Respond(s, 200, "audio/wav", wav);
                return;
            }
            case ("POST", "/v1/audio/transcriptions" or "/transcribe"):
            {
                byte[] audio = null;
                string language = null, responseFormat = "json";
                if (r.ContentType?.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) == true)
                {
                    foreach (var (name, data) in Multipart(r.Body, r.ContentType))
                    {
                        if (name is "file" or "audio") audio = data;
                        else if (name == "language") language = Encoding.UTF8.GetString(data);
                        else if (name == "response_format") responseFormat = Encoding.UTF8.GetString(data);
                    }
                }
                else if (r.ContentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) == true)
                {
                    using var doc = JsonDocument.Parse(r.Body);
                    if (doc.RootElement.TryGetProperty("audio", out var a)) audio = Convert.FromBase64String(a.GetString());
                    if (doc.RootElement.TryGetProperty("language", out var l)) language = l.GetString();
                }
                else audio = r.Body;
                if (audio == null || audio.Length == 0) { Error(s, 400, "Missing audio data"); return; }
                var temp = Path.Combine(Path.GetTempPath(), $"speech-server-{Guid.NewGuid():N}.wav");
                try
                {
                    File.WriteAllBytes(temp, audio);
                    var text = Whisper(temp, asrModel, language, out var error);
                    if (text == null) { Error(s, 500, error); return; }
                    var duration = 0.0;
                    try { duration = SpeechWav.Read(audio).Duration; } catch (InvalidDataException) { }
                    if (responseFormat == "text") Respond(s, 200, "text/plain; charset=utf-8", Encoding.UTF8.GetBytes(text));
                    else Json(s, 200, string.Format(CultureInfo.InvariantCulture, "{{\"text\":{0},\"model\":\"whisper\",\"duration\":{1:0.##}}}", MediaControl.JsonString(text), duration));
                }
                finally { try { File.Delete(temp); } catch (IOException) { } }
                return;
            }
            default:
                Error(s, 404, $"no route for {r.Method} {r.Path}");
                return;
        }
    }

    // multipart/form-data parts as (field name, bytes).
    static IEnumerable<(string Name, byte[] Data)> Multipart(byte[] body, string contentType)
    {
        var boundaryAt = contentType.IndexOf("boundary=", StringComparison.OrdinalIgnoreCase);
        if (boundaryAt < 0) yield break;
        var boundary = Encoding.ASCII.GetBytes("--" + contentType[(boundaryAt + 9)..].Trim('"', ' '));
        var span = body.AsSpan();
        var pos = span.IndexOf(boundary);
        while (pos >= 0)
        {
            var start = pos + boundary.Length;
            if (start + 2 > body.Length || (body[start] == '-' && body[start + 1] == '-')) yield break;
            var next = body.AsSpan(start).IndexOf(boundary);
            if (next < 0) yield break;
            var part = body.AsSpan(start, next).ToArray();
            var headerEnd = part.AsSpan().IndexOf("\r\n\r\n"u8);
            if (headerEnd >= 0)
            {
                var headers = Encoding.UTF8.GetString(part, 0, headerEnd);
                var nameAt = headers.IndexOf("name=\"", StringComparison.Ordinal);
                if (nameAt >= 0)
                {
                    var name = headers[(nameAt + 6)..headers.IndexOf('"', nameAt + 6)];
                    var dataLength = part.Length - headerEnd - 4 - 2;   // trailing CRLF before the boundary
                    yield return (name, part.AsSpan(headerEnd + 4, Math.Max(0, dataLength)).ToArray());
                }
            }
            pos = start + next;
        }
    }
}

// WAV decoding: PCM 8/16/24/32-bit and 32-bit float (and WAVE_FORMAT_EXTENSIBLE), mixed to mono.
sealed class SpeechWav
{
    public float[] Samples;
    public int SampleRate;
    public double Duration => SampleRate > 0 ? Samples.Length / (double)SampleRate : 0;

    public static SpeechWav Read(byte[] d)
    {
        if (d.Length < 12 || Encoding.ASCII.GetString(d, 0, 4) != "RIFF" || Encoding.ASCII.GetString(d, 8, 4) != "WAVE")
            throw new InvalidDataException("not a WAV file");
        int format = 0, channels = 0, rate = 0, bits = 0, pos = 12;
        while (pos + 8 <= d.Length)
        {
            var id = Encoding.ASCII.GetString(d, pos, 4);
            var size = BitConverter.ToInt32(d, pos + 4);
            var body = pos + 8;
            if (id == "fmt ")
            {
                format = BitConverter.ToUInt16(d, body);
                channels = BitConverter.ToUInt16(d, body + 2);
                rate = BitConverter.ToInt32(d, body + 4);
                bits = BitConverter.ToUInt16(d, body + 14);
                if (format == 0xFFFE && size >= 26) format = BitConverter.ToUInt16(d, body + 24);   // extensible: subformat GUID's first word
            }
            else if (id == "data")
            {
                if (channels == 0) throw new InvalidDataException("WAV data before its fmt chunk");
                if (size < 0 || body + size > d.Length) size = d.Length - body;   // streamed WAVs leave the size unset
                var bytesPer = bits / 8;
                var frames = size / (bytesPer * channels);
                var samples = new float[frames];
                for (var f = 0; f < frames; f++)
                {
                    float sum = 0;
                    for (var c = 0; c < channels; c++)
                    {
                        var at = body + (f * channels + c) * bytesPer;
                        sum += (format, bits) switch
                        {
                            (3, 32) => BitConverter.ToSingle(d, at),
                            (1, 8) => (d[at] - 128) / 128f,
                            (1, 16) => BitConverter.ToInt16(d, at) / 32768f,
                            (1, 24) => ((d[at] | d[at + 1] << 8 | (sbyte)d[at + 2] << 16)) / 8388608f,
                            (1, 32) => BitConverter.ToInt32(d, at) / 2147483648f,
                            _ => throw new InvalidDataException($"unsupported WAV encoding (format {format}, {bits}-bit)"),
                        };
                    }
                    samples[f] = sum / channels;
                }
                return new SpeechWav { Samples = samples, SampleRate = rate };
            }
            pos = body + size + (size & 1);
        }
        throw new InvalidDataException("WAV file has no data chunk");
    }

    // The raw sample bytes of a WAV file's data chunk.
    public static byte[] PcmData(byte[] wav)
    {
        var pos = 12;
        while (pos + 8 <= wav.Length)
        {
            var size = BitConverter.ToInt32(wav, pos + 4);
            if (Encoding.ASCII.GetString(wav, pos, 4) == "data") return wav.AsSpan(pos + 8, Math.Min(size, wav.Length - pos - 8)).ToArray();
            pos += 8 + size + (size & 1);
        }
        return [];
    }
}
