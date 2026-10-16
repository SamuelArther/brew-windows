class TreCommand < Formula
  desc "Tree command, improved"
  homepage "https://github.com/dduan/tre"
  url "https://github.com/dduan/tre/releases/download/v0.4.0/tre-v0.4.0-x86_64-pc-windows-gnu.zip"
  sha256 "7be50ba9ef68d6d39c430ae2b1fd0ad4127d8e3eb75be539679e6ffb21742ab8"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/dduan/tre/archive/refs/tags/v0.4.0.tar.gz"
  end

  def install
    bin.install "tre.exe"
  end

  test do
    (testpath/"foo.txt").write("")
    assert_match("── foo.txt", shell_output("#{bin}/tre"))
  end
end
