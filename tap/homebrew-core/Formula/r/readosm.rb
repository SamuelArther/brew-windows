class Readosm < Formula
  desc "Extract valid data from an Open Street Map input file"
  homepage "https://www.gaia-gis.it/fossil/readosm/index"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-readosm-1.1.0.a-4-any.pkg.tar.zst"
  sha256 "b968b86e885545a25faabe4e13c552abfc3bc8efd92fbe20f7ce012b559203ca"
  license any_of: ["MPL-1.1", "GPL-2.0-or-later", "LGPL-2.1-or-later"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url :homepage
    regex(/href=.*?readosm[._-]v?(\d+(?:\.\d+)+[a-z]?)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"readosm.h"
    assert_path_exists lib/"libreadosm.dll.a"
    assert_path_exists bin/"libreadosm-1.dll"
  end
end
