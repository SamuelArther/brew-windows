class Qrupdate < Formula
  desc "Fast updates of QR and Cholesky decompositions"
  homepage "https://gitlab.mpi-magdeburg.mpg.de/koehlerm/qrupdate-ng"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-qrupdate-1.3.1-1-any.pkg.tar.zst"
  sha256 "51a90495d615aea12faa35608caa71039639ec73c49181b2c3ffc1a4a92ee1d1"
  license "GPL-3.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://gitlab.mpi-magdeburg.mpg.de/koehlerm/qrupdate-ng/-/archive/v1.3.1/qrupdate-ng-v1.3.1.tar.bz2"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"qrupdate/qrupdate.h"
    assert_path_exists lib/"libqrupdate.dll.a"
    assert_path_exists bin/"libqrupdate.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/qrupdate-static.pc").read
  end
end
