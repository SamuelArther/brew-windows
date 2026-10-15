class Libdmtx < Formula
  desc "Data Matrix library"
  homepage "https://libdmtx.sourceforge.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libdmtx-0.7.8-1-any.pkg.tar.zst"
  sha256 "d17ed117e8ee5e7b81c5e0bbb1ce069f8795a67813f697abaed022f935cadfe3"
  license "BSD-2-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/dmtx/libdmtx/archive/refs/tags/v0.7.8.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"dmtx.h"
    assert_path_exists lib/"libdmtx.dll.a"
    assert_path_exists bin/"libdmtx-0.dll"
  end
end
