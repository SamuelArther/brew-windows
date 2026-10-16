using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;

namespace WinTools;

// wayland-scanner: turns Wayland protocol XML into client/server headers and the wl_interface
// marshalling tables, the way the C scanner from the wayland formula does. It is the piece of
// Wayland every protocol consumer needs at build time, and it is pure text processing, so the
// output is the same C as on Linux. libwayland itself needs a compositor (on Windows: WSLg).
// QtWaylandScanner.cs adds Qt's qtwaylandscanner on the same protocol parser.
static partial class WaylandScanner
{
    const string Version = "1.26.0";

    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("wayland-scanner", "Generate C code and headers from Wayland protocol XML", Run);

    enum ArgType { Int, Unsigned, Fixed, String, Object, NewId, Array, Fd }

    sealed class Description { public string Summary, Text; }

    sealed class Arg
    {
        public string Name, InterfaceName, Summary, Enum;
        public ArgType Type;
        public bool Nullable;
    }

    sealed class Message
    {
        public string Name, Uppercase;
        public List<Arg> Args = new();
        public int Since = 1, DeprecatedSince, NewIdCount, TypeIndex;
        public bool Destructor, AllNull;
        public Description Description;
        public int Line;
    }

    sealed class Entry
    {
        public string Name, Uppercase, Value, Summary;
        public int Since = 1, DeprecatedSince;
        public Description Description;
    }

    sealed class Enumeration
    {
        public string Name, Uppercase;
        public bool Bitfield;
        public int Since = 1;
        public List<Entry> Entries = new();
        public Description Description;
    }

    sealed class Interface
    {
        public string Name, Uppercase;
        public int Version, Line;
        public List<Message> Requests = new(), Events = new();
        public List<Enumeration> Enums = new();
        public Description Description;
    }

    sealed class Protocol
    {
        public string Name, Uppercase, Copyright;
        public Description Description;
        public List<Interface> Interfaces = new();
        public int NullRunLength, TypeIndex;
        public bool CoreHeaders;
    }

    sealed class ScannerError(string message) : Exception(message);

    static int Run(string[] args)
    {
        bool core = false, strict = false;
        var rest = new List<string>();
        foreach (var arg in args)
        {
            switch (arg)
            {
                case "-h" or "--help":
                    Console.WriteLine(Help);
                    return 0;
                case "-v" or "--version":
                    Console.Error.WriteLine($"wayland-scanner {Version}");
                    return 0;
                case "-c" or "--include-core-only": core = true; break;
                case "-s" or "--strict": strict = true; break;
                default:
                    if (arg.StartsWith('-') && arg.Length > 1) throw new UsageException($"unknown option {arg} (see wayland-scanner --help)");
                    rest.Add(arg);
                    break;
            }
        }
        if (rest.Count is not (1 or 3)) throw new UsageException("expected a mode, optionally followed by input and output files (see --help)");
        var mode = rest[0];
        if (mode is not ("client-header" or "server-header" or "enum-header" or "private-code" or "public-code" or "code"))
            throw new UsageException($"unknown mode '{mode}' (see --help)");
        if (mode == "code")
            Console.Error.WriteLine("Using \"code\" is deprecated - use private-code or public-code.\nSee the help page for details.");

        var inputName = rest.Count == 3 ? rest[1] : "<stdin>";
        string xml;
        try { xml = rest.Count == 3 ? File.ReadAllText(rest[1]) : Console.In.ReadToEnd(); }
        catch (IOException e) { Console.Error.WriteLine($"Could not open input file: {e.Message}"); return 1; }

        Protocol protocol;
        try { protocol = Parse(xml, inputName, strict); }
        catch (ScannerError e) { Console.Error.WriteLine(e.Message); return 1; }
        catch (XmlException e)
        {
            Console.Error.WriteLine($"{inputName}:{e.LineNumber}: error: {e.Message}");
            return 1;
        }
        protocol.CoreHeaders = core;

        var output = new StringBuilder();
        try
        {
            switch (mode)
            {
                case "client-header": EmitHeader(output, protocol, server: false); break;
                case "server-header": EmitHeader(output, protocol, server: true); break;
                case "enum-header": EmitEnumHeader(output, protocol); break;
                case "private-code": EmitCode(output, protocol, isPublic: false); break;
                default: EmitCode(output, protocol, isPublic: true); break;
            }
        }
        catch (ScannerError e) { Console.Error.WriteLine($"{inputName}: {e.Message}"); return 1; }

        var bytes = new UTF8Encoding(false).GetBytes(output.ToString());
        if (rest.Count == 3)
        {
            try { File.WriteAllBytes(rest[2], bytes); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Could not open output file: {e.Message}");
                return 1;
            }
        }
        else
        {
            using var stdout = Console.OpenStandardOutput();
            stdout.Write(bytes);
        }
        return 0;
    }

