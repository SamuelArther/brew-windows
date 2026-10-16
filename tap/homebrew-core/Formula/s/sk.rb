class Sk < Formula
  desc "Fuzzy Finder in rust!"
  homepage "https://github.com/skim-rs/skim"
  url "https://github.com/skim-rs/skim/releases/download/v5.7.4/skim-x86_64-pc-windows-msvc.zip"
  sha256 "a9cecffe3c07b0d88dd4f6108371eeb632c300c81420eecedb25edb4fe971aaa"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/skim-rs/skim/archive/refs/tags/v5.7.4.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "sk.exe"
  end

  test do
    assert_match(/.*world/, pipe_output("#{bin}/sk -f wld", "hello\nworld"))
  end
end
