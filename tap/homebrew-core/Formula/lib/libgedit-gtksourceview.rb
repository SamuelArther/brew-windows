class LibgeditGtksourceview < Formula
  desc "Text editor widget for code editing"
  homepage "https://gitlab.gnome.org/World/gedit/libgedit-gtksourceview"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgedit-gtksourceview-299.7.0-1-any.pkg.tar.zst"
  sha256 "3c9dfcad404aa1985894a08947a5fcf0fc6ea1fb93c2358879bb28e53fac6738"
  license "LGPL-2.1-only"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://gitlab.gnome.org/World/gedit/libgedit-gtksourceview/-/archive/299.8.0/libgedit-gtksourceview-299.8.0.tar.bz2"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libgedit-gtksourceview-300/gtksourceview/gtksource.h"
    assert_path_exists lib/"libgedit-gtksourceview-300.dll.a"
    assert_path_exists bin/"libgedit-gtksourceview-300-5.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libgedit-gtksourceview-300.pc").read
  end
end
