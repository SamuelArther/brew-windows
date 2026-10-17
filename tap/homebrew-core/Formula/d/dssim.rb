class Dssim < Formula
  desc "RGBA Structural Similarity Rust implementation"
  homepage "https://kornel.ski/dssim"
  url "https://github.com/kornelski/dssim/archive/refs/tags/3.4.0.tar.gz"
  sha256 "5267e79f4604558d9f24ce02aa20597396a9b052d0ad1b2f8000d4d6bd162126"
  license "AGPL-3.0-or-later"

  bottle do
    sha256 cellar: :any, x86_64_windows: "9803f8a7a6daab4c3f5a10d397a836148adfde4f073430d1a4a21bc7fb4d3f60"
  end

  depends_on "rust" => :build

  on_intel do
    depends_on "nasm" => :build
  end

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    system bin/"dssim", test_fixtures("test.png"), test_fixtures("test.png")
  end
end
