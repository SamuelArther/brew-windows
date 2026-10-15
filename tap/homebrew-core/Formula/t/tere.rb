class Tere < Formula
  desc "Terminal file explorer"
  homepage "https://github.com/mgunyho/tere"
  url "https://github.com/mgunyho/tere/releases/download/v1.6.0/tere-1.6.0-x86_64-pc-windows-gnu.zip"
  sha256 "cf782e069907df23b0a0bec88552d2ef35e433d5b46f1a855d57f6a70c05d507"
  license "EUPL-1.2"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mgunyho/tere/archive/refs/tags/v1.6.0.tar.gz"
  end

  def install
    bin.install "tere.exe"
  end

  test do
    assert_path_exists bin/"tere.exe"
  end
end
