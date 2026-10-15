class OsmGpsMap < Formula
  desc "GTK+ library to embed OpenStreetMap maps"
  homepage "https://github.com/nzjrs/osm-gps-map"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-osm-gps-map-1.2.2-1-any.pkg.tar.zst"
  sha256 "cbd49a2ec1365547f8a3b85de72f5b91d0434023acb31260f789fbbffe2139cf"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/nzjrs/osm-gps-map/releases/download/1.2.2/osm-gps-map-1.2.2.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"osmgpsmap-1.0/osm-gps-map.h"
    assert_path_exists lib/"libosmgpsmap-1.0.dll.a"
    assert_path_exists bin/"libosmgpsmap-1.0-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/osmgpsmap-1.0.pc").read
  end
end
