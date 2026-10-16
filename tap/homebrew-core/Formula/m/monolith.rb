class Monolith < Formula
  desc "CLI tool for saving complete web pages as a single HTML file"
  homepage "https://github.com/Y2Z/monolith"
  url "https://github.com/Y2Z/monolith/archive/refs/tags/v2.11.5.tar.gz"
  sha256 "dde7ee013e7361a791eae42d8bd0a82045d6396355c5170c79bfc830130a9427"
  license "CC0-1.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "46004df89be8c894ace4b4ce6d825b046ba24948862559ba3caac141c50dd0c1"
  end

  depends_on "pkgconf" => :build
  depends_on "rust" => :build

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    system bin/"monolith", "https://lyrics.github.io/db/P/Portishead/Dummy/Roads/"
  end
end
