using Brew;

// etc/include/lib/share linking: per-file symlinks, Homebrew's skip list, conflicts, empty-dir cleanup,
// and receipts that point outside the linked trees are refused.
static class TreeLinks
{
    public static int Run(string root)
    {
        var saved = Environment.GetEnvironmentVariable("BREW_PREFIX");
        try
        {
            Environment.SetEnvironmentVariable("BREW_PREFIX", Path.Combine(root, "tree-links"));
            var paths = new Paths(); var linker = new Linker(paths);
            var keg = Path.Combine(paths.Rack("tree"), "1");
            void Put(string relative, string text = "x")
            {
                var file = Path.Combine(keg, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(file)); File.WriteAllText(file, text);
            }
            Put("include/tree.h"); Put("lib/pkgconfig/tree.pc"); Put("lib/charset.alias");
            Put("share/man/man1/tree.1"); Put("share/info/dir"); Put("share/zsh/site-functions/_tree"); Put("etc/tree.conf");
            var links = linker.Link(keg);
            foreach (var expected in new[] { "include/tree.h", "lib/pkgconfig/tree.pc", "share/man/man1/tree.1", "share/zsh/site-functions/_tree", "etc/tree.conf" })
                if (new FileInfo(Path.Combine(paths.Prefix, expected)).LinkTarget != Path.Combine(keg, expected.Replace('/', '\\')))
                    throw new Exception($"Not linked: {expected}");
            foreach (var skipped in new[] { "lib/charset.alias", "share/info/dir" })
                if (File.Exists(Path.Combine(paths.Prefix, skipped))) throw new Exception($"Linked a skipped file: {skipped}");

            var other = Path.Combine(paths.Rack("other"), "1");
            Directory.CreateDirectory(Path.Combine(other, "include")); File.WriteAllText(Path.Combine(other, "include", "tree.h"), "y");
            try { linker.Link(other); throw new Exception("Header conflict accepted"); }
            catch (IOException ex) when (ex.Message.Contains("Could not symlink include\\tree.h")) { }

            linker.Unlink(links);
            if (Directory.Exists(Path.Combine(paths.Prefix, "share", "man"))) throw new Exception("Empty link directories left behind");
            if (!Directory.Exists(Path.Combine(paths.Prefix, "share"))) throw new Exception("Top-level share directory removed");

            var outside = Path.Combine(root, "tree-links-outside.txt");
            File.WriteAllText(outside, "keep");
            try { linker.Unlink([new BinLink(outside, outside, false)]); throw new Exception("Receipt outside prefix accepted"); }
            catch (IOException ex) when (ex.Message.Contains("Invalid link path")) { }
            if (!File.Exists(outside)) throw new Exception("Deleted a file outside the prefix");
            return 5;
        }
        finally { Environment.SetEnvironmentVariable("BREW_PREFIX", saved); }
    }
}
