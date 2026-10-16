using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace WinTools;

// swift-section (MachOSwiftSection): dump the Swift metadata a binary carries (nominal types with
// their stored properties and enum cases, protocols, protocol conformances) as Swift-like
// declarations. Reads Mach-O (__swift5_types, __swift5_protos, __swift5_proto), and also Swift for
// Windows' PE/COFF DLLs and EXEs (.sw5tymd, .sw5prt, .sw5prtc) and ELF (swift5_type_metadata ...),
// since the records are the same relative-pointer structures in all three. Mangled type names are
// demangled for the common forms (standard library types, nominal types, generics, optionals,
// tuples, functions, symbolic references); anything else is printed in its mangled form.
static class SwiftSection
{
    const string Version = "0.8.0";

    [ModuleInitializer]
    internal static void Register() =>
        Commands.Add("swift-section", "Dump Swift types, protocols and conformances from Mach-O, PE or ELF files", Run);

    static readonly string[] AllSections = ["types", "protocols", "protocolConformances", "associatedTypes"];

    static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            Console.WriteLine("""
                OVERVIEW: Dump Swift information from a Mach-O, PE/COFF or ELF file.

                USAGE: swift-section <subcommand>

                SUBCOMMANDS:
                  dump <file> [-o <output>] [-s <section> ...] [--architecture <arch>]
                        sections: types, protocols, protocolConformances, associatedTypes (default: all)
                  interface <file> [-o <output>] [--architecture <arch>]
                        all declarations, conformances folded into their types

                  --version   Show the version.
                """);
            return 0;
        }
        if (args[0] == "--version") { Console.WriteLine(Version); return 0; }
        var sub = args[0];
        if (sub is not ("dump" or "interface")) throw new UsageException($"unknown subcommand '{sub}' (see swift-section --help)");
        string file = null, output = null, arch = null;
        var sections = new List<string>();
        for (var i = 1; i < args.Length; i++)
        {
            string Value() => ++i < args.Length ? args[i] : throw new UsageException($"missing value for '{args[i - 1]}'");
            switch (args[i])
            {
                case "-o" or "--output-path": output = Value(); break;
                case "-s" or "--sections":
                    // -s takes one or more section names until the next option.
                    sections.Add(Value());
                    while (i + 1 < args.Length && !args[i + 1].StartsWith('-') && AllSections.Contains(args[i + 1])) sections.Add(args[++i]);
                    break;
                case "-a" or "--architecture": arch = Value(); break;
                case "--demangle-options" or "--color-scheme": Value(); break;
                case "--uses-system-dyld-shared-cache" or "--emit-offset-comments": break;
                default:
                    if (args[i].StartsWith('-')) throw new UsageException($"unknown option '{args[i]}'");
                    if (file != null) throw new UsageException($"unexpected argument '{args[i]}'");
                    file = args[i];
                    break;
            }
        }
        if (file == null) throw new UsageException("missing expected argument '<file-path>'");
        foreach (var s in sections.Where(s => !AllSections.Contains(s)))
            throw new UsageException($"unknown section '{s}' (types, protocols, protocolConformances, associatedTypes)");
        if (sections.Count == 0) sections.AddRange(AllSections);

        Reader r;
        try { r = Reader.Open(file, arch); }
        catch (Exception e) when (e is MachO.FormatException or ElfKit.ElfError or FormatException)
        {
            Console.Error.WriteLine($"swift-section: {file}: {e.Message}");
            return 1;
        }
        var text = new StringBuilder();
        if (sub == "interface") Interface(r, text);
        else
        {
            if (sections.Contains("types")) foreach (var t in r.Types()) text.Append(DumpType(r, t, [])).Append('\n');
            if (sections.Contains("protocols")) foreach (var p in r.Protocols()) text.Append(DumpProtocol(r, p)).Append('\n');
            if (sections.Contains("protocolConformances"))
                foreach (var c in r.Conformances()) text.Append($"extension {c.Type}: {c.Protocol} {{}}\n\n");
            if (sections.Contains("associatedTypes"))
                foreach (var a in r.AssociatedTypes()) text.Append(a).Append('\n');
        }
        if (output != null) File.WriteAllText(output, text.ToString());
        else Console.Write(text.ToString());
        return 0;
    }

    static void Interface(Reader r, StringBuilder text)
    {
        var conformances = r.Conformances().GroupBy(c => c.Type).ToDictionary(g => g.Key, g => g.Select(c => c.Protocol).ToList());
        foreach (var p in r.Protocols()) text.Append(DumpProtocol(r, p)).Append('\n');
        foreach (var t in r.Types())
        {
            text.Append(DumpType(r, t, conformances.GetValueOrDefault(r.ContextName(t)) ?? [])).Append('\n');
            conformances.Remove(r.ContextName(t));
        }
        foreach (var (type, protocols) in conformances) text.Append($"extension {type}: {string.Join(", ", protocols)} {{}}\n\n");
    }

    // ---- declarations ----

    static string DumpType(Reader r, ulong desc, List<string> conformances)
    {
        var flags = r.U32(desc);
        var kind = flags & 0x1f;
        var name = r.ContextName(desc);
        var keyword = kind switch { 16 => "class", 17 => "struct", 18 => "enum", _ => "type" };
        var sb = new StringBuilder();
        var inherits = new List<string>();
        if (kind == 16 && r.Rel(desc + 20) is { } superName && superName != 0) inherits.Add(r.TypeName(superName));
        inherits.AddRange(conformances);
        sb.Append(keyword).Append(' ').Append(name);
        if ((flags & 0x80) != 0) sb.Append("<...>");   // generic: parameters live in the generic context header
        if (inherits.Count > 0) sb.Append(": ").Append(string.Join(", ", inherits));
        sb.Append(" {\n");
        if (r.Rel(desc + 16) is { } fields and not 0)
        {
            var count = r.U32(fields + 12);
            var size = r.U16(fields + 10);
            for (ulong i = 0; i < count && i < 10000; i++)
            {
                var rec = fields + 16 + i * size;
                var fflags = r.U32(rec);
                var fieldName = r.Rel(rec + 8) is { } n and not 0 ? r.CString(n) : "_";
                var typeName = r.Rel(rec + 4) is { } t and not 0 ? r.TypeName(t) : null;
                if (kind == 18)
                {
                    var indirect = (fflags & 1) != 0 ? "indirect " : "";
                    sb.Append($"    {indirect}case {fieldName}{(typeName == null ? "" : typeName.StartsWith('(') ? typeName : $"({typeName})")}\n");
                }
                else
                {
                    var decl = (fflags & 2) != 0 ? "var" : "let";
                    sb.Append($"    {decl} {fieldName}: {typeName ?? "_"}\n");
                }
            }
        }
        sb.Append("}\n");
        return sb.ToString();
    }

    static string DumpProtocol(Reader r, ulong desc)
    {
        var name = r.ContextName(desc);
        var requirements = r.U32(desc + 16);
        var sb = new StringBuilder($"protocol {name} {{\n");
        if (r.Rel(desc + 20) is { } assoc and not 0)
            foreach (var a in r.CString(assoc).Split(' ', StringSplitOptions.RemoveEmptyEntries))
                sb.Append($"    associatedtype {a}\n");
        sb.Append($"    // {requirements} requirement{(requirements == 1 ? "" : "s")}\n}}\n");
        return sb.ToString();
    }

    // ---- binary access ----

    sealed class Reader
    {
        byte[] data;
        int pointerSize;
        readonly List<(string Name, ulong Addr, long Offset, ulong Size)> sections = [];
        readonly Dictionary<ulong, string> names = [];

        public static Reader Open(string path, string arch)
        {
            var bytes = File.ReadAllBytes(path);
            var r = new Reader();
            if (MachO.IsMachO(bytes))
            {
                var m = MachO.Parse(bytes, arch);
                m.Rebases(strict: false);   // resolve chained fixups so indirect pointers are plain addresses
                r.data = m.Data;
                r.pointerSize = m.PointerSize;
                foreach (var s in m.Sections.Where(s => !s.ZeroFill))
                    r.sections.Add((s.Name, s.Addr, s.Offset != 0 ? s.Offset : m.FileOffset(s.Addr), s.Size));
                r.Kinds = ("__swift5_types", "__swift5_protos", "__swift5_proto", "__swift5_assocty");
            }
            else if (bytes.Length > 0x40 && bytes[0] == 'M' && bytes[1] == 'Z')
            {
                // PE/COFF: addresses are RVAs; relative pointers don't care about the image base.
                r.data = bytes;
                var pe = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(0x3c));
                if (pe <= 0 || pe + 24 > bytes.Length || BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(pe)) != 0x4550)
                    throw new FormatException("not a PE file");
                var count = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(pe + 6));
                var optSize = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(pe + 20));
                r.pointerSize = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(pe + 24)) == 0x20b ? 8 : 4;
                var table = pe + 24 + optSize;
                for (var i = 0; i < count; i++)
                {
                    var h = table + i * 40;
                    var name = Encoding.ASCII.GetString(bytes, h, 8).TrimEnd('\0');
                    var vsize = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(h + 8));
                    var rva = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(h + 12));
                    var rawSize = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(h + 16));
                    var raw = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(h + 20));
                    r.sections.Add((name, rva, raw, Math.Min(vsize == 0 ? rawSize : vsize, rawSize)));
                }
                r.Kinds = (".sw5tymd", ".sw5prt", ".sw5prtc", ".sw5asty");
            }
            else if (ElfKit.Elf.IsElf(bytes))
            {
                var elf = ElfKit.Elf.Load(path);
                r.data = elf.Data;
                r.pointerSize = elf.Is64 ? 8 : 4;
                foreach (var s in elf.Sections.Where(s => s.Type != ElfKit.SHT_NOBITS && s.Addr != 0))
                    r.sections.Add((s.Name, s.Addr, (long)s.Offset, s.Size));
                r.Kinds = ("swift5_type_metadata", "swift5_protocols", "swift5_protocol_conformances", "swift5_assocty");
            }
            else throw new FormatException("not a Mach-O, PE/COFF or ELF file");
            return r;
        }

        (string Types, string Protocols, string Conformances, string AssocTypes) Kinds;

        long Offset(ulong addr)
        {
            foreach (var s in sections)
                if (addr >= s.Addr && addr < s.Addr + s.Size) return s.Offset + (long)(addr - s.Addr);
            return -1;
        }

        public uint U32(ulong addr) => Offset(addr) is var o and >= 0 && o + 4 <= data.Length ? BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan((int)o)) : 0;
        public ushort U16(ulong addr) => Offset(addr) is var o and >= 0 && o + 2 <= data.Length ? BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan((int)o)) : (ushort)0;
        ulong Pointer(ulong addr) => Offset(addr) is var o and >= 0 && o + pointerSize <= data.Length
            ? pointerSize == 8 ? BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan((int)o)) : BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan((int)o)) : 0;

        // A 32-bit self-relative pointer at addr; 0 when null.
        public ulong? Rel(ulong addr)
        {
            var v = (int)U32(addr);
            return v == 0 ? 0 : (ulong)((long)addr + v);
        }

        // A relative indirectable pointer: the low bit says it points at a pointer to the target.
        ulong Indirectable(ulong addr)
        {
            var v = (int)U32(addr);
            if (v == 0) return 0;
            var target = (ulong)((long)addr + (v & ~1));
            return (v & 1) != 0 ? Pointer(target) : target;
        }

        public string CString(ulong addr)
        {
            var o = Offset(addr);
            if (o < 0) return "";
            var end = Array.IndexOf(data, (byte)0, (int)o);
            return Encoding.UTF8.GetString(data, (int)o, (end < 0 ? data.Length : end) - (int)o);
        }

        // The records of one metadata section: 32-bit relative pointers, null entries skipped (Swift's
        // COFF sections begin and end with pointer-sized sentinels).
        IEnumerable<(ulong Addr, int Value)> Records(string section, int stride = 4)
        {
            foreach (var s in sections.Where(s => s.Name == section))
                for (ulong p = 0; p + 4 <= s.Size; p += (ulong)stride)
                {
                    var v = (int)U32(s.Addr + p);
                    if (v != 0) yield return (s.Addr + p, v);
                }
        }

        public IEnumerable<ulong> Types()
        {
            foreach (var (addr, v) in Records(Kinds.Types))
            {
                // TypeReferenceKind in the low two bits: 0 direct descriptor, 1 indirect.
                var target = (ulong)((long)addr + (v & ~3));
                var desc = (v & 3) == 1 ? Pointer(target) : (v & 3) == 0 ? target : 0;
                if (desc != 0 && Offset(desc) >= 0) yield return desc;
            }
        }

        public IEnumerable<ulong> Protocols()
        {
            foreach (var (addr, _) in Records(Kinds.Protocols))
                if (Indirectable(addr) is var desc and not 0 && Offset(desc) >= 0) yield return desc;
        }

        public IEnumerable<(string Type, string Protocol)> Conformances()
        {
            foreach (var (addr, v) in Records(Kinds.Conformances))
            {
                var conf = (ulong)((long)addr + v);
                var proto = Indirectable(conf);
                var protoName = proto != 0 && Offset(proto) >= 0 ? ContextName(proto) : "<external protocol>";
                var flags = U32(conf + 12);
                var typeRef = conf + 4;
                var tv = (int)U32(typeRef);
                var target = (ulong)((long)typeRef + tv);
                var typeName = ((flags >> 3) & 7) switch
                {
                    0 => ContextName(target),
                    1 => Pointer(target) is var d and not 0 && Offset(d) >= 0 ? ContextName(d) : "<external type>",
                    2 => CString(target),                                  // Objective-C class name
                    3 => "<Objective-C class>",
                    _ => "<unknown>",
                };
                yield return (typeName, protoName);
            }
        }

        public IEnumerable<string> AssociatedTypes()
        {
            // AssociatedTypeDescriptor: conforming type, protocol, count, record size, then (name, type) records.
            foreach (var s in sections.Where(s => s.Name == Kinds.AssocTypes))
            {
                ulong p = 0;
                while (p + 16 <= s.Size)
                {
                    var at = s.Addr + p;
                    var count = U32(at + 8);
                    var size = U32(at + 12);
                    if (size < 8 || count > 1000) break;
                    var conforming = Rel(at) is { } c and not 0 ? TypeName(c) : "?";
                    var proto = Rel(at + 4) is { } pr and not 0 ? TypeName(pr) : "?";
                    var sb = new StringBuilder($"extension {conforming}: {proto} {{\n");
                    for (ulong i = 0; i < count; i++)
                    {
                        var rec = at + 16 + i * size;
                        var name = Rel(rec) is { } n and not 0 ? CString(n) : "?";
                        var type = Rel(rec + 4) is { } t and not 0 ? TypeName(t) : "?";
                        sb.Append($"    typealias {name} = {type}\n");
                    }
                    yield return sb.Append("}\n").ToString();
                    p += 16 + count * size;
                }
            }
        }

        // The fully qualified name of a context descriptor (Module.Outer.Inner).
        public string ContextName(ulong desc)
        {
            if (names.TryGetValue(desc, out var cached)) return cached;
            names[desc] = "?";   // cycle guard
            var flags = U32(desc);
            var kind = flags & 0x1f;
            var parent = Indirectable(desc + 4);
            var prefix = parent != 0 && Offset(parent) >= 0 ? ContextName(parent) + "." : "";
            string name = kind switch
            {
                0 => CString(Rel(desc + 8) ?? 0),                                           // module
                1 => Rel(desc + 8) is { } ext and not 0 ? TypeName(ext) : "(extension)",      // extension
                2 => "(anonymous context)",
                3 or 16 or 17 or 18 => prefix + CString(Rel(desc + 8) ?? 0),
                _ => prefix + "(context)",
            };
            names[desc] = name;
            return name;
        }

        // Demangles the type name at addr (a mangled name that may embed symbolic references).
        public string TypeName(ulong addr)
        {
            var bytes = new List<byte>();
            var refs = new Dictionary<int, string>();
            for (var p = addr; ; p++)
            {
                var o = Offset(p);
                if (o < 0) break;
                var b = data[o];
                if (b == 0) break;
                if (b is >= 0x01 and <= 0x17)
                {
                    // Symbolic reference: kind byte + 32-bit relative pointer.
                    var target = (ulong)((long)(p + 1) + (int)U32(p + 1));
                    refs[bytes.Count] = b switch
                    {
                        0x01 => ContextName(target),
                        0x02 => Pointer(target) is var d and not 0 && Offset(d) >= 0 ? ContextName(d) : "<external type>",
                        _ => "<symbolic reference>",
                    };
                    bytes.Add(0x01);
                    p += 4;
                    continue;
                }
                if (b is >= 0x18 and <= 0x1f)
                {
                    refs[bytes.Count] = "<symbolic reference>";
                    bytes.Add(0x01);
                    p += (ulong)pointerSize;
                    continue;
                }
                bytes.Add(b);
            }
            return SwiftDemangler.Demangle(Encoding.Latin1.GetString(bytes.ToArray()), refs);
        }
    }
}

