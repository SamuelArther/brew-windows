class Gexiv2 < Formula
  desc "GObject wrapper around the Exiv2 photo metadata library"
  homepage "https://wiki.gnome.org/Projects/gexiv2"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gexiv2-0.14.7-1-any.pkg.tar.zst"
  sha256 "2abd0bb3d11b63120d2f443c5481d8b23a447a2ae43b2b559dcf7266dcbdc445"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.gnome.org/sources/gexiv2/0.16/gexiv2-0.16.2.tar.xz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"gexiv2/gexiv2.h"
    assert_path_exists lib/"libgexiv2.dll.a"
    assert_path_exists bin/"libgexiv2-2.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/gexiv2.pc").read
  end
end
