namespace Brew.Ruby.DevCmd;

[DevCommand("sh")]
sealed class ShCmd : DevCommand
{
    public override CommandParser Args() => new CommandParser("sh")
        .Description("""
            Enter an interactive shell for Homebrew's build environment. Use years-battle-hardened
            build logic to help your `./configure && make && make install`
            and even your `gem install` succeed. Especially handy if you run Homebrew
            in an Xcode-only configuration since it adds tools like `make` to your `$PATH`
            which build systems would not find otherwise.

            With `--ruby`, enter an interactive shell for Homebrew's Ruby environment.
            This sets up the correct Ruby paths, `$GEM_HOME` and bundle
            configuration used by Homebrew's development tools.
            The environment includes gems from the installed groups,
            making tools like RuboCop, Sorbet and RSpec available via `bundle exec`.
            """)
        .Switch(new[] { "-r", "--ruby" }, "Set up Homebrew's Ruby environment.")
        .Flag(new[] { "--env=" }, "Use the standard `$PATH` instead of superenv's when `std` is passed.")
        .Flag(new[] { "-c=", "--cmd=" }, "Execute commands in a non-interactive shell.")
        .Conflicts("--ruby", "--env=")
        .NamedArgs("file", max: 1);

    public override void Run(ParsedArgs args)
    {
        var env = new Dictionary<string, string>();
        string prompt, notice;
        bool hints = Environment.GetEnvironmentVariable("HOMEBREW_NO_ENV_HINTS") == null;
        if (args.Switch("ruby"))
        {
            SetupCommonEnvironment(env, superenv: true);
            prompt = "brew ruby";
            notice = hints ? """
                Your shell has been configured to use Homebrew's Ruby environment.
                This includes the correct Ruby version, GEM_HOME, and bundle configuration.
                Tools like RuboCop, Sorbet, and RSpec are available via `bundle exec`.
                Hide these hints with `HOMEBREW_NO_ENV_HINTS=1` (see `man brew`).
                When done, type `exit`.

                """ : null;
        }
        else
        {
            var envArg = args.Flag("env");
            SetupCommonEnvironment(env, superenv: envArg != "std");
            if (args.Verbose) env["VERBOSE"] = "1";
            prompt = "brew";
            notice = hints ? """
                Your shell has been configured to use Homebrew's build environment;
                this should help you build stuff. Notably though, the system versions of
                gem and pip will ignore our configuration and insist on using the
                environment they were built under (mostly). Sadly, scons will also
                ignore our configuration.
                Hide these hints with `HOMEBREW_NO_ENV_HINTS=1` (see `man brew`).
                When done, type `exit`.

                """ : null;
        }

        var shell = PreferredShell();
        bool ps = IsPowerShell(shell);
        if (!string.IsNullOrEmpty(args.Flag("cmd")))
        {
            var cmd = args.Flag("cmd");
            var argv = ps ? new[] { "-NoProfile", "-Command", cmd } : new[] { "/d", "/c", cmd };
            SafeSystem(shell, argv, env);
        }
        else if (args.Named.Count > 0)
        {
            var file = args.Named[0];
            var argv = ps ? new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", file } : new[] { "/d", "/c", file };
            SafeSystem(shell, argv, env);
        }
        else
        {
            if (!string.IsNullOrEmpty(notice)) Console.Write(notice);
            Console.Out.Flush();
            string[] argv;
            if (ps)
                argv = new[]
                {
                    "-NoExit", "-NoLogo", "-Command",
                    "function global:prompt { $e = [char]27; \"$e[1;32m" + prompt + " $e[1;31m$($PWD.Path) $e[1;34m`$$e[0m \" }",
                };
            else
            {
                env["PROMPT"] = $"$E[1;32m{prompt} $E[1;31m$P $E[1;34m$$$E[0m ";
                argv = new[] { "/k" };
            }
            DevCommands.Exec(shell, argv, null, env);
        }
    }

    static void SafeSystem(string shell, string[] argv, Dictionary<string, string> env)
    {
        int code = DevCommands.Exec(shell, argv, null, env);
        if (code != 0)
            throw new BrewError($"Failure while executing; `{shell} {string.Join(" ", argv)}` exited with {code}.");
    }

    static bool IsPowerShell(string shell) =>
        Path.GetFileNameWithoutExtension(shell).ToLowerInvariant() is "powershell" or "pwsh";

    // Utils::Shell.preferred_path: $SHELL when it names a Windows shell we can drive, else %COMSPEC%.
    static string PreferredShell()
    {
        var sh = Environment.GetEnvironmentVariable("SHELL");
        if (!string.IsNullOrEmpty(sh) && IsPowerShell(sh))
            return File.Exists(sh) ? sh : DevCommands.Which(Path.GetFileName(sh)) ?? sh;
        var comspec = Environment.GetEnvironmentVariable("ComSpec");
        return !string.IsNullOrEmpty(comspec) ? comspec : Path.Combine(Environment.SystemDirectory, "cmd.exe");
    }

    // ENV.setup_build_environment, reduced to what applies on Windows.
    static void SetupCommonEnvironment(Dictionary<string, string> env, bool superenv)
    {
        var paths = DevCommands.Paths;
        var prefix = paths.Prefix;
        env["HOMEBREW_PREFIX"] = prefix;
        env["HOMEBREW_CELLAR"] = paths.Cellar;
        env["HOMEBREW_REPOSITORY"] = paths.Repository;
        env["HOMEBREW_CACHE"] = paths.Cache;
        var jobs = Environment.GetEnvironmentVariable("HOMEBREW_MAKE_JOBS") ?? Environment.ProcessorCount.ToString();
        env["HOMEBREW_MAKE_JOBS"] = jobs;
        env["MAKEFLAGS"] = $"-j{jobs}";
        env["CMAKE_PREFIX_PATH"] = prefix;
        env["PKG_CONFIG_PATH"] = Path.Combine(prefix, "lib", "pkgconfig");
        string Append(string var, string value)
        {
            var cur = Environment.GetEnvironmentVariable(var);
            return string.IsNullOrEmpty(cur) ? value : cur + ";" + value;
        }
        env["INCLUDE"] = Append("INCLUDE", Path.Combine(prefix, "include"));
        env["LIB"] = Append("LIB", Path.Combine(prefix, "lib"));
        if (!superenv)
        {
            env["CPATH"] = Path.Combine(prefix, "include");
            env["LIBRARY_PATH"] = Path.Combine(prefix, "lib");
        }
        var path = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
        var bin = Path.Combine(prefix, "bin");
        path.RemoveAll(p => string.Equals(p.TrimEnd('\\'), bin, StringComparison.OrdinalIgnoreCase));
        path.Insert(0, bin);
        env["PATH"] = string.Join(";", path);
    }
}
