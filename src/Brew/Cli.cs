namespace Brew;

public static class Cli
{
    static readonly Dictionary<string, string> Aliases = new()
    {
        ["ls"] = "list", ["rm"] = "uninstall", ["remove"] = "uninstall", ["up"] = "update",
        ["homepage"] = "home", ["--config"] = "config", ["--repo"] = "--repository", ["abv"] = "info",
        ["-S"] = "search", ["ln"] = "link", ["instal"] = "install", ["uninstal"] = "uninstall",
        ["post_install"] = "postinstall", ["dr"] = "doctor", ["environment"] = "--env",
        ["-v"] = "--version", ["lc"] = "livecheck", ["tc"] = "typecheck", ["x"] = "exec"
    };
    static readonly Dictionary<string, string[]> Options = new()
    {
        ["install"] = ["--ignore-dependencies", "--only-dependencies", "--build-from-source", "-s", "--HEAD", "--overwrite"],
        ["uninstall"] = ["--ignore-dependencies", "--zap"], ["reinstall"] = ["--build-from-source", "-s"],
        ["upgrade"] = ["--dry-run", "-n", "--ignore-pinned"],
        ["list"] = ["--versions", "--pinned", "--installed-on-request", "--installed-as-dependency", "--full-name", "--multiple", "-1", "-l", "-r", "-t"],
        ["info"] = ["--json", "--installed", "--eval-all", "--variations"],
        ["search"] = ["--desc", "--description"], ["desc"] = ["--search", "-s", "--name", "-n", "--description", "-d", "--eval-all"],
        ["deps"] = ["--direct", "--tree", "--installed", "--eval-all", "--include-build", "--include-test", "--include-optional", "--skip-recommended", "--union", "--topological"],
        ["uses"] = ["--installed", "--eval-all", "--recursive", "--include-build", "--include-test", "--include-optional", "--skip-recommended"],
        ["leaves"] = ["--installed-on-request", "--installed-as-dependency"],
        ["missing"] = ["--hide"],
        ["outdated"] = ["--json", "--fetch-HEAD", "--greedy", "--greedy-latest", "--greedy-auto-updates"],
        ["autoremove"] = ["--dry-run", "-n"], ["cleanup"] = ["--dry-run", "-n", "--prune", "--prune-prefix", "-s"],
        ["link"] = ["--overwrite", "--dry-run", "-n"], ["unlink"] = ["--dry-run", "-n"],
        ["pin"] = [], ["unpin"] = [], ["fetch"] = ["--retry", "--deps", "--HEAD", "--build-from-source", "-s"],
        ["update"] = ["--auto-update", "--merge", "--preinstall"], ["doctor"] = ["--list-checks", "--audit-debug"],
        ["config"] = [], ["shellenv"] = [], ["commands"] = ["--include-aliases", "--quiet"],
        ["home"] = ["--dry-run"], ["tap"] = ["--custom-remote", "--force-auto-update", "--repair", "--eval-all"],
        ["untap"] = [], ["tap-info"] = ["--json", "--installed"],
        ["log"] = ["--oneline", "--stat", "-p", "--patch", "--max-count", "-n", "--since", "--until"]
    };

