class Termbg < Formula
  desc "Rust library for terminal background color detection"
  homepage "https://github.com/dalance/termbg"
  url "https://github.com/dalance/termbg/archive/refs/tags/v0.6.2.tar.gz"
  sha256 "b933907d181e59ce0aa522ed598a9fa70125a6523f7fbd1f537c3b99bd75ffdd"
  license any_of: ["Apache-2.0", "MIT"]
  head "https://github.com/dalance/termbg.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "8caad0b8b1d2563a1d956373868c90b76c6359bc68628a73b0925789fb6a74cd"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    # `Cargo.lock` is ignored
    system "cargo", "generate-lockfile"
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/termbg --version")
  end
end
