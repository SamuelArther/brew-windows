class Taglib < Formula
  desc "Audio metadata library"
  homepage "https://taglib.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-taglib-2.2.1-1-any.pkg.tar.zst"
  sha256 "7a3bd5f6d08bebd0f3e2ec8893324e97bbb54b02b48ef476ee613c3e8f5cc0b2"
  license any_of: ["LGPL-2.1-only", "MPL-1.1"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://taglib.org/releases/taglib-2.3.2.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"taglib/tag.h"
    assert_path_exists lib/"libtag.dll.a"
    assert_path_exists bin/"libtag-2.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/taglib.pc").read
  end
end