    const string Help = """
        usage: wayland-scanner [OPTION] [client-header|server-header|enum-header|private-code|public-code] [input_file output_file]

        Converts XML protocol descriptions supplied on stdin or input file to client
        headers, server headers, or protocol marshalling code.

        Use "public-code" only if the marshalling code will be public - aka DSO will export it while "private-code" if the code will not be exported.

        options:
            -h,  --help                  display this help and exit.
            -v,  --version               print the wayland library version that
                                         the scanner was built against.
            -c,  --include-core-only     include the core version of the headers,
                                         that is e.g. wayland-client-core.h instead
                                         of wayland-client.h.
            -s,  --strict                exit immediately with an error if DTD
                                         verification fails.
        """;

    // ---- parsing ----

    static readonly Dictionary<string, string[]> AllowedAttributes = new()
    {
        ["protocol"] = ["name"],
        ["copyright"] = [],
        ["interface"] = ["name", "version", "frozen"],
        ["request"] = ["name", "type", "since", "deprecated-since"],
        ["event"] = ["name", "type", "since", "deprecated-since"],
        ["arg"] = ["name", "type", "summary", "interface", "allow-null", "enum"],
        ["enum"] = ["name", "since", "bitfield"],
        ["entry"] = ["name", "value", "summary", "since", "deprecated-since"],
        ["description"] = ["summary"],
    };

    static Protocol Parse(string xml, string file, bool strict)
    {
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, IgnoreComments = true, IgnoreWhitespace = false };
        using var reader = XmlReader.Create(new StringReader(xml), settings);
        var info = (IXmlLineInfo)reader;
        Protocol protocol = null;
        Interface iface = null;
        Message message = null;
        Enumeration enumeration = null;
        Entry entry = null;
        Description description = null;
        StringBuilder text = null;
        var stack = new Stack<string>();

        ScannerError Fail(string what) => new($"{file}:{info.LineNumber}: error: {what}");

