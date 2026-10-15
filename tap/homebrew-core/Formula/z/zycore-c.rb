class ZycoreC < Formula
  desc "Zyan Core Library for C"
  homepage "https://github.com/zyantific/zycore-c"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zycore-c-1.5.2-2-any.pkg.tar.zst"
  sha256 "3038019a9cf586c07b35826c11c69c35c68d0fec696003d496245950e7214f64"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/zyantific/zycore-c/archive/refs/tags/v1.5.2.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"Zycore/LibC.h"
    assert_path_exists lib/"libZycore.dll.a"
    assert_path_exists bin/"libZycore.dll"
  end
end
