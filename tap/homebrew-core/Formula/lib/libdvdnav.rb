class Libdvdnav < Formula
  desc "DVD navigation library"
  homepage "https://www.videolan.org/developers/libdvdnav.html"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libdvdnav-7.0.0-2-any.pkg.tar.zst"
  sha256 "73fe3183674ca63187c9f0b2d40c9f181213e88cb48ddaf8dc4e2a1e9cf13e3f"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.videolan.org/pub/videolan/libdvdnav/"
    regex(%r{href=["']?v?(\d+(?:\.\d+)+)/?["' >]}i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"dvdnav/dvdnav.h"
    assert_path_exists lib/"libdvdnav.dll.a"
    assert_path_exists bin/"libdvdnav-4.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/dvdnav.pc").read
  end
end
