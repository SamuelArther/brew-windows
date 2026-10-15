class Librest < Formula
  desc "Library to access RESTful web services"
  homepage "https://gitlab.gnome.org/GNOME/librest"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-librest-0.9.1-4-any.pkg.tar.zst"
  sha256 "48d5c330687d938196001c8c380aa626761cf229e5994f3a2612936d665451f9"
  license all_of: ["LGPL-2.1-or-later", "LGPL-3.0-or-later"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.gnome.org/sources/librest/0.10/librest-0.10.2.tar.xz"
    regex(/librest[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"rest-1.0/rest/rest.h"
    assert_path_exists lib/"librest-1.0.dll.a"
    assert_path_exists bin/"librest-1.0-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/rest-1.0.pc").read
  end
end
