class Kotofetch < Formula
  desc "Small, configurable CLI that displays Japanese quotes in the terminal"
  homepage "https://github.com/hxpe-dev/kotofetch"
  url "https://github.com/hxpe-dev/kotofetch/releases/download/v0.2.23/kotofetch--x86_64-pc-windows-msvc.zip"
  sha256 "f358463051978057a6599bc443c6706df99ff27ea8a8b7bcff2760fb50551510"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/hxpe-dev/kotofetch/archive/refs/tags/v0.2.23.tar.gz"
  end

  def install
    bin.install "kotofetch.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/kotofetch --version")

    output = shell_output("#{bin}/kotofetch --index 0 --translation english")
    assert_match "Fall down seven times, stand up eight.", output
  end
end
