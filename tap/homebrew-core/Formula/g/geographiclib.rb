class Geographiclib < Formula
  desc "C++ geography library"
  homepage "https://geographiclib.sourceforge.io/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-geographiclib-2.7-1-any.pkg.tar.zst"
  sha256 "9528e3d0ae394dce757f50b62807ed0358a16acbc457bed47a690be4a2b7ff31"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/geographiclib/geographiclib/archive/refs/tags/r2.7.tar.gz"
    regex(/^r(\d+(?:\.\d+)+)$/i)
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
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
    msys2_bin "ucrt64/bin/Cart3Convert.exe", "Cart3Convert"
    msys2_bin "ucrt64/bin/CartConvert.exe", "CartConvert"
    msys2_bin "ucrt64/bin/Conformal3Proj.exe", "Conformal3Proj"
    msys2_bin "ucrt64/bin/ConicProj.exe", "ConicProj"
    msys2_bin "ucrt64/bin/GeoConvert.exe", "GeoConvert"
    msys2_bin "ucrt64/bin/Geod3Solve.exe", "Geod3Solve"
    msys2_bin "ucrt64/bin/GeodSolve.exe", "GeodSolve"
    msys2_bin "ucrt64/bin/GeodesicProj.exe", "GeodesicProj"
    msys2_bin "ucrt64/bin/GeoidEval.exe", "GeoidEval"
    msys2_bin "ucrt64/bin/Gravity.exe", "Gravity"
    msys2_bin "ucrt64/bin/IntersectTool.exe", "IntersectTool"
    msys2_bin "ucrt64/bin/MagneticField.exe", "MagneticField"
    msys2_bin "ucrt64/bin/Planimeter.exe", "Planimeter"
    msys2_bin "ucrt64/bin/RhumbSolve.exe", "RhumbSolve"
    msys2_bin "ucrt64/bin/TransverseMercatorProj.exe", "TransverseMercatorProj"
  end

  test do
    system bin/"GeoConvert", "-p", "-3", "-m", "--input-string", "33.3 44.4"
  end
end
