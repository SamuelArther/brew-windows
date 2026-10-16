class Gnucobol < Formula
  desc "COBOL85-202x compiler supporting lots of dialect specific extensions"
  homepage "https://gnucobol.sourceforge.io/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gnucobol-1~3.2-10-any.pkg.tar.zst"
  version "3.2"
  sha256 "03f3a543cca8447e9da6022a066d6dba5b600c7d1dce04c5b82eb8ebc17f8a26"
  license "GPL-3.0-or-later"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://ftpmirror.gnu.org/gnucobol/gnucobol-3.2.tar.xz"
    regex(/href=.*?gnucobol[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  resource "mingw-w64-ucrt-x86_64-binutils" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-binutils-2.47-4-any.pkg.tar.zst"
    sha256 "c18a40b871bfddb5da030f015003479ca85625afde1cb89f7313945e6151397d"
  end

  resource "mingw-w64-ucrt-x86_64-bzip2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-bzip2-1.0.8-4-any.pkg.tar.zst"
    sha256 "f03a2174034ddd2d96cecd34f617c5f8e2ef86c812b8d2bb3b8875257f2c8bfa"
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-crt" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-crt-14.0.0.r426.g4564ee4b5-1-any.pkg.tar.zst"
    sha256 "5d1c1f2c3859e690181d9ebcada1640a6744ab8dd5643b4bdbc7fe258af95037"
  end

  resource "mingw-w64-ucrt-x86_64-db" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-db-6.2.32-1-any.pkg.tar.zst"
    sha256 "cccbf189bcbe893f340d624ada7ee0c6267729a48aae0f840eb3e3c0f02ef787"
  end

  resource "mingw-w64-ucrt-x86_64-gcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "0290e91f5a9a8a3699bcea6221a899403bb9bd5d6f7dbd98c883d74e2ef36338"
  end

  resource "mingw-w64-ucrt-x86_64-gettext-runtime" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gettext-runtime-1.0-1-any.pkg.tar.zst"
    sha256 "ba693dda4ac375af76ce481ff3a6e7481286546cc7dc6d56c7021dae34084157"
  end

  resource "mingw-w64-ucrt-x86_64-gmp" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gmp-6.3.0-2-any.pkg.tar.zst"
    sha256 "e82a75968a556484a50084578238a84eb60fb93e34986fd6695c537975bd39ea"
  end

  resource "mingw-w64-ucrt-x86_64-headers" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-headers-14.0.0.r426.g4564ee4b5-1-any.pkg.tar.zst"
    sha256 "b81c19e278ba0fc9f6ad0326d4aef64aea93053a6272916fb4c07069ac0215d0"
  end

  resource "mingw-w64-ucrt-x86_64-isl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-isl-0.28-1-any.pkg.tar.zst"
    sha256 "8594e01a253d5a72646c586cf5a1626c1b1c2be7c30060fe67abd9bebe3fe223"
  end

  resource "mingw-w64-ucrt-x86_64-json-c" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-json-c-0.19-2-any.pkg.tar.zst"
    sha256 "e209dfd2e0955a9ab22bf394b13d477b2d24ae2660327e3981651efd464be0d5"
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

  resource "mingw-w64-ucrt-x86_64-libsystre" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libsystre-1.0.2-3-any.pkg.tar.zst"
    sha256 "62f564b1395d4862db1599b77f6588c32de5c80936845751cf369b31eee1560b"
  end

  resource "mingw-w64-ucrt-x86_64-libtre" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libtre-0.9.0-2-any.pkg.tar.zst"
    sha256 "d50f22545c50d7c0cd584474bba83f0c7a2cd8aab1aed1ad6c1f9c6452f3c4fc"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  resource "mingw-w64-ucrt-x86_64-libxml2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libxml2-2.15.4-1-any.pkg.tar.zst"
    sha256 "ea6b8e7ee24d8674e719f1f0a1b3747a72158ab786dfc5392ccb9f8edd09582a"
  end

  resource "mingw-w64-ucrt-x86_64-mpc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-mpc-1.4.1-1-any.pkg.tar.zst"
    sha256 "f06556e811c711ce91609484e31d358e4f495811ba29813bf2284a3b236d82ce"
  end

  resource "mingw-w64-ucrt-x86_64-mpfr" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-mpfr-4.2.2-3-any.pkg.tar.zst"
    sha256 "6b70a275d2ec75c70aa50a236c218e6b7ee9a5ab4c6d52cb0b580267b7d200fe"
  end

  resource "mingw-w64-ucrt-x86_64-ncurses" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-ncurses-6.6-4-any.pkg.tar.zst"
    sha256 "88331fe78470ef2c5e0280652fee5e10b6dcaa35f8037725c359407f2702309c"
  end

  resource "mingw-w64-ucrt-x86_64-pcre2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-pcre2-10.49-1-any.pkg.tar.zst"
    sha256 "63172179db724ac71e2439a7981134c1bcab8049bdddfd349b32b8f76ed52d1e"
  end

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  resource "mingw-w64-ucrt-x86_64-windows-default-manifest" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-windows-default-manifest-20260815-1-any.pkg.tar.zst"
    sha256 "b39039f754a600cc0dec49df7d675a59a3cb2b63691d91b4ae60735ee7075eca"
  end

  resource "mingw-w64-ucrt-x86_64-wineditline" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-wineditline-2.208-1-any.pkg.tar.zst"
    sha256 "b07b34de1669e16cfd6743691914f5a66ad4013500bbf0e4c7a7644046ae7b1c"
  end

  resource "mingw-w64-ucrt-x86_64-winpthreads" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-winpthreads-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a29b3919ef25c1d1671b8899bd947903bbf8733e7ada4a71be5bf02553dceab3"
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
    msys2_bin "ucrt64/bin/cobc.exe", "cobc"
    msys2_bin "ucrt64/bin/cobcrun.exe", "cobcrun"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cobc --version 2>&1")
  end
end
