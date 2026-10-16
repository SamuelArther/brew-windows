class Clp < Formula
  desc "Linear programming solver"
  homepage "https://github.com/coin-or/Clp"
  url "https://github.com/coin-or/Clp/releases/download/releases/1.17.11/Clp-releases.1.17.11-windows-2025-msvs-v17-Release-x64.zip"
  version "1.17.11"
  sha256 "be65e353b6af6a2a55e6c776cb398a56a5f149ff6823c88e631ec6019c2c5a03"
  license "EPL-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/coin-or/Clp/archive/refs/tags/releases/1.17.11.tar.gz"
    regex(%r{^(?:releases/)?v?(\d+(?:\.\d+)+)$}i)
  end

  def install
    install_tree
    windows_exe "libexec/bin/Clp.exe", "clp"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/clp --version 2>&1")
  end
end
