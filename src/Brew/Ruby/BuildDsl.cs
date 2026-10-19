using System.Globalization;
using System.Text;
using Brew.Formulae;

namespace Brew.Ruby;

// Building from source: Homebrew's std_*_args, completions, bottle DSL, Time, and the
// build-time ENV helpers. Windows differences: executables get ".exe", Go/Rust builds are static.
public static partial class FormulaLoader
{
    static void DefineBuildDsl(Interp it, RClass F)
    {
        FormulaSpec Sp(object self) => SpecOf((RClass)self);
        object Kw(Interp i, object[] a, string key) =>
            a.Length > 0 && a[^1] is RHash h && h.D.TryGetValue(Interp.Sym(key), out var v) ? v : null;
        string Prefix(Interp i, object self) => Ctx(i, self).Prefix;
        string Nm(object self) => ((RObject)self).GetIvar("@name").ToString();
        string Exe(string p) => Path.HasExtension(p) ? p : p + ".exe";

        // ---- bottle do ... end ----
        it.DefS(F, "bottle", (i, self, a, b) =>
        {
            if (b == null) return null;
            var spec = Sp(self).Bottle = new BottleSpec();
            var holder = new RObject { Class = i.ObjectClass, Host = spec };
            var s = i.SingletonOf(holder);
            i.Def(s, "root_url", (i2, _, a2, _) => { spec.RootUrl = i2.ToS(a2[0]); return null; });
            i.Def(s, "rebuild", (i2, _, a2, _) => { spec.Rebuild = (int)i2.Int(a2[0]); return null; });
            i.Def(s, "sha256", (i2, _, a2, _) =>
            {
                if (a2.Length == 0 || a2[^1] is not RHash h) return null;
                var cellar = h.D.TryGetValue(Interp.Sym("cellar"), out var c) ? i2.ToS(c) : "any";
                foreach (var kv in h.D)
                    if (kv.Key is RSymbol { Name: var tag } && tag != "cellar") spec.Files[tag] = (cellar, i2.ToS(kv.Value).ToLowerInvariant());
                return null;
            });
            i.CallProc(b, Array.Empty<object>(), null, holder, s);
            return null;
        });
        it.Def(F, "bottle?", (i, self, a, b) => Ctx(i, self).PouredFromBottle);

        // ---- standard build arguments ----
        it.Def(F, "std_go_args", (i, self, a, b) =>
        {
            var output = Kw(i, a, "output") is { } o ? i.PathStr(o) : Path.Combine(Prefix(i, self), "bin", Nm(self));
            var ldflags = Kw(i, a, "ldflags");
            var gcflags = Kw(i, a, "gcflags");
            var tags = Kw(i, a, "tags");
            var ld = new List<string>();
            if (ldflags is RSymbol { Name: "goreleaser" })
            {
                var version = ((RObject)self).GetIvar("@version").ToString();
                ld.AddRange([$"-X 'main.version={version}'", "-X 'main.commit=Homebrew'",
                    $"-X 'main.date={BuildTime(i).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)}'", "-X 'main.builtBy=Homebrew'"]);
            }
            else if (ldflags is RSymbol sym) throw i.Error(i.ArgumentError, $"Invalid ldflags: :{sym.Name}");
            else if (ldflags != null) ld.AddRange(Strings(i, ldflags));
            if (!i.Env.ContainsKey("HOMEBREW_DEBUG_SYMBOLS")) ld.InsertRange(0, ["-s", "-w"]);
            var args = new List<string> { "-trimpath", "-o=" + Exe(output) };
            if (tags != null) args.Add("-tags=" + string.Join(",", Strings(i, tags)));
            if (ld.Count > 0) args.Add("-ldflags=" + string.Join(" ", ld));
            if (gcflags != null) args.Add("-gcflags=" + string.Join(" ", Strings(i, gcflags)));
            return StrArray(args.ToArray());
        });
        it.Def(F, "std_cargo_args", (i, self, a, b) =>
        {
            var root = Kw(i, a, "root") is { } r ? i.PathStr(r) : Prefix(i, self);
            var path = Kw(i, a, "path") is { } p ? i.PathStr(p) : ".";
            var args = new List<string> { "--jobs", MakeJobs(i).ToString(CultureInfo.InvariantCulture), "--locked", "--root=" + root, "--path=" + path };
            if (Kw(i, a, "features") is { } features) args.Add("--features=" + string.Join(",", Strings(i, features)));
            if (i.FindMethod(((RObject)self).Class, "fetch") is { } fm && fm.Owner != i.ObjectClass.Consts["Formula"]) args.Add("--offline");
            return StrArray(args.ToArray());
        });
        it.Def(F, "std_cargo_fetch_args", (i, self, a, b) => StrArray("--locked", "--target", "host-tuple"));
        // std_meson_args(prefix: self.prefix, libdir: "lib"): glib stages a first build with prefix: buildpath/"staging".
        it.Def(F, "std_meson_args", (i, self, a, b) =>
            StrArray("--prefix=" + (Kw(i, a, "prefix") is { } p ? i.PathStr(p) : Prefix(i, self)),
                "--libdir=" + (Kw(i, a, "libdir") is { } l ? i.PathStr(l) : "lib"), "--buildtype=release", "--wrap-mode=nofallback"));
        it.Def(F, "std_npm_args", (i, self, a, b) =>
            StrArray("--loglevel=silly", "--build-from-source", "--cache=" + Path.Combine(Ctx(i, self).BuildPath, ".brew_home", "npm_cache"),
                "--prefix=" + Path.Combine(Prefix(i, self), "libexec")));
        // Formula#rpath/#loader_path: Linux's $ORIGIN spelling. PE files have no rpath; the value only
        // reaches build systems (CMAKE_INSTALL_RPATH, -Wl,-rpath), which ignore it for Windows targets.
        it.Def(F, "loader_path", (i, self, a, b) => Interp.S("$ORIGIN"));
        it.Def(F, "rpath", (i, self, a, b) =>
        {
            var source = Kw(i, a, "source") is { } s ? i.PathStr(s) : Path.Combine(Prefix(i, self), "bin");
            var target = Kw(i, a, "target") is { } t ? i.PathStr(t) : Path.Combine(Prefix(i, self), "lib");
            return Interp.S("$ORIGIN/" + Path.GetRelativePath(source, target).Replace('\\', '/'));
        });
        it.Def(F, "pwsh_completion", (i, self, a, b) => i.NewPath(Path.Combine(Prefix(i, self), "share", "pwsh", "completions")));
        it.Def(F, "time", (i, self, a, b) => TimeObject(i, BuildTime(i)));
        it.Def(F, "tap", (i, self, a, b) =>
        {
            var tap = new RObject { Class = i.ObjectClass };
            var s = i.SingletonOf(tap);
            i.Def(s, "user", (_, _, _, _) => Interp.S("Homebrew"));
            i.Def(s, "repo", (_, _, _, _) => Interp.S("core"));
            i.Def(s, "name to_s", (_, _, _, _) => Interp.S("homebrew/core"));
            i.Def(s, "core_tap? official?", (_, _, _, _) => true);
            // Bug reports about these builds belong with brew-windows, not upstream homebrew-core (llvm's BUG_REPORT_URL).
            i.Def(s, "remote default_remote", (_, _, _, _) => Interp.S("https://github.com/SamuelArther/brew-windows"));
            i.Def(s, "issues_url", (_, _, _, _) => Interp.S("https://github.com/SamuelArther/brew-windows/issues"));
            return tap;
        });

        it.Def(F, "generate_completions_from_executable", (i, self, a, b) =>
        {
            var commands = a.Where(x => x is not RHash).Select(x => x is RObject ? i.PathStr(x) : i.ToS(x)).ToList();
            var format = Kw(i, a, "shell_parameter_format");
            var executable = commands[0];
            var prefix = Prefix(i, self);
            var baseName = Kw(i, a, "base_name") is { } bn ? i.ToS(bn)
                : i.Abs(executable).StartsWith(Path.Combine(prefix, "bin"), StringComparison.OrdinalIgnoreCase)
                  || i.Abs(executable).StartsWith(Path.Combine(prefix, "sbin"), StringComparison.OrdinalIgnoreCase)
                    ? Path.GetFileNameWithoutExtension(executable) : Nm(self);
            var fmt = format is RSymbol fs ? fs.Name : format == null ? null : i.ToS(format);
            var shells = Kw(i, a, "shells") is RArray sa ? sa.L.Select(x => ((RSymbol)x).Name).ToList()
                : fmt is "cobra" or "typer" ? ["bash", "zsh", "fish", "pwsh"] : ["bash", "zsh", "fish"];
            var paths = new Dictionary<string, string>
            {
                ["bash"] = Path.Combine(prefix, "etc", "bash_completion.d", baseName),
                ["zsh"] = Path.Combine(prefix, "share", "zsh", "site-functions", "_" + baseName),
                ["fish"] = Path.Combine(prefix, "share", "fish", "vendor_completions.d", baseName + ".fish"),
                ["pwsh"] = Path.Combine(prefix, "share", "pwsh", "completions", "_" + baseName + ".ps1"),
            };
            foreach (var shell in shells)
            {
                var param = shell == "pwsh" ? "powershell" : shell;
                var env = new Dictionary<string, string> { ["SHELL"] = shell };
                var extra = new List<string>();
                switch (fmt)
                {
                    case null: extra.Add(param); break;
                    case "arg": extra.Add("--shell=" + param); break;
                    case "clap": env["COMPLETE"] = param; break;
                    case "click": env["_" + Path.GetFileNameWithoutExtension(executable).ToUpperInvariant().Replace('-', '_') + "_COMPLETE"] = param + "_source"; break;
                    case "cobra": extra.AddRange(["completion", param]); break;
                    case "flag": extra.Add("--" + param); break;
                    case "none": break;
                    case "typer": env["_TYPER_COMPLETE_TEST_DISABLE_SHELL_DETECTION"] = "1"; extra.AddRange(["--show-completion", param]); break;
                    default: extra.Add(fmt + shell); break;
                }
                var output = Capture(i, commands.Concat(extra).ToList(), env);
                Directory.CreateDirectory(Path.GetDirectoryName(paths[shell])!);
                File.WriteAllText(paths[shell], output);
            }
            return null;
        });

        // ---- ENV build helpers ----
        var E = it.SingletonOf(it.EnvObject);
        it.Def(E, "make_jobs", (i, self, a, b) => (long)MakeJobs(i));
        it.Def(E, "debug_symbols?", (i, self, a, b) => i.Env.ContainsKey("HOMEBREW_DEBUG_SYMBOLS"));
        it.Def(E, "cc", (i, self, a, b) => i.Env.TryGetValue("CC", out var v) ? Interp.S(v) : Interp.S("clang"));
        it.Def(E, "cxx", (i, self, a, b) => i.Env.TryGetValue("CXX", out var v) ? Interp.S(v) : Interp.S("clang++"));
        it.Def(E, "cflags cxxflags ldflags cppflags", (i, self, a, b) => null);
        // ENV.compiler: the toolchain is MinGW GCC (so `ENV.compiler == :llvm_clang` branches stay off).
        it.Def(E, "compiler", (i, self, a, b) => Interp.Sym(i.Env.TryGetValue("CC", out var cc) && cc.Contains("clang") ? "clang" : "gcc"));
        // ENV.clang / ENV.gcc: switch CC/CXX (clang-build-analyzer's test needs clang's -ftime-trace; LLVM-MinGW is on PATH).
        it.Def(E, "clang llvm_clang", (i, self, a, b) => { i.Env["CC"] = "clang"; i.Env["CXX"] = "clang++"; return null; });
        it.Def(E, "gcc", (i, self, a, b) => { i.Env["CC"] = "gcc"; i.Env["CXX"] = "g++"; return null; });
        // ENV.prepend_create_path: mkpath, then prepend_path (perl formulae: PERL5LIB under libexec).
        it.Def(E, "prepend_create_path", (i, self, a, b) =>
        {
            var dir = i.Abs(i.PathStr(a[1]));
            Directory.CreateDirectory(dir);
            return i.Invoke(self, "prepend_path", new object[] { a[0], i.NewPath(dir) }, null);
        });
        it.Def(E, "append_to_cflags", (i, self, a, b) =>
        {
            foreach (var k in new[] { "CFLAGS", "CXXFLAGS" })
                i.Env[k] = i.Env.TryGetValue(k, out var cur) && cur.Length > 0 ? cur + " " + i.ToS(a[0]) : i.ToS(a[0]);
            return null;
        });
        // ENV.append_to_rustflags (wasm-bindgen's test): upstream's rustc shim adds HOMEBREW_RUSTFLAGS; with no
        // shim here they go straight into RUSTFLAGS.
        it.Def(E, "append_to_rustflags", (i, self, a, b) =>
        {
            foreach (var k in new[] { "HOMEBREW_RUSTFLAGS", "RUSTFLAGS" })
                i.Env[k] = i.Env.TryGetValue(k, out var cur) && cur.Length > 0 ? cur + " " + i.ToS(a[0]) : i.ToS(a[0]);
            return null;
        });
        // ENV.remove(keys, value): drop the first occurrence of value from each (garble's test edits PATH).
        it.Def(E, "remove", (i, self, a, b) =>
        {
            if (a.Length < 2 || a[1] == null) return null;
            var value = i.ToS(a[1]);
            foreach (var key in a[0] is RArray keys ? keys.L.Select(i.ToS) : [i.ToS(a[0])])
            {
                if (!i.Env.TryGetValue(key, out var old)) continue;
                var at = old.IndexOf(value, StringComparison.Ordinal);
                var updated = at < 0 ? old : old.Remove(at, value.Length);
                if (updated.Length == 0) i.Env.Remove(key); else i.Env[key] = updated;
            }
            return null;
        });

        DefineTime(it);
    }

