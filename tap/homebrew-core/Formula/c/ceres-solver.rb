class CeresSolver < Formula
  desc "C++ library for large-scale optimization"
  homepage "http://ceres-solver.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-ceres-solver-2.2.0-11-any.pkg.tar.zst"
  sha256 "4c2a81d2706012abe0d5a70d64a8df6812c1e31dc049bbe4daca142bafe5e2d7"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "http://ceres-solver.org/installation.html"
    regex(/href=.*?ceres-solver[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"ceres/jet.h"
    assert_path_exists lib/"libceres.dll.a"
    assert_path_exists bin/"libceres-4.dll"
  end
end