    public static async Task<int> RunAsync(string[] input)
    {
        CommandOutput.Quiet = false; CommandOutput.Verbose = false;
        try
        {
            var paths = new Paths();
            if (input.Length == 0) { Console.Error.WriteLine(CommandHelp.General); return 1; }
            var command = Aliases.GetValueOrDefault(input[0], input[0]);
            var raw = input.Skip(1).ToArray();
            if (command is "--help" or "-h" or "--usage" or "-?" or "help")
                return await CommandHelp.Print(raw.FirstOrDefault(), raw.Skip(1).ToArray());
            if (command is "--version" or "-v") { Console.WriteLine("Homebrew 7.0.9"); return 0; }
            if (command is "--prefix" or "--cellar" or "--repository" or "--cache") return PathsCommand(paths, command, raw);
            if (command == "--env") { new SystemCommands(paths).Env(); return 0; }
            if (CommandHelp.RubyCommands.Contains(command)
                || typeof(Cli).Assembly.GetType("Brew.Ruby.DevCommands")?.GetMethod("Handles")?.Invoke(null, [command]) is true)
                return await DevBridge.Run(command, raw);
            if (raw.Contains("--help") || raw.Contains("-h")) return await CommandHelp.Print(command, raw);
            if (CommandHelp.NativeDevCommands.Contains(command)) return await new GitDeveloperCommands(paths).Run(command, raw);
            if (!Options.TryGetValue(command, out var allowed)) return CommandHelp.Unknown(command, Aliases.Keys);
            var helpType = typeof(Cli).Assembly.GetType("Brew.Ruby.UserCommandHelp");
            var sharedParser = helpType?.GetMethod("Parser")?.Invoke(null, [command]);
            CommandArguments args;
            if (sharedParser != null)
            {
                object[] parameters = [command, raw, 0];
                var parsed = DevBridge.Invoke(typeof(Cli).Assembly.GetType("Brew.Ruby.DevCommands").GetMethod("ParseUserArgs"), parameters);
                if (parsed == null) return (int)parameters[2];
                args = new CommandArguments(parsed);
            }
            else args = new CommandArguments(raw, allowed, command == "cleanup" ? ["--prune"] : command == "log" ? ["--max-count", "-n", "--since", "--until"] : []);
            CommandOutput.Quiet = args.Has("--quiet", "-q"); CommandOutput.Verbose = args.Has("--verbose", "-v");
            var repository = new FormulaRepository(paths); var manager = new PackageManager(paths);
            var query = new QueryCommands(paths); var store = new InstallationStore(paths);
            var system = new SystemCommands(paths); var taps = new TapCommands(paths);
            var maintenance = new MaintenanceCommands(paths);
            var mutating = new[] { "install", "uninstall", "reinstall", "upgrade", "link", "unlink", "pin", "unpin", "cleanup", "autoremove" }.Contains(command);
            using var gate = mutating ? Lock(paths) : null;
            switch (command)
            {
                case "install":
                    args.RequireNames(command);
                    foreach (var name in args.Names)
                    {
                        if (args.Has("--HEAD", "--build-from-source", "-s")) throw new InvalidOperationException("Building from source is not supported by this Windows installer.");
                        if (args.Has("--only-dependencies"))
                            foreach (var dependency in repository.Load(name).Dependencies) await manager.InstallAsync(dependency, requested: false);
                        else
                        {
                            try { await manager.InstallAsync(name, force: args.Has("--force", "-f"), ignoreDependencies: args.Has("--ignore-dependencies")); }
                            catch (FormulaUnavailableException ex) { return query.SuggestInstall(ex.Name, args.Has("--formula", "--formulae")); }
                        }
                    }
                    break;
                case "uninstall":
                    args.RequireNames(command);
                    foreach (var name in args.Names) manager.Uninstall(name, args.Has("--force", "-f"), args.Has("--ignore-dependencies"));
                    break;
                case "reinstall": args.RequireNames(command); foreach (var name in args.Names) await manager.ReinstallAsync(name); break;
                case "upgrade":
                    var upgrades = args.Names.Count > 0 ? args.Names.ToArray() : query.OutdatedNames().Where(n => !store.IsPinned(n)).ToArray();
                    if (args.Has("--dry-run", "-n")) foreach (var name in upgrades) Console.WriteLine($"Would upgrade {name} {store.Latest(name)?.Receipt.Version} -> {Metadata.Version(repository.Load(name))}");
                    else foreach (var name in upgrades) await manager.UpgradeAsync(name);
                    break;
                case "fetch":
                    args.RequireNames(command);
                    foreach (var name in args.Names)
                    {
                        if (args.Has("--deps")) foreach (var dependency in repository.Load(name).Dependencies) await manager.FetchAsync(dependency);
                        await manager.FetchAsync(name);
                    }
                    break;
                case "list":
                    var listed = args.Names.Count == 0 ? store.All.Select(k => k.Receipt.Name).Distinct().Order(StringComparer.Ordinal).ToArray() : args.Names.Select(store.ResolveName).ToArray();
                    if (args.Has("--pinned")) listed = listed.Where(store.IsPinned).ToArray();
                    if (args.Has("--installed-on-request")) listed = listed.Where(n => store.Kegs(n).Any(k => k.Receipt.InstalledOnRequest)).ToArray();
                    if (args.Has("--installed-as-dependency")) listed = listed.Where(n => store.Kegs(n).All(k => !k.Receipt.InstalledOnRequest)).ToArray();
                    if (args.Has("--multiple")) listed = listed.Where(n => store.Kegs(n).Length > 1).ToArray();
                    if (args.Has("-r")) Array.Reverse(listed);
                    if (args.Names.Count == 0 && !args.Has("--versions")) foreach (var name in listed) Console.WriteLine(name);
                    else if (listed.Length > 0) manager.List(listed, args.Has("--versions"));
                    break;
                case "info": query.Info(args); break;
                case "search": query.Search(args); break;
                case "desc": query.Desc(args); break;
                case "deps": return query.Deps(args);
                case "missing": return query.Missing(args);
                case "uses": query.Uses(args); break;
                case "leaves": foreach (var name in query.Leaves(args)) Console.WriteLine(name); break;
                case "outdated": return query.Outdated(args);
                case "pin": case "unpin": args.RequireNames(command); foreach (var name in args.Names) store.Pin(name, command == "unpin"); break;
                case "link": case "unlink":
                    args.RequireNames(command);
                    foreach (var name in args.Names)
                        if (args.Has("--dry-run", "-n")) Console.WriteLine($"Would {command}: {store.Latest(store.ResolveName(name))?.Path ?? throw new InvalidOperationException($"No such keg: {paths.Rack(Paths.FormulaName(name))}")}");
                        else if (args.Has("--overwrite")) throw new InvalidOperationException("Use brew unlink to remove the existing formula's links before linking a replacement.");
                        else manager.Link(name, command == "unlink", force: args.Has("--force", "-f"));
                    break;
                case "cleanup": maintenance.Cleanup(args); break;
                case "autoremove": maintenance.Autoremove(args); break;
                case "config": await system.Config(); break;
                case "doctor": return system.Doctor();
                case "shellenv": system.Shellenv(args.Names.FirstOrDefault()); break;
                case "commands": foreach (var name in CommandHelp.All.Concat(args.Has("--include-aliases") ? Aliases.Keys : []).Order(StringComparer.Ordinal)) Console.WriteLine(name); break;
                case "tap": return await taps.Tap(args);
                case "untap": taps.Untap(args); break;
                case "tap-info": await taps.Info(args); break;
                case "update": await taps.Update(); break;
                case "home":
                    foreach (var url in args.Names.Count == 0 ? new[] { "https://brew.sh" } : args.Names.Select(n => repository.Load(n).Homepage))
                    {
                        if (args.Has("--dry-run") || Environment.GetEnvironmentVariable("HOMEBREW_BROWSER") == "false") Console.WriteLine(url);
                        else System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
                    }
                    break;
                case "log":
                    var arguments = new List<string> { "-C", paths.Repository, "--no-pager", "log" };
                    arguments.AddRange(raw.Where(a => !args.Names.Contains(a)));
                    if (args.Names.Count > 0) { arguments.Add("--"); arguments.AddRange(args.Names.Select(n => Path.GetRelativePath(paths.Repository, repository.Find(n)))); }
                    return await ProcessRunner.Run("git", arguments);
            }
            return 0;
        }
        catch (Exception ex) { CommandOutput.Error(ex.Message); return 1; }
    }

