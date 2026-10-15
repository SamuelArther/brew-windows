namespace Brew.Ruby.DevCmd;

[DevCommand("options")]
sealed class OptionsCmd : DevCommand
{
    public override CommandParser Args() => new CommandParser("options")
        .Description("Show install options specific to <formula>.")
        .Switch(new[] { "--compact" }, "Show all options on a single line separated by spaces.")
        .Switch(new[] { "--installed" }, "Show options for formulae that are currently installed.")
        .Switch(new[] { "--eval-all" }, null, hidden: true)
        .Flag(new[] { "--command=" }, null, hidden: true)
        .Conflicts("--command", "--installed", "--eval-all")
        .NamedArgs("formula");

    public override void Run(ParsedArgs args)
    {
        IEnumerable<FormulaSpec> formulae;
        if (args.Named.Count > 0) formulae = args.Named.Select(DevCommands.LoadFormula).ToList();
        else
        {
            var all = DevCommands.AllFormulaFiles().Select(FormulaLoader.LoadSpec);
            if (args.Switch("installed")) all = all.Where(f => DevCommands.InstalledKegs(f.Name).Count > 0);
            formulae = all.OrderBy(f => f.Name, StringComparer.Ordinal).ToList();
        }
        var list = formulae.ToList();
        foreach (var f in list)
        {
            if (f.Options.Count == 0) continue;
            var sorted = f.Options.OrderBy(o => "--" + o.Name, StringComparer.Ordinal).ToList();
            if (args.Switch("compact"))
            {
                Console.WriteLine(string.Join(" ", sorted.Select(o => "--" + o.Name)));
                continue;
            }
            if (list.Count > 1) Console.WriteLine(f.Name);
            foreach (var o in sorted) Console.WriteLine($"--{o.Name}\n\t{o.Description}");
            if (f.HeadUrl != null) Console.WriteLine("--HEAD\n\tInstall HEAD version");
            Console.WriteLine();
        }
    }
}