    // StringInreplaceExtension's Makefile helpers, Language::Perl::Shebang and Pathname#install_metafiles, as upstream
    // writes them (utils/string_inreplace_extension.rb, language/perl.rb, metafiles.rb).
    internal const string BuildPrelude = """
        class String
          def change_make_var!(flag, new_value)
            gsub!(/^#{Regexp.escape(flag)}[ \t]*[\\?+:!]?=[ \t]*((?:.*\\\n)*.*)$/, "#{flag}=#{new_value}")
          end

          def remove_make_var!(flags)
            Array(flags).each { |flag| gsub!(/^#{Regexp.escape(flag)}[ \t]*[\\?+:!]?=(?:.*\\\n)*.*$\n?/, "") }
            self
          end

          def get_make_var(flag)
            self[/^#{Regexp.escape(flag)}[ \t]*[\\?+:!]?=[ \t]*((?:.*\\\n)*.*)$/, 1] ||
              raise(ArgumentError, "expected to find make variable #{flag.inspect}")
          end
        end

        module Language
          module Perl
            module Shebang
              PERL_SHEBANG_REGEX = %r{\A#! ?(?:/usr/bin/(?:env )?)?perl( |$)}
              PERL_SHEBANG_MAX_LENGTH = 21

              def perl_shebang_rewrite_info(perl_path)
                Utils::Shebang::RewriteInfo.new(PERL_SHEBANG_REGEX, PERL_SHEBANG_MAX_LENGTH, "#{perl_path}\\1")
              end

              # A brewed perl when the formula depends on one, else whichever perl is on PATH.
              def detected_perl_shebang(formula = self)
                brewed = formula_opt_bin("perl")/"perl.exe"
                perl = formula.deps.any? { |d| d.name == "perl" } && brewed.exist? ? brewed : "/usr/bin/env perl"
                perl_shebang_rewrite_info(perl)
              end
            end
          end
        end

        class Pathname
          METAFILE_LICENSES = %w[copying copyright license licence].freeze
          METAFILE_EXTENSIONS = %w[.adoc .asc .asciidoc .creole .html .markdown .md .mdown .mediawiki .mkdn
                                   .org .pod .rdoc .rst .rtf .textile .txt .wiki].freeze
          METAFILE_BASENAMES = %w[about authors changelog changes history news notes notice readme todo].freeze

          def install_metafiles(from = Pathname.pwd)
            Pathname(from).children.each do |p|
              next if p.directory? || p.size.zero?

              name = p.basename.to_s.downcase
              ext = File.extname(name)
              base = METAFILE_EXTENSIONS.include?(ext) ? File.basename(name, ext) : name
              next unless METAFILE_LICENSES.include?(name.split(/\.|-/).first) || METAFILE_BASENAMES.include?(base)

              install p
            end
          end
        end
        """;

