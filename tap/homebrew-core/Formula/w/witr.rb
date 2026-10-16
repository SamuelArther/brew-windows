class Witr < Formula
  desc "Why is this running?"
  homepage "https://github.com/pranshuparmar/witr"
  url "https://github.com/pranshuparmar/witr/releases/download/v0.3.4/witr-windows-amd64.zip"
  sha256 "34ef930e6f53c026e952f5952442281640bae551e59477137a045f9a2141188f"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/pranshuparmar/witr/archive/refs/tags/v0.3.4.tar.gz"
  end

  def install
    bin.install "witr.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/witr --version")
    assert_match "process 99999999 does not exist", shell_output("#{bin}/witr --pid 99999999 2>&1", 2)
  end
end
