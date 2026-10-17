class Bk < Formula
  desc "Terminal EPUB Reader"
  homepage "https://github.com/aeosynth/bk"
  url "https://github.com/aeosynth/bk/archive/refs/tags/v0.6.0.tar.gz"
  sha256 "c720c8e81e86709f8543ca1a97a3c30b3bb33d55692a536cefed0ad2e3dfabcd"
  license "MIT"
  head "https://github.com/aeosynth/bk.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "b7019ff9184250a3ae1120604f059a884ba59ba911e2091fa5aa866c83ae6db2"
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
    test_epub = test_fixtures("test.epub")
    output = shell_output("#{bin}/bk --meta #{test_epub}")
    assert_match "language: en", output
  end
end
