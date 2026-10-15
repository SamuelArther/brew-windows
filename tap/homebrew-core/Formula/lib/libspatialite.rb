class Libspatialite < Formula
  desc "Adds spatial SQL capabilities to SQLite"
  homepage "https://www.gaia-gis.it/fossil/libspatialite/index"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libspatialite-5.1.0-5-any.pkg.tar.zst"
  sha256 "594f7cd1d61bf87d21d819ec3cc412d6dfb954e04c279d3ac445c8cc866dd871"
  license any_of: ["MPL-1.1", "GPL-2.0-or-later", "LGPL-2.1-or-later"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://www.gaia-gis.it/gaia-sins/libspatialite-sources/"
    regex(/href=.*?libspatialite[._-]v?(\d+(?:\.\d+)+[a-z]?)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"spatialite.h"
    assert_path_exists lib/"libspatialite.dll.a"
    assert_path_exists bin/"libspatialite-5.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/spatialite.pc").read
  end
end
