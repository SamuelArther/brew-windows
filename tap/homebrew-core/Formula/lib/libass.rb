class Libass < Formula
  desc "Subtitle renderer for the ASS/SSA subtitle format"
  homepage "https://github.com/libass/libass"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libass-0.17.5-1-any.pkg.tar.zst"
  sha256 "4385fd47a77784478878f0f03f2d9afa59a5ac71facb7cad1dfe19c555e75762"
  license "ISC"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/libass/libass/releases/download/0.17.5/libass-0.17.5.tar.xz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"ass/ass.h"
    assert_path_exists lib/"libass.dll.a"
    assert_path_exists bin/"libass-9.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libass.pc").read
  end
end
