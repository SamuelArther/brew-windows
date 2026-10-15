class Libdvdread < Formula
  desc "C library for reading DVD-video images"
  homepage "https://www.videolan.org/developers/libdvdnav.html"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libdvdread-7.1.1-1-any.pkg.tar.zst"
  sha256 "416b316f562fcaea29744a559e446ea9bc490f90085bbc925bc9a9e2767cdd41"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.videolan.org/pub/videolan/libdvdread/"
    regex(%r{href=["']?v?(\d+(?:\.\d+)+)/?["' >]}i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"dvdread/dvd_udf.h"
    assert_path_exists lib/"libdvdread.dll.a"
    assert_path_exists bin/"libdvdread-8.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/dvdread.pc").read
  end
end
