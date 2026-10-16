class SpeedtestGo < Formula
  desc "CLI and Go API to Test Internet Speed using speedtest.net"
  homepage "https://github.com/showwin/speedtest-go"
  url "https://github.com/showwin/speedtest-go/releases/download/v1.8.3/speedtest-go_1.8.3_Windows_x86_64.tar.gz"
  sha256 "0b7843d2e9582eee4d1c7390a58e86353444bfe35c27b8073ab68711091be96d"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/showwin/speedtest-go/archive/refs/tags/v1.8.3.tar.gz"
  end

  def install
    bin.install "speedtest-go.exe" => "speedtest.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/speedtest --version 2>&1")
  end
end