// A demangler for Swift type manglings (the operand-stack form Swift uses: operands precede their
// operator). Covers standard library substitutions, nominal types and their contexts, bound generics,
// optionals, arrays/dictionaries, tuples, function types, metatypes, generic parameters and
// substitutions; anything unrecognized falls back to the mangled text.
static class SwiftDemangler
{
    sealed class Node(string kind, string text, List<Node> children = null)
    {
        public readonly string Kind = kind;
        public readonly string Text = text;
        public readonly List<Node> Children = children ?? [];
    }

    sealed class Fail : Exception;

    public static string Demangle(string mangled, Dictionary<int, string> symbolic = null)
    {
        var s = mangled;
        if (s.StartsWith("$s") || s.StartsWith("_$s")) s = s[(s.IndexOf('s') + 1)..];
        try
        {
            var stack = new List<Node>();
            var subs = new List<Node>();
            var i = 0;
            Node Pop() => stack.Count > 0 ? Take(stack) : throw new Fail();
            static Node Take(List<Node> st) { var n = st[^1]; st.RemoveAt(st.Count - 1); return n; }
            while (i < s.Length)
            {
                var c = s[i++];
                if (c == '\x01')
                {
                    var symbol = Nominal(symbolic != null && symbolic.TryGetValue(i - 1, out var n) ? n : "<symbolic>");
                    stack.Add(symbol);
                    subs.Add(symbol);
                    continue;
                }
                if (char.IsDigit(c))
                {
                    var start = i - 1;
                    while (i < s.Length && char.IsDigit(s[i])) i++;
                    var len = int.Parse(s[start..i]);
                    if (len == 0 || i + len > s.Length) throw new Fail();
                    var ident = new Node("ident", s.Substring(i, len));
                    i += len;
                    stack.Add(ident);
                    continue;
                }
                switch (c)
                {
                    case 's': stack.Add(new Node("module", "Swift")); break;
                    case 'S':
                    {
                        if (i >= s.Length) throw new Fail();
                        var k = s[i++];
                        if (k == 'g') { stack.Add(new Node("optional", null, [Pop()])); break; }
                        if (k == 'c' && i < s.Length) { i++; stack.Add(Nominal("Swift.<concurrency type>")); break; }
                        var std = k switch
                        {
                            'a' => "Swift.Array", 'b' => "Swift.Bool", 'c' => "Swift.UnicodeScalar", 'D' => "Swift.Dictionary",
                            'd' => "Swift.Double", 'f' => "Swift.Float", 'h' => "Swift.Set", 'I' => "Swift.DefaultIndices",
                            'i' => "Swift.Int", 'J' => "Swift.Character", 'N' => "Swift.ClosedRange", 'n' => "Swift.Range",
                            'O' => "Swift.ObjectIdentifier", 'P' => "Swift.UnsafePointer", 'p' => "Swift.UnsafeMutablePointer",
                            'Q' => "Swift.Equatable", 'q' => "Swift.Optional", 'R' => "Swift.UnsafeBufferPointer",
                            'r' => "Swift.UnsafeMutableBufferPointer", 'S' => "Swift.String", 's' => "Swift.Substring",
                            'u' => "Swift.UInt", 'V' => "Swift.UnsafeRawPointer", 'v' => "Swift.UnsafeMutableRawPointer",
                            'W' => "Swift.UnsafeRawBufferPointer", 'w' => "Swift.UnsafeMutableRawBufferPointer",
                            'B' => "Swift.BinaryFloatingPoint", 'E' => "Swift.Encodable", 'e' => "Swift.Decodable",
                            'F' => "Swift.FloatingPoint", 'G' => "Swift.RandomNumberGenerator", 'H' => "Swift.Hashable",
                            'j' => "Swift.Numeric", 'K' => "Swift.BidirectionalCollection", 'k' => "Swift.RandomAccessCollection",
                            'L' => "Swift.Comparable", 'l' => "Swift.Collection", 'M' => "Swift.MutableCollection",
                            'm' => "Swift.RangeReplaceableCollection", 'T' => "Swift.Sequence", 't' => "Swift.IteratorProtocol",
                            'U' => "Swift.UnsignedInteger", 'X' => "Swift.RangeExpression", 'x' => "Swift.Strideable",
                            'Y' => "Swift.RawRepresentable", 'y' => "Swift.StringProtocol", 'Z' => "Swift.SignedInteger",
                            'z' => "Swift.BinaryInteger",
                            _ => throw new Fail(),
                        };
                        stack.Add(Nominal(std));
                        break;
                    }
                    case 'V' or 'C' or 'O' or 'P' or 'a':
                    {
                        var name = Pop();
                        var context = Pop();
                        if (context.Kind == "ident") subs.Add(new Node("module", context.Text));
                        var node = Nominal(Render(context) + "." + Render(name));
                        stack.Add(node);
                        subs.Add(node);
                        break;
                    }
                    case 'y': stack.Add(new Node("list", null)); break;
                    case '_': stack.Add(new Node("sep", null)); break;
                    case 'G':
                    {
                        var args = new List<Node>();
                        while (stack.Count > 0 && stack[^1].Kind != "list")
                        {
                            var n = Pop();
                            if (n.Kind != "sep") args.Insert(0, n);
                        }
                        Pop();   // the list marker
                        var bound = new Node("generic", null, [Pop(), .. args]);
                        stack.Add(bound);
                        subs.Add(bound);
                        break;
                    }
                    case 't':
                    {
                        // Elements up to the first-element marker '_' plus the one before it, or 'y' for ().
                        var elems = new List<Node>();
                        if (stack.Count > 0 && stack[^1].Kind == "list") { Pop(); stack.Add(new Node("tuple", null)); break; }
                        while (stack.Count > 0 && stack[^1].Kind != "sep") elems.Insert(0, Pop());
                        if (stack.Count > 0) { Pop(); elems.Insert(0, Pop()); }
                        stack.Add(new Node("tuple", null, elems));
                        break;
                    }
                    case 'c' or 'X' when c == 'c' || i < s.Length && s[i] == 'E':
                    {
                        if (c == 'X') i++;   // XE: @noescape function
                        var parameters = Pop();
                        var result = Pop();
                        stack.Add(new Node("function", null, [parameters, result]));
                        break;
                    }
                    case 'K' or 'Y': break;   // throws, async: decoration only
                    case 'X':
                    {
                        if (i >= s.Length) throw new Fail();
                        var k = s[i++];
                        if (k == 'S' && i < s.Length)
                        {
                            var sugar = s[i++];
                            if (sugar == 'q') stack.Add(new Node("optional", null, [Pop()]));
                            else if (sugar == 'a') stack.Add(new Node("array", null, [Pop()]));
                            else if (sugar == 'D') { var v = Pop(); var key = Pop(); stack.Add(new Node("dict", null, [key, v])); }
                            else throw new Fail();
                        }
                        else if (k is 'w' or 'o' or 'l') { var t = Pop(); stack.Add(new Node("ref", k == 'w' ? "weak" : k == 'o' ? "unowned" : "unowned(unsafe)", [t])); }
                        else if (k is 'p') { stack.Add(new Node("existential", null, [Pop()])); }
                        else if (k is 'M') { stack.Add(new Node("metatype", null, [Pop()])); }
                        else if (k is 'b' or 'B' or 'f' or 'L' or 'C' or 'c') { }   // function conventions
                        else throw new Fail();
                        break;
                    }
                    case 'p': stack.Add(new Node("existential", null, [Pop()])); break;   // protocol list -> any P
                    case 'm': stack.Add(new Node("metatype", null, [Pop()])); break;
                    case 'x': stack.Add(new Node("raw", "A")); break;   // first generic parameter
                    case 'q':
                    {
                        // q_ is the first generic parameter, q<n>_ the (n+1)th; qd... adds depth.
                        var start = i;
                        while (i < s.Length && s[i] != '_') i++;
                        var index = i > start && int.TryParse(s[start..i], out var n) ? n + 1 : 0;
                        i++;
                        stack.Add(new Node("raw", index < 26 ? ((char)('A' + index)).ToString() : $"T{index}"));
                        break;
                    }
                    case 'A':
                    {
                        // Substitution: A<lowercase letters for repeated subs><uppercase index>, or A<n>_.
                        var start = i;
                        if (i < s.Length && char.IsDigit(s[i]))
                        {
                            while (i < s.Length && char.IsDigit(s[i])) i++;
                            var n = int.Parse(s[start..i]) + 26;
                            i++;
                            stack.Add(n < subs.Count ? subs[n] : throw new Fail());
                            break;
                        }
                        while (i < s.Length && char.IsLower(s[i]))
                        {
                            var idx = s[i++] - 'a';
                            stack.Add(idx < subs.Count ? subs[idx] : throw new Fail());
                        }
                        if (i >= s.Length || !char.IsUpper(s[i])) throw new Fail();
                        var last = s[i++] - 'A';
                        stack.Add(last < subs.Count ? subs[last] : throw new Fail());
                        break;
                    }
                    case 'D' or 'z' or 'h' or 'n': break;   // type mangling marker, inout/shared/owned
                    default: throw new Fail();
                }
            }
            var parts = stack.Where(n => n.Kind is not ("sep" or "list")).ToList();
            if (parts.Count != 1) throw new Fail();
            return Render(parts[0]);
        }
        catch (Fail) { return mangled.Length > 0 && mangled[0] < ' ' ? "<symbolic>" : "$s" + mangled; }
        catch (ArgumentException) { return "$s" + mangled; }
    }

