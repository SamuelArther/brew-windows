class Overtls < Formula
  desc "Simple proxy tunnel for bypassing the GFW"
  homepage "https://github.com/ShadowsocksR-Live/overtls"
  url "https://github.com/ShadowsocksR-Live/overtls/releases/download/v0.3.16/overtls-x86_64-pc-windows-msvc.zip"
  sha256 "485de702b7769c668f95e7eb115d03f4f0dbceba12da986714fc37dee152bd90"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ShadowsocksR-Live/overtls/archive/refs/tags/v0.3.16.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/overtls-bin.exe", "overtls-bin"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/overtls-bin --version 2>&1")
  end
end
