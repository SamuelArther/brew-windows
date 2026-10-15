class Imath < Formula
  desc "Library of 2D and 3D vector, matrix, and math operations"
  homepage "https://imath.readthedocs.io/en/latest/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-imath-3.2.3-3-any.pkg.tar.zst"
  sha256 "7a2e2175d5bd126c277e1eb0a059004f8750918e7dbbd447554c7b46dca34d60"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/AcademySoftwareFoundation/Imath/archive/refs/tags/v3.2.3.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"Imath/half.h"
    assert_path_exists lib/"libImath.dll.a"
    assert_path_exists bin/"libImath-3_2.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/Imath.pc").read
  end
end
