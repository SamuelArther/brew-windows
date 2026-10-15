using System.Text;
using System.Text.RegularExpressions;

namespace Brew.Ruby;

// Port of Homebrew's Version (Library/Homebrew/version.rb + version/parser.rb):
// detecting a version from a URL, and Version#<=> token comparison.
// Versions are plain strings here; null (or blank) plays the role of Version::NULL.
public static class Versions
{
    // Ruby regex: ^/$ are line anchors and \d is ASCII-only; {,n} means {0,n}.
    static Regex R(string pattern, RegexOptions extra = RegexOptions.None) =>
        new(pattern.Replace(@"\d", "[0-9]"), RegexOptions.Multiline | RegexOptions.CultureInvariant | extra);

    // ---- parsing ----

    const string NumericWithOptionalDots = @"(?:\d+(?:\.\d+)*)";
    const string NumericWithDots = @"(?:\d+(?:\.\d+)+)";
    const string MinorOrPatch = @"(?:\d+(?:\.\d+){1,2})";
    const string ContentSuffix = @"(?:[._-](?i:bin|dist|stable|src|sources?|final|full))";
    const string PrereleaseSuffix = @"(?:[._-]?(?i:alpha|beta|pre|rc)\.?\d{0,2})";

    sealed record VersionParser(bool Stem, Regex Regex, Func<string, string> Block = null);

    static VersionParser Stem(string re, Func<string, string> block = null) => new(true, R(re), block);
    static VersionParser Url(string re) => new(false, R(re));

    // Same order as Homebrew's VERSION_PARSERS.
    static readonly VersionParser[] Parsers =
    [
        // date-based versioning, e.g. `2023-09-28.tar.gz`, `ltopers-v2017-04-14.tar.gz`
        Stem(@"(?:^|[._-]?)v?(\d{4}-\d{2}-\d{2})"),
        // GitHub tarballs, e.g. `github.com/foo/bar/tarball/v1.2.3`
        Url(@"github\.com/.+/(?:zip|tar)ball/(?:v|\w+-)?((?:\d+[._-])+\d*)$"),
        // GitHub releases, e.g. `github.com/foo/bar/releases/download/v1.2/foo-1.2.0.tar.gz`
        Url($@"github\.com/.+/releases/download/(?:[rvV]_?)?({NumericWithDots})/"),
        // erlang style, e.g. `OTP_R15B01`
        Url(@"[_-]([Rr]\d+[AaBb]\d*(?:-\d+)?)"),
        // e.g. `boost_1_39_0`
        Stem(@"((?:\d+_)+\d+)$", s => s.Replace('_', '.')),
        // e.g. `foobar-4.5.1-1`, `unrtf_0.20.4-1`, `ruby-1.9.1-p243`
        Stem($@"[_-]({NumericWithDots}-(?:p|P|rc|RC)?\d+){ContentSuffix}?$"),
        // hyphenated versions without a name prefix, e.g. `v0.0.8-12.tar.gz`, `3.3.04-1.tar.gz`
        Stem($@"^v?({NumericWithDots}(?:-{NumericWithOptionalDots})+)"),
        // URL with no extension, e.g. `https://waf.io/waf-1.8.12`
        Url($@"[-v]({NumericWithOptionalDots})$"),
        // e.g. `lame-398-1`
        Stem(@"-(\d+-\d+)"),
        // e.g. `foobar-4.5.1`
        Stem($@"-({NumericWithOptionalDots})$"),
        // e.g. `foobar-4.5.1.post1`
        Stem($@"-({NumericWithOptionalDots}(?:.post\d+)?)$"),
        // e.g. `foobar-4.5.1b`
        Stem($@"-({NumericWithOptionalDots}(?:[abc]|rc|RC)\d*)$"),
        // e.g. `foobar-4.5.0-alpha5`, `foobar-4.5.0-beta1`, `foobar-4.50-beta`
        Stem($@"-({NumericWithOptionalDots}-(?:alpha|beta|rc)\d*)$"),
        // e.g. `libidn-1.29-win64.zip`, `libmicrohttpd-0.9.17-w32.zip`
        Stem($@"-({MinorOrPatch})-w(?:in)?(?:32|64)$"),
        // opam packages, e.g. `lablgtk.2.18.3+opam.tar.gz`
        Stem($@"\.({MinorOrPatch})\+opam$"),
        // e.g. `mtools-4.0.18-1.i686.rpm`, `libtasn1-2.8-x64.zip`, `mtools_4.0.18_i386.deb`
        Stem($@"[_-]({MinorOrPatch}(?:-\d+)?)[._-](?:i[36]86|x86|x64(?:[_-](?:32|64))?)$"),
        // e.g. `cli-1.3.0-beta.1.tgz`, `v2.074.0-rc1.tar.gz`, `premake-5.0.0-alpha10-src.zip`
        Stem($@"[-.vV]?({NumericWithDots}{PrereleaseSuffix})"),
        // e.g. `foobar4.5.1`
        Stem($@"({NumericWithOptionalDots})$"),
        // e.g. `foobar-4.5.0-bin`
        Stem($@"[-vV]({NumericWithDots}[abc]?){ContentSuffix}$"),
        // dash version style, e.g. `antlr-3.4-complete.jar`, `nuxeo-server-9.2-tomcat.zip`
        Stem($@"-({NumericWithDots})-"),
        // Debian style, e.g. `dash_0.5.5.1.orig.tar.gz`, `mkcue_1.orig.tar.gz`
        Stem($@"_({NumericWithOptionalDots}[abc]?)\.orig$"),
        // e.g. `openssl-0.9.8s.tar.gz`
        Stem(@"-v?(\d[^-]+)"),
        // e.g. `astyle_1.23_macosx.tar.gz`
        Stem(@"_v?(\d[^_]+)"),
        // version in a URL directory, e.g. `jenkins-ci.org/war/1.486/jenkins.war`
        Url(@"/(?:[rvV]_?)?(\d+\.\d+(?:\.\d+){0,2})"),
        // e.g. `jpegsrc.v8d.tar.gz`
        Stem(@"\.v(\d+[a-z]?)"),
        // e.g. `php.net/get/php-7.1.10.tar.bz2/from/this/mirror`
        Url($@"[-.vV]?({NumericWithDots}{PrereleaseSuffix}?)"),
    ];

