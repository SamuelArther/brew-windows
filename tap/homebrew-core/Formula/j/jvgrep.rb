class Jvgrep < Formula
  desc "Grep for Japanese users of Vim"
  homepage "https://github.com/mattn/jvgrep"
  url "https://github.com/mattn/jvgrep/releases/download/v5.8.16/jvgrep_v5.8.16_windows_amd64.zip"
  sha256 "cc899091281d85834f73965621233c153c82552ac40936109211c7ea038cbc03"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mattn/jvgrep/archive/refs/tags/v5.8.16.tar.gz"
  end

  def install
    bin.install "jvgrep.exe"
  end

  test do
    (testpath/"Hello.txt").write("Hello World!")
    system bin/"jvgrep", "Hello World!", testpath
  end
end