        while (reader.Read())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                {
                    var name = reader.Name;
                    var empty = reader.IsEmptyElement;
                    var attrs = new Dictionary<string, string>();
                    if (reader.MoveToFirstAttribute())
                    {
                        do attrs[reader.Name] = reader.Value; while (reader.MoveToNextAttribute());
                        reader.MoveToElement();
                    }
                    if (!AllowedAttributes.TryGetValue(name, out var allowed))
                    {
                        if (strict) throw Fail($"unknown element {name}");
                        Console.Error.WriteLine($"{file}:{info.LineNumber}: warning: unknown element {name}");
                    }
                    else if (strict && attrs.Keys.FirstOrDefault(a => !allowed.Contains(a)) is { } bad)
                        throw Fail($"attribute {bad} not allowed on element {name}");

                    string Attr(string key) => attrs.GetValueOrDefault(key);
                    string Required(string key) => Attr(key) ?? throw Fail($"no {key} attribute on element {name}");
                    int Version(string key, int fallback)
                    {
                        var v = Attr(key);
                        if (v == null) return fallback;
                        if (!int.TryParse(v, out var n) || n < 1) throw Fail($"invalid integer ({v})");
                        return n;
                    }

                    switch (name)
                    {
                        case "protocol":
                            protocol = new Protocol { Name = Identifier(Required("name"), Fail) };
                            protocol.Uppercase = protocol.Name.ToUpperInvariant();
                            break;
                        case "copyright":
                            text = new StringBuilder();
                            break;
                        case "interface":
                            if (protocol == null) throw Fail("interface outside protocol");
                            iface = new Interface { Name = Identifier(Required("name"), Fail), Line = info.LineNumber };
                            iface.Uppercase = iface.Name.ToUpperInvariant();
                            iface.Version = Version("version", 0);
                            if (iface.Version == 0) throw Fail("no interface version given");
                            protocol.Interfaces.Add(iface);
                            break;
                        case "request" or "event":
                            if (iface == null) throw Fail($"{name} outside interface");
                            message = new Message { Name = Identifier(Required("name"), Fail), Line = info.LineNumber };
                            message.Uppercase = message.Name.ToUpperInvariant();
                            message.Destructor = Attr("type") == "destructor";
                            if (Attr("type") is { } t && t != "destructor") throw Fail($"invalid message type ({t})");
                            message.Since = Version("since", 1);
                            message.DeprecatedSince = Attr("deprecated-since") == null ? 0 : Version("deprecated-since", 0);
                            if (message.Since > iface.Version)
                                throw Fail($"since ({message.Since}) larger than version ({iface.Version})");
                            (name == "request" ? iface.Requests : iface.Events).Add(message);
                            break;
                        case "arg":
                        {
                            if (message == null) throw Fail("arg outside message");
                            var arg = new Arg { Name = Identifier(Required("name"), Fail), Summary = Attr("summary"), Enum = Attr("enum") };
                            arg.Type = Required("type") switch
                            {
                                "int" => ArgType.Int,
                                "uint" => ArgType.Unsigned,
                                "fixed" => ArgType.Fixed,
                                "string" => ArgType.String,
                                "object" => ArgType.Object,
                                "new_id" => ArgType.NewId,
                                "array" => ArgType.Array,
                                "fd" => ArgType.Fd,
                                var other => throw Fail($"unknown type ({other})"),
                            };
                            if (arg.Type == ArgType.NewId) message.NewIdCount++;
                            if (Attr("interface") is { } ifaceName)
                            {
                                if (arg.Type is not (ArgType.Object or ArgType.NewId))
                                    throw Fail($"interface attribute not allowed for type {Attr("type")}");
                                arg.InterfaceName = Identifier(ifaceName, Fail);
                            }
                            switch (Attr("allow-null"))
                            {
                                case null or "false": break;
                                case "true":
                                    if (arg.Type is not (ArgType.String or ArgType.Object or ArgType.NewId or ArgType.Array))
                                        throw Fail("allow-null is only valid for objects, strings, and arrays");
                                    arg.Nullable = true;
                                    break;
                                default: throw Fail("invalid value for allow-null attribute");
                            }
                            if (arg.Enum != null && arg.Type is not (ArgType.Int or ArgType.Unsigned))
                                throw Fail("enumeration-style argument has wrong type");
                            message.Args.Add(arg);
                            break;
                        }
                        case "enum":
                            if (iface == null) throw Fail("enum outside interface");
                            enumeration = new Enumeration { Name = Identifier(Required("name"), Fail) };
                            enumeration.Uppercase = enumeration.Name.ToUpperInvariant();
                            enumeration.Since = Version("since", 1);
                            enumeration.Bitfield = Attr("bitfield") switch
                            {
                                null or "false" => false,
                                "true" => true,
                                _ => throw Fail("invalid value for bitfield attribute"),
                            };
                            iface.Enums.Add(enumeration);
                            break;
                        case "entry":
                        {
                            if (enumeration == null) throw Fail("entry outside enum");
                            entry = new Entry { Name = Required("name"), Value = Required("value"), Summary = Attr("summary") };
                            if (entry.Name.Length == 0 || !entry.Name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_'))
                                throw Fail($"invalid entry name ({entry.Name})");
                            entry.Uppercase = entry.Name.ToUpperInvariant();
                            if (!ValidValue(entry.Value)) throw Fail($"invalid integer ({entry.Value})");
                            entry.Since = Version("since", 1);
                            entry.DeprecatedSince = Attr("deprecated-since") == null ? 0 : Version("deprecated-since", 0);
                            if (entry.Since < enumeration.Since)
                                throw Fail("since attribute on entry is smaller than the enum's");
                            enumeration.Entries.Add(entry);
                            break;
                        }
                        case "description":
                            description = new Description { Summary = Attr("summary") };
                            if (stack.TryPeek(out var parent))
                            {
                                switch (parent)
                                {
                                    case "protocol": protocol.Description = description; break;
                                    case "interface": iface.Description = description; break;
                                    case "request" or "event": message.Description = description; break;
                                    case "enum": enumeration.Description = description; break;
                                    case "entry": entry.Description = description; break;
                                }
                            }
                            if (strict && description.Summary == null) throw Fail("description without summary");
                            text = new StringBuilder();
                            break;
                    }
                    stack.Push(name);
                    if (empty) EndElement(name);
                    break;
                }
                case XmlNodeType.EndElement:
                    EndElement(reader.Name);
                    break;
                case XmlNodeType.Text or XmlNodeType.CDATA or XmlNodeType.Whitespace or XmlNodeType.SignificantWhitespace:
                    text?.Append(reader.Value);
                    break;
            }
        }

        void EndElement(string name)
        {
            stack.Pop();
            switch (name)
            {
                case "copyright":
                    protocol.Copyright = text.Length > 0 ? text.ToString() : null;
                    text = null;
                    break;
                case "description":
                    description.Text = text.Length > 0 ? text.ToString() : null;
                    text = null;
                    break;
                case "request" or "event":
                    if (message.Destructor && message.Args.Any(a => a.Type == ArgType.NewId))
                        throw new ScannerError($"{file}:{message.Line}: error: destructor with new_id arg");
                    message = null;
                    break;
                case "interface":
                    iface = null;
                    break;
                case "enum":
                    if (enumeration.Entries.Count == 0)
                        throw new ScannerError($"{file}:{info.LineNumber}: error: enumeration {enumeration.Name} was empty");
                    enumeration = null;
                    break;
                case "entry":
                    entry = null;
                    break;
            }
        }

        if (protocol == null) throw new ScannerError($"{file}:1: error: no protocol element");
        // Enum references must name an enum of an interface in this file (or another file: "iface.enum").
        foreach (var i in protocol.Interfaces)
            foreach (var m in i.Requests.Concat(i.Events))
                foreach (var a in m.Args.Where(a => a.Enum != null))
                {
                    var dot = a.Enum.IndexOf('.');
                    var owner = dot < 0 ? i : protocol.Interfaces.FirstOrDefault(x => x.Name == a.Enum[..dot]);
                    var target = owner?.Enums.FirstOrDefault(e => e.Name == (dot < 0 ? a.Enum : a.Enum[(dot + 1)..]));
                    if (target is { Bitfield: true } && a.Type == ArgType.Int)
                        throw new ScannerError($"{file}:{m.Line}: error: bitfield-style enum must only be referenced by uint");
                    if (owner != null && target == null)
                        throw new ScannerError($"{file}:{m.Line}: error: could not find enumeration {a.Enum}");
                }
        return protocol;
    }

    static string Identifier(string name, Func<string, ScannerError> fail)
    {
        if (name.Length == 0 || !(char.IsAsciiLetter(name[0]) || name[0] == '_')
            || !name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_'))
            throw fail($"'{name}' is not a valid identifier");
        return name;
    }

    // Decimal, hex (0x) or negative decimal, like strtol with base 0 accepts.
    static bool ValidValue(string v) =>
        v.Length > 0 && (v.StartsWith("0x") || v.StartsWith("0X")
            ? v.Length > 2 && v[2..].All(char.IsAsciiHexDigit)
            : (v[0] == '-' ? v.Length > 1 && v[1..].All(char.IsAsciiDigit) : v.All(char.IsAsciiDigit)));

    // ---- shared output helpers ----

    // n/8 tabs then n%8 spaces, as the C scanner indents continuation lines.
    static string Indent(int n) => new string('\t', n / 8) + new string(' ', n % 8);

    // Re-flows text into a comment: each line trimmed of leading blanks and prefixed " *".
    static void FormatTextToComment(StringBuilder o, string text, bool standalone)
    {
        bool bol = true, started = !standalone;
        int start = 0;
        for (int i = 0; i <= text.Length; i++)
        {
            char c = i < text.Length ? text[i] : '\0';
            if (bol && (c == ' ' || c == '\t')) continue;
            if (bol) { bol = false; start = i; }
            if (c == '\n' || (c == '\0' && start != i))
            {
                o.Append(started ? " *" : "/*").Append(i > start ? " " : "").Append(text, start, i - start).Append('\n');
                bol = true;
                started = true;
            }
        }
        if (started && standalone) o.Append(" */\n\n");
    }

    // Word-wraps a description at column 72 under a "prefix * " lead-in.
    static void DescDump(StringBuilder o, string desc, string prefix)
    {
        int col = 0, i;
        for (i = 0; i < prefix.Length && prefix[i] != '*'; i++) col = prefix[i] == '\t' ? (col + 8) & ~7 : col + 1;
        o.Append(prefix);
        if (desc == null) { o.Append("(none)\n"); return; }
        int startcol = col;
        col += prefix.Length - i;
        char hang = col - startcol > 2 ? '\t' : ' ';
        for (i = 0; i < desc.Length;)
        {
            int k = i, newlines = 0;
            while (i < desc.Length && char.IsWhiteSpace(desc[i])) { if (desc[i] == '\n') newlines++; i++; }
            if (i >= desc.Length) break;
            int j = i;
            while (i < desc.Length && !char.IsWhiteSpace(desc[i])) i++;
            if (newlines > 1) o.Append('\n').Append(Indent(startcol)).Append('*');
            if (newlines > 1 || col + i - j > 72)
            {
                o.Append('\n').Append(Indent(startcol)).Append('*').Append(hang);
                col = startcol;
            }
            if (col > startcol && k > 0) { o.Append(' '); col++; }
            o.Append(desc, j, i - j);
            col += i - j;
        }
        o.Append('\n');
    }

    static string TypeName(Arg a) => a.Type switch
    {
        ArgType.NewId or ArgType.Unsigned => "uint32_t ",
        ArgType.Fixed => "wl_fixed_t ",
        ArgType.String => "const char *",
        ArgType.Object => a.InterfaceName == null ? "void *" : $"struct {a.InterfaceName} *",
        ArgType.Array => "struct wl_array *",
        _ => "int32_t ",
    };

    // Interfaces referenced by object/new_id arguments; also finds the longest all-untyped message.
    static void ForwardDeclarations(Protocol p, List<Message> messages, List<string> types)
    {
        foreach (var m in messages)
        {
            int length = 0;
            m.AllNull = true;
            foreach (var a in m.Args)
            {
                // An untyped new_id is marshalled as three arguments (interface name, version, id).
                length += a.Type == ArgType.NewId && a.InterfaceName == null ? 3 : 1;
                if (a.Type is ArgType.NewId or ArgType.Object && a.InterfaceName != null)
                {
                    m.AllNull = false;
                    types.Add(a.InterfaceName);
                }
            }
            if (m.AllNull && length > p.NullRunLength) p.NullRunLength = length;
        }
    }

    static List<string> SortedTypes(Protocol p, bool includeInterfaces)
    {
        var types = new List<string>();
        foreach (var i in p.Interfaces)
        {
            ForwardDeclarations(p, i.Requests, types);
            ForwardDeclarations(p, i.Events, types);
        }
        if (includeInterfaces) types.AddRange(p.Interfaces.Select(i => i.Name));
        return types.Distinct().Order(StringComparer.Ordinal).ToList();
    }

    // ---- headers ----

    static void EmitHeader(StringBuilder o, Protocol p, bool server)
    {
        var side = server ? "SERVER" : "CLIENT";
        var include = server ? (p.CoreHeaders ? "wayland-server-core.h" : "wayland-server.h")
                             : (p.CoreHeaders ? "wayland-client-core.h" : "wayland-client.h");
        o.Append($"/* Generated by wayland-scanner {Version} */\n\n");
        o.Append($"#ifndef {p.Uppercase}_{side}_PROTOCOL_H\n#define {p.Uppercase}_{side}_PROTOCOL_H\n\n");
        o.Append($"#include <stdint.h>\n#include <stddef.h>\n#include \"{include}\"\n\n");
        o.Append("#ifdef  __cplusplus\nextern \"C\" {\n#endif\n\n");
        if (server) o.Append("struct wl_client;\nstruct wl_resource;\n\n");

        EmitMainpageBlurb(o, p);

        foreach (var t in SortedTypes(p, includeInterfaces: true)) o.Append($"struct {t};\n");
        o.Append('\n');

        foreach (var i in p.Interfaces)
        {
            o.Append($"#ifndef {i.Uppercase}_INTERFACE\n#define {i.Uppercase}_INTERFACE\n");
            o.Append($"/**\n * @page page_iface_{i.Name} {i.Name}\n");
            if (i.Description?.Text != null)
            {
                o.Append($" * @section page_iface_{i.Name}_desc Description\n");
                FormatTextToComment(o, i.Description.Text, false);
            }
            o.Append($" * @section page_iface_{i.Name}_api API\n * See @ref iface_{i.Name}.\n */\n");
            o.Append($"/**\n * @defgroup iface_{i.Name} The {i.Name} interface\n");
            if (i.Description?.Text != null) FormatTextToComment(o, i.Description.Text, false);
            o.Append(" */\n");
            o.Append($"extern const struct wl_interface {i.Name}_interface;\n#endif\n");
        }
        o.Append('\n');

        foreach (var i in p.Interfaces)
        {
            EmitEnumerations(o, i, withValidators: server);
            if (server)
            {
                EmitStructs(o, i.Requests, i, server: true);
                EmitOpcodes(o, i.Events, i);
                EmitOpcodeVersions(o, i.Events, i);
                EmitOpcodeVersions(o, i.Requests, i);
                EmitEventWrappers(o, i.Events, i);
            }
            else
            {
                EmitStructs(o, i.Events, i, server: false);
                EmitOpcodes(o, i.Requests, i);
                EmitOpcodeVersions(o, i.Events, i);
                EmitOpcodeVersions(o, i.Requests, i);
                EmitStubs(o, i.Requests, i);
            }
        }
        o.Append("#ifdef  __cplusplus\n}\n#endif\n\n#endif\n");
    }

    static void EmitEnumHeader(StringBuilder o, Protocol p)
    {
        o.Append($"/* Generated by wayland-scanner {Version} */\n\n");
        o.Append($"#ifndef {p.Uppercase}_ENUM_PROTOCOL_H\n#define {p.Uppercase}_ENUM_PROTOCOL_H\n\n");
        o.Append("#ifdef  __cplusplus\nextern \"C\" {\n#endif\n\n");
        foreach (var i in p.Interfaces) EmitEnumerations(o, i, withValidators: false);
        o.Append("#ifdef  __cplusplus\n}\n#endif\n\n#endif\n");
    }

    static void EmitMainpageBlurb(StringBuilder o, Protocol p)
    {
        o.Append($"/**\n * @page page_{p.Name} The {p.Name} protocol\n");
        if (p.Description != null)
        {
            if (p.Description.Summary != null) o.Append($" * {p.Description.Summary}\n *\n");
            if (p.Description.Text != null)
            {
                o.Append($" * @section page_desc_{p.Name} Description\n");
                FormatTextToComment(o, p.Description.Text, false);
                o.Append(" *\n");
            }
        }
        o.Append($" * @section page_ifaces_{p.Name} Interfaces\n");
        foreach (var i in p.Interfaces)
            o.Append($" * - @subpage page_iface_{i.Name} - {i.Description?.Summary ?? ""}\n");
        if (p.Copyright != null)
        {
            o.Append($" * @section page_copyright_{p.Name} Copyright\n * <pre>\n");
            FormatTextToComment(o, p.Copyright, false);
            o.Append(" * </pre>\n");
        }
        o.Append(" */\n");
    }

    static void EmitEnumerations(StringBuilder o, Interface i, bool withValidators)
    {
        foreach (var e in i.Enums)
        {
            o.Append($"#ifndef {i.Uppercase}_{e.Uppercase}_ENUM\n#define {i.Uppercase}_{e.Uppercase}_ENUM\n");
            if (e.Description != null)
            {
                o.Append($"/**\n * @ingroup iface_{i.Name}\n");
                FormatTextToComment(o, e.Description.Summary ?? "", false);
                if (e.Description.Text != null) FormatTextToComment(o, e.Description.Text, false);
                o.Append(" */\n");
            }
            o.Append($"enum {i.Name}_{e.Name} {{\n");
            foreach (var entry in e.Entries)
            {
                var desc = entry.Description;
                if (entry.Summary != null || entry.Since > 1 || desc != null || entry.DeprecatedSince != 0)
                {
                    o.Append("\t/**\n");
                    if (entry.Summary != null) o.Append($"\t * {entry.Summary}\n");
                    if (desc != null)
                    {
                        o.Append($"\t * {desc.Summary}\n\t *\n");
                        if (desc.Text != null) DescDump(o, desc.Text, "\t * ");
                    }
                    if (entry.Since > 1) o.Append($"\t * @since {entry.Since}\n");
                    if (entry.DeprecatedSince != 0) o.Append($"\t * @deprecated Deprecated since version {entry.DeprecatedSince}\n");
                    o.Append("\t */\n");
                }
                o.Append($"\t{i.Uppercase}_{e.Uppercase}_{entry.Uppercase} = {entry.Value},\n");
            }
            o.Append("};\n");
            foreach (var entry in e.Entries.Where(x => x.Since != 1))
            {
                o.Append($"/**\n * @ingroup iface_{i.Name}\n */\n");
                o.Append($"#define {i.Uppercase}_{e.Uppercase}_{entry.Uppercase}_SINCE_VERSION {entry.Since}\n");
            }
            o.Append($"#endif /* {i.Uppercase}_{e.Uppercase}_ENUM */\n\n");
            if (withValidators) EmitValidator(o, i, e);
        }
    }

    static void EmitValidator(StringBuilder o, Interface i, Enumeration e)
    {
        o.Append($"#ifndef {i.Uppercase}_{e.Uppercase}_ENUM_IS_VALID\n#define {i.Uppercase}_{e.Uppercase}_ENUM_IS_VALID\n");
        o.Append($"/**\n * @ingroup iface_{i.Name}\n * Validate a {i.Name} {e.Name} value.\n *\n");
        o.Append($" * @return true on success, false on error.\n * @ref {i.Name}_{e.Name}\n */\n");
        o.Append($"static inline bool\n{i.Name}_{e.Name}_is_valid(uint32_t value, uint32_t version) {{\n");
        if (e.Bitfield)
        {
            o.Append("\tuint32_t valid = 0;\n");
            foreach (var entry in e.Entries)
                o.Append($"\tif (version >= {entry.Since})\n\t\tvalid |= {i.Uppercase}_{e.Uppercase}_{entry.Uppercase};\n");
            o.Append("\treturn (value & ~valid) == 0;\n");
        }
        else
        {
            o.Append("\tswitch (value) {\n");
            foreach (var entry in e.Entries)
                o.Append($"\tcase {(entry.Value.StartsWith('-') ? "(uint32_t)" : "")}{i.Uppercase}_{e.Uppercase}_{entry.Uppercase}:\n\t\treturn version >= {entry.Since};\n");
            o.Append("\tdefault:\n\t\treturn false;\n\t}\n");
        }
        o.Append("}\n");
        o.Append($"#endif /* {i.Uppercase}_{e.Uppercase}_ENUM_IS_VALID */\n\n");
    }

    static void EmitStructs(StringBuilder o, List<Message> messages, Interface i, bool server)
    {
        if (messages.Count == 0) return;
        var kind = server ? "interface" : "listener";
        o.Append($"/**\n * @ingroup iface_{i.Name}\n * @struct {i.Name}_{kind}\n */\n");
        o.Append($"struct {i.Name}_{kind} {{\n");
        foreach (var m in messages)
        {
            o.Append("\t/**\n");
            if (m.Description != null)
            {
                if (m.Description.Summary != null) o.Append($"\t * {m.Description.Summary}\n");
                o.Append("\t *\n");
                DescDump(o, m.Description.Text, "\t * ");
            }
            foreach (var a in m.Args)
            {
                if (server && a.Type == ArgType.NewId && a.InterfaceName == null)
                    o.Append("\t * @param interface name of the objects interface\n\t * @param version version of the objects interface\n");
                if (a.Summary != null) o.Append($"\t * @param {a.Name} {a.Summary}\n");
            }
            if (m.Since > 1) o.Append($"\t * @since {m.Since}\n");
            if (m.DeprecatedSince != 0) o.Append($"\t * @deprecated Deprecated since version {m.DeprecatedSince}\n");
            o.Append("\t */\n");
            o.Append($"\tvoid (*{m.Name})(");
            int n = m.Name.Length + 17;
            if (server) o.Append($"struct wl_client *client,\n{Indent(n)}struct wl_resource *resource");
            else o.Append($"void *data,\n{Indent(n)}struct {i.Name} *{i.Name}");
            foreach (var a in m.Args)
            {
                o.Append(",\n").Append(Indent(n));
                if (server && a.Type == ArgType.Object) o.Append("struct wl_resource *");
                else if (server && a.Type == ArgType.NewId && a.InterfaceName == null) o.Append("const char *interface, uint32_t version, uint32_t ");
                else if (!server && a.Type == ArgType.Object && a.InterfaceName == null) o.Append("void *");
                else if (!server && a.Type == ArgType.NewId) o.Append($"struct {a.InterfaceName} *");
                else o.Append(TypeName(a));
                o.Append(a.Name);
            }
            o.Append(");\n");
        }
        o.Append("};\n\n");
        if (!server)
        {
            o.Append($"/**\n * @ingroup iface_{i.Name}\n */\n");
            o.Append($"static inline int\n{i.Name}_add_listener(struct {i.Name} *{i.Name},\n");
            o.Append($"{Indent(14 + i.Name.Length)}const struct {i.Name}_listener *listener, void *data)\n{{\n");
            o.Append($"\treturn wl_proxy_add_listener((struct wl_proxy *) {i.Name},\n{Indent(37)}(void (**)(void)) listener, data);\n}}\n\n");
        }
    }

    static void EmitOpcodes(StringBuilder o, List<Message> messages, Interface i)
    {
        if (messages.Count == 0) return;
        for (int n = 0; n < messages.Count; n++) o.Append($"#define {i.Uppercase}_{messages[n].Uppercase} {n}\n");
        o.Append('\n');
    }

    static void EmitOpcodeVersions(StringBuilder o, List<Message> messages, Interface i)
    {
        foreach (var m in messages)
        {
            o.Append($"/**\n * @ingroup iface_{i.Name}\n */\n");
            o.Append($"#define {i.Uppercase}_{m.Uppercase}_SINCE_VERSION {m.Since}\n");
        }
        o.Append('\n');
    }

    static void EmitStubs(StringBuilder o, List<Message> messages, Interface i)
    {
        var n = i.Name;
        o.Append($"/** @ingroup iface_{n} */\nstatic inline void\n{n}_set_user_data(struct {n} *{n}, void *user_data)\n{{\n");
        o.Append($"\twl_proxy_set_user_data((struct wl_proxy *) {n}, user_data);\n}}\n\n");
        o.Append($"/** @ingroup iface_{n} */\nstatic inline void *\n{n}_get_user_data(struct {n} *{n})\n{{\n");
        o.Append($"\treturn wl_proxy_get_user_data((struct wl_proxy *) {n});\n}}\n\n");
        o.Append($"static inline uint32_t\n{n}_get_version(struct {n} *{n})\n{{\n");
        o.Append($"\treturn wl_proxy_get_version((struct wl_proxy *) {n});\n}}\n\n");

        var hasDestroy = messages.Any(m => m.Name == "destroy");
        if (hasDestroy && !messages.Any(m => m.Destructor))
            throw new ScannerError($"{i.Line}: error: interface '{n}' has method named destroy but no destructor");
        if (!hasDestroy && n != "wl_display")
        {
            o.Append($"/** @ingroup iface_{n} */\nstatic inline void\n{n}_destroy(struct {n} *{n})\n{{\n");
            o.Append($"\twl_proxy_destroy((struct wl_proxy *) {n});\n}}\n\n");
        }
        foreach (var m in messages)
        {
            if (m.NewIdCount > 1)
            {
                Console.Error.WriteLine($"warning: request '{n}::{m.Name}' has more than one new_id arg, not emitting stub");
                continue;
            }
            var ret = m.Args.LastOrDefault(a => a.Type == ArgType.NewId);
            o.Append($"/**\n * @ingroup iface_{n}\n");
            if (m.Description?.Text != null) FormatTextToComment(o, m.Description.Text, false);
            o.Append(" */\n");
            o.Append(ret == null ? "static inline void\n" : ret.InterfaceName == null ? "static inline void *\n" : $"static inline struct {ret.InterfaceName} *\n");
            o.Append($"{n}_{m.Name}(struct {n} *{n}");
            foreach (var a in m.Args)
            {
                if (a.Type == ArgType.NewId && a.InterfaceName == null) { o.Append(", const struct wl_interface *interface, uint32_t version"); continue; }
                if (a.Type == ArgType.NewId) continue;
                o.Append(", ").Append(TypeName(a)).Append(a.Name);
            }
            o.Append(")\n{\n\t");
            if (ret != null) o.Append($"struct wl_proxy *{ret.Name};\n\n\t{ret.Name} = ");
            o.Append($"wl_proxy_marshal_flags((struct wl_proxy *) {n},\n\t\t\t {i.Uppercase}_{m.Uppercase}");
            o.Append(ret == null ? ", NULL" : ret.InterfaceName != null ? $", &{ret.InterfaceName}_interface" : ", interface");
            o.Append(ret != null && ret.InterfaceName == null ? ", version" : $", wl_proxy_get_version((struct wl_proxy *) {n})");
            o.Append(m.Destructor ? ", WL_MARSHAL_FLAG_DESTROY" : ", 0");
            foreach (var a in m.Args)
            {
                if (a.Type == ArgType.NewId)
                {
                    if (a.InterfaceName == null) o.Append(", interface->name, version");
                    o.Append(", NULL");
                }
                else o.Append(", ").Append(a.Name);
            }
            o.Append(");\n");
            if (ret != null && ret.InterfaceName == null) o.Append($"\n\treturn (void *) {ret.Name};\n");
            else if (ret != null) o.Append($"\n\treturn (struct {ret.InterfaceName} *) {ret.Name};\n");
            o.Append("}\n\n");
        }
    }

    static void EmitEventWrappers(StringBuilder o, List<Message> messages, Interface i)
    {
        if (i.Name == "wl_display") return;   // libwayland-server hand-writes these
        foreach (var m in messages)
        {
            o.Append($"/**\n * @ingroup iface_{i.Name}\n * Sends an {m.Name} event to the client owning the resource.\n");
            o.Append(" * @param resource_ The client's resource\n");
            foreach (var a in m.Args.Where(a => a.Summary != null)) o.Append($" * @param {a.Name} {a.Summary}\n");
            o.Append(" */\n");
            o.Append($"static inline void\n{i.Name}_send_{m.Name}(struct wl_resource *resource_");
            foreach (var a in m.Args)
                o.Append(", ").Append(a.Type is ArgType.NewId or ArgType.Object ? "struct wl_resource *" : TypeName(a)).Append(a.Name);
            o.Append($")\n{{\n\twl_resource_post_event(resource_, {i.Uppercase}_{m.Uppercase}");
            foreach (var a in m.Args) o.Append(", ").Append(a.Name);
            o.Append(");\n}\n\n");
        }
    }

    // ---- marshalling code ----

    static void EmitCode(StringBuilder o, Protocol p, bool isPublic)
    {
        o.Append($"/* Generated by wayland-scanner {Version} */\n\n");
        if (p.Copyright != null) FormatTextToComment(o, p.Copyright, true);
        o.Append("#include <stdbool.h>\n#include <stdlib.h>\n#include <stdint.h>\n#include \"wayland-util.h\"\n\n");
        var visibility = isPublic ? "WL_EXPORT" : "WL_PRIVATE";
        if (!isPublic)
        {
            o.Append("#ifndef __has_attribute\n# define __has_attribute(x) 0  /* Compatibility with non-clang compilers. */\n#endif\n\n");
            o.Append("#if (__has_attribute(visibility) || defined(__GNUC__) && __GNUC__ >= 4)\n");
            o.Append("#define WL_PRIVATE __attribute__ ((visibility(\"hidden\")))\n#else\n#define WL_PRIVATE\n#endif\n\n");
        }
        foreach (var t in SortedTypes(p, includeInterfaces: false)) o.Append($"extern const struct wl_interface {t}_interface;\n");
        o.Append('\n');

        o.Append($"static const struct wl_interface *{p.Name}_types[] = {{\n");
        for (int n = 0; n < p.NullRunLength; n++) o.Append("\tNULL,\n");
        foreach (var i in p.Interfaces)
        {
            EmitTypes(o, p, i.Requests);
            EmitTypes(o, p, i.Events);
        }
        o.Append("};\n\n");

        foreach (var i in p.Interfaces)
        {
            EmitMessages(o, p.Name, i.Requests, i, "requests");
            EmitMessages(o, p.Name, i.Events, i, "events");
            o.Append($"{visibility} const struct wl_interface {i.Name}_interface = {{\n\t\"{i.Name}\", {i.Version},\n");
            o.Append(i.Requests.Count > 0 ? $"\t{i.Requests.Count}, {i.Name}_requests,\n" : "\t0, NULL,\n");
            o.Append(i.Events.Count > 0 ? $"\t{i.Events.Count}, {i.Name}_events,\n" : "\t0, NULL,\n");
            o.Append("};\n\n");
        }
    }

    static void EmitTypes(StringBuilder o, Protocol p, List<Message> messages)
    {
        foreach (var m in messages)
        {
            if (m.AllNull) { m.TypeIndex = 0; continue; }
            m.TypeIndex = p.NullRunLength + p.TypeIndex;
            p.TypeIndex += m.Args.Count;
            foreach (var a in m.Args)
                o.Append(a.Type is ArgType.NewId or ArgType.Object && a.InterfaceName != null ? $"\t&{a.InterfaceName}_interface,\n" : "\tNULL,\n");
        }
    }

    static void EmitMessages(StringBuilder o, string protocol, List<Message> messages, Interface i, string suffix)
    {
        if (messages.Count == 0) return;
        o.Append($"static const struct wl_message {i.Name}_{suffix}[] = {{\n");
        foreach (var m in messages)
        {
            o.Append($"\t{{ \"{m.Name}\", \"");
            if (m.Since > 1) o.Append(m.Since);
            foreach (var a in m.Args)
            {
                if (a.Nullable) o.Append('?');
                o.Append(a.Type switch
                {
                    ArgType.NewId => a.InterfaceName == null ? "sun" : "n",
                    ArgType.Unsigned => "u",
                    ArgType.Fixed => "f",
                    ArgType.String => "s",
                    ArgType.Object => "o",
                    ArgType.Array => "a",
                    ArgType.Fd => "h",
                    _ => "i",
                });
            }
            o.Append($"\", {protocol}_types + {m.TypeIndex} }},\n");
        }
        o.Append("};\n\n");
    }
}
