using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WinTools;

// bartycrouch: keeps Localizable.strings files in sync with the code. `update` runs the configured
// tasks from .bartycrouch.toml (code: collect NSLocalizedString/String(localized:) keys like
// genstrings and add them to every <lang>.lproj file; transform: turn BartyCrouch.translate(...)
// calls into NSLocalizedString or SwiftGen calls; normalize: harmonize locales with the source
// locale and sort; translate: fill empty values through Microsoft Translator or DeepL). `lint`
// reports duplicate keys and empty values. The interfaces task (Storyboard/XIB strings) needs
// Xcode's ibtool, so it reports the files it had to skip.
static partial class Bartycrouch
{
    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("bartycrouch", "Incrementally update/translate your Strings files", Run);

    const string Version = "4.15.1";

    const string Usage = """
        Usage: bartycrouch <subcommand> [options]

        Subcommands:
          update   Update your .strings file contents according to your configuration
          lint     Lint your .strings file contents
          init     Create a default .bartycrouch.toml configuration file

        Options:
          -v, --verbose            Print more detailed information about the executed command
          -x, --xcode-output       Print warnings & errors in Xcode compatible format
          -w, --fail-on-warnings   Return a non-zero exit code if any warnings are found
          -p, --path <path>        Run in this directory (where .bartycrouch.toml is)
              --version            Show the version
        """;

    static readonly string[] DefaultIgnore = [".git", "carthage", "pods", "build", ".build", "docs"];
    static readonly string[] DefaultIgnoreKeys = ["#bartycrouch-ignore!", "#bc-ignore!", "#i!"];

    sealed class Entry
    {
        public string Key, Value, Comment;
        public int Line;
    }

    static bool verbose, xcode;
    static int warnings;

