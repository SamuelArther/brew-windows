class Cascadia < Formula
  desc "Go cascadia package command-line CSS selector"
  homepage "https://github.com/suntong/cascadia"
  url "https://github.com/suntong/cascadia/releases/download/v1.5.1/cascadia_1.5.1_windows_amd64.zip"
  sha256 "ecf602ed5ed662b2e10783217675da46df22f15850be02f59e4d46812371c30f"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/suntong/cascadia/archive/refs/tags/v1.5.1.tar.gz"
  end

  def install
    bin.install "cascadia.exe"
  end

  test do
    system bin/"cascadia", "--version"
  end
end
