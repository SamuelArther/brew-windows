class Csfml < Formula
  desc "SMFL bindings for C"
  homepage "https://www.sfml-dev.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-csfml-3.0.0.rc3-1-any.pkg.tar.zst"
  sha256 "ff543a08538da3c7482e3ceed1566ca22ad1005911192ba5c1ba2c3d109c9a52"
  license "Zlib"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/SFML/CSFML/archive/refs/tags/3.0.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"CSFML/Audio.h"
    assert_path_exists lib/"libcsfml-audio.dll.a"
    assert_path_exists bin/"libcsfml-audio-3.dll"
  end
end
