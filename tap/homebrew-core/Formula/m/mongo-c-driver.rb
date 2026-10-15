class MongoCDriver < Formula
  desc "C driver for MongoDB"
  homepage "https://github.com/mongodb/mongo-c-driver"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-mongo-c-driver-2.5.4-1-any.pkg.tar.zst"
  sha256 "837f81cd2e622bd5cde1c1c5f6ee044c3140973dbce478d5513d4362f2d2a479"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/mongodb/mongo-c-driver/archive/refs/tags/2.5.6.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"bson/bson.h"
    assert_path_exists lib/"libbson2.dll.a"
    assert_path_exists bin/"libbson2.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/bson2-static.pc").read
  end
end
