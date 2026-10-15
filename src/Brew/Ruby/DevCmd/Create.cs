using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Brew.Ruby.DevCmd;

[DevCommand("create")]
sealed class Create : DevCommand
{
    public override CommandParser Args() => new CommandParser("create")
        .Description("""
            Generate a formula or, with `--cask`, a cask for the downloadable file at <URL>
            and open it in the editor. Homebrew will attempt to automatically derive the
            formula name and version, but if it fails, you'll have to make your own template.
            The `wget` formula serves as a simple example. For the complete API, see:
            <https://docs.brew.sh/rubydoc/Formula>
            """)
        .Switch(new[] { "--autotools" }, "Create a basic template for an Autotools-style build.")
        .Switch(new[] { "--cabal" }, "Create a basic template for a Cabal build.")
        .Switch(new[] { "--cask" }, "Create a basic template for a cask.")
        .Switch(new[] { "--cmake" }, "Create a basic template for a CMake-style build.")
        .Switch(new[] { "--crystal" }, "Create a basic template for a Crystal build.")
        .Switch(new[] { "--go" }, "Create a basic template for a Go build.")
        .Switch(new[] { "--meson" }, "Create a basic template for a Meson-style build.")
        .Switch(new[] { "--node" }, "Create a basic template for a Node build.")
        .Switch(new[] { "--perl" }, "Create a basic template for a Perl build.")
        .Switch(new[] { "--python" }, "Create a basic template for a Python build.")
        .Switch(new[] { "--ruby" }, "Create a basic template for a Ruby build.")
        .Switch(new[] { "--rust" }, "Create a basic template for a Rust build.")
        .Switch(new[] { "--zig" }, "Create a basic template for a Zig build.")
        .Switch(new[] { "--no-fetch" }, "Homebrew will not download <URL> to the cache and will thus not add its SHA-256 " +
                                        "to the formula for you, nor will it check the GitHub API for GitHub projects " +
                                        "(to fill out its description and homepage).")
        .Switch(new[] { "--HEAD" }, "Indicate that <URL> points to the package's repository rather than a file.")
        .Flag(new[] { "--set-name=" }, "Explicitly set the <name> of the new formula or cask.")
        .Flag(new[] { "--set-version=" }, "Explicitly set the <version> of the new formula or cask.")
        .Flag(new[] { "--set-license=" }, "Explicitly set the <license> of the new formula.")
        .Flag(new[] { "--tap=" }, "Generate the new formula within the given tap, specified as <user>`/`<repo>.")
        .Switch(new[] { "-f", "--force" }, "Ignore errors for disallowed formula names and names that shadow aliases.")
        .Switch(new[] { "--no-edit" }, null, hidden: true)
        .Conflicts("--autotools", "--cabal", "--cmake", "--crystal", "--go", "--meson", "--node",
                   "--perl", "--python", "--ruby", "--rust", "--zig", "--cask")
        .Conflicts("--cask", "--HEAD")
        .Conflicts("--cask", "--set-license")
        .NamedArgs("url", number: 1);

    static readonly string[] Modes = { "autotools", "cmake", "crystal", "go", "cabal", "meson", "node", "perl", "python", "ruby", "rust", "zig" };

    public override void Run(ParsedArgs args)
    {
        var path = args.Switch("cask") ? CreateCask(args) : CreateFormula(args);
        if (!args.Switch("no-edit")) Editor.Open(path);
    }

    static string Gets()
    {
        var line = Console.ReadLine();
        return string.IsNullOrEmpty(line) ? null : line.TrimEnd('\r', '\n');
    }

