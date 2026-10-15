class Libical < Formula
  desc "Implementation of iCalendar protocols and data formats"
  homepage "https://libical.github.io/libical/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libical-3.0.20-8-any.pkg.tar.zst"
  sha256 "d8a1da8612d221514fd150fd2277b44480836ebed23ac544dabff34fb3b3f2da"
  license any_of: ["LGPL-2.1-or-later", "MPL-2.0"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/libical/libical/releases/download/v4.0.6/libical-4.0.6.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libical/pvl.h"
    assert_path_exists lib/"libical-glib.dll.a"
    assert_path_exists bin/"libical-glib.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libical-glib.pc").read
  end
end
