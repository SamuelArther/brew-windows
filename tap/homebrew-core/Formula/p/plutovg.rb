class Plutovg < Formula
  desc "Tiny 2D vector graphics library in C"
  homepage "https://github.com/sammycage/plutovg"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-plutovg-1.3.3-1-any.pkg.tar.zst"
  sha256 "d5915c40cff48c2f7b53aed3cb93217811df1d4e4b64f557b77cb013cc2d2d19"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/sammycage/plutovg/archive/refs/tags/v1.3.3.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"plutovg/plutovg.h"
    assert_path_exists lib/"libplutovg.dll.a"
    assert_path_exists bin/"libplutovg.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/plutovg.pc").read
  end
end
