class Gtkdatabox < Formula
  desc "Widget for live display of large amounts of changing data"
  homepage "https://sourceforge.net/projects/gtkdatabox/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gtkdatabox-1.0.0-2-any.pkg.tar.zst"
  sha256 "f9c6d2f50f3b72d89412dabce5af75b5c66f3510e2d53b6521e50202bcd84acc"
  license "LGPL-2.1-or-later"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://downloads.sourceforge.net/project/gtkdatabox/gtkdatabox-1/gtkdatabox-1.0.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"gtkdatabox.h"
    assert_path_exists lib/"libgtkdatabox.dll.a"
    assert_path_exists bin/"libgtkdatabox-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/gtkdatabox.pc").read
  end
end
