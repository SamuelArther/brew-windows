class Muparser < Formula
  desc "C++ math expression parser library"
  homepage "https://github.com/beltoforion/muparser"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-muparser-2.3.5-2-any.pkg.tar.zst"
  sha256 "cae79ce67b3c03b4f3ff94c7b2d1d3313e5138b9ed588ea95a6ae567dc8db2ac"
  license "BSD-2-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/beltoforion/muparser/archive/refs/tags/v2.3.5.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"muParser.h"
    assert_path_exists lib/"libmuparser.dll.a"
    assert_path_exists bin/"libmuparser.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/muparser.pc").read
  end
end
