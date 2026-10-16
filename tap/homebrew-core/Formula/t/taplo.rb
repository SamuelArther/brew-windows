class Taplo < Formula
  desc "TOML toolkit written in Rust"
  homepage "https://taplo.tamasfe.dev"
  url "https://github.com/tamasfe/taplo/archive/refs/tags/0.10.0.tar.gz"
  sha256 "c2f7b3234fc62000689a476b462784db4d1bb2be6edcc186654b211f691efaf8"
  license "MIT"
  head "https://github.com/tamasfe/taplo.git", branch: "master"

  # There can be a notable gap between when a version is tagged and a
  # corresponding release is created, so we check releases instead of the Git
  # tags. Upstream maintains multiple products in this repo and the "latest"
  # release may be for another product, so we have to check multiple releases
  # to identify the correct version.
  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
    strategy :github_releases
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "520b70e9cdb9c0759b4219306d5d488358b61190e8292472ba4a251bbfaaf1a6"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args(path: "crates/taplo-cli", features: "lsp")
    generate_completions_from_executable(bin/"taplo", "completions", shells: [:bash, :zsh, :fish, :pwsh])
  end

  test do
    test_file = testpath/"invalid.toml"
    (testpath/"invalid.toml").write <<~TOML
      # INVALID TOML DOC
      fruit = []

      [[fruit]] # Not allowed
    TOML

    output = shell_output("#{bin}/taplo lint #{test_file} 2>&1", 1)
    assert_match "expected array of tables", output

    assert_match version.to_s, shell_output("#{bin}/taplo --version")
  end
end
