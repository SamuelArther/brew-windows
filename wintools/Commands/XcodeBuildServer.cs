using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace WinTools;

// xcode-build-server: a Build Server Protocol server that gives sourcekit-lsp the compiler flags of
// an Xcode project, so editors (VS Code, Neovim ...) get Swift/Objective-C completion and
// diagnostics for Xcode projects. The flags come from an xcodebuild build log: `parse` turns the
// log's swiftc and clang invocations into the .compile database; `config` writes buildServer.json
// so sourcekit-lsp starts this server; run without arguments (as sourcekit-lsp does) it serves BSP
// over stdin/stdout: build/initialize, workspace/buildTargets, buildTarget/sources,
// textDocument/sourceKitOptions, textDocument/registerForChanges (with build/sourceKitOptionsChanged
// notifications), build/shutdown and build/exit. Building happens wherever Xcode runs; the log can
// be copied from a Mac or CI, and paths in it can be remapped with `parse --map FROM=TO`.
static class XcodeBuildServer
{
    const string Version = "1.3.0";

    [ModuleInitializer]
    internal static void Register() => Commands.Add("xcode-build-server", "Build Server Protocol server giving sourcekit-lsp the flags of Xcode builds", Run);

    static int Run(string[] args)
    {
        if (args.Length > 0 && args[0] is "-h" or "--help" or "help")
        {
            Console.WriteLine("""
                usage: xcode-build-server [-h] {config,parse,serve} ...

                xcode build server

                commands:
                  config    -project X.xcodeproj | -workspace X.xcworkspace -scheme S
                            write buildServer.json for sourcekit-lsp in the current folder
                  parse     [-o OUTPUT] [-a] [--map FROM=TO] [LOG]
                            read an xcodebuild log (file or stdin) into the .compile database
                  serve     serve BSP on stdin/stdout (the default with no command)

                options:
                  -h, --help  show this help message and exit
                  --version   show the version
                """);
            return 0;
        }
        if (args.Length > 0 && args[0] == "--version") { Console.WriteLine(Version); return 0; }
        if (args.Length == 0 || args[0] == "serve") return Serve();
        return args[0] switch
        {
            "config" => Config(args[1..]),
            "parse" => Parse(args[1..]),
            _ => throw new UsageException($"invalid choice: '{args[0]}' (choose from 'config', 'parse', 'serve')"),
        };
    }

    // ---- config ----

