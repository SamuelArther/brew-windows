class Laszip < Formula
  desc "Lossless LiDAR compression"
  homepage "https://laszip.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-laszip-3.4.4-2-any.pkg.tar.zst"
  sha256 "a865770ab4ceea6d6538b7b44e3556b1aae974babf01b566d870415fb5813d1f"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/LASzip/LASzip/archive/refs/tags/3.5.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"laszip/laszip_api.h"
    assert_path_exists lib/"liblaszip3.dll.a"
    assert_path_exists bin/"liblaszip3.dll"
  end
end
