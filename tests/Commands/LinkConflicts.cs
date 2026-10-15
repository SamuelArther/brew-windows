using System.Text.Json;
using Brew;

static class LinkConflicts
{
    public static int Run(string root)
    {
        var saved = Environment.GetEnvironmentVariable("BREW_PREFIX");
        try
        {
            Environment.SetEnvironmentVariable("BREW_PREFIX", Path.Combine(root, "link-conflicts"));
            var paths = new Paths(); var linker = new Linker(paths);
            var first = Path.Combine(paths.Rack("first"), "1"); var second = Path.Combine(paths.Rack("second"), "1");
            foreach (var keg in new[] { first, second })
            { Directory.CreateDirectory(Path.Combine(keg, "bin")); File.WriteAllText(Path.Combine(keg, "bin", "shared.cmd"), "@echo fixture"); }
            var links = linker.Link(first);
            File.WriteAllText(Path.Combine(first, "INSTALL_RECEIPT.json"), JsonSerializer.Serialize(new Receipt("first", "1", "", "", links)));
            File.WriteAllText(Path.Combine(second, "bin", "a-before.cmd"), "@echo partial");
            try { linker.Link(second); throw new Exception("Link collision accepted"); }
            catch (IOException ex) when (ex.Message.Contains("is a symlink belonging to first. You can unlink it:\n  brew unlink first")) { }
            if (File.Exists(Path.Combine(paths.Bin, "a-before.cmd"))) throw new Exception("Link conflict left partial links");
            if (new FileInfo(Path.Combine(paths.Bin, "shared.cmd")).LinkTarget != Path.Combine(first, "bin", "shared.cmd")) throw new Exception("Link conflict replaced original owner");
            linker.Unlink(links); File.WriteAllText(Path.Combine(paths.Bin, "shared.cmd"), "user file");
            try { linker.Link(second); throw new Exception("User file overwritten"); }
            catch (IOException ex) when (ex.Message.Contains("already exists. You may want to remove it:")) { }
            if (File.ReadAllText(Path.Combine(paths.Bin, "shared.cmd")) != "user file") throw new Exception("Link conflict damaged user data");
            return 4;
        }
        finally { Environment.SetEnvironmentVariable("BREW_PREFIX", saved); }
    }
}
