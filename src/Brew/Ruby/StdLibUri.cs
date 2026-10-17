using System.Text;
using System.Text.RegularExpressions;

namespace Brew.Ruby;

// URI (RFC 3986, like Ruby 3.4's default RFC3986_Parser) and the www-form helpers; CGI.escape and friends.
public sealed class RUri
{
    public string Scheme, Userinfo, Host, Path = "", Query, Fragment, Opaque;
    public long? Port;
    public RUri Copy() => (RUri)MemberwiseClone();
}

public sealed partial class Interp
{
    public RClass UriModule, UriGeneric, UriInvalidError;

    static readonly Dictionary<string, long> DefaultPorts = new()
    {
        ["http"] = 80, ["https"] = 443, ["ftp"] = 21, ["ws"] = 80, ["wss"] = 443, ["ldap"] = 389, ["ldaps"] = 636,
    };

    static readonly Regex UriRe = new(
        @"^(?:(?<scheme>[A-Za-z][A-Za-z0-9+\-.]*):)?(?://(?<authority>(?:(?<userinfo>[^@/?#\[\]]*)@)?(?<host>\[[0-9A-Fa-f:.vV]+\]|[^:/?#\[\]@]*)(?::(?<port>[0-9]*))?))?(?<path>[^?#]*)(?:\?(?<query>[^#]*))?(?:#(?<fragment>.*))?$",
        RegexOptions.Singleline);

    // Characters RFC 3986 allows anywhere in a URI (plus '%' for escapes).
    static readonly Regex UriChars = new(@"^[A-Za-z0-9\-._~:/?#\[\]@!$&'()*+,;=%]*$");

    RUri ParseUri(string s)
    {
        var m = UriChars.IsMatch(s) ? UriRe.Match(s) : Match.Empty;
        if (!m.Success || Regex.IsMatch(s, "%(?![0-9A-Fa-f]{2})"))
            throw Error(UriInvalidError, "bad URI (is not URI?): " + InspectStr(s));
        var u = new RUri();
        if (m.Groups["scheme"].Success) u.Scheme = m.Groups["scheme"].Value;
        if (m.Groups["authority"].Success)
        {
            if (m.Groups["userinfo"].Success) u.Userinfo = m.Groups["userinfo"].Value;
            u.Host = m.Groups["host"].Value;
            if (m.Groups["port"].Success && m.Groups["port"].Value.Length > 0) u.Port = long.Parse(m.Groups["port"].Value);
        }
        var path = m.Groups["path"].Value;
        if (u.Scheme != null && !m.Groups["authority"].Success && path.Length > 0 && path[0] != '/')
        {
            // mailto:a@b, urn:isbn:... — an opaque part instead of a path.
            u.Opaque = path + (m.Groups["query"].Success ? "?" + m.Groups["query"].Value : "");
            if (m.Groups["fragment"].Success) u.Fragment = m.Groups["fragment"].Value;
            return u;
        }
        if (u.Scheme == null && !m.Groups["authority"].Success && path.Contains(':') && (path.IndexOf('/') < 0 || path.IndexOf(':') < path.IndexOf('/')))
            throw Error(UriInvalidError, "bad URI (is not URI?): " + InspectStr(s));
        u.Path = path;
        if (m.Groups["query"].Success) u.Query = m.Groups["query"].Value;
        if (m.Groups["fragment"].Success) u.Fragment = m.Groups["fragment"].Value;
        return u;
    }

    static long? DefaultPort(RUri u) => u.Scheme != null && DefaultPorts.TryGetValue(u.Scheme.ToLowerInvariant(), out var p) ? p : null;

    static string UriToS(RUri u)
    {
        var sb = new StringBuilder();
        if (u.Scheme != null) sb.Append(u.Scheme).Append(':');
        if (u.Opaque != null) sb.Append(u.Opaque);
        else
        {
            if (u.Host != null)
            {
                sb.Append("//");
                if (u.Userinfo != null) sb.Append(u.Userinfo).Append('@');
                sb.Append(u.Host);
                if (u.Port != null && u.Port != DefaultPort(u)) sb.Append(':').Append(u.Port);
            }
            sb.Append(u.Path);
            if (u.Query != null) sb.Append('?').Append(u.Query);
        }
        if (u.Fragment != null) sb.Append('#').Append(u.Fragment);
        return sb.ToString();
    }

