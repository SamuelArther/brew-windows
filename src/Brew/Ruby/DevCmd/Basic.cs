namespace Brew.Ruby.DevCmd;

[DevCommand("cat")]
sealed class Cat : DevCommand
{
    public override CommandParser Args() => new CommandParser("cat")
        .Description("Display the source of a <formula> or <cask>.")
        .Switch(new[] { "--formula", "--formulae" }, "Treat all named arguments as formulae.")
        .Switch(new[] { "--cask", "--casks" }, "Treat all named arguments as casks.")
        .Conflicts("--formula", "--cask")
        .NamedArgs(new[] { "formula", "cask" }, min: 1);

    public override void Run(ParsedArgs args)
    {
        var paths = new List<string>();
        foreach (var n in args.Named)
        {
            var p = DevCommands.FormulaPath(n);
            if (p == null) DevCommands.Ofail($"{DevCommands.ExpectedFormulaPath(n)}'s source doesn't exist on disk.");
            else paths.Add(p);
        }
        if (DevCommands.Failed)
        {
            Console.Error.WriteLine("The name may be wrong, or the tap hasn't been tapped. Instead try:");
            var treatAs = args.Switch("cask") ? "--cask " : args.Switch("formula") ? "--formula " : "";
            Console.Error.WriteLine($"  brew info --github {treatAs}{string.Join(" ", args.Named)}");
            return;
        }
        var bat = Environment.GetEnvironmentVariable("HOMEBREW_BAT") != null ? DevCommands.Which("bat") : null;
        if (bat != null)
        {
            DevCommands.Exec(bat, paths, DevCommands.Repository);
            return;
        }
        Console.Out.Flush();
        using var stdout = Console.OpenStandardOutput();
        foreach (var p in paths)
        {
            var bytes = File.ReadAllBytes(p);
            stdout.Write(bytes);
        }
    }
}

[DevCommand("formula")]
sealed class FormulaPathCmd : DevCommand
{
    public override CommandParser Args() => new CommandParser("formula")
        .Description("Display the path where <formula> is located.")
        .NamedArgs("formula", min: 1);

    public override void Run(ParsedArgs args)
    {
        foreach (var n in args.Named)
            if (DevCommands.FormulaPath(n) is { } p) Console.WriteLine(p);
    }
}

[DevCommand("ruby")]
sealed class RubyCmd : DevCommand
{
    public override CommandParser Args() => new CommandParser("ruby")
        .UsageBanner("`ruby` [<options>] (`-e` <text>|<file>)")
        .Description("Run a Ruby instance with Homebrew's libraries loaded. For example,\n`brew ruby -e \"puts :gcc.f.deps\"` or `brew ruby script.rb`.\n\nRun e.g. `brew ruby -- --version` to pass arbitrary arguments to `ruby`.")
        .Flag(new[] { "-r=" }, "Load a library using `require`.")
        .Flag(new[] { "-e=" }, "Execute the given text string as a script.")
        .NamedArgs("file");

    public override void Run(ParsedArgs args)
    {
        string code, file;
        if (args.Flag("e") is { } e) { code = e; file = "-e"; }
        else if (args.Named.Count > 0)
        {
            file = args.Named[0];
            if (file == "--version") { Console.WriteLine("ruby 3.4 (brew-windows mini-Ruby)"); return; }
            if (!File.Exists(file)) throw new BrewError($"No such file or directory -- {file} (LoadError)");
            code = File.ReadAllText(file);
        }
        else { code = Console.In.ReadToEnd(); file = "-"; }
        var it = Shell.NewInterp();
        var argv = new RArray(args.Named.Skip(args.Flag("e") != null ? 0 : 1).Select(x => (object)Interp.S(x)));
        it.ObjectClass.Consts["ARGV"] = argv;
        FormulaLoader.OnBigStack(() =>
        {
            try { it.Run(code, file); }
            catch (RubyError err) when (err.ClassName == "SystemExit")
            {
                if (err.Exc.GetIvar("@status") is long status && status != 0) DevCommands.Failed = true;
            }
            catch (RubyError err)
            {
                Console.Error.WriteLine($"{file}:{it.CurLine}: {err.Message} ({err.ClassName})");
                DevCommands.Failed = true;
            }
            catch (RubySyntaxError err)
            {
                Console.Error.WriteLine($"{file}: {err.Message} (SyntaxError)");
                DevCommands.Failed = true;
            }
            return 0;
        });
    }
}

[DevCommand("irb")]
sealed class Irb : DevCommand
{
    public override CommandParser Args() => new CommandParser("irb")
        .Description("Enter the interactive Homebrew Ruby shell.")
        .Switch(new[] { "--examples" }, "Show several examples.")
        .Switch(new[] { "--pry" }, null, hidden: true);

