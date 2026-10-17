class Nuls < Formula
  desc "NuShell-inspired ls with colorful table output"
  homepage "https://github.com/cesarferreira/nuls"
  url "https://static.crates.io/crates/nuls/nuls-0.2.0.crate"
  sha256 "24fb69fbb3ca465f6e051d36c75867f9fbe3e358eedb931fcb65125e4946e08e"
  license "MIT"
  head "https://github.com/cesarferreira/nuls.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "f95a2f84b8ac95ae60d67d1a6832e3010619f726da3c62ad8879f9e8aefd9184"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    (testpath/"foo.txt").write "hello\n"

    assert_match version.to_s, shell_output("#{bin}/nuls --version")
    assert_match "foo.txt", shell_output("#{bin}/nuls #{testpath}")
  end
end
