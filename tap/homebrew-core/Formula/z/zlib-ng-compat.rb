class ZlibNgCompat < Formula
  desc "Zlib replacement with optimizations for next generation systems"
  homepage "https://github.com/zlib-ng/zlib-ng"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zlib-ng-compat-2.3.3-2-any.pkg.tar.zst"
  sha256 "b3ca6b437fb154a0fa26652002cdda591498ee4a0e08d91cc653c3702d090f25"
  license "Zlib"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/zlib-ng/zlib-ng/archive/refs/tags/2.3.3.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"zlib.h"
    assert_path_exists lib/"libz.dll.a"
    assert_path_exists bin/"zlib1.dll"
  end
end
