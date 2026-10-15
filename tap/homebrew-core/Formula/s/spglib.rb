class Spglib < Formula
  desc "C library for finding and handling crystal symmetries"
  homepage "https://spglib.readthedocs.io/en/latest/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-spglib-2.7.0-4-any.pkg.tar.zst"
  sha256 "09b2cae781a1f2328709ca38900297c0288d6eee7b4b78fc75a51b4d3cdbba52"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/spglib/spglib/archive/refs/tags/v2.8.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"spglib.h"
    assert_path_exists lib/"libspglib_f08.dll.a"
    assert_path_exists bin/"libspglib_f08-2.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/spglib.pc").read
  end
end
