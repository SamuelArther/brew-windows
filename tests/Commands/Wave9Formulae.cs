using Brew;
using Brew.Formulae;
using Brew.Ruby;

// Formula API and build-environment gaps the wave9-foundation-retry, wave9-heavy and wave5-portable batches hit.
static class Wave9Formulae
{
    public static int Run(string root)
    {
        var count = 0;
        var dir = Path.Combine(root, "wave9");
        var formulaDir = Path.Combine(dir, "tap", "Formula");
        Directory.CreateDirectory(formulaDir);
        void Check(bool ok, string label)
        {
            if (!ok) throw new Exception("FAIL " + label);
            count++;
        }
        string Write(string name, string body)
        {
            var path = Path.Combine(formulaDir, name[..1], name + ".rb");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, body);
            return path;
        }
        string Simple(string name, string extra = "") =>
            $"class {FormulaLoader.ClassName(name)} < Formula\n  url \"https://example.com/{name}-1.0.tar.gz\"\n  sha256 \"{new string('0', 64)}\"\n{extra}end\n";
        var probe = Write("w9-probe", Simple("w9-probe"));
        string Eval(string code)
        {
            var it = FormulaLoader.NewInterpreterForTests();
            it.Cwd = dir;
            try { return it.ToS(it.Run(code, probe)); }
            catch (RubyError e) { return $"{e.ClassName}: {e.Message}"; }
        }

        // std_meson_args(prefix:, libdir:): glib's staging build installs into buildpath/"staging", not the keg
        // (its gobject-introspection step then found no glib-2.0.pc).
        var got = Eval("""Formula.allocate.std_meson_args(prefix: "C:/b/staging", libdir: "lib64").inspect""");
        Check(got == """["--prefix=C:/b/staging", "--libdir=lib64", "--buildtype=release", "--wrap-mode=nofallback"]""", "std_meson_args honors prefix: and libdir: " + got);

        // Another formula's pkgetc/keg_only?/deps (certifi: Formula["ca-certificates"].pkgetc; crystal:
        // deps.filter_map(&:to_formula).reject(&:keg_only?)).
        Write("w9-certs", Simple("w9-certs", "  keg_only :versioned_formula\n"));
        Write("w9-lib", Simple("w9-lib"));
        Write("w9-user", Simple("w9-user", "  depends_on \"w9-certs\"\n  depends_on \"w9-lib\"\n  depends_on \"w9-lib-build\" => :build\n"));
        got = Eval("""
            c = Formula["w9-certs"]
            u = Formula["w9-user"]
            [c.pkgetc.to_s.tr("\\", "/").end_with?("/etc/w9-certs"), c.keg_only?, Formula["w9-lib"].keg_only?,
             u.deps.reject(&:build?).map(&:to_formula).reject(&:keg_only?).map(&:name), c.opt_sbin.basename.to_s].inspect
            """);
        Check(got == """[true, true, false, ["w9-lib"], "sbin"]""", "FormulaReference pkgetc/keg_only?/deps: " + got);

        // Formula.names (llvm: the python@ versions it builds bindings for).
        got = Eval("""Formula.names.select { |n| n.start_with?("w9-") }.inspect""");
        Check(got == """["w9-certs", "w9-lib", "w9-probe", "w9-user"]""", "Formula.names lists the tap: " + got);

        // preserve_rpath (swift) loads; deuniversalize_machos (fits) does nothing.
        var swift = FormulaLoader.LoadSpec(Write("w9-rpath", Simple("w9-rpath", "  preserve_rpath\n")));
        Check(swift.Name == "w9-rpath", "preserve_rpath is accepted");
        got = Eval("""Formula.allocate.deuniversalize_machos.inspect""");
        Check(got == "nil", "deuniversalize_machos is a no-op: " + got);

        // A patch block whose url is only on_linux/on_macos isn't a patch here (net-snmp, spidermonkey: "Formula
        // must supply a SHA-256 checksum"); one with a url still is.
        var patched = FormulaLoader.LoadSpec(Write("w9-patch", Simple("w9-patch",
            "  patch do\n    on_linux do\n      url \"https://example.com/linux.patch\"\n      sha256 \"" + new string('1', 64) + "\"\n    end\n  end\n" +
            "  patch do\n    url \"https://example.com/all.patch\"\n    sha256 \"" + new string('2', 64) + "\"\n  end\n")));
        Check(patched.Patches.Count == 1 && patched.Patches[0].Url == "https://example.com/all.patch", "url-less patch blocks are skipped");

