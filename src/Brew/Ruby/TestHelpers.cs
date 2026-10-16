using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Brew.Formulae;

namespace Brew.Ruby;

// What `test do ... end` blocks can use: Homebrew::Assertions (shell_output, pipe_output), Minitest::Assertions,
// testpath, and the Kernel/Process bits formula tests lean on (which, free_port, spawn, Open3, with_env).
public static partial class FormulaLoader
{
    static partial void DefineTestHelpers(Interp it, RClass formula) => TestSupport.Define(it, formula);

    // Kept in one nested class so these helper names can't collide with FormulaLoader's own.
    static class TestSupport
    {
        static readonly object[] None = Array.Empty<object>();

        enum Sink { Inherit, Capture, Discard, Err, Merge }

        internal static void Define(Interp it, RClass formula)
        {
            var F = formula;
            var mt = it.ConstGet(it.ObjectClass, "Minitest") as RClass ?? it.DefineModule("Minitest");
            var assertion = mt.Consts.GetValueOrDefault("Assertion") as RClass ?? it.DefineClass("Assertion", it.ExceptionClass, mt);
            var skip = mt.Consts.GetValueOrDefault("Skip") as RClass ?? it.DefineClass("Skip", assertion, mt);
            var A = mt.Consts.GetValueOrDefault("Assertions") as RClass ?? it.DefineModule("Assertions", mt);
            if (!F.Includes.Contains(it.FileUtilsModule)) F.Includes.Add(it.FileUtilsModule);   // Formula includes FileUtils
            if (!F.Includes.Contains(A)) F.Includes.Add(A);
            DefineMinitest(it, A, assertion, skip);
            BuildError(it);   // defined up front so tests can `assert_raises(BuildError)`

            // Wrap the test block so it runs in Homebrew's test environment (Formula#run_test).
            it.DefS(F, "test", (i, self, a, b) =>
            {
                var spec = SpecOf((RClass)self);
                spec.TestBlock = b == null ? null : new RProc { Self = b.Self, Fn = (ii, s, args, blk) => RunTestBlock(ii, s, b) };
                return null;
            });

            it.Def(F, "testpath", (i, self, a, b) => i.NewPath(TestPath(i, self)));
            it.Def(F, "test_fixtures", (i, self, a, b) => i.NewPath(TestFixture(i, i.PathStr(a[0]))));

            // Homebrew::Assertions#shell_output
            it.Def(F, "shell_output", (i, self, a, b) =>
            {
                i.Arity(a, 1, 2);
                var cmd = a[0];
                var result = a.Length > 1 ? a[1] : 0L;
                i.Invoke(self, "ohai", new object[] { Interp.S(i.ToS(cmd)) }, null);
                var line = CommandLine(i, self, cmd, assertion);
                var (output, _, code) = Shell(i, line, Sink.Capture, Sink.Err, null, false, false, out var binary);
                if (result != null) AssertExit(i, self, assertion, result, code, output);
                return new RString(output) { Binary = binary };
            });

            // Homebrew::Assertions#pipe_output
            it.Def(F, "pipe_output", (i, self, a, b) =>
            {
                i.Arity(a, 1, 3);
                var cmd = a[0];
                var input = a.Length > 1 && a[1] != null ? i.ToS(a[1]) : null;
                var result = a.Length > 2 ? a[2] : null;
                i.Invoke(self, "ohai", new object[] { Interp.S(i.ToS(cmd)) }, null);
                var line = CommandLine(i, self, cmd, assertion);
                var (output, _, code) = Shell(i, line, Sink.Capture, Sink.Err, input, true,
                    a.Length > 1 && a[1] is RString { Binary: true }, out var binary);
                if (result != null) AssertExit(i, self, assertion, result, code, output);
                return new RString(output) { Binary = binary };
            });

            // Kernel#quiet_system: like system, with stdout and stderr discarded
            it.Def(F, "quiet_system", (i, self, a, b) =>
            {
                try { return Run(i, a, Sink.Discard, Sink.Discard) == 0; }
                catch (RubyError e) when (i.IsA(e.Exc, i.ENOENT)) { SetStatus(i, 127); return null; }
            });

            // Formula#mkdir: FileUtils.mkdir_p, then chdir into it for the block
            it.Def(F, "mkdir", (i, self, a, b) =>
            {
                foreach (var p in a[0] is RArray arr ? arr.L : new List<object> { a[0] }) Directory.CreateDirectory(i.Abs(i.PathStr(p)));
                return b == null ? a[0] : i.Invoke(i.DirClass, "chdir", new[] { a[0] }, b);
            });

            DefineProcessHelpers(it);
        }

        // Homebrew's test fixtures (Library/Homebrew/test/support/fixtures, BSD-2-Clause) aren't shipped with
        // brew-windows; the ones a test asks for are fetched from Homebrew/brew at a pinned commit and cached.
        const string FixturesCommit = "f8d1b25f33e032707f151123aca0eac3a239c676";

