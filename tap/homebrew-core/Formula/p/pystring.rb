class Pystring < Formula
  desc "Collection of C++ functions for the interface of Python's string class methods"
  homepage "https://github.com/imageworks/pystring"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-pystring-1.1.4-2-any.pkg.tar.zst"
  sha256 "72ba24a0591e280d7dae14cefb96389e80f4aaa05f7dff4fdcf137411981a03d"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/imageworks/pystring/archive/refs/tags/v1.2.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"pystring/pystring.h"
  end
end
