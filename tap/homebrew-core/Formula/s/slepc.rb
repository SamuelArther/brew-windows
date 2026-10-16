class Slepc < Formula
  desc "Scalable Library for Eigenvalue Problem Computations (real)"
  homepage "https://slepc.upv.es"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-slepc-3.25.0-3-any.pkg.tar.zst"
  sha256 "1e78dcbcb9e31578cb949a34d4b6fdc2c7b18ee6d07eb80d14b8e22182eba089"
  license "BSD-2-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://slepc.upv.es/download/distrib/"
    regex(/href=.*?slepc[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"slepc/slepc.h"
    assert_path_exists bin/"libslepc-cmo.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/slepc-cmo.pc").read
  end
end
