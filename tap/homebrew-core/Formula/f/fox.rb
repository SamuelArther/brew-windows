class Fox < Formula
  desc "Toolkit for developing Graphical User Interfaces easily"
  homepage "http://fox-toolkit.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-fox-1.6.59-2-any.pkg.tar.zst"
  sha256 "c0134c07d9d5abae139c9df1634e9689e81790723672d7e92b6853dba469bf3e"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "http://fox-toolkit.org/download.html"
    regex(%r{href=.*?fox[._-]v?(\d+(?:\.\d+)+)\.t[^"' >]+?["']?[^>]*?>[^<]+?</[^>]+?>\s*\(STABLE\)}im)
    regex(/href=.*?fox[._-]v?(\d+\.\d*[02468](?:\.\d+)*)\.t/i)
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

  resource "mingw-w64-ucrt-x86_64-giflib" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-giflib-6.1.3-1-any.pkg.tar.zst"
    sha256 "9e7ac8feb0e2af6e0fce3c8ead1bd7bcaf018661d08033858c1e2657a04f6f1a"
  end

  resource "mingw-w64-ucrt-x86_64-jbigkit" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-jbigkit-2.1-6-any.pkg.tar.zst"
    sha256 "9e237d1041350e7436294cfa13d4bd3d1606d1f2a32ade2eb2958b4d4e79b7ad"
  end

  resource "mingw-w64-ucrt-x86_64-lerc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-lerc-4.1.1-1-any.pkg.tar.zst"
    sha256 "c0afbf4832488d67030c6f313349cd814946c9a7bc3ae9fd54524825c9ca055d"
  end

  resource "mingw-w64-ucrt-x86_64-libdeflate" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libdeflate-1.26-1-any.pkg.tar.zst"
    sha256 "17720c77bd01641a3554801cafd71d96fb7fff7427f4cc20ac9f36efb8cb3da0"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libiconv" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libiconv-1.19-1-any.pkg.tar.zst"
    sha256 "9a500f38c2b91808741c62fae746b3e9110b33a1ecf5c30fa0c66dbedddf7e16"
  end

  resource "mingw-w64-ucrt-x86_64-libjpeg-turbo" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libjpeg-turbo-3.2.0-1-any.pkg.tar.zst"
    sha256 "7c001d00df557cfee969ad43d37b9c31ae22869e8aac4091a89dc3ae6d95e11d"
  end

  resource "mingw-w64-ucrt-x86_64-libpng" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libpng-1.6.59-1-any.pkg.tar.zst"
    sha256 "7f8448e06eceba8f8ce130390addea49f767bdf8f6b7e7eede8ca6d595e3d3a8"
  end

  resource "mingw-w64-ucrt-x86_64-libstdc++" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libstdc++-16.2.0-6-any.pkg.tar.zst"
    sha256 "33c5e58a85a538cf843a8672991cce591f0231d66da3b66a57629712d64be17f"
  end

  resource "mingw-w64-ucrt-x86_64-libtiff" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libtiff-4.7.2-1-any.pkg.tar.zst"
    sha256 "76904b28133fc3cc0a0e518846c98d3d2166fac70ed9c6184dfe5ef145e5f279"
  end

  resource "mingw-w64-ucrt-x86_64-libwebp" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwebp-1.6.0-1-any.pkg.tar.zst"
    sha256 "34ff8396c403d484c8777ad49ad0fc06dfb94fadf6f14bcf84d50c45bc6fd17e"
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

  resource "mingw-w64-ucrt-x86_64-zstd" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zstd-1.5.7-2-any.pkg.tar.zst"
    sha256 "dbdb8427280046a2b41697780aa4c52983b708082b0da4755951dc3bea96ca89"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/PathFinder.exe", "PathFinder"
    msys2_bin "ucrt64/bin/adie.exe", "adie"
    msys2_bin "ucrt64/bin/calculator.exe", "calculator"
    msys2_bin "ucrt64/bin/reswrap.exe", "reswrap"
    msys2_bin "ucrt64/bin/shutterbug.exe", "shutterbug"
  end

  test do
    assert_path_exists bin/"PathFinder.exe"
  end
end
