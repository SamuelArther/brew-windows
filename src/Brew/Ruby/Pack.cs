using System.Buffers.Binary;
using System.Text;

namespace Brew.Ruby;

// Array#pack and String#unpack/unpack1 for the directives formulae use: integers (c C s S l L q Q n N v V
// with < > _ ! and counts), U, a A Z, H h, B b and m (base64).
public sealed partial class Interp
{
    static IEnumerable<(char Op, int Count, bool Star, bool Big)> Directives(string format)
    {
        for (var i = 0; i < format.Length;)
        {
            var op = format[i++];
            if (char.IsWhiteSpace(op)) continue;
            var big = !BitConverter.IsLittleEndian;
            while (i < format.Length && format[i] is '<' or '>' or '_' or '!')
            {
                if (format[i] == '>') big = true;
                if (format[i] == '<') big = false;
                i++;
            }
            if (i < format.Length && format[i] == '*') { i++; yield return (op, 0, true, big); continue; }
            var start = i;
            while (i < format.Length && char.IsDigit(format[i])) i++;
            yield return (op, i > start ? int.Parse(format[start..i]) : 1, false, big);
        }
    }

    static int Width(char op) => op switch
    {
        'c' or 'C' => 1, 's' or 'S' or 'n' or 'v' => 2, 'l' or 'L' or 'N' or 'V' => 4, 'q' or 'Q' => 8, _ => 0,
    };

    public RString Pack(List<object> values, string format)
    {
        var bytes = new List<byte>();
        var next = 0;
        object Take() => next < values.Count ? values[next++] : throw ArgError("too few arguments");
        foreach (var (op, count, star, bigFlag) in Directives(format))
        {
            var width = Width(op);
            if (width > 0)
            {
                var n = star ? values.Count - next : count;
                var big = op is 'n' or 'N' || (op is not ('v' or 'V') && bigFlag);
                for (var k = 0; k < n; k++)
                {
                    var x = Take();
                    var v = x is double d ? (long)d : Convert.ToInt64(Int(x));
                    var buf = new byte[8];
                    if (big) BinaryPrimitives.WriteInt64BigEndian(buf, v); else BinaryPrimitives.WriteInt64LittleEndian(buf, v);
                    bytes.AddRange(big ? buf[(8 - width)..] : buf[..width]);
                }
                continue;
            }
            switch (op)
            {
                case 'U':
                    for (var k = 0; k < (star ? values.Count - next : count); k++)
                        bytes.AddRange(Encoding.UTF8.GetBytes(char.ConvertFromUtf32((int)Int(Take()))));
                    break;
                case 'a' or 'A' or 'Z':
                {
                    var s = Take() is RString rs && rs.Binary ? Encoding.Latin1.GetBytes(rs.V) : Encoding.UTF8.GetBytes(Str(values[next - 1]));
                    var len = star ? s.Length + (op == 'Z' ? 1 : 0) : count;
                    for (var k = 0; k < len; k++) bytes.Add(k < s.Length ? s[k] : (byte)(op == 'A' ? ' ' : 0));
                    break;
                }
                case 'H' or 'h':
                {
                    var hex = Str(Take());
                    var len = star ? hex.Length : count;
                    for (var k = 0; k < len; k += 2)
                    {
                        int Nib(int idx) => idx < hex.Length && idx < len ? Convert.ToInt32(hex[idx].ToString(), 16) : 0;
                        bytes.Add((byte)(op == 'H' ? Nib(k) << 4 | Nib(k + 1) : Nib(k + 1) << 4 | Nib(k)));
                    }
                    break;
                }
                case 'B' or 'b':
                {
                    var bits = Str(Take());
                    var len = star ? bits.Length : count;
                    for (var k = 0; k < len; k += 8)
                    {
                        var v = 0;
                        for (var j = 0; j < 8; j++)
                            if (k + j < len && k + j < bits.Length && bits[k + j] == '1') v |= op == 'B' ? 0x80 >> j : 1 << j;
                        bytes.Add((byte)v);
                    }
                    break;
                }
                case 'm':
                {
                    var raw = Take() is RString ms && ms.Binary ? Encoding.Latin1.GetBytes(ms.V) : Encoding.UTF8.GetBytes(Str(values[next - 1]));
                    var b64 = Convert.ToBase64String(raw);
                    if (star || count != 0)
                    {
                        var line = (star || count < 3 ? 45 : count / 3 * 3) / 3 * 4;
                        var sb = new StringBuilder();
                        for (var k = 0; k < b64.Length; k += line) sb.Append(b64, k, Math.Min(line, b64.Length - k)).Append('\n');
                        b64 = sb.ToString();
                    }
                    bytes.AddRange(Encoding.ASCII.GetBytes(b64));
                    break;
                }
                case 'x': for (var k = 0; k < count; k++) bytes.Add(0); break;
                default: throw ArgError($"unsupported pack directive '{op}'");
            }
        }
        return new RString(Encoding.Latin1.GetString(bytes.ToArray())) { Binary = true };
    }

