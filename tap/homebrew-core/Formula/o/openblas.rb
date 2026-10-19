class Openblas < Formula
  desc "Optimized BLAS library"
  homepage "https://www.openblas.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openblas-0.3.34-1-any.pkg.tar.zst"
  sha256 "d9e14c9440d9aa70b6d09cdf0b8117995d181ced9c3d233ad7b62a907982add5"
  license all_of: ["BSD-3-Clause", "BSD-2-Clause-Views", "BSD-3-Clause-Open-MPI", "BSD-2-Clause"]
  revision 1

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/OpenMathLib/OpenBLAS/archive/refs/tags/v0.3.34.tar.gz"
    strategy :github_latest
  end

  # libopenblas.dll's runtime packages (fc-libs, omp): numpy's _multiarray_umath failed to load without
  # libgfortran-5.dll and libgomp-1.dll, which MSYS2's own toolchain doesn't install.
  resource "mingw-w64-ucrt-x86_64-libgfortran" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgfortran-16.2.0-6-any.pkg.tar.zst"
    sha256 "34406a0c721e44655c3971ce131efd81652d5f6fa5381eb5700b288bc9a780ab"
  end

  resource "mingw-w64-ucrt-x86_64-libgomp" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgomp-16.2.0-6-any.pkg.tar.zst"
    sha256 "809b95086d8d3ba145e041bb36d5756b4407d1d97c0524ef731d3b94548faa69"
  end

  resource "mingw-w64-ucrt-x86_64-libquadmath" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libquadmath-16.2.0-6-any.pkg.tar.zst"
    sha256 "7b553c0b6dd9cf2f676c0c6812aa359495940dbdd7347f47be1dbd75d29cd611"
  end

  def install
    install_tree
    resources.each { |r| install_resource r.name, "libexec" }
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"openblas/cblas.h"
    assert_path_exists lib/"libopenblas.dll.a"
    assert_path_exists bin/"libopenblas.dll"
    assert_path_exists bin/"libgfortran-5.dll"
    assert_path_exists bin/"libgomp-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/openblas.pc").read
  end
end
