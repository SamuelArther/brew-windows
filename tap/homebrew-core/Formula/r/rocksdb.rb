class Rocksdb < Formula
  desc "Embeddable, persistent key-value store for fast storage"
  homepage "https://rocksdb.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-rocksdb-11.1.1-2-any.pkg.tar.zst"
  sha256 "23c05b9181a5ba16080c340a3036efe96479204f5530d4238b843b91ff610bc4"
  license any_of: ["GPL-2.0-only", "Apache-2.0"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/facebook/rocksdb/archive/refs/tags/v11.8.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"rocksdb/c.h"
    assert_path_exists lib/"librocksdb.dll.a"
    assert_path_exists bin/"librocksdb.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/rocksdb.pc").read
  end
end
