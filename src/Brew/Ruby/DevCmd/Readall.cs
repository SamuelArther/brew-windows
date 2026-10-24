using System.Runtime.InteropServices;

namespace Brew.Ruby.DevCmd;

[DevCommand("readall")]
sealed class ReadallCmd : DevCommand
{
    public override CommandParser Args() => new CommandParser("readall")
        .Description("""
            Import all items from the specified <tap>, or from all installed taps if none is provided.
            This can be useful for debugging issues across all items when making
            significant changes to `formula.rb`, testing the performance of loading
            all items or checking if any current formulae/casks have Ruby issues.
            """)
        .Flag(new[] { "--os=" }, "Read using the given operating system. (Pass `all` to simulate all operating systems.)")
        .Flag(new[] { "--arch=" }, "Read using the given CPU architecture. (Pass `all` to simulate all architectures.)")
        .Switch(new[] { "--aliases" }, "Verify any alias symlinks in each tap.")
        .Switch(new[] { "--syntax" }, "Syntax-check all of Homebrew's Ruby files (if no <tap> is passed).")
        .Switch(new[] { "--eval-all" }, null, hidden: true)
        .Switch(new[] { "--no-simulate" }, "Don't simulate other system configurations when checking formulae and casks.")
        .NamedArgs("tap");

    public override void Run(ParsedArgs args)
    {
        if (args.Switch("eval-all"))
            throw new BrewError("Calling `brew readall --eval-all` is disabled! Use the default trusted-tap behaviour instead.");

        if (args.Switch("syntax") && args.Named.Count == 0)
            if (!ValidRubySyntax(DevCommands.AllFormulaFiles())) DevCommands.Failed = true;

        // Tap.installed, or args.named.to_installed_taps.
        var taps = new List<string>();
        if (args.Named.Count == 0)
        {
            if (Directory.Exists(DevTap.Root))
                taps.AddRange(Directory.GetDirectories(DevTap.Root).OrderBy(DevTap.FromPath, StringComparer.Ordinal));
        }
        else
            foreach (var n in args.Named)
            {
                var name = DevTap.Normalize(n);
                if (!Directory.Exists(DevTap.Path(name))) throw new BrewError(DevTap.TapUnavailableMessage(name));
                taps.Add(DevTap.Path(name));
            }

        // Homebrew's bottle tags are "<arch>_<os>"; Windows only has its own OS to "simulate".
        var arch = RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "arm64" : "x86_64";
        var archs = args.Flag("arch") is { } a ? a == "all" ? new[] { "x86_64", "arm64" } : new[] { a == "intel" ? "x86_64" : a == "arm" ? "arm64" : a } : new[] { arch };
        var oses = args.Flag("os") is { } o ? o == "all" ? new[] { "windows" } : new[] { o } : new[] { "windows" };
        var combos = args.Switch("no-simulate") ? new[] { (string)null } : oses.SelectMany(os => archs.Select(ar => $"{ar}_{os}")).ToArray();

        foreach (var tapDir in taps.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            bool success = true;
            var files = DevTap.FormulaFiles(tapDir).ToList();
            if (args.Switch("aliases") && !ValidAliases(tapDir, files)) success = false;
            var valid = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var tag in combos)
                foreach (var file in files)
                {
                    if (valid.Contains(file)) continue;
                    try
                    {
                        FormulaLoader.LoadSpec(file);
                        valid.Add(file);
                    }
                    catch (Exception e) when (e is not OutOfMemoryException)
                    {
                        Out.Onoe($"Invalid formula ({tag}): {file}");
                        Console.Error.WriteLine(e is InvalidOperationException ? e.Message : $"{e.Message} ({e.GetType().Name})");
                        success = false;
                    }
                }
            if (!success) DevCommands.Failed = true;
        }
    }

    static bool ValidAliases(string tapDir, IEnumerable<string> formulaFiles)
    {
        var aliasDir = Path.Combine(tapDir, "Aliases");
        if (!Directory.Exists(aliasDir)) return true;
        var formulae = new HashSet<string>(formulaFiles.Select(Path.GetFileName));
        bool ok = true;
        foreach (var f in Directory.EnumerateFileSystemEntries(aliasDir).Order(StringComparer.Ordinal))
        {
            var info = new FileInfo(f);
            if (info.LinkTarget == null)
            {
                Out.Onoe($"Non-symlink alias: {f}");
                ok = false;
            }
            else if (!File.Exists(f))
            {
                Out.Onoe($"Non-file alias: {f}");
                ok = false;
            }
            if (formulae.Contains(Path.GetFileName(f) + ".rb"))
            {
                Out.Onoe($"Formula duplicating alias: {f}");
                ok = false;
            }
        }
        return ok;
    }

    // Readall.valid_ruby_syntax?: parse every file, printing errors to stderr.
    static bool ValidRubySyntax(IEnumerable<string> files)
    {
        bool ok = true;
        foreach (var file in files)
        {
            try { FormulaLoader.OnBigStack(() => Parser.Parse(File.ReadAllText(file))); }
            catch (RubySyntaxError e)
            {
                Console.Error.WriteLine($"{file}: {e.Message}");
                ok = false;
            }
        }
        return ok;
    }
}