    static Node Nominal(string name) => new("nominal", name);

    static string Render(Node n) => n.Kind switch
    {
        "ident" or "module" or "nominal" or "raw" => n.Text,
        "optional" => Wrap(n.Children[0]) + "?",
        "array" => "[" + Render(n.Children[0]) + "]",
        "dict" => "[" + Render(n.Children[0]) + " : " + Render(n.Children[1]) + "]",
        "generic" => n.Children[0].Text switch
        {
            "Swift.Optional" when n.Children.Count == 2 => Wrap(n.Children[1]) + "?",
            "Swift.Array" when n.Children.Count == 2 => "[" + Render(n.Children[1]) + "]",
            "Swift.Dictionary" when n.Children.Count == 3 => "[" + Render(n.Children[1]) + " : " + Render(n.Children[2]) + "]",
            _ => Render(n.Children[0]) + "<" + string.Join(", ", n.Children.Skip(1).Select(Render)) + ">",
        },
        "tuple" => "(" + string.Join(", ", n.Children.Select(Render)) + ")",
        "function" => Params(n.Children[0]) + " -> " + Render(n.Children[1]),
        "list" => "()",
        "ref" => n.Text + " " + Render(n.Children[0]),
        "existential" => n.Children[0].Kind == "list" ? "Any" : "any " + Render(n.Children[0]),
        "metatype" => Wrap(n.Children[0]) + ".Type",
        _ => "?",
    };

    static string Params(Node n) => n.Kind is "tuple" or "list" ? Render(n) : "(" + Render(n) + ")";

    static string Wrap(Node n) => n.Kind is "function" or "existential" ? "(" + Render(n) + ")" : Render(n);
}
