class Sundials < Formula
  desc "Nonlinear and differential/algebraic equations solver"
  homepage "https://computing.llnl.gov/projects/sundials"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-sundials-7.5.0-2-any.pkg.tar.zst"
  sha256 "acc9d62bce339e49a552c159a069574e30a1a63bfc0bb29c21fd6ba4d7bee626"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/llnl/sundials/releases/download/v7.9.0/sundials-7.9.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"ida/ida.h"
    assert_path_exists lib/"libsundials_arkode.dll.a"
    assert_path_exists bin/"libsundials_arkode-6.dll"
  end
end
