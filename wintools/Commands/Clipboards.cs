using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace WinTools;

// wl-copy / wl-paste / pngpaste / clipper / clippy + pasty: the Windows clipboard (OpenClipboard and
// friends). Clipboard formats are named with the MIME types wl-clipboard uses: text, PNG, DIB images,
// HTML, RTF and file lists (CF_HDROP, i.e. what Explorer's Copy puts there).
static class Clipboards
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("wl-copy", "Copy standard input or arguments to the clipboard", WlCopy);
        Commands.Add("wl-paste", "Write the clipboard contents to standard output", WlPaste);
        Commands.Add("pngpaste", "Save the image on the clipboard to a PNG (or GIF/JPEG/TIFF/BMP) file", PngPaste);
        Commands.Add("clipper", "Clipboard server: whatever is sent to its port or socket is copied", Clipper);
        Commands.Add("clippy", "Copy files (pasteable into apps), text or recent downloads to the clipboard", Clippy);
        Commands.Add("pasty", "Paste copied files into a folder, or clipboard text to standard output", Pasty);
    }

    static void Version(string name) => Console.WriteLine($"{name} {Commands.Version} (wintools)");

    // ---- wl-copy / wl-paste ----

    static int WlCopy(string[] args)
    {
        bool once = false, clear = false, trim = false;
        string type = null;
        var words = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (words.Count > 0 || arg == "--" || !arg.StartsWith('-') || arg == "-")
            {
                if (arg == "--" && words.Count == 0) { words.AddRange(args[(i + 1)..]); break; }
                words.Add(arg);
                continue;
            }
            switch (arg)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage: wl-copy [options] text to copy
                        Copy content to the clipboard (standard input if no text is given).

                        Options:
                          -o, --paste-once      Only serve one paste request and then clear the clipboard.
                          -f, --foreground      Stay in the foreground (always the case on Windows).
                          -c, --clear           Instead of copying anything, clear the clipboard.
                          -p, --primary         Use the "primary" selection (not available on Windows).
                          -r, --regular         Use the regular clipboard.
                          -n, --trim-newline    Do not copy the trailing newline character.
                          -t, --type mime/type  Override the inferred MIME type for the content.
                          -s, --seat seat-name  Accepted for compatibility (Windows has one clipboard).
                          --sensitive           Keep the content out of clipboard history and cloud sync.
                          -v, --version         Display version info.
                          -h, --help            Display this message.
                        """);
                    return 0;
                case "-v" or "--version": Version("wl-copy"); return 0;
                case "-o" or "--paste-once": once = true; break;
                case "-f" or "--foreground" or "-r" or "--regular": break;
                case "-c" or "--clear": clear = true; break;
                case "-n" or "--trim-newline": trim = true; break;
                case "--sensitive": sensitive = true; break;
                case "-p" or "--primary":
                    Console.Error.WriteLine("wl-copy: Windows has no primary selection, only the clipboard.");
                    return 1;
                case "-t" or "--type" or "-s" or "--seat":
                    if (++i >= args.Length) throw new UsageException($"option {arg} requires an argument");
                    if (arg is "-t" or "--type") type = args[i];
                    break;
                default:
                    if (arg.StartsWith("--type=")) { type = arg[7..]; break; }
                    if (arg.StartsWith("--seat=")) break;
                    throw new UsageException($"unrecognized option '{arg}'");
            }
        }
        if (clear) { Clip.Set([]); return 0; }
        byte[] data = words.Count > 0 ? Encoding.UTF8.GetBytes(string.Join(' ', words)) : ReadStdin();
        type ??= words.Count > 0 ? "text/plain;charset=utf-8" : Sniff(data);
        if (trim && IsText(type))
        {
            if (data.Length > 0 && data[^1] == '\n') data = data[..^1];
            if (data.Length > 0 && data[^1] == '\r') data = data[..^1];
        }
        var items = ItemsFor(type, data);
        if (once) return Clip.ServeOnce(items) ? 0 : 1;
        Clip.Set(items);
        return 0;
    }

    static bool sensitive;

    static int WlPaste(string[] args)
    {
        bool noNewline = false, list = false;
        string type = null;
        string[] watch = null;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage: wl-paste [options]
                        Paste content from the clipboard.

                        Options:
                          -n, --no-newline      Do not append a newline character.
                          -l, --list-types      Instead of pasting, list the offered types.
                          -p, --primary         Use the "primary" selection (not available on Windows).
                          -w, --watch command   Run a command each time the clipboard changes.
                          -t, --type mime/type  Override the inferred MIME type for the content.
                          -s, --seat seat-name  Accepted for compatibility (Windows has one clipboard).
                          -v, --version         Display version info.
                          -h, --help            Display this message.
                        """);
                    return 0;
                case "-v" or "--version": Version("wl-paste"); return 0;
                case "-n" or "--no-newline": noNewline = true; break;
                case "-l" or "--list-types": list = true; break;
                case "-p" or "--primary":
                    Console.Error.WriteLine("wl-paste: Windows has no primary selection, only the clipboard.");
                    return 1;
                case "-w" or "--watch":
                    if (i + 1 >= args.Length) throw new UsageException($"option {arg} requires an argument");
                    watch = args[(i + 1)..];
                    i = args.Length;
                    break;
                case "-t" or "--type" or "-s" or "--seat":
                    if (++i >= args.Length) throw new UsageException($"option {arg} requires an argument");
                    if (arg is "-t" or "--type") type = args[i];
                    break;
                default:
                    if (arg.StartsWith("--type=")) { type = arg[7..]; break; }
                    if (arg.StartsWith("--seat=")) break;
                    throw new UsageException($"unrecognized option '{arg}'");
            }
        }
        if (watch != null) return Watch(watch, type);
        var types = Clip.Types();
        if (list)
        {
            foreach (var t in types) Console.WriteLine(t);
            return 0;
        }
        if (types.Count == 0) { Console.Error.WriteLine("Nothing is copied"); return 1; }
        var chosen = PickType(types, type);
        if (chosen == null) { Console.Error.WriteLine("No suitable type of content copied"); return 1; }
        var data = Clip.Get(chosen);
        if (data == null) { Console.Error.WriteLine("No suitable type of content copied"); return 1; }
        using var stdout = Console.OpenStandardOutput();
        stdout.Write(data);
        if (!noNewline && IsText(chosen)) stdout.WriteByte((byte)'\n');
        return 0;
    }

    // Like wl-paste: an explicit type matches exactly or by its main part ("text" -> "text/plain");
    // otherwise text wins, then the first offered type.
    static string PickType(List<string> types, string wanted)
    {
        if (wanted == null) return types.FirstOrDefault(IsText) ?? types[0];
        return types.FirstOrDefault(t => t.Equals(wanted, StringComparison.OrdinalIgnoreCase))
            ?? types.FirstOrDefault(t => t.StartsWith(wanted + "/", StringComparison.OrdinalIgnoreCase))
            ?? (wanted.Equals("text", StringComparison.OrdinalIgnoreCase) || wanted.StartsWith("text/plain") ? types.FirstOrDefault(IsText) : null);
    }

    // wl-paste --watch: runs the command with the new contents on stdin whenever the clipboard changes
    // (CLIPBOARD_STATE is data, nil or sensitive, as in wl-paste).
    static int Watch(string[] command, string type)
    {
        var last = Clip.Sequence();
        var first = true;
        while (true)
        {
            var seq = Clip.Sequence();
            if (first || seq != last)
            {
                first = false;
                last = seq;
                var types = Clip.Types();
                var chosen = types.Count > 0 ? PickType(types, type) : null;
                var data = chosen != null ? Clip.Get(chosen) : null;
                var state = Clip.IsSensitive() ? "sensitive" : data == null ? "nil" : "data";
                var psi = new ProcessStartInfo(command[0]) { UseShellExecute = false, RedirectStandardInput = true };
                foreach (var a in command[1..]) psi.ArgumentList.Add(a);
                psi.Environment["CLIPBOARD_STATE"] = state;
                try
                {
                    using var p = Process.Start(psi);
                    try
                    {
                        if (data != null && state == "data") p.StandardInput.BaseStream.Write(data);
                        p.StandardInput.Close();
                    }
                    catch (IOException) { }
                    p.WaitForExit();
                }
                catch (System.ComponentModel.Win32Exception e)
                {
                    Console.Error.WriteLine($"wl-paste: {command[0]}: {e.Message}");
                    return 1;
                }
            }
            Thread.Sleep(200);
        }
    }

    internal static bool IsText(string type) =>
        type.StartsWith("text/", StringComparison.OrdinalIgnoreCase) && !type.StartsWith("text/uri-list") && !type.StartsWith("text/html") && !type.StartsWith("text/rtf")
        || type is "TEXT" or "STRING" or "UTF8_STRING";

    static string Sniff(byte[] data)
    {
        ReadOnlySpan<byte> d = data;
        if (d.StartsWith(Icons.PngSignature)) return "image/png";
        if (d.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF])) return "image/jpeg";
        if (d.StartsWith("GIF8"u8)) return "image/gif";
        if (d.StartsWith("BM"u8) && d.Length > 54) return "image/bmp";
        if (d.StartsWith("%PDF"u8)) return "application/pdf";
        if (d.StartsWith("{\\rtf"u8)) return "text/rtf";
        try { new UTF8Encoding(false, true).GetString(data); return "text/plain;charset=utf-8"; }
        catch (DecoderFallbackException) { return "application/octet-stream"; }
    }

    // The clipboard formats that represent one MIME type's data.
    static List<(uint Format, byte[] Data)> ItemsFor(string type, byte[] data)
    {
        var items = new List<(uint, byte[])>();
        var mime = type.Split(';')[0].Trim().ToLowerInvariant();
        if (IsText(type))
            items.Add((CF_UNICODETEXT, Clip.Unicode(Encoding.UTF8.GetString(data))));
        else if (mime == "text/html")
        {
            var html = Encoding.UTF8.GetString(data);
            items.Add((Clip.Register("HTML Format"), Clip.CfHtml(html)));
            items.Add((CF_UNICODETEXT, Clip.Unicode(html)));
        }
        else if (mime is "text/rtf" or "application/rtf")
            items.Add((Clip.Register("Rich Text Format"), [.. data, 0]));
        else if (mime == "text/uri-list")
        {
            var files = Encoding.UTF8.GetString(data).Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith('#'))
                .Select(l => Uri.TryCreate(l, UriKind.Absolute, out var u) && u.IsFile ? u.LocalPath : null).Where(p => p != null).ToList();
            if (files.Count > 0) items.AddRange(Clip.FileItems(files));
            items.Add((CF_UNICODETEXT, Clip.Unicode(Encoding.UTF8.GetString(data))));
        }
        else if (mime.StartsWith("image/"))
        {
            if (mime == "image/png") items.Add((Clip.Register("PNG"), data));
            else items.Add((Clip.Register(mime), data));
            try
            {
                var (w, h, bgra) = Icons.Decode(data);
                items.Add((CF_DIB, Clip.Dib(w, h, bgra)));
            }
            catch (InvalidOperationException) { if (mime != "image/png") throw new InvalidOperationException($"cannot decode {mime} data"); }
        }
        else items.Add((Clip.Register(mime), data));
        if (sensitive)
        {
            items.Add((Clip.Register("ExcludeClipboardContentFromMonitorProcessing"), [0, 0, 0, 0]));
            items.Add((Clip.Register("CanIncludeInClipboardHistory"), [0, 0, 0, 0]));
            items.Add((Clip.Register("CanUploadToCloudClipboard"), [0, 0, 0, 0]));
        }
        return items;
    }

    static byte[] ReadStdin()
    {
        using var stdin = Console.OpenStandardInput();
        using var ms = new MemoryStream();
        stdin.CopyTo(ms);
        return ms.ToArray();
    }

    // ---- pngpaste ----

    static int PngPaste(string[] args)
    {
        bool base64 = false;
        string output = null;
        foreach (var arg in args)
        {
            switch (arg)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage: pngpaste [OPTIONS] <dest.png>
                        Paste the clipboard image into a file. The extension picks the format: png (default),
                        gif, jpg/jpeg, tif/tiff or bmp. Use "-" as <dest.png> for standard output.
                          -b    write base64 encoded PNG to standard output
                          -v    print the version
                        """);
                    return 0;
                case "-v" or "--version": Version("pngpaste"); return 0;
                case "-b": base64 = true; break;
                default:
                    if (arg.StartsWith('-') && arg != "-") throw new UsageException($"unknown option {arg}");
                    output = arg;
                    break;
            }
        }
        if (output == null && !base64) throw new UsageException("missing destination (see pngpaste -h)");
        var image = Clip.Image();
        if (image == null)
        {
            Console.Error.WriteLine("No image data found on the clipboard, or could not convert!");
            return 1;
        }
        var format = base64 || output == "-" ? "png" : Path.GetExtension(output).ToLowerInvariant() switch
        {
            ".gif" => "gif",
            ".jpg" or ".jpeg" => "jpeg",
            ".tif" or ".tiff" => "tiff",
            ".bmp" => "bmp",
            _ => "png",
        };
        var bytes = format == "png" ? image.Value.Png ?? Icons.EncodePng(image.Value.W, image.Value.H, image.Value.Bgra)
            : Icons.Encode(format, image.Value.W, image.Value.H, image.Value.Bgra ?? Icons.Decode(image.Value.Png).Bgra);
        if (base64) Console.WriteLine(Convert.ToBase64String(bytes));
        else if (output == "-") { using var stdout = Console.OpenStandardOutput(); stdout.Write(bytes); }
        else File.WriteAllBytes(output, bytes);
        return 0;
    }

    // ---- clipper ----

    static int Clipper(string[] args)
    {
        string address = null, logfile = null, executable = null, flags = null;
        int? port = null;
        var config = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".clipper.json");
        var explicitConfig = false;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string Value(string name)
            {
                var eq = arg.IndexOf('=');
                if (eq > 0) return arg[(eq + 1)..];
                if (++i >= args.Length) throw new UsageException($"flag needs an argument: {name}");
                return args[i];
            }
            var key = arg.TrimStart('-').Split('=')[0];
            switch (key)
            {
                case "h" or "help":
                    Console.WriteLine("""
                        Usage of clipper:
                          -a, --address string      address to bind to (default "127.0.0.1"); a path means a Unix socket
                          -c, --config string       path to (JSON) config file (default "~/.clipper.json")
                          -e, --executable string   program called to write to the clipboard (default: the Windows clipboard)
                          -f, --flags string        flags to pass to the executable
                          -h, --help                show usage information
                          -l, --logfile string      path to logfile (default "~/.clipper.log")
                          -p, --port int            port to listen on (default 8377)
                          -v, --version             show version information
                        """);
                    return 0;
                case "v" or "version": Version("clipper"); return 0;
                case "a" or "address": address = Value("address"); break;
                case "c" or "config": config = Value("config"); explicitConfig = true; break;
                case "e" or "executable": executable = Value("executable"); break;
                case "f" or "flags": flags = Value("flags"); break;
                case "l" or "logfile": logfile = Value("logfile"); break;
                case "p" or "port":
                    if (!int.TryParse(Value("port"), out var p) || p is < 1 or > 65535) throw new UsageException("invalid port");
                    port = p;
                    break;
                default: throw new UsageException($"flag provided but not defined: {arg}");
            }
        }
        // Command-line flags override the config file, which overrides the defaults.
        if (File.Exists(Expand(config)))
        {
            JsonDocument doc;
            try { doc = JsonDocument.Parse(File.ReadAllText(Expand(config))); }
            catch (JsonException e) { throw new IOException($"{config}: {e.Message}"); }
            using var _ = doc;
            string S(string name) => doc.RootElement.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            address ??= S("address");
            logfile ??= S("logfile");
            executable ??= S("executable");
            flags ??= S("flags");
            if (port == null && doc.RootElement.TryGetProperty("port", out var pv) && pv.TryGetInt32(out var pp)) port = pp;
        }
        else if (explicitConfig) throw new IOException($"config file {config} not found");
        address = Expand(address ?? "127.0.0.1");
        logfile = Expand(logfile ?? "~/.clipper.log");
        using var log = new StreamWriter(File.Open(logfile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)) { AutoFlush = true };
        void Log(string message) => log.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} {message}");

        Socket listener;
        var unix = address.Contains('/') || address.Contains('\\');
        if (unix)
        {
            if (File.Exists(address)) File.Delete(address);
            listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            listener.Bind(new UnixDomainSocketEndPoint(address));
            Console.CancelKeyPress += (_, _) => { try { File.Delete(address); } catch (IOException) { } };
        }
        else
        {
            var ip = address == "localhost" ? IPAddress.Loopback : IPAddress.Parse(address);
            listener = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(ip, port ?? 8377));
        }
        listener.Listen(16);
        Log($"Starting server on {(unix ? address : listener.LocalEndPoint)}");
        while (true)
        {
            using var client = listener.Accept();
            using var stream = new NetworkStream(client);
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            var data = ms.ToArray();
            Log($"Echoing {data.Length} bytes");
            try
            {
                if (executable == null) Clip.Set([(CF_UNICODETEXT, Clip.Unicode(Encoding.UTF8.GetString(data)))]);
                else
                {
                    var psi = new ProcessStartInfo(executable) { UseShellExecute = false, RedirectStandardInput = true };
                    foreach (var f in (flags ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)) psi.ArgumentList.Add(f);
                    using var p = Process.Start(psi);
                    p.StandardInput.BaseStream.Write(data);
                    p.StandardInput.Close();
                    p.WaitForExit();
                    if (p.ExitCode != 0) Log($"{executable} exited with status {p.ExitCode}");
                }
            }
            catch (Exception e) when (e is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                Log($"error: {e.Message}");
            }
        }
    }

    static string Expand(string path) =>
        path.StartsWith('~') ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + path[1..] : path;

    // ---- clippy / pasty ----

    static int Clippy(string[] args)
    {
        if (args.Length > 0 && args[0] == "mcp-server") return McpServer();
        bool text = false, verbose = false, interactive = false, recent = false;
        string recentArg = null;
        var files = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage: clippy [options] [file...]
                        Copy files so they paste into apps as files (Explorer, Outlook, Slack, ...), or copy text.

                          clippy report.pdf         copy a file as a file
                          clippy *.jpg              copy several files
                          clippy -t notes.txt       copy a file's contents as text
                          echo hi | clippy          copy standard input (text, or an image)
                          clippy -r [N|5m|2h]       copy the most recent download(s)
                          clippy -i                 pick from recent downloads
                          clippy --clear            clear the clipboard
                          clippy mcp-server         run as an MCP server on stdin/stdout

                        Options:
                          -t, --text                copy file contents as text
                          -r, --recent [N|duration] most recent download, N downloads, or downloads newer than duration
                          -i, --interactive         choose recent downloads interactively
                          --paste                   paste instead (same as pasty)
                          --clear                   clear the clipboard
                          -v, --verbose             say what was copied
                          --version                 print the version
                        """);
                    return 0;
                case "--version": Version("clippy"); return 0;
                case "-t" or "--text": text = true; break;
                case "-v" or "--verbose": verbose = true; break;
                case "-i" or "--interactive": interactive = true; break;
                case "--clear": Clip.Set([]); return 0;
                case "--paste": return Pasty(args[(i + 1)..]);
                case "-r" or "--recent":
                    recent = true;
                    if (i + 1 < args.Length && RecentSpec(args[i + 1])) recentArg = args[++i];
                    break;
                default:
                    if (arg.StartsWith('-') && arg != "-") throw new UsageException($"unknown option {arg}");
                    files.Add(arg);
                    break;
            }
        }
        if (recent || interactive)
        {
            var downloads = RecentDownloads(recentArg, interactive ? 20 : 1);
            if (downloads.Count == 0) { Console.Error.WriteLine("clippy: no recent downloads found"); return 1; }
            if (interactive)
            {
                for (var n = 0; n < downloads.Count; n++)
                    Console.Error.WriteLine($"{n + 1,3}. {Path.GetFileName(downloads[n])}  ({Ago(File.GetLastWriteTime(downloads[n]))})");
                Console.Error.Write("Copy which (e.g. 1 or 1,3)? ");
                var answer = Console.ReadLine() ?? "";
                downloads = answer.Split(',', ' ').Where(s => int.TryParse(s, out var k) && k >= 1 && k <= downloads.Count)
                    .Select(s => downloads[int.Parse(s) - 1]).Distinct().ToList();
                if (downloads.Count == 0) return 1;
            }
            files.AddRange(downloads);
        }
        if (files.Count == 0)
        {
            if (!Console.IsInputRedirected) throw new UsageException("nothing to copy (give files or pipe text; see clippy -h)");
            var data = ReadStdin();
            Clip.Set(ItemsFor(Sniff(data), data));
            if (verbose) Console.Error.WriteLine($"Copied {data.Length} bytes from standard input");
            return 0;
        }
        var full = files.Select(Path.GetFullPath).ToList();
        foreach (var f in full.Where(f => !File.Exists(f) && !Directory.Exists(f))) throw new IOException($"{f}: No such file or directory");
        if (text)
            Clip.Set([(CF_UNICODETEXT, Clip.Unicode(string.Concat(full.Select(File.ReadAllText))))]);
        else
            Clip.Set(Clip.FileItems(full));
        if (verbose) foreach (var f in full) Console.Error.WriteLine($"Copied {(text ? "contents of " : "")}{f}");
        return 0;
    }

    static bool RecentSpec(string s) => s.Length > 0 && char.IsDigit(s[0]) && (s.All(char.IsDigit) || "smhd".Contains(s[^1]));

    static List<string> RecentDownloads(string spec, int count)
    {
        var folder = Downloads();
        if (!Directory.Exists(folder)) return [];
        var since = DateTime.MinValue;
        if (spec != null && !spec.All(char.IsDigit))
        {
            var n = double.Parse(spec[..^1]);
            since = DateTime.Now - spec[^1] switch { 's' => TimeSpan.FromSeconds(n), 'm' => TimeSpan.FromMinutes(n), 'h' => TimeSpan.FromHours(n), _ => TimeSpan.FromDays(n) };
            count = int.MaxValue;
        }
        else if (spec != null) count = int.Parse(spec);
        string[] partial = [".crdownload", ".part", ".partial", ".download", ".tmp", ".opdownload"];
        return new DirectoryInfo(folder).EnumerateFileSystemInfos()
            .Where(f => !partial.Contains(f.Extension.ToLowerInvariant()) && !f.Attributes.HasFlag(FileAttributes.Hidden) && f.Name != "desktop.ini")
            .Where(f => f.LastWriteTime >= since)
            .OrderByDescending(f => f.LastWriteTime).Take(count).Select(f => f.FullName).ToList();
    }

    static string Ago(DateTime t)
    {
        var d = DateTime.Now - t;
        return d.TotalMinutes < 1 ? "just now" : d.TotalHours < 1 ? $"{(int)d.TotalMinutes}m ago" : d.TotalDays < 1 ? $"{(int)d.TotalHours}h ago" : $"{(int)d.TotalDays}d ago";
    }

    static string Downloads()
    {
        var id = new Guid("374DE290-123F-4565-9164-39C4925E467B");
        if (SHGetKnownFolderPath(ref id, 0, IntPtr.Zero, out var path) == 0)
        {
            var result = Marshal.PtrToStringUni(path);
            Marshal.FreeCoTaskMem(path);
            return result;
        }
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    }

    static int Pasty(string[] args)
    {
        string destination = null;
        foreach (var arg in args)
        {
            switch (arg)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage: pasty [destination]
                        Paste from the clipboard: copied files are copied into destination (default: the current
                        folder); an image is saved to destination (a file, or clipboard-<time>.png in a folder);
                        text goes to standard output, or to destination if it names a file.
                        """);
                    return 0;
                case "--version": Version("pasty"); return 0;
                default:
                    if (arg.StartsWith('-')) throw new UsageException($"unknown option {arg}");
                    destination = arg;
                    break;
            }
        }
        var types = Clip.Types();
        if (types.Contains("text/uri-list") && Clip.Files() is { Count: > 0 } files)
        {
            var dir = destination ?? ".";
            Directory.CreateDirectory(dir);
            foreach (var f in files)
            {
                var target = Path.Combine(dir, Path.GetFileName(f.TrimEnd('\\')));
                if (Directory.Exists(f)) CopyDirectory(f, target);
                else File.Copy(f, target, overwrite: false);
                Console.WriteLine(target);
            }
            return 0;
        }
        if (types.Any(IsText) && (destination == null || !Directory.Exists(destination)))
        {
            var data = Clip.Get(types.First(IsText));
            if (destination != null) File.WriteAllBytes(destination, data);
            else { using var stdout = Console.OpenStandardOutput(); stdout.Write(data); }
            return 0;
        }
        if (Clip.Image() is { } image)
        {
            var file = destination == null || Directory.Exists(destination)
                ? Path.Combine(destination ?? ".", $"clipboard-{DateTime.Now:yyyyMMdd-HHmmss}.png") : destination;
            File.WriteAllBytes(file, image.Png ?? Icons.EncodePng(image.W, image.H, image.Bgra));
            Console.WriteLine(file);
            return 0;
        }
        Console.Error.WriteLine("pasty: nothing to paste");
        return 1;
    }

    static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var f in Directory.EnumerateFiles(source)) File.Copy(f, Path.Combine(target, Path.GetFileName(f)));
        foreach (var d in Directory.EnumerateDirectories(source)) CopyDirectory(d, Path.Combine(target, Path.GetFileName(d)));
    }

    // clippy mcp-server: JSON-RPC 2.0 over stdin/stdout lines, with clippy's tools. The line buffer
    // (buffer_*) lives in this process, so agents can move code between files without the clipboard.
    static int McpServer()
    {
        List<string> buffer = null;
        string bufferSource = null;
        using var stdout = Console.OpenStandardOutput();
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            if (line.Trim().Length == 0) continue;
            JsonDocument doc;
            try { doc = JsonDocument.Parse(line); }
            catch (JsonException) { Respond(stdout, null, null, -32700, "Parse error"); continue; }
            using (doc)
            {
                var root = doc.RootElement;
                var id = root.TryGetProperty("id", out var idv) ? idv.Clone() : (JsonElement?)null;
                var method = root.TryGetProperty("method", out var m) ? m.GetString() : null;
                var p = root.TryGetProperty("params", out var pv) ? pv : default;
                if (id == null) continue;   // notifications (e.g. notifications/initialized) get no reply
                switch (method)
                {
                    case "initialize":
                        var version = p.ValueKind == JsonValueKind.Object && p.TryGetProperty("protocolVersion", out var v) ? v.GetString() : "2025-03-26";
                        Respond(stdout, id, w =>
                        {
                            w.WriteString("protocolVersion", version);
                            w.WriteStartObject("capabilities"); w.WriteStartObject("tools"); w.WriteEndObject(); w.WriteEndObject();
                            w.WriteStartObject("serverInfo"); w.WriteString("name", "clippy"); w.WriteString("version", Commands.Version); w.WriteEndObject();
                        });
                        break;
                    case "ping": Respond(stdout, id, _ => { }); break;
                    case "tools/list":
                        Respond(stdout, id, w =>
                        {
                            w.WriteStartArray("tools");
                            foreach (var (name, desc, schema) in McpTools)
                            {
                                w.WriteStartObject();
                                w.WriteString("name", name);
                                w.WriteString("description", desc);
                                w.WritePropertyName("inputSchema");
                                w.WriteRawValue(schema);
                                w.WriteEndObject();
                            }
                            w.WriteEndArray();
                        });
                        break;
                    case "tools/call":
                        var tool = p.GetProperty("name").GetString();
                        var a = p.TryGetProperty("arguments", out var av) ? av : default;
                        string Arg(string n) => a.ValueKind == JsonValueKind.Object && a.TryGetProperty(n, out var x) && x.ValueKind == JsonValueKind.String ? x.GetString() : null;
                        int? Int(string n) => a.ValueKind == JsonValueKind.Object && a.TryGetProperty(n, out var x) && x.TryGetInt32(out var k) ? k : null;
                        string result;
                        var isError = false;
                        try
                        {
                            result = tool switch
                            {
                                "clipboard_copy" => McpCopy(Arg("text"), Arg("file")),
                                "clipboard_paste" => McpPaste(Arg("destination")),
                                "get_recent_downloads" => string.Join("\n", RecentDownloads(Arg("duration") ?? (Int("count") ?? 10).ToString(), 10)),
                                "buffer_copy" or "buffer_cut" => BufferTake(tool == "buffer_cut", Arg("file"), Int("start_line"), Int("end_line"), out buffer, out bufferSource),
                                "buffer_paste" => BufferPaste(buffer, Arg("file"), Int("at_line"), Arg("mode")),
                                "buffer_list" => buffer == null ? "Buffer is empty" : $"{buffer.Count} lines from {bufferSource}:\n{string.Join("\n", buffer)}",
                                _ => throw new InvalidOperationException($"unknown tool {tool}"),
                            };
                        }
                        catch (Exception e) when (e is IOException or InvalidOperationException or UnauthorizedAccessException or ArgumentException)
                        {
                            result = e.Message;
                            isError = true;
                        }
                        Respond(stdout, id, w =>
                        {
                            w.WriteStartArray("content");
                            w.WriteStartObject(); w.WriteString("type", "text"); w.WriteString("text", result); w.WriteEndObject();
                            w.WriteEndArray();
                            if (isError) w.WriteBoolean("isError", true);
                        });
                        break;
                    default: Respond(stdout, id, null, -32601, $"Method not found: {method}"); break;
                }
            }
        }
        return 0;
    }

    static readonly (string, string, string)[] McpTools =
    [
        ("clipboard_copy", "Copy text, or a file as a file reference, to the system clipboard",
            """{"type":"object","properties":{"text":{"type":"string"},"file":{"type":"string"}}}"""),
        ("clipboard_paste", "Paste clipboard text, or copied files into a destination folder",
            """{"type":"object","properties":{"destination":{"type":"string"}}}"""),
        ("get_recent_downloads", "List recently downloaded files",
            """{"type":"object","properties":{"count":{"type":"integer"},"duration":{"type":"string"}}}"""),
        ("buffer_copy", "Copy lines of a file into clippy's private buffer",
            """{"type":"object","properties":{"file":{"type":"string"},"start_line":{"type":"integer"},"end_line":{"type":"integer"}},"required":["file"]}"""),
        ("buffer_cut", "Cut lines of a file into clippy's private buffer",
            """{"type":"object","properties":{"file":{"type":"string"},"start_line":{"type":"integer"},"end_line":{"type":"integer"}},"required":["file"]}"""),
        ("buffer_paste", "Paste the buffer into a file (mode insert, append or replace)",
            """{"type":"object","properties":{"file":{"type":"string"},"at_line":{"type":"integer"},"mode":{"type":"string"}},"required":["file"]}"""),
        ("buffer_list", "Show the buffer", """{"type":"object","properties":{}}"""),
    ];

    static string McpCopy(string text, string file)
    {
        if (file != null)
        {
            var full = Path.GetFullPath(file);
            if (!File.Exists(full) && !Directory.Exists(full)) throw new IOException($"{file}: No such file or directory");
            Clip.Set(Clip.FileItems([full]));
            return $"Copied {Path.GetFileName(full)} to the clipboard";
        }
        if (text == null) throw new ArgumentException("give text or file");
        Clip.Set([(CF_UNICODETEXT, Clip.Unicode(text))]);
        return $"Copied {text.Length} characters to the clipboard";
    }

    static string McpPaste(string destination)
    {
        var files = Clip.Files();
        if (files.Count > 0)
        {
            var dir = destination ?? ".";
            Directory.CreateDirectory(dir);
            foreach (var f in files.Where(File.Exists)) File.Copy(f, Path.Combine(dir, Path.GetFileName(f)), overwrite: false);
            return $"Pasted {files.Count} file(s) into {Path.GetFullPath(dir)}";
        }
        var text = Clip.Get("text/plain;charset=utf-8");
        return text == null ? "Clipboard is empty" : Encoding.UTF8.GetString(text);
    }

    static string BufferTake(bool cut, string file, int? start, int? end, out List<string> buffer, out string source)
    {
        if (file == null) throw new ArgumentException("file is required");
        var lines = File.ReadAllText(file).Split('\n').ToList();
        int from = Math.Max(1, start ?? 1), to = Math.Min(lines.Count, end ?? lines.Count);
        if (from > to) throw new ArgumentException($"no lines {from}-{to} in {file}");
        buffer = lines.GetRange(from - 1, to - from + 1);
        source = Path.GetFileName(file);
        if (cut)
        {
            lines.RemoveRange(from - 1, to - from + 1);
            File.WriteAllText(file, string.Join("\n", lines));
        }
        return $"{(cut ? "Cut" : "Copied")} {buffer.Count} lines from {source}";
    }

    static string BufferPaste(List<string> buffer, string file, int? at, string mode)
    {
        if (buffer == null) throw new InvalidOperationException("Buffer is empty");
        if (file == null) throw new ArgumentException("file is required");
        var lines = File.Exists(file) ? File.ReadAllText(file).Split('\n').ToList() : [];
        switch (mode ?? "insert")
        {
            case "replace": lines = [.. buffer]; break;
            case "append": lines.AddRange(buffer); break;
            default: lines.InsertRange(Math.Clamp((at ?? lines.Count + 1) - 1, 0, lines.Count), buffer); break;
        }
        File.WriteAllText(file, string.Join("\n", lines));
        return $"Pasted {buffer.Count} lines into {Path.GetFileName(file)}";
    }

    static void Respond(Stream output, JsonElement? id, Action<Utf8JsonWriter> result, int errorCode = 0, string error = null)
    {
        using var ms = new MemoryStream();
        using (var w = new Utf8JsonWriter(ms))
        {
            w.WriteStartObject();
            w.WriteString("jsonrpc", "2.0");
            w.WritePropertyName("id");
            if (id is { } i) i.WriteTo(w); else w.WriteNullValue();
            if (result != null) { w.WriteStartObject("result"); result(w); w.WriteEndObject(); }
            else { w.WriteStartObject("error"); w.WriteNumber("code", errorCode); w.WriteString("message", error); w.WriteEndObject(); }
            w.WriteEndObject();
        }
        ms.WriteByte((byte)'\n');
        output.Write(ms.ToArray());
        output.Flush();
    }

    const uint CF_TEXT = 1, CF_BITMAP = 2, CF_DIB = 8, CF_UNICODETEXT = 13, CF_HDROP = 15, CF_DIBV5 = 17;

    [DllImport("shell32.dll")]
    static extern int SHGetKnownFolderPath(ref Guid id, uint flags, IntPtr token, out IntPtr path);

    // The Win32 clipboard: opening (with retries, since other programs hold it briefly), reading and
    // writing formats, and delayed rendering for --paste-once.
    internal static class Clip
    {
        public static uint Register(string name) => RegisterClipboardFormatW(name);

        public static uint Sequence() => GetClipboardSequenceNumber();

        static void Open(IntPtr owner)
        {
            for (var attempt = 0; attempt < 40; attempt++)
            {
                if (OpenClipboard(owner)) return;
                Thread.Sleep(25);
            }
            throw new IOException("could not open the clipboard (another program is holding it)");
        }

        // Replaces the clipboard contents (an empty list just clears it).
        public static void Set(List<(uint Format, byte[] Data)> items)
        {
            var owner = MessageWindow();
            Open(owner);
            try
            {
                EmptyClipboard();
                foreach (var (format, data) in items)
                    if (SetClipboardData(format, Global(data)) == IntPtr.Zero)
                        throw new IOException($"could not set clipboard data (error {Marshal.GetLastWin32Error()})");
            }
            finally { CloseClipboard(); DestroyWindow(owner); }
        }

        public static byte[] Raw(uint format)
        {
            if (!IsClipboardFormatAvailable(format)) return null;
            Open(IntPtr.Zero);
            try
            {
                var handle = GetClipboardData(format);
                if (handle == IntPtr.Zero) return null;
                var size = (int)GlobalSize(handle);
                var ptr = GlobalLock(handle);
                if (ptr == IntPtr.Zero) return null;
                try
                {
                    var bytes = new byte[size];
                    Marshal.Copy(ptr, bytes, 0, size);
                    return bytes;
                }
                finally { GlobalUnlock(handle); }
            }
            finally { CloseClipboard(); }
        }

        static List<uint> Formats()
        {
            var list = new List<uint>();
            Open(IntPtr.Zero);
            try { for (uint f = 0; (f = EnumClipboardFormats(f)) != 0;) list.Add(f); }
            finally { CloseClipboard(); }
            return list;
        }

        static unsafe string FormatName(uint format)
        {
            var buffer = stackalloc char[256];
            var length = GetClipboardFormatNameW(format, buffer, 256);
            return length > 0 ? new string(buffer, 0, length) : null;
        }

        public static bool IsSensitive() =>
            IsClipboardFormatAvailable(Register("ExcludeClipboardContentFromMonitorProcessing"));

        // Offered MIME types, in wl-paste's vocabulary.
        public static List<string> Types()
        {
            var types = new List<string>();
            void Add(params string[] names) { foreach (var n in names) if (!types.Contains(n)) types.Add(n); }
            foreach (var f in Formats())
            {
                switch (f)
                {
                    case CF_UNICODETEXT or CF_TEXT: Add("text/plain;charset=utf-8", "text/plain", "UTF8_STRING", "STRING", "TEXT"); break;
                    case CF_DIB or CF_DIBV5 or CF_BITMAP: Add("image/png", "image/bmp"); break;
                    case CF_HDROP: Add("text/uri-list"); break;
                    default:
                        var name = FormatName(f);
                        if (name == null) break;
                        if (name == "PNG") Add("image/png");
                        else if (name == "HTML Format") Add("text/html");
                        else if (name == "Rich Text Format") Add("text/rtf");
                        else if (name.Contains('/') && !name.Contains(' ')) Add(name);
                        break;
                }
            }
            return types;
        }

        // The clipboard's data as the given MIME type, or null.
        public static byte[] Get(string type)
        {
            var mime = type.Split(';')[0].Trim().ToLowerInvariant();
            if (IsText(type))
            {
                var raw = Raw(CF_UNICODETEXT);
                if (raw == null) return null;
                var text = Encoding.Unicode.GetString(raw);
                var nul = text.IndexOf('\0');
                return Encoding.UTF8.GetBytes(nul >= 0 ? text[..nul] : text);
            }
            switch (mime)
            {
                case "image/png":
                    var image = Image();
                    return image == null ? null : image.Value.Png ?? Icons.EncodePng(image.Value.W, image.Value.H, image.Value.Bgra);
                case "image/bmp":
                    var dib = Raw(CF_DIB);
                    if (dib == null) return null;
                    // BITMAPFILEHEADER in front of the DIB; pixels start after header, masks and palette.
                    var bmp = new byte[14 + dib.Length];
                    bmp[0] = (byte)'B'; bmp[1] = (byte)'M';
                    BitConverter.TryWriteBytes(bmp.AsSpan(2), bmp.Length);
                    BitConverter.TryWriteBytes(bmp.AsSpan(10), 14 + PixelOffset(dib));
                    dib.CopyTo(bmp, 14);
                    return bmp;
                case "text/uri-list":
                    var files = Files();
                    return files.Count == 0 ? null : Encoding.UTF8.GetBytes(string.Concat(files.Select(f => new Uri(f).AbsoluteUri + "\r\n")));
                case "text/html":
                    var html = Raw(Register("HTML Format"));
                    return html == null ? null : HtmlFragment(html);
                case "text/rtf":
                    return Raw(Register("Rich Text Format")) is { } rtf ? rtf.AsSpan().TrimEnd((byte)0).ToArray() : null;
                default:
                    return Raw(Register(mime));
            }
        }

        public static List<string> Files()
        {
            var raw = Raw(CF_HDROP);
            if (raw == null || raw.Length < 20) return [];
            var offset = BitConverter.ToInt32(raw, 0);
            var wide = BitConverter.ToInt32(raw, 16) != 0;
            var list = wide ? Encoding.Unicode.GetString(raw, offset, raw.Length - offset) : Encoding.Default.GetString(raw, offset, raw.Length - offset);
            return list.Split('\0').TakeWhile(s => s.Length > 0).ToList();
        }

        // CF_HDROP (DROPFILES + double-NUL-terminated paths) plus "Preferred DropEffect: copy".
        public static List<(uint, byte[])> FileItems(List<string> paths)
        {
            var names = Encoding.Unicode.GetBytes(string.Concat(paths.Select(p => p + "\0")) + "\0");
            var drop = new byte[20 + names.Length];
            BitConverter.TryWriteBytes(drop.AsSpan(0), 20);
            BitConverter.TryWriteBytes(drop.AsSpan(16), 1);
            names.CopyTo(drop, 20);
            var text = Unicode(string.Join("\r\n", paths));
            return [(CF_HDROP, drop), (Register("Preferred DropEffect"), BitConverter.GetBytes(1)), (CF_UNICODETEXT, text)];
        }

        // The clipboard image: original PNG bytes when an app offered them, else the decoded DIB.
        public static (byte[] Png, int W, int H, byte[] Bgra)? Image()
        {
            if (Raw(Register("PNG")) is { } png && png.AsSpan().StartsWith(Icons.PngSignature)) return (png, 0, 0, null);
            var dib = Raw(CF_DIBV5) ?? Raw(CF_DIB);
            if (dib == null) return null;
            var decoded = DecodeDib(dib);
            return decoded == null ? null : (null, decoded.Value.W, decoded.Value.H, decoded.Value.Bgra);
        }

        static int PixelOffset(byte[] dib)
        {
            var headerSize = BitConverter.ToInt32(dib, 0);
            var bits = BitConverter.ToUInt16(dib, 14);
            var compression = BitConverter.ToInt32(dib, 16);
            var used = BitConverter.ToInt32(dib, 32);
            var palette = bits <= 8 ? (used > 0 ? used : 1 << bits) : used;
            var masks = headerSize == 40 && compression is 3 or 6 ? (compression == 6 ? 16 : 12) : 0;
            var offset = headerSize + masks + palette * 4;
            // Some programs put the colour masks after a V5 header too; trust the image size if it says so.
            var width = BitConverter.ToInt32(dib, 4);
            var height = Math.Abs(BitConverter.ToInt32(dib, 8));
            var stride = (width * bits + 31) / 32 * 4;
            if (headerSize > 40 && compression == 3 && dib.Length - offset - 12 == stride * height) offset += 12;
            return offset;
        }

        // Uncompressed DIBs (1/4/8-bit palette, 16/24/32-bit) to straight BGRA, top-down.
        static (int W, int H, byte[] Bgra)? DecodeDib(byte[] dib)
        {
            if (dib.Length < 40) return null;
            var headerSize = BitConverter.ToInt32(dib, 0);
            var width = BitConverter.ToInt32(dib, 4);
            var rawHeight = BitConverter.ToInt32(dib, 8);
            var height = Math.Abs(rawHeight);
            var bits = BitConverter.ToUInt16(dib, 14);
            var compression = BitConverter.ToInt32(dib, 16);
            if (width <= 0 || height == 0 || compression is not (0 or 3 or 6)) return null;
            uint rMask = 0xFF0000, gMask = 0xFF00, bMask = 0xFF, aMask = 0;
            if (compression is 3 or 6)
            {
                // The masks follow a 40-byte header, or sit at the same offset inside a V4/V5 header.
                rMask = BitConverter.ToUInt32(dib, 40); gMask = BitConverter.ToUInt32(dib, 44); bMask = BitConverter.ToUInt32(dib, 48);
                if (headerSize > 52 || compression == 6) aMask = BitConverter.ToUInt32(dib, 52);
            }
            else if (bits == 16) { rMask = 0x7C00; gMask = 0x3E0; bMask = 0x1F; }
            else if (bits == 32) aMask = 0xFF000000;
            var used = BitConverter.ToInt32(dib, 32);
            var paletteCount = bits <= 8 ? (used > 0 ? used : 1 << bits) : 0;
            var paletteAt = headerSize + (headerSize == 40 && compression is 3 or 6 ? (compression == 6 ? 16 : 12) : 0);
            var offset = PixelOffset(dib);
            var stride = (width * bits + 31) / 32 * 4;
            if (offset + (long)stride * height > dib.Length) return null;
            var output = new byte[width * height * 4];
            var anyAlpha = false;
            for (var y = 0; y < height; y++)
            {
                var row = offset + (rawHeight > 0 ? height - 1 - y : y) * stride;
                for (var x = 0; x < width; x++)
                {
                    byte r, g, b, a = 255;
                    if (bits <= 8)
                    {
                        var index = (dib[row + x * bits / 8] >> (8 - bits - x * bits % 8)) & ((1 << bits) - 1);
                        if (index >= paletteCount) index = 0;
                        var p = paletteAt + index * 4;
                        b = dib[p]; g = dib[p + 1]; r = dib[p + 2];
                    }
                    else if (bits == 24) { var p = row + x * 3; b = dib[p]; g = dib[p + 1]; r = dib[p + 2]; }
                    else if (bits is 16 or 32)
                    {
                        var v = bits == 16 ? BitConverter.ToUInt16(dib, row + x * 2) : BitConverter.ToUInt32(dib, row + x * 4);
                        r = Channel(v, rMask); g = Channel(v, gMask); b = Channel(v, bMask);
                        if (aMask != 0) { a = Channel(v, aMask); anyAlpha |= a != 0; }
                    }
                    else return null;
                    var o = (y * width + x) * 4;
                    output[o] = b; output[o + 1] = g; output[o + 2] = r; output[o + 3] = a;
                }
            }
            // 32-bit DIBs often leave the alpha byte at zero: that means opaque, not invisible.
            if (aMask != 0 && !anyAlpha) for (var i = 3; i < output.Length; i += 4) output[i] = 255;
            return (width, height, output);
        }

        static byte Channel(uint value, uint mask)
        {
            if (mask == 0) return 0;
            var shift = System.Numerics.BitOperations.TrailingZeroCount(mask);
            var max = mask >> shift;
            return (byte)(((value & mask) >> shift) * 255 / max);
        }

        // A bottom-up 32-bit BI_RGB DIB (Windows synthesises CF_DIBV5 and CF_BITMAP from it).
        public static byte[] Dib(int w, int h, byte[] bgra)
        {
            var dib = new byte[40 + w * h * 4];
            BitConverter.TryWriteBytes(dib.AsSpan(0), 40);
            BitConverter.TryWriteBytes(dib.AsSpan(4), w);
            BitConverter.TryWriteBytes(dib.AsSpan(8), h);
            BitConverter.TryWriteBytes(dib.AsSpan(12), (ushort)1);
            BitConverter.TryWriteBytes(dib.AsSpan(14), (ushort)32);
            BitConverter.TryWriteBytes(dib.AsSpan(20), w * h * 4);
            for (var y = 0; y < h; y++) System.Buffer.BlockCopy(bgra, (h - 1 - y) * w * 4, dib, 40 + y * w * 4, w * 4);
            return dib;
        }

        public static byte[] Unicode(string text) => Encoding.Unicode.GetBytes(text + "\0");

        // CF_HTML: a header of byte offsets in front of the UTF-8 document.
        public static byte[] CfHtml(string html)
        {
            const string header = "Version:0.9\r\nStartHTML:{0:D10}\r\nEndHTML:{1:D10}\r\nStartFragment:{2:D10}\r\nEndFragment:{3:D10}\r\n";
            var hasBody = html.Contains("<body", StringComparison.OrdinalIgnoreCase);
            var prefix = hasBody ? "" : "<html><body>";
            var suffix = hasBody ? "" : "</body></html>";
            var headerLength = Encoding.UTF8.GetByteCount(string.Format(header, 0, 0, 0, 0));
            var start = headerLength;
            var fragStart = start + Encoding.UTF8.GetByteCount(prefix + "<!--StartFragment-->");
            var fragEnd = fragStart + Encoding.UTF8.GetByteCount(html);
            var end = fragEnd + Encoding.UTF8.GetByteCount("<!--EndFragment-->" + suffix);
            var doc = string.Format(header, start, end, fragStart, fragEnd) + prefix + "<!--StartFragment-->" + html + "<!--EndFragment-->" + suffix;
            return [.. Encoding.UTF8.GetBytes(doc), 0];
        }

        static byte[] HtmlFragment(byte[] cfHtml)
        {
            var text = Encoding.UTF8.GetString(cfHtml).TrimEnd('\0');
            int Offset(string key)
            {
                var at = text.IndexOf(key + ":", StringComparison.Ordinal);
                if (at < 0) return -1;
                var end = text.IndexOf('\n', at);
                return int.TryParse(text[(at + key.Length + 1)..end].Trim(), out var v) ? v : -1;
            }
            int start = Offset("StartFragment"), stop = Offset("EndFragment");
            var bytes = Encoding.UTF8.GetBytes(text);
            if (start < 0 || stop < start || stop > bytes.Length)
            {
                start = Offset("StartHTML"); stop = Offset("EndHTML");
                if (start < 0 || stop < start || stop > bytes.Length) return bytes;
            }
            return bytes[start..stop];
        }

        static IntPtr Global(byte[] data)
        {
            var handle = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)Math.Max(1, data.Length));
            if (handle == IntPtr.Zero) throw new IOException("out of memory");
            var ptr = GlobalLock(handle);
            Marshal.Copy(data, 0, ptr, data.Length);
            GlobalUnlock(handle);
            return handle;
        }

        static IntPtr MessageWindow() => CreateWindowExW(0, "STATIC", null, 0, 0, 0, 0, 0, HWND_MESSAGE, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        // --paste-once: offer the formats with delayed rendering, hand them out on the first request,
        // then clear the clipboard. Clipboard monitors are told to keep out so they don't use up the paste.
        static List<(uint Format, byte[] Data)> pending;
        static bool served;

        public static unsafe bool ServeOnce(List<(uint Format, byte[] Data)> items)
        {
            pending = [.. items, (Register("ExcludeClipboardContentFromMonitorProcessing"), [0, 0, 0, 0])];
            var window = MessageWindow();
            SetWindowLongPtrW(window, GWLP_WNDPROC, (IntPtr)(delegate* unmanaged<IntPtr, uint, IntPtr, IntPtr, IntPtr>)&WndProc);
            Open(window);
            EmptyClipboard();
            foreach (var (format, data) in pending)
                SetClipboardData(format, format == pending[^1].Format ? Global(data) : IntPtr.Zero);
            CloseClipboard();
            while (GetMessageW(out var msg, IntPtr.Zero, 0, 0) > 0) DispatchMessageW(ref msg);
            DestroyWindow(window);
            return true;
        }

        [UnmanagedCallersOnly]
        static IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WM_RENDERFORMAT:
                    var format = (uint)wParam;
                    foreach (var (f, data) in pending)
                        if (f == format) SetClipboardData(f, Global(data));
                    if (!served) { served = true; PostMessageW(hwnd, WM_APP, IntPtr.Zero, IntPtr.Zero); }
                    return IntPtr.Zero;
                case WM_APP:
                    // Give the pasting program a moment to read the other formats it asked for.
                    Thread.Sleep(500);
                    if (OpenClipboard(hwnd)) { EmptyClipboard(); CloseClipboard(); }
                    PostQuitMessage(0);
                    return IntPtr.Zero;
                case WM_RENDERALLFORMATS or WM_DESTROYCLIPBOARD:
                    PostQuitMessage(0);
                    return IntPtr.Zero;
            }
            return DefWindowProcW(hwnd, msg, wParam, lParam);
        }

        const uint GMEM_MOVEABLE = 2;
        const uint WM_RENDERFORMAT = 0x305, WM_RENDERALLFORMATS = 0x306, WM_DESTROYCLIPBOARD = 0x307, WM_APP = 0x8000;
        const int GWLP_WNDPROC = -4;
        static readonly IntPtr HWND_MESSAGE = -3;

        [StructLayout(LayoutKind.Sequential)]
        struct MSG { public IntPtr hwnd; public uint message; public IntPtr wParam, lParam; public uint time; public int x, y; }

        [DllImport("user32.dll", SetLastError = true)] static extern bool OpenClipboard(IntPtr owner);
        [DllImport("user32.dll")] static extern bool CloseClipboard();
        [DllImport("user32.dll")] static extern bool EmptyClipboard();
        [DllImport("user32.dll", SetLastError = true)] static extern IntPtr SetClipboardData(uint format, IntPtr data);
        [DllImport("user32.dll")] static extern IntPtr GetClipboardData(uint format);
        [DllImport("user32.dll")] static extern bool IsClipboardFormatAvailable(uint format);
        [DllImport("user32.dll")] static extern uint EnumClipboardFormats(uint format);
        [DllImport("user32.dll")] static extern uint GetClipboardSequenceNumber();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern uint RegisterClipboardFormatW(string name);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern unsafe int GetClipboardFormatNameW(uint format, char* name, int max);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr CreateWindowExW(uint exStyle, string cls, string name, uint style, int x, int y, int w, int h, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);
        [DllImport("user32.dll")] static extern bool DestroyWindow(IntPtr hwnd);
        [DllImport("user32.dll")] static extern IntPtr SetWindowLongPtrW(IntPtr hwnd, int index, IntPtr value);
        [DllImport("user32.dll")] static extern IntPtr DefWindowProcW(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] static extern int GetMessageW(out MSG msg, IntPtr hwnd, uint min, uint max);
        [DllImport("user32.dll")] static extern IntPtr DispatchMessageW(ref MSG msg);
        [DllImport("user32.dll")] static extern bool PostMessageW(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] static extern void PostQuitMessage(int code);
        [DllImport("kernel32.dll")] static extern IntPtr GlobalAlloc(uint flags, UIntPtr bytes);
        [DllImport("kernel32.dll")] static extern IntPtr GlobalLock(IntPtr handle);
        [DllImport("kernel32.dll")] static extern bool GlobalUnlock(IntPtr handle);
        [DllImport("kernel32.dll")] static extern UIntPtr GlobalSize(IntPtr handle);
    }
}
