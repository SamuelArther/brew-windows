using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace WinTools;

// sbjson: SBJson's command-line parser and reformatter. Reads JSON documents from files or stdin
// and writes them back compact (one root per line) or human readable, like SBJson's writer:
// numbers normalised (42.001e3 -> 42001), "key" : value pairs in pretty mode.
static class Sbjson
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("sbjson", "Parse and reformat JSON (SBJson's command-line tool)", Run);
    }

    static int Run(string[] args)
    {
        bool multiRoot = false, unwrap = false, pretty = false, sortKeys = false, verbose = false;
        var maxDepth = 32;
        var files = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage: sbjson [OPTIONS] [FILES]
                        Parses JSON from FILES (or standard input) and writes it back out.

                          -m, --multi-root      accept several top-level documents per input
                          -u, --unwrap-root     write each element of a top-level array as its own document
                          -p, --pretty          human-readable output (alias: --human-readable)
                          -s, --sort-keys       sort object keys
                          -d, --max-depth N     maximum nesting depth (default 32)
                          -v, --verbose         report what is being read
                          -h, --help            show this help
                              --version         print the version
                        """);
                    return 0;
                case "--version": Console.WriteLine($"sbjson {Commands.Version} (wintools)"); return 0;
                case "-m" or "--multi-root": multiRoot = true; break;
                case "-u" or "--unwrap-root": unwrap = true; break;
                case "-p" or "--pretty" or "--human-readable": pretty = true; break;
                case "-s" or "--sort-keys": sortKeys = true; break;
                case "-v" or "--verbose": verbose = true; break;
                case "-d" or "--max-depth":
                    if (++i >= args.Length || !int.TryParse(args[i], out maxDepth) || maxDepth < 1) throw new UsageException("--max-depth needs a positive number");
                    break;
                default:
                    if (arg.StartsWith("--max-depth=")) { if (!int.TryParse(arg[12..], out maxDepth)) throw new UsageException("bad --max-depth"); break; }
                    if (arg.StartsWith('-') && arg != "-") throw new UsageException($"unknown option {arg}");
                    files.Add(arg);
                    break;
            }
        }
        if (files.Count == 0) files.Add("-");
        var status = 0;
        using var stdout = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false));
        foreach (var file in files)
        {
            if (verbose) Console.Error.WriteLine($"sbjson: reading {(file == "-" ? "standard input" : file)}");
            byte[] input;
            if (file == "-")
            {
                using var stdin = Console.OpenStandardInput();
                using var ms = new MemoryStream();
                stdin.CopyTo(ms);
                input = ms.ToArray();
            }
            else input = File.ReadAllBytes(file);
            try
            {
                foreach (var root in Parse(input, multiRoot, maxDepth))
                {
                    using (root)
                    {
                        if (unwrap && root.RootElement.ValueKind == JsonValueKind.Array)
                            foreach (var item in root.RootElement.EnumerateArray()) stdout.Write(Format(item, pretty, sortKeys) + "\n");
                        else stdout.Write(Format(root.RootElement, pretty, sortKeys) + "\n");
                    }
                }
            }
            catch (JsonException e)
            {
                stdout.Flush();
                Console.Error.WriteLine($"sbjson: {(file == "-" ? "stdin" : file)}: {e.Message}");
                status = 1;
            }
        }
        return status;
    }

    static List<JsonDocument> Parse(byte[] input, bool multiRoot, int maxDepth)
    {
        ReadOnlySpan<byte> data = input;
        if (data.StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF])) data = data[3..];
        var reader = new Utf8JsonReader(data, new JsonReaderOptions { AllowMultipleValues = true, MaxDepth = maxDepth });
        var roots = new List<JsonDocument>();
        while (reader.Read())
        {
            if (roots.Count == 1 && !multiRoot)
            {
                roots[0].Dispose();
                throw new JsonException("unexpected data after the first document (use --multi-root)");
            }
            roots.Add(JsonDocument.ParseValue(ref reader));
        }
        if (roots.Count == 0) throw new JsonException("no JSON input");
        return roots;
    }

    internal static string Format(JsonElement element, bool pretty, bool sortKeys)
    {
        var sb = new StringBuilder();
        Write(sb, element, pretty, sortKeys, 0);
        return sb.ToString();
    }

    static void Write(StringBuilder sb, JsonElement e, bool pretty, bool sortKeys, int depth)
    {
        switch (e.ValueKind)
        {
            case JsonValueKind.Object:
                var props = e.EnumerateObject().Select(p => (p.Name, p.Value)).ToList();
                if (sortKeys) props.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
                if (props.Count == 0) { sb.Append("{}"); return; }
                sb.Append('{');
                for (var i = 0; i < props.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    if (pretty) sb.Append('\n').Append(' ', (depth + 1) * 2);
                    String(sb, props[i].Name);
                    sb.Append(pretty ? " : " : ":");
                    Write(sb, props[i].Value, pretty, sortKeys, depth + 1);
                }
                if (pretty) sb.Append('\n').Append(' ', depth * 2);
                sb.Append('}');
                return;
            case JsonValueKind.Array:
                var items = e.EnumerateArray().ToList();
                if (items.Count == 0) { sb.Append("[]"); return; }
                sb.Append('[');
                for (var i = 0; i < items.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    if (pretty) sb.Append('\n').Append(' ', (depth + 1) * 2);
                    Write(sb, items[i], pretty, sortKeys, depth + 1);
                }
                if (pretty) sb.Append('\n').Append(' ', depth * 2);
                sb.Append(']');
                return;
            case JsonValueKind.String: String(sb, e.GetString()); return;
            case JsonValueKind.Number: sb.Append(Number(e.GetRawText())); return;
            case JsonValueKind.True: sb.Append("true"); return;
            case JsonValueKind.False: sb.Append("false"); return;
            default: sb.Append("null"); return;
        }
    }

    // Integers stay exact; anything with a fraction or exponent is normalised through decimal (or double
    // when out of decimal's range), so 42.001e3 prints as 42001 and 1.50 as 1.5.
    internal static string Number(string raw)
    {
        if (raw.IndexOfAny(['.', 'e', 'E']) < 0) return raw;
        if (decimal.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
        {
            var s = (d / 1.0000000000000000000000000000m).ToString(CultureInfo.InvariantCulture);
            return s == "-0" ? "0" : s;
        }
        return double.Parse(raw, CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture);
    }

    static void String(StringBuilder sb, string s)
    {
        sb.Append('"');
        foreach (var c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                default:
                    if (c < 0x20) sb.Append($"\\u{(int)c:x4}");
                    else sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
    }
}
