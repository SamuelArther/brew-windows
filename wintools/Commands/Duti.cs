using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace WinTools;

// duti / defaultbrowser / infat / utiluti: read default apps for file types and URL schemes, and set them.
// Reading uses the shell's own resolution (AssocQueryString, which honours the user's choice) plus the
// RegisteredApplications capabilities apps declare. Windows protects default-app choices with a per-user
// hash and lets only the user change them, so "set" opens Settings > Default apps on the chosen app's
// page to confirm - the counterpart of the confirmation dialog macOS shows for a new default browser.
static unsafe class Duti
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("duti", "Select default apps for file types and URL schemes", RunDuti);
        Commands.Add("defaultbrowser", "Get and set the default web browser", RunDefaultBrowser);
        Commands.Add("infat", "Set default openers for file formats and URL schemes", RunInfat);
        Commands.Add("utiluti", "Work with default apps for file types and URL schemes", RunUtiluti);
    }

    // ---- associations ----------------------------------------------------------------------------

    internal sealed class AppReg
    {
        public string Name, Display, Hive, Exe;
        public Dictionary<string, string> Files = new(StringComparer.OrdinalIgnoreCase);   // .ext -> ProgId
        public Dictionary<string, string> Urls = new(StringComparer.OrdinalIgnoreCase);    // scheme -> ProgId
    }

    // Apps that declare their file types and schemes (what Settings > Default apps lists).
    internal static List<AppReg> RegisteredApps()
    {
        var apps = new List<AppReg>();
        foreach (var (hive, root) in new[] { ("HKCU", Registry.CurrentUser), ("HKLM", Registry.LocalMachine) })
        {
            using var list = root.OpenSubKey(@"Software\RegisteredApplications");
            if (list == null) continue;
            foreach (var name in list.GetValueNames())
            {
                if (list.GetValue(name) is not string path || apps.Any(a => a.Name == name)) continue;
                using var caps = root.OpenSubKey(path);
                if (caps == null) continue;
                var app = new AppReg { Name = name, Hive = hive, Display = Indirect(caps.GetValue("ApplicationName") as string) ?? name };
                foreach (var (sub, map) in new[] { ("FileAssociations", app.Files), ("URLAssociations", app.Urls) })
                {
                    using var key = caps.OpenSubKey(sub);
                    if (key == null) continue;
                    foreach (var v in key.GetValueNames()) if (key.GetValue(v) is string progId) map[v] = progId;
                }
                var anyProgId = app.Urls.Values.Concat(app.Files.Values).FirstOrDefault();
                app.Exe = anyProgId == null ? null : Query(anyProgId, ASSOCSTR_EXECUTABLE);
                // Packaged apps registered here go by their AUMID, which Settings also understands.
                using var application = anyProgId == null ? null : Registry.ClassesRoot.OpenSubKey($@"{anyProgId}\Application");
                if (application?.GetValue("AppUserModelID") is string aumid)
                {
                    if (apps.Any(a => a.Name == aumid)) continue;
                    (app.Name, app.Hive) = (aumid, "AUMID");
                }
                apps.Add(app);
            }
        }
        apps.AddRange(PackagedApps(apps));
        return apps;
    }

    // Store/MSIX apps declare their capabilities in the AppModel repository; their id is the AUMID
    // (e.g. Microsoft.WindowsNotepad_8wekyb3d8bbwe!App), the closest thing Windows has to a bundle id.
    static IEnumerable<AppReg> PackagedApps(List<AppReg> known)
    {
        using var packages = Registry.ClassesRoot.OpenSubKey(@"Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages");
        if (packages == null) yield break;
        foreach (var package in packages.GetSubKeyNames())
        {
            using var pkg = packages.OpenSubKey(package);
            if (pkg == null) continue;
            foreach (var appId in pkg.GetSubKeyNames())
            {
                using var caps = pkg.OpenSubKey($@"{appId}\Capabilities");
                if (caps == null) continue;
                var app = new AppReg { Hive = "AUMID" };
                foreach (var (sub, map) in new[] { ("FileAssociations", app.Files), ("URLAssociations", app.Urls) })
                {
                    using var key = caps.OpenSubKey(sub);
                    if (key == null) continue;
                    foreach (var v in key.GetValueNames()) if (key.GetValue(v) is string progId) map[v] = progId;
                }
                var anyProgId = app.Urls.Values.Concat(app.Files.Values).FirstOrDefault();
                if (anyProgId == null) continue;
                using var application = Registry.ClassesRoot.OpenSubKey($@"{anyProgId}\Application");
                if (application?.GetValue("AppUserModelID") is not string aumid || known.Any(k => k.Name == aumid)) continue;
                app.Name = aumid;
                app.Display = Indirect(caps.GetValue("ApplicationName") as string ?? application.GetValue("ApplicationName") as string) ?? aumid;
                if (app.Display.StartsWith('@')) app.Display = Query(anyProgId, ASSOCSTR_FRIENDLYAPPNAME) ?? aumid;
                app.Exe = Query(anyProgId, ASSOCSTR_EXECUTABLE);
                yield return app;
            }
        }
    }

    // "@%SystemRoot%\system32\shell32.dll,-22067" style names.
    static string Indirect(string value)
    {
        if (string.IsNullOrEmpty(value) || value[0] != '@') return value;
        var buffer = stackalloc char[512];
        return SHLoadIndirectString(value, buffer, 512, IntPtr.Zero) == 0 ? new string(buffer) : value;
    }

    internal static string Query(string assoc, int what)
    {
        uint length = 1024;
        var buffer = stackalloc char[1024];
        var hr = AssocQueryStringW(ASSOCF_NOTRUNCATE | ASSOCF_INIT_IGNOREUNKNOWN, what, assoc, null, buffer, ref length);
        if (hr != 0) return null;
        var text = new string(buffer);
        return text.Length == 0 ? null : text;
    }

    // ".txt", "txt", "public.plain-text" -> ".txt"; "http", "mailto:" -> scheme.
    static readonly Dictionary<string, string> Utis = new(StringComparer.OrdinalIgnoreCase)
    {
        ["public.plain-text"] = ".txt", ["public.text"] = ".txt", ["public.utf8-plain-text"] = ".txt", ["public.html"] = ".html",
        ["public.xml"] = ".xml", ["public.json"] = ".json", ["com.adobe.pdf"] = ".pdf", ["public.jpeg"] = ".jpg", ["public.png"] = ".png",
        ["com.compuserve.gif"] = ".gif", ["public.tiff"] = ".tif", ["public.svg-image"] = ".svg", ["com.microsoft.bmp"] = ".bmp",
        ["public.mp3"] = ".mp3", ["public.mpeg-4"] = ".mp4", ["public.mpeg-4-audio"] = ".m4a", ["com.apple.quicktime-movie"] = ".mov",
        ["public.avi"] = ".avi", ["com.microsoft.waveform-audio"] = ".wav", ["public.zip-archive"] = ".zip", ["public.rtf"] = ".rtf",
        ["net.daringfireball.markdown"] = ".md", ["public.python-script"] = ".py", ["public.shell-script"] = ".sh", ["public.css"] = ".css",
        ["com.netscape.javascript-source"] = ".js", ["public.comma-separated-values-text"] = ".csv", ["public.csv"] = ".csv",
        ["org.openxmlformats.wordprocessingml.document"] = ".docx", ["org.openxmlformats.spreadsheetml.sheet"] = ".xlsx",
        ["org.openxmlformats.presentationml.presentation"] = ".pptx", ["public.yaml"] = ".yaml", ["public.log"] = ".log",
        ["public.c-source"] = ".c", ["public.c-plus-plus-source"] = ".cpp", ["public.c-header"] = ".h", ["com.sun.java-source"] = ".java",
    };

    internal sealed record Target(string Value, bool IsScheme)
    {
        public override string ToString() => IsScheme ? Value + ":" : Value;
    }

    static Target ParseTarget(string text, bool preferScheme = false)
    {
        var t = text.Trim();
        if (t.EndsWith(':')) return new Target(t.TrimEnd(':').ToLowerInvariant(), true);
        if (t.StartsWith('.')) return new Target(t.ToLowerInvariant(), false);
        if (Utis.TryGetValue(t, out var ext)) return new Target(ext, false);
        if (preferScheme || IsScheme(t)) return new Target(t.ToLowerInvariant(), true);
        if (t.Contains('.')) return new Target(t, false);   // a UTI Windows has no type for: nothing handles it
        return new Target("." + t.ToLowerInvariant(), false);
    }

    static bool IsScheme(string name)
    {
        using var key = Registry.ClassesRoot.OpenSubKey(name);
        return key?.GetValue("URL Protocol") != null;
    }

    sealed record Handler(string Id, string Display, string Exe, string ProgId);

    // The effective default for a type or scheme, as Explorer would open it.
    static Handler Default(Target target, List<AppReg> apps)
    {
        var progId = Query(target.Value, ASSOCSTR_PROGID);
        var exe = Query(target.Value, ASSOCSTR_EXECUTABLE);
        if (progId == null && exe == null) return null;
        var display = Query(target.Value, ASSOCSTR_FRIENDLYAPPNAME);
        var app = apps.FirstOrDefault(a => progId != null && (target.IsScheme ? a.Urls : a.Files).TryGetValue(target.Value, out var p) && p.Equals(progId, StringComparison.OrdinalIgnoreCase))
                  ?? apps.FirstOrDefault(a => exe != null && a.Exe != null && a.Exe.Equals(exe, StringComparison.OrdinalIgnoreCase));
        return new Handler(app?.Name ?? progId ?? Path.GetFileNameWithoutExtension(exe), app?.Display ?? display ?? progId, exe, progId);
    }

    // Every app that can handle a type or scheme: declared capabilities plus the type's OpenWith lists.
    static List<Handler> Handlers(Target target, List<AppReg> apps)
    {
        var result = new List<Handler>();
        void Add(Handler h) { if (h != null && !result.Any(r => r.Id.Equals(h.Id, StringComparison.OrdinalIgnoreCase))) result.Add(h); }
        Add(Default(target, apps));
        foreach (var app in apps)
            if ((target.IsScheme ? app.Urls : app.Files).TryGetValue(target.Value, out var progId)) Add(new Handler(app.Name, app.Display, app.Exe, progId));
        if (!target.IsScheme)
        {
            foreach (var root in new[] { Registry.ClassesRoot.OpenSubKey($@"{target.Value}\OpenWithProgids"),
                                         Registry.CurrentUser.OpenSubKey($@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{target.Value}\OpenWithProgids") })
            {
                using var key = root;
                if (key == null) continue;
                foreach (var progId in key.GetValueNames().Where(v => v.Length > 0))
                    if (Query(progId, ASSOCSTR_EXECUTABLE) is string exe) Add(new Handler(progId, Query(progId, ASSOCSTR_FRIENDLYAPPNAME) ?? progId, exe, progId));
            }
            using var list = Registry.ClassesRoot.OpenSubKey($@"{target.Value}\OpenWithList");
            foreach (var app in list?.GetSubKeyNames() ?? [])
                if (Query($@"Applications\{app}", ASSOCSTR_EXECUTABLE) is string exe) Add(new Handler(app, Query($@"Applications\{app}", ASSOCSTR_FRIENDLYAPPNAME) ?? app, exe, $@"Applications\{app}"));
        }
        return result;
    }

    // An app by registered name, display name, exe name or path, or a ProgId it registers.
    static AppReg FindApp(string id, List<AppReg> apps)
    {
        var bare = Path.GetFileNameWithoutExtension(id);
        return apps.FirstOrDefault(a => a.Name.Equals(id, StringComparison.OrdinalIgnoreCase))
            ?? apps.FirstOrDefault(a => a.Display.Equals(id, StringComparison.OrdinalIgnoreCase))
            ?? apps.FirstOrDefault(a => a.Exe != null && (a.Exe.Equals(id, StringComparison.OrdinalIgnoreCase) || Path.GetFileNameWithoutExtension(a.Exe).Equals(bare, StringComparison.OrdinalIgnoreCase)))
            ?? apps.FirstOrDefault(a => a.Files.Values.Concat(a.Urls.Values).Contains(id, StringComparer.OrdinalIgnoreCase))
            ?? apps.FirstOrDefault(a => a.Display.Contains(id, StringComparison.OrdinalIgnoreCase) || a.Name.Contains(id, StringComparison.OrdinalIgnoreCase));
    }

    // Windows lets only the user change defaults: open the app's page in Settings > Default apps.
    static int RequestDefault(AppReg app, IEnumerable<Target> targets, string tool)
    {
        var list = targets.ToList();
        var declared = list.Where(t => (t.IsScheme ? app.Urls : app.Files).ContainsKey(t.Value)).ToList();
        foreach (var t in list.Except(declared))
            Console.Error.WriteLine($"{tool}: {app.Display} doesn't register {t}, so Windows won't offer it there");
        if (declared.Count == 0) return 1;
        var apps = RegisteredApps();
        var pending = declared.Where(t => Default(t, apps)?.Id != app.Name).ToList();
        if (pending.Count == 0) return 0;   // already the default
        var parameter = app.Hive switch { "HKCU" => "registeredAppUser", "AUMID" => "registeredAUMID", _ => "registeredAppMachine" };
        var page = Environment.OSVersion.Version.Build >= 22000 ? $"ms-settings:defaultapps?{parameter}={Uri.EscapeDataString(app.Name)}" : "ms-settings:defaultapps";
        Console.WriteLine($"Windows asks you to confirm default apps: opening Settings > Default apps > {app.Display}. Choose it for {string.Join(", ", pending)}.");
        Process.Start(new ProcessStartInfo(page) { UseShellExecute = true });
        return 0;
    }

    // ---- defaultbrowser --------------------------------------------------------------------------

    static int RunDefaultBrowser(string[] args)
    {
        if (args.Length > 0 && args[0] is "-h" or "--help")
        {
            Console.WriteLine("Usage: defaultbrowser [<browser>]\nWithout arguments lists browsers (* marks the default). With a name (e.g. chrome, firefox, msedge)\nopens Settings to confirm it as the default browser, as Windows requires.");
            return 0;
        }
        if (args.Length > 1) throw new UsageException("too many arguments");
        var apps = RegisteredApps();
        var browsers = apps.Where(a => a.Urls.ContainsKey("http") && a.Exe != null)
            .Select(a => (Short: Path.GetFileNameWithoutExtension(a.Exe).ToLowerInvariant(), App: a)).DistinctBy(b => b.Short).OrderBy(b => b.Short).ToList();
        var current = Default(new Target("http", true), apps);
        if (args.Length == 0)
        {
            foreach (var (name, app) in browsers) Console.WriteLine($"{(current?.Id == app.Name ? "*" : " ")} {name}");
            return 0;
        }
        var wanted = args[0].ToLowerInvariant();
        var match = browsers.FirstOrDefault(b => b.Short == wanted).App ?? FindApp(args[0], browsers.Select(b => b.App).ToList());
        if (match == null)
        {
            Console.Error.WriteLine($"{args[0]} is not available as an HTTP handler");
            return 1;
        }
        return RequestDefault(match, [new Target("http", true), new Target("https", true)], "defaultbrowser");
    }

    // ---- duti ------------------------------------------------------------------------------------

    static int RunDuti(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            Console.WriteLine("""
                usage: duti [ -hvV ] [ -d uti ] [ -l uti ] [ -s id { uti | url_scheme } [ role ] ] [ -x extension ] [ settings_path ]
                  -d uti         Print the default handler of a type (uti, .ext) or url scheme
                  -l uti         List every handler
                  -s id type     Make id the default (opens Settings to confirm, as Windows requires)
                  -x ext         Print the default app's name, path and id for an extension
                  settings_path  Apply "id<TAB>type<TAB>role" / "id<TAB>scheme" lines
                ids are registered app names (Settings > Default apps), program names (notepad) or ProgIds.
                Types are extensions (.txt or txt), common UTIs (public.plain-text) or url schemes (http).
                """);
            return args.Length == 0 ? 2 : 0;
        }
        var apps = RegisteredApps();
        switch (args[0])
        {
            case "-V": Console.WriteLine(Commands.Version); return 0;
            case "-x":
                if (args.Length != 2) throw new UsageException("-x needs an extension");
                var ext = ParseTarget(args[1].StartsWith('.') ? args[1] : "." + args[1]);
                var handler = Default(ext, apps);
                if (handler == null) { Console.Error.WriteLine($"{args[1]}: no default handler"); return 1; }
                Console.WriteLine(handler.Display);
                Console.WriteLine(handler.Exe);
                Console.WriteLine(handler.Id);
                return 0;
            case "-d":
                if (args.Length != 2) throw new UsageException("-d needs a type or url scheme");
                var d = Default(ParseTarget(args[1]), apps);
                if (d == null) { Console.Error.WriteLine($"{args[1]}: no default handler"); return 1; }
                Console.WriteLine(d.Id);
                return 0;
            case "-l":
                if (args.Length != 2) throw new UsageException("-l needs a type or url scheme");
                var all = Handlers(ParseTarget(args[1]), apps);
                if (all.Count == 0) { Console.Error.WriteLine($"{args[1]}: no handlers"); return 1; }
                foreach (var h in all) Console.WriteLine(h.Id);
                return 0;
            case "-s":
                if (args.Length is < 3 or > 4) throw new UsageException("-s needs an id and a type or url scheme");
                var app = FindApp(args[1], apps) ?? throw new InvalidOperationException($"{args[1]}: no such registered application");
                return RequestDefault(app, [ParseTarget(args[2], preferScheme: args.Length == 3 && !args[2].Contains('.'))], "duti");
            default:
                var file = args[^1];
                if (file.StartsWith('-')) throw new UsageException($"unknown option {file}");
                if (file.EndsWith(".plist", StringComparison.OrdinalIgnoreCase)) throw new UsageException("plist settings files aren't supported on Windows; use the tab-separated format");
                var requests = new Dictionary<AppReg, List<Target>>();
                foreach (var (line, n) in File.ReadAllLines(file).Select((l, i) => (l.Trim(), i + 1)))
                {
                    if (line.Length == 0 || line.StartsWith('#')) continue;
                    var fields = line.Split('\t', StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim()).ToArray();
                    if (fields.Length < 2) { Console.Error.WriteLine($"{file}:{n}: expected id<TAB>type[<TAB>role]"); continue; }
                    var a = FindApp(fields[0], apps);
                    if (a == null) { Console.Error.WriteLine($"{file}:{n}: {fields[0]}: no such registered application"); continue; }
                    (requests.TryGetValue(a, out var list) ? list : requests[a] = []).Add(ParseTarget(fields[1], preferScheme: fields.Length == 2));
                }
                return requests.Select(r => RequestDefault(r.Key, r.Value, "duti")).DefaultIfEmpty(0).Max();
        }
    }

    // ---- infat -----------------------------------------------------------------------------------

    static string InfatConfig => Path.Combine(Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") is { Length: > 0 } x ? x
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config"), "infat", "config.toml");

    static int RunInfat(string[] args)
    {
        if (args.Any(a => a is "-h" or "--help"))
        {
            Console.WriteLine($"""
                Usage: infat [OPTIONS] [COMMAND]

                Commands:
                  info  Show the opener of an app, extension, scheme or type
                          infat info --app <app> | --ext <ext> | --scheme <scheme> | --type <type>
                  set   Make an app the opener (Settings asks you to confirm, as Windows requires)
                          infat set <app> --ext <ext> | --scheme <scheme> | --type <type>
                  init  Write a starter config file
                  (none) Apply the config file

                Options:
                  -c, --config <path>  Config file (default {InfatConfig})
                      --robust         Keep going after errors
                  -V, --version        Print version
                """);
            return 0;
        }
        if (args.Any(a => a is "-V" or "--version")) { Console.WriteLine($"infat {Commands.Version}"); return 0; }
        var rest = args.ToList();
        string Option(string name)
        {
            var i = rest.IndexOf(name);
            if (i < 0) return null;
            if (i + 1 >= rest.Count) throw new UsageException($"{name} needs a value");
            var v = rest[i + 1];
            rest.RemoveRange(i, 2);
            return v;
        }
        var config = Option("--config") ?? Option("-c") ?? InfatConfig;
        rest.Remove("--robust"); rest.Remove("-v"); rest.Remove("--verbose"); rest.Remove("-q"); rest.Remove("--quiet");
        var apps = RegisteredApps();
        var command = rest.FirstOrDefault();
        switch (command)
        {
            case "info":
                var app = Option("--app");
                var ext = Option("--ext");
                var scheme = Option("--scheme");
                var type = Option("--type");
                if (app != null)
                {
                    var a = FindApp(app, apps) ?? throw new InvalidOperationException($"{app}: no such registered application");
                    Console.WriteLine($"{a.Display}");
                    Console.WriteLine($"  Identifier: {a.Name}");
                    if (a.Exe != null) Console.WriteLine($"  Path: {a.Exe}");
                    if (a.Files.Count > 0) Console.WriteLine($"  File types: {string.Join(", ", a.Files.Keys.Order())}");
                    if (a.Urls.Count > 0) Console.WriteLine($"  URL schemes: {string.Join(", ", a.Urls.Keys.Order())}");
                    return 0;
                }
                var target = ext != null ? ParseTarget(ext.StartsWith('.') ? ext : "." + ext) : scheme != null ? new Target(scheme.TrimEnd(':').ToLowerInvariant(), true)
                    : type != null ? ParseTarget(type.Contains('.') ? type : "public." + type) : throw new UsageException("info needs --app, --ext, --scheme or --type");
                var handlers = Handlers(target, apps);
                if (handlers.Count == 0) { Console.Error.WriteLine($"No apps open {target}"); return 1; }
                Console.WriteLine($"Default for {target}: {handlers[0].Display} ({handlers[0].Exe ?? handlers[0].Id})");
                if (handlers.Count > 1)
                {
                    Console.WriteLine("Also registered:");
                    foreach (var h in handlers.Skip(1)) Console.WriteLine($"  {h.Display} ({h.Id})");
                }
                return 0;
            case "set":
                var name = rest.ElementAtOrDefault(1) ?? throw new UsageException("set needs an app");
                var e = Option("--ext");
                var s = Option("--scheme");
                var t = Option("--type");
                var tgt = e != null ? ParseTarget(e.StartsWith('.') ? e : "." + e) : s != null ? new Target(s.TrimEnd(':').ToLowerInvariant(), true)
                    : t != null ? ParseTarget(t.Contains('.') ? t : "public." + t) : throw new UsageException("set needs --ext, --scheme or --type");
                var setApp = FindApp(name, apps) ?? throw new InvalidOperationException($"{name}: no such registered application");
                return RequestDefault(setApp, [tgt], "infat");
            case "init":
                if (File.Exists(config)) { Console.WriteLine($"{config} already exists"); return 0; }
                Directory.CreateDirectory(Path.GetDirectoryName(config)!);
                File.WriteAllText(config, "# infat: default openers. Values are app names from `infat info --app`.\n\n[extensions]\n# txt = \"Notepad\"\n\n[schemes]\n# mailto = \"Outlook\"\n\n[types]\n# plain-text = \"Notepad\"\n");
                Console.WriteLine($"Wrote {config}");
                return 0;
            case null:
                if (!File.Exists(config)) throw new InvalidOperationException($"no config at {config} (run infat init)");
                var requests = new Dictionary<AppReg, List<Target>>();
                string section = null;
                foreach (var raw in File.ReadAllLines(config))
                {
                    var line = raw.Split('#')[0].Trim();
                    if (line.Length == 0) continue;
                    if (line.StartsWith('[')) { section = line.Trim('[', ']').Trim(); continue; }
                    var eq = line.IndexOf('=');
                    if (eq <= 0 || section == null) continue;
                    var key = line[..eq].Trim().Trim('"');
                    var value = line[(eq + 1)..].Trim().Trim('"');
                    var targets = section switch
                    {
                        "extensions" => [ParseTarget("." + key.TrimStart('.'))],
                        "schemes" => key == "web" ? [new Target("http", true), new Target("https", true)] : [new Target(key, true)],
                        "types" => [ParseTarget(key.Contains('.') ? key : "public." + key)],
                        _ => new List<Target>(),
                    };
                    var a = FindApp(value, apps);
                    if (a == null) { Console.Error.WriteLine($"infat: {value}: no such registered application"); continue; }
                    (requests.TryGetValue(a, out var list) ? list : requests[a] = []).AddRange(targets);
                }
                return requests.Select(r => RequestDefault(r.Key, r.Value, "infat")).DefaultIfEmpty(0).Max();
            default:
                throw new UsageException($"unknown command '{command}' (info, set, init)");
        }
    }

    // ---- utiluti ---------------------------------------------------------------------------------

    static int RunUtiluti(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            Console.WriteLine("""
                USAGE: utiluti <subcommand>

                SUBCOMMANDS:
                  url <scheme>                   Default app for a URL scheme
                  url list <scheme>              All apps for a URL scheme
                  url set <scheme> <id>          Make id the default (Settings asks you to confirm)
                  type <type>                    Default app for a type (.ext or UTI)
                  type list <type>               All apps for a type
                  type set <type> <id>           Make id the default (Settings asks you to confirm)
                  get-uti <ext>                  Windows' type identifier (ProgId) for an extension
                  file <path>                    Default app for a file
                  app identifier <path>          Registered id of an app (.exe)
                  app version <path>             Version of an app (.exe)
                  app types <id> | schemes <id>  Types or schemes an app declares
                  version                        Print the version
                """);
            return args.Length == 0 ? 2 : 0;
        }
        var apps = RegisteredApps();
        string Arg(int i) => args.Length > i ? args[i] : throw new UsageException($"{string.Join(' ', args)}: missing argument");
        switch (args[0])
        {
            case "version" or "--version": Console.WriteLine(Commands.Version); return 0;
            case "url" or "type":
                var isUrl = args[0] == "url";
                Target T(string s) => isUrl ? new Target(s.TrimEnd(':').ToLowerInvariant(), true) : ParseTarget(s);
                switch (Arg(1))
                {
                    case "list":
                        foreach (var h in Handlers(T(Arg(2)), apps)) Console.WriteLine(h.Id);
                        return 0;
                    case "set":
                        var app = FindApp(Arg(3), apps) ?? throw new InvalidOperationException($"{args[3]}: no such registered application");
                        return RequestDefault(app, [T(Arg(2))], "utiluti");
                    default:
                        var d = Default(T(args[1]), apps);
                        if (d == null) { Console.Error.WriteLine($"no default app for {args[1]}"); return 1; }
                        Console.WriteLine(d.Id);
                        return 0;
                }
            case "get-uti":
                var ext = ParseTarget(Arg(1).StartsWith('.') ? args[1] : "." + args[1]);
                var progId = Query(ext.Value, ASSOCSTR_PROGID) ?? Registry.ClassesRoot.OpenSubKey(ext.Value)?.GetValue("") as string;
                if (progId == null) { Console.Error.WriteLine($"no type registered for {ext}"); return 1; }
                Console.WriteLine(progId);
                return 0;
            case "file":
                var path = Path.GetFullPath(Arg(1));
                var fd = Default(new Target(Path.GetExtension(path).ToLowerInvariant(), false), apps);
                if (fd == null) { Console.Error.WriteLine($"no default app for {path}"); return 1; }
                Console.WriteLine(fd.Exe ?? fd.Id);
                return 0;
            case "app":
                switch (Arg(1))
                {
                    case "identifier":
                        var exe = Path.GetFullPath(Arg(2));
                        var reg = apps.FirstOrDefault(a => a.Exe != null && a.Exe.Equals(exe, StringComparison.OrdinalIgnoreCase));
                        Console.WriteLine(reg?.Name ?? Path.GetFileNameWithoutExtension(exe).ToLowerInvariant());
                        return 0;
                    case "version":
                        var file = Path.GetFullPath(Arg(2));
                        if (!File.Exists(file)) throw new InvalidOperationException($"{args[2]}: No such file");
                        var info = FileVersionInfo.GetVersionInfo(file);
                        var version = info.ProductVersion?.Trim() is { Length: > 0 } pv ? pv : info.FileVersion?.Trim();
                        if (string.IsNullOrEmpty(version)) { Console.Error.WriteLine($"{args[2]} has no version information"); return 1; }
                        Console.WriteLine(version);
                        return 0;
                    case "types" or "schemes":
                        var a = FindApp(Arg(2), apps) ?? throw new InvalidOperationException($"{args[2]}: no such registered application");
                        foreach (var k in (args[1] == "types" ? a.Files : a.Urls).Keys.Order()) Console.WriteLine(k);
                        return 0;
                    default: throw new UsageException($"unknown app subcommand '{args[1]}'");
                }
            case "manage":
                Console.Error.WriteLine("utiluti: managed (MDM) defaults use Group Policy on Windows (Computer Configuration > Administrative Templates > Windows Components > File Explorer > Set a default associations configuration file)");
                return 1;
            default: throw new UsageException($"unknown subcommand '{args[0]}'");
        }
    }

    // ---- Win32 -----------------------------------------------------------------------------------

    const uint ASSOCF_NOTRUNCATE = 0x20, ASSOCF_INIT_IGNOREUNKNOWN = 0x400;
    internal const int ASSOCSTR_EXECUTABLE = 2, ASSOCSTR_FRIENDLYAPPNAME = 4, ASSOCSTR_PROGID = 20;

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    static extern int AssocQueryStringW(uint flags, int str, string assoc, string extra, char* output, ref uint length);

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    static extern int SHLoadIndirectString(string source, char* output, uint size, IntPtr reserved);
}
