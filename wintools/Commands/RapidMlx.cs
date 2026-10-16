using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace WinTools;

// rapid-mlx: local LLM engine with an OpenAI-compatible API. On macOS it runs MLX on Apple Silicon;
// the Windows engine (as yap does with whisper.cpp) is llama.cpp: `rapid-mlx serve <alias>` starts
// llama-server (brew install llama.cpp) on rapid-mlx's port 8000 with the GGUF build of the same
// model from Hugging Face. Aliases keep rapid-mlx's names (qwen3.5-4b-4bit, gpt-oss-20b-4bit, ...),
// `pull`/`ls`/`rm` manage downloaded GGUF files, `chat` and `bench` run llama-cli and llama-bench,
// and `ps` lists the servers rapid-mlx started.
static class RapidMlx
{
    const string Version = "0.15.7";
    const int DefaultPort = 8000;

    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("rapid-mlx", "Local LLM engine with an OpenAI-compatible API (llama.cpp on Windows)", Run);

    // rapid-mlx alias -> GGUF repository, quantization tag, tool-call parser, rough memory need (GB).
    static readonly (string Alias, string Repo, string Quant, string Parser, int MemoryGb)[] Aliases =
    [
        ("qwen3.5-4b-4bit", "unsloth/Qwen3.5-4B-GGUF", "Q4_K_M", "hermes", 4),
        ("qwen3.5-9b-4bit", "unsloth/Qwen3.5-9B-GGUF", "Q4_K_M", "hermes", 8),
        ("qwen3.5-27b-4bit", "unsloth/Qwen3.5-27B-GGUF", "Q4_K_M", "hermes", 20),
        ("qwen3.5-35b-4bit", "unsloth/Qwen3.5-35B-A3B-GGUF", "Q4_K_M", "hermes", 24),
        ("qwen3.6-27b-4bit", "unsloth/Qwen3.6-27B-GGUF", "Q4_K_M", "hermes", 20),
        ("qwen3.6-35b-4bit", "unsloth/Qwen3.6-35B-A3B-GGUF", "UD-Q4_K_M", "hermes", 24),
        ("qwen3-0.6b-4bit", "unsloth/Qwen3-0.6B-GGUF", "Q4_K_M", "hermes", 2),
        ("qwen3-8b-4bit", "unsloth/Qwen3-8B-GGUF", "Q4_K_M", "hermes", 8),
        ("qwen3-4b-instruct-2507-4bit", "unsloth/Qwen3-4B-Instruct-2507-GGUF", "Q4_K_M", "hermes", 4),
        ("qwen3-coder-30b-4bit", "unsloth/Qwen3-Coder-30B-A3B-Instruct-GGUF", "Q4_K_M", "qwen3_coder", 20),
        ("qwen2.5-14b-4bit", "bartowski/Qwen2.5-14B-Instruct-GGUF", "Q4_K_M", "hermes", 12),
        ("llama-3.1-8b-4bit", "bartowski/Meta-Llama-3.1-8B-Instruct-GGUF", "Q4_K_M", "llama3_json", 8),
        ("llama3-1b-4bit", "bartowski/Llama-3.2-1B-Instruct-GGUF", "Q4_K_M", "llama3_json", 2),
        ("llama3-3b-4bit", "bartowski/Llama-3.2-3B-Instruct-GGUF", "Q4_K_M", "llama3_json", 4),
        ("hermes3-8b-4bit", "NousResearch/Hermes-3-Llama-3.1-8B-GGUF", "Q4_K_M", "hermes", 8),
        ("gemma-4-e4b-4bit", "unsloth/gemma-4-e4b-it-GGUF", "Q4_K_M", "gemma4", 6),
        ("gemma-4-12b-4bit", "unsloth/gemma-4-12B-it-GGUF", "Q4_K_M", "gemma4", 10),
        ("gemma-4-31b-4bit", "unsloth/gemma-4-31b-it-GGUF", "Q4_K_M", "gemma4", 24),
        ("gemma3-1b-4bit", "unsloth/gemma-3-1b-it-GGUF", "Q4_K_M", "gemma3", 2),
        ("gemma3-4b-qat-4bit", "google/gemma-3-4b-it-qat-q4_0-gguf", "Q4_0", "gemma3", 4),
        ("gemma3-12b-4bit", "google/gemma-3-12b-it-qat-q4_0-gguf", "Q4_0", "gemma3", 10),
        ("gemma3-27b-qat-4bit", "google/gemma-3-27b-it-qat-q4_0-gguf", "Q4_0", "gemma3", 20),
        ("phi-4-14b-4bit", "unsloth/phi-4-GGUF", "Q4_K_M", "hermes", 12),
        ("phi-4-mini-4bit", "unsloth/Phi-4-mini-instruct-GGUF", "Q4_K_M", "hermes", 4),
        ("mistral-24b-4bit", "unsloth/Mistral-Small-3.1-24B-Instruct-2503-GGUF", "Q4_K_M", "mistral", 18),
        ("devstral-24b-4bit", "unsloth/Devstral-Small-2507-GGUF", "Q4_K_M", "mistral", 18),
        ("gpt-oss-20b-4bit", "ggml-org/gpt-oss-20b-GGUF", "MXFP4", "harmony", 16),
        ("gpt-oss-120b-4bit", "ggml-org/gpt-oss-120b-GGUF", "MXFP4", "harmony", 64),
        ("deepseek-r1-8b-4bit", "unsloth/DeepSeek-R1-0528-Qwen3-8B-GGUF", "Q4_K_M", "hermes", 8),
        ("glm4.7-9b-4bit", "unsloth/GLM-4.7-Flash-GGUF", "Q4_K_M", "glm47", 20),
        ("smollm3-3b-4bit", "ggml-org/SmolLM3-3B-GGUF", "Q4_K_M", "hermes", 4),
        ("granite4-tiny-4bit", "ibm-granite/granite-4.0-h-tiny-GGUF", "Q4_K_M", "granite", 6),
    ];

