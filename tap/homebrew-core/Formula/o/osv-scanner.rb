class OsvScanner < Formula
  desc "Vulnerability scanner which uses the OSV database"
  homepage "https://google.github.io/osv-scanner/"
  url "https://github.com/google/osv-scanner/archive/refs/tags/v2.7.0.tar.gz"
  sha256 "acd8ebbf345a9a04eb299cb8e1339512fd4f2ad402f8f40972edcffa3eb45109"
  license "Apache-2.0"
  head "https://github.com/google/osv-scanner.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "55171fa6b04f7afc2e777839d07ed2a7d3cc648ed39db68deeadc6d35ce5da2e"
  end

  depends_on "go" => [:build, :test]

  # `test do` block queries api.osv.dev
  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./cmd/osv-scanner"
  end

  test do
    (testpath/"go.mod").write <<~GOMOD
      module my-library

      require (
        github.com/BurntSushi/toml v1.0.0
      )
    GOMOD

    scan_output = shell_output("#{bin}/osv-scanner --lockfile #{testpath}/go.mod")
    # Windows prints the path with backslashes.
    go_mod = testpath/"go.mod"
    assert_match "Scanned #{OS.windows? ? go_mod.to_s.tr("/", "\\") : go_mod} file and found 1 package", scan_output
  end
end
