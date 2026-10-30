class Monolith < Formula
  desc "CLI tool for saving complete web pages as a single HTML file"
  homepage "https://github.com/Y2Z/monolith"
  url "https://github.com/Y2Z/monolith/archive/refs/tags/v2.11.7.tar.gz"
  sha256 "f646d8d4f5158b48a38b179a2a06666ec24bbea9796d09e98492658bf2532ed9"
  license "CC0-1.0"
  head "https://github.com/Y2Z/monolith.git", branch: "master"

  depends_on "pkgconf" => :build
  depends_on "rust" => :build

  allow_network_access! :test

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    system bin/"monolith", "https://lyrics.github.io/db/P/Portishead/Dummy/Roads/"
  end
end