        // Kernel#open (openssl@4's test: open("checksum.txt") { |f| f.read(100) }).
        got = Eval("""File.write("checksum.txt", "SHA2-256(t)= abc\n"); open("checksum.txt") { |f| f.read(100).split("=").last.strip }""");
        Check(got == "abc", "Kernel#open opens a file: " + got);

        // (bin/"wxrc").unlink removes wxrc.exe (wxwidgets@3.2).
        got = Eval("""Pathname("ux/bin").mkpath; File.write("ux/bin/wxrc.exe", "x"); File.write("ux/bin/other.exe", "x"); (Pathname("ux/bin")/"wxrc").unlink; File.delete("ux/bin/other"); Dir.children("ux/bin").inspect""");
        Check(got == "[]", "unlink/File.delete of tool remove tool.exe: " + got);

        // `system python, "-m", "pip", "install", ...` gets the Python build environment (numpy built Cython with
        // the 32-bit-hosted cl.exe).
        Check(FormulaLoader.IsPipInstall(["C:/h/opt/python@3.14/libexec/bin/python", "-m", "pip", "install", "--prefix=x", "."]), "python -m pip install is a pip build");
        Check(!FormulaLoader.IsPipInstall(["cargo", "install", "--path", "."]) && !FormulaLoader.IsPipInstall(["python3", "-m", "venv", "x", "y"]), "other commands are not");

        // A new file in a diff with an epoch "---" date (libiconv's utf8mac patch: --- a/lib/utf8mac.h 1970-01-01).
        var src = Path.Combine(dir, "iconv-src");
        Directory.CreateDirectory(Path.Combine(src, "lib"));
        File.WriteAllText(Path.Combine(src, "lib", "converters.h"), "one\ntwo\n");
        Patcher.Apply("--- a/lib/converters.h\t2009-06-21 06:17:33.000000000 -0500\n+++ b/lib/converters.h\t2010-01-20 19:54:37.000000000 -0600\n@@ -1,2 +1,3 @@\n one\n+mac\n two\n" +
            "--- a/lib/utf8mac.h\t1970-01-01 09:00:00.000000000 +0900\n+++ b/lib/utf8mac.h\t2007-11-13 17:42:39.000000000 +0900\n@@ -0,0 +1,2 @@\n+/* utf8mac */\n+#define X 1\n", 1, src);
        Check(File.ReadAllText(Path.Combine(src, "lib", "utf8mac.h")) == "/* utf8mac */\n#define X 1\n", "patch creates a file the diff adds");
        Check(File.ReadAllText(Path.Combine(src, "lib", "converters.h")) == "one\nmac\ntwo\n", "the same diff still patches existing files");

        // A root_url in the legacy repository also tries the bottles repository (eigen, docbook, console_bridge 404'd).
        var legacy = FormulaLoader.Load(Write("w9-bottled", Simple("w9-bottled",
            $"  bottle do\n    root_url \"{Bottles.LegacyDomain}/w9-bottled-1.0\"\n    sha256 cellar: :any, x86_64_windows: \"{new string('3', 64)}\"\n  end\n")));
        var bottle = Bottles.For(legacy);
        Check(bottle.Url == $"{Bottles.LegacyDomain}/w9-bottled-1.0/w9-bottled--1.0.x86_64_windows.bottle.tar.gz", "legacy root_url stays first: " + bottle.Url);
        Check(bottle.Mirrors.SequenceEqual([$"{Bottles.DefaultDomain}/w9-bottled-1.0/w9-bottled--1.0.x86_64_windows.bottle.tar.gz"]), "legacy root_url falls back to the bottles repository: " + string.Join(",", bottle.Mirrors));

        // Rust builds give cc-rs (cl.exe) only flags MSVC takes: it appends CFLAGS to the target-specific ones.
        Check(BuildEnvironment.MsvcSafeFlags("-O2 -pipe -std=gnu17 -Wno-error=int-conversion -Wno-template-body") == "-O2", "GCC-only flags are dropped for cl");

        // Receipts' runtime dependencies are followed for test-time DLL lookup (gd's test loads libpng through libgd).
        var paths = new Paths();
        void Receipt(string name, params string[] deps)
        {
            var opt = Path.Combine(paths.Prefix, "opt", name);
            Directory.CreateDirectory(opt);
            File.WriteAllText(Path.Combine(opt, "INSTALL_RECEIPT.json"), System.Text.Json.JsonSerializer.Serialize(new { Name = name, Dependencies = deps }));
        }
        Receipt("w9-a", "w9-b");
        Receipt("w9-b", "w9-c", "w9-a");
        Receipt("w9-c");
        Check(BuildEnvironment.WithRuntimeDependencies(paths, ["w9-a"]).SequenceEqual(["w9-a", "w9-b", "w9-c"]), "runtime dependencies are followed through receipts");

