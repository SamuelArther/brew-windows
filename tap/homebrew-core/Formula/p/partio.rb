class Partio < Formula
  desc "Particle library for 3D graphics"
  homepage "https://www.disneyanimation.com/open-source/partio/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-partio-1.19.0-4-any.pkg.tar.zst"
  sha256 "db2c13ad3371dbd5fbbb0e3f227f7142d6dd2b2945d3e68ae767c83aa8e9372e"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/wdas/partio/archive/refs/tags/v1.20.0.tar.gz"
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-freeglut" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-freeglut-3.8.0-1-any.pkg.tar.zst"
    sha256 "b9b9212f0b7e5ba42709e153143efac410765bde9fd1ad9991bc025bf1567805"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
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

  def install
    msys2_install
    msys2_bin "ucrt64/bin/partattr.exe", "partattr"
    msys2_bin "ucrt64/bin/partconvert.exe", "partconvert"
    msys2_bin "ucrt64/bin/partinfo.exe", "partinfo"
    msys2_bin "ucrt64/bin/partview.exe", "partview"
  end

  test do
    assert_path_exists bin/"partattr.exe"
  end
end
