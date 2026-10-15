class Rhash < Formula
  desc "Utility for computing and verifying hash sums of files"
  homepage "https://sourceforge.net/projects/rhash/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-rhash-1.4.6-1-any.pkg.tar.zst"
  sha256 "56f4a05b85f066795f2c8d6a4d822bf4b25a8dbf9f4c5b1bb2f7e10f812ad73e"
  license "0BSD"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://downloads.sourceforge.net/project/rhash/rhash/1.4.6/rhash-1.4.6-src.tar.gz"
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

  def install
    msys2_install
    msys2_bin "ucrt64/bin/ed2k-link.exe", "ed2k-link"
    msys2_bin "ucrt64/bin/edonr256-hash.exe", "edonr256-hash"
    msys2_bin "ucrt64/bin/edonr512-hash.exe", "edonr512-hash"
    msys2_bin "ucrt64/bin/gost12-256-hash.exe", "gost12-256-hash"
    msys2_bin "ucrt64/bin/gost12-512-hash.exe", "gost12-512-hash"
    msys2_bin "ucrt64/bin/has160-hash.exe", "has160-hash"
    msys2_bin "ucrt64/bin/magnet-link.exe", "magnet-link"
    msys2_bin "ucrt64/bin/rhash.exe", "rhash"
    msys2_bin "ucrt64/bin/sfv-hash.exe", "sfv-hash"
    msys2_bin "ucrt64/bin/tiger-hash.exe", "tiger-hash"
    msys2_bin "ucrt64/bin/tth-hash.exe", "tth-hash"
    msys2_bin "ucrt64/bin/whirlpool-hash.exe", "whirlpool-hash"
  end

  test do
    (testpath/"test").write("test")
    (testpath/"test.sha1").write("a94a8fe5ccb19ba61c4c0873d391e987982fbbd3 test")
    system bin/"rhash", "-c", "test.sha1"
  end
end