    // String#inspect
    public static string Inspect(string s)
    {
        var sb = new StringBuilder("\"");
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\t': sb.Append("\\t"); break;
                case '\r': sb.Append("\\r"); break;
                case '#' when i + 1 < s.Length && s[i + 1] is '{' or '$' or '@': sb.Append("\\#"); break;
                default:
                    if (c < 0x20) sb.Append($"\\x{(int)c:X2}");
                    else sb.Append(c);
                    break;
            }
        }
        return sb.Append('"').ToString();
    }

    // Pathname#stem
    static string Stem(string url)
    {
        var b = url.TrimEnd('/');
        b = b[(b.LastIndexOf('/') + 1)..];
        int dot = b.LastIndexOf('.');
        return dot > 0 ? b[..dot] : b;
    }

    string CreateCask(ParsedArgs args)
    {
        var url = args.Named[0];
        var setName = args.Flag("set-name")?.Trim();
        string name;
        if (string.IsNullOrEmpty(setName))
        {
            var stem = Stem(url);
            stem = stem[(stem.LastIndexOf('=') + 1)..];
            Console.Write($"Cask name [{stem}]: ");
            name = Gets() ?? stem;
        }
        else name = setName;
        var token = TokenFrom(name);

        var tap = DevTap.Normalize(args.Flag("tap") ?? "homebrew/cask");
        if (!Directory.Exists(DevTap.Path(tap))) throw new BrewError(DevTap.TapUnavailableMessage(tap));
        var caskPath = Path.Combine(DevTap.Path(tap), "Casks", token[..1], token + ".rb");
        Directory.CreateDirectory(Path.GetDirectoryName(caskPath)!);
        if (File.Exists(caskPath)) throw new BrewError($"Cask '{token}' already exists. Run {Formatter.Identifier($"brew edit --cask {token}")} to edit it.");

        var version = args.Flag("set-version") ?? Versions.Parse(Regex.Replace(url.Replace(token, ""), "x86(?:_64)?", ""));
        string interpolatedUrl = url, sha256 = "";
        if (!string.IsNullOrEmpty(version))
        {
            if (!args.Switch("no-fetch")) sha256 = DevHttp.FetchWithoutChecksum(url, token, version).sha256;
            interpolatedUrl = url.Replace(version, "#{version}");
        }
        File.WriteAllText(caskPath, $$"""
            # Documentation: https://docs.brew.sh/Cask-Cookbook
            # PLEASE REMOVE ALL GENERATED COMMENTS BEFORE SUBMITTING YOUR PULL REQUEST!
            cask "{{token}}" do
              version "{{version}}"
              sha256 "{{sha256}}"

              url "{{interpolatedUrl}}"
              name "{{name}}"
              desc ""
              homepage ""

              # Documentation: https://docs.brew.sh/Brew-Livecheck
              livecheck do
                url ""
                strategy ""
              end

              depends_on macos: ""

              app ""

              # Documentation: https://docs.brew.sh/Cask-Cookbook#stanza-zap
              zap trash: ""
            end

            """);
        Out.Puts($"Please run `brew audit --cask --new {token}` before submitting, thanks.");
        return caskPath;
    }

    // Cask::Utils.token_from
    static string TokenFrom(string name)
    {
        var t = name.ToLowerInvariant().Replace("+", "-plus-").Replace("@", "-at-");
        t = Regex.Replace(t, "[ _·•]", "-");
        t = Regex.Replace(t, @"[^\w-]", "");
        t = Regex.Replace(t, "--+", "-");
        return t.Trim('-');
    }

    string CreateFormula(ParsedArgs args)
    {
        var mode = Modes.FirstOrDefault(args.Switch);
        var url = args.Named[0];
        var setName = args.Flag("set-name");
        var setVersion = args.Flag("set-version");
        var tap = string.IsNullOrEmpty(args.Flag("tap")) ? "homebrew/core" : DevTap.Normalize(args.Flag("tap"));
        bool fetch = !args.Switch("no-fetch");
        bool head = args.Switch("HEAD");
        var license = args.Flag("set-license");

        // FormulaCreator#initialize
        string name = setName, user = null, repository = null;
        var gh = Regex.Match(url, @"github\.com/(?<user>[^/]+)/(?<repo>[^/]+).*");
        if (gh.Success)
        {
            user = gh.Groups["user"].Value;
            repository = gh.Groups["repo"].Value;
            if (repository.EndsWith(".git"))
            {
                repository = repository[..^4];
                head = true;
            }
            if (string.IsNullOrWhiteSpace(name)) name = repository;
        }
        else if (string.IsNullOrWhiteSpace(name))
        {
            var stem = Stem(url);
            if (stem.StartsWith("index.cgi") && stem.Contains('=')) name = stem[(stem.LastIndexOf('=') + 1)..];
            else
            {
                var pathver = Versions.Parse(stem) ?? "";
                name = Regex.Replace(stem, $"[-_.]?{Regex.Escape(pathver)}$", "");
            }
        }

        string version;
        bool detectedFromUrl;
        if (!string.IsNullOrWhiteSpace(setVersion)) { version = setVersion; detectedFromUrl = false; }
        else { version = Versions.Parse(url); detectedFromUrl = true; }

        JsonNode github = null;
        if (fetch && user != null && repository != null)
        {
            github = DevHttp.GitHubJson($"{DevHttp.ApiUrl}/repos/{user}/{repository}");
            if (string.IsNullOrEmpty(version) && !head)
            {
                try
                {
                    var latest = DevHttp.GitHubJson($"{DevHttp.ApiUrl}/repos/{user}/{repository}/releases/latest");
                    version = latest["tag_name"]!.GetValue<string>();
                    detectedFromUrl = false;
                    url = $"https://github.com/{user}/{repository}/archive/refs/tags/{version}.tar.gz";
                }
                catch (GitHubApiError e) when (e.Status == 404) { }
            }
        }

        if (string.IsNullOrWhiteSpace(setName))
        {
            Console.Write($"Formula name [{name}]: ");
            if (Gets() is { } confirmed) name = confirmed;
        }

        if (!Directory.Exists(DevTap.Path(tap))) throw new BrewError(DevTap.TapUnavailableMessage(tap));

        if (!args.Switch("force"))
        {
            if (DisallowedReason(name) is { } reason)
                throw new BrewError($"The formula '{name}' is not allowed to be created.\n{reason}\nIf you really want to create this formula use `--force`.");
            var alias = Path.Combine(DevTap.Path("homebrew/core"), "Aliases", name);
            if (File.Exists(alias))
            {
                var target = new FileInfo(alias).LinkTarget ?? File.ReadAllText(alias).Trim();
                var realname = Path.GetFileNameWithoutExtension(target);
                throw new BrewError($"The formula '{realname}' is already aliased to '{name}'.\nPlease check that you are not creating a duplicate.\nTo force creation use `--force`.");
            }
        }

        // FormulaCreator#write_formula!
        if (string.IsNullOrWhiteSpace(name)) throw new BrewError("name is blank!");
        var path = DevTap.NewFormulaPath(tap, name);
        if (File.Exists(path)) throw new BrewError($"{path} already exists");
        if (string.IsNullOrEmpty(version))
            throw new BrewError("Version cannot be determined from URL. Explicitly set the version with `--set-version` instead.");

        string sha256 = null, desc = null, homepage = null;
        List<string> exes = null;
        if (fetch)
        {
            if (!head)
            {
                var (file, sha) = DevHttp.FetchWithoutChecksum(url, name, version);
                var buf = new byte[100];
                int n;
                using (var s = File.OpenRead(file)) n = s.Read(buf, 0, buf.Length);
                if (Encoding.UTF8.GetString(buf, 0, n).Trim().ToLowerInvariant().StartsWith("<!doctype html"))
                    throw new BrewError("Downloaded URL is not archive");
                sha256 = sha;
                exes = Executables(file, url);
            }
            if (github != null)
            {
                desc = github["description"]?.GetValue<string>();
                homepage = github["homepage"]?.GetValue<string>() is { Length: > 0 } hp ? hp : $"https://github.com/{github["full_name"]?.GetValue<string>()}";
                if (license == null && github["license"] is JsonObject lic) license = lic["spdx_id"]?.GetValue<string>();
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, Template(name, mode, url, version, detectedFromUrl, sha256, desc, homepage, license, head, exes));

        Out.Puts($"""
            Please audit and test formula before submitting:
              HOMEBREW_NO_INSTALL_FROM_API=1 brew audit --new {name}
              HOMEBREW_NO_INSTALL_FROM_API=1 brew install --build-from-source --verbose --debug {name}
              HOMEBREW_NO_INSTALL_FROM_API=1 brew test {name}
            """);
        return path;
    }

    // Programs in the download, as install_tree lays them out under libexec.
    static List<string> Executables(string file, string url)
    {
        var basename = Path.GetFileName(new Uri(url).AbsolutePath);
        if (basename.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) return new List<string> { basename };
        if (!new[] { ".zip", ".nupkg" }.Any(e => basename.EndsWith(e, StringComparison.OrdinalIgnoreCase))) return null;
        try
        {
            using var zip = ZipFile.OpenRead(file);
            var entries = zip.Entries.Select(e => e.FullName.Replace('\\', '/')).ToList();
            var tops = entries.Select(e => e.Split('/')[0]).Distinct().ToList();
            var strip = tops.Count == 1 && entries.All(e => e.Contains('/')) ? tops[0] + "/" : "";
            return entries.Where(e => e.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                .Select(e => e[strip.Length..]).OrderBy(e => e.Count(c => c == '/')).ThenBy(e => e, StringComparer.Ordinal).ToList();
        }
        catch (InvalidDataException) { return null; }
    }

    static string Template(string name, string mode, string url, string version, bool detectedFromUrl, string sha256,
        string desc, string homepage, string license, bool head, List<string> exes)
    {
        var sb = new StringBuilder();
        void L(string s = "") => sb.Append(s).Append('\n');
        L("# Documentation: https://docs.brew.sh/Formula-Cookbook");
        L("#                https://docs.brew.sh/rubydoc/Formula");
        L("# PLEASE REMOVE ALL GENERATED COMMENTS BEFORE SUBMITTING YOUR PULL REQUEST!");
        L($"class {FormulaLoader.ClassName(name)} < Formula");
        if (mode == "python") { L("  include Language::Python::Virtualenv"); L(); }
        L($"  desc {Inspect(desc ?? "")}");
        L($"  homepage {Inspect(homepage ?? "")}");
        if (!head)
        {
            L($"  url {Inspect(url)}");
            if (!detectedFromUrl) L($"  version {Inspect(version.StartsWith('v') ? version[1..] : version)}");
            L($"  sha256 {Inspect(sha256 ?? "")}");
        }
        L($"  license {Inspect(license ?? "")}");
        if (head) L($"  head {Inspect(url)}");
        L();
        switch (mode)
        {
            case "cabal":
                L("  depends_on \"cabal-install\" => :build"); L("  depends_on \"ghc\" => :build"); L("  depends_on \"gmp\"");
                L(); L("  uses_from_macos \"libffi\""); break;
            case "cmake": L("  depends_on \"cmake\" => :build"); break;
            case "crystal": L("  depends_on \"crystal\" => :build"); break;
            case "go": L("  depends_on \"go\" => :build"); break;
            case "meson": L("  depends_on \"meson\" => :build"); L("  depends_on \"ninja\" => :build"); break;
            case "node": L("  depends_on \"node\""); break;
            case "perl":
                L("  uses_from_macos \"perl\""); L(); L("  # Add CPAN dependencies as resources"); L("  # resource \"\" do");
                L("  #   url \"\""); L("  #   sha256 \"\""); L("  # end"); break;
            case "python": L($"  depends_on {Inspect(LatestVersionedFormula("python"))}"); break;
            case "ruby": L("  depends_on \"ruby\""); break;
            case "rust": L("  depends_on \"rust\" => :build"); break;
            case "zig": L("  depends_on \"zig\" => :build"); break;
            case null: L("  # depends_on \"cmake\" => :build"); break;
        }
        L();
        if (mode is not ("node" or "python")) { L("  deny_network_access!"); L(); }
        switch (mode)
        {
            case "cabal": L("  def fetch"); L("    system \"cabal\", \"v2-update\""); L("    system \"cabal\", \"v2-install\", \"--only-download\", *std_cabal_v2_args"); L("  end"); L(); break;
            case "crystal": L("  def fetch"); L("    system \"shards\", \"install\", \"--production\", \"--skip-postinstall\""); L("  end"); L(); break;
            case "go": L("  def fetch"); L("    system \"go\", \"mod\", \"download\""); L("  end"); L(); break;
            case "ruby": L("  def fetch"); L("    ENV[\"BUNDLE_PATH\"] = \".bundle\""); L(); L("    system \"bundle\", \"cache\", \"--no-install\""); L("  end"); L(); break;
            case "rust": L("  def fetch"); L("    system \"cargo\", \"fetch\", *std_cargo_fetch_args"); L("  end"); L(); break;
            case "zig": L("  def fetch"); L("    system \"zig\", \"build\", \"--fetch\""); L("  end"); L(); break;
        }
        L("  def install");
        switch (mode)
        {
            case "cabal": L("    system \"cabal\", \"v2-install\", *std_cabal_v2_args"); break;
            case "cmake":
                L("    system \"cmake\", \"-S\", \".\", \"-B\", \"build\", *std_cmake_args"); L("    system \"cmake\", \"--build\", \"build\"");
                L("    system \"cmake\", \"--install\", \"build\""); break;
            case "autotools":
                L("    # Remove unrecognized options if they cause configure to fail");
                L("    # https://docs.brew.sh/rubydoc/Formula.html#std_configure_args-instance_method");
                L("    system \"./configure\", \"--disable-silent-rules\", *std_configure_args");
                L("    system \"make\", \"install\" # if this fails, try separate make/make install steps"); break;
            case "crystal": L("    system \"shards\", \"build\", *std_shards_args"); L($"    bin.install {Inspect($"bin/{name}")}"); break;
            case "go": L("    system \"go\", \"build\", *std_go_args"); break;
            case "meson":
                L("    system \"meson\", \"setup\", \"build\", *std_meson_args"); L("    system \"meson\", \"compile\", \"-C\", \"build\", \"--verbose\"");
                L("    system \"meson\", \"install\", \"-C\", \"build\""); break;
            case "node": L("    system \"npm\", \"install\", *std_npm_args"); L("    bin.install_symlink libexec.glob(\"bin/*\")"); break;
            case "perl":
                L("    ENV.prepend_create_path \"PERL5LIB\", libexec/\"lib/perl5\""); L("    ENV.prepend_path \"PERL5LIB\", libexec/\"lib\""); L();
                L("    # Stage additional dependency (`Makefile.PL` style)."); L("    # resource(\"\").stage do");
                L("    #   system \"perl\", \"Makefile.PL\", \"INSTALL_BASE=#{libexec}\""); L("    #   system \"make\""); L("    #   system \"make\", \"install\"");
                L("    # end"); L(); L("    # Stage additional dependency (`Build.PL` style)."); L("    # resource(\"\").stage do");
                L("    #   system \"perl\", \"Build.PL\", \"--install_base\", libexec"); L("    #   system \"./Build\""); L("    #   system \"./Build\", \"install\"");
                L("    # end"); L(); L("    bin.install name"); L("    bin.env_script_all_files(libexec/\"bin\", PERL5LIB: ENV[\"PERL5LIB\"])"); break;
            case "python": L("    virtualenv_install_with_resources"); break;
            case "ruby":
                L("    ENV[\"GEM_HOME\"] = libexec"); L(); L("    system \"bundle\", \"install\", \"--local\"");
                L("    system \"gem\", \"build\", \"#{name}.gemspec\""); L("    system \"gem\", \"install\", \"--ignore-dependencies\", \"#{name}-#{version}.gem\"");
                L(); L("    bin.install libexec/\"bin/#{name}\""); L("    bin.env_script_all_files(libexec/\"bin\", GEM_HOME: ENV[\"GEM_HOME\"])"); break;
            case "rust": L("    system \"cargo\", \"install\", *std_cargo_args"); break;
            case "zig": L("    system \"zig\", \"build\", *std_zig_args"); break;
            default:
                // Windows: downloads are usually prebuilt, so keep the whole tree in libexec and
                // put a launcher for each program in bin (see docs/FORMULA_API.md).
                L("    # Prebuilt Windows downloads are usually installed whole into `libexec`,");
                L("    # with a `bin` launcher for each program: `windows_exe \"libexec/<program>.exe\", \"<command>\"`.");
                L("    install_tree");
                var picks = PickExecutables(name, exes);
                if (picks.Count == 0) L($"    windows_exe {Inspect($"libexec/{name}.exe")}, {Inspect(name)}");
                foreach (var (exe, command) in picks) L($"    windows_exe {Inspect($"libexec/{exe}")}, {Inspect(command)}");
                break;
        }
        L("  end");
        L();
        L("  test do");
        L("    # `test do` will create, run in and delete a temporary directory.");
        L("    #");
        L("    # This test will fail and we won't accept that! For Homebrew/homebrew-core");
        L("    # this will need to be a test that verifies the functionality of the");
        L($"    # software. Run the test with `brew test {name}`.");
        L("    #");
        L("    # The installed folder is not in the path, so use the entire path to any");
        L("    # executables being tested: `system bin/\"program\", \"do\", \"something\"`.");
        L("    system \"false\"");
        L("  end");
        L("end");
        return sb.ToString();
    }

    // The program named like the formula (or the only one); a bare .exe download becomes `name`.
    static List<(string exe, string command)> PickExecutables(string name, List<string> exes)
    {
        var result = new List<(string, string)>();
        if (exes == null || exes.Count == 0) return result;
        if (exes.Count == 1 && !exes[0].Contains('/')) { result.Add((exes[0], name)); return result; }
        var match = exes.FirstOrDefault(e => string.Equals(Path.GetFileNameWithoutExtension(e), name, StringComparison.OrdinalIgnoreCase));
        var top = exes.Where(e => e.Count(c => c == '/') == exes.Min(x => x.Count(c => c == '/'))).ToList();
        foreach (var e in match != null ? new List<string> { match } : top.Take(5).ToList())
            result.Add((e, Path.GetFileNameWithoutExtension(e).ToLowerInvariant()));
        return result;
    }

    static string LatestVersionedFormula(string name)
    {
        var prefix = name + "@";
        return DevCommands.AllFormulaFiles().Select(Path.GetFileNameWithoutExtension)
            .Where(f => f.StartsWith(prefix))
            .OrderBy(f => f[prefix.Length..], Comparer<string>.Create(Versions.Compare))
            .LastOrDefault() ?? "python";
    }

    // MissingFormula.disallowed_reason
    static string DisallowedReason(string name)
    {
        var n = name.ToLowerInvariant();
        if (n == "gem" || Regex.IsMatch(n, "^rubygems?$")) return "macOS provides gem as part of Ruby. To install a newer version:\n  brew install ruby";
        return n switch
        {
            "pip" => "pip is part of the python formula:\n  brew install python",
            "pil" => "Instead of PIL, consider pillow:\n  brew install pillow",
            "macruby" => "MacRuby has been discontinued. Consider RubyMotion:\n  brew install --cask rubymotion",
            _ when Regex.IsMatch(n, "(?:lib)?lzma") => "lzma is now part of the xz formula:\n  brew install xz",
            "gsutil" => "gsutil is available through pip:\n  pip3 install gsutil",
            "gfortran" => "GNU Fortran is part of the GCC formula:\n  brew install gcc",
            "play" => "Play 2.3 replaces the play command with activator:\n  brew install typesafe-activator\n\nYou can read more about this change at:\n" +
                      $"  {Formatter.Url("https://www.playframework.com/documentation/2.3.x/Migration23")}\n  {Formatter.Url("https://www.playframework.com/documentation/2.3.x/Highlights23")}",
            "haskell-platform" => "The components of the Haskell Platform are available separately.\n\nGlasgow Haskell Compiler:\n  brew install ghc\n\n" +
                                  "Cabal build system:\n  brew install cabal-install\n\nHaskell Stack tool:\n  brew install haskell-stack",
            "mysqldump-secure" => "The creator of mysqldump-secure tried to game our popularity metrics.",
            "ngrok" => "Upstream sunsetted 1.x in March 2016 and 2.x is not open-source.\n\nIf you wish to use the 2.x release you can install it with:\n  brew install --cask ngrok",
            "cargo" => "cargo is part of the rust formula:\n  brew install rust",
            "cargo-completion" => "cargo-completion is part of the rust formula:\n  brew install rust",
            "uconv" => "uconv is part of the icu4c formula:\n  brew install icu4c",
            "postgresql" or "postgres" => "postgresql breaks existing databases on upgrade without human intervention.\n\nSee a more specific version to install with:\n  brew formulae | grep postgresql@",
            _ => null,
        };
    }
}
