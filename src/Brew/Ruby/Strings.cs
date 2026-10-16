using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Brew.Ruby;

public sealed partial class Interp
{
    // Strings are UTF-8 or binary (ASCII-8BIT); Encoding is just enough of Ruby's to name the two.
    const string EncodingPrelude = """
        class Encoding
          attr_reader :name
          def initialize(name, *names)
            @name = name
            @names = [name, *names]
          end
          def to_s = @name
          def names = @names
          def inspect = "#<Encoding:#{@name}>"
          def ==(other) = other.is_a?(Encoding) ? @name == other.name : @names.include?(other.to_s)
          UTF_8 = new("UTF-8")
          BINARY = ASCII_8BIT = new("ASCII-8BIT", "BINARY")
          US_ASCII = new("US-ASCII")
          def self.default_external = UTF_8
          def self.default_internal = nil
          def self.default_external=(value); end
          def self.default_internal=(value); end
          def self.find(name) = [UTF_8, BINARY, US_ASCII].find { |e| e == name.to_s.upcase } || UTF_8
          class CompatibilityError < StandardError; end
          class UndefinedConversionError < StandardError; end
          class InvalidByteSequenceError < StandardError; end
        end

        class String
          def encoding = __binary? ? Encoding::BINARY : Encoding::UTF_8
        end
        """;

