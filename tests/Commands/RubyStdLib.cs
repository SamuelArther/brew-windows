using Brew.Ruby;

// Standard library pieces formula tests use: Bignum, Enumerator, Set, Time/Date, URI, YAML.
// Sockets are only checked for being defined; their runtime tests belong on CI.
static class RubyStdLib
{
    public static int Run(string root)
    {
        var count = 0;
        void Expect(string code, string expected)
        {
            var it = new Interp();
            string got;
            try { got = it.ToS(it.Run(code, "(test)")); }
            catch (RubyError e) { got = $"{e.ClassName}: {e.Message}"; }
            if (got != expected) throw new Exception($"{code}\n  expected {expected}\n  got      {got}");
            count++;
        }

        // Integers promote to Bignum instead of overflowing, and come back to the fast path.
        Expect("""[2**64, 9223372036854775807 + 1, -9223372036854775808 - 1, 3037000500 * 3037000500, 1 << 70, 0xffffffffffffffff].inspect""",
            "[18446744073709551616, 9223372036854775808, -9223372036854775809, 9223372037000250000, 1180591620717411303424, 18446744073709551615]");
        Expect("""["123456789012345678901234567890".to_i, Integer("99999999999999999999"), 1e20.to_i, (2**64).to_s(16), format("%d", -2**70)].inspect""",
            """[123456789012345678901234567890, 99999999999999999999, 100000000000000000000, "10000000000000000", "-1180591620717411303424"]""");
        Expect("""[(2**64).divmod(7), -(2**64) % 7, 2**64 > 2**63, [3, 2**70, 1].max, {2**64 => :a}[2**65 / 2], (2**100 / 2**99).equal?(2), (2**64 - 2**64 + 5).class, 0b1011].inspect""",
            "[[2635249153387078802, 2], 5, true, 1180591620717411303424, :a, true, Integer, 11]");

        // Blockless iteration answers an Enumerator that re-runs the method with a block.
        Expect("""[[1, 2, 3].select.with_index { |_, i| i > 0 }, [1, 2, 3].map.with_index(1) { |x, i| x * i }, %w[a b].each_with_index.map { |x, i| x + i.to_s }].inspect""",
            """[[2, 3], [1, 4, 9], ["a0", "b1"]]""");
        Expect("""e = [1, 2].each; [e.next, e.peek, e.next, (begin; e.next; rescue StopIteration; :stop; end), e.class, 3.times.to_a, (1..3).each_slice(2).to_a].inspect""",
            "[1, 2, 2, :stop, Enumerator, [0, 1, 2], [[1, 2], [3]]]");
        Expect("""g = Enumerator.new { |y| n = 0; loop { y << n; n += 1 } }; [g.take(3), g.lazy.map { |x| x * 2 }.select(&:positive?).first(2), (1..Float::INFINITY).lazy.map { |x| x * x }.first(3)].inspect""",
            "[[0, 1, 2], [2, 4], [1, 4, 9]]");
        Expect("""[1.step(10, 3).to_a, 10.step(1, -4).to_a, 1.0.step(2.0, 0.5).to_a, (1..10).step(4).to_a, 1.step(by: 2, to: 6).to_a, (0.0...1.0).step(0.25).to_a].inspect""",
            "[[1, 4, 7, 10], [10, 6, 2], [1.0, 1.5, 2.0], [1, 5, 9], [1, 3, 5], [0.0, 0.25, 0.5, 0.75]]");
        Expect("""[{a: 1, b: 2}.map.with_index { |(k, v), i| "#{k}#{v}#{i}" }, "ab".each_char.with_index.map { |c, i| c * (i + 1) }, [1, 2].cycle.take(3), [4, 5].each.with_object([]) { |x, acc| acc << x }].inspect""",
            """[["a10", "b21"], ["a", "bb"], [1, 2, 1], [4, 5]]""");

        // Set
        Expect("""s = Set.new([1, 2]); s << 3 << 2; [s.size, s.include?(2), (s | [4]).to_a, (s & Set[1, 9]).to_a, (s - [1]).to_a, Set[1] <= s, s.superset?(Set[1, 2]), s == Set[3, 2, 1], s.subset?(Set[1]), s.inspect].inspect""",
            """[3, true, [1, 2, 3, 4], [1], [2, 3], true, true, true, false, "#<Set: {1, 2, 3}>"]""");
        Expect("""[[1, 1, 2].to_set.size, %w[a b].to_set(&:upcase).to_a, Set[1, 2].map { |x| x * 2 }, Set[1, 2].add?(2), Set["a"].include?("a"), Set[[1, 2]].include?([1, 2])].inspect""",
            """[2, ["A", "B"], [2, 4], nil, true, true]""");

        // Time, Date, DateTime
        Expect("""t = Time.at(1700000000).utc; [t.to_s, t.iso8601, t.strftime("%a %b %e %T %Y|%-m/%-d %I%p %j %z %Z %s"), (t + 1.5).inspect, t - Time.at(1699999990), t.year].inspect""",
            """["2023-11-14 22:13:20 UTC", "2023-11-14T22:13:20Z", "Tue Nov 14 22:13:20 2023|11/14 10PM 318 +0000 UTC 1700000000", "2023-11-14 22:13:21.5 UTC", 10.0, 2023]""");
        Expect("""[Time.new(2024, 1, 2, 3, 4, 5, "+09:00").to_s, Time.utc(2024, 2, 29).strftime("%F %u %V"), Time.iso8601("2024-01-02T03:04:05Z").to_i, Time.parse("Tue, 02 Jan 2024 10:00:00 GMT").to_i, Time.strptime("2024-03-04 05:06:07 +0000", "%Y-%m-%d %H:%M:%S %z").to_i].inspect""",
            """["2024-01-02 03:04:05 +0900", "2024-02-29 4 09", 1704164645, 1704189600, 1709528767]""");
        Expect("""d = Date.new(2024, 1, 31); [d.to_s, (d + 1).to_s, (d >> 1).to_s, d - Date.new(2024, 1, 1), d.strftime("%Y%m%d %A"), Date.parse("3rd Feb 2001").to_s, Date.parse("Jan 5, 2023").to_s, Date.parse("10/11/2012").to_s, Date.strptime("03/04/2024", "%m/%d/%Y").to_s, (Date.new(2024, 1, 1)..Date.new(2024, 1, 3)).map(&:day), d.inspect].inspect""",
            """["2024-01-31", "2024-02-01", "2024-02-29", 30, "20240131 Wednesday", "2001-02-03", "2023-01-05", "2012-11-10", "2024-03-04", [1, 2, 3], "#<Date: 2024-01-31 ((2460341j,0s,0n),+0s,2299161j)>"]""");
        Expect("""dt = DateTime.parse("2024-01-02T03:04:05+07:00"); [dt.to_s, dt.hour, dt.strftime("%s"), dt.zone, (begin; Date.parse("garbage"); rescue Date::Error => e; e.message; end), (begin; Date.new(2023, 2, 30); rescue ArgumentError => e; e.class.name; end)].inspect""",
            """["2024-01-02T03:04:05+07:00", 3, "1704139445", "+07:00", "invalid date", "Date::Error"]""");

        // URI
        Expect("""u = URI("https://user:pw@example.com:8443/a/b?x=1#frag"); [u.class, u.scheme, u.host, u.port, u.path, u.query, u.fragment, u.user, u.password, u.request_uri, u.to_s].inspect""",
            """[URI::HTTPS, "https", "example.com", 8443, "/a/b", "x=1", "frag", "user", "pw", "/a/b?x=1", "https://user:pw@example.com:8443/a/b?x=1#frag"]""");
        Expect("""[URI.join("https://dovecot.org/releases/", "2.3").to_s, URI.join("https://a.com/x/y", "../z").to_s, URI.join("http://a/b/c/d;p?q", "g?y#s").to_s, URI("http://example.com:80/").to_s, URI::HTTP.build(host: "h", path: "/p", query: "a=b").to_s, URI.parse("http://x").port].inspect""",
            """["https://dovecot.org/releases/2.3", "https://a.com/z", "http://a/b/c/g?y#s", "http://example.com/", "http://h/p?a=b", 80]""");
        Expect("""[URI.encode_www_form("a" => "1 2", :b => "x&y", "c" => ["d", "e"]), URI.decode_www_form("a=1+2&b=%26&c").to_h, URI.encode_www_form_component("a b/é*"), URI.decode_www_form_component("a+b%2F%C3%A9"), (begin; URI("http://exa mple.com"); rescue URI::InvalidURIError => e; e.message; end)].inspect""",
            """["a=1+2&b=x%26y&c=d&c=e", {"a" => "1 2", "b" => "&", "c" => ""}, "a+b%2F%C3%A9*", "a b/é", "bad URI (is not URI?): \"http://exa mple.com\""]""");

        // YAML: Psych's scalar resolution, block/flow collections, block scalars, safe_load rules.
        Expect("""YAML.load("a: 1\nb: 1.5\nc: yes\nd: ~\ne: '123'\nf: 0x1F\ng: 1_000\nh: 12:30\ni: [1, two, {k: v}]\nj:\n- x\n- y: 2\n  z: 3\nk: |\n  l1\n    l2\nm: >-\n  f1\n  f2\n\n  p\nn: :sym\n").inspect""",
            """{"a" => 1, "b" => 1.5, "c" => true, "d" => nil, "e" => "123", "f" => 31, "g" => 1000, "h" => 750, "i" => [1, "two", {"k" => "v"}], "j" => ["x", {"y" => 2, "z" => 3}], "k" => "l1\n  l2\n", "m" => "f1 f2\np", "n" => :sym}""");
        Expect("""[YAML.safe_load("a: b", symbolize_names: true), (begin; YAML.safe_load("a: :s"); rescue Psych::DisallowedClass => e; e.message; end), (begin; YAML.load("d: 2024-01-02"); rescue Psych::DisallowedClass => e; e.message; end), YAML.safe_load("d: 2024-01-02", permitted_classes: [Date])["d"].to_s].inspect""",
            """[{a: "b"}, "Tried to load unspecified class: Symbol", "Tried to load unspecified class: Date", "2024-01-02"]""");
        Expect("""y = "base: &b\n  x: 1\n  y: 2\nderived:\n  <<: *b\n  y: 3\n"; [(begin; YAML.load(y); rescue Psych::AliasesNotEnabled; :no_aliases; end), YAML.load(y, aliases: true)["derived"], (begin; YAML.load("a: [1, 2"); rescue Psych::SyntaxError => e; e.message; end)].inspect""",
            """[:no_aliases, {"x" => 1, "y" => 3}, "(<unknown>): did not find expected ',' or ']' while parsing a flow sequence at line 1 column 4"]""");
        Expect("""{ "name" => "x", "n" => 1, "list" => [1, "two", { "a" => nil }], "e" => [], "s" => :s, "num" => "123", "path" => "/usr/bin", "multi" => "l1\nl2\n", "colon" => "a: b" }.to_yaml""",
            "---\nname: x\n\"n\": 1\nlist:\n- 1\n- two\n- a:\ne: []\ns: :s\nnum: '123'\npath: \"/usr/bin\"\nmulti: |\n  l1\n  l2\ncolon: 'a: b'\n");
        var yml = Path.Combine(root, "stdlib.yml").Replace('\\', '/');
        File.WriteAllText(yml, "shards:\n  molinillo:\n    git: https://github.com/crystal-lang/crystal-molinillo.git\n    version: 0.2.0\n");
        Expect($"""YAML.load_file("{yml}").dig("shards", "molinillo", "version")""", "0.2.0");
        Expect("""h = { "a" => { "b" => [1, { "c" => "d e" }] }, "t" => true }; YAML.load(h.to_yaml) == h""", "true");

        // Sockets exist (no socket is opened here).
        Expect("""[TCPServer.ancestors.include?(TCPSocket), TCPSocket.superclass.name, UNIXServer.superclass.name, defined?(Addrinfo), Socket::AF_INET, Errno::ECONNREFUSED.ancestors.include?(SystemCallError)].inspect""",
            """[true, "IPSocket", "UNIXSocket", "constant", 2, true]""");
        return count;
    }
}
