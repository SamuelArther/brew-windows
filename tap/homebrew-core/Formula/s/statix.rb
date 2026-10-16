class Statix < Formula
  desc "Lints and suggestions for the nix programming language"
  homepage "https://github.com/oppiliappan/statix"
  url "https://github.com/oppiliappan/statix/archive/refs/tags/v0.5.8.tar.gz"
  sha256 "547ee83df5814c18f8577b5ca25a1f12a416900b6eaa95821386a28090e8a89d"
  license "MIT"
  head "https://github.com/oppiliappan/statix.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "d325c1dec445c4776ccfbecc0842d46baea62b70b5ed7a29ef4619209fe14b8a"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args(path: "bin")
  end

  test do
    (testpath/"test.nix").write <<~NIX
      github:oppiliappan/statix
    NIX
    assert_match "Found unquoted URI expression", shell_output("#{bin}/statix check test.nix", 1)

    system bin/"statix", "fix", "test.nix"
    system bin/"statix", "check", "test.nix"

    assert_match version.to_s, shell_output("#{bin}/statix --version")
  end
end
