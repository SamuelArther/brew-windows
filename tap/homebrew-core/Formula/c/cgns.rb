class Cgns < Formula
  desc "CFD General Notation System"
  homepage "https://cgns.github.io/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cgns-4.5.2-1-any.pkg.tar.zst"
  sha256 "0350a417a6914fd7e8af39b03403c12f7fc5170e4972cfe490bb19cef94ae15c"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/CGNS/CGNS/archive/refs/tags/v4.5.2.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  resource "mingw-w64-ucrt-x86_64-aws-c-auth" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-auth-1.0.0-1-any.pkg.tar.zst"
    sha256 "465d9e496d69b57b2a19e966fe60832f503a37fdff8ea89743fa09cccf2bc578"
  end

  resource "mingw-w64-ucrt-x86_64-aws-c-cal" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-cal-1.0.0-1-any.pkg.tar.zst"
    sha256 "07f61cf307c232ef7bef0a132fec05711c8d1facfecb161d6f570a243aeb91c9"
  end

  resource "mingw-w64-ucrt-x86_64-aws-c-common" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-common-1.0.2-1-any.pkg.tar.zst"
    sha256 "d7312c06681a0a7762f92ea0dedfdbb16efe4a9ac20ea2e43d095deca7f2a0c5"
  end

  resource "mingw-w64-ucrt-x86_64-aws-c-compression" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-compression-1.0.0-1-any.pkg.tar.zst"
    sha256 "2516f6ab54d19c25e698a715e74f677a0c60379d6d31e7ab3e44132ddb6f7d57"
  end

  resource "mingw-w64-ucrt-x86_64-aws-c-http" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-http-1.0.0-1-any.pkg.tar.zst"
    sha256 "51a61a36fb65283bf3b159d1cf49a47114955d0d8ee2f05d8f253579d5bf0fd4"
  end

  resource "mingw-w64-ucrt-x86_64-aws-c-io" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-io-1.0.0-1-any.pkg.tar.zst"
    sha256 "f44695d9b9a4592e570d362f65d82f516f92a5e375788afd068e02cf80e7f082"
  end

  resource "mingw-w64-ucrt-x86_64-aws-c-s3" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-s3-1.3.0-1-any.pkg.tar.zst"
    sha256 "5039dae6f4f360fb87196b077a202118b10b6a70b45418a306ac141ad72541e7"
  end

  resource "mingw-w64-ucrt-x86_64-aws-c-sdkutils" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-sdkutils-1.0.0-1-any.pkg.tar.zst"
    sha256 "c5d0ab458a3891460d83ce1546511a965b44424c619779dd4e937c9bf9d08c3d"
  end

  resource "mingw-w64-ucrt-x86_64-aws-checksums" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-checksums-1.0.1-1-any.pkg.tar.zst"
    sha256 "339c11e15703e955ad86cb4bd69d1d4fcbfeb93d89daee90107f8c272a17c374"
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-hdf5" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-hdf5-2.2.0-2-any.pkg.tar.zst"
    sha256 "ec34643003bdc6ae7e8416ab5549afb90c960bdc601fab7fdcfec1ee04f8e8ba"
  end

  resource "mingw-w64-ucrt-x86_64-libaec" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libaec-1.1.7-2-any.pkg.tar.zst"
    sha256 "b30e0fce8bff0f782e18e216298bf8f27d9dc3f5cf49b7f319b4e8a359658572"
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

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  resource "mingw-w64-ucrt-x86_64-zlib-ng" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zlib-ng-2.3.3-2-any.pkg.tar.zst"
    sha256 "1d7dda169b6833a8f5d6cbbd2c630e2b88165fa47ed3de709b732fa3259023b7"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/cgnscheck.exe", "cgnscheck"
    msys2_bin "ucrt64/bin/cgnscompress.exe", "cgnscompress"
    msys2_bin "ucrt64/bin/cgnsconvert.exe", "cgnsconvert"
    msys2_bin "ucrt64/bin/cgnsdiff.exe", "cgnsdiff"
    msys2_bin "ucrt64/bin/cgnslist.exe", "cgnslist"
    msys2_bin "ucrt64/bin/cgnsnames.exe", "cgnsnames"
  end

  test do
    assert_path_exists bin/"cgnscheck.exe"
  end
end