    static readonly Regex SourceforgeDownload = R(@"(?:sourceforge\.net|sf\.net)/.*/download$");
    static readonly Regex NoFileExtension = R(@"\.[^a-zA-Z]+$");

    // Version.detect(url, tag:): a formula's version from its URL (or its git tag, when given).
    public static string Detect(string url, string tag = null) => Parse(tag ?? url);

    // Version.parse(spec, detected_from_url:). Returns null where Homebrew returns Version::NULL.
    // URLs are www-form decoded first (as Version.detect does); pass detectedFromUrl: false for raw strings.
    public static string Parse(string url, bool detectedFromUrl = true)
    {
        if (url == null) return null;
        var spec = url;
        if (detectedFromUrl)
        {
            spec = DecodeWwwFormComponent(spec);
            if (spec == null) return null; // Ruby raises on invalid %-encoding
        }
        foreach (var p in Parsers)
        {
            var m = p.Regex.Match(p.Stem ? StemSpec(spec) : spec);
            if (!m.Success || !m.Groups[1].Success) continue;
            var v = m.Groups[1].Value;
            if (string.IsNullOrWhiteSpace(v)) continue;
            if (p.Block != null) v = p.Block(v);
            if (!string.IsNullOrWhiteSpace(v)) return v;
        }
        return null;
    }

    // StemParser.process_spec
    static string StemSpec(string spec)
    {
        if (SourceforgeDownload.IsMatch(spec)) return PathStem(Dirname(spec));
        if (NoFileExtension.IsMatch(spec)) return Basename(spec);
        return PathStem(spec);
    }

