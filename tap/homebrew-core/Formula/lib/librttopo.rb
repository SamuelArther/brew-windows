class Librttopo < Formula
  desc "RT Topology Library"
  homepage "https://git.osgeo.org/gitea/rttopo/librttopo"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-librttopo-1.1.0-2-any.pkg.tar.zst"
  sha256 "7471579f765f82efc97788148946c66639b14ad6fc583940ce22e27834903637"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url :head
    regex(/^(?:librttopo[._-])?v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"librttopo.h"
    assert_path_exists lib/"librttopo.dll.a"
    assert_path_exists bin/"librttopo-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/rttopo.pc").read
  end
end
