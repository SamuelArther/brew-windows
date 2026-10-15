class Raylib < Formula
  desc "Simple and easy-to-use library to learn videogames programming"
  homepage "https://www.raylib.com/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-raylib-6.0-1-any.pkg.tar.zst"
  sha256 "3f39f56b2b50da5f2ebc0a096bb255561e500987f7f3be4fe1b78f9751962560"
  license "Zlib"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/raysan5/raylib/archive/refs/tags/6.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"rlgl.h"
    assert_path_exists lib/"libraylib.dll.a"
    assert_path_exists bin/"libraylib.dll"
  end
end
