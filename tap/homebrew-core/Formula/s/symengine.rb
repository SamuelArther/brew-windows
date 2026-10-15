class Symengine < Formula
  desc "Fast symbolic manipulation library written in C++"
  homepage "https://www.sympy.org/en/index.html"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-symengine-0.15.0-2-any.pkg.tar.zst"
  sha256 "253e5eb7eba03b7a2b7bf85a592e028c6bd712fe6b61d8be0c5df9648675d087"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/symengine/symengine/archive/refs/tags/v0.15.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"symengine/add.h"
    assert_path_exists lib/"libsymengine.dll.a"
    assert_path_exists bin/"libsymengine-0.15.dll"
  end
end
