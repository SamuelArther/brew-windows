class Librevenge < Formula
  desc "Base library for writing document import filters"
  homepage "https://sourceforge.net/p/libwpd/wiki/librevenge/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-librevenge-0.0.6-1-any.pkg.tar.zst"
  sha256 "c614473fdacab65f941ed23ea29c5f13582dfc2355f07d3cf6079258589b192f"
  license any_of: ["LGPL-2.1-or-later", "MPL-2.0"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://sourceforge.net/projects/libwpd/rss?path=/librevenge"
    regex(%r{url=.*?/librevenge[._-]v?(\d+(?:\.\d+)+)\.t}i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"librevenge-0.0/librevenge/RVNGString.h"
    assert_path_exists lib/"librevenge-0.0.dll.a"
    assert_path_exists bin/"librevenge-0.0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/librevenge-0.0.pc").read
  end
end
