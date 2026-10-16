using System.Buffers.Binary;
using System.Text;
using System.Xml.Linq;

namespace WinTools;

// One value in xcresulttool's typed encoding: an object (Fields), an array (Items) or a primitive (Value).
//   [T[K2:_nV12:ActionRecord]K7:actions[...]...]   object; T spells out the type (and supertypes, _s)
//   [S6:StringK2:_vV4:Test]                          object whose type was spelled out before
//   K<len>:<key> <value>, V<len>:<utf-8 text>        fields and primitive values; arrays list objects
sealed class XcNode
{
    public string Type;
    public readonly List<string> Supertypes = [];
    public readonly Dictionary<string, XcNode> Fields = [];
    public readonly List<XcNode> Items = [];
    public string Value;

    public XcNode this[string key] => Fields.GetValueOrDefault(key);
    public string Str(string key) => this[key]?.Value;
    public double? Num(string key) => double.TryParse(Str(key), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;
    public int Int(string key) => (int)(Num(key) ?? 0);
    public IEnumerable<XcNode> Array(string key) => this[key]?.Items ?? Enumerable.Empty<XcNode>();
    public string RefId(string key) => this[key]?.Str("id");
    public bool Is(string type) => Type == type || Supertypes.Contains(type);
    public DateTimeOffset? Date(string key) => DateTimeOffset.TryParse(Str(key), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d) ? d : null;

    public static XcNode Parse(byte[] data)
    {
        var pos = 0;
        return ReadObject(data, ref pos);
    }

    static XcNode ReadObject(byte[] d, ref int p)
    {
        Expect(d, ref p, '[');
        var node = new XcNode();
        if (d[p] == 'T')
        {
            p++;
            ReadTypeDescription(d, ref p, node, true);
        }
        else if (d[p] == 'S') { p++; node.Type = ReadString(d, ref p); }
        else throw new InvalidDataException("xcresult: bad object encoding");
        if (node.Type == "Array")
        {
            while (d[p] != ']') node.Items.Add(ReadObject(d, ref p));
        }
        else
        {
            while (d[p] != ']')
            {
                Expect(d, ref p, 'K');
                var key = ReadString(d, ref p);
                if (d[p] == 'V')
                {
                    p++;
                    var value = ReadString(d, ref p);
                    if (key == "_v") node.Value = value;
                    else node.Fields[key] = new XcNode { Type = "String", Value = value };
                }
                else node.Fields[key] = ReadObject(d, ref p);
            }
        }
        p++;
        return node;
    }

    // [K2:_nV<name>K2:_s[...supertype...]]
    static void ReadTypeDescription(byte[] d, ref int p, XcNode node, bool primary)
    {
        Expect(d, ref p, '[');
        while (d[p] != ']')
        {
            Expect(d, ref p, 'K');
            var key = ReadString(d, ref p);
            if (d[p] == 'V')
            {
                p++;
                var value = ReadString(d, ref p);
                if (key == "_n") { if (primary) node.Type = value; else node.Supertypes.Add(value); }
            }
            else ReadTypeDescription(d, ref p, node, false);
        }
        p++;
    }

    static string ReadString(byte[] d, ref int p)
    {
        var length = 0;
        while (d[p] != ':') length = length * 10 + (d[p++] - '0');
        p++;
        var s = Encoding.UTF8.GetString(d, p, length);
        p += length;
        return s;
    }

    static void Expect(byte[] d, ref int p, char c)
    {
        if (p >= d.Length || d[p] != c) throw new InvalidDataException($"xcresult: expected '{c}' at {p}");
        p++;
    }
}

// Property lists: XML and binary (bplist00) reading, NSKeyedArchiver decoding. Values come back as
// Dictionary<string, object>, List<object>, string, long, double, bool, byte[], DateTime or XcResultPlist.Uid.
static class XcResultPlist
{
    public readonly record struct Uid(int Index);

    public static object Read(byte[] data)
    {
        if (data.Length > 8 && Encoding.ASCII.GetString(data, 0, 8) == "bplist00") return new Binary(data).Top();
        return Xml(XDocument.Parse(Encoding.UTF8.GetString(data).TrimStart('﻿')).Root!.Elements().First());
    }

    static object Xml(XElement e) => e.Name.LocalName switch
    {
        "dict" => e.Elements().Chunk(2).ToDictionary(kv => kv[0].Value, kv => Xml(kv[1])),
        "array" => e.Elements().Select(Xml).ToList(),
        "string" => e.Value,
        "integer" => long.Parse(e.Value, System.Globalization.CultureInfo.InvariantCulture),
        "real" => double.Parse(e.Value, System.Globalization.CultureInfo.InvariantCulture),
        "true" => true,
        "false" => false,
        "date" => DateTime.Parse(e.Value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal),
        "data" => Convert.FromBase64String(string.Concat(e.Value.Where(c => !char.IsWhiteSpace(c)))),
        _ => null,
    };

