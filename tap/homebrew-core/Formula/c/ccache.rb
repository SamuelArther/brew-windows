class Ccache < Formula
  desc "Object-file caching compiler wrapper"
  homepage "https://ccache.dev/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-ccache-4.14.1-1-any.pkg.tar.zst"
  sha256 "2827cdd622c3ad8c5f0868de0b1dcf589880491475f11bb378e039e3dc579025"
  license "GPL-3.0-or-later"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/ccache/ccache/releases/download/v4.14.1/ccache-4.14.1.tar.xz"
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-fmt" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-fmt-12.2.0-1-any.pkg.tar.zst"
    sha256 "7b368e76d231f44c968c6b2a0780ac122e48fac80b35aa9ec353a3bcb440b00f"
  end

  resource "mingw-w64-ucrt-x86_64-hiredis" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-hiredis-1.3.0-2-any.pkg.tar.zst"
    sha256 "e53e5320c4a375f1c7591fe70a698ca00118a09e0850b6ffb8e577fccb938da4"
  end

  resource "mingw-w64-ucrt-x86_64-hwloc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-hwloc-2.15.0-1-any.pkg.tar.zst"
    sha256 "ddd653c07042172cc0fdb462f18313d5369d2c23c5899a0a10d4f3d8ce2bd62c"
  end

  resource "mingw-w64-ucrt-x86_64-libblake3" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libblake3-1.8.7-1-any.pkg.tar.zst"
    sha256 "30a834b952e4437644addbf3e9eac028bfb719258a0095d1a8d427b707972d5b"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libltdl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libltdl-2.6.2-1-any.pkg.tar.zst"
    sha256 "d769ff3399df0c83010ebc16696f0265e3f078469a5087719b235f24c8724a9e"
  end

  resource "mingw-w64-ucrt-x86_64-libstdc++" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libstdc++-16.2.0-6-any.pkg.tar.zst"
    sha256 "33c5e58a85a538cf843a8672991cce591f0231d66da3b66a57629712d64be17f"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  resource "mingw-w64-ucrt-x86_64-openssl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openssl-3.6.5-1-any.pkg.tar.zst"
    sha256 "773e021ebad83f14b5c103db467411e8ff064663e1c7e7f730e2be72f4117064"
  end

  resource "mingw-w64-ucrt-x86_64-tbb" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tbb-2023.1.0-1-any.pkg.tar.zst"
    sha256 "1dcd85163ef30c1ce313d72a9f8df90188da2a1b74effa5bb291250d3f7b0f67"
  end

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  resource "mingw-w64-ucrt-x86_64-xxhash" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-xxhash-0.8.4-1-any.pkg.tar.zst"
    sha256 "a22d39e85edd6f0b4c7fe34733d28f17d49715f6c8dea67c142161c02bfbbde2"
  end

  resource "mingw-w64-ucrt-x86_64-zstd" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zstd-1.5.7-2-any.pkg.tar.zst"
    sha256 "dbdb8427280046a2b41697780aa4c52983b708082b0da4755951dc3bea96ca89"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/ccache.exe", "ccache"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ccache --version 2>&1")
  end
end