        // MSYS2-packaged dependencies: their libexec/ucrt64/bin DLLs are on the build PATH after the toolchain, and a
        // compiler keg's C runtime headers stay out of CPATH/CPPFLAGS (ntfs2btrfs: <cstdlib> lost <stdlib.h>).
        var saved = Environment.GetEnvironmentVariable("HOMEBREW_MSYS2_ROOT");
        var msys = Path.Combine(dir, "msys64");
        Directory.CreateDirectory(Path.Combine(msys, "usr", "bin"));
        File.WriteAllText(Path.Combine(msys, "usr", "bin", "bash.exe"), "");
        var gccKeg = Path.Combine(paths.Prefix, "opt", "w9-gcc", "libexec", "ucrt64");
        Directory.CreateDirectory(Path.Combine(gccKeg, "include"));
        Directory.CreateDirectory(Path.Combine(gccKeg, "bin"));
        File.WriteAllText(Path.Combine(gccKeg, "include", "stdlib.h"), "");
        var pngKeg = Path.Combine(paths.Prefix, "opt", "w9-png", "libexec", "ucrt64");
        Directory.CreateDirectory(Path.Combine(pngKeg, "include"));
        Directory.CreateDirectory(Path.Combine(pngKeg, "bin"));
        Directory.CreateDirectory(Path.Combine(paths.Prefix, "opt", "w9-gcc", "bin"));
        Receipt("w9-png", "w9-conversion");
        Receipt("w9-conversion");
        try
        {
            Environment.SetEnvironmentVariable("HOMEBREW_MSYS2_ROOT", msys);
            Directory.CreateDirectory(Path.Combine(dir, "build"));
            var context = new InstallContext(Path.Combine(dir, "keg"), Path.Combine(dir, "build"), null, paths.Prefix);
            BuildEnvironment.Apply(paths, context, ["w9-gcc", "w9-png"]);
            var env = context.BuildEnvironment;
            var pathList = env["PATH"].Split(';').ToList();
            var toolchainAt = pathList.FindIndex(p => p.Equals(Path.Combine(msys, "usr", "bin"), StringComparison.OrdinalIgnoreCase));
            var pngAt = pathList.FindIndex(p => p.Equals(Path.Combine(pngKeg, "bin"), StringComparison.OrdinalIgnoreCase));
            Check(toolchainAt >= 0 && pngAt > toolchainAt, "MSYS2 kegs' DLL folders follow the toolchain on PATH");
            Check(!pathList.Contains(Path.Combine(paths.Prefix, "opt", "w9-gcc", "bin"), StringComparer.OrdinalIgnoreCase), "a brewed GCC doesn't go ahead of the toolchain's");
            Check(Norm(env["CMAKE_PREFIX_PATH"]).Contains(Norm(Path.Combine(paths.Prefix, "opt", "w9-conversion"))), "dependencies' runtime dependencies are in CMAKE_PREFIX_PATH (fizz: folly needs double-conversion)");
            string Norm(string s) => s.Replace('\\', '/');
            Check(Norm(env["CPATH"]).Contains(Norm(Path.Combine(pngKeg, "include"))) && !Norm(env["CPATH"]).Contains(Norm(Path.Combine(gccKeg, "include"))), "compiler keg's CRT headers stay out of CPATH: " + env["CPATH"]);
            Check(!Norm(env["CPPFLAGS"]).Contains(Norm(Path.Combine(gccKeg, "include"))), "and out of CPPFLAGS");
            Check(!env.TryGetValue("LIBRARY_PATH", out var libraryPath) || libraryPath.Length > 0 && !libraryPath.Split(';').Contains(""), "LIBRARY_PATH is never empty or holds empty elements");
        }
        finally
        {
            Environment.SetEnvironmentVariable("HOMEBREW_MSYS2_ROOT", saved);
            foreach (var name in new[] { "w9-a", "w9-b", "w9-c", "w9-gcc", "w9-png", "w9-conversion" }) Directory.Delete(Path.Combine(paths.Prefix, "opt", name), true);
        }
        return count;
    }
}
