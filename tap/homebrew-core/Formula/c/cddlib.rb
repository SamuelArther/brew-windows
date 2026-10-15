class Cddlib < Formula
  desc "Double description method for general polyhedral cones"
  homepage "https://www.inf.ethz.ch/personal/fukudak/cdd_home/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cddlib-0.94n-3-any.pkg.tar.zst"
  sha256 "332f3ba7f41df9bd76143830d4370282015c833fddd2ca465b5c61f06571e78c"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/cddlib/cddlib/releases/download/0.94n/cddlib-0.94n.tar.gz"
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-gmp" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gmp-6.3.0-2-any.pkg.tar.zst"
    sha256 "e82a75968a556484a50084578238a84eb60fb93e34986fd6695c537975bd39ea"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libstdc++" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libstdc++-16.2.0-6-any.pkg.tar.zst"
    sha256 "33c5e58a85a538cf843a8672991cce591f0231d66da3b66a57629712d64be17f"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/adjacency.exe", "adjacency"
    msys2_bin "ucrt64/bin/adjacency_gmp.exe", "adjacency_gmp"
    msys2_bin "ucrt64/bin/allfaces.exe", "allfaces"
    msys2_bin "ucrt64/bin/allfaces_gmp.exe", "allfaces_gmp"
    msys2_bin "ucrt64/bin/cddexec.exe", "cddexec"
    msys2_bin "ucrt64/bin/cddexec_gmp.exe", "cddexec_gmp"
    msys2_bin "ucrt64/bin/fourier.exe", "fourier"
    msys2_bin "ucrt64/bin/fourier_gmp.exe", "fourier_gmp"
    msys2_bin "ucrt64/bin/lcdd.exe", "lcdd"
    msys2_bin "ucrt64/bin/lcdd_gmp.exe", "lcdd_gmp"
    msys2_bin "ucrt64/bin/projection.exe", "projection"
    msys2_bin "ucrt64/bin/projection_gmp.exe", "projection_gmp"
    msys2_bin "ucrt64/bin/redcheck.exe", "redcheck"
    msys2_bin "ucrt64/bin/redcheck_gmp.exe", "redcheck_gmp"
    msys2_bin "ucrt64/bin/scdd.exe", "scdd"
    msys2_bin "ucrt64/bin/scdd_gmp.exe", "scdd_gmp"
    msys2_bin "ucrt64/bin/testcdd1.exe", "testcdd1"
    msys2_bin "ucrt64/bin/testcdd1_gmp.exe", "testcdd1_gmp"
    msys2_bin "ucrt64/bin/testcdd2.exe", "testcdd2"
    msys2_bin "ucrt64/bin/testcdd2_gmp.exe", "testcdd2_gmp"
    msys2_bin "ucrt64/bin/testlp1.exe", "testlp1"
    msys2_bin "ucrt64/bin/testlp1_gmp.exe", "testlp1_gmp"
    msys2_bin "ucrt64/bin/testlp2.exe", "testlp2"
    msys2_bin "ucrt64/bin/testlp2_gmp.exe", "testlp2_gmp"
    msys2_bin "ucrt64/bin/testlp3.exe", "testlp3"
    msys2_bin "ucrt64/bin/testlp3_gmp.exe", "testlp3_gmp"
    msys2_bin "ucrt64/bin/testshoot.exe", "testshoot"
    msys2_bin "ucrt64/bin/testshoot_gmp.exe", "testshoot_gmp"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/adjacency --version 2>&1")
  end
end
