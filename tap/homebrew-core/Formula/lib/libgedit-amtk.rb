class LibgeditAmtk < Formula
  desc "Actions, Menus and Toolbars Kit for GTK applications"
  homepage "https://gedit-technology.net"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgedit-amtk-5.10.0-1-any.pkg.tar.zst"
  sha256 "13f12865cf928159469b8e93760ce7b2ee33661dbede4e8cd58281e3c6e58f05"
  license "LGPL-3.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://gitlab.gnome.org/World/gedit/libgedit-amtk/-/archive/5.11.0/libgedit-amtk-5.11.0.tar.bz2"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libgedit-amtk-5/amtk/amtk.h"
    assert_path_exists lib/"libgedit-amtk-5.dll.a"
    assert_path_exists bin/"libgedit-amtk-5-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libgedit-amtk-5.pc").read
  end
end
