class Scotch < Formula
  desc "Package for graph partitioning, graph clustering, and sparse matrix ordering"
  homepage "https://gitlab.inria.fr/scotch/scotch"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-scotch-7.0.12-3-any.pkg.tar.zst"
  sha256 "750aa00ae981f45dd16a648f28a15b2d330ee1a50d6b1653a7986dd5ae5b6142"
  license "CECILL-C"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://gitlab.inria.fr/scotch/scotch/-/archive/v7.0.17/scotch-v7.0.17.tar.bz2"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  resource "mingw-w64-ucrt-x86_64-bzip2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-bzip2-1.0.8-4-any.pkg.tar.zst"
    sha256 "f03a2174034ddd2d96cecd34f617c5f8e2ef86c812b8d2bb3b8875257f2c8bfa"
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-gettext-runtime" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gettext-runtime-1.0-1-any.pkg.tar.zst"
    sha256 "ba693dda4ac375af76ce481ff3a6e7481286546cc7dc6d56c7021dae34084157"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libiconv" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libiconv-1.19-1-any.pkg.tar.zst"
    sha256 "9a500f38c2b91808741c62fae746b3e9110b33a1ecf5c30fa0c66dbedddf7e16"
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

  resource "mingw-w64-ucrt-x86_64-xz" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-xz-5.8.4-1-any.pkg.tar.zst"
    sha256 "a9d771f816d8c0c5bde152ffb27a687ede91079b8e77e5b8297efa3b5a835934"
  end

  resource "mingw-w64-ucrt-x86_64-zlib" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zlib-1.3.2-2-any.pkg.tar.zst"
    sha256 "841401182976d2f9e17e5c0ebaac51f2a8014140ea53d67625e91c8fb3c85ea0"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/acpl.exe", "acpl"
    msys2_bin "ucrt64/bin/amk_ccc.exe", "amk_ccc"
    msys2_bin "ucrt64/bin/amk_fft2.exe", "amk_fft2"
    msys2_bin "ucrt64/bin/amk_grf.exe", "amk_grf"
    msys2_bin "ucrt64/bin/amk_hy.exe", "amk_hy"
    msys2_bin "ucrt64/bin/amk_m2.exe", "amk_m2"
    msys2_bin "ucrt64/bin/amk_p2.exe", "amk_p2"
    msys2_bin "ucrt64/bin/atst.exe", "atst"
    msys2_bin "ucrt64/bin/dggath.exe", "dggath"
    msys2_bin "ucrt64/bin/dgmap.exe", "dgmap"
    msys2_bin "ucrt64/bin/dgord.exe", "dgord"
    msys2_bin "ucrt64/bin/dgpart.exe", "dgpart"
    msys2_bin "ucrt64/bin/dgscat.exe", "dgscat"
    msys2_bin "ucrt64/bin/dgtst.exe", "dgtst"
    msys2_bin "ucrt64/bin/gbase.exe", "gbase"
    msys2_bin "ucrt64/bin/gcv.exe", "gcv"
    msys2_bin "ucrt64/bin/gdump.exe", "gdump"
    msys2_bin "ucrt64/bin/gmap.exe", "gmap"
    msys2_bin "ucrt64/bin/gmk_hy.exe", "gmk_hy"
    msys2_bin "ucrt64/bin/gmk_m2.exe", "gmk_m2"
    msys2_bin "ucrt64/bin/gmk_m3.exe", "gmk_m3"
    msys2_bin "ucrt64/bin/gmk_msh.exe", "gmk_msh"
    msys2_bin "ucrt64/bin/gmk_ub2.exe", "gmk_ub2"
    msys2_bin "ucrt64/bin/gmtst.exe", "gmtst"
    msys2_bin "ucrt64/bin/gord.exe", "gord"
    msys2_bin "ucrt64/bin/gotst.exe", "gotst"
    msys2_bin "ucrt64/bin/gout.exe", "gout"
    msys2_bin "ucrt64/bin/gpart.exe", "gpart"
    msys2_bin "ucrt64/bin/gscat.exe", "gscat"
    msys2_bin "ucrt64/bin/gtst.exe", "gtst"
    msys2_bin "ucrt64/bin/mcv.exe", "mcv"
    msys2_bin "ucrt64/bin/mmk_m2.exe", "mmk_m2"
    msys2_bin "ucrt64/bin/mmk_m3.exe", "mmk_m3"
    msys2_bin "ucrt64/bin/mord.exe", "mord"
    msys2_bin "ucrt64/bin/mtst.exe", "mtst"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/acpl -V 2>&1")
  end
end