    public override void Run(ParsedArgs args)
    {
        if (args.Switch("examples"))
        {
            Console.Write("""
                'v8'.f # => instance of the v8 formula
                :hub.f.latest_version_installed?
                :lua.f.methods - 1.methods
                :mpd.f.recursive_dependencies.reject(&:installed?)

                'vlc'.c # => instance of the vlc cask
                :tsh.c.livecheck_defined?

                """);
            return;
        }
        Out.Ohai("Interactive Homebrew Shell", "Example commands available with: `brew irb --examples`");
        var it = Shell.NewInterp();
        int n = 1;
        var buffer = "";
        FormulaLoader.OnBigStack(() =>
        {
            while (true)
            {
                Console.Write(buffer.Length == 0 ? $"brew(main):{n:000}> " : $"brew(main):{n:000}* ");
                var line = Console.ReadLine();
                if (line == null) { Console.WriteLine(); break; }
                if (buffer.Length == 0 && line.Trim() is "exit" or "quit") break;
                buffer += line + "\n";
                n++;
                try
                {
                    var r = it.Run(buffer, "(irb)");
                    Console.WriteLine("=> " + it.Inspect(r));
                    buffer = "";
                }
                catch (RubySyntaxError e) when (e.Message.Contains("end of file") || e.Message.Contains("unterminated")) { }
                catch (RubySyntaxError e) { Console.WriteLine($"(irb):{n - 1}: {e.Message} (SyntaxError)"); buffer = ""; }
                catch (RubyError e) when (e.ClassName == "SystemExit") { break; }
                catch (RubyError e) { Console.WriteLine($"(irb):{n - 1}:in '<main>': {e.Message} ({e.ClassName})"); buffer = ""; }
            }
            return 0;
        });
    }
}

// Interpreter set up like Homebrew's global environment: `"jq".f` / `:jq.f` load formulae.
static class Shell
{
    public static Interp NewInterp()
    {
        var it = new Interp();
        Native f = (i, self, a, b) => FormulaObject(i, DevCommands.LoadFormula(i.StrOrSym(self)));
        it.Def(it.StringClass, "f", f);
        it.Def(it.SymbolClass, "f", f);
        return it;
    }

    static RClass ProxyClass(Interp it)
    {
        if (it.ConstGet(it.ObjectClass, "Formula") is RClass c) return c;
        var F = it.DefineClass("Formula");
        FormulaSpec S(object self) => (FormulaSpec)((RObject)self).Host;
        object Str(string s) => s == null ? null : Interp.S(s);
        it.Def(F, "name full_name", (i, self, a, b) => Str(S(self).Name));
        it.Def(F, "version", (i, self, a, b) => Str(S(self).Version));
        it.Def(F, "pkg_version", (i, self, a, b) => Str(S(self).PkgVersion));
        it.Def(F, "desc", (i, self, a, b) => Str(S(self).Desc));
        it.Def(F, "homepage", (i, self, a, b) => Str(S(self).Homepage));
        it.Def(F, "license", (i, self, a, b) => Str(S(self).License));
        it.Def(F, "path", (i, self, a, b) => i.NewPath(S(self).Path));
        it.Def(F, "stable_url url", (i, self, a, b) => Str(S(self).Url));
        it.Def(F, "deps", (i, self, a, b) => new RArray(S(self).Dependencies.Select(d => (object)Interp.S(d.Name))));
        it.Def(F, "keg_only?", (i, self, a, b) => S(self).KegOnly);
        it.Def(F, "deprecated?", (i, self, a, b) => S(self).Deprecated);
        it.Def(F, "disabled?", (i, self, a, b) => S(self).Disabled);
        it.Def(F, "livecheck_defined?", (i, self, a, b) => S(self).HasLivecheck);
        it.Def(F, "latest_version_installed? any_version_installed?", (i, self, a, b) =>
            DevCommands.InstalledKegs(S(self).Name).Any(k => Path.GetFileName(k) == S(self).PkgVersion));
        it.Def(F, "rack", (i, self, a, b) => i.NewPath(Path.Combine(DevCommands.Cellar, S(self).Name)));
        it.Def(F, "prefix", (i, self, a, b) => i.NewPath(Path.Combine(DevCommands.Cellar, S(self).Name, S(self).PkgVersion)));
        it.Def(F, "opt_prefix", (i, self, a, b) => i.NewPath(Path.Combine(DevCommands.Prefix, "opt", S(self).Name)));
        it.Def(F, "to_s", (i, self, a, b) => Str(S(self).Name));
        it.Def(F, "inspect", (i, self, a, b) => Str($"#<Formulary::FormulaNamespace{S(self).ClassName}: {S(self).Path}>"));
        return F;
    }

    static RObject FormulaObject(Interp it, FormulaSpec spec) => new() { Class = ProxyClass(it), Host = spec };
}
