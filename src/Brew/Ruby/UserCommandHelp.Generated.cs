// Ported from the `cmd_args` blocks of Homebrew's Library/Homebrew/cmd/*.rb and the dev-cmd/*.rb files
// that have no DevCommand class. Commands with subcommands (analytics, bundle, completions, developer,
// services) reproduce the root `brew <cmd> --help` page: the subcommand list is part of the description
// and subcommand-only options are registered hidden so they still parse.
namespace Brew.Ruby;

public static partial class UserCommandHelp
{
    static partial void Register()
    {
        Add("--cache", () => new CommandParser("--cache")
            .Description("""
                Display Homebrew's download cache. See also `$HOMEBREW_CACHE`.

                If a <formula> or <cask> is provided, display the file or directory used to cache it.
                """)
            .Flag(new[] { "--os=" }, "Show cache file for the given operating system. (Pass `all` to show cache files for all operating systems.)")
            .Flag(new[] { "--arch=" }, "Show cache file for the given CPU architecture. (Pass `all` to show cache files for all architectures.)")
            .Switch(new[] { "-s", "--build-from-source" }, "Show the cache file used when building from source.")
            .Switch(new[] { "--force-bottle" }, "Show the cache file used when pouring a bottle.")
            .Flag(new[] { "--bottle-tag=" }, "Show the cache file used when pouring a bottle for the given tag.")
            .Switch(new[] { "--HEAD" }, "Show the cache file used when building from HEAD.")
            .Switch(new[] { "--formula", "--formulae" }, "Only show cache files for formulae.")
            .Switch(new[] { "--cask", "--casks" }, "Only show cache files for casks.")
            .Conflicts("--build-from-source", "--force-bottle", "--bottle-tag", "--HEAD", "--cask")
            .Conflicts("--formula", "--cask")
            .Conflicts("--os", "--bottle-tag")
            .Conflicts("--arch", "--bottle-tag")
            .NamedArgs(new[] { "formula", "cask" }));
        Add("--caskroom", () => new CommandParser("--caskroom")
            .Description("""
                Display Homebrew's Caskroom path.

                If <cask> is provided, display the location in the Caskroom where <cask>
                would be installed, without any sort of versioned directory as the last path.
                """)
            .NamedArgs("cask"));
        Add("--cellar", () => new CommandParser("--cellar")
            .Description("""
                Display Homebrew's Cellar path. *Default:* `$(brew --prefix)/Cellar`, or if
                that directory doesn't exist, `$(brew --repository)/Cellar`.

                If <formula> is provided, display the location in the Cellar where <formula>
                would be installed, without any sort of versioned directory as the last path.
                """)
            .NamedArgs("formula"));
        Add("--env", () => new CommandParser("--env", "environment")
            .Description("""
                Summarise Homebrew's build environment as a plain list.

                If the command's output is sent through a pipe and no shell is specified,
                the list is formatted for export to `bash`(1) unless `--plain` is passed.
                """)
            .Flag(new[] { "--shell=" }, "Generate a list of environment variables for the specified shell, or `--shell=auto` to detect the current shell.")
            .Switch(new[] { "--plain" }, "Generate plain output even when piped.")
            .NamedArgs("formula"), "environment");
        Add("--prefix", () => new CommandParser("--prefix")
            .Description("""
                Display Homebrew's install path. *Default:*

                  - macOS ARM: `/opt/homebrew`
                  - macOS Intel: `/usr/local`
                  - Linux: `/home/linuxbrew/.linuxbrew`

                If <formula> is provided, display the location where <formula> is or would be installed.
                """)
            .Switch(new[] { "--unbrewed" }, "List files in Homebrew's prefix not installed by Homebrew.")
            .Switch(new[] { "--installed" }, "Outputs nothing and returns a failing status code if <formula> is not installed.")
            .Conflicts("--unbrewed", "--installed")
            .NamedArgs("formula"));
        Add("--repository", () => new CommandParser("--repository", "--repo")
            .Description("""
                Display where Homebrew's Git repository is located.

                If <user>`/`<repo> are provided, display where tap <user>`/`<repo>'s directory is located.
                """)
            .NamedArgs("tap"), "--repo");
        Add("--taps", () => new CommandParser("--taps")
            .Description("Display the path to Homebrew’s Taps directory."));
        Add("--version", () => new CommandParser("--version", "-v")
            .Description("""
                Print the version numbers of Homebrew, Homebrew/homebrew-core and
                Homebrew/homebrew-cask (if tapped) to standard output.
                """), "-v");
        Add("alias", () => new CommandParser("alias")
            .UsageBanner("""
                `alias` [`--edit`] [<alias>|<alias>=<command>]

                Show an alias's command. If no alias is given, print the whole list.
                """)
            .Switch(new[] { "--edit" }, "Edit aliases in a text editor. Either one or all aliases may be opened at once. If the given alias doesn't exist it'll be pre-populated with a template.")
            .NamedArgs((string[])null, max: 1));
        Add("analytics", () => new CommandParser("analytics")
            .UsageBanner("`analytics` [<subcommand>]")
            .Description("""
                Control Homebrew's anonymous aggregate user behaviour analytics.
                Read more at <https://docs.brew.sh/Analytics>.

                Subcommands:
                  `state`: Display the current state of Homebrew's analytics.
                  `on`: Turn Homebrew's analytics on.
                  `off`: Turn Homebrew's analytics off.
                """));
        Add("as-brew-user", () => new CommandParser("as-brew-user")
            .UsageBanner("""
                `as-brew-user` <command> [<args> ...]

                Run a Homebrew command as the owner of `HOMEBREW_PREFIX` on macOS or Linux.

                Uses the owner's home and a clean environment. Changing users requires
                permission to use `sudo` or an already-root process; running as the
                owner does not. When `sudo` is disabled or unavailable, root can switch
                users directly.
                """)
            .NamedArgs((string[])null, min: 1));
        Add("as-console-user", () => new CommandParser("as-console-user")
            .UsageBanner("""
                `as-console-user` <command> [<args> ...]

                Run a Homebrew command as the active macOS console user.

                This is intended for MDM, Munki and Jamf workflows where `brew` is
                invoked as root but Homebrew operations should run as the logged-in
                console user. Uses their home and a clean environment, dispatching
                through `HOMEBREW_BREW_FILE`. Use `as-brew-user` to select the prefix owner.
                When `sudo` is disabled or unavailable, root can switch users directly.
                """)
            .NamedArgs((string[])null, min: 1));
        Add("autoremove", () => new CommandParser("autoremove")
            .Description("Uninstall formulae that were only installed as a dependency of another formula and are now no longer needed.")
            .Switch(new[] { "-n", "--dry-run" }, "List what would be uninstalled, but do not actually uninstall anything.")
            .NamedArgs("none"));
        Add("bundle", () => new CommandParser("bundle")
            .UsageBanner("`bundle` [<subcommand>]")
            .Description("""
                Bundler for non-Ruby dependencies from Homebrew formulae, Homebrew casks, Mac App Store dependencies, VSCode (and forks/variants) extensions, Go packages, Cargo packages, uv tools, Flatpak packages, WinGet packages, Krew plugins and npm packages.

                Note: Flatpak support is only available on Linux.

                Subcommands:
                  `sh`: Run your shell in a `brew bundle exec` environment.
                  `remove`: Remove entries that match `name` from your `Brewfile`. Use `--formula`, `--cask`, `--tap`, `--mas`, `--vscode`, `--go`, `--cargo`, `--uv`, `--flatpak`, `--winget`, `--krew` and `--npm` to remove only entries of the corresponding type. Passing `--formula` also removes matches against formula aliases and old formula names.
                  `list`: List all dependencies present in the `Brewfile`.
                  `install`: Install and upgrade (by default) all dependencies from the `Brewfile`.
                  `exec`: Run an external command in an isolated build environment based on the `Brewfile` dependencies.
                  `env`: Print the environment variables that would be set in a `brew bundle exec` environment.
                  `edit`: Edit the `Brewfile` in your editor.
                  `dump`: Write all installed casks/formulae/images/taps into a `Brewfile` in the current directory or to a custom file specified with the `--file` option. This is useful as an installed-state snapshot and can be kept in version control and diffed.
                  `cleanup`: Uninstall all dependencies not present in the `Brewfile`.
                  `check`: Check if all dependencies present in the `Brewfile` are installed.
                  `add`: Add entries to your `Brewfile`. Adds formulae by default. Use `--cask`, `--tap`, `--vscode`, `--go`, `--cargo`, `--uv`, `--flatpak`, `--krew` and `--npm` to add the corresponding entry instead.
                """)
            .Flag(new[] { "--file=" }, "Read from or write to the `Brewfile` from this location. Use `--file=-` to pipe to stdin/stdout.")
            .Switch(new[] { "-g", "--global" }, "Read from or write to the `Brewfile` from `$HOMEBREW_BUNDLE_FILE_GLOBAL` (if set), `${XDG_CONFIG_HOME}/homebrew/Brewfile` (if `$XDG_CONFIG_HOME` is set), `~/.homebrew/Brewfile` or `~/.Brewfile` otherwise.")
            .Switch(new[] { "--install" }, null, hidden: true)
            .Switch(new[] { "--services" }, null, hidden: true)
            .Switch(new[] { "--check" }, null, hidden: true)
            .Switch(new[] { "--no-secrets" }, null, hidden: true)
            .Switch(new[] { "--formula", "--formulae" }, null, hidden: true)
            .Switch(new[] { "--brews" }, null, hidden: true)
            .Switch(new[] { "--cask", "--casks" }, null, hidden: true)
            .Switch(new[] { "--tap", "--taps" }, null, hidden: true)
            .Switch(new[] { "--mas" }, null, hidden: true)
            .Switch(new[] { "--vscode" }, null, hidden: true)
            .Switch(new[] { "--go" }, null, hidden: true)
            .Switch(new[] { "--cargo" }, null, hidden: true)
            .Switch(new[] { "--uv" }, null, hidden: true)
            .Switch(new[] { "--flatpak" }, null, hidden: true)
            .Switch(new[] { "--winget" }, null, hidden: true)
            .Switch(new[] { "--krew" }, null, hidden: true)
            .Switch(new[] { "--npm" }, null, hidden: true)
            .Switch(new[] { "--all" }, null, hidden: true)
            .Switch(new[] { "--no-upgrade" }, null, hidden: true)
            .Switch(new[] { "--upgrade" }, null, hidden: true)
            .Flag(new[] { "--upgrade-formulae=" }, null, hidden: true)
            .Flag(new[] { "--upgrade-formula=" }, null, hidden: true)
            .Flag(new[] { "--jobs=" }, null, hidden: true)
            .Switch(new[] { "-f", "--force" }, null, hidden: true)
            .Switch(new[] { "--cleanup" }, null, hidden: true)
            .Switch(new[] { "--force-cleanup" }, null, hidden: true)
            .Switch(new[] { "--zap" }, null, hidden: true)
            .Flag(new[] { "--sandbox=" }, null, hidden: true)
            .Switch(new[] { "--deny-network" }, null, hidden: true, dependsOn: "--sandbox=")
            .Switch(new[] { "--no-formula", "--no-formulae" }, null, hidden: true)
            .Switch(new[] { "--no-brews" }, null, hidden: true)
            .Switch(new[] { "--no-dump-brew" }, null, hidden: true)
            .Switch(new[] { "--no-cask", "--no-casks" }, null, hidden: true)
            .Switch(new[] { "--no-dump-cask" }, null, hidden: true)
            .Switch(new[] { "--no-tap", "--no-taps" }, null, hidden: true)
            .Switch(new[] { "--no-dump-tap" }, null, hidden: true)
            .Switch(new[] { "--no-mas" }, null, hidden: true)
            .Switch(new[] { "--no-dump-mas" }, null, hidden: true)
            .Switch(new[] { "--no-vscode" }, null, hidden: true)
            .Switch(new[] { "--no-dump-vscode" }, null, hidden: true)
            .Switch(new[] { "--no-go" }, null, hidden: true)
            .Switch(new[] { "--no-dump-go" }, null, hidden: true)
            .Switch(new[] { "--no-cargo" }, null, hidden: true)
            .Switch(new[] { "--no-dump-cargo" }, null, hidden: true)
            .Switch(new[] { "--no-uv" }, null, hidden: true)
            .Switch(new[] { "--no-dump-uv" }, null, hidden: true)
            .Switch(new[] { "--no-flatpak" }, null, hidden: true)
            .Switch(new[] { "--no-dump-flatpak" }, null, hidden: true)
            .Switch(new[] { "--no-winget" }, null, hidden: true)
            .Switch(new[] { "--no-dump-winget" }, null, hidden: true)
            .Switch(new[] { "--no-krew" }, null, hidden: true)
            .Switch(new[] { "--no-dump-krew" }, null, hidden: true)
            .Switch(new[] { "--no-npm" }, null, hidden: true)
            .Switch(new[] { "--no-dump-npm" }, null, hidden: true)
            .Switch(new[] { "--no-describe" }, null, hidden: true)
            .Switch(new[] { "--describe" }, null, hidden: true)
            .Switch(new[] { "--no-restart" }, null, hidden: true)
            .Switch(new[] { "--no-cleanup-brew" }, null, hidden: true)
            .Switch(new[] { "--no-cleanup-cask" }, null, hidden: true)
            .Switch(new[] { "--no-cleanup-tap" }, null, hidden: true)
            .Switch(new[] { "--no-cleanup-mas" }, null, hidden: true)
            .Switch(new[] { "--no-cleanup-vscode" }, null, hidden: true)
            .Switch(new[] { "--no-cleanup-go" }, null, hidden: true)
            .Switch(new[] { "--no-cleanup-cargo" }, null, hidden: true)
            .Switch(new[] { "--no-cleanup-uv" }, null, hidden: true)
            .Switch(new[] { "--no-cleanup-flatpak" }, null, hidden: true)
            .Switch(new[] { "--no-cleanup-winget" }, null, hidden: true)
            .Switch(new[] { "--no-cleanup-krew" }, null, hidden: true)
            .Switch(new[] { "--no-cleanup-npm" }, null, hidden: true)
            .Conflicts("--describe", "--no-describe")
            .Conflicts("--describe", "--no-describe")
            .Conflicts("--formula", "--no-formula")
            .Conflicts("--formula", "--no-formulae")
            .Conflicts("--formula", "--no-brews")
            .Conflicts("--formula", "--no-cleanup-brew")
            .Conflicts("--formula", "--no-dump-brew")
            .Conflicts("--formulae", "--no-formula")
            .Conflicts("--formulae", "--no-formulae")
            .Conflicts("--formulae", "--no-brews")
            .Conflicts("--formulae", "--no-cleanup-brew")
            .Conflicts("--formulae", "--no-dump-brew")
            .Conflicts("--brews", "--no-formula")
            .Conflicts("--brews", "--no-formulae")
            .Conflicts("--brews", "--no-brews")
            .Conflicts("--brews", "--no-cleanup-brew")
            .Conflicts("--brews", "--no-dump-brew")
            .Conflicts("--cask", "--no-cask")
            .Conflicts("--cask", "--no-casks")
            .Conflicts("--cask", "--no-cleanup-cask")
            .Conflicts("--cask", "--no-dump-cask")
            .Conflicts("--casks", "--no-cask")
            .Conflicts("--casks", "--no-casks")
            .Conflicts("--casks", "--no-cleanup-cask")
            .Conflicts("--casks", "--no-dump-cask")
            .Conflicts("--tap", "--no-tap")
            .Conflicts("--tap", "--no-taps")
            .Conflicts("--tap", "--no-cleanup-tap")
            .Conflicts("--tap", "--no-dump-tap")
            .Conflicts("--taps", "--no-tap")
            .Conflicts("--taps", "--no-taps")
            .Conflicts("--taps", "--no-cleanup-tap")
            .Conflicts("--taps", "--no-dump-tap")
            .Conflicts("--mas", "--no-mas")
            .Conflicts("--mas", "--no-cleanup-mas")
            .Conflicts("--mas", "--no-dump-mas")
            .Conflicts("--vscode", "--no-vscode")
            .Conflicts("--vscode", "--no-cleanup-vscode")
            .Conflicts("--vscode", "--no-dump-vscode")
            .Conflicts("--go", "--no-go")
            .Conflicts("--go", "--no-cleanup-go")
            .Conflicts("--go", "--no-dump-go")
            .Conflicts("--cargo", "--no-cargo")
            .Conflicts("--cargo", "--no-cleanup-cargo")
            .Conflicts("--cargo", "--no-dump-cargo")
            .Conflicts("--uv", "--no-uv")
            .Conflicts("--uv", "--no-cleanup-uv")
            .Conflicts("--uv", "--no-dump-uv")
            .Conflicts("--flatpak", "--no-flatpak")
            .Conflicts("--flatpak", "--no-cleanup-flatpak")
            .Conflicts("--flatpak", "--no-dump-flatpak")
            .Conflicts("--winget", "--no-winget")
            .Conflicts("--winget", "--no-cleanup-winget")
            .Conflicts("--winget", "--no-dump-winget")
            .Conflicts("--krew", "--no-krew")
            .Conflicts("--krew", "--no-cleanup-krew")
            .Conflicts("--krew", "--no-dump-krew")
            .Conflicts("--npm", "--no-npm")
            .Conflicts("--npm", "--no-cleanup-npm")
            .Conflicts("--npm", "--no-dump-npm")
            .Conflicts("--file", "--global"));
        Add("casks", () => new CommandParser("casks")
            .Description("List all locally installable casks including short names."));
        Add("cleanup", () => new CommandParser("cleanup")
            .Description("""
                Remove stale lock files and outdated downloads for all formulae and casks,
                and remove old versions of installed formulae. If arguments are specified,
                only do this for the given formulae and casks. Removes all downloads more than
                120 days old. This can be adjusted with `$HOMEBREW_CLEANUP_MAX_AGE_DAYS`.
                """)
            .Flag(new[] { "--prune=" }, "Remove all cache files older than specified <days>. If you want to remove everything, use `--prune=all`.")
            .Switch(new[] { "-n", "--dry-run" }, "Show what would be removed, but do not actually remove anything.")
            .Switch(new[] { "-s", "--scrub" }, "Scrub the cache, including downloads for even the latest versions. Note that downloads for any installed formulae or casks will still not be deleted. If you want to delete those too: `rm -rf \"$(brew --cache)\"`")
            .Switch(new[] { "--prune-prefix" }, "Only prune the symlinks and directories from the prefix and remove no other files.")
            .NamedArgs(new[] { "formula", "cask" }));
        Add("command-not-found-init", () => new CommandParser("command-not-found-init")
            .Description("""
                Print instructions for setting up the command-not-found hook for your shell.
                If the output is not to a tty, print the appropriate handler script for your shell.

                For more information, see:
                  https://docs.brew.sh/Command-Not-Found
                """)
            .NamedArgs("none"));
        Add("command", () => new CommandParser("command")
            .Description("Display the path to the file being used when invoking `brew` <cmd>.")
            .NamedArgs("command", min: 1));
        Add("commands", () => new CommandParser("commands")
            .Description("Show lists of built-in and external commands.")
            .Switch(new[] { "-q", "--quiet" }, "List only the names of commands without category headers.")
            .Switch(new[] { "--include-aliases" }, "Include aliases of internal commands.", dependsOn: "--quiet")
            .NamedArgs("none"));
        Add("completions", () => new CommandParser("completions")
            .UsageBanner("`completions` [<subcommand>]")
            .Description("""
                Control whether Homebrew automatically links external tap shell completion files.
                Read more at <https://docs.brew.sh/Shell-Completion>.

                Subcommands:
                  `unlink`: Unlink Homebrew's completions.
                  `state`: Display the current state of Homebrew's completions.
                  `link`: Link Homebrew's completions.
                """));
        Add("config", () => new CommandParser("config", "--config")
            .Description("""
                Show Homebrew and system configuration info useful for debugging. If you file
                a bug report, you will be required to provide this information.
                """)
            .NamedArgs("none"), "--config");
        Add("deps", () => new CommandParser("deps")
            .Description("""
                Show dependencies for <formula>. When given multiple formula arguments,
                show the intersection of dependencies for each formula. By default, `deps`
                shows all required and recommended dependencies.

                If any version of each formula argument is installed and no other options
                are passed, this command displays their actual runtime dependencies (similar
                to `brew linkage`), which may differ from a formula's declared dependencies.

                *Note:* `--missing` and `--skip-recommended` have precedence over `--include-*`.
                """)
            .Switch(new[] { "-n", "--topological" }, "Sort dependencies in topological order.")
            .Switch(new[] { "-1", "--direct", "--declared", "--1" }, "Show only the direct dependencies declared in the formula.")
            .Switch(new[] { "--union" }, "Show the union of dependencies for multiple <formula>, instead of the intersection.")
            .Switch(new[] { "--full-name" }, "List dependencies by their full name.")
            .Switch(new[] { "--include-implicit" }, "Include implicit dependencies used to download and unpack source files.")
            .Switch(new[] { "--include-build" }, "Include `:build` dependencies for <formula>.")
            .Switch(new[] { "--include-optional" }, "Include `:optional` dependencies for <formula>.")
            .Switch(new[] { "--include-test" }, "Include `:test` dependencies for <formula> (non-recursive unless `--graph` or `--tree`).")
            .Switch(new[] { "--skip-recommended" }, "Skip `:recommended` dependencies for <formula>.")
            .Switch(new[] { "--include-requirements" }, "Include requirements in addition to dependencies for <formula>.")
            .Switch(new[] { "--tree" }, "Show dependencies as a tree. When given multiple formula arguments, show individual trees for each formula.")
            .Switch(new[] { "--prune" }, "Prune parts of tree already seen.", dependsOn: "--tree")
            .Switch(new[] { "--graph" }, "Show dependencies as a directed graph.")
            .Switch(new[] { "--dot" }, "Show text-based graph description in DOT format.", dependsOn: "--graph")
            .Switch(new[] { "--annotate" }, "Mark any build, test, implicit, optional, or recommended dependencies as such in the output.")
            .Switch(new[] { "--installed" }, "List dependencies for formulae that are currently installed. If <formula> is specified, list only its dependencies that are currently installed.")
            .Flag(new[] { "--brewfile" }, "Use formulae and casks listed in a Brewfile as inputs. Defaults to `./Brewfile`; use `--brewfile=`<path> to specify another.")
            .Switch(new[] { "--missing" }, "Show only missing dependencies.")
            .Switch(new[] { "--eval-all" }, null, hidden: true)
            .Switch(new[] { "--for-each" }, "Switch into the mode used when evaluating all formulae and casks, but only list dependencies for each provided <formula>, one formula per line.")
            .Switch(new[] { "--HEAD" }, "Show dependencies for HEAD version instead of stable version.")
            .Flag(new[] { "--os=" }, "Show dependencies for the given operating system.")
            .Flag(new[] { "--arch=" }, "Show dependencies for the given CPU architecture.")
            .Switch(new[] { "--formula", "--formulae" }, "Treat all named arguments as formulae.")
            .Switch(new[] { "--cask", "--casks" }, "Treat all named arguments as casks.")
            .Conflicts("--tree", "--graph")
            .Conflicts("--installed", "--missing")
            .Conflicts("--installed", "--eval-all")
            .Conflicts("--brewfile", "--eval-all")
            .Conflicts("--formula", "--cask")
            .NamedArgs(new[] { "formula", "cask" }));
        Add("desc", () => new CommandParser("desc")
            .Description("""
                Display <formula>'s name and one-line description.
                The cache is created on the first search, making that search slower than subsequent ones.
                """)
            .Switch(new[] { "-s", "--search" }, "Search both names and descriptions for <text>. If <text> is flanked by slashes, it is interpreted as a regular expression.")
            .Switch(new[] { "-n", "--name" }, "Search just names for <text>. If <text> is flanked by slashes, it is interpreted as a regular expression.")
            .Switch(new[] { "-d", "--description" }, "Search just descriptions for <text>. If <text> is flanked by slashes, it is interpreted as a regular expression.")
            .Switch(new[] { "--eval-all" }, null, hidden: true)
            .Switch(new[] { "--formula", "--formulae" }, "Treat all named arguments as formulae.")
            .Switch(new[] { "--cask", "--casks" }, "Treat all named arguments as casks.")
            .Conflicts("--search", "--name", "--description")
            .NamedArgs(new[] { "formula", "cask", "text_or_regex" }, min: 1));
        Add("developer", () => new CommandParser("developer")
            .UsageBanner("`developer` [<subcommand>]")
            .Description("""
                Control Homebrew's developer mode. When developer mode is enabled,
                `brew update` will update Homebrew to the latest commit on the `main`
                branch instead of the latest stable version along with some other behaviour changes.

                Subcommands:
                  `state`: Display the current state of Homebrew's developer mode.
                  `on`: Turn Homebrew's developer mode on.
                  `off`: Turn Homebrew's developer mode off.
                """));
        Add("docs", () => new CommandParser("docs")
            .Description("Open Homebrew's online documentation at <https://docs.brew.sh> in a browser."));
        Add("doctor", () => new CommandParser("doctor", "dr")
            .UsageBanner("`doctor`, `dr` [`--list-checks`] [`--audit-debug`] [<diagnostic_check> ...]")
            .Description("""
                Check your system for potential problems. Will exit with a non-zero status
                if any potential problems are found.

                Please note that these warnings are just used to help the Homebrew maintainers
                with debugging if you file an issue. If everything you use Homebrew for
                is working fine: please don't worry or file an issue; just ignore this.
                """)
            .Switch(new[] { "--list-checks" }, "List all audit methods, which can be run individually if provided as arguments.")
            .Switch(new[] { "--json" }, null, hidden: true)
            .Switch(new[] { "-D", "--audit-debug" }, "Enable debugging and profiling of audit methods.")
            .NamedArgs("diagnostic_check"), "dr");
        Add("exec", () => new CommandParser("exec", "x")
            .UsageBanner("""
                `exec`, `x` [`--formulae=`<formulae>] [`--sandbox=`<path>] [`--deny-network`] [`--`] <command> [<args> ...]

                Run <command> in an environment populated by Homebrew formulae.

                If `--formulae` is passed, Homebrew installs those comma-separated
                formulae if needed, prepends their executable directories and those of
                their dependencies to `PATH` and runs <command>. This allows <command>
                to be a script path such as `./script.sh`.

                If `--formulae` is omitted, Homebrew finds a formula that provides
                <command>, installs it if needed and runs that executable.

                Example: `brew exec --formulae=jq,yq -- ./script.sh`

                Scripts can also use a shebang on systems with `env -S`:
                `#!/usr/bin/env -S brew exec --formulae=jq,yq --`
                """)
            .CommaArray("--formulae", "Comma-separated formulae to install and add to `PATH` before running <command>.")
            .Flag(new[] { "--sandbox=" }, "Run <command> in Homebrew's sandbox, allowing writes to <path> and Homebrew's temporary and cache directories.")
            .Switch(new[] { "--deny-network" }, "Deny network access from inside the sandbox.", dependsOn: "--sandbox=")
            .NamedArgs((string[])null, min: 1), "x");
        Add("fetch", () => new CommandParser("fetch")
            .Description("""
                Download a bottle (if available) or source packages for <formula>e
                and binaries for <cask>s. For files, also print SHA-256 checksums.
                """)
            .Flag(new[] { "--os=" }, "Download for the given operating system. (Pass `all` to download for all operating systems.)")
            .Flag(new[] { "--arch=" }, "Download for the given CPU architecture. (Pass `all` to download for all architectures.)")
            .Switch(new[] { "--all-platforms" }, "Download for every supported operating system and architecture, plus each language for <cask>s, fetching each distinct URL once.")
            .Flag(new[] { "--bottle-tag=" }, "Download a bottle for given tag.")
            .Switch(new[] { "--HEAD" }, "Fetch HEAD version instead of stable version.")
            .Switch(new[] { "-f", "--force" }, "Remove a previously cached version and re-fetch.")
            .Switch(new[] { "-v", "--verbose" }, "Do a verbose VCS checkout, if the URL represents a VCS. This is useful for seeing if an existing VCS cache has been updated.")
            .Switch(new[] { "--retry" }, "Retry if downloading fails or re-download if the checksum of a previously cached version no longer matches. Tries at most 5 times with exponential backoff.")
            .Switch(new[] { "--deps" }, "Also download dependencies for any listed <formula>.")
            .Switch(new[] { "--test" }, "Also download test resources and their patches for each <formula>.")
            .Switch(new[] { "-s", "--build-from-source" }, "Download source packages rather than a bottle.")
            .Switch(new[] { "--build-bottle" }, "Download source packages (for eventual bottling) rather than a bottle.")
            .Switch(new[] { "--force-bottle" }, "Download a bottle if it exists for the current or newest version of macOS, even if it would not be used during installation.")
            .Switch(new[] { "--formula", "--formulae" }, "Treat all named arguments as formulae.")
            .Switch(new[] { "--cask", "--casks" }, "Treat all named arguments as casks.")
            .Conflicts("--build-from-source", "--build-bottle", "--force-bottle", "--bottle-tag")
            .Conflicts("--cask", "--HEAD")
            .Conflicts("--cask", "--deps")
            .Conflicts("--cask", "--test")
            .Conflicts("--cask", "-s")
            .Conflicts("--cask", "--build-bottle")
            .Conflicts("--cask", "--force-bottle")
            .Conflicts("--cask", "--bottle-tag")
            .Conflicts("--formula", "--cask")
            .Conflicts("--os", "--bottle-tag")
            .Conflicts("--arch", "--bottle-tag")
            .Conflicts("--all-platforms", "--os")
            .Conflicts("--all-platforms", "--arch")
            .Conflicts("--all-platforms", "--bottle-tag")
            .NamedArgs(new[] { "formula", "cask" }, min: 1));
        Add("formulae", () => new CommandParser("formulae")
            .Description("List all locally installable formulae including short names."));
        Add("gist-logs", () => new CommandParser("gist-logs")
            .UsageBanner("`gist-logs` [`--new-issue`] [`--private`] <formula>")
            .Description("""
                Upload logs for a failed build of <formula> to a new Gist. Presents an
                error message if no logs are found.
                """)
            .Switch(new[] { "--with-hostname" }, null, hidden: true)
            .Switch(new[] { "-n", "--new-issue" }, "Automatically create a new issue in the appropriate GitHub repository after creating the Gist.")
            .Switch(new[] { "-p", "--private" }, "The Gist will be marked private and will not appear in listings but will be accessible with its link.")
            .NamedArgs("formula", number: 1));
        Add("help", () => new CommandParser("help")
            .Description("""
                Outputs the usage instructions for `brew` <command>.
                Equivalent to `brew --help` <command>.
                """)
            .NamedArgs("command"));
        Add("home", () => new CommandParser("home", "homepage")
            .Description("""
                Open a <formula> or <cask>'s homepage in a browser, or open
                Homebrew's own homepage if no argument is provided.
                """)
            .Switch(new[] { "--formula", "--formulae" }, "Treat all named arguments as formulae.")
            .Switch(new[] { "--cask", "--casks" }, "Treat all named arguments as casks.")
            .Conflicts("--formula", "--cask")
            .NamedArgs(new[] { "formula", "cask" }), "homepage");
        Add("info", () => new CommandParser("info", "abv")
            .Description("""
                Display brief statistics for your Homebrew installation.
                If a <formula> or <cask> is provided, show summary of information about it.
                """)
            .Switch(new[] { "--analytics" }, "List global Homebrew analytics data or, if specified, installation and build error data for <formula> (provided neither `$HOMEBREW_NO_ANALYTICS` nor `$HOMEBREW_NO_GITHUB_API` are set).")
            .Flag(new[] { "--days=" }, "How many days of analytics data to retrieve. The value for <days> must be `30`, `90` or `365`. The default is `30`.", dependsOn: "--analytics")
            .Flag(new[] { "--category=" }, "Which type of analytics data to retrieve. The value for <category> must be `install`, `install-on-request` or `build-error`; `cask-install` or `os-version` may be specified if <formula> is not. The default is `install`.", dependsOn: "--analytics")
            .Switch(new[] { "--github-packages-downloads" }, null, hidden: true)
            .Switch(new[] { "--github" }, "Open the GitHub source page for <formula> and <cask> in a browser. To view the history locally: `brew log -p` <formula> or <cask>")
            .Switch(new[] { "--fetch-manifest" }, null, hidden: true)
            .Flag(new[] { "--json" }, "Print a JSON representation. Currently the default value for <version> is `v1`.`v1` is valid for <formula> only. `v2` is valid for both <formula> and <cask>.See the docs for examples of using the JSON output: <https://docs.brew.sh/Querying-Brew>")
            .Switch(new[] { "--installed" }, "Output a human-readable inventory of installed formulae and casks. If `--json` is passed, print JSON for installed formulae and, with `--json=v2`, installed casks.")
            .Switch(new[] { "--eval-all" }, null, hidden: true, dependsOn: "--json")
            .Switch(new[] { "--variations" }, "Include the variations hash in each formula's JSON output.", dependsOn: "--json")
            .Switch(new[] { "-v", "--verbose" }, "Show more verbose data for <formula>, or full information with `--installed`.")
            .Switch(new[] { "--formula", "--formulae" }, "Treat all named arguments as formulae.")
            .Switch(new[] { "--cask", "--casks" }, "Treat all named arguments as casks.")
            .Switch(new[] { "--sizes" }, "Show the size of installed formulae and casks.")
            .Conflicts("--installed", "--eval-all")
            .Conflicts("--formula", "--cask")
            .Conflicts("--fetch-manifest", "--cask")
            .Conflicts("--fetch-manifest", "--json")
            .NamedArgs(new[] { "formula", "cask" }), "abv");
        Add("install", () => new CommandParser("install")
            .Description("""
                Install a <formula> or <cask>. Additional options specific to a <formula> may be
                appended to the command.

                Unless `$HOMEBREW_NO_INSTALLED_DEPENDENTS_CHECK` is set, `brew upgrade` or `brew reinstall` will be run for
                outdated dependents and dependents with broken linkage, respectively.

                Unless `$HOMEBREW_NO_INSTALL_CLEANUP` is set, `brew cleanup` will then be run for
                the installed formulae and casks or, every 30 days, for all packages.

                Unless `$HOMEBREW_NO_INSTALL_UPGRADE` is set, `brew install` <formula> will upgrade <formula> if it
                is already installed but outdated.
                """)
            .Switch(new[] { "-d", "--debug" }, "If brewing fails, open an interactive debugging session with access to IRB or a shell inside the temporary build directory.")
            .Switch(new[] { "--display-times" }, "Print install times for each package at the end of the run. Enabled by default if `$HOMEBREW_DISPLAY_INSTALL_TIMES` is set.")
            .Switch(new[] { "-f", "--force" }, "Install formulae without checking for previously installed keg-only or non-migrated versions. When installing casks, overwrite existing files (binaries and symlinks are excluded, unless originally from the same cask).")
            .Switch(new[] { "-v", "--verbose" }, "Print the verification and post-install steps.")
            .Switch(new[] { "-n", "--dry-run" }, "Show what would be installed, but do not actually install anything.")
            .Switch(new[] { "--no-ask", "--yes", "-y" }, "Do not ask for confirmation before downloading and installing. Ask mode is the default. Enabled by default if `$HOMEBREW_NO_ASK` is set.")
            .Switch(new[] { "--ask" }, null, hidden: true)
            .Switch(new[] { "--formula", "--formulae" }, "Treat all named arguments as formulae.")
            .Flag(new[] { "--env=" }, null, hidden: true)
            .Switch(new[] { "--ignore-dependencies" }, "An unsupported Homebrew development option to skip installing any dependencies of any kind. If the dependencies are not already present, the formula will have issues. If you're not developing Homebrew, consider adjusting your PATH rather than using this option.")
            .Switch(new[] { "--only-dependencies" }, "Install the dependencies with specified options but do not install the formula itself.")
            .Flag(new[] { "--cc=" }, "Attempt to compile using the specified <compiler>, which should be the name of the compiler's executable, e.g. `gcc-9` for GCC 9. In order to use LLVM's clang, specify `llvm_clang`. To use the Apple-provided clang, specify `clang`. This option will only accept compilers that are provided by Homebrew or bundled with macOS. Please do not file issues if you encounter errors while using this option.")
            .Switch(new[] { "-s", "--build-from-source" }, "Compile <formula> from source even if a bottle is provided. Dependencies will still be installed from bottles if they are available.")
            .Switch(new[] { "--force-bottle" }, "Install from a bottle if it exists for the current or newest version of macOS, even if it would not normally be used for installation.")
            .Switch(new[] { "--include-test" }, "Install testing dependencies required to run `brew test` <formula>.")
            .Switch(new[] { "--HEAD" }, "If <formula> defines it, install the HEAD version, aka. main, trunk, unstable, master.")
            .Switch(new[] { "--fetch-HEAD" }, "Fetch the upstream repository to detect if the HEAD installation of the formula is outdated. Otherwise, the repository's HEAD will only be checked for updates when a new stable or development version has been released.")
            .Switch(new[] { "--keep-tmp" }, "Retain the temporary files created during installation.")
            .Switch(new[] { "--debug-symbols" }, "Generate debug symbols on build. Source will be retained in a cache directory.", dependsOn: "--build-from-source")
            .Switch(new[] { "--build-bottle" }, "Prepare the formula for eventual bottling during installation, skipping any post-install steps.")
            .Switch(new[] { "--skip-post-install" }, "Install but skip any post-install steps.")
            .Switch(new[] { "--skip-link" }, "Install but skip linking the keg into the prefix.")
            .Switch(new[] { "--as-dependency" }, "Install but mark as installed as a dependency and not installed on request.")
            .Flag(new[] { "--bottle-arch=" }, "Optimise bottles for the specified architecture rather than the oldest architecture supported by the version of macOS the bottles are built on.", dependsOn: "--build-bottle")
            .Switch(new[] { "-i", "--interactive" }, "Download and patch <formula>, then open a shell. This allows the user to run `./configure --help` and otherwise determine how to turn the software package into a Homebrew package. If <formula> has a `fetch` block, first open a shell with network access after it runs.")
            .Switch(new[] { "-g", "--git" }, "Create a Git repository, useful for creating patches to the software.")
            .Switch(new[] { "--overwrite" }, "Delete files that already exist in the prefix while linking.")
            .Switch(new[] { "--cask", "--casks" }, "Treat all named arguments as casks.")
            .Switch(new[] { "--[no-]binaries" }, "Disable/enable linking of helper executables (default: enabled).")
            .Switch(new[] { "--require-sha" }, "Require all casks to have a checksum.")
            .Switch(new[] { "--adopt" }, "Adopt existing artifacts in the destination that are identical to those being installed. Cannot be combined with `--force`.")
            .Switch(new[] { "--skip-cask-deps" }, "Skip installing cask dependencies.")
            .Switch(new[] { "--zap" }, "For use with `brew reinstall --cask`. Remove all files associated with a cask. *May remove files which are shared between applications.*")
            .Flag(new[] { "--appdir=" }, "Target location for Applications (default: `/Applications`).")
            .Flag(new[] { "--appimagedir=" }, "Target location for AppImages (default: `~/Applications`).")
            .Flag(new[] { "--keyboard-layoutdir=" }, "Target location for Keyboard Layouts (default: `/Library/Keyboard Layouts`).")
            .Flag(new[] { "--colorpickerdir=" }, "Target location for Color Pickers (default: `~/Library/ColorPickers`).")
            .Flag(new[] { "--prefpanedir=" }, "Target location for Preference Panes (default: `~/Library/PreferencePanes`).")
            .Flag(new[] { "--qlplugindir=" }, "Target location for Quick Look Plugins (default: `~/Library/QuickLook`).")
            .Flag(new[] { "--mdimporterdir=" }, "Target location for Spotlight Plugins (default: `~/Library/Spotlight`).")
            .Flag(new[] { "--dictionarydir=" }, "Target location for Dictionaries (default: `~/Library/Dictionaries`).")
            .Flag(new[] { "--fontdir=" }, "Target location for Fonts (default: `~/Library/Fonts`).")
            .Flag(new[] { "--servicedir=" }, "Target location for Services (default: `~/Library/Services`).")
            .Flag(new[] { "--input-methoddir=" }, "Target location for Input Methods (default: `~/Library/Input Methods`).")
            .Flag(new[] { "--internet-plugindir=" }, "Target location for Internet Plugins (default: `~/Library/Internet Plug-Ins`).")
            .Flag(new[] { "--audio-unit-plugindir=" }, "Target location for Audio Unit Plugins (default: `~/Library/Audio/Plug-Ins/Components`).")
            .Flag(new[] { "--vst-plugindir=" }, "Target location for VST Plugins (default: `~/Library/Audio/Plug-Ins/VST`).")
            .Flag(new[] { "--vst3-plugindir=" }, "Target location for VST3 Plugins (default: `~/Library/Audio/Plug-Ins/VST3`).")
            .Flag(new[] { "--screen-saverdir=" }, "Target location for Screen Savers (default: `~/Library/Screen Savers`).")
            .CommaArray("--language", "Comma-separated list of language codes to prefer for cask installation. The first matching language is used, otherwise it reverts to the cask's default language. The default value is the language of your system.")
            .Conflicts("--cask", "--formulae")
            .Conflicts("--cask", "--env=")
            .Conflicts("--cask", "--ignore-dependencies")
            .Conflicts("--cask", "--only-dependencies")
            .Conflicts("--cask", "--cc=")
            .Conflicts("--cask", "--build-from-source")
            .Conflicts("--cask", "--force-bottle")
            .Conflicts("--cask", "--include-test")
            .Conflicts("--cask", "--HEAD")
            .Conflicts("--cask", "--fetch-HEAD")
            .Conflicts("--cask", "--keep-tmp")
            .Conflicts("--cask", "--debug-symbols")
            .Conflicts("--cask", "--build-bottle")
            .Conflicts("--cask", "--skip-post-install")
            .Conflicts("--cask", "--skip-link")
            .Conflicts("--cask", "--as-dependency")
            .Conflicts("--cask", "--bottle-arch=")
            .Conflicts("--cask", "--interactive")
            .Conflicts("--cask", "--git")
            .Conflicts("--cask", "--overwrite")
            .Conflicts("--formula", "--casks")
            .Conflicts("--formula", "--[no-]binaries")
            .Conflicts("--formula", "--require-sha")
            .Conflicts("--formula", "--adopt")
            .Conflicts("--formula", "--skip-cask-deps")
            .Conflicts("--formula", "--zap")
            .Conflicts("--formula", "--appdir=")
            .Conflicts("--formula", "--appimagedir=")
            .Conflicts("--formula", "--keyboard-layoutdir=")
            .Conflicts("--formula", "--colorpickerdir=")
            .Conflicts("--formula", "--prefpanedir=")
            .Conflicts("--formula", "--qlplugindir=")
            .Conflicts("--formula", "--mdimporterdir=")
            .Conflicts("--formula", "--dictionarydir=")
            .Conflicts("--formula", "--fontdir=")
            .Conflicts("--formula", "--servicedir=")
            .Conflicts("--formula", "--input-methoddir=")
            .Conflicts("--formula", "--internet-plugindir=")
            .Conflicts("--formula", "--audio-unit-plugindir=")
            .Conflicts("--formula", "--vst-plugindir=")
            .Conflicts("--formula", "--vst3-plugindir=")
            .Conflicts("--formula", "--screen-saverdir=")
            .Conflicts("--formula", "--language")
            .Conflicts("--ignore-dependencies", "--only-dependencies")
            .Conflicts("--ask", "--no-ask")
            .Conflicts("--build-from-source", "--build-bottle", "--force-bottle")
            .Conflicts("--adopt", "--force")
            .NamedArgs(new[] { "formula", "cask" }, min: 1), "instal");
        Add("leaves", () => new CommandParser("leaves")
            .Description("List installed formulae that are not dependencies of another installed formula or cask.")
            .Switch(new[] { "-r", "--installed-on-request" }, "Only list leaves that were manually installed.")
            .Switch(new[] { "-p", "--installed-as-dependency" }, "Only list leaves that were installed as dependencies.")
            .Conflicts("--installed-on-request", "--installed-as-dependency")
            .NamedArgs("none"));
        Add("link", () => new CommandParser("link", "ln")
            .Description("""
                Symlink all of <formula>'s installed files or <cask>'s binaries, manpages
                and shell completions into Homebrew's prefix. This is done automatically
                when you install formulae and casks but can be useful for manual
                installations.
                """)
            .Switch(new[] { "--overwrite" }, "Delete files that already exist in the prefix while linking.")
            .Switch(new[] { "-n", "--dry-run" }, "List files which would be linked or deleted by `brew link --overwrite` without actually linking or deleting any files.")
            .Switch(new[] { "-f", "--force" }, "Allow keg-only formulae to be linked. When linking casks, overwrite existing symlinks originally from the same cask.")
            .Switch(new[] { "--HEAD" }, "Link the HEAD version of the formula if it is installed.")
            .Switch(new[] { "--formula", "--formulae" }, "Treat all named arguments as formulae.")
            .Switch(new[] { "--cask", "--casks" }, "Treat all named arguments as casks.")
            .Conflicts("--formula", "--cask")
            .Conflicts("--HEAD", "--cask")
            .NamedArgs(new[] { "installed_formula", "installed_cask" }, min: 1), "ln");
        Add("list", () => new CommandParser("list", "ls")
            .Description("""
                List all installed formulae and casks.
                If <formula> is provided, summarise the paths within its current keg.
                If <cask> is provided, list its artifacts.
                """)
            .Switch(new[] { "--formula", "--formulae" }, "List only formulae, or treat all named arguments as formulae.")
            .Switch(new[] { "--cask", "--casks" }, "List only casks, or treat all named arguments as casks.")
            .Switch(new[] { "--full-name" }, "Print formulae with fully-qualified names. Unless `--full-name`, `--versions` or `--pinned` are passed, other options (i.e. `-1`, `-l`, `-r` and `-t`) are passed to `ls`(1) which produces the actual output.")
            .Switch(new[] { "--versions" }, "Show the version number for installed formulae, or only the specified formulae if <formula> are provided.")
            .Switch(new[] { "--json" }, "Output installed formulae and casks with versions, linked and opt-linked formula versions and pinned versions as JSON using the fast Bash command path. Requires `--versions`, no named arguments and `jq`.")
            .Switch(new[] { "--multiple" }, "Only show formulae with multiple versions installed. Implies `--versions`.")
            .Switch(new[] { "--pinned" }, "List only pinned packages, or only the specified (pinned) packages if <formula> or <cask> are provided. See also `pin`, `unpin`.")
            .Switch(new[] { "--installed-on-request" }, "List the formulae installed on request.")
            .Switch(new[] { "--no-installed-on-request" }, "List the formulae not installed on request (i.e. installed as dependencies).")
            .Switch(new[] { "--installed-as-dependency" }, null, hidden: true)
            .Switch(new[] { "--poured-from-bottle" }, "List the formulae installed from a bottle.")
            .Switch(new[] { "--built-from-source" }, "List the formulae compiled from source.")
            .Switch(new[] { "-1" }, "Force output to be one entry per line. This is the default when output is not to a terminal.")
            .Switch(new[] { "-l" }, "List formulae and/or casks in long format. Has no effect when a formula or cask name is passed as an argument.")
            .Switch(new[] { "-r" }, "Reverse the order of formula and/or cask sorting to list the oldest entries first. Has no effect when a formula or cask name is passed as an argument.")
            .Switch(new[] { "-t" }, "Sort formulae and/or casks by time modified, listing most recently modified first. Has no effect when a formula or cask name is passed as an argument.")
            .Conflicts("--formula", "--cask")
            .Conflicts("--multiple", "--cask")
            .Conflicts("--pinned", "--multiple")
            .Conflicts("--cask", "--installed-on-request")
            .Conflicts("--versions", "--installed-on-request")
            .Conflicts("--multiple", "--installed-on-request")
            .Conflicts("--pinned", "--installed-on-request")
            .Conflicts("-l", "--installed-on-request")
            .Conflicts("--cask", "--no-installed-on-request")
            .Conflicts("--versions", "--no-installed-on-request")
            .Conflicts("--multiple", "--no-installed-on-request")
            .Conflicts("--pinned", "--no-installed-on-request")
            .Conflicts("-l", "--no-installed-on-request")
            .Conflicts("--cask", "--installed-as-dependency")
            .Conflicts("--versions", "--installed-as-dependency")
            .Conflicts("--multiple", "--installed-as-dependency")
            .Conflicts("--pinned", "--installed-as-dependency")
            .Conflicts("-l", "--installed-as-dependency")
            .Conflicts("--cask", "--poured-from-bottle")
            .Conflicts("--versions", "--poured-from-bottle")
            .Conflicts("--multiple", "--poured-from-bottle")
            .Conflicts("--pinned", "--poured-from-bottle")
            .Conflicts("-l", "--poured-from-bottle")
            .Conflicts("--cask", "--built-from-source")
            .Conflicts("--versions", "--built-from-source")
            .Conflicts("--multiple", "--built-from-source")
            .Conflicts("--pinned", "--built-from-source")
            .Conflicts("-l", "--built-from-source")
            .Conflicts("--versions", "-1")
            .Conflicts("--multiple", "-1")
            .Conflicts("--pinned", "-1")
            .Conflicts("--versions", "-l")
            .Conflicts("--multiple", "-l")
            .Conflicts("--pinned", "-l")
            .Conflicts("--versions", "-r")
            .Conflicts("--multiple", "-r")
            .Conflicts("--pinned", "-r")
            .Conflicts("--versions", "-t")
            .Conflicts("--multiple", "-t")
            .Conflicts("--pinned", "-t")
            .Conflicts("--full-name", "--versions")
            .Conflicts("--full-name", "--multiple")
            .Conflicts("--full-name", "--pinned")
            .Conflicts("--full-name", "-l")
            .Conflicts("--full-name", "-r")
            .Conflicts("--full-name", "-t")
            .NamedArgs(new[] { "installed_formula", "installed_cask" }), "ls");
        Add("log", () => new CommandParser("log")
            .Description("""
                Show the `git log` for <formula> or <cask>, or show the log for the Homebrew repository
                if no formula or cask is provided.
                """)
            .Switch(new[] { "-p", "-u", "--patch" }, "Also print patch from commit.")
            .Switch(new[] { "--stat" }, "Also print diffstat from commit.")
            .Switch(new[] { "--oneline" }, "Print only one line per commit.")
            .Switch(new[] { "-1" }, "Print only one commit.")
            .Flag(new[] { "-n", "--max-count=" }, "Print only a specified number of commits.")
            .Switch(new[] { "--formula", "--formulae" }, "Treat all named arguments as formulae.")
            .Switch(new[] { "--cask", "--casks" }, "Treat all named arguments as casks.")
            .Conflicts("-1", "--max-count")
            .Conflicts("--formula", "--cask")
            .NamedArgs(new[] { "formula", "cask" }, max: 1));
        Add("mcp-server", () => new CommandParser("mcp-server")
            .UsageBanner("`mcp-server` [`--debug`]")
            .Description("Starts the Homebrew MCP (Model Context Protocol) server.")
            .Switch(new[] { "-d", "--debug" }, "Enable debug logging to stderr.")
            .Switch(new[] { "--ping" }, null, hidden: true));
        Add("migrate", () => new CommandParser("migrate")
            .Description("""
                Migrate renamed packages to new names, where <formula> are old names of
                packages.
                """)
            .Switch(new[] { "-f", "--force" }, "Treat installed <formula> and provided <formula> as if they are from the same taps and migrate them anyway.")
            .Switch(new[] { "-n", "--dry-run" }, "Show what would be migrated, but do not actually migrate anything.")
            .Switch(new[] { "--formula", "--formulae" }, "Only migrate formulae.")
            .Switch(new[] { "--cask", "--casks" }, "Only migrate casks.")
            .Conflicts("--formula", "--cask")
            .NamedArgs(new[] { "installed_formula", "installed_cask" }, min: 1));
        Add("missing", () => new CommandParser("missing")
            .Description("""
                Check the given <formula> kegs and <cask> installations for missing dependencies.
                If no <formula> or <cask> are provided, check all kegs and casks. Will exit with
                a non-zero status if any kegs or casks are found to be missing dependencies.
                """)
            .CommaArray("--hide", "Act as if none of the specified <hidden> are installed. <hidden> should be a comma-separated list of formulae or casks.")
            .NamedArgs(new[] { "formula", "cask" }));
        Add("nodenv-sync", () => new CommandParser("nodenv-sync")
            .Description("""
                Create symlinks for Homebrew's installed NodeJS versions in `~/.nodenv/versions`.

                Note that older version symlinks will also be created so e.g. NodeJS 19.1.0 will
                also be symlinked to 19.0.0.
                """)
            .NamedArgs("none"));
        Add("options", () => new CommandParser("options")
            .UsageBanner("`options` [`--compact`] [`--installed`] [<formula> ...]")
            .Description("Show install options specific to <formula>.")
            .Switch(new[] { "--compact" }, "Show all options on a single line separated by spaces.")
            .Switch(new[] { "--installed" }, "Show options for formulae that are currently installed.")
            .Switch(new[] { "--eval-all" }, null, hidden: true)
            .Flag(new[] { "--command=" }, null, hidden: true)
            .Conflicts("--command", "--installed", "--eval-all")
            .NamedArgs("formula"));
        Add("outdated", () => new CommandParser("outdated")
            .Description("""
                List installed casks and formulae that have an updated version available. By default, version
                information is displayed in interactive shells and suppressed otherwise.
                """)
            .Switch(new[] { "-q", "--quiet" }, "List only the names of outdated kegs (takes precedence over `--verbose`).")
            .Switch(new[] { "-v", "--verbose" }, "Include detailed version information.")
            .Switch(new[] { "--formula", "--formulae" }, "List only outdated formulae.")
            .Switch(new[] { "--cask", "--casks" }, "List only outdated casks.")
            .Flag(new[] { "--json" }, "Print output in JSON format. There are two versions: `v1` and `v2`. `v1` is deprecated and is currently the default if no version is specified. `v2` prints outdated formulae and casks.")
            .Flag(new[] { "--minimum-version=", "--min-version=" }, "Only list a named formula or cask with an installed version below the given minimum version.")
            .Switch(new[] { "--fetch-HEAD" }, "Fetch the upstream repository to detect if the HEAD installation of the formula is outdated. Otherwise, the repository's HEAD will only be checked for updates when a new stable or development version has been released.")
            .Switch(new[] { "-g", "--greedy" }, "Also include outdated casks with `version :latest` and `auto_updates true` casks that would otherwise be skipped. Enabled by default if `$HOMEBREW_UPGRADE_GREEDY` is set.")
            .Switch(new[] { "--greedy-latest" }, "Also include outdated casks including those with `version :latest`.")
            .Switch(new[] { "--greedy-auto-updates" }, "Also include outdated `auto_updates true` casks that would otherwise be skipped.")
            .Conflicts("--quiet", "--verbose", "--json")
            .Conflicts("--formula", "--cask")
            .NamedArgs(new[] { "formula", "cask" }));
        Add("pin", () => new CommandParser("pin")
            .Description("""
                Pin the specified package, preventing it from being upgraded when
                issuing the `brew upgrade` <formula> or <cask> command. See also `unpin`.

                *Note:* Other packages which depend on newer versions of a pinned formula
                might not install or run correctly.
                Pinned casks with `auto_updates true` may update themselves outside Homebrew.
                """)
            .Switch(new[] { "--formula", "--formulae" }, "Treat all named arguments as formulae.")
            .Switch(new[] { "--cask", "--casks" }, "Treat all named arguments as casks.")
            .Conflicts("--formula", "--cask")
            .NamedArgs(new[] { "installed_formula", "installed_cask" }, min: 1));
        Add("postinstall", () => new CommandParser("postinstall", "post_install")
            .Description("Rerun the post-install steps for <formula>.")
            .NamedArgs("installed_formula", min: 1), "post_install");
        Add("pyenv-sync", () => new CommandParser("pyenv-sync")
            .Description("""
                Create symlinks for Homebrew's installed Python versions in `~/.pyenv/versions`.

                Note that older patch version symlinks will be created and linked to the minor
                version so e.g. Python 3.11.0 will also be symlinked to 3.11.3.
                """)
            .NamedArgs("none"));
        Add("rbenv-sync", () => new CommandParser("rbenv-sync")
            .Description("""
                Create symlinks for Homebrew's installed Ruby versions in `~/.rbenv/versions`.

                Note that older version symlinks will also be created so e.g. Ruby 3.2.1 will
                also be symlinked to 3.2.0.
                """)
            .NamedArgs("none"));
        Add("readall", () => new CommandParser("readall")
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
            .NamedArgs("tap"));
        Add("reinstall", () => new CommandParser("reinstall")
            .Description("""
                Uninstall and then reinstall a <formula> or <cask> using the same options it was
                originally installed with, plus any appended options specific to a <formula>.

                Unless `$HOMEBREW_NO_INSTALLED_DEPENDENTS_CHECK` is set, `brew upgrade` or `brew reinstall` will be run for
                outdated dependents and dependents with broken linkage, respectively.

                Unless `$HOMEBREW_NO_INSTALL_CLEANUP` is set, `brew cleanup` will then be run for the
                reinstalled formulae and casks or, every 30 days, for all packages.
                """)
            .Switch(new[] { "-d", "--debug" }, "If brewing fails, open an interactive debugging session with access to IRB or a shell inside the temporary build directory.")
            .Switch(new[] { "--display-times" }, "Print install times for each package at the end of the run. Enabled by default if `$HOMEBREW_DISPLAY_INSTALL_TIMES` is set.")
            .Switch(new[] { "-f", "--force" }, "Install without checking for previously installed keg-only or non-migrated versions.")
            .Switch(new[] { "-v", "--verbose" }, "Print the verification and post-install steps.")
            .Switch(new[] { "--no-ask", "--yes", "-y" }, "Do not ask for confirmation before downloading and reinstalling. Ask mode is the default. Enabled by default if `$HOMEBREW_NO_ASK` is set.")
            .Switch(new[] { "--ask" }, null, hidden: true)
            .Switch(new[] { "--formula", "--formulae" }, "Treat all named arguments as formulae.")
            .Switch(new[] { "-s", "--build-from-source" }, "Compile <formula> from source even if a bottle is available.")
            .Switch(new[] { "-i", "--interactive" }, "Download and patch <formula>, then open a shell. This allows the user to run `./configure --help` and otherwise determine how to turn the software package into a Homebrew package. If <formula> has a `fetch` block, first open a shell with network access after it runs.")
            .Switch(new[] { "--force-bottle" }, "Install from a bottle if it exists for the current or newest version of macOS, even if it would not normally be used for installation.")
            .Switch(new[] { "--keep-tmp" }, "Retain the temporary files created during installation.")
            .Switch(new[] { "--debug-symbols" }, "Generate debug symbols on build. Source will be retained in a cache directory.", dependsOn: "--build-from-source")
            .Switch(new[] { "-g", "--git" }, "Create a Git repository, useful for creating patches to the software.")
            .Switch(new[] { "--cask", "--casks" }, "Treat all named arguments as casks.")
            .Switch(new[] { "--[no-]binaries" }, "Disable/enable linking of helper executables (default: enabled).")
            .Switch(new[] { "--require-sha" }, "Require all casks to have a checksum.")
            .Switch(new[] { "--adopt" }, "Adopt existing artifacts in the destination that are identical to those being installed. Cannot be combined with `--force`.")
            .Switch(new[] { "--skip-cask-deps" }, "Skip installing cask dependencies.")
            .Switch(new[] { "--zap" }, "For use with `brew reinstall --cask`. Remove all files associated with a cask. *May remove files which are shared between applications.*")
            .Flag(new[] { "--appdir=" }, "Target location for Applications (default: `/Applications`).")
            .Flag(new[] { "--appimagedir=" }, "Target location for AppImages (default: `~/Applications`).")
            .Flag(new[] { "--keyboard-layoutdir=" }, "Target location for Keyboard Layouts (default: `/Library/Keyboard Layouts`).")
            .Flag(new[] { "--colorpickerdir=" }, "Target location for Color Pickers (default: `~/Library/ColorPickers`).")
            .Flag(new[] { "--prefpanedir=" }, "Target location for Preference Panes (default: `~/Library/PreferencePanes`).")
            .Flag(new[] { "--qlplugindir=" }, "Target location for Quick Look Plugins (default: `~/Library/QuickLook`).")
            .Flag(new[] { "--mdimporterdir=" }, "Target location for Spotlight Plugins (default: `~/Library/Spotlight`).")
            .Flag(new[] { "--dictionarydir=" }, "Target location for Dictionaries (default: `~/Library/Dictionaries`).")
            .Flag(new[] { "--fontdir=" }, "Target location for Fonts (default: `~/Library/Fonts`).")
            .Flag(new[] { "--servicedir=" }, "Target location for Services (default: `~/Library/Services`).")
            .Flag(new[] { "--input-methoddir=" }, "Target location for Input Methods (default: `~/Library/Input Methods`).")
            .Flag(new[] { "--internet-plugindir=" }, "Target location for Internet Plugins (default: `~/Library/Internet Plug-Ins`).")
            .Flag(new[] { "--audio-unit-plugindir=" }, "Target location for Audio Unit Plugins (default: `~/Library/Audio/Plug-Ins/Components`).")
            .Flag(new[] { "--vst-plugindir=" }, "Target location for VST Plugins (default: `~/Library/Audio/Plug-Ins/VST`).")
            .Flag(new[] { "--vst3-plugindir=" }, "Target location for VST3 Plugins (default: `~/Library/Audio/Plug-Ins/VST3`).")
            .Flag(new[] { "--screen-saverdir=" }, "Target location for Screen Savers (default: `~/Library/Screen Savers`).")
            .CommaArray("--language", "Comma-separated list of language codes to prefer for cask installation. The first matching language is used, otherwise it reverts to the cask's default language. The default value is the language of your system.")
            .Conflicts("--cask", "--formulae")
            .Conflicts("--cask", "--build-from-source")
            .Conflicts("--cask", "--interactive")
            .Conflicts("--cask", "--force-bottle")
            .Conflicts("--cask", "--keep-tmp")
            .Conflicts("--cask", "--debug-symbols")
            .Conflicts("--cask", "--git")
            .Conflicts("--formula", "--casks")
            .Conflicts("--formula", "--[no-]binaries")
            .Conflicts("--formula", "--require-sha")
            .Conflicts("--formula", "--adopt")
            .Conflicts("--formula", "--skip-cask-deps")
            .Conflicts("--formula", "--zap")
            .Conflicts("--formula", "--appdir=")
            .Conflicts("--formula", "--appimagedir=")
            .Conflicts("--formula", "--keyboard-layoutdir=")
            .Conflicts("--formula", "--colorpickerdir=")
            .Conflicts("--formula", "--prefpanedir=")
            .Conflicts("--formula", "--qlplugindir=")
            .Conflicts("--formula", "--mdimporterdir=")
            .Conflicts("--formula", "--dictionarydir=")
            .Conflicts("--formula", "--fontdir=")
            .Conflicts("--formula", "--servicedir=")
            .Conflicts("--formula", "--input-methoddir=")
            .Conflicts("--formula", "--internet-plugindir=")
            .Conflicts("--formula", "--audio-unit-plugindir=")
            .Conflicts("--formula", "--vst-plugindir=")
            .Conflicts("--formula", "--vst3-plugindir=")
            .Conflicts("--formula", "--screen-saverdir=")
            .Conflicts("--formula", "--language")
            .Conflicts("--build-from-source", "--force-bottle")
            .Conflicts("--ask", "--no-ask")
            .NamedArgs(new[] { "formula", "cask" }, min: 1));
        Add("sandbox-exec", () => new CommandParser("sandbox-exec")
            .UsageBanner("""
                `sandbox-exec` [`--deny-network`] <writable-path> [`--`] <command> [<args> ...]

                Run <command> in Homebrew's sandbox, allowing writes to <writable-path> and
                Homebrew's temporary and cache directories.

                Example: `brew sandbox-exec . -- make test`
                """)
            .Switch(new[] { "--deny-network" }, "Deny network access from inside the sandbox.")
            .NamedArgs((string[])null, min: 2));
        Add("search", () => new CommandParser("search", "-S")
            .Description("""
                Perform a substring search of cask tokens and formula names for <text>. If <text>
                is flanked by slashes, it is interpreted as a regular expression.
                """)
            .Switch(new[] { "--formula", "--formulae" }, "Search for formulae.")
            .Switch(new[] { "--cask", "--casks" }, "Search for casks.")
            .Switch(new[] { "--desc" }, "Search for formulae with a description matching <text> and casks with a name or description matching <text>.")
            .Switch(new[] { "--eval-all" }, null, hidden: true)
            .Switch(new[] { "--pull-request" }, "Search for GitHub pull requests containing <text>.")
            .Switch(new[] { "--open" }, "Search for only open GitHub pull requests.", dependsOn: "--pull-request")
            .Switch(new[] { "--closed" }, "Search for only closed GitHub pull requests.", dependsOn: "--pull-request")
            .Switch(new[] { "--alpine" }, "Search for <text> in the given database.")
            .Switch(new[] { "--repology" }, "Search for <text> in the given database.")
            .Switch(new[] { "--macports" }, "Search for <text> in the given database.")
            .Switch(new[] { "--fink" }, "Search for <text> in the given database.")
            .Switch(new[] { "--opensuse" }, "Search for <text> in the given database.")
            .Switch(new[] { "--fedora" }, "Search for <text> in the given database.")
            .Switch(new[] { "--archlinux" }, "Search for <text> in the given database.")
            .Switch(new[] { "--debian" }, "Search for <text> in the given database.")
            .Switch(new[] { "--ubuntu" }, "Search for <text> in the given database.")
            .Conflicts("--desc", "--pull-request")
            .Conflicts("--open", "--closed")
            .Conflicts("--alpine", "--repology", "--macports", "--fink", "--opensuse", "--fedora", "--archlinux", "--debian", "--ubuntu")
            .NamedArgs("text_or_regex", min: 1), "-S");
        Add("services", () => new CommandParser("services")
            .UsageBanner("`services` [<subcommand>]")
            .Description("""
                Manage background services with macOS' `launchctl`(1) daemon manager or
                Linux's `systemctl`(1) service manager.

                If `sudo` is passed, operate on `/Library/LaunchDaemons` or `/usr/lib/systemd/system` (started at boot).
                Otherwise, operate on `~/Library/LaunchAgents` or `~/.config/systemd/user` (started at login).

                Environment variables can be added or overridden for a service by creating
                `$HOMEBREW_USER_CONFIG_HOME/services/<formula>.env` (defaults to
                `~/.homebrew/services/<formula>.env`). The file uses `KEY=value`
                format, one per line; lines starting with `#` are comments. Changes take
                effect on the next `brew services restart` and persist across upgrades.

                Subcommands:
                  `stop`: Stop the service <formula> immediately and unregister it from launching at login (or boot),
                  `start`: Start the service <formula> immediately and register it to launch at login (or boot).
                  `run`: Run the service <formula> without registering to launch at login (or boot).
                  `restart`: Stop (if necessary) and start the service <formula> immediately and register it to launch at login (or boot).
                  `list`: List information about all managed services for the current user (or root).
                  `kill`: Stop the service <formula> immediately but keep it registered to launch at login (or boot).
                  `info`: List all managed services for the current user (or root).
                  `cleanup`: Remove all unused services.
                """)
            .Flag(new[] { "--sudo-service-user=" }, "When run as root on macOS, run the service(s) as this user.")
            .Flag(new[] { "--max-wait=" }, null, hidden: true)
            .Switch(new[] { "--no-wait" }, null, hidden: true)
            .Switch(new[] { "--keep" }, null, hidden: true)
            .Switch(new[] { "--all" }, null, hidden: true)
            .Flag(new[] { "--file=" }, null, hidden: true)
            .Switch(new[] { "--json" }, null, hidden: true)
            .Conflicts("--all", "--file")
            .Conflicts("--max-wait", "--no-wait"));
        Add("setup-ruby", () => new CommandParser("setup-ruby")
            .Description("Installs and configures Homebrew's Ruby. If `command` is passed, it will only run Bundler if necessary for that command.")
            .NamedArgs("command"));
        Add("shellenv", () => new CommandParser("shellenv")
            .Description("""
                Valid shells: bash|csh|fish|pwsh|sh|tcsh|zsh

                Print export statements. When run in a shell, this installation of Homebrew will be added to your
                `$PATH`, `$MANPATH`, and `$INFOPATH`.

                The variables `$HOMEBREW_PREFIX`, `$HOMEBREW_CELLAR` and `$HOMEBREW_REPOSITORY` are also exported to avoid
                querying them multiple times.
                To help guarantee idempotence, this command produces no output when Homebrew's `bin` and `sbin` directories
                are first and second respectively in your `$PATH`. Consider adding evaluation of this command's output to
                your dotfiles (e.g. `~/.bash_profile` or `~/.zprofile` on macOS and `~/.bashrc` or `~/.zshrc` on Linux)
                with e.g.:
                  `eval "$(brew shellenv zsh)"` or `eval "$(brew shellenv bash)"`

                The shell should be specified explicitly with a supported shell name parameter but will be detected
                automatically if not provided (but this may not be correct). Unknown shells will output POSIX exports.
                """)
            .NamedArgs("shell"));
        Add("source", () => new CommandParser("source")
            .Description("""
                Open a <formula>'s source repository in a browser, or open
                Homebrew's own repository if no argument is provided.

                The repository URL is determined from the formula's head URL,
                stable URL, or homepage. Supports GitHub, GitLab, Bitbucket, Codeberg and
                SourceHut repositories.
                """)
            .NamedArgs("formula"));
        Add("tab", () => new CommandParser("tab")
            .Description("""
                Edit tab information for installed formulae or casks.

                This can be useful when you want to control whether an installed
                formula should be removed by `brew autoremove`.
                To prevent removal, mark the formula as installed on request;
                to allow removal, mark the formula as not installed on request.
                """)
            .Switch(new[] { "--installed-on-request" }, "Mark <installed_formula> or <installed_cask> as installed on request.")
            .Switch(new[] { "--no-installed-on-request" }, "Mark <installed_formula> or <installed_cask> as not installed on request.")
            .Switch(new[] { "--formula", "--formulae" }, "Only mark formulae.")
            .Switch(new[] { "--cask", "--casks" }, "Only mark casks.")
            .Conflicts("--formula", "--cask")
            .Conflicts("--installed-on-request", "--no-installed-on-request")
            .NamedArgs(new[] { "installed_formula", "installed_cask" }, min: 1));
        Add("tap-info", () => new CommandParser("tap-info")
            .Description("""
                Show detailed information about one or more <tap>s.
                If no <tap> names are provided, display brief statistics for all installed taps.
                """)
            .Switch(new[] { "--installed" }, "Show information on each installed tap.")
            .Flag(new[] { "--json" }, "Print a JSON representation of <tap>. Currently the default and only accepted value for <version> is `v1`. See the docs for examples of using the JSON output: <https://docs.brew.sh/Querying-Brew>")
            .NamedArgs("tap"));
        Add("tap", () => new CommandParser("tap")
            .UsageBanner("""
                `tap` [<options>] [<user>`/`<repo>] [<URL>]

                Tap a repository containing formulae, casks, or external commands.
                If no arguments are provided, list all installed taps.

                With <URL> unspecified, tap a repository from GitHub using HTTPS. Since
                so many taps are hosted on GitHub, this command is a shortcut for:

                `brew tap` <user>`/`<repo> `https://github.com/`<user>`/homebrew-`<repo>

                With <URL> specified, tap a repository from anywhere, using any transport
                protocol that `git`(1) handles. Unlike the one-argument form of `tap` which
                simplifies things, the two-argument form makes no assumptions, so taps can
                be cloned from places other than GitHub and using protocols other than HTTPS,
                e.g. SSH, git, HTTP, FTP(S), rsync.
                """)
            .Switch(new[] { "--custom-remote" }, "Install or change a tap with a custom remote. Useful for mirrors.")
            .Switch(new[] { "--repair" }, "Add missing symlinks to tap manpages and shell completions. Correct git remote refs for any taps where upstream HEAD branch has been renamed.")
            .Switch(new[] { "--eval-all" }, null, hidden: true)
            .Switch(new[] { "-f", "--force" }, "Force install core taps even under API mode.")
            .NamedArgs("tap", max: 2));
        Add("trust", () => new CommandParser("trust")
            .Description("""
                Trust non-official tap formulae, casks or commands so Homebrew may load them.
                Trusted entries are stored in `${XDG_CONFIG_HOME}/homebrew/trust.json` if
                `$XDG_CONFIG_HOME` is set or `~/.homebrew/trust.json` otherwise.
                """)
            .Switch(new[] { "--tap", "--taps" }, "Trust the named tap.")
            .Switch(new[] { "--formula", "--formulae" }, "Trust the named formula.")
            .Switch(new[] { "--cask", "--casks" }, "Trust the named cask.")
            .Switch(new[] { "--command", "--commands" }, "Trust the named external command.")
            .Flag(new[] { "--json=" }, "Print trusted entries as JSON. A <version> number is required. The only accepted value for <version> is `v1`.")
            .Conflicts("--tap", "--formula", "--cask", "--command")
            .NamedArgs("target"));
        Add("unalias", () => new CommandParser("unalias")
            .Description("Remove aliases.")
            .NamedArgs("alias", min: 1));
        Add("uninstall", () => new CommandParser("uninstall", "rm", "remove")
            .Description("Uninstall a <formula> or <cask>.")
            .Switch(new[] { "-f", "--force" }, "Delete all installed versions of <formula>. Uninstall even if <cask> is not installed, overwrite existing files and ignore errors when removing files.")
            .Switch(new[] { "--zap" }, "Remove all files associated with a <cask>. *May remove files which are shared between applications.*")
            .Switch(new[] { "--ignore-dependencies" }, "Don't fail uninstall, even if <formula> is a dependency of any installed formulae.")
            .Switch(new[] { "--formula", "--formulae" }, "Treat all named arguments as formulae.")
            .Switch(new[] { "--cask", "--casks" }, "Treat all named arguments as casks.")
            .Conflicts("--formula", "--cask")
            .Conflicts("--formula", "--zap")
            .NamedArgs(new[] { "installed_formula", "installed_cask" }, min: 1), "uninstal", "rm", "remove");
        Add("unlink", () => new CommandParser("unlink")
            .Description("""
                Remove symlinks for <formula> or <cask> from Homebrew's prefix. This can be
                useful for temporarily disabling a formula:
                `brew unlink` <formula> `&&` <commands> `&& brew link` <formula>
                """)
            .Switch(new[] { "-n", "--dry-run" }, "List files which would be unlinked without actually unlinking or deleting any files.")
            .Switch(new[] { "--formula", "--formulae" }, "Treat all named arguments as formulae.")
            .Switch(new[] { "--cask", "--casks" }, "Treat all named arguments as casks.")
            .Conflicts("--formula", "--cask")
            .NamedArgs(new[] { "installed_formula", "installed_cask" }, min: 1));
        Add("unpin", () => new CommandParser("unpin")
            .Description("""
                Unpin the specified package, allowing it to be upgraded by `brew upgrade` <formula> or <cask>.
                See also `pin`.
                """)
            .Switch(new[] { "--formula", "--formulae" }, "Treat all named arguments as formulae.")
            .Switch(new[] { "--cask", "--casks" }, "Treat all named arguments as casks.")
            .Conflicts("--formula", "--cask")
            .NamedArgs(new[] { "installed_formula", "installed_cask" }, min: 1));
        Add("untap", () => new CommandParser("untap")
            .Description("Remove a tapped formula repository.")
            .Switch(new[] { "-f", "--force" }, "Uninstall all formulae and casks from this tap with `--force` before untapping.")
            .NamedArgs("tap", min: 1));
        Add("untrust", () => new CommandParser("untrust")
            .Description("""
                Stop trusting non-official tap formulae, casks or commands.
                Trusted entries are stored in `${XDG_CONFIG_HOME}/homebrew/trust.json` if
                `$XDG_CONFIG_HOME` is set or `~/.homebrew/trust.json` otherwise.
                """)
            .Switch(new[] { "--tap" }, "Untrust the named tap.")
            .Switch(new[] { "--formula", "--formulae" }, "Untrust the named formula.")
            .Switch(new[] { "--cask", "--casks" }, "Untrust the named cask.")
            .Switch(new[] { "--command", "--commands" }, "Untrust the named external command.")
            .Conflicts("--tap", "--formula", "--cask", "--command")
            .NamedArgs("target"));
        Add("update-if-needed", () => new CommandParser("update-if-needed")
            .Description("""
                Runs `brew update --auto-update` only if needed.
                This is a good replacement for `brew update` in scripts where you want
                the no-op case to be both possible and really fast.
                """)
            .NamedArgs("none"));
        Add("update-report", () => new CommandParser("update-report")
            .Description("The Ruby implementation of `brew update`. Never called manually.")
            .Switch(new[] { "--auto-update", "--preinstall" }, "Run in 'auto-update' mode (faster, less output).")
            .Switch(new[] { "-f", "--force" }, "Treat installed and updated formulae as if they are from the same taps and migrate them anyway."));
        Add("update-reset", () => new CommandParser("update-reset")
            .Description("""
                Fetch and reset Homebrew and all tap repositories (or any specified <repository>) using `git`(1) to their latest `origin/HEAD`.

                *Note:* this will destroy all your uncommitted or committed changes.
                """)
            .NamedArgs("repository"));
        Add("update", () => new CommandParser("update", "up")
            .Description("Fetch the newest version of Homebrew and all formulae from GitHub using `git`(1) and perform any necessary migrations.")
            .Switch(new[] { "--merge" }, null, hidden: true)
            .Switch(new[] { "--auto-update" }, "Run on auto-updates (e.g. before `brew install`). Skips some slower steps.")
            .Switch(new[] { "-f", "--force" }, "Always do a slower, full update check (even if unnecessary).")
            .Switch(new[] { "-q", "--quiet" }, "Make some output more quiet.")
            .Switch(new[] { "-v", "--verbose" }, "Print the directories checked and `git` operations performed.")
            .Switch(new[] { "-d", "--debug" }, "Display a trace of all shell commands as they are executed."), "up");
        Add("upgrade", () => new CommandParser("upgrade")
            .Description("""
                Upgrade outdated, unpinned packages using the same options they were originally installed with,
                plus any appended brew formula options. If <cask> or <formula> are specified, upgrade only the given
                <cask> or <formula> (unless they are pinned; see `pin`, `unpin`).

                Unless `$HOMEBREW_NO_INSTALLED_DEPENDENTS_CHECK` is set, `brew upgrade` or `brew reinstall` will be run for
                outdated dependents and dependents with broken linkage, respectively.

                Unless `$HOMEBREW_NO_INSTALL_CLEANUP` is set, `brew cleanup` will then be run for the
                upgraded formulae and casks or, every 30 days, for all packages.
                """)
            .Switch(new[] { "-d", "--debug" }, "If brewing fails, open an interactive debugging session with access to IRB or a shell inside the temporary build directory.")
            .Switch(new[] { "--display-times" }, "Print install times for each package at the end of the run. Enabled by default if `$HOMEBREW_DISPLAY_INSTALL_TIMES` is set.")
            .Switch(new[] { "-f", "--force" }, "Install formulae without checking for previously installed keg-only or non-migrated versions. When installing casks, overwrite existing files (binaries and symlinks are excluded, unless originally from the same cask).")
            .Switch(new[] { "-v", "--verbose" }, "Print the verification and post-install steps.")
            .Switch(new[] { "-n", "--dry-run" }, "Show what would be upgraded, but do not actually upgrade anything.")
            .Flag(new[] { "--minimum-version=", "--min-version=" }, "Only upgrade a named formula or cask with an installed version below the given minimum version.")
            .Switch(new[] { "--no-ask", "--yes", "-y" }, "Do not ask for confirmation before downloading and upgrading. Ask mode is the default. Enabled by default if `$HOMEBREW_NO_ASK` is set.")
            .Switch(new[] { "--ask" }, null, hidden: true)
            .Switch(new[] { "--formula", "--formulae" }, "Treat all named arguments as formulae. If no named arguments are specified, upgrade only outdated formulae.")
            .Switch(new[] { "-s", "--build-from-source" }, "Compile <formula> from source even if a bottle is available.")
            .Switch(new[] { "-i", "--interactive" }, "Download and patch <formula>, then open a shell. This allows the user to run `./configure --help` and otherwise determine how to turn the software package into a Homebrew package. If <formula> has a `fetch` block, first open a shell with network access after it runs.")
            .Switch(new[] { "--force-bottle" }, "Install from a bottle if it exists for the current or newest version of macOS, even if it would not normally be used for installation.")
            .Switch(new[] { "--fetch-HEAD" }, "Fetch the upstream repository to detect if the HEAD installation of the formula is outdated. Otherwise, the repository's HEAD will only be checked for updates when a new stable or development version has been released.")
            .Switch(new[] { "--keep-tmp" }, "Retain the temporary files created during installation.")
            .Switch(new[] { "--debug-symbols" }, "Generate debug symbols on build. Source will be retained in a cache directory.", dependsOn: "--build-from-source")
            .Switch(new[] { "--overwrite" }, "Delete files that already exist in the prefix while linking.")
            .Switch(new[] { "--cask", "--casks" }, "Treat all named arguments as casks. If no named arguments are specified, upgrade only outdated casks.")
            .Switch(new[] { "--skip-cask-deps" }, "Skip installing cask dependencies.")
            .Switch(new[] { "--no-quit" }, "Prevent running cask applications from being quit during upgrade. Enabled by default if `$HOMEBREW_NO_UPGRADE_QUIT_CASKS` is set.")
            .Switch(new[] { "-g", "--greedy" }, "Also include casks with `version :latest` and `auto_updates true` casks that would otherwise be skipped. Enabled by default if `$HOMEBREW_UPGRADE_GREEDY` is set.")
            .Switch(new[] { "--greedy-latest" }, "Also include casks with `version :latest`.")
            .Switch(new[] { "--greedy-auto-updates" }, "Also include `auto_updates true` casks that would otherwise be skipped.")
            .Switch(new[] { "--[no-]binaries" }, "Disable/enable linking of helper executables (default: enabled).")
            .Switch(new[] { "--require-sha" }, "Require all casks to have a checksum.")
            .Flag(new[] { "--appdir=" }, "Target location for Applications (default: `/Applications`).")
            .Flag(new[] { "--appimagedir=" }, "Target location for AppImages (default: `~/Applications`).")
            .Flag(new[] { "--keyboard-layoutdir=" }, "Target location for Keyboard Layouts (default: `/Library/Keyboard Layouts`).")
            .Flag(new[] { "--colorpickerdir=" }, "Target location for Color Pickers (default: `~/Library/ColorPickers`).")
            .Flag(new[] { "--prefpanedir=" }, "Target location for Preference Panes (default: `~/Library/PreferencePanes`).")
            .Flag(new[] { "--qlplugindir=" }, "Target location for Quick Look Plugins (default: `~/Library/QuickLook`).")
            .Flag(new[] { "--mdimporterdir=" }, "Target location for Spotlight Plugins (default: `~/Library/Spotlight`).")
            .Flag(new[] { "--dictionarydir=" }, "Target location for Dictionaries (default: `~/Library/Dictionaries`).")
            .Flag(new[] { "--fontdir=" }, "Target location for Fonts (default: `~/Library/Fonts`).")
            .Flag(new[] { "--servicedir=" }, "Target location for Services (default: `~/Library/Services`).")
            .Flag(new[] { "--input-methoddir=" }, "Target location for Input Methods (default: `~/Library/Input Methods`).")
            .Flag(new[] { "--internet-plugindir=" }, "Target location for Internet Plugins (default: `~/Library/Internet Plug-Ins`).")
            .Flag(new[] { "--audio-unit-plugindir=" }, "Target location for Audio Unit Plugins (default: `~/Library/Audio/Plug-Ins/Components`).")
            .Flag(new[] { "--vst-plugindir=" }, "Target location for VST Plugins (default: `~/Library/Audio/Plug-Ins/VST`).")
            .Flag(new[] { "--vst3-plugindir=" }, "Target location for VST3 Plugins (default: `~/Library/Audio/Plug-Ins/VST3`).")
            .Flag(new[] { "--screen-saverdir=" }, "Target location for Screen Savers (default: `~/Library/Screen Savers`).")
            .CommaArray("--language", "Comma-separated list of language codes to prefer for cask installation. The first matching language is used, otherwise it reverts to the cask's default language. The default value is the language of your system.")
            .Conflicts("--cask", "--formulae")
            .Conflicts("--cask", "--build-from-source")
            .Conflicts("--cask", "--interactive")
            .Conflicts("--cask", "--force-bottle")
            .Conflicts("--cask", "--fetch-HEAD")
            .Conflicts("--cask", "--keep-tmp")
            .Conflicts("--cask", "--debug-symbols")
            .Conflicts("--cask", "--overwrite")
            .Conflicts("--formula", "--casks")
            .Conflicts("--formula", "--skip-cask-deps")
            .Conflicts("--formula", "--no-quit")
            .Conflicts("--formula", "--greedy")
            .Conflicts("--formula", "--greedy-latest")
            .Conflicts("--formula", "--greedy-auto-updates")
            .Conflicts("--formula", "--[no-]binaries")
            .Conflicts("--formula", "--require-sha")
            .Conflicts("--formula", "--appdir=")
            .Conflicts("--formula", "--appimagedir=")
            .Conflicts("--formula", "--keyboard-layoutdir=")
            .Conflicts("--formula", "--colorpickerdir=")
            .Conflicts("--formula", "--prefpanedir=")
            .Conflicts("--formula", "--qlplugindir=")
            .Conflicts("--formula", "--mdimporterdir=")
            .Conflicts("--formula", "--dictionarydir=")
            .Conflicts("--formula", "--fontdir=")
            .Conflicts("--formula", "--servicedir=")
            .Conflicts("--formula", "--input-methoddir=")
            .Conflicts("--formula", "--internet-plugindir=")
            .Conflicts("--formula", "--audio-unit-plugindir=")
            .Conflicts("--formula", "--vst-plugindir=")
            .Conflicts("--formula", "--vst3-plugindir=")
            .Conflicts("--formula", "--screen-saverdir=")
            .Conflicts("--formula", "--language")
            .Conflicts("--build-from-source", "--force-bottle")
            .Conflicts("--ask", "--no-ask")
            .NamedArgs(new[] { "installed_formula", "installed_cask" }));
        Add("uses", () => new CommandParser("uses")
            .Description("""
                Show formulae and casks that specify <formula> as a dependency; that is, show dependents
                of <formula>. When given multiple formula arguments, show the intersection
                of formulae that use <formula>. By default, `uses` shows all formulae and casks that
                specify <formula> as a required or recommended dependency for their stable builds.

                *Note:* `--missing` and `--skip-recommended` have precedence over `--include-*`.
                """)
            .Switch(new[] { "--recursive" }, "Resolve more than one level of dependencies.")
            .Switch(new[] { "--installed" }, "Only list formulae and casks that are currently installed.")
            .Switch(new[] { "--missing" }, "Only list formulae and casks that are not currently installed.")
            .Switch(new[] { "--eval-all" }, null, hidden: true)
            .Switch(new[] { "--include-implicit" }, "Include formulae that have <formula> as an implicit dependency for downloading and unpacking source files.")
            .Switch(new[] { "--include-build" }, "Include formulae that specify <formula> as a `:build` dependency.")
            .Switch(new[] { "--include-test" }, "Include formulae that specify <formula> as a `:test` dependency.")
            .Switch(new[] { "--include-optional" }, "Include formulae that specify <formula> as an `:optional` dependency.")
            .Switch(new[] { "--skip-recommended" }, "Skip all formulae that specify <formula> as a `:recommended` dependency.")
            .Switch(new[] { "--formula", "--formulae" }, "Include only formulae.")
            .Switch(new[] { "--cask", "--casks" }, "Include only casks.")
            .Conflicts("--formula", "--cask")
            .Conflicts("--installed", "--eval-all")
            .Conflicts("--missing", "--installed")
            .NamedArgs("formula", min: 1));
        Add("vendor-install", () => new CommandParser("vendor-install")
            .Description("Install Homebrew's portable Ruby.")
            .NamedArgs("target"));
        Add("version-install", () => new CommandParser("version-install")
            .UsageBanner("""
                `version-install` <formula>[@<version>] [<version>]

                Extract a specific <version> of <formula> into a personal tap and install it.
                The default tap is <user>/versions.
                <user> uses the GitHub username if available and the local username otherwise.
                """)
            .NamedArgs(new[] { "formula", "version" }, min: 1, max: 2));
        Add("vulns", () => new CommandParser("vulns")
            .Description("""
                Check <formula> for known security vulnerabilities using the OSV.dev database.

                With no arguments, all installed formulae are checked.
                """)
            .Switch(new[] { "-d", "--deps" }, "Also check the dependencies of named formulae.")
            .Switch(new[] { "--no-ignore-patches" }, "Report vulnerabilities even when a formula patch resolves them.")
            .Flag(new[] { "--brewfile" }, "Check formulae listed in a Brewfile. Defaults to `./Brewfile`; use `--brewfile=`<path> to specify another.")
            .Switch(new[] { "--fix-available" }, "Only report vulnerabilities that have a released version fix available. Shortcut for `--fix-type=released`.")
            .Switch(new[] { "--no-fix-available" }, "Only report vulnerabilities that do not have a released version fix available (includes unreleased commit SHA patches). Shortcut for `--fix-type=unreleased`.")
            .Flag(new[] { "--fix-type=" }, "Filter findings by fix type: `released` (official version release), `patch` (unreleased commit SHA), `any` (either), `none` (neither), `unreleased` (no released version fix).")
            .Switch(new[] { "--list-skipped" }, "List packages skipped due to missing or unsupported source URL.")
            .Flag(new[] { "-s", "--severity=" }, "Only report findings at or above: `low`, `medium`, `high`, `critical`.")
            .Flag(new[] { "-m", "--max-summary=" }, "Truncate summaries to <n> characters (default 60, 0 for no limit).")
            .Switch(new[] { "-j", "--json" }, "Output JSON.")
            .Conflicts("--fix-available", "--no-fix-available", "--fix-type")
            .NamedArgs("formula"));
        Add("which-formula", () => new CommandParser("which-formula")
            .Description("Show which formula(e) provides the given command.")
            .Switch(new[] { "--explain" }, "Output explanation of how to get <command> by installing one of the providing formulae.")
            .NamedArgs("command", min: 1));
        Add("advisory-match", () => new CommandParser("advisory-match")
            .Description("""
                Match <formula> against OSV.dev (GIT, language-registry and distro
                ecosystems) and CPANSA to produce candidate `BREW-*` advisory records
                for <https://github.com/Homebrew/advisory-database>.

                This is authoring-time tooling for the advisory-database CI and the
                `homebrew-core` PR bot; use `brew vulns` to scan installed formulae.
                """)
            .Switch(new[] { "--all" }, "Match every formula in `homebrew/core`.")
            .Switch(new[] { "--index" }, "Emit the formula-identity index as JSON and exit.")
            .Switch(new[] { "--json" }, "Output candidate records as a JSON array.")
            .Flag(new[] { "--output=" }, "Write each record to <directory> as `BREW-<formula>-<id>.json`, preserving existing `published`/`ranges` fields.")
            .Flag(new[] { "--repology=" }, "Load the formula to distro-package index from <file> instead of the published `data/repology.json`.")
            .Flag(new[] { "--overrides=" }, "Load reviewed formula and advisory matching overrides from <file>.")
            .Flag(new[] { "--rejected-candidates=" }, "Skip new comparable candidates whose record IDs are listed in <file>, before history.", dependsOn: "--output=")
            .Switch(new[] { "--no-history" }, "Skip `FormulaVersions` walks for new ranges; use zero/current `pkg_version` as unverified boundaries.")
            .Switch(new[] { "--new-history" }, "Skip `FormulaVersions` for existing terminal ranges unless their matching provenance changes.", dependsOn: "--output=")
            .Switch(new[] { "--reconcile-history" }, "Reconcile existing matched terminal ranges against complete history; requires `--overrides`.", dependsOn: "--output=")
            .Flag(new[] { "--formula-list=" }, "Match only the core formula names in a newline-separated file with `--new-history` or `--reconcile-history`, using bulk queries.")
            .Conflicts("--formula-list", "--all")
            .Conflicts("--reconcile-history", "--new-history")
            .Conflicts("--reconcile-history", "--no-history")
            .Conflicts("--reconcile-history", "--json")
            .Conflicts("--reconcile-history", "--index")
            .Conflicts("--all", "--index")
            .Conflicts("--all", "--json")
            .Conflicts("--index", "--json")
            .Conflicts("--index", "--output")
            .Conflicts("--no-history", "--new-history")
            .NamedArgs("formula"));
        Add("benchmark", () => new CommandParser("benchmark")
            .Description("""
                Benchmark this `brew` with `hyperfine`, installing `hyperfine` first if it is
                missing. Each of the metadata-cold, archive-cold, archive-warm and fully-warm
                `brew install` workloads is measured separately, as are the metadata-cold,
                archive-cold and archive-warm `brew fetch` workloads for each of 1, 10, 50 and
                100 <formula>e that are available: pass 100 <formula>e for full coverage and
                fewer for a shorter run.

                The first <formula> is used for the install workloads: its dependencies are
                installed once up front and left behind. All <formula>e must initially be
                uninstalled and the archive-cold workloads re-download every bottle in the
                batch. Mean wall times and `$HOMEBREW_PHASE_TIMINGS` phase totals are printed
                and written to `benchmark/results.json`.
                """)
            .Switch(new[] { "--exec" }, "Run `hyperfine` with the arguments given after `--` instead of Homebrew's own workloads, e.g. `brew benchmark --exec -- 'brew --version'`.")
            .Flag(new[] { "--runs=" }, "Number of repetitions of each workload. Defaults to 3.")
            .Conflicts("--exec", "--runs")
            .NamedArgs("formula", min: 1));
        Add("bottle", () => new CommandParser("bottle")
            .Description("""
                Generate a bottle (binary package) from a formula that was installed with
                `--build-bottle`.
                If the formula specifies a rebuild version, it will be incremented in the
                generated DSL. Passing `--keep-old` will attempt to keep it at its original
                value, while `--no-rebuild` will remove it.
                """)
            .Switch(new[] { "--skip-relocation" }, "Do not check if the bottle can be marked as relocatable.")
            .Switch(new[] { "--force-core-tap" }, "Build a bottle even if <formula> is not in `homebrew/core` or any installed taps.")
            .Switch(new[] { "--no-rebuild" }, "If the formula specifies a rebuild version, remove it from the generated DSL.")
            .Switch(new[] { "--keep-old" }, "If the formula specifies a rebuild version, attempt to preserve its value in the generated DSL.")
            .Switch(new[] { "--json" }, "Write bottle information to a JSON file, which can be used as the value for `--merge`.")
            .Switch(new[] { "--merge" }, "Generate an updated bottle block for a formula and optionally merge it into the formula file. Instead of a formula name, requires the path to a JSON file generated with `brew bottle --json` <formula>.")
            .Switch(new[] { "--write" }, "Write changes to the formula file. A new commit will be generated unless `--no-commit` is passed.", dependsOn: "--merge")
            .Switch(new[] { "--no-commit" }, "When passed with `--write`, a new commit will not generated after writing changes to the formula file.", dependsOn: "--write")
            .Switch(new[] { "--only-json-tab" }, "When passed with `--json`, the tab will be written to the JSON file but not the bottle.", dependsOn: "--json")
            .Switch(new[] { "--no-all-checks" }, "Don't try to create an `all` bottle or stop a no-change upload.", dependsOn: "--merge")
            .Flag(new[] { "--committer=" }, null, hidden: true)
            .Flag(new[] { "--root-url=" }, "Use the specified <URL> as the root of the bottle's URL instead of Homebrew's default.")
            .Flag(new[] { "--root-url-using=" }, "Use the specified download strategy class for downloading the bottle's URL instead of Homebrew's default.")
            .Conflicts("--no-rebuild", "--keep-old")
            .NamedArgs(new[] { "installed_formula", "file" }, min: 1));
        Add("bump-cask-pr", () => new CommandParser("bump-cask-pr")
            .Description("""
                Create a pull request to update <cask> with a new version.

                A best effort to determine the <SHA-256> will be made if the value is not
                supplied by the user.
                """)
            .Switch(new[] { "-n", "--dry-run" }, "Print what would be done rather than doing it.")
            .Switch(new[] { "--write-only" }, "Make the expected file modifications without taking any Git actions.")
            .Switch(new[] { "--commit" }, "When passed with `--write-only`, generate a new commit after writing changes to the cask file.", dependsOn: "--write-only")
            .Switch(new[] { "--no-audit" }, "Don't run `brew audit` before opening the PR.")
            .Switch(new[] { "--no-style" }, "Don't run `brew style --fix` before opening the PR.")
            .Switch(new[] { "--no-browse" }, "Print the pull request URL instead of opening in a browser.")
            .Switch(new[] { "--no-fork" }, "Don't try to fork the repository.")
            .Flag(new[] { "--version=" }, "Specify the new <version> for the cask.")
            .Flag(new[] { "--version-arm=" }, "Specify the new cask <version> for macOS on ARM.")
            .Flag(new[] { "--version-intel=" }, "Specify the new cask <version> for macOS on Intel.")
            .Flag(new[] { "--version-linux-arm=" }, "Specify the new cask <version> for Linux on ARM.")
            .Flag(new[] { "--version-linux-intel=" }, "Specify the new cask <version> for Linux on Intel.")
            .Flag(new[] { "--message=" }, "Prepend <message> to the default pull request message.")
            .Flag(new[] { "--url=" }, "Specify the <URL> for the new download.")
            .Flag(new[] { "--sha256=" }, "Specify the <SHA-256> checksum of the new download.")
            .Flag(new[] { "--fork-org=" }, "Use the specified GitHub organisation for forking.")
            .Conflicts("--dry-run", "--write")
            .Conflicts("--version", "--version-arm")
            .Conflicts("--version", "--version-intel")
            .Conflicts("--version", "--version-linux-arm")
            .Conflicts("--version", "--version-linux-intel")
            .NamedArgs("cask", number: 1));
        Add("bump-compatibility-version", () => new CommandParser("bump-compatibility-version")
            .Description("""
                Create a commit to increment the compatibility_version of <formula>. If no
                compatibility_version is present, "compatibility_version 1" will be added.
                """)
            .Switch(new[] { "-n", "--dry-run" }, "Print what would be done rather than doing it.")
            .Switch(new[] { "--write-only" }, "Make the expected file modifications without taking any Git actions.")
            .Flag(new[] { "--message=" }, "Append <message> to the default commit message.")
            .Conflicts("--dry-run", "--write-only")
            .NamedArgs("formula", min: 1));
        Add("bump-python-resources-pr", () => new CommandParser("bump-python-resources-pr")
            .Description("Update vulnerable PyPI resources in <formula>, bump its revision, and create a pull request.")
            .Switch(new[] { "-n", "--dry-run" }, "Print what would be done rather than creating a pull request.")
            .Switch(new[] { "--no-fork" }, "Don't try to fork the repository.")
            .Switch(new[] { "--install-dependencies" }, "Install missing dependencies required to update resources.")
            .CommaArray("--packages", "Names of vulnerable Python packages that must be updated.")
            .Flag(new[] { "--branch=" }, "Branch name to use for the pull request.")
            .Flag(new[] { "--message=" }, "Message to prepend to the pull request body.")
            .Flag(new[] { "--output=" }, "Write the JSON result to this file instead of standard output. Use this when parsing the result, as progress is also printed to standard output.")
            .NamedArgs("formula", number: 1));
        Add("bump-unversioned-casks", () => new CommandParser("bump-unversioned-casks")
            .Description("Check all casks with unversioned URLs in a given <tap> for updates.")
            .Switch(new[] { "-n", "--dry-run" }, "Do everything except caching state and opening pull requests.")
            .Flag(new[] { "--limit=" }, "Maximum runtime in minutes.")
            .Flag(new[] { "--state-file=" }, "File for caching state.")
            .NamedArgs(new[] { "cask", "tap" }, min: 1));
        Add("cask-ci", () => new CommandParser("cask-ci")
            .UsageBanner("""
                `cask-ci` <operation> [<cask>]

                Run Homebrew Cask's internal CI helpers.
                For internal use in Homebrew taps.
                """)
            .NamedArgs((string[])null, min: 1, max: 2));
        Add("contributions", () => new CommandParser("contributions")
            .UsageBanner("""
                `contributions` [`--user=`] [`--repositories=`] [`--quarter=`] [`--from=`] [`--to=`] [`--csv`] [`--maintainer-report-csv`]

                Summarise contributions to Homebrew repositories.
                """)
            .CommaArray("--user", "Specify a comma-separated list of GitHub usernames or email addresses to find contributions from. Omitting this flag searches Homebrew maintainers and requires access to the `Homebrew/maintainers` team. With `--maintainer-report-csv`, only matching period-end Maintainers are included.")
            .CommaArray("--repositories", "Specify a comma-separated list of repositories to search. All repositories must be under the same user or organisation. Omitting this flag, or specifying `--repositories=primary`, searches only the main repositories: `Homebrew/brew`, `Homebrew/homebrew-core`, `Homebrew/homebrew-cask`.")
            .Flag(new[] { "--organisation=", "--organization=", "--org=" }, "Specify the organisation to populate sources repositories from. Omitting this flag searches the Homebrew primary repositories.")
            .Flag(new[] { "--team=" }, "Specify the team to populate users from. The first part of the team name will be used as the organisation.")
            .Flag(new[] { "--quarter=" }, "Homebrew contributions quarter to search (1-4). Omitting this flag searches the past year. If `--from` or `--to` are set, they take precedence.")
            .Flag(new[] { "--from=" }, "Date (ISO 8601 format) to start searching contributions. Omitting this flag searches the past year.")
            .Flag(new[] { "--to=" }, "Date (ISO 8601 format) to stop searching contributions.")
            .Switch(new[] { "--csv" }, "Print a CSV of contributions across repositories over the time period.")
            .Flag(new[] { "--maintainer-report-csv" }, "Print a CSV of Maintainer and Lead Maintainer activity criteria using fetched Git histories and GitHub's existing approved-review search for the Homebrew governance quarter, for example `--maintainer-report-csv=2026-2`. Alternatively, omit the value and set `--from` and `--to` for any reporting period. Also write it in the current directory as `brew-contributions-FROM-to-TO.csv`, or `brew-contributions-FROM-to-TO-USER.csv` when filtered with `--user`. Only Maintainers listed at the end of that period are included. Activity checks use the latest completed governance quarter; role recommendations use up to four full quarters of reports. Review searches return at most 100 results and other counts are capped at 500 per repository and contribution type. Repository-scoped follow-up searches ensure role activity checks remain accurate when a count is capped. Completed-period GitHub searches are cached in Homebrew's cache and removed by normal cache pruning. `YEAR-1` is December of the previous year through February, `YEAR-2` is March through May, `YEAR-3` is June through August and `YEAR-4` is September through November.")
            .Conflicts("--organisation", "--repositories")
            .Conflicts("--organisation", "--team")
            .Conflicts("--user", "--team")
            .Conflicts("--maintainer-report-csv", "--repositories")
            .Conflicts("--maintainer-report-csv", "--organisation")
            .Conflicts("--maintainer-report-csv", "--team")
            .Conflicts("--maintainer-report-csv", "--quarter"));
        Add("debugger", () => new CommandParser("debugger")
            .Description("""
                Run the specified Homebrew command in debug mode.

                To pass flags to the command, use `--` to separate them from the `brew` flags.
                For example: `brew debugger -- list --formula`.
                """)
            .Switch(new[] { "-O", "--open" }, "Start remote debugging over a Unix socket.")
            .NamedArgs("command", min: 1));
        Add("determine-test-runners", () => new CommandParser("determine-test-runners")
            .UsageBanner("""
                `determine-test-runners` {<testing-formulae> [<deleted-formulae>]|--all-supported}

                Determines the runners used to test formulae or their dependents. For internal use in Homebrew taps.
                """)
            .Switch(new[] { "--all-supported" }, "Instead of selecting runners based on the chosen formula, return all supported runners.")
            .Switch(new[] { "--eval-all" }, null, hidden: true)
            .Switch(new[] { "--dependents" }, "Determine runners for testing dependents.")
            .Flag(new[] { "--dependent-shards=" }, null, hidden: true, dependsOn: "--dependents")
            .Conflicts("--all-supported", "--dependents")
            .NamedArgs((string[])null, max: 2));
        Add("dispatch-build-bottle", () => new CommandParser("dispatch-build-bottle")
            .Description("Build bottles for these formulae with GitHub Actions.")
            .Flag(new[] { "--tap=" }, "Target tap repository (default: `homebrew/core`).")
            .Flag(new[] { "--timeout=" }, "Build timeout (in minutes, default: 60).")
            .Flag(new[] { "--issue=" }, "If specified, post a comment to this issue number if the job fails.")
            .CommaArray("--macos", "macOS version (or comma-separated list of versions) the bottle should be built for.")
            .Flag(new[] { "--workflow=" }, "Dispatch specified workflow (default: `dispatch-build-bottle.yml`).")
            .Switch(new[] { "--upload" }, "Upload built bottles.")
            .Switch(new[] { "--linux" }, "Dispatch bottle for Linux x86_64 (using GitHub runners).")
            .Switch(new[] { "--linux-arm64" }, "Dispatch bottle for Linux arm64 (using GitHub runners).")
            .Switch(new[] { "--linux-self-hosted" }, "Dispatch bottle for Linux x86_64 (using self-hosted runner).")
            .Conflicts("--linux", "--linux-self-hosted")
            .NamedArgs("formula", min: 1));
        Add("find-appcast", () => new CommandParser("find-appcast")
            .Description("""
                Find the appcast of the app bundle at <app_path>, for use in a cask
                `livecheck` block.

                Checks for a Sparkle `SUFeedURL` and Electron Builder update metadata.
                """)
            .NamedArgs("app_path", number: 1));
        Add("formula-analytics", () => new CommandParser("formula-analytics")
            .UsageBanner("""
                `formula-analytics`

                Query Homebrew's analytics.
                """)
            .Flag(new[] { "--days-ago=" }, "Query from the specified days ago until the present. The default is 30 days.")
            .Switch(new[] { "--install" }, "Output the number of specifically requested installations or installation as dependencies of formulae. This is the default.")
            .Switch(new[] { "--install-on-request" }, "Output the number of specifically requested installations of formulae.")
            .Switch(new[] { "--cask-install" }, "Output the number of installations of casks.")
            .Switch(new[] { "--build-error" }, "Output the number of build errors for formulae.")
            .Switch(new[] { "--os-version" }, "Output the number of events by OS name and version.")
            .Switch(new[] { "--homebrew-devcmdrun-developer" }, "Output the number of devcmdrun/HOMEBREW_DEVELOPER events.")
            .Switch(new[] { "--homebrew-env-config" }, "Output rates of non-default Homebrew environment configuration variables.")
            .Switch(new[] { "--homebrew-os-arch-ci" }, "Output the number of OS/Architecture/CI events.")
            .Switch(new[] { "--homebrew-prefixes" }, "Output Homebrew prefixes.")
            .Switch(new[] { "--homebrew-versions" }, "Output Homebrew versions.")
            .Switch(new[] { "--brew-command-run" }, "Output `brew` commands run.")
            .Switch(new[] { "--brew-command-run-options" }, "Output `brew` commands run with options.")
            .Switch(new[] { "--brew-test-bot-test" }, "Output `brew test-bot` steps run.")
            .Switch(new[] { "--json" }, "Output JSON. This is required: plain text support has been removed.")
            .Switch(new[] { "--all-core-formulae-json" }, "Output a different JSON format containing the JSON data for all Homebrew/homebrew-core formulae.")
            .Switch(new[] { "--setup" }, "Install the necessary Python dependencies and exit without running a query.")
            .Conflicts("--install", "--cask-install", "--install-on-request", "--build-error", "--os-version", "--homebrew-devcmdrun-developer", "--homebrew-env-config", "--homebrew-os-arch-ci", "--homebrew-prefixes", "--homebrew-versions", "--brew-command-run", "--brew-command-run-options", "--brew-test-bot-test")
            .Conflicts("--json", "--all-core-formulae-json", "--setup")
            .NamedArgs("none"));
        Add("formula-python-resources", () => new CommandParser("formula-python-resources")
            .Description("Print JSON describing the PyPI resources used by <formula>.")
            .Switch(new[] { "--all" }, "Include all available formulae.")
            .Flag(new[] { "--tap=" }, "Only include formulae from the named tap. Requires `--all`.")
            .NamedArgs("formula"));
        Add("generate-advisories-api", () => new CommandParser("generate-advisories-api")
            .Description("""
                Generate advisory API data for <https://formulae.brew.sh> from a checkout of
                <https://github.com/Homebrew/advisory-database>.
                The generated file is written to the current directory.
                """)
            .NamedArgs("directory", number: 1));
        Add("generate-analytics-api", () => new CommandParser("generate-analytics-api")
            .Description("""
                Generates analytics API data files for <https://formulae.brew.sh>.
                The generated files are written to the current directory.
                """)
            .NamedArgs("none"));
        Add("generate-bottle-ci-matrix", () => new CommandParser("generate-bottle-ci-matrix")
            .Description("""
                Generate a GitHub Actions runner matrix for a dispatched bottle build.
                For internal use in Homebrew taps.
                """)
            .CommaArray("--runners", "Build runner names as a comma-separated list.")
            .NamedArgs("formula", number: 1));
        Add("generate-cask-api", () => new CommandParser("generate-cask-api")
            .Description("""
                Generate `homebrew/cask` API data files for <https://formulae.brew.sh>.
                The generated files are written to the current directory.
                """)
            .Switch(new[] { "-n", "--dry-run" }, "Generate API data without writing it to files.")
            .NamedArgs("none"));
        Add("generate-cask-ci-matrix", () => new CommandParser("generate-cask-ci-matrix")
            .Description("""
                Generate a GitHub Actions matrix for a given pull request URL or list of cask names.
                For internal use in Homebrew taps.
                """)
            .Switch(new[] { "--url" }, "Treat named argument as a pull request URL.")
            .Switch(new[] { "--cask", "--casks" }, "Treat all named arguments as cask tokens.")
            .Switch(new[] { "--skip-install" }, "Skip installing casks.")
            .Switch(new[] { "--new" }, "Run new cask checks.")
            .Switch(new[] { "--syntax-only" }, "Only run syntax checks.")
            .Conflicts("--url", "--cask")
            .Conflicts("--syntax-only", "--skip-install")
            .Conflicts("--syntax-only", "--new")
            .NamedArgs(new[] { "cask", "url" }, min: 0));
        Add("generate-cask-token", () => new CommandParser("generate-cask-token")
            .Description("""
                Generate a cask token, filename and header line for an application,
                following the token conventions described in the Cask Cookbook.

                The argument may be either a path to an application bundle
                (e.g. `/Applications/Example App.app`) or the vendor's name for the
                software (e.g. `Example App`).
                """)
            .NamedArgs("app_or_name", number: 1));
        Add("generate-formula-api", () => new CommandParser("generate-formula-api")
            .Description("""
                Generate `homebrew/core` API data files for <https://formulae.brew.sh>.
                The generated files are written to the current directory.
                """)
            .Switch(new[] { "-n", "--dry-run" }, "Generate API data without writing it to files.")
            .NamedArgs("none"));
        Add("generate-internal-api", () => new CommandParser("generate-internal-api")
            .Description("""
                Generate internal API data files for <https://formulae.brew.sh>.
                The generated files are written to the current directory.
                """)
            .Switch(new[] { "-n", "--dry-run" }, "Generate internal API data without writing it to files.")
            .NamedArgs("none"));
        Add("generate-man-completions", () => new CommandParser("generate-man-completions")
            .Description("Generate Homebrew's manpages and shell completions.")
            .Switch(new[] { "--no-exit-code" }, "Exit with code 0 even if no changes were made.")
            .NamedArgs("none"));
        Add("generate-vulns-advisories", () => new CommandParser("generate-vulns-advisories")
            .Description("""
                Generate OSV-schema advisory records for the `Homebrew` ecosystem from
                `homebrew/core` formula patch `resolves` annotations, for
                <https://github.com/Homebrew/advisory-database>.

                Records are written to <directory>.
                """)
            .Switch(new[] { "-n", "--dry-run" }, "List the records that would be generated without writing files or querying OSV.dev.")
            .NamedArgs("directory", number: 1));
        Add("generate-zap", () => new CommandParser("generate-zap")
            .Description("""
                Generate a `zap` stanza for a cask by scanning the system for associated
                files and directories.

                Accepts a cask token (e.g. `firefox`) or, with `--name`, a raw application
                name string (e.g. `Firefox`). When a cask token is given, the application
                name is resolved from the cask's `app` artifact.

                The target application should have been launched at least once so that
                preference files and caches exist on disk.

                Outputs `trash`, `delete`, and `rmdir` directives suitable for pasting
                into a cask definition.
                """)
            .Switch(new[] { "--name" }, "Treat the argument as a raw application name instead of a cask token.")
            .NamedArgs("cask_or_name", number: 1));
        Add("install-bundler-gems", () => new CommandParser("install-bundler-gems")
            .Description("Install Homebrew's Bundler gems.")
            .CommaArray("--groups", "Installs the specified comma-separated list of gem groups (default: last used). Replaces any previously installed groups.")
            .CommaArray("--add-groups", "Installs the specified comma-separated list of gem groups, in addition to those already installed.")
            .Conflicts("--groups", "--add-groups")
            .NamedArgs("none"));
        Add("lgtm", () => new CommandParser("lgtm")
            .Description("""
                Run `brew typecheck`, `brew style --changed` and the relevant `brew tests`,
                `brew audit` and `brew test` checks in one go.
                """)
            .Switch(new[] { "--online" }, "Run additional, slower checks that require a network connection.")
            .NamedArgs("none"));
        Add("pr-automerge", () => new CommandParser("pr-automerge")
            .Description("Find pull requests that can be automatically merged using `brew pr-publish`.")
            .Flag(new[] { "--tap=" }, "Target tap repository (default: `homebrew/core`).")
            .Flag(new[] { "--workflow=" }, "Workflow file to use with `brew pr-publish`.")
            .Flag(new[] { "--with-label=" }, "Pull requests must have this label.")
            .CommaArray("--without-labels", "Pull requests must not have these labels (default: `do not merge`, `new formula`, `automerge-skip`, `pre-release`, `CI-published-bottle-commits`).")
            .Switch(new[] { "--without-approval" }, "Pull requests do not require approval to be merged.")
            .Switch(new[] { "--publish" }, "Run `brew pr-publish` on matching pull requests.")
            .Switch(new[] { "--autosquash" }, "Instruct `brew pr-publish` to automatically reformat and reword commits in the pull request to the preferred format.")
            .Switch(new[] { "--ignore-failures" }, "Include pull requests that have failing status checks.")
            .NamedArgs("none"));
        Add("pr-publish", () => new CommandParser("pr-publish")
            .Description("""
                Publish bottles for a pull request with GitHub Actions.
                Requires write access to the repository.
                """)
            .Switch(new[] { "--autosquash" }, "If supported on the target tap, automatically reformat and reword commits to our preferred format.")
            .Switch(new[] { "--warn-on-upload-failure" }, null, hidden: true)
            .Flag(new[] { "--branch=" }, "Branch to use the workflow from (default: `main`).")
            .Flag(new[] { "--message=" }, "Message to include when autosquashing revision bumps, deletions and rebuilds.", dependsOn: "--autosquash")
            .Flag(new[] { "--tap=" }, "Target tap repository (default: `homebrew/core`).")
            .Flag(new[] { "--workflow=" }, "Target workflow filename (default: `publish-commit-bottles.yml`).")
            .NamedArgs("pull_request", min: 1));
        Add("pr-pull", () => new CommandParser("pr-pull")
            .Description("""
                Download and publish bottles and apply the bottle commit from a
                pull request with artifacts generated by GitHub Actions.
                Requires write access to the repository.
                """)
            .Switch(new[] { "--no-upload" }, "Download the bottles but don't upload them.")
            .Switch(new[] { "--no-commit" }, "Do not generate a new commit before uploading.")
            .Switch(new[] { "--no-cherry-pick" }, "Do not cherry-pick commits from the pull request branch.")
            .Switch(new[] { "-n", "--dry-run" }, "Print what would be done rather than doing it.")
            .Switch(new[] { "--clean" }, "Do not amend the commits from pull requests.")
            .Switch(new[] { "--keep-old" }, "If the formula specifies a rebuild version, attempt to preserve its value in the generated DSL.")
            .Switch(new[] { "--autosquash" }, "Automatically reformat and reword commits in the pull request to our preferred format.")
            .Switch(new[] { "--branch-okay" }, "Do not warn if pulling to a branch besides the repository default (useful for testing).")
            .Switch(new[] { "--resolve" }, "When a patch fails to apply, leave in progress and allow user to resolve, instead of aborting.")
            .Switch(new[] { "--warn-on-upload-failure" }, "Warn instead of raising an error if the bottle upload fails. Useful for repairing bottle uploads that previously failed.")
            .Switch(new[] { "--retain-bottle-dir" }, "Does not clean up the tmp directory for the bottle so it can be used later.")
            .Flag(new[] { "--committer=" }, null, hidden: true)
            .Flag(new[] { "--message=" }, "Message to include when autosquashing revision bumps, deletions and rebuilds.", dependsOn: "--autosquash")
            .Flag(new[] { "--artifact-pattern=", "--artifact=" }, "Download artifacts with the specified pattern (default: `bottles{,_*}`).")
            .Flag(new[] { "--tap=" }, "Target tap repository (default: `homebrew/core`).")
            .Flag(new[] { "--head-sha=" }, "Expected pull request head commit SHA.")
            .Flag(new[] { "--root-url=" }, "Use the specified <URL> as the root of the bottle's URL instead of Homebrew's default.")
            .Flag(new[] { "--root-url-using=" }, "Use the specified download strategy class for downloading the bottle's URL instead of Homebrew's default.")
            .CommaArray("--workflows", "Retrieve artifacts from the specified workflow (default: `tests.yml`). Can be a comma-separated list to include multiple workflows.")
            .CommaArray("--ignore-missing-artifacts", "Comma-separated list of workflows which can be ignored if they have not been run.")
            .Conflicts("--clean", "--autosquash")
            .NamedArgs("pull_request", min: 1));
        Add("pr-upload", () => new CommandParser("pr-upload")
            .Description("Apply the bottle commit and publish bottles to a host.")
            .Switch(new[] { "--keep-old" }, "If the formula specifies a rebuild version, attempt to preserve its value in the generated DSL. When using GitHub Packages, this also appends the manifest to the existing list.")
            .Switch(new[] { "-n", "--dry-run" }, "Print what would be done rather than doing it.")
            .Switch(new[] { "--no-commit" }, "Do not generate a new commit before uploading.")
            .Switch(new[] { "--warn-on-upload-failure" }, "Warn instead of raising an error if the bottle upload fails. Useful for repairing bottle uploads that previously failed.")
            .Switch(new[] { "--upload-only" }, "Skip running `brew bottle` before uploading.")
            .Flag(new[] { "--committer=" }, null, hidden: true)
            .Flag(new[] { "--root-url=" }, "Use the specified <URL> as the root of the bottle's URL instead of Homebrew's default.")
            .Flag(new[] { "--root-url-using=" }, "Use the specified download strategy class for downloading the bottle's URL instead of Homebrew's default.")
            .Conflicts("--upload-only", "--no-commit")
            .NamedArgs("none"));
        Add("prof", () => new CommandParser("prof")
            .Description("Run Homebrew with a Ruby profiler. For example, `brew prof readall`.")
            .Switch(new[] { "--stackprof" }, "Use `stackprof` instead of `ruby-prof` (the default).")
            .Switch(new[] { "--vernier" }, "Use `vernier` instead of `ruby-prof` (the default).")
            .Switch(new[] { "--timings" }, "Record machine-readable timings for Homebrew command phases.")
            .Conflicts("--timings", "--stackprof")
            .Conflicts("--timings", "--vernier")
            .NamedArgs("command", min: 1));
        Add("release", () => new CommandParser("release")
            .Description("""
                Create a new draft Homebrew/brew release with the appropriate version number and release notes.

                By default, `brew release` will bump the patch version number. Pass
                `--major` or `--minor` to bump the major or minor version numbers, respectively.
                The command will fail if the previous major or minor release was made less than
                one month ago.

                Without `--force`, this command will just output the release notes without creating
                the release or triggering the workflow.

                *Note:* Requires write access to the Homebrew/brew repository.
                """)
            .Switch(new[] { "--major" }, "Create a major release.")
            .Switch(new[] { "--minor" }, "Create a minor release.")
            .Switch(new[] { "--force" }, "Actually create the release and trigger the workflow. Without this, just show what would be done.")
            .Conflicts("--major", "--minor")
            .NamedArgs("none"));
        Add("rubocop", () => new CommandParser("rubocop")
            .Description("Installs, configures and runs Homebrew's `rubocop`."));
        Add("rubydoc", () => new CommandParser("rubydoc")
            .Description("Generate Homebrew's RubyDoc documentation.")
            .Switch(new[] { "--only-public" }, "Only generate public API documentation.")
            .Switch(new[] { "--open" }, "Open generated documentation in a browser."));
        Add("tap-new", () => new CommandParser("tap-new")
            .UsageBanner("""
                `tap-new` [<options>] <user>`/`<repo>

                Generate the template files for a new tap.
                """)
            .Switch(new[] { "--no-git" }, "Don't initialise a Git repository for the tap.")
            .Flag(new[] { "--pull-label=" }, null, hidden: true)
            .Flag(new[] { "--branch=" }, "Initialise a Git repository and set up GitHub Actions workflows with the specified branch name (default: `main`).")
            .Switch(new[] { "--github-packages" }, "Upload bottles to GitHub Packages.")
            .NamedArgs("tap", number: 1));
        Add("test-bot", () => new CommandParser("test-bot")
            .UsageBanner("""
                `test-bot` [<options>] [<formula>]

                Tests the full lifecycle of a Homebrew change to a tap (Git repository). For example, for a GitHub Actions pull request that changes a formula `brew test-bot` will ensure the system is cleaned and set up to test the formula, install the formula, run various tests and checks on it, bottle (package) the binaries and test formulae that depend on it to ensure they aren't broken by these changes.

                Only supports GitHub Actions as a CI provider. This is because Homebrew uses GitHub Actions and it's freely available for public and private use with macOS and Linux workers.
                """)
            .Switch(new[] { "--dry-run" }, "Print what would be done rather than doing it.")
            .Switch(new[] { "--cleanup" }, "Clean all state from the Homebrew directory. Use with care!")
            .Switch(new[] { "--skip-setup" }, "Don't check if the local system is set up correctly.")
            .Switch(new[] { "--build-from-source" }, "Build from source rather than building bottles.")
            .Switch(new[] { "--build-dependents-from-source" }, "Build a limited set of dependents from source in addition to testing bottles. Up to 10 per formula per shard, prioritising popular dependents in a sharded group.")
            .Switch(new[] { "--junit" }, "generate a JUnit XML test results file.")
            .Switch(new[] { "--keep-old" }, "Run `brew bottle --keep-old` to build new bottles for a single platform.")
            .Switch(new[] { "--skip-relocation" }, "Run `brew bottle --skip-relocation` to build new bottles that don't require relocation.")
            .Switch(new[] { "--only-json-tab" }, "Run `brew bottle --only-json-tab` to build new bottles that do not contain a tab.")
            .Switch(new[] { "--local" }, "Ask Homebrew to write verbose logs under `./logs/` and set `$HOME` to `./home/`")
            .Flag(new[] { "--tap=" }, "Use the Git repository of the given tap. Defaults to the core tap for syntax checking.")
            .Switch(new[] { "--fail-fast" }, "Immediately exit on a failing step.")
            .Switch(new[] { "-v", "--verbose" }, "Print test step output in real time. Has the side effect of passing output as raw bytes instead of re-encoding in UTF-8.")
            .Switch(new[] { "--test-default-formula" }, "Use a default testing formula when not building a tap and no other formulae are specified.")
            .Flag(new[] { "--root-url=" }, "Use the specified <URL> as the root of the bottle's URL instead of Homebrew's default.")
            .Flag(new[] { "--git-name=" }, "Set the Git author/committer names to the given name.")
            .Flag(new[] { "--git-email=" }, "Set the Git author/committer email to the given email.")
            .Switch(new[] { "--publish" }, "Publish the uploaded bottles.")
            .Switch(new[] { "--skip-online-checks" }, "Don't pass `--online` to `brew audit` and skip `brew livecheck`.")
            .Switch(new[] { "--skip-new" }, "Don't pass `--new` to `brew audit` for new formulae.")
            .Switch(new[] { "--skip-new-strict" }, "Don't pass `--strict` to `brew audit` for new formulae.", dependsOn: "--skip-new")
            .Switch(new[] { "--skip-dependents" }, "Don't test any dependents.")
            .Switch(new[] { "--skip-livecheck" }, "Don't test livecheck.")
            .Switch(new[] { "--[no-]skip-recursive-dependents" }, null, hidden: true)
            .Switch(new[] { "--skip-checksum-only-audit" }, "Don't audit checksum-only changes.")
            .Switch(new[] { "--skip-stable-version-audit" }, "Don't audit the stable version.")
            .Switch(new[] { "--skip-revision-audit" }, "Don't audit the revision.")
            .Switch(new[] { "--only-cleanup-before" }, "Only run the pre-cleanup step. Needs `--cleanup`, except in GitHub Actions.")
            .Switch(new[] { "--only-setup" }, "Only run the local system setup check step.")
            .Switch(new[] { "--only-tap-syntax" }, "Only run the tap syntax check step.")
            .Switch(new[] { "--stable" }, "Only run the tap syntax checks needed on stable brew.", dependsOn: "--only-tap-syntax")
            .Switch(new[] { "--only-formulae" }, "Only run the formulae steps.")
            .Switch(new[] { "--only-formulae-detect" }, "Only run the formulae detection steps.")
            .Switch(new[] { "--only-formulae-dependents" }, "Only run the formulae dependents steps.")
            .Switch(new[] { "--only-bottles-fetch" }, "Only run the bottles fetch steps. This optional post-upload test checks that all the bottles were uploaded correctly. It is not run unless requested and only needs to be run on a single machine. The bottle commit to be tested must be on the tested branch.")
            .Switch(new[] { "--only-cleanup-after" }, "Only run the post-cleanup step. Needs `--cleanup`, except in GitHub Actions.")
            .CommaArray("--testing-formulae", "Use these testing formulae rather than running the formulae detection steps.")
            .CommaArray("--added-formulae", "Use these added formulae rather than running the formulae detection steps.")
            .CommaArray("--deleted-formulae", "Use these deleted formulae rather than running the formulae detection steps.")
            .CommaArray("--skipped-or-failed-formulae", "Use these skipped or failed formulae from formulae steps for a formulae dependents step.")
            .CommaArray("--tested-formulae", "Use these tested formulae from formulae steps for a formulae dependents step.")
            .Switch(new[] { "--print-padded-prefix" }, null, hidden: true)
            .Flag(new[] { "--formulae-dependents-shard=" }, null, hidden: true)
            .Conflicts("--only-formulae-detect", "--testing-formulae")
            .Conflicts("--only-formulae-detect", "--added-formulae")
            .Conflicts("--only-formulae-detect", "--deleted-formulae")
            .Conflicts("--skip-dependents", "--only-formulae-dependents")
            .Conflicts("--only-cleanup-before", "--only-setup", "--only-tap-syntax", "--only-formulae", "--only-formulae-detect", "--only-formulae-dependents", "--only-cleanup-after", "--skip-setup"));
        Add("tests", () => new CommandParser("tests")
            .Description("Run Homebrew's unit and integration tests.")
            .Switch(new[] { "--coverage" }, "Generate code coverage reports.")
            .Switch(new[] { "--generic" }, "Run only OS-agnostic tests.")
            .Switch(new[] { "--online" }, "Include tests that use the GitHub API and tests that use any of the taps for official external commands.")
            .Switch(new[] { "--debug" }, "Enable debugging using `ruby/debug`, or surface the standard `odebug` output.")
            .Switch(new[] { "--changed" }, "Only runs tests on files that were changed from the `main` branch.")
            .Switch(new[] { "--fail-fast" }, "Exit early on the first failing test.")
            .Switch(new[] { "--load-only" }, "Load each test file independently without running its examples.")
            .Switch(new[] { "--no-parallel" }, "Run tests serially.")
            .Switch(new[] { "--stackprof" }, "Use `stackprof` to profile tests.")
            .Switch(new[] { "--vernier" }, "Use `vernier` to profile tests.")
            .Switch(new[] { "--ruby-prof" }, "Use `ruby-prof` to profile tests.")
            .Flag(new[] { "--only=" }, "Run only `<test_script>_spec.rb`. Appending `:<line_number>` will start at a specific line.")
            .Flag(new[] { "--shard=" }, "Run only `<index>` of `<total>` test shards.")
            .Flag(new[] { "--profile=" }, "Output the <n> slowest tests. When run without `--no-parallel` this will output the slowest tests for each parallel test process.")
            .Flag(new[] { "--seed=" }, "Randomise tests with the specified <value> instead of a random seed.")
            .Conflicts("--changed", "--only", "--shard")
            .Conflicts("--stackprof", "--vernier", "--ruby-prof")
            .NamedArgs("none"));
        Add("typecheck", () => new CommandParser("typecheck", "tc")
            .Description("Check for typechecking errors using Sorbet.")
            .Switch(new[] { "--fix" }, "Automatically fix type errors.")
            .Switch(new[] { "-q", "--quiet" }, "Silence all non-critical errors.")
            .Switch(new[] { "--update" }, "Update RBI files.")
            .Switch(new[] { "--update-all" }, "Update all RBI files rather than just updated gems.")
            .Switch(new[] { "--suggest-typed" }, "Try upgrading `typed` sigils.", dependsOn: "--update")
            .Switch(new[] { "--lsp" }, "Start the Sorbet LSP server.")
            .Flag(new[] { "--dir=" }, "Typecheck all files in a specific directory.")
            .Flag(new[] { "--file=" }, "Typecheck a single file.")
            .Flag(new[] { "--ignore=" }, "Ignores input files that contain the given string in their paths (relative to the input path passed to Sorbet).")
            .Conflicts("--dir", "--file")
            .Conflicts("--lsp", "--update")
            .Conflicts("--lsp", "--update-all")
            .Conflicts("--lsp", "--fix")
            .NamedArgs("tap"), "tc");
        Add("unbottled", () => new CommandParser("unbottled")
            .Description("Show the unbottled dependents of formulae.")
            .Flag(new[] { "--tag=" }, "Use the specified bottle tag (e.g. `big_sur`) instead of the current OS.")
            .Switch(new[] { "--dependents" }, "Skip getting analytics data and sort by number of dependents instead.")
            .Switch(new[] { "--total" }, "Print the number of unbottled and total formulae.")
            .Switch(new[] { "--lost" }, "Print the `homebrew/core` commits where bottles were lost in the last week.")
            .Switch(new[] { "--eval-all" }, null, hidden: true)
            .Conflicts("--dependents", "--total", "--lost")
            .NamedArgs("formula"));
        Add("update-license-data", () => new CommandParser("update-license-data")
            .Description("Update SPDX license data in the Homebrew repository.")
            .NamedArgs("none"));
        Add("update-maintainers", () => new CommandParser("update-maintainers")
            .Description("Update the list of maintainers in the `Homebrew/brew` README.")
            .NamedArgs("none"));
        Add("update-perl-resources", () => new CommandParser("update-perl-resources")
            .UsageBanner("`update-perl-resources` [`--print-only`] [`--ignore-errors`] <formula> [...]")
            .Description("Update versions for CPAN resource blocks in <formula>.")
            .Switch(new[] { "-p", "--print-only" }, "Print the updated resource blocks instead of changing <formula>.")
            .Switch(new[] { "-s", "--silent" }, null, hidden: true)
            .Switch(new[] { "--ignore-errors" }, "Continue processing even if some resources can't be resolved.")
            .NamedArgs("formula", min: 1));
        Add("update-portable-ruby", () => new CommandParser("update-portable-ruby")
            .Description("""
                Update the vendored `portable-ruby` from the current `portable-ruby` formula:
                write the version files and bottle checksums, run `brew vendor-install ruby`,
                then sync `utils/ruby.sh`, vendored gems and RBI files to the bundler shipped
                by the new ruby.
                """)
            .Switch(new[] { "--print-target-version" }, "Print the target portable Ruby package version without updating it.")
            .NamedArgs("none"));
        Add("update-python-resources", () => new CommandParser("update-python-resources")
            .Description("Update versions for PyPI resource blocks in <formula>.")
            .Switch(new[] { "-p", "--print-only" }, "Print the updated resource blocks instead of changing <formula>.")
            .Switch(new[] { "-s", "--silent" }, null, hidden: true)
            .Switch(new[] { "--ignore-errors" }, "Record all discovered resources, even those that can't be resolved successfully. This option is ignored for homebrew/core formulae.")
            .Switch(new[] { "--ignore-non-pypi-packages" }, "Don't fail if <formula> is not a PyPI package.")
            .Switch(new[] { "--ignore-main-package-cooldown" }, "Bypass the release cooldown for <formula>'s own package when resolving resources. Its dependencies still respect the cooldown. This option is ignored for official taps.")
            .Switch(new[] { "--install-dependencies" }, "Install missing dependencies required to update resources.")
            .Flag(new[] { "--version=" }, "Use the specified <version> when finding resources for <formula>. If no version is specified, the current version for <formula> will be used.")
            .Flag(new[] { "--package-name=" }, "Use the specified <package-name> when finding resources for <formula>. If no package name is specified, it will be inferred from the formula's stable URL.")
            .CommaArray("--extra-packages", "Include these additional packages when finding resources.")
            .CommaArray("--exclude-packages", "Exclude these packages when finding resources.")
            .NamedArgs("formula", min: 1));
        Add("update-sponsors", () => new CommandParser("update-sponsors")
            .Description("Update the list of GitHub Sponsors in the `Homebrew/brew` README.")
            .NamedArgs("none"));
        Add("update-test", () => new CommandParser("update-test")
            .Description("""
                Run a test of `brew update` with a new repository clone.
                If no options are passed, use `origin/main` as the start commit.
                """)
            .Switch(new[] { "--to-tag" }, "Set `$HOMEBREW_UPDATE_TO_TAG` to test updating between tags.")
            .Switch(new[] { "--keep-tmp" }, "Retain the temporary directory containing the new repository clone.")
            .Flag(new[] { "--commit=" }, "Use the specified <commit> as the start commit.")
            .Flag(new[] { "--before=" }, "Use the commit at the specified <date> as the start commit.")
            .NamedArgs("none"));
        Add("vendor-gems", () => new CommandParser("vendor-gems")
            .UsageBanner("`vendor-gems` [`--update=`] [`--no-commit`]")
            .Description("Install and commit Homebrew's vendored gems.")
            .CommaArray("--update", "Update the specified list of vendored gems to the latest version.")
            .Switch(new[] { "--no-commit" }, "Do not generate a new commit upon completion.")
            .Switch(new[] { "--non-bundler-gems" }, null, hidden: true)
            .NamedArgs("none"));
        Add("verify", () => new CommandParser("verify")
            .Description("""
                Verify the build provenance of bottles using GitHub's attestation tools.
                This is done by first fetching the given bottles and then verifying
                their provenance for `homebrew/core` and third-party taps that provide
                attestations.

                Note that this command depends on the GitHub CLI. Run `brew install gh`.
                """)
            .Flag(new[] { "--os=" }, "Download for the given operating system. (Pass `all` to download for all operating systems.)")
            .Flag(new[] { "--arch=" }, "Download for the given CPU architecture. (Pass `all` to download for all architectures.)")
            .Flag(new[] { "--bottle-tag=" }, "Download a bottle for given tag.")
            .Switch(new[] { "--deps" }, "Also download dependencies for any listed <formula>.")
            .Switch(new[] { "-f", "--force" }, "Remove a previously cached version and re-fetch.")
            .Switch(new[] { "-j", "--json" }, "Return JSON for the attestation data for each bottle.")
            .Conflicts("--os", "--bottle-tag")
            .Conflicts("--arch", "--bottle-tag")
            .NamedArgs("formula", min: 1));
        Add("which-update", () => new CommandParser("which-update")
            .Description("Database update for `brew which-formula`.")
            .Flag(new[] { "--bottle-json-dir=" }, "Use generated bottle JSON files in the given directory to update formula entries.")
            .Flag(new[] { "--removed-formulae-file=" }, "Remove database entries for formulae listed in the given file.")
            .Flag(new[] { "--pull-request=" }, "Update entries for formula changes in the given pull request number.")
            .Flag(new[] { "--repository=" }, "GitHub repository for `--pull-request` (default: `$GITHUB_REPOSITORY`).", dependsOn: "--pull-request")
            .Flag(new[] { "--summary-file=" }, "Output a summary of the changes to a file.")
            .NamedArgs("database", number: 1));
    }
}
