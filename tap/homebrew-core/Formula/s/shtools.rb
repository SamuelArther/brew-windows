class Shtools < Formula
  desc "Spherical Harmonic Tools"
  homepage "https://shtools.github.io/SHTOOLS/"
  url "https://github.com/SHTOOLS/SHTOOLS/archive/refs/tags/v4.14.1.tar.gz"
  sha256 "df2085e1408b078009a798db0c7eb13b56968853d8b86e1a963695fcb985bef7"
  license "BSD-3-Clause"
  head "https://github.com/SHTOOLS/SHTOOLS.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "da4bbf888de2efe7e11abef472f86bd02a4f3eb6e71ae850eb58c1e36b12f9a1"
  end

  depends_on "fftw"
  depends_on "gcc"
  depends_on "openblas"

  on_linux do
    depends_on "libtool" => :build
  end

  def install
    system "make", "fortran"
    system "make", "fortran-mp"
    system "make", "install", "PREFIX=#{prefix}"
  end

  test do
    cp_r "#{share}/examples/shtools", testpath
    m64 = "-m64" if Hardware::CPU.intel?
    system "make", "-C", "shtools/fortran",
                   "run-fortran-tests-no-timing",
                   "F95=gfortran",
                   "F95FLAGS=#{m64} -fPIC -O3 -std=gnu -ffast-math",
                   "MODFLAG=-I#{HOMEBREW_PREFIX}/include",
                   "LIBPATH=#{HOMEBREW_PREFIX}/lib",
                   "LIBNAME=SHTOOLS",
                   "FFTW=-L #{HOMEBREW_PREFIX}/lib -lfftw3 -lm",
                   "LAPACK=-L #{formula_opt_lib("openblas")} -lopenblas",
                   "BLAS="
  end
end
