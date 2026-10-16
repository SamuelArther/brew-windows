using Brew.Ruby;

// The sh subset formula tests' command strings are read with: quoting, $VAR, NAME=value, redirections
// and globs; anything needing a real shell comes back null.
static class PosixLines
{
    public static int Run(string root)
    {
        var dir = Path.Combine(root, "posix-lines");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "a.txt"), "");
        File.WriteAllText(Path.Combine(dir, "b.txt"), "");
        var env = new Dictionary<string, string> { ["HOME"] = "C:/home", ["NAME"] = "cat" };
        PosixShell.Command Parse(string line) => PosixShell.Parse(line, k => env.GetValueOrDefault(k), dir);
        var count = 0;
        void Argv(string line, params string[] expected)
        {
            var got = Parse(line)?.Argv ?? throw new Exception($"Not parsed: {line}");
            if (!got.SequenceEqual(expected)) throw new Exception($"{line}: got [{string.Join(", ", got)}]");
            count++;
        }
        void Shell(string line)
        {
            if (Parse(line) != null) throw new Exception($"Should need a shell: {line}");
            count++;
        }

        Argv(@"C:\x\bin/yq eval --null-input '.key = ""cat""'", @"C:\x\bin/yq", "eval", "--null-input", ".key = \"cat\"");
        Argv("tool \"a $NAME\" ${NAME}s 'lit $NAME' \\$x", "tool", "a cat", "cats", "lit $NAME", "$x");
        Argv("tool ~/x a\\ b \"q\\\"q\"", "tool", "C:/home/x", "a b", "q\"q");
        Argv("tool *.txt '*.txt' none*", "tool", "a.txt", "b.txt", "*.txt", "none*");
        Argv("tool /dev/null # comment", "tool", "/dev/null");

        var c = Parse("FOO=bar tool -v 2>&1");
        if (c.Assignments["FOO"] != "bar" || c.Stderr.Kind != PosixShell.Target.Out || c.Argv.Count != 2) throw new Exception("NAME=value 2>&1");
        c = Parse("tool >/dev/null 2>&1");
        if (c.Stdout.Kind != PosixShell.Target.Null || c.Stderr.Kind != PosixShell.Target.Null) throw new Exception(">/dev/null 2>&1");
        c = Parse("tool 2>&1 >out.txt");
        if (c.Stderr.Kind != PosixShell.Target.Out || c.Stdout.Kind != PosixShell.Target.File || !c.Stdout.Path.EndsWith("out.txt")) throw new Exception("2>&1 >file");
        c = Parse("tool >>log 2>err <in.txt");
        if (!c.Stdout.Append || c.Stderr.Kind != PosixShell.Target.File || c.StdinFile != "in.txt") throw new Exception(">> 2> <");
        c = Parse("tool &>/dev/null");
        if (c.Stdout.Kind != PosixShell.Target.Null || c.Stderr.Kind != PosixShell.Target.Null) throw new Exception("&>/dev/null");
        c = Parse("tool <<< '(+ 1 2)'");
        if (c.HereString != "(+ 1 2)\n") throw new Exception("<<<");
        count += 5;

        foreach (var line in new[] { "yes '' | tool", "a && b", "a; b", "echo $(date)", "echo `date`", "tool &", "(cd x)", "tool <<EOS", "echo $?", "x/*/y" })
            Shell(line);

        var bash = PosixShell.ForBash(@"C:\x\bin\tool 'arg'", [".EXE"]);
        if (bash != "C:/x/bin/tool 'arg'") throw new Exception($"ForBash: {bash}");
        return count + 1;
    }
}
