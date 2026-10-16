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
        // "\x89" is one raw byte (imageoptim-cli compares PNG magic); valid UTF-8 escapes decode.
        Expect($"""File.binwrite("{file}", "\x89PNG"); ["\x89PNG".b == File.binread("{file}")[0, 4], File.binread("{file}")[0, 2].bytesize, "\x89".bytesize, "\xC3\xA9" == "é"].inspect""", "[true, 2, 1, true]");
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
