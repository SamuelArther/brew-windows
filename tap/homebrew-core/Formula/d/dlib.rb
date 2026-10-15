class Dlib < Formula
  desc "C++ library for machine learning"
  homepage "https://dlib.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-dlib-19.24.9-5-any.pkg.tar.zst"
  sha256 "955152e23cf7495b984b10021afe430610cdb88d7aef223daeffcb0e12d91db1"
  license "BSL-1.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/davisking/dlib/archive/refs/tags/v20.0.1.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"dlib/any.h"
    assert_path_exists lib/"libdlib.dll.a"
    assert_path_exists bin/"libdlib.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/dlib-1.pc").read
  end
end
