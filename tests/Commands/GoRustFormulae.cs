using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using Brew;
using Brew.Formulae;
using Brew.Ruby;

// Formula API gaps the wave-1 Go/Rust builds and tests hit (wave1-retry triage).
static class GoRustFormulae
{
    public static int Run(string root)
    {
        var count = 0;
        var dir = Path.Combine(root, "go-rust");
        Directory.CreateDirectory(dir);
        void Expect(string code, string expected)
        {
            var it = FormulaLoader.NewInterpreterForTests();
            it.Cwd = dir;
            string got;
            try { got = it.ToS(it.Run(code, Path.Combine(dir, "probe.rb"))); }
            catch (RubyError e) { got = $"{e.ClassName}: {e.Message}"; }
            catch (RubySyntaxError e) { got = "SyntaxError: " + e.Message; }
            if (got != expected) throw new Exception($"{code}\n  expected {expected}\n  got      {got}");
            count++;
        }
        void Check(bool ok, string label)
        {
            if (!ok) throw new Exception("FAIL " + label);
            count++;
        }

        // `[` right after a block indexes its result, space or not (openshift-cli: `.find { |t| ... } ["annotations"]`).
        Expect("""[{"n" => 1, "x" => 2}].find { |t| t["n"] == 1 } ["x"]""", "2");
        Expect("""([[1, 2]].map do |t| t end [0]).inspect""", "[1, 2]");
        Expect("""[[1].map { |x| x }, [2]].inspect""", "[[1], [2]]");
        // ActiveSupport-style Array#second (sftpgo: `read.lines.second`), Version before any formula version.
        Expect("""["a\nb\nc\n".lines.second, [1, 2, 3, 4, 5].fifth, [1].third.inspect, Version.new("5.9.0").major.to_s].inspect""", """["b\n", 5, "nil", "5"]""");
        // SecureRandom (ntfy), \R (ec), Regexp.escape leaving '/' alone, IO as Enumerable (cloudprober's io.any?).
        Expect("""[SecureRandom.hex(6).size, SecureRandom.uuid.size, "a\r\nb\nc".gsub(/\R/, "@"), Regexp.escape("a/b.c")].inspect""", """[12, 36, "a@b@c", "a/b\\.c"]""");
        Expect("""File.write("lines.txt", "x\ny\n"); [File.open("lines.txt") { |f| f.any? { |l| l == "y\n" } }, IO.ancestors.include?(Enumerable)].inspect""", "[true, true]");
        // ENV.remove (garble), ENV.append_to_rustflags (wasm-bindgen).
        Expect("""ENV["GR_PATH"] = "/shims/shared:/bin"; ENV.remove "GR_PATH", "/shims/shared:"; ENV.delete "RUSTFLAGS"; ENV.append_to_rustflags "-C a"; ENV.append_to_rustflags "-C b"; [ENV["GR_PATH"], ENV["RUSTFLAGS"]].inspect""",
            """["/bin", "-C a -C b"]""");
        // Pathname#each_child(false) yields bare names (ethereum, zsync: "./cmd/#{cmd}").
        Expect("""Pathname("cmds/a").mkpath; Pathname("cmds/b").mkpath; r = []; Pathname("cmds").each_child(false) { |c| r << c.to_s }; [r.sort, Pathname("cmds").each_child.map { |c| c.basename.to_s }.sort].inspect""",
            """[["a", "b"], ["a", "b"]]""");
        // bin/"tool" reads tool.exe (sigsum-go signs (bin/"sigsum-key").read); binread(length, offset) (photocraft-cli).
        Expect("""Pathname("exe/bin").mkpath; File.binwrite("exe/bin/tool.exe", "MZtool"); b = Pathname("exe/bin"); [(b/"tool").read, File.binread(b/"tool"), (b/"tool").size, File.exist?(b/"tool"), (b/"tool").binread(2), File.binread(b/"tool", 3, 2), (b/"tool").binread(4, 99).inspect].inspect""",
            """["MZtool", "MZtool", 6, true, "MZ", "too", "nil"]""");
        // ln_s to a built executable named without .exe links tool.exe (kubectx: ln_s bin/"kubectx", bin/"kubectl-ctx").
        Expect("""Pathname("lnexe/bin").mkpath; File.write("lnexe/bin/kubectx.exe", "x"); bin = Pathname("lnexe/bin").expand_path; FileUtils.ln_s bin/"kubectx", bin/"kubectl-ctx"; FileUtils.ln_sf "kubectx", bin/"kubectl-ns"; Dir.children("lnexe/bin").sort.inspect""",
            """["kubectl-ctx.exe", "kubectl-ns.exe", "kubectx.exe"]""");
        // `inreplace ... do |s|`: StringInreplaceExtension#gsub! takes a Pathname (loki: s.gsub! var, testpath).
        Expect("""File.write("loki.yaml", "dir: /opt/var/loki\n"); Formula.allocate.inreplace("loki.yaml") { |s| s.gsub! Pathname("/opt/var"), Pathname("C:/t") }; File.read("loki.yaml")""",
            "dir: C:/t/loki\n");
        Expect("""begin; "a".gsub(Pathname("a"), "b"); rescue TypeError => e; e.message; end""", "wrong argument type Pathname (expected Regexp)");
        // Homebrew's global paths (cargo-audit: HOMEBREW_CACHE/"cargo_cache"; garble: HOMEBREW_SHIMS_PATH).
        Expect("""[HOMEBREW_CACHE, HOMEBREW_TEMP, HOMEBREW_SHIMS_PATH, HOMEBREW_LIBRARY].map(&:class).uniq.inspect""", "[Pathname]");

        // A bare command name is looked up on PATH only, never in the current directory first
        // (iam-policy-json-to-terraform's tree has a `go` script next to `system "go", "build"`).
        var lookup = FormulaLoader.NewInterpreterForTests();
        lookup.Cwd = dir;
        File.WriteAllText(Path.Combine(dir, "zzlocaltool.cmd"), "@echo off\n");
        lookup.Env["PATH"] = Path.Combine(dir, "nowhere");
        Check(lookup.ResolveProgram("zzlocaltool") == "zzlocaltool", "current directory isn't searched for a bare program name");
        Check(lookup.ResolveProgram("./zzlocaltool").EndsWith("zzlocaltool.cmd", StringComparison.OrdinalIgnoreCase), "an explicit ./ path still resolves");

        // Utils::Git.get_tar_commit_id reads the pax global header `git archive` writes (argocd-vault-plugin).
        const string sha = "0123456789abcdef0123456789abcdef01234567";
        var header = new byte[1024];
        Encoding.ASCII.GetBytes("pax_global_header").CopyTo(header, 0);
        header[156] = (byte)'g';
        Encoding.ASCII.GetBytes($"52 comment={sha}\n").CopyTo(header, 512);
        var tgz = Path.Combine(dir, "v1.0.tar.gz");
        using (var output = File.Create(tgz))
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest))
            gzip.Write(header);
        Expect($"""Utils::Git.get_tar_commit_id("{tgz.Replace('\\', '/')}")""", sha);
        Expect("""File.write("plain.txt", "x" * 2000); Utils::Git.get_tar_commit_id("plain.txt").inspect""", "nil");

        // A name with ".tar." in it is only extracted when the bytes are a tarball (bomctl's *.tar.gz.spdx.json is JSON);
        // a plain .tar is extracted.
        var json = Path.Combine(dir, "spdx-download");
        File.WriteAllText(json, "{\"spdxVersion\": \"SPDX-2.3\"}");
        var stagedJson = Archive.StageAsync(json, "bomctl_0.3.0_linux_amd64.tar.gz.spdx.json", Path.Combine(dir, "stage-json")).GetAwaiter().GetResult();
        Check(File.ReadAllText(Path.Combine(stagedJson, "bomctl_0.3.0_linux_amd64.tar.gz.spdx.json")).Contains("SPDX-2.3"), "JSON named .tar.gz.* staged as a file");
        var tarSource = Path.Combine(dir, "tar-source", "pkg-1.0");
        Directory.CreateDirectory(tarSource);
        File.WriteAllText(Path.Combine(tarSource, "README"), "hi");
        var tar = Path.Combine(dir, "pkg-1.0.tar");
        TarFile.CreateFromDirectory(Path.GetDirectoryName(tarSource)!, tar, includeBaseDirectory: false);
        var stagedTar = Archive.StageAsync(tar, "pkg-1.0.tar", Path.Combine(dir, "stage-tar")).GetAwaiter().GetResult();
        Check(File.Exists(Path.Combine(stagedTar, "README")), "plain .tar extracts");

        // `brew test` shares the package managers' caches with builds (Formula#common_sandbox_env).
        var paths = new Paths();
        Directory.CreateDirectory(Path.Combine(dir, "testpath"));
        var context = new InstallContext(Path.Combine(dir, "keg"), Path.Combine(dir, "testpath"), null, paths.Prefix);
        BuildEnvironment.ApplyForTest(paths, context, []);
        Check(context.BuildEnvironment["CARGO_HOME"] == Path.Combine(paths.CacheRoot, "cargo_cache"), "test CARGO_HOME is the shared cargo cache");
        Check(context.BuildEnvironment["HOMEBREW_CACHE"] == paths.CacheRoot && context.BuildEnvironment.ContainsKey("GOMODCACHE"), "test env has HOMEBREW_CACHE and Go caches");
        return count;
    }
}