        static string TestFixture(Interp i, string name)
        {
            if (name.Length == 0 || name.Split('/', '\\').Any(p => p is "" or "." or ".."))
                throw i.Error(i.ArgumentError, $"invalid test fixture: {name}");
            var cache = i.Env.GetValueOrDefault("HOMEBREW_CACHE") is { Length: > 0 } c ? c
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Homebrew", "cache");
            var file = Path.Combine(cache, "test_fixtures", FixturesCommit[..12], name.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(file)) return file;
            var url = $"https://raw.githubusercontent.com/Homebrew/brew/{FixturesCommit}/Library/Homebrew/test/support/fixtures/{name}";
            using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromMinutes(2) };
            using var response = http.GetAsync(url).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode) throw i.Error(i.ENOENT, $"No such file or directory - test fixture {name} ({(int)response.StatusCode})");
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            var partial = file + ".incomplete";
            using (var output = File.Create(partial)) response.Content.CopyToAsync(output).GetAwaiter().GetResult();
            File.Move(partial, file, true);
            return file;
        }

        // ---- Formula#run_test environment ----

        // RunTest stores the test path as a plain .NET string.
        static string TestPath(Interp i, object self) => (self as RObject)?.GetIvar("@__testpath") switch
        {
            string s => s,
            null => i.Cwd,
            var v => i.PathStr(v),
        };

        static object RunTestBlock(Interp i, object self, RProc block)
        {
            var saved = new Dictionary<string, string>(i.Env, StringComparer.OrdinalIgnoreCase);
            var savedCwd = i.Cwd;
            try
            {
                var home = TestPath(i, self);
                var brewPrefix = ((self as RObject)?.Host as InstallContext)?.BrewPrefix;
                var sensitive = new Regex("(cookie|key|token|password|passphrase|auth)", RegexOptions.IgnoreCase);
                foreach (var k in i.Env.Keys.Where(k => sensitive.IsMatch(k)).ToList()) i.Env.Remove(k);

                var term = i.Env.GetValueOrDefault("TERM");
                i.Env["TERM"] = "dumb";
                if (term != null) i.Env["HOMEBREW_TERM"] = term; else i.Env.Remove("HOMEBREW_TERM");
                i.Env.Remove("HOMEBREW_PATH");
                if (brewPrefix != null)
                {
                    var brewBin = Path.Combine(brewPrefix, "bin");
                    var path = i.Env.GetValueOrDefault("PATH", "");
                    if (!path.Split(';').Any(p => string.Equals(Path.TrimEndingDirectorySeparator(p), brewBin, StringComparison.OrdinalIgnoreCase)))
                        i.Env["PATH"] = path.Length == 0 ? brewBin : path.TrimEnd(';') + ";" + brewBin;
                }
                // Formula#common_sandbox_env / #test_sandbox_env
                i.Env["HOME"] = home;
                i.Env["USERPROFILE"] = home;
                i.Env["GIT_CONFIG_GLOBAL"] = Path.Combine(home, ".gitconfig");
                i.Env["GIT_TERMINAL_PROMPT"] = "0";
                i.Env["GOENV"] = "off";
                i.Env["PIP_CONFIG_FILE"] = "NUL";
                i.Env["NPM_CONFIG_USERCONFIG"] = "NUL";
                if (!i.Env.ContainsKey("CURL_HOME")) i.Env["CURL_HOME"] = home;
                i.Env["PYTHONDONTWRITEBYTECODE"] = "1";
                // macOS/Linux test runs have a UTF-8 locale; Windows pipes default to the ANSI code page.
                i.Env["PYTHONUTF8"] = "1";
                i.Env["PYTHONIOENCODING"] = "utf-8";
                i.Env["XDG_CONFIG_HOME"] = Path.Combine(home, ".config");
                // Windows' known folders (SHGetKnownFolderPath) live under the profile.
                i.Env["LOCALAPPDATA"] = Path.Combine(home, "AppData", "Local");
                i.Env["APPDATA"] = Path.Combine(home, "AppData", "Roaming");
                Directory.CreateDirectory(i.Env["LOCALAPPDATA"]);
                Directory.CreateDirectory(i.Env["APPDATA"]);
                // Shell folders registered as %USERPROFILE%\... resolve inside the test profile; they must exist.
                foreach (var folder in new[] { "Documents", "Desktop", "Downloads" }) Directory.CreateDirectory(Path.Combine(home, folder));

                // Formula#setup_home and the identity sandboxed `git commit` needs
                Directory.CreateDirectory(home);
                File.WriteAllText(Path.Combine(home, ".bazelrc"), $"startup --output_user_root={home.Replace('\\', '/')}/_bazel");
                File.WriteAllText(Path.Combine(home, ".gitconfig"), "[user]\n  name = Homebrew\n  email = brew@example.com\n");

                // Formula#system, finding bin/"tool" as tool.exe or tool.cmd through PATHEXT
                i.Def(i.SingletonOf((RObject)self), "system", (ii, s, a, b) =>
                {
                    var line = string.Join(" ", a.Where(x => x is not RHash).Select(ii.ToS));
                    ii.Invoke(s, "ohai", new object[] { Interp.S(line) }, null);
                    bool ok;
                    try { ok = Run(ii, a, Sink.Inherit, Sink.Inherit) == 0; }
                    catch (RubyError e) when (ii.IsA(e.Exc, ii.ENOENT)) { ok = false; }
                    if (!ok) throw ii.Error(BuildError(ii), $"Failed executing: {line}");
                    return null;
                });

                i.Cwd = home;
                return i.CallProc(block, None, null, self, null);
            }
            finally
            {
                i.Env.Clear();
                foreach (var kv in saved) i.Env[kv.Key] = kv.Value;
                i.Cwd = savedCwd;
            }
        }

        // ---- Minitest::Assertions (minitest 6) ----

        static void DefineMinitest(Interp it, RClass A, RClass assertion, RClass skip)
        {
            object Arg(object[] a, int n) => a.Length > n ? a[n] : null;
            void Count(object self)
            {
                if (self is RObject o && !o.Frozen) o.SetIvar("@assertions", (o.GetIvar("@assertions") is long n ? n : 0L) + 1);
            }
            // Minitest's `assert test, msg`
            bool Assert(Interp i, object self, bool ok, Func<string> msg)
            {
                Count(self);
                if (!ok) throw i.Error(assertion, msg());
                return true;
            }
            // `msg = message(msg, ending) { default }`
            Func<string> Message(Interp i, object msg, string ending, Func<string> dflt) => () =>
            {
                var m = msg;
                if (m is RProc p) m = Interp.S(Chomp(i.ToS(i.CallProc(p, None)), "."));
                var custom = m != null && i.ToS(m).Length > 0 ? i.ToS(m) + ".\n" : "";
                return custom + dflt() + (ending ?? ".");
            };
            string Pp(Interp i, object o) => MuPp(i, o);
            void RespondTo(Interp i, object self, object obj, string meth, object msg) =>
                Assert(i, self, i.RespondTo(obj, meth), Message(i, msg, null, () => $"Expected {Pp(i, obj)} ({i.ClassOf(obj).Name}) to respond to #{meth}"));
            RRegex Matcher(Interp i, object m) => m is RString s ? i.MakeRegex(RegexpEscape(s.V), "") : m as RRegex;
            bool Matches(Interp i, object matcher, object obj) =>
                Interp.Truthy(matcher is RRegex r && obj is RString os ? (object)i.RegexMatch(r, os.V, 0) : i.Invoke(matcher, "=~", new[] { obj }, null));
            string OpName(Interp i, object op) => i.StrOrSym(op);

            it.Def(A, "assertions", (i, self, a, b) => ((RObject)self).GetIvar("@assertions") ?? 0L);
            it.Def(A, "assertions=", (i, self, a, b) => { ((RObject)self).SetIvar("@assertions", a[0]); return a[0]; });

            it.Def(A, "assert", (i, self, a, b) =>
            {
                i.Arity(a, 1, 2);
                var test = a[0];
                var msg = Arg(a, 1);
                return Assert(i, self, Interp.Truthy(test), () => msg == null ? $"Expected {Pp(i, test)} to be truthy." : msg is RProc p ? i.ToS(i.CallProc(p, None)) : i.ToS(msg));
            });
            it.Def(A, "refute", (i, self, a, b) =>
            {
                i.Arity(a, 1, 2);
                var test = a[0];
                var msg = Arg(a, 1);
                return Assert(i, self, !Interp.Truthy(test), () => msg == null ? $"Expected {Pp(i, test)} to not be truthy." : msg is RProc p ? i.ToS(i.CallProc(p, None)) : i.ToS(msg));
            });
            it.Def(A, "flunk", (i, self, a, b) =>
            {
                var msg = Arg(a, 0);
                return Assert(i, self, false, () => msg == null ? "Epic Fail!" : msg is RProc p ? i.ToS(i.CallProc(p, None)) : i.ToS(msg));
            });
            it.Def(A, "pass", (i, self, a, b) => Assert(i, self, true, null));
            it.Def(A, "skip", (i, self, a, b) => throw i.Error(skip, Arg(a, 0) is { } m ? i.ToS(m) : "Skipped, no message given"));

            it.Def(A, "assert_equal", (i, self, a, b) =>
            {
                i.Arity(a, 2, 3);
                object exp = a[0], act = a[1];
                var result = Assert(i, self, i.Equal(exp, act), Message(i, Arg(a, 2), "", () => Diff(i, exp, act)));
                if (exp == null)
                    Assert(i, self, false, Message(i, Interp.S("Use assert_nil if expecting nil."), null, () => "Expected nil to not be nil"));
                return result;
            });
            it.Def(A, "refute_equal", (i, self, a, b) =>
            {
                i.Arity(a, 2, 3);
                object exp = a[0], act = a[1];
                return Assert(i, self, !i.Equal(exp, act), Message(i, Arg(a, 2), null, () => $"Expected {Pp(i, act)} to not be equal to {Pp(i, exp)}"));
            });
            it.Def(A, "assert_nil", (i, self, a, b) =>
                Assert(i, self, a[0] == null, Message(i, Arg(a, 1), null, () => $"Expected {Pp(i, a[0])} to be nil")));
            it.Def(A, "refute_nil", (i, self, a, b) =>
                Assert(i, self, a[0] != null, Message(i, Arg(a, 1), null, () => $"Expected {Pp(i, a[0])} to not be nil")));

            it.Def(A, "assert_empty", (i, self, a, b) =>
            {
                RespondTo(i, self, a[0], "empty?", null);
                return Assert(i, self, Interp.Truthy(i.Invoke(a[0], "empty?", None, null)), Message(i, Arg(a, 1), null, () => $"Expected {Pp(i, a[0])} to be empty"));
            });
            it.Def(A, "refute_empty", (i, self, a, b) =>
            {
                RespondTo(i, self, a[0], "empty?", null);
                return Assert(i, self, !Interp.Truthy(i.Invoke(a[0], "empty?", None, null)), Message(i, Arg(a, 1), null, () => $"Expected {Pp(i, a[0])} to not be empty"));
            });

            it.Def(A, "assert_includes", (i, self, a, b) =>
            {
                i.Arity(a, 2, 3);
                RespondTo(i, self, a[0], "include?", null);
                return Assert(i, self, Interp.Truthy(i.Invoke(a[0], "include?", new[] { a[1] }, null)),
                    Message(i, Arg(a, 2), null, () => $"Expected {Pp(i, a[0])} to include {Pp(i, a[1])}"));
            });
            it.Def(A, "refute_includes", (i, self, a, b) =>
            {
                i.Arity(a, 2, 3);
                RespondTo(i, self, a[0], "include?", null);
                return Assert(i, self, !Interp.Truthy(i.Invoke(a[0], "include?", new[] { a[1] }, null)),
                    Message(i, Arg(a, 2), null, () => $"Expected {Pp(i, a[0])} to not include {Pp(i, a[1])}"));
            });

            it.Def(A, "assert_match", (i, self, a, b) =>
            {
                i.Arity(a, 2, 3);
                RespondTo(i, self, a[0], "=~", null);
                var matcher = a[0] is RString ? Matcher(i, a[0]) : a[0];
                var obj = a[1];
                Assert(i, self, Matches(i, matcher, obj), Message(i, Arg(a, 2), null, () => $"Expected {Pp(i, matcher)} to match {Pp(i, obj)}"));
                return i.Globals.GetValueOrDefault("$~");
            });
            it.Def(A, "refute_match", (i, self, a, b) =>
            {
                i.Arity(a, 2, 3);
                RespondTo(i, self, a[0], "=~", null);
                var matcher = a[0] is RString ? Matcher(i, a[0]) : a[0];
                var obj = a[1];
                return Assert(i, self, !Matches(i, matcher, obj), Message(i, Arg(a, 2), null, () => $"Expected {Pp(i, matcher)} to not match {Pp(i, obj)}"));
            });

            it.Def(A, "assert_predicate", (i, self, a, b) =>
            {
                i.Arity(a, 2, 3);
                return Assert(i, self, Interp.Truthy(i.Invoke(a[0], OpName(i, a[1]), None, null)), Message(i, Arg(a, 2), null, () => $"Expected {Pp(i, a[0])} to be {i.ToS(a[1])}"));
            });
            it.Def(A, "refute_predicate", (i, self, a, b) =>
            {
                i.Arity(a, 2, 3);
                return Assert(i, self, !Interp.Truthy(i.Invoke(a[0], OpName(i, a[1]), None, null)), Message(i, Arg(a, 2), null, () => $"Expected {Pp(i, a[0])} to not be {i.ToS(a[1])}"));
            });
            it.Def(A, "assert_operator", (i, self, a, b) =>
            {
                i.Arity(a, 2, 4);
                if (a.Length == 2) return i.Invoke(self, "assert_predicate", a, null);
                return Assert(i, self, Interp.Truthy(i.Invoke(a[0], OpName(i, a[1]), new[] { a[2] }, null)),
                    Message(i, Arg(a, 3), null, () => $"Expected {Pp(i, a[0])} to be {i.ToS(a[1])} {Pp(i, a[2])}"));
            });
            it.Def(A, "refute_operator", (i, self, a, b) =>
            {
                i.Arity(a, 2, 4);
                if (a.Length == 2) return i.Invoke(self, "refute_predicate", a, null);
                return Assert(i, self, !Interp.Truthy(i.Invoke(a[0], OpName(i, a[1]), new[] { a[2] }, null)),
                    Message(i, Arg(a, 3), null, () => $"Expected {Pp(i, a[0])} to not be {i.ToS(a[1])} {Pp(i, a[2])}"));
            });

            bool Exists(Interp i, object p) => ExistingPath(i, i.PathStr(p)) != null;
            it.Def(A, "assert_path_exists", (i, self, a, b) =>
                Assert(i, self, Exists(i, a[0]), Message(i, Arg(a, 1), null, () => $"Expected path '{i.ToS(a[0])}' to exist")));
            it.Def(A, "refute_path_exists", (i, self, a, b) =>
                Assert(i, self, !Exists(i, a[0]), Message(i, Arg(a, 1), null, () => $"Expected path '{i.ToS(a[0])}' to not exist")));

            it.Def(A, "assert_respond_to", (i, self, a, b) => { RespondTo(i, self, a[0], i.StrOrSym(a[1]), Arg(a, 2) is RHash ? null : Arg(a, 2)); return true; });
            it.Def(A, "refute_respond_to", (i, self, a, b) =>
                Assert(i, self, !i.RespondTo(a[0], i.StrOrSym(a[1])), Message(i, Arg(a, 2) is RHash ? null : Arg(a, 2), null, () => $"Expected {Pp(i, a[0])} to not respond to {i.StrOrSym(a[1])}")));

            it.Def(A, "assert_kind_of", (i, self, a, b) =>
                Assert(i, self, a[0] is RClass c && i.IsA(a[1], c), Message(i, Arg(a, 2), null, () => $"Expected {Pp(i, a[1])} to be a kind of {i.ToS(a[0])}, not {i.ClassOf(a[1]).Name}")));
            it.Def(A, "refute_kind_of", (i, self, a, b) =>
                Assert(i, self, !(a[0] is RClass c && i.IsA(a[1], c)), Message(i, Arg(a, 2), null, () => $"Expected {Pp(i, a[1])} to not be a kind of {i.ToS(a[0])}")));
            it.Def(A, "assert_instance_of", (i, self, a, b) =>
                Assert(i, self, i.ClassOf(a[1]) == a[0], Message(i, Arg(a, 2), null, () => $"Expected {Pp(i, a[1])} to be an instance of {i.ToS(a[0])}, not {i.ClassOf(a[1]).Name}")));
            it.Def(A, "refute_instance_of", (i, self, a, b) =>
                Assert(i, self, i.ClassOf(a[1]) != a[0], Message(i, Arg(a, 2), null, () => $"Expected {Pp(i, a[1])} to not be an instance of {i.ToS(a[0])}")));

            string SameData(Interp i, object exp, object act, string verb) =>
                $"Expected {Pp(i, act)} (oid={i.ToS(i.Invoke(act, "object_id", None, null))}) to {verb} the same as {Pp(i, exp)} (oid={i.ToS(i.Invoke(exp, "object_id", None, null))})";
            it.Def(A, "assert_same", (i, self, a, b) =>
                Assert(i, self, Same(a[0], a[1]), Message(i, Arg(a, 2), null, () => SameData(i, a[0], a[1], "be"))));
            it.Def(A, "refute_same", (i, self, a, b) =>
                Assert(i, self, !Same(a[0], a[1]), Message(i, Arg(a, 2), null, () => SameData(i, a[0], a[1], "not be"))));

            object Delta(Interp i, object exp, object act) => exp is long x && act is long y ? (object)Math.Abs(x - y) : Math.Abs(i.Num(exp) - i.Num(act));
            object InDelta(Interp i, object self, object[] a, bool refute)
            {
                i.Arity(a, 2, 4);
                object exp = a[0], act = a[1], delta = Arg(a, 2) ?? 0.001;
                var n = Delta(i, exp, act);
                bool within = i.Num(delta) >= i.Num(n);
                return Assert(i, self, refute ? !within : within,
                    Message(i, Arg(a, 3), null, () => $"Expected |{i.ToS(exp)} - {i.ToS(act)}| ({i.ToS(n)}) to {(refute ? "not " : "")}be <= {i.ToS(delta)}"));
            }
            it.Def(A, "assert_in_delta", (i, self, a, b) => InDelta(i, self, a, false));
            it.Def(A, "refute_in_delta", (i, self, a, b) => InDelta(i, self, a, true));
            object InEpsilon(Interp i, object self, object[] a, bool refute)
            {
                i.Arity(a, 2, 4);
                var eps = i.Num(Arg(a, 2) ?? 0.001);
                var delta = Math.Min(Math.Abs(i.Num(a[0])), Math.Abs(i.Num(a[1]))) * eps;
                return InDelta(i, self, new[] { a[0], a[1], delta, Arg(a, 3) }, refute);
            }
            it.Def(A, "assert_in_epsilon", (i, self, a, b) => InEpsilon(i, self, a, false));
            it.Def(A, "refute_in_epsilon", (i, self, a, b) => InEpsilon(i, self, a, true));

            it.Def(A, "assert_raises", (i, self, a, b) =>
            {
                if (b == null) Assert(i, self, false, () => "assert_raises requires a block to capture errors.");
                var exp = a.ToList();
                var msg = exp.Count > 0 && exp[^1] is RString ms ? ms.V + ".\n" : "";
                if (msg.Length > 0) exp.RemoveAt(exp.Count - 1);
                if (exp.Count == 0) exp.Add(i.StandardError);
                RObject e;
                try
                {
                    i.CallProc(b, None);
                    e = null;
                }
                catch (Exception ex) when (ex is not (BreakSignal or NextSignal or ReturnSignal or ThrowSignal or RetrySignal))
                {
                    e = i.ToRubyException(ex);
                }
                if (e != null)
                {
                    if (exp.Any(c => c is RClass k && i.IsA(e, k))) { Assert(i, self, true, null); return e; }
                    if (i.IsA(e, assertion) || i.IsA(e, i.SystemExit)) throw new RubyError(e);
                    var where = i.CurLine > 0 ? $"{i.CurrentFile}:{i.CurLine}" : i.CurrentFile;
                    Assert(i, self, false, () => string.Join("\n",
                        $"{msg}{Pp(i, new RArray(exp))} exception expected, not",
                        $"Class: <{e.Class.Name}>",
                        $"Message: <{Interp.InspectStr(i.ToS(i.Invoke(e, "message", None, null)))}>",
                        "---Backtrace---",
                        where,
                        "---------------"));
                }
                var shown = exp.Count == 1 ? exp[0] : new RArray(exp);
                return Assert(i, self, false, () => $"{msg}{Pp(i, shown)} expected but nothing was raised.");
            });
            it.Def(A, "assert_throws", (i, self, a, b) =>
            {
                var tag = a[0];
                bool thrown = true;
                object value = null;
                try { i.CallProc(b, None); thrown = false; }
                catch (ThrowSignal t) when (Interp.Eql(t.Tag, tag) || ReferenceEquals(t.Tag, tag)) { value = t.Value; }
                Assert(i, self, thrown, Message(i, Arg(a, 1), null, () => $"Expected {Pp(i, tag)} to have been thrown"));
                return value;
            });
        }

        static string Chomp(string s, string suffix) => s.EndsWith(suffix) ? s[..^suffix.Length] : s;

        // Ruby's BasicObject#equal? (identity; immediates compare by value).
        static bool Same(object x, object y) => (x, y) switch
        {
            (null, null) => true,
            (long a, long b) => a == b,
            (double a, double b) => a.Equals(b),
            (bool a, bool b) => a == b,
            _ => ReferenceEquals(x, y),
        };

        // mu_pp: obj.inspect (Regexp#inspect escapes '/', which the interpreter's Inspect does not)
        static string MuPp(Interp i, object o) => o is RRegex r ? RegexpInspect(r) : i.Inspect(o);

        static string RegexpInspect(RRegex r)
        {
            var sb = new StringBuilder("/");
            for (int k = 0; k < r.Source.Length; k++)
            {
                char c = r.Source[k];
                if (c == '\\' && k + 1 < r.Source.Length) { sb.Append(c).Append(r.Source[++k]); continue; }
                if (c == '/') sb.Append('\\');
                sb.Append(c);
            }
            return sb.Append('/').Append(r.Flags).ToString();
        }

        // Regexp.escape
        static string RegexpEscape(string s)
        {
            var sb = new StringBuilder();
            foreach (var c in s)
            {
                switch (c)
                {
                    case '\n': sb.Append("\\n"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\v': sb.Append("\\v"); break;
                    case ' ': sb.Append("\\ "); break;
                    case '[' or ']' or '{' or '}' or '(' or ')' or '|' or '-' or '*' or '.' or '\\' or '?' or '+' or '^' or '$' or '#':
                        sb.Append('\\').Append(c);
                        break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        // Minitest::Assertions#diff (with `diff -u` available, as on macOS/Linux)
        static string Diff(Interp i, object exp, object act)
        {
            var expect = ForDiff(MuPp(i, exp));
            var butwas = ForDiff(MuPp(i, act));
            bool e1 = expect.Contains('\n'), e2 = expect.Contains("\\n");
            bool b1 = butwas.Contains('\n'), b2 = butwas.Contains("\\n");
            bool need = e1 ^ e2 || b1 ^ b2 || expect.Length > 30 || butwas.Length > 30 || expect == butwas;
            if (!need) return $"Expected: {MuPp(i, exp)}\n  Actual: {MuPp(i, act)}";
            var result = UnifiedDiff(expect, butwas);
            if (result.Length > 0) return result;
            var klass = i.ClassOf(exp).Name;
            return $"No visible difference in the {klass}#inspect output.\nYou should look at the implementation of #== on {klass} or its members.\n{expect}";
        }

        // mu_pp_for_diff
        static string ForDiff(string str)
        {
            bool single = Regex.IsMatch(str, @"(?<!\\|^)\\n", RegexOptions.Multiline);
            bool dbl = Regex.IsMatch(str, @"(?<=\\|^)\\n", RegexOptions.Multiline);
            if (single ^ dbl)
                str = Regex.Replace(str, @"\\?\\n", m => single ? (m.Value == "\\n" ? "\n" : m.Value) : (m.Value == "\\\\n" ? "\\n\n" : m.Value));
            return Regex.Replace(str, ":0x[a-fA-F0-9]{4,}", ":0xXXXXXX");
        }

        // `diff -u` of two strings as written by IO#puts, with Minitest's header rewrite.
        static string UnifiedDiff(string a, string b)
        {
            string[] Lines(string s) => (s.EndsWith('\n') ? s[..^1] : s).Split('\n');
            var x = Lines(a);
            var y = Lines(b);
            int n = x.Length, m = y.Length;
            var lcs = new int[n + 1, m + 1];
            for (int p = n - 1; p >= 0; p--)
                for (int q = m - 1; q >= 0; q--)
                    lcs[p, q] = x[p] == y[q] ? lcs[p + 1, q + 1] + 1 : Math.Max(lcs[p + 1, q], lcs[p, q + 1]);
            var ops = new List<(char Op, string Text)>();
            for (int p = 0, q = 0; p < n || q < m;)
            {
                if (p < n && q < m && x[p] == y[q]) { ops.Add((' ', x[p])); p++; q++; }
                else if (q >= m || (p < n && lcs[p + 1, q] >= lcs[p, q + 1])) ops.Add(('-', x[p++]));
                else ops.Add(('+', y[q++]));
            }
            // within each change, deletions come first
            for (int s = 0; s < ops.Count;)
            {
                if (ops[s].Op == ' ') { s++; continue; }
                int e = s;
                while (e < ops.Count && ops[e].Op != ' ') e++;
                var run = ops.GetRange(s, e - s);
                ops.RemoveRange(s, e - s);
                ops.InsertRange(s, run.Where(o => o.Op == '-').Concat(run.Where(o => o.Op == '+')));
                s = e;
            }
            var changes = Enumerable.Range(0, ops.Count).Where(k => ops[k].Op != ' ').ToList();
            if (changes.Count == 0) return "";
            var aBefore = new int[ops.Count + 1];
            var bBefore = new int[ops.Count + 1];
            for (int k = 0; k < ops.Count; k++)
            {
                aBefore[k + 1] = aBefore[k] + (ops[k].Op != '+' ? 1 : 0);
                bBefore[k + 1] = bBefore[k] + (ops[k].Op != '-' ? 1 : 0);
            }
            const int context = 3;
            string Range(int start, int count) => count == 1 ? $"{start + 1}" : count == 0 ? $"{start},0" : $"{start + 1},{count}";
            var sb = new StringBuilder("--- expected\n+++ actual\n");
            void Hunk(int first, int last)
            {
                int from = Math.Max(0, first - context), to = Math.Min(ops.Count - 1, last + context);
                int aCount = aBefore[to + 1] - aBefore[from], bCount = bBefore[to + 1] - bBefore[from];
                sb.Append($"@@ -{Range(aBefore[from], aCount)} +{Range(bBefore[from], bCount)} @@\n");
                for (int k = from; k <= to; k++) sb.Append(ops[k].Op).Append(ops[k].Text).Append('\n');
            }
            int start = changes[0], end = changes[0];
            foreach (var c in changes.Skip(1))
            {
                if (c - end - 1 <= 2 * context) { end = c; continue; }
                Hunk(start, end);
                start = end = c;
            }
            Hunk(start, end);
            return sb.ToString();
        }

        // ---- processes ----

        // shell_output/pipe_output: a Pathname runs that file (after assert_path_exists), a String goes to the shell.
        static string CommandLine(Interp i, object self, object cmd, RClass assertion)
        {
            if (cmd is not RObject { Class: var c } || c != i.PathnameClass) return i.ToS(cmd);
            var text = i.ToS(cmd);
            i.Invoke(self, "assert_path_exists", new object[] { cmd, Interp.S($"Pathname '{text}' does not exist!") }, null);
            return "\"" + ExistingPath(i, text) + "\"";
        }

        // The file or directory at p, or (Windows) the program p names via PATHEXT: bin/"tool" is bin	ool.exe.
        static string ExistingPath(Interp i, string p)
        {
            var full = i.Abs(p);
            if (File.Exists(full) || Directory.Exists(full)) return full;
            foreach (var e in PathExts(i))
                if (File.Exists(full + e)) return OnDisk(full + e);
            return null;
        }

        static void AssertExit(Interp i, object self, RClass assertion, object result, int code, string output)
        {
            try { i.Invoke(self, "assert_equal", new[] { result, (long)code }, null); }
            catch (RubyError e) when (i.IsA(e.Exc, assertion))
            {
                if (i.Env.ContainsKey("HOMEBREW_VERBOSE")) i.Out.WriteLine(output.TrimEnd('\n'));
                throw;
            }
        }

        // One command string, run the way Ruby runs it with /bin/sh: a simple command is read here (PosixShell)
        // and started directly; pipelines, lists and the like go to a POSIX shell; cmd.exe is the last resort.
        static (string Out, string Err, int Code) Shell(Interp i, string line, Sink outMode, Sink errMode, string input, bool redirectIn,
            bool inputBinary, out bool binary)
        {
            var cmd = PosixShell.Parse(line, k => i.Env.GetValueOrDefault(k), i.Cwd);
            var program = cmd == null ? null : Runnable(i, cmd.Argv[0]);
            var bin = false;
            if (program == null)
            {
                var fallback = WithEnv(i, ShellEnv, () => Exec(i, FallbackPsi(i, line), outMode, errMode, input, redirectIn, inputBinary, out bin));
                binary = bin;
                return fallback;
            }

            if (cmd.HereString != null) { input = cmd.HereString; redirectIn = true; inputBinary = false; }
            else if (cmd.StdinFile != null)
            {
                var file = cmd.StdinFile == "NUL" ? null : i.Abs(cmd.StdinFile);
                if (file != null && !File.Exists(file))
                {
                    i.Err.WriteLine($"sh: {cmd.StdinFile}: No such file or directory");
                    SetStatus(i, 1);
                    binary = false;
                    return ("", "", 1);
                }
                input = file == null ? "" : Encoding.Latin1.GetString(File.ReadAllBytes(file));
                redirectIn = true;
                inputBinary = true;
            }
            Sink Of(PosixShell.Redirect r, bool stdout) => r.Kind switch
            {
                PosixShell.Target.Null => Sink.Discard,
                PosixShell.Target.File => stdout ? Sink.Capture : (cmd.Stdout.Kind == PosixShell.Target.File && cmd.Stdout.Path == r.Path ? Sink.Merge : Sink.Capture),
                PosixShell.Target.Out => stdout ? outMode : outMode == Sink.Inherit ? Sink.Inherit : Sink.Merge,
                _ => stdout ? Sink.Err : errMode,
            };
            var outSink = Of(cmd.Stdout, true);
            var errSink = Of(cmd.Stderr, false);
            var psi = new ProcessStartInfo(program);
            foreach (var p in cmd.Argv.Skip(1)) psi.ArgumentList.Add(p);
            var result = WithEnv(i, cmd.Assignments, () => Exec(i, psi, outSink, errSink, input, redirectIn, inputBinary, out bin));
            var isBinary = binary = bin;
            void Write(PosixShell.Redirect r, string text, bool bin)
            {
                var bytes = bin ? Encoding.Latin1.GetBytes(text) : new UTF8Encoding(false).GetBytes(text);
                using var f = new FileStream(r.Path, r.Append ? FileMode.Append : FileMode.Create);
                f.Write(bytes);
            }
            var output = result.Out;
            if (cmd.Stdout.Kind == PosixShell.Target.File) { Write(cmd.Stdout, output, isBinary); output = ""; binary = false; }
            if (cmd.Stderr.Kind == PosixShell.Target.File && errSink == Sink.Capture) Write(cmd.Stderr, result.Err, false);
            return (output, result.Err, result.Code);
        }

        static readonly Dictionary<string, string> ShellEnv = new() { ["MSYS2_ARG_CONV_EXCL"] = "*", ["MSYS_NO_PATHCONV"] = "1" };

        // Runs f with extra variables in the interpreter's environment (what `NAME=value cmd` does).
        static T WithEnv<T>(Interp i, Dictionary<string, string> vars, Func<T> f)
        {
            var saved = vars.Keys.ToDictionary(k => k, k => i.Env.GetValueOrDefault(k));
            foreach (var kv in vars) i.Env[kv.Key] = kv.Value;
            try { return f(); }
            finally
            {
                foreach (var kv in saved)
                    if (kv.Value == null) i.Env.Remove(kv.Key); else i.Env[kv.Key] = kv.Value;
            }
        }

        // The program a command word names, or null when it isn't one Windows can start (sh builtins, yes, cat...).
        static string Runnable(Interp i, string word)
        {
            if (word.IndexOfAny(new[] { '/', '\\', ':' }) < 0) return Which(i, word, i.Env.GetValueOrDefault("PATH", ""));
            var full = Resolve(i, word);
            return File.Exists(full) ? full : null;
        }

        // A line the reader can't run itself: a POSIX shell if there is one, else cmd.exe.
        static ProcessStartInfo FallbackPsi(Interp i, string line)
        {
            if (PosixShell.FindShell(k => i.Env.GetValueOrDefault(k)) is not { } bash) return ShellPsi(i, line);
            var psi = new ProcessStartInfo(bash);
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add("PATH=\"$PATH:/usr/bin\"; " + PosixShell.ForBash(line, PathExts(i)));
            return psi;
        }

        // A process for IO.popen's command string: started directly when it's a plain command, else via a shell.
        static ProcessStartInfo LinePsi(Interp i, string line)
        {
            var cmd = PosixShell.Parse(line, k => i.Env.GetValueOrDefault(k), i.Cwd);
            if (cmd is { Redirected: false, Assignments.Count: 0 } && Runnable(i, cmd.Argv[0]) is { } program)
            {
                var direct = new ProcessStartInfo(program);
                foreach (var p in cmd.Argv.Skip(1)) direct.ArgumentList.Add(p);
                return direct;
            }
            return FallbackPsi(i, line);
        }

        // cmd.exe runs the line; a leading "./tool" or "C:/x/bin/tool" program path gets backslashes so cmd can find it.
        static ProcessStartInfo ShellPsi(Interp i, string line)
        {
            var m = Regex.Match(line, @"^\s*([A-Za-z]:[\\/]|\.{1,2}[\\/])[^\s""&|<>()]*");
            if (m.Success && m.Value.Contains('/')) line = m.Value.Replace('/', '\\') + line[m.Length..];
            return new ProcessStartInfo("cmd.exe", "/d /s /c \"" + line + "\"");
        }

        // Kernel#system: one string is a shell line, several run the program directly.
        static int Run(Interp i, object[] a, Sink outMode, Sink errMode)
        {
            var parts = a.Where(x => x is not RHash).SelectMany(x => x is RArray arr ? arr.L : new List<object> { x }).Select(i.ToS).ToList();
            return parts.Count == 1 ? Shell(i, parts[0], outMode, errMode, null, false, false, out _).Code
                : Exec(i, Psi(i, a), outMode, errMode, null, false).Code;
        }

        // Kernel#system-style arguments: one string goes through the shell, several run the program directly.
        static ProcessStartInfo Psi(Interp i, object[] a)
        {
            var parts = a.Where(x => x is not RHash).SelectMany(x => x is RArray arr ? arr.L : new List<object> { x }).Select(i.ToS).ToList();
            if (parts.Count == 0) throw i.ArgCount(a, 1, -1);
            if (parts.Count == 1) return ShellPsi(i, parts[0]);
            var psi = new ProcessStartInfo(Resolve(i, parts[0]));
            foreach (var p in parts.Skip(1)) psi.ArgumentList.Add(p);
            return psi;
        }

        static string[] PathExts(Interp i) =>
            (i.Env.GetValueOrDefault("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD").Split(';', StringSplitOptions.RemoveEmptyEntries);

        static bool Executable(Interp i, string file) =>
            File.Exists(file) && PathExts(i).Any(e => file.EndsWith(e, StringComparison.OrdinalIgnoreCase));

        // Finds a program the way a shell would, but with the interpreter's cwd and PATH.
        static string Resolve(Interp i, string prog)
        {
            if (prog.IndexOfAny(new[] { '/', '\\', ':' }) >= 0)
            {
                var full = i.Abs(prog);
                if (Executable(i, full)) return full;
                foreach (var e in PathExts(i)) if (File.Exists(full + e)) return OnDisk(full + e);
                return full;
            }
            return Which(i, prog, i.Env.GetValueOrDefault("PATH", "")) ?? prog;
        }

        // Utils::Shell.which, plus PATHEXT
        static string Which(Interp i, string cmd, string path)
        {
            foreach (var entry in path.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                string full;
                try { full = Path.GetFullPath(Path.Combine(i.Abs(entry.Trim('"')), cmd)); }
                catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException) { continue; }
                if (Executable(i, full)) return full;
                if (Path.GetExtension(cmd).Length == 0)
                    foreach (var e in PathExts(i))
                        if (File.Exists(full + e)) return OnDisk(full + e);
            }
            return null;
        }

        // The file's real spelling ("cmd.exe", not "cmd.EXE" from PATHEXT).
        static string OnDisk(string file)
        {
            try { return Directory.EnumerateFiles(Path.GetDirectoryName(file)!, Path.GetFileName(file)).FirstOrDefault() ?? file; }
            catch (IOException) { return file; }
        }

        static void SetStatus(Interp i, int code)
        {
            i.Globals["$?"] = (long)code;
            i.Globals["$CHILD_STATUS"] = (long)code;
        }

        static (string Out, string Err, int Code) Exec(Interp i, ProcessStartInfo psi, Sink outMode, Sink errMode, string input, bool redirectIn) =>
            Exec(i, psi, outMode, errMode, input, redirectIn, false, out _);

        // Captured output is UTF-8 text with CRLFs folded, unless it isn't valid UTF-8: then it's kept byte for
        // byte (Latin-1, binary = true), so compressed or image data can be piped back in unchanged.
        static (string Out, string Err, int Code) Exec(Interp i, ProcessStartInfo psi, Sink outMode, Sink errMode, string input, bool redirectIn,
            bool inputBinary, out bool binary)
        {
            if (errMode == Sink.Err && ReferenceEquals(i.Err, Console.Error)) errMode = Sink.Inherit;
            if (outMode == Sink.Err && ReferenceEquals(i.Out, Console.Out)) outMode = Sink.Inherit;
            psi.UseShellExecute = false;
            psi.WorkingDirectory = i.Cwd;
            i.ApplyEnv(psi);
            psi.RedirectStandardOutput = outMode != Sink.Inherit;
            psi.RedirectStandardError = errMode != Sink.Inherit;
            psi.RedirectStandardInput = redirectIn || input != null;
            if (psi.RedirectStandardInput) psi.StandardInputEncoding = new UTF8Encoding(false);
            Process proc;
            try { proc = Process.Start(psi)!; }
            catch (Win32Exception)
            {
                SetStatus(i, 127);
                throw i.Error(i.ENOENT, "No such file or directory - " + psi.FileName);
            }
            using (proc)
            {
                MemoryStream outBuf = new(), errBuf = new();
                Task Pump(StreamReader r, Sink mode, MemoryStream own, TextWriter w) => Task.Run(() =>
                {
                    if (mode == Sink.Err)
                    {
                        var chars = new char[4096];
                        int n;
                        while ((n = r.Read(chars, 0, chars.Length)) > 0) lock (w) w.Write(chars, 0, n);
                        return;
                    }
                    var buf = new byte[4096];
                    int got;
                    while ((got = r.BaseStream.Read(buf, 0, buf.Length)) > 0)
                    {
                        if (mode == Sink.Discard) continue;
                        var target = mode == Sink.Merge ? outBuf : own;
                        lock (target) target.Write(buf, 0, got);
                    }
                });
                var pumps = new List<Task>();
                if (psi.RedirectStandardOutput) pumps.Add(Pump(proc.StandardOutput, outMode, outBuf, i.Out));
                if (psi.RedirectStandardError) pumps.Add(Pump(proc.StandardError, errMode, errBuf, i.Err));
                if (psi.RedirectStandardInput)
                {
                    try
                    {
                        if (input != null)
                        {
                            var bytes = inputBinary ? Encoding.Latin1.GetBytes(input) : new UTF8Encoding(false).GetBytes(input);
                            proc.StandardInput.BaseStream.Write(bytes, 0, bytes.Length);
                        }
                        proc.StandardInput.Close();
                    }
                    catch (IOException) { }
                }
                proc.WaitForExit();
                Task.WaitAll(pumps.ToArray());
                if (errMode == Sink.Err) i.Err.Flush();
                SetStatus(i, proc.ExitCode);
                var output = Decode(outBuf.ToArray(), out binary);
                return (output, Decode(errBuf.ToArray(), out _), proc.ExitCode);
            }
        }

        static string Decode(byte[] bytes, out bool binary)
        {
            binary = false;
            try { return new UTF8Encoding(false, true).GetString(bytes).Replace("\r\n", "\n"); }
            catch (DecoderFallbackException) { binary = true; return Encoding.Latin1.GetString(bytes); }
        }

        // Kernel#which/#with_env/#free_port/#spawn, Process, Open3: only what the interpreter doesn't already provide.
        static void DefineProcessHelpers(Interp it)
        {
            void DefMissing(RClass c, string names, Native fn)
            {
                foreach (var n in names.Split(' '))
                    if (!c.Methods.ContainsKey(n)) it.Def(c, n, fn);
            }
            var K = it.KernelModule;
            it.FileClass.Consts.TryAdd("NULL", Interp.S("NUL"));

            DefMissing(K, "which", (i, self, a, b) =>
            {
                var path = a.Length > 1 ? (a[1] is RArray arr ? string.Join(";", arr.L.Select(i.PathStr)) : i.ToS(a[1])) : i.Env.GetValueOrDefault("PATH", "");
                return Which(i, i.PathStr(a[0]), path) is { } found ? i.NewPath(found) : null;
            });
            DefMissing(K, "with_env", (i, self, a, b) =>
            {
                var old = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var h = a[0] as RHash ?? throw i.TypeErr($"no implicit conversion of {i.TypeName(a[0])} into Hash");
                foreach (var kv in h.D)
                {
                    var k = i.StrOrSym(kv.Key);
                    old.TryAdd(k, i.Env.GetValueOrDefault(k));
                    if (kv.Value == null) i.Env.Remove(k); else i.Env[k] = i.ToS(kv.Value);
                }
                try { return i.CallProc(b, None); }
                finally
                {
                    foreach (var kv in old)
                        if (kv.Value == null) i.Env.Remove(kv.Key); else i.Env[kv.Key] = kv.Value;
                }
            });
            // Homebrew::FreePort (loopback, so no firewall prompt)
            DefMissing(K, "free_port", (i, self, a, b) =>
            {
                var l = new TcpListener(IPAddress.Loopback, 0);
                l.Start();
                var port = ((IPEndPoint)l.LocalEndpoint).Port;
                l.Stop();
                return (long)port;
            });

            // $? is an Integer exit code here; give it Process::Status's common readers.
            DefMissing(it.IntegerClass, "exitstatus", (i, self, a, b) => self);
            DefMissing(it.IntegerClass, "success?", (i, self, a, b) => (long)self == 0);
            DefMissing(it.IntegerClass, "exited?", (i, self, a, b) => true);
            DefMissing(it.IntegerClass, "signaled?", (i, self, a, b) => false);

            var P = it.ConstGet(it.ObjectClass, "Process") as RClass ?? it.DefineModule("Process");
            var PS = it.SingletonOf(P);
            P.Consts.TryAdd("CLOCK_MONOTONIC", 1L);
            P.Consts.TryAdd("CLOCK_REALTIME", 0L);
            var children = new Dictionary<long, Process>();
            Native spawn = (i, self, a, b) =>
            {
                var args = a.ToList();
                RHash env = args.Count > 0 && args[0] is RHash e ? e : null;
                if (env != null) args.RemoveAt(0);
                RHash opts = args.Count > 0 && args[^1] is RHash o ? o : null;
                if (opts != null) args.RemoveAt(args.Count - 1);
                var psi = Psi(i, args.ToArray());
                psi.UseShellExecute = false;
                psi.WorkingDirectory = i.Cwd;
                i.ApplyEnv(psi);
                if (env != null)
                    foreach (var kv in env.D)
                        if (kv.Value == null) psi.Environment.Remove(i.StrOrSym(kv.Key)); else psi.Environment[i.StrOrSym(kv.Key)] = i.ToS(kv.Value);
                bool quiet = false;
                if (opts != null)
                    foreach (var kv in opts.D)
                    {
                        if (kv.Key is RSymbol { Name: "chdir" }) psi.WorkingDirectory = i.Abs(i.PathStr(kv.Value));
                        else if (kv.Value is RString { V: "NUL" or "/dev/null" } || (kv.Value is RArray arr && arr.L.FirstOrDefault() is RString { V: "NUL" or "/dev/null" }))
                            quiet = true;
                    }
                if (quiet) { psi.RedirectStandardOutput = true; psi.RedirectStandardError = true; }
                Process proc;
                try { proc = Process.Start(psi)!; }
                catch (Win32Exception) { throw i.Error(i.ENOENT, "No such file or directory - " + psi.FileName); }
                if (quiet)
                {
                    proc.OutputDataReceived += (_, _) => { };
                    proc.ErrorDataReceived += (_, _) => { };
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();
                }
                lock (children) children[proc.Id] = proc;
                return (long)proc.Id;
            };
            DefMissing(K, "spawn", spawn);
            DefMissing(PS, "spawn", spawn);
            DefMissing(PS, "pid", (i, self, a, b) => (long)Environment.ProcessId);
            DefMissing(PS, "kill", (i, self, a, b) =>
            {
                foreach (var pidObj in a.Skip(1))
                {
                    var pid = i.Int(pidObj);
                    Process proc;
                    lock (children) children.TryGetValue(pid, out proc);
                    try { proc ??= Process.GetProcessById((int)pid); }
                    catch (ArgumentException) { throw i.Error(it.ConstGet(it.ErrnoModule, "ESRCH") as RClass ?? i.SystemCallError, "No such process"); }
                    try { if (!proc.HasExited) proc.Kill(true); }
                    catch (InvalidOperationException) { }
                }
                return (long)(a.Length - 1);
            });
            DefMissing(PS, "wait waitpid wait2", (i, self, a, b) =>
            {
                Process proc = null;
                long pid;
                lock (children)
                {
                    pid = a.Length > 0 && a[0] is long l ? l : children.Keys.FirstOrDefault(-1L);
                    children.Remove(pid, out proc);
                }
                if (proc == null) throw i.Error(it.ConstGet(it.ErrnoModule, "ECHILD") as RClass ?? i.SystemCallError, "No child processes");
                proc.WaitForExit();
                SetStatus(i, proc.ExitCode);
                proc.Dispose();
                return pid;
            });
            DefMissing(PS, "detach", (i, self, a, b) => null);
            DefMissing(PS, "clock_gettime", (i, self, a, b) =>
                a.Length > 0 && a[0] is long c && c == 1L ? Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0);

            // IO.popen(cmd, mode = "r") { |io| ... }: read/gets/each_line/readlines, write/puts/print/<<, close_write, close, pid
            var IOS = it.SingletonOf(it.IOClass);
            if (!IOS.Methods.ContainsKey("popen"))
            {
                var pipeClass = new RClass { Name = "IO", Super = it.IOClass, Class = it.ClassClass };
                Process Pr(object self) => (Process)((RObject)self).Host;
                string Line(Process p) => p.StandardOutput.ReadLine() is { } l ? l + "\n" : null;
                void Close(Interp i, object self)
                {
                    var p = Pr(self);
                    var o = (RObject)self;
                    if (o.Frozen) return;
                    o.Frozen = true;
                    if (p.StartInfo.RedirectStandardInput) try { p.StandardInput.Close(); } catch (IOException) { }
                    if (p.StartInfo.RedirectStandardOutput) p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    SetStatus(i, p.ExitCode);
                    p.Dispose();
                }
                void Write(Process p, string s)
                {
                    p.StandardInput.Write(s);
                    p.StandardInput.Flush();
                }
                it.Def(pipeClass, "read", (i, self, a, b) => Interp.S(Pr(self).StandardOutput.ReadToEnd().Replace("\r\n", "\n")));
                it.Def(pipeClass, "gets readline", (i, self, a, b) => Line(Pr(self)) is { } l ? Interp.S(l) : null);
                it.Def(pipeClass, "readlines", (i, self, a, b) =>
                {
                    var res = new RArray();
                    while (Line(Pr(self)) is { } l) res.L.Add(Interp.S(l));
                    return res;
                });
                it.Def(pipeClass, "each_line each", (i, self, a, b) =>
                {
                    while (Line(Pr(self)) is { } l) i.CallProc(b, new object[] { Interp.S(l) });
                    return self;
                });
                it.Def(pipeClass, "write print <<", (i, self, a, b) => { foreach (var x in a) Write(Pr(self), i.ToS(x)); return self; });
                it.Def(pipeClass, "puts", (i, self, a, b) =>
                {
                    foreach (var x in a.Length == 0 ? new object[] { Interp.S("") } : a)
                    {
                        var s = i.ToS(x);
                        Write(Pr(self), s.EndsWith('\n') ? s : s + "\n");
                    }
                    return null;
                });
                it.Def(pipeClass, "close_write", (i, self, a, b) => { Pr(self).StandardInput.Close(); return null; });
                it.Def(pipeClass, "close", (i, self, a, b) => { Close(i, self); return null; });
                it.Def(pipeClass, "closed?", (i, self, a, b) => ((RObject)self).Frozen);
                it.Def(pipeClass, "pid", (i, self, a, b) => (long)Pr(self).Id);
                it.Def(pipeClass, "sync=", (i, self, a, b) => a[0]);
                it.Def(IOS, "popen", (i, self, a, b) =>
                {
                    var args = a.ToList();
                    RHash opts = args.Count > 1 && args[^1] is RHash oh ? oh : null;
                    if (opts != null) args.RemoveAt(args.Count - 1);
                    var mode = args.Count > 1 ? i.ToS(args[1]) : "r";
                    var cmd = args[0];
                    RHash env = null;
                    ProcessStartInfo psi;
                    if (cmd is RArray arr)
                    {
                        var parts = arr.L.ToList();
                        if (parts.Count > 0 && parts[0] is RHash eh) { env = eh; parts.RemoveAt(0); }
                        if (parts.Count > 0 && parts[^1] is RHash) parts.RemoveAt(parts.Count - 1);
                        psi = new ProcessStartInfo(Resolve(i, i.ToS(parts[0])));
                        foreach (var p in parts.Skip(1)) psi.ArgumentList.Add(i.ToS(p));
                    }
                    else
                    {
                        var line = i.ToS(cmd);
                        if (opts != null && opts.D.TryGetValue(Interp.Sym("err"), out var err) && err is RArray ea && ea.L.LastOrDefault() is RSymbol { Name: "out" })
                            line += " 2>&1";
                        psi = LinePsi(i, line);
                    }
                    psi.UseShellExecute = false;
                    psi.WorkingDirectory = i.Cwd;
                    i.ApplyEnv(psi);
                    if (psi.FileName.EndsWith("bash.exe", StringComparison.OrdinalIgnoreCase))
                        foreach (var kv in ShellEnv) psi.Environment[kv.Key] = kv.Value;
                    if (env != null)
                        foreach (var kv in env.D)
                            if (kv.Value == null) psi.Environment.Remove(i.StrOrSym(kv.Key)); else psi.Environment[i.StrOrSym(kv.Key)] = i.ToS(kv.Value);
                    psi.RedirectStandardOutput = mode.Contains('r') || mode.Contains('+');
                    psi.RedirectStandardInput = mode.Contains('w') || mode.Contains('+');
                    if (psi.RedirectStandardInput) psi.StandardInputEncoding = new UTF8Encoding(false);
                    Process proc;
                    try { proc = Process.Start(psi)!; }
                    catch (Win32Exception) { throw i.Error(i.ENOENT, "No such file or directory - " + psi.FileName); }
                    var io = new RObject { Class = pipeClass, Host = proc };
                    if (b == null) return io;
                    try { return i.CallProc(b, new object[] { io }); }
                    finally { Close(i, io); }
                });
            }

            // Open3.capture2 / capture2e / capture3 (stdin_data:, chdir:)
            var O = it.ConstGet(it.ObjectClass, "Open3") as RClass ?? it.DefineModule("Open3");
            object Capture(Interp i, object[] a, Sink err, int arity)
            {
                var args = a.ToList();
                RHash env = args.Count > 0 && args[0] is RHash e ? e : null;
                if (env != null) args.RemoveAt(0);
                RHash opts = args.Count > 0 && args[^1] is RHash o ? o : null;
                if (opts != null) args.RemoveAt(args.Count - 1);
                string input = null;
                string chdir = null;
                if (opts != null)
                {
                    if (opts.D.TryGetValue(Interp.Sym("stdin_data"), out var sd) && sd != null) input = i.ToS(sd);
                    if (opts.D.TryGetValue(Interp.Sym("chdir"), out var cd) && cd != null) chdir = i.Abs(i.PathStr(cd));
                }
                var psi = Psi(i, args.ToArray());
                var oldCwd = i.Cwd;
                var savedEnv = env == null ? null : new Dictionary<string, string>(i.Env, StringComparer.OrdinalIgnoreCase);
                try
                {
                    if (chdir != null) i.Cwd = chdir;
                    if (env != null)
                        foreach (var kv in env.D)
                            if (kv.Value == null) i.Env.Remove(i.StrOrSym(kv.Key)); else i.Env[i.StrOrSym(kv.Key)] = i.ToS(kv.Value);
                    var (o2, e2, code) = Exec(i, psi, err == Sink.Merge ? Sink.Merge : Sink.Capture, err, input, true);
                    return arity == 3
                        ? new RArray(new object[] { Interp.S(o2), Interp.S(e2), (long)code })
                        : new RArray(new object[] { Interp.S(o2), (long)code });
                }
                finally
                {
                    i.Cwd = oldCwd;
                    if (savedEnv != null) { i.Env.Clear(); foreach (var kv in savedEnv) i.Env[kv.Key] = kv.Value; }
                }
            }
            var OS = it.SingletonOf(O);
            DefMissing(OS, "capture3", (i, self, a, b) => Capture(i, a, Sink.Capture, 3));
            DefMissing(OS, "capture2", (i, self, a, b) => Capture(i, a, Sink.Err, 2));
            DefMissing(OS, "capture2e", (i, self, a, b) => Capture(i, a, Sink.Merge, 2));
        }
    }
}
