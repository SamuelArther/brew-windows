using System.Formats.Tar;
using Brew;
using Brew.Formulae;
using Brew.Ruby;

// Formula API and build-environment gaps the wave9-foundation and wave1-retry2-toolchain batches hit.
static class FoundationFormulae
{
    public static int Run(string root)
    {
        var count = 0;
        var dir = Path.Combine(root, "foundation");
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
        string ClassOf(string name) => FormulaLoader.ClassName(name);
        string Simple(string name, string version, string extra = "") =>
            $"class {ClassOf(name)} < Formula\n  url \"https://example.com/{name.Split('@')[0]}-{version}.tar.gz\"\n  sha256 \"{new string('0', 64)}\"\n{extra}end\n";

        // A livecheck block delegates `version` to its formula (extra-cmake-modules' url uses #{version.major}),
        // and a bare `version` in the class body reads the version the url gives.
        var ecm = FormulaLoader.LoadSpec(Write("ecm-probe", Simple("ecm-probe", "6.31.0",
            "  livecheck do\n    url \"https://kde.org/announcements/frameworks/#{version.major}/\"\n    regex(/v?(\\d+(?:\\.\\d+)+)/i)\n  end\n  desc \"v#{version}\"\n")));
        Check(ecm.Livecheck?.Url == "https://kde.org/announcements/frameworks/6/", "livecheck url uses the formula's version");
        Check(ecm.Desc == "v6.31.0" && ecm.Version == "6.31.0", "class-body version reads the url's version");

        // A resource's `mirror` is kept as its fallback (ant's ivy is gone from dlcdn.apache.org, still on the archive).
        var ant = FormulaLoader.LoadSpec(Write("ant-probe", Simple("ant-probe", "1.10.18",
            "  resource \"ivy\" do\n    url \"https://www.apache.org/dyn/closer.lua?path=ant/ivy/2.5.3/apache-ivy-2.5.3-bin.tar.gz\"\n    mirror \"https://archive.apache.org/dist/ant/ivy/2.5.3/apache-ivy-2.5.3-bin.tar.gz\"\n    sha256 \"" + new string('1', 64) + "\"\n  end\n")));
        Check(ant.Resources.Single().Mirrors.SequenceEqual(["https://archive.apache.org/dist/ant/ivy/2.5.3/apache-ivy-2.5.3-bin.tar.gz"]), "resource mirrors are kept");

        // uses_from_macos "x" => :test is a test dependency when the tap has x (vi-sql and lazysql pipe SQL into
        // sqlite3); one the tap lacks, or a runtime one, stays macOS-only.
        Write("sqlite-probe", Simple("sqlite-probe", "3.0"));
        var user = FormulaLoader.LoadSpec(Write("sql-user", Simple("sql-user", "1.0",
            "  uses_from_macos \"sqlite-probe\" => :test\n  uses_from_macos \"nowhere-probe\" => :test\n  uses_from_macos \"zlib\"\n")));
        Check(user.Dependencies.Any(d => d.Name == "sqlite-probe" && d.Test), "test-only uses_from_macos in the tap is a test dependency");
        Check(!user.Dependencies.Any(d => d.Name is "nowhere-probe" or "zlib"), "other uses_from_macos stay out of the dependencies");

        // Keg.for(python3).to_formula.versioned_formulae (python-setuptools, certifi, pycparser): the other
        // name@version formulae, newest first.
        Write("pyx@3.15", Simple("pyx@3.15", "3.15.0"));
        Write("pyx@3.13", Simple("pyx@3.13", "3.13.9"));
        Write("pyx@3.14", Simple("pyx@3.14", "3.14.2"));
        var prefix = new Paths().Prefix.Replace('\\', '/');
        var it = FormulaLoader.NewInterpreterForTests();
        it.Cwd = dir;
        var probe = Path.Combine(formulaDir, "p", "probe.rb");
        var got = it.ToS(it.Run($$"""
            k = Keg.for(Pathname("{{prefix}}/opt/pyx@3.15/bin/python3.15"))
            [k.name, k.to_formula.versioned_formulae.map(&:name), k.to_formula.versioned_formulae.select { |f| f.version >= "3.14" }.map { |f| f.version.major_minor.to_s }].inspect
            """, probe));
        Check(got == """["pyx@3.15", ["pyx@3.14", "pyx@3.13"], ["3.14"]]""", "Keg.for/versioned_formulae: " + got);
        got = it.ToS(it.Run($$"""begin; Keg.for(Pathname("{{dir.Replace('\\', '/')}}/elsewhere")); rescue NotAKegError => e; "not a keg"; end""", probe));
        Check(got == "not a keg", "a path outside the prefix is not a keg");

        // ENV.deparallelize: one make job, only for its block when it has one; CLASSPATH joins with ';' (antlr's test).
        got = it.ToS(it.Run("""
            ENV["MAKEFLAGS"] = "-j8"; ENV["HOMEBREW_MAKE_JOBS"] = "8"
            inside = ENV.deparallelize { [ENV["MAKEFLAGS"], ENV["HOMEBREW_MAKE_JOBS"]] }
            after = [ENV["MAKEFLAGS"], ENV["HOMEBREW_MAKE_JOBS"]]
            ENV.deparallelize
            ENV.delete "CLASSPATH"; ENV.prepend "CLASSPATH", "C:/x/antlr.jar", ":"; ENV.prepend "CLASSPATH", ".", ":"
            [inside, after, ENV["MAKEFLAGS"], ENV["HOMEBREW_MAKE_JOBS"], ENV["CLASSPATH"]].inspect
            """, probe));
        Check(got == """[[nil, "1"], ["-j8", "8"], nil, "1", ".;C:/x/antlr.jar"]""", "deparallelize and CLASSPATH: " + got);
        // mv of a built executable named without .exe (build2: mv "b/b", "b/b-boot").
        got = it.ToS(it.Run("""Dir.mkdir("mvexe") unless Dir.exist?("mvexe"); File.write("mvexe/b.exe", "x"); FileUtils.mv "mvexe/b", "mvexe/b-boot"; Dir.children("mvexe").sort.inspect""", probe));
        Check(got == """["b-boot.exe"]""", "mv moves tool.exe for tool: " + got);

        // The build environment runs make in parallel (MAKEFLAGS, as superenv sets it).
        var saved = Environment.GetEnvironmentVariable("HOMEBREW_MSYS2_ROOT");
        var msys = Path.Combine(dir, "msys64");
        Directory.CreateDirectory(Path.Combine(msys, "usr", "bin"));
        File.WriteAllText(Path.Combine(msys, "usr", "bin", "bash.exe"), "");
        try
        {
            Environment.SetEnvironmentVariable("HOMEBREW_MSYS2_ROOT", msys);
            Directory.CreateDirectory(Path.Combine(dir, "build"));
            var context = new InstallContext(Path.Combine(dir, "keg"), Path.Combine(dir, "build"), null, prefix);
            BuildEnvironment.Apply(new Paths(), context, []);
            Check(context.BuildEnvironment.GetValueOrDefault("MAKEFLAGS") == "-j" + context.BuildEnvironment["HOMEBREW_MAKE_JOBS"], "build env sets MAKEFLAGS=-j<jobs>");
        }
        finally { Environment.SetEnvironmentVariable("HOMEBREW_MSYS2_ROOT", saved); }

        // An unknown name doesn't surface another formula's load error while aliases are searched (cgal reported
        // extra-cmake-modules' NameError instead of its missing dependency).
        var paths = new Paths();
        var broken = Path.Combine(paths.Taps, "homebrew-core", "Formula", "z", "zz-broken-probe.rb");
        Directory.CreateDirectory(Path.GetDirectoryName(broken)!);
        File.WriteAllText(broken, "class ZzBrokenProbe < Formula\n  url \"https://example.com/x-1.0.tar.gz\"\n  this_is_not_defined\nend\n");
        try
        {
            string error = null;
            try { new FormulaRepository(paths).Find("zz-missing-probe"); }
            catch (InvalidOperationException e) { error = e.GetType().Name; }
            Check(error == nameof(FormulaUnavailableException), "missing name is unavailable despite a broken formula: " + error);
        }
        finally { File.Delete(broken); }

        // Windows' tar makes file symlinks; one pointing at a directory becomes a directory link (whodb-cli's baml
        // resource: languages/rust/baml/types -> ../../../engine/language_client_cffi/types).
        var tgz = Path.Combine(dir, "links-1.0.tar.gz");
        using (var output = File.Create(tgz))
        using (var gzip = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionLevel.Fastest))
        using (var tar = new TarWriter(gzip, TarEntryFormat.Pax))
        {
            tar.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "links-1.0/b/real/"));
            var file = new PaxTarEntry(TarEntryType.RegularFile, "links-1.0/b/real/f.txt") { DataStream = new MemoryStream("hi"u8.ToArray()) };
            tar.WriteEntry(file);
            tar.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "links-1.0/a/"));
            tar.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "links-1.0/a/link") { LinkName = "../b/real" });
        }
        var staged = Archive.StageAsync(tgz, "links-1.0.tar.gz", Path.Combine(dir, "stage-links")).GetAwaiter().GetResult();
        var link = Path.Combine(staged, "a", "link");
        Check(new DirectoryInfo(link).LinkTarget != null && Directory.Exists(link), "directory symlink from a tarball is a directory link");
        Check(File.ReadAllText(Path.Combine(link, "f.txt")) == "hi", "files read through the directory link");

        // So does a poured bottle's (an MSYS2 keg's libexec/bin -> usr/bin).
        var bottle = Path.Combine(dir, "pourlink-1.0.bottle.tar.gz");
        using (var output = File.Create(bottle))
        using (var gzip = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionLevel.Fastest))
        using (var tar = new TarWriter(gzip, TarEntryFormat.Pax))
        {
            tar.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "pourlink/1.0/libexec/"));
            tar.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "pourlink/1.0/libexec/bin") { LinkName = "usr/bin" });
            tar.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "pourlink/1.0/libexec/usr/bin/"));
            tar.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, "pourlink/1.0/libexec/usr/bin/tool.txt") { DataStream = new MemoryStream("t"u8.ToArray()) });
        }
        var keg = Path.Combine(dir, "Cellar", "pourlink", "1.0");
        Bottles.PourAsync(new Paths(), bottle, keg, "any_skip_relocation").GetAwaiter().GetResult();
        Check(File.Exists(Path.Combine(keg, "libexec", "bin", "tool.txt")), "poured bottle's directory link is a directory link");
        return count;
    }
}