    static FileStream Lock(Paths paths)
    {
        Directory.CreateDirectory(paths.Prefix);
        return new FileStream(Path.Combine(paths.Prefix, ".brew.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }

    static int PathsCommand(Paths paths, string command, string[] names)
    {
        var installedOnly = names.Contains("--installed"); names = names.Where(n => n != "--installed").ToArray();
        if (installedOnly && names.Length == 0) throw new InvalidOperationException("Invalid usage: `--installed` requires a formula argument.");
        if (names.Length == 0)
        {
            Console.WriteLine(command switch { "--prefix" => paths.Prefix, "--cellar" => paths.Cellar, "--cache" => paths.Cache, _ => paths.Repository }); return 0;
        }
        var repository = new FormulaRepository(paths); var store = new InstallationStore(paths);
        foreach (var name in names)
        {
            if (command == "--repository") { Console.WriteLine(new TapCommands(paths).DirectoryFor(name)); continue; }
            var formula = repository.Load(name);
            if (installedOnly && store.Latest(formula.Name) == null) return 1;
            Console.WriteLine(command switch
            {
                "--prefix" => Path.Combine(paths.Prefix, "opt", formula.Name),
                "--cellar" => paths.Rack(formula.Name),
                _ => Path.Combine(paths.Cache, formula.Sha256 + "--" + Downloader.FileName(formula.Url))
            });
        }
        return 0;
    }
}
