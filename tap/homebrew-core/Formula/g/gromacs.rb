class Gromacs < Formula
  desc "Versatile package for molecular dynamics calculations"
  homepage "https://www.gromacs.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gromacs-2026.3-1-any.pkg.tar.zst"
  sha256 "c39b03d5deeac7bbaaf6b44a609d00dd499282ed9212b3a7d36747dccd67ac09"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://ftp.gromacs.org/pub/gromacs/"
    regex(/href=.*?gromacs[._-]v?(\d+(?:\.\d+)*)\.t/i)
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-fftw" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-fftw-3.3.11-2-any.pkg.tar.zst"
    sha256 "8bcffc286600ae011726e9356f19ea8afba0a622427c77e89b5a09ecce252d95"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

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

  resource "mingw-w64-ucrt-x86_64-libstdc++" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libstdc++-16.2.0-6-any.pkg.tar.zst"
    sha256 "33c5e58a85a538cf843a8672991cce591f0231d66da3b66a57629712d64be17f"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  resource "mingw-w64-ucrt-x86_64-muparser" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-muparser-2.3.5-2-any.pkg.tar.zst"
    sha256 "cae79ce67b3c03b4f3ff94c7b2d1d3313e5138b9ed588ea95a6ae567dc8db2ac"
  end

  resource "mingw-w64-ucrt-x86_64-openblas" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openblas-0.3.34-1-any.pkg.tar.zst"
    sha256 "d9e14c9440d9aa70b6d09cdf0b8117995d181ced9c3d233ad7b62a907982add5"
  end

  resource "mingw-w64-ucrt-x86_64-opencl-icd" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-opencl-icd-2026.05.29-1-any.pkg.tar.zst"
    sha256 "4e0dab8d5cc3da24edb716d78b013c1a36fc111b06f0f286a949218665d71a98"
  end

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/gmx.exe", "gmx"
  end

  test do
    system bin/"gmx", "help"
  end
end
