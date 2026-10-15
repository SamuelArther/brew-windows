class Depsguard < Formula
  desc "Harden package manager configs against supply chain attacks"
  homepage "https://depsguard.com"
  url "https://github.com/arnica/depsguard/releases/download/v0.1.40/depsguard-windows-amd64.zip"
  sha256 "c607458bfa4e4ebc3ac3eb1ebd34e9269c0eb5d485033befeda2c0a3c61476e8"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/arnica/depsguard/archive/refs/tags/v0.1.40.tar.gz"
  end

  def install
    bin.install "depsguard.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/depsguard --version 2>&1")
  end
end
