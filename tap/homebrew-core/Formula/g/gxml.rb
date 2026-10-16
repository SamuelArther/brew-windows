class Gxml < Formula
  desc "GObject-based XML DOM API"
  homepage "https://wiki.gnome.org/GXml"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gxml-0.20.4-5-any.pkg.tar.zst"
  sha256 "360a54242ca6f9f1873838d45bd570bd9eb3608086c5a341e43cd544f75a9ed0"
  license "LGPL-2.1-or-later"
  revision 2

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://gitlab.gnome.org/GNOME/gxml/-/archive/0.20.4/gxml-0.20.4.tar.bz2"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"gxml-0.20/gxml/gxml.h"
    assert_path_exists lib/"libgxml-0.20.dll.a"
    assert_path_exists bin/"libgxml-0.20-2.0.2.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/gxml-0.20.pc").read
  end
end
