class Dnsglobe < Formula
  desc "Global DNS propagation checker TUI"
  homepage "https://github.com/514-labs/dnsglobe"
  url "https://github.com/514-labs/dnsglobe/releases/download/v0.5.0/dnsglobe-x86_64-pc-windows-msvc.zip"
  sha256 "8efc384e88f1aab3883c522cd23c3c6c2482c9ded14fadadfb04275a363bb0ba"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/514-labs/dnsglobe/archive/refs/tags/v0.5.0.tar.gz"
  end

  def install
    bin.install "dnsglobe.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dnsglobe --version")

    output = shell_output("#{bin}/dnsglobe --once brew.sh")
    assert_match(%r{propagation \(\d+/\d+ responding\)}, output)
  end
end
