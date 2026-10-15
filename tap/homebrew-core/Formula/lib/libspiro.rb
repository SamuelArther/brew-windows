class Libspiro < Formula
  desc "Library to simplify the drawing of curves"
  homepage "https://github.com/fontforge/libspiro"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libspiro-1~20240903-1-any.pkg.tar.zst"
  version "20240903"
  sha256 "308f9b83ec62e81f8b13e4c5b23fd7f3d363f3e3349b329212d7d9af22f89ae4"
  license "GPL-3.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/fontforge/libspiro/releases/download/20240903/libspiro-dist-20240903.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"spiro.h"
    assert_path_exists lib/"libspiro.dll.a"
    assert_path_exists bin/"libspiro-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libspiro.pc").read
  end
end
