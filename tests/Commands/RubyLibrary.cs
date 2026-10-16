using Brew.Ruby;

// Bits of Ruby's standard library formula tests use: json, binary strings, Pathname spelling.
static class RubyLibrary
{
    public static int Run(string root)
    {
        var count = 0;
        void Expect(string code, string expected)
        {
            var it = new Interp();
            var got = it.ToS(it.Run(code, "(test)"));
            if (got != expected) throw new Exception($"{code}\n  expected {expected}\n  got      {got}");
            count++;
        }
        Expect("""JSON.generate(JSON.parse('{"a":[1,2.5,null,true,"x\\"y"]}'))""", """{"a":[1,2.5,null,true,"x\"y"]}""");
        Expect("""JSON.parse('{"a":{"b":2}}', symbolize_names: true)[:a][:b].to_s""", "2");
        Expect("""{ "k" => [1, "v"] }.to_json""", """{"k":[1,"v"]}""");
        Expect("""JSON.pretty_generate({ "a" => 1 })""", "{\n  \"a\": 1\n}");
        Expect("""begin; JSON.parse("{"); "no"; rescue JSON::ParserError; "raised"; end""", "raised");
        Expect("""Pathname.new("C:\\x\\y").join("z").to_s""", "C:/x/y/z");
        Expect("""[0x66, 0xb8, 0, 0xcd].pack("C*").unpack("C*").inspect""", "[102, 184, 0, 205]");
        Expect("""["cafe"].pack("H*").unpack1("H*")""", "cafe");
        Expect("""[1, 2].pack("nN").bytes.inspect""", "[0, 1, 0, 0, 0, 2]");
        Expect("""[513].pack("v").unpack1("v").to_s""", "513");
        Expect("""["aGk="].first.unpack1("m")""", "hi");
        Expect("""["hi"].pack("m0")""", "aGk=");
        Expect("""["89".to_i(8), "102".to_i(2), "zz".to_i(36), "0x1f".to_i(16), "-12abc".to_i].inspect""", "[0, 2, 1295, 31, -12]");
        Expect("""JSON.generate([1.0, 2.5, 1e20])""", "[1.0,2.5,1.0e+20]");
        Expect("""[233].pack("U").bytes.inspect""", "[195, 169]");
        var file = Path.Combine(root, "binwrite.bin").Replace('\\', '/');
        Expect($"""File.binwrite("{file}", [0x89, 0x50].pack("C*")); File.binread("{file}").bytes.inspect""", "[137, 80]");
        // Joining binary data keeps it binary (apptainer's SIF fixture: 713 packs to 0xC9 0x02).
        Expect("""([713].pack("q<") + "x\n").bytesize.to_s""", "10");
        Expect("""s = "".b; s << 200 << "ab"; [s.bytesize, s.encoding.to_s].inspect""", """[3, "ASCII-8BIT"]""");
        Expect("""[("a".b + "é").encoding.to_s, "é".b.bytesize, "é".b.force_encoding(Encoding::UTF_8) == "é"].inspect""", """["UTF-8", 2, true]""");
        Expect($"""[File.size?("{file}"), File.size?("{file}.missing")].inspect""", "[2, nil]");
        // Regexp.last_match(n) is a group (install_steps' {{token}} expansion); MatchData == compares identity, not recursively.
        Expect("""["{{a}}/x".gsub(/\{\{(\w+)\}\}/) { Regexp.last_match(1).upcase }, "ab".match(/a/) == "a"].inspect""", """["A/x", false]""");
        // "\x89" is one raw byte (imageoptim-cli compares PNG magic); valid UTF-8 escapes decode.
        Expect($"""File.binwrite("{file}", "\x89PNG"); ["\x89PNG".b == File.binread("{file}")[0, 4], File.binread("{file}")[0, 2].bytesize, "\x89".bytesize, "\xC3\xA9" == "é"].inspect""", "[true, 2, 1, true]");
        // Core semantics: octal escapes (sshs writes "\003"), retry (cataclysm), bare super, float/%g spelling.
        Expect("""("\016\263\0\f\v").bytes.inspect""", "[14, 179, 0, 12, 11]");
        Expect("""n = 0; out = []; begin; n += 1; raise "x" if n < 3; out << n; rescue; retry; ensure; out << :e; end; out.inspect""", "[3, :e]");
        Expect("""class E1 < StandardError; def initialize(msg = "d") = super; end; begin; raise E1; rescue => e; e.message; end""", "d");
        Expect("""[1e16, 1e15, -0.0, 1.0e-5, 0.0001].inspect""", "[1.0e+16, 1000000000000000.0, -0.0, 1.0e-05, 0.0001]");
        Expect("""[format("%g", 1234567.0), format("%g", 100.0), format("%.3g", 3.14159)].inspect""", """["1.23457e+06", "100", "3.14"]""");
        Expect("""begin; "a" =~ "a"; rescue TypeError; "raised"; end""", "raised");
        Expect("""["\0".inspect, "abc".each_char.to_a.size, ENV.exclude?("BREW_SURELY_UNSET_VAR")].inspect""", """["\"\\u0000\"", 3, true]""");
        // Ruby regexp: \h inside a class, and named groups make plain groups non-capturing.
        Expect("""["ab_f9 z"[/[\h_]+/], "v1.2".match(/v(?<maj>\d+)\.(\d+)/).captures, "x y"[/[^[:space:]]+/]].inspect""", """["ab_f9", ["1"], "x"]""");
        Expect("""[(begin; "a".center; rescue ArgumentError; :arg; end), (begin; {}.merge(1); rescue TypeError; :type; end)].inspect""", "[:arg, :type]");
        Expect("""[Digest::SHA256.hexdigest("abc")[0, 8], (begin; Timeout.timeout(0.2) { loop { } }; rescue Timeout::Error; :expired; end)].inspect""", """["ba7816bf", :expired]""");
        Expect($"""File.binwrite("{file}", [0x89, 0xff, 0x0d, 0x0a].pack("C*")); File.read("{file}").bytes.inspect""", "[137, 255, 10]");
        Expect("""["a\nb\r\n".lines(chomp: true), "a\nb".lines].inspect""", """[["a", "b"], ["a\n", "b"]]""");
        Expect("""[$stdout.fileno, STDERR.fileno, $stdout.tty?].inspect""", "[1, 2, false]");
        // Backticks run through /bin/sh (bash) like Ruby, when there is one.
        if (PosixShell.FindShell(Environment.GetEnvironmentVariable) != null)
            Expect("""`echo 'a  b' "c"`""", "a  b c\n");
        // rm_rf, find, ** and cp_r don't walk into symlinked directories (a link loop used to hang them).
        var tree = Path.Combine(root, "link-loop");
        Directory.CreateDirectory(Path.Combine(tree, "sub"));
        var symlinks = true;
        try { Directory.CreateSymbolicLink(Path.Combine(tree, "sub", "loop"), tree); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { symlinks = false; }
        if (symlinks)
        {
            var t = tree.Replace('\\', '/');
            Expect($"""[Pathname("{t}").find.count, Dir["{t}/**/*"].size, (FileUtils.cp_r("{t}", "{t}-copy"); File.symlink?("{t}-copy/sub/loop"))].inspect""", "[3, 2, true]");
            Expect($"""FileUtils.rm_rf ["{t}", "{t}-copy"]; [Dir.exist?("{t}"), Dir.exist?("{t}-copy")].inspect""", "[false, false]");
        }
        // `return` in a test block leaves the test (Homebrew defines it with define_method).
        var formula = Path.Combine(root, "returner.rb");
        File.WriteAllText(formula, """
            class Returner < Formula
              url "https://example.com/returner-1.0.tar.gz"
              version "1.0"
              test do
                (testpath/"before").write "x"
                [1].each { return if true }
                (testpath/"after").write "y"
              end
            end
            """);
        var testPath = Path.Combine(root, "returner-test");
        Brew.Ruby.FormulaLoader.LoadSpec(formula).RunTest(Path.Combine(root, "returner-prefix"), null, testPath);
        if (!File.Exists(Path.Combine(testPath, "before")) || File.Exists(Path.Combine(testPath, "after"))) throw new Exception("return in test do");
        count++;
        // Formula-level preludes (blank?/present?, post_install_steps) load with the Formula class.
        var formulaIt = Brew.Ruby.FormulaLoader.NewInterpreterForTests();
        foreach (var (code, expected) in new[] { ("[nil.present?, \" \".blank?, [1].present?, {}.blank?].inspect", "[false, true, true, true]"),
                     ("Formula.respond_to?(:post_install_steps).to_s", "true") })
        {
            var got = formulaIt.ToS(formulaIt.Run(code, "(test)"));
            if (got != expected) throw new Exception($"{code}\n  expected {expected}\n  got      {got}");
            count++;
        }
        return count;
    }
}
