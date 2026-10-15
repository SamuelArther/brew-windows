class Binutils < Formula
  desc "GNU binary tools for native development"
  homepage "https://www.gnu.org/software/binutils/binutils.html"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-binutils-2.47-4-any.pkg.tar.zst"
  sha256 "c18a40b871bfddb5da030f015003479ca85625afde1cb89f7313945e6151397d"
  license all_of: ["GPL-2.0-or-later", "GPL-3.0-or-later", "LGPL-2.0-or-later", "LGPL-3.0-only"]

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://ftpmirror.gnu.org/binutils/binutils-2.47.tar.bz2"
  end

  conflicts_with "gcc", because: "both install the same binaries"

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

  resource "mingw-w64-ucrt-x86_64-zlib" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zlib-1.3.2-2-any.pkg.tar.zst"
    sha256 "841401182976d2f9e17e5c0ebaac51f2a8014140ea53d67625e91c8fb3c85ea0"
  end

  resource "mingw-w64-ucrt-x86_64-zstd" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zstd-1.5.7-2-any.pkg.tar.zst"
    sha256 "dbdb8427280046a2b41697780aa4c52983b708082b0da4755951dc3bea96ca89"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/addr2line.exe", "addr2line"
    msys2_bin "ucrt64/bin/ar.exe", "ar"
    msys2_bin "ucrt64/bin/as.exe", "as"
    msys2_bin "ucrt64/bin/c++filt.exe", "c++filt"
    msys2_bin "ucrt64/bin/dlltool.exe", "dlltool"
    msys2_bin "ucrt64/bin/dllwrap.exe", "dllwrap"
    msys2_bin "ucrt64/bin/elfedit.exe", "elfedit"
    msys2_bin "ucrt64/bin/gprof.exe", "gprof"
    msys2_bin "ucrt64/bin/ld.bfd.exe", "ld.bfd"
    msys2_bin "ucrt64/bin/ld.exe", "ld"
    msys2_bin "ucrt64/bin/nm.exe", "nm"
    msys2_bin "ucrt64/bin/objcopy.exe", "objcopy"
    msys2_bin "ucrt64/bin/objdump.exe", "objdump"
    msys2_bin "ucrt64/bin/ranlib.exe", "ranlib"
    msys2_bin "ucrt64/bin/readelf.exe", "readelf"
    msys2_bin "ucrt64/bin/size.exe", "size"
    msys2_bin "ucrt64/bin/strings.exe", "strings"
    msys2_bin "ucrt64/bin/strip.exe", "strip"
    msys2_bin "ucrt64/bin/windmc.exe", "windmc"
    msys2_bin "ucrt64/bin/windres.exe", "windres"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/addr2line --version 2>&1")
  end
end
