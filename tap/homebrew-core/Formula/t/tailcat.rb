class Tailcat < Formula
  desc "Netcat-like tool over Tailscale's data plane, without its control plane"
  homepage "https://github.com/tailscale/tailcat"
  url "https://github.com/tailscale/tailcat/releases/download/v0.7.0/tailcat_0.7.0_windows_amd64.zip"
  sha256 "f04cac07e3bf6c700b0b543df0d3315a3b312cd5a05ee76714e1cc848e9b2dff"
  license "BSD-3-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/tailscale/tailcat/archive/refs/tags/v0.7.0.tar.gz"
  end

  def install
    bin.install "tailcat.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tailcat --version 2>&1")
  end
end
