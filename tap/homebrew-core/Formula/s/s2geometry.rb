class S2geometry < Formula
  desc "Computational geometry and spatial indexing on the sphere"
  homepage "https://github.com/google/s2geometry"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-s2geometry-0.14.0-1-any.pkg.tar.zst"
  sha256 "1882f1ae49edd895df6e68f509e18786a909751b8961f66af2f142abd489d7db"
  license "Apache-2.0"
  revision 2

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url :homepage
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"s2/r2.h"
    assert_path_exists lib/"libs2.dll.a"
    assert_path_exists bin/"libs2.dll"
  end
end
