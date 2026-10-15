class Jemalloc < Formula
  desc "Implementation of malloc emphasizing fragmentation avoidance"
  homepage "https://jemalloc.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-jemalloc-5.4.0-1-any.pkg.tar.zst"
  sha256 "7494cf2d8c6d82b4159c5bd6bc127ce24c453cd08271457e267bc44076fddaa6"
  license "BSD-2-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/jemalloc/jemalloc/releases/download/5.4.0/jemalloc-5.4.0.tar.bz2"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"jemalloc/jemalloc.h"
    assert_path_exists lib/"libjemalloc.dll.a"
    assert_path_exists bin/"libjemalloc.dll"
  end
end
