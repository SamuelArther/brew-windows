class Virtualpg < Formula
  desc "Loadable dynamic extension for SQLite and SpatiaLite"
  homepage "https://www.gaia-gis.it/fossil/virtualpg/index"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-virtualpg-2.0.1-2-any.pkg.tar.zst"
  sha256 "f0fa2762f46baf34cb6432bdbe42c7be5abbbf6d980915bd10f65710ef4f8c93"
  license any_of: ["MPL-1.1", "GPL-2.0-or-later", "LGPL-2.1-or-later"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url :homepage
    regex(/href=.*?virtualpg[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"virtualpg.h"
    assert_path_exists lib/"libvirtualpg.dll.a"
    assert_path_exists bin/"libvirtualpg-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/virtualpg.pc").read
  end
end
