class Pdtm < Formula
  desc "ProjectDiscovery's Open Source Tool Manager"
  homepage "https://projectdiscovery.io"
  url "https://github.com/projectdiscovery/pdtm/releases/download/v0.1.5/pdtm_0.1.5_windows_amd64.zip"
  sha256 "50d931eba9c3fbf5a98e4b3bb12deee7664adfcdc00553da8a985119e0199f09"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/projectdiscovery/pdtm/archive/refs/tags/v0.1.5.tar.gz"
  end

  def install
    bin.install "pdtm.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/pdtm --version 2>&1")
  end
end
