class Cryptominisat < Formula
  desc "Advanced SAT solver"
  homepage "https://www.msoos.org/cryptominisat5/"
  url "https://github.com/msoos/cryptominisat/releases/download/release/v5.17.0/cryptominisat5-v5.17.0-windows-x86_64.zip"
  version "5.17.0"
  sha256 "51ac3edfbe46e44d2a7623ac1fcb2d05a429aa8c212c60085c45b7bb9217588d"
  license "MIT"
  revision 1

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/msoos/cryptominisat/archive/refs/tags/release/v5.17.0.tar.gz"
    regex(%r{^(?:release/)?v?(\d+(?:\.\d+)+)$}i)
  end

  def install
    bin.install "cryptominisat5.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cryptominisat5 --version 2>&1")
  end
end
