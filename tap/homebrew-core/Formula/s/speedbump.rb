class Speedbump < Formula
  desc "TCP proxy for simulating variable, yet predictable network latency"
  homepage "https://github.com/kffl/speedbump"
  url "https://github.com/kffl/speedbump/releases/download/v1.1.0/speedbump-v1.1.0-windows-amd64.zip"
  sha256 "aa9ad66e2c8e6f7ca3ae1603fcac1ad34cef6c6e78a2b6c1138b9c5d365d6608"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/kffl/speedbump/archive/refs/tags/v1.1.0.tar.gz"
  end

  def install
    bin.install "speedbump.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/speedbump --version 2>&1")
  end
end
