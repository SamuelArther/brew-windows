class Argtable < Formula
  desc "ANSI C library for parsing GNU-style command-line options"
  homepage "https://argtable.sourceforge.io"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-argtable-2.13-3-any.pkg.tar.zst"
  sha256 "eac72542634b9ece62d5b9b1471862573a60e2207284443ad35c5a7751daaafb"
  license "LGPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://downloads.sourceforge.net/project/argtable/argtable/argtable-2.13/argtable2-13.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"argtable2.h"
  end
end
