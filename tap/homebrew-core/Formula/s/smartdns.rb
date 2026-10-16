class Smartdns < Formula
  desc "Rule-based DNS server for fast IP resolution, DoT/DoQ/DoH/DoH3 supported"
  homepage "https://github.com/mokeyish/smartdns-rs"
  url "https://github.com/mokeyish/smartdns-rs/releases/download/v0.13.1/smartdns-x86_64-pc-windows-msvc-v0.13.1.zip"
  sha256 "3257a903ec82a8bd7a02104788a321d512c8cef618ac82b7a4cf68ab8f0fa529"
  license "GPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mokeyish/smartdns-rs/archive/refs/tags/v0.13.1.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/smartdns.exe", "smartdns"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/smartdns --version 2>&1")
  end
end
