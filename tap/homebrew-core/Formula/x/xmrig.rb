class Xmrig < Formula
  desc "Monero (XMR) CPU miner"
  homepage "https://xmrig.com/"
  url "https://github.com/xmrig/xmrig/releases/download/v6.26.0/xmrig-6.26.0-windows-gcc-x64.zip"
  sha256 "2de2ae3c2d01e6245e41101571cf7bb83e9236e8361213026308228d227b16fb"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/xmrig/xmrig/archive/refs/tags/v6.26.0.tar.gz"
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/xmrig.exe", "xmrig"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/xmrig --version 2>&1")
  end
end
