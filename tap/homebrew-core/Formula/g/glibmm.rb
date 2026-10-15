class Glibmm < Formula
  desc "C++ interface to glib"
  homepage "https://gtkmm.gnome.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-glibmm-2.66.10-1-any.pkg.tar.zst"
  sha256 "a51696554981949d0b3b8f023bc6ce5f7871cd2c54ebcdcd86f0e3681ff23159"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.gnome.org/sources/glibmm/2.90/glibmm-2.90.0.tar.xz"
    regex(/glibmm[._-]v?(\d+\.\d*[02468](?:\.\d+)*)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"giomm-2.4/giomm.h"
    assert_path_exists lib/"libgiomm-2.4.dll.a"
    assert_path_exists bin/"libgiomm-2.4-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/giomm-2.4.pc").read
  end
end