    static int Config(string[] args)
    {
        string project = null, workspace = null, scheme = null;
        for (var i = 0; i < args.Length; i++)
        {
            string Value() => ++i < args.Length ? args[i] : throw new UsageException($"{args[i - 1]} needs a value");
            switch (args[i])
            {
                case "-project": project = Value(); break;
                case "-workspace": workspace = Value(); break;
                case "-scheme": scheme = Value(); break;
                case "-h" or "--help":
                    Console.WriteLine("usage: xcode-build-server config [-workspace W | -project P] -scheme S");
                    return 0;
                default: throw new UsageException($"unrecognized arguments: {args[i]}");
            }
        }
        if (workspace == null && project == null)
        {
            // Like the original: use the only workspace or project in the folder.
            var found = Directory.GetDirectories(".", "*.xcworkspace").Concat(Directory.GetDirectories(".", "*.xcodeproj")).ToList();
            if (found.Count == 0) { Console.Error.WriteLine("xcode-build-server: no -workspace or -project given and none found here"); return 1; }
            if (found[0].EndsWith(".xcworkspace")) workspace = found[0]; else project = found[0];
        }
        var container = Path.GetFullPath(workspace ?? Path.Combine(project!, "project.xcworkspace"));
        var exe = Environment.ProcessPath ?? "xcode-build-server";
        var argv = Path.GetFileNameWithoutExtension(exe).Equals("wintools", StringComparison.OrdinalIgnoreCase) ? new[] { exe, "xcode-build-server" } : [exe];
        var ms = new MemoryStream();
        using (var w = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true }))
        {
            w.WriteStartObject();
            w.WriteString("name", "xcode build server");
            w.WriteString("version", "0.2");
            w.WriteString("bspVersion", "2.0");
            w.WriteStartArray("languages");
            foreach (var l in new[] { "c", "cpp", "objective-c", "objective-cpp", "swift" }) w.WriteStringValue(l);
            w.WriteEndArray();
            w.WriteStartArray("argv");
            foreach (var a in argv) w.WriteStringValue(a);
            w.WriteEndArray();
            w.WriteString("workspace", container);
            if (scheme != null) w.WriteString("scheme", scheme);
            w.WriteString("kind", "xcode");
            w.WriteEndObject();
        }
        File.WriteAllBytes("buildServer.json", ms.ToArray());
        Console.WriteLine($"updated buildServer.json ({(workspace != null ? "workspace" : "project")} {container}{(scheme != null ? $", scheme {scheme}" : "")})");
        return 0;
    }

    // ---- parse: xcodebuild log -> .compile ----

    sealed class CompileEntry
    {
        public string Directory, Command, File, Output, Module;
        public List<string> Files = [];
        public List<string> FileLists = [];
    }

    static int Parse(string[] args)
    {
        string output = ".compile", log = null;
        var append = false;
        var maps = new List<(string From, string To)>();
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-o" or "--output": output = ++i < args.Length ? args[i] : throw new UsageException("-o needs a path"); break;
                case "-a" or "--append": append = true; break;
                case "--map":
                {
                    var m = ++i < args.Length ? args[i] : throw new UsageException("--map needs FROM=TO");
                    var eq = m.IndexOf('=');
                    if (eq <= 0) throw new UsageException("--map needs FROM=TO");
                    maps.Add((m[..eq], m[(eq + 1)..]));
                    break;
                }
                case "-h" or "--help":
                    Console.WriteLine("usage: xcode-build-server parse [-o OUTPUT] [-a] [--map FROM=TO] [LOG]\n  read an xcodebuild log (or stdin) and write the compile database");
                    return 0;
                default:
                    if (args[i].StartsWith('-') && args[i] != "-") throw new UsageException($"unrecognized arguments: {args[i]}");
                    log = args[i];
                    break;
            }
        }
        var text = log == null || log == "-" ? Console.In.ReadToEnd() : File.ReadAllText(log);
        foreach (var (from, to) in maps) text = text.Replace(from, to);
        var entries = ParseLog(text);
        var existing = append && File.Exists(output) ? LoadCompile(output) : [];
        // Newer entries for the same file or module replace older ones.
        existing.RemoveAll(e => entries.Any(n => n.File != null ? n.File == e.File : n.Module != null && n.Module == e.Module));
        existing.AddRange(entries);
        File.WriteAllBytes(output, WriteCompile(existing));
        Console.Error.WriteLine($"xcode-build-server: {entries.Count} compile commands ({entries.Count(e => e.Module != null)} swift modules) written to {output}");
        return 0;
    }

    static List<CompileEntry> ParseLog(string text)
    {
        var entries = new List<CompileEntry>();
        var dir = "";
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith("cd ")) { dir = Unquote(line[3..].Trim()); continue; }
            // builtin-swiftDriver -- /path/swiftc ...  /  builtin-SwiftDriver -- ...
            var dd = line.IndexOf(" -- ", StringComparison.Ordinal);
            if (line.StartsWith("builtin-swift", StringComparison.OrdinalIgnoreCase) && dd > 0) line = line[(dd + 4)..];
            var words = Split(line);
            if (words.Count < 2) continue;
            var tool = Path.GetFileName(words[0].Replace('\\', '/'));
            if (tool is "swiftc" or "swiftc.exe" && words.Contains("-module-name"))
            {
                var e = new CompileEntry { Directory = dir, Command = line, Module = words[words.IndexOf("-module-name") + 1] };
                for (var k = 1; k < words.Count; k++)
                {
                    var w = words[k];
                    if (w.EndsWith(".swift", StringComparison.Ordinal) && !words[k - 1].StartsWith("-")) e.Files.Add(Abs(dir, w));
                    else if (w == "-filelist" && k + 1 < words.Count) e.FileLists.Add(Abs(dir, words[++k]));
                    else if (w.StartsWith('@') && w.EndsWith(".SwiftFileList")) e.FileLists.Add(Abs(dir, w[1..]));
                }
                entries.RemoveAll(x => x.Module == e.Module);
                entries.Add(e);
            }
            else if (tool is "clang" or "clang++" or "clang.exe" or "clang++.exe" && words.Contains("-c"))
            {
                var c = words.IndexOf("-c");
                if (c + 1 >= words.Count) continue;
                var o = words.IndexOf("-o");
                var e = new CompileEntry { Directory = dir, Command = line, File = Abs(dir, words[c + 1]), Output = o >= 0 && o + 1 < words.Count ? Abs(dir, words[o + 1]) : null };
                entries.RemoveAll(x => x.File == e.File);
                entries.Add(e);
            }
        }
        return entries;
    }

    static string Abs(string dir, string path) =>
        path.StartsWith('/') || Path.IsPathRooted(path) || dir.Length == 0 ? path : dir.TrimEnd('/', '\\') + "/" + path;

    static string Unquote(string s) => s.Length >= 2 && (s[0] == '"' && s[^1] == '"' || s[0] == '\'' && s[^1] == '\'') ? s[1..^1] : s.Replace("\\ ", " ");

    // POSIX shell word splitting (quotes and backslash escapes), as the log prints commands.
    static List<string> Split(string line)
    {
        var words = new List<string>();
        var sb = new StringBuilder();
        var any = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (char.IsWhiteSpace(ch)) { if (any) { words.Add(sb.ToString()); sb.Clear(); any = false; } continue; }
            any = true;
            if (ch == '\\' && i + 1 < line.Length) { sb.Append(line[++i]); continue; }
            if (ch == '\'')
            {
                var end = line.IndexOf('\'', i + 1);
                if (end < 0) end = line.Length;
                sb.Append(line, i + 1, end - i - 1);
                i = end;
                continue;
            }
            if (ch == '"')
            {
                for (i++; i < line.Length && line[i] != '"'; i++)
                {
                    if (line[i] == '\\' && i + 1 < line.Length && line[i + 1] is '"' or '\\' or '$' or '`') i++;
                    sb.Append(line[i]);
                }
                continue;
            }
            sb.Append(ch);
        }
        if (any) words.Add(sb.ToString());
        return words;
    }

    static byte[] WriteCompile(List<CompileEntry> entries)
    {
        var ms = new MemoryStream();
        using var w = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true });
        w.WriteStartArray();
        foreach (var e in entries)
        {
            w.WriteStartObject();
            w.WriteString("directory", e.Directory);
            w.WriteString("command", e.Command);
            if (e.Module != null)
            {
                w.WriteString("module_name", e.Module);
                w.WriteStartArray("files");
                foreach (var f in e.Files) w.WriteStringValue(f);
                w.WriteEndArray();
                w.WriteStartArray("fileLists");
                foreach (var f in e.FileLists) w.WriteStringValue(f);
                w.WriteEndArray();
            }
            else
            {
                w.WriteString("file", e.File);
                if (e.Output != null) w.WriteString("output", e.Output);
            }
            w.WriteEndObject();
        }
        w.WriteEndArray();
        w.Flush();
        return ms.ToArray();
    }

    static List<CompileEntry> LoadCompile(string path)
    {
        var list = new List<CompileEntry>();
        using var doc = JsonDocument.Parse(File.ReadAllBytes(path));
        foreach (var e in doc.RootElement.EnumerateArray())
        {
            string S(string k) => e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            List<string> L(string k) => e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Array ? v.EnumerateArray().Select(x => x.GetString()).ToList() : [];
            list.Add(new CompileEntry { Directory = S("directory") ?? "", Command = S("command") ?? "", File = S("file"), Output = S("output"), Module = S("module_name"), Files = L("files"), FileLists = L("fileLists") });
        }
        return list;
    }

    // ---- flags for a file ----

    // The compiler arguments sourcekit-lsp needs for one source file, from the .compile database.
    static (List<string> Args, string Dir)? Flags(List<CompileEntry> db, string file)
    {
        var norm = Norm(file);
        foreach (var e in db.Where(e => e.File != null))
            if (Norm(e.File) == norm)
            {
                var words = Split(e.Command);
                var args = new List<string>();
                for (var i = 1; i < words.Count; i++)
                {
                    if (words[i] is "-c") { i++; continue; }
                    if (words[i] is "-o" or "-MF" or "--serialize-diagnostics") { i++; continue; }
                    if (words[i] is "-MMD" or "-MD") continue;
                    args.Add(words[i]);
                }
                args.Add(e.File);
                return (args, e.Directory);
            }
        foreach (var e in db.Where(e => e.Module != null))
        {
            var files = e.Files.ToList();
            foreach (var list in e.FileLists.Where(File.Exists)) files.AddRange(File.ReadAllLines(list).Where(l => l.Length > 0));
            if (!files.Any(f => Norm(f) == norm)) continue;
            var words = Split(e.Command);
            var args = new List<string>();
            for (var i = 1; i < words.Count; i++)
            {
                var w = words[i];
                if (w is "-filelist" or "-output-file-map" or "-emit-module-path" or "-emit-objc-header-path" or "-emit-dependencies-path"
                    or "-serialize-diagnostics-path" or "-supplementary-output-file-map" or "-index-unit-output-path") { i++; continue; }
                if (w.StartsWith('@') || w is "-c" or "-emit-module" or "-emit-dependencies" or "-emit-objc-header" or "-incremental"
                    or "-parseable-output" or "-use-frontend-parseable-output" or "-whole-module-optimization" or "-enable-batch-mode" or "-serialize-diagnostics") continue;
                if (w.EndsWith(".swift")) continue;
                args.Add(w);
            }
            args.AddRange(files);
            return (args, e.Directory);
        }
        return null;
    }

    static string Norm(string path)
    {
        if (path.StartsWith("file://")) path = Uri.UnescapeDataString(new Uri(path).AbsolutePath);
        path = path.Replace('\\', '/');
        if (path.Length > 2 && path[0] == '/' && path[2] == ':') path = path[1..];
        return path.ToLowerInvariant();
    }

    // ---- BSP server ----

    static int Serve()
    {
        var input = Console.OpenStandardInput();
        var output = Console.OpenStandardOutput();
        var root = Directory.GetCurrentDirectory();
        var db = new List<CompileEntry>();
        var registered = new HashSet<string>();
        void Reload()
        {
            var compile = Path.Combine(root, ".compile");
            db = File.Exists(compile) ? LoadCompile(compile) : [];
        }
        Reload();
        while (ReadMessage(input) is { } body)
        {
            using var doc = JsonDocument.Parse(body);
            var msg = doc.RootElement;
            var method = msg.TryGetProperty("method", out var m) ? m.GetString() : null;
            var hasId = msg.TryGetProperty("id", out var id);
            var p = msg.TryGetProperty("params", out var pp) ? pp : default;
            switch (method)
            {
                case "build/initialize":
                    if (p.ValueKind == JsonValueKind.Object && p.TryGetProperty("rootUri", out var ru) && ru.GetString() is { } rootUri && rootUri.StartsWith("file:"))
                        root = new Uri(rootUri).LocalPath;
                    Reload();
                    Respond(output, id, w =>
                    {
                        w.WriteStartObject();
                        w.WriteString("displayName", "xcode build server");
                        w.WriteString("version", Version);
                        w.WriteString("bspVersion", "2.0");
                        w.WriteStartObject("capabilities");
                        w.WriteStartArray("languageIds");
                        foreach (var l in new[] { "c", "cpp", "objective-c", "objective-cpp", "swift" }) w.WriteStringValue(l);
                        w.WriteEndArray();
                        w.WriteEndObject();
                        w.WriteString("dataKind", "sourceKit");
                        w.WriteStartObject("data");
                        w.WriteBoolean("sourceKitOptionsProvider", true);
                        var index = Path.Combine(root, ".build", "index");
                        w.WriteString("indexDatabasePath", Path.Combine(index, "db"));
                        w.WriteString("indexStorePath", Path.Combine(index, "store"));
                        w.WriteEndObject();
                        w.WriteEndObject();
                    });
                    break;
                case "build/initialized" or "$/cancelRequest" or "workspace/didChangeWatchedFiles":
                    if (method == "workspace/didChangeWatchedFiles") Reload();
                    break;
                case "workspace/buildTargets":
                    Respond(output, id, w =>
                    {
                        w.WriteStartObject();
                        w.WriteStartArray("targets");
                        w.WriteStartObject();
                        w.WriteStartObject("id");
                        w.WriteString("uri", "xcode-build-server://default");
                        w.WriteEndObject();
                        w.WriteString("displayName", "default");
                        w.WriteStartArray("tags");
                        w.WriteEndArray();
                        w.WriteStartArray("languageIds");
                        foreach (var l in new[] { "c", "cpp", "objective-c", "objective-cpp", "swift" }) w.WriteStringValue(l);
                        w.WriteEndArray();
                        w.WriteStartArray("dependencies");
                        w.WriteEndArray();
                        w.WriteStartObject("capabilities");
                        w.WriteEndObject();
                        w.WriteEndObject();
                        w.WriteEndArray();
                        w.WriteEndObject();
                    });
                    break;
                case "buildTarget/sources":
                    Respond(output, id, w =>
                    {
                        w.WriteStartObject();
                        w.WriteStartArray("items");
                        w.WriteStartObject();
                        w.WriteStartObject("target");
                        w.WriteString("uri", "xcode-build-server://default");
                        w.WriteEndObject();
                        w.WriteStartArray("sources");
                        var files = db.SelectMany(e => e.File != null ? [e.File] : e.Files).Distinct();
                        foreach (var f in files)
                        {
                            w.WriteStartObject();
                            w.WriteString("uri", FileUri(f));
                            w.WriteNumber("kind", 1);
                            w.WriteBoolean("generated", false);
                            w.WriteEndObject();
                        }
                        w.WriteEndArray();
                        w.WriteEndObject();
                        w.WriteEndArray();
                        w.WriteEndObject();
                    });
                    break;
                case "textDocument/sourceKitOptions":
                {
                    var uri = p.GetProperty("textDocument").GetProperty("uri").GetString();
                    var flags = Flags(db, uri);
                    Respond(output, id, w => Options(w, flags));
                    break;
                }
                case "textDocument/registerForChanges":
                {
                    var uri = p.GetProperty("uri").GetString();
                    var action = p.TryGetProperty("action", out var a) ? a.GetString() : "register";
                    Respond(output, id, w => w.WriteNullValue());
                    if (action == "register")
                    {
                        registered.Add(uri);
                        var flags = Flags(db, uri);
                        Notify(output, "build/sourceKitOptionsChanged", w =>
                        {
                            w.WriteStartObject();
                            w.WriteString("uri", uri);
                            w.WritePropertyName("updatedOptions");
                            Options(w, flags);
                            w.WriteEndObject();
                        });
                    }
                    else registered.Remove(uri);
                    break;
                }
                case "buildTarget/prepare" or "workspace/waitForBuildSystemUpdates" or "build/shutdown" or "buildTarget/didChange":
                    if (hasId) Respond(output, id, w => w.WriteNullValue());
                    break;
                case "build/exit":
                    return 0;
                default:
                    if (hasId) Error(output, id, -32601, $"method not found: {method}");
                    break;
            }
        }
        return 0;
    }

    static void Options(Utf8JsonWriter w, (List<string> Args, string Dir)? flags)
    {
        if (flags is not { } f) { w.WriteNullValue(); return; }
        w.WriteStartObject();
        w.WriteStartArray("compilerArguments");
        foreach (var a in f.Args) w.WriteStringValue(a);
        w.WriteEndArray();
        w.WriteStartArray("options");
        foreach (var a in f.Args) w.WriteStringValue(a);
        w.WriteEndArray();
        w.WriteString("workingDirectory", f.Dir);
        w.WriteEndObject();
    }

    static string FileUri(string path) => path.StartsWith('/') ? "file://" + Uri.EscapeDataString(path).Replace("%2F", "/") : new Uri(Path.GetFullPath(path)).AbsoluteUri;

    // LSP framing: "Content-Length: N" headers, a blank line, then N bytes of JSON.
    static byte[] ReadMessage(Stream input)
    {
        var length = -1;
        while (true)
        {
            var header = ReadLine(input);
            if (header == null) return null;
            if (header.Length == 0) break;
            if (header.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)) length = int.Parse(header[15..].Trim());
        }
        if (length < 0) return null;
        var body = new byte[length];
        var read = 0;
        while (read < length)
        {
            var n = input.Read(body, read, length - read);
            if (n <= 0) return null;
            read += n;
        }
        return body;
    }

    static string ReadLine(Stream input)
    {
        var sb = new StringBuilder();
        while (true)
        {
            var b = input.ReadByte();
            if (b < 0) return sb.Length == 0 ? null : sb.ToString();
            if (b == '\n') return sb.ToString().TrimEnd('\r');
            sb.Append((char)b);
        }
    }

    static void Send(Stream output, Action<Utf8JsonWriter> write)
    {
        var ms = new MemoryStream();
        using (var w = new Utf8JsonWriter(ms)) write(w);
        var body = ms.ToArray();
        var header = Encoding.ASCII.GetBytes($"Content-Length: {body.Length}\r\n\r\n");
        output.Write(header);
        output.Write(body);
        output.Flush();
    }

    static void Respond(Stream output, JsonElement id, Action<Utf8JsonWriter> result) => Send(output, w =>
    {
        w.WriteStartObject();
        w.WriteString("jsonrpc", "2.0");
        w.WritePropertyName("id");
        id.WriteTo(w);
        w.WritePropertyName("result");
        result(w);
        w.WriteEndObject();
    });

    static void Notify(Stream output, string method, Action<Utf8JsonWriter> parameters) => Send(output, w =>
    {
        w.WriteStartObject();
        w.WriteString("jsonrpc", "2.0");
        w.WriteString("method", method);
        w.WritePropertyName("params");
        parameters(w);
        w.WriteEndObject();
    });

    static void Error(Stream output, JsonElement id, int code, string message) => Send(output, w =>
    {
        w.WriteStartObject();
        w.WriteString("jsonrpc", "2.0");
        w.WritePropertyName("id");
        id.WriteTo(w);
        w.WriteStartObject("error");
        w.WriteNumber("code", code);
        w.WriteString("message", message);
        w.WriteEndObject();
        w.WriteEndObject();
    });
}