    // URI.decode_www_form_component: '+' -> space, %XX -> byte; null on a bad %-escape.
    static string DecodeWwwFormComponent(string s)
    {
        if (s.IndexOf('%') < 0 && s.IndexOf('+') < 0) return s;
        if (Regex.IsMatch(s, "%(?![0-9A-Fa-f]{2})")) return null;
        var bytes = new List<byte>(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '+') bytes.Add((byte)' ');
            else if (c == '%') { bytes.Add(Convert.ToByte(s.Substring(i + 1, 2), 16)); i += 2; }
            else bytes.AddRange(Encoding.UTF8.GetBytes(c.ToString()));
        }
        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    // ---- Ruby File/Pathname helpers (POSIX semantics, '/' only) ----

    // File.basename
    static string Basename(string path)
    {
        if (path.Length == 0) return "";
        var trimmed = path.TrimEnd('/');
        if (trimmed.Length == 0) return "/";
        return trimmed[(trimmed.LastIndexOf('/') + 1)..];
    }

    // File.dirname
    static string Dirname(string path)
    {
        int root = 0;
        while (root < path.Length && path[root] == '/') root++;
        var rest = path[root..].TrimEnd('/');
        int sep = rest.LastIndexOf('/');
        if (sep < 0) return root == 0 ? "." : "/";
        while (sep > 0 && rest[sep - 1] == '/') sep--;
        return path[..(root + sep)];
    }

    // File.extname: the last '.' after any leading dots ("foo." -> ".").
    static string FileExtname(string basename)
    {
        int start = 0;
        while (start < basename.Length && basename[start] == '.') start++;
        int dot = basename.LastIndexOf('.');
        return dot < start ? "" : basename[dot..];
    }

    static readonly Regex BottleExtname = R(@"\.([a-z0-9_]+)\.bottle\.(?:(\d+)\.)?tar\.gz$");
    static readonly Regex ArchiveExtname = R(@"(\.(?:tar|cpio|pax)\.(?:gz|bz2|lz|xz|zst|Z))\Z");
    static readonly Regex VersionLikeTail = R(@"\b\d+\.\d+[^.]*\Z");

    // Homebrew's Pathname#extname
    static string Extname(string path)
    {
        var b = Basename(path);
        var bottle = BottleExtname.Match(b);
        if (bottle.Success) return bottle.Value;
        var archive = ArchiveExtname.Match(b);
        if (archive.Success) return archive.Groups[1].Value;
        // don't treat version numbers as extname
        if (VersionLikeTail.IsMatch(b) && !b.EndsWith(".7z", StringComparison.Ordinal)) return "";
        return FileExtname(b);
    }

    // Homebrew's Pathname#stem: File.basename(self, extname)
    static string PathStem(string path)
    {
        var b = Basename(path);
        var ext = Extname(path);
        if (ext.Length > 0 && b.Length > ext.Length && b.EndsWith(ext, StringComparison.Ordinal))
            return b[..^ext.Length];
        return b;
    }

    // ---- tokens ----

    enum Kind { Null, String, Numeric, Alpha, Beta, Pre, RC, Patch, Post }

    readonly record struct Token(Kind Kind, string Value)
    {
        public bool Numeric => Kind == Kind.Numeric;
        // CompositeToken#rev: first digit run (0 if none)
        public string Rev => Digits(Regex.Match(Value, "[0-9]+").Value);
        // Ruby's to_s: numeric tokens are Integers, so leading zeros drop
        public override string ToString() => Kind == Kind.Numeric ? Digits(Value) : Value ?? "";
    }

    static readonly Token NullToken = new(Kind.Null, null);

    const string AlphaPattern = "alpha[0-9]*|a[0-9]+";
    const string BetaPattern = "beta[0-9]*|b[0-9]+";
    const string PrePattern = "pre[0-9]*";
    const string RCPattern = "rc[0-9]*";
    const string PatchPattern = "p[0-9]*";
    const string PostPattern = ".post[0-9]+";
    const string NumericPattern = "[0-9]+";
    const string StringPattern = "[a-z]+";

