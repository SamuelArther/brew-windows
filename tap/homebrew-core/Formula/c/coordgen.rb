class Coordgen < Formula
  desc "Schrodinger-developed 2D Coordinate Generation"
  homepage "https://github.com/schrodinger/coordgenlibs"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-coordgen-3.0.2-2-any.pkg.tar.zst"
  sha256 "2fb6ae81840df32875c2cd2b663ddae65329f9efa78557c56f7c8f2a489633c3"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/schrodinger/coordgenlibs/archive/refs/tags/v3.0.2.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"coordgen/CoordgenConfig.hpp"
    assert_path_exists lib/"libcoordgen.dll.a"
    assert_path_exists bin/"libcoordgen-3.dll"
  end
end
