class Portal < Formula
  desc "Quick and easy command-line file transfer utility from any computer to another"
  homepage "https://github.com/SpatiumPortae/portal"
  url "https://github.com/SpatiumPortae/portal/releases/download/v1.2.3/portal_1.2.3_Windows_x86_64.zip"
  sha256 "f775a4a9362da02d9ad19436deb18b01a371ad9ab5ba337c4815a2d00dcfef04"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/SpatiumPortae/portal/archive/refs/tags/v1.2.3.tar.gz"
  end

  def install
    bin.install "portal.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/portal version 2>&1")
  end
end
