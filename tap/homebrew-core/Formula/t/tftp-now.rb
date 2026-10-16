class TftpNow < Formula
  desc "Single-binary TFTP server and client that you can use right now"
  homepage "https://github.com/puhitaku/tftp-now"
  url "https://github.com/puhitaku/tftp-now/releases/download/1.2.0/tftp-now-windows-amd64.exe"
  sha256 "9de4c8a1b2a1184f6cc6591bf54645eb38b3db25e984a0155287aa699cee3c53"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/puhitaku/tftp-now/archive/refs/tags/1.2.0.tar.gz"
  end

  def install
    bin.install "tftp-now-windows-amd64.exe" => "tftp-now.exe"
  end

  test do
    assert_path_exists bin/"tftp-now.exe"
  end
end
