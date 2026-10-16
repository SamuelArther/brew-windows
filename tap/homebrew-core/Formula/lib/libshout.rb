class Libshout < Formula
  desc "Data and connectivity library for the Icecast server"
  homepage "https://icecast.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libshout-2.4.6-6-any.pkg.tar.zst"
  sha256 "21a175c3d3a7d5538015218771c6680ac2846d48c13ff194d33dac8dc921bdee"
  license "LGPL-2.0-or-later"
  revision 4

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://ftp.osuosl.org/pub/xiph/releases/libshout/?C=M&O=D"
    regex(%r{href=(?:["']?|.*?/)libshout[._-]v?(\d+(?:\.\d+)+)\.t}i)
  end

  resource "mingw-w64-ucrt-x86_64-libogg" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libogg-1.3.6-1-any.pkg.tar.zst"
    sha256 "110a34239e8f122da15e041b2601e2f3c6d4c909d738bdf978700601e0f64449"
  end

  resource "mingw-w64-ucrt-x86_64-libpng" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libpng-1.6.59-1-any.pkg.tar.zst"
    sha256 "7f8448e06eceba8f8ce130390addea49f767bdf8f6b7e7eede8ca6d595e3d3a8"
  end

  resource "mingw-w64-ucrt-x86_64-libtheora" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libtheora-1.2.0-1-any.pkg.tar.zst"
    sha256 "7c5d7caadc7da0b293ccb3eb8ed9d00f335fd62527a16cfd2a4fddca43d8de11"
  end

  resource "mingw-w64-ucrt-x86_64-libvorbis" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libvorbis-1.3.7-3-any.pkg.tar.zst"
    sha256 "d013d2c7489b175299f9ea8bd9b70cf456337943b0f13e6eb470beb6c3fc251f"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  resource "mingw-w64-ucrt-x86_64-openssl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openssl-3.6.5-1-any.pkg.tar.zst"
    sha256 "773e021ebad83f14b5c103db467411e8ff064663e1c7e7f730e2be72f4117064"
  end

  resource "mingw-w64-ucrt-x86_64-speex" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-speex-1.2.1-1-any.pkg.tar.zst"
    sha256 "bed78c12555879a6463a6585b01fea72746143c0e98d7015ae99b8e9bb8daba4"
  end

  resource "mingw-w64-ucrt-x86_64-speexdsp" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-speexdsp-1.2.1-2-any.pkg.tar.zst"
    sha256 "397f3e3b7922cc7016c965ba42a67c22372ab8e0c4c144eddd26092a562acbf8"
  end

  resource "mingw-w64-ucrt-x86_64-zlib" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zlib-1.3.2-2-any.pkg.tar.zst"
    sha256 "841401182976d2f9e17e5c0ebaac51f2a8014140ea53d67625e91c8fb3c85ea0"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/shout.exe", "shout"
  end

  test do
    assert_path_exists bin/"shout.exe"
  end
end
