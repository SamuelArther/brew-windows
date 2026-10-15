class Cgl < Formula
  desc "Cut Generation Library"
  homepage "https://github.com/coin-or/Cgl"
  url "https://github.com/coin-or/Cgl/releases/download/releases/0.60.10/Cgl-releases.0.60.10-windows-2025-msvs-v17-Release-x64.zip"
  version "0.60.10"
  sha256 "6fd708e6f05db68d263ac2bd470e3a1b59cb17b1d3fc310ad63348f7d9322888"
  license "EPL-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/coin-or/Cgl/archive/refs/tags/releases/0.60.10.tar.gz"
    regex(%r{^releases/v?(\d+(?:\.\d+)+)$}i)
  end

  def install
    install_tree
    windows_exe "libexec/bin/CglUnitTest.exe", "CglUnitTest"
  end

  test do
    assert_path_exists bin/"CglUnitTest.exe"
  end
end
