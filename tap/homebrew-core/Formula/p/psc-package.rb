class PscPackage < Formula
  desc "Package manager for PureScript based on package sets"
  homepage "https://psc-package.readthedocs.io"
  url "https://github.com/purescript/psc-package/releases/download/v0.6.2/win64.tar.gz"
  sha256 "fdd443a01a83aa13bca1c0e0b3bfadf7311eb14a622528bfcc0154cc3d37c395"
  license "BSD-3-Clause"
  revision 2

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/purescript/psc-package/archive/refs/tags/v0.6.2.tar.gz"
  end

  def install
    bin.install "psc-package.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/psc-package --version 2>&1")
  end
end
