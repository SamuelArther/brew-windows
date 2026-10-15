class Maeparser < Formula
  desc "Maestro file parser"
  homepage "https://github.com/schrodinger/maeparser"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-maeparser-1.3.3-1-any.pkg.tar.zst"
  sha256 "83402ade3415381a097303e1d9de7997f909e4972a6ac155dccae58441700825"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/schrodinger/maeparser/archive/refs/tags/v1.3.3.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"maeparser/Buffer.hpp"
    assert_path_exists lib/"libmaeparser.dll.a"
    assert_path_exists bin/"libmaeparser-1.dll"
  end
end
