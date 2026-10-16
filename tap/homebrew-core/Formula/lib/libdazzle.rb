class Libdazzle < Formula
  desc "GNOME companion library to GObject and Gtk+"
  homepage "https://gitlab.gnome.org/GNOME/libdazzle"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libdazzle-3.44.0-3-any.pkg.tar.zst"
  sha256 "88ee76237a44d53fc42344422c3acdec6cf811fdcb35e5cc20a4c590d4a140f0"
  license "GPL-3.0-or-later"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.gnome.org/sources/libdazzle/3.44/libdazzle-3.44.0.tar.xz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libdazzle-1.0/dazzle.h"
    assert_path_exists lib/"libdazzle-1.0.dll.a"
    assert_path_exists bin/"libdazzle-1.0-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libdazzle-1.0.pc").read
  end
end
