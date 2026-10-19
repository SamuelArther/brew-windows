using System.Security.Cryptography;
using System.IO.Compression;
using Brew;
using Brew.Formulae;
using Brew.Ruby;

// Generic causes behind the wave2-retry, wave3-node, wave6-java, wave8-deprecated and sync-20261018 failures.
static class BatchRetry3
{
    public static async Task<int> Run(string root)
    {
        var count = 0;
        var dir = Path.Combine(root, "retry3");
        Directory.CreateDirectory(dir);
        void Expect(string code, string expected)
        {
            var it = FormulaLoader.NewInterpreterForTests();
            it.Cwd = dir;
            string got;
            try { got = it.ToS(it.Run(code, Path.Combine(dir, "probe.rb"))); }
            catch (RubyError e) { got = $"{e.ClassName}: {e.Message}"; }
            if (got != expected) throw new Exception($"{code}\n  expected {expected}\n  got      {got}");
            count++;
        }
        void Check(bool ok, string label)
        {
            if (!ok) throw new Exception("FAIL " + label);
            count++;
        }

        // Library pieces: Hardware::CPU.bits (clozure-cl), Gem::Version (npm-check-updates), ERB::Util.url_encode
        // (snakeviz), File::Stat#ino (druid), Pathname#fnmatch? (apache-pulsar), ENV.fortran (fypp).
        Expect("""Formula.allocate.deuniversalize_machos("x").inspect""", "nil");
        Expect("""[Hardware::CPU.bits, Gem::Version.new("4.17.1") > Gem::Version.new("3.6.1"), Gem::Version.new("1.10") > "1.9", ERB::Util.url_encode("a b/c")].inspect""",
            """[64, true, true, "a%20b%2Fc"]""");
        Expect("""File.write("st.txt", "abc"); File.write("st2.txt", "abc"); [File.stat("st.txt").size, Pathname("st.txt").stat.ino == File.stat("st.txt").ino, Pathname("st.txt").stat.ino == Pathname("st2.txt").stat.ino].inspect""",
            "[3, true, false]");
        Expect("""[Pathname("C:/x/bin/pulsar-common.sh").fnmatch?("*common.sh"), Pathname("C:/x/bin/pulsar").fnmatch?("*common.sh"), File.fnmatch("*", "a/b"), File.fnmatch("*", "a/b", File::FNM_PATHNAME)].inspect""",
            "[true, false, true, false]");
        Expect("""ENV.fortran; [ENV["FC"], ENV["F77"]].inspect""", """["gfortran", "gfortran"]""");
        Expect("""[rand(1..3).between?(1, 3), rand(5).between?(0, 4), rand.class, rand(0).class].inspect""", "[true, true, Float, Float]");

        // write_jar_script's java_opts expand like bash's: "$JAVA_OPTS" was passed literally and java ran it as the main class.
        Expect("""Pathname("jbin").write_jar_script "C:/x/tool.jar", "tool", "-Xmx1g ${TOOL_OPTS} $JAVA_OPTS"; File.read("jbin/tool.cmd").include?("java.exe\" -Xmx1g %TOOL_OPTS% %JAVA_OPTS% -jar ").to_s""", "true");

        // Env scripts: a default inside a default (leiningen's JAVA_CMD), a .bat sibling looked for when the wrapper runs
        // (prog8 deletes it after writing the wrapper), and a non-shell #! target run through exec (fastqc).
        Expect("""Pathname("ev/libexec").mkpath; File.write("ev/libexec/tool", "#!/usr/bin/env python3\n"); Pathname("ev/bin/tool").write_env_script Pathname("ev/libexec/tool").expand_path, JAVA_HOME: "${JAVA_HOME:-C:/jdk}", JAVA_CMD: "${JAVA_CMD:-${JAVA_HOME:-C:/jdk}/bin/java}"; t = File.read("ev/bin/tool.cmd"); [t.include?('if not defined JAVA_HOME set "JAVA_HOME=C:/jdk"'), t.include?('if defined JAVA_HOME set "BREW_DEFAULT_1=%JAVA_HOME%"'), t.include?('if not defined JAVA_CMD set "JAVA_CMD=%BREW_DEFAULT_1%/bin/java"'), t.include?('bash -c "exec \\"$0\\" \\"$@\\""'), t.include?("goto brew_run_bat")].inspect""",
            "[true, true, true, true, true]");

        // install_symlink of a command that has a .cmd launcher links the launcher too (bpmnlint, basedpyright).
        Expect("""Pathname("lc/libexec/bin").mkpath; File.write("lc/libexec/bin/tool", "#!/bin/sh\n"); File.write("lc/libexec/bin/tool.cmd", "@echo x\r\n"); Pathname("lc/bin").install_symlink Pathname("lc/libexec/bin/tool").expand_path => "renamed"; Dir.children("lc/bin").sort.inspect""",
            """["renamed", "renamed.cmd"]""");
        Expect("""Pathname("lo/libexec/bin").mkpath; File.write("lo/libexec/bin/only.cmd", "@echo x\r\n"); Pathname("lo/bin").install_symlink Pathname("lo/libexec/bin/only").expand_path; Dir.children("lo/bin").inspect""",
            """["only.cmd"]""");

        // Stripping node prebuilds "for other platforms" keeps Windows' (tweakcc: `rm_r d if d.basename.to_s != "darwin-x64"`).
        Expect("""%w[win32-x64 darwin-x64].each { |d| Pathname("nm/node_modules/lief/prebuilds/#{d}").mkpath }; Pathname("nm/node_modules/lief/prebuilds").children.each { |d| FileUtils.rm_r d if d.basename.to_s != "darwin-arm64" }; Dir.children("nm/node_modules/lief/prebuilds").inspect""",
            """["win32-x64"]""");

        // A global npm install lands in lib/node_modules, as on Unix (commitlint: inreplace libexec/"lib/node_modules/...").
        var npmPrefix = Path.Combine(dir, "npm", "libexec");
        var package = Path.Combine(npmPrefix, "node_modules", "@scope", "tool");
        Directory.CreateDirectory(package);
        File.WriteAllText(Path.Combine(package, "package.json"), """{"name": "@scope/tool", "bin": {"tool": "cli.js"}}""");
        File.WriteAllText(Path.Combine(package, "cli.js"), "");
        File.WriteAllText(Path.Combine(npmPrefix, "tool.cmd"), "@\"%~dp0\\node_modules\\@scope\\tool\\cli.js\" %*\r\n");
        FormulaLoader.WriteNpmBinLinks(FormulaLoader.NewInterpreterForTests(), ["npm", "install", "--global", "--prefix=" + npmPrefix, "x.tgz"]);
        var moved = Path.Combine(npmPrefix, "lib", "node_modules", "@scope", "tool");
        Check(File.Exists(Path.Combine(moved, "package.json")) && !Directory.Exists(Path.Combine(npmPrefix, "node_modules")), "npm packages move to lib/node_modules");
        Check(File.ReadAllText(Path.Combine(npmPrefix, "bin", "tool.cmd")).Contains(Path.Combine("lib", "node_modules")), "bin launcher runs the moved package");
        Check(File.ReadAllText(Path.Combine(npmPrefix, "bin", "tool")).StartsWith("#!/bin/sh\nexec "), "bin/<name> shell launcher, as npm links on Unix");
        Check(!File.Exists(Path.Combine(npmPrefix, "tool.cmd")), "npm's stale prefix-root launcher is removed");

        // A pending windows_exe command keeps its arguments (a pip launcher's script) when run during install.
        var pending = FormulaLoader.NewInterpreterForTests();
        pending.Cwd = dir;
        pending.PendingCommand = full => full.EndsWith("robocop", StringComparison.OrdinalIgnoreCase) ? ("python.exe", new[] { "C:/v/Scripts/robocop" }) : ((string, string[])?)null;
        Check(pending.PendingArguments("bin/robocop").SequenceEqual(["C:/v/Scripts/robocop"]) && pending.PendingArguments("python").Count == 0,
            "pending command arguments come first");

        // `patch do on_macos { file ... } end` is no patch on Windows (lua@5.4 failed "must supply a SHA-256 checksum").
        var formulaDir = Path.Combine(dir, "Formula");
        Directory.CreateDirectory(formulaDir);
        var osPatch = Path.Combine(formulaDir, "ospatch.rb");
        File.WriteAllText(osPatch, "class Ospatch < Formula\n  url \"https://example.invalid/ospatch-1.0.tar.gz\"\n  sha256 \"" + new string('0', 64) + "\"\n" +
            "  patch do\n    on_macos do\n      file \"Patches/x/mac.patch\"\n    end\n  end\n  patch :DATA\n  def install\n  end\nend\n__END__\n--- a/x\n+++ b/x\n");
        Check(FormulaLoader.Load(osPatch).Patches.Count == 1, "an OS-only patch block adds no patch on Windows");
        // `resource "...", :test` is a test resource: not staged by the install, not pip-installed (regipy).
        var testResource = Path.Combine(formulaDir, "testres.rb");
        File.WriteAllText(testResource, "class Testres < Formula\n  url \"https://example.invalid/testres-1.0.tar.gz\"\n  sha256 \"" + new string('0', 64) + "\"\n" +
            "  resource \"homebrew-hive\", :test do\n    url \"https://example.invalid/hive.xz\"\n    sha256 \"" + new string('1', 64) + "\"\n  end\n" +
            "  resource \"six\" do\n    url \"https://example.invalid/six-1.0.tar.gz\"\n    sha256 \"" + new string('2', 64) + "\"\n  end\n  def install\n  end\nend\n");
        var loaded = FormulaLoader.Load(testResource);
        Check(loaded.Resources.Single(r => r.Name == "homebrew-hive").Test && !loaded.Resources.Single(r => r.Name == "six").Test, "a :test resource is marked as one");

        // GNU patch takes the old name when the new one points outside the tree (dvdrtools' MacPorts patches).
        var patchTree = Path.Combine(dir, "patch-tree");
        Directory.CreateDirectory(Path.Combine(patchTree, "cdda2wav"));
        File.WriteAllText(Path.Combine(patchTree, "cdda2wav", "cdda2wav.c"), "a\nb\nc\n");
        Patcher.Apply("--- cdda2wav/cdda2wav.c\t2005-02-06\n+++ ../tool-patched/cdda2wav/cdda2wav.c\t2005-09-15\n@@ -1,3 +1,3 @@\n a\n-b\n+B\n c\n", 0, patchTree);
        Check(File.ReadAllText(Path.Combine(patchTree, "cdda2wav", "cdda2wav.c")) == "a\nB\nc\n", "patch falls back to the old name over an escaping new one");
        string escaped = null;
        try { Patcher.Apply("--- /dev/null\n+++ ../outside.c\n@@ -0,0 +1 @@\n+x\n", 0, patchTree); }
        catch (InvalidOperationException e) { escaped = e.Message; }
        Check(escaped != null && escaped.Contains("escapes") && !File.Exists(Path.Combine(dir, "outside.c")), "a new file outside the tree is still refused");

        // Rust builds: cc-rs 1.2 adds CFLAGS to CFLAGS_<target>, so GCC's -W options can't stay there (D8021 from cl).
        var savedMsys = Environment.GetEnvironmentVariable("HOMEBREW_MSYS2_ROOT");
        var msys = Path.Combine(dir, "msys64");
        Directory.CreateDirectory(Path.Combine(msys, "usr", "bin"));
        File.WriteAllText(Path.Combine(msys, "usr", "bin", "bash.exe"), "");
        try
        {
            Environment.SetEnvironmentVariable("HOMEBREW_MSYS2_ROOT", msys);
            Directory.CreateDirectory(Path.Combine(dir, "build"));
            var context = new InstallContext(Path.Combine(dir, "keg"), Path.Combine(dir, "build"), null, Path.Combine(root, "prefix"));
            BuildEnvironment.Apply(new Paths(), context, ["rust"]);
            if (context.BuildEnvironment.ContainsKey("CC_x86_64_pc_windows_msvc"))
            {
                Check(!context.BuildEnvironment["CFLAGS"].Contains("-W") && !context.BuildEnvironment["CXXFLAGS"].Contains("-W"), "Rust build CFLAGS carry no GCC warning options");
                // blake3's build script recognizes MSVC only by the names "cl"/"cl.exe" (cargo-c: LNK1181 on *_windows_gnu.o).
                Check(context.BuildEnvironment["CC_x86_64_pc_windows_msvc"] == "cl.exe" && context.BuildEnvironment["PATH"].Split(';').Any(d => File.Exists(Path.Combine(d.Trim('"'), "cl.exe"))),
                    "cc-rs gets cl by name, with Visual Studio's folder on PATH");
            }
            var c = new InstallContext(Path.Combine(dir, "keg2"), Path.Combine(dir, "build"), null, Path.Combine(root, "prefix"));
            BuildEnvironment.Apply(new Paths(), c, []);
            Check(c.BuildEnvironment["CFLAGS"].Contains("-Wno-error=incompatible-pointer-types"), "C builds keep GCC's -Wno-error options");
        }
        finally { Environment.SetEnvironmentVariable("HOMEBREW_MSYS2_ROOT", savedMsys); }

        // The machine's JAVA_HOME doesn't reach formula code (bin/brew filters it), so Java wrappers use the brewed JDK.
        var repo = Path.Combine(root, "repository");
        var cache = Environment.GetEnvironmentVariable("BREW_CACHE")!;
        var zip = Path.Combine(dir, "javahome-1.0.zip");
        using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
        using (var writer = new StreamWriter(archive.CreateEntry("package/readme.txt").Open())) writer.Write("x");
        var sha = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(zip)));
        File.Copy(zip, Path.Combine(cache, sha + "--javahome-1.0.zip"), overwrite: true);
        var formula = Path.Combine(repo, "tap", "homebrew-core", "Formula", "j", "javahome.rb");
        Directory.CreateDirectory(Path.GetDirectoryName(formula)!);
        File.WriteAllText(formula, "class Javahome < Formula\n  desc \"fixture\"\n  homepage \"https://example.invalid\"\n  url \"https://example.invalid/javahome-1.0.zip\"\n" +
            $"  sha256 \"{sha}\"\n  license \"MIT\"\n  def install\n    (prefix/\"java_home.txt\").write ENV.fetch(\"JAVA_HOME\", \"unset\")\n  end\nend\n");
        var savedJava = Environment.GetEnvironmentVariable("JAVA_HOME");
        try
        {
            Environment.SetEnvironmentVariable("JAVA_HOME", @"C:\hostedtoolcache\windows\Java_Temurin-Hotspot_jdk\17\x64");
            var output = new StringWriter();
            var oldOut = Console.Out;
            var oldErr = Console.Error;
            int code;
            try { Console.SetOut(output); Console.SetError(output); code = await Cli.RunAsync(["install", "javahome"]); }
            finally { Console.SetOut(oldOut); Console.SetError(oldErr); }
            Check(code == 0, "JAVA_HOME fixture installs: " + output);
            var written = Path.Combine(new Paths().Cellar, "javahome", "1.0", "java_home.txt");
            Check(File.ReadAllText(written) == "unset", "install doesn't see the machine's JAVA_HOME: " + File.ReadAllText(written));
            Check(await Cli.RunAsync(["uninstall", "javahome"]) == 0, "JAVA_HOME fixture uninstalls");
        }
        finally
        {
            Environment.SetEnvironmentVariable("JAVA_HOME", savedJava);
            File.Delete(formula);
        }
        return count;
    }
}
