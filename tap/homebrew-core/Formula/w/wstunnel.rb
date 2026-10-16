class Wstunnel < Formula
  desc "Tunnel all your traffic over Websocket or HTTP2"
  homepage "https://github.com/erebe/wstunnel"
  url "https://github.com/erebe/wstunnel/releases/download/v11.0.0/wstunnel_11.0.0_windows_amd64.tar.gz"
  sha256 "024323c9c2dd1ed1c6f38d417d9b2776e2f0083bba507e80d4a8124c8451b164"
  license "BSD-3-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/erebe/wstunnel/archive/refs/tags/v11.0.0.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "wstunnel.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/wstunnel --version 2>&1")
  end
end
