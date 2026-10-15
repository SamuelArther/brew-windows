class Spatialindex < Formula
  desc "General framework for developing spatial indices"
  homepage "https://libspatialindex.org/en/latest/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-spatialindex-2.1.0-3-any.pkg.tar.zst"
  sha256 "428e6dabdbf830a383efebf96f39c05f55e52419e424564806e56cc040d82937"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/libspatialindex/libspatialindex/releases/download/2.1.0/spatialindex-src-2.1.0.tar.bz2"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"spatialindex/Ball.h"
    assert_path_exists lib/"libspatialindex.dll.a"
    assert_path_exists bin/"libspatialindex-8.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libspatialindex.pc").read
  end
end
