class Openturns < Formula
  desc "Probabilistic modelling and uncertainty quantification library"
  homepage "https://github.com/openturns/openturns"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openturns-1.27-1-any.pkg.tar.zst"
  sha256 "829826be74fe254617d08f5ee5db9d30e7ced72c27cbec1171905550b1e8e183"
  license "LGPL-3.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/openturns/openturns/archive/refs/tags/v1.27.3.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"openturns/OT.hxx"
    assert_path_exists lib/"libOT.dll.a"
    assert_path_exists bin/"libOT-0.28.dll"
  end
end
