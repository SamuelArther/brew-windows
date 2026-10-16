class Chiko < Formula
  desc "Ultimate Beauty gRPC Client for your Terminal"
  homepage "https://github.com/felangga/chiko"
  url "https://github.com/felangga/chiko/releases/download/v0.2.0/chiko_0.2.0_windows_amd64.tar.gz"
  sha256 "1620112c3d9af122cd91016e09adc42e6c04299278f456b555d76f932b033ea3"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/felangga/chiko/archive/refs/tags/v0.2.0.tar.gz"
  end

  def install
    bin.install "chiko.exe"
  end

  test do
    system bin/"chiko", "--help"
  end
end
