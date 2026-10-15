class Openblas64 < Formula
  desc "Optimized BLAS library"
  homepage "https://www.openblas.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openblas64-0.3.34-1-any.pkg.tar.zst"
  sha256 "4b47b52d84c75c4a9fa02e2130355e67cdbfcdcfc3741f8f6105c9b061859615"
  license all_of: ["BSD-3-Clause", "BSD-2-Clause-Views", "BSD-3-Clause-Open-MPI", "BSD-2-Clause"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/OpenMathLib/OpenBLAS/archive/refs/tags/v0.3.34.tar.gz"
    strategy :github_latest
  end

  keg_only "the headers conflict with `openblas`"

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"openblas64/cblas.h"
    assert_path_exists lib/"libopenblas_64.dll.a"
    assert_path_exists bin/"libopenblas_64.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/openblas64.pc").read
  end
end