    RClass UriClassFor(string scheme)
    {
        var name = scheme?.ToUpperInvariant() switch
        {
            "HTTP" => "HTTP", "HTTPS" => "HTTPS", "FTP" => "FTP", "FILE" => "File", "MAILTO" => "MailTo",
            "LDAP" => "LDAP", "LDAPS" => "LDAPS", "WS" => "WS", "WSS" => "WSS", _ => null,
        };
        return name != null && UriModule.Consts.GetValueOrDefault(name) is RClass c ? c : UriGeneric;
    }

    RObject NewUri(RUri u, RClass cls = null)
    {
        if (u.Port == null && u.Host != null) u.Port = DefaultPort(u);
        return new RObject { Class = cls ?? UriClassFor(u.Scheme), Host = u };
    }

    RUri UriArg(object o) => o switch
    {
        RObject { Host: RUri u } => u,
        _ => ParseUri(Str(o)),
    };

    // RFC 3986 5.2.4
    static string RemoveDotSegments(string path)
    {
        var input = path;
        var output = new StringBuilder();
        while (input.Length > 0)
        {
            if (input.StartsWith("../")) input = input[3..];
            else if (input.StartsWith("./")) input = input[2..];
            else if (input.StartsWith("/./")) input = input[2..];
            else if (input == "/.") input = "/";
            else if (input.StartsWith("/../") || input == "/..")
            {
                input = input == "/.." ? "/" : input[3..];
                var o = output.ToString();
                int cut = o.LastIndexOf('/');
                output.Clear().Append(cut < 0 ? "" : o[..cut]);
            }
            else if (input is "." or "..") input = "";
            else
            {
                int start = input[0] == '/' ? 1 : 0;
                int next = input.IndexOf('/', start);
                if (next < 0) next = input.Length;
                output.Append(input[..next]);
                input = input[next..];
            }
        }
        return output.ToString();
    }

    // RFC 3986 5.2.2: resolve `rel` against `b`.
    RUri MergeUri(RUri b, RUri r)
    {
        if (b.Scheme == null && r.Scheme == null) throw Error(UriModule.Consts["BadURIError"] as RClass, "both URI are relative");
        var t = new RUri();
        if (r.Scheme != null)
        {
            t = r.Copy();
            t.Path = RemoveDotSegments(r.Path);
            return t;
        }
        if (r.Host != null)
        {
            t = r.Copy();
            t.Scheme = b.Scheme;
            t.Path = RemoveDotSegments(r.Path);
            return t;
        }
        t.Scheme = b.Scheme; t.Userinfo = b.Userinfo; t.Host = b.Host; t.Port = b.Port;
        if (r.Path.Length == 0)
        {
            t.Path = b.Path;
            t.Query = r.Query ?? b.Query;
        }
        else
        {
            if (r.Path.StartsWith('/')) t.Path = RemoveDotSegments(r.Path);
            else
            {
                string merged = b.Host != null && b.Path.Length == 0 ? "/" + r.Path : (b.Path.LastIndexOf('/') is var k && k >= 0 ? b.Path[..(k + 1)] : "") + r.Path;
                t.Path = RemoveDotSegments(merged);
            }
            t.Query = r.Query;
        }
        t.Fragment = r.Fragment;
        return t;
    }

    // URI.encode_www_form_component: unreserved *-._ and alphanumerics stay, space is '+', the rest %XX.
    public static string WwwEncode(string s, bool spaceAsPlus = true, string keep = "*-._")
    {
        var sb = new StringBuilder();
        foreach (var b in RString.Utf8(s))
        {
            char c = (char)b;
            if (b < 0x80 && (char.IsAsciiLetterOrDigit(c) || keep.IndexOf(c) >= 0)) sb.Append(c);
            else if (c == ' ' && spaceAsPlus) sb.Append('+');
            else sb.Append('%').Append(b.ToString("X2"));
        }
        return sb.ToString();
    }

