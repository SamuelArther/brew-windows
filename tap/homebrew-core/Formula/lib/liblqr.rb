class Liblqr < Formula
  desc "C/C++ seam carving library"
  homepage "https://liblqr.wikidot.com/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-liblqr-0.4.3-3-any.pkg.tar.zst"
  sha256 "6e1b18ca4ed41607951e45c682b05c359be6d04271d589ea75a4f8472b8dfa01"
  license "LGPL-3.0-only"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/carlobaldassi/liblqr/archive/refs/tags/v0.4.3.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"lqr-1/lqr.h"
    assert_path_exists lib/"liblqr-1.dll.a"
    assert_path_exists bin/"liblqr-1-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/lqr-1.pc").read
  end
end
