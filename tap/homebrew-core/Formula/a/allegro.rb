class Allegro < Formula
  desc "C/C++ multimedia library for cross-platform game development"
  homepage "https://liballeg.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-allegro-5.2.11.1-3-any.pkg.tar.zst"
  sha256 "13a281721f0edbd84ae66528645ae0c5e8141fdf72e31335009d42e148564994"
  license "Zlib"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/liballeg/allegro5/releases/download/5.2.11.3/allegro-5.2.11.3.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"allegro5/cpu.h"
    assert_path_exists lib/"liballegro.dll.a"
    assert_path_exists bin/"allegro-5.2.dll"
  end
end