    public List<object> Unpack(RString s, string format)
    {
        var data = s.Binary ? Encoding.Latin1.GetBytes(s.V) : Encoding.UTF8.GetBytes(s.V);
        var at = 0;
        var result = new List<object>();
        foreach (var (op, count, star, bigFlag) in Directives(format))
        {
            var width = Width(op);
            if (width > 0)
            {
                var big = op is 'n' or 'N' || (op is not ('v' or 'V') && bigFlag);
                var signed = op is 'c' or 's' or 'l' or 'q';
                var n = star ? (data.Length - at) / width : count;
                for (var k = 0; k < n; k++)
                {
                    if (at + width > data.Length) { result.Add(null); continue; }
                    ulong v = 0;
                    for (var j = 0; j < width; j++)
                        v |= (ulong)data[at + (big ? width - 1 - j : j)] << (8 * j);
                    at += width;
                    if (signed && width < 8 && (v & (1UL << (8 * width - 1))) != 0) v |= ulong.MaxValue << (8 * width);
                    result.Add(signed ? (long)v : (object)(long)v);
                }
                continue;
            }
            switch (op)
            {
                case 'U':
                {
                    var text = Encoding.UTF8.GetString(data, at, data.Length - at);
                    var runes = text.EnumerateRunes().Take(star ? int.MaxValue : count).ToList();
                    foreach (var r in runes) { result.Add((long)r.Value); at += r.Utf8SequenceLength; }
                    break;
                }
                case 'a' or 'A' or 'Z':
                {
                    var len = star ? data.Length - at : Math.Min(count, data.Length - at);
                    var chunk = data[at..(at + len)];
                    at += len;
                    if (op == 'Z' && Array.IndexOf(chunk, (byte)0) is var z and >= 0) { chunk = chunk[..z]; if (star) at = at - len + z + 1; }
                    var str = Encoding.Latin1.GetString(chunk);
                    if (op == 'A') str = str.TrimEnd(' ', '\0');
                    result.Add(new RString(str) { Binary = true });
                    break;
                }
                case 'H' or 'h':
                {
                    var nibbles = star ? (data.Length - at) * 2 : count;
                    var sb = new StringBuilder();
                    for (var k = 0; k < nibbles && at + k / 2 < data.Length; k++)
                    {
                        var b = data[at + k / 2];
                        var nib = (op == 'H') == (k % 2 == 0) ? b >> 4 : b & 0xF;
                        sb.Append("0123456789abcdef"[nib]);
                    }
                    at += (nibbles + 1) / 2;
                    result.Add(S(sb.ToString()));
                    break;
                }
                case 'B' or 'b':
                {
                    var bits = star ? (data.Length - at) * 8 : count;
                    var sb = new StringBuilder();
                    for (var k = 0; k < bits && at + k / 8 < data.Length; k++)
                    {
                        var b = data[at + k / 8];
                        sb.Append(((op == 'B' ? b >> (7 - k % 8) : b >> (k % 8)) & 1) == 1 ? '1' : '0');
                    }
                    at += (bits + 7) / 8;
                    result.Add(S(sb.ToString()));
                    break;
                }
                case 'm':
                {
                    var text = Encoding.ASCII.GetString(data, at, data.Length - at);
                    var clean = new string(text.Where(c => char.IsLetterOrDigit(c) || c is '+' or '/' or '=' or '-' or '_').ToArray()).Replace('-', '+').Replace('_', '/');
                    clean = clean.PadRight((clean.Length + 3) / 4 * 4, '=');
                    result.Add(new RString(Encoding.Latin1.GetString(Convert.FromBase64String(clean))) { Binary = true });
                    at = data.Length;
                    break;
                }
                case 'x': at += count; break;
                default: throw ArgError($"unsupported unpack directive '{op}'");
            }
        }
        return result;
    }
}
