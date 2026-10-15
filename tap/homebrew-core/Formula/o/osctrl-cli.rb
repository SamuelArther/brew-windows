class OsctrlCli < Formula
  desc "Fast and efficient osquery management"
  homepage "https://osctrl.net"
  url "https://github.com/jmpsec/osctrl/releases/download/v0.6.0/osctrl-Windows-x86_64.zip"
  sha256 "dd36fcd35d325012340fcf6dfa301ffa718fd2809602312591cb8c7459dcb347"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/jmpsec/osctrl/archive/refs/tags/v0.5.9.tar.gz"
  end

  def install
    bin.install "osctrl-cli-windows-amd64.exe"
  end

  test do
    assert_path_exists bin/"osctrl-cli-windows-amd64.exe"
  end
end
