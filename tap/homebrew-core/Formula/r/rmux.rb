class Rmux < Formula
  desc "Terminal multiplexer with a tmux-style CLI and daemon runtime"
  homepage "https://rmux.io"
  url "https://static.crates.io/crates/rmux/rmux-0.10.0.crate"
  sha256 "116b669b1cf4f994f6296a3aa5b329e14c6af26390d12ac0741e2aa31481b630"
  license any_of: ["MIT", "Apache-2.0"]

  bottle do
    sha256 cellar: :any, x86_64_windows: "eeb905525c2b0b19421949ffd4d83f1abd2d15688e9922dfbf506aaa9da3ba20"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
    man1.install "docs/man/rmux.1"
  end

  test do
    assert_match "rmux #{version}", shell_output("#{bin}/rmux -V")
    diagnostics = JSON.parse(shell_output("#{bin}/rmux diagnose --json"))
    assert_equal version.to_s, diagnostics.fetch("version")
  end
end
