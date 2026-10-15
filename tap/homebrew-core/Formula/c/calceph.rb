class Calceph < Formula
  desc "C library to access the binary planetary ephemeris files"
  homepage "https://calceph.imcce.fr"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-calceph-5.0.1-1-any.pkg.tar.zst"
  sha256 "180202593e6908ca1acd2020414f6d24179fbd938523fe16f2875fdc7a043ba3"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url :homepage
    regex(/href=.*?calceph[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libgfortran" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgfortran-16.2.0-6-any.pkg.tar.zst"
    sha256 "34406a0c721e44655c3971ce131efd81652d5f6fa5381eb5700b288bc9a780ab"
  end

  resource "mingw-w64-ucrt-x86_64-libquadmath" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libquadmath-16.2.0-6-any.pkg.tar.zst"
    sha256 "7b553c0b6dd9cf2f676c0c6812aa359495940dbdd7347f47be1dbd75d29cd611"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/calceph_inspector.exe", "calceph_inspector"
    msys2_bin "ucrt64/bin/calceph_queryorientation.exe", "calceph_queryorientation"
    msys2_bin "ucrt64/bin/calceph_queryposition.exe", "calceph_queryposition"
  end

  test do
    assert_path_exists bin/"calceph_inspector.exe"
  end
end
