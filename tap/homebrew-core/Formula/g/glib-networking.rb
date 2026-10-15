class GlibNetworking < Formula
  desc "Network related modules for glib"
  homepage "https://gitlab.gnome.org/GNOME/glib-networking"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-glib-networking-2.90.0-1-any.pkg.tar.zst"
  sha256 "798d4bf112cf569fbb67cc9e8ee3ef8fdbddb61dc34e13692636a387adb4cf91"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.gnome.org/sources/glib-networking/2.80/glib-networking-2.80.1.tar.xz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists lib/"gio/modules/libgiognomeproxy.dll"
  end
end