    sealed class Binary
    {
        readonly byte[] d;
        readonly int offsetSize, refSize;
        readonly long[] offsets;
        readonly long top;

        public Binary(byte[] data)
        {
            d = data;
            var trailer = data.Length - 32;
            offsetSize = data[trailer + 6];
            refSize = data[trailer + 7];
            var count = (long)BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(trailer + 8));
            top = (long)BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(trailer + 16));
            var table = (long)BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(trailer + 24));
            offsets = new long[count];
            for (var i = 0; i < count; i++) offsets[i] = (long)Sized(table + i * offsetSize, offsetSize);
        }

        ulong Sized(long at, int size)
        {
            ulong v = 0;
            for (var i = 0; i < size; i++) v = v << 8 | d[at + i];
            return v;
        }

        public object Top() => Object(top);

        object Object(long index)
        {
            var at = offsets[index];
            var marker = d[at];
            int high = marker >> 4, low = marker & 15;
            long Count(ref long p)
            {
                if (low != 15) return low;
                var size = 1 << (d[p] & 15);
                var value = (long)Sized(p + 1, size);
                p += 1 + size;
                return value;
            }
            var pos = at + 1;
            switch (high)
            {
                case 0: return marker == 8 ? false : marker == 9 ? true : null;
                case 1: return (long)Sized(pos, 1 << low);
                case 2:
                    return low == 2 ? BinaryPrimitives.ReadSingleBigEndian(d.AsSpan((int)pos)) : BinaryPrimitives.ReadDoubleBigEndian(d.AsSpan((int)pos));
                case 3: return new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(BinaryPrimitives.ReadDoubleBigEndian(d.AsSpan((int)pos)));
                case 4: { var n = Count(ref pos); return d.AsSpan((int)pos, (int)n).ToArray(); }
                case 5: { var n = Count(ref pos); return Encoding.ASCII.GetString(d, (int)pos, (int)n); }
                case 6: { var n = Count(ref pos); return Encoding.BigEndianUnicode.GetString(d, (int)pos, (int)n * 2); }
                case 8: return new Uid((int)Sized(pos, low + 1));
                case 10 or 12:
                {
                    var n = Count(ref pos);
                    var list = new List<object>((int)n);
                    for (var i = 0; i < n; i++) list.Add(Object((long)Sized(pos + i * refSize, refSize)));
                    return list;
                }
                case 13:
                {
                    var n = Count(ref pos);
                    var dict = new Dictionary<string, object>((int)n);
                    for (var i = 0; i < n; i++)
                    {
                        var key = Object((long)Sized(pos + i * refSize, refSize))?.ToString() ?? "";
                        dict[key] = Object((long)Sized(pos + (n + i) * refSize, refSize));
                    }
                    return dict;
                }
                default: throw new InvalidDataException($"bplist: unknown object type 0x{marker:x2}");
            }
        }
    }

    // NSKeyedArchiver: resolves $top.root through the $objects table. Archived objects become
    // dictionaries with a "$class" entry holding the class name; NSArray/NSSet become lists,
    // NSDictionary a Dictionary, NSString/NSData their values.
    public static object Unarchive(byte[] data)
    {
        if (Read(data) is not Dictionary<string, object> archive || archive.GetValueOrDefault("$objects") is not List<object> objects)
            throw new InvalidDataException("not an NSKeyedArchiver archive");
        var decoded = new Dictionary<int, object>();
        object Resolve(object value)
        {
            if (value is not Uid uid) return value;
            if (decoded.TryGetValue(uid.Index, out var done)) return done;
            var raw = objects[uid.Index];
            if (raw is "$null") return decoded[uid.Index] = null;
            if (raw is not Dictionary<string, object> dict) return decoded[uid.Index] = raw;
            if (dict.ContainsKey("$classname")) return decoded[uid.Index] = dict;
            var className = (Resolve(dict.GetValueOrDefault("$class")) as Dictionary<string, object>)?.GetValueOrDefault("$classname") as string;
            switch (className)
            {
                case "NSArray" or "NSMutableArray" or "NSSet" or "NSMutableSet" or "NSOrderedSet" or "NSMutableOrderedSet":
                {
                    var list = new List<object>();
                    decoded[uid.Index] = list;
                    foreach (var item in dict.GetValueOrDefault("NS.objects") as List<object> ?? []) list.Add(Resolve(item));
                    return list;
                }
                case "NSDictionary" or "NSMutableDictionary":
                {
                    var result = new Dictionary<string, object>();
                    decoded[uid.Index] = result;
                    var keys = dict.GetValueOrDefault("NS.keys") as List<object> ?? [];
                    var values = dict.GetValueOrDefault("NS.objects") as List<object> ?? [];
                    for (var i = 0; i < keys.Count && i < values.Count; i++) result[Resolve(keys[i])?.ToString() ?? ""] = Resolve(values[i]);
                    return result;
                }
                case "NSString" or "NSMutableString":
                    return decoded[uid.Index] = Resolve(dict.GetValueOrDefault("NS.string"));
                case "NSData" or "NSMutableData":
                    return decoded[uid.Index] = Resolve(dict.GetValueOrDefault("NS.data"));
                case "NSDate":
                    return decoded[uid.Index] = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Convert.ToDouble(dict.GetValueOrDefault("NS.time") ?? 0.0));
                default:
                {
                    var result = new Dictionary<string, object> { ["$class"] = className };
                    decoded[uid.Index] = result;
                    foreach (var (k, v) in dict) if (k != "$class") result[k] = Resolve(v);
                    return result;
                }
            }
        }
        var topDict = archive.GetValueOrDefault("$top") as Dictionary<string, object>;
        var root = topDict?.GetValueOrDefault("root") ?? topDict?.Values.FirstOrDefault();
        return Resolve(root);
    }
}

