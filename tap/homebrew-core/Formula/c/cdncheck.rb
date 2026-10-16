class Cdncheck < Formula
  desc "Utility to detect various technology for a given IP address"
  homepage "https://projectdiscovery.io"
  url "https://github.com/projectdiscovery/cdncheck/releases/download/v1.3.2/cdncheck_1.3.2_windows_amd64.zip"
  sha256 "dd3c50ce0e7069fe40c72247ed4f6f02f1f6d35a10bbe99f2090835305062bcc"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/projectdiscovery/cdncheck/archive/refs/tags/v1.3.2.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "cdncheck.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cdncheck --version 2>&1")
  end
end