    sealed class RmModel
    {
        public string Alias, Repo, Quant, Parser, LocalFile;
        public int MemoryGb;
        public string Spec => LocalFile ?? $"{Repo}:{Quant}";
    }

    static string DataDir => Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA") is { Length: > 0 } l ? l
        : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "rapid-mlx");
    static string ModelsDir => Environment.GetEnvironmentVariable("RAPID_MLX_MODELS") is { Length: > 0 } m ? m : Path.Combine(DataDir, "models");
    static string ServersDir => Path.Combine(DataDir, "servers");

    // An alias, a "repo[:quant]" Hugging Face GGUF repository, or a local .gguf file.
    static RmModel Resolve(string name)
    {
        if (name.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase))
        {
            if (!File.Exists(name)) throw new IOException($"{name}: No such file");
            return new RmModel { Alias = Path.GetFileNameWithoutExtension(name), LocalFile = Path.GetFullPath(name), Quant = "(file)" };
        }
        foreach (var a in Aliases)
            if (a.Alias.Equals(name, StringComparison.OrdinalIgnoreCase))
                return WithLocal(new RmModel { Alias = a.Alias, Repo = a.Repo, Quant = a.Quant, Parser = a.Parser, MemoryGb = a.MemoryGb });
        if (name.Contains('/'))
        {
            var colon = name.IndexOf(':');
            var repo = colon < 0 ? name : name[..colon];
            return WithLocal(new RmModel { Alias = name, Repo = repo, Quant = colon < 0 ? "Q4_K_M" : name[(colon + 1)..] });
        }
        var close = Aliases.Select(a => a.Alias).Where(a => a.Contains(name, StringComparison.OrdinalIgnoreCase)).Take(5).ToList();
        throw new UsageException($"unknown model '{name}'" + (close.Count > 0 ? $" (did you mean: {string.Join(", ", close)}?)" : " (see rapid-mlx models)"));
    }

    // A file `rapid-mlx pull` downloaded takes precedence over downloading again.
    static RmModel WithLocal(RmModel m)
    {
        m.LocalFile = PulledFile(m);
        return m;
    }

    static string PulledFile(RmModel m)
    {
        var dir = Path.Combine(ModelsDir, m.Repo.Replace('/', '_'));
        if (!Directory.Exists(dir)) return null;
        return Directory.GetFiles(dir, "*.gguf").FirstOrDefault(f => Path.GetFileName(f).Contains(m.Quant, StringComparison.OrdinalIgnoreCase));
    }

    static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help") { Help(); return 0; }
        var rest = args[1..];
        switch (args[0])
        {
            case "--version" or "version": Console.WriteLine($"rapid-mlx {Version}"); return 0;
            case "help":
                if (rest.Length == 0) { Help(); return 0; }
                return Run([rest[0], "--help"]);
            case "serve": return Serve(rest);
            case "models": return rest.Contains("--cached") ? Cached() : Models(rest);
            case "ls": return Cached();
            case "info": return Info(rest);
            case "pull": return Pull(rest);
            case "rm": return Remove(rest);
            case "chat" or "run": return Chat(rest);
            case "bench": return Bench(rest);
            case "ps": return Ps();
            case "doctor": return Doctor();
            case "upgrade" or "update":
                Console.WriteLine("rapid-mlx is installed by brew: run `brew upgrade rapid-mlx` (and `brew upgrade llama.cpp` for the engine).");
                return 0;
            default:
                throw new UsageException($"unknown command '{args[0]}' (serve, models, ls, info, pull, rm, chat, bench, ps, doctor, version)");
        }
    }

    static bool WantsHelp(string[] args, string usage)
    {
        if (!args.Any(a => a is "-h" or "--help")) return false;
        Console.WriteLine(usage);
        return true;
    }

    // ---- serve ------------------------------------------------------------------------------------

    static int Serve(string[] args)
    {
        if (WantsHelp(args, """
            usage: rapid-mlx serve <model> [--host HOST] [--port PORT] [--api-key KEY] [--ctx-size N]
                                   [--max-num-seqs N] [--gpu-layers N] [--served-model-name NAME]

            Start an OpenAI-compatible server (/v1/chat/completions, /v1/completions, /v1/embeddings,
            /v1/models, /health) with llama.cpp's llama-server. <model> is a rapid-mlx alias, a
            Hugging Face GGUF repo (owner/name[:quant]) or a .gguf file. Default port: 8000.
            Extra llama-server flags can follow `--`.
            """)) return 0;
        string model = null, host = "127.0.0.1", apiKey = null, name = null;
        int port = DefaultPort;
        int? ctx = null, seqs = null, gpuLayers = null;
        var passthrough = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Next() => i + 1 < args.Length ? args[++i] : throw new UsageException($"{a} needs a value");
            switch (a)
            {
                case "--": passthrough.AddRange(args[(i + 1)..]); i = args.Length; break;
                case "--host": host = Next(); break;
                case "--port": port = int.Parse(Next()); break;
                case "--api-key": apiKey = Next(); break;
                case "--ctx-size" or "--max-model-len": ctx = int.Parse(Next()); break;
                case "--max-num-seqs": seqs = int.Parse(Next()); break;
                case "--gpu-layers": gpuLayers = int.Parse(Next()); break;
                case "--served-model-name": name = Next(); break;
                case "--model": model = Next(); break;
                default:
                    if (a.StartsWith('-')) throw new UsageException($"unknown option {a} (pass llama-server flags after --)");
                    model = a;
                    break;
            }
        }
        if (model == null) throw new UsageException("serve needs a model (see rapid-mlx models)");
        var m = Resolve(model);
        var server = FindTool("llama-server") ?? throw new InvalidOperationException("llama.cpp's llama-server was not found; install it with: brew install llama.cpp");

        var argv = new List<string>();
        if (m.LocalFile != null) argv.AddRange(["-m", m.LocalFile]); else argv.AddRange(["-hf", m.Spec]);
        argv.AddRange(["--host", host, "--port", port.ToString(), "--alias", name ?? m.Alias, "--jinja"]);
        if (apiKey != null) argv.AddRange(["--api-key", apiKey]);
        if (ctx is int c) argv.AddRange(["--ctx-size", c.ToString()]);
        if (seqs is int s) argv.AddRange(["--parallel", s.ToString()]);
        if (gpuLayers is int g) argv.AddRange(["--n-gpu-layers", g.ToString()]);
        argv.AddRange(passthrough);

        Console.WriteLine($"rapid-mlx {Version} serving {m.Alias} ({m.Spec}) with llama.cpp");
        Console.WriteLine($"  OpenAI-compatible API: http://{host}:{port}/v1");
        var psi = new ProcessStartInfo(server) { UseShellExecute = false };
        foreach (var a in argv) psi.ArgumentList.Add(a);
        using var p = Start(psi);
        Directory.CreateDirectory(ServersDir);
        var record = Path.Combine(ServersDir, $"{p.Id}.json");
        File.WriteAllText(record, $"{{\"pid\":{p.Id},\"model\":{JsonString(m.Alias)},\"host\":{JsonString(host)},\"port\":{port}}}");
        try { p.WaitForExit(); }
        finally { try { File.Delete(record); } catch (IOException) { } }
        return p.ExitCode;
    }

    static string JsonString(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    static int Ps()
    {
        Console.WriteLine($"{"PID",-8}{"MODEL",-32}URL");
        if (!Directory.Exists(ServersDir)) return 0;
        foreach (var file in Directory.GetFiles(ServersDir, "*.json"))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(file));
                var r = doc.RootElement;
                var pid = r.GetProperty("pid").GetInt32();
                bool alive;
                try { using var proc = Process.GetProcessById(pid); alive = !proc.HasExited; }
                catch (ArgumentException) { alive = false; }
                if (!alive) { File.Delete(file); continue; }
                Console.WriteLine($"{pid,-8}{r.GetProperty("model").GetString(),-32}http://{r.GetProperty("host").GetString()}:{r.GetProperty("port").GetInt32()}/v1");
            }
            catch (Exception e) when (e is JsonException or KeyNotFoundException or IOException or InvalidOperationException) { }
        }
        return 0;
    }

    // ---- models -----------------------------------------------------------------------------------

    static int Models(string[] args)
    {
        if (WantsHelp(args, "usage: rapid-mlx models [--cached]\n\nList the model aliases (GGUF builds served by llama.cpp).")) return 0;
        Console.WriteLine($"{"ALIAS",-30}{"MIN RAM",-9}GGUF REPOSITORY");
        foreach (var a in Aliases)
            Console.WriteLine($"{a.Alias,-30}{a.MemoryGb + " GB",-9}{a.Repo}:{a.Quant}");
        return 0;
    }

    static int Cached()
    {
        if (!Directory.Exists(ModelsDir)) { Console.WriteLine("No models downloaded yet (rapid-mlx pull <model>)."); return 0; }
        var files = Directory.GetFiles(ModelsDir, "*.gguf", SearchOption.AllDirectories);
        if (files.Length == 0) { Console.WriteLine("No models downloaded yet (rapid-mlx pull <model>)."); return 0; }
        foreach (var f in files.Order())
        {
            var repo = Path.GetFileName(Path.GetDirectoryName(f)).Replace('_', '/');
            var alias = Aliases.FirstOrDefault(a => a.Repo == repo && Path.GetFileName(f).Contains(a.Quant, StringComparison.OrdinalIgnoreCase)).Alias;
            Console.WriteLine($"{alias ?? repo,-30}{FormatSize(new FileInfo(f).Length),-10}{f}");
        }
        return 0;
    }

    static string FormatSize(long bytes) => bytes >= 1L << 30 ? $"{bytes / (double)(1L << 30):0.0} GB" : $"{bytes / (double)(1L << 20):0} MB";

    static int Info(string[] args)
    {
        if (WantsHelp(args, "usage: rapid-mlx info <model>\n\nShow the profile of a model alias, repository or .gguf file (offline).")) return 0;
        var name = args.FirstOrDefault(a => !a.StartsWith('-')) ?? throw new UsageException("info needs a model name or alias");
        var m = Resolve(name);
        if (m.Repo != null && !name.Contains('/')) Console.WriteLine($"  alias: {name} -> {m.Repo}:{m.Quant}");
        const int inner = 60;
        string Row(string text) => $"| {text,-inner} |";
        Console.WriteLine();
        Console.WriteLine("+" + new string('-', inner + 2) + "+");
        Console.WriteLine(Row($"Model: {m.Alias}"));
        Console.WriteLine(Row(new string('-', inner)));
        Console.WriteLine(Row($"Source       : {(m.Repo != null ? m.Repo : m.LocalFile)}"));
        Console.WriteLine(Row($"Quantization : {m.Quant}"));
        Console.WriteLine(Row("Engine       : llama.cpp (llama-server), GGUF"));
        if (m.Parser != null) Console.WriteLine(Row($"Tool calls   : {m.Parser} (via the chat template, --jinja)"));
        if (m.MemoryGb > 0) Console.WriteLine(Row($"Memory       : ~{m.MemoryGb} GB RAM/VRAM"));
        Console.WriteLine(Row($"Downloaded   : {(m.LocalFile != null ? "yes" : "no")}"));
        Console.WriteLine("+" + new string('-', inner + 2) + "+");
        Console.WriteLine();
        Console.WriteLine($"  Start: rapid-mlx serve {name}");
        return 0;
    }

    // ---- pull / rm --------------------------------------------------------------------------------

    static int Pull(string[] args)
    {
        if (WantsHelp(args, "usage: rapid-mlx pull <model>\n\nDownload a model's GGUF file (no server).")) return 0;
        var name = args.FirstOrDefault(a => !a.StartsWith('-')) ?? throw new UsageException("pull needs a model");
        var m = Resolve(name);
        if (m.Repo == null) { Console.WriteLine($"{m.LocalFile} is already local."); return 0; }
        if (m.LocalFile != null) { Console.WriteLine($"{m.Alias} is already downloaded: {m.LocalFile}"); return 0; }

        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"rapid-mlx/{Version}");
        if (Environment.GetEnvironmentVariable("HF_TOKEN") is { Length: > 0 } token)
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        List<string> files;
        try
        {
            using var doc = JsonDocument.Parse(http.GetStringAsync($"https://huggingface.co/api/models/{m.Repo}").GetAwaiter().GetResult());
            files = doc.RootElement.GetProperty("siblings").EnumerateArray().Select(s => s.GetProperty("rfilename").GetString()).ToList();
        }
        catch (HttpRequestException e) { throw new IOException($"cannot reach Hugging Face for {m.Repo}: {e.Message}"); }
        var ggufs = files.Where(f => f.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase) && !f.Contains("mmproj", StringComparison.OrdinalIgnoreCase)).ToList();
        var pick = ggufs.Where(f => Path.GetFileName(f).Contains(m.Quant, StringComparison.OrdinalIgnoreCase)).OrderBy(f => f.Length).ToList();
        if (pick.Count == 0) throw new InvalidOperationException($"{m.Repo} has no {m.Quant} GGUF file");
        if (pick.Any(f => System.Text.RegularExpressions.Regex.IsMatch(f, @"-0000\d-of-\d{5}\.gguf$")))
            pick = pick.Where(f => System.Text.RegularExpressions.Regex.IsMatch(f, @"-\d{5}-of-\d{5}\.gguf$")).ToList();   // split model: all parts
        else pick = [pick[0]];

        var dir = Path.Combine(ModelsDir, m.Repo.Replace('/', '_'));
        Directory.CreateDirectory(dir);
        foreach (var file in pick)
        {
            var target = Path.Combine(dir, Path.GetFileName(file));
            Console.WriteLine($"Downloading {m.Repo}/{file}");
            using var response = http.GetAsync($"https://huggingface.co/{m.Repo}/resolve/main/{file}", HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength ?? 0;
            using var input = response.Content.ReadAsStream();
            using (var output = File.Create(target + ".part"))
            {
                var buffer = new byte[1 << 20];
                long done = 0, lastShown = 0;
                for (int n; (n = input.Read(buffer)) > 0;)
                {
                    output.Write(buffer, 0, n);
                    done += n;
                    if (done - lastShown >= 256L << 20 || done == total)
                    {
                        lastShown = done;
                        Console.Error.Write(total > 0 ? $"\r  {FormatSize(done)} / {FormatSize(total)}   " : $"\r  {FormatSize(done)}   ");
                    }
                }
            }
            Console.Error.WriteLine();
            File.Move(target + ".part", target, overwrite: true);
        }
        Console.WriteLine($"Pulled {m.Alias} into {dir}");
        return 0;
    }

    static int Remove(string[] args)
    {
        if (WantsHelp(args, "usage: rapid-mlx rm <model>\n\nDelete a downloaded model.")) return 0;
        var name = args.FirstOrDefault(a => !a.StartsWith('-')) ?? throw new UsageException("rm needs a model");
        var m = Resolve(name);
        if (m.Repo == null || m.LocalFile == null) { Console.Error.WriteLine($"{name} is not downloaded."); return 1; }
        var dir = Path.GetDirectoryName(m.LocalFile);
        foreach (var f in Directory.GetFiles(dir, "*.gguf").Where(f => Path.GetFileName(f).Contains(m.Quant, StringComparison.OrdinalIgnoreCase))) File.Delete(f);
        if (!Directory.EnumerateFileSystemEntries(dir).Any()) Directory.Delete(dir);
        Console.WriteLine($"Removed {m.Alias}");
        return 0;
    }

    // ---- chat / bench / doctor ----------------------------------------------------------------------

    static int Chat(string[] args)
    {
        if (WantsHelp(args, "usage: rapid-mlx chat <model> [--system PROMPT]\n\nInteractive chat in the terminal (llama.cpp's llama-cli).")) return 0;
        string model = null, system = null;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--system") system = i + 1 < args.Length ? args[++i] : throw new UsageException("--system needs a prompt");
            else if (args[i].StartsWith('-')) throw new UsageException($"unknown option {args[i]}");
            else model = args[i];
        }
        var m = Resolve(model ?? throw new UsageException("chat needs a model"));
        var cli = FindTool("llama-cli") ?? throw new InvalidOperationException("llama.cpp's llama-cli was not found; install it with: brew install llama.cpp");
        var psi = new ProcessStartInfo(cli) { UseShellExecute = false };
        foreach (var a in (m.LocalFile != null ? new[] { "-m", m.LocalFile } : ["-hf", m.Spec]).Concat(["-cnv", "--jinja"])) psi.ArgumentList.Add(a);
        if (system != null) { psi.ArgumentList.Add("-sys"); psi.ArgumentList.Add(system); }
        using var p = Start(psi);
        p.WaitForExit();
        return p.ExitCode;
    }

    static int Bench(string[] args)
    {
        if (WantsHelp(args, "usage: rapid-mlx bench <model>\n\nPrompt-processing and generation speed (llama.cpp's llama-bench); pull the model first.")) return 0;
        var m = Resolve(args.FirstOrDefault(a => !a.StartsWith('-')) ?? throw new UsageException("bench needs a model"));
        if (m.LocalFile == null) { Console.Error.WriteLine($"rapid-mlx: download the model first: rapid-mlx pull {m.Alias}"); return 1; }
        var bench = FindTool("llama-bench") ?? throw new InvalidOperationException("llama.cpp's llama-bench was not found; install it with: brew install llama.cpp");
        var psi = new ProcessStartInfo(bench) { UseShellExecute = false };
        psi.ArgumentList.Add("-m");
        psi.ArgumentList.Add(m.LocalFile);
        using var p = Start(psi);
        p.WaitForExit();
        return p.ExitCode;
    }

    static int Doctor()
    {
        var ok = true;
        void Check(string what, bool good, string detail)
        {
            Console.WriteLine($"  {(good ? "ok" : "!!")} {what,-14} {detail}");
            ok &= good;
        }
        Console.WriteLine($"rapid-mlx {Version} (Windows, llama.cpp engine)");
        var server = FindTool("llama-server");
        Check("llama-server", server != null, server ?? "not found: brew install llama.cpp");
        Check("llama-cli", FindTool("llama-cli") != null, FindTool("llama-cli") ?? "not found: brew install llama.cpp");
        Check("models dir", true, ModelsDir);
        var memory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        Check("memory", true, $"{memory / (double)(1L << 30):0.0} GB");
        return ok ? 0 : 1;
    }

    // ---- helpers ----------------------------------------------------------------------------------

    static string FindTool(string name)
    {
        var dirs = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator).ToList();
        if (Environment.GetEnvironmentVariable("HOMEBREW_PREFIX") is { Length: > 0 } prefix) dirs.Add(Path.Combine(prefix, "opt", "llama.cpp", "bin"));
        foreach (var dir in dirs.Where(d => !string.IsNullOrWhiteSpace(d)))
            foreach (var ext in new[] { ".exe", ".cmd" })
            {
                try { var full = Path.Combine(dir.Trim('"'), name + ext); if (File.Exists(full)) return full; }
                catch (ArgumentException) { }
            }
        return null;
    }

    static Process Start(ProcessStartInfo psi)
    {
        try { return Process.Start(psi); }
        catch (Win32Exception e) { throw new InvalidOperationException($"could not run {psi.FileName}: {e.Message}"); }
    }

    static void Help() => Console.WriteLine($"""
        rapid-mlx {Version} - fast local AI engine with an OpenAI-compatible API (llama.cpp on Windows)

        Usage: rapid-mlx <command> [options]

        Serve:
          serve <model>       Start an OpenAI-compatible server (default http://127.0.0.1:{DefaultPort}/v1)
          chat <model>        Interactive chat REPL with a model (alias: run)
          ps                  List running rapid-mlx servers

        Models:
          models              List available model aliases
          info <model>        Show the profile for a model name or alias
          pull <model>        Download a model's GGUF file (no server)
          ls                  List downloaded models (alias for `models --cached`)
          rm <model>          Remove a downloaded model

        Tools:
          bench <model>       Run a speed benchmark (llama-bench)
          doctor              Check the environment
          version             Show version number
          help [command]      Show help for a command

        The engine is llama.cpp (brew install llama.cpp); models are GGUF builds of rapid-mlx's aliases
        from Hugging Face, or any owner/repo[:quant] or .gguf file. RAPID_MLX_MODELS sets the download
        folder and HF_TOKEN authenticates gated repositories.
        """);
}
