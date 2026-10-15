class Librime < Formula
  desc "Rime Input Method Engine"
  homepage "https://rime.im"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-librime-1.17.0-2-any.pkg.tar.zst"
  sha256 "221c70b768c30da620789e1951c8571187ea4da269a666eed36bafa5ecab6b1c"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/rime/librime.git"
  end

  resource "mingw-w64-ucrt-x86_64-capnproto" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-capnproto-1.4.0-3-any.pkg.tar.zst"
    sha256 "7299dfaf259a6bcc2d81b8db00e741b292ba3971ba741521457c5ca34b34dd57"
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-gflags" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gflags-2.3.0-1-any.pkg.tar.zst"
    sha256 "ad16b6b48d3f10edd880297dfcdaa7efbf17c3cfffd6e72d1e7a9c69d6259b5b"
  end

  resource "mingw-w64-ucrt-x86_64-glog" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-glog-0.7.1-10-any.pkg.tar.zst"
    sha256 "eafd5d29a59890a5f102ece7749b2631c310910fc216752be6b20286754cd5f7"
  end

  resource "mingw-w64-ucrt-x86_64-leveldb" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-leveldb-1.23-3-any.pkg.tar.zst"
    sha256 "c684a12490c5f8fcbcf2875c5816d91c6a1016d9ce2be4d4ca75200b919055dd"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libstdc++" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libstdc++-16.2.0-6-any.pkg.tar.zst"
    sha256 "33c5e58a85a538cf843a8672991cce591f0231d66da3b66a57629712d64be17f"
  end

  resource "mingw-w64-ucrt-x86_64-libunwind" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libunwind-23.1.3-1-any.pkg.tar.zst"
    sha256 "62156b01dd43cf41fe868b3c62fcfab93ed50da2a595ea73373675eb635ab6d4"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  resource "mingw-w64-ucrt-x86_64-lua" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-lua-5.5.1-1-any.pkg.tar.zst"
    sha256 "0365515318bd4c2027a8e86970d3b2bba8b4257254d1041d960bca165a43843e"
  end

  resource "mingw-w64-ucrt-x86_64-marisa" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-marisa-0.2.7-1-any.pkg.tar.zst"
    sha256 "60f1c815fc31a098fee0fc01cbb36b3926fc1184c4c25b33fa996e6f68d889a1"
  end

  resource "mingw-w64-ucrt-x86_64-opencc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-opencc-1.3.1-1-any.pkg.tar.zst"
    sha256 "4cdee6cfcb007b547802c0a137084f9dccf60afdd87266089176f313cbad740e"
  end

  resource "mingw-w64-ucrt-x86_64-openssl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openssl-3.6.5-1-any.pkg.tar.zst"
    sha256 "773e021ebad83f14b5c103db467411e8ff064663e1c7e7f730e2be72f4117064"
  end

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  resource "mingw-w64-ucrt-x86_64-yaml-cpp" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-yaml-cpp-0.9.0-1-any.pkg.tar.zst"
    sha256 "4c4240f5590115ace50e3a39259a51f671dabbdf3138d13d6dc72885f0393a85"
  end

  resource "mingw-w64-ucrt-x86_64-zlib" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zlib-1.3.2-2-any.pkg.tar.zst"
    sha256 "841401182976d2f9e17e5c0ebaac51f2a8014140ea53d67625e91c8fb3c85ea0"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/rime_deployer.exe", "rime_deployer"
    msys2_bin "ucrt64/bin/rime_dict_manager.exe", "rime_dict_manager"
    msys2_bin "ucrt64/bin/rime_patch.exe", "rime_patch"
  end

  test do
    assert_path_exists bin/"rime_deployer.exe"
  end
end