// Read-only SQLite through winsqlite3.dll, which ships with Windows 10 and 11.
sealed class XcSqlite : IDisposable
{
    IntPtr db;

    public XcSqlite(string path)
    {
        // immutable=1: no locks, journals or -shm files are created next to the bundle's database.
        var uri = "file:///" + Uri.EscapeDataString(path.Replace('\\', '/')).Replace("%2F", "/").Replace("%3A", ":") + "?immutable=1";
        var rc = sqlite3_open_v2(Encoding.UTF8.GetBytes(uri + "\0"), out db, 0x01 | 0x40, IntPtr.Zero);
        if (rc != 0) throw new IOException($"{path}: cannot open database (SQLite error {rc})");
    }

    public bool HasTable(string name) => Query("SELECT name FROM sqlite_master WHERE type='table' AND name='" + name.Replace("'", "''") + "'").Count > 0;

    public List<Dictionary<string, object>> Query(string sql)
    {
        var rows = new List<Dictionary<string, object>>();
        if (sqlite3_prepare_v2(db, Encoding.UTF8.GetBytes(sql + "\0"), -1, out var stmt, IntPtr.Zero) != 0)
            throw new InvalidDataException("xcresult database: " + System.Runtime.InteropServices.Marshal.PtrToStringUTF8(sqlite3_errmsg(db)));
        try
        {
            var columns = sqlite3_column_count(stmt);
            var names = new string[columns];
            for (var i = 0; i < columns; i++) names[i] = System.Runtime.InteropServices.Marshal.PtrToStringUTF8(sqlite3_column_name(stmt, i));
            while (sqlite3_step(stmt) == 100)
            {
                var row = new Dictionary<string, object>(columns, StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < columns; i++)
                    row[names[i]] = sqlite3_column_type(stmt, i) switch
                    {
                        1 => sqlite3_column_int64(stmt, i),
                        2 => sqlite3_column_double(stmt, i),
                        3 => System.Runtime.InteropServices.Marshal.PtrToStringUTF8(sqlite3_column_text(stmt, i)),
                        _ => null,
                    };
                rows.Add(row);
            }
        }
        finally { sqlite3_finalize(stmt); }
        return rows;
    }

    public void Dispose()
    {
        if (db != IntPtr.Zero) sqlite3_close(db);
        db = IntPtr.Zero;
    }

    const string Lib = "winsqlite3.dll";
    [System.Runtime.InteropServices.DllImport(Lib)] static extern int sqlite3_open_v2(byte[] filename, out IntPtr db, int flags, IntPtr vfs);
    [System.Runtime.InteropServices.DllImport(Lib)] static extern int sqlite3_prepare_v2(IntPtr db, byte[] sql, int bytes, out IntPtr stmt, IntPtr tail);
    [System.Runtime.InteropServices.DllImport(Lib)] static extern int sqlite3_step(IntPtr stmt);
    [System.Runtime.InteropServices.DllImport(Lib)] static extern int sqlite3_column_count(IntPtr stmt);
    [System.Runtime.InteropServices.DllImport(Lib)] static extern IntPtr sqlite3_column_name(IntPtr stmt, int column);
    [System.Runtime.InteropServices.DllImport(Lib)] static extern int sqlite3_column_type(IntPtr stmt, int column);
    [System.Runtime.InteropServices.DllImport(Lib)] static extern long sqlite3_column_int64(IntPtr stmt, int column);
    [System.Runtime.InteropServices.DllImport(Lib)] static extern double sqlite3_column_double(IntPtr stmt, int column);
    [System.Runtime.InteropServices.DllImport(Lib)] static extern IntPtr sqlite3_column_text(IntPtr stmt, int column);
    [System.Runtime.InteropServices.DllImport(Lib)] static extern int sqlite3_finalize(IntPtr stmt);
    [System.Runtime.InteropServices.DllImport(Lib)] static extern int sqlite3_close(IntPtr db);
    [System.Runtime.InteropServices.DllImport(Lib)] static extern IntPtr sqlite3_errmsg(IntPtr db);
}
