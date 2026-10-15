class Judy < Formula
  desc "State-of-the-art C library that implements a sparse dynamic array"
  homepage "https://judy.sourceforge.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-judy-1.0.5-1-any.pkg.tar.zst"
  sha256 "df0c7a625165a8c0de48f0925aeba4c4cf5277d516c42c997113759bbe1a1059"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://downloads.sourceforge.net/project/judy/judy/Judy-1.0.5/Judy-1.0.5.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"Judy.h"
    assert_path_exists lib/"libJudy.dll.a"
    assert_path_exists bin/"libJudy-1.dll"
  end
end
