class Coinutils < Formula
  desc "COIN-OR utilities"
  homepage "https://github.com/coin-or/CoinUtils"
  url "https://github.com/coin-or/CoinUtils/releases/download/releases/2.11.13/CoinUtils-releases.2.11.13-windows-2025-msvs-v17-Release-x64.zip"
  version "2.11.13"
  sha256 "802e2c2ddeed89f2528271a59f748a2dafb2581e88b59a012d4980f867046847"
  license "EPL-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url :homepage
    regex(%r{^(?:releases/)?(\d+(?:\.\d+)+)$}i)
  end

  def install
    install_tree
    windows_exe "libexec/bin/CoinUtilsUnitTest.exe", "CoinUtilsUnitTest"
  end

  test do
    assert_path_exists bin/"CoinUtilsUnitTest.exe"
  end
end
