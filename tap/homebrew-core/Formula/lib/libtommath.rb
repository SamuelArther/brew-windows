class Libtommath < Formula
  desc "C library for number theoretic multiple-precision integers"
  homepage "https://www.libtom.net/LibTomMath/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libtommath-1.3.0-2-any.pkg.tar.zst"
  sha256 "647644b6e7c73f1644e45d1aa772ebf6f2e9ca190c193a214417ac119694cf9f"
  license "Unlicense"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/libtom/libtommath/releases/download/v1.3.0/ltm-1.3.0.tar.xz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"tommath.h"
    assert_path_exists lib/"libtommath.dll.a"
    assert_path_exists bin/"libtommath-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libtommath.pc").read
  end
end
