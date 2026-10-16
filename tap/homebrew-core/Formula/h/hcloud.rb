class Hcloud < Formula
  desc "Command-line interface for Hetzner Cloud"
  homepage "https://github.com/hetznercloud/cli"
  url "https://github.com/hetznercloud/cli/releases/download/v1.70.1/hcloud-windows-amd64.zip"
  sha256 "a8c7325062f14aa882260d23d00d0d222d61cf4096471deaa507409bc455782a"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/hetznercloud/cli/archive/refs/tags/v1.70.1.tar.gz"
  end

  def install
    bin.install "hcloud.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/hcloud version 2>&1")
  end
end