    static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help") { Console.WriteLine(Usage); return 0; }
        if (args[0] == "--version") { Console.WriteLine(Version); return 0; }
        var sub = args[0];
        var failOnWarnings = false;
        string dir = ".";
        verbose = xcode = false;
        warnings = 0;
        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help": Console.WriteLine(Usage); return 0;
                case "-v" or "--verbose": verbose = true; break;
                case "-x" or "--xcode-output": xcode = true; break;
                case "-w" or "--fail-on-warnings": failOnWarnings = true; break;
                case "-p" or "--path":
                    dir = i + 1 < args.Length ? args[++i] : throw new UsageException("--path needs a value");
                    break;
                default: throw new UsageException($"unknown option {args[i]} (see bartycrouch --help)");
            }
        }
        if (!Directory.Exists(dir)) throw new InvalidOperationException($"{dir}: no such directory");
        dir = Path.GetFullPath(dir);
        var configPath = Path.Combine(dir, ".bartycrouch.toml");

        switch (sub)
        {
            case "init":
                if (File.Exists(configPath)) throw new InvalidOperationException("a .bartycrouch.toml already exists here");
                File.WriteAllText(configPath, DefaultConfig);
                Info($"Successfully created .bartycrouch.toml");
                return 0;
            case "update":
            case "lint":
                var config = File.Exists(configPath) ? ParseToml(File.ReadAllText(configPath)) : [];
                if (sub == "lint") Lint(dir, config);
                else Update(dir, config);
                if (warnings > 0) Console.WriteLine($"Found {warnings} warning(s).");
                return failOnWarnings && warnings > 0 ? 1 : 0;
            default:
                throw new UsageException($"unknown subcommand '{sub}' (see bartycrouch --help)");
        }
    }

    const string DefaultConfig = """
        [update]
        tasks = ["interfaces", "code", "transform", "normalize"]

        [update.interfaces]
        paths = ["."]
        subpathsToIgnore = [".git", "carthage", "pods", "build", ".build", "docs"]
        defaultToBase = false
        ignoreEmptyStrings = false
        unstripped = false
        ignoreKeys = ["#bartycrouch-ignore!", "#bc-ignore!", "#i!"]

        [update.code]
        codePaths = ["."]
        subpathsToIgnore = [".git", "carthage", "pods", "build", ".build", "docs"]
        localizablePaths = ["."]
        defaultToKeys = false
        additive = true
        unstripped = false
        ignoreKeys = ["#bartycrouch-ignore!", "#bc-ignore!", "#i!"]

        [update.transform]
        codePaths = ["."]
        subpathsToIgnore = [".git", "carthage", "pods", "build", ".build", "docs"]
        localizablePaths = ["."]
        transformer = "foundation"
        supportedLanguageEnumPath = "."
        typeName = "BartyCrouch"
        translateMethodName = "translate"

        [update.normalize]
        paths = ["."]
        subpathsToIgnore = [".git", "carthage", "pods", "build", ".build", "docs"]
        sourceLocale = "en"
        harmonizeWithSource = true
        sortByKeys = true

        [lint]
        paths = ["."]
        subpathsToIgnore = [".git", "carthage", "pods", "build", ".build", "docs"]
        duplicateKeys = true
        emptyValues = true

        """;

    // ---- update ----

    static void Update(string root, Dictionary<string, object> config)
    {
        var tasks = Strings(config, "update.tasks") ?? ["interfaces", "code", "transform", "normalize"];
        foreach (var task in tasks)
        {
            var prefix = "update." + task + ".";
            switch (task)
            {
                case "interfaces": Interfaces(root, config, prefix); break;
                case "code": CodeTask(root, config, prefix); break;
                case "transform": Transform(root, config, prefix); break;
                case "normalize": Normalize(root, config, prefix); break;
                case "translate": Translate(root, config, prefix); break;
                default: throw new InvalidOperationException($"unknown update task '{task}' in .bartycrouch.toml");
            }
        }
    }

    static void Interfaces(string root, Dictionary<string, object> config, string p)
    {
        var ignore = Strings(config, p + "subpathsToIgnore") ?? DefaultIgnore;
        var files = (Strings(config, p + "paths") ?? ["."])
            .SelectMany(path => Files(root, path, ignore, f => f.EndsWith(".storyboard", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".xib", StringComparison.OrdinalIgnoreCase)))
            .Distinct().ToList();
        foreach (var f in files)
            Warn(f, 0, "Interfaces", "Storyboard/XIB strings are extracted by Xcode's ibtool, which only runs on macOS; skipped");
    }

    static void CodeTask(string root, Dictionary<string, object> config, string p)
    {
        var ignore = Strings(config, p + "subpathsToIgnore") ?? DefaultIgnore;
        var ignoreKeys = Strings(config, p + "ignoreKeys") ?? DefaultIgnoreKeys;
        var defaultToKeys = Bool(config, p + "defaultToKeys", false);
        var additive = Bool(config, p + "additive", true);
        var table = Str(config, p + "customLocalizableName") ?? "Localizable";
        var functions = new List<string> { "NSLocalizedString", "String(localized:", "LocalizedStringResource(" };
        if (Str(config, p + "customFunction") is { } custom) functions.Add(custom);
        if (Str(config, p + "plainCustomFunction") is { } plain) functions.Add(plain);

        var found = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in (Strings(config, p + "codePaths") ?? ["."]).SelectMany(path => Files(root, path, ignore, IsCode)).Distinct())
        {
            var text = File.ReadAllText(file);
            foreach (var (key, comment, tableName) in ScanCalls(text, functions))
            {
                if ((tableName ?? "Localizable") != table && !(tableName == null && table != "Localizable")) continue;
                if (ignoreKeys.Any(k => (comment ?? "").Contains(k) || key.Contains(k))) continue;
                if (!found.ContainsKey(key) || string.IsNullOrEmpty(found[key])) found[key] = comment;
            }
        }
        if (verbose) Info($"code: found {found.Count} localized key(s) in code");
        foreach (var file in (Strings(config, p + "localizablePaths") ?? ["."]).SelectMany(path => Files(root, path, ignore, f => IsStrings(f, table))).Distinct())
        {
            var entries = ParseStrings(file);
            var keys = entries.Select(e => e.Key).ToHashSet(StringComparer.Ordinal);
            var changed = false;
            foreach (var e in entries)
                if (found.TryGetValue(e.Key, out var c) && c != null && c.Length > 0 && e.Comment != c) { e.Comment = c; changed = true; }
            foreach (var (key, comment) in found.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                if (keys.Contains(key)) continue;
                entries.Add(new Entry { Key = key, Value = defaultToKeys ? key : "", Comment = string.IsNullOrEmpty(comment) ? "No comment provided by engineer." : comment });
                changed = true;
            }
            if (!additive)
                changed |= entries.RemoveAll(e => !found.ContainsKey(e.Key)) > 0;
            if (changed) WriteStrings(file, entries);
            if (verbose) Info($"code: updated {Rel(root, file)}");
        }
    }

    static bool IsCode(string f) => Path.GetExtension(f).ToLowerInvariant() is ".swift" or ".m" or ".mm" or ".h";

    static bool IsStrings(string f, string table = null) =>
        f.EndsWith(".strings", StringComparison.OrdinalIgnoreCase) &&
        Path.GetFileName(Path.GetDirectoryName(f))!.EndsWith(".lproj", StringComparison.OrdinalIgnoreCase) &&
        (table == null || Path.GetFileNameWithoutExtension(f) == table);

    // Finds calls like NSLocalizedString("key", tableName: "T", comment: "c"), NSLocalizedString(@"key", @"c")
    // and String(localized: "key", comment: "c"); returns (key, comment, table).
    static IEnumerable<(string Key, string Comment, string Table)> ScanCalls(string text, List<string> functions)
    {
        foreach (var fn in functions)
        {
            var callStart = fn.EndsWith('(') || fn.EndsWith(':') ? fn : fn + "(";
            var at = 0;
            while ((at = text.IndexOf(callStart, at, StringComparison.Ordinal)) >= 0)
            {
                // Must not be part of a longer identifier.
                if (at > 0 && (char.IsLetterOrDigit(text[at - 1]) || text[at - 1] == '_')) { at += callStart.Length; continue; }
                var open = text.IndexOf('(', at);
                var args = Balanced(text, open);
                at += callStart.Length;
                if (args == null) continue;
                var literals = StringLiteral().Matches(args);
                if (literals.Count == 0) continue;
                // Skip calls whose key is not a literal (e.g. NSLocalizedString(variable, ...)).
                var head = args.TrimStart();
                if (head.StartsWith("localized:")) head = head[10..].TrimStart();
                if (!(head.StartsWith('"') || head.StartsWith("@\""))) continue;
                var key = Unescape(literals[0].Groups[1].Value);
                string comment = null, table = null;
                var labelled = LabelledLiteral().Matches(args);
                foreach (Match m in labelled)
                {
                    if (m.Groups[1].Value == "comment") comment = Unescape(m.Groups[2].Value);
                    else if (m.Groups[1].Value is "tableName" or "table") table = Unescape(m.Groups[2].Value);
                }
                // Objective-C: NSLocalizedString(@"key", @"comment")
                if (comment == null && labelled.Count == 0 && literals.Count > 1 && literals[1].Value.StartsWith('@'))
                    comment = Unescape(literals[1].Groups[1].Value);
                yield return (key, comment, table);
            }
        }
    }

    static string Balanced(string text, int open)
    {
        if (open < 0) return null;
        var depth = 0;
        var inString = false;
        for (var i = open; i < text.Length; i++)
        {
            var c = text[i];
            if (inString)
            {
                if (c == '\\') i++;
                else if (c == '"') inString = false;
                continue;
            }
            if (c == '"') inString = true;
            else if (c == '(') depth++;
            else if (c == ')' && --depth == 0) return text[(open + 1)..i];
        }
        return null;
    }

    static void Transform(string root, Dictionary<string, object> config, string p)
    {
        var ignore = Strings(config, p + "subpathsToIgnore") ?? DefaultIgnore;
        var typeName = Str(config, p + "typeName") ?? "BartyCrouch";
        var method = Str(config, p + "translateMethodName") ?? "translate";
        var transformer = Str(config, p + "transformer") ?? "foundation";
        var call = typeName + "." + method + "(";
        var codeFiles = (Strings(config, p + "codePaths") ?? ["."]).SelectMany(path => Files(root, path, ignore, f => f.EndsWith(".swift", StringComparison.OrdinalIgnoreCase))).Distinct().ToList();

        // Language enum: `case english = "en"` inside the SupportedLanguage enum.
        var languages = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in Files(root, Str(config, p + "supportedLanguageEnumPath") ?? ".", ignore, f => f.EndsWith(".swift", StringComparison.OrdinalIgnoreCase)))
        {
            var text = File.ReadAllText(file);
            var m = SupportedLanguageEnum().Match(text);
            if (!m.Success) continue;
            var body = Balanced(text.Replace('{', '(').Replace('}', ')'), m.Index + m.Length - 1);
            foreach (Match c in LanguageCase().Matches(body ?? "")) languages[c.Groups[1].Value] = c.Groups[2].Value;
        }

        var translations = new List<(string Key, string Comment, Dictionary<string, string> Values)>();
        foreach (var file in codeFiles)
        {
            var text = File.ReadAllText(file);
            if (!text.Contains(call)) continue;
            var sb = new StringBuilder();
            var last = 0;
            int at;
            while ((at = text.IndexOf(call, last, StringComparison.Ordinal)) >= 0)
            {
                var args = Balanced(text, at + call.Length - 1);
                if (args == null) break;
                var end = at + call.Length + args.Length + 1;
                var key = LabelledLiteral().Matches(args).FirstOrDefault(m => m.Groups[1].Value == "key")?.Groups[2].Value;
                var comment = LabelledLiteral().Matches(args).FirstOrDefault(m => m.Groups[1].Value == "comment")?.Groups[2].Value;
                if (key == null) { sb.Append(text, last, end - last); last = end; continue; }
                var values = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (Match t in TranslationPair().Matches(args))
                {
                    var lang = languages.GetValueOrDefault(t.Groups[1].Value)
                        ?? throw new InvalidOperationException($"{file}: unknown language case .{t.Groups[1].Value} (no SupportedLanguage enum entry found)");
                    values[lang] = Unescape(t.Groups[2].Value);
                }
                translations.Add((Unescape(key), comment == null ? null : Unescape(comment), values));
                sb.Append(text, last, at - last);
                sb.Append(transformer == "swiftgenStructured"
                    ? SwiftgenPath(Unescape(key))
                    : comment == null ? $"NSLocalizedString(\"{key}\", comment: \"\")" : $"NSLocalizedString(\"{key}\", comment: \"{comment}\")");
                last = end;
            }
            sb.Append(text, last, text.Length - last);
            if (sb.ToString() != text) { File.WriteAllText(file, sb.ToString()); if (verbose) Info($"transform: rewrote {Rel(root, file)}"); }
        }
        if (translations.Count == 0) return;
        foreach (var file in (Strings(config, p + "localizablePaths") ?? ["."]).SelectMany(path => Files(root, path, ignore, f => IsStrings(f, "Localizable"))).Distinct())
        {
            var lang = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(file));
            var entries = ParseStrings(file);
            foreach (var (key, comment, values) in translations)
            {
                var value = values.GetValueOrDefault(lang) ?? "";
                var existing = entries.FirstOrDefault(e => e.Key == key);
                if (existing == null) entries.Add(new Entry { Key = key, Value = value, Comment = comment });
                else if (value.Length > 0) existing.Value = value;
            }
            WriteStrings(file, entries);
        }
    }

    // SwiftGen's structured names: "onboarding.first-page.title" -> L10n.Onboarding.FirstPage.title
    static string SwiftgenPath(string key)
    {
        var parts = key.Split('.');
        string Camel(string s, bool upper)
        {
            var words = Regex.Split(s, "[^A-Za-z0-9]+").Where(w => w.Length > 0).ToList();
            if (words.Count == 0) return "_";
            var r = string.Concat(words.Select((w, i) => i == 0 && !upper ? char.ToLowerInvariant(w[0]) + w[1..] : char.ToUpperInvariant(w[0]) + w[1..]));
            return char.IsDigit(r[0]) ? "_" + r : r;
        }
        return "L10n." + string.Join('.', parts.Select((x, i) => Camel(x, upper: i < parts.Length - 1)));
    }

    static void Normalize(string root, Dictionary<string, object> config, string p)
    {
        var ignore = Strings(config, p + "subpathsToIgnore") ?? DefaultIgnore;
        var source = Str(config, p + "sourceLocale") ?? "en";
        var harmonize = Bool(config, p + "harmonizeWithSource", true);
        var sort = Bool(config, p + "sortByKeys", true);
        var files = (Strings(config, p + "paths") ?? ["."]).SelectMany(path => Files(root, path, ignore, f => IsStrings(f))).Distinct().ToList();
        // Group the same table across locales: <dir>/<lang>.lproj/<table>.strings
        foreach (var group in files.GroupBy(f => Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(f))!, Path.GetFileName(f)), StringComparer.OrdinalIgnoreCase))
        {
            var byLang = group.ToDictionary(f => Path.GetFileNameWithoutExtension(Path.GetDirectoryName(f))!, StringComparer.OrdinalIgnoreCase);
            var sourceFile = byLang.GetValueOrDefault(source) ?? byLang.GetValueOrDefault("Base");
            var sourceEntries = sourceFile != null ? ParseStrings(sourceFile) : null;
            foreach (var file in group)
            {
                var entries = ParseStrings(file);
                if (harmonize && sourceEntries != null && file != sourceFile)
                {
                    var keys = sourceEntries.Select(e => e.Key).ToHashSet(StringComparer.Ordinal);
                    entries.RemoveAll(e => !keys.Contains(e.Key));
                    var have = entries.Select(e => e.Key).ToHashSet(StringComparer.Ordinal);
                    foreach (var s in sourceEntries.Where(s => !have.Contains(s.Key)))
                        entries.Add(new Entry { Key = s.Key, Value = "", Comment = s.Comment });
                }
                if (sort) entries = entries.OrderBy(e => e.Key, StringComparer.Ordinal).ToList();
                WriteStrings(file, entries);
            }
        }
    }

    static void Translate(string root, Dictionary<string, object> config, string p)
    {
        var secret = Str(config, p + "secret");
        var translator = Str(config, p + "translator") ?? "microsoftTranslator";
        var source = Str(config, p + "sourceLocale") ?? "en";
        if (string.IsNullOrEmpty(secret))
            throw new InvalidOperationException("translate: no secret configured in [update.translate] of .bartycrouch.toml");
        var ignore = Strings(config, p + "subpathsToIgnore") ?? DefaultIgnore;
        var files = (Strings(config, p + "paths") ?? ["."]).SelectMany(path => Files(root, path, ignore, f => IsStrings(f))).Distinct().ToList();
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        foreach (var group in files.GroupBy(f => Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(f))!, Path.GetFileName(f)), StringComparer.OrdinalIgnoreCase))
        {
            var byLang = group.ToDictionary(f => Path.GetFileNameWithoutExtension(Path.GetDirectoryName(f))!, StringComparer.OrdinalIgnoreCase);
            if (!byLang.TryGetValue(source, out var sourceFile)) continue;
            var sourceValues = ParseStrings(sourceFile).GroupBy(e => e.Key).ToDictionary(g => g.Key, g => g.First().Value, StringComparer.Ordinal);
            foreach (var (lang, file) in byLang)
            {
                if (string.Equals(lang, source, StringComparison.OrdinalIgnoreCase) || lang == "Base") continue;
                var entries = ParseStrings(file);
                var todo = entries.Where(e => e.Value.Length == 0 && sourceValues.TryGetValue(e.Key, out var v) && v.Length > 0).ToList();
                if (todo.Count == 0) continue;
                var texts = todo.Select(e => sourceValues[e.Key]).ToList();
                var results = translator == "deepL"
                    ? DeepL(http, secret, source, lang, texts)
                    : Microsoft(http, secret, source, lang, texts);
                for (var i = 0; i < todo.Count; i++) todo[i].Value = results[i];
                WriteStrings(file, entries);
                Info($"translate: {todo.Count} value(s) translated into {lang} ({Rel(root, file)})");
            }
        }
    }

    static List<string> Microsoft(HttpClient http, string secret, string from, string to, List<string> texts)
    {
        using var body = new MemoryStream();
        using (var w = new Utf8JsonWriter(body))
        {
            w.WriteStartArray();
            foreach (var t in texts) { w.WriteStartObject(); w.WriteString("Text", t); w.WriteEndObject(); }
            w.WriteEndArray();
        }
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}");
        request.Headers.Add("Ocp-Apim-Subscription-Key", secret);
        request.Content = new ByteArrayContent(body.ToArray());
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        using var doc = Send(http, request);
        return doc.RootElement.EnumerateArray().Select(r => r.GetProperty("translations")[0].GetProperty("text").GetString() ?? "").ToList();
    }

    static List<string> DeepL(HttpClient http, string secret, string from, string to, List<string> texts)
    {
        var host = secret.EndsWith(":fx", StringComparison.Ordinal) ? "api-free.deepl.com" : "api.deepl.com";
        var form = texts.Select(t => new KeyValuePair<string, string>("text", t)).ToList();
        form.Add(new("source_lang", from.Split('-')[0].ToUpperInvariant()));
        form.Add(new("target_lang", to.ToUpperInvariant()));
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://{host}/v2/translate") { Content = new FormUrlEncodedContent(form) };
        request.Headers.Authorization = new AuthenticationHeaderValue("DeepL-Auth-Key", secret);
        using var doc = Send(http, request);
        return doc.RootElement.GetProperty("translations").EnumerateArray().Select(t => t.GetProperty("text").GetString() ?? "").ToList();
    }

    static JsonDocument Send(HttpClient http, HttpRequestMessage request)
    {
        HttpResponseMessage response;
        try { response = http.Send(request); }
        catch (HttpRequestException e) { throw new InvalidOperationException($"translate: {e.Message}"); }
        using (response)
        {
            var text = new StreamReader(response.Content.ReadAsStream()).ReadToEnd();
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"translate: {(int)response.StatusCode} {response.ReasonPhrase}: {text}");
            return JsonDocument.Parse(text);
        }
    }

    // ---- lint ----

    static void Lint(string root, Dictionary<string, object> config)
    {
        var ignore = Strings(config, "lint.subpathsToIgnore") ?? DefaultIgnore;
        var duplicates = Bool(config, "lint.duplicateKeys", true);
        var empties = Bool(config, "lint.emptyValues", true);
        var files = (Strings(config, "lint.paths") ?? ["."]).SelectMany(path => Files(root, path, ignore, f => IsStrings(f))).Distinct().ToList();
        foreach (var file in files)
        {
            var entries = ParseStrings(file);
            if (duplicates)
                foreach (var dup in entries.GroupBy(e => e.Key).Where(g => g.Count() > 1))
                    foreach (var e in dup.Skip(1))
                        Warn(file, e.Line, "Duplicate Keys", $"Found duplicate key '{e.Key}' (first defined on line {dup.First().Line}).");
            if (empties)
                foreach (var e in entries.Where(e => e.Value.Length == 0))
                    Warn(file, e.Line, "Empty Values", $"Found empty value for key '{e.Key}'.");
        }
        if (verbose) Info($"lint: checked {files.Count} file(s)");
    }

    // ---- .strings files ----

    static List<Entry> ParseStrings(string path)
    {
        var text = File.ReadAllText(path);
        var list = new List<Entry>();
        string comment = null;
        var i = 0;
        var line = 1;
        void Skip(int n) { for (var k = 0; k < n && i < text.Length; k++, i++) if (text[i] == '\n') line++; }
        while (i < text.Length)
        {
            var c = text[i];
            if (char.IsWhiteSpace(c)) { Skip(1); continue; }
            if (text.AsSpan(i).StartsWith("/*"))
            {
                var end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                if (end < 0) end = text.Length - 2;
                comment = text[(i + 2)..end].Trim();
                Skip(end + 2 - i);
                continue;
            }
            if (text.AsSpan(i).StartsWith("//"))
            {
                var end = text.IndexOf('\n', i);
                if (end < 0) end = text.Length;
                comment = text[(i + 2)..end].Trim();
                Skip(end - i);
                continue;
            }
            if (c == '"')
            {
                var entryLine = line;
                var key = ReadQuoted(text, ref i, ref line);
                while (i < text.Length && char.IsWhiteSpace(text[i])) Skip(1);
                string value = key;
                if (i < text.Length && text[i] == '=')
                {
                    Skip(1);
                    while (i < text.Length && char.IsWhiteSpace(text[i])) Skip(1);
                    if (i < text.Length && text[i] == '"') value = ReadQuoted(text, ref i, ref line);
                    else throw new InvalidOperationException($"{path}:{line}: expected a quoted value");
                }
                while (i < text.Length && text[i] != ';' && text[i] != '\n') Skip(1);
                Skip(1);
                list.Add(new Entry { Key = key, Value = value, Comment = comment, Line = entryLine });
                comment = null;
                continue;
            }
            throw new InvalidOperationException($"{path}:{line}: unexpected character '{c}'");
        }
        return list;
    }

    static string ReadQuoted(string text, ref int i, ref int line)
    {
        var sb = new StringBuilder();
        i++;
        while (i < text.Length && text[i] != '"')
        {
            if (text[i] == '\n') line++;
            if (text[i] == '\\' && i + 1 < text.Length) { sb.Append(text[i]).Append(text[i + 1]); i += 2; continue; }
            sb.Append(text[i++]);
        }
        i++;
        return sb.ToString();   // kept escaped, written back verbatim
    }

    static void WriteStrings(string path, List<Entry> entries)
    {
        var sb = new StringBuilder();
        foreach (var e in entries)
        {
            if (sb.Length > 0) sb.Append('\n');
            if (!string.IsNullOrEmpty(e.Comment)) sb.Append($"/* {e.Comment} */\n");
            sb.Append($"\"{e.Key}\" = \"{e.Value}\";\n");
        }
        File.WriteAllText(path, sb.ToString());
    }

    // .strings files use the same backslash escapes as Swift/Objective-C literals, so keys and
    // values are carried over exactly as written in the code.
    static string Unescape(string literal) => literal;

    // ---- helpers ----

    static IEnumerable<string> Files(string root, string sub, string[] ignore, Func<string, bool> filter)
    {
        var start = Path.GetFullPath(Path.Combine(root, sub));
        if (File.Exists(start)) { if (filter(start)) yield return start; yield break; }
        if (!Directory.Exists(start)) yield break;
        var stack = new Stack<string>();
        stack.Push(start);
        while (stack.Count > 0)
        {
            var dir = stack.Pop();
            IEnumerable<string> children, files;
            try { children = Directory.EnumerateDirectories(dir).ToList(); files = Directory.EnumerateFiles(dir).ToList(); }
            catch (UnauthorizedAccessException) { continue; }
            foreach (var f in files.Order(StringComparer.Ordinal)) if (filter(f)) yield return f;
            foreach (var d in children.OrderDescending(StringComparer.Ordinal))
                if (!ignore.Any(x => string.Equals(Path.GetFileName(d), x, StringComparison.OrdinalIgnoreCase))) stack.Push(d);
        }
    }

    static string Rel(string root, string path) => Path.GetRelativePath(root, path);

    static void Info(string message) => Console.WriteLine(message);

    static void Warn(string file, int line, string category, string message)
    {
        warnings++;
        Console.WriteLine(xcode
            ? $"{file}:{line}: warning: BartyCrouch: [{category}] {message}"
            : $"warning: [{category}] {Path.GetRelativePath(Environment.CurrentDirectory, file)}{(line > 0 ? ":" + line : "")}: {message}");
    }

    static string Str(Dictionary<string, object> c, string key) => c.TryGetValue(key, out var v) ? v as string : null;
    static bool Bool(Dictionary<string, object> c, string key, bool fallback) => c.TryGetValue(key, out var v) && v is bool b ? b : fallback;
    static string[] Strings(Dictionary<string, object> c, string key) =>
        c.TryGetValue(key, out var v) ? v switch { List<object> l => l.Select(x => x?.ToString() ?? "").ToArray(), string s => [s], _ => null } : null;

    // Just enough TOML for BartyCrouch's config: [tables], strings, booleans, numbers, arrays.
    internal static Dictionary<string, object> ParseToml(string text)
    {
        var result = new Dictionary<string, object>(StringComparer.Ordinal);
        var table = "";
        var lines = text.Replace("\r\n", "\n").Split('\n');
        for (var n = 0; n < lines.Length; n++)
        {
            var line = StripComment(lines[n]).Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith('['))
            {
                table = line.Trim('[', ']').Trim();
                continue;
            }
            var eq = line.IndexOf('=');
            if (eq < 0) throw new InvalidOperationException($".bartycrouch.toml:{n + 1}: expected key = value");
            var key = line[..eq].Trim().Trim('"');
            var value = line[(eq + 1)..].Trim();
            // Arrays may span lines.
            while (value.StartsWith('[') && Depth(value) > 0 && n + 1 < lines.Length) value += " " + StripComment(lines[++n]).Trim();
            var pos = 0;
            result[table.Length > 0 ? table + "." + key : key] = TomlValue(value, ref pos);
        }
        return result;
    }

    static int Depth(string s)
    {
        var d = 0;
        var q = '\0';
        foreach (var c in s)
        {
            if (q != '\0') { if (c == q) q = '\0'; continue; }
            if (c is '"' or '\'') q = c;
            else if (c == '[') d++;
            else if (c == ']') d--;
        }
        return d;
    }

    static string StripComment(string line)
    {
        var q = '\0';
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (q != '\0') { if (c == '\\' && q == '"') i++; else if (c == q) q = '\0'; continue; }
            if (c is '"' or '\'') q = c;
            else if (c == '#') return line[..i];
        }
        return line;
    }

    static object TomlValue(string s, ref int i)
    {
        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        if (i >= s.Length) return null;
        var c = s[i];
        if (c == '"' || c == '\'')
        {
            var sb = new StringBuilder();
            i++;
            while (i < s.Length && s[i] != c)
            {
                if (c == '"' && s[i] == '\\' && i + 1 < s.Length)
                {
                    i++;
                    sb.Append(s[i] switch { 'n' => '\n', 't' => '\t', _ => s[i] });
                    i++;
                    continue;
                }
                sb.Append(s[i++]);
            }
            i++;
            return sb.ToString();
        }
        if (c == '[')
        {
            var list = new List<object>();
            i++;
            while (true)
            {
                while (i < s.Length && (char.IsWhiteSpace(s[i]) || s[i] == ',')) i++;
                if (i >= s.Length || s[i] == ']') { i++; break; }
                list.Add(TomlValue(s, ref i));
            }
            return list;
        }
        var start = i;
        while (i < s.Length && s[i] != ',' && s[i] != ']' && !char.IsWhiteSpace(s[i])) i++;
        var word = s[start..i];
        if (word == "true") return true;
        if (word == "false") return false;
        return long.TryParse(word, out var l) ? l : word;
    }

    [GeneratedRegex("@?\"((?:[^\"\\\\]|\\\\.)*)\"")]
    private static partial Regex StringLiteral();

    [GeneratedRegex("(\\w+)\\s*:\\s*@?\"((?:[^\"\\\\]|\\\\.)*)\"")]
    private static partial Regex LabelledLiteral();

    [GeneratedRegex(@"enum\s+SupportedLanguage\s*:\s*String[^{]*\{")]
    private static partial Regex SupportedLanguageEnum();

    [GeneratedRegex("case\\s+(\\w+)\\s*=\\s*\"([^\"]+)\"")]
    private static partial Regex LanguageCase();

    [GeneratedRegex("\\.(\\w+)\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"")]
    private static partial Regex TranslationPair();
}
