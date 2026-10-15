class Tiledb < Formula
  desc "Universal storage engine"
  homepage "https://tiledb.com/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tiledb-2.27.0-8-any.pkg.tar.zst"
  sha256 "6131be49cf3b1d4cd9e3b07106fa43c7c1e094c5c1de38a4c25fd4a2a816268f"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/TileDB-Inc/TileDB/archive/refs/tags/2.30.1.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"tiledb_export.h"
    assert_path_exists lib/"libtiledb.dll.a"
    assert_path_exists bin/"libtiledb-2.27.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/tiledb.pc").read
  end
end
