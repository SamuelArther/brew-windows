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