    public string WwwDecode(string s, bool plusAsSpace = true)
    {
        if (Regex.IsMatch(s, "%(?![0-9A-Fa-f]{2})")) throw ArgError("invalid %-encoding (" + s + ")");
        var bytes = new List<byte>(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '+' && plusAsSpace) bytes.Add((byte)' ');
            else if (c == '%') { bytes.Add(Convert.ToByte(s.Substring(i + 1, 2), 16)); i += 2; }
            else bytes.AddRange(RString.Utf8(c.ToString()));
        }
        return DecodeUtf8Keep(bytes.ToArray());
    }

    // UTF-8 text, with bytes that aren't UTF-8 kept as raw bytes (RString's surrogate escapes).
    static string DecodeUtf8Keep(byte[] bytes)
    {
        try { return new UTF8Encoding(false, true).GetString(bytes); }
        catch (DecoderFallbackException) { return string.Concat(bytes.Select(b => b < 0x80 ? ((char)b).ToString() : ((char)(RString.RawByteBase + b)).ToString())); }
    }

    void InitUri()
    {
        var U = UriModule = DefineModule("URI");
        var uerr = DefineClass("Error", StandardError, U);
        UriInvalidError = DefineClass("InvalidURIError", uerr, U);
        DefineClass("InvalidComponentError", uerr, U);
        DefineClass("BadURIError", uerr, U);
        var G = UriGeneric = DefineClass("Generic", ObjectClass, U);
        var http = DefineClass("HTTP", G, U);
        DefineClass("HTTPS", http, U);
        DefineClass("FTP", G, U);
        DefineClass("File", G, U);
        DefineClass("MailTo", G, U);
        var ldap = DefineClass("LDAP", G, U);
        DefineClass("LDAPS", ldap, U);
        var ws = DefineClass("WS", G, U);
        DefineClass("WSS", ws, U);
        RUri Me(object o) => (RUri)((RObject)o).Host;
        RUri Mut(Interp it, object o)
        {
            if (((RObject)o).Frozen) throw it.Error(it.FrozenError, "can't modify frozen " + ((RObject)o).Class.Name);
            return Me(o);
        }

        DefS(U, "parse", (it, self, a, b) => a[0] is RObject { Host: RUri } ? a[0] : it.NewUri(it.ParseUri(it.Str(a[0]))));
        Def(KernelModule, "URI", (it, self, a, b) => a[0] is RObject { Host: RUri } ? a[0] : it.NewUri(it.ParseUri(it.Str(a[0]))));
        DefS(U, "join", (it, self, a, b) =>
        {
            if (a.Length == 0) throw it.ArgCount(a, 1, -1);
            var cur = it.UriArg(a[0]);
            foreach (var x in a.Skip(1)) cur = it.MergeUri(cur, it.UriArg(x));
            return it.NewUri(cur.Copy());
        });
        DefS(U, "split", (it, self, a, b) =>
        {
            var u = it.ParseUri(it.Str(a[0]));
            return new RArray(new object[] { Ns(u.Scheme), Ns(u.Userinfo), Ns(u.Host), u.Port?.ToString() is { } ps ? S(ps) : null, null, u.Opaque == null ? S(u.Path) : null, Ns(u.Opaque), Ns(u.Query), Ns(u.Fragment) });
        });
        DefS(U, "encode_www_form_component", (it, self, a, b) => S(WwwEncode(it.ToS(a[0]))));
        DefS(U, "encode_uri_component", (it, self, a, b) => S(WwwEncode(it.ToS(a[0]), false)));
        DefS(U, "decode_www_form_component", (it, self, a, b) => S(it.WwwDecode(it.Str(a[0]))));
        DefS(U, "decode_uri_component", (it, self, a, b) => S(it.WwwDecode(it.Str(a[0]), false)));
        DefS(U, "encode_www_form", (it, self, a, b) =>
        {
            var parts = new List<string>();
            foreach (var pair in it.ToList(a[0]))
            {
                var kv = pair is RArray pa ? pa.L : new List<object> { pair };
                var k = WwwEncode(it.ToS(kv[0]));
                if (kv.Count < 2 || kv[1] == null) { parts.Add(k); continue; }
                if (kv[1] is RArray vs)
                {
                    if (vs.L.Count == 0) parts.Add(k);
                    foreach (var v in vs.L) parts.Add(v == null ? k : k + "=" + WwwEncode(it.ToS(v)));
                }
                else parts.Add(k + "=" + WwwEncode(it.ToS(kv[1])));
            }
            return S(string.Join("&", parts));
        });
        DefS(U, "decode_www_form", (it, self, a, b) =>
        {
            var str = it.Str(a[0]);
            var res = new RArray();
            if (str.Length == 0) return res;
            var sep = Kw(a, "separator") is RString sp ? sp.V : "&";
            foreach (var part in str.Split(sep))
            {
                if (part.Length == 0) continue;
                int eq = part.IndexOf('=');
                var k = eq < 0 ? part : part[..eq];
                var v = eq < 0 ? "" : part[(eq + 1)..];
                res.L.Add(new RArray(new object[] { S(it.WwwDecode(k)), S(it.WwwDecode(v)) }));
            }
            return res;
        });
        DefS(U, "extract", (it, self, a, b) =>
        {
            var re = it.UriRegex(a.Length > 1 ? a[1] : null);
            var res = new RArray(re.Matches(it.Str(a[0])).Select(m => (object)S(m.Value)));
            if (b != null) { foreach (var x in res.L) it.CallProc(b, new[] { x }); return null; }
            return res;
        });
        DefS(U, "regexp", (it, self, a, b) => it.NewRegexObject(it.UriRegex(a.Length > 0 ? a[0] : null)));
        DefS(U, "scheme_list", (it, self, a, b) =>
        {
            var h = new RHash();
            foreach (var n in new[] { "FILE", "FTP", "HTTP", "HTTPS", "LDAP", "LDAPS", "MAILTO", "WS", "WSS" }) h.D[S(n)] = it.UriClassFor(n);
            return h;
        });

        // URI::DEFAULT_PARSER / URI::RFC2396_PARSER / URI::RFC3986_PARSER
        var parserClass = DefineClass("RFC3986_Parser", ObjectClass, U);
        U.Consts["RFC2396_Parser"] = parserClass;
        U.Consts["Parser"] = parserClass;
        var parser = new RObject { Class = parserClass };
        U.Consts["DEFAULT_PARSER"] = parser;
        U.Consts["RFC3986_PARSER"] = parser;
        U.Consts["RFC2396_PARSER"] = parser;
        DefS(parserClass, "new", (it, self, a, b) => new RObject { Class = (RClass)self });
        Def(parserClass, "parse", (it, self, a, b) => it.NewUri(it.ParseUri(it.Str(a[0]))));
        Def(parserClass, "join", (it, self, a, b) => it.Invoke(U, "join", a, null));
        Def(parserClass, "split", (it, self, a, b) => it.Invoke(U, "split", a, null));
        Def(parserClass, "extract", (it, self, a, b) => it.Invoke(U, "extract", a, b));
        Def(parserClass, "make_regexp", (it, self, a, b) => it.NewRegexObject(it.UriRegex(a.Length > 0 ? a[0] : null)));
        // RFC2396_Parser#escape: everything but unreserved and reserved characters is %-escaped.
        Def(parserClass, "escape", (it, self, a, b) =>
        {
            var str = it.Str(a[0]);
            Func<char, bool> keep = a.Length > 1 && a[1] is RRegex unsafeRe
                ? c => !unsafeRe.R.IsMatch(c.ToString())
                : c => char.IsAsciiLetterOrDigit(c) || "-_.!~*'();/?:@&=+$,[]#".IndexOf(c) >= 0;
            var sb = new StringBuilder();
            foreach (var ch in str)
            {
                if (ch < 0x80 && keep(ch)) sb.Append(ch);
                else foreach (var by in RString.Utf8(ch.ToString())) sb.Append('%').Append(by.ToString("X2"));
            }
            return S(sb.ToString());
        });
        Def(parserClass, "unescape", (it, self, a, b) => S(it.WwwDecode(it.Str(a[0]), false)));
        Def(parserClass, "regexp", (it, self, a, b) =>
        {
            var h = new RHash();
            h.D[Sym("ABS_URI")] = it.NewRegexObject(new Regex(@"\A\s*" + UriRe.ToString()[1..^1] + @"\s*\z"));
            h.D[Sym("UNSAFE")] = it.NewRegexObject(new Regex(@"[^-_.!~*'()a-zA-Z\d;/?:@&=+$,\[\]]"));
            return h;
        });

        // ---- URI::Generic ----
        Def(G, "scheme", (it, self, a, b) => Ns(Me(self).Scheme));
        Def(G, "host", (it, self, a, b) => Ns(Me(self).Host));
        Def(G, "hostname", (it, self, a, b) => Me(self).Host is { } h ? S(h.StartsWith('[') && h.EndsWith(']') ? h[1..^1] : h) : null);
        Def(G, "port", (it, self, a, b) => Me(self).Port);
        Def(G, "default_port", (it, self, a, b) => DefaultPort(Me(self)));
        Def(G, "path", (it, self, a, b) => Me(self).Opaque != null ? null : S(Me(self).Path));
        Def(G, "query", (it, self, a, b) => Ns(Me(self).Query));
        Def(G, "fragment", (it, self, a, b) => Ns(Me(self).Fragment));
        Def(G, "opaque", (it, self, a, b) => Ns(Me(self).Opaque));
        Def(G, "userinfo", (it, self, a, b) => Ns(Me(self).Userinfo));
        Def(G, "user", (it, self, a, b) => Me(self).Userinfo is { } ui ? S(ui.Split(':', 2)[0]) : null);
        Def(G, "password", (it, self, a, b) => Me(self).Userinfo is { } ui && ui.IndexOf(':') is var k && k >= 0 ? S(ui[(k + 1)..]) : null);
        Def(G, "decoded_user", (it, self, a, b) => Me(self).Userinfo is { } ui ? S(it.WwwDecode(ui.Split(':', 2)[0], false)) : null);
        Def(G, "decoded_password", (it, self, a, b) => Me(self).Userinfo is { } ui && ui.IndexOf(':') is var k && k >= 0 ? S(it.WwwDecode(ui[(k + 1)..], false)) : null);
        Def(G, "scheme=", (it, self, a, b) => { Mut(it, self).Scheme = it.NilOrStr(a[0]); return a[0]; });
        Def(G, "host=", (it, self, a, b) => { Mut(it, self).Host = it.NilOrStr(a[0]); return a[0]; });
        Def(G, "hostname=", (it, self, a, b) => { var h = it.NilOrStr(a[0]); Mut(it, self).Host = h != null && h.Contains(':') && !h.StartsWith('[') ? "[" + h + "]" : h; return a[0]; });
        Def(G, "port=", (it, self, a, b) => { Mut(it, self).Port = a[0] == null ? null : a[0] is RString ps ? long.Parse(ps.V) : it.Int(a[0]); return a[0]; });
        Def(G, "path=", (it, self, a, b) => { Mut(it, self).Path = it.NilOrStr(a[0]) ?? ""; return a[0]; });
        Def(G, "query=", (it, self, a, b) => { Mut(it, self).Query = it.NilOrStr(a[0]); return a[0]; });
        Def(G, "fragment=", (it, self, a, b) => { Mut(it, self).Fragment = it.NilOrStr(a[0]); return a[0]; });
        Def(G, "userinfo=", (it, self, a, b) => { Mut(it, self).Userinfo = a[0] is RArray ua ? string.Join(":", ua.L.Select(it.ToS)) : it.NilOrStr(a[0]); return a[0]; });
        Def(G, "user=", (it, self, a, b) =>
        {
            var u = Mut(it, self);
            var pw = u.Userinfo?.IndexOf(':') is int k && k >= 0 ? u.Userinfo[k..] : "";
            u.Userinfo = it.NilOrStr(a[0]) is { } user ? user + pw : null;
            return a[0];
        });
        Def(G, "password=", (it, self, a, b) =>
        {
            var u = Mut(it, self);
            var user = u.Userinfo?.Split(':', 2)[0] ?? throw it.Error(it.UriModule.Consts["InvalidURIError"] as RClass, "password component depends user component");
            u.Userinfo = it.NilOrStr(a[0]) is { } pw ? user + ":" + pw : user;
            return a[0];
        });
        Def(G, "to_s to_str", (it, self, a, b) => S(UriToS(Me(self))));
        Def(G, "inspect", (it, self, a, b) => S($"#<{((RObject)self).Class.Name} {UriToS(Me(self))}>"));
        Def(G, "absolute? absolute", (it, self, a, b) => Me(self).Scheme != null);
        Def(G, "relative?", (it, self, a, b) => Me(self).Scheme == null);
        Def(G, "hierarchical?", (it, self, a, b) => Me(self).Opaque == null);
        Def(G, "merge + join", (it, self, a, b) => it.NewUri(it.MergeUri(Me(self), it.UriArg(a[0]))));
        Def(G, "route_from", (it, self, a, b) => it.NewUri(new RUri { Path = Me(self).Path }));
        Def(G, "normalize", (it, self, a, b) =>
        {
            var u = Me(self).Copy();
            u.Scheme = u.Scheme?.ToLowerInvariant();
            u.Host = u.Host?.ToLowerInvariant();
            if (u.Path.Length == 0 && u.Host != null && u.Opaque == null) u.Path = "/";
            return it.NewUri(u, ((RObject)self).Class);
        });
        Def(G, "normalize!", (it, self, a, b) =>
        {
            var u = Mut(it, self);
            u.Scheme = u.Scheme?.ToLowerInvariant();
            u.Host = u.Host?.ToLowerInvariant();
            if (u.Path.Length == 0 && u.Host != null && u.Opaque == null) u.Path = "/";
            return self;
        });
        Def(G, "== eql?", (it, self, a, b) => a[0] is RObject { Host: RUri o } && UriToS(Normalized(Me(self))) == UriToS(Normalized(o)));
        Def(G, "hash", (it, self, a, b) => (long)UriToS(Normalized(Me(self))).GetHashCode());
        Def(G, "dup clone", (it, self, a, b) => it.NewUri(Me(self).Copy(), ((RObject)self).Class));
        Def(G, "freeze", (it, self, a, b) => { ((RObject)self).Frozen = true; return self; });
        Def(G, "component", (it, self, a, b) => new RArray(new object[] { Sym("scheme"), Sym("userinfo"), Sym("host"), Sym("port"), Sym("path"), Sym("query"), Sym("fragment") }));
        Def(G, "select", (it, self, a, b) => new RArray(a.Select(x => it.Invoke(self, it.StrOrSym(x), NoArgs, null))));
        Def(G, "find_proxy", (it, self, a, b) => null);
        Def(G, "request_uri", (it, self, a, b) =>
        {
            var u = Me(self);
            if (u.Opaque != null) return null;
            return S((u.Path.Length == 0 ? "/" : u.Path) + (u.Query != null ? "?" + u.Query : ""));
        });
        Def(G, "authority", (it, self, a, b) =>
        {
            var u = Me(self);
            if (u.Host == null) return null;
            return S(u.Host + (u.Port != null && u.Port != DefaultPort(u) ? ":" + u.Port : ""));
        });
        Def(G, "origin", (it, self, a, b) =>
        {
            var u = Me(self);
            return S(u.Scheme + "://" + u.Host + (u.Port != null && u.Port != DefaultPort(u) ? ":" + u.Port : ""));
        });
        Def(G, "to_json", (it, self, a, b) => S("\"" + UriToS(Me(self)).Replace("\\", "\\\\").Replace("\"", "\\\"") + "\""));
        DefS(G, "default_port", (it, self, a, b) => it.SchemeOf((RClass)self) is { } sc && DefaultPorts.TryGetValue(sc, out var p) ? p : null);

        // URI::HTTP.build(host: "x", path: "/y") / build(["userinfo", "host", port, "path", "query", "fragment"])
        DefS(G, "build", (it, self, a, b) =>
        {
            var cls = (RClass)self;
            var u = new RUri { Scheme = it.SchemeOf(cls) };
            string Get(int idx, string key)
            {
                if (a.Length > 0 && a[0] is RArray arr) return idx < arr.L.Count && arr.L[idx] != null ? it.ToS(arr.L[idx]) : null;
                return a.Length > 0 && a[0] is RHash h && h.D.TryGetValue(Sym(key), out var v) && v != null ? it.ToS(v) : null;
            }
            bool generic = cls == it.UriGeneric;
            int o = generic ? 1 : 0;
            if (generic) u.Scheme = Get(0, "scheme");
            u.Userinfo = Get(o, "userinfo");
            u.Host = Get(o + 1, "host");
            var port = Get(o + 2, "port");
            if (port != null) u.Port = long.Parse(port);
            if (generic) { var reg = Get(o + 3, "registry"); o++; }
            u.Path = Get(o + 3, "path") ?? "";
            if (generic) { u.Opaque = Get(o + 4, "opaque"); o++; }
            u.Query = Get(o + 4, "query");
            u.Fragment = Get(o + 5, "fragment");
            if (u.Host != null && u.Path.Length > 0 && u.Path[0] != '/')
                throw it.Error(it.UriModule.Consts["InvalidComponentError"] as RClass, "bad component(expected absolute path component): " + u.Path);
            return it.NewUri(u, cls);
        });

        // CGI.escape / unescape / escapeHTML / unescapeHTML (require "cgi").
        var cgi = ObjectClass.Consts.GetValueOrDefault("CGI") as RClass ?? DefineClass("CGI");
        DefS(cgi, "escape", (it, self, a, b) => S(WwwEncode(it.Str(a[0]))));
        DefS(cgi, "escapeURIComponent escape_uri_component", (it, self, a, b) => S(WwwEncode(it.Str(a[0]), false)));
        DefS(cgi, "unescape", (it, self, a, b) => S(it.WwwDecode(it.Str(a[0]))));
        DefS(cgi, "unescapeURIComponent unescape_uri_component", (it, self, a, b) => S(it.WwwDecode(it.Str(a[0]), false)));
        DefS(cgi, "escapeHTML escape_html h", (it, self, a, b) =>
            S(it.Str(a[0]).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#39;")));
        DefS(cgi, "unescapeHTML unescape_html", (it, self, a, b) =>
            S(Regex.Replace(it.Str(a[0]), @"&(amp|lt|gt|quot|apos|#39|#x([0-9a-fA-F]+)|#(\d+));", m => m.Groups[1].Value switch
            {
                "amp" => "&", "lt" => "<", "gt" => ">", "quot" => "\"", "apos" or "#39" => "'",
                _ => char.ConvertFromUtf32(m.Groups[2].Success ? Convert.ToInt32(m.Groups[2].Value, 16) : int.Parse(m.Groups[3].Value)),
            })));
    }

    static RUri Normalized(RUri u)
    {
        var n = u.Copy();
        n.Scheme = n.Scheme?.ToLowerInvariant();
        n.Host = n.Host?.ToLowerInvariant();
        return n;
    }

    string SchemeOf(RClass c)
    {
        for (var k = c; k != null && k != UriGeneric; k = k.Super)
            if (k.LexicalParent == UriModule) return k.Name[(k.Name.LastIndexOf(':') + 1)..].ToLowerInvariant();
        return null;
    }

    string NilOrStr(object o) => o == null ? null : ToS(o);

    static object Ns(string s) => s == null ? null : S(s);

    Regex UriRegex(object schemes)
    {
        var list = schemes is RArray arr ? arr.L.Select(ToS).ToList() : schemes is RString one ? new List<string> { one.V } : null;
        var scheme = list == null ? "[A-Za-z][A-Za-z0-9+\\-.]*" : "(?:" + string.Join("|", list.Select(Regex.Escape)) + ")";
        return new Regex(@"(?<![A-Za-z0-9+\-.])" + scheme + @":(?://[^\s<>""{}|\\^`/?#]*)?[^\s<>""{}|\\^`]*", RegexOptions.IgnoreCase);
    }

    RRegex NewRegexObject(Regex r) => new() { R = r, Source = r.ToString(), Flags = "" };
}
