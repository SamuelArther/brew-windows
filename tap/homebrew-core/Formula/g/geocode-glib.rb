class GeocodeGlib < Formula
  desc "GNOME library for gecoding and reverse geocoding"
  homepage "https://gitlab.gnome.org/GNOME/geocode-glib"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-geocode-glib-3.26.4-3-any.pkg.tar.zst"
  sha256 "01ade9ec3b12718d06d507dbfbbe2e2b6672b10feac0c356c0b914880d37887b"
  license "GPL-2.0-or-later"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.gnome.org/sources/geocode-glib/3.26/geocode-glib-3.26.4.tar.xz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"geocode-glib-2.0/geocode-glib/geocode-glib.h"
    assert_path_exists lib/"libgeocode-glib-2.dll.a"
    assert_path_exists bin/"libgeocode-glib-2-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/geocode-glib-2.0.pc").read
  end
end
