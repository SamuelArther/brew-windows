namespace Brew.Ruby.DevCmd;

[DevCommand("unpack")]
sealed class UnpackCmd : DevCommand
{
    public override CommandParser Args() => new CommandParser("unpack")
        .Description("""
            Unpack the files for the <formula> or <cask> into subdirectories of the current
            working directory.
            """)
        .Flag(new[] { "--destdir=" }, "Create subdirectories in the directory named by <path> instead.")
        .Switch(new[] { "--patch" }, "Patches for <formula> will be applied to the unpacked source.")
        .Switch(new[] { "-g", "--git" }, "Initialise a Git repository in the unpacked source. This is useful for creating patches for the software.")
        .Switch(new[] { "-f", "--force" }, "Overwrite the destination directory if it already exists.")
        .Switch(new[] { "--formula", "--formulae" }, "Treat all named arguments as formulae.")
        .Switch(new[] { "--cask", "--casks" }, "Treat all named arguments as casks.")
        .Conflicts("--git", "--patch")
        .Conflicts("--formula", "--cask")
        .Conflicts("--cask", "--patch")
        .Conflicts("--cask", "--git")
        .NamedArgs(new[] { "formula", "cask" }, min: 1);

    public override void Run(ParsedArgs args)
    {
        var formulae = new List<FormulaSpec>();
        foreach (var n in args.Named)
        {
            if (args.Switch("cask")) throw new BrewError($"Cask '{n}' is unavailable: No Cask with this name exists.");
            if (DevCommands.FormulaPath(n) == null)
                throw new BrewError(DevCommands.UnavailableMessage(n));
            try { formulae.Add(DevCommands.LoadFormula(n)); }
            catch (Exception e) when (e is not (BrewError or OutOfMemoryException)) { throw new BrewError(e.Message); }
        }

        string unpackDir;
        if (args.Flag("destdir") is { } dir)
        {
            unpackDir = Path.GetFullPath(dir);
            Directory.CreateDirectory(unpackDir);
        }
        else unpackDir = Environment.CurrentDirectory;
        if (!Writable(unpackDir)) throw new BrewError($"Cannot write to {unpackDir}");

        foreach (var f in formulae) UnpackFormula(f, unpackDir, args);
    }

    static bool Writable(string dir)
    {
        try
        {
            var probe = Path.Combine(dir, ".brew-unpack-" + Guid.NewGuid().ToString("N"));
            File.WriteAllText(probe, "");
            File.Delete(probe);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return false; }
    }

    static void UnpackFormula(FormulaSpec f, string unpackDir, ParsedArgs args)
    {
        var stageDir = Path.Combine(unpackDir, $"{f.Name}-{f.Version}");
        if (Directory.Exists(stageDir) || File.Exists(stageDir))
        {
            if (!args.Switch("force")) throw new BrewError($"Destination {stageDir} already exists!");
            // A previous `--git` unpack holds a git repository, whose objects are read-only.
            if (Directory.Exists(stageDir)) PackageManager.DeleteTree(stageDir); else File.Delete(stageDir);
        }

        Out.Oh1($"Unpacking {Formatter.Identifier(f.Name)} to: {stageDir}");

        // formula.brew: fetch, then stage into a temporary build directory and copy it over.
        var cached = new Downloader(DevCommands.Paths.Cache).FetchAsync(f.Url, f.Sha256).GetAwaiter().GetResult();
        var filename = Path.GetFileName(new Uri(f.Url).LocalPath);
        var tmp = Path.Combine(Path.GetTempPath(), $"{f.Name}-{Guid.NewGuid().ToString("N")[..8]}");
        try
        {
            // `--patch`: FormulaLoader ignores `patch` declarations (none of this tap's formulae use them).
            var staged = Archive.StageAsync(cached, filename, tmp).GetAwaiter().GetResult();
            CopyTree(staged, stageDir);
        }
        finally
        {
            try { PackageManager.DeleteTree(tmp); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        }

        if (!args.Switch("git")) return;
        Out.Ohai("Setting up Git repository");
        var git = DevCommands.Which("git") ?? throw new BrewError("Git must be installed to use `--git`.");
        DevCommands.Exec(git, new[] { "init", "-q" }, stageDir);
        DevCommands.Exec(git, new[] { "add", "-A" }, stageDir);
        DevCommands.Exec(git, new[] { "commit", "-q", "-m", "brew-unpack" }, stageDir);
    }

    static void CopyTree(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var d in Directory.GetDirectories(from, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(to, Path.GetRelativePath(from, d)));
        foreach (var file in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
        {
            var dest = Path.Combine(to, Path.GetRelativePath(from, file));
            File.Copy(file, dest, true);
            File.SetLastWriteTimeUtc(dest, File.GetLastWriteTimeUtc(file));
        }
    }
}