    static IEnumerable<string> Strings(Interp i, object v) => v is RArray arr ? arr.L.Select(i.ToS) : [i.ToS(v)];

    static int MakeJobs(Interp i) =>
        i.Env.TryGetValue("HOMEBREW_MAKE_JOBS", out var j) && int.TryParse(j, out var n) && n > 0 ? n : Environment.ProcessorCount;

    // Reproducible like Homebrew's SOURCE_DATE_EPOCH.
    static DateTime BuildTime(Interp i) =>
        i.Env.TryGetValue("SOURCE_DATE_EPOCH", out var e) && long.TryParse(e, out var s)
            ? DateTimeOffset.FromUnixTimeSeconds(s).UtcDateTime : DateTime.UtcNow;

    // Utils.safe_popen_read semantics: run argv directly, return stdout, fail on non-zero exit.
    static string Capture(Interp i, List<string> argv, Dictionary<string, string> env)
    {
        var psi = new System.Diagnostics.ProcessStartInfo(i.ResolveProgram(argv[0]))
        { RedirectStandardOutput = true, UseShellExecute = false, WorkingDirectory = i.Cwd };
        foreach (var x in i.PendingArguments(argv[0]).Concat(argv.Skip(1))) psi.ArgumentList.Add(x);
        i.ApplyEnv(psi);
        foreach (var kv in env) psi.Environment[kv.Key] = kv.Value;
        using var proc = System.Diagnostics.Process.Start(psi)!;
        var output = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit();
        if (proc.ExitCode != 0)
            throw i.Error(BuildError(i), $"Failure while executing; `{string.Join(" ", argv)}` exited with {proc.ExitCode}.");
        return output.Replace("\r\n", "\n");
    }

