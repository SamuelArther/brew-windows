class Lrdf < Formula
  desc "RDF library for accessing plugin metadata in the LADSPA plugin system"
  homepage "https://github.com/swh/LRDF"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-lrdf-0.6.1-2-any.pkg.tar.zst"
  sha256 "301b49d150043ce1fd647cdd99de36f16e82d822d719f87c454087dd975297fd"
  license "GPL-2.0-only"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/swh/LRDF/archive/refs/tags/v0.6.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"lrdf.h"
    assert_path_exists lib/"liblrdf.dll.a"
    assert_path_exists bin/"liblrdf-2.dll"
  end
end
