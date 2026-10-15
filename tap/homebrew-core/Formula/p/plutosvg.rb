class Plutosvg < Formula
  desc "Tiny SVG rendering library in C"
  homepage "https://github.com/sammycage/plutosvg"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-plutosvg-0.0.8-1-any.pkg.tar.zst"
  sha256 "1ecdaa9c77ce47948af2d1598a6c503bdc31a9222ebe64592a2578a1f6fd0406"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/sammycage/plutosvg/archive/refs/tags/v0.0.8.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"plutosvg/plutosvg.h"
    assert_path_exists lib/"libplutosvg.dll.a"
    assert_path_exists bin/"libplutosvg-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/plutosvg.pc").read
  end
end
