class SatelliteTracker < Formula
  desc "Terminal-based real-time satellite tracking and orbit prediction application"
  homepage "https://github.com/ShenMian/tracker"
  url "https://github.com/ShenMian/tracker/releases/download/v0.2.1/tracker-windows-x86_64.zip"
  sha256 "67eb2d4ba2e36aec1fe5eb858c65cddaed82908403281c77b4cd3a378fbc3dc4"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ShenMian/tracker/archive/refs/tags/v0.2.1.tar.gz"
  end

  def install
    bin.install "tracker.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tracker --version 2>&1")
  end
end
