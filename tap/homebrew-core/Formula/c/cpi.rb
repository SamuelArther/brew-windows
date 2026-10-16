class Cpi < Formula
  desc "Tiny c++ interpreter"
  homepage "https://treefrogframework.github.io/cpi/"
  url "https://github.com/treefrogframework/cpi/releases/download/v2.2.4/cpi-v2.2.4-windows.7z"
  sha256 "bcfd9e07eaccbc0f6a0372d7a3eadeebc6ecf499d293b74548ab539d7243941c"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/treefrogframework/cpi/archive/refs/tags/v2.2.4.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    windows_exe "libexec/cpi.exe", "cpi"
  end

  test do
    assert_path_exists bin/"cpi.exe"
  end
end
