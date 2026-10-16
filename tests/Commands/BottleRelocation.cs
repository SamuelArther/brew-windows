using System.IO.Compression;
using System.Text;
using Brew;

// Bottle relocation: text files (native, forward-slash and JSON-escaped prefixes) and
// pip/distlib launchers (exe + "#!<python>\n" + zip) round-trip to a different prefix.
static class BottleRelocation
{
    public static int Run(string root)
    {
        var dir = Path.Combine(root, "relocation");
        Directory.CreateDirectory(dir);
        var oldPrefix = @"C:\Builder\Homebrew";
        var newPrefix = @"D:\Somewhere Else\Homebrew";

        string Content(string prefix)
        {
            var native = Path.Combine(prefix, "lib");
            var posix = prefix.Replace(Path.DirectorySeparatorChar, '/') + "/share";
            var escaped = JsonEscape(Path.Combine(prefix, "etc"));
            return "a=" + native + "\nb=" + posix + "\nc=\"" + escaped + "\"\n";
        }
        var text = Path.Combine(dir, "config.txt");
        File.WriteAllText(text, Content(oldPrefix));
        if (!Bottles.Relocate(text, oldPrefix, true) || File.ReadAllText(text).Contains("Builder")) throw new Exception("Text prefix not replaced");
        Bottles.Relocate(text, newPrefix, false);
        if (File.ReadAllText(text) != Content(newPrefix)) throw new Exception("Text relocation wrong:\n" + File.ReadAllText(text));

        using var zip = new MemoryStream();
        using (var archive = new ZipArchive(zip, ZipArchiveMode.Create, true))
        using (var writer = new StreamWriter(archive.CreateEntry("__main__.py").Open()))
            writer.Write("print('hi')");
        var payload = zip.ToArray();
        var stub = new byte[4096];
        stub[0] = (byte)'M';
        stub[1] = (byte)'Z';
        string Shebang(string prefix) => "#!" + Path.Combine(prefix, "Cellar", "tool", "1", "libexec", "Scripts", "python.exe") + "\n";
        var launcher = Path.Combine(dir, "tool.exe");
        File.WriteAllBytes(launcher, [.. stub, .. Encoding.UTF8.GetBytes(Shebang(oldPrefix)), .. payload]);
        if (!Bottles.RelocateLauncher(launcher, oldPrefix, true)) throw new Exception("Launcher shebang not found");
        Bottles.RelocateLauncher(launcher, newPrefix, false);
        var bytes = File.ReadAllBytes(launcher);
        var shebang = Encoding.UTF8.GetBytes(Shebang(newPrefix));
        if (!bytes.AsSpan(4096, shebang.Length).SequenceEqual(shebang)) throw new Exception("Launcher shebang wrong");
        if (!bytes.AsSpan(4096 + shebang.Length).SequenceEqual(payload)) throw new Exception("Launcher zip payload changed");
        using (var reread = new ZipArchive(new MemoryStream(bytes[(4096 + shebang.Length)..])))
            if (reread.GetEntry("__main__.py") == null) throw new Exception("Launcher zip unreadable");
        return 4;
    }

    static string JsonEscape(string value) => System.Text.Json.JsonSerializer.Serialize(value)[1..^1];
}
