class LibtorrentRasterbar < Formula
  desc "C++ bittorrent library with Python bindings"
  homepage "https://www.libtorrent.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libtorrent-rasterbar-2.1.2-1-any.pkg.tar.zst"
  sha256 "57bd3304b3178fd58a52ecb516eaa6e99087622401047ace2e37d2630efea233"
  license "BSD-3-Clause"
  revision 2

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/arvidn/libtorrent/releases/download/v2.1.2/libtorrent-rasterbar-2.1.2.tar.gz"
    regex(/^v?(\d+(?:[._]\d+)+)$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libtorrent/fwd.hpp"
    assert_path_exists lib/"libtorrent-rasterbar.dll.a"
    assert_path_exists bin/"libtorrent-rasterbar.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libtorrent-rasterbar.pc").read
  end
end