    // ---- minimal Time (UTC) ----
    static RClass TimeClass(Interp it) => it.ObjectClass.Consts.GetValueOrDefault("Time") as RClass ?? DefineTime(it);

    static RObject TimeObject(Interp it, DateTime t) => new() { Class = TimeClass(it), Host = DateTime.SpecifyKind(t, DateTimeKind.Utc) };

    static RClass DefineTime(Interp it)
    {
        if (it.ObjectClass.Consts.GetValueOrDefault("Time") is RClass existing) return existing;
        var T = it.DefineClass("Time");
        DateTime D(object o) => (DateTime)((RObject)o).Host;
        it.DefS(T, "now", (i, s, a, b) => TimeObject(i, BuildTime(i)));
        it.DefS(T, "at", (i, s, a, b) => TimeObject(i, DateTimeOffset.FromUnixTimeSeconds(i.Int(a[0])).UtcDateTime));
        it.Def(T, "utc getutc localtime", (i, s, a, b) => s);
        it.Def(T, "to_i", (i, s, a, b) => new DateTimeOffset(D(s)).ToUnixTimeSeconds());
        it.Def(T, "year", (i, s, a, b) => (long)D(s).Year);
        it.Def(T, "month mon", (i, s, a, b) => (long)D(s).Month);
        it.Def(T, "day mday", (i, s, a, b) => (long)D(s).Day);
        it.Def(T, "iso8601 xmlschema", (i, s, a, b) => Interp.S(D(s).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)));
        it.Def(T, "to_s inspect", (i, s, a, b) => Interp.S(D(s).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC"));
        it.Def(T, "strftime", (i, s, a, b) => Interp.S(Strftime(D(s), i.ToS(a[0]))));
        return T;
    }

    static string Strftime(DateTime t, string f)
    {
        var sb = new StringBuilder();
        for (int k = 0; k < f.Length; k++)
        {
            if (f[k] != '%' || k + 1 >= f.Length) { sb.Append(f[k]); continue; }
            var c = f[++k];
            sb.Append(c switch
            {
                'Y' => t.Year.ToString("D4"), 'm' => t.Month.ToString("D2"), 'd' => t.Day.ToString("D2"),
                'H' => t.Hour.ToString("D2"), 'M' => t.Minute.ToString("D2"), 'S' => t.Second.ToString("D2"),
                'F' => t.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), 'T' => t.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                'y' => (t.Year % 100).ToString("D2"), 'b' => t.ToString("MMM", CultureInfo.InvariantCulture),
                'B' => t.ToString("MMMM", CultureInfo.InvariantCulture), 'z' => "+0000", 'Z' => "UTC", 's' => new DateTimeOffset(t).ToUnixTimeSeconds().ToString(),
                'j' => t.DayOfYear.ToString("D3"), '%' => "%", _ => "%" + c,
            });
        }
        return sb.ToString();
    }
}
