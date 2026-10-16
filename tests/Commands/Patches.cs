using Brew;

// Patcher: exact, offset and fuzzy hunks, CRLF files, created files, and escaping targets.
static class Patches
{
    public static int Run(string root)
    {
        var dir = Path.Combine(root, "patches");
        Directory.CreateDirectory(Path.Combine(dir, "src"));
        var lines = Enumerable.Range(1, 30).Select(n => $"line {n}").ToList();
        File.WriteAllText(Path.Combine(dir, "src", "a.c"), string.Join("\n", lines) + "\n");
        // CRLF file whose content drifted by two lines from what the patch expects.
        File.WriteAllText(Path.Combine(dir, "src", "b.c"), "extra 1\r\nextra 2\r\none\r\ntwo\r\nthree\r\nfour\r\n");
        var diff = """
            --- a/src/a.c	2024-01-01
            +++ b/src/a.c	2024-01-02
            @@ -9,3 +9,3 @@
             line 9
            -line 10
            +line ten
             line 11
            @@ -20,3 +20,4 @@
             CHANGED-CONTEXT
             line 21
            +inserted
             line 22
            --- a/src/b.c
            +++ b/src/b.c
            @@ -1,3 +1,3 @@
             one
            -two
            +TWO
             three
            --- /dev/null
            +++ b/src/new.txt
            @@ -0,0 +1,2 @@
            +hello
            +world
            """;
        Patcher.Apply(diff, 1, dir);
        var a = File.ReadAllText(Path.Combine(dir, "src", "a.c"));
        if (!a.Contains("line 9\nline ten\nline 11\n") || !a.Contains("line 21\ninserted\nline 22\n")) throw new Exception("Exact/fuzzy hunk not applied:\n" + a);
        if (File.ReadAllText(Path.Combine(dir, "src", "b.c")) != "extra 1\r\nextra 2\r\none\r\nTWO\r\nthree\r\nfour\r\n") throw new Exception("Offset hunk or CRLF handling wrong");
        if (File.ReadAllText(Path.Combine(dir, "src", "new.txt")) != "hello\nworld\n") throw new Exception("New file not created");

        try { Patcher.Apply("--- a/../evil\n+++ b/../evil\n@@ -0,0 +1 @@\n+x\n", 1, dir); throw new Exception("Escaping patch accepted"); }
        catch (InvalidOperationException ex) when (ex.Message.Contains("escapes the staged source tree")) { }
        if (File.Exists(Path.Combine(root, "evil"))) throw new Exception("Escaping patch wrote outside the tree");
        try { Patcher.Apply("--- a/src/a.c\n+++ b/src/a.c\n@@ -1,2 +1,2 @@\n-nope\n-nada\n+x\n+y\n", 1, dir); throw new Exception("Mismatched hunk accepted"); }
        catch (InvalidOperationException ex) when (ex.Message.Contains("FAILED")) { }
        return 6;
    }
}
