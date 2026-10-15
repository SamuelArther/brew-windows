class Libharu < Formula
  desc "Library for generating PDF files"
  homepage "https://github.com/libharu/libharu"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libharu-2.4.6-1-any.pkg.tar.zst"
  sha256 "c9a15b6ef7482782578b7212d20e36bac5e022ac025e17a5a5e721f4c8725093"
  license "Zlib"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/libharu/libharu/archive/refs/tags/v2.4.6.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"hpdf.h"
    assert_path_exists lib/"libhpdf.dll.a"
    assert_path_exists bin/"libhpdf.dll"
  end
end
