class Tproxy < Formula
  desc "CLI tool to proxy and analyze TCP connections"
  homepage "https://go-zero.dev"
  url "https://github.com/kevwan/tproxy/releases/download/v0.9.2/tproxy-v0.9.2-windows-amd64.zip"
  sha256 "e551f2ae8e14b6a97c930309d3135a33a0a28c37549369698625650f2877dbbd"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/kevwan/tproxy/archive/refs/tags/v0.9.2.tar.gz"
  end

  def install
    bin.install "tproxy.exe"
  end

  test do
    system bin/"tproxy", "--help"
  end
end
