class Termbg < Formula
  desc "Rust library for terminal background color detection"
  homepage "https://github.com/dalance/termbg"
  url "https://github.com/dalance/termbg/archive/refs/tags/v0.6.2.tar.gz"
  sha256 "b933907d181e59ce0aa522ed598a9fa70125a6523f7fbd1f537c3b99bd75ffdd"
  license any_of: ["Apache-2.0", "MIT"]
  head "https://github.com/dalance/termbg.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "19ac14e7e28e065e52219b39f5a05c9b7a55d795f2106925a619279a023eb71d"
  end

  depends_on "rust" => :build

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/termbg --version")
  end
end
