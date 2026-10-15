class Libspng < Formula
  desc "C library for reading and writing PNG format files"
  homepage "https://libspng.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libspng-0.7.4-1-any.pkg.tar.zst"
  sha256 "c17195981d1401a80f40e5c5a2295ff344c2ba99f8ba508259ba14bd0ffb48c7"
  license "BSD-2-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/randy408/libspng/archive/refs/tags/v0.7.4.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"spng.h"
    assert_path_exists lib/"libspng.dll.a"
    assert_path_exists bin/"libspng.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libspng.pc").read
  end
end
