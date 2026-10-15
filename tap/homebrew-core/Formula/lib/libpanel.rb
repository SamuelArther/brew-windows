class Libpanel < Formula
  desc "Dock/panel library for GTK 4"
  homepage "https://gitlab.gnome.org/GNOME/libpanel"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libpanel-1.10.4-1-any.pkg.tar.zst"
  sha256 "3d8291675bcaea8381e6504e20e3651fec231b4b3bbf7559bf568e6a6c4ef898"
  license "LGPL-3.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.gnome.org/sources/libpanel/1.10/libpanel-1.10.4.tar.xz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libpanel-1/libpanel.h"
    assert_path_exists lib/"libpanel-1.dll.a"
    assert_path_exists bin/"libpanel-1-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libpanel-1.pc").read
  end
end
