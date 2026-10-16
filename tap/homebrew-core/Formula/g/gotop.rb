class Gotop < Formula
  desc "Terminal based graphical activity monitor inspired by gtop and vtop"
  homepage "https://github.com/xxxserxxx/gotop"
  url "https://github.com/xxxserxxx/gotop/releases/download/v4.2.0/gotop_v4.2.0_windows_amd64.zip"
  sha256 "2d8a1462aeead9a8a05a76948b8b0085e7d1f133cd2e44fd63dc188d3b4b3c70"
  license "BSD-3-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/xxxserxxx/gotop/archive/refs/tags/v4.2.0.tar.gz"
  end

  def install
    bin.install "gotop.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gotop --version 2>&1")
  end
end
