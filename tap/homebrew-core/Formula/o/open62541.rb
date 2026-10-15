class Open62541 < Formula
  desc "Open source implementation of OPC UA"
  homepage "https://open62541.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-open62541-1.5.2.r0.g12ae25e5f-3-any.pkg.tar.zst"
  sha256 "637e2f330aea28a4cf21a4590e80fd36602ca047f860370030737a2c5a78c481"
  license "MPL-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/open62541/open62541/archive/refs/tags/v1.5.9.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"open62541/util.h"
    assert_path_exists lib/"libopen62541.dll.a"
    assert_path_exists bin/"libopen62541.dll"
  end
end
