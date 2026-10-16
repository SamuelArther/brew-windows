class Veclibfort < Formula
  desc "GNU Fortran compatible BLAS and LAPACK for Windows (OpenBLAS as libvecLibFort)"
  homepage "https://www.openblas.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openblas-0.3.34-1-any.pkg.tar.zst"
  sha256 "d9e14c9440d9aa70b6d09cdf0b8117995d181ced9c3d233ad7b62a907982add5"
  license all_of: ["BSD-3-Clause", "BSD-2-Clause"]

  # vecLibFort wraps Apple's vecLib (Accelerate's BLAS/LAPACK) so gfortran-compiled code can call
  # it: vecLib uses the f2c calling convention for functions returning REAL or COMPLEX, gfortran
  # does not. Windows has no system BLAS; the optimized BLAS/LAPACK that gfortran code links on
  # Windows is OpenBLAS, built with gfortran's own convention, so no wrapper is needed. This keg
  # carries MSYS2's UCRT64 OpenBLAS with its gfortran runtime and also installs it as
  # libvecLibFort, so `-lvecLibFort` keeps working for code written against vecLibFort.

  keg_only "it conflicts with openblas"

  resource "mingw-w64-ucrt-x86_64-libgfortran" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgfortran-16.2.0-6-any.pkg.tar.zst"
    sha256 "34406a0c721e44655c3971ce131efd81652d5f6fa5381eb5700b288bc9a780ab"
  end

  resource "mingw-w64-ucrt-x86_64-libquadmath" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libquadmath-16.2.0-6-any.pkg.tar.zst"
    sha256 "7b553c0b6dd9cf2f676c0c6812aa359495940dbdd7347f47be1dbd75d29cd611"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  def install
    install_tree
    install_resource "mingw-w64-ucrt-x86_64-libgfortran", "libexec"
    install_resource "mingw-w64-ucrt-x86_64-libquadmath", "libexec"
    install_resource "mingw-w64-ucrt-x86_64-libgcc", "libexec"
    install_resource "mingw-w64-ucrt-x86_64-libwinpthread", "libexec"
    install_library
    # vecLibFort's library name: the import library still loads libopenblas.dll.
    cp lib/"libopenblas.dll.a", lib/"libvecLibFort.dll.a"
    cp lib/"libopenblas.a", lib/"libvecLibFort.a" if (lib/"libopenblas.a").exist?
  end

  test do
    # No Fortran compiler on Windows to build homebrew-core's tester.f90; check the library instead.
    assert_path_exists lib/"libvecLibFort.dll.a"
    assert_path_exists bin/"libopenblas.dll"
    assert_path_exists bin/"libgfortran-5.dll"
    assert_match "cblas_sdot", (include/"openblas/cblas.h").read
  end
end
