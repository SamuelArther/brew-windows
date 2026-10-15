class Freetype < Formula
  desc "Software library to render fonts"
  homepage "https://www.freetype.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-freetype-2.15.0-1-any.pkg.tar.zst"
  sha256 "a9a691f71d1a553099c54ca666e611abfb2b4ff08dae24f2a5af72e22a7e5feb"
  license "FTL"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://downloads.sourceforge.net/project/freetype/freetype2/2.15.0/freetype-2.15.0.tar.xz"
    regex(/url=.*?freetype[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"freetype2/ft2build.h"
    assert_path_exists lib/"libfreetype.dll.a"
    assert_path_exists bin/"libfreetype-6.dll"
  end
end
