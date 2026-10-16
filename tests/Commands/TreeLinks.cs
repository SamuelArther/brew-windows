using System.Text.Json;
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
            // A keg's directory symlink, linked into the prefix: another keg's real directory there is a
            // conflict, never linked through the link (into the first keg).
            var dirLinker = Path.Combine(paths.Rack("dirlink"), "1");
            Directory.CreateDirectory(Path.Combine(dirLinker, "libexec", "data")); Directory.CreateDirectory(Path.Combine(dirLinker, "share"));
            Directory.CreateSymbolicLink(Path.Combine(dirLinker, "share", "data"), Path.Combine(dirLinker, "libexec", "data"));
            var dirLinks = linker.Link(dirLinker);
            File.WriteAllText(Path.Combine(dirLinker, "INSTALL_RECEIPT.json"), JsonSerializer.Serialize(new Receipt("dirlink", "1", "", "", dirLinks)));
            var intruder = Path.Combine(paths.Rack("intruder"), "1");
            Directory.CreateDirectory(Path.Combine(intruder, "share", "data")); File.WriteAllText(Path.Combine(intruder, "share", "data", "extra.txt"), "z");
            try { linker.Link(intruder); throw new Exception("Linked through another keg's directory link"); }
            catch (IOException ex) when (ex.Message.Contains("is a symlink belonging to dirlink")) { }
            if (Directory.EnumerateFileSystemEntries(Path.Combine(dirLinker, "libexec", "data")).Any()) throw new Exception("Link written inside another keg");

            // A directory link to a real directory of a keg (as Homebrew links them) is "unbrewed" into a
            // real directory with links to that keg's files, so the second keg fits; the receipt follows.
            var whole = Path.Combine(paths.Rack("whole"), "1"); var wholeDoc = Path.Combine(whole, "share", "doc");
            Directory.CreateDirectory(wholeDoc); File.WriteAllText(Path.Combine(wholeDoc, "whole.txt"), "w");
            var docLink = Path.Combine(paths.Prefix, "share", "doc");
            Directory.CreateDirectory(Path.GetDirectoryName(docLink)); Directory.CreateSymbolicLink(docLink, wholeDoc);
            File.WriteAllText(Path.Combine(whole, "INSTALL_RECEIPT.json"), JsonSerializer.Serialize(new Receipt("whole", "1", "", "", [new BinLink(docLink, wholeDoc, false)])));
            var joiner = Path.Combine(paths.Rack("joiner"), "1");
            Directory.CreateDirectory(Path.Combine(joiner, "share", "doc")); File.WriteAllText(Path.Combine(joiner, "share", "doc", "joiner.txt"), "j");
            linker.Link(joiner);
            if (new FileInfo(docLink).LinkTarget != null) throw new Exception("Directory link not unbrewed");
            if (new FileInfo(Path.Combine(docLink, "whole.txt")).LinkTarget != Path.Combine(wholeDoc, "whole.txt")) throw new Exception("Unbrewed keg's file not relinked");
            if (new FileInfo(Path.Combine(docLink, "joiner.txt")).LinkTarget != Path.Combine(joiner, "share", "doc", "joiner.txt")) throw new Exception("Second keg not linked");
            if (File.Exists(Path.Combine(wholeDoc, "joiner.txt"))) throw new Exception("Linked into the unbrewed keg");
            var wholeLinks = new InstallationStore(paths).Latest("whole").Receipt.Links;
            if (wholeLinks.Count != 1 || wholeLinks[0].Path != Path.Combine(docLink, "whole.txt")) throw new Exception("Unbrewed keg's receipt not updated");

            // A link whose target is gone is replaced, as Homebrew's make_relative_symlink does.
            var dangling = Path.Combine(paths.Prefix, "include", "gone.h");
            Directory.CreateDirectory(Path.GetDirectoryName(dangling)); File.CreateSymbolicLink(dangling, Path.Combine(root, "tree-links-missing", "gone.h"));
            var fresh = Path.Combine(paths.Rack("fresh"), "1");
            Directory.CreateDirectory(Path.Combine(fresh, "include")); File.WriteAllText(Path.Combine(fresh, "include", "gone.h"), "g");
            linker.Link(fresh);
            if (new FileInfo(dangling).LinkTarget != Path.Combine(fresh, "include", "gone.h")) throw new Exception("Broken link not replaced");
            return 12;
        }
        finally { Environment.SetEnvironmentVariable("BREW_PREFIX", saved); }
    }
}