    static Regex Whole(string p) => new($@"\A(?:{p})\z", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    // Token.create checks in this order.
    static readonly (Regex, Kind)[] TokenKinds =
    [
        (Whole(AlphaPattern), Kind.Alpha),
        (Whole(BetaPattern), Kind.Beta),
        (Whole(RCPattern), Kind.RC),
        (Whole(PrePattern), Kind.Pre),
        (Whole(PatchPattern), Kind.Patch),
        (Whole(PostPattern), Kind.Post),
        (Whole(NumericPattern), Kind.Numeric),
        (Whole(StringPattern), Kind.String),
    ];

    // SCAN_PATTERN = Regexp.union(Alpha, Beta, Pre, RC, Patch, Post, Numeric, String)
    static readonly Regex ScanPattern = new(
        string.Join("|", AlphaPattern, BetaPattern, PrePattern, RCPattern, PatchPattern, PostPattern, NumericPattern, StringPattern),
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    static Token CreateToken(string val)
    {
        foreach (var (re, kind) in TokenKinds)
            if (re.IsMatch(val)) return new Token(kind, val);
        throw new ArgumentException("Cannot find a matching token pattern");
    }

    static List<Token> Tokenize(string version) =>
        ScanPattern.Matches(version).Select(m => CreateToken(m.Value)).ToList();

    // Ruby Integer#to_s of a digit string (arbitrary size).
    static string Digits(string s)
    {
        if (string.IsNullOrEmpty(s)) return "0";
        var t = s.TrimStart('0');
        return t.Length == 0 ? "0" : t;
    }

    static int CompareDigits(string a, string b)
    {
        a = Digits(a);
        b = Digits(b);
        if (a.Length != b.Length) return a.Length < b.Length ? -1 : 1;
        return Math.Sign(string.CompareOrdinal(a, b));
    }

    static bool IsZero(Token t) => Digits(t.Value) == "0";

    // Token#<=> for every token class.
    static int CompareTokens(Token a, Token b)
    {
        switch (a.Kind)
        {
            case Kind.Null:
                return b.Kind switch
                {
                    Kind.Null => 0,
                    Kind.Numeric => IsZero(b) ? 0 : -1,
                    Kind.Alpha or Kind.Beta or Kind.Pre or Kind.RC => 1,
                    _ => -1,
                };
            case Kind.Numeric:
                return b.Kind switch
                {
                    Kind.Numeric => CompareDigits(a.Value, b.Value),
                    Kind.Null => IsZero(a) ? 0 : 1,
                    _ => 1,
                };
            case Kind.Alpha:
                if (b.Kind == Kind.Alpha) return CompareDigits(a.Rev, b.Rev);
                if (b.Kind is Kind.Beta or Kind.RC or Kind.Pre or Kind.Patch or Kind.Post) return -1;
                break;
            case Kind.Beta:
                if (b.Kind == Kind.Beta) return CompareDigits(a.Rev, b.Rev);
                if (b.Kind == Kind.Alpha) return 1;
                if (b.Kind is Kind.Pre or Kind.RC or Kind.Patch or Kind.Post) return -1;
                break;
            case Kind.Pre:
                if (b.Kind == Kind.Pre) return CompareDigits(a.Rev, b.Rev);
                if (b.Kind is Kind.Alpha or Kind.Beta) return 1;
                if (b.Kind is Kind.RC or Kind.Patch or Kind.Post) return -1;
                break;
            case Kind.RC:
                if (b.Kind == Kind.RC) return CompareDigits(a.Rev, b.Rev);
                if (b.Kind is Kind.Alpha or Kind.Beta or Kind.Pre) return 1;
                if (b.Kind is Kind.Patch or Kind.Post) return -1;
                break;
            case Kind.Patch:
            case Kind.Post:
                if (b.Kind == a.Kind) return CompareDigits(a.Rev, b.Rev);
                if (b.Kind is Kind.Alpha or Kind.Beta or Kind.RC or Kind.Pre) return 1;
                break;
        }
        // StringToken#<=>
        return b.Kind switch
        {
            Kind.Numeric => -1,
            Kind.Null => -CompareTokens(b, a),
            _ => Math.Sign(string.CompareOrdinal(a.Value, b.Value)),
        };
    }

    // ---- Version API ----

    static readonly Regex HeadVersion = new(@"\AHEAD(?:-(?<commit>.*))?\Z", RegexOptions.CultureInvariant);

    static bool IsNull(string v) => string.IsNullOrWhiteSpace(v);

    public static bool IsHead(string version) => version != null && HeadVersion.IsMatch(version);

    // The commit of a HEAD version ("HEAD-abcdef" -> "abcdef"), else null.
    public static string Commit(string version)
    {
        if (version == null) return null;
        var m = HeadVersion.Match(version);
        return m.Success && m.Groups["commit"].Success ? m.Groups["commit"].Value : null;
    }

    // Version#<=>. Null/blank is Version::NULL, which sorts below everything
    // (Ruby returns nil for NULL <=> NULL; this returns 0).
    public static int Compare(string a, string b)
    {
        if (IsNull(b)) return IsNull(a) ? 0 : 1;
        if (IsNull(a)) return -1;
        if (a == b) return 0;
        bool ha = IsHead(a), hb = IsHead(b);
        if (ha && !hb) return 1;
        if (!ha && hb) return -1;
        if (ha && hb) return 0;

        var lt = Tokenize(a);
        var rt = Tokenize(b);
        int max = Math.Max(lt.Count, rt.Count);
        int l = 0, r = 0;
        while (l < max)
        {
            var x = l < lt.Count ? lt[l] : NullToken;
            var y = r < rt.Count ? rt[r] : NullToken;
            if (CompareTokens(x, y) == 0)
            {
                l++;
                r++;
            }
            else if (x.Numeric && !y.Numeric)
            {
                if (CompareTokens(x, NullToken) > 0) return 1;
                l++;
            }
            else if (!x.Numeric && y.Numeric)
            {
                if (CompareTokens(y, NullToken) > 0) return -1;
                r++;
            }
            else
            {
                return CompareTokens(x, y);
            }
        }
        return 0;
    }

    // Version#== (NULL never equals anything, itself included).
    public static bool Equal(string a, string b) => !IsNull(a) && !IsNull(b) && Compare(a, b) == 0;

    // Version#compare(comparator, other)
    public static bool Compare(string a, string comparator, string b) => comparator switch
    {
        ">=" => !IsNull(a) && Compare(a, b) >= 0,
        ">" => !IsNull(a) && Compare(a, b) > 0,
        "<" => !IsNull(b) && Compare(a, b) < 0,
        "<=" => !IsNull(b) && Compare(a, b) <= 0,
        "==" => Equal(a, b),
        "!=" => !Equal(a, b),
        _ => throw new ArgumentException($"Unknown comparator: {comparator}"),
    };

    // Version's tokens as strings (numeric tokens keep their original digits).
    public static IReadOnlyList<string> Tokens(string version) =>
        IsNull(version) ? [] : Tokenize(version).Select(t => t.Value).ToList();

    static string TokenAt(string version, int i)
    {
        if (IsNull(version)) return null;
        var t = Tokenize(version);
        return i < t.Count ? t[i].ToString() : null;
    }

    // Version#major / #minor / #patch, as the token's to_s ("01" -> "1"); null when absent.
    public static string Major(string version) => TokenAt(version, 0);
    public static string Minor(string version) => TokenAt(version, 1);
    public static string Patch(string version) => TokenAt(version, 2);

    // Version#major_minor / #major_minor_patch: first 2/3 tokens joined with "."; null for NULL.
    public static string MajorMinor(string version) => FirstTokens(version, 2);
    public static string MajorMinorPatch(string version) => FirstTokens(version, 3);

    static string FirstTokens(string version, int n)
    {
        if (IsNull(version)) return null;
        var t = Tokenize(version).Take(n).Select(x => x.ToString()).ToList();
        return t.Count == 0 ? null : string.Join(".", t);
    }

    // An IComparer for sorting version strings the Homebrew way.
    public static readonly IComparer<string> Comparer = Comparer<string>.Create(Compare);
}
