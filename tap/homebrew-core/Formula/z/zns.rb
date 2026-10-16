class Zns < Formula
  desc "CLI tool for querying DNS records with readable, colored output"
  homepage "https://github.com/znscli/zns"
  url "https://github.com/znscli/zns/releases/download/v0.6.0/zns_0.6.0_windows_amd64.tar.gz"
  sha256 "bfc6278b77d4de249dc0ec2adbea933971873c2ad153b9636e6c95dd56f96515"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/znscli/zns/archive/refs/tags/v0.6.0.tar.gz"
  end

  def install
    bin.install "zns.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/zns --version")
    assert_match "hera.ns.cloudflare.com.", shell_output("#{bin}/zns example.com -q NS --server 1.1.1.1")
  end
end
