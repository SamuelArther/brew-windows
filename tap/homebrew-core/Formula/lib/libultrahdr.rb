class Libultrahdr < Formula
  desc "Reference codec for the Ultra HDR format"
  homepage "https://developer.android.com/media/platform/hdr-image-format"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libultrahdr-1.5.1-1-any.pkg.tar.zst"
  sha256 "adb0e5b334fb9e41fbc2135438c350c57fea262661cd23370e9a6a2eb1bab1cc"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/google/libultrahdr/archive/refs/tags/v2.0.2.tar.gz"
  end

  resource "mingw-w64-ucrt-x86_64-angleproject" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-angleproject-2.1.r25748.890b5d8f-6-any.pkg.tar.zst"
    sha256 "3c184061a3a3ef6478055e11eb786e20c40e62c1c239a1d65bf1399b7e2e7113"
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-egl-headers" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-egl-headers-1.5.r284.3ae2b7c-1-any.pkg.tar.zst"
    sha256 "29743159048cf216f75d0dc3eb3e0a04a07515db17d839803e54b060f5aeb0b3"
  end

  resource "mingw-w64-ucrt-x86_64-gles-headers" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gles-headers-3.2.r1065.7fc154c-1-any.pkg.tar.zst"
    sha256 "066007a50bd08f062b5494f2c4811a219e1a65f4ecdce2beb453f6d9d8b92098"
  end

  resource "mingw-w64-ucrt-x86_64-jsoncpp" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-jsoncpp-1.9.8-1-any.pkg.tar.zst"
    sha256 "c8bf6af2e6d7972f748f738ba8e510777437dcfd50846c378bc6e0990ce7ce2d"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
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

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  resource "mingw-w64-ucrt-x86_64-rapidjson" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-rapidjson-1.1.0-8-any.pkg.tar.zst"
    sha256 "e25d28edfbdb3db5b6873538664032618ab52ed5244a87bffbf2a5ca3fd23e69"
  end

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  resource "mingw-w64-ucrt-x86_64-zlib" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zlib-1.3.2-2-any.pkg.tar.zst"
    sha256 "841401182976d2f9e17e5c0ebaac51f2a8014140ea53d67625e91c8fb3c85ea0"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/ultrahdr_app.exe", "ultrahdr_app"
  end

  test do
    assert_path_exists bin/"ultrahdr_app.exe"
  end
end
