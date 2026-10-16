class Arpack < Formula
  desc "Routines to solve large scale eigenvalue problems"
  homepage "https://github.com/opencollab/arpack-ng"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-arpack-3.9.1-9-any.pkg.tar.zst"
  sha256 "82ae1ae5696556faea65d36fb782c270d2e17db34eea8f33bc2c7f90ff0b0d03"
  license "BSD-3-Clause"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/opencollab/arpack-ng/archive/refs/tags/3.9.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"arpack/stat.h"
    assert_path_exists lib/"libarpack.dll.a"
    assert_path_exists bin/"libarpack.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/arpack.pc").read
  end
end
