class Oras < Formula
  desc "OCI Registry As Storage"
  homepage "https://oras.land"
  url "https://github.com/oras-project/oras/releases/download/v1.3.4/oras_1.3.4_windows_amd64.zip"
  sha256 "ffdb6aa40267686b5d507da1f21a57fc502a9a7c86b90c54557d335644c99dbd"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/oras-project/oras/archive/refs/tags/v1.3.4.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "oras.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/oras version 2>&1")
  end
end
