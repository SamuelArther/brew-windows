class Libdvdcss < Formula
  desc "Access DVDs as block devices without the decryption"
  homepage "https://www.videolan.org/developers/libdvdcss.html"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libdvdcss-1.6.0-1-any.pkg.tar.zst"
  sha256 "7f1ef10e14db4aa96976689540a6742857f3ca62192d4e7e7748d1ee4629df41"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.videolan.org/pub/libdvdcss/"
    regex(%r{href=["']?v?(\d+(?:\.\d+)+)/?["' >]}i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"dvdcss/dvdcss.h"
    assert_path_exists lib/"libdvdcss.dll.a"
    assert_path_exists bin/"libdvdcss-2.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libdvdcss.pc").read
  end
end
