class Gtkmm3 < Formula
  desc "C++ interfaces for GTK+ and GNOME"
  homepage "https://www.gtkmm.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gtkmm3-3.24.10-3-any.pkg.tar.zst"
  sha256 "4ce4331839aa9b0b0b6f2fe771d9cf17c8a527cd37148eefb2bc1d53964d4b6e"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.gnome.org/sources/gtkmm/3.24/gtkmm-3.24.11.tar.xz"
    regex(/gtkmm[._-]v?(3\.([0-8]\d*?)?[02468](?:\.\d+)*?)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"gdkmm-3.0/gdkmm.h"
    assert_path_exists lib/"libgdkmm-3.0.dll.a"
    assert_path_exists bin/"libgdkmm-3.0-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/gdkmm-3.0.pc").read
  end
end
