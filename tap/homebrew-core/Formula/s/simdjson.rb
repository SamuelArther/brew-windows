class Simdjson < Formula
  desc "SIMD-accelerated C++ JSON parser"
  homepage "https://simdjson.org"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-simdjson-5.0.5-1-any.pkg.tar.zst"
  sha256 "558b602dde2b91ac7f1a4ed2d5154d465cbf3cd68f86606dcfbd97a20b873ffa"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/simdjson/simdjson/archive/refs/tags/v5.0.5.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"simdjson.h"
    assert_path_exists lib/"libsimdjson.dll.a"
    assert_path_exists bin/"libsimdjson.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/simdjson.pc").read
  end
end
