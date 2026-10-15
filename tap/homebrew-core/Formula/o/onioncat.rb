class Onioncat < Formula
  desc "VPN-adapter that provides location privacy using Tor or I2P"
  homepage "https://github.com/rahra/onioncat"
  url "https://github.com/rahra/onioncat/releases/download/v4.11.0/onioncat-4.11.0-win.zip"
  sha256 "f7f7bf2fb0e1b828af539bf773fa8c4a4628971b6c0e33047d57fc657e11f0b7"
  license "GPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/rahra/onioncat/releases/download/v4.11.0/onioncat-4.11.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/ocat.exe", "ocat"
  end

  test do
    system bin/"ocat", "-i", "fncuwbiisyh6ak3i.onion" # convert keybase's address to IPv6 address format
  end
end
