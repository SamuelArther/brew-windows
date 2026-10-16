using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace WinTools;

// lttng-ust: LTTng's userspace tracer. Its Windows-native counterpart is ETW with TraceLogging
// (the manifest-free event API in <TraceLoggingProvider.h>). lttng-gen-tp, which turns a .tp template
// of TRACEPOINT_EVENT definitions into C tracepoint boilerplate, is reproduced here: it emits a
// TraceLogging header (and optional .c) so the same source that calls tracepoint(provider, event, ...)
// builds on Windows and the events show up in WPA / tracelog / xperf. No tracing is performed by this
// command; it only generates code, exactly like the upstream lttng-gen-tp.
static class LttngUst
{
    const string Version = "2.16.0";

    [ModuleInitializer]
    internal static void Register() => Commands.Add("lttng-gen-tp", "Generate ETW TraceLogging tracepoints from an LTTng .tp template", Run);

    static int Run(string[] args)
    {
        var outputs = new List<string>();
        string input = null;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            switch (a)
            {
                case "-h" or "--help": Help(); return 0;
                case "-v" or "--version": Console.WriteLine($"lttng-gen-tp (wintools, TraceLogging) {Version}"); return 0;
                case "-o": outputs.Add(i + 1 < args.Length ? args[++i] : throw new UsageException("-o needs a filename")); break;
                default:
                    if (a.StartsWith("-o")) outputs.Add(a[2..]);
                    else if (a.StartsWith('-')) throw new UsageException($"unknown option {a} (see lttng-gen-tp --help)");
                    else input = a;
                    break;
            }
        }
        if (input == null) throw new UsageException("no .tp template given (see lttng-gen-tp --help)");
        if (!File.Exists(input)) throw new IOException($"{input}: No such file");

        var events = Parse(File.ReadAllText(input));
        if (events.Count == 0) throw new InvalidOperationException($"{input}: no TRACEPOINT_EVENT definitions found");
        var providers = events.Select(e => e.Provider).Distinct().ToList();

