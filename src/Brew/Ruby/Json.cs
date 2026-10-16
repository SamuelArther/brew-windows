using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Brew.Ruby;

// Ruby's json library: JSON.parse / generate / pretty_generate, #to_json and JSON::ParserError.
public sealed partial class Interp
{
    void InitJson()
    {
        var json = DefineModule("JSON");
        var jsonError = DefineClass("JSONError", StandardErrorClass(), json);
        var parserError = DefineClass("ParserError", jsonError, json);
        DefineClass("GeneratorError", jsonError, json);

        DefS(json, "parse", (it, self, a, b) =>
        {
            var symbolize = a.Length > 1 && a[^1] is RHash opts && opts.D.TryGetValue(Interp.Sym("symbolize_names"), out var s) && Truthy(s);
            try
            {
                using var doc = JsonDocument.Parse(it.Str(a[0]), new JsonDocumentOptions { AllowTrailingCommas = false, MaxDepth = 256 });
                return it.FromJson(doc.RootElement, symbolize);
            }
            catch (JsonException e) { throw it.Error(parserError, e.Message); }
        });
        DefS(json, "generate dump", (it, self, a, b) => S(it.ToJson(a[0], null, 0)));
        DefS(json, "pretty_generate", (it, self, a, b) => S(it.ToJson(a[0], "  ", 0)));
        Def(ObjectClass, "to_json", (it, self, a, b) => S(it.ToJson(self, null, 0)));
    }

    RClass StandardErrorClass() => ConstGet(ObjectClass, "StandardError") as RClass ?? RuntimeError;

    object FromJson(JsonElement e, bool symbolize) => e.ValueKind switch
    {
        JsonValueKind.Object => new RHash { D = Ordered(e.EnumerateObject().Select(p => new KeyValuePair<object, object>(symbolize ? Interp.Sym(p.Name) : S(p.Name), FromJson(p.Value, symbolize)))) },
        JsonValueKind.Array => new RArray(e.EnumerateArray().Select(x => FromJson(x, symbolize))),
        JsonValueKind.String => S(e.GetString()),
        JsonValueKind.Number => e.TryGetInt64(out var l) ? l : (object)e.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null,
    };

    static OrderedDictionary<object, object> Ordered(IEnumerable<KeyValuePair<object, object>> pairs)
    {
        var d = new OrderedDictionary<object, object>(KeyComparer.Instance);
        foreach (var kv in pairs) d[kv.Key] = kv.Value;
        return d;
    }

    string ToJson(object v, string indent, int depth)
    {
        string Pad(int n) => indent == null ? "" : "\n" + string.Concat(Enumerable.Repeat(indent, n));
        switch (v)
        {
            case null: return "null";
            case bool t: return t ? "true" : "false";
            case long or int or System.Numerics.BigInteger: return Convert.ToString(v, CultureInfo.InvariantCulture);
            case double d: return d.ToString("R", CultureInfo.InvariantCulture);
            case RString s: return Quote(s.V);
            case RSymbol sym: return Quote(sym.Name);
            case RArray arr:
                if (arr.L.Count == 0) return "[]";
                return "[" + string.Join(",", arr.L.Select(x => Pad(depth + 1) + ToJson(x, indent, depth + 1))) + Pad(depth) + "]";
            case RHash h:
                if (h.D.Count == 0) return "{}";
                var sep = indent == null ? ":" : ": ";
                return "{" + string.Join(",", h.D.Select(kv => Pad(depth + 1) + Quote(kv.Key is RSymbol k ? k.Name : ToS(kv.Key)) + sep + ToJson(kv.Value, indent, depth + 1))) + Pad(depth) + "}";
            default:
                if (v is RObject && RespondTo(v, "to_h")) return ToJson(Invoke(v, "to_h", NoArgs, null), indent, depth);
                return Quote(ToS(v));
        }
    }

    static string Quote(string s)
    {
        var sb = new StringBuilder("\"");
        foreach (var c in s)
            sb.Append(c switch
            {
                '"' => "\\\"", '\\' => "\\\\", '\n' => "\\n", '\r' => "\\r", '\t' => "\\t", '\b' => "\\b", '\f' => "\\f",
                < ' ' => $"\\u{(int)c:x4}",
                _ => c.ToString(),
            });
        return sb.Append('"').ToString();
    }
}
