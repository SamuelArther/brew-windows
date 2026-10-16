class Leveldb < Formula
  desc "Key-value storage library with ordered mapping"
  homepage "https://github.com/google/leveldb/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-leveldb-1.23-3-any.pkg.tar.zst"
  sha256 "c684a12490c5f8fcbcf2875c5816d91c6a1016d9ce2be4d4ca75200b919055dd"
  license "BSD-3-Clause"
  revision 2

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/google/leveldb/archive/refs/tags/1.23.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"leveldb/c.h"
    assert_path_exists lib/"libleveldb.dll.a"
    assert_path_exists bin/"libleveldb.dll"
  end
end
