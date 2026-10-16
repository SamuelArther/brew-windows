using System.IO.Compression;
using System.Text;
using Brew;

// Bottle relocation: text files (native, forward-slash and JSON-escaped prefixes) and
// pip/distlib launchers (exe + "#!<python>\n" + zip) round-trip to a different prefix.
static class BottleRelocation
{
    public static async Task<int> Run(string root)
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

        // Relocation edits bytes: a Latin-1 file (0xE9 is not UTF-8) keeps everything but the prefix.
        var latin1 = Path.Combine(dir, "latin1.pc");
        byte[] Latin1(string value) => Encoding.Latin1.GetBytes(value);
        File.WriteAllBytes(latin1, Latin1("# café\nprefix=" + oldPrefix + "\n"));
        Bottles.Relocate(latin1, oldPrefix, true);
        Bottles.Relocate(latin1, newPrefix, false);
        if (!File.ReadAllBytes(latin1).SequenceEqual(Latin1("# café\nprefix=" + newPrefix + "\n")))
            throw new Exception("Relocation re-encoded a Latin-1 file: " + Convert.ToHexString(File.ReadAllBytes(latin1)));

        // `brew bottle` of a keg with an absolute link into itself (msys2_install's libexec/bin used to be
        // one) and a link cycle: the installed keg is never rewritten through the staged copy's link, and
        // the poured keg's link stays inside the new keg.
        var savedPrefix = Environment.GetEnvironmentVariable("BREW_PREFIX");
        try
        {
            var buildPrefix = Path.Combine(dir, "build-prefix");
            Environment.SetEnvironmentVariable("BREW_PREFIX", buildPrefix + "\\");
            var paths = new Paths();
            if (paths.Prefix != buildPrefix) throw new Exception("Prefix keeps its trailing separator: " + paths.Prefix);
            var keg = Path.Combine(buildPrefix, "Cellar", "fixture", "1.0");
            var usrBin = Path.Combine(keg, "libexec", "usr", "bin");
            Directory.CreateDirectory(usrBin); Directory.CreateDirectory(Path.Combine(keg, "share"));
            var script = Path.Combine(usrBin, "tool.sh");
            File.WriteAllText(script, "#!/bin/sh\necho " + buildPrefix + "\n");
            Directory.CreateSymbolicLink(Path.Combine(keg, "libexec", "bin"), usrBin);
            Directory.CreateSymbolicLink(Path.Combine(keg, "share", "loop"), "..");
            var bottle = Path.Combine(dir, "fixture--1.0.x86_64_windows.bottle.tar.gz");
            var cellar = await Bottles.CreateAsync(paths, keg, bottle);
            if (File.ReadAllText(script) != "#!/bin/sh\necho " + buildPrefix + "\n") throw new Exception("Bottling rewrote the installed keg: " + File.ReadAllText(script));

            var pourPrefix = Path.Combine(dir, "pour prefix");
            Environment.SetEnvironmentVariable("BREW_PREFIX", pourPrefix);
            var poured = Path.Combine(pourPrefix, "Cellar", "fixture", "1.0");
            await Bottles.PourAsync(new Paths(), bottle, poured, cellar);
            var link = new DirectoryInfo(Path.Combine(poured, "libexec", "bin")).LinkTarget;
            if (link == null || Path.IsPathRooted(link)) throw new Exception("Bottled keg link is not relative: " + link);
            if (File.ReadAllText(Path.Combine(poured, "libexec", "bin", "tool.sh")) != "#!/bin/sh\necho " + pourPrefix + "\n")
                throw new Exception("Poured script not relocated through its keg");
        }
        finally { Environment.SetEnvironmentVariable("BREW_PREFIX", savedPrefix); }
        return 9;
    }

    static string JsonEscape(string value) => System.Text.Json.JsonSerializer.Serialize(value)[1..^1];
}
