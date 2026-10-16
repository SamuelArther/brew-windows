class Devil < Formula
  desc "Cross-platform image library"
  homepage "https://sourceforge.net/projects/openil/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-devil-1.8.0-12-any.pkg.tar.zst"
  sha256 "a2d511d4a68d4d40e402120f4601964e12def6b1f8e025fe74b5324ad9d2a8ce"
  license "LGPL-2.1-only"
  revision 6

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://downloads.sourceforge.net/project/openil/DevIL/1.8.0/DevIL-1.8.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"IL/il.h"
    assert_path_exists lib/"libIL.dll.a"
    assert_path_exists bin/"libIL.dll"
  end
end