    void InitStrings()
    {
        var St = StringClass;
        RString Self(object o) => (RString)o;
        // A binary (ASCII-8BIT) string holds one byte per char; others are UTF-8.
        static byte[] RawBytes(RString s) => RString.Bytes(s);
        // Ruby's encoding rules for joining strings: binary (ASCII-8BIT) data stays binary, with the other
        // side's UTF-8 bytes appended byte for byte, unless the binary side is plain ASCII.
        static void Append(RString target, object value, Interp it)
        {
            if (value is long l) { target.V += target.Binary && l is >= 0 and < 256 ? ((char)l).ToString() : char.ConvertFromUtf32((int)l); return; }
            var text = it.Str(value);
            bool otherBinary = value is RString { Binary: true };
            if (target.Binary == otherBinary) { target.V += text; return; }
            if (target.Binary ? IsAscii(target.V) && !IsAscii(text) : IsAscii(text) && !IsAscii(target.V))
            {   // ASCII-only binary data adopts the UTF-8 side's encoding
                target.V += text; target.Binary = false; return;
            }
            if (target.Binary) target.V += Encoding.Latin1.GetString(RString.Utf8(text));
            else { target.V = Encoding.Latin1.GetString(RString.Utf8(target.V)) + text; target.Binary = true; }
        }
        static bool IsAscii(string s) => s.All(c => c < 0x80);
        RString Mut(Interp it, object o)
        {
            var s = (RString)o;
            if (s.Frozen) throw it.Error(it.FrozenError, $"can't modify frozen String: {InspectStr(s.V)}");
            return s;
        }
        // defines both `name` and `name!` from a string transform
        void Bang(string name, Func<Interp, string, object[], string> f)
        {
            Def(St, name, (it, self, a, b) => S(f(it, Self(self).V, a)));
            Def(St, name + "!", (it, self, a, b) =>
            {
                var s = Mut(it, self);
                var r = f(it, s.V, a);
                if (r == s.V) return null;
                s.V = r;
                return s;
            });
        }

        Def(St, "initialize", (it, self, a, b) => { Self(self).V = a.Length > 0 && a[0] is RString s ? s.V : ""; return null; });
        Def(St, "+", (it, self, a, b) =>
        {
            var left = Self(self);
            var result = new RString(left.V) { Binary = left.Binary };
            Append(result, a[0], it);
            return result;
        });
        Def(St, "*", (it, self, a, b) =>
        {
            long n = it.Int(a[0]);
            if (n < 0) throw it.ArgError("negative argument");
            return S(string.Concat(Enumerable.Repeat(Self(self).V, (int)n)));
        });
        Def(St, "%", (it, self, a, b) => S(it.Format(Self(self).V, a[0] is RArray arr ? arr.L.ToArray() : new[] { a[0] })));
        Def(St, "== === eql?", (it, self, a, b) => a[0] is RString o && o.Equals(Self(self)));
        Def(St, "<=>", (it, self, a, b) => a[0] is RString o ? (long)Math.Sign(string.CompareOrdinal(Self(self).V, o.V)) : null);
        Def(St, "=~", (it, self, a, b) => a[0] switch
        {
            RRegex r => it.RegexMatch(r, Self(self).V, 0) is { } m ? (long)m.M.Index : null,
            RString => throw it.TypeErr("wrong argument type String (expected Regexp)"),
            _ => it.Invoke(a[0], "=~", new[] { self }, null),
        });
        Def(St, "match", (it, self, a, b) =>
        {
            var m = it.RegexMatch(it.ToRegex(a[0]), Self(self).V, a.Length > 1 ? (int)it.Int(a[1]) : 0);
            return m != null && b != null ? it.CallProc(b, new object[] { m }) : m;
        });
        Def(St, "match?", (it, self, a, b) => it.ToRegex(a[0]).R.IsMatch(Self(self).V, a.Length > 1 ? (int)it.Int(a[1]) : 0));
        Def(St, "[] slice", (it, self, a, b) => it.StrIndex(Self(self).V, a));
        Def(St, "[]=", (it, self, a, b) =>
        {
            var s = Mut(it, self);
            var val = it.Str(a[^1]);
            var key = a[..^1];
            if (key[0] is RRegex re)
            {
                var m = re.R.Match(s.V);
                if (!m.Success) throw it.Error(it.IndexError, "regexp not matched");
                var g = key.Length > 1 ? m.Groups[(int)it.Int(key[1])] : m.Groups[0];
                s.V = s.V[..g.Index] + val + s.V[(g.Index + g.Length)..];
                return a[^1];
            }
            if (key[0] is RString sub)
            {
                int i = s.V.IndexOf(sub.V, StringComparison.Ordinal);
                if (i < 0) throw it.Error(it.IndexError, "string not matched");
                s.V = s.V[..i] + val + s.V[(i + sub.V.Length)..];
                return a[^1];
            }
            var (start, len) = it.SliceBounds(s.V.Length, key);
            if (start < 0) throw it.Error(it.IndexError, $"index {it.Inspect(key[0])} out of string");
            s.V = s.V[..start] + val + s.V[Math.Min(s.V.Length, start + len)..];
            return a[^1];
        });
        Def(St, "length size", (it, self, a, b) => (long)Self(self).V.Length);
        Def(St, "bytesize", (it, self, a, b) => (long)RawBytes(Self(self)).Length);
        Def(St, "empty?", (it, self, a, b) => Self(self).V.Length == 0);
        Bang("upcase", (it, s, a) => s.ToUpperInvariant());
        Bang("downcase", (it, s, a) => s.ToLowerInvariant());
        Bang("capitalize", (it, s, a) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant());
        Bang("swapcase", (it, s, a) => new string(s.Select(c => char.IsUpper(c) ? char.ToLowerInvariant(c) : char.ToUpperInvariant(c)).ToArray()));
        Bang("strip", (it, s, a) => s.Trim(' ', '\t', '\n', '\v', '\f', '\r', '\0'));
        Bang("lstrip", (it, s, a) => s.TrimStart(' ', '\t', '\n', '\v', '\f', '\r', '\0'));
        Bang("rstrip", (it, s, a) => s.TrimEnd(' ', '\t', '\n', '\v', '\f', '\r', '\0'));
        Bang("chomp", (it, s, a) =>
        {
            if (a.Length > 0) { var suf = it.Str(a[0]); return s.EndsWith(suf, StringComparison.Ordinal) ? s[..^suf.Length] : s; }
            if (s.EndsWith("\r\n")) return s[..^2];
            return s.EndsWith('\n') || s.EndsWith('\r') ? s[..^1] : s;
        });
        Bang("chop", (it, s, a) => s.EndsWith("\r\n") ? s[..^2] : s.Length > 0 ? s[..^1] : s);
        Bang("squeeze", (it, s, a) =>
        {
            var set = a.Length > 0 ? TrSet(it.Str(a[0])) : null;
            var sb = new StringBuilder();
            foreach (var c in s)
                if (sb.Length == 0 || sb[^1] != c || (set != null && !set(c))) sb.Append(c);
            return sb.ToString();
        });
        Bang("delete", (it, s, a) => { var set = TrSet(it.Str(a[0])); return new string(s.Where(c => !set(c)).ToArray()); });
        Bang("tr", (it, s, a) => Tr(s, it.Str(a[0]), it.Str(a[1])));
        Bang("reverse", (it, s, a) => new string(s.Reverse().ToArray()));
        Bang("delete_prefix", (it, s, a) => { var p = it.Str(a[0]); return s.StartsWith(p, StringComparison.Ordinal) ? s[p.Length..] : s; });
        Bang("delete_suffix", (it, s, a) => { var p = it.Str(a[0]); return s.EndsWith(p, StringComparison.Ordinal) ? s[..^p.Length] : s; });
        Bang("succ", (it, s, a) => Succ(s));
        Def(St, "next", (it, self, a, b) => S(Succ(Self(self).V)));
        Def(St, "sub", (it, self, a, b) => S(it.Sub(Self(self).V, a, b, false)));
        Def(St, "gsub", (it, self, a, b) => S(it.Sub(Self(self).V, a, b, true)));
        Def(St, "sub!", (it, self, a, b) => { var s = Mut(it, self); var r = it.Sub(s.V, a, b, false); if (r == s.V) return null; s.V = r; return s; });
        Def(St, "gsub!", (it, self, a, b) => { var s = Mut(it, self); var r = it.Sub(s.V, a, b, true); if (r == s.V) return null; s.V = r; return s; });
        Def(St, "count", (it, self, a, b) => { var set = TrSet(it.Str(a[0])); return (long)Self(self).V.Count(c => set(c)); });
        Def(St, "chars", (it, self, a, b) => new RArray(Self(self).V.Select(c => (object)S(c.ToString()))));
        Def(St, "each_char", (it, self, a, b) =>
        {
            if (b == null) return new RArray(Self(self).V.Select(c => (object)S(c.ToString())));
            foreach (var c in Self(self).V.ToArray()) it.Yield(b, S(c.ToString()));
            return self;
        });
        Def(St, "bytes", (it, self, a, b) => new RArray(RawBytes(Self(self)).Select(x => (object)(long)x)));
        Def(St, "lines", (it, self, a, b) => new RArray(Lines(Self(self).V).Select(l => (object)S(l))));
        Def(St, "each_line", (it, self, a, b) =>
        {
            if (b == null) return new RArray(Lines(Self(self).V).Select(l => (object)S(l)));
            foreach (var l in Lines(Self(self).V)) it.CallProc(b, new object[] { S(l) });
            return self;
        });
        Def(St, "split", (it, self, a, b) => new RArray(it.Split(Self(self).V, a.Length > 0 ? a[0] : null, a.Length > 1 ? (int)it.Int(a[1]) : 0).Select(x => (object)S(x))));
        Def(St, "start_with?", (it, self, a, b) => a.Any(x => x is RRegex r
            ? r.R.Match(Self(self).V) is { Success: true, Index: 0 }
            : Self(self).V.StartsWith(it.Str(x), StringComparison.Ordinal)));
        Def(St, "end_with?", (it, self, a, b) => a.Any(x => Self(self).V.EndsWith(it.Str(x), StringComparison.Ordinal)));
        Def(St, "include?", (it, self, a, b) => Self(self).V.Contains(it.Str(a[0]), StringComparison.Ordinal));
        Def(St, "exclude?", (it, self, a, b) => !Self(self).V.Contains(it.Str(a[0]), StringComparison.Ordinal));   // ActiveSupport
        Def(St, "index", (it, self, a, b) =>
        {
            var s = Self(self).V;
            int start = a.Length > 1 ? (int)it.Int(a[1]) : 0;
            if (start < 0) start += s.Length;
            if (start < 0 || start > s.Length) return null;
            if (a[0] is RRegex r) { var m = it.RegexMatch(r, s, start); return m == null ? null : (long)m.M.Index; }
            int i = s.IndexOf(it.Str(a[0]), start, StringComparison.Ordinal);
            return i < 0 ? null : (long)i;
        });
        Def(St, "rindex", (it, self, a, b) =>
        {
            var s = Self(self).V;
            int start = a.Length > 1 ? (int)it.Int(a[1]) : s.Length;
            if (start < 0) start += s.Length;
            if (a[0] is RRegex r)
            {
                for (int i = Math.Min(start, s.Length); i >= 0; i--)
                    if (r.R.Match(s, i) is { Success: true } m && m.Index == i) return (long)i;
                return null;
            }
            var sub = it.Str(a[0]);
            int from = Math.Min(start + sub.Length - 1, s.Length - 1);
            int idx = from < 0 ? (sub.Length == 0 ? 0 : -1) : s.LastIndexOf(sub, from, StringComparison.Ordinal);
            return idx < 0 ? null : (long)idx;
        });
        Def(St, "replace", (it, self, a, b) => { Mut(it, self).V = it.Str(a[0]); return self; });
        Def(St, "<< concat", (it, self, a, b) =>
        {
            var s = Mut(it, self);
            foreach (var x in a) Append(s, x, it);
            return s;
        });
        Def(St, "prepend", (it, self, a, b) => { var s = Mut(it, self); s.V = string.Concat(a.Select(it.Str)) + s.V; return s; });
        Def(St, "insert", (it, self, a, b) =>
        {
            var s = Mut(it, self);
            int i = (int)it.Int(a[0]);
            if (i < 0) i += s.V.Length + 1;
            s.V = s.V.Insert(i, it.Str(a[1]));
            return s;
        });
        Def(St, "to_s to_str", (it, self, a, b) => self);
        Def(St, "to_sym intern", (it, self, a, b) => Sym(Self(self).V));
        Def(St, "to_i", (it, self, a, b) =>
        {
            int radix = a.Length > 0 ? (int)it.Int(a[0]) : 10;
            if (radix < 2 || radix > 36) throw it.ArgError($"invalid radix {radix}");
            var text = Self(self).V.TrimStart();
            var sign = 1L;
            if (text.StartsWith('-') || text.StartsWith('+')) { if (text[0] == '-') sign = -1; text = text[1..]; }
            // Ruby accepts the radix's own prefix (0x, 0b, 0o) and digits up to the first invalid one.
            var prefix = radix switch { 16 => "0x", 2 => "0b", 8 => "0o", _ => null };
            if (prefix != null && text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) text = text[2..];
            System.Numerics.BigInteger value = 0;
            var any = false;
            for (var k = 0; k < text.Length; k++)
            {
                var ch = char.ToLowerInvariant(text[k]);
                if (ch == '_' && any && k + 1 < text.Length && text[k + 1] != '_') continue;
                var digit = ch is >= '0' and <= '9' ? ch - '0' : ch is >= 'a' and <= 'z' ? ch - 'a' + 10 : 99;
                if (digit >= radix) break;
                value = value * radix + digit;
                any = true;
            }
            value *= sign;
            // The interpreter has no Bignum; out-of-range values saturate.
            return value > long.MaxValue ? long.MaxValue : value < long.MinValue ? long.MinValue : (long)value;
        });
        Def(St, "to_f", (it, self, a, b) =>
        {
            var m = Regex.Match(Self(self).V, @"^\s*[-+]?(\d[\d_]*)?(\.\d+)?([eE][-+]?\d+)?");
            return m.Success && m.Length > 0 && double.TryParse(m.Value.Replace("_", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0.0;
        });
        Def(St, "hex", (it, self, a, b) => it.Invoke(self, "to_i", new object[] { 16L }, null));
        Def(St, "ord", (it, self, a, b) => Self(self).V.Length == 0 ? throw it.ArgError("empty string") : (long)char.ConvertToUtf32(Self(self).V, 0));
        Def(St, "inspect dump", (it, self, a, b) => S(InspectStr(Self(self).V)));
        Def(St, "center", (it, self, a, b) =>
        {
            var s = Self(self).V;
            int w = (int)it.Int(a[0]);
            var pad = a.Length > 1 ? it.Str(a[1]) : " ";
            if (w <= s.Length) return S(s);
            int total = w - s.Length, left = total / 2, right = total - left;
            return S(Pad(pad, left) + s + Pad(pad, right));
        });
        Def(St, "ljust", (it, self, a, b) => { var s = Self(self).V; int w = (int)it.Int(a[0]); return S(w <= s.Length ? s : s + Pad(a.Length > 1 ? it.Str(a[1]) : " ", w - s.Length)); });
        Def(St, "rjust", (it, self, a, b) => { var s = Self(self).V; int w = (int)it.Int(a[0]); return S(w <= s.Length ? s : Pad(a.Length > 1 ? it.Str(a[1]) : " ", w - s.Length) + s); });
        Def(St, "+@", (it, self, a, b) => Self(self).Frozen ? S(Self(self).V) : self);
        Def(St, "-@ dedup", (it, self, a, b) => Self(self).Frozen ? self : new RString(Self(self).V) { Frozen = true });
        Def(St, "hash", (it, self, a, b) => (long)Self(self).V.GetHashCode());
        Def(St, "scan", (it, self, a, b) =>
        {
            var s = Self(self).V;
            var re = it.ToRegex(a[0]);
            var res = new RArray();
            foreach (Match m in re.R.Matches(s))
            {
                object item = m.Groups.Count > 1
                    ? new RArray(m.Groups.Cast<Group>().Skip(1).Select(g => g.Success ? (object)S(g.Value) : null))
                    : S(m.Value);
                it.Globals["$~"] = new RMatch { M = m, Input = s };
                if (b != null) it.CallProc(b, new[] { item });
                else res.L.Add(item);
            }
            return b != null ? self : res;
        });
        Def(St, "partition", (it, self, a, b) =>
        {
            var s = Self(self).V;
            if (a[0] is RRegex r)
            {
                var m = r.R.Match(s);
                return m.Success ? Triple(s[..m.Index], m.Value, s[(m.Index + m.Length)..]) : Triple(s, "", "");
            }
            var sep = it.Str(a[0]);
            int i = s.IndexOf(sep, StringComparison.Ordinal);
            return i < 0 ? Triple(s, "", "") : Triple(s[..i], sep, s[(i + sep.Length)..]);
        });
        Def(St, "rpartition", (it, self, a, b) =>
        {
            var s = Self(self).V;
            var sep = it.Str(a[0]);
            int i = s.LastIndexOf(sep, StringComparison.Ordinal);
            return i < 0 ? Triple("", "", s) : Triple(s[..i], sep, s[(i + sep.Length)..]);
        });
        Def(St, "casecmp", (it, self, a, b) => a[0] is RString o ? (long)Math.Sign(string.Compare(Self(self).V, o.V, StringComparison.OrdinalIgnoreCase)) : null);
        Def(St, "casecmp?", (it, self, a, b) => a[0] is RString o ? string.Equals(Self(self).V, o.V, StringComparison.OrdinalIgnoreCase) : null);
        Def(St, "__binary?", (it, self, a, b) => Self(self).Binary);   // String#encoding is in the Encoding prelude
        Def(St, "encode unicode_normalize scrub", (it, self, a, b) => self);
        Def(St, "b", (it, self, a, b) => new RString(Encoding.Latin1.GetString(RawBytes(Self(self)))) { Binary = true });
        Def(St, "force_encoding", (it, self, a, b) =>
        {
            var s = Self(self);
            bool binary = it.ToS(a[0]).ToUpperInvariant() is "BINARY" or "ASCII-8BIT";
            if (binary != s.Binary)
            {
                var bytes = RawBytes(s);
                s.V = binary ? Encoding.Latin1.GetString(bytes) : Encoding.UTF8.GetString(bytes);
                s.Binary = binary;
            }
            return s;
        });
        Def(St, "valid_encoding?", (it, self, a, b) => true);
        Def(St, "ascii_only?", (it, self, a, b) => Self(self).V.All(c => c < 128));
        Def(St, "each_byte", (it, self, a, b) =>
        {
            if (b == null) return new RArray(RawBytes(Self(self)).Select(x => (object)(long)x));
            foreach (var x in RawBytes(Self(self))) it.Yield(b, (long)x);
            return self;
        });
        Def(St, "unpack", (it, self, a, b) => new RArray(it.Unpack(Self(self), it.Str(a[0]))));
        Def(St, "unpack1", (it, self, a, b) => it.Unpack(Self(self), it.Str(a[0])).FirstOrDefault());
        DefS(St, "new", (it, self, a, b) => S(a.Length > 0 && a[0] is RString s ? s.V : ""));

        // ---- Regexp ----
        var R = RegexpClass;
        RegexpClass.Consts["IGNORECASE"] = 1L;
        RegexpClass.Consts["EXTENDED"] = 2L;
        RegexpClass.Consts["MULTILINE"] = 4L;
        DefS(R, "new compile", (it, self, a, b) =>
        {
            if (a[0] is RRegex r) return r;
            string flags = a.Length > 1 ? a[1] switch
            {
                long l => ((l & 1) != 0 ? "i" : "") + ((l & 2) != 0 ? "x" : "") + ((l & 4) != 0 ? "m" : ""),
                RString fs => fs.V,
                true => "i",
                _ => "",
            } : "";
            return it.MakeRegex(it.Str(a[0]), flags);
        });
        DefS(R, "escape quote", (it, self, a, b) => S(RubyEscape(it.StrOrSym(a[0]))));
        DefS(R, "union", (it, self, a, b) =>
        {
            var items = a.Length == 1 && a[0] is RArray arr ? arr.L : a.ToList();
            return it.MakeRegex(string.Join("|", items.Select(x => x is RRegex r ? r.Source : RubyEscape(it.Str(x)))), "");
        });
        DefS(R, "last_match", (it, self, a, b) => it.Globals.GetValueOrDefault("$~"));
        Def(R, "source", (it, self, a, b) => S(((RRegex)self).Source));
        Def(R, "to_s", (it, self, a, b) =>
        {
            var r = (RRegex)self;
            string on = string.Concat("mix".Where(c => r.Flags.Contains(c)));
            string off = string.Concat("mix".Where(c => !r.Flags.Contains(c)));
            return S($"(?{on}{(off.Length > 0 ? "-" + off : "")}:{r.Source})");
        });
        Def(R, "inspect", (it, self, a, b) => S(it.Inspect(self)));
        Def(R, "match", (it, self, a, b) => a[0] == null ? null : it.RegexMatch((RRegex)self, it.StrOrSym(a[0]), a.Length > 1 ? (int)it.Int(a[1]) : 0));
        Def(R, "match?", (it, self, a, b) => a[0] != null && ((RRegex)self).R.IsMatch(it.StrOrSym(a[0])));
        Def(R, "=~", (it, self, a, b) => a[0] == null ? null : it.RegexMatch((RRegex)self, it.StrOrSym(a[0]), 0) is { } m ? (long)m.M.Index : null);
        Def(R, "===", (it, self, a, b) => a[0] is RString or RSymbol && it.RegexMatch((RRegex)self, it.StrOrSym(a[0]), 0) != null);
        Def(R, "== eql?", (it, self, a, b) => a[0] is RRegex o && o.Source == ((RRegex)self).Source && o.Flags == ((RRegex)self).Flags);
        Def(R, "options", (it, self, a, b) => { var f = ((RRegex)self).Flags; return (long)((f.Contains('i') ? 1 : 0) | (f.Contains('x') ? 2 : 0) | (f.Contains('m') ? 4 : 0)); });
        Def(R, "names", (it, self, a, b) => new RArray(((RRegex)self).R.GetGroupNames().Where(n => !int.TryParse(n, out _)).Select(n => (object)S(n))));
        Def(R, "freeze", (it, self, a, b) => self);

        // ---- MatchData ----
        var Md = MatchDataClass;
        Def(Md, "[]", (it, self, a, b) =>
        {
            var m = ((RMatch)self).M;
            Group g = a[0] switch
            {
                long l => l < m.Groups.Count && l >= 0 ? m.Groups[(int)l] : null,
                RString s => m.Groups[s.V],
                RSymbol y => m.Groups[y.Name],
                _ => throw it.TypeErr("no implicit conversion into Integer"),
            };
            if (g == null) return null;
            if (a[0] is not long && !((RMatch)self).M.Groups.Keys.Contains(it.StrOrSym(a[0])))
                throw it.Error(it.IndexError, $"undefined group name reference: {it.StrOrSym(a[0])}");
            return g.Success ? S(g.Value) : null;
        });
        Def(Md, "captures", (it, self, a, b) => new RArray(((RMatch)self).M.Groups.Cast<Group>().Skip(1).Select(g => g.Success ? (object)S(g.Value) : null)));
        Def(Md, "named_captures", (it, self, a, b) =>
        {
            var m = ((RMatch)self).M;
            var h = new RHash();
            foreach (Group g in m.Groups)
                if (!int.TryParse(g.Name, out _)) h.D[S(g.Name)] = g.Success ? S(g.Value) : null;
            return h;
        });
        Def(Md, "names", (it, self, a, b) => new RArray(((RMatch)self).M.Groups.Cast<Group>().Where(g => !int.TryParse(g.Name, out _)).Select(g => (object)S(g.Name))));
        Def(Md, "to_a", (it, self, a, b) => new RArray(((RMatch)self).M.Groups.Cast<Group>().Select(g => g.Success ? (object)S(g.Value) : null)));
        Def(Md, "to_s", (it, self, a, b) => S(((RMatch)self).M.Value));
        Def(Md, "pre_match", (it, self, a, b) => S(((RMatch)self).Input[..((RMatch)self).M.Index]));
        Def(Md, "post_match", (it, self, a, b) => { var m = ((RMatch)self).M; return S(((RMatch)self).Input[(m.Index + m.Length)..]); });
        Def(Md, "begin", (it, self, a, b) => (long)((RMatch)self).M.Groups[(int)it.Int(a[0])].Index);
        Def(Md, "end", (it, self, a, b) => { var g = ((RMatch)self).M.Groups[(int)it.Int(a[0])]; return (long)(g.Index + g.Length); });
        Def(Md, "size length", (it, self, a, b) => (long)((RMatch)self).M.Groups.Count);
        Def(Md, "values_at", (it, self, a, b) => new RArray(a.Select(i => { var g = ((RMatch)self).M.Groups[(int)it.Int(i)]; return g.Success ? (object)S(g.Value) : null; })));
        Def(Md, "inspect", (it, self, a, b) => S($"#<MatchData {InspectStr(((RMatch)self).M.Value)}>"));

        // Strings cut from binary data stay binary (binread[0, 4], split, strip ...), as in Ruby.
        var textResults = new HashSet<string> { "inspect", "dump", "to_json", "b", "force_encoding", "__binary?", "+", "<<", "concat",
            "encode", "unicode_normalize", "scrub", "to_s", "to_str" };
        foreach (var m in St.Methods.Values.Where(m => m.Fn != null && !textResults.Contains(m.Name)).ToList())
        {
            var fn = m.Fn;
            m.Fn = (it, self, a, b) =>
            {
                var result = fn(it, self, a, b);
                if (self is RString { Binary: true })
                {
                    if (result is RString s && !ReferenceEquals(s, self) && !a.Any(x => ReferenceEquals(x, s))) s.Binary = true;
                    else if (result is RArray arr) foreach (var x in arr.L) if (x is RString xs) xs.Binary = true;
                }
                return result;
            };
        }
    }

    static RArray Triple(string a, string b, string c) => new(new object[] { S(a), S(b), S(c) });

    static string Pad(string pad, int n)
    {
        var sb = new StringBuilder();
        while (sb.Length < n) sb.Append(pad);
        return sb.ToString(0, n);
    }

    static IEnumerable<string> Lines(string s)
    {
        int start = 0;
        for (int i = 0; i < s.Length; i++)
            if (s[i] == '\n') { yield return s[start..(i + 1)]; start = i + 1; }
        if (start < s.Length) yield return s[start..];
    }

    public static string RubyEscape(string s)
    {
        var sb = new StringBuilder();
        foreach (var c in s)
        {
            switch (c)
            {
                case '\n': sb.Append("\\n"); break;
                case '\t': sb.Append("\\t"); break;
                case '\r': sb.Append("\\r"); break;
                case ' ': sb.Append("\\ "); break;
                default:
                    if (".*?+^$|()[]{}\\/-#".Contains(c)) sb.Append('\\');
                    sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    public RRegex ToRegex(object o) => o switch
    {
        RRegex r => r,
        RString s => MakeRegex(RubyEscape(s.V), ""),
        _ => throw TypeErr($"wrong argument type {TypeName(o)} (expected Regexp)"),
    };

    public (int start, int len) SliceBounds(int length, object[] a)
    {
        if (a[0] is RRange r)
        {
            int from = r.From == null ? 0 : (int)Int(r.From);
            int to = r.To == null ? length - 1 : (int)Int(r.To);
            if (from < 0) from += length;
            if (to < 0) to += length;
            if (r.Excl && r.To != null) to--;
            if (from < 0 || from > length) return (-1, 0);
            return (from, Math.Max(0, Math.Min(to, length - 1) - from + 1));
        }
        int start = (int)Int(a[0]);
        if (start < 0) start += length;
        if (a.Length > 1)
        {
            int len = (int)Int(a[1]);
            if (start < 0 || start > length || len < 0) return (-1, 0);
            return (start, Math.Min(len, length - start));
        }
        if (start < 0 || start >= length) return (-1, 0);
        return (start, 1);
    }

    object StrIndex(string s, object[] a)
    {
        switch (a[0])
        {
            case RString sub: return s.Contains(sub.V, StringComparison.Ordinal) ? S(sub.V) : null;
            case RRegex re:
            {
                var m = RegexMatch(re, s, 0);
                if (m == null) return null;
                var g = a.Length > 1 ? (a[1] is long gi ? m.M.Groups[(int)gi] : m.M.Groups[StrOrSym(a[1])]) : m.M.Groups[0];
                return g.Success ? S(g.Value) : null;
            }
        }
        var (start, len) = SliceBounds(s.Length, a);
        if (start < 0) return null;
        if (a.Length == 1 && a[0] is long && start >= s.Length) return null;
        return S(s.Substring(start, len));
    }

    string Sub(string s, object[] a, RProc blk, bool global)
    {
        var re = ToRegex(a[0]);
        RHash map = a.Length > 1 ? a[1] as RHash : null;
        string repl = a.Length > 1 && map == null ? Str(a[1]) : null;
        if (repl == null && map == null && blk == null) throw ArgCount(a, 1, 2);
        var sb = new StringBuilder();
        int pos = 0;
        var m = re.R.Match(s);
        while (m.Success)
        {
            sb.Append(s, pos, m.Index - pos);
            if (repl != null) sb.Append(ExpandReplacement(repl, m));
            else if (map != null) sb.Append(ToS(HashGet(map, S(m.Value))));
            else
            {
                Globals["$~"] = new RMatch { M = m, Input = s };
                sb.Append(ToS(CallProc(blk, new object[] { S(m.Value) })));
            }
            pos = m.Index + m.Length;
            if (!global) break;
            if (m.Length == 0)
            {
                if (pos < s.Length) sb.Append(s[pos]);
                pos++;
                if (pos > s.Length) break;
                m = re.R.Match(s, pos);
            }
            else m = m.NextMatch();
        }
        if (pos < s.Length) sb.Append(s, pos, s.Length - pos);
        return sb.ToString();
    }

    static string ExpandReplacement(string repl, Match m)
    {
        if (!repl.Contains('\\')) return repl;
        var sb = new StringBuilder();
        for (int i = 0; i < repl.Length; i++)
        {
            char c = repl[i];
            if (c != '\\' || i + 1 >= repl.Length) { sb.Append(c); continue; }
            char n = repl[++i];
            if (char.IsDigit(n)) sb.Append(m.Groups[n - '0'].Value);
            else if (n == '&' || n == '0') sb.Append(m.Value);
            else if (n == '`') sb.Append(m.Result("$`"));
            else if (n == '\'') sb.Append(m.Result("$'"));
            else if (n == '\\') sb.Append('\\');
            else if (n == 'k' && i + 1 < repl.Length && repl[i + 1] == '<')
            {
                int e = repl.IndexOf('>', i);
                sb.Append(m.Groups[repl[(i + 2)..e]].Value);
                i = e;
            }
            else sb.Append('\\').Append(n);
        }
        return sb.ToString();
    }

    List<string> Split(string s, object sep, int limit)
    {
        List<string> parts;
        if (sep == null || (sep is RString ws && ws.V == " "))
        {
            parts = new List<string>();
            var t = s.TrimStart(' ', '\t', '\n', '\v', '\f', '\r');
            while (t.Length > 0)
            {
                if (limit > 0 && parts.Count == limit - 1) { parts.Add(t); t = ""; break; }
                int i = 0;
                while (i < t.Length && !char.IsWhiteSpace(t[i])) i++;
                parts.Add(t[..i]);
                t = t[i..].TrimStart(' ', '\t', '\n', '\v', '\f', '\r');
            }
        }
        else if (sep is RString ss)
        {
            if (ss.V.Length == 0) parts = s.Select(c => c.ToString()).ToList();
            else parts = limit > 0 ? s.Split(ss.V, limit).ToList() : s.Split(ss.V).ToList();
        }
        else
        {
            var re = ToRegex(sep).R;
            parts = new List<string>();
            int pos = 0;
            foreach (Match m in re.Matches(s))
            {
                if (limit > 0 && parts.Count == limit - 1) break;
                if (m.Length == 0)
                {
                    if (m.Index >= s.Length) break;
                    if (m.Index == pos) continue;
                }
                parts.Add(s[pos..m.Index]);
                for (int g = 1; g < m.Groups.Count; g++) if (m.Groups[g].Success) parts.Add(m.Groups[g].Value);
                pos = m.Index + m.Length;
            }
            parts.Add(s[pos..]);
        }
        if (limit == 0) while (parts.Count > 0 && parts[^1].Length == 0) parts.RemoveAt(parts.Count - 1);
        return parts;
    }

    static Func<char, bool> TrSet(string spec)
    {
        bool neg = spec.Length > 1 && spec[0] == '^';
        var chars = ExpandTr(neg ? spec[1..] : spec);
        var set = new HashSet<char>(chars);
        return c => set.Contains(c) != neg;
    }

    static List<char> ExpandTr(string spec)
    {
        var r = new List<char>();
        for (int i = 0; i < spec.Length; i++)
        {
            if (spec[i] == '\\' && i + 1 < spec.Length) { r.Add(spec[++i]); continue; }
            if (i + 2 < spec.Length && spec[i + 1] == '-')
            {
                for (char c = spec[i]; c <= spec[i + 2]; c++) r.Add(c);
                i += 2;
            }
            else r.Add(spec[i]);
        }
        return r;
    }

    static string Tr(string s, string from, string to)
    {
        bool neg = from.Length > 1 && from[0] == '^';
        var f = ExpandTr(neg ? from[1..] : from);
        var t = ExpandTr(to);
        var sb = new StringBuilder();
        foreach (var c in s)
        {
            int i = f.IndexOf(c);
            if (neg) sb.Append(i < 0 ? (t.Count > 0 ? t[^1] : c) : c);
            else if (i < 0) sb.Append(c);
            else if (t.Count == 0) continue;
            else sb.Append(i < t.Count ? t[i] : t[^1]);
        }
        return sb.ToString();
    }

    static string Succ(string s)
    {
        if (s.Length == 0) return "";
        var chars = s.ToCharArray();
        int i = chars.Length - 1;
        while (i >= 0 && !char.IsLetterOrDigit(chars[i])) i--;
        if (i < 0) { chars[^1]++; return new string(chars); }
        while (true)
        {
            char c = chars[i];
            if (c == 'z') { chars[i] = 'a'; }
            else if (c == 'Z') { chars[i] = 'A'; }
            else if (c == '9') { chars[i] = '0'; }
            else { chars[i]++; return new string(chars); }
            int j = i - 1;
            while (j >= 0 && !char.IsLetterOrDigit(chars[j])) j--;
            if (j < 0)
            {
                char ins = c == '9' ? '1' : c == 'z' ? 'a' : 'A';
                return new string(chars).Insert(i, ins.ToString());
            }
            i = j;
        }
    }
}
