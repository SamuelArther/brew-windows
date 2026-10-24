class Monolith < Formula
  desc "CLI tool for saving complete web pages as a single HTML file"
  homepage "https://github.com/Y2Z/monolith"
  url "https://github.com/Y2Z/monolith/archive/refs/tags/v2.11.6.tar.gz"
  sha256 "19390d1032cd2e10a7a589228fd035ab60b07f093a46e8a2bce4fa1c08f5e66b"
  license "CC0-1.0"
  head "https://github.com/Y2Z/monolith.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "59365bdd4bc6f999d303461024205761bf24a0a6548ccd8e62db1ac4c48627ec"
  end

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
