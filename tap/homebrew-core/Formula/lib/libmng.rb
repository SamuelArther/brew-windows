class Libmng < Formula
  desc "MNG/JNG reference library"
  homepage "https://sourceforge.net/projects/libmng/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libmng-2.0.3-7-any.pkg.tar.zst"
  sha256 "140a618c256624d86d01207e769e44bce5cfe6150492ae04bf4e3e9e258499c8"
  license "Zlib"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://downloads.sourceforge.net/project/libmng/libmng-devel/2.0.3/libmng-2.0.3.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libmng.h"
    assert_path_exists lib/"libmng.dll.a"
    assert_path_exists bin/"libmng-2.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libmng-static.pc").read
  end
end