        // Default to <basename>.h, matching lttng-gen-tp's default output name.
        if (outputs.Count == 0) outputs.Add(Path.ChangeExtension(input, ".h"));
        foreach (var output in outputs)
        {
            var ext = Path.GetExtension(output).ToLowerInvariant();
            var text = ext == ".c" ? GenerateSource(providers, Path.GetFileName(output)) : GenerateHeader(events, providers, output);
            File.WriteAllText(output, text);
            Console.WriteLine(output);
        }
        return 0;
    }

    sealed record Field(string Kind, string Type, string Name, string Expr);
    sealed record Event(string Provider, string Name, List<(string Type, string Name)> Args, List<Field> Fields);

    // Parse the TRACEPOINT_EVENT(provider, name, TP_ARGS(...), TP_FIELDS(...)) blocks.
    static List<Event> Parse(string source)
    {
        source = StripComments(source);
        var events = new List<Event>();
        var pos = 0;
        while (true)
        {
            var at = source.IndexOf("TRACEPOINT_EVENT", pos, StringComparison.Ordinal);
            if (at < 0) break;
            var open = source.IndexOf('(', at);
            if (open < 0) break;
            var body = Balanced(source, open, out pos);
            var parts = SplitTopLevel(body);
            if (parts.Count < 2) continue;
            var provider = parts[0].Trim();
            var name = parts[1].Trim();
            var args = new List<(string, string)>();
            var fields = new List<Field>();
            foreach (var part in parts.Skip(2))
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("TP_ARGS")) args = ParseArgs(Inner(trimmed));
                else if (trimmed.StartsWith("TP_FIELDS")) fields = ParseFields(Inner(trimmed));
            }
            if (provider.Length > 0 && name.Length > 0) events.Add(new Event(provider, name, args, fields));
        }
        return events;
    }

    static List<(string Type, string Name)> ParseArgs(string inner)
    {
        var tokens = SplitTopLevel(inner).Select(t => t.Trim()).Where(t => t.Length > 0).ToList();
        var result = new List<(string, string)>();
        for (var i = 0; i + 1 < tokens.Count; i += 2) result.Add((tokens[i], tokens[i + 1]));
        return result;
    }

    static List<Field> ParseFields(string inner)
    {
        var fields = new List<Field>();
        foreach (Match m in Regex.Matches(inner, @"(ctf_\w+)\s*\(([^()]*)\)"))
        {
            var kind = m.Groups[1].Value;
            var a = SplitTopLevel(m.Groups[2].Value).Select(x => x.Trim()).ToList();
            // ctf_string(name, expr); ctf_integer(type, name, expr); ctf_float(type, name, expr).
            if (kind.StartsWith("ctf_string") && a.Count >= 2) fields.Add(new Field(kind, "char*", a[0], a[1]));
            else if (a.Count >= 3) fields.Add(new Field(kind, a[0], a[1], a[2]));
            else if (a.Count == 2) fields.Add(new Field(kind, "int", a[0], a[1]));
        }
        return fields;
    }

    static string GenerateHeader(List<Event> events, List<string> providers, string path)
    {
        var guard = Regex.Replace(Path.GetFileName(path).ToUpperInvariant(), @"[^A-Z0-9]", "_") + "_";
        var sb = new StringBuilder();
        sb.AppendLine($"/* Generated by lttng-gen-tp (wintools): ETW TraceLogging equivalent of LTTng-UST tracepoints. */");
        sb.AppendLine($"#ifndef {guard}");
        sb.AppendLine($"#define {guard}");
        sb.AppendLine();
        sb.AppendLine("#include <windows.h>");
        sb.AppendLine("#include <TraceLoggingProvider.h>");
        sb.AppendLine();
        sb.AppendLine("/* Call TraceLoggingRegister(<provider>) once at startup and TraceLoggingUnregister() at exit.");
        sb.AppendLine("   Define the provider in exactly one .c/.cpp file (generate it with -o <name>.c). */");
        foreach (var p in providers)
        {
            sb.AppendLine($"TRACELOGGING_DECLARE_PROVIDER({p});");
            sb.AppendLine($"/* Provider GUID derived from the name \"{p}\": {GuidFor(p)} */");
        }
        sb.AppendLine();
        sb.AppendLine("/* tracepoint(provider, event, args...) expands to the matching event macro below. */");
        sb.AppendLine("#define tracepoint(_provider, _event, ...) _tp_##_provider##_##_event(__VA_ARGS__)");
        sb.AppendLine();
        foreach (var e in events)
        {
            var argNames = string.Join(", ", e.Args.Select(a => a.Name));
            sb.AppendLine($"#define _tp_{e.Provider}_{e.Name}({argNames}) \\");
            sb.AppendLine($"    TraceLoggingWrite({e.Provider}, \"{e.Name}\"{(e.Fields.Count > 0 ? ", \\" : ")")}");
            for (var i = 0; i < e.Fields.Count; i++)
            {
                var f = e.Fields[i];
                var last = i == e.Fields.Count - 1;
                sb.AppendLine($"        {Mapping(f)}{(last ? ")" : ", \\")}");
            }
            sb.AppendLine();
        }
        sb.AppendLine($"#endif /* {guard} */");
        return sb.ToString();
    }

    static string GenerateSource(List<string> providers, string headerName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("/* Generated by lttng-gen-tp (wintools): TraceLogging provider definitions. */");
        sb.AppendLine($"#include \"{Path.ChangeExtension(headerName, ".h")}\"");
        sb.AppendLine();
        foreach (var p in providers)
            sb.AppendLine($"TRACELOGGING_DEFINE_PROVIDER({p}, \"{p}\", ({GuidBody(p)}));");
        sb.AppendLine();
        sb.AppendLine("/* Register all providers; call once at startup. */");
        sb.AppendLine("void lttng_tp_register(void) {");
        foreach (var p in providers) sb.AppendLine($"    TraceLoggingRegister({p});");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("void lttng_tp_unregister(void) {");
        foreach (var p in providers) sb.AppendLine($"    TraceLoggingUnregister({p});");
        sb.AppendLine("}");
        return sb.ToString();
    }

    static string Mapping(Field f)
    {
        if (f.Kind.StartsWith("ctf_string")) return $"TraceLoggingString({f.Expr}, \"{f.Name}\")";
        if (f.Kind.StartsWith("ctf_float")) return $"TraceLoggingFloat64((double)({f.Expr}), \"{f.Name}\")";
        var macro = f.Type.ToLowerInvariant() switch
        {
            var t when t.Contains("unsigned") && t.Contains("long long") => "TraceLoggingUInt64",
            var t when t.Contains("long long") => "TraceLoggingInt64",
            var t when t.Contains("unsigned") => "TraceLoggingUInt32",
            var t when t.Contains("size_t") || t.Contains("uint64") => "TraceLoggingUInt64",
            var t when t.Contains("int64") => "TraceLoggingInt64",
            _ => "TraceLoggingInt32",
        };
        return $"{macro}(({f.Type})({f.Expr}), \"{f.Name}\")";
    }

    // ---- small parsing helpers ----

    static string StripComments(string s)
    {
        s = Regex.Replace(s, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        s = Regex.Replace(s, @"//[^\n]*", " ");
        return s;
    }

    // Content between the parens starting at `open`; sets `after` to just past the close paren.
    static string Balanced(string s, int open, out int after)
    {
        var depth = 0;
        for (var i = open; i < s.Length; i++)
        {
            if (s[i] == '(') depth++;
            else if (s[i] == ')' && --depth == 0) { after = i + 1; return s.Substring(open + 1, i - open - 1); }
        }
        after = s.Length;
        return s[(open + 1)..];
    }

    static string Inner(string call)
    {
        var open = call.IndexOf('(');
        return open < 0 ? "" : Balanced(call, open, out _);
    }

    // Split on commas that are not inside parentheses.
    static List<string> SplitTopLevel(string s)
    {
        var parts = new List<string>();
        var depth = 0;
        var start = 0;
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] == '(') depth++;
            else if (s[i] == ')') depth--;
            else if (s[i] == ',' && depth == 0) { parts.Add(s[start..i]); start = i + 1; }
        }
        if (start < s.Length) parts.Add(s[start..]);
        return parts;
    }

    // A stable GUID derived from the provider name (FNV-1a over the name, filled into 16 bytes), so
    // the generated code compiles; replace it with a registered GUID if you need a fixed identity.
    static byte[] GuidBytes(string name)
    {
        var bytes = new byte[16];
        ulong hash = 14695981039346656037;
        for (var i = 0; i < 16; i++)
        {
            foreach (var c in name) { hash ^= (byte)(c + i); hash *= 1099511628211; }
            bytes[i] = (byte)(hash >> (8 * (i % 8)));
        }
        return bytes;
    }

    static string GuidFor(string name)
    {
        var b = GuidBytes(name);
        return new Guid(b).ToString();
    }

    static string GuidBody(string name)
    {
        var g = new Guid(GuidBytes(name));
        var b = g.ToByteArray();
        // TraceLogging's provider-id form: (data1, data2, data3, b0,...,b7).
        return $"0x{BitConverter.ToUInt32(b, 0):x8}, 0x{BitConverter.ToUInt16(b, 4):x4}, 0x{BitConverter.ToUInt16(b, 6):x4}, " +
               string.Join(", ", b.Skip(8).Select(x => $"0x{x:x2}"));
    }

    static void Help() => Console.WriteLine("""
        Usage: lttng-gen-tp [-o OUTPUT] TEMPLATE.tp

        Generate ETW TraceLogging tracepoints from an LTTng-UST .tp template (the Windows-native
        equivalent of lttng-ust). With no -o, writes TEMPLATE.h. Give -o <name>.h for the header
        and -o <name>.c for the provider definitions; include the header and call tracepoint(...)
        exactly as with LTTng-UST.

        Options:
          -o OUTPUT       Output file (.h for the header, .c for provider definitions)
          -v, --version   Print version
          -h, --help      Show this help
        """);
}
