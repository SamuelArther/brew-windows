using System.IO.Compression;
using Brew;
using Brew.Ruby;

// Formula API gaps the wave-4 C builds hit: Homebrew DSL helpers, Ruby library bits, download names,
// single-file gzip staging and config.guess/config.sub refreshing.
static class FormulaDsl
{
    public static int Run(string root)
    {
        var count = 0;
        var dir = Path.Combine(root, "formula-dsl");
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

        // StringInreplaceExtension's Makefile helpers (luajit-openresty, ninvaders, stress-ng, libsvm).
        Expect("""s = +"CC = cc\nCFLAGS=-O2 \\\n  -g\nX := 1\n"; s.change_make_var! "CC", "gcc"; v = s.get_make_var("X"); s.remove_make_var! "CFLAGS"; [s, v].inspect""",
            """["CC=gcc\nX := 1\n", "1"]""");
        // sub!/gsub! answer nil only when nothing matched (woof: rewrite_shebang of an already-right shebang).
        Expect("""s = +"#!/usr/bin/env python3\n"; [s.gsub!(/python3/, "python3").nil?, s.sub!(/nope/, "x").nil?].inspect""", "[false, true]");
        // Random (lzfse), Etc (slashem), __dir__ (symlinks' test), Language::Perl::Shebang (xmltoman).
        Expect("""r = Random.new(42); [r.rand(10).class, r.rand(1..3).between?(1, 3), r.rand.class, Random.new.bytes(5).bytesize, Random.new(7).seed].inspect""",
            "[Integer, true, Float, 5, 7]");
        Expect("""[Etc.getpwuid.name.empty?, Etc.getpwuid.gid.class, Etc.nprocessors.positive?].inspect""", "[false, Integer, true]");
        Expect("""[__dir__.end_with?("formula-dsl"), File.basename(__FILE__)].inspect""", """[true, "probe.rb"]""");
        Expect("""Language::Perl::Shebang::PERL_SHEBANG_REGEX.match?("#!/usr/bin/env perl -w\n").to_s""", "true");
        // ENV.prepend_create_path (perltidy, resty, st), ENV.compiler (juman), ENV.clang (clang-build-analyzer's test).
        Expect("""ENV.prepend_create_path "BREW_DSL_PROBE", "made/here"; [Dir.exist?("made/here"), ENV["BREW_DSL_PROBE"].end_with?("made/here"), ENV.compiler].inspect""",
            "[true, true, :gcc]");
        Expect("""ENV.clang; [ENV["CC"], ENV["CXX"], ENV.compiler].inspect""", """["clang", "clang++", :clang]""");
        // File#seek and an "r+b" write-back (lziprecover's test corrupts one byte in place).
        Expect("""File.binwrite("seek.bin", "abcdef"); c = File.open("seek.bin", "r+b") { |f| f.seek(2); x = f.read(1); f.seek(2); f.write("Z"); x }; [c, File.binread("seek.bin")].inspect""",
            """["c", "abZdef"]""");
        // Pathname#mkpath returns self (sbtenv: prefix.install_symlink (var/"lib/sbtenv/plugins").mkpath).
        Expect("""Pathname("mk/path").mkpath.to_s""", "mk/path");
        // Pathname#install_metafiles (zsh-lovers).
        Expect("""Dir.mkdir("meta") unless Dir.exist?("meta"); %w[README.md COPYING NEWS main.c].each { |f| File.write("meta/#{f}", "x") }; Pathname("metadoc").install_metafiles("meta"); Dir.children("metadoc").sort.inspect""",
            """["COPYING", "NEWS", "README.md"]""");
        // Formula#shared_library in MinGW's spelling, #std_swift_args, #zsh_function / #fish_function.
        Expect("""f = Formula.allocate; [f.shared_library("libfoo"), f.shared_library("libfoo", 2), f.shared_library("libfoo", "*"), f.shared_library("*")].inspect""",
            """["libfoo.dll", "libfoo-2.dll", "libfoo{,-*}.dll", "*.dll"]""");
        Expect("""Formula.allocate.std_swift_args.first(2).inspect""", """["--configuration", "release"]""");

        // A query can name the download (Maven's remotecontent?filepath=..., S3's response-content-disposition).
        Check(Downloader.FileName("https://search.maven.org/remotecontent?filepath=org/springframework/springloaded/1.2.6.RELEASE/springloaded-1.2.6.RELEASE.jar")
            == "springloaded-1.2.6.RELEASE.jar", "query file name");
        Check(Downloader.FileName("https://x.s3.amazonaws.com/blob?response-content-disposition=attachment%3B%20filename%3Dtool-1.0.tar.gz&X-Amz=1")
            == "tool-1.0.tar.gz", "content-disposition file name");
        Check(Downloader.FileName("https://github.com/o/r/commit/abc.patch?full_index=1") == "abc.patch", "query without a file keeps the path name");

        // A gzipped single file stages unpacked under its name without .gz (zimfw.zsh.gz); a gzipped tar still extracts.
        var gz = Path.Combine(dir, "download.gz");
        using (var output = File.Create(gz))
        using (var stream = new GZipStream(output, CompressionLevel.Fastest))
            stream.Write("source zimfw\n"u8);
        var staged = Archive.StageAsync(gz, "zimfw.zsh.gz", Path.Combine(dir, "stage-gz")).GetAwaiter().GetResult();
        Check(File.ReadAllText(Path.Combine(staged, "zimfw.zsh")) == "source zimfw\n", "single gzip file unpacks");
        var kept = Archive.StageAsync(gz, "boot.amd64-unix.tgz", Path.Combine(dir, "stage-nounzip"), noUnzip: true).GetAwaiter().GetResult();
        Check(File.Exists(Path.Combine(kept, "boot.amd64-unix.tgz")), "nounzip resource is staged as downloaded");

        // config.guess/config.sub older than the toolchain's automake copies are replaced before a build.
        var saved = Environment.GetEnvironmentVariable("HOMEBREW_MSYS2_ROOT");
        var msys = Path.Combine(dir, "msys64");
        Directory.CreateDirectory(Path.Combine(msys, "usr", "bin"));
        File.WriteAllText(Path.Combine(msys, "usr", "bin", "bash.exe"), "");
        var automake = Path.Combine(msys, "usr", "share", "automake-1.18");
        Directory.CreateDirectory(automake);
        File.WriteAllText(Path.Combine(automake, "config.guess"), "#! /bin/sh\ntimestamp='2025-07-10'\necho fresh-guess\n");
        File.WriteAllText(Path.Combine(automake, "config.sub"), "#! /bin/sh\ntimestamp='2025-07-10'\necho fresh-sub\n");
        var tree = Path.Combine(dir, "tree");
        Directory.CreateDirectory(Path.Combine(tree, "config"));
        File.WriteAllText(Path.Combine(tree, "config.sub"), "#! /bin/sh\ntimestamp='2003-02-22'\necho old\n");
        File.WriteAllText(Path.Combine(tree, "config", "config.guess"), "#! /bin/sh\ntimestamp='2009-12-30'\necho old\n");
        File.WriteAllText(Path.Combine(tree, "config", "config.sub"), "#! /bin/sh\ntimestamp='2026-01-01'\necho newer\n");
        File.WriteAllText(Path.Combine(tree, "config.guess"), "not GNU's script\n");
        try
        {
            Environment.SetEnvironmentVariable("HOMEBREW_MSYS2_ROOT", msys);
            BuildEnvironment.RefreshConfigScripts(tree);
        }
        finally { Environment.SetEnvironmentVariable("HOMEBREW_MSYS2_ROOT", saved); }
        Check(File.ReadAllText(Path.Combine(tree, "config.sub")).Contains("fresh-sub"), "old config.sub refreshed");
        Check(File.ReadAllText(Path.Combine(tree, "config", "config.guess")).Contains("fresh-guess"), "old config.guess refreshed");
        Check(File.ReadAllText(Path.Combine(tree, "config", "config.sub")).Contains("newer"), "newer config.sub kept");
        Check(File.ReadAllText(Path.Combine(tree, "config.guess")) == "not GNU's script\n", "non-GNU config.guess left alone");
        return count;

        void Check(bool ok, string label)
        {
            if (!ok) throw new Exception("FAIL " + label);
            count++;
        }
    }
}
