class Nyan < Formula
  desc "Colorizing `cat` command with syntax highlighting"
  homepage "https://github.com/toshimaru/nyan"
  url "https://github.com/toshimaru/nyan/releases/download/v1.2.6/nyan_Windows_amd64.zip"
  sha256 "110ea3e8ee1a8e60ab6ed1227ffdb915b193f9f8aef23c5761868d89238363e8"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/toshimaru/nyan/archive/refs/tags/v1.2.6.tar.gz"
  end

  def install
    bin.install "nyan.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/nyan --version")
    (testpath/"test.txt").write "nyan is a colourful cat."
    assert_match "nyan is a colourful cat.", shell_output("#{bin}/nyan test.txt")
  end
end
